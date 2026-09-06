using System.Security.Cryptography;
using System.Text;

namespace StrikeLedger.Core;

/// <summary>Origin survives every branch, emitted object and reflection until a new neutral action.</summary>
public sealed record RootAttackId(string SessionId,int Round,int OriginSeat,int ActionOrdinal):IComparable<RootAttackId>
{
 public string Key=>$"{SessionId.Length}:{SessionId}|{Round}|{OriginSeat}|{ActionOrdinal}";
 public int CompareTo(RootAttackId? other)
 {
  if(other is null)return 1;
  int c=StringComparer.Ordinal.Compare(SessionId,other.SessionId);if(c!=0)return c;
  c=Round.CompareTo(other.Round);if(c!=0)return c;c=OriginSeat.CompareTo(other.OriginSeat);
  return c!=0?c:ActionOrdinal.CompareTo(other.ActionOrdinal);
 }
 internal void Validate()
 {
  if(string.IsNullOrEmpty(SessionId)||SessionId.Length>128||Round is <1 or >9||OriginSeat is <0 or >1||ActionOrdinal<1)
   throw new InvalidDataException("Invalid root attack identity");
 }
 internal void Write(BinaryWriter w){w.Write(SessionId);w.Write(Round);w.Write(OriginSeat);w.Write(ActionOrdinal);}
 internal static RootAttackId Read(BinaryReader r){var root=new RootAttackId(r.ReadString(),r.ReadInt32(),r.ReadInt32(),r.ReadInt32());root.Validate();return root;}
}

public enum ContactOutcome { Hit,Parry,Block,Armor,Throw,Counter }
public enum AttackKind { Direct,Projectile,Field,Throw }
public enum SkillRewardCategory { None,CounterHit,AntiAir,PerfectParry }
public sealed record ContactActorPrestate(int X,int Y,int Facing,string ActionId,int ActionFrame,int Hitstun,int Blockstun,int KnockdownTicks,int DizzyTicks,bool Captured,int ComboCount,int JumpStart)
{
 internal void Write(BinaryWriter w){w.Write(X);w.Write(Y);w.Write(Facing);w.Write(ActionId);w.Write(ActionFrame);w.Write(Hitstun);w.Write(Blockstun);w.Write(KnockdownTicks);w.Write(DizzyTicks);w.Write(Captured);w.Write(ComboCount);w.Write(JumpStart);}
}
public sealed record SkillRewardPolicy(int CounterHit=50,int AntiAir=75,int PerfectParry=100,int CategoryLimit=2,int TotalLimit=300,int PerfectWindowEligibleTicks=2)
{
 public int Amount(SkillRewardCategory category)=>category switch{SkillRewardCategory.CounterHit=>CounterHit,SkillRewardCategory.AntiAir=>AntiAir,SkillRewardCategory.PerfectParry=>PerfectParry,_=>0};
 internal void Validate()
 {
  if(CounterHit is <0 or >1000||AntiAir is <0 or >1000||PerfectParry is <0 or >1000||CategoryLimit is <0 or >10||TotalLimit is <0 or >3000||PerfectWindowEligibleTicks is <1 or >10)
   throw new InvalidDataException("Invalid skill reward policy");
 }
}

/// <summary>Immutable simultaneous pre-contact state plus the accepted collision outcome; never inferred from post-hit animation.</summary>
public sealed record ResolvedContactFacts(
 long Tick,int EarnerSeat,RootAttackId Root,int EffectiveOwner,int ContactOrdinal,
 ContactOutcome Outcome=ContactOutcome.Hit,AttackKind AttackKind=AttackKind.Direct,int ActualDamage=0,int ThreatDamage=0,bool Accepted=true,
 bool AttackerGrounded=false,bool AttackerPrejump=false,bool DefenderAirborne=false,bool DefenderVoluntaryAir=false,
 bool DefenderDisabled=false,bool DefenderInCombo=false,bool DefenderOffensiveStartup=false,
 long ParryArmId=0,long ParryEligibleAge=-1,bool FreshManualParry=false,bool BonusIneligibleFrozenEdge=false,bool Training=false,
 string MoveId="",string SourceFighterId="",int WorldX=0,int WorldY=0,string EventId="",ContactActorPrestate? AttackerPrestate=null,ContactActorPrestate? DefenderPrestate=null)
{
 public string Identity=>EventId.Length>0?EventId:$"{Tick}|{EarnerSeat}|{Root.Key}|{ContactOrdinal}";
 internal void Validate()
 {
  Root.Validate();
  if(Tick<0||EarnerSeat is <0 or >1||EffectiveOwner is <0 or >1||ContactOrdinal<0||!Enum.IsDefined(Outcome)||!Enum.IsDefined(AttackKind)||ActualDamage<0||ThreatDamage<0||ParryArmId<0||EventId.Length>256||MoveId.Length>128||SourceFighterId.Length>128)
   throw new InvalidDataException("Invalid resolved contact facts");
 }
 internal string Fingerprint()
 {
  using var stream=new MemoryStream();using(var w=new BinaryWriter(stream,Encoding.UTF8,true))
  {
   w.Write(Tick);w.Write(EarnerSeat);Root.Write(w);w.Write(EffectiveOwner);w.Write(ContactOrdinal);w.Write((int)Outcome);w.Write((int)AttackKind);
   w.Write(ActualDamage);w.Write(ThreatDamage);w.Write(Accepted);w.Write(AttackerGrounded);w.Write(AttackerPrejump);w.Write(DefenderAirborne);w.Write(DefenderVoluntaryAir);
   w.Write(DefenderDisabled);w.Write(DefenderInCombo);w.Write(DefenderOffensiveStartup);w.Write(ParryArmId);w.Write(ParryEligibleAge);w.Write(FreshManualParry);w.Write(BonusIneligibleFrozenEdge);w.Write(Training);
   w.Write(MoveId);w.Write(SourceFighterId);w.Write(WorldX);w.Write(WorldY);w.Write(EventId);
   w.Write(AttackerPrestate is not null);AttackerPrestate?.Write(w);w.Write(DefenderPrestate is not null);DefenderPrestate?.Write(w);
  }
  return Convert.ToHexString(SHA256.HashData(stream.ToArray()));
 }
}

public sealed record SkillRewardReceipt(string Id,long Tick,int EarnerSeat,RootAttackId Root,int ContactOrdinal,SkillRewardCategory Category,int Nominal,int Allowed,int Capped)
{
 public int Amount=>Allowed;
}
public sealed record SkillRewardDecision(SkillRewardCategory Category,int Nominal,int Allowed,int Capped,string Reason,SkillRewardReceipt? Receipt=null)
{
 public int Amount=>Allowed;
}
public sealed record SkillSettlementReceipt(int Earned,int Granted,int Clipped);

/// <summary>Round-local, nonspendable receipt ledger. It has no bank reference or activation authority.</summary>
public sealed class SkillRewardLedger
{
 public const int MaximumFacts=8192;
 readonly SkillRewardPolicy _policy;
 readonly Dictionary<string,string> _facts=new(StringComparer.Ordinal);
 readonly Dictionary<string,SkillRewardCategory> _roots=new(StringComparer.Ordinal);
 readonly List<SkillRewardReceipt> _receipts=[];
 readonly int[] _counts=new int[4];
 (long Tick,int Seat,RootAttackId Root,int Ordinal)? _lastOrder;
 public int Pending {get;private set;}
 public IReadOnlyList<SkillRewardReceipt> Receipts=>_receipts.AsReadOnly();
 public int FactCount=>_facts.Count;
 public int RootCount=>_roots.Count;
 public int Count(SkillRewardCategory category)=>_counts[(int)category];
 public SkillRewardLedger(SkillRewardPolicy? policy=null){_policy=policy??new();_policy.Validate();}
 public static SkillRewardCategory Classify(ResolvedContactFacts f,SkillRewardPolicy? policy=null)
 {
  if(!f.Accepted)return SkillRewardCategory.None;
  var rules=policy??new();
  if(f.Outcome==ContactOutcome.Parry)
   return f.ThreatDamage>0&&f.AttackKind is AttackKind.Direct or AttackKind.Projectile or AttackKind.Field&&f.Root.OriginSeat!=f.EarnerSeat&&f.EffectiveOwner==1-f.EarnerSeat&&f.FreshManualParry&&f.ParryArmId>0&&!f.BonusIneligibleFrozenEdge&&f.ParryEligibleAge>=0&&f.ParryEligibleAge<rules.PerfectWindowEligibleTicks?SkillRewardCategory.PerfectParry:SkillRewardCategory.None;
  if(f.Outcome!=ContactOutcome.Hit||f.ActualDamage<=0||f.AttackKind!=AttackKind.Direct||f.EffectiveOwner!=f.EarnerSeat||f.Root.OriginSeat!=f.EarnerSeat||f.DefenderDisabled||f.DefenderInCombo)return SkillRewardCategory.None;
  if(f.AttackerGrounded&&!f.AttackerPrejump&&f.DefenderAirborne&&f.DefenderVoluntaryAir)return SkillRewardCategory.AntiAir;
  return f.DefenderOffensiveStartup?SkillRewardCategory.CounterHit:SkillRewardCategory.None;
 }
 static int Compare((long Tick,int Seat,RootAttackId Root,int Ordinal) a,(long Tick,int Seat,RootAttackId Root,int Ordinal) b)
 {
  int c=a.Tick.CompareTo(b.Tick);if(c!=0)return c;c=a.Seat.CompareTo(b.Seat);if(c!=0)return c;c=a.Root.CompareTo(b.Root);return c!=0?c:a.Ordinal.CompareTo(b.Ordinal);
 }
 public SkillRewardDecision Apply(ResolvedContactFacts f)
 {
  f.Validate();
  // Untimed practice can run indefinitely; diagnostics never grow a competitive round ledger.
  if(f.Training)return new(Classify(f,_policy),0,0,0,"training_only");
  string fingerprint=f.Fingerprint();
  if(_facts.TryGetValue(f.Identity,out var prior))
  {
   if(prior!=fingerprint)throw new InvalidDataException("Conflicting resolved contact identity");
   return new(SkillRewardCategory.None,0,0,0,"duplicate_event");
  }
  var order=(f.Tick,f.EarnerSeat,f.Root,f.ContactOrdinal);
  if(_lastOrder is {} last&&Compare(order,last)<0)throw new InvalidDataException("Unsorted authoritative contact facts");
  if(_facts.Count>=MaximumFacts)throw new InvalidDataException("Round contact fact bound exceeded");
  var category=Classify(f,_policy);
  // The first actual direct hit uses the opener even if it earns nothing; later branches cannot manufacture another opener.
  bool opener=f.Accepted&&f.Outcome==ContactOutcome.Hit&&f.ActualDamage>0&&f.AttackKind==AttackKind.Direct&&f.Root.OriginSeat==f.EarnerSeat&&f.EffectiveOwner==f.EarnerSeat;
  string rootKey=(category==SkillRewardCategory.PerfectParry?"defense:":"offense:")+f.Root.Key;
  bool usesRoot=opener||category!=SkillRewardCategory.None;
  if(usesRoot&&!_roots.ContainsKey(rootKey)&&_roots.Count>=MaximumFacts)throw new InvalidDataException("Round reward root bound exceeded");
  _facts.Add(f.Identity,fingerprint);_lastOrder=order;
  if(!usesRoot)return new(category,0,0,0,"ineligible");
  if(!_roots.TryAdd(rootKey,category))return new(category,0,0,0,"duplicate_root");
  if(category==SkillRewardCategory.None)return new(category,0,0,0,"ineligible_opener");
  int nominal=_policy.Amount(category);
  if(_counts[(int)category]>=_policy.CategoryLimit)return new(category,nominal,0,nominal,"category_cap");
  int allowed=Math.Min(nominal,Math.Max(0,_policy.TotalLimit-Pending));
  if(allowed==0)return new(category,nominal,0,nominal,"global_cap");
  _counts[(int)category]++;Pending+=allowed;
  var receipt=new SkillRewardReceipt($"{f.Root.SessionId.Length}:{f.Root.SessionId}|{f.Root.Round}|{f.EarnerSeat}|{rootKey}",f.Tick,f.EarnerSeat,f.Root,f.ContactOrdinal,category,nominal,allowed,nominal-allowed);
  _receipts.Add(receipt);return new(category,nominal,allowed,nominal-allowed,"awarded",receipt);
 }
 internal void Write(BinaryWriter w)
 {
  w.Write(Pending);foreach(int count in _counts)w.Write(count);
  w.Write(_lastOrder is not null);if(_lastOrder is {} last){w.Write(last.Tick);w.Write(last.Seat);last.Root.Write(w);w.Write(last.Ordinal);}
  w.Write(_facts.Count);foreach(var pair in _facts.OrderBy(x=>x.Key,StringComparer.Ordinal)){w.Write(pair.Key);w.Write(pair.Value);}
  w.Write(_roots.Count);foreach(var pair in _roots.OrderBy(x=>x.Key,StringComparer.Ordinal)){w.Write(pair.Key);w.Write((int)pair.Value);}
  w.Write(_receipts.Count);foreach(var r in _receipts){w.Write(r.Id);w.Write(r.Tick);w.Write(r.EarnerSeat);r.Root.Write(w);w.Write(r.ContactOrdinal);w.Write((int)r.Category);w.Write(r.Nominal);w.Write(r.Allowed);w.Write(r.Capped);}
 }
 internal static SkillRewardLedger Read(BinaryReader r,SkillRewardPolicy policy)
 {
  var ledger=new SkillRewardLedger(policy){Pending=r.ReadInt32()};
  for(int i=0;i<4;i++)ledger._counts[i]=r.ReadInt32();
  if(r.ReadBoolean())ledger._lastOrder=(r.ReadInt64(),r.ReadInt32(),RootAttackId.Read(r),r.ReadInt32());
  static int Count(BinaryReader reader,int bound){int n=reader.ReadInt32();if(n<0||n>bound)throw new InvalidDataException("Skill collection bound");return n;}
  for(int n=Count(r,MaximumFacts);n>0;n--){string key=r.ReadString(),hash=r.ReadString();if(key.Length>512||hash.Length!=64||hash.Any(c=>!Uri.IsHexDigit(c))||!ledger._facts.TryAdd(key,hash))throw new InvalidDataException("Skill fact identity");}
  for(int n=Count(r,MaximumFacts);n>0;n--){string key=r.ReadString();var category=(SkillRewardCategory)r.ReadInt32();if(key.Length>512||!Enum.IsDefined(category)||!ledger._roots.TryAdd(key,category))throw new InvalidDataException("Skill root identity");}
  for(int n=Count(r,policy.CategoryLimit*3);n>0;n--)
  {
   var item=new SkillRewardReceipt(r.ReadString(),r.ReadInt64(),r.ReadInt32(),RootAttackId.Read(r),r.ReadInt32(),(SkillRewardCategory)r.ReadInt32(),r.ReadInt32(),r.ReadInt32(),r.ReadInt32());
   string rootKey=(item.Category==SkillRewardCategory.PerfectParry?"defense:":"offense:")+item.Root.Key;
   if(item.Id.Length>512||item.Tick<0||item.EarnerSeat is <0 or >1||item.ContactOrdinal<0||!Enum.IsDefined(item.Category)||item.Category==SkillRewardCategory.None||item.Nominal!=policy.Amount(item.Category)||item.Allowed<=0||item.Allowed>item.Nominal||item.Capped!=item.Nominal-item.Allowed||!ledger._roots.TryGetValue(rootKey,out var category)||category!=item.Category||ledger._receipts.Any(x=>x.Id==item.Id))throw new InvalidDataException("Skill receipt bounds");
   ledger._receipts.Add(item);
  }
  if(ledger.Pending<0||ledger.Pending>policy.TotalLimit||ledger.Pending!=ledger._receipts.Sum(x=>x.Allowed)||ledger._counts[0]!=0||Enumerable.Range(1,3).Any(i=>ledger._counts[i]!=ledger._receipts.Count(x=>(int)x.Category==i)||ledger._counts[i]>policy.CategoryLimit)||ledger._facts.Count==0&&ledger._lastOrder is not null||ledger._facts.Count>0&&ledger._lastOrder is null)
   throw new InvalidDataException("Skill ledger totals");
  if(ledger._lastOrder is {} last&&(last.Tick<0||last.Seat is <0 or >1||last.Ordinal<0))throw new InvalidDataException("Skill order bounds");
  return ledger;
 }
}

public sealed partial class PlayerState
{
 public SkillRewardLedger SkillRewards {get;internal set;}=new();
 public int PendingSkillCredits=>SkillRewards.Pending;
 public IReadOnlyList<SkillRewardReceipt> SkillReceipts=>SkillRewards.Receipts;
}
