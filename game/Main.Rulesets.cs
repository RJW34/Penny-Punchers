using Godot;
using StrikeLedger.Core;
using System.Text.Json;

public partial class Main
{
    string rulesetId="buyables_core";
    // Legacy callers receive no free match-level entitlement. The mixed shop cart selects the round art.
    string SelectedArt(int seat)=>"";
    string RulesetName=>rulesetId=="buyables_full"?"Expanded trial":"Core trial";
    void LoadContentRuleset(string id)
    {
        if(id is not("buyables_core" or "buyables_full"))throw new InvalidDataException("Unknown local ruleset");
        var loaded=GameContent.Load(dataPath,id);
        var root=id=="buyables_core"?dataPath:System.IO.Path.Combine(dataPath,"rulesets",id);
        var json=new Dictionary<string,JsonElement>();
        foreach(var f in loaded.Fighters.Keys)
        {using var document=JsonDocument.Parse(System.IO.File.ReadAllText(System.IO.Path.Combine(root,"fighters",f+".json")));json[f]=document.RootElement.Clone();}
        using var itemDocument=JsonDocument.Parse(System.IO.File.ReadAllText(System.IO.Path.Combine(root,"items.json")));
        content=loaded;rulesetId=id;fighterJson.Clear();foreach(var entry in json)fighterJson[entry.Key]=entry.Value;items=itemDocument.RootElement.GetProperty("items").Clone();
        for(int seat=0;seat<2;seat++){if(!content.Fighters.ContainsKey(fighter[seat]))fighter[seat]=content.Fighters.Keys.First();}
    }
    void RulesetMenu()
    {
        Clear("rulesets");Heading("Choose the move library","TWO TRIALS. ONE WALLET.","Each trial has its own replay and private-match identity. Both use shop-only money and next-shop skill rewards.");
        Panel(54,186,570,377);Panel(662,186,564,377);
        Text("CORE TRIAL",77,208,31,Gold);Text("Core rentals / two choices per slot\n\nNew prices, Slipstream branches, Rivet Lift,\nfeints and range checks.\n\nBuy up to two EX licenses and one super.",77,268,22,Cream,522);
        Text("EXPANDED TRIAL",684,208,31,Cyan);Text("Expanded rentals / four choices per slot\n\nAdds counters, armor, air techniques,\nprojectile interactions and fields.\n\nOvertime and Prism Lattice are live\noptional super permits in this trial.",684,268,22,Cream,520);
        void Choose(string id){if(peer!=null){Toast("Leave the private match before changing trials.");return;}sim=null;recorder=null;replay=null;LoadContentRuleset(id);Select(mode);}
        Button("Choose core trial →",77,503,522,()=>Choose("buyables_core"),rulesetId=="buyables_core");
        Button("Choose expanded trial →",684,503,520,()=>Choose("buyables_full"),rulesetId=="buyables_full");
        Text("Trial balance is provisional. The lab explains each move and its free counterplay.",56,598,20,Muted,1158);Back(()=>Select(mode));
    }
}
