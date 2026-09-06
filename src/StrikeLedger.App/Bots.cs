using StrikeLedger.Core;

namespace StrikeLedger.App;

public enum BotMode { Passive, Guard, Jump, Throw, Conservative, Adaptive, Balanced=Adaptive, PerfectParryTraining, PokeTraining, ProjectileTraining }
public sealed record BotObservation(long Tick,int Distance,int Facing,int OpponentY,string OpponentAction,int Health,int OpponentHealth,int BankCredits,string Fighter)
{
    public string SelectedSuper {get;init;}="";
    public string[] OwnedEx {get;init;}=[];public int SuperUsesRemaining {get;init;}
    public string[] OpponentOwnedEx {get;init;}=[];public int OpponentSuperUses {get;init;}
    public int OpponentSkillCredits {get;init;}
    public string[] Leases {get;init;}=[];
    public string[] OpponentLeases {get;init;}=[];
    public string OpponentSuper {get;init;}="";
    public int OpponentCredits {get;init;}
    public string OpponentFighter {get;init;}="vale";
    public bool Actionable {get;init;}
}
public sealed record BotSnapshot(uint RandomState,int ThinkCooldown,BotObservation[] Observations,(byte Direction,Buttons Buttons)[] Queue,long LastTick,InputFrame LastInput,BotMode Mode,long OriginTick);
public sealed class BotController
{
    readonly Queue<BotObservation> observations=new();
    readonly Queue<(byte Direction,Buttons Buttons)> commands=new();
    uint rng;
    int cooldown;
    long lastTick=-1,originTick=-1;
    InputFrame lastInput;
    public int Seat {get;}
    public BotMode Mode {get;set;}
    public int ReactionTicks {get;}=12;
    public BotController(int seat,BotMode mode=BotMode.Adaptive,uint seed=1)
    {if(seat is <0 or >1)throw new ArgumentOutOfRangeException(nameof(seat));Seat=seat;Mode=mode;rng=seed==0?1:seed;}
    uint Random(){rng^=rng<<13;rng^=rng>>17;rng^=rng<<5;return rng;}
    public BotSnapshot Capture()=>new(rng,cooldown,observations.ToArray(),commands.ToArray(),lastTick,lastInput,Mode,originTick);
    public void Restore(BotSnapshot snapshot){rng=snapshot.RandomState;cooldown=snapshot.ThinkCooldown;lastTick=snapshot.LastTick;lastInput=snapshot.LastInput;Mode=snapshot.Mode;originTick=snapshot.OriginTick;observations.Clear();commands.Clear();foreach(var o in snapshot.Observations)observations.Enqueue(o);foreach(var c in snapshot.Queue)commands.Enqueue(c);}
    public InputFrame Next(Simulation simulation)
    {
        var me=simulation.Players[Seat];var other=simulation.Players[1-Seat];long tick=simulation.Tick;
        if(tick==lastTick)return lastInput;
        if(originTick<0)originTick=tick;
        observations.Enqueue(new(tick,Math.Abs(me.X-other.X),me.Facing,other.Y,other.ActionId,me.Health,other.Health,me.Credits,me.FighterId)
        {SelectedSuper=me.SelectedSuper,Leases=me.OwnedProductIds.ToArray(),OpponentLeases=other.OwnedProductIds.ToArray(),OwnedEx=me.OwnedEx.ToArray(),SuperUsesRemaining=me.SuperUsesRemaining,OpponentOwnedEx=other.OwnedEx.ToArray(),OpponentSuperUses=other.SuperUsesRemaining,OpponentSkillCredits=other.PendingSkillCredits,OpponentSuper=other.SelectedSuper,OpponentCredits=other.Credits,OpponentFighter=other.FighterId,Actionable=me.Actionable});
        while(observations.Count>ReactionTicks+1)observations.Dequeue();
        InputFrame Frame(byte direction=5,Buttons buttons=Buttons.None){lastTick=tick;return lastInput=new(Seat,tick,direction,buttons);}
        if(simulation.Phase!=MatchPhase.Fight){commands.Clear();return Frame();}
        if(Mode==BotMode.Passive)return Frame();
        if(Mode==BotMode.Guard)return Frame((byte)(me.Facing==1?1:3));
        if(Mode==BotMode.Jump)return Frame(8);
        if(Mode==BotMode.Throw)return (tick-originTick)%36==0?Frame(5,Buttons.LP|Buttons.LK):Frame((byte)(me.Facing==1?6:4));
        if(Mode==BotMode.PerfectParryTraining)
        {if(!simulation.Config.Training)throw new InvalidOperationException("Perfect parry dummy is training-only");return (tick-originTick)%3==0?Frame((byte)(me.Facing==1?6:4)):Frame();}
        if(Mode is BotMode.PokeTraining or BotMode.ProjectileTraining)
        {
            if(!simulation.Config.Training)throw new InvalidOperationException("Scripted counter dummy is training-only");
            if(commands.Count==0&&me.Actionable&&(tick-originTick)%90==12)
            {
                var practiceFighter=simulation.Content.Fighters[me.FighterId];var move=Mode==BotMode.ProjectileTraining?practiceFighter.Moves.FirstOrDefault(m=>m.Availability=="base"&&m.CreditCost==0&&m.Projectile!=null):practiceFighter.Move("s_hp");
                if(move!=null)foreach(var input in CommandEncoder.Encode(simulation.Content,move,me.Facing))commands.Enqueue((input.Direction,input.Held));
            }
            if(commands.Count>0){var sample=commands.Dequeue();return Frame(sample.Direction,sample.Buttons);}return Frame();
        }
        if(commands.Count>0){var c=commands.Dequeue();return Frame(c.Direction,c.Buttons);}
        if(cooldown-->0)return Frame();
        if(observations.Count<=ReactionTicks)return Frame();
        var seen=observations.Peek();byte f=(byte)(seen.Facing==1?6:4),b=(byte)(seen.Facing==1?4:6);
        byte Relative(byte direction)=>CoreMath.RelativeDirection(direction,seen.Facing);
        uint roll=Random()%100;
        var definition=simulation.Content.Fighters[seen.Fighter];
        var selected=definition.SuperArts.FirstOrDefault(a=>a.Id==seen.SelectedSuper);
        InputFrame QueueMove(MoveDefinition move)
        {
            foreach(var input in CommandEncoder.Encode(simulation.Content,move,seen.Facing))commands.Enqueue((input.Direction,input.Held));
            cooldown=Math.Max(12,move.TotalTicks/2);var first=commands.Dequeue();return Frame(first.Direction,first.Buttons);
        }
        bool throwThreat=seen.OpponentLeases.Any(id=>simulation.Content.Items.TryGetValue(id,out var item)&&simulation.Content.Fighters[seen.OpponentFighter].Move(item.MoveId).Throw is not null);
        if(throwThreat&&seen.Distance<38000&&roll<24){cooldown=18;return Frame(Relative(9));}
        int defendChance=Mode==BotMode.Conservative?70:seen.OpponentSuperUses>0?50:42;
        if(seen.OpponentAction.Length>0 && roll<defendChance){cooldown=8;return Frame(seen.OpponentY>0?b:Relative(1));}
        if(seen.Actionable&&selected!=null&&seen.SuperUsesRemaining>0&&roll<12)return QueueMove(definition.Move(selected.MoveId));
        if(seen.Actionable&&seen.Leases.Length>0&&roll<38)
        {
            var options=seen.Leases.Where(id=>simulation.Content.Items[id].Slot is not("ex" or "super")).Select(id=>definition.Move(simulation.Content.Items[id].MoveId))
                .Where(m=>SupportsRental(m)).Where(m=>m.Throw is not null?seen.Distance<42000:m.Projectile is not null?seen.Distance>65000:seen.Distance<100000)
                .ToArray();
            if(options.Length>0)return QueueMove(options[(int)(Random()%(uint)options.Length)]);
        }
        if(seen.Distance>140000){cooldown=3;return Frame(f);}
        if(seen.Distance>75000 && roll<28){cooldown=15;return Frame(Relative(9));}
        if(seen.Actionable&&seen.OwnedEx.Length>0&&roll<34)
        {
            var options=seen.OwnedEx.Select(definition.Move).Where(m=>m.Projectile!=null?seen.Distance>65000:seen.Distance<100000).ToArray();
            if(options.Length>0)return QueueMove(options[(int)(Random()%(uint)options.Length)]);
        }
        if(seen.Fighter=="rook"&&roll<25)return QueueMove(definition.Moves.Single(m=>m.Command=="qcf+HP"));
        if(seen.Fighter=="vale"&&roll<24)return QueueMove(definition.Moves.Single(m=>m.Command=="charge_back+HP"));
        if(seen.Distance<46000 && roll<42){cooldown=22;return Frame(5,Buttons.LP|Buttons.LK);}
        if(seen.Distance>65000){cooldown=2;return Frame(f);}
        cooldown=(Mode==BotMode.Conservative?20:12)+(int)(Random()%12);
        Buttons button=(Random()%6) switch{0=>Buttons.LP,1=>Buttons.MP,2=>Buttons.HP,3=>Buttons.LK,4=>Buttons.MK,_=>Buttons.HK};
        return Frame(roll<50?(byte)2:(byte)5,button);
    }
    public static bool SupportsRental(MoveDefinition move)=>move.DerivedFrom.Length==0&&!move.Command.StartsWith("air:",StringComparison.Ordinal)&&!move.Command.StartsWith("J+",StringComparison.Ordinal)&&(move.Hitboxes.Length>0||move.Projectile is not null||move.Throw is not null);
    public PreparationPlan ChoosePreparation(Simulation simulation)
    {
        var me=simulation.Players[Seat];var definition=simulation.Content.Fighters[me.FighterId];
        var options=simulation.Content.Items.Values.Where(i=>i.EligibleFighters.Contains(me.FighterId))
            .Where(i=>i.Slot=="super"||SupportsRental(definition.Move(i.MoveId))).OrderBy(i=>i.Id,StringComparer.Ordinal).ToArray();
        var plan=new List<string>();int bank=me.Credits;
        // Delayed public history adjusts a bounded purchase priority; it never exposes future inputs.
        var seen=observations.LastOrDefault();bool sawJump=observations.Count(o=>o.OpponentY>0)>3;
        var ranked=options.OrderBy(i=>i.Slot=="ex"?0:i.Slot=="super"?1:2)
            .ThenBy(i=>sawJump&&definition.Move(i.MoveId).Hitboxes.Any(h=>h.Y>50000)?0:1)
            .ThenBy(i=>(Array.IndexOf(options,i)+simulation.RoundId+Seat)%Math.Max(1,options.Length)).ToArray();
        foreach(var item in ranked)
        {
            var candidate=plan.Append(item.Id).ToArray();var quote=PurchasePlanning.Preview(simulation.Content,me.FighterId,bank,candidate);
            if(!quote.Valid)continue;
            if(Mode==BotMode.Conservative&&quote.Remaining<Math.Min(600,bank)&&plan.Count>0)continue;
            plan.Add(item.Id);if(plan.Count>=6)break;
        }
        return PurchasePlanning.QuotedPlan(simulation.Content,me.FighterId,bank,plan);
    }
}
