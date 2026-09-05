using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using StrikeLedger.Core;

namespace StrikeLedger.App;

public enum PeerStatus { Connecting, Preparation, Reveal, Playing, AwaitingSettlement, RoundResult, MatchOver, Paused, Aborted, Disconnected }
public sealed record PeerHello(int Protocol,string Build,string ContentHash,string Session,int Seat,MatchConfig Config);
public sealed record PlanCommit(int Round,int Version,string Hash);
public sealed record PlanReveal(int Round,int Version,PreparationPlan Plan,string Nonce);
public sealed record StateHash(long Tick,string Hash,int Round);
public sealed record RoundControl(int Round,string Hash);
public sealed record ConfirmedPlayerEconomy(int Credits,int ScoreHalfPoints,int RecoveryTier,SpendReceipt[] Receipts);
public sealed record ConfirmedNetworkRound(int Round,string Hash,ConfirmedPlayerEconomy[] Players,SettlementReceipt Settlement);
public sealed class PrivateMatchPeer : IDisposable
{
    readonly UdpTransport transport;
    readonly MatchConfig config;
    readonly Simulation replaySimulation;
    readonly ReplayRecorder recorder;
    readonly Dictionary<long,string> remoteHashes=new();
    readonly HashSet<long> sentHashes=new();
    PlanCommit? ownCommit,otherCommit;
    PlanReveal? ownReveal,otherReveal;
    RoundControl? otherStart,otherSettlement;
    bool hello,committed,started,settled,sentSettlement,revealSent;
    long phaseSince=Environment.TickCount64;
    long lastKeepalive;
    int round=1;
    readonly List<ConfirmedNetworkRound> confirmedRounds=new();
    public IReadOnlyList<ConfirmedNetworkRound> ConfirmedRounds=>confirmedRounds.AsReadOnly();
    public Simulation Simulation {get;}
    public RollbackSession Rollback {get;}
    public PeerStatus Status {get;private set;}=PeerStatus.Connecting;
    public string Diagnostic {get;private set;}="Connecting to private peer";
    public string? DesyncDump {get;private set;}
    public int Round=>round;
    public int MalformedPackets=>transport.MalformedPackets;
    public int Retransmissions=>transport.Retransmissions;
    public event Action<string>? Notice;
    public PrivateMatchPeer(GameContent content,MatchConfig matchConfig,int localSeat,IPEndPoint bind,IPEndPoint remote,FaultProfile? faults=null)
    {
        if(matchConfig.Training || matchConfig.Assist)throw new ArgumentException("Training/assist state cannot enter competitive network session");
        config=matchConfig;Simulation=new(content,config);Rollback=new(Simulation,localSeat);replaySimulation=new(content,config);recorder=new(content,config);
        transport=new(bind,remote,config.SessionId,faults);transport.Packet+=Receive;
        Send(PacketKind.Hello,new PeerHello(1,ReplayFormat.Build,content.ContentHash,config.SessionId,localSeat,config));
    }
    void Send<T>(PacketKind kind,T value,bool reliable=true)=>transport.Send(kind,JsonSerializer.SerializeToUtf8Bytes(value,ReplayFormat.Json),reliable);
    static T Read<T>(byte[] payload)=>JsonSerializer.Deserialize<T>(payload,ReplayFormat.Json)??throw new InvalidDataException("Empty control payload");
    void Change(PeerStatus status,string diagnostic){Status=status;Diagnostic=diagnostic;phaseSince=Environment.TickCount64;Notice?.Invoke(diagnostic);}
    void Abort(string reason)
    {
        if(Status==PeerStatus.Aborted)return;
        DesyncDump=JsonSerializer.Serialize(new{reason,build=ReplayFormat.Build,content=Simulation.Content.ContentHash,round,tick=Simulation.Tick,hash=Simulation.Hash(),confirmed=Rollback.ConfirmedThroughTick,inputs=Rollback.RecentLocalInputs(),snapshot=Convert.ToBase64String(Simulation.Capture().Bytes)});
        Change(PeerStatus.Aborted,reason);
    }
    public void SubmitPreparation(PreparationPlan plan)
    {
        if(Status!=PeerStatus.Preparation || ownCommit is not null)throw new InvalidOperationException("Preparation is not accepting a new plan");
        ValidatePlan(plan);
        var preflight=new Simulation(Simulation.Content,config);preflight.Restore(Simulation.Capture());
        preflight.CommitPreparation(Rollback.LocalSeat==0?plan:new([]),Rollback.LocalSeat==1?plan:new([]));
        string nonce=Convert.ToHexString(RandomNumberGenerator.GetBytes(24)).ToLowerInvariant();
        ownReveal=new(round,1,new(plan.ItemIds.Order(StringComparer.Ordinal).ToArray(),plan.ReserveFloor),nonce);
        ownCommit=new(round,1,CommitHash(ownReveal));Send(PacketKind.Commit,ownCommit);Progress();
    }
    static void ValidatePlan(PreparationPlan plan)
    {if(plan is null || plan.ItemIds is null || plan.ItemIds.Length>3 || plan.ItemIds.Any(x=>x is null || x.Length is <1 or >64) || plan.ReserveFloor is <0 or >3600)throw new InvalidDataException("Malformed preparation plan");}
    static string CommitHash(PlanReveal reveal)=>Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(reveal,ReplayFormat.Json))).ToLowerInvariant();
    public void Poll()
    {
        if(Status is PeerStatus.Aborted or PeerStatus.Disconnected)return;
        try
        {
            if(Environment.TickCount64-lastKeepalive>=1000){transport.Send(PacketKind.Ping,Array.Empty<byte>(),false);lastKeepalive=Environment.TickCount64;}
            transport.Poll();if(Status is PeerStatus.Aborted or PeerStatus.Disconnected)return;Progress();
            if(Environment.TickCount64-transport.LastReceiveMilliseconds>15000)Change(PeerStatus.Disconnected,"Private peer timed out; no predicted payout applied");
            if(Status==PeerStatus.Connecting&&Environment.TickCount64-phaseSince>15000)Abort("Compatible private handshake deadline expired");
            if(Status==PeerStatus.Preparation && Environment.TickCount64-phaseSince>15000 && ownCommit is null)SubmitPreparation(new([]));
            if(Status is PeerStatus.Preparation or PeerStatus.Reveal && Environment.TickCount64-phaseSince>30000)Abort("Preparation commitment/reveal deadline expired");
            if(Status is PeerStatus.Playing or PeerStatus.AwaitingSettlement)
            {
                SendInputs();RecordConfirmedInputs();
                foreach(long t in remoteHashes.Keys.ToArray())
                {string? own=Rollback.HashAfter(t);if(own is null)continue;if(!string.Equals(own,remoteHashes[t],StringComparison.OrdinalIgnoreCase)){Abort($"Confirmed state desync at tick {t}; no automatic economy mutation attempted");return;}remoteHashes.Remove(t);}
                long confirmed=Rollback.ConfirmedThroughTick;
                long latest=(confirmed+1)/60*60-1;
                if(latest>=0 && !sentHashes.Contains(latest))
                {string? hash=Rollback.HashAfter(latest);if(hash is not null){Send(PacketKind.Hash,new StateHash(latest,hash,round));sentHashes.Add(latest);}}
                TrySettlement();
            }
        }
        catch(Exception ex)when(ex is JsonException or InvalidDataException or InvalidOperationException or IOException or ArgumentException){Abort(ex.Message);}
    }
    public bool Advance(byte direction,Buttons held)
    {
        if(Status!=PeerStatus.Playing || Simulation.Phase==MatchPhase.PendingResult)return false;
        try{bool advanced=Rollback.Advance(direction,held);SendInputs();TrySettlement();return advanced;}
        catch(Exception ex)when(ex is InvalidDataException or InvalidOperationException or ArgumentException){Abort(ex.Message);return false;}
    }
    void SendInputs()
    {
        var frames=Rollback.RecentLocalInputs(32);
        using var stream=new MemoryStream();using var w=new BinaryWriter(stream);
        w.Write(round);w.Write((byte)Rollback.LocalSeat);w.Write((byte)frames.Count);
        foreach(var input in frames){w.Write(input.Frame);w.Write(input.Direction);w.Write((byte)input.Held);}
        transport.Send(PacketKind.Inputs,stream.ToArray(),false);
    }
    void Receive(ReceivedPacket packet)
    {
        if(Status is PeerStatus.Aborted or PeerStatus.Disconnected)return;
        try
        {
            if(packet.Kind==PacketKind.Ping){if(packet.Payload.Length!=0)throw new InvalidDataException("Malformed keepalive");return;}
            if(packet.Kind==PacketKind.Hello)
            {
                var h=Read<PeerHello>(packet.Payload);
                if(h.Protocol!=1 || h.Build!=ReplayFormat.Build || h.ContentHash!=Simulation.Content.ContentHash || h.Session!=config.SessionId || h.Seat!=Rollback.RemoteSeat || h.Config!=config)throw new InvalidDataException("Private peer protocol, build, content, configuration or seat mismatch");
                if(!hello){hello=true;Change(PeerStatus.Preparation,"Peer verified. Lock a preparation plan.");}return;
            }
            if(!hello)throw new InvalidDataException("Control arrived before verified handshake");
            switch(packet.Kind)
            {
                case PacketKind.Commit:
                    var commit=Read<PlanCommit>(packet.Payload);if(commit.Round<round)return;
                    if(commit.Round!=round || commit.Version!=1 || commit.Hash is null || commit.Hash.Length!=64 || otherCommit is not null && otherCommit!=commit)throw new InvalidDataException("Invalid or changed preparation commitment");otherCommit=commit;break;
                case PacketKind.Reveal:
                    var reveal=Read<PlanReveal>(packet.Payload);if(reveal.Round<round)return;
                    if(reveal.Round!=round || reveal.Version!=1 || reveal.Nonce is null || reveal.Nonce.Length!=48 || otherCommit is null)throw new InvalidDataException("Reveal without valid commitment");ValidatePlan(reveal.Plan);
                    if(!CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(CommitHash(reveal)),Encoding.ASCII.GetBytes(otherCommit.Hash)))throw new InvalidDataException("Preparation reveal does not match commitment");
                    otherReveal=reveal;break;
                case PacketKind.Start:
                    var start=Read<RoundControl>(packet.Payload);if(start.Round<round)return;if(start.Round!=round)throw new InvalidDataException("Future round start");otherStart=start;break;
                case PacketKind.Inputs:
                    using(var r=new BinaryReader(new MemoryStream(packet.Payload)))
                    {
                        if(packet.Payload.Length<6)throw new InvalidDataException("Short input packet");int packetRound=r.ReadInt32();int seat=r.ReadByte(),count=r.ReadByte();
                        if(seat!=Rollback.RemoteSeat || count>64 || packet.Payload.Length!=6+count*10)throw new InvalidDataException("Malformed or forged-seat input packet");
                        if(packetRound<round)return;if(packetRound>round)throw new InvalidDataException("Future-round input injection");
                        for(int i=0;i<count;i++){var input=new InputFrame(seat,r.ReadInt64(),r.ReadByte(),(Buttons)r.ReadByte());if(started)Rollback.SubmitRemote(input);}
                    }break;
                case PacketKind.Hash:
                    var h=Read<StateHash>(packet.Payload);if(h.Round<round)return;if(h.Round!=round || h.Tick<0 || h.Tick>Simulation.Tick+64 || h.Hash is null || h.Hash.Length!=64 || remoteHashes.Count>240)throw new InvalidDataException("Invalid state-hash checkpoint");remoteHashes[h.Tick]=h.Hash;break;
                case PacketKind.Settlement:
                    var settlement=Read<RoundControl>(packet.Payload);if(settlement.Round<round)return;if(settlement.Round!=round)throw new InvalidDataException("Future settlement");otherSettlement=settlement;TrySettlement();break;
                case PacketKind.NextRound:
                    var next=Read<RoundControl>(packet.Payload);if(next.Round<round)return;if(next.Round!=round || next.Hash!=Simulation.Hash() || !settled)throw new InvalidDataException("Next round without matching settled state");RestartRound();break;
                case PacketKind.Pause:
                    if(Read<RoundControl>(packet.Payload).Round!=round)throw new InvalidDataException("Pause for incorrect round");
                    if(Status==PeerStatus.Playing)Change(PeerStatus.Paused,"Private match paused by peer");break;
                case PacketKind.Resume:
                    if(Read<RoundControl>(packet.Payload).Round!=round)throw new InvalidDataException("Resume for incorrect round");
                    if(Status==PeerStatus.Paused)Change(PeerStatus.Playing,"Private match resumed");break;
                case PacketKind.Disconnect:Change(PeerStatus.Disconnected,"Peer disconnected; incomplete round has no payout");break;
                default:throw new InvalidDataException("Unexpected private control kind");
            }
            Progress();
        }
        catch(Exception ex)when(ex is JsonException or InvalidDataException or InvalidOperationException or ArgumentException or EndOfStreamException){Abort(ex.Message);}
    }
    void Progress()
    {
        if(Status is PeerStatus.Aborted or PeerStatus.Disconnected)return;
        if(ownCommit is not null && otherCommit is not null && !revealSent){Send(PacketKind.Reveal,ownReveal!);revealSent=true;Change(PeerStatus.Reveal,"Both plans committed. Validating simultaneous reveal.");}
        if(ownReveal is not null && otherReveal is not null && !committed)
        {
            var a=Rollback.LocalSeat==0?ownReveal.Plan:otherReveal.Plan;var b=Rollback.LocalSeat==1?ownReveal.Plan:otherReveal.Plan;
            Simulation.CommitPreparation(a,b,$"{config.SessionId}:prep:{round}");recorder.CommitPreparation(replaySimulation,a,b,$"{config.SessionId}:prep:{round}");committed=true;Send(PacketKind.Start,new RoundControl(round,Simulation.Hash()));
        }
        if(committed && otherStart is not null && !started)
        {
            if(otherStart.Hash!=Simulation.Hash()){Abort("Preparation state hash mismatch");return;}
            Rollback.ResetTimeline();started=true;Change(PeerStatus.Playing,"Plans revealed. Deterministic reveal/countdown begins.");
        }
    }
    void TrySettlement()
    {
        if(Status is PeerStatus.Aborted or PeerStatus.Disconnected)return;
        if(settled || Simulation.Phase!=MatchPhase.PendingResult || Simulation.PendingResult is not {} terminal || Rollback.ConfirmedThroughTick<terminal.TerminalTick)return;
        string hash=Simulation.Hash();
        if(!sentSettlement){Send(PacketKind.Settlement,new RoundControl(round,hash));sentSettlement=true;Change(PeerStatus.AwaitingSettlement,"Terminal input confirmed; matching peer terminal state before payout");}
        if(otherSettlement is null)return;
        if(otherSettlement.Hash!=hash){Abort("Terminal state hash mismatch; settlement withheld");return;}
        RecordConfirmedInputs();
        if(replaySimulation.Hash()!=hash){Abort("Confirmed input recording diverged before settlement");return;}
        var receipt=Simulation.SettleRound(Rollback.ConfirmedThroughTick,$"{config.SessionId}:settle:{round}");settled=true;
        recorder.SettleRound(replaySimulation,Rollback.ConfirmedThroughTick,$"{config.SessionId}:settle:{round}");
        confirmedRounds.Add(new(round,Simulation.Hash(),Simulation.Players.Select(p=>new ConfirmedPlayerEconomy(p.Credits,p.ScoreHalfPoints,p.RecoveryTier,p.SpendReceipts.ToArray())).ToArray(),receipt));
        Change(Simulation.Phase==MatchPhase.MatchOver?PeerStatus.MatchOver:PeerStatus.RoundResult,"Both terminal states match. Round settlement committed once.");
    }
    public void ContinueMatch()
    {
        if(Rollback.LocalSeat!=0 || Status!=PeerStatus.RoundResult)throw new InvalidOperationException("Only host can continue a settled round");
        Send(PacketKind.NextRound,new RoundControl(round,Simulation.Hash()));RestartRound();
    }
    void RestartRound()
    {
        Simulation.NextRound();recorder.NextRound(replaySimulation);round++;ownCommit=null;otherCommit=null;ownReveal=null;otherReveal=null;otherStart=null;otherSettlement=null;
        committed=false;started=false;settled=false;sentSettlement=false;revealSent=false;remoteHashes.Clear();sentHashes.Clear();Rollback.ResetTimeline();Change(PeerStatus.Preparation,"Next preparation; persistent wallets retained");
    }
    public void Pause(){if(Status!=PeerStatus.Playing)return;Send(PacketKind.Pause,new RoundControl(round,""));Change(PeerStatus.Paused,"Private match paused");}
    public void Resume(){if(Status!=PeerStatus.Paused)return;Send(PacketKind.Resume,new RoundControl(round,""));Change(PeerStatus.Playing,"Private match resumed");}
    void RecordConfirmedInputs()
    {
        if(!started)return;
        while(replaySimulation.Tick<=Rollback.ConfirmedThroughTick && replaySimulation.Phase is not (MatchPhase.PendingResult or MatchPhase.RoundResult or MatchPhase.MatchOver))
        {var inputs=Rollback.ConfirmedInputs(replaySimulation.Tick);recorder.Step(replaySimulation,inputs.Seat0,inputs.Seat1);}
    }
    public void SaveReplay(string path){RecordConfirmedInputs();recorder.Save(path);ReplayFormat.ExportEconomy(recorder.Record,Path.ChangeExtension(path,"economy.json"));}
    public void Disconnect(){Send(PacketKind.Disconnect,new {round});transport.Poll();Change(PeerStatus.Disconnected,"Disconnected");}
    public void Dispose()=>transport.Dispose();
}
