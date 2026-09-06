using Godot;
using System.Text.Json;

namespace StrikeLedger.Presentation;

/// <summary>Read-only cel selection. Canonical action frames are never retimed by artwork.</summary>
public partial class FighterSprites : Node2D
{
    private sealed record Cel(Texture2D Texture, Rect2 Region, Vector2 Pivot, float Scale, int Facing);
    private sealed class Atlas
    {
        public readonly Dictionary<string, Cel> Cells = new();
        public readonly Dictionary<string, CelHold[]> States = new();
        public readonly Dictionary<string, Dictionary<string, CelHold[]>> Moves = new();
        public bool ChromaKey;
    }
    private static readonly Dictionary<string, Atlas> Atlases = new();
    private FighterRenderState? _fighter;
    private Atlas? _atlas;
    private ShaderMaterial? _palette;
    private int _lastTick=-1, _stateAge;
    private string _lastState="";
    public string CurrentCel {get;private set;}="";
    public Rect2 LocalBounds {get;private set;}=new(-130,-280,260,300);

    public override void _Ready()
    {
        TextureFilter=TextureFilterEnum.Nearest;
        _palette=new ShaderMaterial{Shader=GD.Load<Shader>("res://Presentation/FighterPalette.gdshader")};
        Material=_palette;
    }
    public void SetFighter(FighterRenderState fighter,int tick)
    {
        _fighter=fighter;
        if(!Atlases.TryGetValue(fighter.Id,out _atlas))
        {
            _atlas=LoadAtlas(fighter.Id);Atlases[fighter.Id]=_atlas;
        }
        if(tick<_lastTick||fighter.State!=_lastState)_stateAge=0;
        else if(!fighter.Frozen&&_lastTick>=0)_stateAge+=Math.Clamp(tick-_lastTick,0,6);
        _lastTick=tick;_lastState=fighter.State;
        _palette?.SetShaderParameter("alternate",fighter.Palette%2!=0);
        _palette?.SetShaderParameter("vale",fighter.Id=="vale");
        _palette?.SetShaderParameter("chroma_key",_atlas.ChromaKey);
        SelectCel();
        QueueRedraw();
    }
    public void ResetTimeline(){_lastTick=-1;_stateAge=0;_lastState="";}
    public override void _ExitTree(){_palette?.Dispose();_palette=null;}
    public static void ReleaseAtlases()
    {
        foreach(var texture in Atlases.Values.SelectMany(a=>a.Cells.Values).Select(c=>c.Texture).Distinct())texture.Dispose();
        Atlases.Clear();
    }
    private static Atlas LoadAtlas(string id)
    {
        var atlas=new Atlas();
        if(id is not ("rook" or "vale"))
        {
            GD.PushWarning($"No presentation pack registered for fighter '{id}'. Using clearly labelled Rook development placeholder; this is not authored roster art.");
            id="rook";
        }
        string root="res://Assets/AfterHours/Fighters/"+(id=="vale"?"Vale":"Rook")+"/";
        using var doc=JsonDocument.Parse(Godot.FileAccess.GetFileAsString(root+"atlas.json"));
        atlas.ChromaKey=doc.RootElement.TryGetProperty("chroma_key",out _);
        var textureCache=new Dictionary<string,Texture2D>();
        foreach(var entry in doc.RootElement.GetProperty("cells").EnumerateObject())
        {
            var c=entry.Value;var path=c.GetProperty("texture").GetString()!;
            if(!textureCache.TryGetValue(path,out var texture)){texture=GD.Load<Texture2D>(root+path);textureCache[path]=texture;}
            var r=c.GetProperty("rect").EnumerateArray().Select(x=>x.GetSingle()).ToArray();
            var p=c.GetProperty("pivot").EnumerateArray().Select(x=>x.GetSingle()).ToArray();
            atlas.Cells[entry.Name]=new(texture,new(r[0],r[1],r[2],r[3]),new(p[0],p[1]),c.GetProperty("scale").GetSingle(),c.TryGetProperty("facing",out var f)?f.GetInt32():1);
        }
        using var timing=JsonDocument.Parse(Godot.FileAccess.GetFileAsString("res://Presentation/animation-timing.json"));
        var profile=timing.RootElement.GetProperty("fighters").GetProperty(id);
        foreach(var state in profile.GetProperty("states").EnumerateObject())atlas.States[state.Name]=Holds(state.Value);
        foreach(var move in profile.GetProperty("moves").EnumerateObject())
        {
            var phases=new Dictionary<string,CelHold[]>();
            foreach(var phase in new[]{"startup","active","recovery"})phases[phase]=Holds(move.Value.GetProperty(phase));
            atlas.Moves[move.Name]=phases;
        }
        foreach(var hold in atlas.States.Values.SelectMany(c=>c).Concat(atlas.Moves.Values.SelectMany(m=>m.Values).SelectMany(c=>c)))
            if(!atlas.Cells.ContainsKey(hold.Cel)||hold.Ticks<1)throw new InvalidDataException($"Invalid cel hold {id}/{hold.Cel}");
        return atlas;
    }
    private static CelHold[] Holds(JsonElement value)=>value.EnumerateArray().Select(v=>new CelHold(v.GetProperty("cel").GetString()!,v.GetProperty("ticks").GetInt32())).ToArray();
    private void SelectCel()
    {
        if(_fighter is not {} f||_atlas is not {} atlas)return;
        CelHold[] names;int tick;bool loop=false;
        IEnumerable<CelHold>? boundsClip=null;
        bool interrupted=f.State is "knockdown" or "falling" or "wakeup" or "dizzy" or "hit" or "lowhit" or "airhit" or "thrown" or "guard" or "crouchguard" or "win" or "defeat" or "draw" or "intro"
            ||f.MoveId=="buy_r_g3_vault_hop"&&!f.Grounded;
        // Feints are deliberately indistinguishable during the shared anticipation.
        // The canonical mimic contract supplies the source; no generated flash,
        // tell, or separate pose is allowed before the abort begins.
        bool shared=!interrupted&&f.ActionFrame<f.MimicSharedTicks&&f.MimicSourceMoveId.Length>0;
        string visibleMove=shared?f.MimicSourceMoveId:f.MoveId;
        if(!interrupted&&visibleMove.Length>0&&atlas.Moves.TryGetValue(visibleMove,out var phases))
        {
            int startup=shared?f.MimicSourceStartup:f.Startup,active=shared?f.MimicSourceActive:f.Active;
            string phase=f.ActionFrame<startup?"startup":f.ActionFrame<startup+active?"active":"recovery";
            int start=phase=="startup"?0:phase=="active"?startup:startup+active;
            names=phases[phase];tick=Math.Max(0,f.ActionFrame-start);
            boundsClip=phases.Values.SelectMany(c=>c);
        }
        else
        {
            if(!atlas.States.TryGetValue(f.State,out names!))
            {
                string fallback=f.State switch{"walkback"=>"walk","rise" or "fall" or "apex" or "superjump"=>"jump","redparry" or "airparry"=>"parry","redlowparry"=>"crouchparry","takeoff" or "landing"=>"crouch","lowhit" or "airhit" or "thrown"=>"hit","falling" or "wakeup" or "defeat"=>"knockdown",_=>"idle"};
                names=atlas.States.TryGetValue(fallback,out var found)?found:atlas.States["idle"];
            }
            loop=f.State is "idle" or "walk" or "walkback" or "dizzy";
            tick=_stateAge;
        }
        CurrentCel=CelTimeline.Select(names,tick,loop);
        bool first=true;Rect2 bounds=default;
        foreach(var hold in boundsClip??names)
        {
            var c=atlas.Cells[hold.Cel];var rect=new Rect2(-c.Pivot*c.Scale,c.Region.Size*c.Scale);
            if(c.Facing<0)rect=new Rect2(-rect.End.X,rect.Position.Y,rect.Size.X,rect.Size.Y);
            bounds=first?rect:bounds.Merge(rect);first=false;
        }
        LocalBounds=bounds;
    }
    public override void _Draw()
    {
        if(_atlas is not {} atlas||CurrentCel.Length==0)return;
        var cel=atlas.Cells[CurrentCel];
        float scale=cel.Scale;
        DrawSetTransform(Vector2.Zero,0,new Vector2(cel.Facing,1));
        DrawTextureRectRegion(cel.Texture,new Rect2(-cel.Pivot*scale,cel.Region.Size*scale),cel.Region);
        if(_fighter?.Id is {} id&&id is not ("rook" or "vale"))
        {
            DrawSetTransform(Vector2.Zero);
            DrawRect(new Rect2(-112,-306,224,24),new Color(.05f,.05f,.07f,.95f));
            DrawString(UiTypography.Body,new(-108,-288),"ART PLACEHOLDER / "+id.ToUpperInvariant(),HorizontalAlignment.Left,216,12,Colors.White);
        }
    }
}
