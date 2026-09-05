using System.Text.Json;
using StrikeLedger.App;
using StrikeLedger.Core;

internal static class TrainingDrillTests
{
    public static void Run(GameContent content,string output)
    {
        string directory=Path.Combine(output,"drills");Directory.CreateDirectory(directory);var results=new List<object>();
        foreach(var drill in TrainingSession.Drills)
        {
            string fighter=drill.Id is "charge_side_switch" or "red_parry" or "multihit_parry" or "eco_defense"?"vale":"rook";
            var t=new TrainingSession(content,fighter);t.Reset(drill.Id);t.DummyMode=BotMode.Passive;
            for(int n=0;n<7202;n++)t.FrameAdvance(5,Buttons.None);
            Require(t.Failed&&!t.Success,drill.Id+" must visibly fail on a no-input timeout");t.ExportRecording(Path.Combine(directory,drill.Id+"-timeout.json"));
            t.Reset(drill.Id);t.DummyMode=BotMode.Passive;var fixtures=new List<object>();
            void Capture(string reason)=>fixtures.Add(new{reason,tick=t.Simulation.Tick,recordingIndex=t.Recording.Count,snapshot=Convert.ToBase64String(t.Simulation.Capture().Bytes)});
            void Place(bool left=false)
            {t.Simulation.SetTrainingState(0,x:left?16000:720000,y:0,health:1000,stun:0);t.Simulation.SetTrainingState(1,x:left?48000:752000,y:0,health:1000,stun:0,credits:3600);Capture("training position/health controls");}
            StepResult Step(byte relative=5,Buttons buttons=Buttons.None)=>t.FrameAdvance(CoreMath.RelativeDirection(relative,t.Simulation.Players[0].Facing),buttons);
            void Wait(int ticks){for(int n=0;n<ticks;n++)Step();}
            void Until(Func<bool> condition,int maximum=300){for(int n=0;n<maximum&&!condition();n++)Step();Require(condition(),drill.Id+" fixture condition timed out");}
            void Motion(Buttons buttons,bool twice=false){foreach(byte d in twice?new byte[]{2,3,6,2,3}:new byte[]{2,3})Step(d);Step(6,buttons);}
            void Enemy(string move){var status=t.Simulation.TryStartAction(1,move);Require(status is ActivationStatus.Free or ActivationStatus.Paid,"Illegal dummy fixture "+move);Capture("explicit deterministic dummy action fixture: "+move);}
            void Fresh(){t.Simulation.TrainingReset(drill.Id is "insufficient_funds" or "eco_defense"?0:3600);Place(true);Capture("training quick-reset fixture; drill progress preserved");}
            Place();Capture("initial drill fixture");
            switch(drill.Id)
            {
                case "motion_both_facings":
                    Motion(Buttons.LP);Wait(120);t.SwapSides();Capture("training swap-sides control");Step();Motion(Buttons.LP);Wait(40);break;
                case "charge_side_switch":
                    for(int n=0;n<45;n++)Step(4);t.SwapSides();Capture("cross-up after full back charge");Step();for(int n=0;n<45;n++)Step(4);Step(6,Buttons.LP);Wait(10);break;
                case "same_tick_ex":Motion(Buttons.LP|Buttons.MP);Wait(10);break;
                case "link_cancel":
                    Step(5,Buttons.LP);Until(()=>t.Simulation.Players[0].Contact);Until(()=>t.Simulation.Players[0].Actionable);Step(5,Buttons.LP);Wait(35);
                    Place();Step(5,Buttons.LP);Until(()=>t.Simulation.Players[0].Contact);Motion(Buttons.LP);Wait(40);break;
                case "target_combo":
                    Step(5,Buttons.LP);Until(()=>t.Simulation.Players[0].Contact);Until(()=>t.Simulation.Players[0].Hitstop==0);Step(5,Buttons.MP);Wait(20);break;
                case "high_low_air_parry":
                    foreach(string move in new[]{"s_lp","c_lk","j_lp"})
                    {
                        Fresh();if(move=="j_lp"){t.Simulation.SetTrainingState(0,y:40000);t.Simulation.SetTrainingState(1,y:40000);Capture("air parry position control");}
                        Enemy(move);int arm=t.Simulation.Content.Fighters[t.Simulation.Players[1].FighterId].Move(move).Hitboxes.Min(h=>h.Start)-1;bool armed=false;
                        for(int n=0;n<50;n++){bool tap=!armed&&t.Simulation.Players[1].ActionFrame==arm&&t.Simulation.Players[1].Hitstop==0;Step(tap?(move=="c_lk"?(byte)2:(byte)6):(byte)5);armed|=tap;}
                    }break;
                case "red_parry":
                    Fresh();Enemy("super_1");bool blocked=false;for(int n=0;n<100&&!blocked;n++)blocked=Step(4).Events.Any(e=>e.Kind==CombatEventKind.Block&&e.Seat==0);Require(blocked,"Red drill first hit did not block");
                    bool redArmed=false;for(int n=0;n<100&&!t.Success;n++){bool tap=!redArmed&&t.Simulation.Players[1].Hitstop==0&&t.Simulation.Players[1].ActionFrame==8;Step(tap?(byte)6:(byte)5);redArmed|=tap;}break;
                case "multihit_parry":
                    Fresh();Enemy("super_1");var armedFrames=new HashSet<int>();for(int n=0;n<240;n++){var enemy=t.Simulation.Players[1];bool tap=enemy.ActionId=="super_1"&&enemy.Hitstop==0&&t.Simulation.FullFreeze==0&&new[]{5,9,13,17,21}.Contains(enemy.ActionFrame)&&armedFrames.Add(enemy.ActionFrame);Step(tap?(byte)6:(byte)5);}break;
                case "jumpin_antiair":
                    for(int offset=0;offset<29&&!t.Success;offset++)
                    {Fresh();t.DummyMode=BotMode.Jump;Until(()=>t.Simulation.Players[1].Y>0);Wait(offset);Step(5,Buttons.HP);Wait(45);}break;
                case "throw_tech":Enemy("throw_forward");Wait(5);Step(5,Buttons.LP|Buttons.LK);Wait(30);break;
                case "kara_throw":Step(6,Buttons.HP);Step(5,Buttons.LP|Buttons.LK);Wait(25);break;
                case "quickrise_reversal":
                    Fresh();Enemy("rise_l");Until(()=>t.Simulation.Players[0].KnockdownTicks>0&&t.Simulation.Players[0].Grounded&&t.Simulation.Players[0].Hitstop==0);Step(2);
                    Until(()=>t.Simulation.Players[0].KnockdownTicks<=2&&t.Simulation.Players[0].Hitstop==0);Step(5,Buttons.LP);Wait(10);break;
                case "hitconfirm_super":Step(5,Buttons.LP);Until(()=>t.Simulation.Players[0].Contact);Motion(Buttons.HP,true);Wait(50);break;
                case "insufficient_funds":Motion(Buttons.LP|Buttons.MP);Wait(10);break;
                case "eco_defense":
                    Fresh();Enemy("super_1");for(int n=0;n<150;n++)Step(4);Place(true);Step(5,Buttons.HP);Wait(40);break;
                case "rollback_spend":Motion(Buttons.LP|Buttons.MP);Wait(10);t.SaveCheckpoint();Wait(15);t.RestoreCheckpoint();Capture("restore paid-startup checkpoint including dummy state");break;
                default:throw new InvalidOperationException("Unimplemented drill trace "+drill.Id);
            }
            t.ExportRecording(Path.Combine(directory,drill.Id+"-success.json"));File.WriteAllText(Path.Combine(directory,drill.Id+"-fixtures.json"),JsonSerializer.Serialize(fixtures));
            results.Add(new{drill.Id,timeoutFailed=true,success=t.Success,t.Feedback,frames=t.Recording.Count,hash=t.Simulation.Hash()});
            File.WriteAllText(Path.Combine(directory,"results.json"),JsonSerializer.Serialize(new{build=ReplayFormat.Build,contentHash=content.ContentHash,results},new JsonSerializerOptions{WriteIndented=true}));
            Require(t.Success,drill.Id+" evaluator did not pass its real input/fixture trace: "+t.Feedback);
            Console.WriteLine("PASS drill "+drill.Id);
        }
    }
    static void Require(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
}
