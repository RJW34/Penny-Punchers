using StrikeLedger.Core;

/// <summary>Scripted counterplay measurements, not a human reaction or matchup-balance verdict.</summary>
public static class ShopPressureTests
{
 public static readonly List<object> Evidence=[];
 static void Check(bool condition,string reason){if(!condition)throw new Exception(reason);}
 static IEnumerable<(byte Direction,Buttons Held)> Encode(GameContent c,MoveDefinition move,int facing)
 {
  yield return(5,Buttons.None);var parts=move.Command.Split('+');Buttons button=parts[1]=="PP"?Buttons.LP|Buttons.MP:Buttons.LK|Buttons.MK;
  if(parts[0].StartsWith("charge_")){for(int i=0;i<c.Inputs.ChargeHoldTicks;i++)yield return(CoreMath.RelativeDirection(parts[0]=="charge_back"?(byte)4:(byte)2,facing),Buttons.None);yield return(CoreMath.RelativeDirection(parts[0]=="charge_back"?(byte)6:(byte)8,facing),button);}
  else{var pattern=c.Inputs.MotionPatterns[parts[0]];for(int i=0;i<pattern.Length;i++)yield return(CoreMath.RelativeDirection((byte)pattern[i],facing),i==pattern.Length-1?button:Buttons.None);}
 }
 public static IEnumerable<(string Id,Action Test)> Cases(GameContent c)
 {
  yield return("shop_ex_zero_bank_corner_counterplay",()=>
  {
   int cases=0,parryWitnesses=0;
   foreach(var fighter in c.Fighters.Values)foreach(var move in fighter.Moves.Where(m=>m.Kind=="ex_special"))foreach(bool mirror in new[]{false,true})foreach(string response in new[]{"guard","parry","jump","throw","retreat"})
   {
    var sim=new Simulation(c,new(){Training=true,Fighter0=fighter.Id,Fighter1="rook",SessionId="shop-ex-pressure"});sim.TrainingReset(0);sim.SetTrainingLoadout(0,new([move.Availability]));
    sim.SetTrainingState(0,x:mirror?51000:717000);sim.SetTrainingState(1,x:mirror?16000:752000,health:100);sim.Step(new(0,sim.Tick,5,Buttons.None),new(1,sim.Tick,5,Buttons.None));
    string initial=Convert.ToBase64String(sim.Capture().Bytes);var frames=new List<LegalFrame>();var commands=new Queue<(byte Direction,Buttons Held)>(Encode(c,move,sim.Players[0].Facing));bool started=false,responded=false,parryHeld=false;
    for(int tick=0;tick<240&&sim.Phase==MatchPhase.Fight;tick++)
    {
     var attack=commands.Count>0?commands.Dequeue():((byte)5,Buttons.None);var defender=sim.Players[1];byte direction=5;Buttons buttons=Buttons.None;
     if(response=="guard")direction=CoreMath.RelativeDirection(1,defender.Facing);
     if(response=="retreat")direction=CoreMath.RelativeDirection(4,defender.Facing);
     if(started&&!responded&&response is "jump" or "throw"){responded=true;if(response=="jump")direction=8;else buttons=Buttons.LP|Buttons.LK;}
     if(response=="parry")
     {
      var attacker=sim.Players[0];bool direct=move.Hitboxes.Length>0&&attacker.ActionId==move.Id&&attacker.Hitstop==0&&attacker.ActionFrame>=move.Startup&&attacker.ActionFrame<move.Startup+move.Active;
      bool shot=sim.Projectiles.Any(p=>p.Owner==0&&p.ContactCooldown==0&&p.Hitstop==0&&Math.Abs(p.X-defender.X)<(p.Definition.Width+34000)/2+Math.Abs(p.Vx));
      if(!parryHeld&&defender.Hitstop==0&&(direct||shot)){direction=move.Hitboxes.FirstOrDefault()?.Level=="low"?(byte)2:CoreMath.RelativeDirection(6,defender.Facing);parryHeld=true;}else parryHeld=false;
     }
     var result=sim.Step(new(0,sim.Tick,attack.Item1,attack.Item2),new(1,sim.Tick,direction,buttons));frames.Add(new(result.Tick,attack.Item1,attack.Item2,direction,buttons,result.Events.ToArray(),result.Hash));started|=result.Events.Any(e=>e.Kind==CombatEventKind.ActionStarted&&e.Seat==0&&e.MoveId==move.Id);
     Check(sim.Players.All(p=>p.Credits==0&&p.SpendReceipts.Count==0),"pressure cannot debit or award spendable fight money");
    }
    Check(started,"licensed EX input actually starts "+fighter.Id+":"+move.Id);var events=frames.SelectMany(f=>f.Events).ToArray();int parries=events.Count(e=>e.Kind==CombatEventKind.Parry&&e.Seat==1);parryWitnesses+=parries>0?1:0;
    var replay=new Simulation(c,sim.Config);replay.Restore(new(Convert.FromBase64String(initial)));foreach(var frame in frames){var actual=replay.Step(new(0,replay.Tick,frame.D0,frame.B0),new(1,replay.Tick,frame.D1,frame.B1));Check(actual.Hash==frame.Hash&&actual.Events.SequenceEqual(frame.Events),"counterplay trace replay");}
    string id=$"shop-pressure-{fighter.Id}-{move.Id}-{mirror}-{response}";AuditRegressionTests.Traces.Add(new(id,sim.Config,initial,frames.ToArray(),sim.Hash()));
    Evidence.Add(new{id,fighter=fighter.Id,move=move.Id,mirror,response,startingDefenderHealth=100,bank=0,ownedDefenseProducts=0,licensedStartup=started,defenderDamage=100-sim.Players[1].Health,defenderSurvived=sim.Players[1].Health>0,parries,blocks=events.Count(e=>e.Kind==CombatEventKind.Block&&e.Seat==1),freeResponseStarts=events.Where(e=>e.Kind is CombatEventKind.ActionStarted or CombatEventKind.Jump&&e.Seat==1).Select(e=>e.MoveId).ToArray(),reconstructed=true,scope="Scripted legal response; predictive parry timing, not measured human reaction. Failed responses are recorded outcomes in an already-cornered100HP state."});cases++;
   }
   Check(cases==c.Fighters.Values.Sum(f=>f.Moves.Count(m=>m.Kind=="ex_special"))*10,"all EX/facing/response cells executed");Check(parryWitnesses>0,"at least one actual zero-bank parry witness exists");
  });
 }
}
