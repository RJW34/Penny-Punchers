using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using StrikeLedger.App;
using StrikeLedger.Core;
internal static class UpgradeTests
{
    static void Require(bool ok,string message){if(!ok)throw new InvalidOperationException(message);}
    public static void Run(GameContent content,string output)
    {
        var checks=new List<object>();void Check(string id,Action action){action();checks.Add(new{id,passed=true});Console.WriteLine("PASS upgrade "+id);}
        Check("command_encoder_neutral_entrypoints_both_facings",()=>
        {
            foreach(var fighter in content.Fighters.Values)foreach(var move in fighter.Moves.Where(m=>m.DerivedFrom.Length==0&&!m.Command.StartsWith("air:",StringComparison.Ordinal)))foreach(int facing in new[]{1,-1})
            {
                var s=new Simulation(content,new MatchConfig{Fighter0=fighter.Id,Fighter1=fighter.Id,Training=true,Super0=fighter.SuperArts.FirstOrDefault(a=>a.MoveId==move.Id)?.Id??fighter.DefaultSuper});s.TrainingReset(content.Economy.Cap);
                s.SetTrainingState(0,x:facing==1?330000:386000);s.SetTrainingState(1,x:facing==1?386000:330000);
                var rental=content.Items.Values.FirstOrDefault(i=>i.EligibleFighters.Contains(fighter.Id)&&i.MoveId==move.Id);if(rental!=null)s.SetTrainingLoadout(0,new([rental.Id]));
                if(move.Command.StartsWith("J+")){s.SetTrainingState(0,y:60000);s.SetTrainingState(1,y:60000);}
                if(move.Command.StartsWith("CLOSE+")){s.SetTrainingState(0,x:facing==1?330000:362000);s.SetTrainingState(1,x:facing==1?362000:330000);}
                bool started=false;foreach(var input in CommandEncoder.Encode(content,move,facing)){var result=s.Step(new(0,s.Tick,input.Direction,input.Held),new(1,s.Tick,5,Buttons.None));started|=result.Events.Any(e=>e.Seat==0&&e.Kind==CombatEventKind.ActionStarted&&e.MoveId==move.Id);}
                Require(started,$"Encoder did not execute {fighter.Id}/{move.Id} facing {facing}");
            }
        });
        Check("training_loadouts_checkpoint_progress",()=>
        {
            var cfg=new TrainingConfiguration{Fighter0="vale",Fighter1="vale",Credits0=1200,Credits1=900,Loadout0=new([content.Items.Values.First(i=>i.EligibleFighters.Contains("vale")&&i.Slot=="technique").Id]),Loadout1=new([content.Items.Values.First(i=>i.EligibleFighters.Contains("vale")&&i.Slot=="signature").Id])};var t=new TrainingSession(content,cfg);t.Reset("same_tick_ex");
            Require(t.Simulation.Players[0].Credits==1200&&t.Simulation.Players[1].Credits==900,"Training configuration credits lost");t.SaveCheckpoint();
            foreach(var i in CommandEncoder.Encode(content,"vale","pulse_ex"))t.FrameAdvance(i.Direction,i.Held);Require(t.Success,"EX drill did not complete");t.RestoreCheckpoint();Require(!t.Success&&t.Recording.Count==0,"Checkpoint retained future drill progress");
            t.Reset();Require(t.Simulation.Config.Fighter0=="vale"&&t.Simulation.Config.Fighter1=="vale","Independent lineup lost");Require(cfg.Loadout0.ItemIds.All(t.Simulation.Players[0].OwnedProductIds.Contains)&&cfg.Loadout1.ItemIds.All(t.Simulation.Players[1].OwnedProductIds.Contains),"Reset erased training loadout");
        });
        Check("replay_atomic_unique_failure_and_receipts",()=>
        {
            var config=new MatchConfig();var sim=new Simulation(content,config);var recorder=new ReplayRecorder(content,config);recorder.CommitPreparation(sim,new([]),new([]));recorder.BeginFight(sim);recorder.Step(sim,new(0,sim.Tick,5,Buttons.None),new(1,sim.Tick,5,Buttons.None));
            string directory=Path.Combine(output,"storage-"+Guid.NewGuid().ToString("N"));var a=ReplayStore.SaveUnique(recorder.Record,directory);var b=ReplayStore.SaveUnique(recorder.Record,directory);Require(a.Success&&b.Success&&a.Path!=b.Path,"Same-moment save overwrote recording");
            using(var locked=new FileStream(a.Path,FileMode.Open,FileAccess.Read,FileShare.Read)){bool failed=false;try{ReplayFormat.Save(recorder.Record,a.Path);}catch(Exception error)when(error is IOException or UnauthorizedAccessException){failed=true;}Require(failed,"Locked recording overwrite was not rejected");}
            Require(ReplayStore.List(directory,content.ContentHash).Count==2,"Archive count wrong");Require(!Directory.GetFiles(directory,"*.tmp").Any(),"Atomic temp leaked");
            var loaded=ReplayFormat.Load(a.Path,content.ContentHash);var bad=loaded.Commands.Last();bad.Wallet0++;var player=new ReplayPlayer(content,loaded);bool rejected=false;try{while(player.Step()){}}catch(InvalidDataException){rejected=true;}Require(rejected,"Later replay wallet corruption accepted");
        });
        Check("continuous_link_fixture_search",()=>
        {
            var traces=new List<object>();bool found=false;
            foreach(var first in new[]{Buttons.LP,Buttons.MP,Buttons.HP,Buttons.LK,Buttons.MK})foreach(var second in new[]{Buttons.LP,Buttons.MP,Buttons.HP,Buttons.LK,Buttons.MK})
            {
                var t=new TrainingSession(content);t.Reset("link_cancel");t.DummyMode=BotMode.Passive;t.Simulation.SetTrainingState(0,x:720000);t.Simulation.SetTrainingState(1,x:752000);
                t.FrameAdvance(5,first);for(int n=0;n<150&&!t.Simulation.Players[0].Actionable;n++)t.FrameAdvance(5,Buttons.None);
                int remaining=t.Simulation.Players[1].Hitstun;int before=t.Simulation.Players[1].ComboCount;t.FrameAdvance(5,second);for(int n=0;n<25;n++)t.FrameAdvance(5,Buttons.None);
                bool link=t.Feedback.Contains("link,")||t.Feedback.Contains("link.");bool combined=t.Simulation.Players[1].ComboCount>1||t.Recording.SelectMany(f=>f.Events).Count(e=>e.Kind==CombatEventKind.Hit&&e.Seat==0)>1;
                if(link){traces.Add(new{first=first.ToString(),second=second.ToString(),remaining,before,combined,feedback=t.Feedback});found=true;}
            }
            File.WriteAllText(Path.Combine(output,"measured-link-routes.json"),JsonSerializer.Serialize(traces));Require(found,"No genuine normal link found");
        });
        Check("rollback_reversible_local_cues",()=>
        {
            var simulation=new Simulation(content,new MatchConfig{Training=true});simulation.TrainingReset();simulation.SetTrainingState(0,x:720000);simulation.SetTrainingState(1,x:752000);var rollback=new RollbackSession(simulation,0,0);
            for(int n=0;n<8;n++)rollback.Advance(5,n==6?Buttons.MP:Buttons.None);
            var predicted=rollback.DrainPresentationChanges();var action=predicted.Single(c=>c.Speculative&&c.Event.Kind==CombatEventKind.ActionStarted);
            Require(predicted.All(c=>c.Event.Kind is not(CombatEventKind.SuperUseConsumed or CombatEventKind.SkillAward or CombatEventKind.Hit or CombatEventKind.RoundTerminal or CombatEventKind.Settlement)),"Unconfirmed outcome was presented");
            rollback.SubmitRemote(new(1,0,5,Buttons.LP));var corrected=rollback.DrainPresentationChanges();Require(corrected.Any(c=>c.Key==action.Key&&c.Kind==PresentationChangeKind.Cancel),"Invalidated local startup cue was not cancelled");
        });
        Check("training_rejects_gapped_links",()=>
        {
            var t=new TrainingSession(content);t.Reset("link_cancel");t.DummyMode=BotMode.Passive;t.Simulation.SetTrainingState(0,x:720000);t.Simulation.SetTrainingState(1,x:752000);
            t.FrameAdvance(5,Buttons.MP);for(int n=0;n<40;n++)t.FrameAdvance(5,Buttons.None);t.FrameAdvance(5,Buttons.MK);for(int n=0;n<20;n++)t.FrameAdvance(5,Buttons.None);
            Require(!t.Feedback.StartsWith("Progress: link")&&!t.Success,"Independent normals were labeled a true link");
        });
        Check("socket_lobby_pause_barrier_leave",()=>Network(content));
        Check("socket_mutual_rematch_and_round_ledger",()=>Rematch(content));
        File.WriteAllText(Path.Combine(output,"upgrade-regressions.json"),JsonSerializer.Serialize(new{build=ReplayFormat.Build,contentHash=content.ContentHash,checks},new JsonSerializerOptions{WriteIndented=true}));
    }
    static void Rematch(GameContent content)
    {
        int x=Port(),y=Port();while(x==y)y=Port();var config=new MatchConfig{SessionId="rematch-"+x};using var a=new PrivateMatchPeer(content,config,0,new(IPAddress.Loopback,x),new(IPAddress.Loopback,y));using var b=new PrivateMatchPeer(content,config,1,new(IPAddress.Loopback,y),new(IPAddress.Loopback,x));
        var bots=new[]{new BotController(0,BotMode.Adaptive,17),new BotController(1,BotMode.Adaptive,31)};int[] planned={0,0};long deadline=Environment.TickCount64+30000;
        while((a.Status!=PeerStatus.MatchOver||b.Status!=PeerStatus.MatchOver)&&Environment.TickCount64<deadline)
        {
            var peers=new[]{a,b};for(int seat=0;seat<2;seat++){var p=peers[seat];p.Poll();Require(p.Status is not(PeerStatus.Aborted or PeerStatus.Disconnected),p.Diagnostic);if(p.Status==PeerStatus.Preparation&&planned[seat]!=p.Round){p.SubmitPreparation(bots[seat].ChoosePreparation(p.Simulation));planned[seat]=p.Round;}if(p.Status==PeerStatus.Playing){var input=bots[seat].Next(p.Simulation);p.Advance(input.Direction,input.Held);}}
            if(a.Status==PeerStatus.RoundResult)a.ContinueMatch();
        }
        Require(a.Status==PeerStatus.MatchOver&&b.Status==PeerStatus.MatchOver,"Full match for rematch fixture timed out");var record=a.CaptureReplay();var ledger=RoundEconomyLedger.From(record);Require(ledger.Count==a.Simulation.CompletedRounds&&ledger.All(r=>r.Seats.All(s=>s.OpeningCredits-s.PreparationSpend+s.GrantedIncome==s.ClosingCredits)),"Round ledger does not conserve credits");
        var verification=new ReplayPlayer(content,record);var terminalTicks=new List<long>();while(verification.Step()){if(verification.Simulation.Phase==MatchPhase.PendingResult&&verification.Simulation.PendingResult is {} terminal)terminalTicks.Add(terminal.TerminalTick);}
        Require(ledger.Select(r=>r.TerminalTick).SequenceEqual(terminalTicks),"Ledger terminal tick must name the actual consumed contact frame, not the later settlement boundary");
        a.RequestRematch();for(int n=0;n<20;n++){a.Poll();b.Poll();}Require(a.Status==PeerStatus.MatchOver&&b.Status==PeerStatus.MatchOver,"One-sided rematch reset peer");b.RequestRematch();for(int n=0;n<1000&&(a.Status!=PeerStatus.Preparation||b.Status!=PeerStatus.Preparation);n++){a.Poll();b.Poll();}
        Require(a.Status==PeerStatus.Preparation&&b.Status==PeerStatus.Preparation&&a.Simulation.Hash()==b.Simulation.Hash(),"Mutual rematch did not agree");Require(a.Simulation.Players.All(p=>p.Credits==content.Economy.StartingCredits&&p.ScoreHalfPoints==0)&&a.Simulation.Tick==0,"Rematch retained prior economy or score");
    }
    static int Port(){using var s=new Socket(AddressFamily.InterNetwork,SocketType.Dgram,ProtocolType.Udp);s.Bind(new IPEndPoint(IPAddress.Loopback,0));return ((IPEndPoint)s.LocalEndPoint!).Port;}
    static void Network(GameContent content)
    {
        int x=Port(),y=Port();while(x==y)y=Port();string session="upgrade-"+x;var config=new MatchConfig{SessionId=session};
        using var a=new PrivateMatchPeer(content,config,0,new(IPAddress.Loopback,x),new(IPAddress.Loopback,y),new(100,20,3,3,3,21),true);
        using var b=new PrivateMatchPeer(content,config with{Fighter0="vale",Super0="art_3"},1,new(IPAddress.Loopback,y),new(IPAddress.Loopback,x),new(150,20,3,3,3,22),true);
        void Pump(){a.Poll();b.Poll();Require(a.Status!=PeerStatus.Aborted&&b.Status!=PeerStatus.Aborted,a.Diagnostic+" / "+b.Diagnostic);Thread.Sleep(1);}
        void Until(Func<bool> condition){long end=Environment.TickCount64+10000;while(!condition()&&Environment.TickCount64<end)Pump();Require(condition(),"Network fixture deadline: "+a.Status+" / "+a.Diagnostic+" // "+b.Status+" / "+b.Diagnostic);}
        Until(()=>a.LocalSelection!=null&&a.RemoteSelection!=null&&b.LocalSelection!=null&&b.RemoteSelection!=null);
        a.SelectLineup("vale","grid");b.SelectLineup("rook");Until(()=>a.RemoteSelection?.Fighter=="rook"&&b.RemoteSelection?.Fighter=="vale");a.ReadyLineup();b.ReadyLineup();Until(()=>a.Status==PeerStatus.Preparation&&b.Status==PeerStatus.Preparation);
        Require(a.Simulation.Config==b.Simulation.Config&&a.Simulation.Config.Fighter0=="vale"&&a.Simulation.Config.Fighter1=="rook"&&a.Simulation.Config.StageId=="grid","Lobby agreement failed");
        a.SubmitPreparation(new([]));b.SubmitPreparation(new([]));Until(()=>a.Status==PeerStatus.Playing&&b.Status==PeerStatus.Playing);
        for(int n=0;n<200;n++){a.Advance(5,Buttons.None);if(n%3!=0)b.Advance(5,Buttons.None);Pump();}b.Pause();Until(()=>a.Status==PeerStatus.Paused&&b.Status==PeerStatus.Paused);
        Require(a.Simulation.Tick==b.Simulation.Tick&&a.Simulation.Hash()==b.Simulation.Hash(),"Pause stopped at different canonical ticks");long tick=a.Simulation.Tick;for(int n=0;n<40;n++)Pump();Require(a.Simulation.Tick==tick&&b.Simulation.Tick==tick,"Paused simulation advanced");
        b.Resume();Until(()=>a.Status==PeerStatus.Playing&&b.Status==PeerStatus.Playing);Require(a.ResumeCountdownTicks==120&&b.ResumeCountdownTicks==120,"No shared neutral resume countdown");
        b.Dispose();Until(()=>a.Status==PeerStatus.Disconnected);Require(a.Simulation.CompletedRounds==0,"Leave settled an incomplete round");
    }
}
