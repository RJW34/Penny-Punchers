using System.Security.Cryptography;
using System.Text.Json;
using StrikeLedger.Core;
namespace StrikeLedger.App;
public sealed record LobbySelection(int Seat,int Revision,string Fighter,string Stage);
public sealed record LobbyReady(string ConfigurationHash);
public sealed record PauseControl(int Round,string Stage,long Tick,string Hash="");
public sealed partial class PrivateMatchPeer
{
    readonly bool negotiate;readonly string connectionSession;
    LobbySelection? localSelection,remoteSelection;string? ownReady,remoteReady;int revision;
    public LobbySelection? LocalSelection=>localSelection;
    public LobbySelection? RemoteSelection=>remoteSelection;
    public bool LocalReady=>ownReady is not null;
    public bool RemoteReady=>remoteReady is not null;
    public string LobbyStage=>Rollback.LocalSeat==0?localSelection?.Stage??config.StageId:remoteSelection?.Stage??config.StageId;
    public void SelectLineup(string fighter,string? stage=null)
    {
        if(Status!=PeerStatus.Lobby)throw new InvalidOperationException("Lineup changes require the lobby");
        var selection=new LobbySelection(Rollback.LocalSeat,++revision,fighter,Rollback.LocalSeat==0?stage??LobbyStage:"");ValidateSelection(selection);
        localSelection=selection;ownReady=null;remoteReady=null;Send(PacketKind.Selection,selection);
    }
    void PublishSelection()=>SelectLineup(Rollback.LocalSeat==0?config.Fighter0:config.Fighter1,config.StageId);
    void ValidateSelection(LobbySelection selection)
    {
        if(selection.Fighter is null||selection.Stage is null||selection.Fighter.Length>80||selection.Stage.Length>80||selection.Revision<1||selection.Revision>10000||!Simulation.Content.Fighters.TryGetValue(selection.Fighter,out _))throw new InvalidDataException("Invalid lobby fighter");
        if(selection.Seat==0&&!Simulation.Content.Stages.ContainsKey(selection.Stage))throw new InvalidDataException("Invalid host stage");
        if(selection.Seat==1&&selection.Stage.Length>0)throw new InvalidDataException("Only host selects the stage");
    }
    MatchConfig LobbyConfig()
    {
        if(localSelection==null||remoteSelection==null)throw new InvalidOperationException("Waiting for both seat selections");
        var a=Rollback.LocalSeat==0?localSelection:remoteSelection;var b=Rollback.LocalSeat==1?localSelection:remoteSelection;
        return new(){Fighter0=a.Fighter,Fighter1=b.Fighter,StageId=a.Stage,SessionId=connectionSession};
    }
    string LobbyHash()=>Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new{configuration=LobbyConfig(),hostRevision=Rollback.LocalSeat==0?localSelection!.Revision:remoteSelection!.Revision,joinRevision=Rollback.LocalSeat==1?localSelection!.Revision:remoteSelection!.Revision},ReplayFormat.Json)));
    public void ReadyLineup(){if(Status!=PeerStatus.Lobby)throw new InvalidOperationException("Not in a lobby");ownReady=LobbyHash();Send(PacketKind.Ready,new LobbyReady(ownReady));ProgressLobby();}
    void ReceiveSelection(LobbySelection selection)
    {
        if(Status!=PeerStatus.Lobby||selection.Seat!=Rollback.RemoteSeat)throw new InvalidDataException("Seat cannot edit its opponent's lineup");ValidateSelection(selection);
        if(remoteSelection is not null&&selection.Revision<=remoteSelection.Revision)throw new InvalidDataException("Lobby revision did not increase");
        remoteSelection=selection;ownReady=null;remoteReady=null;
    }
    void ReceiveReady(LobbyReady ready)
    {
        if(Status!=PeerStatus.Lobby)throw new InvalidDataException("Ready outside lobby");
        if(ready.ConfigurationHash is null||ready.ConfigurationHash.Length!=64)throw new InvalidDataException("Malformed ready hash");
        if(localSelection!=null&&remoteSelection!=null&&ready.ConfigurationHash==LobbyHash())remoteReady=ready.ConfigurationHash;
    }
    void ProgressLobby()
    {
        if(Status!=PeerStatus.Lobby||ownReady==null||remoteReady!=ownReady||ownReady!=LobbyHash())return;
        config=LobbyConfig();ResetSimulations();Change(PeerStatus.Preparation,"Both lineups ready. Lock a preparation plan.");
    }
    void ResetSimulations()
    {
        lastValidDraft=new([]);var content=Simulation.Content;int seat=Rollback.LocalSeat;Simulation=new(content,config);Rollback=new(Simulation,seat);replaySimulation=new(content,config);recorder=new(content,config);
        ownCommit=null;otherCommit=null;ownReveal=null;otherReveal=null;otherStart=null;otherSettlement=null;committed=false;started=false;settled=false;sentSettlement=false;revealSent=false;
        remoteHashes.Clear();sentHashes.Clear();confirmedRounds.Clear();pauseTarget=-1;otherPauseHash=null;sentPauseReady=false;sentResume=false;receivedResume=false;resumeCountdown=0;
    }
    long pauseTarget=-1;string? otherPauseHash;bool sentPauseReady,sentResume,receivedResume;int resumeCountdown;
    public long PauseTick=>pauseTarget;public int ResumeCountdownTicks=>resumeCountdown;
    public void Pause()
    {
        if(Status!=PeerStatus.Playing)return;
        Change(PeerStatus.Pausing,"Negotiating a shared pause tick");
        if(Rollback.LocalSeat==0)ChoosePauseTarget(Simulation.Tick);else Send(PacketKind.Pause,new PauseControl(round,"request",Simulation.Tick));
    }
    void ChoosePauseTarget(long requesterTick)
    {
        pauseTarget=Math.Max(Simulation.Tick,requesterTick)+Rollback.InputDelay+Rollback.PredictionWindow+4;
        sentPauseReady=false;otherPauseHash=null;Change(PeerStatus.Pausing,$"Pausing together at tick {pauseTarget}");Send(PacketKind.Pause,new PauseControl(round,"target",pauseTarget));
    }
    void ReceivePause(PauseControl control)
    {
        if(control.Round<round)return;if(control.Round!=round||control.Tick<0||control.Tick>Simulation.Tick+240)throw new InvalidDataException("Invalid pause barrier");
        if(Status is not (PeerStatus.Playing or PeerStatus.Pausing or PeerStatus.Paused))return;
        if(control.Stage=="request")
        {if(Rollback.LocalSeat!=0)throw new InvalidDataException("Only host coordinates pause target");if(Status!=PeerStatus.Paused)ChoosePauseTarget(control.Tick);}
        else if(control.Stage=="target")
        {
            if(Rollback.LocalSeat!=1)throw new InvalidDataException("Forged host pause target");
            if(control.Tick<Simulation.Tick){Send(PacketKind.Pause,new PauseControl(round,"request",Simulation.Tick));return;}
            pauseTarget=control.Tick;otherPauseHash=null;sentPauseReady=false;Change(PeerStatus.Pausing,$"Pausing together at tick {pauseTarget}");
        }
        else if(control.Stage=="ready")
        {if(control.Tick!=pauseTarget)return;if(control.Hash is null||control.Hash.Length!=64)throw new InvalidDataException("Malformed pause hash");otherPauseHash=control.Hash;}
        else throw new InvalidDataException("Unknown pause barrier stage");
        ProgressPause();
    }
    void ProgressPause()
    {
        if(Status!=PeerStatus.Pausing||pauseTarget<0||Simulation.Tick!=pauseTarget||Rollback.ConfirmedThroughTick<pauseTarget-1)return;
        string hash=Simulation.Hash();if(!sentPauseReady){sentPauseReady=true;Send(PacketKind.Pause,new PauseControl(round,"ready",pauseTarget,hash));}
        if(otherPauseHash==null)return;if(otherPauseHash!=hash){Abort("Pause barrier state mismatch");return;}
        Change(PeerStatus.Paused,$"Both peers paused at confirmed tick {pauseTarget}");
    }
    public void Resume()
    {
        if(Status!=PeerStatus.Paused)return;
        if(!sentResume){Send(PacketKind.Resume,new RoundControl(round,Simulation.Hash()));sentResume=true;}
        FinishResume();
    }
    void ReceiveResume(RoundControl control)
    {
        if(control.Round<round)return;if(control.Round!=round||Status is not(PeerStatus.Paused or PeerStatus.Pausing)||control.Hash!=Simulation.Hash())throw new InvalidDataException("Resume requires the shared paused state");
        receivedResume=true;if(Status==PeerStatus.Paused)Resume();
    }
    void FinishResume()
    {
        if(!sentResume||!receivedResume)return;pauseTarget=-1;sentPauseReady=false;otherPauseHash=null;sentResume=false;receivedResume=false;resumeCountdown=120;
        Change(PeerStatus.Playing,"Resume countdown: two seconds of neutral input, then fight");
    }
    bool ownRematch,otherRematch;int matchGeneration;
    public bool RematchRequested=>ownRematch;
    public void RequestRematch()
    {
        if(Status!=PeerStatus.MatchOver)throw new InvalidOperationException("Rematch requires a confirmed completed match");
        if(!ownRematch){ownRematch=true;Send(PacketKind.Rematch,new RoundControl(round,Simulation.Hash()));}FinishRematch();
    }
    void ReceiveRematch(RoundControl control)
    {
        if(control.Round<round)return;if(control.Round!=round||Status!=PeerStatus.MatchOver||control.Hash!=Simulation.Hash())throw new InvalidDataException("Rematch requires matching completed state");otherRematch=true;FinishRematch();
    }
    void FinishRematch()
    {
        if(!ownRematch||!otherRematch)return;matchGeneration++;round+=100;config=config with{SessionId=connectionSession+"/rematch/"+matchGeneration};ResetSimulations();ownRematch=false;otherRematch=false;
        Change(PeerStatus.Preparation,"Mutual rematch ready. Fresh wallets; connection retained.");
    }
}
