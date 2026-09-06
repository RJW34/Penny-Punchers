using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using StrikeLedger.App;
using StrikeLedger.Core;

/// <summary>Trusted, isolated experimental fixture runner. It cannot alter a shipped match or shipping data.</summary>
public static class BuyablesPilot
{
 sealed record Command(string Kind,long Tick,byte D0=5,Buttons B0=Buttons.None,byte D1=5,Buttons B1=Buttons.None,PreparationPlan? Plan0=null,PreparationPlan? Plan1=null,string Hash="",CombatEvent[]? Events=null);
 sealed record Trace(string Format,string ContentHash,string Build,MatchConfig Config,string InitialSnapshot,Command[] Commands,string FinalHash);
 public sealed record Sample(string Id,string Kind,int Seed,string Policy,string Item,int Seat,bool Rental,string Payout,string Gap,int OpeningSpend,int Steps,int Rounds,int ActorPoints,int OpponentPoints,double ActorResult,int RentalStarts,int RentalContacts,int PaidStarts,int PaidDenied,int Spend,int Damage,string InitialHash,string FinalHash,string TracePath,string TraceSha256,bool Reconstructed,int[] OpeningWallets,double OpeningRoundResult);
 static void Check(bool value,string reason){if(!value)throw new InvalidOperationException(reason);}
 static void Set(object obj,string property,int value)=>obj.GetType().GetProperty(property,BindingFlags.Public|BindingFlags.Instance)!.SetValue(obj,value);
 static string HashFile(string path)=>Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant();
 public static int Run(GameContent content,string data,string output,int seedStart,int seeds,string scope)
 {
  if(scope is not("all" or "rentals" or "opening"))throw new ArgumentException("Pilot scope must be all/rentals/opening");
  var watch=Stopwatch.StartNew();var samples=new List<Sample>();Directory.CreateDirectory(Path.Combine(output,"traces"));
  using var sampleLog=new StreamWriter(Path.Combine(output,"samples.jsonl")){AutoFlush=true};
  void Add(Sample sample){samples.Add(sample);sampleLog.WriteLine(JsonSerializer.Serialize(sample));Console.WriteLine($"{samples.Count}: {sample.Id} result={sample.ActorResult:F1} ticks={sample.Steps} rentalStarts={sample.RentalStarts} replay=verified");}
  if(scope is "all" or "rentals")
  foreach(var item in content.Items.Values.OrderBy(i=>i.Id,StringComparer.Ordinal))foreach(string policy in new[]{"pressure","spacing"})for(int seed=seedStart;seed<seedStart+seeds;seed++)for(int seat=0;seat<2;seat++)foreach(bool rental in new[]{false,true})
  {
   string id=$"rental-{item.Id}-{policy}-s{seed}-seat{seat}-{(rental?"lease":"save")}";string fighter=item.EligibleFighters[0];
   var sim=new Simulation(content,new(){SessionId=id,Fighter0=seat==0?fighter:"rook",Fighter1=seat==1?fighter:"rook"});foreach(var p in sim.Players)Set(p,nameof(PlayerState.Credits),1800);
   Add(Match(sim,id,"rental-round",seed,policy,item.Id,seat,rental,"A","equal-opening",0,output,true));
  }
  var variants=new Dictionary<string,GameContent>();var identities=new List<object>();
  if(scope is "all" or "opening")
  {
   foreach(var (name,schedule) in new[]{("A",new[]{900,1200,1500}),("B",new[]{1200,1200,1500}),("C",new[]{1200,1200,1200})})
   {
    var directory=Path.Combine(output,"isolated-rulesets",name);Directory.CreateDirectory(directory);
    foreach(var path in Directory.GetFiles(data,"*.json",SearchOption.AllDirectories).Where(p=>!Path.GetRelativePath(data,p).Split(Path.DirectorySeparatorChar).Contains("rulesets")))
    {var target=Path.Combine(directory,Path.GetRelativePath(data,path));Directory.CreateDirectory(Path.GetDirectoryName(target)!);File.Copy(path,target,true);}
    string economyPath=Path.Combine(directory,"economy.json");var economy=JsonNode.Parse(File.ReadAllText(economyPath))!;
    if(economy["loss_payouts"] is not JsonArray)throw new InvalidDataException("Cannot locate canonical loss_payouts");
    if(name!="A"){economy["loss_payouts"]=new JsonArray(schedule.Select(n=>JsonValue.Create(n)).ToArray());File.WriteAllText(economyPath,economy.ToJsonString(new JsonSerializerOptions{WriteIndented=true}));}
    variants[name]=GameContent.Load(directory);identities.Add(new{name,lossPayouts=schedule,contentHash=variants[name].ContentHash,files=Directory.GetFiles(directory,"*.json",SearchOption.AllDirectories).Select(p=>new{path=Path.GetRelativePath(directory,p).Replace('\\','/'),sha256=HashFile(p)}).ToArray()});
   }
   foreach(var payout in variants)foreach(int spend in new[]{0,300,600})foreach(string gap in new[]{"current","trailer-topup-300","leader-subtract-300"})for(int seed=seedStart;seed<seedStart+seeds;seed++)for(int seat=0;seat<2;seat++)
   {
    string id=$"opening-{payout.Key}-{spend}-{gap}-s{seed}-seat{seat}";var sim=new Simulation(payout.Value,new(){SessionId=id,Fighter0=seat==0?"rook":"vale",Fighter1=seat==0?"vale":"rook"});
    Set(sim,nameof(Simulation.RoundId),2);Set(sim,nameof(Simulation.CompletedRounds),1);Set(sim.Players[seat],nameof(PlayerState.ScoreHalfPoints),2);
    Set(sim.Players[1-seat],nameof(PlayerState.RecoveryTier),1);
    Set(sim.Players[seat],nameof(PlayerState.Credits),payout.Value.Economy.StartingCredits+payout.Value.Economy.WinPayout-spend-(gap=="leader-subtract-300"?300:0));Set(sim.Players[1-seat],nameof(PlayerState.Credits),payout.Value.Economy.StartingCredits+payout.Value.Economy.LossPayouts[0]-spend+(gap=="trailer-topup-300"?300:0));
    Add(Match(sim,id,"opening-full-match",seed,"pressure","",seat,false,payout.Key,gap,spend,output,false));
   }
  }
  var paired=samples.Where(s=>s.Kind=="rental-round").GroupBy(s=>(s.Item,s.Policy)).Select(group=>
  {
   var blocks=group.GroupBy(s=>s.Seed).Select(seed=>new{seed=seed.Key,delta=seed.Where(s=>s.Rental).Average(s=>s.ActorResult)-seed.Where(s=>!s.Rental).Average(s=>s.ActorResult)}).ToArray();
   return new{item=group.Key.Item,policy=group.Key.Policy,seedBlocks=blocks,pairedInterval=Interval(blocks.Select(b=>b.delta).ToArray()),actualRentalStarts=group.Where(s=>s.Rental).Sum(s=>s.RentalStarts),actualRentalContacts=group.Where(s=>s.Rental).Sum(s=>s.RentalContacts)};
  }).ToArray();
  var gapEffects=samples.Where(s=>s.Kind=="opening-full-match").GroupBy(s=>(s.Payout,s.OpeningSpend,s.Gap)).Select(group=>
  {
   var reference=samples.Where(s=>s.Kind=="opening-full-match"&&s.Payout==group.Key.Payout&&s.OpeningSpend==group.Key.OpeningSpend&&s.Gap=="current").ToArray();
   var blocks=group.GroupBy(s=>s.Seed).Select(s=>new{seed=s.Key,leaderResult=s.Average(x=>x.ActorResult),round2Result=s.Average(x=>x.OpeningRoundResult),delta=s.Average(x=>x.ActorResult)-reference.Where(x=>x.Seed==s.Key).Average(x=>x.ActorResult),round2Delta=s.Average(x=>x.OpeningRoundResult)-reference.Where(x=>x.Seed==s.Key).Average(x=>x.OpeningRoundResult)}).ToArray();
   return new{payout=group.Key.Payout,openingSpend=group.Key.OpeningSpend,gap=group.Key.Gap,seedBlocks=blocks,meanLeaderResult=group.Average(s=>s.ActorResult),pairedMatchDelta=Interval(blocks.Select(b=>b.delta).ToArray()),pairedRound2Delta=Interval(blocks.Select(b=>b.round2Delta).ToArray())};
  }).ToArray();
  File.WriteAllText(Path.Combine(output,"summary.json"),JsonSerializer.Serialize(new{format="penny-buyables-paired-pilot-v1",build=ReplayFormat.Build,contentHash=content.ContentHash,command=Environment.GetCommandLineArgs(),scope,seeds,seedStart,elapsedSeconds=watch.Elapsed.TotalSeconds,samples=samples.Count,completed=samples.Count,excluded=0,totalTicks=samples.Sum(s=>(long)s.Steps),allTracesReconstructed=samples.All(s=>s.Reconstructed),rentalAlternativesRun=samples.Where(s=>s.Rental).Select(s=>s.Item).Distinct().Count(),rentalAlternativesWithActualStart=samples.Where(s=>s.Rental&&s.RentalStarts>0).Select(s=>s.Item).Distinct().Count(),paired,gapEffects,isolatedPayoutVariants=identities,scoreOnlyBenchmark=new{context="first-to-five, leader1-0, independent fair future rounds without draws",exactNumerator=163,exactDenominator=256,leaderMatchWin=163.0/256,kind="analytical reference, not measured bot result"},limitations=new[]{"Every rental is paired with saving the identical opening fee; fixed opponent Thomas and selected art_1, neutral stage spawn. This is a pilot, not the fighter/art/stage factorial.","Rental comparisons run one actual timed/KO round. They cannot establish match-level purchase value. Opening-gap experiments do complete the match from an explicitly recorded round2 score1-0 fixture.","Mirrored runs are averaged inside seed blocks, not treated as independent trials. Two seeds produce extremely wide t intervals; they do not certify balance.","Agents use ordinary encoded commands. The pressure/spacing item policy changes available action decisions; it does not infer optimal counterplay or human recognition.","A failed-to-contact rental remains a measured zero-contact policy sample, not proof that its mechanic is ineffective. Check conformance tests separately.","Payout variants exist only in copied evidence data. The shipping payout schedule is unchanged A.","No human competitive balance, silhouette calibration, two-PC device latency, or psychological first-loss interpretation is claimed."}},new JsonSerializerOptions{WriteIndented=true}));
  return 0;
 }
 static object Interval(double[] blocks)
 {
  double mean=blocks.Average();if(blocks.Length<2)return new{mean,seedBlocks=blocks.Length,lower=(double?)null,upper=(double?)null,method="insufficient independent seed blocks"};
  // Each paired block delta is bounded [-1,1]. A distribution-free bound remains wide
  // when two observed seed deltas happen to be identical, unlike a collapsed t interval.
  double half=Math.Sqrt(2*Math.Log(40)/blocks.Length);
  return new{mean,seedBlocks=blocks.Length,lower=(double?)Math.Max(-1,mean-half),upper=(double?)Math.Min(1,mean+half),method="95% Hoeffding bound for independent seed-block deltas in [-1,1]; conditional model only, no multiple-comparison guarantee"};
 }
 static Sample Match(Simulation sim,string id,string kind,int seed,string policy,string item,int seat,bool rental,string payout,string gap,int openingSpend,string output,bool oneRound)
 {
  string initial=Convert.ToBase64String(sim.Capture().Bytes),initialHash=sim.Hash();var commands=new List<Command>();var agents=new[]{new Agent(0,seat==0?policy:"pressure",(uint)(seed*10+(seat==0?1:2)),seat==0&&rental?item:""),new Agent(1,seat==1?policy:"pressure",(uint)(seed*10+(seat==1?1:2)),seat==1&&rental?item:"")};
  int[] openingWallets=sim.Players.Select(p=>p.Credits).ToArray();int steps=0,rounds=0,starts=0,contacts=0,paid=0,denied=0,spend=0,damage=0;double result=0,openingRoundResult=0;
  while(sim.Phase!=MatchPhase.MatchOver&&steps<90000)
  {
   if(sim.Phase==MatchPhase.Preparation)
   {
    var p0=new PreparationPlan(seat==0&&rental?[item]:[]);var p1=new PreparationPlan(seat==1&&rental?[item]:[]);sim.CommitPreparation(p0,p1);sim.BeginFight();commands.Add(new("prepare-begin",sim.Tick,Plan0:p0,Plan1:p1,Hash:sim.Hash()));
   }
   if(sim.Phase==MatchPhase.PendingResult)
   {
    result=sim.PendingResult!.WinnerSeat<0?.5:sim.PendingResult.WinnerSeat==seat?1:0;if(rounds==0)openingRoundResult=result;
    var closingBeforePayout=sim.Players.Select(p=>p.Credits).ToArray();var settlement=sim.SettleRound(sim.Tick);rounds++;
    File.AppendAllText(Path.Combine(output,"round-economy.jsonl"),JsonSerializer.Serialize(new{match=id,round=sim.RoundId,score=sim.Players.Select(p=>p.ScoreHalfPoints).ToArray(),beforePayout=closingBeforePayout,settlement,afterPayout=sim.Players.Select(p=>p.Credits).ToArray(),usableInContinuingMatch=sim.Phase!=MatchPhase.MatchOver})+Environment.NewLine);
    commands.Add(new("settle",sim.Tick,Hash:sim.Hash()));if(oneRound||sim.Phase==MatchPhase.MatchOver)break;sim.NextRound();commands.Add(new("next-round",sim.Tick,Hash:sim.Hash()));continue;
   }
   var a=agents[0].Next(sim);var b=agents[1].Next(sim);var frame=sim.Step(a,b);steps++;commands.Add(new("step",a.Frame,a.Direction,a.Held,b.Direction,b.Held,Hash:steps%60==0?frame.Hash:"",Events:frame.Events.ToArray()));
   foreach(var e in frame.Events)
   {
    if(e.Kind==CombatEventKind.ActionStarted&&e.Seat==seat){if(e.MoveId==item||sim.Content.Fighters[sim.Players[seat].FighterId].Moves.Any(m=>m.Id==e.MoveId&&m.Availability==item))starts++;if(e.Value>0)paid++;}
    if(e.Kind==CombatEventKind.Rejected&&e.Seat==seat&&e.Value>0)denied++;
    if(e.Kind==CombatEventKind.Spend&&e.Seat==seat)spend+=e.Value;
    if(e.Kind is CombatEventKind.Hit or CombatEventKind.Throw&&e.Seat==seat){damage+=e.Value;if(e.MoveId==item||sim.Content.Fighters[sim.Players[seat].FighterId].Moves.Any(m=>m.Id==e.MoveId&&m.Availability==item))contacts++;}
   }
  }
  Check(oneRound?rounds==1:sim.Phase==MatchPhase.MatchOver,"bounded pilot did not complete "+id);
  int actor=sim.Players[seat].ScoreHalfPoints,opponent=sim.Players[1-seat].ScoreHalfPoints;if(!oneRound)result=actor==opponent?.5:actor>opponent?1:0;
  var trace=new Trace("penny-balance-trusted-fixture-command-trace-v1",sim.Content.ContentHash,ReplayFormat.Build,sim.Config,initial,commands.ToArray(),sim.Hash());
  var replay=new Simulation(sim.Content,sim.Config);replay.Restore(new(Convert.FromBase64String(initial)));
  foreach(var command in commands)
  {
   Check(replay.Tick==command.Tick,"pilot trace command tick");
   switch(command.Kind){case "prepare-begin":replay.CommitPreparation(command.Plan0!,command.Plan1!);replay.BeginFight();break;case "settle":replay.SettleRound(replay.Tick);break;case "next-round":replay.NextRound();break;case "step":var r=replay.Step(new(0,replay.Tick,command.D0,command.B0),new(1,replay.Tick,command.D1,command.B1));Check(r.Events.SequenceEqual(command.Events!),"pilot replay event divergence "+id);break;default:throw new InvalidDataException("Unknown fixture command");}
   if(command.Hash.Length>0)Check(replay.Hash()==command.Hash,"pilot checkpoint divergence "+id);
  }
  Check(replay.Hash()==trace.FinalHash,"pilot final state divergence "+id);string path=Path.Combine(output,"traces",id+".json.gz");using(var file=File.Create(path))using(var gzip=new GZipStream(file,CompressionLevel.SmallestSize))JsonSerializer.Serialize(gzip,trace);
  return new(id,kind,seed,policy,item,seat,rental,payout,gap,openingSpend,steps,rounds,actor,opponent,result,starts,contacts,paid,denied,spend,damage,initialHash,sim.Hash(),"traces/"+Path.GetFileName(path),HashFile(path),true,openingWallets,openingRoundResult);
 }
 sealed class Agent(int seat,string policy,uint seed,string item)
 {
  readonly BotController baseline=new(seat,BotMode.Adaptive,seed);readonly Queue<CommandInput> queue=[];long last=-1000;string branchAction="";
  public InputFrame Next(Simulation sim)
  {
   var p=sim.Players[seat];if(queue.Count>0){var input=queue.Dequeue();return new(seat,sim.Tick,input.Direction,input.Held);}
   if(p.ActionId.Length>0&&p.Hitstop==0)
   {
    var move=sim.Content.Fighters[p.FighterId].Move(p.ActionId);var branch=move.Branches.FirstOrDefault(b=>p.ActionFrame>=(b.RelativeToContact?p.FirstContactFrame:0)+b.Start&&p.ActionFrame<(b.RelativeToContact?p.FirstContactFrame:0)+b.End&&(b.Contact=="none"||p.Contact));
    if(branch is not null&&branchAction!=p.ActionId){branchAction=p.ActionId;return new(seat,sim.Tick,5,branch.Button=="K"?Buttons.MK:Buttons.MP);}
   }
   if(item.Length>0&&p.Actionable&&sim.Tick-last>=(policy=="pressure"?100:160))
   {
    var move=sim.Content.Fighters[p.FighterId].Move(item);last=sim.Tick;branchAction="";
    if(move.Command.StartsWith("air:")&&p.Grounded){queue.Enqueue(new(CoreMath.RelativeDirection(9,p.Facing),Buttons.None));for(int i=0;i<13;i++)queue.Enqueue(new(5,Buttons.None));}
    foreach(var input in CommandEncoder.Encode(sim.Content,move,p.Facing))queue.Enqueue(input);var next=queue.Dequeue();return new(seat,sim.Tick,next.Direction,next.Held);
   }
   return baseline.Next(sim);
  }
 }
}
