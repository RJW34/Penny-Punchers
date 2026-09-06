using StrikeLedger.Core;
using System.Text.Json;
using System.Text.Json.Nodes;

public static class AuditRegressionTests
{
 public static readonly List<object> Observations=[];
 public static readonly List<LegalTrace> Traces=[];
 public static string Item(GameContent content,string legacy)=>content.Items.ContainsKey(legacy)?legacy:content.Items.Values.Single(i=>i.LegacyItemId==legacy).Id;
 static void Check(bool value,string message){if(!value)throw new Exception(message);}
 static StepResult Step(Simulation s,byte d0=5,Buttons b0=Buttons.None,byte d1=5,Buttons b1=Buttons.None)=>s.Step(new(0,s.Tick,d0,b0),new(1,s.Tick,d1,b1));
 static Simulation New(GameContent c,string f="rook",string other="rook",bool mirror=false)
 {
  var s=new Simulation(c,new(){Fighter0=f,Fighter1=other,Training=true,SessionId="audit-regression"});s.TrainingReset(c.Economy.Cap);
  s.SetTrainingState(0,x:mirror?418000:350000);s.SetTrainingState(1,x:mirror?383000:385000);Step(s);foreach(int seat in new[]{0,1})s.SetTrainingLoadout(seat,new([c.Fighters[s.Players[seat].FighterId].Move("super_1").Availability]));return s;
 }
 static void Record(string id,Simulation s,IEnumerable<(byte D0,Buttons B0,byte D1,Buttons B1)> inputs)
 {
  string initial=Convert.ToBase64String(s.Capture().Bytes);var frames=new List<LegalFrame>();
  foreach(var i in inputs){var r=Step(s,i.D0,i.B0,i.D1,i.B1);frames.Add(new(r.Tick,i.D0,i.B0,i.D1,i.B1,r.Events.ToArray(),r.Hash));}
  var replay=new Simulation(s.Content,s.Config);replay.Restore(new(Convert.FromBase64String(initial)));
  foreach(var f in frames){var r=Step(replay,f.D0,f.B0,f.D1,f.B1);Check(r.Hash==f.Hash&&r.Events.SequenceEqual(f.Events),id+" exact legal trace reconstruction");}
  Check(s.Hash()==replay.Hash(),id+" final state reconstruction");Traces.Add(new(id,s.Config,initial,frames.ToArray(),s.Hash()));
 }
 public static IEnumerable<(string Id,Action Test)> Cases(GameContent content,string data)
 {
  yield return ("audit_mutual_throws_snapshot",()=>
  {
   foreach(string mode in new[]{"normal-normal","command-command","command-normal","normal-command","command-one-tick-late"})foreach(bool mirror in new[]{false,true})
   {
    var s=New(content,mirror:mirror);s.SetTrainingLoadout(0,new([Item(content,"rook_clinch")]));s.SetTrainingLoadout(1,new([Item(content,"rook_clinch")]));
    var input=new List<(byte,Buttons,byte,Buttons)>();
    (byte,Buttons) Seat(int seat,int tick)
    {
     bool command=mode=="command-command"||seat==0&&mode is "command-normal" or "command-one-tick-late"||seat==1&&mode=="normal-command";
     int late=mode=="command-one-tick-late"&&seat==0?1:0;
     if(command)return (tick-late) switch{1=>((byte)2,Buttons.None),2=>((byte)1,Buttons.None),3=>((byte)4,Buttons.LP),_=>((byte)5,Buttons.None)};
     int normalTick=3+content.Fighters["rook"].Move(content.Items[Item(content,"rook_clinch")].MoveId).Startup-content.Fighters["rook"].Move("throw_forward").Startup;
     return tick==normalTick?((byte)5,Buttons.LP|Buttons.LK):((byte)5,Buttons.None);
    }
    for(int t=0;t<35;t++){var a=Seat(0,t);var b=Seat(1,t);input.Add((CoreMath.RelativeDirection(a.Item1,mirror?-1:1),a.Item2,CoreMath.RelativeDirection(b.Item1,mirror?1:-1),b.Item2));}
    Record(mode+" mirror="+mirror,s,input);var events=Traces[^1].Frames.SelectMany(f=>f.Events).ToArray();
    int h0=s.Players[0].Health,h1=s.Players[1].Health;
    if(mode is "normal-normal" or "command-command")Check(h0==1000&&h1==1000&&events.Count(e=>e.Kind==CombatEventKind.ThrowTech)==1,"symmetric reciprocal throw separation "+mode);
    else if(mode=="command-one-tick-late")Check(h0<1000&&h1==1000,"earlier normal captures command startup without stale second throw");
    else Check(mode=="command-normal"?h0==1000&&h1==865:h1==1000&&h0==865,"same-frame command wins mixed throw without seat bias "+mode);
    if(mode=="command-command")Check(events.Any(e=>e.Kind==CombatEventKind.ThrowTech&&e.Detail=="command-clash"),"command clash identified separately from manual tech");
    Check(s.Players.All(p=>p.SpendReceipts.Count==0),"throws never create activation credit charges");
    Observations.Add(new{requirement="PP040",mode,mirror,health0=h0,health1=h1,events=events.Where(e=>e.Kind is CombatEventKind.Throw or CombatEventKind.ThrowTech).ToArray()});
   }
  });
  yield return ("audit_air_parry_landing",()=>
  {
   foreach(bool mirror in new[]{false,true})foreach(bool frozen in new[]{false,true})
   {
    var s=New(content,mirror:mirror);s.SetTrainingState(1,x:mirror?16000:752000);Step(s,8);
    int count=0;do{Step(s);Check(++count<100,"jump reaches descending landing boundary");}while(s.Players[0].Y==0||s.Players[0].Vy>=0||s.Players[0].Y>15000);
    if(frozen){Check(s.TryStartAction(1,"super_1")==ActivationStatus.SuperUse,"opponent freeze setup");}
    var fwd=(byte)(mirror?4:6);Record("air-landing "+mirror+" "+frozen,s,new[]{(fwd,Buttons.None,(byte)5,Buttons.None)}.Concat(Enumerable.Repeat(((byte)5,Buttons.None,(byte)5,Buttons.None),45)));
    Check(s.Players[0].Grounded&&s.Players[0].Parry==ParryKind.None&&s.Players[0].ParryTicks==0,"air/deferred air arm must expire at landing");
    Check(!Traces[^1].Frames.SelectMany(f=>f.Events).Any(e=>e.Kind==CombatEventKind.Parry),"fixture has no accidental grounded air parry");
    Observations.Add(new{requirement="PP041",mirror,frozen,landTick=Traces[^1].Frames.First(f=>f.Events.Any(e=>e.Kind==CombatEventKind.Land&&e.Seat==0)).Tick,finalParry=s.Players[0].Parry.ToString()});
   }
  });
  yield return ("audit_taunt_commitment",()=>
  {
   foreach(bool mirror in new[]{false,true})foreach(byte parryDirection in new byte[]{2,6})
   {
    var s=New(content,mirror:mirror);var start=Step(s,b1:Buttons.LP);Check(s.Players[1].ActionId=="s_lp","legal normal setup");Step(s);
    Record("taunt "+mirror+" "+parryDirection,s,new[]{(CoreMath.RelativeDirection(parryDirection,mirror?-1:1),Buttons.HP|Buttons.HK,(byte)5,Buttons.None)}.Concat(Enumerable.Repeat(((byte)5,Buttons.None,(byte)5,Buttons.None),8)));
    var events=Traces[^1].Frames.SelectMany(f=>f.Events).ToArray();Check(events.Any(e=>e.Kind==CombatEventKind.ActionStarted&&e.MoveId=="taunt"),"taunt starts on fresh chord");Check(events.Any(e=>e.Kind==CombatEventKind.Hit&&e.Target==0)&&!events.Any(e=>e.Kind==CombatEventKind.Parry),"taunt must relinquish same-tick low/high parry");
   }
   var freeze=New(content);freeze.SetTrainingState(1,x:752000);freeze.TryStartAction(1,"super_1");Step(freeze,6,Buttons.HP|Buttons.HK);Check(freeze.Players[0].DashTicks==0,"taunt cannot mutate commitment during full freeze");
   var jump=New(content);Step(jump,6);Step(jump,8);Check(jump.Players[0].JumpStart>0&&jump.Players[0].Parry==ParryKind.None,"prejump commitment clears an earlier grounded parry arm");
  });
  yield return ("audit_training_branch_isolation",()=>
  {
   var original=new Simulation(content,new(){SessionId="competitive-practice-source"});original.BeginFight();Step(original,b0:Buttons.HP);var hash=original.Hash();var branch=original.CreateTrainingBranch();
   Check(branch.Config.Training&&!original.Config.Training&&branch.Tick==original.Tick&&branch.Players[0].ActionId==original.Players[0].ActionId,"practice branch preserves world and makes explicit training config");
   branch.SetTrainingState(0,credits:0,health:100);Step(branch);Check(original.Hash()==hash,"practice mutations cannot touch competitive source");bool rejected=false;try{original.Restore(branch.Capture());}catch(InvalidDataException){rejected=true;}Check(rejected&&original.Hash()==hash,"training snapshot cannot enter competitive source");
   var restored=new Simulation(content,branch.Config);restored.Restore(branch.Capture());Check(restored.Hash()==branch.Hash(),"practice checkpoint roundtrip");
  });
  yield return ("audit_facing_and_guard_posture",()=>
  {
   foreach(bool mirror in new[]{false,true})
   {
    var s=New(content,mirror:mirror);int initial=s.Players[0].Facing;Step(s,8);while(s.Players[0].Y==0)Step(s);s.SetTrainingState(1,x:mirror?650000:100000);Step(s);Check(s.Players[0].Facing==initial,"neutral airborne facing remains committed");
    for(int i=0;i<100&&!s.Players[0].Actionable;i++)Step(s);for(int i=0;i<100&&!s.Players[0].Grounded;i++)Step(s);for(int i=0;i<8;i++)Step(s);Check(s.Players[0].Facing==-initial,"ground neutral adopts new facing epoch after landing recovery");
    var guard=New(content,mirror:mirror);guard.SetTrainingState(0,x:mirror?51000:717000);guard.SetTrainingState(1,x:mirror?16000:752000);byte back=mirror?(byte)4:(byte)6,downback=mirror?(byte)1:(byte)3;
    Step(guard,b0:Buttons.LP,d1:back);for(int i=0;i<20&&guard.Players[1].Blockstun==0;i++)Step(guard,d1:back);Check(guard.Players[1].Blockstun>0,"legal blocked normal fixture");int x=guard.Players[1].X;
    Step(guard,d1:downback);Check(guard.Players[1].Crouching&&guard.Players[1].X==x,"crouch guard geometry switches in blockstop without walking");int crouchTop=guard.Hurtboxes(1).Max(b=>b.Top);Step(guard,d1:back);Check(!guard.Players[1].Crouching&&guard.Hurtboxes(1).Max(b=>b.Top)>crouchTop,"standing guard and body geometry agree during blockstun");
   }
  });
  yield return ("audit_contact_event_coordinates",()=>
  {
   foreach(bool mirror in new[]{false,true})
   {
    var s=New(content,mirror:mirror);Record("contact-coordinate "+mirror,s,new[]{((byte)5,Buttons.HP,(byte)5,Buttons.None)}.Concat(Enumerable.Repeat(((byte)5,Buttons.None,(byte)5,Buttons.None),50)));
    var hit=Traces[^1].Frames.SelectMany(f=>f.Events).First(e=>e.Kind==CombatEventKind.Hit);Check(hit.WorldX.HasValue&&hit.WorldY>0&&hit.Facing==(mirror?-1:1)&&hit.ActionOrdinal>0,"contact metadata captures source and feet-relative world height");
    Check(hit.WorldX!=s.Players[1].X,"contact point is not later defender root");Observations.Add(new{requirement="PP033",mirror,hit});
   }
  });
  yield return ("audit_authored_recovery_hurtboxes",()=>
  {
   foreach(bool mirror in new[]{false,true})
   {
    var s=New(content,"rook","vale",mirror);s.SetTrainingState(0,x:mirror?418000:350000);s.SetTrainingState(1,x:mirror?353000:415000);
    var inputs=Enumerable.Range(0,45).Select(t=>((byte)5,t==0?Buttons.HK:Buttons.None,(byte)5,t==7?Buttons.HP:Buttons.None)).ToArray();
    Record("recovery-limb "+mirror,s,inputs);var events=Traces[^1].Frames.SelectMany(f=>f.Events).ToArray();
    Check(!events.Any(e=>e.Kind==CombatEventKind.Hit&&e.Seat==0)&&events.Any(e=>e.Kind==CombatEventKind.Hit&&e.Seat==1),"spaced whiff is punished through extended recovery limb");
    WithData(data,dir=>
    {
     var path=Path.Combine(dir,"fighters","rook.json");var j=JsonNode.Parse(File.ReadAllText(path))!;var move=j["moves"]!.AsArray().First(m=>m!["id"]!.GetValue<string>()=="s_hk")!;
     var windows=move["hurtbox_windows"]!.AsArray();foreach(var w in windows.Where(w=>w!["start"]!.GetValue<int>()>=12).ToArray())windows.Remove(w);File.WriteAllText(path,j.ToJsonString());
     var control=New(GameContent.Load(dir),"rook","vale",mirror);control.SetTrainingState(0,x:mirror?418000:350000);control.SetTrainingState(1,x:mirror?353000:415000);
     foreach(var i in inputs)Step(control,i.Item1,i.Item2,i.Item3,i.Item4);Check(control.Players[0].Health==1000,"isolated no-recovery-extension control whiffs at same legal input spacing");
    });
    var frozen=New(content);frozen.SetTrainingState(1,x:752000);frozen.TryStartAction(0,"s_hk");for(int i=0;i<13;i++)Step(frozen);frozen.SetTrainingState(1,x:752000);frozen.TryStartAction(1,"super_1");int clock=frozen.Players[0].ActionFrame;var boxes=frozen.Hurtboxes(0).ToArray();for(int i=0;i<5;i++)Step(frozen);Check(frozen.Players[0].ActionFrame==clock&&boxes.SequenceEqual(frozen.Hurtboxes(0)),"authored vulnerability phase stays on frozen action clock");
   }
   WithData(data,dir=>{var path=Path.Combine(dir,"fighters","rook.json");var j=JsonNode.Parse(File.ReadAllText(path))!;var windows=j["moves"]![0]!["hurtbox_windows"]!.AsArray();windows.Add(windows[0]!.DeepClone());File.WriteAllText(path,j.ToJsonString());bool rejected=false;try{GameContent.Load(dir);}catch(InvalidDataException){rejected=true;}Check(rejected,"ambiguous overlapping phase data is rejected");});
  });
  yield return ("audit_rental_niches_and_counters",()=>
  {
   foreach(bool mirror in new[]{false,true})
   {
    int[] health=new int[2];for(int rental=0;rental<2;rental++)
    {
     var s=New(content,"vale","rook",mirror);s.SetTrainingState(1,x:mirror?368000:400000);if(rental==1)s.SetTrainingLoadout(0,new(["vale_long_check"]));
     var fwd=CoreMath.RelativeDirection(6,mirror?-1:1);Record("long-check-control "+mirror+" "+rental,s,Enumerable.Range(0,55).Select(t=>(t==0?fwd:(byte)5,t==0?Buttons.HP:Buttons.None,(byte)5,t==0?Buttons.HK:Buttons.None)));
     health[rental]=s.Players[0].Health;
    }
    Check(health[0]<1000&&health[1]==1000,"Long Check has a demonstrated high-poke niche over replaced standing normal");
    foreach(string fighter in new[]{"rook","vale"})foreach(string move in new[]{"shop_step_feint","shop_sway_feint"})foreach(bool low in new[]{false,true})
    {
     var s=New(content,fighter,"rook",mirror);s.SetTrainingState(1,x:low?(mirror?383000:385000):(mirror?368000:400000));s.SetTrainingLoadout(0,new([fighter+move[4..]]));
     // Opponent commits first; the feint is timed around the known active window, with no reaction claim.
     byte back=CoreMath.RelativeDirection(4,mirror?-1:1);int opponentStart=low?7:0;
     Record(move+" "+fighter+" "+mirror+" low="+low,s,Enumerable.Range(0,40).Select(t=>(t==6?back:(byte)5,t==6?Buttons.HP|Buttons.HK:Buttons.None,t==opponentStart&&low?(byte)2:(byte)5,t==opponentStart?(low?Buttons.MK:Buttons.HK):Buttons.None)));
     Check(s.Players[0].Health==(low?s.Players[0].Health:1000)&&(!low||s.Players[0].Health<1000),"feint high-poke evasion and grounded low counter "+fighter+" "+move+" "+low);
     Check(s.Players[1].Health==1000&&s.Players[0].SpendReceipts.Count==0,"feint has no hit or activation charge");
     Observations.Add(new{requirement="PP014/PP016/PP079",fighter,move,mirror,low,health=s.Players[0].Health});
    }
   }
  });
  yield return ("audit_canonical_purchase_and_registry",()=>
  {
   WithData(data,dir=>
   {
    var economy=JsonNode.Parse(File.ReadAllText(Path.Combine(dir,"economy.json")))!;economy["wallet_cap"]=6000;economy["loadout_cap"]=2400;File.WriteAllText(Path.Combine(dir,"economy.json"),economy.ToJsonString());
    var items=JsonNode.Parse(File.ReadAllText(Path.Combine(dir,"items.json")))!;items["items"]!.AsArray().First(i=>i!["id"]!.GetValue<string>()==Item(content,"rook_clinch"))!["price"]=1900;File.WriteAllText(Path.Combine(dir,"items.json"),items.ToJsonString());
    var third=JsonNode.Parse(File.ReadAllText(Path.Combine(dir,"fighters","rook.json")))!;third["id"]="fixture_third";third["display_name"]="Fixture third";third["health"]=1250;
    foreach(var m in third["moves"]!.AsArray().Where(m=>m!["availability"]!.GetValue<string>()!="base"&&m["kind"]!.GetValue<string>() is not("super" or "ex_special")).ToArray())third["moves"]!.AsArray().Remove(m);
    foreach(var item in items["items"]!.AsArray().Where(i=>i!["slot"]!.GetValue<string>() is "ex" or "super"&&i["eligible_fighters"]!.AsArray().Any(f=>f!.GetValue<string>()=="rook")))item!["eligible_fighters"]!.AsArray().Add("fixture_third");File.WriteAllText(Path.Combine(dir,"items.json"),items.ToJsonString());
    File.WriteAllText(Path.Combine(dir,"fighters","fixture_third.json"),third.ToJsonString());
    var stage=JsonNode.Parse(File.ReadAllText(Directory.GetFiles(Path.Combine(dir,"stages"),"*.json")[0]))!;stage["id"]="fixture_stage";stage["right"]=900000;File.WriteAllText(Path.Combine(dir,"stages","fixture_stage.json"),stage.ToJsonString());
    var c=GameContent.Load(dir);Check(c.Fighters.Count==3&&c.Stages.Count==content.Stages.Count+1&&c.Fighters["fixture_third"].Health==1250,"third registry fixture without required production IDs/counts");
    var s=new Simulation(c,new(){Training=true,Fighter0="rook",Fighter1="fixture_third",StageId="fixture_stage"});s.SetTrainingState(0,credits:6000);
    var plan=new PreparationPlan([Item(c,"rook_clinch"),Item(c,"rook_step_feint")]);var q=c.QuotePreparation("rook",6000,plan);Check(q.Valid&&q.TotalCost==2200&&q.Remaining==3800,"item-specific price and canonical cap drive quote");var receipt=s.CommitPreparation(plan,new([]));Check(receipt.Cost0==q.TotalCost&&receipt.Credits0==q.Remaining,"preview matches atomic debit");s.BeginFight();Check(s.Players[1].MaxHealth==1250,"registry health enters simulation");
    var bad=c.QuotePreparation("rook",1000,plan);Check(!bad.Valid,"unaffordable quote rejected");Observations.Add(new{requirement="PP021/PP066",fixtureHash=c.ContentHash,quote=q,receipt,thirdHealth=s.Players[1].MaxHealth});
   });
   WithData(data,dir=>{var path=Path.Combine(dir,"items.json");var j=JsonNode.Parse(File.ReadAllText(path))!;j["items"]![0]!["alters_system_defense"]=true;File.WriteAllText(path,j.ToJsonString());bool rejected=false;try{GameContent.Load(dir);}catch(InvalidDataException){rejected=true;}Check(rejected,"passive system-defense rental rejected by runtime loader");});
  });
 }
 public static void WithData(string source,Action<string> action)
 {
  var dir=Path.Combine(Path.GetTempPath(),"penny-audit-fixture-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);
  try{foreach(var file in Directory.GetFiles(source,"*.json",SearchOption.AllDirectories)){var target=Path.Combine(dir,Path.GetRelativePath(source,file));Directory.CreateDirectory(Path.GetDirectoryName(target)!);File.Copy(file,target);}action(dir);}finally{Directory.Delete(dir,true);}
 }
}
public sealed record LegalFrame(long Tick,byte D0,Buttons B0,byte D1,Buttons B1,CombatEvent[] Events,string Hash);
public sealed record LegalTrace(string Id,MatchConfig Config,string InitialSnapshotBase64,LegalFrame[] Frames,string FinalHash);

