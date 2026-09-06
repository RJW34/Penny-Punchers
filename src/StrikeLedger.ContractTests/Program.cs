using System.Text.Json;
using StrikeLedger.Core;
// Production arithmetic and catalog contracts only; separate suites verify actual gameplay.
static string FindRoot()
{
    var d=new DirectoryInfo(Environment.CurrentDirectory);
    while(d is not null){if(File.Exists(Path.Combine(d.FullName,"fixtures","shop_v2_economy_vectors.json")))return d.FullName;d=d.Parent;}
    throw new DirectoryNotFoundException("Run from the Penny-Punchers project root.");
}
static void Check(bool condition,string message){if(!condition)throw new Exception("CONTRACT CHECK FAILED: "+message);}
static string? Option(string[] args,string key){int i=Array.IndexOf(args,key);return i>=0&&i+1<args.Length?args[i+1]:null;}
try
{
    var root=FindRoot();var content=GameContent.Load(Option(args,"--data")??Path.Combine(root,"data"));var rules=content.Economy;int checks=0;
    using var file=JsonDocument.Parse(File.ReadAllText(Path.Combine(root,"fixtures","shop_v2_economy_vectors.json")));
    foreach(var v in file.RootElement.GetProperty("payouts").EnumerateArray())
    {
        var b=v.GetProperty("before");var exp=v.GetProperty("expected");
        var w=new Wallet(b.GetProperty("credits").GetInt32(),b.GetProperty("recovery_tier").GetInt32(),rules);
        var o=Enum.Parse<Outcome>(v.GetProperty("outcome").GetString()!,true);var (a,r)=EconomySeed.Payout(w,o,rules);
        Check(a.Credits==exp.GetProperty("credits").GetInt32()&&a.RecoveryTier==exp.GetProperty("recovery_tier").GetInt32(),"payout vector");
        Check(r.Clipped==v.GetProperty("clipped").GetInt32(),"clipped payout");checks++;
    }
    foreach(var v in file.RootElement.GetProperty("carts").EnumerateArray())
    {
        var products=v.GetProperty("products").EnumerateArray().Select(x=>x.GetString()!).ToArray();
        var quote=content.QuotePreparation(v.GetProperty("fighter").GetString()!,v.GetProperty("bank").GetInt32(),new PreparationPlan(products));
        Check(quote.Valid==v.GetProperty("valid").GetBoolean(),"mixed-cart eligibility");
        if(quote.Valid){Check(quote.TotalCost==v.GetProperty("cost").GetInt32(),"mixed-cart price");Check(quote.Remaining==v.GetProperty("remaining").GetInt32(),"saved bank");}
        checks++;
    }
    var report=new {scope="PRODUCTION_ARITHMETIC_AND_CATALOG_ONLY",status="PASS",passed=checks,content_hash=content.ContentHash,core_mvid=typeof(Simulation).Assembly.ManifestModule.ModuleVersionId,native_gameplay_verified=false};
    var json=JsonSerializer.Serialize(report,new JsonSerializerOptions{WriteIndented=true});
    if(Option(args,"--evidence-dir") is {} dir){Directory.CreateDirectory(dir);File.WriteAllText(Path.Combine(dir,"contract-conformance.json"),json+"\n");}
    Console.WriteLine(json);return 0;
}
catch(Exception e){Console.Error.WriteLine(e);return 1;}
