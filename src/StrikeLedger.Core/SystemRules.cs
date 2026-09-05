namespace StrikeLedger.Core;
public sealed class InputRules
{
 public int HistoryFrames {get;set;}=120; public int MotionTotalWindow {get;set;}=20; public int MotionStepGap {get;set;}=8; public int MotionButtonWindow {get;set;}=5; public int DoubleMotionTotalWindow {get;set;}=30;
 public int ChargeHoldTicks {get;set;}=45; public int ChargeReleaseWindow {get;set;}=10; public int ReversalBufferTicks {get;set;}=2;
 public Dictionary<string,int[]> MotionPatterns {get;set;}=new(StringComparer.Ordinal){["qcf"]=[2,3,6],["qcb"]=[2,1,4],["dp"]=[6,2,3],["double_qcf"]=[2,3,6,2,3,6]};
}
public sealed class GlobalPhysicsRules
{
 public int NormalJumpLandingTicks {get;set;}=3; public int JumpAttackLandingTicks {get;set;}=4; public int DashInputWindow {get;set;}=10;
}
public sealed class ParryRules
{
 public int HighWindow {get;set;}=10; public int LowWindow {get;set;}=10; public int AirWindow {get;set;}=7; public int RedWindow {get;set;}=2;
 public int MissRetryTicks {get;set;}=20; public int DefenderFreeze {get;set;}=8; public int AttackerFreeze {get;set;}=14;
}
public sealed class BlockRules { public int NormalChip {get;set;} public int SpecialChipPercent {get;set;}=10; public int SuperChipPercent {get;set;}=10; }
public sealed class ThrowRules
{
 public int TechWindow {get;set;}=5; public int PostWakeupImmunity {get;set;}=2; public int PostHitstunImmunity {get;set;}=2; public int PostBlockstunImmunity {get;set;}=2;
}
public sealed class StunRules { public int DecayDelay {get;set;}=120; public int DecayPerTick {get;set;}=5; public int DizzyTicks {get;set;}=90; }
public sealed class DamageRules { public int ComboStepPercent {get;set;}=10; public int ComboFloorPercent {get;set;}=30; public int CounterhitPercent {get;set;}=110; }
public sealed class JuggleRules { public int InitialBudget {get;set;}=10; }
public sealed class KnockdownRules { public int QuickRiseInputWindow {get;set;}=5; public int QuickRiseTicks {get;set;}=24; public int NormalRiseTicks {get;set;}=42; public int HardRiseTicks {get;set;}=55; }
public sealed class CombatRules
{
 public int HitstopNormal {get;set;}=8; public int HitstopHeavy {get;set;}=11; public int Blockstop {get;set;}=7;
 public ParryRules Parry {get;set;}=new(); public BlockRules Block {get;set;}=new(); public ThrowRules Throw {get;set;}=new(); public StunRules Stun {get;set;}=new(); public DamageRules Damage {get;set;}=new(); public JuggleRules Juggle {get;set;}=new(); public KnockdownRules Knockdown {get;set;}=new();
}
