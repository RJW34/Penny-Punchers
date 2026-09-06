using Godot;
using StrikeLedger.Core;

public partial class Main
{
    async Task UiBuyablesCatalogEvidence()
    {
        UiStep("trial selection, complete catalog and real practice");Title();await UiFrames(3);
        await UiActivate("01   Versus CPU");await UiActivate(RulesetName);
        UiRequire(screen=="rulesets","Trial chooser opens through the production lineup");await UiCapture("buyables-01-trial-chooser");
        await UiActivate("Choose expanded trial");UiRequire(rulesetId=="buyables_full"&&content.Items.Count==38&&content.Items.Values.Count(i=>PurchaseSlots.Contains(i.Slot))==24,"Expanded shop-only trial preserves24 rentals plus14 EX/super products");
        UiRequire(content.Fighters["rook"].DisplayName=="Vincent"&&content.Fighters["vale"].DisplayName=="Thomas","Both requested character names are visible registry names");
        for(int i=0;i<4&&stageId!="marist_green";i++)await UiActivate("Stage:");
        UiRequire(stageId=="marist_green","Marist Green is reachable through stage selection");await UiCapture("buyables-02-expanded-lineup");
        await UiActivate("Move catalog");UiRequire(screen=="catalog","Catalog opens before timed preparation");
        await UiActivate("Page ");UiRequire(ui.GetChildren().OfType<Button>().Any(b=>!b.IsQueuedForDeletion()&&b.Text.StartsWith("Page 2/2")),"Second rental page exposes all expanded choices");
        await UiCapture("buyables-03-rental-page-two");
        await UiActivate("EX licenses");UiRequire(ui.GetChildren().OfType<Button>().Count(b=>!b.IsQueuedForDeletion()&&(b.Text.Contains("600 CR")||b.Text.Contains("900 CR")))==4,"EX catalog shows four registry-priced repeatable licenses");
        await UiCapture("buyables-04-ex-catalog");await UiActivate("Super permits");
        UiRequire(ui.GetChildren().OfType<Button>().Any(b=>!b.IsQueuedForDeletion()&&b.Text.Contains("Overtime")),"Replacement install is visible only in the expanded library");
        await UiCapture("buyables-05-super-catalog");await UiActivate("Round rentals");
        string rental=content.Items.Values.Where(i=>PurchaseSlots.Contains(i.Slot)&&i.EligibleFighters.Contains(fighter[0])).OrderBy(i=>i.Id,StringComparer.Ordinal).First().MoveId;
        await UiActivate("Practice this move");UiRequire(screen=="lab_comparison"&&lab!=null,"Catalog opens the actual practice/comparison menu");
        await UiCapture("buyables-06-practice-menu");await UiActivate("Demonstrate selected move");
        for(int i=0;i<130;i++)await UiFrames(1);
        UiRequire(lab!.Recording.SelectMany(f=>f.Events).Any(e=>e.Kind==CombatEventKind.ActionStarted&&e.MoveId==rental),"Native practice feeds the selected rental through the real recognizer");
        await UiTap(UiPad0,JoyButton.Start);await UiActivate("Demo / free comparison");await UiActivate("Demonstrate free comparison");
        for(int i=0;i<100;i++)await UiFrames(1);
        UiRequire(sim!.Players[0].Leases.Count==0&&sim.Players[0].SpendReceipts.Count==0&&lab.Recording.SelectMany(f=>f.Events).Any(e=>e.Kind==CombatEventKind.ActionStarted),"Paired native free-kit demonstration removes equipment and has no activation fee");
        await UiCapture("buyables-07-free-comparison");Title();await UiFrames(3);await UiActivate("01   Versus CPU");await UiActivate(RulesetName);await UiActivate("Choose core trial");
        UiRequire(rulesetId=="buyables_core"&&content.Items.Count==26&&content.Items.Values.Count(i=>PurchaseSlots.Contains(i.Slot))==12&&content.Fighters["rook"].SuperArts.Any(a=>a.Name=="Rush Cascade"),"Core shop-only trial retains the third art and12 rentals plus14 EX/super products");
    }
}
