namespace StrikeLedger.Core;

/// <summary>Versioned active-move rules. Durations use the actor clock; positions use integer world units.</summary>
public sealed class ActorRulesDefinition
{
 public string Type {get;set;}="";
 public int CounterStart {get;set;} public int CounterEnd {get;set;} public string Riposte {get;set;}="";
 public int ArmorStart {get;set;} public int ArmorEnd {get;set;} public int MaxHoldStartup {get;set;}
 public int MinimumHeight {get;set;} public int FlightTicks {get;set;} public int LandingRecovery {get;set;}
 public int TravelStart {get;set;} public int TravelEnd {get;set;} public int TravelDistance {get;set;}
 public int ApexHeight {get;set;} public int VerticalVelocity {get;set;} public int Gravity {get;set;} public int HorizontalVelocity {get;set;}
 public string TriggerButton {get;set;}="HP";
}
public sealed partial class MoveDefinition { public ActorRulesDefinition? ActorRules {get;set;} }
public sealed partial class PlayerState
{
 public bool ArmorActive {get;internal set;} public bool CounterActive {get;internal set;}
 public int? DestinationX {get;internal set;}
 public int HeldStartupTicks {get;internal set;}
 internal bool ActorArmorConsumed,ActorCounterConsumed,ActorLanded,ActorTravelStopped;
 internal int ActorOriginX,ActorFlightAge,ActorLandingRecovery;
 internal string ActorLandingMove="";
}
public sealed partial class Simulation
{
 ActorRulesDefinition? ActorRules(PlayerState p)=>p.ActionId.Length==0?null:Content.Fighters[p.FighterId].Move(p.ActionId).ActorRules;
 void RefreshActorStatus(PlayerState p)
 {
  var rules=ActorRules(p);
  p.ArmorActive=rules?.Type=="armor_hold"&&!p.ActorArmorConsumed&&p.ActionFrame>=rules.ArmorStart&&p.ActionFrame<rules.ArmorEnd;
  p.CounterActive=rules?.Type=="counter"&&!p.ActorCounterConsumed&&p.ActionFrame>=rules.CounterStart&&p.ActionFrame<rules.CounterEnd;
  if(rules?.Type!="slipgate")p.DestinationX=null;
 }
 MoveDefinition? RecognizeActorMove(PlayerState p,byte direction,Buttons pressed)
 {
  // Returning the equipped command before testing its height/state prevents an illegal input
  // from silently becoming an ordinary air normal or ground throw.
  if(p.Grounded)return null;
  return Content.Fighters[p.FighterId].Moves.FirstOrDefault(m=>Available(p,m)&&m.ActorRules is {} r&&
   (r.Type=="air_throw"&&Has(pressed,Buttons.LP|Buttons.LK)||r.Type=="dive"&&direction==2&&(pressed&Buttons.MK)!=0));
 }
 bool CanStartActorMove(PlayerState p,MoveDefinition move)
 {
  var rules=move.ActorRules;if(rules is null)return true;
  if(rules.Type=="dive")return !p.Grounded&&p.Y>=rules.MinimumHeight&&!p.AirAttack&&p.Vx*p.Facing>0;
  if(rules.Type=="air_throw")return !p.Grounded&&!p.AirAttack;
  return p.Grounded;
 }
 void StartActorMove(PlayerState p,MoveDefinition move)
 {
  p.ArmorActive=p.CounterActive=false;p.ActorArmorConsumed=p.ActorCounterConsumed=p.ActorLanded=p.ActorTravelStopped=false;
  p.HeldStartupTicks=0;p.DestinationX=null;p.ActorOriginX=p.X;p.ActorFlightAge=0;
  var rules=move.ActorRules;if(rules is null)return;
  if(rules.Type is "dive" or "air_throw" or "vault")
  {p.ActorLandingRecovery=rules.LandingRecovery;p.ActorLandingMove=move.Id;}
  if(rules.Type=="vault")p.JumpStart=move.Startup;
  if(rules.Type=="slipgate")p.DestinationX=Math.Clamp(p.X-rules.TravelDistance*p.Facing,_stage.Left+16000,_stage.Right-16000);
 }
 bool AdvanceActorMove(PlayerState p,byte direction,Buttons pressed)
 {
  var rules=ActorRules(p);p.ArmorActive=p.CounterActive=false;if(rules is null)return false;
  p.ArmorActive=rules.Type=="armor_hold"&&!p.ActorArmorConsumed&&p.ActionFrame>=rules.ArmorStart&&p.ActionFrame<rules.ArmorEnd;
  p.CounterActive=rules.Type=="counter"&&!p.ActorCounterConsumed&&p.ActionFrame>=rules.CounterStart&&p.ActionFrame<rules.CounterEnd;
  if(rules.Type=="vault")
  {
   var move=Content.Fighters[p.FighterId].Move(p.ActionId);
   if(p.ActionFrame<move.Startup){p.JumpStart=move.Startup-p.ActionFrame;return true;}
   p.JumpStart=0;
   if(p.ActorFlightAge==0){p.VoluntaryAir=true;p.Vy=rules.VerticalVelocity;p.Vx=rules.HorizontalVelocity*p.ActionFacing;Emit(CombatEventKind.Jump,p.Seat,move:move.Id,detail:"vault");}
   p.ActorFlightAge++;int targetX=p.X+p.Vx;var enemy=_players[1-p.Seat];
   int separation=(Pushbox(p).Width+Pushbox(enemy).Width)/2;
   targetX=p.ActorOriginX<enemy.X?Math.Min(targetX,enemy.X-separation):Math.Max(targetX,enemy.X+separation);
   p.X=targetX;p.Y+=p.Vy;p.Vy-=rules.Gravity;p.X=Math.Clamp(p.X,_stage.Left+16000,_stage.Right-16000);
   if(p.Y<=0||p.ActorFlightAge>=rules.FlightTicks)
   {p.Y=0;p.Vx=p.Vy=0;HandleActorLanding(p);Emit(CombatEventKind.Land,p.Seat,move:move.Id,detail:"vault");}
   return true;
  }
  if(rules.Type=="slipgate")
  {
   if(p.ActionFrame>=rules.TravelStart&&p.ActionFrame<rules.TravelEnd&&!p.ActorTravelStopped)
   {
    int age=p.ActionFrame-rules.TravelStart+1;
    int target=p.ActorOriginX+(int)((long)(p.DestinationX!.Value-p.ActorOriginX)*age/(rules.TravelEnd-rules.TravelStart));
    var enemy=_players[1-p.Seat];var own=Pushbox(p);var other=Pushbox(enemy);
    if(own.Bottom<=other.Top&&own.Top>=other.Bottom)
    {
     int separation=(own.Width+other.Width)/2;
     int bounded=p.ActorOriginX<enemy.X?Math.Min(target,enemy.X-separation):Math.Max(target,enemy.X+separation);
     if(bounded!=target){target=bounded;p.ActorTravelStopped=true;}
    }
    p.X=Math.Clamp(target,_stage.Left+16000,_stage.Right-16000);
   }
   return true;
  }
  if(rules.Type=="dive"&&p.ActionFrame>=Content.Fighters[p.FighterId].Move(p.ActionId).Startup)
  {p.Vx=rules.HorizontalVelocity*p.ActionFacing;p.Vy=-Math.Abs(rules.VerticalVelocity);}
  return false;
 }
 bool HoldActorActionFrame(PlayerState p)
 {
  var rules=ActorRules(p);
  // The flight has its own bounded age. Keep the authored action alive until its
  // landing hook installs recovery, even though this non-attack has zero active frames.
  if(rules?.Type=="vault")return p.ActionFrame>=Content.Fighters[p.FighterId].Move(p.ActionId).Startup;
  if(rules?.Type!="armor_hold")return false;
  var move=Content.Fighters[p.FighterId].Move(p.ActionId);
  var held=p.History.LastOrDefault()?.Held??Buttons.None;
  if(p.ActionFrame!=move.Startup-1||!Enum.TryParse<Buttons>(rules.TriggerButton,out var button)||(held&button)==0||p.HeldStartupTicks>=rules.MaxHoldStartup-move.Startup)return false;
  p.HeldStartupTicks++;return true;
 }
 bool HandleActorLanding(PlayerState p)
 {
  if(p.ActorLandingRecovery<=0)return false;
  p.LandingTicks=Math.Max(p.LandingTicks,p.ActorLandingRecovery);p.ActionId="";p.ActionFrame=0;p.AirAttack=false;p.ActorLanded=true;p.VoluntaryAir=false;
  p.ActorLandingRecovery=0;p.ActorLandingMove="";p.ArmorActive=p.CounterActive=false;p.DestinationX=null;
  p.JuggleBudget=Content.Combat.Juggle.InitialBudget;ClearParry(p);return true;
 }
 bool ActorIgnoresProjectile(PlayerState defender,ProjectileState projectile)
 {
  var rules=ActorRules(defender);
  return rules?.Type=="slipgate"&&defender.ActionFrame>=rules.TravelStart&&defender.ActionFrame<rules.TravelEnd&&
   !projectile.IsField&&projectile.Definition.Tier=="normal"&&ProjectileMove(projectile).Kind is not("super" or "ex_special");
 }
 void ResolveActorCounters(List<ContactCandidate> candidates)
 {
  var catches=new List<(PlayerState Defender,ContactCandidate Contact,string Riposte)>();
  foreach(var defender in _players)
  {
   var rules=ActorRules(defender);if(rules?.Type!="counter"||defender.ActorCounterConsumed||defender.ActionFrame<rules.CounterStart||defender.ActionFrame>=rules.CounterEnd)continue;
   var contact=candidates.Where(c=>c.Defender==defender.Seat&&c.Projectile is null&&c.Move.Kind!="super"&&c.Hit.Level is "mid" or "overhead"&&
    _players[c.Attacker].Grounded&&(_players[c.Attacker].X-defender.X)*defender.ActionFacing>=0).OrderByDescending(c=>c.Hit.Rank).ThenBy(c=>c.Hit.HitGroup).FirstOrDefault();
   if(contact is not null)catches.Add((defender,contact,rules.Riposte));
  }
  foreach(var (defender,contact,riposte) in catches)
  {
   candidates.Remove(contact);_players[contact.Attacker].HitGroups|=1UL<<contact.Hit.HitGroup;
   defender.ActorCounterConsumed=true;StartDerivedAction(defender,riposte);
   Emit(CombatEventKind.CounterCaught,defender.Seat,contact.Attacker,riposte,detail:"grounded-front-strike",worldX:contact.WorldX,worldY:contact.WorldY);
  }
 }
 bool AbsorbActorHit(ContactCandidate contact,int damage)
 {
  var defender=_players[contact.Defender];var rules=ActorRules(defender);
  if(rules?.Type!="armor_hold"||defender.ActorArmorConsumed||defender.ActionFrame<rules.ArmorStart||defender.ActionFrame>=rules.ArmorEnd||
   contact.Projectile is not null||contact.Move.Kind=="super"||contact.Hit.Level=="low")return false;
  defender.ActorArmorConsumed=true;defender.ArmorActive=false;
  // Lethal damage and the stun-limit transition retain the normal interruption path.
  return damage<defender.Health&&(defender.DizzyThisCombo||defender.Stun+contact.Hit.Stun<Content.Fighters[defender.FighterId].StunLimit);
 }
 void ResolveActorAirThrows(List<ContactCandidate> strikes,Buttons[] pressed)
 {
  var attempts=new List<(PlayerState Attacker,PlayerState Defender,MoveDefinition Move)>();
  foreach(var p in _players)
  {
   if(p.ActionId.Length==0||p.Hitstop>0||p.Grounded)continue;
   var move=Content.Fighters[p.FighterId].Move(p.ActionId);var d=_players[1-p.Seat];var grab=move.Throw;
   if(move.ActorRules?.Type!="air_throw"||grab is null||p.ActionFrame<move.Startup||p.ActionFrame>=move.Startup+move.Active||p.HitGroups!=0||
    d.Grounded||d.Hitstun>0||d.Blockstun>0||d.KnockdownTicks>0||d.DizzyTicks>0||d.ThrowAttacker>=0||d.ComboCount>0||d.JuggleBudget<Content.Combat.Juggle.InitialBudget||
    Invulnerable(d,"throw")||strikes.Any(c=>c.Defender==p.Seat))continue;
   int width=(Pushbox(p).Width+Pushbox(d).Width)/2;
   if(Math.Abs(p.X-d.X)<=grab.Range+width&&Math.Abs(p.Y-d.Y)<=36000)attempts.Add((p,d,move));
  }
  if(attempts.Count==2){SeparateTech(_players[0],_players[1],"air-command-clash");return;}
  foreach(var (p,d,move) in attempts)
  {
   p.HitGroups=1;OnActorInterrupted(d,"capture");OnObjectOwnerInterrupted(d,"capture");EndInstall(d);ClearParry(d);
   d.ActionId="";d.ActionFrame=0;d.ThrowAttacker=p.Seat;d.ThrowMove=move.Id;d.ThrowAge=0;
   CompleteThrow(d);d.Vx=2800*p.ActionFacing;d.Vy=3000;d.Y=Math.Max(d.Y,1000);
  }
 }
 void OnActorInterrupted(PlayerState p,string reason)
 {
  p.ArmorActive=p.CounterActive=false;p.DestinationX=null;p.HeldStartupTicks=0;p.VoluntaryAir=false;
  p.ActorLandingRecovery=0;p.ActorLandingMove="";
 }
 void WriteActorState(BinaryWriter writer)
 {
  foreach(var p in _players)
  {
   writer.Write(p.ArmorActive);writer.Write(p.CounterActive);writer.Write(p.DestinationX.HasValue);if(p.DestinationX.HasValue)writer.Write(p.DestinationX.Value);
   writer.Write(p.HeldStartupTicks);writer.Write(p.ActorArmorConsumed);writer.Write(p.ActorCounterConsumed);writer.Write(p.ActorLanded);writer.Write(p.ActorTravelStopped);
   writer.Write(p.ActorOriginX);writer.Write(p.ActorFlightAge);writer.Write(p.ActorLandingRecovery);writer.Write(p.ActorLandingMove);
  }
 }
 void ReadActorState(BinaryReader reader)
 {
  foreach(var p in _players)
  {
   p.ArmorActive=reader.ReadBoolean();p.CounterActive=reader.ReadBoolean();p.DestinationX=reader.ReadBoolean()?reader.ReadInt32():null;
   p.HeldStartupTicks=reader.ReadInt32();p.ActorArmorConsumed=reader.ReadBoolean();p.ActorCounterConsumed=reader.ReadBoolean();p.ActorLanded=reader.ReadBoolean();p.ActorTravelStopped=reader.ReadBoolean();
   p.ActorOriginX=reader.ReadInt32();p.ActorFlightAge=reader.ReadInt32();p.ActorLandingRecovery=reader.ReadInt32();p.ActorLandingMove=reader.ReadString();
   if(p.HeldStartupTicks is <0 or >600||p.ActorFlightAge is <0 or >600||p.ActorLandingRecovery is <0 or >120||
    p.DestinationX is {} x&&(x<_stage.Left||x>_stage.Right)||p.ActorLandingMove.Length>0&&!Content.Fighters[p.FighterId].Moves.Any(m=>m.Id==p.ActorLandingMove&&m.ActorRules is not null))
    throw new InvalidDataException("Invalid actor-rule snapshot");
  }
 }
 internal static void ValidateActorMove(MoveDefinition move,FighterDefinition fighter)
 {
  var rules=move.ActorRules;if(rules is null)return;
  if(rules.Type is not("counter" or "armor_hold" or "vault" or "dive" or "air_throw" or "slipgate")||move.CreditCost!=0||move.Availability=="base"||move.CancelRules.Length>0||move.KaraThrowEligible)
   throw new InvalidDataException("Invalid active rental rules: "+move.Id);
  if(rules.Type=="counter"&&(rules.CounterStart<0||rules.CounterEnd<=rules.CounterStart||rules.CounterEnd>move.TotalTicks||fighter.Move(rules.Riposte).DerivedFrom!=move.Id))throw new InvalidDataException("Invalid counter window/riposte");
  if(rules.Type=="armor_hold"&&(rules.ArmorStart<0||rules.ArmorEnd<=rules.ArmorStart||rules.ArmorEnd>move.Startup||rules.MaxHoldStartup<move.Startup||rules.MaxHoldStartup>120))throw new InvalidDataException("Invalid hold/armor window");
  if(rules.Type=="vault"&&(rules.FlightTicks is <1 or >120||rules.Gravity<=0||rules.VerticalVelocity<=0))throw new InvalidDataException("Invalid vault trajectory");
  if(rules.Type=="slipgate"&&(rules.TravelStart<0||rules.TravelEnd<=rules.TravelStart||rules.TravelEnd>move.TotalTicks||rules.TravelDistance is <1 or >120000))throw new InvalidDataException("Invalid Slipgate trajectory");
  if(rules.Type is "dive" or "air_throw"&&(!move.Command.StartsWith("air:",StringComparison.Ordinal)||rules.LandingRecovery is <1 or >60))throw new InvalidDataException("Invalid air command/landing commitment");
  if(rules.Type=="dive"&&(rules.MinimumHeight<1||rules.VerticalVelocity<=0||move.Hitboxes.Any(h=>h.Level!="mid")))throw new InvalidDataException("Invalid descending heel");
  if(rules.Type=="air_throw"&&(move.Throw is null||move.Throw.Techable))throw new InvalidDataException("Invalid air throw");
 }
}
