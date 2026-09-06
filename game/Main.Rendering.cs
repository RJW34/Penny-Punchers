using Godot;
using StrikeLedger.Core;
using StrikeLedger.Presentation;
using StrikeLedger.App;

public partial class Main
{
    private Simulation? _presentationSimulation;
    readonly int[] renderedX={288000,480000},renderedDx={0,0};long renderedMotionTick=-1;
    private readonly string[] _parryVisualKind=["parry","parry"];
    private readonly long[] _throwVisualUntil=[-1,-1],_techVisualUntil=[-1,-1],_tauntVisualUntil=[-1,-1];
    private long _lastPresentationTick=-1, _lastObservedStepTick=-1;
    private readonly long[] _parryVisualUntil=[-1,-1], _lastRejectedCue=[-100,-100];
    private readonly HashSet<string> _presentationKeys=[];
    private readonly string[] _tauntCueKeys=["",""];
    private readonly int[] _renderedFacing=[1,-1];
    private readonly bool[] _renderedCrouch=[false,false];
    private readonly long[] _turnUntil=[-1,-1],_postureUntil=[-1,-1];
    private readonly string[] _postureState=["",""];
    void ResetPresentationTimeline()
    {
        _presentationSimulation=null;_lastObservedStepTick=-1;_lastPresentationTick=-1;
        _presentationKeys.Clear();arena.ResetEffects();_hudSimulation=null;
        Array.Fill(_parryVisualUntil,-1);Array.Fill(_throwVisualUntil,-1);Array.Fill(_techVisualUntil,-1);Array.Fill(_tauntVisualUntil,-1);
        Array.Fill(_turnUntil,-1);Array.Fill(_postureUntil,-1);
        renderedMotionTick=-1;
    }

    private void PreparePresentationTimeline()
    {
        if(sim==null)return;
        if(!ReferenceEquals(_presentationSimulation,sim)||sim.Tick<_lastPresentationTick)
        {
            _presentationSimulation=sim;_lastObservedStepTick=-1;_presentationKeys.Clear();
            Array.Fill(_parryVisualUntil,-1);Array.Fill(_lastRejectedCue,-100);
            Array.Fill(_throwVisualUntil,-1);Array.Fill(_techVisualUntil,-1);Array.Fill(_tauntVisualUntil,-1);
            arena.ResetEffects();renderedMotionTick=-1;for(int i=0;i<2;i++)renderedX[i]=sim.Players[i].X;
            for(int i=0;i<2;i++){lastAction[i]=sim.Players[i].ActionId;lastHealth[i]=sim.Players[i].Health;lastWallet[i]=sim.Players[i].Credits;_renderedFacing[i]=sim.Players[i].Facing;_renderedCrouch[i]=sim.Players[i].Crouching;}
        }
        _lastPresentationTick=sim.Tick;
    }

    void ObserveChanges(StepResult? result=null)
    {
        if(sim==null)return;
        PreparePresentationTimeline();TrackShopPublicState();
        IReadOnlyList<(CombatEvent Event,string Key)> events;
        if(peer!=null)
        {
            var added=new List<(CombatEvent,string)>();
            foreach(var change in peer.Rollback.DrainPresentationChanges())
            {
                if(change.Kind==PresentationChangeKind.Cancel)
                {
                    _presentationKeys.Remove(change.Key);arena.CancelCue(change.Key);
                    for(int i=0;i<2;i++)if(_tauntCueKeys[i]==change.Key)_tauntVisualUntil[i]=-1;
                }
                else if(change.Kind==PresentationChangeKind.Add&&_presentationKeys.Add(change.Key))
                {
                    added.Add((change.Event with{WorldX=change.Event.WorldX??change.ActorX,WorldY=change.Event.WorldY??change.ActorY},change.Key));
                    if(!change.Speculative)TrackShopPublicEvent(change.Event,change.Key);
                }
                else if(change.Kind==PresentationChangeKind.Confirm)
                    TrackShopPublicEvent(change.Event,change.Key);
            }
            events=added;
        }
        else
        {
            result??=mode=="replay"?replay?.LastStepResult:null;
            if(result==null||result.Tick==_lastObservedStepTick)return;
            _lastObservedStepTick=result.Tick;events=result.Events.Select((e,i)=>(e,$"local:{sim.RoundId}:{e.Tick}:{i}")).ToArray();
        }
        foreach(var entry in events)
        {
            var e=entry.Event;string key=entry.Key;if(peer==null)TrackShopPublicEvent(e,key);int age=(int)Math.Clamp(sim.Tick-e.Tick-1,0,10000);
            int seat=Math.Clamp(e.Seat,0,1);var actor=sim.Players[seat];
            int target=e.Target is >=0 and <=1?e.Target:seat;var victim=sim.Players[target];
            string sourceFighter=e.SourceFighterId.Length>0?e.SourceFighterId:e.Kind is CombatEventKind.Block or CombatEventKind.Parry?victim.FighterId:actor.FighterId;
            var move=content.Fighters[sourceFighter].Moves.FirstOrDefault(m=>m.Id==e.MoveId);
            int strength=move is null?1:Math.Clamp(Math.Max(move.Hitboxes.Select(h=>h.Rank).DefaultIfEmpty(1).Max(),move.Projectile?.Rank??1),1,3);
            int contactHeight=(e.MoveId.StartsWith("c_")||move?.Hitboxes.FirstOrDefault()?.Level=="low")?20000:55000;
            int contactX=e.WorldX??actor.X,contactY=e.WorldY??actor.Y+contactHeight;
            void Impact(string kind,int affectedSeat,int power)=>arena.TriggerEffect(kind,contactX,contactY,power,age,key,affectedSeat,sourceFighter,e.MoveId);
            switch(e.Kind)
            {
                case CombatEventKind.ActionStarted:
                    if(e.MoveId=="taunt"){_tauntVisualUntil[seat]=e.Tick+24;_tauntCueKeys[seat]=key;}
                    if(age<12&&e.Detail!="install-active")arena.PlayCue(move?.Kind=="super"?"super":move?.Kind=="ex_special"?"ex":"swing",actor.FighterId,key);
                    evidenceLog?.WriteLine($"ACTION {e.Tick} P{seat+1} {e.MoveId} credit={actor.Credits}");
                    break;
                case CombatEventKind.SuperUseConsumed:
                    arena.ShowWalletCue(seat,"SUPER USE COMMITTED",key:key);
                    break;
                case CombatEventKind.SkillAward:
                    if(e.Value>0){arena.ShowWalletCue(seat,$"{e.Detail.Split('·')[0].Trim()} +{e.Value} NEXT SHOP",key:key);arena.PlayCue("confirm",actor.FighterId,key);}
                    break;
                case CombatEventKind.SkillOpportunity:
                    arena.ShowWalletCue(seat,$"{e.Detail.Split('·')[0].Trim()} +{e.Value} / PRACTICE ONLY",key:key);
                    break;
                case CombatEventKind.Rejected:
                    if(sim.Tick-_lastRejectedCue[seat]>=18)
                    {
                        _lastRejectedCue[seat]=sim.Tick;
                        string message=e.Detail switch
                        {
                            "locked"=>move?.Kind=="super"?"SUPER NOT PURCHASED":move?.Kind=="ex_special"?"EX NOT IN THIS ROUND'S KIT":"MOVE NOT IN THIS ROUND'S KIT",
                            "exhausted"=>"SUPER ALREADY USED",
                            "actor-state"=>move?.ActorRules?.Type=="dive"?$"DIVE: FORWARD JUMP ABOVE {move.ActorRules.MinimumHeight/1000} / NO PRIOR AIR ATTACK":"MOVE NOT LEGAL IN THIS STATE",
                            "ex-not-owned" or "ex-unowned" or "license"=>"EX NOT IN THIS ROUND'S KIT",
                            "super-not-owned" or "super-unowned" or "super-not-purchased"=>"SUPER NOT PURCHASED",
                            "super-used" or "super-already-used"=>"SUPER ALREADY USED",
                            _=>e.Detail.Replace('-', ' ').ToUpperInvariant()
                        };
                        arena.ShowWalletCue(seat,message,true,key);arena.PlayCue("denied");
                    }
                    break;
                case CombatEventKind.Hit:
                    Impact(e.Detail=="armor"?"armor":e.Detail=="counter"?"counterhit":move?.Kind=="super"?"superhit":move?.Kind=="ex_special"?"exhit":strength==3?"heavyhit":"hit",target,strength);
                    break;
                case CombatEventKind.Block:
                    Impact("block",seat,strength);
                    break;
                case CombatEventKind.Parry:
                    _parryVisualUntil[seat]=e.Tick+9;
                    _parryVisualKind[seat]=e.Detail.Split(':')[0] switch{"Low"=>"crouchparry","Air"=>"airparry","RedHigh"=>"redparry","RedLow"=>"redlowparry",_=>"parry"};
                    Impact("parry",seat,strength);
                    break;
                case CombatEventKind.Throw:
                    _throwVisualUntil[target]=e.Detail=="capture"?e.Tick+content.Combat.Throw.TechWindow:-1;
                    if(e.Detail=="damage")Impact("throw",target,3);
                    break;
                case CombatEventKind.ThrowTech:
                    _throwVisualUntil[seat]=_throwVisualUntil[target]=-1;
                    _techVisualUntil[seat]=_techVisualUntil[target]=e.Tick+10;
                    Impact("throwtech",seat,2);
                    break;
                case CombatEventKind.ProjectileClash:
                    Impact(e.Detail=="reflected"?"reflect":e.Detail is "intercepted" or "field-destroyed"?"dissipate":"block",-1,2);
                    break;
                case CombatEventKind.CounterCaught:
                    Impact("countercatch",seat,1);
                    break;
                case CombatEventKind.Land:
                    if(e.Detail!="wakeup")arena.TriggerEffect("land",e.WorldX??actor.X,e.WorldY??0,1,age,key,seat,actor.FighterId);
                    break;
                case CombatEventKind.Dash:
                    arena.TriggerEffect("dash",e.WorldX??actor.X,e.WorldY??0,1,age,key,seat,actor.FighterId);
                    break;
                case CombatEventKind.Jump:
                    if(age<8)arena.PlayCue("breath",actor.FighterId,key);
                    break;
                case CombatEventKind.RoundTerminal:
                    arena.PlayCue(e.Detail=="TIME"?"round":"ko");
                    break;
            }
        }
        for(int i=0;i<2;i++){lastAction[i]=sim.Players[i].ActionId;lastHealth[i]=sim.Players[i].Health;lastWallet[i]=sim.Players[i].Credits;}
    }

    void UpdateArena()
    {
        using var updateProfile=RuntimeProfiler.Measure("UpdateArena");
        if(sim==null)return;
        PreparePresentationTimeline();TrackShopPublicState();
        var fighters=sim.Players.Select(p=>
        {
            bool motionFrozen=p.Hitstop>0||sim.FullFreeze>0;
            int dx=sim.Tick==renderedMotionTick||motionFrozen?renderedDx[p.Seat]:p.X-renderedX[p.Seat];
            if(sim.Tick!=renderedMotionTick){renderedDx[p.Seat]=dx;renderedX[p.Seat]=p.X;}
            if(!motionFrozen&&sim.Tick!=renderedMotionTick)
            {
                if(_renderedFacing[p.Seat]!=p.Facing)_turnUntil[p.Seat]=sim.Tick+3;
                if(_renderedCrouch[p.Seat]!=p.Crouching){_postureUntil[p.Seat]=sim.Tick+3;_postureState[p.Seat]=p.Crouching?"crouch_down":"crouch_up";}
                _renderedFacing[p.Seat]=p.Facing;_renderedCrouch[p.Seat]=p.Crouching;
            }
            // Tick advances during a global freeze; these observed poses follow the
            // already-authoritative defender freeze / taunt recovery instead.
            if(sim.Tick>=_parryVisualUntil[p.Seat]&&p.Hitstop==0)_parryVisualUntil[p.Seat]=-1;
            if(p.DashTicks==0)_tauntVisualUntil[p.Seat]=-1;
            var move=p.ActionId.Length==0?null:content.Fighters[p.FighterId].Move(p.ActionId);
            bool settled=sim.Phase is MatchPhase.RoundResult or MatchPhase.MatchOver;
            string state=settled?(sim.PendingResult?.WinnerSeat<0?"draw":sim.PendingResult?.WinnerSeat==p.Seat?"win":"defeat")
                :sim.Phase is MatchPhase.Reveal or MatchPhase.Countdown?"intro"
                :p.Health==0?"knockdown"
                :p.KnockdownTicks>0?(!p.Grounded?"falling":p.KnockdownTicks<=10?"wakeup":"knockdown")
                :p.DizzyTicks>0?"dizzy"
                :p.Hitstun>0?!p.Grounded?"airhit":p.Crouching?"lowhit":"hit"
                :p.Blockstun>0?p.Crouching?"crouchguard":"guard"
                :sim.Tick<_throwVisualUntil[p.Seat]?"thrown"
                :sim.Tick<_techVisualUntil[p.Seat]?"tech"
                :_parryVisualUntil[p.Seat]>=0&&(sim.Tick<_parryVisualUntil[p.Seat]||p.Hitstop>0)?_parryVisualKind[p.Seat]
                :_tauntVisualUntil[p.Seat]>=0&&p.DashTicks>0?"taunt"
                :p.DashTicks>0?dx*p.Facing<0?"dashback":"dash"
                :!p.Grounded?p.Vy>800?"rise":p.Vy< -800?"fall":"apex"
                :p.JumpStart>0?"takeoff"
                :p.LandingTicks>0?"landing"
                :p.ActionId.Length==0&&sim.Tick<_turnUntil[p.Seat]?"turn"
                :p.ActionId.Length==0&&sim.Tick<_postureUntil[p.Seat]?_postureState[p.Seat]
                :p.Crouching?"crouch"
                :dx!=0&&p.ActionId.Length==0?dx*p.Facing<0?"walkback":"walk":"idle";
            var boxes=new List<RenderBox>();
            if(debugBoxes)
            {
                RenderBox Box(WorldBox box,string kind)=>new(){X=box.Left-p.X,Y=box.Bottom-p.Y,Width=box.Width,Height=box.Height,Kind=kind};
                boxes.Add(Box(sim.Pushbox(p),"push"));
                boxes.AddRange(sim.Hurtboxes(p.Seat).Select(box=>Box(box,"hurt")));
                boxes.AddRange(sim.Hitboxes(p.Seat).Select(box=>Box(box,"hit")));
            }
            bool mirror=sim.Players[0].FighterId==sim.Players[1].FighterId;
            var mimicSource=move?.Mimic is {} mimic?content.Fighters[p.FighterId].Move(mimic.SourceMoveId):null;
            return new FighterRenderState
            {
                Id=p.FighterId,X=p.X,Y=p.Y,Facing=p.ActionId.Length>0?p.ActionFacing:p.Facing,MoveId=p.ActionId,State=state,ActionFrame=p.ActionFrame,
                Startup=move?.Startup??3,Active=move?.Active??2,Recovery=move?.Recovery??12,Strength=RenderStrength(p.ActionId),
                Palette=mirror?p.Seat:0,Grounded=p.Grounded,Frozen=p.Hitstop>0||sim.FullFreeze>0,Boxes=boxes.ToArray(),InstallTicks=p.InstallTicks,ArmorActive=p.ArmorActive,CounterActive=p.CounterActive,DestinationX=p.DestinationX,
                MimicSourceMoveId=move?.Mimic?.SourceMoveId??"",MimicSharedTicks=move?.Mimic?.SharedTicks??0,MimicSourceStartup=mimicSource?.Startup??0,MimicSourceActive=mimicSource?.Active??0
            };
        }).ToArray();
        renderedMotionTick=sim.Tick;var projectiles=sim.Projectiles.Where(p=>!p.IsField).Select(p=>new ProjectileRenderState
        {
            Id=p.Id,FighterId=p.OriginalFighter,MoveId=p.OriginalMoveId,Age=p.Age,
            X=p.X,Y=p.Y,Facing=p.Vx==0?p.Facing:Math.Sign(p.Vx),Owner=p.Owner,IsSuper=content.Fighters[p.OriginalFighter].Move(p.OriginalMoveId).Kind=="super",Width=p.Definition.Width,Height=p.Definition.Height,
            VelocityX=p.Vx,VelocityY=p.Vy,ReflectionDepth=p.ReflectionDepth
        }).ToArray();
        var fields=sim.Fields.Select(p=>
        {
            var rules=content.Fighters[p.OriginalFighter].Move(p.OriginalMoveId).ObjectRules!;
            return new FieldRenderState{Id=p.Id,Kind=p.ObjectKind,Owner=p.Owner,X=p.X,Y=p.Y,Age=p.Age,LifeTicks=p.LifeTicks,HitsRemaining=p.HitsRemaining,ContactCooldown=p.ContactCooldown,
                Armed=p.Armed,Triggered=p.Triggered,TriggerTicks=p.TriggerTicks,ArmingTicks=rules.ArmingTicks,TriggerWindup=rules.TriggerWindup,TriggerRadius=rules.TriggerRadius,
                MarkerWidth=rules.MarkerWidth,MarkerHeight=rules.MarkerHeight,Width=p.Definition.Width,Height=p.Definition.Height};
        }).ToArray();
        arena.SetState(new ArenaRenderState
        {
            Fighters=fighters,Projectiles=projectiles,Fields=fields,Tick=(int)sim.Tick,StageId=sim.Config.StageId,TrainingGrid=sim.Config.StageId=="grid",Phase=sim.Phase.ToString(),Frozen=sim.FullFreeze>0,DebugBoxes=debugBoxes,
            ReducedFlashes=settings.ReducedFlashes,ShakeScale=settings.Shake?1:0
        });
    }

    private static int RenderStrength(string move)=>move.StartsWith("super")||move.EndsWith("hp")||move.EndsWith("hk")||move.EndsWith("_h")||move.EndsWith("_ex")?3:move.EndsWith("mp")||move.EndsWith("mk")||move.EndsWith("_m")?2:1;
}
