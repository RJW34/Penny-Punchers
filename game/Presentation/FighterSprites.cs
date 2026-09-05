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
        public readonly Dictionary<string, string[]> States = new();
        public readonly Dictionary<string, Dictionary<string, string[]>> Moves = new();
        public bool ChromaKey;
    }
    private static readonly Dictionary<string, Atlas> Atlases = new();
    private FighterRenderState? _fighter;
    private Atlas? _atlas;
    private ShaderMaterial? _palette;
    private int _lastTick=-1, _stateAge;
    private string _lastState="";
    public string CurrentCel {get;private set;}="";

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
        foreach(var state in doc.RootElement.GetProperty("states").EnumerateObject())atlas.States[state.Name]=Names(state.Value);
        foreach(var move in doc.RootElement.GetProperty("moves").EnumerateObject())
        {
            var phases=new Dictionary<string,string[]>();
            foreach(var phase in new[]{"startup","active","recovery"})phases[phase]=Names(move.Value.GetProperty(phase));
            atlas.Moves[move.Name]=phases;
        }
        return atlas;
    }
    private static string[] Names(JsonElement value)=>value.EnumerateArray().Select(v=>v.GetString()!).ToArray();
    public override void _Draw()
    {
        if(_fighter is not {} f||_atlas is not {} atlas)return;
        string[] names;int index;
        bool interrupted=f.State is "knockdown" or "falling" or "wakeup" or "dizzy" or "hit" or "lowhit" or "airhit" or "thrown" or "guard" or "crouchguard" or "win" or "defeat" or "draw" or "intro";
        if(!interrupted&&f.MoveId.Length>0&&atlas.Moves.TryGetValue(f.MoveId,out var phases))
        {
            string phase=f.ActionFrame<f.Startup?"startup":f.ActionFrame<f.Startup+f.Active?"active":"recovery";
            int start=phase=="startup"?0:phase=="active"?f.Startup:f.Startup+f.Active;
            int duration=phase=="startup"?f.Startup:phase=="active"?f.Active:f.Recovery;
            names=phases[phase];index=Math.Clamp((f.ActionFrame-start)*names.Length/Math.Max(1,duration),0,names.Length-1);
        }
        else
        {
            if(!atlas.States.TryGetValue(f.State,out names!))
            {
                string fallback=f.State switch{"walkback"=>"walk","rise" or "fall" or "apex" or "superjump"=>"jump","redparry" or "airparry"=>"parry","redlowparry"=>"crouchparry","takeoff" or "landing"=>"crouch","lowhit" or "airhit" or "thrown"=>"hit","falling" or "wakeup" or "defeat"=>"knockdown",_=>"idle"};
                names=atlas.States.TryGetValue(fallback,out var found)?found:atlas.States["idle"];
            }
            bool loop=f.State is "idle" or "walk" or "walkback" or "dizzy";
            index=loop?(_stateAge/7)%names.Length:Math.Min(names.Length-1,_stateAge/5);
        }
        CurrentCel=names[index];var cel=atlas.Cells[CurrentCel];
        float scale=cel.Scale;
        DrawSetTransform(Vector2.Zero,0,new Vector2(cel.Facing,1));
        DrawTextureRectRegion(cel.Texture,new Rect2(-cel.Pivot*scale,cel.Region.Size*scale),cel.Region);
    }
}
