using System.Security.Cryptography;
using System.Text.Json;
using StrikeLedger.Core;

var content=GameContent.Load(args[0]);var output=Path.GetFullPath(args[1]);Directory.CreateDirectory(output);
var measurements=new List<object>();
void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
Simulation Fresh(string fighter="rook",string art="art_1")
{
 var s=new Simulation(content,new MatchConfig{Fighter0=fighter,Fighter1=fighter=="rook"?"vale":"rook",Super0=art,SessionId="feel-measurement"});s.BeginFight();return s;
}
Clock State(Simulation s)=>new(s.Tick,s.TimerTicks,s.FullFreeze,s.Players.Select(p=>new PlayerClock(p.X,p.Y,p.Vx,p.Vy,p.ActionId,p.ActionFrame,p.Hitstop,p.Hitstun,p.Blockstun,p.ParryTicks,p.ParryRetry,p.BackCharge,p.DownCharge,p.JumpStart,p.LandingTicks,p.DashTicks,p.InputHistory.LastOrDefault()?.Tick??-1)).ToArray());
StepResult Step(Simulation s,List<Frame> frames,byte d0=5,Buttons b0=Buttons.None,byte d1=5,Buttons b1=Buttons.None)
{
 var before=State(s);var r=s.Step(new(0,s.Tick,d0,b0),new(1,s.Tick,d1,b1));frames.Add(new(before.Tick,d0,b0,d1,b1,before,State(s),r.Events.ToArray(),r.Hash));return r;
}
object Trace(Simulation s,string snapshot,List<Frame> frames)=>new{config=s.Config,initialCanonicalSnapshotBase64=snapshot,frames,finalHash=s.Hash()};
string Snapshot(Simulation s)=>Convert.ToBase64String(s.Capture().Bytes);
double Ms(long ticks)=>ticks*1000.0/60.0;
void CloseDistance(Simulation s)
{
 for(int i=0;i<180&&Math.Abs(s.Players[0].X-s.Players[1].X)>33000;i++)s.Step(new(0,s.Tick,6,Buttons.None),new(1,s.Tick,5,Buttons.None));
 for(int i=0;i<15;i++)s.Step(new(0,s.Tick,5,Buttons.None),new(1,s.Tick,5,Buttons.None));
 Require(Math.Abs(s.Players[0].X-s.Players[1].X)<=33000,"close-range setup failed");
}
foreach(string fighter in new[]{"rook","vale"})
{
 foreach(byte direction in new byte[]{6,4})
 {
  var s=Fresh(fighter);var frames=new List<Frame>();string start=Snapshot(s);int x=s.Players[0].X;
  for(int i=0;i<8;i++)Step(s,frames,direction);
  int moved=s.Players[0].X-x;Step(s,frames);int stopDelta=frames[^1].After.Players[0].X-frames[^1].Before.Players[0].X;
  var deltas=frames.Take(8).Select(f=>f.After.Players[0].X-f.Before.Players[0].X).ToArray();Require(deltas.Distinct().Count()==1&&moved!=0&&stopDelta==0,"walk constant speed / immediate neutral stop measurement");
  measurements.Add(new{id="walk_and_stop",fighter,direction,observedDeltasPerStep=deltas,totalWalkDisplacement=moved,neutralInputStopDisplacement=stopDelta,neutralStopAdditionalTicks=0,trace=Trace(s,start,frames)});
 }
 foreach(byte direction in new byte[]{6,4})
 {
  var s=Fresh(fighter);var frames=new List<Frame>();string start=Snapshot(s);Step(s,frames,direction);Step(s,frames);int onset=frames.Count;int x=s.Players[0].X;
  var trigger=Step(s,frames,direction);Require(trigger.Events.Any(e=>e.Kind==CombatEventKind.Dash),"double-tap did not trigger dash");
  while(s.Players[0].DashTicks>0&&frames.Count<80)Step(s,frames);Require(s.Players[0].DashTicks==0,"dash failed to end");int duration=frames.Count-onset;int distance=s.Players[0].X-x;Step(s,frames);
  Require(frames[^1].After.Players[0].X==frames[^1].Before.Players[0].X,"dash did not stop on neutral");
  measurements.Add(new{id="dash",fighter,direction,observedMovementSteps=duration,simulationMilliseconds=Ms(duration),observedDisplacement=distance,perStepDisplacements=frames.Skip(onset).Take(duration).Select(f=>f.After.Players[0].X-f.Before.Players[0].X),trace=Trace(s,start,frames)});
 }
 {
  var s=Fresh(fighter);var frames=new List<Frame>();string start=Snapshot(s);long input=s.Tick,launch=-1,land=-1;Step(s,frames,8);
  for(int i=0;i<180&&land<0;i++){var r=Step(s,frames);if(r.Events.Any(e=>e.Kind==CombatEventKind.Jump&&e.Seat==0))launch=r.Tick;if(r.Events.Any(e=>e.Kind==CombatEventKind.Land&&e.Seat==0))land=r.Tick;}
  Require(launch>=0&&land>launch,"normal vertical jump did not launch/land");int landingCounter=s.Players[0].LandingTicks;long landingState=s.Tick;
  while(s.Players[0].LandingTicks>0&&s.Tick-landingState<20)Step(s,frames);
  Require(s.Players[0].LandingTicks==0&&s.Players[0].Actionable,"landing recovery did not expire");
  measurements.Add(new{id="normal_vertical_jump",fighter,inputTick=input,launchEventTick=launch,landEventTick=land,inputToLaunchTickOffset=launch-input,launchToLandTickOffset=land-launch,observedAirborneStates=frames.Count(f=>f.After.Players[0].Y>0),peakHeight=frames.Max(f=>f.After.Players[0].Y),landingRecoveryCounterAtContact=landingCounter,observedLandingRecoveryAdvanceCalls=s.Tick-landingState,airborneSimulationMilliseconds=Ms(land-launch),trace=Trace(s,start,frames)});
 }
}
{
 var s=Fresh();var frames=new List<Frame>();string start=Snapshot(s);Step(s,frames,2);Step(s,frames,3);long completion=s.Tick;var r=Step(s,frames,6,Buttons.LP);var started=r.Events.Single(e=>e.Kind==CombatEventKind.ActionStarted&&e.Seat==0);
 Require(started.MoveId=="pulse_l"&&started.Tick==completion,"QCF completion did not start free special on sampled tick");
 measurements.Add(new{id="qcf_input_to_startup",fighter="rook",move=started.MoveId,completingInputTick=completion,actionStartedEventTick=started.Tick,observedTickOffset=started.Tick-completion,stepCallsIncludingCompletion=1,deviceOrRenderLatencyMeasured=false,trace=Trace(s,start,frames)});
}
foreach(var buttons in new[]{Buttons.LP,Buttons.MP,Buttons.HP})
{
 var s=Fresh();CloseDistance(s);var frames=new List<Frame>();string start=Snapshot(s);var attack=Step(s,frames,5,buttons);CombatEvent? contact=null;
 for(int i=0;i<90&&contact is null;i++){var r=Step(s,frames);contact=r.Events.FirstOrDefault(e=>e.Kind==CombatEventKind.Hit&&e.Seat==0);}
 Require(contact is not null,"normal strike did not contact");int[] initialStop=s.Players.Select(p=>p.Hitstop).ToArray();int startIndex=frames.Count;int timer=s.TimerTicks;
 while(s.Players.Any(p=>p.Hitstop>0)&&frames.Count-startIndex<60)Step(s,frames);
 var frozen=frames.Skip(startIndex).ToArray();int[] observed=Enumerable.Range(0,2).Select(seat=>frozen.Count(f=>f.Before.Players[seat].Hitstop>0)).ToArray();
 foreach(var f in frozen)for(int seat=0;seat<2;seat++)if(f.Before.Players[seat].Hitstop>0){var a=f.Before.Players[seat];var b=f.After.Players[seat];Require(a.X==b.X&&a.Y==b.Y&&a.ActionFrame==b.ActionFrame&&a.Hitstun==b.Hitstun&&a.Blockstun==b.Blockstun,"hitstop advanced a frozen player clock");}
 Require(initialStop.SequenceEqual(observed),"hitstop countdown/duration mismatch");int timerAdvanced=timer-s.TimerTicks;Step(s,frames);
 measurements.Add(new{id="normal_contact_hitstop",buttons=buttons.ToString(),move=contact!.MoveId,hitEventTick=contact.Tick,initialHitstopCounters=initialStop,observedFrozenPlayerSteps=observed,simulationMilliseconds=observed.Select(x=>Ms(x)),timerTicksAdvancedDuringHitstop=timerAdvanced,playerMotionActionAndStunClocksHeld=true,inputSampleClockContinues=frozen.All(f=>f.After.Players.All(p=>p.LastInputTick==f.InputTick)),trace=Trace(s,start,frames)});
}
{
 var s=Fresh();CloseDistance(s);var frames=new List<Frame>();string start=Snapshot(s);Step(s,frames,5,Buttons.LP);var move=content.Fighters[s.Players[0].FighterId].Move(s.Players[0].ActionId);
 while(s.Players[0].ActionFrame<move.Startup)Step(s,frames);
 var r=Step(s,frames,d1:4);var parry=r.Events.Single(e=>e.Kind==CombatEventKind.Parry);int[] counters=s.Players.Select(p=>p.Hitstop).ToArray();int from=frames.Count;int timer=s.TimerTicks;
 while(s.Players.Any(p=>p.Hitstop>0)&&frames.Count-from<60)Step(s,frames);
 var frozen=frames.Skip(from).ToArray();int[] observed=Enumerable.Range(0,2).Select(seat=>frozen.Count(f=>f.Before.Players[seat].Hitstop>0)).ToArray();Require(counters.SequenceEqual(observed)&&s.Players[1].Health==1000,"parry freeze measurement failed");
 foreach(var f in frozen)for(int seat=0;seat<2;seat++)if(f.Before.Players[seat].Hitstop>0){var a=f.Before.Players[seat];var b=f.After.Players[seat];Require(a.X==b.X&&a.Y==b.Y&&a.ActionFrame==b.ActionFrame&&a.ParryRetry==b.ParryRetry,"parry freeze advanced player clocks");}
 measurements.Add(new{id="high_parry_freeze",kind=parry.Detail,parryEventTick=parry.Tick,attackerInitialFreeze=counters[0],defenderInitialFreeze=counters[1],observedFrozenPlayerSteps=observed,simulationMilliseconds=observed.Select(x=>Ms(x)),timerTicksAdvancedDuringFreeze=timer-s.TimerTicks,playerMotionActionAndParryRetryClocksHeld=true,trace=Trace(s,start,frames)});
}
foreach(string fighter in new[]{"rook","vale"})foreach(string art in new[]{"art_1","art_2","art_3"})
{
 var s=Fresh(fighter,art);
 for(int i=0;i<6500&&s.Phase==MatchPhase.Fight;i++)
 {
  var p=s.Players[0];int gap=Math.Abs(p.X-s.Players[1].X);byte dir=gap>33000?(byte)(p.Facing==1?6:4):(byte)5;
  s.Step(new(0,s.Tick,dir,p.Actionable&&s.Tick%2==0?Buttons.HP:Buttons.None),new(1,s.Tick,5,Buttons.None));
 }
 Require(s.Phase==MatchPhase.PendingResult&&s.PendingResult!.WinnerSeat==0,"competitive funding round did not end in earned payout");
 long fundingTicks=s.Tick;s.SettleRound(s.Tick);var fundingReceipt=s.LastSettlement;s.NextRound();s.BeginFight();Require(s.Players[0].Credits==1800,"actual confirmed win did not fund1800 credits");
 var frames=new List<Frame>();string start=Snapshot(s);foreach(byte d in new byte[]{2,3,6,2,3})Step(s,frames,d,d1:6);
 int from=frames.Count;int wallet=s.Players[0].Credits;var r=Step(s,frames,6,Buttons.LP,6);var begun=r.Events.Single(e=>e.Kind==CombatEventKind.ActionStarted&&e.Seat==0);int remaining=s.FullFreeze;
 Require(begun.MoveId==art.Replace("art_","super_")&&remaining>0,"selected super did not enter arena freeze");
 while(s.FullFreeze>0&&frames.Count-from<90)Step(s,frames,d1:6);
 var frozen=frames.Skip(from).ToArray();Require(frozen.Length==remaining+1,"superfreeze onset-inclusive duration mismatch");
 foreach(var f in frozen){Require(f.Before.TimerTicks==f.After.TimerTicks,"superfreeze advanced round timer");for(int seat=0;seat<2;seat++){var a=f.Before.Players[seat];var b=f.After.Players[seat];Require(a.X==b.X&&a.Y==b.Y&&a.ActionFrame==b.ActionFrame&&a.Hitstop==b.Hitstop,"superfreeze advanced world action/motion/hitstop clocks");Require(b.LastInputTick==f.InputTick,"superfreeze lost sampled inputs");}}
 int chargeBefore=frozen[0].Before.Players[1].BackCharge,chargeAfter=frozen[^1].After.Players[1].BackCharge;Step(s,frames);
 Require(frames[^1].After.TimerTicks==frames[^1].Before.TimerTicks-1&&frames[^1].After.Players[0].ActionFrame==1,"superfreeze did not release world clocks");
 measurements.Add(new{id="selected_super_freeze",fighter,art,move=begun.MoveId,competitiveFundingRoundTicks=fundingTicks,confirmedFundingReceipt=fundingReceipt,startupEventTick=begun.Tick,paidDebit=wallet-s.Players[0].Credits,remainingCounterAfterStartupCall=remaining,observedFreezeCallsIncludingStartup=frozen.Length,simulationMilliseconds=Ms(frozen.Length),timerActionMotionHitstopClocksHeld=true,inputSamplingContinues=true,defenderBackChargeIncreaseDuringFreeze=chargeAfter-chargeBefore,firstThawedActionFrame=s.Players[0].ActionFrame,trace=Trace(s,start,frames)});
}
var corePath=typeof(Simulation).Assembly.Location;var report=new{passed=true,utc=DateTime.UtcNow,scope="Measured production core state before and after actual fixed60 Step calls; all simulations competitive and funded supers use actual confirmed round payouts. These are discrete simulation timings, not OS input, display latency, or rendered FPS measurements.",tickRate=60,coordinateUnits="Authored integer world units (1000 units per rendered logical unit)",coreAssemblyId=typeof(Simulation).Assembly.ManifestModule.ModuleVersionId.ToString(),coreBinaryPath=corePath,coreBinarySha256=Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(corePath))).ToLowerInvariant(),contentHash=content.ContentHash,measurements};
File.WriteAllText(Path.Combine(output,"feel-measurements.json"),JsonSerializer.Serialize(report,new JsonSerializerOptions{WriteIndented=true}));
Console.WriteLine($"PASS {measurements.Count} actual production movement/input/freeze measurements; Core SHA256 {report.coreBinarySha256}");
foreach(var m in measurements)Console.WriteLine(JsonSerializer.Serialize(m).Split("\"trace\"")[0].TrimEnd(','));
return 0;

record PlayerClock(int X,int Y,int Vx,int Vy,string Action,int ActionFrame,int Hitstop,int Hitstun,int Blockstun,int ParryTicks,int ParryRetry,int BackCharge,int DownCharge,int JumpStart,int LandingTicks,int DashTicks,long LastInputTick);
record Clock(long Tick,int TimerTicks,int FullFreeze,PlayerClock[] Players);
record Frame(long InputTick,byte Direction0,Buttons Buttons0,byte Direction1,Buttons Buttons1,Clock Before,Clock After,CombatEvent[] Events,string CanonicalHashAfter);
