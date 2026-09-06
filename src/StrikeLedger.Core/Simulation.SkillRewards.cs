namespace StrikeLedger.Core;

public sealed partial class Simulation
{
 ResolvedContactFacts[] _lastResolvedContacts=[];
 public IReadOnlyList<ResolvedContactFacts> LastResolvedContacts=>Array.AsReadOnly(_lastResolvedContacts);
 void ClearSkillDiagnostics()=>_lastResolvedContacts=[];
 void ProcessSkillContacts(IEnumerable<ResolvedContactFacts> facts)
 {
  _lastResolvedContacts=[];if(Phase!=MatchPhase.Fight)return;
  _lastResolvedContacts=facts.OrderBy(f=>f.Tick).ThenBy(f=>f.EarnerSeat).ThenBy(f=>f.Root).ThenBy(f=>f.ContactOrdinal).ToArray();
  foreach(var fact in _lastResolvedContacts)
  {
   if(fact.Tick!=Tick||fact.Root.SessionId!=Config.SessionId||fact.Root.Round!=RoundId||fact.Training!=Config.Training)throw new InvalidDataException("Contact facts do not belong to current world");
   var decision=_players[fact.EarnerSeat].SkillRewards.Apply(fact);
   if(Config.Training)
   {
    var category=SkillRewardLedger.Classify(fact,Content.SkillRewards);
    if(category!=SkillRewardCategory.None)Emit(CombatEventKind.SkillOpportunity,fact.EarnerSeat,1-fact.EarnerSeat,fact.MoveId,Content.SkillRewards.Amount(category),SkillLabel(category)+" · PRACTICE ONLY",fact.WorldX,fact.WorldY,actionOrdinal:fact.Root.ActionOrdinal,sourceFighterId:fact.SourceFighterId);
   }
   else if(decision.Allowed>0)Emit(CombatEventKind.SkillAward,fact.EarnerSeat,1-fact.EarnerSeat,fact.MoveId,decision.Allowed,SkillLabel(decision.Category)+" · NEXT SHOP",fact.WorldX,fact.WorldY,actionOrdinal:fact.Root.ActionOrdinal,sourceFighterId:fact.SourceFighterId);
  }
 }
 public static string SkillLabel(SkillRewardCategory category)=>category switch{SkillRewardCategory.CounterHit=>"COUNTER-HIT",SkillRewardCategory.AntiAir=>"ANTI-AIR",SkillRewardCategory.PerfectParry=>"PERFECT PARRY",_=>""};
 static SkillSettlementReceipt BuildSkillSettlement(PlayerState player,int walletSpace)
 {
  int earned=player.PendingSkillCredits,granted=Math.Min(earned,Math.Max(0,walletSpace));return new(earned,granted,earned-granted);
 }
 void WriteSkillState(BinaryWriter writer){foreach(var player in _players)player.SkillRewards.Write(writer);}
 void ReadSkillState(BinaryReader reader){foreach(var player in _players)player.SkillRewards=SkillRewardLedger.Read(reader,Content.SkillRewards);}
}
