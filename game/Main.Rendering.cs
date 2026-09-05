using Godot;
using StrikeLedger.Core;
using StrikeLedger.Presentation;

public partial class Main
{
    private Simulation? _presentationSimulation;
    readonly int[] renderedX={288000,480000},renderedDx={0,0};long renderedMotionTick=-1;
    private long _lastPresentationTick=-1, _lastObservedStepTick=-1;
    private readonly long[] _parryVisualUntil=[-1,-1], _lastRejectedCue=[-100,-100];

    private void PreparePresentationTimeline()
    {
        if(sim==null)return;
        if(!ReferenceEquals(_presentationSimulation,sim)||sim.Tick<_lastPresentationTick)
        {
            _presentationSimulation=sim;_lastObservedStepTick=-1;
            Array.Fill(_parryVisualUntil,-1);Array.Fill(_lastRejectedCue,-100);
            arena.ResetEffects();renderedMotionTick=-1;for(int i=0;i<2;i++)renderedX[i]=sim.Players[i].X;
            for(int i=0;i<2;i++){lastAction[i]=sim.Players[i].ActionId;lastHealth[i]=sim.Players[i].Health;lastWallet[i]=sim.Players[i].Credits;}
        }
        _lastPresentationTick=sim.Tick;
    }

    void ObserveChanges(StepResult? result=null)
    {
        if(sim==null)return;
        PreparePresentationTimeline();
        IReadOnlyList<CombatEvent> events;
        if(peer!=null)events=peer.Rollback.DrainPresentationEvents();
        else
        {
            result??=mode=="replay"?replay?.LastStepResult:null;
            if(result==null||result.Tick==_lastObservedStepTick)return;
            _lastObservedStepTick=result.Tick;events=result.Events;
        }
        foreach(var e in events)
        {
            int seat=Math.Clamp(e.Seat,0,1);var actor=sim.Players[seat];
            int target=e.Target is >=0 and <=1?e.Target:seat;var victim=sim.Players[target];
            var move=content.Fighters[actor.FighterId].Moves.FirstOrDefault(m=>m.Id==e.MoveId)
                ??content.Fighters[victim.FighterId].Moves.FirstOrDefault(m=>m.Id==e.MoveId);
            int strength=RenderStrength(e.MoveId);
            int contactHeight=(e.MoveId.StartsWith("c_")||move?.Hitboxes.FirstOrDefault()?.Level=="low")?20000:55000;
            switch(e.Kind)
            {
                case CombatEventKind.ActionStarted:
                    arena.PlayCue(e.MoveId.StartsWith("super")?"super":e.MoveId.EndsWith("_ex")?"ex":"swing");
                    evidenceLog?.WriteLine($"ACTION {e.Tick} P{seat+1} {e.MoveId} credit={actor.Credits}");
                    break;
                case CombatEventKind.Spend:
                    string reason=e.MoveId.StartsWith("super")?"SUPER":e.MoveId.EndsWith("_ex")?"EX":"MOVE";
                    arena.ShowWalletCue(seat,$"{reason}  −{e.Value:N0} CR");
                    evidenceLog?.WriteLine($"DEBIT {e.Tick} P{seat+1} {e.MoveId} −{e.Value} credit={actor.Credits}");
                    break;
                case CombatEventKind.Rejected:
                    if(sim.Tick-_lastRejectedCue[seat]>=18)
                    {
                        _lastRejectedCue[seat]=sim.Tick;
                        arena.ShowWalletCue(seat,e.Detail=="reserve"?"RESERVE PROTECTED":"INSUFFICIENT CREDITS",true);arena.PlayCue("denied");
                    }
                    break;
                case CombatEventKind.Hit:
                    arena.TriggerEffect(e.Detail=="counter"?"counterhit":strength==3?"heavyhit":"hit",victim.X,victim.Y+contactHeight,strength);
                    break;
                case CombatEventKind.Block:
                    arena.TriggerEffect("block",actor.X+actor.Facing*18000,actor.Y+contactHeight,strength);
                    break;
                case CombatEventKind.Parry:
                    _parryVisualUntil[seat]=sim.Tick+9;
                    arena.TriggerEffect("parry",actor.X+actor.Facing*18000,actor.Y+contactHeight,strength);
                    break;
                case CombatEventKind.Throw:
                    if(e.Detail=="damage")arena.TriggerEffect("throw",victim.X,victim.Y+26000,3);
                    break;
                case CombatEventKind.ThrowTech:
                    arena.TriggerEffect("parry",(actor.X+victim.X)/2,(actor.Y+victim.Y)/2+45000,2);
                    break;
                case CombatEventKind.ProjectileClash:
                    arena.TriggerEffect("block",(actor.X+victim.X)/2,55000,2);
                    break;
                case CombatEventKind.Land:
                    if(e.Detail!="wakeup")arena.TriggerEffect("land",actor.X,0);
                    break;
                case CombatEventKind.Dash:
                    arena.TriggerEffect("dash",actor.X,0);
                    break;
                case CombatEventKind.RoundTerminal:
                    arena.PlayCue("ko");
                    break;
            }
        }
        for(int i=0;i<2;i++){lastAction[i]=sim.Players[i].ActionId;lastHealth[i]=sim.Players[i].Health;lastWallet[i]=sim.Players[i].Credits;}
    }

    void UpdateArena()
    {
        using var updateProfile=RuntimeProfiler.Measure("UpdateArena");
        if(sim==null)return;
        PreparePresentationTimeline();
        var fighters=sim.Players.Select(p=>
        {
            int dx=sim.Tick!=renderedMotionTick?p.X-renderedX[p.Seat]:renderedDx[p.Seat];if(sim.Tick!=renderedMotionTick){renderedDx[p.Seat]=dx;renderedX[p.Seat]=p.X;}
            var move=p.ActionId.Length==0?null:content.Fighters[p.FighterId].Move(p.ActionId);
            string state=p.Health==0||p.KnockdownTicks>0?"knockdown"
                :p.DizzyTicks>0?"dizzy"
                :p.Hitstun>0?"hit"
                :sim.Tick<_parryVisualUntil[p.Seat]?p.Crouching?"crouchparry":"parry"
                :p.Blockstun>0?p.Crouching?"crouchguard":"guard"
                :sim.PendingResult?.WinnerSeat==p.Seat&&sim.Phase is MatchPhase.RoundResult or MatchPhase.MatchOver?"win"
                :p.DashTicks>0?dx*p.Facing<0?"dashback":"dash"
                :!p.Grounded?"jump"
                :p.JumpStart>0?"crouch"
                :p.Crouching?"crouch"
                :dx!=0&&p.ActionId.Length==0?"walk":"idle";
            var boxes=new List<RenderBox>();
            if(debugBoxes)
            {
                RenderBox Box(WorldBox box,string kind)=>new(){X=box.Left-p.X,Y=box.Bottom-p.Y,Width=box.Width,Height=box.Height,Kind=kind};
                boxes.Add(Box(sim.Pushbox(p),"push"));
                boxes.AddRange(sim.Hurtboxes(p.Seat).Select(box=>Box(box,"hurt")));
                boxes.AddRange(sim.Hitboxes(p.Seat).Select(box=>Box(box,"hit")));
            }
            bool mirror=sim.Players[0].FighterId==sim.Players[1].FighterId;
            return new FighterRenderState
            {
                Id=p.FighterId,X=p.X,Y=p.Y,Facing=p.ActionId.Length>0?p.ActionFacing:p.Facing,MoveId=p.ActionId,State=state,ActionFrame=p.ActionFrame,
                Startup=move?.Startup??3,Active=move?.Active??2,Recovery=move?.Recovery??12,Strength=RenderStrength(p.ActionId),
                Palette=mirror?p.Seat:0,Grounded=p.Grounded,Boxes=boxes.ToArray()
            };
        }).ToArray();
        renderedMotionTick=sim.Tick;var projectiles=sim.Projectiles.Select(p=>new ProjectileRenderState
        {
            X=p.X,Y=p.Y,Facing=p.Vx==0?p.Facing:Math.Sign(p.Vx),Owner=p.Owner,IsSuper=p.MoveId.StartsWith("super")||p.MoveId.EndsWith("_ex"),Width=p.Definition.Width,Height=p.Definition.Height
        }).ToArray();
        arena.SetState(new ArenaRenderState
        {
            Fighters=fighters,Projectiles=projectiles,Tick=(int)sim.Tick,TrainingGrid=mode=="training"||sim.Config.StageId=="grid",DebugBoxes=debugBoxes,
            ReducedFlashes=settings.ReducedFlashes,ShakeScale=settings.Shake?1:0
        });
    }

    private static int RenderStrength(string move)=>move.StartsWith("super")||move.EndsWith("hp")||move.EndsWith("hk")||move.EndsWith("_h")||move.EndsWith("_ex")?3:move.EndsWith("mp")||move.EndsWith("mk")||move.EndsWith("_m")?2:1;
}
