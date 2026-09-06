using Godot;
using System.Text.Json;

namespace StrikeLedger.Presentation;

/// <summary>
/// After Hours raster arena and explicit fighter cels. All animation, particles,
/// camera motion and audio are cosmetic: SetState is the sole simulation input.
/// </summary>
public partial class ArenaView : Node2D
{
    private ArenaRenderState _state = new();
    private readonly List<Impact> _impacts = [];
    private readonly List<WalletCue> _walletCues = [];
    private readonly Random _cosmetic = new(7119);
    private ArenaAudio? _audio;
    private Texture2D? _foundry, _grid, _effects;
    private readonly Dictionary<string,Texture2D> _panoramas=new();
    private readonly Dictionary<string,ImpactProfile> _impactProfiles=new();
    private sealed record ImpactProfile(int Size,float Shake,string Sound,int Cel,int Frames,int Lifetime);
    private readonly FighterSprites[] _sprites = [new(),new()];
    private ArenaOverlay? _overlay;
    private float _time, _camera = 384000, _shake, _shakeTime;
    private int _shakeClock;
    private CameraFrame _framing=new(384000,1);
    private const float Ground = CameraFrame.Floor;
    private float Units=>_framing.Units;
    private readonly Color _ink = C("101a21"), _cream = C("ece7d4"), _amber = C("efa94c"), _cyan = C("62d7d3");
    private Font Font => UiTypography.Body;

    public ArenaRenderState State => _state;
    public string[] RenderedCels=>_sprites.Where(s=>s.Visible).Select(s=>s.CurrentCel).ToArray();
    public CameraFrame Framing=>_framing;
    public int ImpactCount=>_impacts.Count;
    public Rect2[] FighterScreenBounds=>_state.Fighters.Take(_sprites.Length).Select((f,i)=>
    {
        var b=_sprites[i].LocalBounds;float left=f.Facing<0?-b.End.X:b.Position.X;
        return new Rect2(WorldToScreen(f.X,f.Y)+new Vector2(left,b.Position.Y)*_framing.Zoom,b.Size*_framing.Zoom);
    }).ToArray();
    public override void _Ready()
    {
        ZIndex = -1;
        TextureFilter=TextureFilterEnum.Nearest;
        _foundry=GD.Load<Texture2D>("res://Assets/AfterHours/Stages/foundry.png");
        _grid=GD.Load<Texture2D>("res://Assets/AfterHours/Stages/grid.png");
        foreach(string id in new[]{"marist_green","marist_gates"})_panoramas[id]=GD.Load<Texture2D>($"res://Assets/AfterHours/Stages/{id}-panorama.png");
        _effects=GD.Load<Texture2D>("res://Assets/AfterHours/Effects/combat-atlas.png");
        using(var profiles=JsonDocument.Parse(Godot.FileAccess.GetFileAsString("res://Presentation/impact-profiles.json")))
            foreach(var fighter in profiles.RootElement.GetProperty("fighters").EnumerateObject())foreach(var move in fighter.Value.EnumerateObject())
            {
                var p=move.Value;_impactProfiles[fighter.Name+":"+move.Name]=new(p.GetProperty("size").GetInt32(),p.GetProperty("shake").GetSingle(),p.GetProperty("sound").GetString()!,p.GetProperty("contact_cel").GetInt32(),p.GetProperty("frames").GetInt32(),p.GetProperty("lifetime_ticks").GetInt32());
            }
        foreach(var sprite in _sprites)AddChild(sprite);
        _overlay=new ArenaOverlay{Arena=this};AddChild(_overlay);
        _audio = new ArenaAudio();
        AddChild(_audio);
        ProcessMode = ProcessModeEnum.Always;
    }

    public void SetState(ArenaRenderState state)
    {
        int advance=Math.Clamp(state.Tick-_state.Tick,0,120);
        if(state.Tick<_state.Tick)ResetEffects();
        foreach(var impact in _impacts)
            if(!state.Frozen&&(impact.FreezeSeat<0||impact.FreezeSeat>=state.Fighters.Length||!state.Fighters[impact.FreezeSeat].Frozen))impact.AgeTicks+=advance;
        _impacts.RemoveAll(i=>i.AgeTicks>=i.LifetimeTicks);
        if(!state.Frozen)foreach(var cue in _walletCues)cue.AgeTicks+=advance;
        if(!state.Frozen&&!state.Fighters.Any(f=>f.Frozen)){_shakeTime=Math.Max(0,_shakeTime-advance/60f);_shakeClock+=advance;}
        _walletCues.RemoveAll(c=>c.AgeTicks>=96);
        _state=state;
        for(int i=0;i<_sprites.Length;i++)
        {
            _sprites[i].Visible=i<state.Fighters.Length;
            if(i<state.Fighters.Length)_sprites[i].SetFighter(state.Fighters[i],state.Tick);
        }
        var subjects=new List<CameraSubject>();
        for(int i=0;i<state.Fighters.Length&&i<_sprites.Length;i++)
        {
            var f=state.Fighters[i];var b=_sprites[i].LocalBounds;
            subjects.Add(new(f.X,f.Y,f.Facing<0?-b.End.X:b.Position.X,f.Facing<0?-b.Position.X:b.End.X,-b.Position.Y,b.End.Y));
        }
        _framing=CameraFrame.Fit(subjects);_camera=_framing.WorldX;
        _audio?.SetContext(state.StageId,state.Phase,state.Frozen);
        QueueRedraw();
    }
    public override void _ExitTree()
    {
        _foundry?.Dispose();_grid?.Dispose();_effects?.Dispose();
        _foundry=_grid=_effects=null;
        foreach(var texture in _panoramas.Values)texture.Dispose();_panoramas.Clear();
        FighterSprites.ReleaseAtlases();
    }
    public void SetVolumes(float master, float music, float sfx) => _audio?.SetVolumes(master, music, sfx);
    public void PlayCue(string cue,string fighterId="",string key="") => _audio?.PlayCue(cue,fighterId,key);
    public void ShutdownAudio() => _audio?.ShutdownAudio();
    public void ResetEffects() { _impacts.Clear(); _walletCues.Clear(); _shakeTime=0; foreach(var sprite in _sprites)sprite.ResetTimeline(); }
    public void ShowWalletCue(int seat, string text, bool rejected=false,string key="")
    {
        _walletCues.RemoveAll(c=>c.Seat==seat);
        _walletCues.Add(new WalletCue(seat,text,rejected,key));
    }
    public void CancelCue(string key){_impacts.RemoveAll(i=>i.Key==key);_walletCues.RemoveAll(c=>c.Key==key);_audio?.CancelCue(key);}

    public void TriggerEffect(string kind, int xMilli, int yMilli = 50000, int strength = 1,int ageTicks=0,string key="",int freezeSeat=-1,string fighterId="",string moveId="")
    {
        kind = kind.ToLowerInvariant();
        if (_impacts.Count > 64) _impacts.RemoveAt(0);
        var impact=new Impact(kind,xMilli,yMilli,Math.Clamp(strength,1,3),_cosmetic.NextSingle()*6.28f,key,freezeSeat){AgeTicks=Math.Max(0,ageTicks)};
        if(kind.Contains("hit")&&_impactProfiles.TryGetValue(fighterId+":"+moveId,out var profile))impact.Profile=profile;
        if(impact.AgeTicks>=impact.LifetimeTicks)return;
        _impacts.Add(impact);
        if (kind.Contains("hit") || kind.Contains("super") || kind.Contains("throw"))
        {
            _shake = MathF.Max(_shake, impact.Profile?.Shake??(2f + strength * 1.4f));
            _shakeTime = .15f;
        }
        _audio?.PlayCue(impact.Profile?.Sound??kind,fighterId,key);
    }

    public override void _Process(double delta)
    {
        var dt = (float)Math.Min(delta, .1);
        _time += dt;
        // Only quiet environmental ambience uses wall time. Combat cues and shake
        // lifetime advance with SetState ticks, so pause and frame advance inspect them.
        var shake = _shakeTime > 0 ? new Vector2(MathF.Sin(_shakeClock * 2.2f), MathF.Cos(_shakeClock * 2.85f)) * _shake * _state.ShakeScale * (_shakeTime / .15f) : Vector2.Zero;
        for(int i=0;i<_state.Fighters.Length&&i<_sprites.Length;i++)
        {
            var f=_state.Fighters[i];
            _sprites[i].Position=(WorldToScreen(f.X,f.Y)+shake).Round();
            _sprites[i].Scale=new Vector2(f.Facing<0?-_framing.Zoom:_framing.Zoom,_framing.Zoom);
        }
        QueueRedraw();_overlay?.QueueRedraw();
    }

    public Vector2 WorldToScreen(int x, int y) => new(_framing.ScreenX(x),_framing.ScreenY(y));

    public override void _Draw()
    {
        using var drawProfile=RuntimeProfiler.Measure("ArenaDraw");
        DrawSetTransform(Vector2.Zero);
        string stage=_state.TrainingGrid?"grid":_state.StageId;
        if(_panoramas.TryGetValue(stage,out var panorama))
        {
            // Cover the playable aperture (HUD bottom 154 to footer top 672)
            // independently of fighter zoom. A .75 floor keeps the source sky at
            // y151 and its ground at y691, so no stretched edge row is exposed.
            // Preserve the panorama's aspect; collision and actors still use the
            // shared world projection and never depend on this distant artwork.
            float scale=Math.Max(.75f,_framing.Zoom),width=2048*scale,height=720*scale;
            float x=640-_camera/768000f*width;
            x=Math.Clamp(x,1280-width,0);
            float y=Ground-604*scale;
            DrawRect(new Rect2(0,0,1280,720),stage=="marist_green"?C("233a30"):C("102435"));
            DrawTextureRect(panorama,new Rect2(x,y,width,height),false);
            // Extend only the quiet sky/ground outside the source viewport, never
            // the recognizable architecture. Normal 1x framing samples it intact.
            if(y>0)DrawTextureRectRegion(panorama,new Rect2(x,0,width,y+1),new Rect2(0,0,panorama.GetWidth(),1));
            if(y+height<720)DrawTextureRectRegion(panorama,new Rect2(x,y+height-1,width,721-y-height),new Rect2(0,panorama.GetHeight()-1,panorama.GetWidth(),1));
        }
        else if((stage=="grid"?_grid:_foundry) is {} backdrop)
        {
            float scale=1344f/backdrop.GetWidth();
            float height=backdrop.GetHeight()*scale;
            float x=-32-(_camera-384000)*.00012f;
            DrawTextureRect(backdrop,new Rect2(MathF.Round(x),MathF.Round(Ground-height*.845f),1344,height),false);
        }
        DrawAmbience(stage);
        DrawCorner(0,true);DrawCorner(768000,false);
        if(_state.TrainingGrid&&_state.DebugBoxes)DrawGrid();
        foreach(var f in _state.Fighters)
        {
            var ground=WorldToScreen(f.X,0);
            float falloff=Math.Clamp(1-f.Y/250000f,.32f,1);
            DrawEffectCel(this,35,ground+new Vector2(0,5),new Vector2(155,30)*falloff*_framing.Zoom,new Color(.13f,.15f,.15f,.63f));
        }
        // Receipts are live data and sit above the arena, below its HUD.
        foreach(var cue in _walletCues)
        {
            float alpha=Math.Min(1,(1.6f-cue.Age)*3);
            float x=cue.Seat==0?42:736;
            DrawRect(new Rect2(x-7,164,509,28),new Color(.035f,.065f,.078f,.9f*alpha));
            DrawLine(new(x-7,164),new(x-7,192),new Color(cue.Rejected?_cream:cue.Seat==0?_amber:_cyan,alpha),2);
            DrawString(Font,new(x+3,184),cue.Text,HorizontalAlignment.Left,-1,13,new Color(cue.Rejected?_cream:cue.Seat==0?_amber:_cyan,alpha));
        }
        // Floor-edge markers carry the fighters' location even on monochrome displays.
        for (int i = 0; i < _state.Fighters.Length; i++)
        {
            var s = WorldToScreen(_state.Fighters[i].X, 0);
            var col = i == 0 ? _amber : _cyan;
            Poly([new(s.X-5, Ground+15), new(s.X+5,Ground+15), new(s.X,Ground+20)], col);
        }
    }

    private void DrawCorner(int worldX, bool left)
    {
        var x=WorldToScreen(worldX,0).X;
        if(x < -40 || x>1320) return;
        DrawRect(new Rect2(x-10,426,20,181),C("111c24"));
        DrawRect(new Rect2(x-7,426,14,10),_amber);
        DrawRect(new Rect2(x-6,440,12,112),C("8c7950"));
        for(int i=0;i<8;i++) Poly([new(x-6,442+i*14),new(x+6,449+i*14),new(x+6,456+i*14),new(x-6,449+i*14)],C("29383a"));
        DrawRect(new Rect2(x-6,559,12,40),C("68786f"));
        DrawString(Font,new(x+(left?16:-52),582),left?"01":"02",HorizontalAlignment.Left,-1,16,_cream);
    }

    private void DrawGrid()
    {
        DrawRect(new Rect2(0,142,1280,462),new Color(.022f,.056f,.073f,.93f));
        for(int u=0;u<=768;u+=20)
        {
            float x=WorldToScreen(u*1000,0).X;
            bool major=u%100==0;
            DrawLine(new(x,142),new(x,604),major?C("345059"):C("19323b"),major?1.5f:1);
            if(major) DrawString(Font,new(x+4,594),u.ToString(),HorizontalAlignment.Left,-1,10,C("7a9290"));
        }
        for(int u=0;u<=180;u+=20)
        {
            float y=Ground-u*1000*Units;
            if(y<142)continue;
            DrawLine(new(0,y),new(1280,y),u%100==0?C("345059"):C("19323b"),1);
            DrawString(Font,new(15,y-5),u.ToString("000"),HorizontalAlignment.Left,-1,10,C("7a9290"));
        }
        DrawLine(new(0,Ground),new(1280,Ground),_cream,2);
        DrawString(Font,new(42,176),"MEASUREMENT FLOOR",HorizontalAlignment.Left,-1,14,_cream);
        DrawString(Font,new(42,196),"20 UNIT GRID   /   IDENTICAL ARENA GEOMETRY",HorizontalAlignment.Left,-1,10,C("6e918f"));
    }

    private void DrawBoxes(Node2D canvas,FighterRenderState f)
    {
        foreach(var b in f.Boxes)
        {
            var point=WorldToScreen(f.X+b.X,f.Y+b.Y+b.Height);
            var rect=new Rect2(point,new Vector2(b.Width,b.Height)*Units);
            var col=b.Kind=="hit"?C("ef7261"):b.Kind=="push"?C("dcce77"):_cyan;
            canvas.DrawRect(rect,new Color(col,.08f));canvas.DrawRect(rect,new Color(col,.8f),false,1.2f);
        }
    }

    private sealed class Impact(string kind,int x,int y,int strength,float seed,string key,int freezeSeat)
    {
        public string Kind=kind;
        public int X=x,Y=y,Strength=strength;
        public float Seed=seed;
        public string Key=key;public int FreezeSeat=freezeSeat,AgeTicks;
        public ImpactProfile? Profile;
        public int LifetimeTicks=>Profile?.Lifetime??(Kind.Contains("parry")?25:Kind.Contains("super")?37:19);
        public float Age=>AgeTicks/60f;
        public float Lifetime=>LifetimeTicks/60f;
    }

    private void DrawAmbience(string stage)
    {
        if(stage=="grid"||_state.ReducedFlashes)return;
        // Original code-drawn background motes, not invented image layers. They
        // stay dim and above the contact plane; collision and foot visibility do
        // not depend on the backdrop or weather.
        for(int i=0;i<12;i++)
        {
            float x=(i*113+_time*(stage=="marist_gates"?15:4)+83)%1280;
            float y=192+(i*47)%195+MathF.Sin(_time*.4f+i)*5;
            if(stage=="marist_gates")DrawLine(new(x,y),new(x-2,y+5),new Color(.45f,.61f,.70f,.12f));
            else DrawRect(new Rect2(MathF.Round(x),MathF.Round(y),2,2),stage=="marist_green"?new Color(.92f,.85f,.57f,.12f):new Color(.9f,.55f,.19f,.18f));
        }
    }
    private sealed class WalletCue(int seat,string text,bool rejected,string key)
    {
        public int Seat=seat,AgeTicks;public string Key=key; public string Text=text; public bool Rejected=rejected; public float Age=>AgeTicks/60f;
    }

    internal void DrawOverlay(Node2D canvas)
    {
        using var objectProfile=RuntimeProfiler.Measure("ArenaOverlay");
        DrawFields(canvas);
        foreach(var projectile in _state.Projectiles)
        {
            int cel=(projectile.FighterId=="vale"?21:18)+Math.Min(2,projectile.Age/4%3);
            if(projectile.MoveId.EndsWith("_ex"))cel=24+projectile.Age/4%3;
            var pos=WorldToScreen(projectile.X,projectile.Y);
            float size=Math.Clamp(projectile.Width*CameraFrame.BaseUnits*1.45f,75,135)*_framing.Zoom;
            if(projectile.MoveId is "buy_v_s1_return_pulse" or "buy_v_s3_arc_pulse")
            {
                var travel=new Vector2(projectile.VelocityX,-projectile.VelocityY).Normalized();
                var owner=projectile.Owner==0?_amber:_cyan;
                for(int streak=0;streak<3;streak++)
                    canvas.DrawLine(pos-travel*(size*.28f+streak*13*_framing.Zoom),pos-travel*(size*.28f+(streak+1)*13*_framing.Zoom),new Color(owner,.55f-streak*.15f),Math.Max(1,3*_framing.Zoom));
            }
            float angle=projectile.VelocityY==0?0:MathF.Atan2(-projectile.VelocityY,Math.Abs(projectile.VelocityX));
            if(projectile.Facing<0)angle=-angle;
            canvas.DrawSetTransform(pos.Round(),angle,new Vector2(projectile.Facing<0?-1:1,1));
            DrawEffectCel(canvas,cel,Vector2.Zero,new Vector2(size,size),Colors.White);
            canvas.DrawSetTransform(Vector2.Zero);
            if(projectile.ReflectionDepth>0)
            {
                Color owner=projectile.Owner==0?_amber:_cyan;
                canvas.DrawArc(pos,size*.3f,0,Mathf.Tau,16,new Color(owner,.85f),2);
                ObjectLabel(canvas,pos+new Vector2(-44,-size*.4f),$"P{projectile.Owner+1} / REFLECT",owner,88);
            }
            if(_state.DebugBoxes)canvas.DrawRect(new Rect2(pos-new Vector2(projectile.Width,projectile.Height)*Units*.5f,new Vector2(projectile.Width,projectile.Height)*Units),new Color(_cyan,.85f),false,1.2f);
        }
        foreach(var impact in _impacts)
        {
            float progress=impact.Age/impact.Lifetime;
            bool parry=impact.Kind.Contains("parry"),block=impact.Kind.Contains("block")||impact.Kind.Contains("guard"),ground=impact.Kind.Contains("land")||impact.Kind.Contains("dash");
            bool super=impact.Kind.Contains("super"),ex=impact.Kind.Contains("ex"),tech=impact.Kind.Contains("tech");
            int cel=tech?33:super?27+Math.Min(2,(int)(progress*3)):ex?24+Math.Min(2,(int)(progress*3)):ground?(impact.Kind.Contains("dash")?31:32):parry?15+Math.Min(2,(int)(progress*3)):block||impact.Kind is "armor" or "reflect" or "countercatch"?12+Math.Min(2,(int)(progress*3)):(impact.Strength>=3?6:0)+Math.Min(5,(int)(progress*6));
            float size=ground?110:parry?125:block?100:impact.Strength>=3?160:115;
            if(impact.Profile is {} profile){cel=profile.Cel+Math.Min(profile.Frames-1,(int)(progress*profile.Frames));size=profile.Size;}
            var pos=WorldToScreen(impact.X,impact.Y)+(ground?new Vector2(0,-25):Vector2.Zero);
            float alpha=_state.ReducedFlashes?.68f:1;
            if(impact.Kind!="dissipate")DrawEffectCel(canvas,cel,pos,new Vector2(size,size)*_framing.Zoom,new Color(1,1,1,alpha));
            if(impact.Kind is "reflect" or "dissipate" or "armor")
            {
                var color=impact.Kind=="reflect"?_cyan:impact.Kind=="armor"?_amber:_cream;
                float r=(20+progress*40)*_framing.Zoom;
                canvas.DrawArc(pos,r,0,Mathf.Tau,18,new Color(color,1-progress),2);
                if(impact.Kind=="dissipate")
                    for(int i=0;i<4;i++){float a=i*Mathf.Pi/2+.3f;var axis=Vector2.FromAngle(a);canvas.DrawLine(pos+axis*r,pos+axis*(r+12*_framing.Zoom),new Color(color,1-progress),2);}
            }
        }
        for(int seat=0;seat<_state.Fighters.Length;seat++)
        {
            var f=_state.Fighters[seat];DrawActorStatus(canvas,f,seat);
            if(f.State=="dizzy")DrawEffectCel(canvas,34,WorldToScreen(f.X,f.Y+100000),new Vector2(75,60)*_framing.Zoom,Colors.White);
            if(_state.DebugBoxes)DrawBoxes(canvas,f);
        }
    }
    private void DrawEffectCel(Node2D canvas,int index,Vector2 center,Vector2 size,Color tint)
    {
        if(_effects==null)return;
        float w=_effects.GetWidth()/6f,h=_effects.GetHeight()/6f;
        var source=new Rect2((index%6)*w+2,(index/6)*h+2,w-4,h-4);
        canvas.DrawTextureRectRegion(_effects,new Rect2((center-size/2).Round(),size.Round()),source,tint);
    }
    private void Poly(Vector2[] points,Color color)=>DrawColoredPolygon(points,color);
    private static Color C(string html)=>Color.FromHtml(html);
}
