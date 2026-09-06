using StrikeLedger.Core;

public static class ShopSkillRewardTests
{
 static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
 static void Reject(Action action,string message){try{action();}catch(InvalidDataException){return;}throw new Exception(message);}
 static ResolvedContactFacts Hit(int n=1,int seat=0)=>new(n,seat,new("skill-tests",1,seat,n),seat,0,ActualDamage:35,ThreatDamage:35,AttackerGrounded:true,DefenderOffensiveStartup:true);
 static ResolvedContactFacts Parry(int n=1,int seat=0)=>new(n,seat,new("skill-tests",1,1-seat,n),1-seat,0,ContactOutcome.Parry,ThreatDamage:35,ParryArmId:n,ParryEligibleAge:0,FreshManualParry:true);
 static SkillRewardCategory Classify(ResolvedContactFacts f)=>SkillRewardLedger.Classify(f);
 public static IEnumerable<(string Id,Action Test)> Cases(GameContent content)
 {
  yield return("shop_skill_counter_immutable_facts",()=>
  {
   foreach(int seat in new[]{0,1})
   {
    var f=Hit(seat:seat);Check(Classify(f)==SkillRewardCategory.CounterHit,"offensive startup direct opener");
    foreach(var denied in new[]{f with{DefenderOffensiveStartup=false},f with{DefenderDisabled=true},f with{DefenderInCombo=true},f with{ActualDamage=0},f with{Accepted=false},f with{Outcome=ContactOutcome.Armor},f with{Outcome=ContactOutcome.Block},f with{Outcome=ContactOutcome.Counter},f with{AttackKind=AttackKind.Projectile},f with{AttackKind=AttackKind.Field},f with{AttackKind=AttackKind.Throw},f with{EffectiveOwner=1-seat}})
     Check(Classify(denied)==SkillRewardCategory.None,"no income for ineligible prestate/provenance "+denied);
   }
  });
  yield return("shop_skill_antiair_opener_priority",()=>
  {
   foreach(int seat in new[]{0,1})
   {
    var f=Hit(seat:seat) with{DefenderAirborne=true,DefenderVoluntaryAir=true};
    Check(Classify(f)==SkillRewardCategory.AntiAir,"AA takes priority over CH");
    foreach(var denied in new[]{f with{DefenderOffensiveStartup=false,DefenderAirborne=false},f with{DefenderOffensiveStartup=false,DefenderVoluntaryAir=false},f with{DefenderOffensiveStartup=false,AttackerGrounded=false},f with{DefenderOffensiveStartup=false,AttackerPrejump=true},f with{DefenderDisabled=true},f with{DefenderInCombo=true},f with{AttackKind=AttackKind.Projectile}})
     Check(Classify(denied)==SkillRewardCategory.None,"AA prestate exclusion "+denied);
    var ledger=new SkillRewardLedger();Check(ledger.Apply(f).Allowed==75,"AA overlap pays75 only");
    Check(ledger.Apply(f with{Tick=2,ContactOrdinal=1}).Allowed==0,"same attack branch cannot earn twice");
    var early=new SkillRewardLedger();early.Apply(Hit(seat:seat));Check(early.Apply(f with{Tick=3,ContactOrdinal=2}).Allowed==0,"early CH cannot upgrade to later AA");
    var ordinary=new SkillRewardLedger();ordinary.Apply(Hit(seat:seat) with{DefenderOffensiveStartup=false});
    Check(ordinary.Apply(f with{Tick=3,ContactOrdinal=2}).Allowed==0,"ordinary opener cannot manufacture a later rewarding branch");
   }
  });
  yield return("shop_skill_manual_precision_and_origin",()=>
  {
   foreach(int seat in new[]{0,1})foreach(var kind in new[]{AttackKind.Direct,AttackKind.Projectile,AttackKind.Field})
   {
    var f=Parry(seat:seat) with{AttackKind=kind};
    Check(Classify(f)==SkillRewardCategory.PerfectParry&&Classify(f with{ParryEligibleAge=1})==SkillRewardCategory.PerfectParry,"eligible clocks0/1");
    foreach(var denied in new[]{f with{ParryEligibleAge=2},f with{ParryEligibleAge=-1},f with{BonusIneligibleFrozenEdge=true},f with{FreshManualParry=false},f with{ParryArmId=0},f with{ThreatDamage=0},f with{Root=f.Root with{OriginSeat=seat}},f with{EffectiveOwner=seat},f with{AttackKind=AttackKind.Throw}})
     Check(Classify(denied)==SkillRewardCategory.None,"precision timing/origin exclusion "+denied);
    var ledger=new SkillRewardLedger();Check(ledger.Apply(f).Allowed==100,"manual enemy threat rewards");
    Check(ledger.Apply(f with{Tick=2,ParryArmId=2,ContactOrdinal=1}).Allowed==0,"fresh tap against same multi-hit root defends without extra income");
   }
  });
  yield return("shop_skill_caps_and_partial_receipts",()=>
  {
   var ledger=new SkillRewardLedger();ledger.Apply(Hit(1));ledger.Apply(Hit(2));
   var categoryCapped=ledger.Apply(Hit(3));Check(categoryCapped.Allowed==0&&categoryCapped.Capped==50&&categoryCapped.Reason=="category_cap","CH capped after two paid receipts");
   ledger.Apply(Hit(4) with{DefenderAirborne=true,DefenderVoluntaryAir=true});ledger.Apply(Parry(5));
   var partial=ledger.Apply(Parry(6));Check(partial.Nominal==100&&partial.Allowed==25&&partial.Capped==75&&ledger.Pending==300,"last award has honest25 partial100 receipt");
   int roots=ledger.RootCount;var capped=ledger.Apply(Hit(7) with{DefenderAirborne=true,DefenderVoluntaryAir=true});
   Check(capped.Allowed==0&&ledger.RootCount==roots+1,"globally capped decision still consumes root");
   Check(ledger.Apply(Hit(7) with{Tick=8,ContactOrdinal=1}).Reason=="duplicate_root","no later capped-root fallback");
   var priority=new SkillRewardLedger();for(int n=1;n<=3;n++)priority.Apply(Hit(n) with{DefenderAirborne=true,DefenderVoluntaryAir=true});
   Check(priority.Pending==150&&priority.Count(SkillRewardCategory.CounterHit)==0,"capped AA never falls back to CH");
   Check(ledger.Receipts.Sum(x=>x.Allowed)==ledger.Pending&&ledger.Receipts.All(x=>x.Nominal==x.Allowed+x.Capped),"receipt arithmetic exact");
  });
  yield return("shop_skill_duplicates_order_and_fail_closed",()=>
  {
   var ledger=new SkillRewardLedger();var first=Parry(1) with{EventId="event-identity"};ledger.Apply(first);
   Check(ledger.Apply(first).Reason=="duplicate_event"&&ledger.Pending==100,"identical duplicate ignored");
   Reject(()=>ledger.Apply(first with{ParryEligibleAge=1}),"conflicting identity must fail closed");Check(ledger.Pending==100&&ledger.FactCount==1,"conflict cannot mutate ledger");
   ledger.Apply(Parry(3));Reject(()=>ledger.Apply(Parry(2)),"unsorted fact rejected");Check(ledger.Pending==200,"unsorted cannot mutate ledger");
   var plain=new SkillRewardLedger();var ineligible=Hit() with{EventId="ordinary",DefenderOffensiveStartup=false};plain.Apply(ineligible);
   Reject(()=>plain.Apply(ineligible with{DefenderOffensiveStartup=true}),"ineligible fact identity is also protected");
   Reject(()=>plain.Apply(Hit(2) with{Root=new("",1,0,1)}),"malformed root fails");
  });
  yield return("shop_skill_training_policy_and_bounded_feeding",()=>
  {
   var training=new SkillRewardLedger();var f=Parry() with{Training=true};Check(Classify(f)==SkillRewardCategory.PerfectParry,"practice diagnostic can classify precision");
   Check(training.Apply(f).Allowed==0&&training.Pending==0&&training.Receipts.Count==0&&training.FactCount==0,"untimed training grants no competitive income or unbounded ledger history");
   var disabled=new SkillRewardLedger(new(TotalLimit:0));Check(disabled.Apply(Parry()).Allowed==0,"explicit no-skill laboratory control");
   var rng=new Random(2002);var ledgers=new[]{new SkillRewardLedger(),new SkillRewardLedger()};
   for(int tick=1;tick<=2000;tick++)
   {
    int seat=rng.Next(2),kind=rng.Next(3);var fact=kind==0?Parry(tick,seat):Hit(tick,seat) with{DefenderAirborne=kind==1,DefenderVoluntaryAir=kind==1};
    ledgers[seat].Apply(fact);
    Check(ledgers.All(x=>x.Pending<=300&&Enum.GetValues<SkillRewardCategory>().All(c=>x.Count(c)<=2)),"feeding cannot exceed per-player category/global caps");
   }
   Check(ledgers.All(x=>x.Pending==300&&x.Receipts.Sum(r=>r.Allowed)==300),"both caps independently reached from deterministic feeding");
   Check(ledgers.Sum(x=>x.FactCount)==2000&&ledgers.All(x=>x.RootCount<=2000),"round-local bounded fact/root storage");
  });
 }
}
