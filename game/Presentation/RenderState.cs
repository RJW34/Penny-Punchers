namespace StrikeLedger.Presentation;

/// <summary>Presentation-only snapshot. Rendering never modifies simulation state.</summary>
public sealed class ArenaRenderState
{
    public FighterRenderState[] Fighters { get; set; } = [];
    public ProjectileRenderState[] Projectiles { get; set; } = [];
    public FieldRenderState[] Fields { get; set; } = [];
    public bool TrainingGrid { get; set; }
    public string StageId { get; set; } = "foundry";
    public string Phase { get; set; } = "Fight";
    public bool Frozen { get; set; }
    public bool DebugBoxes { get; set; }
    public bool ReducedFlashes { get; set; }
    public float ShakeScale { get; set; } = 1;
    public int Tick { get; set; }
    public string Banner { get; set; } = "";
}

public sealed class FighterRenderState
{
    public string Id { get; set; } = "rook";
    public string State { get; set; } = "idle";
    public string MoveId { get; set; } = "";
    public int X { get; set; } = 288000;
    public int Y { get; set; }
    public int Facing { get; set; } = 1;
    public int ActionFrame { get; set; }
    public int Startup { get; set; } = 3;
    public int Active { get; set; } = 2;
    public int Recovery { get; set; } = 12;
    public int Strength { get; set; } = 1;
    public int Palette { get; set; }
    public bool Grounded { get; set; } = true;
    public bool Frozen { get; set; }
    public string MimicSourceMoveId { get; set; } = "";
    public int MimicSharedTicks { get; set; }
    public int MimicSourceStartup { get; set; }
    public int MimicSourceActive { get; set; }
    public int InstallTicks { get; set; }
    public bool ArmorActive { get; set; }
    public bool CounterActive { get; set; }
    public int? DestinationX { get; set; }
    public RenderBox[] Boxes { get; set; } = [];
}

public sealed class ProjectileRenderState
{
    public int Id { get; set; }
    public string FighterId { get; set; } = "rook";
    public string MoveId { get; set; } = "";
    public int Age { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public int Facing { get; set; } = 1;
    public int Owner { get; set; }
    public bool IsSuper { get; set; }
    public int Width { get; set; } = 22000;
    public int Height { get; set; } = 14000;
    public int VelocityX { get; set; }
    public int VelocityY { get; set; }
    public int ReflectionDepth { get; set; }
}

/// <summary>Actual Core object state, including its arm/trigger phase and remaining contacts.</summary>
public sealed class FieldRenderState
{
    public int Id { get; set; }
    public string Kind { get; set; } = "";
    public int Owner { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public int Age { get; set; }
    public int LifeTicks { get; set; }
    public int HitsRemaining { get; set; }
    public int ContactCooldown { get; set; }
    public int ArmingTicks { get; set; }
    public bool Armed { get; set; }
    public bool Triggered { get; set; }
    public int TriggerTicks { get; set; } = -1;
    public int TriggerWindup { get; set; }
    public int TriggerRadius { get; set; }
    public int MarkerWidth { get; set; }
    public int MarkerHeight { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
}

public sealed class RenderBox
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public string Kind { get; set; } = "hurt";
}
