using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
namespace StrikeLedger.Core;

public class BoxDefinition { public int X {get;set;} public int Y {get;set;} public int Width {get;set;} public int Height {get;set;} }
public sealed class HitboxDefinition : BoxDefinition
{
 public string Id {get;set;}=""; public int Start {get;set;} public int End {get;set;} public string Level {get;set;}="mid";
 public int Damage {get;set;} public int Stun {get;set;} public int Rank {get;set;} public int Hitstun {get;set;} public int Blockstun {get;set;}
 public int PushX {get;set;} public int LaunchY {get;set;} public string Knockdown {get;set;}="none"; public int HitGroup {get;set;} public int JuggleCost {get;set;} public bool Parryable {get;set;}=true;
}
public sealed class MovementDefinition { public int Start {get;set;} public int End {get;set;} public int Vx {get;set;} public int Vy {get;set;} }
public sealed class CancelDefinition { public int Start {get;set;} public int End {get;set;} public bool RequiresContact {get;set;} public string[] Targets {get;set;}=[]; }
public sealed class InvulnerabilityDefinition { public int Start {get;set;} public int End {get;set;} public string[] To {get;set;}=[]; }
public sealed class ThrowDefinition { public int Range {get;set;} public int Damage {get;set;} public int Stun {get;set;} public bool Techable {get;set;} public bool SwapSides {get;set;} public string Knockdown {get;set;}="hard"; }
public sealed class ProjectileDefinition : BoxDefinition
{
 public int SpawnTick {get;set;} public int SpawnX {get;set;} public int SpawnY {get;set;} public int Vx {get;set;} public int LifeTicks {get;set;} public int Hits {get;set;} public int RehitTicks {get;set;}
 public int Damage {get;set;} public int Stun {get;set;} public int Hitstun {get;set;} public int Blockstun {get;set;} public string Level {get;set;}="mid"; public int MaxActivePerOwner {get;set;} public int Rank {get;set;} public int JuggleCost {get;set;} public bool Parryable {get;set;}=true; public int TurnAfterTicks {get;set;}
}
public sealed class MoveDefinition
{
 public string Id {get;set;}=""; public string Name {get;set;}=""; public string Kind {get;set;}="normal"; public string Command {get;set;}="";
 public int Startup {get;set;} public int Active {get;set;} public int Recovery {get;set;} public HitboxDefinition[] Hitboxes {get;set;}=[];
 public CancelDefinition[] CancelRules {get;set;}=[]; public bool NegativeEdge {get;set;} public string Availability {get;set;}="base"; public string Pose {get;set;}="";
 public InvulnerabilityDefinition[] Invulnerability {get;set;}=[]; public MovementDefinition[] Movement {get;set;}=[]; public ProjectileDefinition? Projectile {get;set;} public ThrowDefinition? Throw {get;set;}
 public int CreditCost {get;set;} public bool DebitOnStart {get;set;} public int SuperFreeze {get;set;} public bool KaraThrowEligible {get;set;} public int KaraThrowTicks {get;set;} public string DesignRole {get;set;}="";
 public int TotalTicks => Startup+Active+Recovery;
}
public sealed class FighterPhysics
{
 public int WalkForward {get;set;} public int WalkBack {get;set;} public int DashForwardTicks {get;set;} public int DashForwardDistance {get;set;} public int DashBackTicks {get;set;} public int DashBackDistance {get;set;}
 public int JumpStartTicks {get;set;} public int JumpVelocity {get;set;} public int SuperJumpVelocity {get;set;} public int JumpHorizontal {get;set;} public int Gravity {get;set;} public int ProximityThreshold {get;set;}
}
public sealed class SuperArtDefinition { public string Id {get;set;}=""; public string Name {get;set;}=""; public string MoveId {get;set;}=""; public int CreditCost {get;set;} public string Role {get;set;}=""; }
public sealed class TargetComboDefinition { public string From {get;set;}=""; public string To {get;set;}=""; public int Start {get;set;} public int End {get;set;} public bool RequiresContact {get;set;} }
public sealed class FighterDefinition
{
 public string Id {get;set;}=""; public string DisplayName {get;set;}=""; public string Archetype {get;set;}=""; public int Health {get;set;} public int StunLimit {get;set;}
 public FighterPhysics Physics {get;set;}=new(); public BoxDefinition Pushbox {get;set;}=new(); public Dictionary<string,BoxDefinition[]> Hurtboxes {get;set;}=[];
 public SuperArtDefinition[] SuperArts {get;set;}=[]; public MoveDefinition[] Moves {get;set;}=[]; public string DefaultSuper {get;set;}="art_1"; public TargetComboDefinition[] TargetCombos {get;set;}=[];
 public MoveDefinition Move(string id)=>Moves.First(x=>x.Id==id);
}
public sealed class ItemDefinition
{
 public string Id {get;set;}=""; public string Name {get;set;}=""; public string Slot {get;set;}=""; public int Price {get;set;} public string[] EligibleFighters {get;set;}=[];
 public string MoveId {get;set;}=""; public string? Replaces {get;set;} public string Description {get;set;}=""; public string Tradeoff {get;set;}="";
}
public sealed class StageDefinition { public string Id {get;set;}="foundry"; public string Name {get;set;}="Foundry Ring"; public int Left {get;set;} public int Right {get;set;}=768000; public int Floor {get;set;} public int[] SpawnX {get;set;}=[288000,480000]; public int CameraWidth {get;set;}=480000; }
public sealed class GameContent
{
 public IReadOnlyDictionary<string,FighterDefinition> Fighters {get;private init;}=new Dictionary<string,FighterDefinition>();
 public IReadOnlyDictionary<string,ItemDefinition> Items {get;private init;}=new Dictionary<string,ItemDefinition>();
 public IReadOnlyDictionary<string,StageDefinition> Stages {get;private init;}=new Dictionary<string,StageDefinition>();
 public EconomyRules Economy {get;private init;}=new(600,3600,1200,900,[900,1200,1500]);
 public CombatRules Combat {get;private init;}=new(); public InputRules Inputs {get;private init;}=new(); public GlobalPhysicsRules Physics {get;private init;}=new();
 public string ContentHash {get;private init;}=""; public int RoundTicks {get;private init;}=3600; public int CountdownTicks {get;private init;}=120; public int PreparationTicks {get;private init;}=900; public int RevealTicks {get;private init;}=180;
 static readonly JsonSerializerOptions JsonOptions=new(){PropertyNamingPolicy=JsonNamingPolicy.SnakeCaseLower};
 public static GameContent Load(string directory)
 {
  T Read<T>(string path)
  {
   var text=File.ReadAllText(Path.Combine(directory,path));if(text.Length>2000000)throw new InvalidDataException("Content file exceeds bound");using var document=JsonDocument.Parse(text);
   if(!document.RootElement.TryGetProperty("version",out var version)||version.GetInt32()!=1)throw new InvalidDataException("Unsupported content version: "+path);
   return JsonSerializer.Deserialize<T>(text,JsonOptions)??throw new InvalidDataException(path);
  }
  var fighters=Directory.GetFiles(Path.Combine(directory,"fighters"),"*.json").Order(StringComparer.Ordinal).Select(x=>Read<FighterDefinition>(Path.GetRelativePath(directory,x))).ToDictionary(x=>x.Id,StringComparer.Ordinal);
  var stages=Directory.GetFiles(Path.Combine(directory,"stages"),"*.json").Order(StringComparer.Ordinal).Select(x=>Read<StageDefinition>(Path.GetRelativePath(directory,x))).ToDictionary(x=>x.Id,StringComparer.Ordinal);
  using var itemsJson=JsonDocument.Parse(File.ReadAllText(Path.Combine(directory,"items.json")));
  var items=JsonSerializer.Deserialize<ItemDefinition[]>(itemsJson.RootElement.GetProperty("items"),JsonOptions)!.ToDictionary(x=>x.Id,StringComparer.Ordinal);
  using var rules=JsonDocument.Parse(File.ReadAllText(Path.Combine(directory,"rules.json")));
  using var economy=JsonDocument.Parse(File.ReadAllText(Path.Combine(directory,"economy.json")));
  if(fighters.Count!=2||!fighters.ContainsKey("rook")||!fighters.ContainsKey("vale")||items.Count!=12||stages.Count!=2)throw new InvalidDataException("Bounded content roster mismatch");
  if(rules.RootElement.GetProperty("tick_hz").GetInt32()!=60||rules.RootElement.GetProperty("max_players").GetInt32()!=2||rules.RootElement.GetProperty("max_rounds").GetInt32()!=9||rules.RootElement.GetProperty("target_half_points").GetInt32()!=10||economy.RootElement.GetProperty("resource_model").GetString()!="one_persistent_scalar_credit_wallet")throw new InvalidDataException("Unsupported fighting/resource contract");
  foreach(var stage in stages.Values)if(stage.Right-stage.Left!=768000||stage.Floor!=0||stage.SpawnX.Length!=2||stage.SpawnX.Any(x=>x<stage.Left+16000||x>stage.Right-16000))throw new InvalidDataException("Invalid stage geometry");
  var combat=Read<CombatRules>("combat.json");var inputs=Read<InputRules>("inputs.json");var physics=Read<GlobalPhysicsRules>("physics.json");
  if(inputs.HistoryFrames is <30 or >120||inputs.MotionTotalWindow is <1 or >120||inputs.MotionStepGap is <1 or >30||inputs.MotionButtonWindow is <1 or >30||inputs.ChargeHoldTicks is <1 or >120||inputs.ChargeReleaseWindow is <1 or >30||inputs.ReversalBufferTicks is <0 or >3||inputs.MotionPatterns.Count!=4||inputs.MotionPatterns.Values.Any(p=>p.Length is <2 or >8||p.Any(d=>d is <1 or >9)))throw new InvalidDataException("Invalid input bounds");
  if(combat.Parry.HighWindow is <1 or >30||combat.Parry.LowWindow is <1 or >30||combat.Parry.AirWindow is <1 or >30||combat.Parry.RedWindow is <1 or >10||combat.Damage.ComboFloorPercent is <1 or >100||combat.Damage.ComboStepPercent is <0 or >100||combat.Damage.CounterhitPercent is <100 or >200||combat.Juggle.InitialBudget is <1 or >100||combat.Throw.TechWindow is <1 or >15)throw new InvalidDataException("Invalid combat bounds");
  foreach(var f in fighters.Values)
  {
   if(f.Moves.Length!=49||f.Moves.Select(x=>x.Id).Distinct().Count()!=49||f.Health<=0||f.Health>10000||f.StunLimit<=0)throw new InvalidDataException("Invalid fighter "+f.Id);
   if(f.SuperArts.Length!=3||f.SuperArts.Select(a=>a.Id).Distinct().Count()!=3||f.Physics.WalkForward is <1 or >10000||f.Physics.WalkBack is <1 or >10000||f.Physics.DashForwardTicks is <1 or >120||f.Physics.DashBackTicks is <1 or >120||f.Physics.Gravity is <1 or >10000||f.Physics.JumpVelocity is <1 or >50000)throw new InvalidDataException("Invalid fighter movement/art bounds "+f.Id);
   // Starter body shapes used bottom-left coordinates; attacks use the documented center convention.
   foreach(var box in f.Hurtboxes.Values.SelectMany(x=>x).Append(f.Pushbox)){ValidateBox(box);box.X+=box.Width/2;box.Y+=box.Height/2;}
   foreach(var m in f.Moves)
   {
    if(m.TotalTicks<=0||m.TotalTicks>600||m.Startup<0||m.Active<0||m.Recovery<0||m.CreditCost<0||m.CreditCost>3600)throw new InvalidDataException("Invalid move "+m.Id);
    if((m.CreditCost>0)!=m.DebitOnStart||m.CreditCost>0&&m.Kind is not("super" or "ex_special"))throw new InvalidDataException("Invalid activation "+m.Id);
    if(m.Availability!="base"&&(!items.TryGetValue(m.Availability,out var item)||item.MoveId!=m.Id||!item.EligibleFighters.Contains(f.Id)))throw new InvalidDataException("Invalid lease "+m.Id);
    foreach(var h in m.Hitboxes){ValidateBox(h);if(h.Start<m.Startup||h.End<=h.Start||h.End>m.Startup+m.Active||h.Damage<0||h.Damage>1000||h.HitGroup is <0 or >63||h.Hitstun is <0 or >600||h.Blockstun is <0 or >600||h.Level is not("mid" or "low" or "overhead" or "air"))throw new InvalidDataException("Invalid hitbox "+m.Id);}
    foreach(var movement in m.Movement)if(movement.Start<0||movement.End<=movement.Start||movement.End>m.TotalTicks||Math.Abs((long)movement.Vx)>50000||Math.Abs((long)movement.Vy)>50000)throw new InvalidDataException("Invalid movement "+m.Id);
    foreach(var cancel in m.CancelRules)if(cancel.Start<0||cancel.End<=cancel.Start||cancel.End>m.TotalTicks||cancel.Targets.Any(t=>t is not("special" or "ex_special" or "selected_super")))throw new InvalidDataException("Invalid cancel "+m.Id);
    foreach(var invulnerability in m.Invulnerability)if(invulnerability.Start<0||invulnerability.End<=invulnerability.Start||invulnerability.End>m.TotalTicks||invulnerability.To.Any(t=>t is not("strike" or "projectile" or "throw")))throw new InvalidDataException("Invalid invulnerability "+m.Id);
    if(m.Throw is {} t&&(t.Range is <1 or >150000||t.Damage is <0 or >1000))throw new InvalidDataException("Invalid throw "+m.Id);
    if(m.Projectile is {} p){ValidateBox(p);if(p.SpawnTick<0||p.SpawnTick>=m.TotalTicks||p.Hits is <1 or >16||p.LifeTicks is <1 or >600||p.RehitTicks<1||p.MaxActivePerOwner is <1 or >8||Math.Abs((long)p.Vx)>50000||p.TurnAfterTicks<0||p.TurnAfterTicks>p.LifeTicks)throw new InvalidDataException("Invalid projectile "+m.Id);}
   }
   foreach(var art in f.SuperArts)if(f.Move(art.MoveId).CreditCost!=art.CreditCost)throw new InvalidDataException("Super cost mismatch");
   foreach(var t in f.TargetCombos){f.Move(t.From);f.Move(t.To);}
  }
  foreach(var i in items.Values){if(i.Price<0||i.Price>1800||!new[]{"signature","technique","gambit"}.Contains(i.Slot)||i.EligibleFighters.Any(x=>!fighters.ContainsKey(x)))throw new InvalidDataException("Invalid item "+i.Id);foreach(var fighter in i.EligibleFighters){fighters[fighter].Move(i.MoveId);if(i.Replaces is not null)fighters[fighter].Move(i.Replaces);}}
  using var hash=IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
  foreach(var p in Directory.GetFiles(directory,"*.json",SearchOption.AllDirectories).OrderBy(x=>Path.GetRelativePath(directory,x).Replace('\\','/'),StringComparer.Ordinal)){hash.AppendData(Encoding.UTF8.GetBytes(Path.GetRelativePath(directory,p).Replace('\\','/')));hash.AppendData(File.ReadAllBytes(p));}
  return new GameContent{Fighters=fighters,Items=items,Stages=stages,Economy=EconomyRules.Load(Path.Combine(directory,"economy.json")),Combat=combat,Inputs=inputs,Physics=physics,ContentHash=Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant(),RoundTicks=rules.RootElement.GetProperty("round_ticks").GetInt32(),CountdownTicks=rules.RootElement.GetProperty("countdown_ticks").GetInt32(),PreparationTicks=economy.RootElement.GetProperty("preparation_seconds").GetInt32()*60,RevealTicks=economy.RootElement.GetProperty("reveal_seconds").GetInt32()*60};
 }
 static void ValidateBox(BoxDefinition b){if(b.Width<=0||b.Width>768000||b.Height<=0||b.Height>400000)throw new InvalidDataException("Invalid box extents");}
}
