using Godot;
using StrikeLedger.App;
using StrikeLedger.Core;
public partial class Main
{
    TrainingConfiguration? pendingLabConfiguration;readonly int[] labLoadoutCategory=new int[2];
    void TryRentalInLab(int seat,string itemId)=>TryMoveInLab(seat,content.Items[itemId].MoveId);
    void OpenTrainingLoadouts(){pendingLabConfiguration=lab?.Configuration??new();DrawTrainingLoadouts();}
    void DrawTrainingLoadouts()
    {
        if(pendingLabConfiguration is not {} cfg)return;paused=true;Clear("lab_loadouts");Heading("Training fixture","BUILD BOTH SIDES.","Choose owned licenses and one super permit independently. Applying resets uses and the drill; no competitive purchase or payout occurs.");
        for(int seat=0;seat<2;seat++)
        {
            int s=seat;float x=55+s*610;string id=s==0?cfg.Fighter0:cfg.Fighter1;var plan=s==0?cfg.Loadout0:cfg.Loadout1;
            Button($"P{s+1}: {FighterName(id)}",x,187,554,()=>{string next=id=="rook"?"vale":"rook";pendingLabConfiguration=s==0?cfg with{Fighter0=next,Loadout0=new([])}:cfg with{Fighter1=next,Loadout1=new([])};DrawTrainingLoadouts();},s==0);
            string[] slots={"signature","technique","gambit","ex","super"};string slot=slots[labLoadoutCategory[s]%slots.Length];
            Button("Category: "+slot.ToUpperInvariant()+" →",x,242,554,()=>{labLoadoutCategory[s]=(labLoadoutCategory[s]+1)%slots.Length;DrawTrainingLoadouts();},false,20);
            var options=content.Items.Values.Where(i=>i.Slot==slot&&i.EligibleFighters.Contains(id)).OrderBy(i=>i.Id,StringComparer.Ordinal).ToArray();
            for(int row=0;row<options.Length;row++)
            {
                var item=options[row];bool owned=plan.ItemIds.Contains(item.Id);
                Button((owned?"✓ ":"+ ")+item.Name,x,297+row*52,554,()=>
                {
                    var ids=plan.ItemIds.ToList();if(owned)ids.Remove(item.Id);else{if(slot!="ex")ids.RemoveAll(i=>content.Items[i].Slot==slot);ids.Add(item.Id);}
                    var quote=PurchasePlanning.Preview(content,id,content.Economy.Cap,ids);if(!quote.Valid){Toast(quote.Error);return;}
                    var updated=new PreparationPlan(quote.ItemIds);pendingLabConfiguration=s==0?cfg with{Loadout0=updated}:cfg with{Loadout1=updated};DrawTrainingLoadouts();
                },false,18);
            }
            int credits=s==0?cfg.Credits0:cfg.Credits1;Button($"Saved bank: {credits} CR ↔",x,527,554,()=>{int next=credits>=content.Economy.Cap?0:Math.Min(content.Economy.Cap,credits+300);pendingLabConfiguration=s==0?cfg with{Credits0=next}:cfg with{Credits1=next};DrawTrainingLoadouts();},false,20);
            Button("Clear this kit",x,578,554,()=>{pendingLabConfiguration=s==0?cfg with{Loadout0=new([])}:cfg with{Loadout1=new([])};DrawTrainingLoadouts();},false,18);
        }
        Button("Apply fixture →",650,648,569,()=>{try{lab!.Configure(pendingLabConfiguration!);ClearPracticeSelection();BeginLab();}catch(Exception error){Toast(error.Message);}},false,23);Back(LabMenu);
    }
}
