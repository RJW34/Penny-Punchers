using Godot;
using StrikeLedger.Core;
using System.Text.Json;

public partial class Main
{
    // This is a rendered training test, not recorded human or physical-controller play.
    // Training setup is confined to segment boundaries; every action uses legal InputFrame input.
    private bool _combatShowcaseActive, _showcaseCorrection, _showcaseTechSent, _showcaseProjectileOwnerFree;
    private int _showcaseCase=-1, _showcaseFrame, _showcaseHold, _showcaseFailures;
    private double _showcaseElapsed;
    private SimulationSnapshot? _showcaseBaseline;
    private Label? _showcaseTitle, _showcaseDetail;
    private readonly HashSet<int> _showcaseParryFrames=[];
    private readonly HashSet<string> _showcaseShots=[];
    private readonly List<CombatEvent> _showcaseEvents=[];
    private readonly List<object> _showcaseTrace=[], _showcaseChecks=[], _showcaseSetups=[];
    private static readonly string[] ShowcaseNames=[
        "01 / FOOTWORK", "02 / FREE PROJECTILE", "03 / EXACT-WALLET EX", "04 / HIGH PARRY",
        "05 / LOW PARRY", "06 / PROJECTILE PARRY", "07 / THROW", "08 / THROW TECH",
        "09 / FIVE-HIT SUPER PARRY", "10 / VALE CHARGE", "11 / ROLLBACK TRAINING"
    ];
    private static readonly string[] ShowcaseDetails=[
        "Walk · double-tap dash · forward jump · landing. No leases or credits.",
        "Rook: down, down-forward, forward + LP. A free special at zero credits.",
        "Rook: quarter-circle + LP+MP. Exactly 300 CR debited at startup, once.",
        "Vale taps toward on the incoming jab. Fresh directional edge; zero damage.",
        "Vale taps down on the incoming crouching kick. Low parry; zero damage.",
        "Vale taps toward as the EX pulse reaches her. Projectile freezes; owner stays free.",
        "LP+LK captures at close range. Damage follows the throw-tech window.",
        "Defender presses LP+LK inside the tech window. Both fighters separate unharmed.",
        "Double quarter-circle + LP spends 900 CR. Five separately timed parry edges.",
        "Vale holds back for 45 frames, then forward + LP. Free charge projectile.",
        "UNCONFIRMED preview: paid super K.O. No presentation events or settlement released."
    ];

    void BeginCombatShowcase()
    {
        _combatShowcaseActive=true;_showcaseElapsed=0;
        System.IO.Directory.CreateDirectory(evidenceDir);
        settings=new GameSettings{PersistenceEnabled=false,Master=.65f,Music=.3f,Sfx=.7f,Shake=false};
        input=new InputRouter(settings);arena.SetVolumes(settings.Master,settings.Music,settings.Sfx);
        mode="training";paused=true;frameStep=false;lab=null;peer=null;recorder=null;replay=null;
        trainingMode="SCRIPTED LEGAL INPUT / FRAME ADVANCE";Clear("fight");
        var layer=new CanvasLayer{Layer=80};AddChild(layer);
        var box=new ColorRect{Position=new Vector2(0,609),Size=new Vector2(1280,78),Color=new Color(.025f,.055f,.07f,1),MouseFilter=Control.MouseFilterEnum.Ignore};layer.AddChild(box);
        _showcaseTitle=new Label{Position=new Vector2(24,616),MouseFilter=Control.MouseFilterEnum.Ignore};
        _showcaseTitle.AddThemeFontSizeOverride("font_size",20);_showcaseTitle.AddThemeColorOverride("font_color",Gold);layer.AddChild(_showcaseTitle);
        _showcaseDetail=new Label{Position=new Vector2(24,651),Size=new Vector2(1232,34),AutowrapMode=TextServer.AutowrapMode.WordSmart,MouseFilter=Control.MouseFilterEnum.Ignore};
        _showcaseDetail.AddThemeFontSizeOverride("font_size",15);_showcaseDetail.AddThemeColorOverride("font_color",Cream);layer.AddChild(_showcaseDetail);
        BeginShowcaseSegment();
    }

    void BeginShowcaseSegment()
    {
        _showcaseCase++;_showcaseFrame=0;_showcaseHold=0;_showcaseCorrection=false;_showcaseTechSent=false;_showcaseProjectileOwnerFree=false;
        _showcaseEvents.Clear();_showcaseParryFrames.Clear();
        if(_showcaseCase>=ShowcaseNames.Length){FinishCombatShowcase();return;}
        int credits=_showcaseCase is 2 or 5?300:_showcaseCase==8?3600:_showcaseCase==10?900:0;
        int x0=_showcaseCase==0?240000:_showcaseCase==1?280000:_showcaseCase==2?270000:350000;
        int x1=_showcaseCase==0?560000:_showcaseCase==1?470000:_showcaseCase==2?600000:_showcaseCase==5?450000:_showcaseCase==9?550000:385000;
        sim=new Simulation(content,new MatchConfig{Fighter0=_showcaseCase==9?"vale":"rook",Fighter1=_showcaseCase==9?"rook":"vale",Training=true,StageId="grid",SessionId="rendered-training-"+_showcaseCase});
        fighter=[sim.Config.Fighter0,sim.Config.Fighter1];art=[0,0];
        sim.TrainingReset(credits);sim.SetTrainingState(0,x:x0);sim.SetTrainingState(1,x:x1,health:_showcaseCase==10?1:1000);
        _showcaseBaseline=sim.Capture();debugBoxes=_showcaseCase is 3 or 4 or 5;
        inputHistory[0].Clear();inputHistory[1].Clear();arena.ResetEffects();
        _showcaseTitle!.Text=ShowcaseNames[_showcaseCase]+"   /   SCRIPTED TRAINING · ACTUAL CORE INPUTS";
        _showcaseDetail!.Text=ShowcaseDetails[_showcaseCase];
        _showcaseSetups.Add(new{segment=ShowcaseNames[_showcaseCase],trainingOnly=true,fighters=fighter,credits,x0,x1,health1=sim.Players[1].Health,startHash=sim.Hash()});
        GD.Print("SHOWCASE "+ShowcaseNames[_showcaseCase]);UpdateArena();QueueRedraw();
    }

    void TickCombatShowcase(double delta)
    {
        if(!_combatShowcaseActive||sim==null)return;
        _showcaseElapsed+=delta;
        if(_showcaseElapsed>110){ShowcaseCheck(false,"Driver finishes within 110 seconds");FinishCombatShowcase();return;}
        if(_showcaseCase==10&&!_showcaseCorrection&&sim.Phase==MatchPhase.PendingResult)
        {
            if(_showcaseHold++==0)
            {
                ShowcaseCheck(sim.Players[1].Health==0&&sim.LastSettlement==null,"Speculative paid K.O. exists, with no settlement");
                ShowcaseCapture("11-speculative-ko");
            }
            if(_showcaseHold<65)return;
            sim.Restore(_showcaseBaseline!);_showcaseCorrection=true;_showcaseFrame=29;_showcaseParryFrames.Clear();
            _showcaseTrace.Add(new{segment=11,operation="restore-before-paid-startup-and-resimulate",hash=sim.Hash(),presentSpeculativeEvents=false});
            _showcaseEvents.Clear();arena.ResetEffects();UpdateArena();
            _showcaseDetail!.Text="CORRECTED branch: late parry inputs erase the K.O. One 900 CR debit; no payout, no score.";
        }
        byte d0=5,d1=5;Buttons b0=Buttons.None,b1=Buttons.None;
        int f=_showcaseFrame++;
        void Qcf(int start,Buttons button)
        {
            if(f==start)d0=2;else if(f==start+1)d0=3;else if(f==start+2){d0=6;b0=button;}
        }
        void Super()
        {
            if(f is >=30 and <=35){d0=new byte[]{2,3,6,2,3,6}[f-30];if(f==35)b0=Buttons.LP;}
        }
        void FiveParries()
        {
            var p=sim.Players[0];
            if(p.ActionId=="super_1"&&p.Hitstop==0&&sim.FullFreeze==0&&p.ActionFrame is 5 or 9 or 13 or 17 or 21&&_showcaseParryFrames.Add(p.ActionFrame))d1=4;
        }
        switch(_showcaseCase)
        {
            case 0:
                if(f is >=30 and <50||f is 63 or 65)d0=6;
                if(f==110)d0=9;
                if(f is 210 or 212)d0=4;
                break;
            case 1:Qcf(30,Buttons.LP);break;
            case 2:Qcf(30,Buttons.LP|Buttons.MP);break;
            case 3:if(f==30)b0=Buttons.LP;if(f==33)d1=4;break;
            case 4:if(f==30){d0=2;b0=Buttons.LK;}if(f==33)d1=2;break;
            case 5:
                Qcf(30,Buttons.LP|Buttons.MP);
                var projectile=sim.Projectiles.FirstOrDefault();
                if(projectile!=null&&projectile.Hitstop==0&&projectile.ContactCooldown==0&&projectile.X+projectile.Vx+projectile.Definition.Width/2>=sim.Players[1].X-18000&&lastInputs[1].Direction!=4)d1=4;
                break;
            case 6:if(f==30)b0=Buttons.LP|Buttons.LK;break;
            case 7:
                if(f==30)b0=Buttons.LP|Buttons.LK;
                if(!_showcaseTechSent&&_showcaseEvents.Any(e=>e.Kind==CombatEventKind.Throw&&e.Detail=="capture")){b1=Buttons.LP|Buttons.LK;_showcaseTechSent=true;}
                break;
            case 8:Super();FiveParries();break;
            case 9:if(f is >=20 and <65)d0=4;if(f==65){d0=6;b0=Buttons.LP;}break;
            case 10:Super();if(_showcaseCorrection)FiveParries();break;
        }
        lastInputs=[new InputFrame(0,sim.Tick,d0,b0),new InputFrame(1,sim.Tick,d1,b1)];
        var step=sim.Step(lastInputs[0],lastInputs[1]);
        bool publish=_showcaseCase!=10||_showcaseCorrection;
        _showcaseTrace.Add(new{segment=_showcaseCase+1,displayFrame=f,tick=step.Tick,revision=_showcaseCorrection?1:0,d0,d1,b0=b0.ToString(),b1=b1.ToString(),presentationEventsReleased=publish,events=step.Events,hash=step.Hash});
        _showcaseEvents.AddRange(step.Events);
        if(publish)ObserveChanges(step);
        foreach(var e in step.Events)
        {
            if(e.Kind==CombatEventKind.Parry&&_showcaseCase==5)_showcaseProjectileOwnerFree|=sim.Players[0].Hitstop==0&&sim.Projectiles.Any(p=>p.Hitstop>0);
            if(e.Kind is CombatEventKind.Parry or CombatEventKind.ThrowTech or CombatEventKind.ProjectileSpawn or CombatEventKind.Jump or CombatEventKind.Throw)
                ShowcaseCapture($"{_showcaseCase+1:00}-{(_showcaseCorrection?"corrected-":"")}{e.Kind.ToString().ToLowerInvariant()}");
        }
        UpdateArena();QueueRedraw();
        if(f>=299)
        {
            VerifyShowcaseSegment();
            BeginShowcaseSegment();
        }
    }

    void VerifyShowcaseSegment()
    {
        if(sim==null)return;
        bool Has(CombatEventKind kind,string move="")=>_showcaseEvents.Any(e=>e.Kind==kind&&(move==""||e.MoveId==move));
        switch(_showcaseCase)
        {
            case 0:ShowcaseCheck(Has(CombatEventKind.Dash)&&Has(CombatEventKind.Jump)&&Has(CombatEventKind.Land),"Legal movement produces dash, jump and landing events");break;
            case 1:ShowcaseCheck(Has(CombatEventKind.ProjectileSpawn,"pulse_l")&&Has(CombatEventKind.Hit)&&sim.Players[0].Credits==0,"Free projectile launches and hits at zero credits");break;
            case 2:ShowcaseCheck(Has(CombatEventKind.ProjectileSpawn,"pulse_ex")&&sim.Players[0].Credits==0&&sim.Players[0].SpendReceipts.Count==1,"EX starts at exactly 300 CR and has one receipt");break;
            case 3:ShowcaseCheck(_showcaseEvents.Any(e=>e.Kind==CombatEventKind.Parry&&e.Detail.StartsWith("High:"))&&sim.Players[1].Health==1000,"Fresh toward edge high-parries with zero damage");break;
            case 4:ShowcaseCheck(_showcaseEvents.Any(e=>e.Kind==CombatEventKind.Parry&&e.Detail.StartsWith("Low:"))&&sim.Players[1].Health==1000,"Fresh down edge low-parries with zero damage");break;
            case 5:ShowcaseCheck(Has(CombatEventKind.Parry)&&_showcaseProjectileOwnerFree,"Projectile parry freezes its source projectile without freezing the owner");break;
            case 6:ShowcaseCheck(_showcaseEvents.Any(e=>e.Kind==CombatEventKind.Throw&&e.Detail=="damage")&&sim.Players[1].Health<1000,"Untouched throw reaches delayed damage");break;
            case 7:ShowcaseCheck(Has(CombatEventKind.ThrowTech)&&sim.Players.All(p=>p.Health==1000),"Legal defender LP+LK tech prevents throw damage");break;
            case 8:ShowcaseCheck(_showcaseEvents.Count(e=>e.Kind==CombatEventKind.Parry)==5&&sim.Players[1].Health==1000&&sim.Players[0].Credits==2700&&sim.Players[0].SpendReceipts.Count==1,"Five fresh parry inputs defend all super hits; exactly one 900 CR debit");break;
            case 9:ShowcaseCheck(Has(CombatEventKind.ProjectileSpawn,"pulse_l")&&!Has(CombatEventKind.Spend),"45-frame charge produces Vale's free projectile");break;
            case 10:
                ShowcaseCheck(_showcaseCorrection&&_showcaseEvents.Count(e=>e.Kind==CombatEventKind.Parry)==5&&sim.Players[1].Health==1&&sim.Phase==MatchPhase.Fight,"Restored branch with late parries removes the speculative K.O.");
                ShowcaseCheck(sim.Players[0].Credits==0&&sim.Players[0].SpendReceipts.Count==1&&sim.Players.All(p=>p.ScoreHalfPoints==0)&&sim.LastSettlement==null,"Correction preserves one debit, zero points and no settlement");break;
        }
    }

    void ShowcaseCheck(bool passed,string assertion)
    {
        _showcaseChecks.Add(new{segment=_showcaseCase+1,passed,assertion});if(!passed)_showcaseFailures++;
        GD.Print($"SHOWCASE {(passed?"PASS":"FAIL")} {assertion}");
    }
    void ShowcaseCapture(string name)
    {
        if(DisplayServer.GetName()=="headless"||pendingCapture!=""||!_showcaseShots.Add(name))return;
        Capture("combat-"+name);
    }
    async void FinishCombatShowcase()
    {
        if(!_combatShowcaseActive)return;_combatShowcaseActive=false;
        _showcaseTitle!.Text=$"SCRIPTED TRAINING COMPLETE / {_showcaseChecks.Count-_showcaseFailures} CHECKS PASSED / {_showcaseFailures} FAILED";
        _showcaseDetail!.Text="All attacks came from legal directional/button frames. Training setup and snapshot correction are explicitly recorded.";
        Capture("combat-final");
        var report=new{success=_showcaseFailures==0,kind="rendered scripted training with legal core InputFrames",physicalControllerClaim=false,elapsedSeconds=_showcaseElapsed,renderer=DisplayServer.GetName(),setups=_showcaseSetups,checks=_showcaseChecks,captures=_showcaseShots,trace=_showcaseTrace,rollbackScope="Explicit training snapshot restore and resimulation; supplemental visual evidence, not a network transport test."};
        System.IO.File.WriteAllText(System.IO.Path.Combine(evidenceDir,"combat-visual-showcase.json"),JsonSerializer.Serialize(report,new JsonSerializerOptions{WriteIndented=true}));
        for(int i=0;i<12;i++)await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
        GD.Print($"COMBAT_SHOWCASE_RESULT success={_showcaseFailures==0} checks={_showcaseChecks.Count}");QuitGame(_showcaseFailures==0?0:9);
    }
}
