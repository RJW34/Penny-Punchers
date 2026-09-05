using Godot;
using StrikeLedger.App;
public partial class Main
{
    static void ExportEconomyFiles(string replayPath,ReplayRecord record){string prefix=System.IO.Path.ChangeExtension(replayPath,null);ReplayFormat.ExportEconomy(record,prefix+".economy.json");var lines=new List<string>{"# Round economy","","Credits come only from confirmed round settlements. Preparation and action debits use the same wallet.","","| Tick | Event | P1 credits | P2 credits |","|---:|---|---:|---:|"};foreach(var c in record.Commands.Where(c=>c.Kind is "preparation" or "settlement"))lines.Add($"| {c.Tick} | {c.Kind} | {c.Wallet0} | {c.Wallet1} |");System.IO.File.WriteAllLines(prefix+".economy.md",lines);}
}
