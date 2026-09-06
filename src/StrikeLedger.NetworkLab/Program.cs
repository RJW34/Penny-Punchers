using System.Diagnostics;
using System.Net;
using System.Text.Json;
using StrikeLedger.App;
using StrikeLedger.Core;

var options=new Dictionary<string,string>(StringComparer.Ordinal);
for(int n=0;n<args.Length;n++){if(!args[n].StartsWith("--"))throw new ArgumentException("Expected --option value");if(n+1<args.Length&&!args[n+1].StartsWith("--"))options[args[n]]=args[++n];else options[args[n]]="true";}
string Get(string key,string value)=>options.GetValueOrDefault(key,value);
int Number(string key,int value)=>int.Parse(Get(key,value.ToString()));
string data=Path.GetFullPath(Get("--data","data"));
string output=Path.GetFullPath(Get("--evidence-dir",Get("--output","reports/network-lab")));Directory.CreateDirectory(output);
try
{
    var content=GameContent.Load(data);
    if(options.ContainsKey("--self-test")||options.ContainsKey("--scenario")){SelfTests(content,output,Get("--scenario","all"));return 0;}
    int seat=Number("--seat",0);
    var faults=new FaultProfile(Number("--rtt",0),Number("--jitter",0),Number("--loss",0),Number("--duplicates",0),Number("--reorder",0),Number("--seed",1)+seat*1723);
    var config=new MatchConfig{SessionId=Get("--session","private-lab"),Fighter0="rook",Fighter1="vale"};
    using var log=new StreamWriter(Path.Combine(output,$"peer{seat}.jsonl")){AutoFlush=true};
    using var peer=new PrivateMatchPeer(content,config,seat,new IPEndPoint(IPAddress.Loopback,Number("--local-port",27843+seat)),new IPEndPoint(IPAddress.Loopback,Number("--remote-port",27844-seat)),faults);
    var bot=new BotController(seat,BotMode.Adaptive,(uint)Number("--seed",1)+(uint)seat*47);
    var watch=Stopwatch.StartNew();long previousTick=-1;int plannedRound=0;int resultRound=0;
    peer.Notice+=message=>{Console.WriteLine($"seat={seat} tick={peer.Simulation.Tick} round={peer.Round} status={peer.Status} {message}");log.WriteLine(JsonSerializer.Serialize(new{utc=DateTime.UtcNow,peer.Round,tick=peer.Simulation.Tick,status=peer.Status.ToString(),message,hash=peer.Simulation.Hash()}));};
    while(watch.Elapsed.TotalSeconds<Number("--max-seconds",900))
    {
        peer.Poll();
        if(peer.Status==PeerStatus.Preparation && plannedRound!=peer.Round){peer.SubmitPreparation(bot.ChoosePreparation(peer.Simulation));plannedRound=peer.Round;}
        bool advanced=false;
        if(peer.Status==PeerStatus.Playing)
        {
            var input=bot.Next(peer.Simulation);advanced=peer.Advance(input.Direction,input.Held);
            if(peer.Simulation.Tick!=previousTick && peer.Simulation.Tick%60==0)
            {previousTick=peer.Simulation.Tick;log.WriteLine(JsonSerializer.Serialize(new{tick=previousTick,round=peer.Round,phase=peer.Simulation.Phase.ToString(),confirmed=peer.Rollback.ConfirmedThroughTick,hash=peer.Simulation.Hash(),wallets=peer.Simulation.Players.Select(p=>p.Credits),health=peer.Simulation.Players.Select(p=>p.Health),actions=peer.Simulation.Players.Select(p=>p.ActionId),rollbacks=peer.Rollback.RollbackCount}));}
        }
        if(peer.Status is PeerStatus.RoundResult or PeerStatus.MatchOver && resultRound!=peer.Round)
        {resultRound=peer.Round;if(peer.Status==PeerStatus.RoundResult && seat==0)peer.ContinueMatch();}
        if(peer.Status is PeerStatus.MatchOver or PeerStatus.Aborted or PeerStatus.Disconnected)break;
        if(advanced)Thread.Yield();else Thread.Sleep(1);
    }
    // Keep the final transport alive briefly so the other process receives/re-ACKs the terminal control.
    if(peer.Status==PeerStatus.MatchOver)for(int n=0;n<300;n++){peer.Poll();Thread.Sleep(2);}
    bool completed=peer.Status==PeerStatus.MatchOver;
    var result=new{completed,seat,utc=DateTime.UtcNow,platform=Environment.OSVersion.ToString(),build=ReplayFormat.Build,contentHash=content.ContentHash,status=peer.Status.ToString(),peer.Diagnostic,rounds=peer.Round,ticks=peer.Simulation.Tick,finalHash=peer.Simulation.Hash(),wallets=peer.Simulation.Players.Select(p=>p.Credits).ToArray(),scores=peer.Simulation.Players.Select(p=>p.ScoreHalfPoints).ToArray(),peer.Rollback.RollbackCount,peer.Rollback.ResimulatedFrames,peer.Rollback.StallCount,peer.Retransmissions,peer.MalformedPackets,elapsedSeconds=watch.Elapsed.TotalSeconds,faults,roundReceipts=peer.ConfirmedRounds};
    File.WriteAllText(Path.Combine(output,$"peer{seat}.result.json"),JsonSerializer.Serialize(result,new JsonSerializerOptions{WriteIndented=true}));
    File.WriteAllBytes(Path.Combine(output,$"peer{seat}.final-state.bin"),peer.Simulation.SerializeCanonical());
    if(completed)
    {
        string replayPath=Path.Combine(output,$"peer{seat}.slreplay");peer.SaveReplay(replayPath);var replay=new ReplayPlayer(content,ReplayFormat.Load(replayPath,content.ContentHash));while(replay.Step()){}
        if(replay.Simulation.Hash()!=peer.Simulation.Hash())throw new InvalidOperationException("Saved actual network replay differs from final peer state");
    }
    if(peer.DesyncDump is not null)File.WriteAllText(Path.Combine(output,$"peer{seat}.desync.json"),peer.DesyncDump);
    Console.WriteLine(JsonSerializer.Serialize(result));return completed?0:2;
}
catch(Exception ex){Console.Error.WriteLine(ex);File.WriteAllText(Path.Combine(output,"error-"+Get("--seat","tests")+".txt"),ex.ToString());return 1;}

static void SelfTests(GameContent content,string output,string scenario="all")
{
    var results=new List<object>();
    string[] selected=scenario switch
    {
        "all"=>[],"shop_timeout"=>["shop_timeout"],"shop_only_v2"=>["shop_only_v2"],"cpu_capability"=>["cpu_capability"],"training_demonstrations"=>["training_demonstrations"],"buyable_objects"=>["buyable_objects"],"upgrade_regressions"=>["upgrade_regressions"],"bot_economic_match" or "replay_wallet_seek"=>["full_bot_match_replay_and_seek"],
        "rollback_startup_debits"=>["rollback_corrects_input_wallet_receipts","prediction_stalls_after_eight_frames"],
        "rollback_ko_to_parry"=>["late_parry_retracts_predicted_paid_super_ko"],
        "training_drills"=>["training_controls_are_isolated_and_record_real_inputs","all_training_drill_evaluators"],
        "malformed_packet_replay"=>["replay_malformed_identity_bounds","actual_udp_rejects_incompatible_handshake_and_forged_seat","actual_udp_oversized_packets_are_bounded"],
        _=>throw new ArgumentException("Unknown/unimplemented App scenario: "+scenario)
    };
    void Check(string name,Action run){if(selected.Length>0&&!selected.Contains(name))return;var watch=Stopwatch.StartNew();run();results.Add(new{name,passed=true,milliseconds=watch.Elapsed.TotalMilliseconds});Console.WriteLine("PASS "+name);}
    void Assert(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
    Simulation Fight(string session,bool training=false){var s=new Simulation(content,new MatchConfig{SessionId=session,Training=training});s.CommitPreparation(new(session=="rollback"?[content.Items.Values.Single(i=>i.EligibleFighters.Contains("rook")&&i.MoveId=="pulse_ex").Id]:[]),new([]));s.BeginFight();return s;}
    Check("rollback_corrects_input_wallet_receipts",()=>
    {
        var straight=Fight("rollback");var rollback=Fight("rollback");var session=new RollbackSession(rollback,0,0);
        var a=new List<InputFrame>();var b=new List<InputFrame>();
        for(int t=0;t<180;t++)
        {
            byte dir=(byte)((t%60) switch{1=>2,2=>3,3=>6,_=>5});Buttons buttons=t%60==3?Buttons.LP|Buttons.MP:Buttons.None;
            a.Add(new(0,t,dir,buttons));b.Add(new(1,t,t%11==0?(byte)4:(byte)5,t%29==0?Buttons.HK:Buttons.None));
            straight.Step(a[t],b[t]);Assert(session.Advance(dir,buttons),"Unexpected bounded stall");
            if(t>=4)session.SubmitRemote(b[t-4]);
        }
        foreach(var input in b.TakeLast(4))session.SubmitRemote(input);
        Assert(straight.Hash()==rollback.Hash(),"Restored simulation diverged from authoritative inputs");Assert(session.RollbackCount>0,"No actual late-input correction exercised");
        Assert(straight.Players[0].OwnedEx.SequenceEqual(rollback.Players[0].OwnedEx)&&straight.Players[0].SuperUseReceipts.SequenceEqual(rollback.Players[0].SuperUseReceipts)&&straight.Players[0].SkillReceipts.SequenceEqual(rollback.Players[0].SkillReceipts),"Capability/reward receipts differ after resimulation");
        bool rejected=false;try{session.SubmitRemote(new(0,181,5,Buttons.None));}catch(InvalidDataException){rejected=true;}Assert(rejected,"Cross-seat input accepted");
    });
    Check("prediction_stalls_after_eight_frames",()=>
    {var s=Fight("stall");var r=new RollbackSession(s,0,0);for(int t=0;t<8;t++)Assert(r.Advance(5,Buttons.None),"Stalled before prediction cap");long tick=s.Tick;Assert(!r.Advance(5,Buttons.None)&&s.Tick==tick,"Prediction continued beyond cap");});
    Check("late_parry_retracts_predicted_paid_super_ko",()=>
    {
        Simulation Setup(){var s=new Simulation(content,new MatchConfig{SessionId="late-parry",Training=true});s.TrainingReset();s.SetTrainingLoadout(0,new([content.Items.Values.Single(i=>i.EligibleFighters.Contains("rook")&&i.MoveId=="super_1").Id]));s.SetTrainingState(0,x:330000,credits:0);s.SetTrainingState(1,x:374000,health:20);return s;}
        InputFrame Attack(long t)=>new(0,t,(byte)(t switch{0=>2,1=>3,2=>6,3=>2,4=>3,5=>6,_=>5}),t==5?Buttons.HP:Buttons.None);
        var scout=Setup();for(int n=0;n<150&&scout.Phase!=MatchPhase.PendingResult;n++)scout.Step(Attack(scout.Tick),new(1,scout.Tick,5,Buttons.None));
        Assert(scout.Phase==MatchPhase.PendingResult && scout.Players[0].SuperUseReceipts.Count==1,"Scout did not produce a real prepaid-super KO");long terminal=scout.PendingResult!.TerminalTick;
        var predicted=Setup();var rollback=new RollbackSession(predicted,0,0);var authoritative=Setup();
        for(long t=0;t<=terminal;t++)
        {
            var own=Attack(t);var actual=new InputFrame(1,t,t==terminal?(byte)4:(byte)5,Buttons.None);
            authoritative.Step(own,actual);Assert(rollback.Advance(own.Direction,own.Held),"Unexpected prediction stall");if(t<terminal)rollback.SubmitRemote(actual);
        }
        Assert(predicted.Phase==MatchPhase.PendingResult,"Expected speculative KO before late input");int wallet=predicted.Players[0].Credits;
        rollback.SubmitRemote(new(1,terminal,4,Buttons.None));
        Assert(predicted.Phase==MatchPhase.Fight && predicted.Players[1].Health==20,"Late parry failed to retract KO");
        Assert(predicted.Hash()==authoritative.Hash(),"Late parry corrected state differs from authoritative playback");
        Assert(predicted.Players[0].Credits==wallet && predicted.Players[0].SuperUseReceipts.Count==1 && predicted.CompletedRounds==0,"Rollback duplicated super use or paid predicted settlement");
        var earlier=Setup();var earlierSession=new RollbackSession(earlier,0,0);var actualEarlier=Setup();
        for(long t=0;t<=terminal+3;t++)
        {
            var own=Attack(t);var remote=new InputFrame(1,t,t==terminal-1?(byte)6:(byte)5,Buttons.None);
            if(t<=terminal)actualEarlier.Step(own,remote);
            Assert(earlierSession.Advance(own.Direction,own.Held),"Earlier KO setup unexpectedly stalled");
            if(t<terminal)earlierSession.SubmitRemote(remote);
        }
        earlierSession.SubmitRemote(new(1,terminal,5,Buttons.None));
        Assert(earlier.Phase==MatchPhase.PendingResult && earlier.Tick==terminal+1 && earlier.Hash()==actualEarlier.Hash(),"Earlier corrected KO did not truncate obsolete future simulation");
        File.WriteAllText(Path.Combine(output,"late-parry.json"),JsonSerializer.Serialize(new{terminal,hash=predicted.Hash(),wallet,receipts=predicted.Players[0].SuperUseReceipts,rollback.RollbackCount,settledRounds=predicted.CompletedRounds}));
    });
    Check("full_bot_match_replay_and_seek",()=>
    {
        var config=new MatchConfig{SessionId="replay-test"};var s=new Simulation(content,config);var record=new ReplayRecorder(content,config);var bots=new[]{new BotController(0,BotMode.Adaptive,1),new BotController(1,BotMode.Adaptive,91)};
        int rounds=0,steps=0;while(s.Phase!=MatchPhase.MatchOver && steps<50000)
        {
            if(s.Phase==MatchPhase.Preparation){rounds++;record.CommitPreparation(s,bots[0].ChoosePreparation(s),bots[1].ChoosePreparation(s),$"prep:{rounds}");record.BeginFight(s);}
            if(s.Phase==MatchPhase.PendingResult)record.SettleRound(s,s.Tick,$"settle:{rounds}");
            if(s.Phase==MatchPhase.RoundResult){record.NextRound(s);continue;}
            if(s.Phase==MatchPhase.MatchOver)break;
            record.Step(s,bots[0].Next(s),bots[1].Next(s));steps++;
        }
        Assert(s.Phase==MatchPhase.MatchOver,"Bot match failed to terminate");string path=Path.Combine(output,"full-match.slreplay");record.Save(path);ReplayFormat.ExportEconomy(record.Record,Path.Combine(output,"economy.json"));
        var replay=new ReplayPlayer(content,ReplayFormat.Load(path,content.ContentHash));while(replay.Step()){}Assert(replay.Simulation.Hash()==s.Hash(),"Fresh replay final state mismatch");
        long middle=s.Tick/2;replay.Seek(middle);string middleHash=replay.Simulation.Hash();replay.Seek(0);replay.Seek(middle);Assert(middleHash==replay.Simulation.Hash(),"Seek retained future wallet/input state");while(replay.Step()){}Assert(replay.Simulation.Hash()==s.Hash(),"Replay seek changed final economy");
        File.WriteAllText(Path.Combine(output,"bot-match-result.json"),JsonSerializer.Serialize(new{rounds,steps,hash=s.Hash(),wallets=s.Players.Select(p=>p.Credits),scores=s.Players.Select(p=>p.ScoreHalfPoints)}));
    });
    Check("replay_malformed_identity_bounds",()=>
    {
        var config=new MatchConfig();var r=new ReplayRecord(new(ReplayFormat.Version,ReplayFormat.Build,"wrong",config,1,false),new());bool rejected=false;try{ReplayFormat.Validate(r,content.ContentHash);}catch(InvalidDataException){rejected=true;}Assert(rejected,"Content mismatch accepted");
        r=new(new(ReplayFormat.Version,ReplayFormat.Build,content.ContentHash,config,1,false),new(){new(){Kind="wallet",Tick=0}});rejected=false;try{ReplayFormat.Validate(r,content.ContentHash);}catch(InvalidDataException){rejected=true;}Assert(rejected,"Wallet mutation replay command accepted");
        r=new(new(ReplayFormat.Version,ReplayFormat.Build,content.ContentHash,new MatchConfig{Assist=true},1,false),new());rejected=false;try{ReplayFormat.Validate(r,content.ContentHash);}catch(InvalidDataException){rejected=true;}Assert(rejected,"Assisted configuration hidden by unassisted replay header");
    });
    Check("training_controls_are_isolated_and_record_real_inputs",()=>
    {
        var t=new TrainingSession(content);t.Reset("same_tick_ex");t.FrameAdvance(2,Buttons.None);t.FrameAdvance(3,Buttons.None);t.FrameAdvance(6,Buttons.LP|Buttons.MP);Assert(t.Recording.SelectMany(f=>f.Events).Any(e=>e.Kind==CombatEventKind.ActionStarted&&e.MoveId=="pulse_ex")&&t.Simulation.Players[0].Credits==3600,"Training licensed EX did not execute with a frozen bank");
        t.SaveCheckpoint();string hash=t.Simulation.Hash();for(int n=0;n<10;n++)t.FrameAdvance(5,Buttons.None);t.RestoreCheckpoint();Assert(t.Simulation.Hash()==hash,"Training checkpoint diverged");t.ExportRecording(Path.Combine(output,"training-inputs.json"));
        t.Reset("eco_defense");t.DummyMode=BotMode.Adaptive;for(int n=0;n<20;n++)t.FrameAdvance(5,Buttons.None);t.SaveCheckpoint();
        for(int n=0;n<90;n++)t.FrameAdvance(5,Buttons.None);string advanced=t.Simulation.Hash();t.RestoreCheckpoint();for(int n=0;n<90;n++)t.FrameAdvance(5,Buttons.None);Assert(advanced==t.Simulation.Hash(),"Checkpoint failed to restore dummy RNG, observations or queued inputs");
        t.Reset("throw_tech");for(int n=0;n<90;n++)t.FrameAdvance(5,Buttons.None);var firstDummy=t.Recording.Select(f=>(f.Direction1,f.Buttons1)).ToArray();
        t.Reset("throw_tech");for(int n=0;n<90;n++)t.FrameAdvance(5,Buttons.None);Assert(firstDummy.SequenceEqual(t.Recording.Select(f=>(f.Direction1,f.Buttons1))),"Drill reset retained dummy timeline or absolute throw clock");
        var competitive=Fight("competitive");bool rejected=false;try{competitive.SetTrainingState(0,credits:3600);}catch(InvalidOperationException){rejected=true;}Assert(rejected,"Training credits leaked into competitive match");
    });
    Check("actual_udp_rejects_incompatible_handshake_and_forged_seat",()=>
    {
        int Port(){using var s=new System.Net.Sockets.Socket(System.Net.Sockets.AddressFamily.InterNetwork,System.Net.Sockets.SocketType.Dgram,System.Net.Sockets.ProtocolType.Udp);s.Bind(new IPEndPoint(IPAddress.Loopback,0));return ((IPEndPoint)s.LocalEndPoint!).Port;}
        void Attack(bool mismatch)
        {
            int first=Port(),second=Port();while(first==second)second=Port();string id="malformed-"+first;var config=new MatchConfig{SessionId=id};
            using var peer=new PrivateMatchPeer(content,config,0,new(IPAddress.Loopback,first),new(IPAddress.Loopback,second));
            using var attacker=new UdpTransport(new(IPAddress.Loopback,second),new(IPAddress.Loopback,first),id);
            var h=new PeerHello(3,mismatch?"incompatible-build":ReplayFormat.Build,content.ContentHash,id,1,config);
            attacker.Send(PacketKind.Hello,JsonSerializer.SerializeToUtf8Bytes(h));
            for(int n=0;n<100 && peer.Status==PeerStatus.Connecting;n++){attacker.Poll();peer.Poll();Thread.Sleep(1);}
            if(!mismatch){using var bytes=new MemoryStream();using var w=new BinaryWriter(bytes);w.Write(1);w.Write((byte)0);w.Write((byte)1);w.Write(0L);w.Write((byte)5);w.Write((byte)0);attacker.Send(PacketKind.Inputs,bytes.ToArray(),false);}
            for(int n=0;n<300 && peer.Status!=PeerStatus.Aborted;n++){attacker.Poll();peer.Poll();Thread.Sleep(1);}
            Assert(peer.Status==PeerStatus.Aborted,mismatch?"Incompatible handshake accepted":"Forged-seat datagram accepted");Assert(peer.Simulation.Players.All(p=>p.Credits==content.Economy.StartingCredits),"Malformed remote control changed credits");
        }
        Attack(true);Attack(false);
    });
    Check("actual_udp_oversized_packets_are_bounded",()=>
    {
        using var source=new System.Net.Sockets.Socket(System.Net.Sockets.AddressFamily.InterNetwork,System.Net.Sockets.SocketType.Dgram,System.Net.Sockets.ProtocolType.Udp);source.Bind(new IPEndPoint(IPAddress.Loopback,0));
        int sourcePort=((IPEndPoint)source.LocalEndPoint!).Port;
        using var reserve=new System.Net.Sockets.Socket(System.Net.Sockets.AddressFamily.InterNetwork,System.Net.Sockets.SocketType.Dgram,System.Net.Sockets.ProtocolType.Udp);reserve.Bind(new IPEndPoint(IPAddress.Loopback,0));int targetPort=((IPEndPoint)reserve.LocalEndPoint!).Port;reserve.Close();
        using var receiver=new UdpTransport(new(IPAddress.Loopback,targetPort),new(IPAddress.Loopback,sourcePort),"bounds");int delivered=0;receiver.Packet+=_=>delivered++;
        source.SendTo(new byte[5000],new IPEndPoint(IPAddress.Loopback,targetPort));source.SendTo(new byte[12],new IPEndPoint(IPAddress.Loopback,targetPort));source.SendTo(new byte[32],new IPEndPoint(IPAddress.Loopback,targetPort));
        for(int n=0;n<100&&receiver.MalformedPackets<3;n++){receiver.Poll();Thread.Sleep(1);}Assert(receiver.MalformedPackets==3&&delivered==0,"Oversized/malformed datagrams escaped codec bounds");
    });
    Check("actual_udp_disconnect_preparation_paid_startup_pause_resume",()=>
    {
        int Port(){using var s=new System.Net.Sockets.Socket(System.Net.Sockets.AddressFamily.InterNetwork,System.Net.Sockets.SocketType.Dgram,System.Net.Sockets.ProtocolType.Udp);s.Bind(new IPEndPoint(IPAddress.Loopback,0));return ((IPEndPoint)s.LocalEndPoint!).Port;}
        foreach(bool duringStartup in new[]{false,true})
        {
            int first=Port(),second=Port();while(first==second)second=Port();var config=new MatchConfig{SessionId="disconnect-"+first};
            using var a=new PrivateMatchPeer(content,config,0,new(IPAddress.Loopback,first),new(IPAddress.Loopback,second));using var b=new PrivateMatchPeer(content,config,1,new(IPAddress.Loopback,second),new(IPAddress.Loopback,first));
            void Pump(){a.Poll();b.Poll();Thread.Yield();}
            for(int n=0;n<10000&&(a.Status==PeerStatus.Connecting||b.Status==PeerStatus.Connecting);n++)Pump();
            Assert(a.Status==PeerStatus.Preparation&&b.Status==PeerStatus.Preparation,"Test peers did not establish handshake");
            a.SubmitPreparation(new(duringStartup?[content.Items.Values.Single(i=>i.EligibleFighters.Contains("rook")&&i.MoveId=="pulse_ex").Id]:[]));
            if(duringStartup)
            {
                b.SubmitPreparation(new([]));for(int n=0;n<10000&&(a.Status!=PeerStatus.Playing||b.Status!=PeerStatus.Playing);n++)Pump();
                a.Pause();for(int n=0;n<10000&&b.Status!=PeerStatus.Paused;n++)Pump();Assert(b.Status==PeerStatus.Paused,"Reliable pause not delivered");
                var pauseWatch=Stopwatch.StartNew();while(pauseWatch.ElapsedMilliseconds<16000){Pump();Thread.Sleep(10);}Assert(a.Status==PeerStatus.Paused&&b.Status==PeerStatus.Paused,"Healthy paused peers timed out after15 seconds");
                a.Resume();for(int n=0;n<10000&&b.Status!=PeerStatus.Playing;n++)Pump();Assert(b.Status==PeerStatus.Playing,"Reliable resume not delivered");
                long fightStart=-1;bool startedEx=false;
                for(int n=0;n<20000 && !startedEx;n++)
                {
                    Pump();if(fightStart<0&&a.Simulation.Phase==MatchPhase.Fight)fightStart=a.Simulation.Tick;
                    long t=fightStart<0?-1:a.Simulation.Tick-fightStart;
                    byte direction=(byte)(t switch{0=>2,1=>3,2=>6,_=>5});Buttons buttons=t==2?Buttons.LP|Buttons.MP:Buttons.None;
                    a.Advance(direction,buttons);b.Advance(5,Buttons.None);startedEx|=a.Simulation.Players[0].ActionId=="pulse_ex";
                }
                Assert(startedEx,"Disconnect test did not reach actual licensed EX startup");
            }
            b.Disconnect();for(int n=0;n<10000&&a.Status!=PeerStatus.Disconnected;n++)Pump();Assert(a.Status==PeerStatus.Disconnected,"Reliable disconnect not delivered");
            Assert(a.Simulation.CompletedRounds==0,"Disconnected round received a payout");Assert(a.Simulation.Players[0].Credits==content.Economy.StartingCredits-(duringStartup?content.Items.Values.Single(i=>i.EligibleFighters.Contains("rook")&&i.MoveId=="pulse_ex").Price:0),"Disconnect refunded shop purchase or changed frozen bank");
        }
    });
    if(scenario=="buyable_objects")Check("buyable_objects",()=>BuyableObjectTests.Run(content,output));
    Check("shop_timeout",()=>ShopV2AppTests.Timeout(content,output));
    Check("shop_only_v2",()=>ShopV2AppTests.Run(content,output));
    Check("cpu_capability",()=>CpuCapabilityTests.Run(content,output));
    Check("training_demonstrations",()=>DemonstrationTests.Run(content,output));
    Check("upgrade_regressions",()=>UpgradeTests.Run(content,output));
    Check("all_training_drill_evaluators",()=>TrainingDrillTests.Run(content,output));
    File.WriteAllText(Path.Combine(output,"self-tests.json"),JsonSerializer.Serialize(new{utc=DateTime.UtcNow,platform=Environment.OSVersion.ToString(),build=ReplayFormat.Build,contentHash=content.ContentHash,command=Environment.GetCommandLineArgs(),exitCode=0,results},new JsonSerializerOptions{WriteIndented=true}));
}
