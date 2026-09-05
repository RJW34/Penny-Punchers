namespace StrikeLedger.Core;
public enum MatchPhase { Preparation, Reveal, Countdown, Fight, PendingResult, RoundResult, MatchOver }
public enum ParryKind { None, High, Low, Air, RedHigh, RedLow }
public enum CombatEventKind { ActionStarted, Spend, Rejected, Hit, Block, Parry, Throw, ThrowTech, ProjectileSpawn, ProjectileClash, Knockdown, Dizzy, RoundTerminal, Settlement, Preparation, Jump, Dash, Land }
public sealed record CombatEvent(long Tick,CombatEventKind Kind,int Seat,int Target=-1,string MoveId="",int Value=0,string Detail="");
public sealed record StepResult(long Tick,IReadOnlyList<CombatEvent> Events,string Hash);
public sealed record MatchConfig
{
 public string Fighter0 {get;init;}="rook"; public string Fighter1 {get;init;}="vale"; public string Super0 {get;init;}="art_1"; public string Super1 {get;init;}="art_1";
 public string SessionId {get;init;}="local"; public string StageId {get;init;}="foundry"; public bool Training {get;init;} public bool Assist {get;init;}
}
public sealed record PreparationPlan(string[] ItemIds,int ReserveFloor=0);
public sealed record PreparationReceipt(string Key,int Round,int Cost0,int Cost1,int Credits0,int Credits1);
public sealed record TerminalResult(int WinnerSeat,long TerminalTick,string Reason);
public sealed record SettlementReceipt(string Key,int Round,int WinnerSeat,PayoutReceipt Payout0,PayoutReceipt Payout1,string MatchDecision);
public sealed class SimulationSnapshot
{
 readonly byte[] _bytes; public byte[] Bytes=>_bytes.ToArray();
 public SimulationSnapshot(byte[] bytes){if(bytes.Length is <16 or >2000000)throw new ArgumentException("Invalid snapshot size");_bytes=bytes.ToArray();}
}
public sealed record InputSample(long Tick,byte RawDirection,byte RelativeDirection,Buttons Held,int FacingEpoch);
public sealed class PlayerState
{
 public int Seat {get;internal set;} public string FighterId {get;internal set;}=""; public string SelectedSuper {get;internal set;}="";
 public int Credits {get;internal set;} public int ReserveFloor {get;internal set;} public int RecoveryTier {get;internal set;} public int ScoreHalfPoints {get;internal set;}
 public int Health {get;internal set;} public int MaxHealth {get;internal set;} public int Stun {get;internal set;}
 public int X {get;internal set;} public int Y {get;internal set;} public int Vx {get;internal set;} public int Vy {get;internal set;} public int Facing {get;internal set;}=1;
 public int FacingEpoch {get;internal set;} public string ActionId {get;internal set;}=""; public int ActionFrame {get;internal set;} public int ActionFacing {get;internal set;}=1;
 public int Hitstop {get;internal set;} public int Hitstun {get;internal set;} public int Blockstun {get;internal set;} public int KnockdownTicks {get;internal set;} public int DizzyTicks {get;internal set;}
 public bool Grounded=>Y==0; public bool Crouching {get;internal set;} public bool Contact {get;internal set;} public int ComboCount {get;internal set;} public int JuggleBudget {get;internal set;}=10;
 public ParryKind Parry {get;internal set;} public int ParryTicks {get;internal set;} public int ParryRetry {get;internal set;} public int BackCharge {get;internal set;} public int DownCharge {get;internal set;}
 public int ThrowImmunity {get;internal set;} public int JumpStart {get;internal set;} public int LandingTicks {get;internal set;} public int DashTicks {get;internal set;}
 public IReadOnlyList<string> Leases=>LeaseIds.AsReadOnly(); public IReadOnlyList<SpendReceipt> SpendReceipts=>Receipts.AsReadOnly(); public IReadOnlyList<InputSample> InputHistory=>History.AsReadOnly();
 internal List<string> LeaseIds=[]; internal List<SpendReceipt> Receipts=[]; internal List<InputSample> History=[];
 internal long BackReadyUntil=-1,DownReadyUntil=-1,LastForwardTap=-100,LastBackTap=-100; internal int LastDirection=5,NeutralTicks=1;
 internal Buttons LastButtons,SuppressNegativeEdge; internal int StunDelay,KnockdownAge,ActionOrdinal,JumpHorizontal,JumpVelocity,DashVelocity,ThrowAge; internal bool HardKnockdown,DizzyThisCombo,AirAttack,ProjectileSpawned;
 internal ulong HitGroups; internal string BufferedAction=""; internal long BufferExpires=-1; internal bool BufferedReversal; internal ParryKind DeferredParry; internal int ThrowAttacker=-1; internal string ThrowMove=""; internal int PreviousX,PreviousY;
 public bool Actionable=>Hitstop==0&&Hitstun==0&&Blockstun==0&&KnockdownTicks==0&&DizzyTicks==0&&JumpStart==0&&LandingTicks==0&&DashTicks==0&&ThrowAttacker<0&&ActionId.Length==0;
}
public sealed class ProjectileState
{
 public int Id {get;internal set;} public int Owner {get;internal set;} public string MoveId {get;internal set;}=""; public int X {get;internal set;} public int Y {get;internal set;} public int Vx {get;internal set;}
 public int LifeTicks {get;internal set;} public int HitsRemaining {get;internal set;} public int Hitstop {get;internal set;} public int ContactCooldown {get;internal set;} public int Facing {get;internal set;}
 internal int PreviousX; public ProjectileDefinition Definition {get;internal set;}=new();
}
public readonly record struct WorldBox(int X,int Y,int Width,int Height)
{
 public int Left=>X-Width/2; public int Right=>X+Width/2; public int Bottom=>Y-Height/2; public int Top=>Y+Height/2;
 public bool Intersects(WorldBox b)=>Left<=b.Right&&Right>=b.Left&&Bottom<=b.Top&&Top>=b.Bottom;
}
