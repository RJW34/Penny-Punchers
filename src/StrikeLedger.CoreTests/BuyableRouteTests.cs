using StrikeLedger.Core;

/// <summary>Training setup is explicit; every action transition below uses ordinary per-tick input.</summary>
public static class BuyableRouteTests
{
 static void Check(bool pass,string reason){if(!pass)throw new Exception(reason);}
 sealed class Trace
 {
  public Simulation Sim {get;}public List<LegalFrame> Frames {get;}=[];readonly string initial;readonly string name;
  public Trace(string id,Simulation sim){name=id;Sim=sim;initial=Convert.ToBase64String(sim.Capture().Bytes);}
  public StepResult Step(byte direction=5,Buttons buttons=Buttons.None,byte defense=5,Buttons other=Buttons.None)
  {
   var a=CoreMath.RelativeDirection(direction,Sim.Players[0].Facing);var b=CoreMath.RelativeDirection(defense,Sim.Players[1].Facing);
   var result=Sim.Step(new(0,Sim.Tick,a,buttons),new(1,Sim.Tick,b,other));Frames.Add(new(result.Tick,a,buttons,b,other,result.Events.ToArray(),result.Hash));return result;
  }
  public void Wait(int ticks,byte defense=5){for(int t=0;t<ticks;t++)Step(defense:defense);}
  public void Motion(string pattern,Buttons button=Buttons.LP,byte defense=5)
  {Step(defense:defense);var patternInputs=Sim.Content.Inputs.MotionPatterns[pattern];for(int t=0;t<patternInputs.Length;t++)Step((byte)patternInputs[t],t==patternInputs.Length-1?button:Buttons.None,defense);}
  public void Until(Func<bool> condition,int maximum=200,byte defense=5)
  {for(int i=0;!condition()&&i<maximum;i++)Step(defense:defense);Check(condition(),name+" timed out at "+Sim.Players[0].ActionId+":"+Sim.Players[0].ActionFrame);}
  public void Verify()
  {
   var replay=new Simulation(Sim.Content,Sim.Config);replay.Restore(new(Convert.FromBase64String(initial)));
   foreach(var f in Frames){var r=replay.Step(new(0,replay.Tick,f.D0,f.B0),new(1,replay.Tick,f.D1,f.B1));Check(r.Hash==f.Hash&&r.Events.SequenceEqual(f.Events),name+" deterministic replay tick "+f.Tick);}
   Check(replay.Hash()==Sim.Hash(),name+" final replay hash");AuditRegressionTests.Traces.Add(new(name,Sim.Config,initial,Frames.ToArray(),Sim.Hash()));
  }
  public IEnumerable<CombatEvent> Events=>Frames.SelectMany(f=>f.Events);
 }
 static Simulation New(GameContent content,string item,bool mirror=false,int distance=35000,string art="art_1",int credits=3600)
 {
  var sim=new Simulation(content,new(){Training=true,Fighter0="rook",Fighter1="rook",Super0=art,SessionId="buyable-route-conformance"});sim.TrainingReset(credits);
  sim.SetTrainingState(0,x:mirror?16000+distance:752000-distance);sim.SetTrainingState(1,x:mirror?16000:752000);
  sim.Step(new(0,sim.Tick,5,Buttons.None),new(1,sim.Tick,5,Buttons.None));if(item.Length>0)sim.SetTrainingLoadout(0,new([item]));else {var move=content.Fighters["rook"].Move(content.Fighters["rook"].SuperArts.Single(a=>a.Id==art).MoveId);if(credits>=content.Items[move.Availability].Price)sim.SetTrainingLoadout(0,new([move.Availability]));}sim.SetTrainingLoadout(1,new([content.Fighters["rook"].Move("super_1").Availability]));return sim;
 }
 public static IEnumerable<(string Id,Action Test)> Cases(GameContent content)
 {
  if(!content.Items.ContainsKey("buy_r_s2_slipstream"))yield break;
  yield return ("buy_route_slipstream_fresh_edges",()=>
  {
   foreach(bool mirror in new[]{false,true})
   {
    var noBranch=new Trace("Slipstream no follow-up "+mirror,New(content,"buy_r_s2_slipstream",mirror,180000));noBranch.Motion("qcb");int origin=noBranch.Sim.Players[0].X;
    noBranch.Wait(40);Check(noBranch.Sim.Players[0].X-origin==42000*(mirror?-1:1),"exact 42-unit entry displacement");Check(noBranch.Events.Count(e=>e.Kind==CombatEventKind.ActionStarted)==1,"entry never automatically branches");Check(noBranch.Sim.Players[0].SpendReceipts.Count==0,"rental branches are activation-free");noBranch.Verify();
    foreach(var (button,id) in new[]{(Buttons.MP,"buy_r_s2_straight"),(Buttons.MK,"buy_r_s2_upper")})
    {
     var t=new Trace("Slipstream "+id+" "+mirror,New(content,"buy_r_s2_slipstream",mirror,70000));t.Motion("qcb");t.Until(()=>t.Sim.Players[0].ActionFrame==8);t.Step(buttons:button);
     Check(t.Sim.Players[0].ActionId==id,"fresh chosen branch starts at first accepted edge");t.Wait(110);Check(t.Events.Any(e=>e.Kind==CombatEventKind.Hit&&e.MoveId==id),"chosen follow-up produces its independent strike");t.Verify();
    }
    foreach(int edge in new[]{7,13})
    {
     var t=new Trace("Slipstream rejected edge "+edge+" "+mirror,New(content,"buy_r_s2_slipstream",mirror,180000));t.Motion("qcb");t.Until(()=>t.Sim.Players[0].ActionFrame==edge);t.Step(buttons:Buttons.MP);t.Wait(30);
     Check(!t.Events.Any(e=>e.MoveId=="buy_r_s2_straight"&&e.Kind==CombatEventKind.ActionStarted),"branch window is exactly half-open [8,13)");t.Verify();
    }
   }
  });
  if(content.Items.ContainsKey("buy_r_s3_chain1"))yield return ("buy_route_rivet_contact_chain",()=>
  {
   foreach(bool mirror in new[]{false,true})foreach(string defense in new[]{"hit","block","parry","whiff"})
   {
    var t=new Trace("Rivet "+defense+" "+mirror,New(content,"buy_r_s3_chain1",mirror,defense=="whiff"?220000:35000));byte guard=defense=="block"?(byte)4:(byte)5;
    t.Motion("qcb",defense:guard);t.Until(()=>t.Sim.Players[0].ActionFrame==10,defense:guard);t.Step(defense:defense=="parry"?(byte)6:guard);t.Until(()=>t.Sim.Players[0].Hitstop==0,defense:guard);t.Step(buttons:Buttons.MP,defense:guard);
    bool permitted=defense is "hit" or "block";Check((t.Sim.Players[0].ActionId=="buy_r_s3_chain2")==permitted,"second stage requires actual non-parried contact");
    if(permitted)
    {
     t.Until(()=>t.Sim.Players[0].Contact&&t.Sim.Players[0].Hitstop==0,defense:guard);t.Step(buttons:Buttons.HP,defense:guard);Check(t.Sim.Players[0].ActionId=="buy_r_s3_chain3","fresh third-stage contact branch");
     t.Wait(90,guard);Check(t.Events.Any(e=>e.MoveId=="buy_r_s3_chain3"&&e.Kind==(defense=="hit"?CombatEventKind.Hit:CombatEventKind.Block)),"final independent strike resolves");
    }
    else t.Wait(70);
    Check(t.Sim.Players[0].SpendReceipts.Count==0,"finite sequence never creates activation debit");t.Verify();
   }
  });
  if(content.Items.ContainsKey("buy_r_t3_rivet_lift"))yield return ("buy_route_lift_hit_only_jump",()=>
  {
   foreach(bool mirror in new[]{false,true})foreach(string defense in new[]{"hit","block","parry","whiff"})
   {
    var t=new Trace("Lift jump "+defense+" "+mirror,New(content,"buy_r_t3_rivet_lift",mirror,defense=="whiff"?220000:35000));byte guard=defense=="block"?(byte)4:(byte)5;
    t.Step(6,Buttons.HP,guard);t.Until(()=>t.Sim.Players[0].ActionFrame==12,defense:guard);t.Step(defense:defense=="parry"?(byte)6:guard);t.Until(()=>t.Sim.Players[0].Hitstop==0,defense:guard);
    int combo=t.Sim.Players[1].ComboCount,juggle=t.Sim.Players[1].JuggleBudget;t.Step(9,defense:guard);bool jumped=t.Events.Any(e=>e.Kind==CombatEventKind.ActionStarted&&e.MoveId=="jump_cancel");Check(jumped==(defense=="hit"),"jump-cancel requires hit, excluding block/parry/whiff");
    if(jumped)Check(t.Sim.Players[1].ComboCount==combo&&t.Sim.Players[1].JuggleBudget==juggle&&combo>0,"jump transition retains defender combo/juggle budget");t.Wait(50);t.Verify();
   }
  });
  if(content.Fighters["rook"].Moves.Any(m=>m.Install is not null))
  {
   yield return ("buy_route_overtime_wallet_timer",()=>
   {
    foreach(bool mirror in new[]{false,true})foreach(int credits in new[]{1499,1500,1501,3600})
    {
     var t=new Trace("Overtime threshold/timer "+credits+" "+mirror,New(content,"",mirror,220000,"art_3",credits));t.Motion("double_qcf");
     if(credits<1500){t.Wait(20);Check(t.Sim.Players[0].Credits==credits&&t.Sim.Players[0].InstallTicks==0&&!t.Events.Any(e=>e.Kind is CombatEventKind.Spend or CombatEventKind.ActionStarted),"insufficient install cannot start or fall back");}
     else
     {
      t.Until(()=>t.Sim.Players[0].InstallTicks>0);Check(t.Sim.Players[0].InstallTicks==240&&t.Sim.Players[0].Credits==credits,"install starts full timer after vulnerable activation and charges once");
      int remaining=t.Sim.Players[0].InstallTicks;t.Motion("double_qcf");Check(t.Sim.Players[0].SuperUseReceipts.Count==1&&t.Sim.Players[0].InstallTicks==remaining-7,"active reactivation rejects before debit without resetting timer");
      var checkpoint=t.Sim.Capture();var hash=t.Sim.Hash();t.Wait(50);t.Sim.Restore(checkpoint);Check(t.Sim.Hash()==hash,"active install snapshot reconstructs graph and timer");
      // The temporary replay above is separately checked; remove its non-monotonic trace frames before continuing.
      t.Frames.RemoveRange(t.Frames.Count-50,50);
      t.Until(()=>t.Sim.Players[0].InstallTicks==0,300);Check(t.Sim.Players[0].InstallId=="","expiry clears selected install identity");
      if(credits==3600){t.Motion("double_qcf");t.Wait(20);Check(t.Sim.Players[0].Credits==3600&&t.Sim.Players[0].SuperUseReceipts.Count==1&&t.Sim.Players[0].InstallTicks==0,"expired install cannot reacquire the spent single-use permit");}
     }
     t.Verify();
    }
   });
   yield return ("buy_route_overtime_all_eight_hit_edges",()=>
   {
    var install=content.Fighters["rook"].Moves.Single(m=>m.Install is not null).Install!;
    foreach(bool mirror in new[]{false,true})foreach(var edge in install.Edges)foreach(string defense in new[]{"hit","block","parry","whiff"})
    {
     bool approach=edge.From=="s_mp"&&defense!="whiff";
     var t=new Trace("Overtime "+edge.From+" -> "+edge.To+" "+defense+" "+mirror,New(content,"",mirror,defense=="whiff"?220000:approach?181000:32000,"art_3"));t.Motion("double_qcf");t.Until(()=>t.Sim.Players[0].InstallTicks>0);
     byte posture=edge.From.StartsWith("c_")?(byte)2:(byte)5,guard=defense=="block"?(byte)1:(byte)5;var source=content.Fighters["rook"].Move(edge.From);var from=Enum.Parse<Buttons>(edge.From[2..].ToUpperInvariant());var to=Enum.Parse<Buttons>(edge.To[2..].ToUpperInvariant());
     // Far standing MP needs an approaching opponent to connect without proximity substitution.
     // This uses real walking, with the first forward-parry arm expired before the strike.
     if(approach)t.Until(()=>Math.Abs(t.Sim.Players[0].X-t.Sim.Players[1].X)<=55000,100,6);
     t.Step(posture,from,approach?(byte)6:guard);Check(t.Sim.Players[0].ActionId==edge.From,"source is exact ground-normal graph node");
     if(approach){t.Until(()=>t.Sim.Players[0].ActionFrame==source.Startup-1,defense:6);t.Step();}
     else t.Until(()=>t.Sim.Players[0].ActionFrame==source.Startup,defense:guard);
     t.Step(defense:defense=="parry"?(source.Hitboxes[0].Level=="low"?(byte)2:(byte)6):guard);t.Until(()=>t.Sim.Players[0].Hitstop==0,defense:guard);t.Step(posture,to,guard);
     Check((t.Sim.Players[0].ActionId==edge.To)==(defense=="hit"),"graph edge requires actual hit "+edge.From+" "+defense);
     if(defense=="hit")Check(t.Events.Any(e=>e.Kind==CombatEventKind.Hit&&e.MoveId==edge.From),"source hit is observed, not fabricated contact state");t.Wait(50);t.Verify();
    }
   });
   yield return ("buy_route_overtime_freeze_and_interruptions",()=>
   {
    foreach(bool mirror in new[]{false,true})foreach(string mode in new[]{"global-freeze","plain-hit","knockdown","capture","dizzy","startup-interrupt"})
    {
     var t=new Trace("Overtime interruption "+mode+" "+mirror,New(content,"",mirror,32000,"art_3"));t.Motion("double_qcf");
     if(mode=="startup-interrupt")
     {
      t.Step(other:Buttons.LP);t.Until(()=>t.Sim.Players[0].Hitstun>0);t.Wait(30);Check(t.Sim.Players[0].InstallTicks==0&&t.Sim.Players[0].Credits==3600&&t.Sim.Players[0].SuperUseReceipts.Count==1,"vulnerable activation interruption sinks fee without granting install");t.Verify();continue;
     }
     t.Until(()=>t.Sim.Players[0].InstallTicks>0);
     if(mode=="dizzy")
     {
      t.Verify();t.Sim.SetTrainingState(0,stun:995);t=new Trace("Overtime dizzy threshold fixture "+mirror,t.Sim);
     }
     if(mode=="global-freeze")
     {
      var pattern=content.Inputs.MotionPatterns["double_qcf"];foreach(var (dir,index) in pattern.Select((d,i)=>(d,i)))t.Step(defense:(byte)dir,other:index==pattern.Length-1?Buttons.LP:Buttons.None);
      Check(t.Sim.FullFreeze>0,"opponent actual super produces global freeze");int remaining=t.Sim.Players[0].InstallTicks;t.Until(()=>t.Sim.FullFreeze==0);Check(t.Sim.Players[0].InstallTicks==remaining,"global freeze holds install time exactly");
     }
     else
     {
      t.Step(defense:mode=="knockdown"?(byte)2:(byte)5,other:mode=="capture"?Buttons.LP|Buttons.LK:mode=="knockdown"?Buttons.HK:Buttons.LP);
      t.Until(()=>mode switch{"plain-hit"=>t.Sim.Players[0].Hitstop>0,"knockdown"=>t.Sim.Players[0].KnockdownTicks>0,"capture"=>t.Events.Any(e=>e.Kind==CombatEventKind.Throw&&e.Detail=="capture"),_=>t.Sim.Players[0].DizzyTicks>0});
      if(mode=="plain-hit")
      {int remaining=t.Sim.Players[0].InstallTicks;Check(remaining>0&&t.Sim.Players[0].Hitstun>0,"plain hit retains active install");t.Step();Check(t.Sim.Players[0].InstallTicks==remaining-1,"actor hitstop consumes one unfrozen global install tick");}
      else Check(t.Sim.Players[0].InstallTicks==0&&t.Sim.Players[0].InstallId=="","authoritative "+mode+" removes install immediately");
     }
     t.Verify();
    }
   });
  }
  yield return ("buy_route_mimic_exact_commitment",()=>
  {
   foreach(string fighter in new[]{"rook","vale"})foreach(bool mirror in new[]{false,true})
   {
    string item=fighter=="rook"?"buy_r_g1_false_start":"buy_v_g1_false_pulse";var sim=new Simulation(content,new(){Training=true,Fighter0=fighter,Fighter1="rook"});sim.TrainingReset();sim.SetTrainingState(0,x:mirror?500000:250000);sim.SetTrainingState(1,x:mirror?250000:500000);sim.Step(new(0,sim.Tick,5,Buttons.None),new(1,sim.Tick,5,Buttons.None));sim.SetTrainingLoadout(0,new([item]));
    var t=new Trace("Mimic "+fighter+" "+mirror,sim);int origin=sim.Players[0].X;t.Step(4,Buttons.HP|Buttons.HK);var m=content.Fighters[fighter].Move(item);Check(m.Mimic is {SharedTicks:6,AbortTicks:6}&&sim.Players[0].ActionId==item,"six-tick source anticipation then six-tick abort is selected");
    t.Wait(11);Check(sim.Players[0].Actionable&&sim.Players[0].X-origin==(fighter=="rook"?3900*(mirror?-1:1):0),"exact 12-tick commitment and source motion only");Check(!t.Events.Any(e=>e.Kind is CombatEventKind.Hit or CombatEventKind.ProjectileSpawn or CombatEventKind.Spend),"no disguised attack, projectile or credit effect");t.Verify();
   }
  });
 }
}
