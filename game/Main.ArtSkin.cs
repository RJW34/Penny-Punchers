using Godot;
using StrikeLedger.Presentation;
using StrikeLedger.Core;

public partial class Main
{
    // Supplied AFTER HOURS artboards remain unchanged. These rectangular regions are
    // reusable decoration; all values, state labels and GUI callbacks remain live.
    readonly Dictionary<string,Texture2D> uiArtTextures=new();
    readonly Dictionary<string,AtlasTexture> uiArtCrops=new();
    readonly Dictionary<string,StyleBoxTexture> uiArtFrames=new();
    readonly float[] _healthTrail=[0,0];readonly int[] _hudHealth=[0,0];readonly long[] _healthTrailWait=[0,0];
    Simulation? _hudSimulation;long _hudTick=-1;
    Texture2D UiArt(string file)
    {
        if(!uiArtTextures.TryGetValue(file,out var texture))
            uiArtTextures[file]=texture=GD.Load<Texture2D>("res://Assets/AfterHours/UI/"+file+".png");
        return texture;
    }
    AtlasTexture UiArt(string file,Rect2I rect)
    {
        string key=$"{file}:{rect.Position.X},{rect.Position.Y},{rect.Size.X},{rect.Size.Y}";
        if(!uiArtCrops.TryGetValue(key,out var crop))
            uiArtCrops[key]=crop=new AtlasTexture{Atlas=UiArt(file),Region=new Rect2(rect.Position,rect.Size),FilterClip=true};
        return crop;
    }
    StyleBoxTexture UiArtFrame(string state="normal")
    {
        if(!uiArtFrames.TryGetValue(state,out var box))
        {
            box=new StyleBoxTexture
            {
                Texture=UiArt("interface-atlas",new Rect2I(51,41,316,184)),
                TextureMarginLeft=15,TextureMarginRight=15,TextureMarginTop=15,TextureMarginBottom=15,
                ContentMarginLeft=17,ContentMarginRight=15,ContentMarginTop=4,ContentMarginBottom=4,
                ModulateColor=state switch
                {
                    "hover" or "active-p1"=>new Color(1.15f,1.02f,.72f),"active-p2"=>new Color(.82f,1.18f,1.14f),"pressed"=>new Color(.65f,.77f,.79f),
                    "panel"=>new Color(.67f,.80f,.87f,.97f),"hud"=>new Color(.46f,.64f,.70f,.99f),
                    _=>new Color(.78f,.90f,.95f)
                }
            };
            uiArtFrames[state]=box;
        }
        return box;
    }
    TextureRect UiArtImage(Texture2D texture,Rect2 rect,Color? tint=null)
    {
        var image=new TextureRect
        {
            ExpandMode=TextureRect.ExpandModeEnum.IgnoreSize,StretchMode=TextureRect.StretchModeEnum.Scale,
            Texture=texture,Position=rect.Position,Size=rect.Size,
            MouseFilter=Control.MouseFilterEnum.Ignore,TextureFilter=CanvasItem.TextureFilterEnum.Nearest,
            Modulate=tint??Colors.White
        };
        ui.AddChild(image);return image;
    }
    void UiArtBackdrop(bool title=false)
    {
        arena.Visible=false;
        UiArtImage(UiArt("title-backdrop"),new Rect2(0,0,1280,720));
        if(!title)ui.AddChild(new ColorRect{Position=Vector2.Zero,Size=new Vector2(1280,720),Color=new Color(.018f,.035f,.05f,.87f),MouseFilter=Control.MouseFilterEnum.Ignore});
    }
    void UiArtPanel(float x,float y,float width,float height,Color? tint=null)
    {
        if(tint.HasValue)
        {
            ui.AddChild(new ColorRect{Position=new(x,y),Size=new(width,height),Color=tint.Value,MouseFilter=Control.MouseFilterEnum.Ignore});
            return;
        }
        var panel=new NinePatchRect
        {
            Position=new(x,y),Size=new(width,height),Texture=UiArtFrame("panel").Texture,
            PatchMarginLeft=15,PatchMarginRight=15,PatchMarginTop=15,PatchMarginBottom=15,
            Modulate=new Color(.68f,.80f,.87f,.97f),MouseFilter=Control.MouseFilterEnum.Ignore,
            TextureFilter=CanvasItem.TextureFilterEnum.Nearest
        };
        ui.AddChild(panel);
    }
    void UiSkinButton(Button button)
    {
        button.TextureFilter=CanvasItem.TextureFilterEnum.Nearest;
        button.AddThemeColorOverride("font_color",Cream);
        button.AddThemeColorOverride("font_hover_color",Cream);
        button.AddThemeColorOverride("font_focus_color",Gold);
        button.AddThemeStyleboxOverride("normal",UiArtFrame());
        button.AddThemeStyleboxOverride("hover",UiArtFrame("hover"));
        button.AddThemeStyleboxOverride("pressed",UiArtFrame("pressed"));
        button.AddThemeStyleboxOverride("focus",new StyleBoxFlat{BgColor=Colors.Transparent,BorderColor=Gold,BorderWidthLeft=3,BorderWidthRight=2,BorderWidthTop=2,BorderWidthBottom=2});
    }
    Texture2D UiPortrait(string fighterId,bool faceOnly=false)
    {
        return fighterId!="vale"
            ?UiArt("rook-identity",faceOnly?new Rect2I(127,24,274,335):new Rect2I(13,12,485,484))
            :UiArt("vale-identity",faceOnly?new Rect2I(818,23,281,340):new Rect2I(760,19,344,474));
    }
    void UiPortraitPanel(string fighterId,Rect2 rect)
    {
        UiArtPanel(rect.Position.X,rect.Position.Y,rect.Size.X,rect.Size.Y);
        var portrait=UiArtImage(UiPortrait(fighterId,true),new Rect2(rect.Position+new Vector2(7,7),rect.Size-new Vector2(14,14)));
        portrait.StretchMode=TextureRect.StretchModeEnum.KeepAspectCentered;
        if(fighterId is not ("rook" or "vale"))Text("ART PLACEHOLDER",rect.Position.X+8,rect.End.Y-24,12,Cream,rect.Size.X-16);
    }
    Texture2D UiLeaseArt(string fighterId,int column,bool super=false)
    {
        int row=(fighterId=="vale"?2:0)+(super?1:0);
        int[] xs=[40,289,539,788,1038,1289],ys=[53,287,523,757];
        return UiArt("lease-icons",new Rect2I(xs[Math.Clamp(column,0,5)],ys[row],212,199));
    }
    void UiScreenIcon()
    {
        int column=screen switch{"settings"=>0,"help" or "moves"=>1,"replays" or "replay_controls"=>2,"labmenu" or "lab_state"=>3,"select" or "prep" or "results"=>4,_=>5};
        UiArtImage(UiArt("interface-atlas",new Rect2I(60+column*240,527,197,122)),new Rect2(1157,31,68,43),new Color(.8f,.84f,.83f));
    }
    void UiArtWordmark()
    {
        var penny=Text("PENNY",54,74,88,Gold,500);penny.AddThemeFontOverride("font",UiTypography.Heading);
        var punchers=Text("PUNCHERS",54,158,88,Cream,530);punchers.AddThemeFontOverride("font",UiTypography.Heading);
        Text("ONE WALLET. EVERY DECISION COUNTS.",59,271,19,Cream,620);
    }
    void UiRefreshPreparationSkin()
    {
        foreach(var entry in purchaseButtons)
        {
            var button=entry.Value;if(button.IsQueuedForDeletion())continue;
            int seat=entry.Key.Seat,row=entry.Key.Row;
            bool active=row==_preparationFocus[seat]&&!ready[seat];
            button.AddThemeStyleboxOverride("normal",UiArtFrame(active?(seat==0?"active-p1":"active-p2"):"normal"));
        }
    }
    void DrawArtHud()
    {
        if(sim==null)return;
        if(!ReferenceEquals(_hudSimulation,sim)||sim.Tick<_hudTick){_hudSimulation=sim;for(int i=0;i<2;i++)_healthTrail[i]=_hudHealth[i]=sim.Players[i].Health;_hudTick=sim.Tick;}
        int elapsed=(int)Math.Clamp(sim.Tick-_hudTick,0,6);_hudTick=sim.Tick;
        TextureFilter=CanvasItem.TextureFilterEnum.Nearest;
        DrawStyleBox(UiArtFrame("hud"),new Rect2(-12,-12,1304,171));
        for(int seat=0;seat<2;seat++)
        {
            var p=sim.Players[seat];bool left=seat==0;Color accent=left?Gold:Cyan;
            var definition=content.Fighters[p.FighterId];
            var selected=definition.SuperArts.FirstOrDefault(a=>a.Id==p.SelectedSuper);
            var ownedEx=p.OwnedEx.Select(id=>definition.Move(id)).OrderBy(m=>m.Id,StringComparer.Ordinal).ToArray();
            if(p.Health>_healthTrail[seat])_healthTrail[seat]=p.Health;
            if(p.Health<_hudHealth[seat])_healthTrailWait[seat]=sim.Tick+21;
            _hudHealth[seat]=p.Health;
            if(sim.Tick>_healthTrailWait[seat]&&p.Hitstop==0&&sim.FullFreeze==0)_healthTrail[seat]=Math.Max(p.Health,_healthTrail[seat]-elapsed*Math.Max(1,p.MaxHealth/45f));
            float portraitX=left?27:1181,barX=left?117:713,textX=left?42:736;
            DrawStyleBox(UiArtFrame(),new Rect2(portraitX,14,73,77));
            var portrait=UiPortrait(p.FighterId,true);var slot=new Rect2(portraitX+6,20,61,65);
            float portraitScale=Math.Min(slot.Size.X/portrait.GetWidth(),slot.Size.Y/portrait.GetHeight());var portraitSize=portrait.GetSize()*portraitScale;
            DrawTextureRect(portrait,new Rect2(slot.GetCenter()-portraitSize/2,portraitSize),false);
            DrawString(UiTypography.Heading,new(barX,30),FighterName(p.FighterId).ToUpperInvariant(),HorizontalAlignment.Left,p.InstallTicks>0?124:265,26,Cream);
            if(p.InstallTicks>0)
            {
                // A diminishing installed state, never a refillable gauge or cast stock.
                var clock=new Vector2(barX+137,23);DrawArc(clock,7,0,Mathf.Tau,16,accent,1.5f);
                DrawLine(clock,clock+new Vector2(0,-4),accent,1.5f);DrawLine(clock,clock+new Vector2(4,1),accent,1.5f);
                DrawString(font,new(barX+150,28),$"OVERTIME {p.InstallTicks/60f:0.0}s",HorizontalAlignment.Left,117,12,accent);
            }
            DrawString(font,new(barX+271,29),$"P{seat+1} / {p.ScoreHalfPoints/2f:0.#} PTS",HorizontalAlignment.Right,177,16,accent);
            var frame=UiArt("hud-atlas",HealthFrameSource(seat));
            DrawTextureRect(frame,HealthFrameDestination(seat),false);
            float health=Math.Clamp(p.Health/(float)Math.Max(1,p.MaxHealth),0,1);
            float trail=Math.Clamp(_healthTrail[seat]/Math.Max(1,p.MaxHealth),0,1);
            if(trail>health)DrawHealthFill(seat,trail,new Color(.84f,.41f,.29f,.85f));
            if(health>0)
                DrawHealthFill(seat,health,Colors.White,UiArt("hud-atlas",health<.2f?new Rect2I(83,380,57,19):left?new Rect2I(172,48,400,21):new Rect2I(991,48,390,21)));
            if(ownedEx.Length==0)DrawString(font,new(textX,102),"EX / NONE IN THIS ROUND'S KIT",HorizontalAlignment.Left,502,16,Muted);
            for(int exIndex=0;exIndex<ownedEx.Length;exIndex++)
            {
                var ex=ownedEx[exIndex];float exX=textX+exIndex*250;bool active=p.ActionId==ex.Id;
                DrawRect(new Rect2(exX,83,27,24),active?accent:new Color(accent,.18f));
                DrawString(font,new(exX+4,100),"EX",HorizontalAlignment.Left,24,14,active?new Color(.04f,.07f,.09f):accent);
                DrawString(font,new(exX+34,102),ex.Name,HorizontalAlignment.Left,208,16,active?accent:Cream);
            }
            string superState=selected==null?"SUPER / NONE":$"SUPER {(p.SuperUsesRemaining>0?"READY":"USED")} / {selected.Name.ToUpperInvariant()}";
            DrawString(font,new(textX,124),superState,HorizontalAlignment.Left,502,17,selected!=null&&p.SuperUsesRemaining>0?accent:Muted);
            DrawString(font,new(textX,140),$"SAVED BANK {p.Credits}",HorizontalAlignment.Left,157,12,Muted);
            DrawString(font,new(textX+157,140),$"+{p.PendingSkillCredits} NEXT SHOP",HorizontalAlignment.Left,155,12,p.PendingSkillCredits>0?accent:Muted);
            DrawString(font,new(textX+316,140),$"STUN {p.Stun}/{definition.StunLimit}",HorizontalAlignment.Left,157,12,Muted);
            DrawRect(new Rect2(textX+316,144,150,3),new Color(.08f,.13f,.16f));
            DrawRect(new Rect2(textX+316,144,150*Math.Clamp(p.Stun/(float)Math.Max(1,definition.StunLimit),0,1),3),new Color(.75f,.78f,.74f));
            var rentals=p.OwnedProductIds.Where(content.Items.ContainsKey).Select(id=>content.Items[id]).Where(i=>PurchaseSlots.Contains(i.Slot)).Select(i=>i.Name).ToArray();
            DrawString(font,new(textX,158),rentals.Length==0?"BASE MOVES / NO RENTAL REPLACEMENTS":string.Join(" / ",rentals).ToUpperInvariant(),HorizontalAlignment.Left,502,11,Muted);

        }
        DrawStyleBox(UiArtFrame(),new Rect2(587,14,106,106));
        DrawString(font,new(594,77),$"{Math.Max(0,sim.TimerTicks+59)/60:00}",HorizontalAlignment.Center,92,47,Cream);
        DrawString(font,new(588,138),$"ROUND {sim.RoundId}",HorizontalAlignment.Center,104,13,Gold);
    }
    static Rect2I HealthFrameSource(int seat)=>seat==0?new(129,176,512,54):new(897,176,511,54);
    static Rect2 HealthFrameDestination(int seat)=>new(seat==0?113:709,39,454,40);
    static Vector2[] HealthAperture(int seat)
    {
        // Measured inside the cream border of the supplied empty-frame artwork.
        // Coordinates share the frame's source-to-destination transform: neither
        // live HP nor the delayed damage trail can cover its sloping edges.
        Vector2[] source=seat==0?[new(139,185),new(575,185),new(598,211),new(163,211)]
            :[new(962,185),new(1394,185),new(1371,211),new(940,211)];
        var crop=HealthFrameSource(seat);var destination=HealthFrameDestination(seat);
        return source.Select(p=>destination.Position+(p-(Vector2)crop.Position)*destination.Size/(Vector2)crop.Size).ToArray();
    }
    static Vector2[] HealthFillPolygon(int seat,float ratio)
    {
        ratio=Math.Clamp(ratio,0,1);if(ratio==0)return [];
        var aperture=HealthAperture(seat);if(ratio==1)return aperture;
        float min=aperture.Min(p=>p.X),max=aperture.Max(p=>p.X);
        float edge=seat==0?min+(max-min)*ratio:max-(max-min)*ratio;
        bool Inside(Vector2 p)=>seat==0?p.X<=edge:p.X>=edge;
        var clipped=new List<Vector2>(5);var previous=aperture[^1];bool wasInside=Inside(previous);
        foreach(var point in aperture)
        {
            bool isInside=Inside(point);
            if(isInside!=wasInside)clipped.Add(previous.Lerp(point,(edge-previous.X)/(point.X-previous.X)));
            if(isInside)clipped.Add(point);
            previous=point;wasInside=isInside;
        }
        return clipped.ToArray();
    }
    void DrawHealthFill(int seat,float ratio,Color color,Texture2D? texture=null)
    {
        var polygon=HealthFillPolygon(seat,ratio);if(polygon.Length<3)return;
        if(texture==null){DrawColoredPolygon(polygon,color);return;}
        var aperture=HealthAperture(seat);float min=aperture.Min(p=>p.X),max=aperture.Max(p=>p.X);
        float top=aperture.Min(p=>p.Y),bottom=aperture.Max(p=>p.Y);
        var uv=polygon.Select(p=>new Vector2((p.X-min)/(max-min),(p.Y-top)/(bottom-top))).ToArray();
        DrawPolygon(polygon,new Color[]{color},uv,texture);
    }
    void DrawArtAnnouncement(string text)
    {
        if(text is "K.O." or "ROUND SETTLED"&&sim?.PendingResult is {} terminal)text=terminal.Reason=="TIME"?terminal.WinnerSeat<0?"TIME / DRAW":"TIME UP":terminal.WinnerSeat<0?"DOUBLE K.O.":"K.O.";
        if(text=="K.O.")DrawTextureRect(UiArt("branding-announcements",new Rect2I(28,561,484,142)),new Rect2(397,278,486,142),false);
        else{DrawStyleBox(UiArtFrame(),new Rect2(352,270,576,104));DrawString(font,new(397,334),text,HorizontalAlignment.Center,486,36,Cream);}
    }
    void ReleaseUiArt()
    {
        foreach(var frame in uiArtFrames.Values)frame.Dispose();uiArtFrames.Clear();
        foreach(var crop in uiArtCrops.Values)crop.Dispose();uiArtCrops.Clear();
        foreach(var texture in uiArtTextures.Values)texture.Dispose();uiArtTextures.Clear();
    }
}
