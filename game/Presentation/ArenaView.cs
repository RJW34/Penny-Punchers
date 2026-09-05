using Godot;

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
    private readonly FighterSprites[] _sprites = [new(),new()];
    private ArenaOverlay? _overlay;
    private float _time, _camera = 384000, _shake, _shakeTime;
    private const float Ground = 604f, Units = 1280f / 480000f;
    private readonly Color _ink = C("101a21"), _cream = C("ece7d4"), _amber = C("efa94c"), _cyan = C("62d7d3");
    private Font Font => ThemeDB.FallbackFont;

    public ArenaRenderState State => _state;
    public string[] RenderedCels=>_sprites.Where(s=>s.Visible).Select(s=>s.CurrentCel).ToArray();
    public override void _Ready()
    {
        ZIndex = -1;
        TextureFilter=TextureFilterEnum.Nearest;
        _foundry=GD.Load<Texture2D>("res://Assets/AfterHours/Stages/foundry.png");
        _grid=GD.Load<Texture2D>("res://Assets/AfterHours/Stages/grid.png");
        _effects=GD.Load<Texture2D>("res://Assets/AfterHours/Effects/combat-atlas.png");
        foreach(var sprite in _sprites)AddChild(sprite);
        _overlay=new ArenaOverlay{Arena=this};AddChild(_overlay);
        _audio = new ArenaAudio();
        AddChild(_audio);
        ProcessMode = ProcessModeEnum.Always;
    }

    public void SetState(ArenaRenderState state)
    {
        _state=state;
        for(int i=0;i<_sprites.Length;i++)
        {
            _sprites[i].Visible=i<state.Fighters.Length;
            if(i<state.Fighters.Length)_sprites[i].SetFighter(state.Fighters[i],state.Tick);
        }
        QueueRedraw();
    }
    public override void _ExitTree()
    {
        _foundry?.Dispose();_grid?.Dispose();_effects?.Dispose();
        _foundry=_grid=_effects=null;
        FighterSprites.ReleaseAtlases();
    }
    public void SetVolumes(float master, float music, float sfx) => _audio?.SetVolumes(master, music, sfx);
    public void PlayCue(string cue) => _audio?.PlayCue(cue);
    public void ShutdownAudio() => _audio?.ShutdownAudio();
    public void ResetEffects() { _impacts.Clear(); _walletCues.Clear(); _shakeTime=0; foreach(var sprite in _sprites)sprite.ResetTimeline(); }
    public void ShowWalletCue(int seat, string text, bool rejected=false)
    {
        _walletCues.RemoveAll(c=>c.Seat==seat);
        _walletCues.Add(new WalletCue(seat,text,rejected));
    }

    public void TriggerEffect(string kind, int xMilli, int yMilli = 50000, int strength = 1)
    {
        kind = kind.ToLowerInvariant();
        if (_impacts.Count > 64) _impacts.RemoveAt(0);
        _impacts.Add(new Impact(kind, xMilli, yMilli, Math.Clamp(strength, 1, 3), _cosmetic.NextSingle() * 6.28f));
        if (kind.Contains("hit") || kind.Contains("super") || kind.Contains("throw"))
        {
            _shake = MathF.Max(_shake, 2f + strength * 1.4f);
            _shakeTime = .15f;
        }
        _audio?.PlayCue(kind);
    }

    public override void _Process(double delta)
    {
        var dt = (float)Math.Min(delta, .1);
        _time += dt;
        _shakeTime = Math.Max(0, _shakeTime - dt);
        for (int i = _impacts.Count - 1; i >= 0; i--)
        {
            _impacts[i].Age += dt;
            if (_impacts[i].Age > _impacts[i].Lifetime) _impacts.RemoveAt(i);
        }
        for(int i=_walletCues.Count-1;i>=0;i--){_walletCues[i].Age+=dt;if(_walletCues[i].Age>1.6f)_walletCues.RemoveAt(i);}
        if (_state.Fighters.Length > 0)
        {
            float midpoint = (float)_state.Fighters.Average(f => f.X);
            _camera = Math.Clamp(midpoint, 240000, 528000);
        }
        var shake = _shakeTime > 0 ? new Vector2(MathF.Sin(_time * 132), MathF.Cos(_time * 171)) * _shake * _state.ShakeScale * (_shakeTime / .15f) : Vector2.Zero;
        for(int i=0;i<_state.Fighters.Length&&i<_sprites.Length;i++)
        {
            var f=_state.Fighters[i];
            _sprites[i].Position=(WorldToScreen(f.X,f.Y)+shake).Round();
            _sprites[i].Scale=new Vector2(f.Facing<0?-1:1,1);
        }
        QueueRedraw();_overlay?.QueueRedraw();
    }

    public Vector2 WorldToScreen(int x, int y) => new(640 + (x - _camera) * Units, Ground - y * Units);

    public override void _Draw()
    {
        using var drawProfile=RuntimeProfiler.Measure("ArenaDraw");
        DrawSetTransform(Vector2.Zero);
        var backdrop=_state.TrainingGrid?_grid:_foundry;
        if(backdrop!=null)
        {
            float scale=1344f/backdrop.GetWidth();
            float height=backdrop.GetHeight()*scale;
            float x=-32-(_camera-384000)*.00012f;
            DrawTextureRect(backdrop,new Rect2(MathF.Round(x),MathF.Round(Ground-height*.845f),1344,height),false);
        }
        DrawCorner(0,true);DrawCorner(768000,false);
        if(_state.TrainingGrid&&_state.DebugBoxes)DrawGrid();
        foreach(var f in _state.Fighters)
        {
            var ground=WorldToScreen(f.X,0);
            float falloff=Math.Clamp(1-f.Y/250000f,.32f,1);
            DrawEffectCel(this,35,ground+new Vector2(0,5),new Vector2(155,30)*falloff,new Color(.13f,.15f,.15f,.63f));
        }
        // Receipts are live data and sit above the arena, below its HUD.
        foreach(var cue in _walletCues)
        {
            float alpha=Math.Min(1,(1.6f-cue.Age)*3);
            float x=cue.Seat==0?42:936;
            DrawRect(new Rect2(x-7,164,310,28),new Color(.035f,.065f,.078f,.9f*alpha));
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

    private sealed class Impact(string kind,int x,int y,int strength,float seed)
    {
        public string Kind=kind;
        public int X=x,Y=y,Strength=strength;
        public float Seed=seed,Age;
        public float Lifetime=>Kind.Contains("parry")?.42f:Kind.Contains("super")?.62f:.32f;
    }
    private sealed class WalletCue(int seat,string text,bool rejected)
    {
        public int Seat=seat; public string Text=text; public bool Rejected=rejected; public float Age;
    }

    internal void DrawOverlay(Node2D canvas)
    {
        foreach(var projectile in _state.Projectiles)
        {
            int cel=(projectile.FighterId=="vale"?21:18)+Math.Min(2,projectile.Age/4%3);
            if(projectile.MoveId.EndsWith("_ex"))cel=24+projectile.Age/4%3;
            var pos=WorldToScreen(projectile.X,projectile.Y);
            float size=Math.Clamp(projectile.Width*Units*1.45f,75,135);
            canvas.DrawSetTransform(pos.Round(),0,new Vector2(projectile.Facing<0?-1:1,1));
            DrawEffectCel(canvas,cel,Vector2.Zero,new Vector2(size,size),Colors.White);
            canvas.DrawSetTransform(Vector2.Zero);
            if(_state.DebugBoxes)canvas.DrawRect(new Rect2(pos-new Vector2(projectile.Width,projectile.Height)*Units*.5f,new Vector2(projectile.Width,projectile.Height)*Units),new Color(_cyan,.85f),false,1.2f);
        }
        foreach(var impact in _impacts)
        {
            float progress=impact.Age/impact.Lifetime;
            bool parry=impact.Kind.Contains("parry"),block=impact.Kind.Contains("block")||impact.Kind.Contains("guard"),ground=impact.Kind.Contains("land")||impact.Kind.Contains("dash");
            int cel=ground?(impact.Kind.Contains("dash")?31:32):parry?15+Math.Min(2,(int)(progress*3)):block?12+Math.Min(2,(int)(progress*3)):(impact.Strength>=3?6:0)+Math.Min(5,(int)(progress*6));
            float size=ground?110:parry?125:block?100:impact.Strength>=3?160:115;
            var pos=WorldToScreen(impact.X,impact.Y)+(ground?new Vector2(0,-25):Vector2.Zero);
            float alpha=_state.ReducedFlashes?.68f:1;
            DrawEffectCel(canvas,cel,pos,new Vector2(size,size),new Color(1,1,1,alpha));
        }
        foreach(var f in _state.Fighters)
        {
            if(f.State=="dizzy")DrawEffectCel(canvas,34,WorldToScreen(f.X,f.Y+100000),new Vector2(75,60),Colors.White);
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
