using StrikeLedger.Core;

public static class PaidOptionTests
{
 static void Check(bool value,string reason){if(!value)throw new Exception(reason);}
 static StepResult Step(Simulation s,byte d0=5,Buttons b0=Buttons.None,byte d1=5,Buttons b1=Buttons.None)=>s.Step(new(0,s.Tick,d0,b0),new(1,s.Tick,d1,b1));
 static IEnumerable<(byte,Buttons,byte,Buttons)> Motion(Simulation s,string pattern)
 {
  yield return (5,Buttons.None,5,Buttons.None);var directions=s.Content.Inputs.MotionPatterns[pattern];
  for(int i=0;i<directions.Length;i++)yield return (CoreMath.RelativeDirection((byte)directions[i],s.Players[0].Facing),i==directions.Length-1?Buttons.LP:Buttons.None,5,Buttons.None);
 }
 static void Trace(string id,Simulation s,IEnumerable<(byte D0,Buttons B0,byte D1,Buttons B1)> inputs)
 {
  string initial=Convert.ToBase64String(s.Capture().Bytes);var frames=new List<LegalFrame>();foreach(var input in inputs){var r=Step(s,input.D0,input.B0,input.D1,input.B1);frames.Add(new(r.Tick,input.D0,input.B0,input.D1,input.B1,r.Events.ToArray(),r.Hash));}
  var replay=new Simulation(s.Content,s.Config);replay.Restore(new(Convert.FromBase64String(initial)));foreach(var f in frames){var r=Step(replay,f.D0,f.B0,f.D1,f.B1);Check(r.Hash==f.Hash&&r.Events.SequenceEqual(f.Events),id+" reconstructed canonical frames/events");}
  AuditRegressionTests.Traces.Add(new(id,s.Config,initial,frames.ToArray(),s.Hash()));
 }
 static Simulation New(GameContent c,string fighter,string art,int credits,bool mirror,bool distant=false,bool owned=true)
 {
  var s=new Simulation(c,new(){Training=true,Fighter0=fighter,Fighter1="rook",Super0=art,SessionId="paid-option-audit"});s.TrainingReset(credits);
  s.SetTrainingState(0,x:distant?(mirror?752000:16000):(mirror?51000:717000));s.SetTrainingState(1,x:mirror?16000:752000);Step(s);if(owned)s.SetTrainingLoadout(0,new([c.Fighters[fighter].Move(c.Fighters[fighter].SuperArts.Single(a=>a.Id==art).MoveId).Availability]));return s;
 }
 public static IEnumerable<(string Id,Action Test)> Cases(GameContent content)
 {
  yield return (content.Items.Values.Any(i=>i.CatalogId.Length>0)?"buy_catalog_conventional_art_cost_defense_repeat":"audit_six_super_cost_defense_repeat",()=>
  {
   foreach(var fighter in content.Fighters.Values)foreach(var art in fighter.SuperArts.Where(a=>fighter.Move(a.MoveId).Install is null&&fighter.Move(a.MoveId).ObjectRules is null))foreach(bool mirror in new[]{false,true})
   {
    foreach(int delta in new[]{-1,0,1})
    {
     int price=content.Items[fighter.Move(art.MoveId).Availability].Price;var q=content.QuotePreparation(fighter.Id,price+delta,new([fighter.Move(art.MoveId).Availability]));Check(q.Valid==(delta>=0),"exact shop permit threshold");var s=New(content,fighter.Id,art.Id,price+delta,mirror,true,delta>=0);Trace(fighter.Id+" "+art.Id+" threshold "+delta+" "+mirror,s,Motion(s,"double_qcf").Concat(Enumerable.Repeat(((byte)5,Buttons.None,(byte)5,Buttons.None),200)));
     var events=AuditRegressionTests.Traces[^1].Frames.SelectMany(f=>f.Events).ToArray();Check(s.Players[0].SuperUseReceipts.Count==(delta<0?0:1)&&s.Players[0].Credits==price+delta,"exact permit shop threshold and frozen fight bank");
     Check(events.Any(e=>e.Kind==CombatEventKind.ActionStarted&&e.MoveId==art.MoveId)==(delta>=0),"threshold action start");if(delta<0)Check(!events.Any(e=>e.Kind==CombatEventKind.ActionStarted||e.Kind==CombatEventKind.Spend),"unowned super has no normal fallback");
    }
    foreach(string defense in new[]{"hit","block","parry","whiff"})
    {
     var s=New(content,fighter.Id,art.Id,content.Economy.Cap,mirror,defense=="whiff");var move=fighter.Move(art.MoveId);bool armed=false;
     IEnumerable<(byte,Buttons,byte,Buttons)> Inputs()
     {
      foreach(var i in Motion(s,"double_qcf"))yield return i;
      for(int t=0;t<220;t++)
      {
       byte direction=defense=="block"?CoreMath.RelativeDirection(4,s.Players[1].Facing):(byte)5;
       if(defense=="parry"&&!armed&&s.FullFreeze==0&&s.Players[0].ActionId==move.Id&&s.Players[0].ActionFrame==move.Startup){direction=CoreMath.RelativeDirection(6,s.Players[1].Facing);armed=true;}
       yield return (5,Buttons.None,direction,Buttons.None);
      }
     }
     Trace(fighter.Id+" "+art.Id+" defense="+defense+" "+mirror,s,Inputs());var events=AuditRegressionTests.Traces[^1].Frames.SelectMany(f=>f.Events).ToArray();
     Check(s.Players[0].Credits==content.Economy.Cap&&s.Players[0].SuperUseReceipts.Count==1,"defense and whiff consume the prepaid use without bank writes");
     if(defense=="hit")Check(events.Any(e=>e.Kind==CombatEventKind.Hit&&e.Seat==0),"selected art hits");
     if(defense=="block")Check(events.Any(e=>e.Kind==CombatEventKind.Block&&e.Seat==1),"selected art is blockable");
     if(defense=="parry")Check(events.Any(e=>e.Kind==CombatEventKind.Parry&&e.Seat==1),"selected art first group is parryable; remaining groups need new edges");
     if(defense=="whiff")Check(!events.Any(e=>e.Kind is CombatEventKind.Hit or CombatEventKind.Block or CombatEventKind.Parry),"distant art whiff fixture");
     AuditRegressionTests.Observations.Add(new{requirement="PP081",fighter=fighter.Id,art=art.Id,shopPrice=content.Items[fighter.Move(art.MoveId).Availability].Price,activationPrice=0,usesRemaining=s.Players[0].SuperUsesRemaining,defense,mirror,actualDamage=1000-s.Players[1].Health,hitEvents=events.Count(e=>e.Kind==CombatEventKind.Hit),parryEvents=events.Count(e=>e.Kind==CombatEventKind.Parry),closingCredits=s.Players[0].Credits});
    }
    var repeated=New(content,fighter.Id,art.Id,content.Economy.Cap,mirror,true);int capacity=1;
    IEnumerable<(byte,Buttons,byte,Buttons)> Repeated(){for(int n=0;n<capacity+1;n++){foreach(var i in Motion(repeated,"double_qcf"))yield return i;for(int t=0;t<220;t++)yield return (5,Buttons.None,5,Buttons.None);}}
    Trace(fighter.Id+" "+art.Id+" repeated "+mirror,repeated,Repeated());Check(repeated.Players[0].SuperUseReceipts.Count==capacity&&repeated.Players[0].Credits==content.Economy.Cap,"one actual art use then exhausted intent");
   }
  });
  yield return ("audit_returning_pulse_ownership",()=>
  {
   foreach(bool mirror in new[]{false,true})foreach(string defense in new[]{"hit","block","parry","whiff"})
   {
    var s=New(content,"vale","art_1",content.Economy.Cap,mirror,defense=="whiff");s.SetTrainingLoadout(0,new([AuditRegressionTests.Item(content,"vale_return_pulse")]));bool armed=false;bool turned=false;int maxOwned=0;
    IEnumerable<(byte,Buttons,byte,Buttons)> Inputs()
    {
     foreach(var i in Motion(s,"qcf"))yield return i;
     for(int t=0;t<170;t++)
     {
      byte direction=defense=="block"?CoreMath.RelativeDirection(4,s.Players[1].Facing):(byte)5;
      if(defense=="parry"&&!armed&&s.Players[0].ActionFrame==22){direction=CoreMath.RelativeDirection(6,s.Players[1].Facing);armed=true;}
      turned|=s.Projectiles.Any(p=>p.Owner==0&&p.Facing!=s.Players[0].ActionFacing);maxOwned=Math.Max(maxOwned,s.Projectiles.Count(p=>p.Owner==0));yield return (5,Buttons.None,direction,Buttons.None);
     }
    }
    Trace("returning-pulse "+defense+" "+mirror,s,Inputs());var events=AuditRegressionTests.Traces[^1].Frames.SelectMany(f=>f.Events).ToArray();var contacts=events.Where(e=>e.MoveId==content.Items[AuditRegressionTests.Item(content,"vale_return_pulse")].MoveId&&e.Kind is CombatEventKind.Hit or CombatEventKind.Block or CombatEventKind.Parry).ToArray();
    Check(maxOwned<=1&&events.Count(e=>e.Kind==CombatEventKind.ProjectileSpawn)==1&&s.Projectiles.Count==0&&s.Players[0].SpendReceipts.Count==0,"leased return shot is one owned projectile with no activation debit");
    Check(defense=="whiff"?turned&&contacts.Length==0:contacts.Length==1,"one contact consumes return shot; whiff actually reverses");
    if(defense=="parry")Check(contacts[0].Kind==CombatEventKind.Parry&&!turned,"parry consumes without reflection");
    AuditRegressionTests.Observations.Add(new{requirement="PP080",defense,mirror,turned,contacts=contacts.Length,maxOwned});
   }
   foreach(string denied in new[]{content.Items[AuditRegressionTests.Item(content,"vale_return_pulse")].MoveId,"pulse_l","pulse_ex","super_2"})
   {
    var s=New(content,"vale","art_2",content.Economy.Cap,false,true);var ids=new List<string>{AuditRegressionTests.Item(content,"vale_return_pulse")};var deniedMove=content.Fighters["vale"].Move(denied);if(deniedMove.Availability!="base"&&!ids.Contains(deniedMove.Availability))ids.Add(deniedMove.Availability);s.SetTrainingLoadout(0,new(ids.ToArray()));foreach(var i in Motion(s,"qcf"))Step(s,i.Item1,i.Item2,i.Item3,i.Item4);for(int t=0;t<49;t++)Step(s);
    Check(s.Players[0].Actionable&&s.Projectiles.Count==1,"neutral caster with owned return shot");int credits=s.Players[0].Credits;Check(s.TryStartAction(0,denied)==ActivationStatus.Illegal&&s.Players[0].Credits==credits&&s.Players[0].SpendReceipts.Count==0,"shared owner slot rejects "+denied+" without debit or freeze");
   }
  });
 }
}
