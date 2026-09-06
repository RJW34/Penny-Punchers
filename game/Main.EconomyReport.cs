using Godot;
using StrikeLedger.App;
public partial class Main
{
    static void ExportEconomyFiles(string replayPath,ReplayRecord record)
    {
        string prefix=System.IO.Path.ChangeExtension(replayPath,null);ReplayFormat.ExportEconomy(record,prefix+".economy.json");
        var lines=new List<string>{"# Round economy","","Confirmed shop-only receipts. Opening bank − purchases + outcome grant + skill grant = closing bank. Bank is frozen throughout combat. Final-round funds are postmatch and unused.","","| Round | Seat | Opening | Purchases | Saved | Outcome nominal/granted | Skill earned/granted | Bank clipping | Closing | Tier | Next shop |","|---:|---:|---:|---:|---:|---|---|---:|---:|---|---|"};
        foreach(var round in RoundEconomyLedger.From(record))for(int seat=0;seat<2;seat++)
        {
            var row=round.Seats[seat];lines.Add($"| {round.Round} | {seat+1} | {row.OpeningCredits} | {row.PreparationSpend} | {row.SavedBank} | {row.ResultNominal}/{row.ResultGranted} | {row.SkillEarned}/{row.SkillGranted} | {row.ClippedIncome} | {row.ClosingCredits} | {row.OldRecoveryTier} → {row.NewRecoveryTier} | {(round.HasNextShop?"yes":"postmatch/unused")} |");
            foreach(var reward in row.Rewards)lines.Add($"\nRound {round.Round}, P{seat+1}, tick {reward.Tick}: {reward.Category}; nominal {reward.Nominal}, allowed {reward.Allowed}, skill-cap loss {reward.Capped}. Root `{reward.Root.Key}`.");
        }
        System.IO.File.WriteAllLines(prefix+".economy.md",lines);
    }
}
