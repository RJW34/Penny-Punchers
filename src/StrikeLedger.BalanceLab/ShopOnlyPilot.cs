using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using StrikeLedger.App;
using StrikeLedger.Core;

/// <summary>Trusted, isolated experimental fixture runner. It cannot alter a shipped match or shipping data.</summary>
public static class ShopOnlyPilot
{
 sealed record Command(string Kind,long Tick,byte D0=5,Buttons B0=Buttons.None,byte D1=5,Buttons B1=Buttons.None,PreparationPlan? Plan0=null,PreparationPlan? Plan1=null,string Hash="",CombatEvent[]? Events=null);
 sealed record Trace(string Format,string ContentHash,string Build,MatchConfig Config,string InitialSnapshot,Command[] Commands,string FinalHash);
 public sealed record Sample(string Id,string Kind,int Seed,string Policy,string Item,int Seat,bool Rental,string Payout,string Gap,int OpeningSpend,int Steps,int Rounds,int ActorPoints,int OpponentPoints,double ActorResult,int RentalStarts,int RentalContacts,int PaidStarts,int PaidDenied,int Spend,int Damage,string InitialHash,string FinalHash,string TracePath,string TraceSha256,bool Reconstructed,int[] OpeningWallets,double OpeningRoundResult){public Dictionary<string,int> ProductStarts {get;init;}=[];public int[] SkillEarned {get;init;}=[];public int[] SkillGranted {get;init;}=[];public int[] SkillClipped {get;init;}=[];public int[] SuperUsesConsumed {get;init;}=[];}
 static void Check(bool value,string reason){if(!value)throw new InvalidOperationException(reason);}
 static void Set(object obj,string property,int value)=>obj.GetType().GetProperty(property,BindingFlags.Public|BindingFlags.Instance)!.SetValue(obj,value);
 static string HashFile(string path)=>Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant();
 public static int Run(GameContent content,string data,string output,int seedStart,int seeds,string scope)
 {
  if(scope is not("all" or "rentals" or "opening" or "smoke"))throw new ArgumentException("Pilot scope must be all/rentals/opening");
  var watch=Stopwatch.StartNew();var samples=new List<Sample>();Directory.CreateDirectory(Path.Combine(output,"traces"));
  using var sampleLog=new StreamWriter(Path.Combine(output,"samples.jsonl")){AutoFlush=true};
  void Add(Sample sample){samples.Add(sample);sampleLog.WriteLine(JsonSerializer.Serialize(sample));Console.WriteLine($"{samples.Count}: {sample.Id} result={sample.ActorResult:F1} ticks={sample.Steps} rentalStarts={sample.RentalStarts} replay=verified");}
  if(scope is "all" or "rentals" or "smoke")
  foreach(var item in content.Items.Values.OrderBy(i=>i.Id,StringComparer.Ordinal).Where((_,index)=>scope!="smoke"||index==0))foreach(string policy in new[]{"pressure","spacing"})for(int seed=seedStart;seed<seedStart+seeds;seed++)for(int seat=0;seat<2;seat++)foreach(bool rental in new[]{false,true})
  {
   string id=$"rental-{item.Id}-{policy}-s{seed}-seat{seat}-{(rental?"lease":"save")}";string fighter=item.EligibleFighters[0];
   var sim=new Simulation(content,new(){SessionId=id,Fighter0=seat==0?fighter:"rook",Fighter1=seat==1?fighter:"rook"});foreach(var p in sim.Players)Set(p,nameof(PlayerState.Credits),2400);
   Add(Match(sim,id,"rental-round",seed,policy,item.Id,seat,rental,"C","equal-opening",0,output,true));
  }
  if(scope is "all" or "rentals")foreach(var fighter in content.Fighters.Values)
  {
   var ex=content.Items.Values.Where(i=>i.Slot=="ex"&&i.EligibleFighters.Contains(fighter.Id)).OrderBy(i=>i.Id,StringComparer.Ordinal).ToArray();
   for(int a=0;a<ex.Length;a++)for(int b=a+1;b<ex.Length;b++)foreach(string policy in new[]{"pressure","spacing"})for(int seed=seedStart;seed<seedStart+seeds;seed++)for(int seat=0;seat<2;seat++)foreach(bool buy in new[]{false,true})
   {
    string item=ex[a].Id+"|"+ex[b].Id,id=$"ex-pair-{ex[a].Id}-{ex[b].Id}-{policy}-s{seed}-seat{seat}-{buy}";var sim=new Simulation(content,new(){SessionId=id,Fighter0=seat==0?fighter.Id:"rook",Fighter1=seat==1?fighter.Id:"rook"});foreach(var player in sim.Players)Set(player,nameof(PlayerState.Credits),2400);
    Add(Match(sim,id,"rental-round",seed,policy,item,seat,buy,"C","equal-opening",0,output,true));
   }
  }
  var variants=new Dictionary<string,GameContent>();var identities=new List<object>();
  if(scope is "all" or "opening")
  {
   foreach(var (name,schedule) in new[]{("A",new[]{900,1200,1500}),("B",new[]{900,1200,1500}),("C",new[]{1200,1200,1500}),("D",new[]{1200,1200,1500})})
   {
    var directory=Path.Combine(output,"isolated-rulesets",name);Directory.CreateDirectory(directory);
    foreach(var path in Directory.GetFiles(data,"*.json",SearchOption.AllDirectories).Where(p=>!Path.GetRelativePath(data,p).Split(Path.DirectorySeparatorChar).Contains("rulesets")))
    {var target=Path.Combine(directory,Path.GetRelativePath(data,path));Directory.CreateDirectory(Path.GetDirectoryName(target)!);File.Copy(path,target,true);}
    string economyPath=Path.Combine(directory,"economy.json");var economy=JsonNode.Parse(File.ReadAllText(economyPath))!;
    if(economy["loss_payouts"] is not JsonArray)throw new InvalidDataException("Cannot locate canonical loss_payouts");
    if(name!="C"){economy["loss_payouts"]=new JsonArray(schedule.Select(n=>JsonValue.Create(n)).ToArray());if(name is "A" or "D")economy["reward_policy"]!["total_limit"]=0;File.WriteAllText(economyPath,economy.ToJsonString(new JsonSerializerOptions{WriteIndented=true}));}
    variants[name]=GameContent.Load(directory);if(name=="C")Check(variants[name].ContentHash==content.ContentHash,"candidate control must preserve exact source bytes");identities.Add(new{name,lossPayouts=schedule,skillEnabled=name is "B" or "C",contentHash=variants[name].ContentHash,files=Directory.GetFiles(directory,"*.json",SearchOption.AllDirectories).Select(p=>new{path=Path.GetRelativePath(directory,p).Replace('\\','/'),sha256=HashFile(p)}).ToArray()});
   }
   foreach(var payout in variants)foreach(string policy in new[]{"pressure","spacing","reward-seeking"})for(int seed=seedStart;seed<seedStart+seeds;seed++)for(int seat=0;seat<2;seat++)
   {
    string id=$"crossover-{payout.Key}-{policy}-s{seed}-seat{seat}";var sim=new Simulation(payout.Value,new(){SessionId=id,Fighter0=seat==0?"rook":"vale",Fighter1=seat==0?"vale":"rook"});
    Add(Match(sim,id,"crossover-full-match",seed,policy,"",seat,false,payout.Key,"fresh-match",0,output,false));
   }
   foreach(var payout in variants)foreach(int spend in new[]{0,600})foreach(string gap in new[]{"earned-gap","equal-bank"})for(int seed=seedStart;seed<seedStart+seeds;seed++)for(int seat=0;seat<2;seat++)
   {
    string id=$"opening-{payout.Key}-{spend}-{gap}-s{seed}-seat{seat}";var sim=new Simulation(payout.Value,new(){SessionId=id,Fighter0=seat==0?"rook":"vale",Fighter1=seat==0?"vale":"rook"});
    Set(sim,nameof(Simulation.RoundId),2);Set(sim,nameof(Simulation.CompletedRounds),1);Set(sim.Players[seat],nameof(PlayerState.ScoreHalfPoints),2);Set(sim.Players[1-seat],nameof(PlayerState.RecoveryTier),1);
    int leader=600+1200-spend+(payout.Key is "B" or "C"?300:0),trailer=600+payout.Value.Economy.LossPayouts[0]-spend+(payout.Key is "B" or "C"?100:0);
    if(gap=="equal-bank")leader=trailer;Set(sim.Players[seat],nameof(PlayerState.Credits),leader);Set(sim.Players[1-seat],nameof(PlayerState.Credits),trailer);
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
   var reference=samples.Where(s=>s.Kind=="opening-full-match"&&s.Payout==group.Key.Payout&&s.OpeningSpend==group.Key.OpeningSpend&&s.Gap=="earned-gap").ToArray();
   var blocks=group.GroupBy(s=>s.Seed).Select(s=>new{seed=s.Key,leaderResult=s.Average(x=>x.ActorResult),round2Result=s.Average(x=>x.OpeningRoundResult),delta=s.Average(x=>x.ActorResult)-reference.Where(x=>x.Seed==s.Key).Average(x=>x.ActorResult),round2Delta=s.Average(x=>x.OpeningRoundResult)-reference.Where(x=>x.Seed==s.Key).Average(x=>x.OpeningRoundResult)}).ToArray();
   return new{payout=group.Key.Payout,openingSpend=group.Key.OpeningSpend,gap=group.Key.Gap,seedBlocks=blocks,meanLeaderResult=group.Average(s=>s.ActorResult),pairedMatchDelta=Interval(blocks.Select(b=>b.delta).ToArray()),pairedRound2Delta=Interval(blocks.Select(b=>b.round2Delta).ToArray())};
  }).ToArray();
  File.WriteAllText(Path.Combine(output,"summary.json"),JsonSerializer.Serialize(new{format="penny-shop-only-v2-paired-pilot",build=ReplayFormat.Build,contentHash=content.ContentHash,command=Environment.GetCommandLineArgs(),scope,seeds,seedStart,elapsedSeconds=watch.Elapsed.TotalSeconds,samples=samples.Count,completed=samples.Count,excluded=0,totalTicks=samples.Sum(s=>(long)s.Steps),fightBankEqualityAssertions=samples.Sum(s=>(long)s.Steps),allTracesReconstructed=samples.All(s=>s.Reconstructed),rentalAlternativesRun=samples.Where(s=>s.Rental).Select(s=>s.Item).Distinct().Count(),rentalAlternativesWithActualStart=samples.Where(s=>s.Rental&&s.RentalStarts>0).Select(s=>s.Item).Distinct().Count(),paired,gapEffects,crossover=samples.Where(s=>s.Kind=="crossover-full-match").GroupBy(s=>new{s.Policy,s.Payout}).Select(g=>new{g.Key,samples=g.Count(),meanResult=g.Average(x=>x.ActorResult),meanRounds=g.Average(x=>x.Rounds),meanSkillEarned=g.Average(x=>x.SkillEarned.Sum())}).ToArray(),productStartCoverage=samples.Where(s=>s.Rental).SelectMany(s=>s.ProductStarts).GroupBy(x=>x.Key).Select(g=>new{product=g.Key,starts=g.Sum(x=>x.Value)}).ToArray(),isolatedPayoutVariants=identities,scoreOnlyBenchmark=new{context="first-to-five, leader1-0, independent fair future rounds without draws",exactNumerator=163,exactDenominator=256,leaderMatchWin=163.0/256,kind="analytical reference, not measured bot result"},limitations=new[]{"Each product is paired with no purchase retaining its exact fee; fixed opponent Thomas, neutral stage spawn. Core/full catalog and supported opponent factorial remain separate.","Rental comparisons run one actual timed/KO round. They cannot establish match-level purchase value. Opening-gap experiments do complete the match from an explicitly recorded round2 score1-0 fixture.","Mirrored runs are averaged inside seed blocks. Conditional Hoeffding intervals with two seeds remain wide; no human balance conclusion follows.","Agents use ordinary encoded commands. The pressure/spacing item policy changes available action decisions; it does not infer optimal counterplay or human recognition.","A zero-contact product remains a measured policy outcome, not proof its mechanic is ineffective. Product counters and conformance are reported separately. Reward-seeking uses a 12-tick-old observation to attempt ordinary anti-air or manual forward parry; it has no privileged classifier access.","All four controls use shop-only capabilities: A historical payouts/no skill, B historical/capped skill, C candidate/capped skill, D candidate/no skill. C is byte-identical to shipping data; all others are isolated copies.","Round-two earned-gap fixtures explicitly assume leader300/trailer100 skill credits when enabled; these are controlled initial balances, not a fabricated played first round. Equal-bank controls lower only the leader bank while retaining the same 1-0 score.","No human competitive balance, silhouette calibration, two-PC device latency, or psychological first-loss interpretation is claimed."}},new JsonSerializerOptions{WriteIndented=true}));
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
  int[] openingWallets=sim.Players.Select(p=>p.Credits).ToArray();int steps=0,rounds=0,starts=0,contacts=0,paid=0,denied=0,spend=0,damage=0;double result=0,openingRoundResult=0;var productStarts=new Dictionary<string,int>();int[] skillEarned=[0,0],skillGranted=[0,0],skillClipped=[0,0],superUses=[0,0];
  while(sim.Phase!=MatchPhase.MatchOver&&steps<90000)
  {
   if(sim.Phase==MatchPhase.Preparation)
   {
    var p0=oneRound?new PreparationPlan(seat==0&&rental?item.Split('|'):[]):agents[0].Prepare(sim);var p1=oneRound?new PreparationPlan(seat==1&&rental?item.Split('|'):[]):agents[1].Prepare(sim);sim.CommitPreparation(p0,p1);sim.BeginFight();commands.Add(new("prepare-begin",sim.Tick,Plan0:p0,Plan1:p1,Hash:sim.Hash()));
   }
   if(sim.Phase==MatchPhase.PendingResult)
   {
    result=sim.PendingResult!.WinnerSeat<0?.5:sim.PendingResult.WinnerSeat==seat?1:0;if(rounds==0)openingRoundResult=result;
    var closingBeforePayout=sim.Players.Select(p=>p.Credits).ToArray();var settlement=sim.SettleRound(sim.Tick);rounds++;for(int player=0;player<2;player++){var skill=player==0?settlement.SkillPayout0:settlement.SkillPayout1;skillEarned[player]+=skill.Earned;skillGranted[player]+=skill.Granted;skillClipped[player]+=skill.Clipped;superUses[player]+=sim.Players[player].SuperUseReceipts.Count;}
    File.AppendAllText(Path.Combine(output,"round-economy.jsonl"),JsonSerializer.Serialize(new{match=id,round=sim.RoundId,score=sim.Players.Select(p=>p.ScoreHalfPoints).ToArray(),beforePayout=closingBeforePayout,settlement,afterPayout=sim.Players.Select(p=>p.Credits).ToArray(),usableInContinuingMatch=sim.Phase!=MatchPhase.MatchOver})+Environment.NewLine);
    commands.Add(new("settle",sim.Tick,Hash:sim.Hash()));if(oneRound||sim.Phase==MatchPhase.MatchOver)break;sim.NextRound();commands.Add(new("next-round",sim.Tick,Hash:sim.Hash()));continue;
   }
   var banks=sim.Players.Select(p=>p.Credits).ToArray();var a=agents[0].Next(sim);var b=agents[1].Next(sim);var frame=sim.Step(a,b);Check(banks.SequenceEqual(sim.Players.Select(p=>p.Credits)),"fight bank mutation");steps++;commands.Add(new("step",a.Frame,a.Direction,a.Held,b.Direction,b.Held,Hash:steps%60==0?frame.Hash:"",Events:frame.Events.ToArray()));
   foreach(var e in frame.Events)
   {
    foreach(var agent in agents)agent.Observe(e);
    if(e.Kind==CombatEventKind.ActionStarted&&e.Seat==seat){var action=sim.Content.Fighters[sim.Players[seat].FighterId].Moves.FirstOrDefault(m=>m.Id==e.MoveId);if(action is not null&&action.Availability!="base")productStarts[action.Availability]=productStarts.GetValueOrDefault(action.Availability)+1;}
    if(e.Kind==CombatEventKind.ActionStarted&&e.Seat==seat){if(e.MoveId==item||sim.Content.Fighters[sim.Players[seat].FighterId].Moves.Any(m=>m.Id==e.MoveId&&item.Split('|').Contains(m.Availability)))starts++;if(sim.Content.Fighters[sim.Players[seat].FighterId].Moves.FirstOrDefault(m=>m.Id==e.MoveId)?.Kind is "ex_special" or "super")paid++;}
    if(e.Kind==CombatEventKind.Rejected&&e.Seat==seat&&e.Detail is "locked" or "exhausted")denied++;
    if(e.Kind==CombatEventKind.Spend&&e.Seat==seat)spend+=e.Value;
    if(e.Kind is CombatEventKind.Hit or CombatEventKind.Throw&&e.Seat==seat){damage+=e.Value;if(e.MoveId==item||sim.Content.Fighters[sim.Players[seat].FighterId].Moves.Any(m=>m.Id==e.MoveId&&item.Split('|').Contains(m.Availability)))contacts++;}
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
  return new(id,kind,seed,policy,item,seat,rental,payout,gap,openingSpend,steps,rounds,actor,opponent,result,starts,contacts,paid,denied,spend,damage,initialHash,sim.Hash(),"traces/"+Path.GetFileName(path),HashFile(path),true,openingWallets,openingRoundResult){ProductStarts=productStarts,SkillEarned=skillEarned,SkillGranted=skillGranted,SkillClipped=skillClipped,SuperUsesConsumed=superUses};
 }
 sealed class Agent(int seat,string policy,uint seed,string item)
 {
  readonly BotController baseline=new(seat,policy=="spacing"?BotMode.Conservative:BotMode.Adaptive,seed);readonly Queue<CommandInput> queue=[];long last=-1000,lastContact=-1000;string branchAction="";int selection;readonly Queue<(long Tick,int Distance,int OpponentY,string OpponentAction,int Facing)> observations=[];long lastRewardAttempt=-1000;
  public PreparationPlan Prepare(Simulation sim){queue.Clear();return baseline.ChoosePreparation(sim);}
  public void Observe(CombatEvent e){if(e.Kind is CombatEventKind.Hit or CombatEventKind.Block&&(e.Seat==seat||e.Target==seat))lastContact=e.Tick;}
  public InputFrame Next(Simulation sim)
  {
   var p=sim.Players[seat];var opponent=sim.Players[1-seat];observations.Enqueue((sim.Tick,Math.Abs(p.X-opponent.X),opponent.Y,opponent.ActionId,p.Facing));while(observations.Count>13)observations.Dequeue();if(queue.Count>0){var input=queue.Dequeue();return new(seat,sim.Tick,input.Direction,input.Held);}
   if(p.ActionId.Length>0&&p.Hitstop==0)
   {
    var move=sim.Content.Fighters[p.FighterId].Move(p.ActionId);var branch=move.Branches.FirstOrDefault(b=>p.ActionFrame>=(b.RelativeToContact?p.FirstContactFrame:0)+b.Start&&p.ActionFrame<(b.RelativeToContact?p.FirstContactFrame:0)+b.End&&(b.Contact=="none"||p.Contact));
    if(branch is not null&&branchAction!=p.ActionId&&(branch.Contact=="none"||sim.Tick-lastContact>=12)){branchAction=p.ActionId;return new(seat,sim.Tick,5,branch.Button=="K"?Buttons.MK:Buttons.MP);}
   }
   if(policy=="reward-seeking"&&observations.Count==13&&p.Actionable&&sim.Tick-lastRewardAttempt>=30)
   {
    var seen=observations.Peek();if(seen.Distance<80000&&seen.OpponentY>0){lastRewardAttempt=sim.Tick;foreach(var input in CommandEncoder.Encode(sim.Content,sim.Content.Fighters[p.FighterId].Move("rise_h"),seen.Facing))queue.Enqueue(input);var queued=queue.Dequeue();return new(seat,sim.Tick,queued.Direction,queued.Held);}
    if(seen.Distance<60000&&seen.OpponentAction.Length>0){lastRewardAttempt=sim.Tick;return new(seat,sim.Tick,CoreMath.RelativeDirection(6,seen.Facing),Buttons.None);}
   }
   if(item.Length>0&&p.Actionable&&sim.Tick-last>=(policy=="pressure"?100:160))
   {
    var ids=item.Split('|');var product=sim.Content.Items[ids[selection++%ids.Length]];var move=sim.Content.Fighters[p.FighterId].Move(product.MoveId);last=sim.Tick;if(sim.CapabilityStatus(seat,move.Id)==ActivationStatus.Exhausted)return baseline.Next(sim);branchAction="";
    if(move.Command.StartsWith("air:")&&p.Grounded){queue.Enqueue(new(CoreMath.RelativeDirection(9,p.Facing),Buttons.None));for(int i=0;i<13;i++)queue.Enqueue(new(5,Buttons.None));}
    foreach(var input in CommandEncoder.Encode(sim.Content,move,p.Facing))queue.Enqueue(input);var next=queue.Dequeue();return new(seat,sim.Tick,next.Direction,next.Held);
   }
   return baseline.Next(sim);
  }
 }
}
