using Godot;
using StrikeLedger.Core;
using StrikeLedger.App;
using System.Text.Json;

public partial class Main
{
    // Navigate the production semantic rows through the owning software controller.
    async Task UiPrepSelect(int seat,int row)
    {
        var rows=PrepRows(seat);int target=Array.IndexOf(rows,row);
        if(target<0)throw new InvalidOperationException($"Unknown preparation row {seat}/{row}");
        for(int n=0;n<=rows.Length;n++)
        {
            int current=Array.IndexOf(rows,_preparationFocus[seat]);if(current==target)return;
            int down=(target-current+rows.Length)%rows.Length,up=(current-target+rows.Length)%rows.Length;
            await UiTap(seat==0?UiPad0:UiPad1,down<=up?JoyButton.DpadDown:JoyButton.DpadUp);
        }
        throw new InvalidOperationException($"Controller cannot reach preparation row {seat}/{row}");
    }

    // A competitive, replayable input sequence supplies the money and history.
    // Only menu navigation uses injected controller events; no actor, wallet,
    // reward, receipt or public-history state is fabricated by this driver.
    async Task UiShopOnlyPresentationEvidence()
    {
        await UiHealthBarEvidence();
        await UiNetworkTerminalHistoryEvidence();
        UiStep("shop-only earned history, real cart and super-use HUD");Title();
        mode="local";fighter=["rook","rook"];stageId="marist_green";paused=true;debugBoxes=false;lab=null;replay=null;
        var config=new MatchConfig{Fighter0="rook",Fighter1="rook",StageId=stageId,SessionId="ui-shop-only-real-inputs"};
        sim=new Simulation(content,config);recorder=new ReplayRecorder(content,config);ResetShopDrafts();ResetPresentationTimeline();Clear("fight");
        var events=new List<CombatEvent>();int steps=0;
        StepResult Step(byte d0=5,Buttons b0=Buttons.None,byte d1=5,Buttons b1=Buttons.None)
        {
            var result=recorder.Step(sim,new(0,sim.Tick,CoreMath.RelativeDirection(d0,sim.Players[0].Facing),b0),new(1,sim.Tick,CoreMath.RelativeDirection(d1,sim.Players[1].Facing),b1));
            events.AddRange(result.Events);ObserveChanges(result);UpdateArena();steps++;return result;
        }
        void Wait(int ticks){for(int i=0;i<ticks&&sim.Phase==MatchPhase.Fight;i++)Step();}
        void Countdown()
        {
            for(int i=0;i<600&&sim.Phase!=MatchPhase.Fight;i++)Step();
            UiRequire(sim.Phase==MatchPhase.Fight,"Shop visual sequence reaches Fight through real reveal/countdown steps");
        }
        recorder.CommitPreparation(sim,new([]),new([]));Countdown();
        int approach=0;while(Math.Abs(sim.Players[0].X-sim.Players[1].X)>36000&&approach++<180)Step(6,d1:6);
        UiRequire(approach<180,"Shop visual sequence uses legal walking to reach contact range");Wait(30);
        Step(d1:8);int prejump=0;while(sim.Players[1].Grounded&&prejump++<8)Step();
        UiRequire(!sim.Players[1].Grounded&&sim.Players[1].VoluntaryAir,"Actual opponent jump establishes voluntary airborne state");
        Step(b0:Buttons.LP);for(int i=0;i<15&&sim.Players[0].PendingSkillCredits==0;i++)Step();
        UiRequire(sim.Players[0].PendingSkillCredits==75&&sim.Players.All(p=>p.Credits==600)&&events.Any(e=>e.Kind==CombatEventKind.SkillAward&&e.Value==75),"Actual grounded anti-air earns75 NEXT SHOP without changing600 bank");
        await UiCapture("shop-v2-01-actual-pending-antiair");Wait(70);

        int attacks=0;
        while(sim.Phase==MatchPhase.Fight&&attacks++<60)
        {
            int walk=0;while(sim.Phase==MatchPhase.Fight&&Math.Abs(sim.Players[0].X-sim.Players[1].X)>36000&&walk++<90)Step(6);
            Wait(20);if(sim.Phase==MatchPhase.Fight)Step(b0:Buttons.LP);Wait(60);
            if(attacks%8==0)await UiFrames(1);
        }
        UiRequire(sim.Phase==MatchPhase.PendingResult&&sim.PendingResult?.WinnerSeat==0,"Ordinary legal inputs complete round1 with an actual winning terminal result");
        int earned=sim.Players[0].PendingSkillCredits;recorder.SettleRound(sim,sim.Tick);var settlement=sim.LastSettlement!;
        UiRequire(earned==75&&settlement.SkillPayout0.Granted==75&&sim.Players[0].Credits==1875,"Confirmed round settlement deposits actual1200 outcome and75 skill income");
        recorder.NextRound(sim);Preparation();paused=true;
        UiRequire(publicShopRounds.TryGetValue(1,out var facts)&&facts.Jumps[1]==1&&facts.Awards[0]==75,"Prior-round facts derive from the real confirmed jump and award events");
        await UiPrepSelect(0,PrepActionRow(0)+5);await UiTap(UiPad0,JoyButton.A);
        var popup=ui.GetChildren().OfType<PopupMenu>().Single(p=>!p.IsQueuedForDeletion());
        var factLines=Enumerable.Range(0,popup.ItemCount).Select(popup.GetItemText).ToArray();
        UiRequire(prepPopupSeat==0&&factLines.Any(t=>t.Contains("1 jumps"))&&factLines.Any(t=>t.Contains("you +75")),"Owner-controlled Facts popup shows actual observed jump and earned skill history");
        await UiCapture("shop-v2-02-confirmed-prior-facts");await UiTap(UiPad0,JoyButton.B);

        var rental=SlotItems(0,2).First(i=>i.Price==300&&content.Fighters["rook"].Move(i.MoveId).Mimic is not null);
        await UiPrepSelect(0,2);for(int i=0;i<=SlotItems(0,2).Length&&!DraftItems(0).Contains(rental.Id);i++)await UiTap(UiPad0,JoyButton.DpadRight);
        UiRequire(DraftItems(0).Contains(rental.Id),"Controller selects a real300-credit feint rental with full purpose/tradeoff detail");
        UiRequire(purchaseDetailLabels[0] is { } detail&&detail.Text.Contains(rental.Description)&&detail.Text.Contains(rental.Tradeoff),"Refreshed focused shop card retains the current product description and complete tradeoff");
        await UiCapture("shop-v2-03-focused-rental-detail");
        var ex=ShopProducts(0,"ex").First(i=>i.Price==600);var permit=ShopProducts(0,"super").Single(i=>i.Price==900);
        foreach(var product in new[]{ex,permit})
        {
            int row=purchaseProductIds.Single(k=>k.Key.Seat==0&&k.Value==product.Id).Key.Row;
            await UiPrepSelect(0,row);await UiTap(UiPad0,JoyButton.A);
        }
        UiRequire(DraftItems(0).Length==3&&DraftCost(0)==1800&&PlanPreview(0).Remaining==75&&sim.Players[0].Credits==1875,"Mixed rental/EX/super cart is affordable using the real prior-round payout, before atomic debit");
        UiRequire(purchaseDetailLabels[0] is { } superDetail&&superDetail.Text.Contains(permit.Description)&&superDetail.Text.Contains(permit.Tradeoff),"Repeated cart refresh retains the currently focused super detail");
        await UiCapture("shop-v2-04-mixed-super-cart");
        await UiPrepSelect(0,PrepActionRow(0)+4);await UiTap(UiPad0,JoyButton.A);await UiPrepSelect(1,PrepActionRow(1)+4);await UiTap(UiPad1,JoyButton.A);
        UiRequire(screen=="fight"&&sim.Players[0].Credits==75&&sim.Players[0].OwnedEx.Contains(ex.MoveId)&&sim.Players[0].SuperUsesRemaining==1,"Actual Ready callbacks atomically equip the license and one purchased super use");
        Countdown();UpdateArena();await UiCapture("shop-v2-05-super-ready");
        // A fresh complete motion supplies the actual attack button once;
        // no direct TryStartAction or assisted input is involved.
        Step();byte[] motion=[2,3,6,2,3,6];for(int n=0;n<motion.Length;n++)Step(motion[n],n==motion.Length-1?Buttons.LP:Buttons.None);
        UiRequire(sim.Players[0].SuperUsesRemaining==0&&sim.Players[0].SuperUseReceipts.Count==1&&sim.Players[0].Credits==75&&events.Count(e=>e.Kind==CombatEventKind.SuperUseConsumed)==1,"Fresh double-QCF+P consumes the purchased super once and preserves saved bank");
        await UiCapture("shop-v2-06-super-used");Wait(100);int beforeEvents=events.Count;
        Step();for(int n=0;n<motion.Length;n++)Step(motion[n],n==motion.Length-1?Buttons.LP:Buttons.None);
        UiRequire(events.Skip(beforeEvents).Any(e=>e.Kind==CombatEventKind.Rejected&&e.Detail=="exhausted")&&sim.Players[0].SuperUseReceipts.Count==1&&sim.Players[0].Credits==75,"A second legal super command is rejected as exhausted without another use or debit");
        await UiCapture("shop-v2-07-super-used-rejection");

        string tracePath=System.IO.Path.Combine(evidenceDir,"shop-v2-presentation.replay.json");recorder.Save(tracePath);string finalHash=sim.Hash();
        var verified=new ReplayPlayer(content,ReplayFormat.Load(tracePath,content.ContentHash));int replaySteps=0;while(verified.FrameAdvance())if(++replaySteps%1200==0)await UiFrames(1);
        UiRequire(verified.Simulation.Hash()==finalHash,"Complete shop visual sequence reconstructs actual inputs, purchases, awards and settlement from its competitive replay");
        System.IO.File.WriteAllText(System.IO.Path.Combine(evidenceDir,"shop-v2-presentation.json"),JsonSerializer.Serialize(new{passed=true,scope="Competitive Core input sequence and real GUI controller callbacks; no training mutations, fabricated history or physical-device claim",contentHash=content.ContentHash,build=ReplayFormat.Build,steps,attacks,earned,settlement,factLines,products=sim.Players[0].OwnedProductIds,bank=sim.Players[0].Credits,superUses=sim.Players[0].SuperUseReceipts,trace=tracePath,finalHash,reconstructed=true},new JsonSerializerOptions{WriteIndented=true}));
        Title();await UiFrames(3);
    }

    async Task UiHealthBarEvidence()
    {
        UiStep("both health-bar apertures / actual combat damage");Title();
        mode="local";fighter=["rook","vale"];stageId="foundry";paused=true;debugBoxes=false;lab=null;replay=null;
        var config=new MatchConfig{Fighter0="rook",Fighter1="vale",StageId=stageId,SessionId="ui-health-frame-real-inputs"};
        sim=new Simulation(content,config);recorder=new ReplayRecorder(content,config);ResetPresentationTimeline();Clear("fight");
        int steps=0;var snapshots=new List<object>();
        void Step(byte a=5,byte b=5,Buttons ab=Buttons.None,Buttons bb=Buttons.None)
        {
            var result=recorder.Step(sim,new(0,sim.Tick,CoreMath.RelativeDirection(a,sim.Players[0].Facing),ab),new(1,sim.Tick,CoreMath.RelativeDirection(b,sim.Players[1].Facing),bb));
            ObserveChanges(result);UpdateArena();steps++;
        }
        void Wait(int count){for(int i=0;i<count&&sim.Phase==MatchPhase.Fight;i++)Step();}
        async Task CaptureHealth(string name)
        {
            UpdateArena();await UiCapture(name);
            snapshots.Add(new{name,tick=sim.Tick,seats=sim.Players.Select(p=>new{p.Seat,p.Health,p.MaxHealth,ratio=p.Health/(float)p.MaxHealth,frame=HealthFrameDestination(p.Seat),aperture=HealthAperture(p.Seat),fill=HealthFillPolygon(p.Seat,p.Health/(float)p.MaxHealth)}).ToArray()});
        }
        recorder.CommitPreparation(sim,new([]),new([]));
        for(int i=0;i<600&&sim.Phase!=MatchPhase.Fight;i++)Step();
        UiRequire(sim.Phase==MatchPhase.Fight&&sim.Players.All(p=>p.Health==p.MaxHealth),"Health-frame review starts with both actual full-health actors");
        await CaptureHealth("health-frame-01-full");
        foreach(float threshold in new[]{.5f,.1f})
        {
            for(int attacker=0;attacker<2;attacker++)
            {
                var target=sim.Players[1-attacker];int attacks=0;
                while(target.Health>target.MaxHealth*threshold&&sim.Phase==MatchPhase.Fight&&attacks++<40)
                {
                    int approach=0;
                    while(Math.Abs(sim.Players[0].X-sim.Players[1].X)>35000&&approach++<180)
                        Step(attacker==0?(byte)6:(byte)5,attacker==1?(byte)6:(byte)5);
                    Wait(2);int before=target.Health;
                    Step(ab:attacker==0?Buttons.LP:Buttons.None,bb:attacker==1?Buttons.LP:Buttons.None);Wait(36);
                    UiRequire(target.Health<before,"Health-frame damage is a real LP contact from seat "+attacker);
                    await UiFrames(1);
                }
                UiRequire(target.Health>0&&target.Health<=target.MaxHealth*threshold&&sim.Phase==MatchPhase.Fight,"Health-frame seat "+(1-attacker)+" reaches "+threshold+" health through legal contacts without a terminal result");
            }
            Wait(36);await CaptureHealth(threshold==.5f?"health-frame-02-partial":"health-frame-03-near-empty");
        }
        foreach(int seat in new[]{0,1})
        {
            var aperture=HealthAperture(seat);var full=HealthFillPolygon(seat,1);
            UiRequire(HealthFillPolygon(seat,0).Length==0&&full.SequenceEqual(aperture),"Health-frame seat "+seat+" maps zero/full HP to empty/exact inner aperture");
            foreach(float ratio in new[]{.01f,.1f,.5f,.99f})
            {
                var polygon=HealthFillPolygon(seat,ratio);float min=aperture.Min(p=>p.X),max=aperture.Max(p=>p.X);
                float actual=seat==0?polygon.Max(p=>p.X):polygon.Min(p=>p.X),expected=seat==0?min+(max-min)*ratio:max-(max-min)*ratio;
                UiRequire(polygon.All(p=>p.X>=min-.001f&&p.X<=max+.001f)&&Math.Abs(actual-expected)<.001f,"Health-frame seat "+seat+" clips "+ratio+" HP from its mirrored anchor");
            }
        }
        string trace=System.IO.Path.Combine(evidenceDir,"health-frame.replay.json");recorder.Save(trace);string finalHash=sim.Hash();
        var verified=new ReplayPlayer(content,ReplayFormat.Load(trace,content.ContentHash));int replaySteps=0;while(verified.FrameAdvance())if(++replaySteps%1200==0)await UiFrames(1);
        UiRequire(verified.Simulation.Hash()==finalHash,"Health-frame visual damage sequence reconstructs from its competitive input replay");
        System.IO.File.WriteAllText(System.IO.Path.Combine(evidenceDir,"health-frame-evidence.json"),JsonSerializer.Serialize(new{passed=true,scope="Real competitive normal attacks; live production HUD and original atlas border. No HP mutation or pose fixture.",steps,snapshots,trace,finalHash,reconstructed=true},new JsonSerializerOptions{WriteIndented=true,IncludeFields=true}));
        Title();await UiFrames(3);
    }
}
