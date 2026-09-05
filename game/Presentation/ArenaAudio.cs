using Godot;

namespace StrikeLedger.Presentation;

/// <summary>Local, original procedural sound. Voices are bounded; no gameplay state lives here.</summary>
public partial class ArenaAudio : Node
{
    private readonly Dictionary<string, AudioStream> _sounds = new();
    private readonly List<AudioStreamPlayer> _voices = [];
    private AudioStreamPlayer? _music;
    private int _nextVoice;
    private bool _closed;
    private float _master = .75f, _musicVolume = .35f, _sfxVolume = .8f;

    public override void _Ready()
    {
        foreach (string name in new[] { "hit", "heavy", "block", "parry", "swing", "throw", "land", "ex", "super", "round", "ko", "select", "confirm", "cancel", "denied" })
        {
            string path = $"res://Presentation/Audio/{name}.wav";
            if (ResourceLoader.Exists(path)) _sounds[name] = GD.Load<AudioStream>(path);
        }
        for (int i = 0; i < 10; i++)
        {
            var player = new AudioStreamPlayer { MaxPolyphony = 1 };
            AddChild(player);
            _voices.Add(player);
        }
        const string musicPath = "res://Presentation/Audio/foundry_loop.wav";
        if (ResourceLoader.Exists(musicPath))
        {
            var stream = GD.Load<AudioStreamWav>(musicPath);
            stream.LoopMode = AudioStreamWav.LoopModeEnum.Forward;
            stream.LoopBegin = 0;
            stream.LoopEnd = stream.Data.Length / 2;
            _music = new AudioStreamPlayer { Stream = stream };
            AddChild(_music);
            _music.Play();
        }
        ApplyVolumes();
    }

    public void SetVolumes(float master, float music, float sfx)
    {
        _master = Math.Clamp(master, 0, 1);
        _musicVolume = Math.Clamp(music, 0, 1);
        _sfxVolume = Math.Clamp(sfx, 0, 1);
        ApplyVolumes();
    }

    private void ApplyVolumes()
    {
        if (_music != null) _music.VolumeDb = Db(_master * _musicVolume * .65f);
        foreach (var voice in _voices) voice.VolumeDb = Db(_master * _sfxVolume);
    }

    public void PlayCue(string cue)
    {
        if(_closed)return;
        string key = cue.ToLowerInvariant();
        key = key.Contains("parry") ? "parry"
            : key.Contains("block") || key.Contains("guard") ? "block"
            : key.Contains("heavy") || key.Contains("counter") ? "heavy"
            : key.Contains("hit") ? "hit"
            : key.Contains("super") ? "super"
            : key.Contains("throw") ? "throw"
            : key.Contains("land") || key.Contains("dash") ? "land"
            : key.Contains("round") || key.Contains("fight") ? "round"
            : key.Contains("knockout") || key == "ko" || key.Contains("win") ? "ko"
            : key.Contains("denied") || key.Contains("insufficient") ? "denied"
            : key.Contains("ex") || key.Contains("spend") || key.Contains("debit") ? "ex"
            : key.Contains("confirm") || key.Contains("accept") ? "confirm"
            : key.Contains("cancel") || key.Contains("back") ? "cancel"
            : key.Contains("select") || key.Contains("menu") ? "select"
            : key.Contains("attack") || key.Contains("move") || key.Contains("swing") ? "swing"
            : key;
        if (_voices.Count == 0 || !_sounds.TryGetValue(key, out var stream)) return;
        var player = _voices[_nextVoice++ % _voices.Count];
        player.Stop();
        player.Stream = stream;
        player.Play();
    }

    public void ShutdownAudio()
    {
        if(_closed)return;_closed=true;
        // Call before quitting and allow the audio thread to drain its stop commands.
        foreach(var voice in _voices){voice.Stop();voice.Stream=null;}
        if(_music!=null){_music.Stop();_music.Stream=null;}
        _voices.Clear();_sounds.Clear();_music=null;
    }

    public override void _ExitTree()=>ShutdownAudio();

    private static float Db(float linear) => linear <= .0001f ? -80 : Mathf.LinearToDb(linear);
}
