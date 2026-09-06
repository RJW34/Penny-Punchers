namespace StrikeLedger.Core;

/// <summary>Authored object semantics. All positions use integer world units; timing excludes global freeze.</summary>
public sealed class ObjectRulesDefinition
{
    public string Kind {get;set;}=""; // anchor, prism, intercept, reflect
    public int ActiveStart {get;set;}public int ActiveEnd {get;set;}
    public BoxDefinition InteractionBox {get;set;}=new(){X=28000,Y=34000,Width=36000,Height=48000};
    public int PlacementNear {get;set;}=45000;public int PlacementFar {get;set;}=90000;
    public int LifeTicks {get;set;}=180;public int ArmingTicks {get;set;}
    public int TriggerRadius {get;set;}=24000;public int TriggerWindup {get;set;}=8;
    public int MarkerWidth {get;set;}=18000;public int MarkerHeight {get;set;}=18000;
    public ProjectileDefinition Contact {get;set;}=new(){Width=18000,Height=70000,Damage=45,Stun=45,Hitstun=20,Blockstun=16,Hits=2,RehitTicks=18,Rank=2,JuggleCost=2,MaxActivePerOwner=1};
}
public sealed partial class MoveDefinition { public ObjectRulesDefinition? ObjectRules {get;set;} }
public sealed partial class ProjectileDefinition
{
    public int Vy {get;set;}
    public string Tier {get;set;}="normal";
}
public sealed partial class PlayerState
{
    internal int ObjectPlacementX,ObjectPlacementY;
    internal bool ObjectInteractionConsumed;
}
public sealed partial class ProjectileState
{
    public bool IsField {get;internal set;}
    public string ObjectKind {get;internal set;}="";
    public int Age {get;internal set;}
    public long SpawnTick {get;internal set;}
    public int TriggerTicks {get;internal set;}=-1;
    public int Vy {get;internal set;}
    public int PreviousY {get;internal set;}
    public int ReflectionDepth {get;internal set;}
    public string OriginalFighter {get;internal set;}="";
    public string OriginalMoveId {get;internal set;}="";
    public int OriginalOwner {get;internal set;}
    public int ActionOrdinal {get;internal set;}
    public bool Armed {get;internal set;}
    public bool Triggered {get;internal set;}
}
public sealed partial class Simulation
{
    public IReadOnlyList<ProjectileState> Fields=>_projectiles.Where(p=>p.IsField&&p.LifeTicks>0&&p.HitsRemaining>0).ToArray();
    static bool IsFieldMove(MoveDefinition move)=>move.ObjectRules?.Kind is "anchor" or "prism";
    static bool OrdinaryShot(ProjectileState projectile)=>!projectile.IsField;
    MoveDefinition ProjectileMove(ProjectileState projectile)=>Content.Fighters[projectile.OriginalFighter].Move(projectile.OriginalMoveId);
    ProjectileDefinition ProjectileDefinitionFor(ProjectileState projectile)
    {
        var move=ProjectileMove(projectile);
        return projectile.IsField?move.ObjectRules?.Contact??throw new InvalidDataException("Field definition missing"):move.Projectile??throw new InvalidDataException("Projectile definition missing");
    }
    bool TryObjectPlacement(PlayerState actor,MoveDefinition move,out int x,out int y)
    {
        var rules=move.ObjectRules!;int distance=rules.PlacementNear;
        if(rules.Kind=="prism"&&actor.History.LastOrDefault() is {} sample&&(sample.Held&(Buttons.MP|Buttons.HP))!=0)distance=rules.PlacementFar;
        int half=rules.Contact.Width/2;int facing=actor.Facing;
        x=Math.Clamp(actor.X+distance*facing,_stage.Left+half,_stage.Right-half);y=rules.Contact.Height/2;
        if(rules.Kind!="prism")return true;
        var enemy=Pushbox(_players[1-actor.Seat]);
        if(x+half>=enemy.Left&&x-half<=enemy.Right)x=facing==1?enemy.Left-half-1:enemy.Right+half+1;
        return x-half>=_stage.Left&&x+half<=_stage.Right&&(x-actor.X)*facing>=half;
    }
    bool CanStartObjectMove(PlayerState actor,MoveDefinition move)
    {
        if(!IsFieldMove(move))return true;
        if(_projectiles.Any(p=>p.Owner==actor.Seat&&p.IsField&&p.LifeTicks>0&&p.HitsRemaining>0))return false;
        return TryObjectPlacement(actor,move,out _,out _);
    }
    void StartObjectMove(PlayerState actor,MoveDefinition move)
    {
        actor.ObjectInteractionConsumed=false;
        if(IsFieldMove(move))TryObjectPlacement(actor,move,out actor.ObjectPlacementX,out actor.ObjectPlacementY);
    }
    void InitializeProjectileObject(ProjectileState projectile,PlayerState actor,MoveDefinition move)
    {
        projectile.OriginalFighter=actor.FighterId;projectile.OriginalMoveId=move.Id;projectile.OriginalOwner=actor.Seat;projectile.ActionOrdinal=actor.ActionOrdinal;
        projectile.RootAttack=actor.RootAttack;projectile.SpawnTick=Tick;projectile.Vy=projectile.Definition.Vy;projectile.PreviousY=projectile.Y;projectile.ReflectionDepth=0;
    }
    void AdvanceProjectileVertical(ProjectileState projectile)
    {projectile.PreviousY=projectile.Y;projectile.Y+=projectile.Vy;projectile.Age++;}
    void AdvanceObjectAction(PlayerState actor,MoveDefinition move)
    {
        if(!IsFieldMove(move)||actor.ProjectileSpawned||actor.ActionFrame!=move.Startup)return;
        var rules=move.ObjectRules!;actor.ProjectileSpawned=true;
        var field=new ProjectileState{Id=_nextProjectileId++,Owner=actor.Seat,MoveId=move.Id,IsField=true,ObjectKind=rules.Kind,
            X=actor.ObjectPlacementX,Y=actor.ObjectPlacementY,Facing=actor.ActionFacing,LifeTicks=rules.LifeTicks,HitsRemaining=rules.Contact.Hits,Definition=rules.Contact};
        InitializeProjectileObject(field,actor,move);field.Vy=0;field.PreviousX=field.X;field.Armed=rules.ArmingTicks==0;_projectiles.Add(field);
        Emit(CombatEventKind.ProjectileSpawn,actor.Seat,move:move.Id,detail:"field:"+rules.Kind,worldX:field.X,worldY:field.Y,projectileId:field.Id,actionOrdinal:field.ActionOrdinal);
    }
    void AdvanceObjects()
    {
        foreach(var field in _projectiles.Where(p=>p.IsField))
        {
            if(field.SpawnTick==Tick)continue;
            field.PreviousX=field.X;field.PreviousY=field.Y;field.Age++;field.LifeTicks--;if(field.ContactCooldown>0)field.ContactCooldown--;if(field.Hitstop>0)field.Hitstop--;
            var rules=ProjectileMove(field).ObjectRules!;field.Armed=field.Age>=rules.ArmingTicks;
            if(field.Triggered&&field.TriggerTicks>0)field.TriggerTicks--;
        }
        _projectiles.RemoveAll(p=>p.LifeTicks<=0||p.HitsRemaining<=0||p.Y>400000||p.Y< -50000);
    }
    bool ObjectCanContact(ProjectileState projectile)
    {
        if(!projectile.IsField)return true;
        return projectile.Armed&&(projectile.ObjectKind!="anchor"||projectile.Triggered&&projectile.TriggerTicks==0);
    }
    bool EligibleNormalProjectile(ProjectileState projectile)=>!projectile.IsField&&ProjectileMove(projectile).Kind is not("super" or "ex_special")&&projectile.Definition.Tier=="normal";
    WorldBox FieldMarker(ProjectileState field)
    {
        var rules=ProjectileMove(field).ObjectRules!;
        return field.ObjectKind=="anchor"?new(field.X,rules.MarkerHeight/2,rules.MarkerWidth,rules.MarkerHeight):new(field.X,field.Y,field.Definition.Width,field.Definition.Height);
    }
    void ResolveObjectInteractions()
    {
        // Collect destruction before mutation so neither field can pulse/reflect on its destruction tick.
        var destroyed=new HashSet<int>();
        foreach(var field in _projectiles.Where(p=>p.IsField&&p.HitsRemaining>0))
        {
            int enemy=1-field.Owner;var actor=_players[enemy];if(actor.Hitstop>0)continue;
            if(Hitboxes(enemy).Any(h=>h.Intersects(FieldMarker(field))))destroyed.Add(field.Id);
        }
        foreach(var field in _projectiles.Where(p=>destroyed.Contains(p.Id))){field.HitsRemaining=0;Emit(CombatEventKind.ProjectileClash,1-field.Owner,field.Owner,field.MoveId,detail:"field-destroyed",worldX:field.X,worldY:field.Y,projectileId:field.Id);}
        var interactions=new List<(int ShotId,int Owner,int SourceId,ProjectileState? Field,PlayerState? Actor,string Kind)>();
        foreach(var shot in _projectiles.Where(p=>EligibleNormalProjectile(p)&&p.HitsRemaining>0))
        {
            var box=Swept(new(shot.X,shot.Y,shot.Definition.Width,shot.Definition.Height),shot.PreviousX,shot.PreviousY);
            foreach(var actor in _players)
            {
                if(actor.Seat==shot.Owner||actor.Hitstop>0||actor.ActionId.Length==0||actor.ObjectInteractionConsumed)continue;
                var rules=Content.Fighters[actor.FighterId].Move(actor.ActionId).ObjectRules;
                if(rules?.Kind is not("intercept" or "reflect")||actor.ActionFrame<rules.ActiveStart||actor.ActionFrame>=rules.ActiveEnd)continue;
                if(box.Intersects(Box(rules.InteractionBox,actor.X,actor.Y,actor.ActionFacing)))interactions.Add((shot.Id,actor.Seat,actor.Seat,null,actor,rules.Kind));
            }
            foreach(var field in _projectiles.Where(p=>p.IsField&&p.ObjectKind=="prism"&&p.Owner!=shot.Owner&&p.HitsRemaining>0&&p.ContactCooldown==0))
                if(box.Intersects(FieldMarker(field)))interactions.Add((shot.Id,field.Owner,field.Id,field,null,"reflect"));
        }
        foreach(var group in interactions.GroupBy(i=>i.ShotId).OrderBy(g=>g.Key))
        {
            var shot=_projectiles.First(p=>p.Id==group.Key);var choices=group.OrderBy(i=>i.Field is null?0:1).ThenBy(i=>i.SourceId).ToArray();var choice=choices[0];
            if(choice.Actor?.ObjectInteractionConsumed==true||choice.Field is {HitsRemaining:<=0})continue;
            if(choice.Actor is {} actor)actor.ObjectInteractionConsumed=true;if(choice.Field is {} field){field.HitsRemaining--;field.ContactCooldown=field.Definition.RehitTicks;}
            bool reflect=choice.Kind=="reflect"&&shot.ReflectionDepth==0&&!_projectiles.Any(p=>p!=shot&&OrdinaryShot(p)&&p.Owner==choice.Owner&&p.HitsRemaining>0);
            if(reflect){shot.Owner=choice.Owner;shot.Vx=-shot.Vx;shot.Facing=-shot.Facing;shot.ReflectionDepth=1;shot.PreviousX=shot.X;shot.PreviousY=shot.Y;}
            else shot.HitsRemaining=0;
            Emit(CombatEventKind.ProjectileClash,choice.Owner,shot.OriginalOwner,shot.MoveId,detail:reflect?"reflected":"intercepted",worldX:shot.X,worldY:shot.Y,projectileId:shot.Id,actionOrdinal:shot.ActionOrdinal,sourceFighterId:shot.OriginalFighter);
        }
        _projectiles.RemoveAll(p=>p.HitsRemaining<=0);
        foreach(var field in _projectiles.Where(p=>p.IsField&&p.ObjectKind=="anchor"&&p.Armed&&!p.Triggered))
        {
            var rules=ProjectileMove(field).ObjectRules!;var target=_players[1-field.Owner];
            if(Math.Abs(target.X-field.X)<=rules.TriggerRadius&&target.Y<=rules.Contact.Height){field.Triggered=true;field.TriggerTicks=rules.TriggerWindup;}
        }
    }
    readonly HashSet<int> authorizedFieldContacts=new();
    void PrepareObjectContactResolution(List<ContactCandidate> candidates)
    {
        authorizedFieldContacts.Clear();
        // Direct attacks and ordinary shots can remove a field before its pulse. Two
        // simultaneous fields retain the same immutable trade policy as direct strikes.
        foreach(var incoming in candidates.Where(c=>c.Projectile?.IsField!=true&&!c.Blocked&&!c.Parried).GroupBy(c=>c.Defender))
        {
            var defender=_players[incoming.Key];bool knockdown=incoming.Any(c=>c.Hit.Knockdown!="none");
            bool dizzy=!defender.DizzyThisCombo&&(long)defender.Stun+incoming.Sum(c=>(long)c.Hit.Stun)>=Content.Fighters[defender.FighterId].StunLimit;
            foreach(var field in _projectiles.Where(p=>p.IsField&&p.Owner==incoming.Key))
                if(field.ObjectKind=="anchor"||knockdown||dizzy)field.HitsRemaining=0;
        }
        foreach(var field in _projectiles.Where(p=>p.IsField&&p.HitsRemaining>0))authorizedFieldContacts.Add(field.Id);
    }
    bool ObjectContactAuthorized(ProjectileState field)=>authorizedFieldContacts.Contains(field.Id);
    void OnObjectOwnerInterrupted(PlayerState owner,string reason)
    {
        foreach(var field in _projectiles.Where(p=>p.IsField&&p.Owner==owner.Seat))
            if(reason is "knockdown" or "capture" or "dizzy"||field.ObjectKind=="anchor"&&reason=="hit")field.HitsRemaining=0;
    }
    void ClearObjects(){authorizedFieldContacts.Clear();_projectiles.RemoveAll(p=>p.IsField);foreach(var p in _players){p.ObjectPlacementX=p.ObjectPlacementY=0;p.ObjectInteractionConsumed=false;}}
    void WriteObjectState(BinaryWriter writer)
    {
        foreach(var actor in _players){writer.Write(actor.ObjectPlacementX);writer.Write(actor.ObjectPlacementY);writer.Write(actor.ObjectInteractionConsumed);}
        writer.Write(_projectiles.Count);
        foreach(var p in _projectiles.OrderBy(p=>p.Id))
        {writer.Write(p.Id);writer.Write(p.SpawnTick);writer.Write(p.IsField);writer.Write(p.ObjectKind);writer.Write(p.Age);writer.Write(p.TriggerTicks);writer.Write(p.Vy);writer.Write(p.PreviousY);writer.Write(p.ReflectionDepth);writer.Write(p.OriginalFighter);writer.Write(p.OriginalMoveId);writer.Write(p.OriginalOwner);writer.Write(p.ActionOrdinal);writer.Write(p.Armed);writer.Write(p.Triggered);}
    }
    void ReadObjectState(BinaryReader reader)
    {
        foreach(var actor in _players){actor.ObjectPlacementX=reader.ReadInt32();actor.ObjectPlacementY=reader.ReadInt32();actor.ObjectInteractionConsumed=reader.ReadBoolean();}
        int count=ReadCount(reader,16);if(count!=_projectiles.Count)throw new InvalidDataException("Object metadata count mismatch");var ids=new HashSet<int>();
        for(int n=0;n<count;n++)
        {
            int id=reader.ReadInt32();var p=_projectiles.FirstOrDefault(p=>p.Id==id)??throw new InvalidDataException("Unknown object identity");if(!ids.Add(id))throw new InvalidDataException("Duplicate object metadata");
            p.SpawnTick=reader.ReadInt64();p.IsField=reader.ReadBoolean();p.ObjectKind=reader.ReadString();p.Age=reader.ReadInt32();p.TriggerTicks=reader.ReadInt32();p.Vy=reader.ReadInt32();p.PreviousY=reader.ReadInt32();p.ReflectionDepth=reader.ReadInt32();p.OriginalFighter=reader.ReadString();p.OriginalMoveId=reader.ReadString();p.OriginalOwner=reader.ReadInt32();p.ActionOrdinal=reader.ReadInt32();p.Armed=reader.ReadBoolean();p.Triggered=reader.ReadBoolean();
            if(p.SpawnTick<0||p.SpawnTick>Tick||p.Age<0||p.ReflectionDepth is <0 or >1||p.OriginalOwner is <0 or >1||p.ActionOrdinal<0||p.OriginalMoveId!=p.MoveId||!Content.Fighters.ContainsKey(p.OriginalFighter)||p.IsField&&p.ObjectKind is not("anchor" or "prism"))throw new InvalidDataException("Object metadata bounds");
            p.Definition=ProjectileDefinitionFor(p);
        }
    }
    internal static void ValidateObjectMove(MoveDefinition move,FighterDefinition fighter)
    {
        void Check(bool valid,string reason){if(!valid)throw new InvalidDataException($"{fighter.Id}/{move.Id}: {reason}");}
        if(move.Projectile is {} projectile){Check(projectile.Tier is "normal" or "ex" or "super","Unknown projectile tier");Check(Math.Abs((long)projectile.Vy)<=100000,"Vertical projectile velocity out of bounds");}
        if(move.ObjectRules is not {} rules)return;
        Check(rules.Kind is "anchor" or "prism" or "intercept" or "reflect","Unknown object action");
        Check(move.Projectile is null&&move.Throw is null,"Object actions cannot double-spawn an ordinary projectile or throw");
        if(rules.Kind is "anchor" or "prism")
        {
            Check(move.Active>=1,"Field requires a placement action frame");Check(rules.LifeTicks is >0 and <=3600&&rules.ArmingTicks>=0&&rules.ArmingTicks<rules.LifeTicks,"Invalid field lifetime/arming");
            Check(rules.PlacementNear is >0 and <=500000&&rules.PlacementFar>=rules.PlacementNear&&rules.PlacementFar<=500000,"Invalid field placement");
            Check(rules.Contact.Width is >0 and <=200000&&rules.Contact.Height is >0 and <=300000&&rules.MarkerWidth is >0 and <=200000&&rules.MarkerHeight is >0 and <=300000,"Invalid field geometry");
            Check(rules.Contact.Hits is >0 and <=8&&rules.Contact.RehitTicks is >0 and <=300&&rules.Contact.Level=="mid"&&rules.Contact.Parryable,"Fields require finite contacts, mid guard and parry");
            Check(rules.Contact.Damage is >=0 and <=1000&&rules.Contact.Stun is >=0 and <=1000&&rules.Contact.Hitstun is >=0 and <=300&&rules.Contact.Blockstun is >=0 and <=300&&rules.Contact.JuggleCost is >=0 and <=100,"Invalid field contact payload");
            Check(rules.TriggerRadius is >0 and <=200000&&rules.TriggerWindup is >0 and <=300,"Invalid field trigger");
            Check(move.Hitboxes.Length==0,"Field setter must not duplicate contact hitboxes");
        }
        else
        {
            Check(rules.ActiveStart>=0&&rules.ActiveEnd>rules.ActiveStart&&rules.ActiveEnd<=move.TotalTicks,"Invalid interception interval");
            Check(rules.InteractionBox.Width is >0 and <=200000&&rules.InteractionBox.Height is >0 and <=300000,"Invalid interception geometry");
            Check(move.Hitboxes.Length==0&&move.CreditCost==0,"Interceptors are nonattacking rentals");
        }
    }
    void CopyObjectState(Simulation source)
    {
        // CopyWorld transfers the fully validated actor/projectile objects from its detached temporary.
        // Their scalar metadata and immutable content definitions already travel with those objects.
    }
}
