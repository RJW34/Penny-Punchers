using System.Security.Cryptography;
using StrikeLedger.Core;

namespace StrikeLedger.App;

public enum PresentationChangeKind { Add, Confirm, Cancel }
public sealed record PresentationChange(string Key,PresentationChangeKind Kind,CombatEvent Event,bool Speculative,
    int ActorX,int ActorY,int TargetX,int TargetY);

/// <summary>One local seat, complete core snapshots, and an eight-tick bounded prediction timeline.</summary>
public sealed class RollbackSession
{
    readonly SortedDictionary<long,InputFrame> local=new(),remote=new(),usedRemote=new();
    readonly SortedDictionary<long,SimulationSnapshot> snapshots=new();
    readonly SortedDictionary<long,IReadOnlyList<CombatEvent>> presentationEvents=new();
    readonly SortedDictionary<long,PresentationChange[]> locatedEvents=new();
    readonly Dictionary<string,PresentationChange> speculative=new(StringComparer.Ordinal);
    readonly List<PresentationChange> changes=new();
    readonly HashSet<string> presented=new(StringComparer.Ordinal);
    long confirmedPresentedThrough=-1;
    long deliveredThrough=-1;
    readonly int history;
    long remoteThrough=-1;
    public Simulation Simulation {get;}
    public int LocalSeat {get;}
    public int RemoteSeat=>1-LocalSeat;
    public int InputDelay {get;}
    public int PredictionWindow {get;}
    public long ConfirmedThroughTick=>Math.Min(remoteThrough,Simulation.Tick-1);
    public int RollbackCount {get;private set;}
    public int ResimulatedFrames {get;private set;}
    public int StallCount {get;private set;}
    public bool IsStalled {get;private set;}
    public long LastInvalidatedTick {get;private set;}=-1;
    public event Action<long>? TimelineCorrected;
    public RollbackSession(Simulation simulation,int localSeat,int inputDelay=2,int predictionWindow=8,int snapshotHistory=240)
    {
        if(localSeat is <0 or >1 || inputDelay is <0 or >6 || predictionWindow is <1 or >8 || snapshotHistory is <60 or >240)throw new ArgumentException("Invalid bounded rollback settings");
        Simulation=simulation;LocalSeat=localSeat;InputDelay=inputDelay;PredictionWindow=predictionWindow;history=snapshotHistory;
        ResetTimeline();
    }
    public void ResetTimeline()
    {
        changes.Clear();foreach(var cue in speculative.Values.Where(c=>presented.Contains(c.Key)))changes.Add(cue with{Kind=PresentationChangeKind.Cancel});
        speculative.Clear();presented.Clear();locatedEvents.Clear();confirmedPresentedThrough=Simulation.Tick-1;
        local.Clear();remote.Clear();usedRemote.Clear();snapshots.Clear();presentationEvents.Clear();deliveredThrough=Simulation.Tick-1;
        remoteThrough=Simulation.Tick-1;IsStalled=false;
        for(long t=Simulation.Tick;t<Simulation.Tick+InputDelay;t++)
        {local[t]=new(LocalSeat,t,5,Buttons.None);remote[t]=new(RemoteSeat,t,5,Buttons.None);remoteThrough=t;}
        snapshots[Simulation.Tick]=Simulation.Capture();
    }
    public IReadOnlyList<InputFrame> RecentLocalInputs(int count=32)=>local.Values.TakeLast(Math.Clamp(count,1,64)).ToArray();
    public (InputFrame Seat0,InputFrame Seat1) ConfirmedInputs(long tick)
    {
        if(tick>ConfirmedThroughTick || !local.TryGetValue(tick,out var a) || !remote.TryGetValue(tick,out var b))throw new InvalidOperationException("Requested input is unconfirmed or outside retained history");
        return LocalSeat==0?(a,b):(b,a);
    }
    public bool Advance(byte direction,Buttons held)
    {
        long tick=Simulation.Tick,target=tick+InputDelay;
        if(!local.ContainsKey(target))local[target]=new(LocalSeat,target,direction,held);
        if(tick-remoteThrough>PredictionWindow){IsStalled=true;StallCount++;return false;}
        IsStalled=false;
        if(!local.TryGetValue(tick,out var own))throw new InvalidOperationException("Local delayed input missing");
        var opponent=Predict(tick);usedRemote[tick]=opponent;snapshots[tick]=Simulation.Capture();
        var result=LocalSeat==0?Simulation.Step(own,opponent):Simulation.Step(opponent,own);RecordPresentation(tick,result,true);
        snapshots[Simulation.Tick]=Simulation.Capture();Prune();return true;
    }
    InputFrame Predict(long tick)
    {
        if(remote.TryGetValue(tick,out var exact))return exact;
        var previous=remote.LastOrDefault(p=>p.Key<tick);
        return previous.Value==default?new(RemoteSeat,tick,5,Buttons.None):new(RemoteSeat,tick,previous.Value.Direction,previous.Value.Held);
    }
    public bool SubmitRemote(InputFrame input)
    {
        if(input.SeatId!=RemoteSeat)throw new InvalidDataException("Cross-seat input rejected");
        if(input.Frame<Simulation.Tick-history)return false;
        if(input.Frame>Simulation.Tick+64)throw new InvalidDataException("Remote input exceeds bounded future window");
        if(remote.TryGetValue(input.Frame,out var prior))
        {if(prior!=input)throw new InvalidDataException("Peer changed an already authoritative input");return false;}
        remote[input.Frame]=input;
        while(remote.ContainsKey(remoteThrough+1))remoteThrough++;
        if(usedRemote.TryGetValue(input.Frame,out var predicted) && predicted!=input)
        {
            long end=Simulation.Tick;
            if(!snapshots.TryGetValue(input.Frame,out var snapshot))throw new InvalidOperationException("Rollback snapshot expired");
            Simulation.Restore(snapshot);RollbackCount++;LastInvalidatedTick=input.Frame;TimelineCorrected?.Invoke(input.Frame);
            for(long tick=input.Frame;tick<end;tick++)
            {
                snapshots[tick]=Simulation.Capture();var actual=Predict(tick);usedRemote[tick]=actual;
                var result=LocalSeat==0?Simulation.Step(local[tick],actual):Simulation.Step(actual,local[tick]);RecordPresentation(tick,result,false);
                ResimulatedFrames++;
                if(Simulation.Phase==MatchPhase.PendingResult)break;
            }
            // A corrected strike can terminate earlier than the predicted timeline. Discard
            // obsolete future state/events; retained authoritative inputs remain immutable.
            foreach(long key in snapshots.Keys.Where(k=>k>Simulation.Tick).ToArray())snapshots.Remove(key);
            foreach(long key in usedRemote.Keys.Where(k=>k>=Simulation.Tick).ToArray())usedRemote.Remove(key);
            foreach(long key in presentationEvents.Keys.Where(k=>k>=Simulation.Tick).ToArray())presentationEvents.Remove(key);
            foreach(long key in locatedEvents.Keys.Where(k=>k>=Simulation.Tick).ToArray())locatedEvents.Remove(key);
            ReconcileSpeculation(input.Frame);
            snapshots[Simulation.Tick]=Simulation.Capture();
        }
        return true;
    }
    /// <summary>Only confirmed events are presented. Late corrections cannot replay a debit sound or leave a false KO effect.</summary>
    public IReadOnlyList<CombatEvent> DrainPresentationEvents()
    {
        ConfirmPresentation();
        var events=new List<CombatEvent>();long confirmed=ConfirmedThroughTick;
        foreach(var entry in presentationEvents.Where(p=>p.Key>deliveredThrough && p.Key<=confirmed))events.AddRange(entry.Value);
        deliveredThrough=Math.Max(deliveredThrough,confirmed);
        foreach(long key in presentationEvents.Keys.TakeWhile(k=>k<=deliveredThrough).ToArray())presentationEvents.Remove(key);
        return events;
    }
    static bool Reversible(CombatEvent e)=>e.Kind is CombatEventKind.ActionStarted or CombatEventKind.Jump or CombatEventKind.Dash;
    void RecordPresentation(long tick,StepResult result,bool publish)
    {
        presentationEvents[tick]=result.Events;
        locatedEvents[tick]=result.Events.Select((e,index)=>
        {
            var actor=e.Seat is >=0 and <2?Simulation.Players[e.Seat]:null;
            var target=e.Target is >=0 and <2?Simulation.Players[e.Target]:actor;
            return new PresentationChange($"{Simulation.RoundId}:{e.Tick}:{e.Seat}:{e.Kind}:{e.MoveId}:{e.ActionOrdinal}:{e.ProjectileId}:{e.HitGroup}:{e.SourceFighterId}:{index}",
                PresentationChangeKind.Add,e,e.Seat==LocalSeat&&Reversible(e),actor?.X??0,actor?.Y??0,target?.X??0,target?.Y??0);
        }).ToArray();
        if(publish)foreach(var cue in locatedEvents[tick].Where(c=>c.Speculative))PublishSpeculative(cue);
    }
    void PublishSpeculative(PresentationChange cue)
    {
        if(speculative.ContainsKey(cue.Key))return;
        speculative[cue.Key]=cue;changes.Add(cue);
    }
    void CancelSpeculative(PresentationChange cue)
    {
        changes.RemoveAll(c=>c.Key==cue.Key);
        if(presented.Remove(cue.Key))changes.Add(cue with{Kind=PresentationChangeKind.Cancel});
        speculative.Remove(cue.Key);
    }
    void ReconcileSpeculation(long from)
    {
        var corrected=locatedEvents.Where(p=>p.Key>=from).SelectMany(p=>p.Value).Where(c=>c.Speculative).ToDictionary(c=>c.Key,StringComparer.Ordinal);
        foreach(var old in speculative.Values.Where(c=>c.Event.Tick>=from).ToArray())
            if(!corrected.TryGetValue(old.Key,out var current)||old!=current)CancelSpeculative(old);
        foreach(var cue in corrected.Values)PublishSpeculative(cue);
    }
    void ConfirmPresentation()
    {
        long confirmed=ConfirmedThroughTick;
        foreach(var cue in locatedEvents.Where(p=>p.Key>confirmedPresentedThrough&&p.Key<=confirmed).SelectMany(p=>p.Value))
        {
            if(speculative.Remove(cue.Key))
            {
                if(presented.Remove(cue.Key))changes.Add(cue with{Kind=PresentationChangeKind.Confirm,Speculative=false});
                else{changes.RemoveAll(c=>c.Key==cue.Key);changes.Add(cue with{Speculative=false});}
            }
            else changes.Add(cue with{Speculative=false});
        }
        confirmedPresentedThrough=Math.Max(confirmedPresentedThrough,confirmed);
        foreach(long key in locatedEvents.Keys.TakeWhile(k=>k<=confirmedPresentedThrough).ToArray())locatedEvents.Remove(key);
    }
    /// <summary>Immediate reversible local cues plus confirmed outcomes, with correction identities. Never predicts money or results.</summary>
    public IReadOnlyList<PresentationChange> DrainPresentationChanges()
    {
        ConfirmPresentation();var result=changes.ToArray();changes.Clear();
        foreach(var cue in result.Where(c=>c.Kind==PresentationChangeKind.Add&&c.Speculative))presented.Add(cue.Key);
        return result;
    }
    public string? HashAfter(long tick)
    {
        if(tick>ConfirmedThroughTick || !snapshots.TryGetValue(tick+1,out var snapshot))return null;
        return Convert.ToHexString(SHA256.HashData(snapshot.Bytes)).ToLowerInvariant();
    }
    void Prune()
    {
        long minimum=Simulation.Tick-history;
        foreach(var key in snapshots.Keys.TakeWhile(k=>k<minimum).ToArray())snapshots.Remove(key);
        foreach(var key in local.Keys.TakeWhile(k=>k<minimum).ToArray())local.Remove(key);
        foreach(var key in usedRemote.Keys.TakeWhile(k=>k<minimum).ToArray())usedRemote.Remove(key);
        // Keep one preceding remote sample so prediction retains held state after pruning.
        foreach(var key in remote.Keys.Where(k=>k<minimum).SkipLast(1).ToArray())remote.Remove(key);
    }
}
