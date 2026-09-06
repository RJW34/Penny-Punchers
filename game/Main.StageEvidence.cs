using Godot;
using StrikeLedger.Core;
using StrikeLedger.Presentation;
using System.Text.Json;

public partial class Main
{
    // Software validation of legal Core input through the production adapter and
    // actual native rendering. It does not certify physical input or human feel.
    async Task UiStageCameraEvidence()
    {
        var priorSim=sim;var priorRecorder=recorder;string priorMode=mode;bool priorPaused=paused,priorBoxes=debugBoxes;
        var results=new List<object>();
        bool InFrame()=>arena.FighterScreenBounds.All(b=>b.Position.X>=CameraFrame.SafeLeft-1&&b.End.X<=CameraFrame.SafeRight+1&&b.Position.Y>=CameraFrame.SafeTop-1&&b.End.Y<=CameraFrame.SafeBottom+1);
        try
        {
            recorder=null;mode="local";paused=true;debugBoxes=false;Clear("fight");
            foreach(string stage in new[]{"foundry","grid","marist_green","marist_gates"})
            {
                UiStep("legal movement / "+stage);
                sim=new Simulation(content,new MatchConfig{Fighter0="rook",Fighter1="vale",StageId=stage,SessionId="stage-native-"+stage});sim.BeginFight();
                ResetPresentationTimeline();UpdateArena();await UiFrames(3);
                bool contained=InFrame();int steps=0;
                void Step(byte a,byte b,Buttons ab=Buttons.None,Buttons bb=Buttons.None)
                {
                    var result=sim.Step(new(0,sim.Tick,a,ab),new(1,sim.Tick,b,bb));ObserveChanges(result);UpdateArena();contained&=InFrame();steps++;
                }
                for(int i=0;i<180;i++)Step(4,6);
                UiRequire(sim.Players[0].X==sim.Stage.Left+16000&&sim.Players[1].X==sim.Stage.Right-16000,stage+": legal away inputs reach both opposite bounds");
                UiRequire(contained,stage+": both full cel envelopes remain within safe frame throughout legal separation");
                await UiCapture("stage-"+stage+"-opposite-corners");
                int maxY=0;
                for(int i=0;i<42;i++)
                {
                    Step(i==0?(byte)8:(byte)5,i==0?(byte)8:(byte)5);
                    maxY=Math.Max(maxY,sim.Players.Max(p=>p.Y));
                    if(i==18){await UiFrames(3);await UiCapture("stage-"+stage+"-corner-jumps");}
                }
                UiRequire(maxY>80000&&contained,stage+": simultaneous legal jumps preserve both full silhouettes");
                // Move together, land one real attack, then verify a wall-clock
                // pause cannot consume the impact's simulation-tick lifetime.
                int approachTicks=0;
                int ContactSeparation()=>(sim.Pushbox(sim.Players[0]).Width+sim.Pushbox(sim.Players[1]).Width)/2+1000;
                while(approachTicks<240&&Math.Abs(sim.Players[1].X-sim.Players[0].X)>ContactSeparation())
                {
                    Step(6,4);approachTicks++;
                }
                UiRequire(Math.Abs(sim.Players[1].X-sim.Players[0].X)<=ContactSeparation(),stage+": bounded legal approach reaches pushbox contact range");
                bool contact=false;
                for(int i=0;i<32&&!contact;i++)
                {
                    var result=sim.Step(new(0,sim.Tick,5,i==0?Buttons.HP:Buttons.None),new(1,sim.Tick,5,Buttons.None));ObserveChanges(result);UpdateArena();steps++;
                    contact=result.Events.Any(e=>e.Kind==CombatEventKind.Hit);
                }
                UiRequire(contact,stage+": real heavy contact drives the production event adapter");
                int impacts=arena.ImpactCount;var before=arena.RenderedCels.ToArray();long tick=sim.Tick;
                await UiFrames(12);
                UiRequire(impacts>0&&arena.ImpactCount==impacts&&sim.Tick==tick&&arena.RenderedCels.SequenceEqual(before),stage+": paused contact retains sparks and fighter cel");
                await UiCapture("stage-"+stage+"-paused-contact");
                results.Add(new{stage,legalInputSteps=steps,approachTicks,maxJumpY=maxY,contained,contact,zoom=arena.Framing.Zoom,worldX=arena.Framing.WorldX,passed=true});
            }
        }
        finally
        {
            sim=priorSim;recorder=priorRecorder;mode=priorMode;paused=priorPaused;debugBoxes=priorBoxes;ResetPresentationTimeline();Clear("fight");UpdateArena();
            System.IO.File.WriteAllText(System.IO.Path.Combine(evidenceDir,"stage-camera-evidence.json"),JsonSerializer.Serialize(new{utc=DateTime.UtcNow,scope="Actual legal input frames through Core and production presentation; software fixture, not physical controller certification",safeRect=new[]{40,174,1240,664},results},new JsonSerializerOptions{WriteIndented=true}));
        }
    }
}
