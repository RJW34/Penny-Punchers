using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text.Json;
using StrikeLedger.App;
using StrikeLedger.Core;
internal static class ShopV2AppTests
{
    static void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    static void Reject(Action action,string message){try{action();}catch(Exception e)when(e is InvalidDataException or ArgumentException or InvalidOperationException){return;}throw new InvalidOperationException(message);}
    public static void Run(GameContent content,string output)
    {
        var results=new List<object>();
        void Check(string id,Action test){test();results.Add(new{id,passed=true});Console.WriteLine("PASS shop-v2-app "+id);File.WriteAllText(Path.Combine(output,"shop-v2-app.json"),JsonSerializer.Serialize(new{build=ReplayFormat.Build,contentHash=content.ContentHash,results},new JsonSerializerOptions{WriteIndented=true}));}
        string Product(string fighter,string move)=>content.Items.Values.Single(i=>i.EligibleFighters.Contains(fighter)&&i.MoveId==move).Id;
        Check("quoted_cart_cannot_launder_reserve_stale_content_or_price",()=>
        {
            var sim=new Simulation(content);var recorder=new ReplayRecorder(content,sim.Config);string before=sim.Hash();string id=Product("rook","pulse_ex");
            foreach(var bad in new[]{new PreparationPlan([id],1),new([id]){ContentHash="stale"},new([id]){QuotedCost=1},new([id,id])})
            {Reject(()=>recorder.CommitPreparation(sim,bad,new([])),"Invalid cart was re-quoted into authority");Require(sim.Hash()==before&&recorder.Record.Commands.Count==0,"Rejected cart mutated replay or bank");}
            var quote=PurchasePlanning.QuotedPlan(content,"rook",600,[id]);Require(quote.ContentHash==content.ContentHash&&quote.QuotedCost==600,"Missing quote identity");recorder.CommitPreparation(sim,quote,new([]));Require(sim.Players[0].Credits==0&&sim.Players[0].OwnedEx.Contains("pulse_ex"),"Atomic license purchase failed");
        });
        Check("rollback_retracts_false_prepaid_startup",()=>
        {
            Simulation Setup(){var s=new Simulation(content,new MatchConfig{Training=true,SessionId="v2-false-use"});s.TrainingReset(0);s.SetTrainingLoadout(0,new([Product("rook","super_1")]));s.SetTrainingState(0,x:720000);s.SetTrainingState(1,x:752000);return s;}
            var predicted=Setup();var straight=Setup();var rollback=new RollbackSession(predicted,0,0);var actual=new List<InputFrame>();var timeline=new List<object>();
            for(int t=0;t<8;t++)
            {
                byte direction=(byte)(t switch{0=>2,1=>3,2=>6,3=>2,4=>3,5=>6,_=>5});var own=new InputFrame(0,t,direction,t==5?Buttons.HP:Buttons.None);var other=new InputFrame(1,t,5,t==0?Buttons.LP:Buttons.None);actual.Add(other);
                var step=straight.Step(own,other);Require(rollback.Advance(direction,own.Held),"False-use prediction stalled");timeline.Add(new{t,own,other,step.Events,predicted=predicted.Players[0].SuperUsesRemaining,actual=straight.Players[0].SuperUsesRemaining});
            }
            Require(predicted.Players[0].SuperUsesRemaining==0&&predicted.Players[0].SuperUseReceipts.Count==1,"Fixture did not predict a prepaid startup");Require(straight.Players[0].SuperUsesRemaining==1,"Actual interrupt did not prevent super startup");
            Require(!rollback.DrainPresentationChanges().Any(c=>c.Event.Kind is CombatEventKind.SuperUseConsumed or CombatEventKind.SkillAward),"Unconfirmed monetary/use cue escaped");foreach(var input in actual)rollback.SubmitRemote(input);
            Require(predicted.Hash()==straight.Hash()&&predicted.Players[0].SuperUsesRemaining==1&&predicted.Players[0].SuperUseReceipts.Count==0&&predicted.Players[0].Credits==0,"Rollback failed to restore prepaid entitlement");Require(!rollback.DrainPresentationChanges().Any(c=>c.Event.Kind==CombatEventKind.SuperUseConsumed),"False use remained in confirmed presentation");
            File.WriteAllText(Path.Combine(output,"false-prepaid-startup.json"),JsonSerializer.Serialize(new{timeline,hash=predicted.Hash(),rollback.RollbackCount}));
        });
        Check("rollback_retracts_false_counter_reward",()=>
        {
            Simulation Setup(){var s=new Simulation(content,new MatchConfig{SessionId="v2-false-counter"});s.CommitPreparation(new([]),new([]));s.BeginFight();for(int n=0;n<100&&Math.Abs(s.Players[0].X-s.Players[1].X)>32000;n++)s.Step(new(0,s.Tick,6,Buttons.None),new(1,s.Tick,4,Buttons.None));s.Step(new(0,s.Tick,5,Buttons.None),new(1,s.Tick,5,Buttons.None));Require(s.TryStartAction(1,"command_fhp")==ActivationStatus.Free,"Legal forward-HP fixture failed");return s;}
            var predicted=Setup();var straight=Setup();long start=predicted.Tick;var snapshot=predicted.Capture();var rollback=new RollbackSession(predicted,0,0);var actual=new List<InputFrame>();var timeline=new List<object>();
            for(int n=0;n<8;n++)
            {
                var own=new InputFrame(0,start+n,5,n==4?Buttons.LP:Buttons.None);var other=new InputFrame(1,start+n,5,n==0?Buttons.LP|Buttons.LK:Buttons.None);actual.Add(other);var step=straight.Step(own,other);Require(rollback.Advance(own.Direction,own.Held),"False-reward prediction stalled");timeline.Add(new{own,other,step.Events,pending=predicted.Players[0].PendingSkillCredits,actualPending=straight.Players[0].PendingSkillCredits});
            }
            Require(predicted.Players[0].PendingSkillCredits==50,"Fixture did not predict counter reward");Require(straight.Players[0].PendingSkillCredits==0,"Late kara throw did not remove counter opportunity");Require(!rollback.DrainPresentationChanges().Any(c=>c.Event.Kind==CombatEventKind.SkillAward),"Unconfirmed reward popup escaped");
            foreach(var input in actual)rollback.SubmitRemote(input);Require(predicted.Hash()==straight.Hash()&&predicted.Players.All(p=>p.PendingSkillCredits==0&&p.Credits==600),"False skill reward survived authoritative correction");Require(!rollback.DrainPresentationChanges().Any(c=>c.Event.Kind==CombatEventKind.SkillAward),"False confirmed reward survived");
            File.WriteAllText(Path.Combine(output,"false-counter-reward.json"),JsonSerializer.Serialize(new{fixture="Walk to push distance then legal forward HP start via deterministic test action API; late actual kara-throw command cancels it",snapshot=Convert.ToBase64String(snapshot.Bytes),timeline,hash=predicted.Hash(),rollback.RollbackCount}));
        });
        Check("actual_udp_legacy_header_and_forged_bonus_rejected",()=>
        {
            int Port(){using var socket=new Socket(AddressFamily.InterNetwork,SocketType.Dgram,ProtocolType.Udp);socket.Bind(new IPEndPoint(IPAddress.Loopback,0));return ((IPEndPoint)socket.LocalEndPoint!).Port;}
            int x=Port(),y=Port();while(x==y)y=Port();const string id="v2-forged-bonus";var config=new MatchConfig{SessionId=id};
            using(var peer=new PrivateMatchPeer(content,config,0,new(IPAddress.Loopback,x),new(IPAddress.Loopback,y)))
            using(var attacker=new UdpTransport(new(IPAddress.Loopback,y),new(IPAddress.Loopback,x),id))
            {
                attacker.Send(PacketKind.Hello,JsonSerializer.SerializeToUtf8Bytes(new PeerHello(3,ReplayFormat.Build,content.ContentHash,id,1,config)));
                for(int n=0;n<100&&peer.Status==PeerStatus.Connecting;n++){attacker.Poll();peer.Poll();Thread.Sleep(1);}Require(peer.Status==PeerStatus.Preparation,"Valid v2 test handshake failed");
                attacker.Send(PacketKind.Commit,JsonSerializer.SerializeToUtf8Bytes(new{Round=1,Version=2,Hash=new string('0',64),BonusCredits=300}));
                for(int n=0;n<100&&peer.Status!=PeerStatus.Aborted;n++){attacker.Poll();peer.Poll();Thread.Sleep(1);}Require(peer.Status==PeerStatus.Aborted&&peer.Simulation.Players.All(p=>p.Credits==600&&p.PendingSkillCredits==0),"Forged bonus control changed economy or escaped rejection");
            }
            x=Port();y=Port();while(x==y)y=Port();using var receiver=new UdpTransport(new(IPAddress.Loopback,x),new(IPAddress.Loopback,y),id);using var old=new Socket(AddressFamily.InterNetwork,SocketType.Dgram,ProtocolType.Udp);old.Bind(new IPEndPoint(IPAddress.Loopback,y));int delivered=0;receiver.Packet+=_=>delivered++;
            byte[] legacy=new byte[32];System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(legacy,0x314c5355);SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(id)).AsSpan(0,16).CopyTo(legacy.AsSpan(4));legacy[20]=(byte)PacketKind.Ping;System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(legacy.AsSpan(24),0x80000001);old.SendTo(legacy,new IPEndPoint(IPAddress.Loopback,x));
            for(int n=0;n<100&&receiver.MalformedPackets==0;n++){receiver.Poll();Thread.Sleep(1);}Require(receiver.MalformedPackets==1&&delivered==0,"Legacy transport header was accepted as v2");
        });
        Check("full_replay_skill_receipts_seek_and_tamper",()=>
        {
            var sim=new Simulation(content,new MatchConfig{SessionId="v2-round-ledger"});var recorder=new ReplayRecorder(content,sim.Config);var bots=new[]{new BotController(0,BotMode.Adaptive,1),new BotController(1,BotMode.Adaptive,91)};
            for(int n=0;n<50000&&sim.Phase!=MatchPhase.MatchOver;n++)
            {
                if(sim.Phase==MatchPhase.Preparation){recorder.CommitPreparation(sim,bots[0].ChoosePreparation(sim),bots[1].ChoosePreparation(sim));recorder.BeginFight(sim);}
                if(sim.Phase==MatchPhase.PendingResult)recorder.SettleRound(sim,sim.Tick);
                if(sim.Phase==MatchPhase.RoundResult){recorder.NextRound(sim);continue;}if(sim.Phase==MatchPhase.MatchOver)break;
                recorder.Step(sim,bots[0].Next(sim),bots[1].Next(sim));
            }
            Require(sim.Phase==MatchPhase.MatchOver,"Competitive ledger match timed out");string path=Path.Combine(output,"v2-full-match.json");recorder.Save(path);var record=ReplayFormat.Load(path,content.ContentHash);var ledger=RoundEconomyLedger.From(record);Require(ledger.Count==sim.CompletedRounds&&ledger.Sum(r=>r.Seats.Sum(s=>s.SkillEarned))>0,"No genuine skill receipts witnessed");Require(ledger.Last().HasNextShop==false,"Final receipt promises another shop");
            var player=new ReplayPlayer(content,record);while(player.Step()){}Require(player.Simulation.Hash()==sim.Hash(),"Full replay state mismatch");player.Seek(sim.Tick/2);player.Seek(0);while(player.Step()){}Require(player.Simulation.Hash()==sim.Hash(),"Seek duplicated use or reward receipts");
            var award=record.Commands.First(c=>c.Events.Any(e=>e.Kind==CombatEventKind.SkillAward));int at=Array.FindIndex(award.Events,e=>e.Kind==CombatEventKind.SkillAward);award.Events[at]=award.Events[at] with{Value=award.Events[at].Value+1};var tampered=new ReplayPlayer(content,record);Reject(()=>{while(tampered.Step()){}},"Tampered skill receipt accepted");
            var legacy=File.ReadAllText(path).Replace("\"Version\":4","\"Version\":3");string legacyPath=Path.Combine(output,"legacy-refused.json");File.WriteAllText(legacyPath,legacy);string legacyHash=Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(legacyPath)));Reject(()=>ReplayFormat.Load(legacyPath,content.ContentHash),"Legacy replay reinterpreted");Require(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(legacyPath)))==legacyHash,"Legacy refusal modified source recording");
            ReplayFormat.ExportEconomy(recorder.Record,Path.Combine(output,"v2-round-ledger.json"));
        });
    }
    public static void Timeout(GameContent content,string output)
    {
        int Port(){using var socket=new Socket(AddressFamily.InterNetwork,SocketType.Dgram,ProtocolType.Udp);socket.Bind(new IPEndPoint(IPAddress.Loopback,0));return ((IPEndPoint)socket.LocalEndPoint!).Port;}
        int x=Port(),y=Port();while(x==y)y=Port();var config=new MatchConfig{SessionId="last-valid-timeout-"+x};
        using var a=new PrivateMatchPeer(content,config,0,new(IPAddress.Loopback,x),new(IPAddress.Loopback,y));using var b=new PrivateMatchPeer(content,config,1,new(IPAddress.Loopback,y),new(IPAddress.Loopback,x));
        long start=Environment.TickCount64;void Pump(){a.Poll();b.Poll();Require(a.Status!=PeerStatus.Aborted&&b.Status!=PeerStatus.Aborted,a.Diagnostic+" / "+b.Diagnostic);Thread.Sleep(5);}
        while((a.Status==PeerStatus.Connecting||b.Status==PeerStatus.Connecting)&&Environment.TickCount64-start<5000)Pump();Require(a.Status==PeerStatus.Preparation&&b.Status==PeerStatus.Preparation,"Timeout test handshake failed");
        string license=content.Items.Values.Single(i=>i.EligibleFighters.Contains("rook")&&i.MoveId=="pulse_ex").Id;a.SetPreparationDraft(new([license]));
        Reject(()=>a.SetPreparationDraft(new([license,license])),"Invalid cart replaced prior valid draft");
        while((a.Status!=PeerStatus.Playing||b.Status!=PeerStatus.Playing)&&Environment.TickCount64-start<22000)Pump();
        Require(a.Status==PeerStatus.Playing&&b.Status==PeerStatus.Playing&&a.LocalPlanSubmitted&&b.LocalPlanSubmitted,"Real15-second timeout did not commit both last-valid carts");
        Require(a.Simulation.Hash()==b.Simulation.Hash()&&a.Simulation.Players[0].OwnedEx.SequenceEqual(new[]{"pulse_ex"})&&a.Simulation.Players[0].Credits==0&&a.Simulation.Players[1].Credits==600,"Timeout wiped valid license, crossed seats, or double charged");
        File.WriteAllText(Path.Combine(output,"shop-timeout.json"),JsonSerializer.Serialize(new{passed=true,build=ReplayFormat.Build,contentHash=content.ContentHash,elapsedMilliseconds=Environment.TickCount64-start,realUdp=true,noManualSubmit=true,lastValidLicense=license,rejectedDuplicate=true,hash=a.Simulation.Hash(),banks=a.Simulation.Players.Select(p=>p.Credits),products=a.Simulation.Players.Select(p=>p.OwnedProductIds)},new JsonSerializerOptions{WriteIndented=true}));
    }

}
