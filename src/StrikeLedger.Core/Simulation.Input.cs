namespace StrikeLedger.Core;
public sealed partial class Simulation
{
 IReadOnlyDictionary<string,int[]> Patterns=>Content.Inputs.MotionPatterns;
 void Sample(PlayerState p,InputFrame input,byte direction)
 {
  p.History.Add(new(Tick,input.Direction,direction,input.Held,p.FacingEpoch));if(p.History.Count>Content.Inputs.HistoryFrames)p.History.RemoveAt(0);
  if(direction is 1 or 4 or 7){p.BackCharge++;if(p.BackCharge>=Content.Inputs.ChargeHoldTicks)p.BackReadyUntil=Tick+Content.Inputs.ChargeReleaseWindow;}else p.BackCharge=0;
  if(direction is 1 or 2 or 3){p.DownCharge++;if(p.DownCharge>=Content.Inputs.ChargeHoldTicks)p.DownReadyUntil=Tick+Content.Inputs.ChargeReleaseWindow;}else p.DownCharge=0;
 }
 public bool RecognizesMotion(int seat,string pattern)
 {
  var p=_players[seat];if(pattern=="charge_back")return p.History.LastOrDefault()?.RelativeDirection is 3 or 6 or 9&&p.BackReadyUntil>=Tick;
  if(pattern=="charge_down")return p.History.LastOrDefault()?.RelativeDirection is 7 or 8 or 9&&p.DownReadyUntil>=Tick;
  if(!Patterns.TryGetValue(pattern,out var target))return false;
  int cursor=p.History.Count-1;while(cursor>=0&&p.History[cursor].RelativeDirection==5)cursor--;
  if(cursor<0||Tick-p.History[cursor].Tick>Content.Inputs.MotionButtonWindow)return false;
  long end=p.History[cursor].Tick,previous=end;
  for(int i=target.Length-1;i>=0;i--)
  {
   if(cursor<0)return false;var entry=p.History[cursor];if(entry.FacingEpoch!=p.FacingEpoch||entry.RelativeDirection!=target[i]||previous-entry.Tick>Content.Inputs.MotionStepGap)return false;
   previous=entry.Tick;while(cursor>=0&&p.History[cursor].RelativeDirection==target[i])cursor--;
  }
  return end-previous<=(pattern=="double_qcf"?Content.Inputs.DoubleMotionTotalWindow:Content.Inputs.MotionTotalWindow);
 }
 MoveDefinition? Recognize(PlayerState p,byte direction,Buttons pressed,Buttons released)
 {
  var f=Content.Fighters[p.FighterId];var available=f.Moves.Where(m=>Available(p,m)).ToArray();
  bool Button(string token,Buttons edge)=>token switch{"P"=>(edge&(Buttons.LP|Buttons.MP|Buttons.HP))!=0,"K"=>(edge&(Buttons.LK|Buttons.MK|Buttons.HK))!=0,"PP"=>CountButtons(edge&(Buttons.LP|Buttons.MP|Buttons.HP))>=2,"KK"=>CountButtons(edge&(Buttons.LK|Buttons.MK|Buttons.HK))>=2,_=>Enum.TryParse<Buttons>(token,out var b)&&(edge&b)!=0};
  bool MotionMatch(MoveDefinition m)
  {
   var split=m.Command.Split('+');if(split.Length!=2||!Patterns.ContainsKey(split[0])&&!split[0].StartsWith("charge_",StringComparison.Ordinal))return false;
   return RecognizesMotion(p.Seat,split[0])&&(Button(split[1],pressed)||m.CreditCost==0&&m.NegativeEdge&&CountButtons(released)==1&&Button(split[1],released));
  }
  if(pressed!=Buttons.None||released!=Buttons.None)
  {
   foreach(var kind in new[]{"super","ex_special","special"})
   {
    // DP is checked before QCF; available lease commands do not duplicate the base kit.
    var result=available.Where(m=>m.Kind==kind).OrderByDescending(m=>m.Command.StartsWith("dp+",StringComparison.Ordinal)).FirstOrDefault(MotionMatch);if(result is not null)return result;
   }
  }
  if(Has(pressed,Buttons.LP|Buttons.LK))return f.Move(direction is 1 or 4 or 7?"throw_back":"throw_forward");
  if(Has(pressed,Buttons.MP|Buttons.MK))return f.Move("leap_overhead");
  if(Has(pressed,Buttons.HP|Buttons.HK))
  {
   var gambit=available.FirstOrDefault(m=>m.Kind=="gambit");if(gambit is not null&&direction is 1 or 4 or 7)return gambit;
   // Cosmetic taunt uses the same committed duration as the available feint, without buffs.
   if(p.Actionable&&p.Grounded){p.DashTicks=24;p.DashVelocity=0;Emit(CombatEventKind.ActionStarted,p.Seat,move:"taunt");}return null;
  }
  if(pressed==Buttons.None)return null;
  var button=new[]{Buttons.HP,Buttons.HK,Buttons.MP,Buttons.MK,Buttons.LP,Buttons.LK}.First(b=>(pressed&b)!=0);
  if(!p.Grounded)return f.Move("j_"+button.ToString().ToLowerInvariant());
  if(direction==6&&button==Buttons.HP)return available.First(m=>m.Command=="6+HP");
  if(direction is 1 or 2 or 3)return f.Move("c_"+button.ToString().ToLowerInvariant());
  if(p.ActionId.Length>0)
  {
   var target=f.TargetCombos.FirstOrDefault(t=>t.From==p.ActionId&&p.ActionFrame>=t.Start&&p.ActionFrame<t.End&&(!t.RequiresContact||p.Contact)&&f.Move(t.To).Command.EndsWith("+"+button,StringComparison.Ordinal));
   if(target is not null)return f.Move(target.To);
  }
  if(button is Buttons.MP or Buttons.HP&&Math.Abs(_players[1-p.Seat].X-p.X)<=f.Physics.ProximityThreshold)return f.Move("close_"+button.ToString().ToLowerInvariant());
  return f.Move("s_"+button.ToString().ToLowerInvariant());
 }
 static int CountButtons(Buttons b)=>System.Numerics.BitOperations.PopCount((uint)b);
 bool Available(PlayerState p,MoveDefinition m)
 {
  if(m.Kind=="super"&&Content.Fighters[p.FighterId].SuperArts.First(x=>x.Id==p.SelectedSuper).MoveId!=m.Id)return false;
  if(m.Availability!="base"&&!p.LeaseIds.Contains(m.Availability))return false;
  if(p.LeaseIds.Any(x=>Content.Items[x].Replaces==m.Id))return false;return true;
 }
 bool CanTransition(PlayerState p,MoveDefinition move,bool ignoreFreeze)
 {
  if(Phase!=MatchPhase.Fight||!Available(p,move)||p.Health==0||p.ThrowAttacker>=0||p.Hitstun>0||p.Blockstun>0||p.KnockdownTicks>0||p.DizzyTicks>0||p.JumpStart>0||p.LandingTicks>0||p.DashTicks>0||!ignoreFreeze&&p.Hitstop>0)return false;
  if(move.Command.StartsWith("J+",StringComparison.Ordinal)!=!p.Grounded)return false;
  if(move.Projectile is {} projectile&&_projectiles.Count(x=>x.Owner==p.Seat)>=projectile.MaxActivePerOwner)return false;
  if(p.ActionId.Length==0)return true;var f=Content.Fighters[p.FighterId];var current=f.Move(p.ActionId);
  if(move.Kind=="throw"&&current.KaraThrowEligible&&p.ActionFrame<current.KaraThrowTicks)return true;
  if(f.TargetCombos.Any(t=>t.From==p.ActionId&&t.To==move.Id&&p.ActionFrame>=t.Start&&p.ActionFrame<t.End&&(!t.RequiresContact||p.Contact)))return true;
  return current.CancelRules.Any(c=>p.ActionFrame>=c.Start&&p.ActionFrame<c.End&&(!c.RequiresContact||p.Contact)&&c.Targets.Contains(move.Kind=="super"?"selected_super":move.Kind));
 }
 public ActivationStatus TryStartAction(int seat,string moveId)
 {
  var p=_players[seat];var move=Content.Fighters[p.FighterId].Move(moveId);if(!CanTransition(p,move,false))return ActivationStatus.Illegal;
  if(p.Credits<move.CreditCost){Emit(CombatEventKind.Rejected,seat,move:move.Id,value:move.CreditCost,detail:"credits");return ActivationStatus.Insufficient;}
  if(p.Credits-move.CreditCost<p.ReserveFloor){Emit(CombatEventKind.Rejected,seat,move:move.Id,value:move.CreditCost,detail:"reserve");return ActivationStatus.Reserve;}
  p.ActionOrdinal++;p.ActionId=move.Id;p.ActionFrame=0;p.ActionFacing=p.Facing;p.Contact=false;p.HitGroups=0;p.ProjectileSpawned=false;p.AirAttack=!p.Grounded;p.BufferedAction="";p.Credits-=move.CreditCost;p.Parry=ParryKind.None;p.ParryTicks=0;
  if(move.CreditCost>0){var key=$"{Config.SessionId}:{RoundId}:{Tick}:{seat}:{p.ActionOrdinal}";p.Receipts.Add(new(key,move.Id,move.CreditCost));Emit(CombatEventKind.Spend,seat,move:move.Id,value:move.CreditCost);}
  if(move.Command.StartsWith("charge_back",StringComparison.Ordinal)){p.BackReadyUntil=-1;p.BackCharge=0;}if(move.Command.StartsWith("charge_down",StringComparison.Ordinal)){p.DownReadyUntil=-1;p.DownCharge=0;}
  FullFreeze=Math.Max(FullFreeze,move.SuperFreeze);Emit(CombatEventKind.ActionStarted,seat,move:move.Id,value:move.CreditCost,detail:p.BufferedReversal?"reversal":"");p.BufferedReversal=false;return move.CreditCost==0?ActivationStatus.Free:ActivationStatus.Paid;
 }
 void UpdateFacing(PlayerState p,PlayerState opponent)
 {
  if(!p.Actionable)return;var facing=opponent.X==p.X?p.Facing:opponent.X>p.X?1:-1;if(facing==p.Facing)return;
  p.Facing=facing;p.FacingEpoch++;p.BackCharge=0;p.BackReadyUntil=-1;p.LastForwardTap=p.LastBackTap=-100;p.BufferedAction="";
 }
 void UpdateParryEdge(PlayerState p,byte dir,bool frozen)
 {
  if(p.Hitstun>0||p.KnockdownTicks>0||p.DizzyTicks>0||p.ThrowAttacker>=0||p.ActionId.Length>0||p.DashTicks>0||p.JumpStart>0||p.LandingTicks>0||p.ParryRetry>0)return;
  var edge=p.LastDirection==5&&dir is 2 or 6;
  ParryKind kind=ParryKind.None;
  if(edge)kind=!p.Grounded&&dir==6?ParryKind.Air:p.Grounded?(p.Blockstun>0?(dir==2?ParryKind.RedLow:ParryKind.RedHigh):(dir==2?ParryKind.Low:ParryKind.High)):ParryKind.None;
  if(frozen){if(kind!=ParryKind.None)p.DeferredParry=kind;return;}
  if(p.DeferredParry!=ParryKind.None){kind=p.DeferredParry;p.DeferredParry=ParryKind.None;}
  if(kind!=ParryKind.None){p.Parry=kind;p.ParryTicks=kind is ParryKind.RedHigh or ParryKind.RedLow?Content.Combat.Parry.RedWindow:kind==ParryKind.Air?Content.Combat.Parry.AirWindow:kind==ParryKind.Low?Content.Combat.Parry.LowWindow:Content.Combat.Parry.HighWindow;}
 }
}
