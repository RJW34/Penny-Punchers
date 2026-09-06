using Godot;
namespace StrikeLedger.Presentation;

/// <summary>Original procedural sounds; bounded voices with defense/terminal priority.</summary>
public partial class ArenaAudio : Node
{
    private sealed class Voice(AudioStreamPlayer player)
    {
        public AudioStreamPlayer Player=player;public int Priority;public string Key="";
        public ulong Started;
    }
    readonly Dictionary<string,AudioStream> _sounds=[];
    readonly Dictionary<string,AudioStreamWav> _loops=[];
    readonly List<Voice> _voices=[];
    readonly Dictionary<string,int> _variation=[];
    AudioStreamPlayer? _music;
    bool _closed;string _stage="",_phase="";bool _frozen;
    float _master=.75f,_musicVolume=.35f,_sfxVolume=.8f;
    public override void _Ready()
    {
        foreach(string name in new[]{"hit","hit_1","hit_2","heavy","heavy_1","heavy_2","block","parry","swing","throw","land","ex","super","round","ko","select","confirm","cancel","denied","rook_breath","vale_breath"})
        {
            string path=$"res://Presentation/Audio/{name}.wav";
            if(ResourceLoader.Exists(path))_sounds[name]=GD.Load<AudioStream>(path);
        }
        for(int i=0;i<16;i++){var player=new AudioStreamPlayer{MaxPolyphony=1};AddChild(player);_voices.Add(new(player));}
        foreach(string name in new[]{"foundry","marist_green","marist_gates"})
        {
            var stream=GD.Load<AudioStreamWav>($"res://Presentation/Audio/{name}_loop.wav");
            stream.LoopMode=AudioStreamWav.LoopModeEnum.Forward;stream.LoopBegin=0;stream.LoopEnd=stream.Data.Length/2;_loops[name]=stream;
        }
        _music=new AudioStreamPlayer();AddChild(_music);SetContext("foundry","Preparation",false);ApplyVolumes();
    }
    public void SetContext(string stage,string phase,bool frozen)
    {
        if(_closed||_music==null)return;
        if(!_loops.ContainsKey(stage))stage="foundry";
        if(_stage!=stage){_stage=stage;_music.Stop();_music.Stream=_loops[stage];_music.Play();}
        if(_phase!=phase||_frozen!=frozen){_phase=phase;_frozen=frozen;ApplyVolumes();}
    }
    public void SetVolumes(float master,float music,float sfx)
    {
        _master=Math.Clamp(master,0,1);_musicVolume=Math.Clamp(music,0,1);_sfxVolume=Math.Clamp(sfx,0,1);ApplyVolumes();
    }
    void ApplyVolumes()
    {
        if(_music!=null)_music.VolumeDb=Db(_master*_musicVolume*.65f*(_phase=="Fight"?1:.6f)*(_frozen?.55f:1));
        foreach(var voice in _voices)voice.Player.VolumeDb=Db(_master*_sfxVolume);
    }
    public void PlayCue(string cue,string fighterId="",string key="")
    {
        if(_closed)return;
        string name=cue.ToLowerInvariant();
        name=name is "reflect" or "armor" or "countercatch"?"block":name=="dissipate"?"cancel":name.Contains("parry")||name.Contains("tech")?"parry":name.Contains("block")||name.Contains("guard")?"block"
            :name.Contains("knockout")||name=="ko"||name.Contains("win")?"ko":name.Contains("super")?"super":name.Contains("ex")?"ex"
            :name.Contains("heavy")||name.Contains("counter")?"heavy":name.Contains("hit")?"hit":name.Contains("throw")?"throw"
            :name.Contains("land")||name.Contains("dash")?"land":name.Contains("round")||name.Contains("fight")?"round"
            :name.Contains("denied")||name.Contains("insufficient")?"denied":name.Contains("confirm")||name.Contains("accept")?"confirm"
            :name.Contains("cancel")||name.Contains("back")?"cancel":name.Contains("select")||name.Contains("menu")?"select"
            :name.Contains("attack")||name.Contains("move")||name.Contains("swing")?"swing":name;
        int priority=name switch{"ko"=>100,"parry"=>90,"block"=>80,"super"=>75,"ex"=>65,"throw"=>60,"heavy"=>50,"hit"=>45,"round"=>70,"land"=>8,"swing" or "breath"=>10,_=>25};
        int variation=_variation.GetValueOrDefault(name);_variation[name]=variation+1;
        string file=name=="breath"?(fighterId=="vale"?"vale_breath":"rook_breath"):(name is "hit" or "heavy")&&variation%3!=0?$"{name}_{variation%3}":name;
        if(!_sounds.TryGetValue(file,out var stream))return;
        var voice=_voices.FirstOrDefault(v=>!v.Player.Playing);
        if(voice==null)
        {
            voice=_voices.Where(v=>v.Priority<=priority).OrderBy(v=>v.Priority).ThenBy(v=>v.Started).FirstOrDefault();
            if(voice==null)return;
        }
        voice.Player.Stop();voice.Priority=priority;voice.Key=key;voice.Started=Time.GetTicksMsec();voice.Player.Stream=stream;
        voice.Player.PitchScale=(name is "hit" or "heavy" or "swing" or "land")?(fighterId=="vale"?1.06f:.97f)*((variation%3) switch{1=>.97f,2=>1.03f,_=>1}):1;
        voice.Player.VolumeDb=Db(_master*_sfxVolume*(name is "swing" or "breath"?.65f:1));voice.Player.Play();
    }
    public void CancelCue(string key)
    {
        if(key.Length==0)return;
        foreach(var voice in _voices.Where(v=>v.Key==key)){voice.Player.Stop();voice.Key="";}
    }
    public void ShutdownAudio()
    {
        if(_closed)return;_closed=true;
        foreach(var voice in _voices){voice.Player.Stop();voice.Player.Stream=null;}
        if(_music!=null){_music.Stop();_music.Stream=null;}
        _voices.Clear();_sounds.Clear();_loops.Clear();_music=null;
    }
    public override void _ExitTree()=>ShutdownAudio();
    static float Db(float linear)=>linear<=.0001f?-80:Mathf.LinearToDb(linear);
}
