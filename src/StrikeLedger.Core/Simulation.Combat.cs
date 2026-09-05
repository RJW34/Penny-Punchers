namespace StrikeLedger.Core;
public sealed partial class Simulation
{
 int ScaledDamage(int damage,int index,bool counterHit=false)
 {
  long baseDamage=(long)damage*(counterHit?Content.Combat.Damage.CounterhitPercent:100)/100;
  return checked((int)(baseDamage*Math.Max(Content.Combat.Damage.ComboFloorPercent,100-Content.Combat.Damage.ComboStepPercent*index)/100));
 }
 void AdvancePlayer(PlayerState p,byte direction,Buttons pressed)
 {
  var f=Content.Fighters[p.FighterId];
  if(p.ThrowImmunity>0)p.ThrowImmunity--;
  if(p.ParryRetry>0)p.ParryRetry--;
  if(p.ThrowAttacker>=0){p.ThrowAge++;if(p.ThrowAge>=Content.Combat.Throw.TechWindow)CompleteThrow(p);return;}
  if(p.StunDelay>0)p.StunDelay--;else if(p.Stun>0)p.Stun=Math.Max(0,p.Stun-Content.Combat.Stun.DecayPerTick);
  if(p.Hitstun>0){p.Hitstun--;if(p.Hitstun==0)p.ThrowImmunity=Content.Combat.Throw.PostHitstunImmunity;}
  if(p.Blockstun>0){p.Blockstun--;if(p.Blockstun==0)p.ThrowImmunity=Content.Combat.Throw.PostBlockstunImmunity;}
  if(p.DizzyTicks>0)p.DizzyTicks--;
  if(p.KnockdownTicks>0&&p.Grounded)
  {
   if(!p.HardKnockdown&&p.KnockdownAge<Content.Combat.Knockdown.QuickRiseInputWindow&&direction==2&&(p.History.Count<2||p.History[^2].RelativeDirection!=2)){p.KnockdownTicks=Math.Min(p.KnockdownTicks,Content.Combat.Knockdown.QuickRiseTicks);Emit(CombatEventKind.Land,p.Seat,detail:"quick-rise");}
   p.KnockdownTicks--;p.KnockdownAge++;if(p.KnockdownTicks==0){p.ThrowImmunity=Content.Combat.Throw.PostWakeupImmunity;Emit(CombatEventKind.Land,p.Seat,detail:"wakeup");}
  }
  if(p.LandingTicks>0)p.LandingTicks--;
  if(p.JumpStart>0)
  {
   p.JumpStart--;if(p.JumpStart==0){p.Vy=p.JumpVelocity;p.Vx=p.JumpHorizontal;Emit(CombatEventKind.Jump,p.Seat);}
  }
  else if(p.DashTicks>0){p.X+=p.DashVelocity;p.DashTicks--;}
  else if(p.ActionId.Length==0&&p.Hitstun==0&&p.Blockstun==0&&p.KnockdownTicks==0&&p.DizzyTicks==0&&p.LandingTicks==0&&p.Grounded)
  {
   p.Crouching=direction is 1 or 2 or 3;p.Vx=0;
   bool edge=p.History.Count==1||p.History[^2].RelativeDirection!=direction;
   if(direction is 7 or 8 or 9)
   {
    p.JumpStart=f.Physics.JumpStartTicks;p.JumpHorizontal=direction==8?0:direction==9?f.Physics.JumpHorizontal*p.Facing:-f.Physics.JumpHorizontal*p.Facing;
    var super=p.History.TakeLast(6).Any(x=>x.RelativeDirection is 1 or 2 or 3);p.JumpVelocity=super?f.Physics.SuperJumpVelocity:f.Physics.JumpVelocity;p.Crouching=false;
   }
   else if(edge&&direction is 4 or 6)
   {
    var last=direction==6?p.LastForwardTap:p.LastBackTap;
    bool released=p.History.Any(h=>h.Tick>last&&h.Tick<Tick&&(h.RelativeDirection==5||h.RelativeDirection==(direction==6?4:6)));
    if(Tick-last<=Content.Physics.DashInputWindow&&released){p.DashTicks=direction==6?f.Physics.DashForwardTicks:f.Physics.DashBackTicks;p.DashVelocity=(direction==6?f.Physics.DashForwardDistance/p.DashTicks:-f.Physics.DashBackDistance/p.DashTicks)*p.Facing;p.Parry=ParryKind.None;p.ParryTicks=0;p.X+=p.DashVelocity;p.DashTicks--;Emit(CombatEventKind.Dash,p.Seat,detail:direction==6?"forward":"back");}
    if(direction==6)p.LastForwardTap=Tick;else p.LastBackTap=Tick;
    if(p.DashTicks==0)p.X+=(direction==6?f.Physics.WalkForward:-f.Physics.WalkBack)*p.Facing;
   }
   else if(direction is 4 or 6)p.X+=(direction==6?f.Physics.WalkForward:-f.Physics.WalkBack)*p.Facing;
  }
  bool authored=false;
  if(p.ActionId.Length>0)
  {
   var m=f.Move(p.ActionId);p.Crouching=m.Command.StartsWith("C+",StringComparison.Ordinal);
   var motion=m.Movement.FirstOrDefault(x=>p.ActionFrame>=x.Start&&p.ActionFrame<x.End);
   if(motion is not null){p.Vx=motion.Vx*p.ActionFacing;p.Vy=motion.Vy;p.X+=p.Vx;p.Y+=p.Vy;authored=true;}
   if(m.Projectile is {} def&&!p.ProjectileSpawned&&p.ActionFrame==def.SpawnTick)
   {
    p.ProjectileSpawned=true;
    if(_projectiles.Count(x=>x.Owner==p.Seat)<def.MaxActivePerOwner)
    {var projectile=new ProjectileState{Id=_nextProjectileId++,Owner=p.Seat,MoveId=m.Id,X=p.X+def.SpawnX*p.ActionFacing,Y=p.Y+def.SpawnY,Vx=def.Vx*p.ActionFacing,LifeTicks=def.LifeTicks,HitsRemaining=def.Hits,Facing=p.ActionFacing,Definition=def};projectile.PreviousX=projectile.X;_projectiles.Add(projectile);Emit(CombatEventKind.ProjectileSpawn,p.Seat,move:m.Id);}
   }
  }
  if(!authored&&(p.Y>0||p.Vy>0)){p.X+=p.Vx;p.Y+=p.Vy;p.Vy-=f.Physics.Gravity;}
  if(p.Y<=0)
  {
   if(p.PreviousY>0){p.Y=0;p.Vx=p.Vy=0;p.KnockdownAge=0;if(p.KnockdownTicks==0&&(p.ActionId.Length==0||p.AirAttack)){p.LandingTicks=p.AirAttack?Content.Physics.JumpAttackLandingTicks:Content.Physics.NormalJumpLandingTicks;p.ActionId="";p.ActionFrame=0;p.AirAttack=false;p.JuggleBudget=Content.Combat.Juggle.InitialBudget;}Emit(CombatEventKind.Land,p.Seat);}
   p.Y=0;if(p.JumpStart==0&&p.PreviousY>0)p.Vy=0;
  }
  p.X=Math.Clamp(p.X,_stage.Left+16000,_stage.Right-16000);
 }
 void FinishPlayerTick(PlayerState p)
 {
  if(p.Hitstop==0)
  {
   if(p.ParryTicks>0&&--p.ParryTicks==0){p.Parry=ParryKind.None;p.ParryRetry=Content.Combat.Parry.MissRetryTicks;}
   if(p.ActionId.Length>0&&++p.ActionFrame>=Content.Fighters[p.FighterId].Move(p.ActionId).TotalTicks){p.ActionId="";p.ActionFrame=0;p.Contact=false;if(p.Grounded)p.Vx=p.Vy=0;}
   if(p.Actionable&&p.Grounded){p.ComboCount=0;p.DizzyThisCombo=false;p.JuggleBudget=Content.Combat.Juggle.InitialBudget;}
  }
 }
 void ResolvePushboxes()
 {
  var a=_players[0];var b=_players[1];var ab=Pushbox(a);var bb=Pushbox(b);if(!ab.Intersects(bb))return;
  var left=a.X<b.X||a.X==b.X&&a.Facing==1?a:b;var right=ReferenceEquals(left,a)?b:a;
  int overlap=Pushbox(left).Right-Pushbox(right).Left;if(overlap<=0)return;
  int moveLeft=overlap/2,moveRight=overlap-moveLeft;
  int availableLeft=left.X-(_stage.Left+16000),availableRight=_stage.Right-16000-right.X;
  if(moveLeft>availableLeft){moveRight+=moveLeft-availableLeft;moveLeft=availableLeft;}if(moveRight>availableRight){moveLeft+=moveRight-availableRight;moveRight=availableRight;}
  left.X-=Math.Min(moveLeft,availableLeft);right.X+=Math.Min(moveRight,availableRight);
 }
 public WorldBox Pushbox(PlayerState p)=>Box(Content.Fighters[p.FighterId].Pushbox,p.X,p.Y,p.Facing);
 static WorldBox Box(BoxDefinition b,int x,int y,int facing)=>new(x+b.X*facing,y+b.Y,b.Width,b.Height);
 public IReadOnlyList<WorldBox> Hurtboxes(int seat)
 {
  var p=_players[seat];var f=Content.Fighters[p.FighterId];var boxes=f.Hurtboxes[!p.Grounded?"air":p.Crouching?"crouching":"standing"].Select(b=>Box(b,p.X,p.Y,p.Facing)).ToList();
  // Active limbs extend the hurt silhouette, keeping visible committed attacks vulnerable.
  if(p.ActionId.Length>0)foreach(var h in f.Move(p.ActionId).Hitboxes.Where(h=>p.ActionFrame>=h.Start&&p.ActionFrame<h.End))boxes.Add(new(p.X+h.X*p.ActionFacing,p.Y+h.Y,Math.Max(6000,h.Width/2),Math.Max(6000,h.Height/2)));
  return boxes;
 }
 public IReadOnlyList<WorldBox> Hitboxes(int seat)
 {
  var p=_players[seat];if(p.ActionId.Length==0)return [];return Content.Fighters[p.FighterId].Move(p.ActionId).Hitboxes.Where(h=>p.ActionFrame>=h.Start&&p.ActionFrame<h.End).Select(h=>Box(h,p.X,p.Y,p.ActionFacing)).ToArray();
 }
 static WorldBox Swept(WorldBox now,int oldX,int oldY)=>new((now.X+oldX)/2,(now.Y+oldY)/2,now.Width+Math.Abs(now.X-oldX),now.Height+Math.Abs(now.Y-oldY));
 void AdvanceProjectiles()
 {
  foreach(var p in _projectiles)
  {
   p.PreviousX=p.X;if(p.Hitstop>0){p.Hitstop--;continue;}p.X+=p.Vx;p.LifeTicks--;if(p.Definition.TurnAfterTicks>0&&p.Definition.LifeTicks-p.LifeTicks==p.Definition.TurnAfterTicks){p.Vx=-p.Vx;p.Facing=-p.Facing;}if(p.ContactCooldown>0)p.ContactCooldown--;
  }
  for(int i=0;i<_projectiles.Count;i++)for(int j=i+1;j<_projectiles.Count;j++)
  {
   var a=_projectiles[i];var b=_projectiles[j];if(a.Owner==b.Owner||a.HitsRemaining<=0||b.HitsRemaining<=0)continue;
   var aa=new WorldBox(a.X,a.Y,a.Definition.Width,a.Definition.Height);var bb=new WorldBox(b.X,b.Y,b.Definition.Width,b.Definition.Height);
   if(!Swept(aa,a.PreviousX,a.Y).Intersects(Swept(bb,b.PreviousX,b.Y)))continue;
   if(a.Definition.Rank<=b.Definition.Rank)a.HitsRemaining=0;if(b.Definition.Rank<=a.Definition.Rank)b.HitsRemaining=0;
   Emit(CombatEventKind.ProjectileClash,a.Owner,b.Owner,a.MoveId);
  }
  _projectiles.RemoveAll(p=>p.LifeTicks<=0||p.HitsRemaining<=0||p.X<_stage.Left-50000||p.X>_stage.Right+50000);
 }
 sealed record ContactCandidate(int Attacker,int Defender,MoveDefinition Move,HitboxDefinition Hit,ProjectileState? Projectile,int Facing,bool Blocked,bool Parried,bool CounterHit);
 bool Invulnerable(PlayerState defender,string type)=>defender.ActionId.Length>0&&Content.Fighters[defender.FighterId].Move(defender.ActionId).Invulnerability.Any(i=>defender.ActionFrame>=i.Start&&defender.ActionFrame<i.End&&i.To.Contains(type));
 bool CanParry(PlayerState d,HitboxDefinition h)=>h.Parryable&&d.ParryTicks>0&&d.Parry switch{ParryKind.Air=>true,ParryKind.High or ParryKind.RedHigh=>h.Level!="low",ParryKind.Low or ParryKind.RedLow=>h.Level is "low" or "mid",_=>false};
 bool CanBlock(PlayerState d,HitboxDefinition h,byte direction)=>d.Grounded&&d.ActionId.Length==0&&d.Hitstun==0&&d.KnockdownTicks==0&&d.DizzyTicks==0&&d.JumpStart==0&&d.DashTicks==0&&d.ThrowAttacker<0&&(direction==4&&h.Level!="low"||direction==1&&h.Level is "low" or "mid");
 void ResolveContacts(byte[] directions,Buttons[] pressed)
 {
  var candidates=new List<ContactCandidate>();
  for(int s=0;s<2;s++)
  {
   var p=_players[s];var d=_players[1-s];if(p.ActionId.Length==0||p.Hitstop>0||d.KnockdownTicks>0&&d.Grounded||d.ThrowAttacker>=0)continue;var move=Content.Fighters[p.FighterId].Move(p.ActionId);
   if(Invulnerable(d,"strike"))continue;
   foreach(var h in move.Hitboxes)
   {
    if(p.ActionFrame<h.Start||p.ActionFrame>=h.End||(p.HitGroups&(1UL<<h.HitGroup))!=0||!d.Grounded&&d.JuggleBudget<h.JuggleCost)continue;
    var box=Box(h,p.X,p.Y,p.ActionFacing);var sweep=Swept(box,p.PreviousX+h.X*p.ActionFacing,p.PreviousY+h.Y);
    if(!Hurtboxes(d.Seat).Any(sweep.Intersects))continue;
    candidates.Add(new(s,d.Seat,move,h,null,p.ActionFacing,CanBlock(d,h,directions[d.Seat]),CanParry(d,h),d.ActionId.Length>0&&d.ActionFrame<Content.Fighters[d.FighterId].Move(d.ActionId).Startup));
   }
  }
  foreach(var p in _projectiles)
  {
   var d=_players[1-p.Owner];if(p.Hitstop>0||p.ContactCooldown>0||d.KnockdownTicks>0&&d.Grounded||d.ThrowAttacker>=0||Invulnerable(d,"projectile")||!d.Grounded&&d.JuggleBudget<p.Definition.JuggleCost)continue;
   var def=p.Definition;var box=Swept(new(p.X,p.Y,def.Width,def.Height),p.PreviousX,p.Y);if(!Hurtboxes(d.Seat).Any(box.Intersects))continue;
   var h=new HitboxDefinition{Damage=def.Damage,Stun=def.Stun,Rank=def.Rank,Hitstun=def.Hitstun,Blockstun=def.Blockstun,Level=def.Level,JuggleCost=def.JuggleCost,Parryable=def.Parryable,PushX=9000};
   candidates.Add(new(p.Owner,d.Seat,Content.Fighters[_players[p.Owner].FighterId].Move(p.MoveId),h,p,p.Facing,CanBlock(d,h,directions[d.Seat]),CanParry(d,h),false));
  }
  // Only reciprocal direct strikes participate in rank comparison; distant attacks do not clash.
  var direct=candidates.Where(c=>c.Projectile is null).ToArray();
  candidates.RemoveAll(c=>c.Projectile is null&&direct.Any(other=>other.Attacker==c.Defender&&other.Defender==c.Attacker&&other.Hit.Rank>c.Hit.Rank));
  var armConsumed=new bool[2];var consumedGroups=new HashSet<(int,int)>();
  foreach(var c in candidates.OrderByDescending(c=>c.Hit.Rank).ThenBy(c=>c.Projectile?.Id??0).ThenBy(c=>c.Attacker).ThenBy(c=>c.Hit.HitGroup))
  {
   if(c.Projectile is null&&!consumedGroups.Add((c.Attacker,c.Hit.HitGroup)))continue;
   var p=_players[c.Attacker];var d=_players[c.Defender];if(c.Projectile is null)p.HitGroups|=1UL<<c.Hit.HitGroup;
   if(c.Projectile is {} projectile){projectile.HitsRemaining--;projectile.ContactCooldown=projectile.Definition.RehitTicks;}
   if(c.Parried&&!armConsumed[c.Defender])
   {
    var parryKind=d.Parry;armConsumed[c.Defender]=true;d.Parry=ParryKind.None;d.ParryTicks=d.ParryRetry=0;d.Blockstun=0;d.Hitstop=Math.Max(d.Hitstop,Content.Combat.Parry.DefenderFreeze);
    if(c.Projectile is {} pr)pr.Hitstop=Content.Combat.Parry.AttackerFreeze;else p.Hitstop=Math.Max(p.Hitstop,Content.Combat.Parry.AttackerFreeze);Emit(CombatEventKind.Parry,d.Seat,p.Seat,c.Move.Id,detail:parryKind+":"+c.Hit.Level);continue;
   }
   if(c.Blocked)
   {
    var chip=c.Move.Kind is "special" or "ex_special" or "super"?ScaledDamage(c.Hit.Damage,d.ComboCount)*(c.Move.Kind=="super"?Content.Combat.Block.SuperChipPercent:Content.Combat.Block.SpecialChipPercent)/100:Content.Combat.Block.NormalChip;
    d.Health=Math.Max(0,d.Health-chip);d.Blockstun=Math.Max(d.Blockstun,c.Hit.Blockstun);d.Hitstop=Math.Max(d.Hitstop,Content.Combat.Blockstop);if(c.Projectile is null){p.Hitstop=Math.Max(p.Hitstop,Content.Combat.Blockstop);p.Contact=true;}PushBack(p,d,c.Facing,c.Hit.PushX);Emit(CombatEventKind.Block,d.Seat,p.Seat,c.Move.Id,chip);continue;
   }
   int damage=ScaledDamage(c.Hit.Damage,d.ComboCount,c.CounterHit);d.Health=Math.Max(0,d.Health-damage);d.ComboCount++;d.Stun+=c.Hit.Stun;d.StunDelay=Content.Combat.Stun.DecayDelay;d.Hitstun=Math.Max(d.Hitstun,c.Hit.Hitstun);d.Blockstun=0;d.ActionId="";d.ActionFrame=0;d.DashTicks=d.JumpStart=0;d.Parry=ParryKind.None;d.ParryTicks=0;
   int stop=c.Hit.Rank>=3?Content.Combat.HitstopHeavy:Content.Combat.HitstopNormal;d.Hitstop=Math.Max(d.Hitstop,stop);if(c.Projectile is null){p.Hitstop=Math.Max(p.Hitstop,stop);p.Contact=true;}
   if(!d.Grounded)d.JuggleBudget=Math.Max(0,d.JuggleBudget-c.Hit.JuggleCost);
   PushBack(p,d,c.Facing,c.Hit.PushX);
   if(c.Hit.LaunchY>0){d.Vy=c.Hit.LaunchY;d.Y=Math.Max(1,d.Y);d.Vx=c.Facing*1300;}
   if(c.Hit.Knockdown!="none")KnockDown(d,c.Hit.Knockdown);
   if(d.Stun>=Content.Fighters[d.FighterId].StunLimit&&!d.DizzyThisCombo){d.Stun=0;d.DizzyTicks=Content.Combat.Stun.DizzyTicks;d.DizzyThisCombo=true;Emit(CombatEventKind.Dizzy,d.Seat);}
   Emit(CombatEventKind.Hit,p.Seat,d.Seat,c.Move.Id,damage,c.CounterHit?"counter":"");
  }
  _projectiles.RemoveAll(p=>p.HitsRemaining<=0);
  ResolveThrows(candidates,pressed);ResolvePushboxes();
 }
 void PushBack(PlayerState attacker,PlayerState defender,int facing,int distance)
 {
  int target=defender.X+facing*distance;int clamped=Math.Clamp(target,_stage.Left+16000,_stage.Right-16000);int spill=target-clamped;defender.X=clamped;
  attacker.X=Math.Clamp(attacker.X-spill,_stage.Left+16000,_stage.Right-16000);
 }
 void KnockDown(PlayerState p,string kind)
 {
  p.HardKnockdown=kind=="hard";p.KnockdownTicks=p.HardKnockdown?Content.Combat.Knockdown.HardRiseTicks:Content.Combat.Knockdown.NormalRiseTicks;p.KnockdownAge=0;p.Hitstun=0;p.ActionId="";Emit(CombatEventKind.Knockdown,p.Seat,detail:kind);
 }
 void ResolveThrows(List<ContactCandidate> strikes,Buttons[] pressed)
 {
  var attempts=new List<(PlayerState Attacker,PlayerState Defender,MoveDefinition Move)>();
  foreach(var p in _players)
  {
   if(p.ActionId.Length==0||p.Hitstop>0)continue;var m=Content.Fighters[p.FighterId].Move(p.ActionId);var d=_players[1-p.Seat];var t=m.Throw;
   if(t is null||p.ActionFrame<m.Startup||p.ActionFrame>=m.Startup+m.Active||p.HitGroups!=0||!p.Grounded||!d.Grounded||d.JumpStart>0||d.ThrowImmunity>0||d.Hitstun>0||d.Blockstun>0||d.KnockdownTicks>0||d.ThrowAttacker>=0||Invulnerable(d,"throw")||strikes.Any(x=>x.Defender==p.Seat))continue;
   if(Math.Abs(p.X-d.X)>t.Range+(Content.Fighters[p.FighterId].Pushbox.Width+Content.Fighters[d.FighterId].Pushbox.Width)/2)continue;
   attempts.Add((p,d,m));
  }
  if(attempts.Count==2&&attempts.All(a=>a.Move.Throw!.Techable)){SeparateTech(_players[0],_players[1]);return;}
  foreach(var (p,d,m) in attempts)
  {
   p.HitGroups=1;if(m.Throw!.Techable&&Has(pressed[d.Seat],Buttons.LP|Buttons.LK)){SeparateTech(p,d);continue;}
   d.ActionId="";d.ActionFrame=0;d.ThrowAttacker=p.Seat;d.ThrowMove=m.Id;d.ThrowAge=0;d.Parry=ParryKind.None;d.ParryTicks=0;
   if(!m.Throw.Techable)CompleteThrow(d);else Emit(CombatEventKind.Throw,p.Seat,d.Seat,m.Id,detail:"capture");
  }
 }
 void TechThrow(PlayerState defender){if(defender.ThrowAttacker>=0)SeparateTech(_players[defender.ThrowAttacker],defender);}
 void SeparateTech(PlayerState a,PlayerState b)
 {
  a.ThrowAttacker=b.ThrowAttacker=-1;a.ActionId=b.ActionId="";a.Hitstun=b.Hitstun=0;a.LandingTicks=b.LandingTicks=10;a.ThrowImmunity=b.ThrowImmunity=2;int sign=a.X<b.X?1:-1;
  a.X=Math.Clamp(a.X-sign*18000,_stage.Left+16000,_stage.Right-16000);b.X=Math.Clamp(b.X+sign*18000,_stage.Left+16000,_stage.Right-16000);Emit(CombatEventKind.ThrowTech,a.Seat,b.Seat);
 }
 void CompleteThrow(PlayerState d)
 {
  var p=_players[d.ThrowAttacker];var move=Content.Fighters[p.FighterId].Move(d.ThrowMove);var t=move.Throw!;d.ThrowAttacker=-1;
  int damage=ScaledDamage(t.Damage,d.ComboCount);d.Health=Math.Max(0,d.Health-damage);d.ComboCount++;d.Stun+=t.Stun;d.StunDelay=Content.Combat.Stun.DecayDelay;
  if(t.SwapSides){(p.X,d.X)=(d.X,p.X);p.Facing=-p.Facing;d.Facing=-d.Facing;p.FacingEpoch++;d.FacingEpoch++;p.BackCharge=d.BackCharge=0;p.BackReadyUntil=d.BackReadyUntil=-1;}
  KnockDown(d,t.Knockdown);d.Hitstop=Content.Combat.HitstopNormal;p.Hitstop=Content.Combat.HitstopNormal;Emit(CombatEventKind.Throw,p.Seat,d.Seat,move.Id,damage,"damage");
  if(d.Stun>=Content.Fighters[d.FighterId].StunLimit&&!d.DizzyThisCombo){d.Stun=0;d.DizzyTicks=Content.Combat.Stun.DizzyTicks;d.DizzyThisCombo=true;Emit(CombatEventKind.Dizzy,d.Seat);}
 }
}
