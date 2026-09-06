using Godot;
using StrikeLedger.Core;
public partial class Main
{
    sealed class PublicRoundView
    {
        public string[][] Products=[[],[]];
        public int[] Jumps=[0,0],Counters=[0,0],Parries=[0,0],Awards=[0,0];
        public bool[] SuperUsed=[false,false];
        public readonly HashSet<string> Events=[];
    }
    string publicShopSession="";
    readonly Dictionary<int,PublicRoundView> publicShopRounds=[];
    void SetCpuDraft()
    {
        if(sim==null||bot1==null)return;if(freeKitEvidence)return;
        var plan=bot1.ChoosePreparation(sim);DraftFromItems(1,plan.ItemIds);
    }
    void TrackShopPublicState()
    {
        if(sim==null||sim.Config.Training||mode=="replay")return;
        if(publicShopSession!=sim.Config.SessionId){publicShopSession=sim.Config.SessionId;publicShopRounds.Clear();}
        if(sim.Phase is not(MatchPhase.Fight or MatchPhase.Reveal or MatchPhase.Countdown))return;
        if(!publicShopRounds.TryGetValue(sim.RoundId,out var view))publicShopRounds[sim.RoundId]=view=new();
        for(int seat=0;seat<2;seat++)view.Products[seat]=sim.Players[seat].OwnedProductIds.Order(StringComparer.Ordinal).ToArray();
        while(publicShopRounds.Count>9)publicShopRounds.Remove(publicShopRounds.Keys.Min());
    }
    void TrackShopPublicEvent(CombatEvent e,string key)
    {
        if(sim==null||sim.Config.Training||mode=="replay")return;
        // A late confirmed cue can be drained after the next shop opens. Keep
        // the round captured in its immutable presentation key, not today's UI.
        var parts=key.Split(':');int round=sim.RoundId;
        if(parts.Length>0)int.TryParse(parts[0]=="local"&&parts.Length>1?parts[1]:parts[0],out round);
        if(round<1)return;
        if(!publicShopRounds.TryGetValue(round,out var view))publicShopRounds[round]=view=new();
        if(!view.Events.Add(key))return;int seat=Math.Clamp(e.Seat,0,1);
        if(e.Kind==CombatEventKind.Jump)view.Jumps[seat]++;
        if(e.Kind==CombatEventKind.Hit&&e.Detail=="counter")view.Counters[seat]++;
        if(e.Kind==CombatEventKind.Parry)view.Parries[seat]++;
        if(e.Kind==CombatEventKind.SkillAward)view.Awards[seat]+=e.Value;
        if(e.Kind==CombatEventKind.SuperUseConsumed)view.SuperUsed[seat]=true;
    }
    void DrawPreviousShopFacts()
    {
        if(sim==null)return;
        for(int seat=0;seat<2;seat++)
        {
            int other=1-seat;float x=54+seat*608;string line="FIRST ROUND / Both players start with the complete free base kit.";
            if(publicShopRounds.TryGetValue(sim.RoundId-1,out var view))
            {
                line=$"PREVIOUS OPPONENT / {view.Jumps[other]} jumps · {view.Counters[other]} counters · {view.Parries[other]} parries · skill +{view.Awards[other]}";
            }
            var strip=Text(line,x,146,11,Muted,570);strip.ClipText=true;strip.Size=new(570,18);strip.TooltipText=line;strip.MouseFilter=Control.MouseFilterEnum.Pass;
        }
    }
    void ShowPreviousShopFacts(int seat)
    {
        if(sim==null)return;var popup=new PopupMenu();ui.AddChild(popup);prepPopupSeat=seat;
        // Facts are non-actionable rows, but must retain readable contrast.
        popup.AddThemeColorOverride("font_disabled_color",Cream);
        popup.AddThemeColorOverride("font_color",Cream);
        void Fact(string text){popup.AddItem(text);popup.SetItemDisabled(popup.ItemCount-1,true);}
        Fact("PREVIOUS ROUND / PUBLIC OBSERVATIONS");
        if(publicShopRounds.TryGetValue(sim.RoundId-1,out var view))
        {
            int other=1-seat;Fact("Opponent: "+FighterName(fighter[other]));
            Fact("Revealed kit:");
            if(view.Products[other].Length==0)Fact("  Free base kit / no purchases");
            foreach(string id in view.Products[other])Fact("  "+(content.Items.TryGetValue(id,out var item)?item.Name:id));
            Fact("Super: "+(view.SuperUsed[other]?"used":"not used"));
            Fact($"Observed: {view.Jumps[other]} jumps, {view.Counters[other]} counter contacts, {view.Parries[other]} parries");
            Fact($"Skill awards: opponent +{view.Awards[other]}, you +{view.Awards[seat]} for next shop");
            Fact("These are counts from one round, not a prediction of their next plan.");
        }
        else Fact("No completed round in this match yet.");
        Fact("The buy-period timer continues while this view is open.");
        popup.AddItem("Close",1000);popup.IdPressed+=_=>popup.Hide();popup.PopupHide+=()=>{prepPopupSeat=-1;popup.QueueFree();};
        popup.Popup(new Rect2I(new Vector2I(seat==0?54:662,175),new Vector2I(570,0)));
    }
    // The old lease line is intentionally not drawn: the live HUD displays the complete mixed kit.
    void DrawLeaseSummary(){}
    string LeasePreview(int seat,int slot,int selected)
    {
        var options=SlotItems(seat,slot);return selected<0||selected>=options.Length?"Keep free base kit":RentalDescription(seat,options[selected]);
    }
}
