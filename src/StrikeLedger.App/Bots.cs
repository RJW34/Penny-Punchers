using StrikeLedger.Core;

namespace StrikeLedger.App;

public enum BotMode { Passive, Guard, Jump, Throw, Conservative, Adaptive, Balanced=Adaptive, PerfectParryTraining }
public sealed record BotObservation(long Tick,int Distance,int Facing,int OpponentY,string OpponentAction,int Health,int OpponentHealth,int Credits,int Reserve,string Fighter);
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
        observations.Enqueue(new(tick,Math.Abs(me.X-other.X),me.Facing,other.Y,other.ActionId,me.Health,other.Health,me.Credits,me.ReserveFloor,me.FighterId));
        while(observations.Count>ReactionTicks+1)observations.Dequeue();
        InputFrame Frame(byte direction=5,Buttons buttons=Buttons.None){lastTick=tick;return lastInput=new(Seat,tick,direction,buttons);}
        if(simulation.Phase!=MatchPhase.Fight){commands.Clear();return Frame();}
        if(Mode==BotMode.Passive)return Frame();
        if(Mode==BotMode.Guard)return Frame((byte)(me.Facing==1?1:3));
        if(Mode==BotMode.Jump)return Frame(8);
        if(Mode==BotMode.Throw)return (tick-originTick)%36==0?Frame(5,Buttons.LP|Buttons.LK):Frame((byte)(me.Facing==1?6:4));
        if(Mode==BotMode.PerfectParryTraining)
        {if(!simulation.Config.Training)throw new InvalidOperationException("Perfect parry dummy is training-only");return (tick-originTick)%3==0?Frame((byte)(me.Facing==1?6:4)):Frame();}
        if(commands.Count>0){var c=commands.Dequeue();return Frame(c.Direction,c.Buttons);}
        if(cooldown-->0)return Frame();
        if(observations.Count<=ReactionTicks)return Frame();
        var seen=observations.Peek();byte f=(byte)(seen.Facing==1?6:4),b=(byte)(seen.Facing==1?4:6);
        byte Relative(byte direction)=>CoreMath.RelativeDirection(direction,seen.Facing);
        uint roll=Random()%100;
        if(seen.OpponentAction.Length>0 && roll< (Mode==BotMode.Conservative?70:42)){cooldown=8;return Frame(seen.OpponentY>0?b:Relative(1));}
        if(seen.Distance>140000){cooldown=3;return Frame(f);}
        if(seen.Distance>75000 && roll<28){cooldown=15;return Frame(Relative(9));}
        bool canEx=seen.Credits-seen.Reserve>=300;
        bool canSuper=seen.Credits-seen.Reserve>=1500;
        if(seen.Fighter=="rook" && roll<25)
        {
            if(canSuper && roll<7){foreach(byte d in new byte[]{2,3,6,2,3})commands.Enqueue((Relative(d),Buttons.None));commands.Enqueue((f,Buttons.HP));}
            else{commands.Enqueue((Relative(3),Buttons.None));commands.Enqueue((f,canEx&&roll<15?Buttons.LP|Buttons.MP:Buttons.HP));}
            cooldown=22;return Frame(2);
        }
        if(seen.Fighter=="vale" && roll<24)
        {for(int n=0;n<46;n++)commands.Enqueue((Relative(1),Buttons.None));commands.Enqueue((f,canEx?Buttons.LP|Buttons.MP:Buttons.HP));cooldown=18;return Frame(Relative(1));}
        if(seen.Distance<46000 && roll<42){cooldown=22;return Frame(5,Buttons.LP|Buttons.LK);}
        if(seen.Distance>65000){cooldown=2;return Frame(f);}
        cooldown=(Mode==BotMode.Conservative?20:12)+(int)(Random()%12);
        Buttons button=(Random()%6) switch{0=>Buttons.LP,1=>Buttons.MP,2=>Buttons.HP,3=>Buttons.LK,4=>Buttons.MK,_=>Buttons.HK};
        return Frame(roll<50?(byte)2:(byte)5,button);
    }
    public PreparationPlan ChoosePreparation(Simulation simulation)
    {
        var me=simulation.Players[Seat];int available=me.Credits;
        // Reserve keeps two EX attempts available; last-match rounds spend more freely.
        int reserve=simulation.Players.Any(p=>p.ScoreHalfPoints>=8)?0:Math.Min(600,available);
        string id=me.FighterId=="rook"?"rook_high_hook":"vale_heel_arc";
        var item=simulation.Content.Items.Values.FirstOrDefault(i=>i.Id==id);
        return item is not null && available-item.Price>=reserve?new([id],reserve):new([],reserve);
    }
}
