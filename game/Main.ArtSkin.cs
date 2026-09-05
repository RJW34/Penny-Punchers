using Godot;

public partial class Main
{
    // Supplied AFTER HOURS artboards remain unchanged. These rectangular regions are
    // reusable decoration; all values, state labels and GUI callbacks remain live.
    readonly Dictionary<string,Texture2D> uiArtTextures=new();
    readonly Dictionary<string,AtlasTexture> uiArtCrops=new();
    readonly Dictionary<string,StyleBoxTexture> uiArtFrames=new();
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
        return fighterId=="rook"
            ?UiArt("rook-identity",faceOnly?new Rect2I(127,24,274,335):new Rect2I(13,12,485,484))
            :UiArt("vale-identity",faceOnly?new Rect2I(818,23,281,340):new Rect2I(760,19,344,474));
    }
    void UiPortraitPanel(string fighterId,Rect2 rect)
    {
        UiArtPanel(rect.Position.X,rect.Position.Y,rect.Size.X,rect.Size.Y);
        UiArtImage(UiPortrait(fighterId,true),new Rect2(rect.Position+new Vector2(7,7),rect.Size-new Vector2(14,14)));
    }
    Texture2D UiLeaseArt(string fighterId,int column,bool super=false)
    {
        int row=(fighterId=="rook"?0:2)+(super?1:0);
        int[] xs=[40,289,539,788,1038,1289],ys=[53,287,523,757];
        return UiArt("lease-icons",new Rect2I(xs[Math.Clamp(column,0,5)],ys[row],212,199));
    }
    void UiScreenIcon()
    {
        int column=screen switch{"settings"=>0,"help" or "moves"=>1,"replays" or "replay_controls"=>2,"labmenu" or "lab_state"=>3,"select" or "prep" or "results"=>4,_=>5};
        UiArtImage(UiArt("interface-atlas",new Rect2I(60+column*240,527,197,122)),new Rect2(1157,31,68,43),new Color(.8f,.84f,.83f));
    }
    void UiArtWordmark()=>UiArtImage(UiArt("branding-announcements",new Rect2I(70,19,690,358)),new Rect2(52,48,461,239));
    void UiRefreshPreparationSkin()
    {
        foreach(var button in ui.GetChildren().OfType<Button>().Where(b=>!b.IsQueuedForDeletion()))
        {
            if(button.Position.Y<240)continue;
            int seat=button.Position.X>640?1:0;
            int row=button.Position.Y>=640?4:button.Position.Y>=500?3:(int)Math.Round((button.Position.Y-247)/87);
            if(row is <0 or >4)continue;
            bool active=row==_preparationFocus[seat]&&!ready[seat];
            button.AddThemeStyleboxOverride("normal",UiArtFrame(active?(seat==0?"active-p1":"active-p2"):"normal"));
        }
    }
    void DrawArtHud()
    {
        if(sim==null)return;
        TextureFilter=CanvasItem.TextureFilterEnum.Nearest;
        DrawStyleBox(UiArtFrame("hud"),new Rect2(-12,-12,1304,171));
        for(int seat=0;seat<2;seat++)
        {
            var p=sim.Players[seat];bool left=seat==0;Color accent=left?Gold:Cyan;
            float portraitX=left?27:1181,barX=left?117:713,textX=left?42:736;
            DrawStyleBox(UiArtFrame(),new Rect2(portraitX,14,73,77));
            DrawTextureRect(UiPortrait(p.FighterId,true),new Rect2(portraitX+6,20,61,65),false);
            DrawString(font,new(barX,30),p.FighterId.ToUpperInvariant(),HorizontalAlignment.Left,265,24,Cream);
            DrawString(font,new(barX+271,29),$"P{seat+1} / {p.ScoreHalfPoints/2f:0.#} PTS",HorizontalAlignment.Right,177,16,accent);
            var frame=UiArt("hud-atlas",left?new Rect2I(129,176,512,54):new Rect2I(897,176,511,54));
            DrawTextureRect(frame,new Rect2(barX-4,39,454,40),false);
            float health=Math.Clamp(p.Health/1000f,0,1),length=422*health;
            if(length>0)
            {
                var fill=UiArt("hud-atlas",p.Health<200?new Rect2I(83,380,57,19):left?new Rect2I(172,48,400,21):new Rect2I(991,48,390,21));
                DrawTextureRect(fill,new Rect2(left?barX+11:barX+11+422-length,49,length,19),false);
            }
            DrawTextureRect(UiArt("hud-atlas",left?new Rect2I(29,528,83,86):new Rect2I(323,529,83,83)),new Rect2(textX,83,29,30),false);
            DrawString(font,new(textX+37,108),$"{p.Credits:N0} CR",HorizontalAlignment.Left,185,26,accent);
            DrawString(font,new(textX+217,106),$"EX 300  {(p.Credits-p.ReserveFloor>=300?"READY":"LOW FUNDS")}",HorizontalAlignment.Left,285,14,Muted);
            DrawString(font,new(textX,128),$"ART {art[seat]+1} / {900+art[seat]*300} CR   RESERVE {p.ReserveFloor}   STUN {p.Stun}",HorizontalAlignment.Left,502,13,Muted);
        }
        DrawStyleBox(UiArtFrame(),new Rect2(587,14,106,106));
        DrawString(font,new(594,77),$"{Math.Max(0,sim.TimerTicks+59)/60:00}",HorizontalAlignment.Center,92,47,Cream);
        DrawString(font,new(588,138),$"ROUND {sim.RoundId}",HorizontalAlignment.Center,104,13,Gold);
        DrawLeaseSummary();
    }
    void DrawArtAnnouncement(string text)
    {
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
