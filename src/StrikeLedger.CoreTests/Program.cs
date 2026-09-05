using StrikeLedger.Core;
using System.Diagnostics;
using System.Text.Json;

var data=args.Contains("--data")?Path.GetFullPath(args[Array.IndexOf(args,"--data")+1]):FindData();var content=GameContent.Load(data);var results=new List<object>();var actionCoverage=new List<object>();object? performance=null;int failures=0;
var actionReplays=new SortedDictionary<string,ActionConformanceReplay>(StringComparer.Ordinal);
string? requested=args.Contains("--scenario")?args[Array.IndexOf(args,"--scenario")+1]:null;
uint testSeed=args.Contains("--seed")?uint.Parse(args[Array.IndexOf(args,"--seed")+1]):1;
var aliases=new Dictionary<string,string>{{"atomic_plan_commit","preparation_atomic_idempotent"},{"nine_round_draw","nine_round_match_draw"},{"rollback_startup_debits","rollback_spend_receipts"},{"rollback_ko_to_parry","confirmed_settlement_only"}};
void Check(bool condition,string reason){if(!condition)throw new Exception(reason);}
void Equal<T>(T expected,T actual,string reason)where T:notnull=>Check(EqualityComparer<T>.Default.Equals(expected,actual),$"{reason}: expected {expected}, got {actual}");
void Run(string id,Action test)
{
 if(requested is not null&&(aliases.GetValueOrDefault(requested)??requested)!=id)return;
 if(requested is not null)id=requested;
 var sw=Stopwatch.StartNew();try{test();Console.WriteLine("PASS "+id);results.Add(new{id,pass=true,milliseconds=sw.Elapsed.TotalMilliseconds});}catch(Exception e){failures++;Console.WriteLine("FAIL "+id+": "+e.Message);results.Add(new{id,pass=false,error=e.ToString()});}
}
Simulation New(string fighter="rook",string other="vale",int credits=3600,string super="art_1",bool close=false)
{
 var s=new Simulation(content,new(){Fighter0=fighter,Fighter1=other,Super0=super,Training=true});s.SetTrainingState(0,credits:credits,x:close?350000:null);s.SetTrainingState(1,x:close?385000:null);s.BeginFight();return s;
}
StepResult Step(Simulation s,byte d0=5,Buttons b0=Buttons.None,byte d1=5,Buttons b1=Buttons.None)=>s.Step(new(0,s.Tick,d0,b0),new(1,s.Tick,d1,b1));
void Wait(Simulation s,int frames,byte d1=5){for(int i=0;i<frames;i++){if(s.Phase!=MatchPhase.Fight)break;Step(s,d1:d1);}}
void Qcf(Simulation s,Buttons b){Step(s,2);Step(s,3);Step(s,6,b);}
void Dp(Simulation s,Buttons b){Step(s,6);Step(s,2);Step(s,3,b);}
bool Event(StepResult result,CombatEventKind kind)=>result.Events.Any(e=>e.Kind==kind);
void VerifyActionReplay(ActionConformanceReplay trace)
{
 Check(trace.Config.Training,"action conformance replays must be visibly training-only");Check(trace.Frames.Length is >0 and <=600,"action trace frame bound");Check(trace.InitialSnapshotBase64.Length<200000,"action trace snapshot bound");
 var replay=new Simulation(content,trace.Config);replay.Restore(new(Convert.FromBase64String(trace.InitialSnapshotBase64)));
 Equal(trace.StartStatus,replay.TryStartAction(0,trace.MoveId),"reconstructed startup status "+trace.MoveId);Equal(trace.StartedHash,replay.Hash(),"reconstructed startup state and debit "+trace.MoveId);
 foreach(var frame in trace.Frames)
 {
  Equal(frame.Tick,replay.Tick,"reconstructed input tick "+trace.MoveId);var result=Step(replay,frame.Direction0,frame.Buttons0,frame.Direction1,frame.Buttons1);
  Check(frame.Events.SequenceEqual(result.Events),"reconstructed contact events "+trace.MoveId+" tick "+frame.Tick);if(frame.Hash.Length>0)Equal(frame.Hash,replay.Hash(),"reconstructed periodic hash "+trace.MoveId);
 }
 Equal(trace.FinalHash,replay.Hash(),"reconstructed complete action trace "+trace.MoveId);
}
if(args.Contains("--verify-action-replay"))
{
 string path=Path.GetFullPath(args[Array.IndexOf(args,"--verify-action-replay")+1]);if(new FileInfo(path).Length>64*1024*1024)throw new InvalidDataException("Action replay exceeds64MiB");
 var artifact=JsonSerializer.Deserialize<ActionConformanceFile>(File.ReadAllText(path))??throw new InvalidDataException("Empty action replay");
 Check(artifact.Version==1&&artifact.Format=="strike-ledger-training-action-conformance"&&artifact.TrainingOnly,"unknown or competitive action replay format");Equal(content.ContentHash,artifact.ContentHash,"action trace content identity");Equal(typeof(Simulation).Assembly.ManifestModule.ModuleVersionId.ToString(),artifact.CoreAssemblyId,"action trace core build identity");Check(artifact.Replays.Length is >0 and <=98,"bounded action trace roster");
 foreach(var trace in artifact.Replays)VerifyActionReplay(trace);Console.WriteLine($"PASS reconstructed {artifact.Replays.Length} training action replays from snapshots and recorded inputs/events");
 if(args.Contains("--evidence-dir")){var dir=args[Array.IndexOf(args,"--evidence-dir")+1];Directory.CreateDirectory(dir);File.WriteAllText(Path.Combine(dir,"action-replay-verification.json"),JsonSerializer.Serialize(new{passed=true,replayedActions=artifact.Replays.Length,coreAssemblyId=artifact.CoreAssemblyId,contentHash=artifact.ContentHash,sourceArtifact=path},new JsonSerializerOptions{WriteIndented=true}));}
 return 0;
}

void ExerciseMoves(IEnumerable<FighterDefinition> fighters,bool leasesOnly=false)
{
 foreach(var f in fighters)foreach(var move in f.Moves.Where(m=>!leasesOnly||m.Availability!="base"))
 {
  var art=f.SuperArts.FirstOrDefault(a=>a.MoveId==move.Id)?.Id??"art_1";var s=new Simulation(content,new(){Fighter0=f.Id,Fighter1=f.Id,Super0=art,Training=true});s.SetTrainingState(0,350000,move.Command.StartsWith("J+")?40000:0,credits:3600);s.SetTrainingState(1,382000);
  s.CommitPreparation(new(move.Availability=="base"?[]:[move.Availability]),new([]));s.BeginFight();
  string initialSnapshot=Convert.ToBase64String(s.Capture().Bytes);var status=s.TryStartAction(0,move.Id);Check(status is ActivationStatus.Free or ActivationStatus.Paid,f.Id+" "+move.Id+" did not start");string startedHash=s.Hash();
  int oldX=s.Players[0].X;bool effect=false;var frames=new List<ActionConformanceFrame>();
  for(int i=0;i<250&&s.Phase==MatchPhase.Fight;i++){long tick=s.Tick;var r=Step(s);effect|=r.Events.Any(e=>e.Seat==0&&e.Kind is CombatEventKind.Hit or CombatEventKind.Throw or CombatEventKind.ProjectileSpawn);frames.Add(new(tick,5,Buttons.None,5,Buttons.None,r.Events.ToArray(),i%60==0?r.Hash:""));}
  if(move.Kind=="gambit")Check(s.Players[0].X!=oldX,"feint must move "+move.Id);else Check(effect,"no authored combat effect "+f.Id+" "+move.Id);
  actionCoverage.Add(new{fighter=f.Id,move=move.Id,kind=move.Kind,command=move.Command,started=status.ToString(),damage=1000-s.Players[1].Health,displacement=s.Players[0].X-oldX,effectObserved=effect,finalCredits=s.Players[0].Credits,receipts=s.Players[0].SpendReceipts.Count});
  var trace=new ActionConformanceReplay(s.Config,move.Id,initialSnapshot,status,startedHash,frames.ToArray(),s.Hash());VerifyActionReplay(trace);actionReplays[f.Id+":"+move.Id]=trace;
 }
}
Run("content_all_98_actions",()=>{Equal(98,content.Fighters.Values.Sum(f=>f.Moves.Length),"complete roster count");Equal(12,content.Items.Count,"lease count");ExerciseMoves(content.Fighters.Values);});
Run("all_rook_moves",()=>ExerciseMoves([content.Fighters["rook"]]));
Run("all_vale_moves",()=>ExerciseMoves([content.Fighters["vale"]]));
Run("all_lease_items",()=>ExerciseMoves(content.Fighters.Values,true));
Run("all_98_commands_recognized",()=>
{
 foreach(var f in content.Fighters.Values)foreach(var move in f.Moves)
 {
  var art=f.SuperArts.FirstOrDefault(a=>a.MoveId==move.Id)?.Id??"art_1";var s=new Simulation(content,new(){Fighter0=f.Id,Fighter1=f.Id,Super0=art,Training=true});s.SetTrainingState(0,350000,move.Command.StartsWith("J+")?120000:0,credits:3600);s.SetTrainingState(1,move.Command.StartsWith("CLOSE+")?382000:440000);
  s.CommitPreparation(new(move.Availability=="base"?[]:[move.Availability]),new([]));s.BeginFight();
  var parts=move.Command.Split('+');Buttons buttons=Buttons.None;foreach(var token in parts.Skip(1))buttons|=token switch{"P"=>Buttons.LP,"K"=>Buttons.LK,"PP"=>Buttons.LP|Buttons.MP,"KK"=>Buttons.LK|Buttons.MK,_=>Enum.TryParse<Buttons>(token,out var b)?b:Buttons.None};
  byte final=5;
  if(parts[0] is "qcf" or "qcb" or "dp" or "double_qcf")
  {
   byte[] directions=parts[0] switch{"qcf"=>[2,3,6],"qcb"=>[2,1,4],"dp"=>[6,2,3],_=>[2,3,6,2,3,6]};foreach(var d in directions[..^1])Step(s,d);final=directions[^1];
  }
  else if(parts[0]=="charge_back"){for(int i=0;i<45;i++)Step(s,4);final=6;}
  else if(parts[0]=="charge_down"){for(int i=0;i<45;i++)Step(s,2);final=8;}
  else if(parts[0]=="C")final=2;
  else if(byte.TryParse(parts[0],out var dir))final=dir;
  else if(Enum.TryParse<Buttons>(parts[0],out var first))buttons|=first;
  Step(s,final,buttons);Equal(move.Id,s.Players[0].ActionId,"recognizer "+f.Id+" "+move.Command+" "+move.Id);
 }
});
Run("input_motion_both_sides",()=>
{
 var a=New();Qcf(a,Buttons.LP);Equal("pulse_l",a.Players[0].ActionId,"qcf left");
 var b=New();b.SetTrainingState(0,500000);b.SetTrainingState(1,300000);Step(b);Step(b,2);Step(b,1);Step(b,4,Buttons.MP);Equal("pulse_m",b.Players[0].ActionId,"mirrored qcf");
 var dp=New();Dp(dp,Buttons.HP);Equal("rise_h",dp.Players[0].ActionId,"DP command");
 var bad=New();Step(bad,2);Step(bad,6,Buttons.LP);Check(bad.Players[0].ActionId!="pulse_l","missing diagonal accepted");
 var stale=New();Step(stale,2);Wait(stale,10);Step(stale,3);Step(stale,6,Buttons.LP);Check(stale.Players[0].ActionId!="pulse_l","expired step accepted");
});
Run("charge_crossup",()=>
{
 var s=New("vale");for(int i=0;i<45;i++)Step(s,4);Step(s,6,Buttons.LP);Equal("pulse_l",s.Players[0].ActionId,"45 frame back charge");
 var shortCharge=New("vale");for(int i=0;i<44;i++)Step(shortCharge,4);Step(shortCharge,6,Buttons.LP);Check(shortCharge.Players[0].ActionId!="pulse_l","44 charge must fail");
 var cross=New("vale");for(int i=0;i<45;i++)Step(cross,1);cross.SetTrainingState(0,550000);cross.SetTrainingState(1,350000);Step(cross,8,Buttons.LK);Equal("rise_l",cross.Players[0].ActionId,"vertical charge survives cross-up");
 var horizontal=New("vale");for(int i=0;i<45;i++)Step(horizontal,4);horizontal.SetTrainingState(0,550000);horizontal.SetTrainingState(1,350000);Step(horizontal,4,Buttons.LP);Check(horizontal.Players[0].ActionId!="pulse_l","horizontal charge clears cross-up");
});
Run("exact_credit_ex",()=>{var s=New(credits:300);Qcf(s,Buttons.LP|Buttons.MP);Equal("pulse_ex",s.Players[0].ActionId,"EX startup");Equal(0,s.Players[0].Credits,"exact credits");Equal(1,s.Players[0].SpendReceipts.Count,"single receipt");Wait(s,80);Equal(0,s.Players[0].Credits,"no refund on whiff");});
Run("insufficient_credit_no_fallback",()=>{var s=New(credits:299);Step(s,2);Step(s,3);var r=Step(s,6,Buttons.LP|Buttons.MP);Equal("",s.Players[0].ActionId,"no fallback");Equal(299,s.Players[0].Credits,"no debit");Check(Event(r,CombatEventKind.Rejected),"rejection event");Step(s,6,Buttons.LP);Equal("",s.Players[0].ActionId,"partial release cannot fallback");for(int i=0;i<30;i++){Step(s);Equal("",s.Players[0].ActionId,"release and delayed action forbidden "+i);}});
Run("reserve_two_ex_then_deny",()=>
{
 var s=new Simulation(content,new(){Training=true});s.SetTrainingState(0,credits:1500);s.CommitPreparation(new([],900),new([]));s.BeginFight();Equal(ActivationStatus.Paid,s.TryStartAction(0,"rise_ex"),"first ex");Wait(s,120);Equal(ActivationStatus.Paid,s.TryStartAction(0,"rise_ex"),"second ex");Wait(s,120);Equal(ActivationStatus.Reserve,s.TryStartAction(0,"rise_ex"),"floor rejects third");Equal(900,s.Players[0].Credits,"protected balance");Equal(2,s.Players[0].SpendReceipts.Count,"two receipts");
});
Run("negative_edge_paid_forbidden",()=>
{
 var paid=New();Step(paid,5,Buttons.LP|Buttons.MP);Wait(paid,40);Step(paid,2,Buttons.LP|Buttons.MP);Step(paid,3,Buttons.LP|Buttons.MP);Step(paid,6);Check(paid.Players[0].ActionId!="pulse_ex","release cannot cause EX");Equal(0,paid.Players[0].SpendReceipts.Count,"release cannot spend");
 var ordinary=New();Step(ordinary,5,Buttons.LP);for(int i=0;i<20;i++)Step(ordinary,5,Buttons.LP);Step(ordinary,2,Buttons.LP);Step(ordinary,3,Buttons.LP);Step(ordinary,6);Equal("pulse_l",ordinary.Players[0].ActionId,"ordinary negative edge");
});
Run("free_kit_round",()=>
{
 var s=New(credits:0,close:true);Step(s,5,Buttons.LP);Wait(s,40);Check(s.Players[1].Health<1000,"free normal hits");Equal(0,s.Players[0].Credits,"no earned gauge");Dp(s,Buttons.LP);Equal("rise_l",s.Players[0].ActionId,"ordinary specials free");Equal(0,s.Players[0].Credits,"free special unchanged credits");
 for(int i=0;i<6000&&s.Phase==MatchPhase.Fight;i++){var p=s.Players[0];int distance=Math.Abs(p.X-s.Players[1].X);Step(s,distance>33000?(byte)(p.Facing==1?6:4):(byte)5,p.Actionable&&s.Tick%2==0?Buttons.HP:Buttons.None);}
 Equal(MatchPhase.PendingResult,s.Phase,"complete zero-credit free-kit round reaches KO");Equal(0,s.PendingResult!.WinnerSeat,"free fighter wins round");Equal(0,s.Players[0].Credits,"whole round earns no combat credits");Equal(0,s.Players[0].SpendReceipts.Count,"whole zero-credit round has no paid activation");
});
Run("high_low_parry",()=>
{
 var s=New(close:true);Step(s,5,Buttons.LP);Wait(s,2);var r=Step(s,d1:4);Check(Event(r,CombatEventKind.Parry),"high parry catches LP");Equal(1000,s.Players[1].Health,"parry zero damage");Wait(s,20);Equal(1000,s.Players[1].Health,"parry consumes hit group");
 var low=New(close:true);Step(low,2,Buttons.LK);Wait(low,2);var lr=Step(low,d1:2);Check(Event(lr,CombatEventKind.Parry),"low parry catches crouching kick");
 var wrong=New(close:true);Step(wrong,2,Buttons.LK);Wait(wrong,2);var wr=Step(wrong,d1:4);Check(Event(wr,CombatEventKind.Hit),"high parry loses to low");
});
Run("red_parry_boundary",()=>
{
 foreach(int armFrame in new[]{7,8,9})
 {
  var s=New(close:true);s.SetTrainingState(0,720000);s.SetTrainingState(1,752000);s.TryStartAction(0,"super_1");bool firstBlocked=false;
  for(int i=0;i<100&&!firstBlocked;i++)firstBlocked=Event(Step(s,d1:6),CombatEventKind.Block);
  Check(firstBlocked,"first super hit must be blocked");bool red=false;bool armed=false;
  for(int i=0;i<100&&s.Players[0].ActionFrame<=10;i++)
  {
   bool arm=!armed&&s.Players[0].Hitstop==0&&s.Players[0].ActionFrame==armFrame;var r=Step(s,d1:arm?(byte)4:(byte)5);armed|=arm;red|=r.Events.Any(e=>e.Kind==CombatEventKind.Parry&&e.Detail.StartsWith("Red",StringComparison.Ordinal));
   if(arm)Check(s.Players[1].Blockstun>0||red,"red arm alone must not erase blockstun");
  }
  Equal(armFrame>=8,red,"two eligible tick red window at action-frame "+armFrame);
 }
});
Run("multihit_super_parry",()=>
{
 var s=New(close:true);s.TryStartAction(0,"super_1");int parries=0;var starts=new HashSet<int>{5,9,13,17,21};var armedFrames=new HashSet<int>();
 for(int i=0;i<240;i++)
 {
  var p=s.Players[0];bool arm=p.ActionId=="super_1"&&p.Hitstop==0&&s.FullFreeze==0&&starts.Contains(p.ActionFrame)&&armedFrames.Add(p.ActionFrame);
  var r=Step(s,d1:arm?(byte)4:(byte)5);parries+=r.Events.Count(e=>e.Kind==CombatEventKind.Parry);
 }
 Equal(5,parries,"all five distinct super groups parried with fresh edges");Equal(1000,s.Players[1].Health,"full parry zero damage");Equal(2700,s.Players[0].Credits,"entire super cost remains paid");Equal(1,s.Players[0].SpendReceipts.Count,"one super receipt");
 var held=New(close:true);held.TryStartAction(0,"super_1");bool start=false;int heldParries=0;
 for(int i=0;i<200;i++){if(held.Players[0].ActionFrame==5&&held.FullFreeze==0)start=true;heldParries+=Step(held,d1:start?(byte)4:(byte)5).Events.Count(e=>e.Kind==CombatEventKind.Parry);}
 Equal(1,heldParries,"holding forward must not auto-parry a multihit");Check(held.Players[1].Health<1000,"subsequent super hits damage held-forward defender");
});
Run("ex_cancel_super_costs",()=>
{
 var s=New(close:true);Equal(ActivationStatus.Paid,s.TryStartAction(0,"knee_ex"),"EX start");
 for(int i=0;i<80&&!s.Players[0].Contact;i++)Step(s);Check(s.Players[0].Contact,"EX must contact");while(s.Players[0].Hitstop>0)Step(s);
 Equal(ActivationStatus.Paid,s.TryStartAction(0,"super_1"),"legal EX-super cancel at "+s.Players[0].ActionId+" frame "+s.Players[0].ActionFrame+" Y "+s.Players[0].Y);Equal(2400,s.Players[0].Credits,"both startup prices charged");Equal(2,s.Players[0].SpendReceipts.Count,"two canonical receipts");
});
Run("same_tick_supers_and_freeze",()=>
{
 var s=New("rook","rook");s.SetTrainingState(1,credits:3600);byte[] motion=[2,3,6,2,3,6];StepResult? r=null;
 for(int i=0;i<motion.Length;i++)r=Step(s,motion[i],i==5?Buttons.LP:Buttons.None,CoreMath.RelativeDirection(motion[i],-1),i==5?Buttons.LP:Buttons.None);
 Equal("super_1",s.Players[0].ActionId,"left super");Equal("super_1",s.Players[1].ActionId,"right simultaneous super");Equal(2700,s.Players[0].Credits,"left debit");Equal(2700,s.Players[1].Credits,"right debit");Equal(19,s.FullFreeze,"one max shared freeze");Check(r!.Events.Count(e=>e.Kind==CombatEventKind.Spend)==2,"both startup receipts same tick");int timer=s.TimerTicks;Wait(s,19);Equal(timer,s.TimerTicks,"full freeze pauses round timer");Equal(0,s.Players[0].ActionFrame,"action frames pause through freeze");
});
Run("quickrise_and_jump_throw_immunity",()=>
{
 var s=New(close:true);s.TryStartAction(0,"rise_l");bool knocked=false;
 for(int i=0;i<120;i++){Step(s);knocked|=s.Players[1].KnockdownTicks>0;if(knocked&&s.Players[1].Grounded&&s.Players[1].Hitstop==0)break;}
 Check(knocked&&s.Players[1].KnockdownTicks>24,"soft knockdown impact");var quick=Step(s,d1:2);Check(quick.Events.Any(e=>e.Kind==CombatEventKind.Land&&e.Detail=="quick-rise"),"quickrise edge event");Check(s.Players[1].KnockdownTicks<=24,"quickrise reduces recovery");
 var hard=New(close:true);hard.TryStartAction(0,"throw_forward");Wait(hard,15);var noQuick=Step(hard,d1:2);Check(!noQuick.Events.Any(e=>e.Detail=="quick-rise"),"hard knockdown cannot quickrise");
 var jump=New(close:true);jump.TryStartAction(0,"throw_forward");Wait(jump,3);Step(jump,d1:8);Wait(jump,12);Equal(1000,jump.Players[1].Health,"jump startup throw invulnerable");
});
Run("air_parry_and_projectile_source_freeze",()=>
{
 var air=New(close:true);air.SetTrainingState(0,y:40000);air.SetTrainingState(1,y:40000);Step(air,b0:Buttons.LP);Wait(air,2);var parry=Step(air,d1:4);Check(parry.Events.Any(e=>e.Kind==CombatEventKind.Parry&&e.Detail.StartsWith("Air:",StringComparison.Ordinal)),"air parry catches actual air strike");Equal(1000,air.Players[1].Health,"air parry zero damage");
 var s=New();s.SetTrainingState(0,350000);s.SetTrainingState(1,450000);s.TryStartAction(0,"pulse_ex");bool found=false;
 for(int i=0;i<100;i++)
 {
  var projectile=s.Projectiles.FirstOrDefault();bool arm=projectile is not null&&projectile.X+projectile.Vx+projectile.Definition.Width/2>=s.Players[1].X-18000;
  var r=Step(s,d1:arm?(byte)4:(byte)5);if(Event(r,CombatEventKind.Parry)){found=true;Equal(0,s.Players[0].Hitstop,"remote projectile parry must not freeze owner");Check(s.Projectiles.Single().Hitstop>0,"surviving EX projectile receives source freeze");Equal(1000,s.Players[1].Health,"projectile parry zero chip");break;}
 }
 Check(found,"projectile parry executes");
});
Run("target_combo_reversal_and_dizzy",()=>
{
 var target=New(close:true);target.TryStartAction(0,"s_lp");while(!target.Players[0].Contact)Step(target);while(target.Players[0].Hitstop>0)Step(target);Step(target,b0:Buttons.MP);Equal("s_mp",target.Players[0].ActionId,"target combo uses named move instead of close dispatch");
 var s=New(close:true);s.TryStartAction(1,"s_lp");for(int i=0;i<80&&s.Players[0].Hitstun==0;i++)Step(s);Check(s.Players[0].Hitstun>0,"reversal defender hitstun");
 while(s.Players[0].Hitstop>0||s.Players[0].Hitstun>2)Step(s);Step(s,b0:Buttons.LP);Check(s.Players[0].ActionId.Length==0,"reversal input must not escape hitstun");bool reversal=false;for(int i=0;i<4;i++)reversal|=Step(s).Events.Any(e=>e.Kind==CombatEventKind.ActionStarted&&e.Seat==0&&e.Detail=="reversal");Check(reversal,"final-two-tick reversal buffer executes exactly at recovery");
 var dizzy=New(close:true);dizzy.SetTrainingState(1,stun:980);Step(dizzy,b0:Buttons.LP);Wait(dizzy,4);Check(dizzy.Players[1].DizzyTicks>0,"authored stun reaches dizzy");Equal(0,dizzy.Players[1].Stun,"dizzy resets buildup");Equal(600,dizzy.Players[1].Credits,"dizzy cannot generate currency");
});
Run("economy_hand_derived_match_sequences",()=>
{
 var s=New(credits:600);s.TryStartAction(0,"rise_ex");Wait(s,120);s.TryStartAction(0,"rise_ex");Wait(s,120);Equal(0,s.Players[0].Credits,"opening two EX spend 600");
 void Lose(Simulation sim){sim.SetTrainingState(0,x:350000,health:1);sim.SetTrainingState(1,x:382000);sim.TryStartAction(1,"s_lp");Wait(sim,8);Equal(1,sim.PendingResult!.WinnerSeat,"opponent wins actual LP contact");sim.SettleRound(sim.PendingResult.TerminalTick);}
 Lose(s);Equal(900,s.Players[0].Credits,"first loss pays900");Equal(1,s.Players[0].RecoveryTier,"loss tier1");s.NextRound();s.BeginFight();Lose(s);Equal(2100,s.Players[0].Credits,"saved900 plus pre-result-tier1 payout1200");Equal(2,s.Players[0].RecoveryTier,"second loss tier2");
 var rematch=new Simulation(content);Equal(600,rematch.Players[0].Credits,"new match wallet reset");Equal(0,rematch.CompletedRounds,"new match scores reset");
});
Run("blocking_chip_and_no_air_block",()=>
{
 var s=New(close:true);s.SetTrainingState(0,720000);s.SetTrainingState(1,752000);Step(s,5,Buttons.LP,d1:6);Wait(s,5,d1:6);Equal(1000,s.Players[1].Health,"normal zero chip");Check(s.Players[1].Blockstun>0,"stand guard blockstun");
 var low=New(close:true);low.SetTrainingState(0,720000);low.SetTrainingState(1,752000);Step(low,2,Buttons.LK,d1:6);Wait(low,5,d1:6);Check(low.Players[1].Health<1000,"low beats standing guard");
 var crouch=New(close:true);Step(crouch,2,Buttons.LK,d1:3);Wait(crouch,5,d1:3);Equal(1000,crouch.Players[1].Health,"crouching guard catches low");
 var chip=New(close:true);chip.SetTrainingState(0,720000);chip.SetTrainingState(1,752000);chip.TryStartAction(0,"sway_l");Wait(chip,25,d1:6);Check(chip.Players[1].Health<1000&&chip.Players[1].Health>950,"special chip damage");
});
Run("simultaneous_trade_and_rank",()=>
{
 var s=New("rook","rook",close:true);Step(s,b0:Buttons.LP,b1:Buttons.LP);Wait(s,4);Equal(s.Players[0].Health,s.Players[1].Health,"equal rank symmetric trade");Check(s.Players[0].Health<1000,"trade deals damage");
 var rank=New("rook","rook",close:true);rank.TryStartAction(0,"s_hp");Wait(rank,5);Step(rank,b1:Buttons.LP);Wait(rank,4);Check(rank.Players[0].Health==1000&&rank.Players[1].Health<1000,"higher rank wins reciprocal contact");
});
Run("paid_startup_interrupted",()=>
{
 var s=New("rook","rook",close:true);s.TryStartAction(1,"s_hp");Wait(s,7);Equal(ActivationStatus.Paid,s.TryStartAction(0,"pulse_ex"),"legal paid startup");Wait(s,3);Equal("",s.Players[0].ActionId,"paid startup interrupted");Equal(3300,s.Players[0].Credits,"interruption no refund");
});
Run("cancel_contact_not_whiff",()=>
{
 var s=New(close:true);s.TryStartAction(0,"s_lp");Wait(s,12);Check(s.Players[0].Contact,"LP contact");while(s.Players[0].Hitstop>0)Step(s);Equal(ActivationStatus.Paid,s.TryStartAction(0,"rise_ex"),"contact special cancel");
 var whiff=New();whiff.TryStartAction(0,"s_lp");Wait(whiff,4);Equal(ActivationStatus.Illegal,whiff.TryStartAction(0,"rise_ex"),"whiff cannot cancel");Equal(3600,whiff.Players[0].Credits,"illegal cancel no charge");
});
Run("throw_kara_quickrise",()=>
{
 var s=New(close:true);Step(s,b0:Buttons.LP|Buttons.LK);Wait(s,4);Check(s.Players[1].Health==1000,"throw damage waits tech window");var tech=Step(s,b1:Buttons.LP|Buttons.LK);Check(Event(tech,CombatEventKind.ThrowTech),"normal throw tech");Equal(1000,s.Players[1].Health,"tech zero damage");
 var kara=New(close:true);kara.TryStartAction(0,"command_fhp");Step(kara);int x=kara.Players[0].X;Equal(ActivationStatus.Free,kara.TryStartAction(0,"throw_forward"),"tagged kara throw");Equal(x,kara.Players[0].X,"kara retains frame0 movement");Wait(kara,15);Check(kara.Players[1].Health<1000,"kara throw connects");
 var invalid=New(close:true);invalid.TryStartAction(0,"s_mp");Step(invalid);Equal(ActivationStatus.Illegal,invalid.TryStartAction(0,"throw_forward"),"untagged kara rejected");
});
Run("projectile_cap_and_clash",()=>
{
 var s=New("rook","rook");s.TryStartAction(0,"pulse_l");s.TryStartAction(1,"pulse_l");bool clash=false;for(int i=0;i<100;i++)clash|=Event(Step(s),CombatEventKind.ProjectileClash);Check(clash,"projectile clash must execute");Equal(0,s.Projectiles.Count,"equal rank cancels both");Equal(1000,s.Players[0].Health,"clash protects left");Equal(1000,s.Players[1].Health,"clash protects right");
});
Run("preparation_atomic_idempotent",()=>
{
 var s=new Simulation(content,new(){Training=true});string before=s.Hash();try{s.CommitPreparation(new(["rook_step_feint"]),new(["vale_return_pulse"]));throw new Exception("invalid commit accepted");}catch(ArgumentException){}Equal(before,s.Hash(),"invalid other plan rolls back both");
 var receipt=s.CommitPreparation(new(["rook_step_feint"]),new([]),"test");Equal(receipt,s.CommitPreparation(new(["rook_step_feint"]),new([]),"test"),"duplicate receipt");Equal(300,s.Players[0].Credits,"one preparation debit");
 try{s.CommitPreparation(new([]),new([]),"test");throw new Exception("changed payload accepted");}catch(InvalidOperationException){}
});
Run("leased_replacement_and_selected_super",()=>
{
 var s=new Simulation(content,new(){Training=true});s.SetTrainingState(0,credits:3600);s.CommitPreparation(new(["rook_high_hook"]),new([]));s.BeginFight();Equal(ActivationStatus.Illegal,s.TryStartAction(0,"command_fhp"),"base replaced");Step(s,6,Buttons.HP);Equal("shop_high_hook",s.Players[0].ActionId,"leased command priority");
 var selected=New();Equal(ActivationStatus.Illegal,selected.TryStartAction(0,"super_2"),"unselected super locked");Equal(ActivationStatus.Paid,selected.TryStartAction(0,"super_1"),"selected super allowed");Equal(2700,selected.Players[0].Credits,"selected super price");
});
Run("rollback_spend_receipts",()=>
{
 var s=New();Step(s,2);Step(s,3);var snap=s.Capture();Step(s,6,Buttons.LP|Buttons.MP);string withSpend=s.Hash();Equal(3300,s.Players[0].Credits,"initial EX");s.Restore(snap);Step(s,6,Buttons.LP|Buttons.MP);Equal(withSpend,s.Hash(),"same replay restores identical full state");Equal(1,s.Players[0].SpendReceipts.Count,"one replay receipt");s.Restore(snap);Step(s,6);Equal(3600,s.Players[0].Credits,"corrected input erases speculative spend");Equal(0,s.Players[0].SpendReceipts.Count,"corrected receipt removed");
});
Run("confirmed_settlement_only",()=>
{
 var s=New(close:true);s.SetTrainingState(1,health:1);var snapshot=s.Capture();Step(s,b0:Buttons.LP);Wait(s,4);Equal(MatchPhase.PendingResult,s.Phase,"KO pending");Equal(0,s.Players[0].ScoreHalfPoints,"no speculative score");Equal(3600,s.Players[0].Credits,"no speculative payout");
 try{s.SettleRound(s.PendingResult!.TerminalTick-1);throw new Exception("unconfirmed settlement accepted");}catch(InvalidOperationException){}
 var receipt=s.SettleRound(s.PendingResult!.TerminalTick);Equal(2,s.Players[0].ScoreHalfPoints,"confirmed score");Equal(1200,receipt.Payout0.Clipped,"cap clipping recorded");Equal(receipt,s.SettleRound(s.PendingResult.TerminalTick),"settlement idempotence");
 s.Restore(snapshot);Step(s,b0:Buttons.LP);Wait(s,2);Step(s,d1:4);Equal(MatchPhase.Fight,s.Phase,"late parry removes speculative KO");Equal(0,s.Players[0].ScoreHalfPoints,"rollback removes payout and score");
});
Run("nine_round_match_draw",()=>
{
 var s=New();for(int round=1;round<=9;round++){s.SetTrainingState(0,health:0);s.SetTrainingState(1,health:0);Step(s);Equal(-1,s.PendingResult!.WinnerSeat,"double KO draw");s.SettleRound(s.PendingResult.TerminalTick);if(round<9){s.NextRound();s.BeginFight();}}
 Equal(MatchPhase.MatchOver,s.Phase,"regulation cap");Equal("DRAW",s.MatchDecision,"honest match draw");Equal(9,s.Players[0].ScoreHalfPoints,"half points");Equal(9,s.Players[1].ScoreHalfPoints,"half points");
});
Run("snapshot_determinism_and_bounds",()=>
{
 var a=New();var b=New();uint seed=testSeed^1234567;var snapshots=new List<(SimulationSnapshot Snapshot,string Hash)>();
 for(int i=0;i<800&&a.Phase==MatchPhase.Fight;i++){seed^=seed<<13;seed^=seed>>17;seed^=seed<<5;byte d=(byte)(1+seed%9);var btn=(Buttons)(1<<(int)((seed>>8)%6));Step(a,d,i%9==0?btn:Buttons.None);Step(b,d,i%9==0?btn:Buttons.None);Equal(a.Hash(),b.Hash(),"independent deterministic trace "+i);if(i%67==0)snapshots.Add((a.Capture(),a.Hash()));}
 foreach(var (snapshot,hash) in snapshots){b.Restore(snapshot);Equal(hash,b.Hash(),"snapshot exact state roundtrip");}
 var bytes=a.SerializeCanonical();bytes[0]^=255;var before=a.Hash();try{a.Restore(new(bytes));throw new Exception("corrupt snapshot accepted");}catch(InvalidDataException){}Equal(before,a.Hash(),"failed restore atomic");
});
Run("movement_jump_dash_and_walls",()=>
{
 var s=New();int x=s.Players[0].X;Step(s,6);Equal(x+3000,s.Players[0].X,"walk fixed distance");Step(s);int stopped=s.Players[0].X;Step(s);Equal(stopped,s.Players[0].X,"walk stops immediately");Step(s,6);Check(s.Players[0].DashTicks>0,"double tap dash");Wait(s,30);Step(s,9);Wait(s,5);Check(s.Players[0].Y>0,"jump launches after startup");int vx=s.Players[0].Vx;Step(s,4);Equal(vx,s.Players[0].Vx,"no air steering");Wait(s,70);Equal(0,s.Players[0].Y,"land on floor");s.SetTrainingState(0,16000);for(int i=0;i<20;i++)Step(s,4);Check(s.Players[0].X>=16000,"solid stage wall");
});
Run("seat_swap_mirror_symmetry",()=>
{
 var a=New("rook","vale");var b=New("vale","rook");a.SetTrainingState(0,350000);a.SetTrainingState(1,390000);b.SetTrainingState(0,378000);b.SetTrainingState(1,418000);b.SetTrainingState(1,credits:3600);a.SetTrainingState(1,credits:3600);
 uint seed=testSeed^912781;
 for(int i=0;i<1200&&a.Phase==MatchPhase.Fight;i++)
 {
  seed^=seed<<13;seed^=seed>>17;seed^=seed<<5;byte d0=(byte)(1+seed%9),d1=(byte)(1+(seed>>10)%9);var b0=i%13==0?(Buttons)(1<<(int)((seed>>19)%6)):Buttons.None;var b1=i%17==0?(Buttons)(1<<(int)((seed>>24)%6)):Buttons.None;
  Step(a,d0,b0,d1,b1);Step(b,CoreMath.RelativeDirection(d1,-1),b1,CoreMath.RelativeDirection(d0,-1),b0);
  for(int seat=0;seat<2;seat++){var p=a.Players[seat];var q=b.Players[1-seat];Equal(p.Health,q.Health,"mirrored health "+i);Equal(p.Credits,q.Credits,"mirrored credits "+i);Equal(p.X,768000-q.X,"mirrored x "+i);Equal(p.Y,q.Y,"mirrored y "+i);Equal(p.ActionId,q.ActionId,"mirrored action "+i);Equal(p.ActionFrame,q.ActionFrame,"mirrored action clock "+i);}
 }
 Equal(a.Phase,b.Phase,"mirrored terminal phase");
});
Run("performance_and_rollback_budget",()=>
{
 var s=New("rook","rook");s.SetTrainingState(0,200000);s.SetTrainingState(1,568000);s.SetTrainingState(1,credits:3600);
 (byte Direction,Buttons Held) Input(long frame)=>(frame%70) switch{0=>(2,Buttons.None),1=>(3,Buttons.None),2=>(6,Buttons.LP),_=>(5,Buttons.None)};
 StepResult Advance(Simulation sim){var input=Input(sim.Tick);return Step(sim,input.Direction,input.Held,CoreMath.RelativeDirection(input.Direction,-1),input.Held);}
 for(int i=0;i<500;i++)Advance(s);var samples=new double[2500];long allocatedBefore=GC.GetAllocatedBytesForCurrentThread();
 for(int i=0;i<samples.Length;i++){long start=Stopwatch.GetTimestamp();Advance(s);samples[i]=Stopwatch.GetElapsedTime(start).TotalMilliseconds;}
 long allocated=GC.GetAllocatedBytesForCurrentThread()-allocatedBefore;Check(s.Phase==MatchPhase.Fight&&s.Tick==3000,"profile actually advanced 3000 fight ticks");
 double Percentile(double[] values,double p){var sorted=values.Order().ToArray();return sorted[(int)Math.Ceiling((sorted.Length-1)*p)];}
 var windows=new List<object>();foreach(int window in new[]{8,240})
 {
  var snap=s.Capture();for(int i=0;i<window;i++)Advance(s);var expected=s.Hash();var times=new double[20];
  for(int repeat=0;repeat<times.Length;repeat++){s.Restore(snap);long start=Stopwatch.GetTimestamp();for(int i=0;i<window;i++)Advance(s);times[repeat]=Stopwatch.GetElapsedTime(start).TotalMilliseconds;Equal(expected,s.Hash(),"rollback profiling must reproduce full state");}
  windows.Add(new{ticks=window,p50Milliseconds=Percentile(times,.5),p95Milliseconds=Percentile(times,.95),worstMilliseconds=times.Max()});
 }
 performance=new{configuration=BuildConfiguration(),framework=System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,os=System.Runtime.InteropServices.RuntimeInformation.OSDescription,architecture=System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString(),logicalProcessors=Environment.ProcessorCount,warmupTicks=500,sampleTicks=samples.Length,p50Milliseconds=Percentile(samples,.5),p95Milliseconds=Percentile(samples,.95),p99Milliseconds=Percentile(samples,.99),worstMilliseconds=samples.Max(),allocatedBytesPerStep=allocated/samples.Length,snapshotBytes=s.SerializeCanonical().Length,rollback=windows};
 Check(Percentile(samples,.95)<16.667,"core p95 exceeds fixed-frame budget on actual host");
});

if(results.Count==0){Console.Error.WriteLine("Unknown scenario; no test was executed.");return 2;}
var report=new{schema_version=1,core="production",core_assembly_id=typeof(Simulation).Assembly.ManifestModule.ModuleVersionId.ToString(),seed=testSeed,content_hash=content.ContentHash,passed=results.Count-failures,failed=failures,results,actionCoverage,performance};
if(args.Contains("--evidence-dir")){var dir=args[Array.IndexOf(args,"--evidence-dir")+1];Directory.CreateDirectory(dir);File.WriteAllText(Path.Combine(dir,"core-conformance.json"),JsonSerializer.Serialize(report,new JsonSerializerOptions{WriteIndented=true}));if(actionReplays.Count>0){var artifact=new ActionConformanceFile(1,"strike-ledger-training-action-conformance",true,typeof(Simulation).Assembly.ManifestModule.ModuleVersionId.ToString(),content.ContentHash,actionReplays.Values.ToArray());File.WriteAllText(Path.Combine(dir,"action-replays.training.json"),JsonSerializer.Serialize(artifact));}}
Console.WriteLine($"{results.Count-failures}/{results.Count} production core scenarios passed.");return failures==0?0:1;

static string FindData()
{
 foreach(var start in new[]{Environment.CurrentDirectory,AppContext.BaseDirectory})for(var dir=new DirectoryInfo(start);dir is not null;dir=dir.Parent)if(File.Exists(Path.Combine(dir.FullName,"data","rules.json")))return Path.Combine(dir.FullName,"data");
 throw new DirectoryNotFoundException("Provide --data PATH to the Strike Ledger canonical data directory.");
}
static string BuildConfiguration()
{
#if DEBUG
 return "Debug";
#else
 return "Release";
#endif
}
public sealed record ActionConformanceFile(int Version,string Format,bool TrainingOnly,string CoreAssemblyId,string ContentHash,ActionConformanceReplay[] Replays);
public sealed record ActionConformanceReplay(MatchConfig Config,string MoveId,string InitialSnapshotBase64,ActivationStatus StartStatus,string StartedHash,ActionConformanceFrame[] Frames,string FinalHash);
public sealed record ActionConformanceFrame(long Tick,byte Direction0,Buttons Buttons0,byte Direction1,Buttons Buttons1,CombatEvent[] Events,string Hash);
