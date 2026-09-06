using StrikeLedger.Core;
namespace StrikeLedger.App;

public sealed record TrainingConfiguration
{
    public string Fighter0 {get;init;}="rook";public string Fighter1 {get;init;}="vale";
    public int Credits0 {get;init;}=3600;public int Credits1 {get;init;}=3600;
    public PreparationPlan Loadout0 {get;init;}=new([]);public PreparationPlan Loadout1 {get;init;}=new([]);
    public string StageId {get;init;}="grid";
    internal MatchConfig MatchConfig()=>new(){Fighter0=Fighter0,Fighter1=Fighter1,StageId=StageId,SessionId="training",Training=true};
}
public sealed partial class TrainingSession
{
    public TrainingConfiguration Configuration {get;private set;}
    public bool UseDrillFeed {get;set;}=true;
    readonly Queue<CommandInput> feed=new();
    bool comboOpen;string hitAction="";int hitOrdinal;
    sealed record EvaluatorCheckpoint(string[] Milestones,DrillFrame[] Recording,long StartTick,long LastHit,long LastParry,long LastKnockdown,int Parries,int InitialFacing,
        string LinkPending,string LastParryMove,int LastParryFrame,bool ComboOpen,string HitAction,int HitOrdinal,bool Success,bool Failed,string Feedback,bool UseFeed,CommandInput[] Feed,int DemoSeat,string DemoMove,CommandInput[] DemoInputs,string[] DemoBranches,bool DemoStarted);
    EvaluatorCheckpoint? checkpointEvaluator;
    public void Configure(TrainingConfiguration configuration)
    {
        var candidate=new TrainingSession(Simulation.Content,configuration);
        Configuration=candidate.Configuration;Simulation=candidate.Simulation;Reset(Drill.Id);
    }
    public static TrainingSession FromReplay(Simulation source,uint seed=1)
    {
        var branch=source.CreateTrainingBranch();var a=branch.Players[0];var b=branch.Players[1];
        var configuration=new TrainingConfiguration{Fighter0=a.FighterId,Fighter1=b.FighterId,Credits0=a.Credits,Credits1=b.Credits,Loadout0=new(a.OwnedProductIds.ToArray()),Loadout1=new(b.OwnedProductIds.ToArray()),StageId=branch.Config.StageId};
        var training=new TrainingSession(source.Content,configuration,seed){Simulation=branch};training.startTick=branch.Tick;training.Feedback="Detached replay practice. Reset uses this lineup and loadout; the competitive recording stays unchanged.";return training;
    }
    void ApplyConfiguredLoadouts()
    {
        Simulation.SetTrainingLoadout(0,Configuration.Loadout0);
        Simulation.SetTrainingLoadout(1,Configuration.Loadout1);
    }
    void ResetWorld()
    {
        Simulation.TrainingReset(Drill.Id is "eco_defense" or "owned_ex_zero"?0:Drill.Id=="insufficient_funds"?Simulation.Content.Economy.Cap:Configuration.Credits0);
        Simulation.SetTrainingState(0,x:330000);Simulation.SetTrainingState(1,x:Drill.Id is "high_low_air_parry" or "red_parry" or "multihit_parry" or "quickrise_reversal" or "eco_defense"?362000:386000,credits:Configuration.Credits1);
        ApplyConfiguredLoadouts();ConfigureDrillCapabilities();comboOpen=false;linkPendingMove="";hitAction="";feed.Clear();
    }
    void CaptureEvaluator()=>checkpointEvaluator=new(milestones.ToArray(),recording.ToArray(),startTick,lastHit,lastParry,lastKnockdown,parries,initialFacing,linkPendingMove,lastParryMove,lastParryActionFrame,comboOpen,hitAction,hitOrdinal,Success,Failed,Feedback,UseDrillFeed,feed.ToArray(),demonstrationSeat,demonstrationMove,demonstration.ToArray(),demonstratedBranches.ToArray(),demonstrationStarted);
    void RestoreEvaluator()
    {
        if(checkpointEvaluator is not {} c)return;
        milestones.Clear();milestones.UnionWith(c.Milestones);recording.Clear();recording.AddRange(c.Recording);
        startTick=c.StartTick;lastHit=c.LastHit;lastParry=c.LastParry;lastKnockdown=c.LastKnockdown;parries=c.Parries;initialFacing=c.InitialFacing;
        linkPendingMove=c.LinkPending;lastParryMove=c.LastParryMove;lastParryActionFrame=c.LastParryFrame;comboOpen=c.ComboOpen;hitAction=c.HitAction;hitOrdinal=c.HitOrdinal;
        Success=c.Success;Failed=c.Failed;Feedback=c.Feedback;UseDrillFeed=c.UseFeed;feed.Clear();foreach(var input in c.Feed)feed.Enqueue(input);demonstrationSeat=c.DemoSeat;demonstrationMove=c.DemoMove;demonstrationStarted=c.DemoStarted;demonstration.Clear();foreach(var input in c.DemoInputs)demonstration.Enqueue(input);demonstratedBranches.Clear();demonstratedBranches.UnionWith(c.DemoBranches);
    }
    void ConfigureDrillCapabilities()
    {
        var content=Simulation.Content;
        void Equip(int seat,string slot,string? moveId=null)
        {
            var actor=Simulation.Players[seat];if(actor.OwnedProductIds.Any(id=>moveId==null?content.Items[id].Slot==slot:content.Items[id].MoveId==moveId))return;
            var item=content.Items.Values.Where(i=>i.EligibleFighters.Contains(actor.FighterId)&&i.Slot==slot&&(moveId==null||i.MoveId==moveId))
                .OrderBy(i=>slot=="ex"&&content.Fighters[actor.FighterId].Move(i.MoveId).Projectile!=null?0:1).ThenBy(i=>i.Id,StringComparer.Ordinal).First();
            var ids=actor.OwnedProductIds.Where(id=>slot!="super"||content.Items[id].Slot!="super").Append(item.Id).ToArray();var quote=content.QuotePreparation(actor.FighterId,content.Economy.Cap,new(ids));
            if(!quote.Valid)throw new InvalidOperationException("This drill needs room for its "+slot+" fixture: "+quote.Error);
            Simulation.SetTrainingLoadout(seat,new(ids));
        }
        if(Drill.Id is "insufficient_funds" or "eco_defense")Simulation.SetTrainingLoadout(0,new([]));
        if(Drill.Id is "same_tick_ex" or "owned_ex_zero" or "rollback_spend")Equip(0,"ex");
        if(Drill.Id is "hitconfirm_super" or "super_one_use")Equip(0,"super");
        if(Drill.Id is "red_parry" or "multihit_parry" or "eco_defense")Equip(1,"super","super_1");
    }
    InputFrame DrillInput()
    {
        long tick=Simulation.Tick;var p=Simulation.Players[1];var content=Simulation.Content;
        InputFrame Neutral()=>new(1,tick,5,Buttons.None);
        if(feed.Count>0){var sample=feed.Dequeue();return new(1,tick,sample.Direction,sample.Held);}
        long age=tick-startTick;int cycle=(int)(age%240);if(cycle!=60||!p.Actionable)return Drill.Dummy is BotMode.Passive?Neutral():Drill.Id is "jumpin_antiair" or "anti_air_reward" or "throw_tech"?dummy.Next(Simulation):Neutral();
        var fighter=content.Fighters[p.FighterId];string? move=Drill.Id switch
        {
            "high_low_air_parry"=>((age/240)%3) switch{0=>"s_lp",1=>"c_lk",_=>"j_lp"},
            "red_parry" or "multihit_parry" or "eco_defense"=>fighter.SuperArts.FirstOrDefault(a=>a.Id==p.SelectedSuper)?.MoveId,
            "counter_hit_reward" or "perfect_parry_reward"=>"s_hp",
            "quickrise_reversal"=>fighter.Moves.FirstOrDefault(m=>m.Availability=="base"&&m.Hitboxes.Any(h=>h.Knockdown=="soft"))?.Id,
            _=>null
        };
        if(move==null)return Neutral();
        var definition=fighter.Move(move);
        if(definition.Kind=="super"&&p.SuperUsesRemaining==0)
        {if(Simulation.Projectiles.Any(o=>o.Owner==1))return Neutral();Simulation.SetTrainingLoadout(1,new(p.OwnedProductIds.ToArray()));}
        if(move.StartsWith("j_",StringComparison.Ordinal)){feed.Enqueue(new(8,Buttons.None));for(int i=0;i<12;i++)feed.Enqueue(new(5,Buttons.None));}
        foreach(var input in CommandEncoder.Encode(content,definition,p.Facing))feed.Enqueue(input);
        var next=feed.Dequeue();return new(1,tick,next.Direction,next.Held);
    }
}
