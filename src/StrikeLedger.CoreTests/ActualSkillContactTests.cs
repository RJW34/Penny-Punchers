using StrikeLedger.Core;

/// <summary>Collision/provenance coverage using actual competitive input, not supplied oracle facts.</summary>
public static class ActualSkillContactTests
{
 public static List<object> Evidence {get;}=[];
 static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
 static StepResult Step(Simulation s,byte direction0=5,Buttons buttons0=Buttons.None,byte direction1=5,Buttons buttons1=Buttons.None)
 {
  var before=s.Capture();var replay=new Simulation(s.Content,s.Config);replay.Restore(before);
  var a=new InputFrame(0,s.Tick,CoreMath.RelativeDirection(direction0,s.Players[0].Facing),buttons0);var b=new InputFrame(1,s.Tick,CoreMath.RelativeDirection(direction1,s.Players[1].Facing),buttons1);
  var actual=s.Step(a,b);var repeated=replay.Step(a,b);
  Check(actual.Hash==repeated.Hash&&actual.Events.SequenceEqual(repeated.Events),"actual contact tick reconstructs snapshot, roots, skill receipts and events");return actual;
 }
 static StepResult Seat(Simulation s,int seat,byte direction=5,Buttons buttons=Buttons.None)=>seat==0?Step(s,direction,buttons):Step(s,direction1:direction,buttons1:buttons);
 static void Wait(Simulation s,int ticks){for(int i=0;i<ticks&&s.Phase==MatchPhase.Fight;i++)Step(s);}
 static Simulation Close(GameContent c,int equippedSeat=-1,string product="")
 {
  var s=new Simulation(c,new(){Fighter0="rook",Fighter1="rook",SessionId="actual-contact-evidence"});s.CommitPreparation(new(equippedSeat==0?[product]:[]),new(equippedSeat==1?[product]:[]));s.BeginFight();
  int steps=0;while(Math.Abs(s.Players[0].X-s.Players[1].X)>36000&&steps++<180)Step(s,6,direction1:6);Check(steps<180,"legal approach reaches contact spacing");Wait(s,30);return s;
 }
 static ResolvedContactFacts Contact(Simulation s,int attacker,string? move=null,int ticks=50)
 {
  for(int i=0;i<ticks&&s.Phase==MatchPhase.Fight;i++)
  {
   Step(s);var fact=s.LastResolvedContacts.FirstOrDefault(f=>f.EffectiveOwner==attacker&&f.Outcome==ContactOutcome.Hit&&(move==null||f.MoveId==move));
   if(fact is not null){Evidence.Add(new{content=s.Content.ContentHash,core=typeof(Simulation).Assembly.ManifestModule.ModuleVersionId,kind="actual_accepted_contact",fact,bank=s.Players.Select(p=>p.Credits).ToArray(),pending=s.Players.Select(p=>p.PendingSkillCredits).ToArray(),hash=s.Hash()});return fact;}
  }
  throw new Exception("No actual accepted hit for seat "+attacker+" move "+move);
 }
 public static IEnumerable<(string Id,Action Test)> Cases(GameContent c)
 {
  yield return("shop_actual_repeated_baits_and_partial_cap",()=>
  {
   foreach(int earner in new[]{0,1})
   {
    var s=Close(c);int other=1-earner;var roots=new HashSet<RootAttackId>();
    void Ready()
    {
     Wait(s,80);int walk=0;while(Math.Abs(s.Players[0].X-s.Players[1].X)>36000&&walk++<180)Seat(s,earner,6);
     Wait(s,45);Check(s.Phase==MatchPhase.Fight&&s.Players.All(p=>p.Grounded&&p.Hitstun==0&&p.ParryRetry==0),"actual bait loop reaches neutral and clears approach-direction parry retry legally");
    }
    for(int n=0;n<3;n++)
    {
     Ready();if(earner==0)Step(s,buttons0:Buttons.LP,buttons1:Buttons.HP);else Step(s,buttons0:Buttons.HP,buttons1:Buttons.LP);
     var fact=Contact(s,earner,"s_lp",15);Check(roots.Add(fact.Root)&&SkillRewardLedger.Classify(fact)==SkillRewardCategory.CounterHit,"each repeated CH bait has a distinct eligible root");
     Check(s.Players[earner].PendingSkillCredits==Math.Min(n+1,2)*50,"third real CH bait pays no extra category income");
    }
    for(int n=0;n<3;n++)
    {
     Ready();Seat(s,other,8);int age=0;while(s.Players[other].Grounded&&age++<8)Step(s);Seat(s,earner,buttons:Buttons.LP);
     var fact=Contact(s,earner,"s_lp",15);Check(roots.Add(fact.Root)&&SkillRewardLedger.Classify(fact)==SkillRewardCategory.AntiAir,"each repeated AA bait is an actual fresh voluntary jump opener");
     Check(s.Players[earner].PendingSkillCredits==100+Math.Min(n+1,2)*75,"third real AA bait pays no extra category income");
    }
    void Precision(Simulation world)
    {
     Seat(world,other,buttons:Buttons.LP);while(world.Players[other].ActionFrame<c.Fighters["rook"].Move("s_lp").Startup)Step(world);
     var result=Seat(world,earner,6);var facts=world.LastResolvedContacts.ToArray();
     Check(result.Events.Any(e=>e.Kind==CombatEventKind.Parry&&e.Seat==earner)&&facts.Any(f=>f.EarnerSeat==earner&&f.Outcome==ContactOutcome.Parry&&f.ParryEligibleAge==0),"actual fresh manual edge parries at eligible age0: "+System.Text.Json.JsonSerializer.Serialize(new{earner,world.Tick,events=result.Events,facts,players=world.Players.Select(p=>new{p.X,p.Y,p.Health,p.ActionId,p.ActionFrame,p.ParryTicks,p.ParryRetry,p.PendingSkillCredits})}));
     Evidence.Add(new{kind="actual_repeated_precision_contact",content=c.ContentHash,facts,bank=world.Players.Select(p=>p.Credits).ToArray(),pending=world.Players.Select(p=>p.PendingSkillCredits).ToArray(),receipts=world.Players[earner].SkillReceipts.ToArray(),hash=world.Hash()});
    }
    for(int n=0;n<3;n++)
    {
     Ready();Precision(s);Check(s.Players[earner].PendingSkillCredits==300,"real precision bait clips to aggregate300 and later roots pay zero");
     Check(s.Players.All(p=>p.Credits==600),"all bait income remains nonspendable during Fight");
    }
    var partial=s.Players[earner].SkillReceipts.Single(r=>r.Category==SkillRewardCategory.PerfectParry);
    Check(partial.Nominal==100&&partial.Allowed==50&&partial.Capped==50,"actual mixed-category final receipt truthfully reports partial50");
    Check(s.Players[earner].SkillReceipts.Count==5&&s.Players[earner].SkillRewards.RootCount>=9,"capped fresh real roots are consumed without additional paid receipts");
    var precisionOnly=Close(c);
    for(int n=0;n<3;n++){Wait(precisionOnly,80);int approach=0;while(Math.Abs(precisionOnly.Players[0].X-precisionOnly.Players[1].X)>36000&&approach++<180)Seat(precisionOnly,earner,6);Wait(precisionOnly,45);Precision(precisionOnly);Check(precisionOnly.Players[earner].PendingSkillCredits==Math.Min(n+1,2)*100,"three real perfect parries respect the independent two-paid category cap");}
   }
  });
  yield return("shop_actual_antiair_both_seats",()=>
  {
   foreach(int attacker in new[]{0,1})
   {
    var s=Close(c);int defender=1-attacker;Seat(s,defender,8);int age=0;while(s.Players[defender].Grounded&&age++<8)Step(s);
    Check(!s.Players[defender].Grounded&&s.Players[defender].VoluntaryAir,"real jump establishes voluntary-air state");
    Seat(s,attacker,buttons:Buttons.LP);var fact=Contact(s,attacker,"s_lp",15);
    Check(fact.AttackerGrounded&&!fact.AttackerPrejump&&fact.DefenderAirborne&&fact.DefenderVoluntaryAir&&!fact.DefenderInCombo,"AA captures simultaneous actual prestate");
    Check(fact.AttackerPrestate is not null&&fact.DefenderPrestate is {Y:>0,Hitstun:0,ComboCount:0},"auditable full prestate is attached before hit mutation");
    Check(s.Players[attacker].PendingSkillCredits==75&&s.Players[attacker].Credits==600,"grounded anti-air accrues75 pending without bank movement");
   }
  });
  yield return("shop_actual_ground_launch_not_antiair",()=>
  {
   var item=c.Items.Values.Single(i=>i.CatalogId=="R-T3");
   foreach(int attacker in new[]{0,1})
   {
    var s=Close(c,attacker,item.Id);Seat(s,attacker,6,Buttons.HP);var fact=Contact(s,attacker,item.MoveId);
    Check(!fact.DefenderAirborne&&fact.DefenderPrestate is {Y:0},"ground launcher stores target before launch");
    Check(s.Players[1-attacker].Y>0,"same real hit actually launches target");
    Check(SkillRewardLedger.Classify(fact)==SkillRewardCategory.None&&s.Players[attacker].PendingSkillCredits==0,"post-hit airborne geometry cannot invent anti-air income");
   }
  });
  yield return("shop_actual_passive_feint_not_counter_income",()=>
  {
   var gambit=c.Items.Values.First(i=>i.EligibleFighters.Contains("rook")&&i.Slot=="gambit"&&c.Fighters["rook"].Move(i.MoveId).Mimic is not null);
   foreach(int attacker in new[]{0,1})
   {
    var s=Close(c,1-attacker,gambit.Id);
    if(attacker==0)Step(s,buttons0:Buttons.LP,direction1:4,buttons1:Buttons.HP|Buttons.HK);else Step(s,4,Buttons.HP|Buttons.HK,buttons1:Buttons.LP);
    var fact=Contact(s,attacker,"s_lp",15);
    Check(fact.DefenderPrestate?.ActionId==gambit.MoveId&&!fact.DefenderOffensiveStartup,"passive feint has actual nonempty startup but no offensive capability");
    Check(s.Players[attacker].PendingSkillCredits==0,"existing counter-damage label does not pay passive-feint reward");
   }
  });
  yield return("shop_actual_lethal_tick_reward_and_settlement",()=>
  {
   foreach(int attacker in new[]{0,1})
   {
    var s=Close(c);int defender=1-attacker;int counterDamage=c.Fighters["rook"].Move("s_lp").Hitboxes[0].Damage*c.Combat.Damage.CounterhitPercent/100;
    int attacks=0;
    while(s.Players[defender].Health>counterDamage&&attacks++<60)
    {
     // Re-approach after real pushback, then allow hitstun/recovery to end before the next distinct opener.
     int walk=0;while(Math.Abs(s.Players[0].X-s.Players[1].X)>36000&&walk++<90)Seat(s,attacker,6);
     Wait(s,20);Seat(s,attacker,buttons:Buttons.LP);Contact(s,attacker,"s_lp",15);Wait(s,45);
    }
    Check(attacks<60&&s.Players[defender].Health>0&&s.Players[attacker].PendingSkillCredits==0,"ordinary legal hits reduce health without awarding damage income");
    while(Math.Abs(s.Players[0].X-s.Players[1].X)>36000)Seat(s,attacker,6);Wait(s,20);
    var before=s.Capture();
    if(attacker==0)Step(s,buttons0:Buttons.LP,buttons1:Buttons.HP);else Step(s,buttons0:Buttons.HP,buttons1:Buttons.LP);
    var terminalFact=Contact(s,attacker,"s_lp",15);
    Check(s.Phase==MatchPhase.PendingResult&&terminalFact.Tick==s.PendingResult!.TerminalTick&&s.Players[attacker].PendingSkillCredits==50,"actual lethal startup interrupt pays on terminal tick before settlement");
    Check(s.Players[attacker].Credits==600,"terminal prediction still has original frozen bank");
    var receipt=s.SettleRound(s.PendingResult!.TerminalTick);var bonus=attacker==0?receipt.SkillPayout0:receipt.SkillPayout1;
    Check(bonus==new SkillSettlementReceipt(50,50,0)&&s.Players[attacker].Credits==1850,"confirmed lethal receipt deposits outcome1200 plus earned50 exactly");
    Check(ReferenceEquals(receipt,s.SettleRound(s.PendingResult.TerminalTick)),"confirmed duplicate settlement does not deposit again");
    s.Restore(before);Step(s);Check(s.Players[attacker].PendingSkillCredits==0&&s.Players[attacker].Credits==600&&s.Phase==MatchPhase.Fight,"restoring before terminal removes prior outcome and skill grant together");
   }
  });
 }
}
