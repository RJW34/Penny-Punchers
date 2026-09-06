using StrikeLedger.Core;
namespace StrikeLedger.App;
public sealed record RoundEconomySeat(int OpeningCredits,int PreparationSpend,int SavedBank,
    int ResultNominal,int ResultGranted,int ResultClipped,int SkillNominal,int SkillEarned,int SkillCapped,int SkillGranted,int SkillClipped,
    int ClosingCredits,int OldRecoveryTier,int NewRecoveryTier,string[] Loadout,string SuperArtId,int SuperUsesConsumed,SkillRewardReceipt[] Rewards)
{
    public int NominalIncome=>ResultNominal+SkillEarned;
    public int GrantedIncome=>ResultGranted+SkillGranted;
    public int ClippedIncome=>ResultClipped+SkillClipped;
}
public sealed record RoundEconomyRecord(int Round,long TerminalTick,int WinnerSeat,string MatchDecision,RoundEconomySeat[] Seats)
{ public bool HasNextShop=>MatchDecision=="CONTINUE"; }
public static class RoundEconomyLedger
{
    public static IReadOnlyList<RoundEconomyRecord> From(ReplayRecord replay)
    {
        var result=new List<RoundEconomyRecord>();ReplayCommand? preparation=null;var events=new List<CombatEvent>();
        foreach(var command in replay.Commands)
        {
            if(command.Kind=="preparation"){preparation=command;events.Clear();}
            events.AddRange(command.Events);
            if(command.Settlement is not {} settled)continue;
            if(preparation?.Preparation is not {} paid)throw new InvalidDataException("Settlement has no recorded preparation receipt");
            var seats=new RoundEconomySeat[2];
            for(int seat=0;seat<2;seat++)
            {
                var payout=seat==0?settled.Payout0:settled.Payout1;var skills=seat==0?settled.SkillPayout0:settled.SkillPayout1;
                var plan=seat==0?preparation.Plan0!:preparation.Plan1!;
                int cost=seat==0?paid.Cost0:paid.Cost1,saved=seat==0?paid.Credits0:paid.Credits1,closing=seat==0?command.Wallet0:command.Wallet1;
                var rewards=command.Skills.Where(r=>r.EarnerSeat==seat).ToArray();int uses=(seat==0?command.Uses0:command.Uses1).Length;
                if(saved+payout.Granted+skills.Granted!=closing||payout.Nominal!=payout.Granted+payout.Clipped||skills.Earned!=skills.Granted+skills.Clipped||skills.Earned!=rewards.Sum(r=>r.Allowed)||events.Where(e=>e.Kind==CombatEventKind.SkillAward&&e.Seat==seat).Sum(e=>e.Value)!=skills.Earned)
                    throw new InvalidDataException("Shop-only round economy conservation failed");
                seats[seat]=new(saved+cost,cost,saved,payout.Nominal,payout.Granted,payout.Clipped,rewards.Sum(r=>r.Nominal),skills.Earned,rewards.Sum(r=>r.Capped),skills.Granted,skills.Clipped,closing,payout.OldTier,payout.NewTier,plan.ItemIds.ToArray(),seat==0?paid.SuperArt0:paid.SuperArt1,uses,rewards);
            }
            result.Add(new(settled.Round,command.Tick-1,settled.WinnerSeat,settled.MatchDecision,seats));preparation=null;events.Clear();
        }
        return result;
    }
}
