using Godot;
using StrikeLedger.Core;
using StrikeLedger.App;

public partial class Main
{
    void CycleFighter(int seat)
    {
        var ids=content.Fighters.Keys.Order(StringComparer.Ordinal).ToArray();fighter[seat]=ids[(Array.IndexOf(ids,fighter[seat])+1)%ids.Length];
    }
    void Select(string nextMode)
    {
        mode=nextMode;Clear("select");Heading("Choose your corner",mode=="training"?"TRAINING LAB":"THE LINEUP","Choose fighters and stage. Buy EX licenses and an optional super during each round's shop.");
        for(int seat=0;seat<2;seat++)
        {
            int s=seat;float x=54+seat*608;var definition=content.Fighters[fighter[seat]];Panel(x,167,570,416);
            Text(seat==0?"01 / YOUR CORNER":mode=="bot"?"02 / CPU OPPONENT":"02 / OPPOSING CORNER",x+22,181,16,seat==0?Gold:Cyan);
            UiPortraitPanel(fighter[seat],new Rect2(x+438,181,110,110));
            Button(FighterName(fighter[seat]).ToUpperInvariant()+" ↔",x+20,216,399,()=>{CycleFighter(s);Select(mode);},seat==0,30);
            Text(definition.Archetype.Replace('_',' '),x+23,272,17,Muted,400);
            Text("YOUR ROUND, YOUR KIT",x+23,322,26,seat==0?Gold:Cyan);
            Text("Ordinary attacks and defense stay free.\n\nBuy up to two repeatable EX families.\nChoose one optional, single-use super each round.\nAll purchases expire after the round.",x+23,365,20,Cream,520);
            Button("Device: "+(mode=="bot"&&seat==1?"CPU":input.DeviceName(seat)),x+20,523,526,()=>{CycleDevice(s);Select(mode);},false,16);
        }
        if(!content.Stages.ContainsKey(stageId))stageId=content.Stages.Keys.First();
        Button("Stage: "+content.Stages[stageId].Name+" ↔",54,592,784,()=>{var stages=content.Stages.Keys.Order(StringComparer.Ordinal).ToArray();stageId=stages[(Array.IndexOf(stages,stageId)+1)%stages.Length];Select(mode);},false,20);
        Button(RulesetName+" ↔",854,592,372,RulesetMenu,false,19);
        Back();Button("Move catalog / try",253,648,279,()=>RentalCatalog(0),false,17);
        if(mode=="bot")Button("CPU: "+cpuPolicy+" ↔",550,648,350,()=>{cpuPolicy=cpuPolicy==BotMode.Adaptive?BotMode.Conservative:BotMode.Adaptive;Select(mode);},false,18);
        Button(mode=="training"?"Enter lab →":"Start match →",918,648,308,()=>{if(mode=="local"&&(input.Devices[0]<-1||input.Devices[1]<-1)){Toast("Assign keyboard + controller, or two controllers.");return;}StartMatch();});
    }
    void RentalCatalog(int seat,int selected=0,int category=0)
    {
        Clear("catalog");var definition=content.Fighters[fighter[seat]];category=Math.Clamp(category,0,3);
        var products=content.Items.Values.Where(i=>i.EligibleFighters.Contains(fighter[seat])).ToArray();
        var catalog=category==3?definition.Moves.OrderBy(m=>m.Id,StringComparer.Ordinal).ToArray():products.Where(i=>category==0?PurchaseSlots.Contains(i.Slot):i.Slot==(category==1?"ex":"super")).OrderBy(i=>i.Id,StringComparer.Ordinal).Select(i=>definition.Move(i.MoveId)).ToArray();
        selected=Math.Clamp(selected,0,Math.Max(0,catalog.Length-1));int page=selected/6;
        Heading("Untimed move catalog",FighterName(fighter[seat]).ToUpperInvariant()+" / KNOW YOUR PLAN.","Live moves and shop products. Nothing is purchased here; practice uses an explicitly labelled lab kit.");
        Button("Fighter: "+FighterName(fighter[seat])+" ↔",54,171,394,()=>{CycleFighter(seat);RentalCatalog(seat,0,category);},true,20);
        string[] categories=["Round rentals","EX licenses","Super permits","All actions"];
        for(int i=0;i<4;i++){int tab=i;Button((i==category?"● ":"")+categories[i],480+i*188,171,178,()=>RentalCatalog(seat,0,tab),false,15);}
        for(int row=0;row<6&&page*6+row<catalog.Length;row++)
        {
            int choice=page*6+row;var move=catalog[choice];var product=products.FirstOrDefault(i=>i.MoveId==move.Id);
            string cost=product!=null?$"{product.Price} CR":move.DerivedFrom.Length>0?"BRANCH":"BASE";
            var button=Button($"{(choice==selected?"●":"○")} {move.Name} / {cost}",54,232+row*55,394,()=>RentalCatalog(seat,choice,category),false,17);button.Size=new(394,47);
        }
        if(catalog.Length>6)Button($"Page {page+1}/{(catalog.Length+5)/6} →",54,568,394,()=>RentalCatalog(seat,((page+1)%((catalog.Length+5)/6))*6,category),false,18);
        if(catalog.Length>0)
        {
            var move=catalog[selected];var product=products.FirstOrDefault(i=>i.MoveId==move.Id);
            var entry=move;var seen=new HashSet<string>();
            while(product==null&&entry.DerivedFrom.Length>0&&seen.Add(entry.Id)){entry=definition.Move(entry.DerivedFrom);product=products.FirstOrDefault(i=>i.MoveId==entry.Id);}
            string practiceEntry=entry.Id;Panel(480,230,746,391);Text(move.Name.ToUpperInvariant(),502,245,28,Gold);
            string policy=product==null?"FREE BASE KIT":product.Slot=="super"?"SUPER PERMIT / ONE USE THIS ROUND":product.Slot=="ex"?"EX LICENSE / REPEATABLE THIS ROUND":"ROUND RENTAL / REPEATABLE";
            Text(policy+(product==null?"":$" / {product.Price} CR IN SHOP"),502,286,17,Cyan,698);
            Text($"{move.Command} / {move.Startup}/{move.Active}/{move.Recovery}f",502,318,19,Cream,698);
            string details=product==null?move.DesignRole.Replace('_',' '):$"{product.Description}\n\nTradeoff: {product.Tradeoff}";
            if(move.DerivedFrom.Length>0)details=$"Branch of {entry.Name}. Start the entry move, then use this move's legal branch input/window.\n\n"+details;
            var detail=new RichTextLabel{Text=details,Position=new(502,358),Size=new(698,137),ScrollActive=true,FitContent=false,FocusMode=Control.FocusModeEnum.All};detail.AddThemeFontSizeOverride("normal_font_size",17);detail.AddThemeColorOverride("default_color",Cream);ui.AddChild(detail);
            string comparison=product?.Replaces is {} replacement?$"Replaces free {definition.Move(replacement).Name}: {definition.Move(replacement).Startup}/{definition.Move(replacement).Active}/{definition.Move(replacement).Recovery}f.":product?.Slot=="ex"?"Buy the enhanced family; the ordinary variants remain free.":product?.Slot=="super"?"Choose again next shop. Whiff, block, parry or interruption still uses the permit.":"Ordinary attacks and defense remain free.";
            Text(comparison,502,504,17,Muted,698);
            Text(product==null?"No purchase or activation fee.":$"Shop price {product.Price} CR. At a {content.Economy.Cap} CR bank, keep {content.Economy.Cap-product.Price} CR after this product. No combat payments.",502,558,16,Gold,698);
            Button("Full card / counterplay",480,648,370,()=>CatalogFullCard(seat,selected,category,move,product),false,18);
            Button("Practice this move →",868,648,358,()=>TryMoveInLab(seat,move.DerivedFrom.Length>0?practiceEntry:move.Id),false,19);
        }
        Back(()=>Select(mode));
    }
    void CatalogFullCard(int seat,int selected,int category,MoveDefinition move,ItemDefinition? product)
    {
        Clear("catalog_card");Heading("Full move card",move.Name.ToUpperInvariant(),"Current live content. Frame counts are not an unmeasured advantage claim.");Panel(54,174,1172,445);
        string policy=product==null?"FREE BASE MOVE":product.Slot=="super"?"ONE SUPER USE THIS ROUND":"REPEATABLE ROUND LICENSE";
        string source=move.DerivedFrom.Length>0?$"Branch from {content.Fighters[fighter[seat]].Move(move.DerivedFrom).Name}. ":"";
        Text($"{policy} / {(product==null?0:product.Price)} CR IN SHOP\n{move.Command} / {move.Startup} startup · {move.Active} active · {move.Recovery} recovery\n\n{source}{product?.Description??move.DesignRole.Replace('_',' ')}\n\nCOUNTERPLAY / {product?.Tradeoff??"Use the authored spacing, guard, parry and recovery rules."}\n\nAll money is spent at shop lock. Ordinary moves and defense remain free.",77,197,21,Cream,1124);
        Text(ShopDeviceLegend(seat),78,565,16,Gold,1117);Back(()=>RentalCatalog(seat,selected,category));
    }
    string ShopDeviceLegend(int seat)
    {
        string[] names=["LP","MP","HP","LK","MK","HK"];int device=input.Devices[seat];
        if(device==-1)return "KEYBOARD / "+string.Join(" · ",names.Select((n,i)=>n+" "+OS.GetKeycodeString((Key)settings.Keys[4+i])));
        if(device>=0){var mapping=input.Mapping(device);return "CONTROLLER / "+string.Join(" · ",names.Select((n,i)=>n+" "+GameSettings.PadBindingName(mapping[i])));}
        return "Assign a device in the lineup to display its current bindings. Relative motion directions remain manual.";
    }

}
