using Godot;
using StrikeLedger.Core;
using StrikeLedger.App;
using System.Text.Json;

public partial class Main
{
    static readonly string[] PurchaseSlots=["signature","technique","gambit"];
    readonly Dictionary<(int Seat,int Row),Button> purchaseButtons=[];
    readonly Dictionary<(int Seat,int Row),string> purchaseProductIds=[];
    readonly Label?[] purchaseDetailLabels=new Label?[2];
    readonly HashSet<string>[] shopDraft=[new(StringComparer.Ordinal),new(StringComparer.Ordinal)];
    readonly string[][] lastValidShopDraft=[[],[]],previousShopPlans=[[],[]];
    readonly bool[] hasPreviousShopPlan=[false,false];
    int prepPopupSeat=-1;
    ItemDefinition[] ShopProducts(int seat,string slot)=>content.Items.Values.Where(i=>i.Slot==slot&&i.EligibleFighters.Contains(fighter[seat])).OrderBy(i=>i.Id,StringComparer.Ordinal).ToArray();
    ItemDefinition[] SlotItems(int seat,int slot)=>ShopProducts(seat,PurchaseSlots[slot]);
    JsonElement[] ItemsFor(int seat)=>PurchaseSlots.SelectMany(slot=>items.EnumerateArray().Where(i=>i.GetProperty("slot").GetString()==slot&&i.GetProperty("eligible_fighters").EnumerateArray().Any(v=>v.GetString()==fighter[seat])).OrderBy(i=>i.GetProperty("id").GetString(),StringComparer.Ordinal)).ToArray();
    string[] DraftItems(int seat)=>shopDraft[seat].Order(StringComparer.Ordinal).ToArray();
    string[] LastValidDraftItems(int seat)=>lastValidShopDraft[seat].ToArray();
    int DraftCost(int seat)=>DraftItems(seat).Where(content.Items.ContainsKey).Sum(id=>content.Items[id].Price);
    PurchasePreview PlanPreview(int seat)=>PurchasePlanning.Preview(content,fighter[seat],sim!.Players[seat].Credits,DraftItems(seat));
    int[] PrepRows(int seat)=>purchaseButtons.Keys.Where(k=>k.Seat==seat).Select(k=>k.Row).Order().ToArray();
    int PrepActionRow(int seat)=>3+ShopProducts(seat,"ex").Length+ShopProducts(seat,"super").Length;
    void ResetShopDrafts()
    {
        for(int seat=0;seat<2;seat++){shopDraft[seat].Clear();lastValidShopDraft[seat]=[];if(sim?.CompletedRounds==0){previousShopPlans[seat]=[];hasPreviousShopPlan[seat]=false;}}
        if(sim?.CompletedRounds==0){publicShopRounds.Clear();publicShopSession=sim.Config.SessionId;}
        purchaseProductIds.Clear();Array.Fill(_preparationFocus,0);
    }
    bool ApplyShopDraft(int seat,IEnumerable<string> proposed,bool notify=true)
    {
        if(sim==null)return false;
        var ids=proposed.ToArray();var quote=PurchasePlanning.Preview(content,fighter[seat],sim.Players[seat].Credits,ids);
        if(!quote.Valid){if(notify)Toast(quote.Error+". Last valid cart is unchanged.");return false;}
        shopDraft[seat].Clear();shopDraft[seat].UnionWith(ids);lastValidShopDraft[seat]=DraftItems(seat);return true;
    }
    void DraftFromItems(int seat,IEnumerable<string> ids)=>ApplyShopDraft(seat,ids);
    void RememberShopPlan(int seat){previousShopPlans[seat]=LastValidDraftItems(seat);hasPreviousShopPlan[seat]=true;}
    void RefreshPurchase(bool network){if(network)NetworkPreparation(false);else Preparation(false);}
    bool CanEditShop(int seat,bool network)=>sim!=null&&sim.Phase==MatchPhase.Preparation&&(network||!ready[seat]);
    string[] ReplaceShopSlot(int seat,string slot,string? id)=>DraftItems(seat).Where(existing=>content.Items[existing].Slot!=slot).Concat(id==null?[]:[id]).ToArray();
    void OpenSlotOptions(int seat,int slot,bool network)
    {
        if(!CanEditShop(seat,network))return;
        var options=SlotItems(seat,slot);var popup=new PopupMenu();ui.AddChild(popup);prepPopupSeat=seat;
        popup.AddItem("Keep base kit / 0 CR",0);
        for(int index=0;index<options.Length;index++)
        {
            var item=options[index];var quote=PurchasePlanning.Preview(content,fighter[seat],sim!.Players[seat].Credits,ReplaceShopSlot(seat,item.Slot,item.Id));
            popup.AddItem($"{item.Name} / {item.Price} CR"+(quote.Valid?"":" / "+quote.Error),index+1);
            popup.SetItemTooltip(index+1,RentalDescription(seat,item));
        }
        popup.IdPressed+=id=>
        {
            if(!CanEditShop(seat,network))return;
            string? product=id==0?null:options[(int)id-1].Id;
            if(ApplyShopDraft(seat,ReplaceShopSlot(seat,PurchaseSlots[slot],product)))RefreshPurchase(network);
        };
        popup.PopupHide+=()=>{prepPopupSeat=-1;popup.QueueFree();};
        var origin=purchaseButtons[(seat,slot)].GetScreenPosition();popup.Popup(new Rect2I((Vector2I)origin,new Vector2I(534,0)));
    }
    // Left/right is one explicit attempted edit. Unaffordable choices are explained, never silently skipped.
    void CycleDraft(int seat,int slot,int direction=1,bool network=false)
    {
        if(!CanEditShop(seat,network))return;var options=SlotItems(seat,slot);
        int before=Array.FindIndex(options,i=>shopDraft[seat].Contains(i.Id));int next=(before+1+(direction<0?-1:1)+options.Length+1)%(options.Length+1)-1;
        if(ApplyShopDraft(seat,ReplaceShopSlot(seat,PurchaseSlots[slot],next<0?null:options[next].Id)))RefreshPurchase(network);
    }
    void ToggleShopProduct(int seat,string id,bool network)
    {
        if(!CanEditShop(seat,network))return;var item=content.Items[id];
        var proposed=shopDraft[seat].Contains(id)?DraftItems(seat).Where(i=>i!=id).ToArray():item.Slot=="super"?ReplaceShopSlot(seat,"super",id):DraftItems(seat).Append(id).ToArray();
        if(ApplyShopDraft(seat,proposed))RefreshPurchase(network);
    }
    void DrawPurchasePanel(int seat,float x,float y,bool network=false)
    {
        if(sim==null)return;var p=sim.Players[seat];var quote=PlanPreview(seat);Color accent=seat==0?Gold:Cyan;
        Panel(x,y,570,465);Text($"P{seat+1} / {FighterName(fighter[seat]).ToUpperInvariant()}",x+16,y+8,21,accent);
        Text($"BANK {p.Credits:N0}",x+390,y+8,23,Cream);
        Text($"CART {quote.TotalCost} / {content.Preparation.LoadoutCap} CR    KEEP {quote.Remaining} CR",x+16,y+38,17,Cream,532);
        Button Add(string label,int row,float bx,float by,float width,Action action,string product="",int size=15)
        {
            var b=Button(label,bx,by,width,action,false,size);b.Size=new(width,29);purchaseButtons[(seat,row)]=b;purchaseProductIds[(seat,row)]=product;
            b.FocusEntered+=()=>{_preparationFocus[seat]=row;UpdatePurchaseDetail(seat,network);};return b;
        }
        for(int slot=0;slot<3;slot++)
        {
            int captured=slot;var item=SlotItems(seat,slot).FirstOrDefault(i=>shopDraft[seat].Contains(i.Id));
            Add($"{PurchaseSlots[slot].ToUpperInvariant()} / "+(item==null?"Base kit · 0 CR":$"{item.Name} · {item.Price} CR")+" ▾",slot,x+16,y+67+slot*32,538,()=>OpenSlotOptions(seat,captured,network),item?.Id??"");
        }
        var ex=ShopProducts(seat,"ex");Text($"EX LICENSES / {ex.Count(i=>shopDraft[seat].Contains(i.Id))} OF 2 · REPEATABLE",x+17,y+165,13,accent);
        for(int n=0;n<ex.Length;n++)
        {
            var item=ex[n];Add($"{(shopDraft[seat].Contains(item.Id)?"✓":"○")} {item.Name} · {item.Price}",3+n,x+16+n%2*273,y+187+n/2*32,265,()=>ToggleShopProduct(seat,item.Id,network),item.Id,14);
        }
        int superRow=3+ex.Length;var supers=ShopProducts(seat,"super");Text("SUPER / OPTIONAL · ONE USE · CHOOSE AGAIN NEXT ROUND",x+17,y+253,13,accent);
        for(int n=0;n<supers.Length;n++)
        {
            var item=supers[n];Add($"{(shopDraft[seat].Contains(item.Id)?"✓":"○")} {item.Name} / {item.Price} CR",superRow+n,x+16,y+275+n*31,538,()=>ToggleShopProduct(seat,item.Id,network),item.Id,15);
        }
        var detail=Text("",x+18,y+373,14,Muted,534);purchaseDetailLabels[seat]=detail;
        detail.ClipText=true;detail.MaxLinesVisible=4;detail.Size=new(534,70);
        Text("TIMEOUT LOCKS THE LAST VALID CART SHOWN ABOVE",x+18,y+447,11,accent,534);
        int row=PrepActionRow(seat);float ay=y+474;
        Add("Save",row,x,ay,102,()=>SaveBudgetPlan(seat),size:14);
        Add("Load",row+1,x+111,ay,102,()=>LoadBudgetPlan(seat,network),size:14);
        Add("Repeat last",row+2,x+222,ay,157,()=>RepeatBudgetPlan(seat,network),size:14);
        Add("Keep cash",row+3,x+388,ay,182,()=>{if(CanEditShop(seat,network)&&ApplyShopDraft(seat,[]))RefreshPurchase(network);},size:14);
        Add(ready[seat]&&!network?$"P{seat+1} LOCKED ✓":$"P{seat+1} READY →",row+4,x,ay+36,258,()=>{if(network)SubmitNetworkPlan(seat);else ReadyPlan(seat);},size:20);
        Add("Prior round facts →",row+5,x+276,ay+36,294,()=>ShowPreviousShopFacts(seat),size:17);
        UpdatePurchaseDetail(seat,network);
    }
    string RentalDescription(int seat,ItemDefinition item)
    {
        var f=content.Fighters[fighter[seat]];var move=f.Move(item.MoveId);
        // The section above already states repeatable/one-use ownership. Keep
        // this four-line space for the decision: command, replacement and risk.
        string replaces=item.Replaces==null?"Base kit retained":$"Replaces {f.Move(item.Replaces).Name}";
        return $"{move.Command} · {move.Startup}/{move.Active}/{move.Recovery}f · {replaces}\n{item.Description}\nTradeoff: {item.Tradeoff}";
    }
    void UpdatePurchaseDetail(int seat,bool network)
    {
        // Clear queues the previous controls until the frame ends. A new label
        // can therefore receive a different Godot node name during a refresh;
        // hold the actual current control instead of resolving a display name.
        var label=purchaseDetailLabels[seat];
        string id=purchaseProductIds.GetValueOrDefault((seat,_preparationFocus[seat]),"");
        if(label!=null&&GodotObject.IsInstanceValid(label)&&!label.IsQueuedForDeletion())label.Text=content.Items.TryGetValue(id,out var item)?RentalDescription(seat,item):"All normals, ordinary specials and defense remain free.\nPurchases expire after this round. EX repeats; a super has one use.\nUse the untimed catalog for full comparisons and practice.";
        RefreshPreparationFocus();
    }
    void Preparation(bool reset=true)
    {
        if(sim==null)return;
        if(reset){ResetShopDrafts();ready=[false,mode is "bot" or "demo"];prepTicks=content.PreparationTicks;if(mode is "bot" or "demo")SetCpuDraft();}
        Clear("prep");purchaseButtons.Clear();purchaseProductIds.Clear();
        Heading($"ROUND {sim.RoundId} / BUY PERIOD","BUILD YOUR ROUND.","Local carts are visible. Each player owns their panel. All money is spent here; keep the rest for later.");
        DrawPreviousShopFacts();
        for(int seat=0;seat<2;seat++)DrawPurchasePanel(seat,54+seat*608,166);
        statusLabel=Text($"LOCK IN {(prepTicks+59)/60:00}s",1035,78,19,Gold);RefreshPreparationFocus();
    }
    void ReadyPlan(int seat)
    {
        if(sim==null||ready[seat])return;if(!PlanPreview(seat).Valid){Toast(PlanPreview(seat).Error);return;}
        ready[seat]=true;Preparation(false);if(ready.All(v=>v))LockPlans();
    }
    void LockPlans()
    {
        if(sim==null)return;
        try
        {
            var p0=PurchasePlanning.QuotedPlan(content,fighter[0],sim.Players[0].Credits,LastValidDraftItems(0));var p1=PurchasePlanning.QuotedPlan(content,fighter[1],sim.Players[1].Credits,LastValidDraftItems(1));
            if(recorder!=null)recorder.CommitPreparation(sim,p0,p1,$"prep-{sim.RoundId}");else sim.CommitPreparation(p0,p1);
            RememberShopPlan(0);RememberShopPlan(1);Clear("fight");inputGrace=18;arena.PlayCue("round");
        }
        catch(Exception ex){Toast(ex.Message);ready=[false,false];prepTicks=Math.Max(60,prepTicks);Preparation(false);}
    }
    void SaveBudgetPlan(int seat)
    {
        settings.BudgetPlans[fighter[seat]]=LastValidDraftItems(seat);settings.BudgetPlanQuotes[fighter[seat]]=new(PlanPreview(seat).TotalCost,content.ContentHash);settings.Save();Toast(settings.LastSaveError.Length>0?"Plan active; save failed: "+settings.LastSaveError:"Saved product IDs. Loading will recheck current prices, trial and bank.");
    }
    void LoadBudgetPlan(int seat,bool network=false)
    {
        if(!CanEditShop(seat,network))return;
        if(!settings.BudgetPlans.TryGetValue(fighter[seat],out var saved)){Toast("No saved plan for this fighter.");return;}
        if(ApplyShopDraft(seat,saved))
        {
            int total=PlanPreview(seat).TotalCost;string message=$"Loaded and checked: {total} CR. Ready commits it.";
            if(settings.BudgetPlanQuotes.TryGetValue(fighter[seat],out var old)&&(old.Cost!=total||old.ContentHash!=content.ContentHash))message=$"Saved quote {old.Cost} → now {total} CR (content changed). Ready commits it.";
            RefreshPurchase(network);Toast(message);
        }
    }
    void RepeatBudgetPlan(int seat,bool network=false)
    {
        if(!CanEditShop(seat,network))return;
        if(!hasPreviousShopPlan[seat]){Toast("No previous round plan in this match yet.");return;}
        if(previousShopPlans[seat].Length==0){if(ApplyShopDraft(seat,[])){RefreshPurchase(network);Toast("Repeated the previous free base-kit plan. Ready commits it.");}return;}
        if(ApplyShopDraft(seat,previousShopPlans[seat])){RefreshPurchase(network);Toast($"Previous plan repriced at {PlanPreview(seat).TotalCost} CR. Ready commits it.");}
    }
}
