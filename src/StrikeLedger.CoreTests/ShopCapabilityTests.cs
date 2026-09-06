using StrikeLedger.Core;

/// <summary>Shop-v2 conformance: real legal input, atomic purchases, frozen banks and canonical rollback.</summary>
public static class ShopCapabilityTests
{
 static void Check(bool pass,string reason){if(!pass)throw new Exception(reason);}
 static StepResult Step(Simulation s,byte d0=5,Buttons b0=Buttons.None,byte d1=5,Buttons b1=Buttons.None)=>s.Step(new(0,s.Tick,d0,b0),new(1,s.Tick,d1,b1));
 static void Wait(Simulation s,int n){for(int i=0;i<n&&s.Phase==MatchPhase.Fight;i++)Step(s);}
 static string Product(GameContent c,string fighter,string move)=>c.Items.Values.Single(i=>i.EligibleFighters.Contains(fighter)&&i.MoveId==move).Id;
 static void Reject(Action a,string reason){try{a();}catch(ArgumentException){return;}catch(InvalidOperationException){return;}throw new Exception(reason);}
 static IEnumerable<(byte Direction,Buttons Buttons)> Command(GameContent c,MoveDefinition m,int facing)
 {
  yield return(5,Buttons.None);var parts=m.Command.Split('+');var button=parts[1] switch{"PP"=>Buttons.LP|Buttons.MP,"KK"=>Buttons.LK|Buttons.MK,"P"=>Buttons.LP,"K"=>Buttons.LK,_=>Enum.Parse<Buttons>(parts[1])};
  if(parts[0].StartsWith("charge_")){for(int i=0;i<c.Inputs.ChargeHoldTicks;i++)yield return(CoreMath.RelativeDirection(parts[0]=="charge_back"?(byte)4:(byte)2,facing),Buttons.None);yield return(CoreMath.RelativeDirection(parts[0]=="charge_back"?(byte)6:(byte)8,facing),button);}
  else{var pattern=c.Inputs.MotionPatterns[parts[0]];for(int i=0;i<pattern.Length;i++)yield return(CoreMath.RelativeDirection((byte)pattern[i],facing),i==pattern.Length-1?button:Buttons.None);}
 }
 static List<CombatEvent> InputAction(Simulation s,MoveDefinition move)
 {var events=new List<CombatEvent>();foreach(var input in Command(s.Content,move,s.Players[0].Facing))events.AddRange(Step(s,input.Direction,input.Buttons).Events);return events;}
 static Simulation Practice(GameContent c,string fighter,string move,bool owned,bool mirror=false)
 {
  var s=new Simulation(c,new(){Training=true,Fighter0=fighter,Fighter1=fighter,SessionId="shop-v2-capability"});s.TrainingReset(0);
  s.SetTrainingState(0,x:mirror?752000:16000);s.SetTrainingState(1,x:mirror?16000:752000);Step(s);
  if(owned)s.SetTrainingLoadout(0,new([Product(c,fighter,move)]));return s;
 }
 public static IEnumerable<(string Id,Action Test)> Cases(GameContent c)
 {
  yield return("shop_registry_resource_contract",()=>
  {
   Check(c.Preparation.LoadoutCap==2400&&c.Preparation.MaxProducts==6&&c.Preparation.SlotLimits["ex"]==2&&c.Preparation.SlotLimits["super"]==1,"mixed six-product slot limits");
   Check(c.Economy.LossPayouts.SequenceEqual(new[]{1200,1200,1500}),"new default loss schedule");
   foreach(var f in c.Fighters.Values)foreach(var m in f.Moves){Check(m.CreditCost==0&&!m.DebitOnStart,"no action contains runtime currency cost");if(m.Kind is "super" or "ex_special")Check(c.Items.ContainsKey(m.Availability)&&m.AccessPolicy!="base","complete vocabulary is purchased explicitly");}
   var s=new Simulation(c,new(){Super0="art_3"});Check(s.Players.All(p=>p.SelectedSuper==""&&p.SuperUsesRemaining==0&&p.OwnedEx.Count==0),"legacy config cannot grant art selection or capability");
  });
  yield return("shop_atomic_cart_and_thresholds",()=>
  {
   foreach(var item in c.Items.Values)foreach(int delta in new[]{-1,0,1})
   {var q=c.QuotePreparation(item.EligibleFighters[0],item.Price+delta,new([item.Id]));Check(q.Valid==(delta>=0),"exact shop threshold "+item.Id);if(q.Valid)Check(q.Remaining==delta&&q.TotalCost==item.Price,"exact quote arithmetic");}
   var sim=new Simulation(c);string ex=Product(c,"rook","pulse_ex");var before=sim.Hash();Reject(()=>sim.CommitPreparation(new([ex]),new([ex])),"opponent eligibility atomic reject");Check(sim.Hash()==before,"invalid second plan leaves whole world byte-identical");
   Reject(()=>sim.CommitPreparation(new([ex],1),new([])),"reserve unsupported");Reject(()=>sim.CommitPreparation(new([ex],ContentHash:"stale"),new([])),"stale hash unsupported");Reject(()=>sim.CommitPreparation(new([ex],QuotedCost:599),new([])),"stale quote unsupported");Check(sim.Hash()==before,"all preflight failures atomic");
   var plan=new PreparationPlan([ex],ContentHash:c.ContentHash,QuotedCost:600);var receipt=sim.CommitPreparation(plan,new([]),"atomic");Check(receipt.Cost0==600&&receipt.Credits0==0&&receipt.OpeningCredits0==600&&receipt.ProductIds0.SequenceEqual(new[]{ex}),"typed preparation receipt");Check(ReferenceEquals(receipt,sim.CommitPreparation(plan,new([]),"atomic")),"exact duplicate returns original receipt");Reject(()=>sim.CommitPreparation(new([]),new([]),"atomic"),"conflicting payload rejected");
   var allEx=c.Items.Values.Where(i=>i.Slot=="ex"&&i.EligibleFighters.Contains("rook")).Take(3).Select(i=>i.Id).ToArray();Check(!c.QuotePreparation("rook",3600,new(allEx)).Valid,"third EX rejected");
   var arts=c.Items.Values.Where(i=>i.Slot=="super"&&i.EligibleFighters.Contains("rook")).Take(2).Select(i=>i.Id).ToArray();Check(!c.QuotePreparation("rook",3600,new(arts)).Valid,"second art rejected");
  });
  yield return("shop_all_ex_repeat_without_bank",()=>
  {
   foreach(var fighter in c.Fighters.Values)foreach(var move in fighter.Moves.Where(m=>m.Kind=="ex_special"))foreach(bool mirror in new[]{false,true})
   {
    var s=Practice(c,fighter.Id,move.Id,true,mirror);int starts=0;for(int repeat=0;repeat<6;repeat++){var events=InputAction(s,move);starts+=events.Count(e=>e.Kind==CombatEventKind.ActionStarted&&e.MoveId==move.Id);Check(s.Players[0].Credits==0&&s.Players[0].SpendReceipts.Count==0,"zero-bank EX has no fee or ammo");Wait(s,240);}
    Check(starts==6&&s.Players[0].OwnedEx.Contains(move.Id),"all six legal repeats of "+fighter.Id+" "+move.Id);
   }
  });
  yield return("shop_locked_vocabulary_and_release",()=>
  {
   foreach(var fighter in c.Fighters.Values)foreach(var move in fighter.Moves.Where(m=>m.Kind is "ex_special" or "super"))foreach(bool mirror in new[]{false,true})
   {
    var s=Practice(c,fighter.Id,move.Id,false,mirror);s.SetTrainingState(0,credits:3600);var events=InputAction(s,move);
    Check(events.Any(e=>e.Kind==CombatEventKind.Rejected&&e.Detail=="locked")&&!events.Any(e=>e.Kind==CombatEventKind.ActionStarted),"rich unowned command locks without fallback "+move.Id);
    for(int t=0;t<30;t++){var r=Step(s,CoreMath.RelativeDirection(6,s.Players[0].Facing),t==0?(move.Command.EndsWith("KK")?Buttons.LK:Buttons.LP):Buttons.None);Check(!r.Events.Any(e=>e.Kind==CombatEventKind.ActionStarted),"partial/full release cannot become delayed normal");}
    Check(s.Players[0].Credits==3600&&s.Players[0].SuperUseReceipts.Count==0,"rejected intent consumes nothing");
   }
  });
  yield return("shop_all_super_once_rollback",()=>
  {
   foreach(var fighter in c.Fighters.Values)foreach(var art in fighter.SuperArts)foreach(bool mirror in new[]{false,true})
   {
    var move=fighter.Move(art.MoveId);var s=Practice(c,fighter.Id,move.Id,true,mirror);var snapshot=s.Capture();var inputs=Command(c,move,s.Players[0].Facing).ToArray();var frames=new List<StepResult>();
    foreach(var input in inputs)frames.Add(Step(s,input.Direction,input.Buttons));Check(s.Players[0].SuperUsesRemaining==0&&s.Players[0].SuperUseReceipts.Count==1&&s.Players[0].Credits==0,"prepaid startup consumes exactly one use without bank");string hash=s.Hash();
    s.Restore(snapshot);for(int i=0;i<inputs.Length;i++){var r=Step(s,inputs[i].Direction,inputs[i].Buttons);Check(r.Hash==frames[i].Hash&&r.Events.SequenceEqual(frames[i].Events),"super rollback reproduces events/receipt/root");}Check(hash==s.Hash(),"identical first use hash");
    Wait(s,400);var denied=InputAction(s,move);Check(denied.Any(e=>e.Kind==CombatEventKind.Rejected&&e.Detail=="exhausted")&&!denied.Any(e=>e.Kind==CombatEventKind.ActionStarted)&&s.Players[0].SuperUseReceipts.Count==1,"expired install/field/ordinary art cannot reacquire use");
    s.Restore(snapshot);foreach(var input in inputs)Step(s,input.Direction);Check(s.Players[0].SuperUsesRemaining==1&&s.Players[0].SuperUseReceipts.Count==0,"corrected input erases speculative consumption");
   }
  });
  yield return("shop_competitive_bank_and_expiry",()=>
  {
   var s=new Simulation(c);s.CommitPreparation(new([Product(c,"rook","pulse_ex")]),new([]));s.BeginFight();int starts=0;
   for(int i=0;i<2;i++){starts+=InputAction(s,c.Fighters["rook"].Move("pulse_ex")).Count(e=>e.Kind==CombatEventKind.ActionStarted&&e.MoveId=="pulse_ex");Wait(s,180);Check(s.Players[0].Credits==0,"competitive bank remains frozen during legal repeats");}
   Check(starts==2,"competitive licensed repeats actually started");while(s.Phase==MatchPhase.Fight)Step(s);var pending=s.Capture();int bank=s.Players[0].Credits;Reject(()=>s.SettleRound(s.PendingResult!.TerminalTick-1),"unconfirmed settlement rejected");Check(s.Players[0].Credits==bank,"pending result does not deposit");
   var receipt=s.SettleRound(s.PendingResult!.TerminalTick);Check(s.Players[0].Credits==bank+receipt.Payout0.Granted+receipt.SkillPayout0.Granted,"settlement result then skill grant");Check(s.Players[0].OwnedEx.Count==1,"result preserves visible previous kit");s.Restore(pending);s.SettleRound(s.PendingResult!.TerminalTick);s.NextRound();Check(s.Players.All(p=>p.OwnedProductIds.Count==0&&p.OwnedEx.Count==0&&p.SuperUsesRemaining==0&&p.SelectedSuper==""&&p.PendingSkillCredits==0),"all capabilities and pending ledger expire at next round");
  });
  yield return("shop_precision_freeze_entry_clock",()=>
  {
   foreach(int defender in new[]{0,1})foreach(bool sameTickArm in new[]{false,true})
   {
    var s=new Simulation(c,new(){Training=true,Fighter0="rook",Fighter1="rook"});s.TrainingReset();s.SetTrainingLoadout(1-defender,new([Product(c,"rook","super_1")]));
    StepResult Both(byte defensive,byte attack,Buttons button)=>defender==0?Step(s,defensive,d1:CoreMath.RelativeDirection(attack,-1),b1:button):Step(s,attack,button,CoreMath.RelativeDirection(defensive,-1));
    foreach(byte dir in new byte[]{2,3,6,2,3})Both(5,dir,Buttons.None);
    if(!sameTickArm)Both(6,3,Buttons.None);long eligible=s.Players[defender].EligibleDefenseTick;long oldStamp=s.Players[defender].ParryArmEligibleTick;
    Both(sameTickArm?(byte)6:(byte)5,6,Buttons.LP);var p=s.Players[defender];Check(s.FullFreeze>0&&p.EligibleDefenseTick==eligible,"new freeze skips contact clock for either seat");Check(p.ParryArmEligibleTick==(sameTickArm?eligible:oldStamp)&&!p.ParryBonusFrozen,"simultaneous fresh edge retains sampled eligibility, prior edge retains age");
    while(s.FullFreeze>0)Both(5,5,Buttons.None);long clock=p.EligibleDefenseTick;Both(5,5,Buttons.None);Check(p.EligibleDefenseTick==clock+1,"first contact-evaluable tick advances once");
    s.TrainingReset();s.SetTrainingLoadout(1-defender,new([Product(c,"rook","super_1")]));
    Check(s.TryStartAction(1-defender,"super_1")==ActivationStatus.SuperUse,"frozen-edge setup");
    Both(6,5,Buttons.None);Check(s.Players[defender].ParryBonusFrozen,"edge originally entered during active freeze is ineligible for the bonus");
    var snapshot=s.Capture();string hash=s.Hash();Both(5,5,Buttons.None);s.Restore(snapshot);Check(s.Hash()==hash&&s.Players[defender].ParryBonusFrozen,"frozen-edge origin survives rollback");
   }
  });
  yield return("shop_actual_counter_and_parry_rewards",()=>
  {
   Simulation Close(){var sim=new Simulation(c,new(){Fighter0="rook",Fighter1="rook",SessionId="actual-skill"});sim.BeginFight();while(Math.Abs(sim.Players[0].X-sim.Players[1].X)>36000)Step(sim,6,d1:4);Wait(sim,30);return sim;}
   var hit=Close();var snapshot=hit.Capture();Step(hit,b0:Buttons.LP,b1:Buttons.HP);for(int t=0;t<6&&hit.Players[0].PendingSkillCredits==0;t++)Step(hit);Check(hit.Players[0].PendingSkillCredits==50&&hit.Players[0].Credits==600,"actual simultaneous startup contact accrues CH pending only");Check(hit.Players[0].SkillReceipts.Single().Category==SkillRewardCategory.CounterHit,"typed CH receipt");string hash=hit.Hash();long target=hit.Tick;hit.Restore(snapshot);Step(hit,b0:Buttons.LP,b1:Buttons.HP);while(hit.Tick<target)Step(hit);Check(hit.Hash()==hash,"real CH ledger rollback byte identity");
   foreach(int age in new[]{0,1,2})
   {
    var parry=Close();Step(parry,b0:Buttons.LP);while(parry.Players[0].ActionFrame<c.Fighters["rook"].Move("s_lp").Startup-age)Step(parry);Step(parry,d1:4);for(int t=0;t<4;t++)Step(parry);
    Check(parry.Players[1].Health==1000,"all legal parries defend age="+age+" hp="+parry.Players[1].Health+" retry="+parry.Players[1].ParryRetry);Check(parry.Players[1].PendingSkillCredits==(age<2?100:0),"perfect eligible clock age "+age);Check(parry.Players[1].Credits==600,"pending precision reward cannot spend now");
   }
  });
 }
}
