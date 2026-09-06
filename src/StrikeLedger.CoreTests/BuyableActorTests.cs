using StrikeLedger.Core;

public static class BuyableActorTests
{
 static void Check(bool value,string reason){if(!value)throw new Exception(reason);}
 static StepResult Step(Simulation s,byte direction=5,Buttons buttons=Buttons.None,byte otherDirection=5,Buttons otherButtons=Buttons.None)
 {
  var snapshot=s.Capture();var clone=new Simulation(s.Content,s.Config);clone.Restore(snapshot);
  var a=new InputFrame(0,s.Tick,CoreMath.RelativeDirection(direction,s.Players[0].Facing),buttons);
  var b=new InputFrame(1,s.Tick,CoreMath.RelativeDirection(otherDirection,s.Players[1].Facing),otherButtons);
  var result=s.Step(a,b);var replay=clone.Step(a,b);
  Check(result.Hash==replay.Hash&&result.Events.SequenceEqual(replay.Events),"actor tick snapshot/events reconstruct exactly");return result;
 }
 static (Simulation Sim,MoveDefinition Move) Fixture(GameContent content,string catalog,bool mirror=false,int gap=45000)
 {
  var item=content.Items.Values.Single(i=>i.CatalogId==catalog);string f=item.EligibleFighters[0];
  var s=new Simulation(content,new(){Fighter0=f,Fighter1="rook",Training=true,SessionId="buyable-actors"});s.TrainingReset(3600);
  s.SetTrainingState(0,x:mirror?450000:300000);s.SetTrainingState(1,x:mirror?450000-gap:300000+gap);Step(s);
  s.SetTrainingLoadout(0,new([item.Id]));return(s,content.Fighters[f].Move(item.MoveId));
 }
 static void Start(Simulation s,MoveDefinition move)
 {
  if(move.Command.StartsWith("qcb")){Step(s,2);Step(s,1);Step(s,4,Buttons.LP);}
  else if(move.Command=="6+HP")Step(s,6,Buttons.HP);
  else Step(s,4,Buttons.HP|Buttons.HK);
  Check(s.Players[0].ActionId==move.Id,"real input starts "+move.CatalogId);
 }
 public static IEnumerable<(string Id,Action Test)> Cases(GameContent content)
 {
  yield return("buy_actor_counter_classes",()=>
  {
   foreach(bool mirror in new[]{false,true})foreach(string attack in new[]{"s_lp","c_lk","pulse_l","super_1"})
   {
    var(s,move)=Fixture(content,"R-S4",mirror);
    if(attack=="super_1")s.SetTrainingLoadout(1,new([content.Items.Values.Single(i=>i.EligibleFighters.Contains("rook")&&i.MoveId==attack).Id]));
    Start(s,move);
    while(s.Players[0].ActionFrame<5)Step(s);
    Check(s.TryStartAction(1,attack) is ActivationStatus.Free or ActivationStatus.Licensed or ActivationStatus.SuperUse,"counter attacker setup "+attack);
    var events=new List<CombatEvent>();for(int i=0;i<55&&s.Phase==MatchPhase.Fight;i++)events.AddRange(Step(s).Events);
    bool caught=events.Any(e=>e.Kind==CombatEventKind.CounterCaught);
    Check(caught==(attack=="s_lp"),"counter class filter "+attack+" mirror "+mirror);
    Check(s.Players[0].SpendReceipts.Count==0,"counter/riposte have no activation fee");
   }
   foreach(bool mirror in new[]{false,true})
   {
    var(s,m)=Fixture(content,"R-S4",mirror);Start(s,m);while(s.Players[0].ActionFrame<5)Step(s);
    s.SetTrainingState(1,x:s.Players[0].X-s.Players[0].Facing*35000);Step(s);s.TryStartAction(1,"s_lp");
    var events=new List<CombatEvent>();for(int i=0;i<20;i++)events.AddRange(Step(s).Events);
    Check(!events.Any(e=>e.Kind==CombatEventKind.CounterCaught),"rear direct attack bypasses counter");
   }
  });
  yield return("buy_actor_hold_armor",()=>
  {
   foreach(bool mirror in new[]{false,true})
   {
    var(s,m)=Fixture(content,"R-T4",mirror,gap:150000);Start(s,m);int ticks=1;int activeTicks=0;
    while(s.Players[0].ActionFrame<m.Startup&&ticks<70){if(s.Players[0].ArmorActive)activeTicks++;Step(s,6,Buttons.HP);ticks++;}
    Check(ticks==36&&s.Players[0].HeldStartupTicks==18,"held startup is bounded to36 actor ticks");
    Check(activeTicks<=8&&!s.Players[0].ArmorActive,"holding does not extend/rearm armor");
    var(receive,armor)=Fixture(content,"R-T4",mirror);Start(receive,armor);while(receive.Players[0].ActionFrame<6)Step(receive,6,Buttons.HP);
    receive.TryStartAction(1,"s_lp");var events=new List<CombatEvent>();for(int i=0;i<10;i++)events.AddRange(Step(receive,6,Buttons.HP).Events);
    Check(events.Any(e=>e.Kind==CombatEventKind.Hit&&e.Target==0&&e.Detail=="armor"),"one eligible hit is absorbed with full damage");
    Check(receive.Players[0].Health<receive.Players[0].MaxHealth&&receive.Players[0].ActionId==armor.Id,"armor retains action, not health");
    var(lethal,l)=Fixture(content,"R-T4",mirror);lethal.SetTrainingState(0,health:1);Start(lethal,l);while(lethal.Players[0].ActionFrame<6)Step(lethal);lethal.TryStartAction(1,"s_lp");
    for(int i=0;i<10&&lethal.Phase==MatchPhase.Fight;i++)Step(lethal);
    Check(lethal.Players[0].Health==0&&lethal.Phase==MatchPhase.PendingResult,"lethal armor contact ends round");
   }
  });
  yield return("buy_actor_aligned_exclusions_and_prejump",()=>
  {
   foreach(bool mirror in new[]{false,true})
   {
    bool projectileInsideWindow=false;
    for(int lead=0;lead<=3&&!projectileInsideWindow;lead++)
    {
     var(s,counter)=Fixture(content,"R-S4",mirror);s.TryStartAction(1,"pulse_l");for(int i=0;i<lead;i++)Step(s);Start(s,counter);
     for(int i=0;i<24;i++)
     {
      bool live=s.Players[0].ActionId==counter.Id&&s.Players[0].ActionFrame>=5&&s.Players[0].ActionFrame<11;
      var result=Step(s);
      if(live&&result.Events.Any(e=>e.Kind==CombatEventKind.Hit&&e.Seat==1&&e.MoveId=="pulse_l"))
      {projectileInsideWindow=true;Check(!result.Events.Any(e=>e.Kind==CombatEventKind.CounterCaught),"projectile contacts inside live counter window without auto-catch");break;}
     }
    }
    Check(projectileInsideWindow,"projectile exclusion has an actual aligned contact witness, mirror "+mirror);
    var(thrown,c)=Fixture(content,"R-S4",mirror,gap:35000);Start(thrown,c);while(thrown.Players[0].ActionFrame<5)Step(thrown);thrown.TryStartAction(1,"throw_forward");
    var throwEvents=new List<CombatEvent>();for(int i=0;i<35;i++)throwEvents.AddRange(Step(thrown).Events);
    Check(throwEvents.Any(e=>e.Kind==CombatEventKind.Throw&&e.Seat==1)&&!throwEvents.Any(e=>e.Kind==CombatEventKind.CounterCaught),"normal throw bypasses counter and actually captures");
    var(vault,v)=Fixture(content,"R-G3",mirror,gap:35000);vault.TryStartAction(1,"throw_forward");Step(vault);Start(vault,v);
    Check(vault.Players[0].JumpStart>0&&!vault.Players[0].VoluntaryAir,"vault startup is prejump, not grounded neutral or airborne yet");
    var vaultEvents=new List<CombatEvent>();for(int i=0;i<35;i++)vaultEvents.AddRange(Step(vault).Events);
    Check(!vaultEvents.Any(e=>e.Kind==CombatEventKind.Throw&&e.Seat==1)&&vault.Players[0].Health==vault.Players[0].MaxHealth,"vault prejump rejects the same normal throw timing as an ordinary jump");
    var(dizzy,armor)=Fixture(content,"R-T4",mirror);Start(dizzy,armor);while(dizzy.Players[0].ActionFrame<6)Step(dizzy);dizzy.SetTrainingState(0,stun:content.Fighters[dizzy.Players[0].FighterId].StunLimit-1);dizzy.TryStartAction(1,"s_lp");
    var dizzyEvents=new List<CombatEvent>();for(int i=0;i<10;i++)dizzyEvents.AddRange(Step(dizzy).Events);
    Check(dizzyEvents.Any(e=>e.Kind==CombatEventKind.Dizzy)&&dizzy.Players[0].ActionId!=armor.Id,"stun-limit transition bypasses armor retention");
   }
  });
  yield return("buy_actor_vault_and_slipgate",()=>
  {
   foreach(bool mirror in new[]{false,true})
   {
    var(s,m)=Fixture(content,"R-G3",mirror,gap:180000);int origin=s.Players[0].X;Start(s,m);int peak=0,airTicks=0,maxLanding=0;
    for(int i=0;i<42;i++)
    {
     if(!s.Players[0].Grounded){airTicks++;Check(s.Players[0].VoluntaryAir,"vault actual flight is voluntary airborne provenance");Check(s.TryStartAction(0,"j_lp")==ActivationStatus.Illegal,"vault disallows air attacks");}
     peak=Math.Max(peak,s.Players[0].Y);maxLanding=Math.Max(maxLanding,s.Players[0].LandingTicks);Step(s);
    }
    Check(peak==40000&&airTicks==19&&maxLanding==8,"vault exact discrete arc and8 landing ticks");
    Check(Math.Abs(s.Players[0].X-origin)==20000&&s.Players[0].Grounded&&s.Players[0].Actionable&&!s.Players[0].VoluntaryAir,"vault20-unit translation, clears air provenance and returns actionable");
    var(g,gate)=Fixture(content,"V-G4",mirror,gap:180000);origin=g.Players[0].X;Start(g,gate);int destination=g.Players[0].DestinationX!.Value;
    Check(destination==origin-80000*g.Players[0].Facing,"Slipgate freezes bounded startup destination");
    for(int i=0;i<40;i++)Step(g);
    Check(g.Players[0].X==destination&&g.Players[0].Actionable&&g.Players[0].DestinationX==null,"Slipgate completes fixed travel/recovery and clears its destination marker");
    var(wall,w)=Fixture(content,"V-G4",mirror,gap:120000);wall.SetTrainingState(0,x:mirror?752000:16000);Start(wall,w);origin=wall.Players[0].X;
    for(int i=0;i<15;i++)Step(wall);Check(wall.Players[0].X==origin&&!wall.Players[0].Actionable,"zero-distance wall use retains commitment");
   }
  });
  yield return("buy_actor_air_input_and_landing",()=>
  {
   foreach(bool mirror in new[]{false,true})
   {
    var(s,m)=Fixture(content,"V-T3",mirror,gap:230000);Step(s,9);while(s.Players[0].Y==0)Step(s);
    var denied=Step(s,2,Buttons.MK);Check(s.Players[0].ActionId==""&&!denied.Events.Any(e=>e.Kind==CombatEventKind.ActionStarted)&&denied.Events.Any(e=>e.Kind==CombatEventKind.Rejected&&e.Detail=="actor-state"),"low-height equipped dive rejects explicitly without falling back to j_mk");
    while(s.Players[0].Y<50000)Step(s);Step(s);Step(s,2,Buttons.MK);Check(s.Players[0].ActionId==m.Id,"legal forward-jump dive input");
    int landing=0;for(int i=0;i<60;i++){landing=Math.Max(landing,s.Players[0].LandingTicks);Step(s);}
    Check(landing==16&&s.Players[0].Actionable&&s.Players[0].SpendReceipts.Count==0,"whiffed dive mandatory16landing and free use");
    var(air,grab)=Fixture(content,"V-T4",mirror,gap:40000);Step(air,8,otherDirection:8);while(air.Players.Any(p=>p.Grounded))Step(air);
    Step(air,buttons:Buttons.LP|Buttons.LK);Check(air.Players[0].ActionId==grab.Id,"fresh air-throw chord starts Skycatch");
    var events=new List<CombatEvent>();int maxLanding=0;for(int i=0;i<90;i++){events.AddRange(Step(air).Events);maxLanding=Math.Max(maxLanding,air.Players[0].LandingTicks);}
    Check(events.Any(e=>e.Kind==CombatEventKind.Throw&&e.Seat==0&&e.Value==105),"untechable eligible air capture deals105");
    Check(maxLanding==18&&air.Players[0].SpendReceipts.Count==0,"air capture keeps18landing and no activation fee");
   }
  });
 }
}
