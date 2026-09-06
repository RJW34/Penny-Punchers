using StrikeLedger.Core;
namespace StrikeLedger.App;
public sealed partial class TrainingSession
{
    readonly Queue<CommandInput> demonstration=new();readonly HashSet<string> demonstratedBranches=new(StringComparer.Ordinal);
    int demonstrationSeat;string demonstrationMove="";bool demonstrationStarted;
    public bool Demonstrating=>demonstrationMove.Length>0;
    public string DemonstrationMove=>demonstrationMove;
    public void BeginDemonstration(int seat,string moveId,bool reset=true)
    {
        if(seat is <0 or >1)throw new ArgumentOutOfRangeException(nameof(seat));var definition=Simulation.Content.Fighters[Simulation.Players[seat].FighterId];var move=definition.Move(moveId);
        if(move.DerivedFrom.Length>0)throw new ArgumentException("Demonstrate the branch entry action first");
        var mode=dummy.Mode;if(reset)Reset(Drill.Id);dummy.Mode=mode;UseDrillFeed=false;RecordingDummy=false;PlayingDummy=false;demonstration.Clear();demonstratedBranches.Clear();demonstrationSeat=seat;demonstrationMove=moveId;demonstrationStarted=false;
        int facing=Simulation.Players[seat].Facing;
        if(move.Command.StartsWith("J+",StringComparison.Ordinal)||move.Command.StartsWith("air:",StringComparison.Ordinal))
        {demonstration.Enqueue(new(CoreMath.RelativeDirection(9,facing),Buttons.None));for(int n=0;n<definition.Physics.JumpStartTicks+8;n++)demonstration.Enqueue(new(5,Buttons.None));}
        foreach(var input in CommandEncoder.Encode(Simulation.Content,move,facing))demonstration.Enqueue(input);
        Feedback="Input demonstration: "+move.Name+" / "+move.Command+". Same legal parser: licenses and prepaid use apply; bank stays frozen.";
    }
    void ResetDemonstration(){demonstration.Clear();demonstratedBranches.Clear();demonstrationMove="";demonstrationStarted=false;}
    void ObserveDemonstration(StepResult result)
    {
        if(!Demonstrating)return;
        if(result.Events.Any(e=>e.Seat==demonstrationSeat&&e.Kind==CombatEventKind.ActionStarted&&e.MoveId==demonstrationMove))demonstrationStarted=true;
        if(demonstration.Count==0&&!demonstrationStarted)
        {
            var rejection=result.Events.FirstOrDefault(e=>e.Seat==demonstrationSeat&&e.Kind==CombatEventKind.Rejected);
            if(rejection!=null){Feedback="Demonstration rejected by ordinary rules: "+rejection.Detail;ResetDemonstration();}
        }
    }
    void ApplyDemonstration(ref InputFrame input,ref InputFrame opponent)
    {
        if(!Demonstrating)return;var actor=Simulation.Players[demonstrationSeat];var fighter=Simulation.Content.Fighters[actor.FighterId];
        if(demonstration.Count==0&&actor.ActionId.Length>0&&!demonstratedBranches.Contains(actor.ActionId))
        {
            var branch=fighter.Move(actor.ActionId).Branches.FirstOrDefault(b=>
            {
                int origin=b.RelativeToContact?(b.Contact=="hit"?actor.FirstHitFrame:actor.FirstContactFrame):0;
                return origin>=0&&actor.ActionFrame>=origin+b.Start&&actor.ActionFrame<origin+b.End-1&&(b.Contact=="none"||b.Contact=="hit"&&actor.HitContact||b.Contact=="contact"&&actor.Contact);
            });
            if(branch!=null){demonstratedBranches.Add(actor.ActionId);demonstration.Enqueue(new(5,Buttons.None));demonstration.Enqueue(new(5,branch.Button=="P"?Buttons.LP:Buttons.LK));}
        }
        var sample=demonstration.Count>0?demonstration.Dequeue():new CommandInput(5,Buttons.None);
        if(demonstrationSeat==0)input=new(0,Simulation.Tick,sample.Direction,sample.Held);else opponent=new(1,Simulation.Tick,sample.Direction,sample.Held);
        if(demonstration.Count==0&&demonstrationStarted&&actor.Actionable){demonstrationMove="";Feedback="Demonstration finished. Inspect the recorded input, action and contact trace; change the dummy or compare with the free kit.";}
    }
}
