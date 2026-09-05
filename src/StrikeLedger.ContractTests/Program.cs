using System.Text.Json;
using StrikeLedger.Core;
// This harness verifies only seed arithmetic. It is NOT game acceptance.
static string FindRoot()
{
    var d=new DirectoryInfo(Environment.CurrentDirectory);
    while(d is not null){if(File.Exists(Path.Combine(d.FullName,"data","economy.json")))return d.FullName;d=d.Parent;}
    throw new DirectoryNotFoundException("Run from the extracted project root.");
}
static void Check(bool condition,string message){if(!condition)throw new Exception("SEED CHECK FAILED: "+message);}
try
{
    var root=FindRoot();var rules=EconomyRules.Load(Path.Combine(root,"data","economy.json"));int checks=0;
    using var file=JsonDocument.Parse(File.ReadAllText(Path.Combine(root,"fixtures","economy_vectors.json")));
    foreach(var v in file.RootElement.GetProperty("payouts").EnumerateArray())
    {
        var b=v.GetProperty("before");var exp=v.GetProperty("expected");
        var w=new Wallet(b.GetProperty("credits").GetInt32(),b.GetProperty("recovery_tier").GetInt32(),rules);
        var o=Enum.Parse<Outcome>(v.GetProperty("outcome").GetString()!,true);var (a,r)=EconomySeed.Payout(w,o,rules);
        Check(a.Credits==exp.GetProperty("credits").GetInt32()&&a.RecoveryTier==exp.GetProperty("recovery_tier").GetInt32(),"payout vector");
        Check(r.Clipped==v.GetProperty("clipped").GetInt32(),"clipped payout");checks++;
    }
    foreach(var v in file.RootElement.GetProperty("spends").EnumerateArray())
    {
        var s=new SpendSnapshot(new Wallet(v.GetProperty("credits").GetInt32(),0,rules),v.GetProperty("floor").GetInt32());
        var (a,status)=s.Activate("test",v.GetProperty("cost").GetInt32(),"e",v.GetProperty("legal").GetBoolean(),rules);
        Check(a.Wallet.Credits==v.GetProperty("expected_credits").GetInt32(),"spend vector");
        Check(status.ToString().ToUpperInvariant()==v.GetProperty("status").GetString(),"spend status");checks++;
    }
    var before=new SpendSnapshot(new Wallet(1200,0,rules),0);var first=before.Activate("super",900,"f10",true,rules).State;
    var repeated=first.Activate("super",900,"f10",true,rules);Check(repeated.State.Wallet.Credits==300&&repeated.Status==ActivationStatus.Duplicate,"single debit");
    Check(before.Wallet.Credits==1200&&before.Receipts.Count==0,"immutable snapshot restore");checks+=2;
    Console.WriteLine($"SCAFFOLD_SEED_CONFORMANCE_ONLY: {checks} checks passed. Gameplay/network/device/feel NOT VERIFIED.");return 0;
}
catch(Exception e){Console.Error.WriteLine(e);return 1;}
