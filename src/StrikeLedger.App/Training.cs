using System.Text.Json;
using StrikeLedger.Core;

namespace StrikeLedger.App;

public sealed record DrillDefinition(string Id,string Title,string Instructions,BotMode Dummy);
public sealed record DrillFrame(long Tick,byte Direction0,Buttons Buttons0,byte Direction1,Buttons Buttons1,string Hash,CombatEvent[] Events);
public sealed class TrainingSession
{
    public static readonly DrillDefinition[] Drills=
    [
        new("motion_both_facings","Motion, both sides","Execute a free motion special facing right, swap sides, then execute one facing left.",BotMode.Passive),
        new("charge_side_switch","Charge and cross-up","Hold back for 45 ticks. Swap sides: horizontal charge must reset. Build charge and execute a charge special.",BotMode.Passive),
        new("same_tick_ex","EX chord","Enter a special motion and press two punches or two kicks on the same tick. A paid EX startup must occur.",BotMode.Passive),
        new("link_cancel","Link versus cancel","Hit with a normal, then connect another normal after recovery. Also cancel a contacted normal into a free special.",BotMode.Passive),
        new("target_combo","Target combo","Connect the fighter's defined target-combo sequence during its cancel window.",BotMode.Passive),
        new("high_low_air_parry","High, low and air parry","Tap forward against a high/mid strike; down against a low; then parry while airborne. Each type must connect.",BotMode.Conservative),
        new("red_parry","Red parry","Block the first hit, return to neutral, then tap the matching parry direction during blockstun for the next hit.",BotMode.Conservative),
        new("multihit_parry","Multihit parry","Parry two distinct hits in one attacking action; one tap cannot protect every hit.",BotMode.Conservative),
        new("jumpin_antiair","Jump-in anti-air","Interrupt the jumping dummy with an anti-air strike while you remain grounded.",BotMode.Jump),
        new("throw_tech","Throw tech","Press LP + LK inside the opponent's normal-throw tech window.",BotMode.Throw),
        new("kara_throw","Kara throw","Start forward HP and cancel into LP + LK during the first two frames, before contact.",BotMode.Passive),
        new("quickrise_reversal","Quick rise and reversal","After a soft knockdown, quick rise and use a reversal buffered in the final two recovery ticks.",BotMode.Conservative),
        new("hitconfirm_super","Confirm into paid super","Hit-confirm a normal or special into the match-selected super. The full credit cost debits on startup.",BotMode.Passive),
        new("insufficient_funds","Insufficient funds","Set credits to zero and enter a valid paid motion/chord. It must be rejected, with no fallback attack or debit.",BotMode.Passive),
        new("eco_defense","Eco versus funded","At zero credits, block or parry the funded opponent's offense, then hit with a free normal or special.",BotMode.Conservative),
        new("rollback_spend","Rollback spend trace","Use an EX, save a checkpoint, advance, then restore it. Wallet, receipts and input history must restore together.",BotMode.Passive)
    ];
    readonly HashSet<string> milestones=new(StringComparer.Ordinal);
    readonly List<DrillFrame> recording=new();
    readonly BotController dummy;
    readonly BotSnapshot initialDummy;
    BotSnapshot? savedDummy;
    (byte Direction,Buttons Buttons)[] savedDummyInputs=[];
    int savedDummyIndex;
    bool savedRecordingDummy,savedPlayingDummy;
    readonly List<(byte Direction,Buttons Buttons)> dummyInputs=new();
    int dummyPlaybackIndex;
    public bool RecordingDummy {get;private set;}
    public bool PlayingDummy {get;private set;}
    public BotMode DummyMode {get=>dummy.Mode;set=>dummy.Mode=value;}
    SimulationSnapshot? saved;
    string savedHash="";
    long startTick,lastHit=-100,lastParry=-100,lastKnockdown=-100;
    int parries,initialFacing;
    string linkPendingMove="",lastParryMove="";
    int lastParryActionFrame=-1;
    public Simulation Simulation {get;}
    public DrillDefinition Drill {get;private set;}=Drills[0];
    public bool Paused {get;set;}
    public double Speed {get;set;}=1;
    public bool ShowHitboxes {get;set;}=true;
    public bool Success {get;private set;}
    public bool Failed {get;private set;}
    public string Feedback {get;private set;}="Follow the drill instructions.";
    public IReadOnlyList<DrillFrame> Recording=>recording;
    public TrainingSession(GameContent content,string fighter="rook",string super="art_1",uint seed=1)
    {
        Simulation=new(content,new MatchConfig{Fighter0=fighter,Fighter1=fighter=="rook"?"vale":"rook",Super0=super,Training=true,StageId="grid",SessionId="training"});
        dummy=new(1,BotMode.Passive,seed);initialDummy=dummy.Capture();Reset(Drills[0].Id);
    }
    public void Reset(string? drillId=null)
    {
        if(drillId is not null)Drill=Drills.FirstOrDefault(d=>d.Id==drillId)??throw new ArgumentException("Unknown training drill");
        int credits=Drill.Id is "insufficient_funds" or "eco_defense"?0:3600;
        Simulation.TrainingReset(credits);Simulation.SetTrainingState(0,x:330000);Simulation.SetTrainingState(1,x:386000,credits:3600);
        dummy.Restore(initialDummy);dummy.Mode=Drill.Dummy;dummyInputs.Clear();dummyPlaybackIndex=0;RecordingDummy=false;PlayingDummy=false;
        milestones.Clear();recording.Clear();saved=null;savedDummy=null;savedHash="";startTick=Simulation.Tick;lastHit=lastParry=lastKnockdown=-100;parries=0;initialFacing=Simulation.Players[0].Facing;
        linkPendingMove="";lastParryMove="";lastParryActionFrame=-1;Success=false;Failed=false;Feedback=Drill.Instructions;
        if(Drill.Id=="charge_side_switch" && Simulation.Players[0].FighterId!="vale")Feedback="This charge drill requires Vale. Select Vale to practice charge and facing reset.";
    }
    public StepResult FrameAdvance(byte direction,Buttons buttons)
    {
        if(Simulation.Phase==MatchPhase.PendingResult)Simulation.TrainingReset(Simulation.Players[0].Credits);
        var input=new InputFrame(0,Simulation.Tick,RecordingDummy?(byte)5:direction,RecordingDummy?Buttons.None:buttons);var other=dummy.Next(Simulation);
        if(RecordingDummy)
        {if(dummyInputs.Count<3600)dummyInputs.Add((direction,buttons));else RecordingDummy=false;other=new(1,Simulation.Tick,direction,buttons);}
        else if(PlayingDummy && dummyInputs.Count>0)
        {var d=dummyInputs[dummyPlaybackIndex++%dummyInputs.Count];other=new(1,Simulation.Tick,d.Direction,d.Buttons);}
        string beforeAction=Simulation.Players[0].ActionId;int beforeFrame=Simulation.Players[0].ActionFrame;bool beforeContact=Simulation.Players[0].Contact;
        var result=Simulation.Step(input,other);
        if(recording.Count<7200)recording.Add(new(input.Frame,input.Direction,input.Held,other.Direction,other.Held,Simulation.Hash(),result.Events.ToArray()));
        Evaluate(result.Events,beforeAction,beforeFrame,beforeContact);return result;
    }
    public void SwapSides()
    {
        int a=Simulation.Players[0].X,b=Simulation.Players[1].X;bool charged=Simulation.Players[0].BackCharge>=45;
        Simulation.SetTrainingState(0,x:b);Simulation.SetTrainingState(1,x:a);
        if(charged)milestones.Add("charged-before-cross");
    }
    public void BeginDummyRecording(){dummyInputs.Clear();RecordingDummy=true;PlayingDummy=false;dummyPlaybackIndex=0;Feedback="Recording dummy input. Your controls now operate seat 2.";}
    public void EndDummyRecording(){RecordingDummy=false;Feedback=$"Recorded {dummyInputs.Count} dummy frames.";}
    public void PlayDummyRecording(){RecordingDummy=false;PlayingDummy=dummyInputs.Count>0;dummyPlaybackIndex=0;Feedback=PlayingDummy?"Dummy recording loops from frame zero.":"Record dummy input first.";}
    public void StopDummyPlayback(){PlayingDummy=false;dummyPlaybackIndex=0;}
    public void SaveCheckpoint(){saved=Simulation.Capture();savedHash=Simulation.Hash();savedDummy=dummy.Capture();savedDummyInputs=dummyInputs.ToArray();savedDummyIndex=dummyPlaybackIndex;savedRecordingDummy=RecordingDummy;savedPlayingDummy=PlayingDummy;Feedback="Checkpoint saved: world, wallet, receipts, input history and dummy state.";}
    public void RestoreCheckpoint()
    {
        if(saved is null){Feedback="Save a checkpoint first.";return;}
        Simulation.Restore(saved);
        if(savedDummy is not null)dummy.Restore(savedDummy);dummyInputs.Clear();dummyInputs.AddRange(savedDummyInputs);dummyPlaybackIndex=savedDummyIndex;RecordingDummy=savedRecordingDummy;PlayingDummy=savedPlayingDummy;
        if(Simulation.Hash()!=savedHash)throw new InvalidOperationException("Training checkpoint did not restore canonical state");
        if(Drill.Id=="rollback_spend" && Simulation.Players[0].SpendReceipts.Count>0){Success=true;Feedback="PASS — checkpoint restored wallet, spend receipts and input history exactly.";}
    }
    public void ExportRecording(string path)
    {
        var output=new{trainingOnly=true,contentHash=Simulation.Content.ContentHash,drill=Drill.Id,success=Success,failed=Failed,frames=recording};
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);File.WriteAllText(path,JsonSerializer.Serialize(output));
    }
    void Evaluate(IReadOnlyList<CombatEvent> events,string beforeAction,int beforeFrame,bool beforeContact)
    {
        var p=Simulation.Players[0];long tick=Simulation.Tick;
        if(Drill.Id=="charge_side_switch" && milestones.Contains("charged-before-cross") && p.Facing!=initialFacing && p.BackCharge<45)milestones.Add("charge-cleared");
        foreach(var e in events)
        {
            if(e.Kind==CombatEventKind.Knockdown && e.Seat==0)lastKnockdown=tick;
            if(e.Kind==CombatEventKind.ThrowTech && (e.Seat==0||e.Target==0))milestones.Add("tech");
            if(e.Seat!=0)continue;
            var move=Simulation.Content.Fighters[p.FighterId].Moves.FirstOrDefault(m=>m.Id==e.MoveId);
            if(e.Kind==CombatEventKind.ActionStarted)
            {
                if(move?.Kind=="special" && (move.Command.StartsWith("qcf")||move.Command.StartsWith("qcb")||move.Command.StartsWith("dp")))milestones.Add(p.Facing==1?"right-motion":"left-motion");
                if(move?.Kind=="special" && p.FighterId=="vale" && move.Command.StartsWith("charge_"))milestones.Add("charge-special");
                if(move?.Kind=="ex_special")milestones.Add("ex");
                if(move?.Kind=="special" && beforeAction.Length>0 && beforeContact)milestones.Add("cancel");
                if(move?.Kind=="normal" && beforeAction.Length==0 && tick-lastHit<=40)linkPendingMove=e.MoveId;
                if(Simulation.Content.Fighters[p.FighterId].TargetCombos.Any(t=>t.From==beforeAction && t.To==e.MoveId && beforeFrame>=t.Start && beforeFrame<t.End && (!t.RequiresContact||beforeContact)))milestones.Add("target");
                if(move?.Throw is not null && beforeAction=="command_fhp" && beforeFrame<2 && !beforeContact)milestones.Add("kara");
                if(move?.Kind=="super" && beforeAction.Length>0 && beforeContact && tick-lastHit<=30 && p.SpendReceipts.Any(r=>r.MoveId==move.Id))milestones.Add("confirm-super");
                if(e.Detail=="reversal" && milestones.Contains("quick-rise"))milestones.Add("rise-reversal");
            }
            if(e.Kind==CombatEventKind.Hit)
            {lastHit=tick;if(e.MoveId==linkPendingMove){milestones.Add("link");linkPendingMove="";}if(p.Grounded && Simulation.Players[1].Y>0)milestones.Add("antiair");if(p.Credits==0 && move?.CreditCost==0)milestones.Add("eco-hit");}
            if(e.Kind==CombatEventKind.Block)milestones.Add("defended");
            if(e.Kind==CombatEventKind.Parry)
            {
                milestones.Add("defended");string detail=e.Detail.ToLowerInvariant();
                if(detail.Contains("red"))milestones.Add("red");if(detail.Contains("low"))milestones.Add("low");else if(!p.Grounded||detail.Contains("air"))milestones.Add("air");else milestones.Add("high");
                int enemyFrame=Simulation.Players[1].ActionFrame;
                parries=tick-lastParry<90 && e.MoveId==lastParryMove && enemyFrame>lastParryActionFrame?parries+1:1;lastParry=tick;lastParryMove=e.MoveId;lastParryActionFrame=enemyFrame;if(parries>=2)milestones.Add("multihit");
            }
            if(e.Kind==CombatEventKind.Land && e.Detail=="quick-rise" && lastKnockdown>=0)milestones.Add("quick-rise");
            if(e.Kind==CombatEventKind.Rejected && p.Credits==0 && p.SpendReceipts.Count==0 && p.ActionId.Length==0)milestones.Add("denied");
        }
        string[] required=Drill.Id switch
        {
            "motion_both_facings"=>["right-motion","left-motion"],"charge_side_switch"=>["charge-cleared","charge-special"],"same_tick_ex"=>["ex"],"link_cancel"=>["link","cancel"],"target_combo"=>["target"],"high_low_air_parry"=>["high","low","air"],"red_parry"=>["red"],"multihit_parry"=>["multihit"],"jumpin_antiair"=>["antiair"],"throw_tech"=>["tech"],"kara_throw"=>["kara"],"quickrise_reversal"=>["rise-reversal"],"hitconfirm_super"=>["confirm-super"],"insufficient_funds"=>["denied"],"eco_defense"=>["defended","eco-hit"],_=>["checkpoint"]
        };
        if(required.All(milestones.Contains)){Success=true;Failed=false;Feedback="PASS — "+Drill.Title;}
        else if(tick-startTick>7200){Failed=true;Feedback="Drill timed out. Reset and try again. Completed: "+string.Join(", ",milestones);}
        else if(milestones.Count>0)Feedback="Progress: "+string.Join(", ",milestones)+". Remaining: "+string.Join(", ",required.Where(r=>!milestones.Contains(r)));
    }
}
