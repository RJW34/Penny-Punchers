using System.Security.Cryptography;
using System.Text;
namespace StrikeLedger.Core;
public sealed partial class Simulation
{
 public SimulationSnapshot Capture()=>new(SerializeCanonical());
 public string Hash()=>Convert.ToHexString(SHA256.HashData(SerializeCanonical())).ToLowerInvariant();
 public byte[] SerializeCanonical()
 {
  using var stream=new MemoryStream();using var w=new BinaryWriter(stream,Encoding.UTF8,true);
  w.Write(0x534C534E);w.Write(3);w.Write(Content.ContentHash);w.Write(Config.SessionId);w.Write(Config.Fighter0);w.Write(Config.Fighter1);w.Write(Config.Super0);w.Write(Config.Super1);w.Write(Config.StageId);w.Write(Config.Training);w.Write(Config.Assist);
  w.Write(Tick);w.Write((int)Phase);w.Write(RoundId);w.Write(CompletedRounds);w.Write(TimerTicks);w.Write(PhaseTicks);w.Write(FullFreeze);w.Write(_nextProjectileId);w.Write(MatchDecision);w.Write(_preparationPayload);w.Write(_settlementPayload);
  w.Write(PendingResult is not null);if(PendingResult is {} terminal){w.Write(terminal.WinnerSeat);w.Write(terminal.TerminalTick);w.Write(terminal.Reason);}
  w.Write(LastPreparation is not null);if(LastPreparation is {} prep){w.Write(prep.Key);w.Write(prep.Round);w.Write(prep.Cost0);w.Write(prep.Cost1);w.Write(prep.Credits0);w.Write(prep.Credits1);}
  w.Write(LastSettlement is not null);if(LastSettlement is {} settlement){w.Write(settlement.Key);w.Write(settlement.Round);w.Write(settlement.WinnerSeat);WritePayout(w,settlement.Payout0);WritePayout(w,settlement.Payout1);w.Write(settlement.MatchDecision);}
  foreach(var p in _players)
  {
   w.Write(p.Seat);w.Write(p.FighterId);w.Write(p.SelectedSuper);w.Write(p.Credits);w.Write(p.ReserveFloor);w.Write(p.RecoveryTier);w.Write(p.ScoreHalfPoints);w.Write(p.Health);w.Write(p.MaxHealth);w.Write(p.Stun);
   w.Write(p.X);w.Write(p.Y);w.Write(p.Vx);w.Write(p.Vy);w.Write(p.Facing);w.Write(p.FacingEpoch);w.Write(p.ActionId);w.Write(p.ActionFrame);w.Write(p.ActionFacing);
   w.Write(p.Hitstop);w.Write(p.Hitstun);w.Write(p.Blockstun);w.Write(p.KnockdownTicks);w.Write(p.DizzyTicks);w.Write(p.Crouching);w.Write(p.Contact);w.Write(p.ComboCount);w.Write(p.JuggleBudget);
   w.Write((int)p.Parry);w.Write(p.ParryTicks);w.Write(p.ParryRetry);w.Write(p.BackCharge);w.Write(p.DownCharge);w.Write(p.ThrowImmunity);w.Write(p.JumpStart);w.Write(p.LandingTicks);w.Write(p.DashTicks);
   w.Write(p.BackReadyUntil);w.Write(p.DownReadyUntil);w.Write(p.LastForwardTap);w.Write(p.LastBackTap);w.Write(p.LastDirection);w.Write(p.NeutralTicks);w.Write((byte)p.LastButtons);w.Write((byte)p.SuppressNegativeEdge);
   w.Write(p.StunDelay);w.Write(p.KnockdownAge);w.Write(p.ActionOrdinal);w.Write(p.JumpHorizontal);w.Write(p.JumpVelocity);w.Write(p.DashVelocity);w.Write(p.ThrowAge);
   w.Write(p.HardKnockdown);w.Write(p.DizzyThisCombo);w.Write(p.AirAttack);w.Write(p.ProjectileSpawned);w.Write(p.HitGroups);w.Write(p.BufferedAction);w.Write(p.BufferExpires);w.Write(p.BufferedReversal);w.Write((int)p.DeferredParry);w.Write(p.ThrowAttacker);w.Write(p.ThrowMove);w.Write(p.PreviousX);w.Write(p.PreviousY);
   w.Write(p.LeaseIds.Count);foreach(var lease in p.LeaseIds.Order(StringComparer.Ordinal))w.Write(lease);
   w.Write(p.Receipts.Count);foreach(var receipt in p.Receipts){w.Write(receipt.Key);w.Write(receipt.MoveId);w.Write(receipt.Cost);}
   w.Write(p.History.Count);foreach(var h in p.History){w.Write(h.Tick);w.Write(h.RawDirection);w.Write(h.RelativeDirection);w.Write((byte)h.Held);w.Write(h.FacingEpoch);}
  }
  w.Write(_projectiles.Count);foreach(var p in _projectiles.OrderBy(x=>x.Id)){w.Write(p.Id);w.Write(p.Owner);w.Write(p.MoveId);w.Write(p.X);w.Write(p.Y);w.Write(p.Vx);w.Write(p.LifeTicks);w.Write(p.HitsRemaining);w.Write(p.Hitstop);w.Write(p.ContactCooldown);w.Write(p.Facing);w.Write(p.PreviousX);}
  WriteRouteState(w);WriteActorState(w);WriteObjectState(w);WriteV2State(w);
  w.Flush();return stream.ToArray();
 }
 static void WritePayout(BinaryWriter w,PayoutReceipt p){w.Write(p.Nominal);w.Write(p.Granted);w.Write(p.Clipped);w.Write(p.OldTier);w.Write(p.NewTier);}
 static PayoutReceipt ReadPayout(BinaryReader r)=>new(r.ReadInt32(),r.ReadInt32(),r.ReadInt32(),r.ReadInt32(),r.ReadInt32());
 public void Restore(SimulationSnapshot snapshot)
 {
  var temporary=new Simulation(Content,Config);temporary.ReadCanonical(snapshot.Bytes);
  CopyWorld(temporary);
 }
 /// <summary>Detached practice takeover of a live fighting snapshot. Competitive source/config are never mutated.</summary>
 public Simulation CreateTrainingBranch(SimulationSnapshot? snapshot=null)
 {
  var validated=new Simulation(Content,Config);validated.Restore(snapshot??Capture());
  if(validated.Phase!=MatchPhase.Fight)throw new InvalidOperationException("Practice takeover requires a fighting snapshot");
  var branch=new Simulation(Content,Config with{Training=true});branch.CopyWorld(validated);return branch;
 }
 void CopyWorld(Simulation temporary)
 {
  Tick=temporary.Tick;Phase=temporary.Phase;RoundId=temporary.RoundId;CompletedRounds=temporary.CompletedRounds;TimerTicks=temporary.TimerTicks;PhaseTicks=temporary.PhaseTicks;FullFreeze=temporary.FullFreeze;_nextProjectileId=temporary._nextProjectileId;MatchDecision=temporary.MatchDecision;_preparationPayload=temporary._preparationPayload;_settlementPayload=temporary._settlementPayload;PendingResult=temporary.PendingResult;LastPreparation=temporary.LastPreparation;LastSettlement=temporary.LastSettlement;_players=temporary._players;_projectiles.Clear();_projectiles.AddRange(temporary._projectiles);CopyObjectState(temporary);_rootContacts.Clear();foreach(var pair in temporary._rootContacts)_rootContacts.Add(pair.Key,pair.Value);_pendingContactFacts.Clear();ClearSkillDiagnostics();_events.Clear();
 }
 void ReadCanonical(byte[] bytes)
 {
  using var stream=new MemoryStream(bytes,false);using var r=new BinaryReader(stream,Encoding.UTF8);
  if(r.ReadInt32()!=0x534C534E||r.ReadInt32()!=3||r.ReadString()!=Content.ContentHash||r.ReadString()!=Config.SessionId||r.ReadString()!=Config.Fighter0||r.ReadString()!=Config.Fighter1||r.ReadString()!=Config.Super0||r.ReadString()!=Config.Super1||r.ReadString()!=Config.StageId||r.ReadBoolean()!=Config.Training||r.ReadBoolean()!=Config.Assist)throw new InvalidDataException("Snapshot content/config/version mismatch");
  Tick=r.ReadInt64();Phase=(MatchPhase)r.ReadInt32();RoundId=r.ReadInt32();CompletedRounds=r.ReadInt32();TimerTicks=r.ReadInt32();PhaseTicks=r.ReadInt32();FullFreeze=r.ReadInt32();_nextProjectileId=r.ReadInt32();MatchDecision=r.ReadString();_preparationPayload=r.ReadString();_settlementPayload=r.ReadString();
  if(Tick<0||!Enum.IsDefined(Phase)||RoundId is <1 or >9||CompletedRounds is <0 or >9||TimerTicks<0||FullFreeze is <0 or >600)throw new InvalidDataException("Snapshot clock bounds");
  if(r.ReadBoolean())PendingResult=new(r.ReadInt32(),r.ReadInt64(),r.ReadString());
  if(r.ReadBoolean())LastPreparation=new(r.ReadString(),r.ReadInt32(),r.ReadInt32(),r.ReadInt32(),r.ReadInt32(),r.ReadInt32());
  if(r.ReadBoolean())LastSettlement=new(r.ReadString(),r.ReadInt32(),r.ReadInt32(),ReadPayout(r),ReadPayout(r),r.ReadString());
  for(int seat=0;seat<2;seat++)
  {
   var p=new PlayerState{Seat=r.ReadInt32(),FighterId=r.ReadString(),SelectedSuper=r.ReadString(),Credits=r.ReadInt32(),ReserveFloor=r.ReadInt32(),RecoveryTier=r.ReadInt32(),ScoreHalfPoints=r.ReadInt32(),Health=r.ReadInt32(),MaxHealth=r.ReadInt32(),Stun=r.ReadInt32(),
    X=r.ReadInt32(),Y=r.ReadInt32(),Vx=r.ReadInt32(),Vy=r.ReadInt32(),Facing=r.ReadInt32(),FacingEpoch=r.ReadInt32(),ActionId=r.ReadString(),ActionFrame=r.ReadInt32(),ActionFacing=r.ReadInt32(),
    Hitstop=r.ReadInt32(),Hitstun=r.ReadInt32(),Blockstun=r.ReadInt32(),KnockdownTicks=r.ReadInt32(),DizzyTicks=r.ReadInt32(),Crouching=r.ReadBoolean(),Contact=r.ReadBoolean(),ComboCount=r.ReadInt32(),JuggleBudget=r.ReadInt32(),
    Parry=(ParryKind)r.ReadInt32(),ParryTicks=r.ReadInt32(),ParryRetry=r.ReadInt32(),BackCharge=r.ReadInt32(),DownCharge=r.ReadInt32(),ThrowImmunity=r.ReadInt32(),JumpStart=r.ReadInt32(),LandingTicks=r.ReadInt32(),DashTicks=r.ReadInt32(),
    BackReadyUntil=r.ReadInt64(),DownReadyUntil=r.ReadInt64(),LastForwardTap=r.ReadInt64(),LastBackTap=r.ReadInt64(),LastDirection=r.ReadInt32(),NeutralTicks=r.ReadInt32(),LastButtons=(Buttons)r.ReadByte(),SuppressNegativeEdge=(Buttons)r.ReadByte(),
    StunDelay=r.ReadInt32(),KnockdownAge=r.ReadInt32(),ActionOrdinal=r.ReadInt32(),JumpHorizontal=r.ReadInt32(),JumpVelocity=r.ReadInt32(),DashVelocity=r.ReadInt32(),ThrowAge=r.ReadInt32(),
    HardKnockdown=r.ReadBoolean(),DizzyThisCombo=r.ReadBoolean(),AirAttack=r.ReadBoolean(),ProjectileSpawned=r.ReadBoolean(),HitGroups=r.ReadUInt64(),BufferedAction=r.ReadString(),BufferExpires=r.ReadInt64(),BufferedReversal=r.ReadBoolean(),DeferredParry=(ParryKind)r.ReadInt32(),ThrowAttacker=r.ReadInt32(),ThrowMove=r.ReadString(),PreviousX=r.ReadInt32(),PreviousY=r.ReadInt32()};
   if(p.Seat!=seat||!Content.Fighters.ContainsKey(p.FighterId)||p.Credits<0||p.Credits>Content.Economy.Cap||p.ReserveFloor<0||p.ReserveFloor>p.Credits||p.Health<0||p.Health>p.MaxHealth||p.Facing is not(1 or -1)||p.ActionFrame<0||p.RecoveryTier is <0 or >2)throw new InvalidDataException("Snapshot player bounds");
   var f=Content.Fighters[p.FighterId];if(p.ActionId.Length>0)f.Move(p.ActionId);
   for(int n=ReadCount(r,Content.Preparation.MaxProducts);n>0;n--){var id=r.ReadString();if(!Content.Items.ContainsKey(id))throw new InvalidDataException("Unknown lease");p.LeaseIds.Add(id);}
   for(int n=ReadCount(r,4096);n>0;n--)p.Receipts.Add(new(r.ReadString(),r.ReadString(),r.ReadInt32()));
   for(int n=ReadCount(r,120);n>0;n--){var sample=new InputSample(r.ReadInt64(),r.ReadByte(),r.ReadByte(),(Buttons)r.ReadByte(),r.ReadInt32());if(sample.RawDirection is <1 or >9||sample.RelativeDirection is <1 or >9||(byte)sample.Held>63)throw new InvalidDataException("Snapshot input bounds");p.History.Add(sample);}
   _players[seat]=p;
  }
  for(int n=ReadCount(r,16);n>0;n--)
  {
   var p=new ProjectileState{Id=r.ReadInt32(),Owner=r.ReadInt32(),MoveId=r.ReadString(),X=r.ReadInt32(),Y=r.ReadInt32(),Vx=r.ReadInt32(),LifeTicks=r.ReadInt32(),HitsRemaining=r.ReadInt32(),Hitstop=r.ReadInt32(),ContactCooldown=r.ReadInt32(),Facing=r.ReadInt32(),PreviousX=r.ReadInt32()};
   if(p.Owner is <0 or >1)throw new InvalidDataException("Snapshot projectile owner");_projectiles.Add(p);
  }
  ReadRouteState(r);ReadActorState(r);ReadObjectState(r);ReadV2State(r);
  if(stream.Position!=stream.Length||_players.Sum(p=>p.ScoreHalfPoints)!=CompletedRounds*2)throw new InvalidDataException("Snapshot trailing data or score parity");
 }
 static int ReadCount(BinaryReader reader,int max){int count=reader.ReadInt32();if(count<0||count>max)throw new InvalidDataException("Snapshot collection bound");return count;}
}
