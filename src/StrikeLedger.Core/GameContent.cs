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
/// <summary>Authored center-coordinate body/limb shapes on the frozen action clock; intervals are half-open.</summary>
public sealed class HurtboxWindowDefinition { public int Start {get;set;} public int End {get;set;} public bool ReplaceBody {get;set;} public BoxDefinition[] Boxes {get;set;}=[]; public string Intent {get;set;}=""; }
public sealed class ThrowDefinition { public int Range {get;set;} public int Damage {get;set;} public int Stun {get;set;} public bool Techable {get;set;} public bool SwapSides {get;set;} public string Knockdown {get;set;}="hard"; }
public sealed partial class ProjectileDefinition : BoxDefinition
{
 public int SpawnTick {get;set;} public int SpawnX {get;set;} public int SpawnY {get;set;} public int Vx {get;set;} public int LifeTicks {get;set;} public int Hits {get;set;} public int RehitTicks {get;set;}
 public int Damage {get;set;} public int Stun {get;set;} public int Hitstun {get;set;} public int Blockstun {get;set;} public string Level {get;set;}="mid"; public int MaxActivePerOwner {get;set;} public int Rank {get;set;} public int JuggleCost {get;set;} public bool Parryable {get;set;}=true; public int TurnAfterTicks {get;set;}
}
public sealed partial class MoveDefinition
{
 public string Id {get;set;}=""; public string Name {get;set;}=""; public string Kind {get;set;}="normal"; public string Command {get;set;}="";
 public int Startup {get;set;} public int Active {get;set;} public int Recovery {get;set;} public HitboxDefinition[] Hitboxes {get;set;}=[];
 public HurtboxWindowDefinition[] HurtboxWindows {get;set;}=[];
 public CancelDefinition[] CancelRules {get;set;}=[]; public bool NegativeEdge {get;set;} public string Availability {get;set;}="base"; public string Pose {get;set;}="";
 public InvulnerabilityDefinition[] Invulnerability {get;set;}=[]; public MovementDefinition[] Movement {get;set;}=[]; public ProjectileDefinition? Projectile {get;set;} public ThrowDefinition? Throw {get;set;}
 public int CreditCost {get;set;} public bool DebitOnStart {get;set;} public int SuperFreeze {get;set;} public bool KaraThrowEligible {get;set;} public int KaraThrowTicks {get;set;} public string DesignRole {get;set;}="";
 public string AccessPolicy {get;set;}="base";
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
 public string CatalogId {get;set;}="";public string? LegacyItemId {get;set;}
 public string AccessPolicy {get;set;}="round_license";public string ImplementationStatus {get;set;}="implemented";public string[] ConflictsWith {get;set;}=[];
 public string Duration {get;set;}=""; public int CommitFrame {get;set;} public bool RefundAfterLock {get;set;} public bool AltersSystemDefense {get;set;} public int ActivationCreditCost {get;set;} public int? UseLimit {get;set;} public string Debit {get;set;}="";
 public string MoveId {get;set;}=""; public string? Replaces {get;set;} public string Description {get;set;}=""; public string Tradeoff {get;set;}="";
}
public sealed class StageDefinition { public string Id {get;set;}="foundry"; public string Name {get;set;}="Foundry Ring"; public int Left {get;set;} public int Right {get;set;}=768000; public int Floor {get;set;} public int[] SpawnX {get;set;}=[288000,480000]; public int CameraWidth {get;set;}=480000; }
public sealed partial class GameContent
{
 public IReadOnlyDictionary<string,FighterDefinition> Fighters {get;private init;}=new Dictionary<string,FighterDefinition>();
 public IReadOnlyDictionary<string,ItemDefinition> Items {get;private init;}=new Dictionary<string,ItemDefinition>();
 public IReadOnlyDictionary<string,StageDefinition> Stages {get;private init;}=new Dictionary<string,StageDefinition>();
 public EconomyRules Economy {get;private init;}=new(600,3600,1200,900,[900,1200,1500]);
 public CombatRules Combat {get;private init;}=new(); public InputRules Inputs {get;private init;}=new(); public GlobalPhysicsRules Physics {get;private init;}=new();
 public string ContentHash {get;private init;}=""; public int RoundTicks {get;private init;}=3600; public int CountdownTicks {get;private init;}=120; public int PreparationTicks {get;private init;}=900; public int RevealTicks {get;private init;}=180;
 static readonly JsonSerializerOptions JsonOptions=new(){PropertyNamingPolicy=JsonNamingPolicy.SnakeCaseLower};
 public static GameContent Load(string directory,string? rulesetId=null)
 {
  directory=Path.GetFullPath(directory);
  if(rulesetId is not null && rulesetId is not("buyables_core" or "pp-shop-only-v2-core"))
  {
   if(rulesetId is not("buyables_full" or "pp-shop-only-v2-full"))throw new InvalidDataException("Unknown ruleset selection");
   directory=Path.Combine(directory,"rulesets","buyables_full");
  }
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
  if(fighters.Count is <1 or >32||items.Count>256||stages.Count is <1 or >64)throw new InvalidDataException("Content registry exceeds semantic bounds");
  var econ=EconomyRules.Load(Path.Combine(directory,"economy.json"));
  var slotPrices=economy.RootElement.GetProperty("slot_prices").EnumerateObject().ToDictionary(x=>x.Name,x=>x.Value.GetInt32(),StringComparer.Ordinal);
  var preparation=new PreparationRules(economy.RootElement.GetProperty("loadout_cap").GetInt32(),new System.Collections.ObjectModel.ReadOnlyDictionary<string,int>(slotPrices),economy.RootElement.GetProperty("default_reserve_floor").GetInt32(),economy.RootElement.GetProperty("ex_activation_cost").GetInt32()){SlotLimits=economy.RootElement.GetProperty("slot_limits").EnumerateObject().ToDictionary(x=>x.Name,x=>x.Value.GetInt32(),StringComparer.Ordinal)};
  var rewards=JsonSerializer.Deserialize<SkillRewardPolicy>(economy.RootElement.GetProperty("reward_policy"),JsonOptions)!;rewards.Validate();
  if(slotPrices.Count is <1 or >8||slotPrices.Any(x=>string.IsNullOrWhiteSpace(x.Key)||x.Value<0||x.Value>econ.Cap)||preparation.LoadoutCap<0||preparation.LoadoutCap>econ.Cap||preparation.DefaultReserveFloor<0||preparation.DefaultReserveFloor>econ.StartingCredits||preparation.ExActivationCost!=0||preparation.DefaultReserveFloor!=0||preparation.SlotLimits.Values.Any(x=>x<1||x>6)||preparation.MaxProducts>6)throw new InvalidDataException("Invalid preparation rules");
  var rj=rules.RootElement;
  var matchRules=new MatchRules(rj.GetProperty("ruleset_id").GetString()!,rj.GetProperty("tick_hz").GetInt32(),rj.GetProperty("max_players").GetInt32(),rj.GetProperty("max_rounds").GetInt32(),rj.GetProperty("target_half_points").GetInt32(),rj.GetProperty("round_ticks").GetInt32(),rj.GetProperty("countdown_ticks").GetInt32(),rj.GetProperty("default_stage").GetString()!);
  if(!stages.ContainsKey(matchRules.DefaultStage)||matchRules.RoundTicks is <60 or >36000||matchRules.CountdownTicks is <0 or >3600)throw new InvalidDataException("Invalid match timing/default stage");
  if(rules.RootElement.GetProperty("tick_hz").GetInt32()!=60||rules.RootElement.GetProperty("max_players").GetInt32()!=2||rules.RootElement.GetProperty("max_rounds").GetInt32()!=9||rules.RootElement.GetProperty("target_half_points").GetInt32()!=10||economy.RootElement.GetProperty("resource_model").GetString()!="shop_only_bank_round_capabilities")throw new InvalidDataException("Unsupported fighting/resource contract");
  foreach(var stage in stages.Values)if((long)stage.Right-stage.Left is <200000 or >2000000||stage.Floor!=0||stage.CameraWidth is <100000 or >2000000||stage.SpawnX.Length!=2||stage.SpawnX.Any(x=>x<stage.Left+16000||x>stage.Right-16000))throw new InvalidDataException("Invalid stage geometry");
  var combat=Read<CombatRules>("combat.json");var inputs=Read<InputRules>("inputs.json");var physics=Read<GlobalPhysicsRules>("physics.json");
  if(inputs.HistoryFrames is <30 or >120||inputs.MotionTotalWindow is <1 or >120||inputs.MotionStepGap is <1 or >30||inputs.MotionButtonWindow is <1 or >30||inputs.ChargeHoldTicks is <1 or >120||inputs.ChargeReleaseWindow is <1 or >30||inputs.ReversalBufferTicks is <0 or >3||!new[]{"qcf","qcb","dp","double_qcf"}.All(inputs.MotionPatterns.ContainsKey)||inputs.MotionPatterns.Values.Any(p=>p.Length is <2 or >8||p.Any(d=>d is <1 or >9)))throw new InvalidDataException("Invalid input bounds");
  if(combat.Parry.HighWindow is <1 or >30||combat.Parry.LowWindow is <1 or >30||combat.Parry.AirWindow is <1 or >30||combat.Parry.RedWindow is <1 or >10||combat.Damage.ComboFloorPercent is <1 or >100||combat.Damage.ComboStepPercent is <0 or >100||combat.Damage.CounterhitPercent is <100 or >200||combat.Juggle.InitialBudget is <1 or >100||combat.Throw.TechWindow is <1 or >15)throw new InvalidDataException("Invalid combat bounds");
  foreach(var f in fighters.Values)
  {
   if(f.Moves.Length is <24 or >256||f.Moves.Select(x=>x.Id).Distinct().Count()!=f.Moves.Length||string.IsNullOrWhiteSpace(f.Id)||string.IsNullOrWhiteSpace(f.DisplayName)||f.Health<=0||f.Health>10000||f.StunLimit<=0)throw new InvalidDataException("Invalid fighter "+f.Id);
   if(f.SuperArts.Length is <1 or >8||f.SuperArts.Select(a=>a.Id).Distinct().Count()!=f.SuperArts.Length||!f.SuperArts.Any(a=>a.Id==f.DefaultSuper)||f.Physics.WalkForward is <1 or >10000||f.Physics.WalkBack is <1 or >10000||f.Physics.DashForwardTicks is <1 or >120||f.Physics.DashBackTicks is <1 or >120||f.Physics.Gravity is <1 or >10000||f.Physics.JumpVelocity is <1 or >50000)throw new InvalidDataException("Invalid fighter movement/art bounds "+f.Id);
   foreach(var id in new[]{"throw_forward","throw_back","leap_overhead","command_fhp","close_mp","close_hp"}.Concat(new[]{"s_","c_","j_"}.SelectMany(prefix=>new[]{"lp","mp","hp","lk","mk","hk"}.Select(b=>prefix+b))))if(!f.Moves.Any(m=>m.Id==id))throw new InvalidDataException("Missing universal action role "+f.Id+":"+id);
   if(!new[]{"standing","crouching","air"}.All(k=>f.Hurtboxes.TryGetValue(k,out var boxes)&&boxes.Length is >0 and <=8))throw new InvalidDataException("Missing body posture "+f.Id);
   // Starter body shapes used bottom-left coordinates; attacks use the documented center convention.
   foreach(var box in f.Hurtboxes.Values.SelectMany(x=>x).Append(f.Pushbox)){ValidateBox(box);box.X+=box.Width/2;box.Y+=box.Height/2;}
   foreach(var m in f.Moves)
   {
    if(m.TotalTicks<=0||m.TotalTicks>600||m.Startup<0||m.Active<0||m.Recovery<0||m.CreditCost<0||m.CreditCost>econ.Cap)throw new InvalidDataException("Invalid move "+m.Id);
    if(m.CreditCost!=0||m.DebitOnStart||m.AccessPolicy is not("base" or "round_license" or "prepaid_super")||m.Kind=="super"&&m.AccessPolicy!="prepaid_super"||m.Kind=="ex_special"&&m.AccessPolicy!="round_license"||m.AccessPolicy=="base"!=(m.Availability=="base"))throw new InvalidDataException("Invalid activation "+m.Id);
    if(m.Availability!="base"&&(!items.TryGetValue(m.Availability,out var item)||item.MoveId!=m.Id&&item.MoveId!=m.DerivedFrom||!item.EligibleFighters.Contains(f.Id)))throw new InvalidDataException("Invalid lease "+m.Id);
    foreach(var h in m.Hitboxes){ValidateBox(h);if(h.Start<m.Startup||h.End<=h.Start||h.End>m.Startup+m.Active||h.Damage<0||h.Damage>1000||h.HitGroup is <0 or >63||h.Hitstun is <0 or >600||h.Blockstun is <0 or >600||h.Level is not("mid" or "low" or "overhead" or "air"))throw new InvalidDataException("Invalid hitbox "+m.Id);}
    if(m.HurtboxWindows.Length>32)throw new InvalidDataException("Too many authored hurtbox phases "+m.Id);
    int previousEnd=-1;foreach(var window in m.HurtboxWindows.OrderBy(w=>w.Start))
    {
     if(window.Start<0||window.Start<previousEnd||window.End<=window.Start||window.End>m.TotalTicks||window.Boxes.Length is <1 or >8||string.IsNullOrWhiteSpace(window.Intent))throw new InvalidDataException("Invalid or overlapping hurtbox phase "+m.Id);
     foreach(var box in window.Boxes)ValidateBox(box);previousEnd=window.End;
    }
    foreach(var movement in m.Movement)if(movement.Start<0||movement.End<=movement.Start||movement.End>m.TotalTicks||Math.Abs((long)movement.Vx)>50000||Math.Abs((long)movement.Vy)>50000)throw new InvalidDataException("Invalid movement "+m.Id);
    foreach(var cancel in m.CancelRules)if(cancel.Start<0||cancel.End<=cancel.Start||cancel.End>m.TotalTicks||cancel.Targets.Any(t=>t is not("special" or "ex_special" or "selected_super")))throw new InvalidDataException("Invalid cancel "+m.Id);
    foreach(var invulnerability in m.Invulnerability)if(invulnerability.Start<0||invulnerability.End<=invulnerability.Start||invulnerability.End>m.TotalTicks||invulnerability.To.Any(t=>t is not("strike" or "projectile" or "throw")))throw new InvalidDataException("Invalid invulnerability "+m.Id);
    if(m.Throw is {} t&&(t.Range is <1 or >150000||t.Damage is <0 or >1000))throw new InvalidDataException("Invalid throw "+m.Id);
    if(m.Projectile is {} p){ValidateBox(p);if(p.SpawnTick<0||p.SpawnTick>=m.TotalTicks||p.Hits is <1 or >16||p.LifeTicks is <1 or >600||p.RehitTicks<1||p.MaxActivePerOwner is <1 or >8||Math.Abs((long)p.Vx)>50000||p.TurnAfterTicks<0||p.TurnAfterTicks>p.LifeTicks)throw new InvalidDataException("Invalid projectile "+m.Id);}
   }
   foreach(var move in f.Moves){Simulation.ValidateRouteMove(move,f);Simulation.ValidateActorMove(move,f);Simulation.ValidateObjectMove(move,f);}
   foreach(var art in f.SuperArts)if(f.Move(art.MoveId).Kind!="super"||f.Move(art.MoveId).CreditCost!=art.CreditCost)throw new InvalidDataException("Super cost mismatch");
   foreach(var t in f.TargetCombos){f.Move(t.From);f.Move(t.To);}
  }
  foreach(var i in items.Values){if(i.Price<0||i.Price>econ.Cap||!slotPrices.ContainsKey(i.Slot)||i.EligibleFighters.Length==0||i.EligibleFighters.Any(x=>!fighters.ContainsKey(x))||i.Duration!="one_round"||i.CommitFrame!=0||i.RefundAfterLock||i.AltersSystemDefense||i.ActivationCreditCost!=0||(i.Slot=="super"?i.UseLimit!=1:i.UseLimit is not null)||i.AccessPolicy!=(i.Slot=="super"?"prepaid_super":"round_license")||i.ImplementationStatus!="implemented"||i.ConflictsWith.Any(x=>!items.ContainsKey(x))||i.Debit!="preparation_commit")throw new InvalidDataException("Invalid item "+i.Id);foreach(var fighter in i.EligibleFighters){fighters[fighter].Move(i.MoveId);if(i.Replaces is not null)fighters[fighter].Move(i.Replaces);}}
  using var hash=IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
  foreach(var p in Directory.GetFiles(directory,"*.json",SearchOption.AllDirectories).Where(x=>Path.GetRelativePath(directory,x).Split(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar)[0]!="rulesets").OrderBy(x=>Path.GetRelativePath(directory,x).Replace('\\','/'),StringComparer.Ordinal)){hash.AppendData(Encoding.UTF8.GetBytes(Path.GetRelativePath(directory,p).Replace('\\','/')));hash.AppendData(File.ReadAllBytes(p));}
  return new GameContent{Fighters=fighters,Items=items,Stages=stages,Economy=econ,Preparation=preparation,SkillRewards=rewards,Rules=matchRules,Combat=combat,Inputs=inputs,Physics=physics,ContentHash=Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant(),RoundTicks=rules.RootElement.GetProperty("round_ticks").GetInt32(),CountdownTicks=rules.RootElement.GetProperty("countdown_ticks").GetInt32(),PreparationTicks=economy.RootElement.GetProperty("preparation_seconds").GetInt32()*60,RevealTicks=economy.RootElement.GetProperty("reveal_seconds").GetInt32()*60};
 }
 static void ValidateBox(BoxDefinition b){if(b.Width<=0||b.Width>768000||b.Height<=0||b.Height>400000)throw new InvalidDataException("Invalid box extents");}
}
