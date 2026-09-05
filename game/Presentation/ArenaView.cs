using Godot;

namespace StrikeLedger.Presentation;

/// <summary>
/// Original retained-state vector arena and articulated fighters. All animation, particles,
/// camera motion and audio are cosmetic: SetState is the sole simulation input.
/// </summary>
public partial class ArenaView : Node2D
{
    private ArenaRenderState _state = new();
    private readonly List<Impact> _impacts = [];
    private readonly List<WalletCue> _walletCues = [];
    private readonly Random _cosmetic = new(7119);
    private ArenaAudio? _audio;
    private SubViewport? _backdropViewport;
    private ViewportTexture? _backdropTexture;
    private float _time, _camera = 384000, _shake, _shakeTime;
    private const float Ground = 604f, Units = 1280f / 480000f;
    private readonly Color _ink = C("101a21"), _cream = C("ece7d4"), _amber = C("efa94c"), _cyan = C("62d7d3");
    private Font Font => ThemeDB.FallbackFont;

    public ArenaRenderState State => _state;
    public override void _Ready()
    {
        ZIndex = -1;
        _backdropViewport=new SubViewport
        {
            Size=new Vector2I(1280,720), Disable3D=true,
            TransparentBg=false, RenderTargetUpdateMode=SubViewport.UpdateMode.Once
        };
        AddChild(_backdropViewport);
        _backdropViewport.AddChild(new FoundryBackdrop());
        _backdropTexture=_backdropViewport.GetTexture();
        _audio = new ArenaAudio();
        AddChild(_audio);
        ProcessMode = ProcessModeEnum.Always;
    }

    public override void _ExitTree()
    {
        _backdropTexture?.Dispose();
        _backdropTexture=null;
        _backdropViewport=null;
    }

    public void SetState(ArenaRenderState state) { _state = state; QueueRedraw(); }
    public void SetVolumes(float master, float music, float sfx) => _audio?.SetVolumes(master, music, sfx);
    public void PlayCue(string cue) => _audio?.PlayCue(cue);
    public void ShutdownAudio() => _audio?.ShutdownAudio();
    public void ResetEffects() { _impacts.Clear(); _walletCues.Clear(); _shakeTime=0; }
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
        QueueRedraw();
    }

    public Vector2 WorldToScreen(int x, int y) => new(640 + (x - _camera) * Units, Ground - y * Units);

    public override void _Draw()
    {
        using var drawProfile=RuntimeProfiler.Measure("ArenaDraw");
        DrawSetTransform(Vector2.Zero);
        if(_backdropTexture!=null)DrawTextureRect(_backdropTexture,new Rect2(0,0,1280,720),false);
        // Wall markers retain their live camera alignment over the cached decoration.
        DrawCorner(0,true);DrawCorner(768000,false);
        if (_state.TrainingGrid) DrawGrid();
        var shake = _shakeTime > 0 ? new Vector2(MathF.Sin(_time * 132), MathF.Cos(_time * 171)) * _shake * _state.ShakeScale * (_shakeTime / .15f) : Vector2.Zero;
        foreach (var f in _state.Fighters)
        {
            var ground = WorldToScreen(f.X, 0);
            var falloff = Math.Clamp(1 - f.Y / 250000f, .32f, 1);
            Ellipse(ground + new Vector2(0, 3), 66 * falloff, 13 * falloff, new Color(.025f, .04f, .045f, .55f));
            Ellipse(ground + new Vector2(0, 3), 42 * falloff, 7 * falloff, new Color(.015f, .027f, .032f, .35f));
        }
        foreach (var f in _state.Fighters)
        {
            var pos = WorldToScreen(f.X, f.Y) + shake;
            DrawSetTransform(pos, 0, new Vector2((f.Facing < 0 ? -1 : 1) * Units * 1000, Units * 1000));
            DrawFighter(f);
            DrawSetTransform(Vector2.Zero);
            if (_state.DebugBoxes) DrawBoxes(f);
        }
        foreach (var projectile in _state.Projectiles) DrawProjectile(projectile);
        foreach (var impact in _impacts) DrawImpact(impact);
        DrawSetTransform(Vector2.Zero);
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

    private void DrawFighter(FighterRenderState f)
    {
        bool vale=f.Id.Equals("vale",StringComparison.OrdinalIgnoreCase);
        var p=PoseFor(f,vale);
        bool alt=f.Palette%2!=0;
        Color cloth=vale?(alt?C("b46752"):C("31888a")):(alt?C("64708e"):C("466879"));
        Color bright=vale?(alt?C("e9b99b"):C("8ae0d0")):(alt?C("d2bbd8"):C("a8c3c3"));
        Color accent=vale?(alt?C("b8d5c9"):C("dfbb77")):(alt?C("91ccae"):C("e9a44d"));
        Color skin=vale?C("b98c74"):C("c9966b");
        Color skinLight=vale?C("debaa0"):C("e6b98b");
        Color pants=vale?C("182e38"):C("263945");
        Color shade=vale?C("17474f"):C("263e4e");
        var state=f.State.ToLowerInvariant();
        float action=ActionEnvelope(f);
        bool paid=f.MoveId.Contains("_ex") || f.MoveId.StartsWith("super");
        if(paid && !_state.ReducedFlashes)
        {
            var glow=f.MoveId.StartsWith("super")?_cyan:_amber;
            DrawArc(p.Hip+new Vector2(0,-13),46+MathF.Sin(_time*20)*2,-2.7f,1.2f,28,new Color(glow,.25f),1.2f,true);
            DrawArc(p.Hip+new Vector2(0,-13),50,-.2f,2.8f,28,new Color(glow,.12f),1,true);
        }
        // Back arm/leg are shaded; overlapping fully filled shapes retain a human silhouette.
        Limb(p.Hip+new Vector2(-5,0),p.BackKnee,p.BackFoot,vale?7:9,vale?5:6.2f,pants.Darkened(.2f),pants);
        Boot(p.BackFoot,p.BackKnee,vale,true,accent.Darkened(.35f));
        if(vale)
        {
            var tail=p.Hip+new Vector2(-16,29+MathF.Sin(_time*5)*2);
            Shape([p.Chest+new Vector2(-10,8),p.Hip+new Vector2(8,2),tail+new Vector2(5,0),tail+new Vector2(-12,-5)],shade);
            DrawLine(p.Hip+new Vector2(-10,1),tail,accent.Darkened(.4f),1.2f,true);
        }
        Arm(p.Chest+new Vector2(-9,-2),p.BackElbow,p.BackHand,vale,skin.Darkened(.18f),cloth.Darkened(.2f),accent.Darkened(.2f),true);
        // Forward trousers: wide brawler cargos versus narrow spacing-fighter leggings.
        Limb(p.Hip+new Vector2(5,0),p.FrontKnee,p.FrontFoot,vale?6.5f:9.3f,vale?4.3f:6.7f,pants,vale?C("34505a"):C("536269"));
        if(!vale)
        {
            var thigh=p.Hip.Lerp(p.FrontKnee,.65f);
            Shape([thigh+new Vector2(-5,-5),thigh+new Vector2(4,-4),thigh+new Vector2(4,3),thigh+new Vector2(-5,4)],C("3b535e"),.7f);
            DrawLine(thigh+new Vector2(-4,-3),thigh+new Vector2(3,-3),C("86968e"),.65f);
        }
        else
        {
            DrawLine(p.FrontKnee.Lerp(p.FrontFoot,.1f),p.FrontKnee.Lerp(p.FrontFoot,.77f),bright.Darkened(.18f),2.3f,true);
        }
        Boot(p.FrontFoot,p.FrontKnee,vale,false,accent);
        // Shaped jacket / sleeveless tunic with original trim and patches.
        var shoulderL=p.Chest+new Vector2(vale?-11:-15,-3);
        var shoulderR=p.Chest+new Vector2(vale?12:17,-2);
        var waistL=p.Hip+new Vector2(-9,1);
        var waistR=p.Hip+new Vector2(10,1);
        Shape([shoulderL,p.Chest+new Vector2(-4,-7),p.Chest+new Vector2(6,-7),shoulderR,waistR,waistL],cloth);
        Poly([shoulderL,p.Chest+new Vector2(-4,-3),p.Hip+new Vector2(-2,0),waistL],shade);
        // A horizontal knockdown rig must not fold the standing jacket highlight into a bow-tie polygon.
        if(MathF.Abs(p.Chest.X-p.Hip.X)<18)
            Poly([p.Chest+new Vector2(5,-4),shoulderR,p.Hip+new Vector2(9,-4),p.Hip+new Vector2(4,-3)],bright.Darkened(.27f));
        DrawLine(p.Chest+new Vector2(0,-4),p.Hip+new Vector2(2,-2),accent,vale?1.1f:1.3f,true);
        if(vale)
        {
            Shape([p.Hip+new Vector2(-8,-3),p.Hip+new Vector2(11,-3),p.Hip+new Vector2(23,22),p.Hip+new Vector2(7,19)],cloth,.8f);
            DrawLine(p.Hip+new Vector2(10,1),p.Hip+new Vector2(21,20),accent,1.2f,true);
            Poly([p.Chest+new Vector2(-8,-4),p.Chest+new Vector2(0,7),p.Chest+new Vector2(8,-4)],C("182e36"));
            DrawLine(p.Chest+new Vector2(-7,-4),p.Chest+new Vector2(0,5),accent,.7f,true);
        }
        else
        {
            Shape([p.Chest+new Vector2(-13,0),p.Chest+new Vector2(-3,0),p.Chest+new Vector2(-3,8),p.Chest+new Vector2(-11,9)],accent,.6f);
            DrawLine(p.Chest+new Vector2(-11,2),p.Chest+new Vector2(-6,2),_ink,.7f,true);
            DrawLine(p.Chest+new Vector2(-11,4),p.Chest+new Vector2(-5,4),_ink,.7f,true);
            DrawLine(p.Chest+new Vector2(7,7),p.Chest+new Vector2(12,7),_ink,1,true);
        }
        LimbSegment(p.Hip+new Vector2(-9,0),p.Hip+new Vector2(10,0),2.7f,2.7f,_ink);
        Shape([p.Hip+new Vector2(0,-2),p.Hip+new Vector2(5,-2),p.Hip+new Vector2(5,2),p.Hip+new Vector2(0,2)],accent,.5f);
        // Neck and facial construction retain readable human features at gameplay scale.
        LimbSegment(p.Chest+new Vector2(1,-5),p.Head+new Vector2(-1,6),vale?3.5f:4.5f,3.5f,skin.Darkened(.18f));
        Head(p.Head,vale,skin,skinLight,accent,bright,state);
        Arm(p.Chest+new Vector2(vale?9:12,-1),p.FrontElbow,p.FrontHand,vale,skin,cloth,accent,false);
        // Near-arm highlights and wraps have enough contrast for mirror matches.
        DrawLine(p.FrontElbow.Lerp(p.FrontHand,.4f)+new Vector2(-.7f,-1),p.FrontElbow.Lerp(p.FrontHand,.75f)+new Vector2(-.7f,-1),skinLight,1.2f,true);
        if((f.MoveId.Length>0 && action>.7f) && !f.MoveId.Contains("feint"))
        {
            bool kick=f.MoveId.EndsWith("k")||f.MoveId.Contains("heel")||f.MoveId.Contains("knee")||(vale&&f.MoveId.Contains("rise"));
            var tip=kick?p.FrontFoot:p.FrontHand;
            var trail=tip-new Vector2(14+f.Strength*4,4);
            DrawLine(trail,tip,new Color(accent,.3f),1.3f,true);
            DrawLine(trail+new Vector2(4,3),tip+new Vector2(-3,3),new Color(_cream,.25f),.6f,true);
        }
        if(state.Contains("dizzy"))
        {
            for(int i=0;i<3;i++)
            {
                float a=_time*3+i*Mathf.Tau/3;
                var v=p.Head+new Vector2(MathF.Cos(a)*13,-13+MathF.Sin(a)*3);
                Star(v,2.2f,5,_amber,a);
            }
        }
        if(state.Contains("parry"))
        {
            var v=p.FrontHand+new Vector2(5,0);
            DrawArc(v,8,-1.5f,1.5f,12,_cyan,1,true);
            DrawLine(v+new Vector2(-3,-4),v+new Vector2(3,4),_cream,.7f,true);
        }
    }

    private void Arm(Vector2 shoulder,Vector2 elbow,Vector2 hand,bool vale,Color skin,Color cloth,Color accent,bool back)
    {
        LimbSegment(shoulder,elbow,vale?4.1f:6.2f,vale?3.6f:4.8f,vale?skin:cloth,true);
        if(!vale) LimbSegment(shoulder.Lerp(elbow,.3f),elbow.Lerp(shoulder,.15f),5.4f,4.7f,cloth.Lightened(.11f));
        LimbSegment(elbow,hand,vale?3.4f:4.3f,vale?2.6f:3.6f,skin,true);
        var cuff=elbow.Lerp(hand,.74f);
        LimbSegment(cuff,hand,vale?3.8f:5,vale?3.2f:4.4f,vale?C("ddd6bf"):accent,true);
        var direction=(hand-elbow).Normalized();
        var perpendicular=new Vector2(-direction.Y,direction.X);
        for(int i=0;i<3;i++)
        {
            var center=cuff.Lerp(hand,i/3f);
            DrawLine(center-perpendicular*(vale?3.2f:4f),center+perpendicular*(vale?3.2f:4f),back?C("668080"):C("667c7a"),.55f,true);
        }
        Shape([hand+new Vector2(-3,-3),hand+new Vector2(2,-4),hand+new Vector2(5,-1),hand+new Vector2(4,3),hand+new Vector2(-2,4)],skin,.8f);
        DrawLine(hand+new Vector2(1,-2),hand+new Vector2(4,-1),skin.Lightened(.2f),.6f,true);
        if(vale) LimbSegment(shoulder.Lerp(elbow,.4f),shoulder.Lerp(elbow,.54f),4.7f,4.5f,accent);
    }

    private void Head(Vector2 head,bool vale,Color skin,Color light,Color accent,Color bright,string state)
    {
        var h=head;
        Shape([h+new Vector2(-7,-6),h+new Vector2(4,-8),h+new Vector2(9,-3),h+new Vector2(8,2),h+new Vector2(10,4),h+new Vector2(7,5),h+new Vector2(5,10),h+new Vector2(-1,10),h+new Vector2(-7,5)],skin,1.1f);
        Poly([h+new Vector2(2,-5),h+new Vector2(7,-3),h+new Vector2(6,1),h+new Vector2(7,5),h+new Vector2(4,7),h+new Vector2(2,5)],light);
        DrawCircle(h+new Vector2(-6,2),2.2f,skin.Lightened(.08f));
        DrawLine(h+new Vector2(-7,1),h+new Vector2(-6,4),skin.Darkened(.3f),.7f,true);
        if(vale)
        {
            // Swept silver hair, dark undercut, tied long teal ribbon: Vale's own silhouette.
            Shape([h+new Vector2(-8,3),h+new Vector2(-10,-5),h+new Vector2(-6,-11),h+new Vector2(4,-11),h+new Vector2(10,-6),h+new Vector2(6,-4),h+new Vector2(1,-6),h+new Vector2(-3,2)],C("c4cebf"),1);
            Poly([h+new Vector2(-8,-7),h+new Vector2(-3,-10),h+new Vector2(6,-8),h+new Vector2(-3,-5)],C("f0edda"));
            Shape([h+new Vector2(-8,-5),h+new Vector2(-15,-4),h+new Vector2(-17,2),h+new Vector2(-12,3),h+new Vector2(-8,0)],C("98afa9"),.7f);
            float flutter=MathF.Sin(_time*6)*2;
            Poly([h+new Vector2(-12,1),h+new Vector2(-15,2),h+new Vector2(-27,20+flutter),h+new Vector2(-18,15+flutter)],bright);
            Poly([h+new Vector2(-12,2),h+new Vector2(-13,5),h+new Vector2(-17,25-flutter),h+new Vector2(-21,21-flutter)],accent);
            DrawCircle(h+new Vector2(-6,6),1,accent);
        }
        else
        {
            // Angular close crop and one swept top lock; no headband or copied costume.
            Shape([h+new Vector2(-8,3),h+new Vector2(-9,-6),h+new Vector2(-7,-10),h+new Vector2(-1,-12),h+new Vector2(1,-10),h+new Vector2(6,-11),h+new Vector2(10,-6),h+new Vector2(5,-3),h+new Vector2(-2,-5),h+new Vector2(-4,2)],C("293239"),1);
            Poly([h+new Vector2(-6,-8),h+new Vector2(0,-10),h+new Vector2(7,-7),h+new Vector2(-2,-6)],C("586362"));
            Poly([h+new Vector2(-2,7),h+new Vector2(5,7),h+new Vector2(5,10),h+new Vector2(0,11),h+new Vector2(-3,9)],C("725d49"));
        }
        bool hurt=state.Contains("hit")||state.Contains("dizzy")||state.Contains("knock");
        DrawLine(h+new Vector2(2,-1),h+new Vector2(7,-1.7f),_ink,.9f,true);
        DrawLine(h+new Vector2(3,1),h+new Vector2(6.6f,hurt?2:1),_ink,.8f,true);
        if(!hurt) DrawCircle(h+new Vector2(6.2f,.8f),.55f,_cream);
        DrawLine(h+new Vector2(4,6.5f),h+new Vector2(7,6),C("77513c"),.6f,true);
    }

    private void Boot(Vector2 foot,Vector2 knee,bool vale,bool back,Color accent)
    {
        var up=(knee-foot).Normalized();
        var forward=new Vector2(-up.Y,up.X);
        if(forward.X<0)forward=-forward;
        var heel=foot-forward*4;
        var toe=foot+forward*(vale?9:11);
        Shape([heel+up*9,foot+forward*5+up*8,toe+up*2,toe-forward*.2f-up*1,heel-up*1],back?C("16262c"):C("21323a"),1);
        DrawLine(heel,toe,back?C("566d69"):C("c3c6ae"),2,true);
        DrawLine(foot+up*6,foot+forward*5+up*5,accent,1.3f,true);
        if(!vale)DrawLine(heel+up*7,foot+up*8,C("7c8c80"),1.1f,true);
    }

    private sealed class Pose
    {
        public Vector2 Hip,Chest,Head,FrontElbow,FrontHand,BackElbow,BackHand,FrontKnee,FrontFoot,BackKnee,BackFoot;
        public Pose Copy() => (Pose)MemberwiseClone();
        public void Lerp(Pose p,float amount)
        {
            Hip=Hip.Lerp(p.Hip,amount);Chest=Chest.Lerp(p.Chest,amount);Head=Head.Lerp(p.Head,amount);
            FrontElbow=FrontElbow.Lerp(p.FrontElbow,amount);FrontHand=FrontHand.Lerp(p.FrontHand,amount);
            BackElbow=BackElbow.Lerp(p.BackElbow,amount);BackHand=BackHand.Lerp(p.BackHand,amount);
            FrontKnee=FrontKnee.Lerp(p.FrontKnee,amount);FrontFoot=FrontFoot.Lerp(p.FrontFoot,amount);
            BackKnee=BackKnee.Lerp(p.BackKnee,amount);BackFoot=BackFoot.Lerp(p.BackFoot,amount);
        }
        public void OffsetTorso(Vector2 offset)
        {
            Hip+=offset;Chest+=offset;Head+=offset;FrontElbow+=offset;FrontHand+=offset;BackElbow+=offset;BackHand+=offset;
        }
    }

    private Pose PoseFor(FighterRenderState f,bool vale)
    {
        float t=_time*3.2f+f.Palette*1.2f;
        var p=vale?new Pose
        {
            Hip=new(-4,-45),Chest=new(-6,-70),Head=new(-3,-84),FrontElbow=new(13,-64),FrontHand=new(27,-76),
            BackElbow=new(-20,-65),BackHand=new(-11,-77),FrontKnee=new(14,-27),FrontFoot=new(24,-2),BackKnee=new(-19,-24),BackFoot=new(-28,-2)
        }:new Pose
        {
            Hip=new(-4,-42),Chest=new(-3,-66),Head=new(3,-80),FrontElbow=new(20,-61),FrontHand=new(25,-74),
            BackElbow=new(-19,-59),BackHand=new(-5,-68),FrontKnee=new(15,-24),FrontFoot=new(22,-2),BackKnee=new(-18,-24),BackFoot=new(-26,-2)
        };
        p.OffsetTorso(new(MathF.Sin(t)*.65f,MathF.Sin(t*2)*.75f));
        string state=f.State.ToLowerInvariant(),move=f.MoveId.ToLowerInvariant();
        bool crouch=state.Contains("crouch")||move.StartsWith("c_")||move.Contains("low_");
        if(crouch)
        {
            p.OffsetTorso(new(-3,26));
            p.FrontKnee=new(21,-14);p.FrontFoot=new(28,-2);p.BackKnee=new(-20,-10);p.BackFoot=new(-24,-2);
            p.Chest+=new Vector2(3,-2);p.Head+=new Vector2(2,-2);
        }
        if(state.Contains("walk")||state.Contains("run"))
        {
            float gait=MathF.Sin(_time*11);
            p.FrontFoot=new(22+gait*14,-2-Math.Max(0,gait)*6);p.BackFoot=new(-22-gait*14,-2-Math.Max(0,-gait)*6);
            p.FrontKnee=new(8+gait*13,-25);p.BackKnee=new(-8-gait*13,-24);
            p.OffsetTorso(new(0,-MathF.Abs(gait)*1.6f));p.FrontHand+=new Vector2(gait*2,0);
        }
        if(state.Contains("dash"))
        {
            bool back=state.Contains("back");
            p.OffsetTorso(new(back?-9:14,5));p.Head+=new Vector2(back?-2:7,0);p.Chest+=new Vector2(back?-2:5,0);
            p.FrontKnee=new(13,-24);p.FrontFoot=new(31,-4);p.BackKnee=new(-21,-21);p.BackFoot=new(-39,-5);
        }
        if(!f.Grounded||state.Contains("jump")||move.StartsWith("j_"))
        {
            p.FrontKnee=new(16,-31);p.FrontFoot=new(5,-15);p.BackKnee=new(-18,-22);p.BackFoot=new(-29,-10);
            p.FrontElbow+=new Vector2(-2,-7);p.FrontHand+=new Vector2(-5,-7);
            p.BackHand+=new Vector2(-4,-8);
        }
        if(state.Contains("guard")||state.Contains("block")||state.Contains("parry"))
        {
            p.Chest+=new Vector2(-4,1);p.Head+=new Vector2(-7,1);
            p.FrontElbow=p.Chest+new Vector2(16,8);p.FrontHand=p.Head+new Vector2(19,-1);
            p.BackElbow=p.Chest+new Vector2(8,12);p.BackHand=p.Chest+new Vector2(13,-6);
            if(state.Contains("parry")){p.FrontElbow+=new Vector2(7,-3);p.FrontHand+=new Vector2(10,-3);}
        }
        if(state.Contains("hit")||state.Contains("hurt"))
        {
            p.Chest+=new Vector2(-13,4);p.Head+=new Vector2(-18,3);
            p.FrontHand+=new Vector2(-20,9);p.FrontElbow+=new Vector2(-9,5);p.BackHand+=new Vector2(-17,8);
        }
        if(state.Contains("dizzy"))
        {
            p.OffsetTorso(new(MathF.Sin(_time*4)*4,4));p.Head+=new Vector2(5,5);
            p.FrontElbow=p.Chest+new Vector2(14,15);p.FrontHand=p.Chest+new Vector2(10,27);
            p.BackElbow=p.Chest+new Vector2(-14,13);p.BackHand=p.Chest+new Vector2(-10,27);
        }
        if(state.Contains("knock")||state.Contains("down")||state.Contains("ko"))
        {
            p.Hip=new(-9,-10);p.Chest=new(-34,-10);p.Head=new(-48,-10);
            p.FrontElbow=new(-25,-7);p.FrontHand=new(-8,-5);p.BackElbow=new(-37,-6);p.BackHand=new(-30,-3);
            p.FrontKnee=new(10,-9);p.FrontFoot=new(29,-3);p.BackKnee=new(5,-11);p.BackFoot=new(23,-5);
            return p;
        }
        if(state.Contains("win"))
        {
            p.FrontElbow=p.Chest+new Vector2(15,-15);p.FrontHand=p.Head+new Vector2(10,-22);
            p.BackElbow=p.Chest+new Vector2(-18,12);p.BackHand=p.Hip+new Vector2(-11,-3);
        }
        if(move.Length==0)return p;
        var target=p.Copy();
        float strength=move.Contains("_h")||move.EndsWith("hp")||move.EndsWith("hk")||move.StartsWith("super")?3:move.Contains("_m")||move.EndsWith("mp")||move.EndsWith("mk")?2:Math.Clamp(f.Strength,1,3);
        float reach=31+strength*8+(vale?5:0);
        bool kick=move.EndsWith("lk")||move.EndsWith("mk")||move.EndsWith("hk")||move.Contains("heel")||move.Contains("knee")||(vale&&move=="command_fhp");
        if(kick)
        {
            target.OffsetTorso(new(-7,0));
            target.FrontKnee=new(reach*.49f,crouch?-13:-48+strength*2);
            target.FrontFoot=new(reach+4,crouch?-5:-47+(3-strength)*10);
            target.BackFoot=new(-15,-2);target.BackKnee=new(-10,-25);
            target.FrontElbow+=new Vector2(-6,-4);target.FrontHand+=new Vector2(-13,0);
            if(move.Contains("knee")) {target.FrontKnee=new(30,-63);target.FrontFoot=new(14,-39);target.BackFoot=new(-24,-18);}
            if(move.Contains("heel_arc")){target.FrontKnee=new(21,-65);target.FrontFoot=new(20,-93);}
        }
        else
        {
            target.OffsetTorso(new(5+strength,0));target.Head+=new Vector2(3,0);
            target.FrontElbow=target.Chest+new Vector2(reach*.5f,-1);
            target.FrontHand=target.Chest+new Vector2(reach,2+(crouch?3:0));
            target.BackHand=target.Chest+new Vector2(2,5);
            if(move.Contains("hp")||move.Contains("high_hook")){target.FrontElbow+=new Vector2(5,-8);target.FrontHand+=new Vector2(-1,-12);}
        }
        if(move.Contains("pulse"))
        {
            target.Chest+=new Vector2(-2,0);target.FrontElbow=target.Chest+new Vector2(19,10);target.FrontHand=target.Chest+new Vector2(37,0);
            target.BackElbow=target.Chest+new Vector2(9,15);target.BackHand=target.Chest+new Vector2(31,7);
        }
        if(move.Contains("rise")||move=="super_2"&&!vale||move=="super_1"&&vale)
        {
            target.OffsetTorso(new(5,-8));
            if(vale){target.FrontKnee=new(22,-67);target.FrontFoot=new(34,-98);target.BackFoot=new(-22,-12);target.Chest+=new Vector2(-7,2);target.Head+=new Vector2(-9,3);}
            else{target.FrontElbow=target.Head+new Vector2(12,-5);target.FrontHand=target.Head+new Vector2(1,3);target.FrontKnee=new(20,-35);target.FrontFoot=new(11,-16);}
        }
        if(move.Contains("throw")||move.Contains("clinch"))
        {
            target.FrontElbow=target.Chest+new Vector2(25,5);target.FrontHand=target.Chest+new Vector2(37,-11);
            target.BackElbow=target.Chest+new Vector2(12,13);target.BackHand=target.Chest+new Vector2(32,8);
            if(move.Contains("back")){target.OffsetTorso(new(-10,8));target.Head+=new Vector2(-6,0);}
        }
        if(move.Contains("sway")||move.Contains("feint"))
        {
            target.OffsetTorso(new(-14,5));target.Head+=new Vector2(-7,0);
            target.FrontElbow=target.Chest+new Vector2(15,6);target.FrontHand=target.Chest+new Vector2(move.Contains("feint")?27:45,-3);
        }
        if(move.Contains("overhead"))
        {
            target.FrontElbow=target.Head+new Vector2(17,-10);target.FrontHand=target.Head+new Vector2(27,2);
            target.BackElbow=target.Head+new Vector2(3,-16);target.BackHand=target.Head+new Vector2(15,-11);
        }
        if(move.StartsWith("super") && !(move=="super_2"&&!vale || move=="super_1"&&vale))
        {
            float alternate=MathF.Sin(f.ActionFrame*.5f);
            if(vale){target.FrontKnee=new(28,-48);target.FrontFoot=new(64,-53+alternate*15);target.Chest+=new Vector2(-9,0);target.Head+=new Vector2(-13,0);}
            else{target.FrontHand=target.Chest+new Vector2(55,-4+alternate*12);target.BackHand=target.Chest+new Vector2(24-alternate*16,-2);}
        }
        // Anticipation explicitly winds into the strike, then recovers over authored frames.
        float envelope=ActionEnvelope(f);
        if(f.ActionFrame<f.Startup)
        {
            float anticipation=Math.Clamp(f.ActionFrame/(float)Math.Max(1,f.Startup),0,1);
            p.OffsetTorso(new(-3*anticipation,2*anticipation));p.FrontHand+=new Vector2(-5*anticipation,2*anticipation);
        }
        p.Lerp(target,envelope);
        return p;
    }

    private static float ActionEnvelope(FighterRenderState f)
    {
        int frame=Math.Max(0,f.ActionFrame),startup=Math.Max(1,f.Startup),active=Math.Max(1,f.Active),recovery=Math.Max(1,f.Recovery);
        if(frame<startup)return MathF.Pow(frame/(float)startup,4)*.65f;
        if(frame<startup+active)return 1;
        float t=Math.Clamp((frame-startup-active)/(float)recovery,0,1);
        return 1-t*t*(3-2*t);
    }

    private void DrawProjectile(ProjectileRenderState projectile)
    {
        var pos=WorldToScreen(projectile.X,projectile.Y);
        var col=projectile.Owner%2==0?_amber:_cyan;
        float size=Math.Clamp(projectile.Width*Units*.5f,12,projectile.IsSuper?36:28);
        float pulse=1+MathF.Sin(_time*30)*.08f;
        int dir=projectile.Facing<0?-1:1;
        Ellipse(pos,size*1.5f*pulse,size*pulse,new Color(col,.09f));
        for(int i=0;i<4;i++)
        {
            float x=pos.X-dir*(14+i*14);
            DrawLine(new(x-dir*15,pos.Y-8+i*5),new(x+dir*8,pos.Y-8+i*5),new Color(col,.44f-i*.09f),3-i*.45f,true);
        }
        Poly([pos+new Vector2(-dir*size*.8f,-size*.65f),pos+new Vector2(dir*size*.35f,-size),pos+new Vector2(dir*size,0),pos+new Vector2(dir*size*.35f,size),pos+new Vector2(-dir*size*.8f,size*.65f),pos+new Vector2(-dir*size*.35f,0)],col);
        Poly([pos+new Vector2(-dir*size*.2f,-size*.5f),pos+new Vector2(dir*size*.75f,0),pos+new Vector2(-dir*size*.2f,size*.5f),pos+new Vector2(dir*size*.1f,0)],_cream);
        DrawArc(pos,size*1.3f,_time*8,_time*8+4,22,new Color(col,.72f),1.5f,true);
        if(_state.DebugBoxes)DrawRect(new Rect2(pos-new Vector2(projectile.Width,projectile.Height)*Units*.5f,new Vector2(projectile.Width,projectile.Height)*Units),new Color(_cyan,.85f),false,1.2f);
    }

    private void DrawBoxes(FighterRenderState f)
    {
        foreach(var b in f.Boxes)
        {
            var point=WorldToScreen(f.X+b.X,f.Y+b.Y+b.Height);
            var rect=new Rect2(point,new Vector2(b.Width,b.Height)*Units);
            var col=b.Kind=="hit"?C("ef7261"):b.Kind=="push"?C("dcce77"):_cyan;
            DrawRect(rect,new Color(col,.08f));DrawRect(rect,new Color(col,.8f),false,1.2f);
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

    private void DrawImpact(Impact impact)
    {
        var pos=WorldToScreen(impact.X,impact.Y);
        float life=impact.Age/impact.Lifetime;
        float fade=1-life;
        bool parry=impact.Kind.Contains("parry"),block=impact.Kind.Contains("block")||impact.Kind.Contains("guard"),ground=impact.Kind.Contains("land")||impact.Kind.Contains("dash");
        var col=parry?_cyan:block?C("a2c0ce"):_amber;
        if(ground)
        {
            for(int i=0;i<6;i++)Ellipse(pos+new Vector2((i-2.5f)*(8+life*13),-life*(12+i%2*9)),7+life*7,3+life*2,new Color(_cream,fade*.18f));
            return;
        }
        float radius=10+life*(35+impact.Strength*8);
        if(parry)
        {
            // Parry: expanding diamond and four clean rays, distinct without colour.
            PolyOutline([pos+new Vector2(0,-radius),pos+new Vector2(radius,0),pos+new Vector2(0,radius),pos+new Vector2(-radius,0)],new Color(col,fade),Math.Max(1,3*fade));
            for(int i=0;i<4;i++){float a=i*Mathf.Pi/2;DrawLine(pos+Vector2.FromAngle(a)*radius*.6f,pos+Vector2.FromAngle(a)*(radius+20*fade),new Color(_cream,fade),2,true);}
            DrawArc(pos,radius*.65f,0,Mathf.Tau,32,new Color(_cream,fade*.6f),1,true);
        }
        else if(block)
        {
            // Block: two semicircular shields, with no hit-shaped burst.
            DrawArc(pos,radius,-1.35f,1.35f,24,new Color(col,fade),3*fade+.5f,true);
            DrawArc(pos,radius*.76f,-1.35f,1.35f,24,new Color(_cream,fade*.7f),1,true);
            DrawLine(pos+new Vector2(0,-10*fade),pos+new Vector2(0,10*fade),new Color(col,fade),2,true);
        }
        else
        {
            if(!_state.ReducedFlashes) Star(pos,Math.Max(1,20*fade+impact.Strength*5),8,new Color(_cream,fade*.9f),impact.Seed);
            for(int i=0;i<11;i++)
            {
                float a=impact.Seed+i*Mathf.Tau/11;
                var d=Vector2.FromAngle(a);
                float dist=radius*(.7f+(i%3)*.19f);
                DrawLine(pos+d*dist,pos+d*(dist+10*fade+(i%4)*3),new Color(i%3==0?_cream:col,fade),i%3==0?2:1,true);
            }
            DrawArc(pos,radius*.7f,impact.Seed,impact.Seed+4.8f,26,new Color(col,fade*.6f),1.2f,true);
        }
    }

    private void Limb(Vector2 start,Vector2 joint,Vector2 end,float upper,float lower,Color color,Color highlight)
    {
        LimbSegment(start,joint,upper,lower+1,color,true);LimbSegment(joint,end,lower+1,lower,color,true);
        var dir=(end-joint).Normalized();var perp=new Vector2(-dir.Y,dir.X);
        DrawLine(joint+perp*lower*.45f,end+perp*lower*.45f,highlight,Math.Max(1,lower*.32f),true);
    }
    private void LimbSegment(Vector2 a,Vector2 b,float widthA,float widthB,Color color,bool outline=false)
    {
        var direction=(b-a).Normalized();var normal=new Vector2(-direction.Y,direction.X);
        var points=new[]{a+normal*widthA,a-normal*widthA,b-normal*widthB,b+normal*widthB};
        if(outline){DrawLine(a,b,_ink,Math.Max(widthA,widthB)*2+2,true);DrawCircle(a,widthA+.7f,_ink);DrawCircle(b,widthB+.7f,_ink);}
        Poly(points,color);DrawCircle(a,widthA,color);DrawCircle(b,widthB,color);
    }
    private void Shape(Vector2[] points,Color color,float width=1.2f){Poly(points,color);PolyOutline(points,_ink,width);}
    private void Poly(Vector2[] points,Color color)=>DrawColoredPolygon(points,color);
    private void PolyOutline(Vector2[] points,Color color,float width)
    {
        var closed=new Vector2[points.Length+1];Array.Copy(points,closed,points.Length);closed[^1]=points[0];DrawPolyline(closed,color,width,true);
    }
    private void Ellipse(Vector2 center,float rx,float ry,Color color)
    {
        var points=new Vector2[32];for(int i=0;i<32;i++){float a=i*Mathf.Tau/32;points[i]=center+new Vector2(MathF.Cos(a)*rx,MathF.Sin(a)*ry);}Poly(points,color);
    }
    private void Star(Vector2 center,float radius,int points,Color color,float rotation=0)
    {
        var vertices=new Vector2[points*2];for(int i=0;i<vertices.Length;i++)vertices[i]=center+Vector2.FromAngle(rotation+i*Mathf.Tau/vertices.Length)*(i%2==0?radius:radius*.26f);Poly(vertices,color);
    }
    private static Color C(string html)=>Color.FromHtml(html);
}
