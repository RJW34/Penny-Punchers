using Godot;
using StrikeLedger.Core;
using StrikeLedger.App;
using StrikeLedger.Presentation;
using System.Text.Json;
using System.Net;

public partial class Main : Node2D
{
    readonly Color Ink=new("111923"),Cream=new("f4e5c5"),Muted=new("afbebd"),Gold=new("edaa54"),Cyan=new("69b8ac");
    GameContent content=null!; Simulation? sim; GameSettings settings=null!;InputRouter input=null!;ArenaView arena=null!;
    Control ui=null!;Font font=null!;string screen="title",mode="bot",toast="";double toastTime,screenTime;bool paused,frameStep,debugBoxes;
    string[] fighter={"rook","vale"};int[] art={0,0};int[][] draft={new[]{-1,-1,-1},new[]{-1,-1,-1}};int[] floor={0,0};bool[] ready={false,false};
    readonly Dictionary<string,JsonElement> fighterJson=new();JsonElement items;string dataPath="";int prepTicks;BotController? bot0,bot1;
    ReplayRecorder? recorder;ReplayPlayer? replay;int remapIndex=-1,remapDevice=-1;bool captureBinding;Label? statusLabel;
    string trainingMode="Stand";int dummyMode,drillIndex;readonly string[] drills={"Free practice","High parry: tap toward at impact","Low parry: tap down at impact","Throw tech: LP + LK","Confirm: LP > MP > special","Kara: forward HP > LP+LK","Budget: QCF + PP costs 300"};
    PrivateMatchPeer? peer;bool networkHost;string networkAddress="127.0.0.1",networkPort="27961",networkCode="ledger";
    InputFrame[] lastInputs={new(0,0,5,Buttons.None),new(1,0,5,Buttons.None)};int inputGrace;
    readonly Queue<string>[] inputHistory={new(),new()};string[] lastAction={"",""};int[] lastHealth={1000,1000},lastWallet={600,600};
    bool smoke,freeKitEvidence;string scenario="",evidenceDir="";int autoTicks;BotMode cpuPolicy=BotMode.Adaptive;System.IO.StreamWriter? evidenceLog;bool captureRequested;
    public override void _Ready()
    {
        GetTree().AutoAcceptQuit=false;font=ThemeDB.FallbackFont;settings=GameSettings.Load();settings.Apply();input=new(settings);arena=new ArenaView();AddChild(arena);arena.SetVolumes(settings.Master,settings.Music,settings.Sfx);
        var layer=new CanvasLayer();AddChild(layer);ui=new Control();ui.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);layer.AddChild(ui);
        var args=OS.GetCmdlineUserArgs();string Arg(string key,string fallback=""){int i=Array.IndexOf(args,key);return i>=0&&i+1<args.Length?args[i+1]:fallback;}
        freeKitEvidence=args.Contains("--free-kit");computerEvidence=args.Contains("--computer-evidence");scenario=Arg("--scenario");smoke=args.Contains("--smoke")||scenario=="native_export_smoke"||scenario=="whole_local_match";evidenceDir=Arg("--evidence-dir",ProjectSettings.GlobalizePath("user://evidence"));
        try{dataPath=ExtractContent();content=GameContent.Load(dataPath);foreach(var f in fighter)fighterJson[f]=JsonDocument.Parse(Godot.FileAccess.GetFileAsString($"res://GeneratedData/fighters/{f}.json")).RootElement.Clone();items=JsonDocument.Parse(Godot.FileAccess.GetFileAsString("res://GeneratedData/items.json")).RootElement.GetProperty("items").Clone();}
        catch(Exception e){GD.PushError(e.ToString());QuitGame(2);return;}Input.JoyConnectionChanged+=OnJoyChanged;Title();if(computerEvidence)Capture("title");
        if(args.Contains("--art-review")){BeginArtReview();return;}
        if(args.Contains("--combat-showcase")||scenario=="combat_visual_showcase"){BeginCombatShowcase();return;}
        if(args.Contains("--ui-smoke")||scenario=="controller_menu_flow"){BeginUiSmoke();return;}
        if(args.Contains("--network-evidence")){BeginNetworkEvidence(args);return;}
        if(smoke){System.IO.Directory.CreateDirectory(evidenceDir);evidenceLog=new(System.IO.Path.Combine(evidenceDir,"gameplay.log"));mode="demo";StartMatch();}
        else if(scenario!=""){GD.PushError("Unknown shell scenario: "+scenario);QuitGame(2);}
    }
    string ExtractContent(){using var manifest=JsonDocument.Parse(Godot.FileAccess.GetFileAsString("res://GeneratedData/content_manifest.json"));string digest=manifest.RootElement.GetProperty("content_sha256").GetString()!;string dest=ProjectSettings.GlobalizePath("user://content/"+digest);foreach(var entry in manifest.RootElement.GetProperty("files").EnumerateArray()){string rel=entry.GetProperty("path").GetString()!;if(rel.Contains("..")||System.IO.Path.IsPathRooted(rel))throw new InvalidDataException("Unsafe content path");string target=System.IO.Path.Combine(dest,rel);System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(target)!);System.IO.File.WriteAllBytes(target,Godot.FileAccess.GetFileAsBytes("res://GeneratedData/"+rel));}return dest;}
    void StartMatch(){if(mode=="training"){StartLab();return;}var config=new MatchConfig{Fighter0=fighter[0],Fighter1=fighter[1],Super0=$"art_{art[0]+1}",Super1=$"art_{art[1]+1}",SessionId="local",Training=mode=="training"};sim=new Simulation(content,config);bot0=new(0,BotMode.Adaptive,17);bot1=new(1,cpuPolicy,31);recorder=mode=="training"?null:new ReplayRecorder(content,config);lastWallet=new[]{600,600};lastHealth=new[]{1000,1000};paused=false;if(mode=="training"){sim.CommitPreparation(new([]),new([]));sim.BeginFight();Clear("fight");inputGrace=12;}else Preparation();}
    public override void _PhysicsProcess(double delta){if(quitting||_artReviewActive)return;TickUiSmoke(delta);TickCombatShowcase(delta);TickNetworkEvidence(delta);int ticks=smoke&&DisplayServer.GetName()=="headless"?60:1;for(int i=0;i<ticks;i++)TickShell(delta);}
    void TickShell(double delta)
    {
        if(content==null)return;screenTime+=delta;toastTime=Math.Max(0,toastTime-delta);if(screen is "title" or "select")IdleScene();
        if(screen=="controls"&&statusLabel!=null)statusLabel.Text=$"P1 {input.Sample(0,0).Direction}/{input.Sample(0,0).Held}  P2 {input.Sample(1,0).Direction}/{input.Sample(1,0).Held}";
        if(screen=="prep"){prepTicks--;if(statusLabel!=null)statusLabel.Text=$"LOCK IN {Math.Max(0,(prepTicks+59)/60):00}s";if(prepTicks<=0||(mode=="demo"&&screenTime>.5))LockPlans();}
        if(peer!=null)TickNetwork();else if(sim!=null&&(screen=="fight"||screen=="pause")){if(frameStep&&mode=="training"){frameStep=false;TickLab();}else if(!paused||frameStep){frameStep=false;TickGame();}UpdateArena();}QueueRedraw();
    }
    void TickGame()
    {
        using var tickProfile=StrikeLedger.Presentation.RuntimeProfiler.Measure("TickGame");
        if(sim==null)return;if(mode=="training"){labAccumulator+=lab!.Speed;if(labAccumulator>=1){labAccumulator--;TickLab();}return;}if(mode=="replay"){try{AdvanceReplay();}catch(Exception e){paused=true;Toast("Replay rejected: "+e.Message);}return;}
        if(sim.Phase==MatchPhase.PendingResult){if(recorder!=null)recorder.SettleRound(sim,sim.Tick,$"result-{sim.RoundId}");else sim.SettleRound(sim.Tick);evidenceLog?.WriteLine($"SETTLE {sim.Tick} {sim.Hash()}");screenTime=0;}
        if(sim.Phase is MatchPhase.RoundResult or MatchPhase.MatchOver){if(screenTime>2){if(sim.Phase==MatchPhase.MatchOver){FinishMatch();return;}if(recorder!=null)recorder.NextRound(sim);else sim.NextRound();Preparation();}return;}
        var a=input.Sample(0,sim.Tick);var b=input.Sample(1,sim.Tick);if(mode=="demo"){a=bot0!.Next(sim);b=bot1!.Next(sim);}else if(mode=="bot")b=bot1!.Next(sim);if(mode=="training")b=TrainingInput();if(inputGrace>0){inputGrace--;a=new(0,sim.Tick,5,Buttons.None);b=new(1,sim.Tick,5,Buttons.None);}
        lastInputs=new[]{a,b};for(int s=0;s<2;s++){string line=$"{lastInputs[s].Direction} {lastInputs[s].Held}";if(inputHistory[s].Count==0||inputHistory[s].Last()!=line){inputHistory[s].Enqueue(line);while(inputHistory[s].Count>9)inputHistory[s].Dequeue();}}
        var stepResult=recorder!=null?recorder.Step(sim,a,b):sim.Step(a,b);ObserveChanges(stepResult);if(smoke){autoTicks++;if(autoTicks%240==0){evidenceLog?.WriteLine($"TICK {sim.Tick} {sim.Phase} HEALTH {sim.Players[0].Health}/{sim.Players[1].Health} CREDIT {sim.Players[0].Credits}/{sim.Players[1].Credits} HASH {sim.Hash()}");Capture($"frame-{autoTicks:000000}");}if(autoTicks>60000){GD.PushError("Smoke timeout");evidenceLog?.Dispose();QuitGame(3);}}
    }
    InputFrame TrainingInput(){var p=sim!.Players[1];byte back=(byte)(p.Facing==1?4:6);return dummyMode switch{1=>new(1,sim.Tick,back,Buttons.None),2=>new(1,sim.Tick,(byte)(back==4?1:3),Buttons.None),3=>bot1!.Next(sim),4=>new(1,sim.Tick,5,sim.Tick%70==0?Buttons.LP:Buttons.None),_=>new(1,sim.Tick,5,Buttons.None)};}
    JsonElement? MoveJson(string f,string id)=>fighterJson[f].GetProperty("moves").EnumerateArray().Where(m=>m.GetProperty("id").GetString()==id).Select(m=>(JsonElement?)m).FirstOrDefault();
    public override void _Draw()
    {
        using var drawProfile=StrikeLedger.Presentation.RuntimeProfiler.Measure("HudDraw");
        if(font==null)return;if(sim!=null&&(screen is "fight" or "pause")){
            DrawArtHud();
            string banner=sim.Phase switch{MatchPhase.Reveal=>"LOADOUTS LOCKED",MatchPhase.Countdown=>"GET READY",MatchPhase.PendingResult=>peer==null?"K.O.":"CONFIRMING RESULT",MatchPhase.RoundResult=>"ROUND SETTLED",MatchPhase.MatchOver=>"MATCH COMPLETE",_=>""};if(banner!="")DrawArtAnnouncement(banner);
            if(mode=="training"){DrawStyleBox(UiArtFrame("hud"),new Rect2(0,609,1280,111));DrawString(font,new(24,635),$"LAB / {(paused?"PAUSED":"LIVE")}  DUMMY: {lab?.DummyMode.ToString()??trainingMode}  {lab?.Drill.Title}",HorizontalAlignment.Left,-1,17,Gold);DrawString(font,new(24,666),"F1 pause  . frame  F2 reset  F3 dummy  F4 drill  F5 boxes  ESC menu",HorizontalAlignment.Left,-1,16,Cream);DrawString(font,new(24,698),$"P1 {lastInputs[0].Direction}/{lastInputs[0].Held} {sim.Players[0].ActionId} f{sim.Players[0].ActionFrame} | P2 {lastInputs[1].Direction}/{lastInputs[1].Held}",HorizontalAlignment.Left,-1,15,Muted);for(int i=0;i<inputHistory[0].Count;i++)DrawString(font,new(42,224+i*23),inputHistory[0].ElementAt(i),HorizontalAlignment.Left,-1,14,Muted);}else {DrawStyleBox(UiArtFrame("hud"),new Rect2(-12,672,1304,60));DrawString(font,new(42,702),"FIRST TO 5 POINTS / MAX 9 ROUNDS                              ESC / START  pause & move list",HorizontalAlignment.Left,-1,14,Muted);if(peer!=null)DrawString(font,new(40,658),$"PRIVATE / DELAY {peer.Rollback.InputDelay}f / ROLLBACK {peer.Rollback.RollbackCount} / {(peer.Rollback.IsStalled?"WAITING FOR INPUT":"CONNECTED")}",HorizontalAlignment.Left,-1,14,Cyan);}
        DrawLabDiagnostics();DrawReplayOverlay();}if(toastTime>0){DrawStyleBox(UiArtFrame("pressed"),new Rect2(350,158,580,43));DrawString(font,new(369,187),toast,HorizontalAlignment.Center,542,19,Gold);}
    }
    void Toast(string message){toast=message;toastTime=2.6;}
    string pendingCapture="";int captureDelay;bool computerEvidence;
    void Capture(string name){if(DisplayServer.GetName()=="headless"||OS.GetCmdlineUserArgs().Contains("--no-screenshots"))return;System.IO.Directory.CreateDirectory(evidenceDir);pendingCapture=name;captureDelay=3;}
    public override void _Process(double delta){RecordRenderedFrame(delta);if(pendingCapture!=""&&captureDelay-->0)return;if(pendingCapture!=""){var path=System.IO.Path.Combine(evidenceDir,pendingCapture+".png");using(var captureImage=GetViewport().GetTexture().GetImage())captureImage.SavePng(path);GD.Print("CAPTURE "+path);pendingCapture="";}}
    public override void _Input(InputEvent e){if(RouteControlInput(e))return;if(computerEvidence&&e is InputEventKey k&&k.Pressed&&!k.Echo)GD.Print($"KEY {k.Keycode}/{k.PhysicalKeycode} SCREEN {screen}");if(e is InputEventKey shot&&shot.Pressed&&(shot.Keycode==Key.F12||shot.PhysicalKeycode==Key.F12)){Capture("manual-"+DateTime.Now.ToString("HHmmss"));GetViewport().SetInputAsHandled();}}
    async void CompleteSmoke(){evidenceLog?.Dispose();evidenceLog=null;for(int frame=0;frame<6;frame++)await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);if(networkEvidence){var grace=System.Diagnostics.Stopwatch.StartNew();while(grace.Elapsed.TotalMilliseconds<700){peer?.Poll();await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);}}WriteRuntimeEvidence();QuitGame(0);}
    public override void _ExitTree(){ReleaseUiArt();Input.JoyConnectionChanged-=OnJoyChanged;peer?.Dispose();evidenceLog?.Dispose();}
    public override void _UnhandledInput(InputEvent e){if(e is InputEventKey shot&&shot.Pressed&&shot.PhysicalKeycode==Key.F12){System.IO.Directory.CreateDirectory(evidenceDir);Capture("manual-"+DateTime.Now.ToString("HHmmss"));return;}if(captureBinding){if(e is InputEventKey k&&k.Pressed&&!k.Echo&&remapDevice==-1){settings.Keys[remapIndex]=(long)k.PhysicalKeycode;settings.Save();captureBinding=false;ControlsMenu();}else if(e is InputEventJoypadButton j&&j.Pressed&&j.Device==remapDevice){input.Mapping(j.Device)[remapIndex]=(int)j.ButtonIndex;settings.Save();captureBinding=false;ControlsMenu();}GetViewport().SetInputAsHandled();return;}bool esc=e.IsActionPressed("ui_cancel")||(e is InputEventJoypadButton j2&&j2.Pressed&&j2.ButtonIndex==JoyButton.Start);if(esc){if(screen=="pad_text")NetworkMenu();else if(screen=="fight")PauseMenu();else if(screen is "pause" or "labmenu" or "replay_controls" or "lab_state")Resume();else if(sim!=null&&screen is "moves" or "controls" or "settings")PauseMenu();else if(screen!="title")Title();GetViewport().SetInputAsHandled();return;}if(screen=="fight"&&mode=="training"&&e is InputEventKey key&&key.Pressed&&!key.Echo){switch(key.Keycode){case Key.F1:paused=!paused;break;case Key.Period:paused=true;frameStep=true;break;case Key.F2:ResetLabDrill(lab!.Drill.Id);break;case Key.F3:dummyMode=(dummyMode+1)%5;trainingMode=new[]{"Stand","Guard","Jump","CPU","Throw"}[dummyMode];lab!.DummyMode=new[]{BotMode.Passive,BotMode.Guard,BotMode.Jump,BotMode.Adaptive,BotMode.Throw}[dummyMode];break;case Key.F4:drillIndex=(drillIndex+1)%TrainingSession.Drills.Length;ResetLabDrill(TrainingSession.Drills[drillIndex].Id);break;case Key.F5:debugBoxes=!debugBoxes;break;}GetViewport().SetInputAsHandled();}}
    void OnJoyChanged(long device,bool connected){if(!connected&&sim!=null&&mode!="demo"&&input.Devices.Contains((int)device)){if(peer!=null){peer.Dispose();peer=null;Title();Toast("Controller disconnected. Private match aborted.");}else{PauseMenu();Toast("Controller disconnected. Reconnect and assign it in Controls.");}}}
}
