using Godot;
using StrikeLedger.App;
using StrikeLedger.Core;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;

public partial class Main
{
    async Task UiNetworkTerminalHistoryEvidence()
    {
        UiStep("late UDP terminal reward reaches actual public history");Title();
        static int Port(){using var socket=new Socket(AddressFamily.InterNetwork,SocketType.Dgram,ProtocolType.Udp);socket.Bind(new IPEndPoint(IPAddress.Loopback,0));return ((IPEndPoint)socket.LocalEndPoint!).Port;}
        int port0=Port(),port1=Port();while(port1==port0)port1=Port();
        var cfg=new MatchConfig{Fighter0="rook",Fighter1="rook",StageId="grid",SessionId="ui-late-terminal-reward"};
        using var host=new PrivateMatchPeer(content,cfg,0,new(IPAddress.Loopback,port0),new(IPAddress.Loopback,port1));
        using var guest=new PrivateMatchPeer(content,cfg,1,new(IPAddress.Loopback,port1),new(IPAddress.Loopback,port0));
        peer=host;sim=host.Simulation;fighter=["rook","rook"];mode="network";networkHost=true;networkEvidence=false;paused=true;recorder=null;replay=null;
        ResetShopDrafts();ResetPresentationTimeline();Clear("fight");
        void Healthy(){if(host.Status is PeerStatus.Aborted or PeerStatus.Disconnected||guest.Status is PeerStatus.Aborted or PeerStatus.Disconnected)throw new InvalidOperationException(host.Diagnostic+" / "+guest.Diagnostic);}
        void PollBoth(){host.Poll();guest.Poll();Healthy();}
        for(int n=0;n<1000&&(host.Status!=PeerStatus.Preparation||guest.Status!=PeerStatus.Preparation);n++){PollBoth();System.Threading.Thread.Sleep(1);}
        UiRequire(host.Status==PeerStatus.Preparation&&guest.Status==PeerStatus.Preparation,"Two real UDP peers complete a compatible private handshake");
        host.SubmitPreparation(new([]));guest.SubmitPreparation(new([]));
        for(int n=0;n<1000&&(host.Status!=PeerStatus.Playing||guest.Status!=PeerStatus.Playing);n++){PollBoth();System.Threading.Thread.Sleep(1);}
        UiRequire(host.Status==PeerStatus.Playing&&guest.Status==PeerStatus.Playing,"Actual empty commit/reveal starts the private round");
        int steps=0;
        void Step(byte direction0=5,Buttons buttons0=Buttons.None,byte direction1=5,Buttons buttons1=Buttons.None)
        {
            long tick=host.Simulation.Tick;
            if(!host.Advance(direction0,buttons0)||!guest.Advance(direction1,buttons1))throw new InvalidOperationException("Synchronized terminal fixture unexpectedly stalled");
            for(int n=0;n<1000&&(host.Rollback.ConfirmedThroughTick<tick||guest.Rollback.ConfirmedThroughTick<tick);n++){PollBoth();if(n%10==9)System.Threading.Thread.Sleep(1);}
            Healthy();if(host.Rollback.ConfirmedThroughTick<tick||guest.Rollback.ConfirmedThroughTick<tick||host.Simulation.Hash()!=guest.Simulation.Hash())throw new InvalidOperationException("Fixture inputs did not confirm identically");
            ObserveChanges();steps++;
        }
        void Wait(int ticks){for(int n=0;n<ticks&&sim.Phase==MatchPhase.Fight;n++)Step();}
        for(int n=0;n<600&&sim.Phase!=MatchPhase.Fight;n++)Step();
        UiRequire(sim.Phase==MatchPhase.Fight,"Private reveal/countdown advances through actual paired inputs");
        int attacks=0;
        while(sim.Players[1].Health>35&&sim.Phase==MatchPhase.Fight&&attacks++<40)
        {
            for(int n=0;n<100&&Math.Abs(sim.Players[0].X-sim.Players[1].X)>34000;n++)Step(6);
            Wait(12);Step(buttons0:Buttons.LP);Wait(45);
        }
        for(int n=0;n<100&&Math.Abs(sim.Players[0].X-sim.Players[1].X)>34000;n++)Step(6);
        Wait(20);
        UiRequire(sim.Phase==MatchPhase.Fight&&sim.Players[1].Health is >0 and <=35&&sim.Players[0].PendingSkillCredits==0,"Legal neutral jabs reduce the opponent to a lethal-jab range without fabricated health or earlier rewards");
        long gapStart=sim.Tick,confirmedBefore=host.Rollback.ConfirmedThroughTick;
        // Flush the local jab before the guest queues its startup: transport
        // Send schedules datagrams until Poll, so withholding both directions
        // would test a missing attack rather than delayed remote confirmation.
        host.Advance(5,Buttons.LP);host.Poll();guest.Poll();guest.Advance(4,Buttons.HP);
        // Guest receives real jab input; host deliberately receives no packets
        // until the lethal contact. The late HP startup turns that hit into CH.
        for(int n=0;n<7&&(host.Simulation.Phase==MatchPhase.Fight||guest.Simulation.Phase==MatchPhase.Fight);n++)
        {
            guest.Poll();Healthy();
            if(host.Simulation.Phase==MatchPhase.Fight)host.Advance(5,Buttons.None);
            if(guest.Simulation.Phase==MatchPhase.Fight)guest.Advance(5,Buttons.None);
            ObserveChanges();
        }
        for(int n=0;n<10;n++){guest.Poll();System.Threading.Thread.Sleep(1);}
        UiRequire(host.Simulation.Phase==MatchPhase.PendingResult&&guest.Simulation.Phase==MatchPhase.PendingResult,"Both real input timelines reach the lethal contact before settlement");
        UiRequire(host.Simulation.Players[0].PendingSkillCredits==0&&guest.Simulation.Players[0].PendingSkillCredits==50&&host.Rollback.ConfirmedThroughTick<host.Simulation.PendingResult!.TerminalTick,"Withheld actual remote startup leaves only the host's terminal counter reward unconfirmed");
        screenTime=0;networkResultRound=-1;TickNetwork();
        UiRequire(host.Status is PeerStatus.AwaitingSettlement or PeerStatus.RoundResult,"The production network tick confirms late input and leaves Playing during Poll");
        UiRequire(publicShopRounds.TryGetValue(1,out var facts)&&facts.Awards[0]==50&&facts.Counters[0]==1,"Production post-Poll drain records the final confirmed reward/counter before result handling");
        for(int n=0;n<1000&&host.Status!=PeerStatus.RoundResult;n++){guest.Poll();TickNetwork();if(n%10==9)System.Threading.Thread.Sleep(1);}
        UiRequire(host.Status==PeerStatus.RoundResult,"Both terminal hashes acknowledge one confirmed round settlement");
        TickNetwork();
        UiRequire(publicShopRounds[1].Awards[0]==50&&sim.Players[0].Credits==1850&&host.Rollback.RollbackCount>0,"Repeated result polling does not duplicate history or the confirmed skill deposit");
        string replayPath=System.IO.Path.Combine(evidenceDir,"late-terminal-history.replay.json");host.SaveReplay(replayPath);
        var verified=new ReplayPlayer(content,ReplayFormat.Load(replayPath,content.ContentHash));while(verified.Step()){}
        UiRequire(verified.Simulation.Hash()==host.Simulation.Hash(),"Late-terminal private replay reconstructs exact corrected rewards and settlement");
        System.IO.File.WriteAllText(System.IO.Path.Combine(evidenceDir,"late-terminal-history.json"),JsonSerializer.Serialize(new{passed=true,scope="Two actual UDP peers, real neutral attacks, withheld remote input, production TickNetwork/ObserveChanges/public history; no training mutations or physical-device claim",build=ReplayFormat.Build,contentHash=content.ContentHash,steps,attacks,gapStart,confirmedBefore,terminal=sim.PendingResult,rollbacks=host.Rollback.RollbackCount,awards=publicShopRounds[1].Awards,counters=publicShopRounds[1].Counters,settlement=sim.LastSettlement,finalHash=sim.Hash(),replayPath},new JsonSerializerOptions{WriteIndented=true}));
        Title();await UiFrames(2);
    }
}
