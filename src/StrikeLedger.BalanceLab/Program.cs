using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using StrikeLedger.App;
using StrikeLedger.Core;

var argsMap=new Dictionary<string,string>();for(int i=0;i<args.Length;i+=2){if(i+1>=args.Length)throw new ArgumentException("Use --option value");argsMap[args[i]]=args[i+1];}
string Get(string key,string fallback)=>argsMap.GetValueOrDefault(key,fallback);
string output=Path.GetFullPath(Get("--evidence-dir",Get("--output","reports/balance-lab")));Directory.CreateDirectory(output);Directory.CreateDirectory(Path.Combine(output,"frozen-states"));
var content=GameContent.Load(Get("--data","data"));int seeds=int.Parse(Get("--seeds","1"));if(seeds is <1 or >20)throw new ArgumentException("Seed count must be 1..20");
int seedStart=int.Parse(Get("--seed","1"));if(seedStart is <1 or >100000)throw new ArgumentException("Seed must be 1..100000");
string scenario=Get("--scenario","all");if(scenario is not ("all" or "zero_vs_full_wallet" or "recovery_farming"))throw new ArgumentException("Unknown balance scenario");
string[] policies=["spend-on-confirm","conservative-reserve","force-ex","bank-super","lease-focused","zero-spend-defense","controlled-losing"];
int[] budgets=[0,300,600,900,1800,3600];
var records=new List<MatchRecord>();var watch=Stopwatch.StartNew();using var roundsFile=new StreamWriter(Path.Combine(output,"rounds.jsonl")){AutoFlush=true};using var matchesFile=new StreamWriter(Path.Combine(output,"matches.jsonl")){AutoFlush=true};
for(int seed=seedStart;seed<seedStart+seeds;seed++)for(int policyIndex=0;policyIndex<policies.Length;policyIndex++)for(int contextIndex=0;contextIndex<9;contextIndex++)
{
    int budgetIndex=contextIndex%budgets.Length;
    if(scenario=="zero_vs_full_wallet" && budgetIndex is not (0 or 5))continue;
    if(scenario=="recovery_farming" && policyIndex is not (0 or 6))continue;
    int pair=contextIndex;
    string fighterA=pair%4<2?"rook":"vale",fighterB=(pair%4) switch{0=>"vale",1=>"rook",2=>"rook",_=>"vale"};
    string artA="art_"+(pair%3+1),artB="art_"+(pair/3%3+1);
    string stratum=(pair%6) switch{0=>"neutral",1=>"corner",2=>"health-lead",3=>"recovery-tier-2",4=>"final-round",_=>"cap"};
    for(int budgetOrder=0;budgetOrder<2;budgetOrder++)
    {
    int openingA=budgets[budgetOrder==0?budgetIndex:5-budgetIndex],openingB=budgets[budgetOrder==0?5-budgetIndex:budgetIndex];
    for(int swap=0;swap<2;swap++)
    {
        int actorSeat=swap;string id=$"s{seed}-p{policyIndex}-pair{contextIndex}-budget{budgetOrder}-swap{swap}";
        var config=new MatchConfig{SessionId=id,Fighter0=swap==0?fighterA:fighterB,Fighter1=swap==0?fighterB:fighterA,Super0=swap==0?artA:artB,Super1=swap==0?artB:artA};
        var simulation=new Simulation(content,config);
        // Trusted experimental fixture setup ONLY. Reflection remains in this separate lab
        // executable; the shipped App/network exposes no competitive-wallet mutation API.
        Set(simulation.Players[actorSeat],nameof(PlayerState.Credits),openingA);Set(simulation.Players[1-actorSeat],nameof(PlayerState.Credits),openingB);
        if(stratum=="corner"){Set(simulation.Players[0],nameof(PlayerState.X),swap==0?22000:678000);Set(simulation.Players[1],nameof(PlayerState.X),swap==0?90000:746000);}
        if(stratum=="health-lead")Set(simulation.Players[1-actorSeat],nameof(PlayerState.Health),600);
        if(stratum=="recovery-tier-2")foreach(var p in simulation.Players)Set(p,nameof(PlayerState.RecoveryTier),2);
        if(stratum=="final-round"){foreach(var p in simulation.Players)Set(p,nameof(PlayerState.ScoreHalfPoints),8);Set(simulation,nameof(Simulation.CompletedRounds),8);Set(simulation,nameof(Simulation.RoundId),9);}
        byte[] initial=simulation.SerializeCanonical();string initialHash=simulation.Hash();File.WriteAllBytes(Path.Combine(output,"frozen-states",id+".bin"),initial);
        var bots=new PolicyController[2];bots[actorSeat]=new(actorSeat,policies[policyIndex],(uint)(seed*100+1));bots[1-actorSeat]=new(1-actorSeat,"spend-on-confirm",(uint)(seed*100+2));
        int steps=0,playedRounds=0;RoundTelemetry? telemetry=null;bool aborted=false;string failure="";
        try
        {
            while(simulation.Phase!=MatchPhase.MatchOver && steps<50000)
            {
                if(simulation.Phase==MatchPhase.Preparation)
                {
                    telemetry=new(id,simulation.RoundId,simulation.Players.Select(p=>p.Credits).ToArray(),simulation.Players.Select(p=>p.RecoveryTier).ToArray(),simulation.Players.Select(p=>p.Health).ToArray());
                    var receipt=simulation.CommitPreparation(bots[0].Prepare(simulation),bots[1].Prepare(simulation));telemetry.LeaseCosts=[receipt.Cost0,receipt.Cost1];simulation.BeginFight();
                }
                if(simulation.Phase==MatchPhase.PendingResult)
                {
                    int winner=simulation.PendingResult!.WinnerSeat;var payout=simulation.SettleRound(simulation.Tick);playedRounds++;
                    telemetry!.Winner=winner;telemetry.Reason=simulation.PendingResult.Reason;telemetry.Payouts=[payout.Payout0,payout.Payout1];telemetry.ClosingCredits=simulation.Players.Select(p=>p.Credits).ToArray();telemetry.TerminalHash=simulation.Hash();
                    roundsFile.WriteLine(JsonSerializer.Serialize(telemetry));
                    if(simulation.Phase==MatchPhase.MatchOver)break;simulation.NextRound();continue;
                }
                int stun0=simulation.Players[0].Stun,stun1=simulation.Players[1].Stun;
                var result=simulation.Step(bots[0].Next(simulation),bots[1].Next(simulation));steps++;
                telemetry!.StunInflicted[0]+=Math.Max(0,simulation.Players[1].Stun-stun1);telemetry.StunInflicted[1]+=Math.Max(0,simulation.Players[0].Stun-stun0);
                foreach(var e in result.Events)
                {
                    foreach(var b in bots)b.Observe(e);
                    if(e.Seat is <0 or >1)continue;int seat=e.Seat;
                    var fighter=content.Fighters[simulation.Players[seat].FighterId];var move=fighter.Moves.FirstOrDefault(m=>m.Id==e.MoveId);
                    if(e.Kind==CombatEventKind.ActionStarted && move is not null)
                    {telemetry!.EndMelee(seat,false);telemetry.Actions[seat]++;if(move.Kind is "ex_special" or "super"){telemetry.PaidAccepted[seat]++;telemetry.PaidAttempts[seat]++;}telemetry.StartedMoves[seat].Add(e.MoveId);if(move.Projectile is null&&(move.Hitboxes.Length>0||move.Throw is not null))telemetry.TrackMelee(seat,e.MoveId);else if(move.Projectile is not null)telemetry.ProjectileStartups[seat]++;}
                    if(e.Kind==CombatEventKind.Rejected && move?.CreditCost>0){telemetry!.PaidRejected[seat]++;telemetry.PaidAttempts[seat]++;}
                    if(e.Kind==CombatEventKind.Spend)telemetry!.CombatSpend[seat]+=e.Value;
                    if(e.Kind is CombatEventKind.Hit or CombatEventKind.Throw){telemetry!.Hits[seat]++;telemetry.Damage[seat]+=e.Value;telemetry.ContactMoves[seat].Add(e.MoveId);telemetry.ContactMelee(seat,e.MoveId);}
                    if(e.Kind==CombatEventKind.Block){telemetry!.Blocks[seat]++;if(e.Target>=0){telemetry.ContactMoves[e.Target].Add(e.MoveId);telemetry.ContactMelee(e.Target,e.MoveId);}}
                    if(e.Kind==CombatEventKind.Parry){telemetry!.Parries[seat]++;if(e.Target>=0)telemetry.ContactMelee(e.Target,e.MoveId);}
                    if(simulation.Players.Any(p=>p.Credits<0||p.Credits>content.Economy.Cap))throw new InvalidOperationException("Wallet bound violated");
                }
                for(int seat=0;seat<2;seat++){var p=simulation.Players[seat];if(p.ActionId.Length==0)telemetry.EndMelee(seat,p.Hitstun>0||p.KnockdownTicks>0);}
            }
            if(simulation.Phase!=MatchPhase.MatchOver)throw new InvalidOperationException("Full match exceeded 50,000-tick bound");
        }
        catch(Exception ex){aborted=true;failure=ex.Message;}
        int actorPoints=simulation.Players[actorSeat].ScoreHalfPoints,opponentPoints=simulation.Players[1-actorSeat].ScoreHalfPoints;
        var match=new MatchRecord(id,seed,policies[policyIndex],actorSeat,fighterA,fighterB,artA,artB,openingA,openingB,stratum,initialHash,simulation.Hash(),playedRounds,steps,actorPoints,opponentPoints,actorPoints.CompareTo(opponentPoints),aborted,failure);
        records.Add(match);matchesFile.WriteLine(JsonSerializer.Serialize(match));Console.WriteLine($"{records.Count}: {id} {policies[policyIndex]} {actorPoints/2.0}:{opponentPoints/2.0} {steps} ticks{(aborted?" ABORTED "+failure:"")}");
    }
    }
}
var summary=policies.Select(policy=>
{
    var sample=records.Where(r=>r.Policy==policy&&!r.Aborted).ToArray();int n=sample.Length,wins=sample.Count(r=>r.Verdict>0),draws=sample.Count(r=>r.Verdict==0);double p=n==0?0:(double)wins/n,z=1.959963984540054,denominator=1+z*z/Math.Max(1,n),center=(p+z*z/(2*Math.Max(1,n)))/denominator,half=z*Math.Sqrt(p*(1-p)/Math.Max(1,n)+z*z/(4.0*Math.Max(1,n)*Math.Max(1,n)))/denominator;
    return new{policy,completedMatches=n,wins,draws,losses=n-wins-draws,winFraction=p,wilson95=new[]{Math.Max(0,center-half),Math.Min(1,center+half)}};
}).ToArray();
File.WriteAllText(Path.Combine(output,"summary.json"),JsonSerializer.Serialize(new{utc=DateTime.UtcNow,build=ReplayFormat.Build,contentHash=content.ContentHash,platform=Environment.OSVersion.ToString(),elapsedSeconds=watch.Elapsed.TotalSeconds,command=Environment.GetCommandLineArgs(),exitCode=records.Any(r=>r.Aborted)?1:0,scenario,samples=records.Count,excluded=records.Count(r=>r.Aborted),policies=summary,limitations=new[]{"Bounded stratified 252-match matrix per seed: all 7 policies share 9 identical gameplay contexts, each with reversed budgets and both mirrored seats; not the full factorial across every possible factor.","Full competitive simulation after explicitly recorded trusted fixture initialization.","Bot behavior uses normalized InputFrames only; minimum observation delay 12 ticks, seeded input decisions, no opponent input reading. Common adaptive combat seed across policies isolates the selected policy changes.","Whiff telemetry covers completed melee actions separately from interrupted actions; projectile startups/contacts are reported separately.","Stun telemetry sums observed positive buildup; a same-tick dizzy reset can undercount inflicted stun.","Paid attempts count commands accepted or rejected by core transition checks, excluding motions entered while unactionable.","Wilson intervals describe independent bot-run win counts; paired-seat/seed correlations and human feel are not modeled.","No claim of human competitive balance; actual player agency/feel evaluation remains external."}},new JsonSerializerOptions{WriteIndented=true}));
return records.Any(r=>r.Aborted)?1:0;

static void Set(object instance,string property,int value)=>instance.GetType().GetProperty(property,BindingFlags.Public|BindingFlags.Instance)!.SetValue(instance,value);
public sealed record MatchRecord(string Id,int Seed,string Policy,int ActorSeat,string FighterA,string FighterB,string ArtA,string ArtB,int OpeningA,int OpeningB,string Stratum,string InitialHash,string FinalHash,int PlayedRounds,int Ticks,int ActorHalfPoints,int OpponentHalfPoints,int Verdict,bool Aborted,string Failure);
public sealed class RoundTelemetry(string match,int round,int[] opening,int[] tiers,int[] health)
{
    public string Match {get;}=match;public int Round {get;}=round;public int[] OpeningCredits {get;}=opening;public int[] OpeningRecoveryTiers {get;}=tiers;public int[] OpeningHealth {get;}=health;
    public int[] LeaseCosts {get;set;}=[0,0];public int[] PaidAttempts {get;}=[0,0];public int[] PaidAccepted {get;}=[0,0];public int[] PaidRejected {get;}=[0,0];public int[] CombatSpend {get;}=[0,0];public int[] Actions {get;}=[0,0];public int[] Hits {get;}=[0,0];public int[] Blocks {get;}=[0,0];public int[] Parries {get;}=[0,0];public int[] Damage {get;}=[0,0];
    public List<string>[] StartedMoves {get;}=[new(),new()];public HashSet<string>[] ContactMoves {get;}=[new(),new()];
    public int[] StunInflicted {get;}=[0,0];public int[] MeleeWhiffs {get;}=[0,0];public int[] InterruptedMelee {get;}=[0,0];public int[] ProjectileStartups {get;}=[0,0];
    readonly string[] activeMelee=["",""];readonly bool[] meleeContact=[false,false];
    public void TrackMelee(int seat,string move){activeMelee[seat]=move;meleeContact[seat]=false;}
    public void ContactMelee(int seat,string move){if(activeMelee[seat]==move)meleeContact[seat]=true;}
    public void EndMelee(int seat,bool interrupted){if(activeMelee[seat].Length>0&&!meleeContact[seat]){if(interrupted)InterruptedMelee[seat]++;else MeleeWhiffs[seat]++;}activeMelee[seat]="";}
    public int Winner {get;set;}public string Reason {get;set;}="";public PayoutReceipt[] Payouts {get;set;}=[];public int[] ClosingCredits {get;set;}=[];public string TerminalHash {get;set;}="";
}
public sealed class PolicyController
{
    readonly BotController baseline;readonly Queue<(byte Direction,Buttons Buttons)> commands=new();readonly int seat;readonly string policy;long lastConfirm=-1000,lastForced=-1000;
    public PolicyController(int seat,string policy,uint seed){this.seat=seat;this.policy=policy;baseline=new(seat,BotMode.Adaptive,seed);}
    public void Observe(CombatEvent e){if(e.Kind==CombatEventKind.Hit&&e.Seat==seat)lastConfirm=e.Tick;}
    public PreparationPlan Prepare(Simulation s)
    {
        var p=s.Players[seat];commands.Clear();bool decisive=s.Players.Any(x=>x.ScoreHalfPoints>=8)||s.RoundId==9;
        if(policy=="zero-spend-defense")return new([],p.Credits);
        int reserve=policy=="conservative-reserve"&&!decisive?Math.Min(900,p.Credits):0;
        if(policy!="lease-focused")return new([],reserve);
        var picked=new List<string>();int remaining=p.Credits,total=0;var slots=new HashSet<string>();
        foreach(var item in s.Content.Items.Values.Where(i=>i.EligibleFighters.Contains(p.FighterId)).OrderBy(i=>i.Price).ThenBy(i=>i.Id,StringComparer.Ordinal))
        {if(slots.Contains(item.Slot)||item.Price>remaining||total+item.Price>1800)continue;picked.Add(item.Id);slots.Add(item.Slot);remaining-=item.Price;total+=item.Price;}
        return new(picked.ToArray(),0);
    }
    public InputFrame Next(Simulation s)
    {
        var p=s.Players[seat];long tick=s.Tick;if(policy=="controlled-losing"&&s.RoundId<=2)return new(seat,tick,5,Buttons.None);
        if(commands.Count>0){var c=commands.Dequeue();return new(seat,tick,CoreMath.RelativeDirection(c.Direction,p.Facing),c.Buttons);}
        if(policy=="lease-focused"&&s.Phase==MatchPhase.Fight&&p.Actionable&&p.Leases.Count>0&&tick-lastForced>=120)
        {
            var item=s.Content.Items[p.Leases[(int)(tick/120%p.Leases.Count)]];var move=s.Content.Fighters[p.FighterId].Move(item.MoveId);lastForced=tick;
            if(move.Command=="6+HP")return new(seat,tick,CoreMath.RelativeDirection(6,p.Facing),Buttons.HP);
            if(move.Command=="HP+HK")return new(seat,tick,5,Buttons.HP|Buttons.HK);
            if(move.Command.StartsWith("qcb")){commands.Enqueue((2,Buttons.None));commands.Enqueue((1,Buttons.None));commands.Enqueue((4,move.Command.EndsWith("K")?Buttons.LK:Buttons.LP));}
            else if(move.Command.StartsWith("qcf")){commands.Enqueue((2,Buttons.None));commands.Enqueue((3,Buttons.None));commands.Enqueue((6,move.Command.EndsWith("K")?Buttons.LK:Buttons.LP));}
            if(commands.Count>0){var c=commands.Dequeue();return new(seat,tick,CoreMath.RelativeDirection(c.Direction,p.Facing),c.Buttons);}
        }
        int superCost=s.Content.Fighters[p.FighterId].SuperArts.Single(a=>a.Id==p.SelectedSuper).CreditCost;
        bool confirm=tick-lastConfirm>=12&&tick-lastConfirm<40;
        bool force=s.Phase==MatchPhase.Fight&&p.Actionable&&tick-lastForced>=120&&(policy=="force-ex"||policy=="bank-super"&&p.Credits>=superCost||policy=="spend-on-confirm"&&confirm);
        if(force)
        {
            lastForced=tick;
            if(policy=="bank-super"&&p.Credits>=superCost){foreach(byte d in new byte[]{2,3,6,2,3})commands.Enqueue((d,Buttons.None));commands.Enqueue((6,Buttons.HP));}
            else if(p.FighterId=="vale"){for(int n=0;n<46;n++)commands.Enqueue((1,Buttons.None));commands.Enqueue((6,Buttons.LP|Buttons.MP));}
            else{commands.Enqueue((2,Buttons.None));commands.Enqueue((3,Buttons.None));commands.Enqueue((6,Buttons.LP|Buttons.MP));}
            var c=commands.Dequeue();return new(seat,tick,CoreMath.RelativeDirection(c.Direction,p.Facing),c.Buttons);
        }
        var input=baseline.Next(s);
        if(policy=="bank-super"||policy=="spend-on-confirm"&&!confirm)
        {
            int punches=(byte)input.Held&7,kicks=(byte)input.Held&56;
            if((punches&(punches-1))!=0)punches&=-punches;if((kicks&(kicks-1))!=0)kicks&=-kicks;
            input=new(seat,tick,input.Direction,(Buttons)(punches|kicks));
        }
        if(policy=="spend-on-confirm"&&!confirm&&((byte)input.Held&7)!=0)
        {
            byte[] pattern=[2,3,6,2,3,6];int index=0;
            foreach(byte direction in p.InputHistory.Where(h=>h.Tick>=tick-30&&h.FacingEpoch==p.FacingEpoch).Select(h=>h.RelativeDirection).Append(CoreMath.RelativeDirection(input.Direction,p.Facing)))if(direction==pattern[index]&&++index==pattern.Length)break;
            if(index==pattern.Length)input=new(seat,tick,input.Direction,Buttons.None);
        }
        return input;
    }
}
