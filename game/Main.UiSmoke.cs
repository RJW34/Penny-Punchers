using Godot;
using StrikeLedger.App;
using StrikeLedger.Core;
using System.Text.Json;

public partial class Main
{
    private bool _uiSmokeActive;
    private bool _uiPrivateTextEntryTested;
    private double _uiSmokeElapsed;
    private readonly List<object> _uiSmokeChecks=[];
    private readonly List<string> _uiSmokeCaptures=[];
    private Label? _uiSmokeLabel;
    private string _uiSmokeStep="initialization",_uiReplayFixture="";
    private int[] _uiActualHardware=[];
    private const int UiPad0=14,UiPad1=15;

    /// <summary>Explicit software input/UI test. It does not certify physical controllers.</summary>
    void BeginUiSmoke()
    {
        _uiSmokeActive=true;_uiSmokeElapsed=0;
        System.IO.Directory.CreateDirectory(evidenceDir);
        _uiActualHardware=Input.GetConnectedJoypads().ToArray();
        settings=new GameSettings{PersistenceEnabled=false,Music=0,Sfx=0};
        input=new InputRouter(settings);input.InstallSimulatedUiDevices(UiPad0,UiPad1);
        input.Assign(0,UiPad0);input.Assign(1,UiPad1);arena.SetVolumes(0,0,0);
        GD.Print("UI_ACCEPT_MAP "+string.Join(" | ",InputMap.ActionGetEvents("ui_accept").Select(e=>e.AsText()+" device="+e.Device)));
        var layer=new CanvasLayer{Layer=80};AddChild(layer);
        var banner=new ColorRect{Position=new Vector2(755,5),Size=new Vector2(518,25),Color=new Color(.04f,.085f,.10f,.94f),MouseFilter=Control.MouseFilterEnum.Ignore};layer.AddChild(banner);
        _uiSmokeLabel=new Label{Position=new Vector2(765,7),Text="SOFTWARE CONTROLLER TEST · NO PHYSICAL-PAD CLAIM",MouseFilter=Control.MouseFilterEnum.Ignore};
        _uiSmokeLabel.AddThemeFontSizeOverride("font_size",12);_uiSmokeLabel.AddThemeColorOverride("font_color",Cyan);layer.AddChild(_uiSmokeLabel);
        Callable.From(RunUiSmoke).CallDeferred();
    }

    void TickUiSmoke(double delta)
    {
        if(!_uiSmokeActive)return;
        _uiSmokeElapsed+=delta;
        if(_uiSmokeElapsed>240)FinishUiSmoke(false,"Timed out at "+_uiSmokeStep);
    }

    private async void RunUiSmoke()
    {
        try
        {
            await UiFrames(5);UiStep("title and D-pad navigation");
            UiRequire(screen=="title","Title is rendered");await UiCapture("ui-01-title");
            await UiActivate("01   Versus CPU");UiRequire(screen=="select"&&mode=="bot","Controller opens CPU lineup");
            var cpuButton=ui.GetChildren().OfType<Button>().First(b=>!b.IsQueuedForDeletion()&&b.Text.StartsWith("CPU:"));
            var navHint=ui.GetChildren().OfType<Label>().First(l=>!l.IsQueuedForDeletion()&&l.Text.StartsWith("ARROWS / D-PAD"));
            UiRequire(!cpuButton.GetGlobalRect().Intersects(navHint.GetGlobalRect()),"CPU selector and navigation hint do not overlap");
            await UiCapture("ui-01a-cpu-lineup-footer");
            await UiTap(UiPad0,JoyButton.B);UiRequire(screen=="title","Controller returns from CPU lineup");
            await UiTap(UiPad0,JoyButton.DpadDown);
            UiRequire(GetViewport().GuiGetFocusOwner() is Button focused&&focused.Text.Contains("Local versus"),"D-pad moves focus to local versus");
            await UiTap(UiPad0,JoyButton.A);UiRequire(screen=="select"&&mode=="local","Controller confirms local lineup");
            await UiActivate("Start match");UiRequire(screen=="prep","Controller starts actual local preparation");

            UiStep("independent shop-only mixed carts");
            int p2Super=purchaseProductIds.Single(k=>k.Key.Seat==1&&k.Value==ShopProducts(1,"super")[0].Id).Key.Row;
            await UiPrepSelect(1,p2Super);await UiTap(UiPad1,JoyButton.A);
            UiRequire(DraftItems(1).Length==0&&DraftItems(0).Length==0&&PlanPreview(1).Valid,"Unaffordable super preserves both last-valid carts");
            var exProduct=ShopProducts(0,"ex").First(p=>p.Price==600);
            int exRow=purchaseProductIds.Single(k=>k.Key.Seat==0&&k.Value==exProduct.Id).Key.Row;
            await UiPrepSelect(0,exRow);await UiTap(UiPad0,JoyButton.A);
            UiRequire(DraftItems(0).SequenceEqual(new[]{exProduct.Id})&&DraftCost(0)==600&&DraftItems(1).Length==0,"P1 buys an actual EX license in its own cart");
            await UiPrepSelect(0,PrepActionRow(0));await UiTap(UiPad0,JoyButton.A);
            await UiPrepSelect(0,PrepActionRow(0)+3);await UiTap(UiPad0,JoyButton.A);UiRequire(DraftItems(0).Length==0,"Controller Keep cash clears its own cart");
            await UiPrepSelect(0,PrepActionRow(0)+1);await UiTap(UiPad0,JoyButton.A);UiRequire(DraftItems(0).Contains(exProduct.Id),"Controller Load revalidates and restores a saved EX plan");
            await UiPrepSelect(1,2);await UiTap(UiPad1,JoyButton.DpadRight);
            UiRequire(DraftCost(1)==300&&DraftItems(0).Contains(exProduct.Id),"P2 changes only its own rental slot");
            await UiPrepSelect(1,0);await UiTap(UiPad1,JoyButton.A);
            UiRequire(prepPopupSeat==1,"P2 owns its rental popup");int p1Focus=_preparationFocus[0];
            await UiTap(UiPad0,JoyButton.DpadDown);UiRequire(prepPopupSeat==1&&_preparationFocus[0]==p1Focus,"Other controller cannot steer an owned popup");
            await UiTap(UiPad1,JoyButton.B);UiRequire(prepPopupSeat==-1&&screen=="prep","Owner cancels popup without leaving the shop");
            await UiCapture("ui-02-independent-preparation");
            await UiPrepSelect(0,PrepActionRow(0)+4);await UiTap(UiPad0,JoyButton.A);
            UiRequire(ready[0]&&!ready[1],"P1 lock does not lock P2");
            await UiPrepSelect(1,PrepActionRow(1)+4);
            await UiTap(UiPad1,JoyButton.A);UiRequire(screen=="fight","P2 lock reveals both plans and starts match lifecycle");
            for(int i=0;i<420&&sim?.Phase!=MatchPhase.Fight;i++)await UiFrames(1);
            UiRequire(sim?.Phase==MatchPhase.Fight,"Reveal and countdown reach real Fight phase");
            UiRequire(sim!.Players[0].Credits==0&&sim.Players[1].Credits==300&&sim.Players[0].Leases.Contains(exProduct.Id),"Shop charges the EX license and rental once before combat");
            await UiFrames(12);await UiCapture("ui-03-local-fight");
            await UiCombatInputBoundaries();
            await UiStageCameraEvidence();
            await UiTap(UiPad0,JoyButton.Start);UiRequire(screen=="pause"&&paused,"Start pauses local match");
            long pausedTick=sim.Tick;await UiFrames(5);UiRequire(sim.Tick==pausedTick,"Paused simulation does not advance");

            // Replay fixture consists of the actual preceding GUI-started match history.
            _uiReplayFixture=ProjectSettings.GlobalizePath("user://replays/ui-smoke-"+Guid.NewGuid().ToString("N")+".json");
            ReplayFormat.Save(recorder!.Record,_uiReplayFixture);

            UiStep("settings and two-device remapping");
            await UiActivate("Settings");UiRequire(screen=="settings","Settings opens through a controller button");
            bool previousFlash=settings.ReducedFlashes;await UiActivate("Flashes:");UiRequire(settings.ReducedFlashes!=previousFlash,"Controller changes flash preference");
            var slider=ui.GetChildren().OfType<HSlider>().First(x=>!x.IsQueuedForDeletion());float previousVolume=settings.Master;
            slider.GrabFocus();await UiFrames(1);await UiTap(UiPad0,JoyButton.DpadRight);UiRequire(settings.Master>previousVolume,"D-pad adjusts a real volume slider");
            await UiActivate("Controls & remapping");UiRequire(screen=="controls","Input workshop opens without a mouse");
            UiRequire(ui.GetChildren().OfType<Button>().Any(b=>b.Text.Contains("SIMULATED CONTROLLER 1")),"Software provider is visibly labeled");
            await UiActivate("REMAPPING PAD");UiRequire(_padRemapTarget==UiPad1,"Controller selector targets second controller");
            int originalOther=input.Mapping(UiPad0)[0];
            await UiActivate("PAD LP /");UiRequire(captureBinding&&remapDevice==UiPad1,"Second-pad button capture armed");
            await UiTap(UiPad1,JoyButton.B);
            UiRequire(!captureBinding&&screen=="controls"&&input.Mapping(UiPad1)[0]==(int)JoyButton.B,"B remaps without dismissing the controls page");
            UiRequire(input.Mapping(UiPad0)[0]==originalOther&&input.Mapping(UiPad1)[4]==(int)JoyButton.X,"Pad maps are independent and duplicate is swapped");
            await UiActivate("LP / U");UiRequire(captureBinding&&remapDevice==-1,"Keyboard binding can be selected using the controller");
            Input.ParseInputEvent(new InputEventKey{PhysicalKeycode=Key.Pause,Keycode=Key.I,Pressed=true});await UiFrames(2);
            Input.ParseInputEvent(new InputEventKey{PhysicalKeycode=Key.Pause,Keycode=Key.I,Pressed=false});await UiFrames(2);
            UiRequire(settings.Keys[4]==(long)Key.I&&settings.Keys[5]==(long)Key.U,"Injected logical key is captured and duplicate swaps");
            await UiCapture("ui-04-two-device-remapping");
            await UiTap(UiPad0,JoyButton.B);UiRequire(screen=="pause","Back returns to paused local game");
            await UiActivate("Return to title");UiRequire(screen=="title","Controller can leave a local match");

            UiStep("verified replay controls");
            await UiActivate("Replays");UiRequire(screen=="replays","Replay archive opens");
            var fixtureButton=ui.GetChildren().OfType<Button>().First(b=>!b.IsQueuedForDeletion()&&b.HasMeta("replay_path")&&string.Equals(System.IO.Path.GetFullPath(b.GetMeta("replay_path").AsString()),System.IO.Path.GetFullPath(_uiReplayFixture),StringComparison.OrdinalIgnoreCase));
            fixtureButton.GrabFocus();await UiFrames(2);await UiTap(UiPad0,JoyButton.A);
            UiRequire(mode=="replay"&&screen=="fight"&&replay!=null,"Actual recorded fixture opens through archive GUI");
            await UiTap(UiPad0,JoyButton.Start);UiRequire(screen=="replay_controls","Replay controls are controller-accessible");
            double speed=replay!.Speed;await UiActivate("Speed ");UiRequire(replay.Speed!=speed,"Controller changes replay speed");
            await UiActivate("Wallet graph:");UiRequire(showWalletGraph,"Controller enables wallet history display");
            await UiCapture("ui-05-replay-controls");
            await UiActivate("Restart replay");UiRequire(screen=="fight"&&sim!.Tick<120,"Controller seek restores an early replay tick");
            await UiTap(UiPad0,JoyButton.Start);await UiActivate("← Back");UiRequire(screen=="title","Controller returns from replay to title");

            UiStep("training tools and dummy recording");
            await UiActivate("03   Training lab");await UiActivate("Enter lab");UiRequire(mode=="training"&&screen=="fight"&&lab!=null,"Controller starts actual training session");
            await UiTap(UiPad0,JoyButton.Start);UiRequire(screen=="labmenu","Training menu opens with Start");
            int previousDrill=drillIndex;await UiActivate("Next drill");UiRequire(drillIndex!=previousDrill,"Controller switches actual training drill");
            await UiActivate("Save checkpoint");string checkpoint=sim!.Hash();
            await UiActivate("Swap sides");UiRequire(screen=="fight","Swap sides returns to training");
            await UiTap(UiPad0,JoyButton.Start);await UiActivate("Restore checkpoint");UiRequire(screen=="fight","Restore checkpoint returns to training");
            await UiTap(UiPad0,JoyButton.Start);bool boxes=debugBoxes;await UiActivate("Hitboxes:");UiRequire(debugBoxes!=boxes,"Controller toggles actual collision boxes");
            await UiActivate("Single frame");long frameTick=sim.Tick;UiRequire(paused&&screen=="fight","Single-frame command returns to paused arena");await UiFrames(3);UiRequire(sim.Tick==frameTick,"Single-frame command does not leave simulation running");
            await UiCapture("ui-06-training-grid-and-boxes");
            await UiTap(UiPad0,JoyButton.Start);await UiActivate("Record dummy");UiRequire(lab!.RecordingDummy,"Controller starts dummy recording");
            await UiTap(UiPad0,JoyButton.X);await UiFrames(12);
            await UiTap(UiPad0,JoyButton.Start);await UiActivate("Stop dummy recording");UiRequire(!lab.RecordingDummy,"Controller stops dummy recording");
            await UiTap(UiPad0,JoyButton.Start);await UiActivate("Loop recorded dummy");UiRequire(lab.PlayingDummy,"Controller starts recorded dummy playback");
            await UiTap(UiPad0,JoyButton.Start);await UiActivate("← Back");UiRequire(screen=="title","Controller exits training");

            UiStep("manual and private-screen navigation");
            await UiActivate("Field manual");UiRequire(screen=="help","Controller opens field manual");
            await UiActivate("Move list");UiRequire(screen=="moves","Controller opens move list");
            await UiActivate("Page ");await UiFrames(2);UiRequire(ui.GetChildren().OfType<Button>().Any(b=>b.Text.Contains("Page 2/5")),"Controller pages through command reference");
            await UiTap(UiPad0,JoyButton.B);UiRequire(screen=="title","Controller returns from command reference");
            await UiActivate("04   Private match");UiRequire(screen=="network_menu","Controller reaches private match setup");
            await UiActivate("Edit with pad");UiRequire(screen=="pad_text","Controller opens on-screen IP editor");
            await UiActivate("Clear");foreach(char c in "127.0.0.1")await UiActivate(c.ToString());
            await UiCapture("ui-07a-controller-text-entry");await UiActivate("Done");UiRequire(networkAddress=="127.0.0.1","Controller commits typed IP address");
            await UiActivate("Edit with pad",1);await UiActivate("Clear");foreach(char c in "27961")await UiActivate(c.ToString());await UiActivate("Done");
            UiRequire(networkPort=="27961","Controller commits typed UDP port");
            await UiActivate("Edit with pad",2);await UiActivate("Clear");foreach(char c in "test")await UiActivate(c.ToString());await UiActivate("Done");
            UiRequire(networkCode=="test","Controller commits typed match phrase");_uiPrivateTextEntryTested=true;
            await UiCapture("ui-07-private-setup");
            await UiShopOnlyPresentationEvidence();
            await UiCompletedMatchRematch();
            await UiBuyablesCatalogEvidence();
            UiRequire(!settings.PersistenceEnabled,"Software test does not persist user's mappings/preferences");
            UiRequire(Input.GetConnectedJoypads().SequenceEqual(_uiActualHardware),"Software provider did not fabricate physical Godot connections");
            FinishUiSmoke(true,"");
        }
        catch(Exception ex){FinishUiSmoke(false,ex.ToString());}
    }

    private async Task UiCompletedMatchRematch()
    {
        UiStep("verified completed replay and controller rematch");
        // Generate a complete competitive record using production bot inputs, then
        // verify it independently before using its terminal state in the real results UI.
        var config=new MatchConfig{Fighter0="rook",Fighter1="vale",SessionId="ui-result-replay"};
        var completed=new Simulation(content,config);var history=new ReplayRecorder(content,config);
        var left=new BotController(0,BotMode.Adaptive,17);var right=new BotController(1,BotMode.Adaptive,31);
        int operations=0;
        while(completed.Phase!=MatchPhase.MatchOver&&operations++<65000)
        {
            switch(completed.Phase)
            {
                case MatchPhase.Preparation:history.CommitPreparation(completed,left.ChoosePreparation(completed),right.ChoosePreparation(completed));break;
                case MatchPhase.PendingResult:history.SettleRound(completed,completed.Tick);break;
                case MatchPhase.RoundResult:history.NextRound(completed);break;
                default:history.Step(completed,left.Next(completed),right.Next(completed));break;
            }
            if(operations%1200==0)await UiFrames(1);
        }
        UiRequire(completed.Phase==MatchPhase.MatchOver,"Legal production inputs complete a competitive match fixture");
        string path=System.IO.Path.Combine(evidenceDir,"ui-completed-match.replay.json");history.Save(path);
        var verified=new ReplayPlayer(content,ReplayFormat.Load(path,content.ContentHash));int replaySteps=0;
        while(verified.FrameAdvance()){if(++replaySteps%1200==0)await UiFrames(1);}
        UiRequire(verified.Simulation.Hash()==completed.Hash(),"Completed replay verifies every hash and wallet before result display");
        sim=verified.Simulation;mode="local";fighter=["rook","vale"];art=[0,0];paused=true;recorder=null;replay=null;lab=null;debugBoxes=false;
        UpdateArena();FinishMatch();await UiFrames(3);
        UiRequire(screen=="results"&&sim.CompletedRounds>0,"Real completed-match result screen opens");
        await UiCapture("ui-08-verified-match-result");
        var finished=sim;await UiActivate("Run it back");
        UiRequire(screen=="prep"&&sim!=finished,"Controller result rematch creates a fresh match");
        UiRequire(sim!.Players.All(p=>p.Credits==600),"Rematch resets both wallets to 600 CR");
        UiRequire(sim.Players.All(p=>p.ScoreHalfPoints==0&&p.Leases.Count==0)&&sim.RoundId==1,"Rematch resets points, leases and round number");
        await UiCapture("ui-09-rematch-fresh-wallet");
    }

    private async Task UiFrames(int count){for(int i=0;i<count;i++)await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);}
    private async Task UiTap(int device,JoyButton button)
    {
        var press=new InputEventJoypadButton{Device=device,ButtonIndex=button,Pressed=true};
        GD.Print($"UI_JOY {device} {button} accept={press.IsActionPressed("ui_accept")} focus={(GetViewport().GuiGetFocusOwner() as Button)?.Text} screen={screen}");
        Input.ParseInputEvent(press);await UiFrames(2);
        Input.ParseInputEvent(new InputEventJoypadButton{Device=device,ButtonIndex=button,Pressed=false});await UiFrames(3);
    }
    private async Task UiActivate(string prefix,int index=0)
    {
        var b=ui.GetChildren().OfType<Button>().Where(b=>!b.IsQueuedForDeletion()&&b.Text.StartsWith(prefix,StringComparison.Ordinal)).Skip(index).FirstOrDefault();
        if(b==null)throw new InvalidOperationException($"No button '{prefix}' on screen '{screen}'. Available: "+string.Join(" | ",ui.GetChildren().OfType<Button>().Where(x=>!x.IsQueuedForDeletion()).Select(x=>x.Text)));
        b.GrabFocus();await UiFrames(2);await UiTap(UiPad0,JoyButton.A);
    }
    private async Task UiCapture(string name)
    {
        if(DisplayServer.GetName()=="headless")return;
        Capture(name);await UiFrames(4);
        string path=System.IO.Path.Combine(evidenceDir,name+".png");UiRequire(System.IO.File.Exists(path),"Viewport capture exists: "+name);_uiSmokeCaptures.Add(path);
    }
    private void UiStep(string name){_uiSmokeStep=name;GD.Print("UI_SOFTWARE_STEP "+name);}
    private void UiRequire(bool condition,string name)
    {
        _uiSmokeChecks.Add(new{name,passed=condition,screen,simulationTick=sim?.Tick,phase=sim?.Phase.ToString(),elapsedSeconds=_uiSmokeElapsed});
        if(!condition)throw new InvalidOperationException(name);GD.Print("UI_SOFTWARE_PASS "+name);
    }
    private void FinishUiSmoke(bool success,string error)
    {
        if(!_uiSmokeActive)return;_uiSmokeActive=false;
        if(_uiReplayFixture.Length>0&&System.IO.File.Exists(_uiReplayFixture))System.IO.File.Delete(_uiReplayFixture);
        var report=new
        {
            scenario="controller_menu_flow",success,error,step=_uiSmokeStep,elapsedSeconds=_uiSmokeElapsed,
            inputMethod="Godot Input.ParseInputEvent joypad pressed/released events with actual GUI focus and callbacks",
            softwareControllers=true,physicalControllersTested=false,physicalInventory=_uiActualHardware,
            settingsPersisted=false,privateNetworkTextEntryTested=_uiPrivateTextEntryTested,checks=_uiSmokeChecks,captures=_uiSmokeCaptures
        };
        System.IO.File.WriteAllText(System.IO.Path.Combine(evidenceDir,"controller-menu-flow.json"),JsonSerializer.Serialize(report,new JsonSerializerOptions{WriteIndented=true}));
        if(success)GD.Print("UI_SOFTWARE_PASS controller_menu_flow "+_uiSmokeChecks.Count+" assertions; physical device gate remains pending.");else GD.PushError("UI_SOFTWARE_FAIL "+error);
        QuitGame(success?0:8);
    }
}
