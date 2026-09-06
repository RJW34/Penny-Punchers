namespace StrikeLedger.Core;

public sealed partial class PlayerState
{
 public RootAttackId? RootAttack {get;internal set;}
 public long EligibleDefenseTick {get;internal set;}public long ParryArmId {get;internal set;}public long ParryArmEligibleTick {get;internal set;}
 public bool ParryBonusFrozen {get;internal set;}public bool VoluntaryAir {get;internal set;}
 internal bool RootContinuation;
}
public sealed partial class ProjectileState { public RootAttackId? RootAttack {get;internal set;} }
public sealed partial class Simulation
{
 readonly Dictionary<RootAttackId,int> _rootContacts=[];
 readonly List<ResolvedContactFacts> _pendingContactFacts=[];
 bool OffensiveStartup(PlayerState p)
 {
  if(p.ActionId.Length==0)return false;var m=Content.Fighters[p.FighterId].Move(p.ActionId);
  return p.ActionFrame<m.Startup&&(m.Hitboxes.Any(h=>h.Damage>0)||m.Projectile?.Damage>0||m.Throw?.Damage>0);
 }
 ResolvedContactFacts PreContactFact(ContactCandidate c)
 {
  var a=_players[c.Attacker];var d=_players[c.Defender];var root=c.Projectile?.RootAttack??a.RootAttack??throw new InvalidOperationException("Contact missing root attack provenance");
  return new(Tick,c.Attacker,root,c.Attacker,0,AttackKind:c.Projectile is null?AttackKind.Direct:c.Projectile.IsField?AttackKind.Field:AttackKind.Projectile,
   ThreatDamage:c.Hit.Damage,AttackerGrounded:a.Grounded,AttackerPrejump:a.JumpStart>0,DefenderAirborne:!d.Grounded,DefenderVoluntaryAir:d.VoluntaryAir,
   DefenderDisabled:d.Hitstun>0||d.Blockstun>0||d.KnockdownTicks>0||d.DizzyTicks>0||d.ThrowAttacker>=0,DefenderInCombo:d.ComboCount>0,DefenderOffensiveStartup:OffensiveStartup(d),
   ParryArmId:d.ParryArmId,ParryEligibleAge:d.EligibleDefenseTick-d.ParryArmEligibleTick,FreshManualParry:d.ParryArmId>0&&d.ParryTicks>0,BonusIneligibleFrozenEdge:d.ParryBonusFrozen,
   Training:Config.Training,MoveId:c.Move.Id,SourceFighterId:c.Projectile?.OriginalFighter??a.FighterId,WorldX:c.WorldX,WorldY:c.WorldY,AttackerPrestate:ActorPrestate(a),DefenderPrestate:ActorPrestate(d));
 }
 static ContactActorPrestate ActorPrestate(PlayerState p)=>new(p.X,p.Y,p.Facing,p.ActionId,p.ActionFrame,p.Hitstun,p.Blockstun,p.KnockdownTicks,p.DizzyTicks,p.ThrowAttacker>=0,p.ComboCount,p.JumpStart);
 void AcceptContactFact(ContactCandidate c,ContactOutcome outcome,int damage=0)
 {
  var fact=c.Facts??throw new InvalidOperationException("Missing immutable pre-contact facts");
  AcceptResolvedFact(fact with{EarnerSeat=outcome==ContactOutcome.Parry?c.Defender:c.Attacker,Outcome=outcome,ActualDamage=damage});
 }
 void AcceptResolvedFact(ResolvedContactFacts fact)
 {
  if(!_rootContacts.ContainsKey(fact.Root)&&_rootContacts.Count>=SkillRewardLedger.MaximumFacts)
  {
   // Practice has no competitive ledger and may run indefinitely; obsolete completed roots can be pruned safely.
   if(!Config.Training)throw new InvalidDataException("Round attack root bound exceeded");
   var live=_players.Select(p=>p.RootAttack).Concat(_projectiles.Select(p=>p.RootAttack)).ToHashSet();foreach(var key in _rootContacts.Keys.Where(k=>!live.Contains(k)).ToArray())_rootContacts.Remove(key);
  }
  int ordinal=_rootContacts.GetValueOrDefault(fact.Root);_rootContacts[fact.Root]=checked(ordinal+1);
  _pendingContactFacts.Add(fact with{ContactOrdinal=ordinal});
 }
 void RecordThrowFact(PlayerState attacker,PlayerState defender,MoveDefinition move,int damage,int x,int y)
 {
  var root=attacker.RootAttack??throw new InvalidOperationException("Throw missing attack root");AcceptResolvedFact(new(Tick,attacker.Seat,root,attacker.Seat,0,ContactOutcome.Throw,AttackKind.Throw,ActualDamage:damage,ThreatDamage:move.Throw!.Damage,Training:Config.Training,MoveId:move.Id,SourceFighterId:attacker.FighterId,WorldX:x,WorldY:y,AttackerPrestate:ActorPrestate(attacker),DefenderPrestate:ActorPrestate(defender)));
 }
 void WriteV2State(BinaryWriter w)
 {
  foreach(var p in _players)
  {
   w.Write(p.OpeningBankCredits);w.Write(p.RoundPurchaseCost);w.Write(p.SuperUsesRemaining);w.Write(p.OwnedExIds.Count);foreach(var id in p.OwnedExIds.Order(StringComparer.Ordinal))w.Write(id);
   w.Write(p.UseReceipts.Count);foreach(var r in p.UseReceipts){w.Write(r.Key);w.Write(r.MoveId);w.Write(r.Tick);w.Write(r.ActionOrdinal);}
   WriteRoot(w,p.RootAttack);w.Write(p.RootContinuation);w.Write(p.EligibleDefenseTick);w.Write(p.ParryArmId);w.Write(p.ParryArmEligibleTick);w.Write(p.ParryBonusFrozen);w.Write(p.VoluntaryAir);
  }
  foreach(var p in _projectiles.OrderBy(p=>p.Id))WriteRoot(w,p.RootAttack);
  w.Write(_rootContacts.Count);foreach(var pair in _rootContacts.OrderBy(p=>p.Key)){pair.Key.Write(w);w.Write(pair.Value);}
  if(LastPreparation is {} prep){w.Write(prep.OpeningCredits0);w.Write(prep.OpeningCredits1);WriteIds(w,prep.ProductIds0);WriteIds(w,prep.ProductIds1);w.Write(prep.SuperArt0);w.Write(prep.SuperArt1);}
  if(LastSettlement is {} settle){WriteSkillPayout(w,settle.SkillPayout0);WriteSkillPayout(w,settle.SkillPayout1);}
  WriteSkillState(w);
 }
 void ReadV2State(BinaryReader r)
 {
  foreach(var p in _players)
  {
   p.OpeningBankCredits=r.ReadInt32();p.RoundPurchaseCost=r.ReadInt32();p.SuperUsesRemaining=r.ReadInt32();p.OwnedExIds=ReadIds(r,2).ToList();
   for(int n=ReadCount(r,1);n>0;n--)p.UseReceipts.Add(new(r.ReadString(),r.ReadString(),r.ReadInt64(),r.ReadInt32()));
   p.RootAttack=ReadRoot(r);p.RootContinuation=r.ReadBoolean();p.EligibleDefenseTick=r.ReadInt64();p.ParryArmId=r.ReadInt64();p.ParryArmEligibleTick=r.ReadInt64();p.ParryBonusFrozen=r.ReadBoolean();p.VoluntaryAir=r.ReadBoolean();
   var quote=Content.QuotePreparation(p.FighterId,Content.Economy.Cap,new(p.LeaseIds.ToArray()));
   if(!quote.Valid||p.ReserveFloor!=0||p.Receipts.Count!=0||p.OpeningBankCredits<0||p.OpeningBankCredits>Content.Economy.Cap||p.RoundPurchaseCost<0||p.RoundPurchaseCost>Content.Preparation.LoadoutCap||p.SuperUsesRemaining is <0 or >1||p.EligibleDefenseTick<0||p.ParryArmId<0||p.ParryArmEligibleTick<0||p.ParryArmEligibleTick>p.EligibleDefenseTick||p.EligibleDefenseTick>Tick||p.ParryArmId>Tick||p.RootAttack is {} playerRoot&&playerRoot.OriginSeat!=p.Seat||!p.OwnedExIds.Order(StringComparer.Ordinal).SequenceEqual(quote.ExMoveIds.Order(StringComparer.Ordinal))||p.SelectedSuper!=quote.SelectedSuper||p.SuperUsesRemaining>quote.SuperCount||p.UseReceipts.Any(x=>x.Tick<0||x.ActionOrdinal<1||Content.Fighters[p.FighterId].Move(x.MoveId).Kind!="super")||quote.SuperCount==1&&p.SuperUsesRemaining+p.UseReceipts.Count!=1)
    throw new InvalidDataException("Invalid shop-only capability snapshot");
  }
  foreach(var p in _projectiles.OrderBy(p=>p.Id))p.RootAttack=ReadRoot(r)??throw new InvalidDataException("Projectile missing root");
  for(int n=ReadCount(r,SkillRewardLedger.MaximumFacts);n>0;n--){var root=RootAttackId.Read(r);int value=r.ReadInt32();if(root.SessionId!=Config.SessionId||root.Round!=RoundId||value<1||!_rootContacts.TryAdd(root,value))throw new InvalidDataException("Root contact counter bounds");}
  if(LastPreparation is {} prep)LastPreparation=prep with{OpeningCredits0=r.ReadInt32(),OpeningCredits1=r.ReadInt32(),ProductIds0=ReadIds(r,Content.Preparation.MaxProducts),ProductIds1=ReadIds(r,Content.Preparation.MaxProducts),SuperArt0=r.ReadString(),SuperArt1=r.ReadString()};
  if(LastSettlement is {} settle)LastSettlement=settle with{SkillPayout0=ReadSkillPayout(r),SkillPayout1=ReadSkillPayout(r)};
  ReadSkillState(r);
 }
 static void WriteRoot(BinaryWriter w,RootAttackId? root){w.Write(root is not null);root?.Write(w);}
 RootAttackId? ReadRoot(BinaryReader r){if(!r.ReadBoolean())return null;var root=RootAttackId.Read(r);if(root.SessionId!=Config.SessionId||root.Round!=RoundId)throw new InvalidDataException("Foreign attack root");return root;}
 static void WriteIds(BinaryWriter w,string[] ids){w.Write(ids.Length);foreach(var id in ids)w.Write(id);}
 static string[] ReadIds(BinaryReader r,int max){var ids=new string[ReadCount(r,max)];for(int i=0;i<ids.Length;i++)ids[i]=r.ReadString();return ids;}
 static void WriteSkillPayout(BinaryWriter w,SkillSettlementReceipt p){w.Write(p.Earned);w.Write(p.Granted);w.Write(p.Clipped);}
 static SkillSettlementReceipt ReadSkillPayout(BinaryReader r){var p=new SkillSettlementReceipt(r.ReadInt32(),r.ReadInt32(),r.ReadInt32());if(p.Earned<0||p.Granted<0||p.Clipped<0||p.Earned!=p.Granted+p.Clipped)throw new InvalidDataException("Skill settlement bounds");return p;}
}
