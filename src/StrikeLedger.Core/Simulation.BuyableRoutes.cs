namespace StrikeLedger.Core;

public sealed class BranchDefinition
{
 public string Button {get;set;}="P";public string Target {get;set;}="";public int Start {get;set;}public int End {get;set;}
 public string Contact {get;set;}="none";public bool RelativeToContact {get;set;}
}
public sealed class JumpCancelDefinition { public int Start {get;set;} public int End {get;set;}=4; }
public sealed class InstallEdgeDefinition { public string From {get;set;}="";public string To {get;set;}="";public int Start {get;set;}public int End {get;set;} }
public sealed class InstallDefinition { public int DurationTicks {get;set;}=240;public InstallEdgeDefinition[] Edges {get;set;}=[]; }
public sealed class MimicDefinition { public string SourceMoveId {get;set;}="";public int SharedTicks {get;set;}=6;public int AbortTicks {get;set;}=6; }
public sealed partial class MoveDefinition
{
 public string CatalogId {get;set;}="";public string DerivedFrom {get;set;}="";public BranchDefinition[] Branches {get;set;}=[];public JumpCancelDefinition? JumpCancel {get;set;}
 public InstallDefinition? Install {get;set;}public MimicDefinition? Mimic {get;set;}
}
public sealed partial class PlayerState
{
 public bool HitContact {get;internal set;}public int FirstHitFrame {get;internal set;}=-1;public int FirstContactFrame {get;internal set;}=-1;
 public string InstallId {get;internal set;}="";public int InstallTicks {get;internal set;}
}
public sealed partial class Simulation
{
 bool BranchOpen(PlayerState p,BranchDefinition b)
 {
  if(b.Contact=="hit"&&!p.HitContact||b.Contact=="contact"&&!p.Contact)return false;
  int origin=b.RelativeToContact?(b.Contact=="hit"?p.FirstHitFrame:p.FirstContactFrame):0;
  return origin>=0&&p.ActionFrame>=origin+b.Start&&p.ActionFrame<origin+b.End;
 }
 MoveDefinition? RecognizeBranchMove(PlayerState p,Buttons pressed)
 {
  if(p.ActionId.Length==0)return null;var f=Content.Fighters[p.FighterId];
  foreach(var b in f.Move(p.ActionId).Branches)
  {
   var mask=b.Button=="P"?Buttons.LP|Buttons.MP|Buttons.HP:Buttons.LK|Buttons.MK|Buttons.HK;
   if(CountButtons(pressed&mask)==1&&BranchOpen(p,b))return f.Move(b.Target);
  }
  // Overtime deliberately selects the exact graph node, bypassing proximity substitution only for its explicit edge.
  if(p.InstallTicks>0&&p.HitContact&&p.Grounded)
  {
   var install=f.Move(p.InstallId).Install!;
   foreach(var edge in install.Edges.Where(e=>e.From==p.ActionId&&p.ActionFrame>=e.Start&&p.ActionFrame<e.End))
   {
    var target=f.Move(edge.To);var button=Enum.Parse<Buttons>(edge.To[2..].ToUpperInvariant());
    if((pressed&button)!=0)return target;
   }
  }
  return null;
 }
 bool CanBranch(PlayerState p,MoveDefinition target)=>p.ActionId.Length>0&&Content.Fighters[p.FighterId].Move(p.ActionId).Branches.Any(b=>b.Target==target.Id&&BranchOpen(p,b));
 bool CanStartInstall(PlayerState p,MoveDefinition move)=>move.Install is null||p.InstallTicks==0;
 bool CanInstallChain(PlayerState p,MoveDefinition target)=>p.InstallTicks>0&&p.HitContact&&p.Grounded&&Content.Fighters[p.FighterId].Move(p.InstallId).Install!.Edges.Any(e=>e.From==p.ActionId&&e.To==target.Id&&p.ActionFrame>=e.Start&&p.ActionFrame<e.End);
 void TryJumpCancel(PlayerState p,byte direction,bool frozen)
 {
  if(frozen||p.Hitstop>0||!p.Grounded||direction is not(7 or 8 or 9)||p.LastDirection is 7 or 8 or 9||!p.HitContact||p.ActionId.Length==0)return;
  var f=Content.Fighters[p.FighterId];var cancel=f.Move(p.ActionId).JumpCancel;if(cancel is null||p.ActionFrame<p.FirstHitFrame+cancel.Start||p.ActionFrame>=p.FirstHitFrame+cancel.End)return;
  // Source contact/defender juggle state are retained; this only transitions the attacker to ordinary jump startup.
  p.RootContinuation=true;p.ActionId="";p.ActionFrame=0;p.Contact=false;p.HitContact=false;ClearParry(p);p.JumpStart=f.Physics.JumpStartTicks;p.JumpVelocity=f.Physics.JumpVelocity;
  p.JumpHorizontal=direction==8?0:direction==9?f.Physics.JumpHorizontal*p.Facing:-f.Physics.JumpHorizontal*p.Facing;p.Crouching=false;
  Emit(CombatEventKind.ActionStarted,p.Seat,move:"jump_cancel",detail:"hit-only");
 }
 void CompleteInstallActivation(PlayerState p)
 {
  var move=Content.Fighters[p.FighterId].Move(p.ActionId);if(move.Install is not {} install)return;
  p.InstallId=move.Id;p.InstallTicks=install.DurationTicks;Emit(CombatEventKind.InstallStarted,p.Seat,move:move.Id,detail:"install-active");
 }
 void AdvanceInstalls(){foreach(var p in _players)if(p.InstallTicks>0&&--p.InstallTicks==0)EndInstall(p);}
 void EndInstall(PlayerState p){p.InstallId="";p.InstallTicks=0;}
 void WriteRouteState(BinaryWriter w)
 {
  foreach(var p in _players){w.Write(p.HitContact);w.Write(p.FirstHitFrame);w.Write(p.FirstContactFrame);w.Write(p.InstallId);w.Write(p.InstallTicks);}
 }
 void ReadRouteState(BinaryReader r)
 {
  foreach(var p in _players)
  {
   p.HitContact=r.ReadBoolean();p.FirstHitFrame=r.ReadInt32();p.FirstContactFrame=r.ReadInt32();p.InstallId=r.ReadString();p.InstallTicks=r.ReadInt32();
   if(p.InstallTicks is <0 or >3600||p.FirstHitFrame is < -1 or >600||p.FirstContactFrame is < -1 or >600||p.InstallTicks>0&&(p.InstallId.Length==0||Content.Fighters[p.FighterId].Move(p.InstallId).Install is null))throw new InvalidDataException("Invalid route/install snapshot");
  }
 }
 internal static void ValidateRouteMove(MoveDefinition m,FighterDefinition f)
 {
  foreach(var b in m.Branches)
  {
   var target=f.Move(b.Target);if(b.Button is not("P" or "K")||b.Contact is not("none" or "hit" or "contact")||b.Start<0||b.End<=b.Start||b.End>m.TotalTicks||target.CreditCost!=0||target.Availability!=m.Availability||b.Target==m.Id)throw new InvalidDataException("Invalid branch "+m.Id);
  }
  bool BranchCycle(string node,HashSet<string> path){if(!path.Add(node))return true;foreach(var edge in f.Move(node).Branches)if(BranchCycle(edge.Target,new(path)))return true;return false;}
  if(m.Branches.Length>0&&BranchCycle(m.Id,[]))throw new InvalidDataException("Branch graph must be finite and acyclic");
  if(m.JumpCancel is {} j&&(j.Start<0||j.End<=j.Start||j.End>20||m.Hitboxes.Length==0))throw new InvalidDataException("Invalid jump cancel "+m.Id);
  if(m.Mimic is {} mimic){f.Move(mimic.SourceMoveId);if(mimic.SharedTicks<1||mimic.SharedTicks+mimic.AbortTicks!=m.TotalTicks||m.Hitboxes.Length>0||m.Projectile is not null||m.CreditCost!=0)throw new InvalidDataException("Invalid mimic "+m.Id);}
  if(m.Install is {} install)
  {
   if(install.DurationTicks is <1 or >3600||m.Kind!="super"||m.Hitboxes.Length>0||install.Edges.Length is <1 or >32)throw new InvalidDataException("Invalid install "+m.Id);
   foreach(var e in install.Edges)
   {
    var source=f.Move(e.From);var target=f.Move(e.To);if(source.Kind!="normal"||target.Kind!="normal"||e.From.Length!=4||e.To.Length!=4||!e.From.StartsWith("s_")&&!e.From.StartsWith("c_")||e.From[..2]!=e.To[..2]||e.From[3]!=e.To[3]||"lmh".IndexOf(e.To[2])!="lmh".IndexOf(e.From[2])+1||e.Start<source.Startup||e.End<=e.Start||e.End>source.TotalTicks)throw new InvalidDataException("Invalid HIT-only install edge");
   }
   // Reject graph cycles independently of ID/strength naming.
   bool Cycle(string node,HashSet<string> path){if(!path.Add(node))return true;foreach(var e in install.Edges.Where(e=>e.From==node))if(Cycle(e.To,new(path)))return true;return false;}
   if(install.Edges.Any(e=>Cycle(e.From,[])))throw new InvalidDataException("Install graph must be acyclic");
  }
 }
}
