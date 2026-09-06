using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using StrikeLedger.App;
using StrikeLedger.Core;

/// <summary>Declared policy experiments from trusted, explicit decisive-round fixtures.</summary>
public static class ShopStrategyCells
{
 sealed record Command(string Kind,long Tick,byte D0=5,Buttons B0=Buttons.None,byte D1=5,Buttons B1=Buttons.None,string Hash="",CombatEvent[]? Events=null,PreparationPlan? P0=null,PreparationPlan? P1=null);
 sealed record Trace(string Format,string ContentHash,string Build,MatchConfig Config,string InitialSnapshot,Command[] Commands,string FinalHash);
 sealed record Sample(string Id,string Policy,int Seed,int Seat,bool Treatment,string[] Products,int Steps,int Winner,string TerminalReason,double ActorResult,int[] FinalScores,int[] FinalHealth,int[] BankAfterShop,int[] ClosingBank,int SuperUses,int SuperUsesRemaining,int ActorBlocks,int OpponentBlocks,int ActorHits,PreparationReceipt Preparation,SettlementReceipt Settlement,string TracePath,string TraceSha256,bool Reconstructed);
 static void Check(bool pass,string reason){if(!pass)throw new InvalidOperationException(reason);}
 static void Set(object target,string property,object value)=>target.GetType().GetProperty(property,BindingFlags.Public|BindingFlags.Instance)!.SetValue(target,value);
 static string Hash(string path)=>Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant();
 public static int Run(GameContent content,string output,int start,int seeds)
 {
  var watch=Stopwatch.StartNew();Directory.CreateDirectory(Path.Combine(output,"traces"));var samples=new List<Sample>();
  using var records=new StreamWriter(Path.Combine(output,"strategy-samples.jsonl")){AutoFlush=true};
  foreach(string policy in new[]{"unused-permit-threat","draw-seeking","chip-out","decisive-cash-out"})for(int seed=start;seed<start+seeds;seed++)foreach(int seat in new[]{0,1})foreach(bool treatment in new[]{false,true})
  {
   string id=$"{policy}-s{seed}-seat{seat}-{(treatment?"treatment":"control")}";
   var sim=new Simulation(content,new(){Fighter0=seat==0?"rook":"vale",Fighter1=seat==1?"rook":"vale",SessionId="strategy-"+id});
   foreach(var p in sim.Players){Set(p,nameof(PlayerState.Credits),1200);Set(p,nameof(PlayerState.ScoreHalfPoints),8);}
   Set(sim,nameof(Simulation.CompletedRounds),8);Set(sim,nameof(Simulation.RoundId),9);
   if(policy=="chip-out")
   {
    Set(sim.Players[seat],nameof(PlayerState.X),seat==0?330000:438000);Set(sim.Players[1-seat],nameof(PlayerState.X),seat==0?395000:373000);Set(sim.Players[1-seat],nameof(PlayerState.Health),9);
   }
   string[] products=treatment&&policy!="draw-seeking"?[content.Items.Values.Single(i=>i.EligibleFighters.Contains("rook")&&i.MoveId==(policy=="chip-out"?"pulse_ex":"super_2")).Id]:[];
   var sample=Play(sim,id,policy,seed,seat,treatment,products,output);samples.Add(sample);records.WriteLine(JsonSerializer.Serialize(sample));Console.WriteLine($"{samples.Count}: {id} result={sample.ActorResult} ticks={sample.Steps} use={sample.SuperUses} blocks={sample.OpponentBlocks} replay=verified");
  }
  Check(samples.Count==4*seeds*2*2&&samples.All(s=>s.Reconstructed),"all intended strategy cells complete");
  Check(samples.Where(s=>s.Policy=="unused-permit-threat"&&s.Treatment).All(s=>s.SuperUses==0&&s.SuperUsesRemaining==1),"unused-permit policy really retains the unspent use through results");
  Check(samples.Where(s=>s.Policy=="chip-out").All(s=>s.OpponentBlocks>0&&s.Winner==s.Seat&&s.FinalHealth[1-s.Seat]==0&&s.ActorHits==0),"chip-out has actual blocking-only KO in both purchase and free controls");
  Check(samples.Where(s=>s.Policy=="decisive-cash-out"&&s.Treatment).All(s=>s.SuperUses==1),"cash-out policy actually commits its one permit");
  var paired=samples.GroupBy(s=>s.Policy).Select(g=>new{policy=g.Key,seedBlocks=g.GroupBy(s=>s.Seed).Select(seed=>new{seed=seed.Key,treatment=seed.Where(s=>s.Treatment).Average(s=>s.ActorResult),control=seed.Where(s=>!s.Treatment).Average(s=>s.ActorResult),delta=seed.Where(s=>s.Treatment).Average(s=>s.ActorResult)-seed.Where(s=>!s.Treatment).Average(s=>s.ActorResult)}).ToArray(),treatmentDraws=g.Count(s=>s.Treatment&&s.Winner<0),controlDraws=g.Count(s=>!s.Treatment&&s.Winner<0),treatmentMeanSteps=g.Where(s=>s.Treatment).Average(s=>s.Steps),controlMeanSteps=g.Where(s=>!s.Treatment).Average(s=>s.Steps)}).ToArray();
  File.WriteAllText(Path.Combine(output,"strategy-summary.json"),JsonSerializer.Serialize(new{format="shop-v2-actual-strategy-cells",passed=true,command=Environment.GetCommandLineArgs(),build=ReplayFormat.Build,contentHash=content.ContentHash,completed=samples.Count,excluded=0,allTracesReconstructed=true,fightBankEqualityAssertions=samples.Sum(s=>(long)s.Steps),elapsedSeconds=watch.Elapsed.TotalSeconds,paired,policies=new[]{"unused-permit-threat: buy super2 and deliberately never use it; identical free-attack policy versus no purchase. Opponent is existing Adaptive bot, whose 12-tick-old public observation changes defensive probability when an opposing unused permit exists.","draw-seeking: retreat/crouch-guard without attacks versus the same Adaptive opponent; control uses free attacks, both buy nothing. Failed draw attempts remain outcomes, not excluded samples.","chip-out: at explicit nine-HP defender and 65-unit gap, repeat licensed EX pulse versus retained cash and free heavy pulse; defender holds legal crouch guard. Both controls must produce blocking-only KO.","decisive-cash-out: buy and use super2 at the first legal opportunity, then free attacks; control retains all cash and uses the same free policy, versus Adaptive opponent."},limits=new[]{"Every fixture begins at explicit round9 score4-4. It is a completed decisive round and match continuation, not fabricated eight-round history.","Both players begin with1200; trusted initial positions/health/scores are serialized before purchase. No combat state mutation, credit packet, direct action start, or instant reward classification drives policy.","Policy observations are delayed12ticks; command entry uses shared production CommandEncoder. Guard-only chip target is a declared cooperative defense fixture, not adapted human counterplay.","Unused threat measures this authored opponent probability rule only; no psychological human deterrence claim. Draw seeking is an attempted policy, not a promise of a drawn result.","Two chosen seeds and counterbalanced seats provide witnesses, not calibrated human balance or optimal strategy. Terminal balances cannot fund another round in the completed match."}},new JsonSerializerOptions{WriteIndented=true}));return 0;
 }
 static Sample Play(Simulation sim,string id,string policy,int seed,int seat,bool treatment,string[] products,string output)
 {
  string initial=Convert.ToBase64String(sim.Capture().Bytes);var commands=new List<Command>();var p0=new PreparationPlan(seat==0?products:[]);var p1=new PreparationPlan(seat==1?products:[]);var purchase=sim.CommitPreparation(p0,p1);sim.BeginFight();commands.Add(new("prepare-begin",sim.Tick,Hash:sim.Hash(),P0:p0,P1:p1));
  var bank=sim.Players.Select(p=>p.Credits).ToArray();var actor=new Actor(seat,policy,treatment);var opponent=new BotController(1-seat,policy=="chip-out"?BotMode.Guard:BotMode.Adaptive,(uint)seed);int steps=0,actorBlocks=0,otherBlocks=0,hits=0;
  while(sim.Phase==MatchPhase.Fight&&steps<12000)
  {
   var a=actor.Next(sim);var b=opponent.Next(sim);var r=seat==0?sim.Step(a,b):sim.Step(b,a);steps++;
   Check(sim.Players.Select(p=>p.Credits).SequenceEqual(bank),"strategy fight bank must be frozen");
   commands.Add(new("step",r.Tick,seat==0?a.Direction:b.Direction,seat==0?a.Held:b.Held,seat==0?b.Direction:a.Direction,seat==0?b.Held:a.Held,steps%60==0?r.Hash:"",r.Events.ToArray()));
   actorBlocks+=r.Events.Count(e=>e.Kind==CombatEventKind.Block&&e.Seat==seat);otherBlocks+=r.Events.Count(e=>e.Kind==CombatEventKind.Block&&e.Seat==1-seat);hits+=r.Events.Count(e=>e.Kind==CombatEventKind.Hit&&e.Seat==seat);
  }
  Check(sim.Phase==MatchPhase.PendingResult,"strategy must reach a real terminal result");int winner=sim.PendingResult!.WinnerSeat;string reason=sim.PendingResult.Reason;var receipt=sim.SettleRound(sim.Tick);commands.Add(new("settle",sim.Tick,Hash:sim.Hash()));Check(sim.Phase==MatchPhase.MatchOver,"decisive fixture must complete its match");
  var trace=new Trace("shop-v2-trusted-strategy-command-trace",sim.Content.ContentHash,ReplayFormat.Build,sim.Config,initial,commands.ToArray(),sim.Hash());var replay=new Simulation(sim.Content,sim.Config);replay.Restore(new(Convert.FromBase64String(initial)));
  foreach(var c in commands)
  {
   Check(replay.Tick==c.Tick,"strategy replay tick");switch(c.Kind){case "prepare-begin":replay.CommitPreparation(c.P0!,c.P1!);replay.BeginFight();break;case "settle":replay.SettleRound(replay.Tick);break;case "step":var r=replay.Step(new(0,replay.Tick,c.D0,c.B0),new(1,replay.Tick,c.D1,c.B1));Check(r.Events.SequenceEqual(c.Events!),"strategy event reconstruction");break;default:throw new InvalidDataException("strategy command");}
   if(c.Hash.Length>0)Check(replay.Hash()==c.Hash,"strategy canonical checkpoint");
  }
  Check(replay.Hash()==trace.FinalHash,"strategy final-state reconstruction");string path=Path.Combine(output,"traces",id+".json.gz");using(var f=File.Create(path))using(var z=new GZipStream(f,CompressionLevel.SmallestSize))JsonSerializer.Serialize(z,trace);
  return new(id,policy,seed,seat,treatment,products,steps,winner,reason,winner<0?.5:winner==seat?1:0,sim.Players.Select(p=>p.ScoreHalfPoints).ToArray(),sim.Players.Select(p=>p.Health).ToArray(),bank,sim.Players.Select(p=>p.Credits).ToArray(),sim.Players[seat].SuperUseReceipts.Count,sim.Players[seat].SuperUsesRemaining,actorBlocks,otherBlocks,hits,purchase,receipt,"traces/"+Path.GetFileName(path),Hash(path),true);
 }
 sealed class Actor(int seat,string policy,bool treatment)
 {
  readonly Queue<CommandInput> queue=[];readonly Queue<(int Distance,int Facing)> observations=[];long last=-1000;
  public InputFrame Next(Simulation s)
  {
   var me=s.Players[seat];observations.Enqueue((Math.Abs(me.X-s.Players[1-seat].X),me.Facing));while(observations.Count>13)observations.Dequeue();
   InputFrame Frame(byte d=5,Buttons b=Buttons.None)=>new(seat,s.Tick,d,b);
   if(queue.Count>0){var c=queue.Dequeue();return Frame(c.Direction,c.Held);}if(observations.Count<13)return Frame();var seen=observations.Peek();byte Relative(byte d)=>CoreMath.RelativeDirection(d,seen.Facing);
   if(policy=="draw-seeking"&&treatment)return Frame(Relative(1));
   if(!me.Actionable)return Frame(Relative(1));
   string? move=null;
   if(policy=="decisive-cash-out"&&treatment&&me.SuperUsesRemaining>0)move="super_2";
   else if(s.Tick-last>=70)move=policy=="chip-out"?(treatment?"pulse_ex":"pulse_h"):seen.Distance>95000?"pulse_h":"s_hp";
   if(move is not null){last=s.Tick;foreach(var c in CommandEncoder.Encode(s.Content,me.FighterId,move,seen.Facing))queue.Enqueue(c);var c0=queue.Dequeue();return Frame(c0.Direction,c0.Held);}
   return policy=="chip-out"?Frame():Frame(seen.Distance>60000?Relative(6):Relative(1));
  }
 }
}
