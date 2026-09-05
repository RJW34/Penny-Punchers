using Godot;
using StrikeLedger.Presentation;
using System.Text.Json;

public partial class Main
{
    bool _artReviewActive;
    void BeginArtReview()
    {
        _artReviewActive=true;paused=true;sim=null;Clear("art-review");
        System.IO.Directory.CreateDirectory(evidenceDir);
        Callable.From(RunArtReview).CallDeferred();
    }
    async void RunArtReview()
    {
        var checks=new List<object>();var captures=new List<string>();
        int tick=0;bool pass=true;
        Label title=Text("AFTER HOURS / RENDERER REVIEW",34,24,26,Gold);
        Label detail=Text("Presentation fixtures · canonical timings · no simulated combat claims",36,61,16,Cream);
        Label footer=Text("ROOT 604 PX / NEAREST FILTER / LIVE SIMULATION ADAPTER",36,669,16,Cream);
        try
        {
            foreach(string id in new[]{"rook","vale"})
            {
                string directory=id=="rook"?"Rook":"Vale";
                using var atlas=JsonDocument.Parse(Godot.FileAccess.GetFileAsString($"res://Assets/AfterHours/Fighters/{directory}/atlas.json"));
                foreach(var move in content.Fighters[id].Moves)
                {
                    foreach(var phase in new[]{"startup","active","recovery"})
                    {
                        int start=phase=="startup"?0:phase=="active"?move.Startup:move.Startup+move.Active;
                        int duration=phase=="startup"?move.Startup:phase=="active"?move.Active:move.Recovery;
                        if(duration==0)continue;
                        int frame=start+duration/2;
                        var f=new FighterRenderState{Id=id,X=300000,Facing=1,MoveId=move.Id,ActionFrame=frame,Startup=move.Startup,Active=move.Active,Recovery=move.Recovery,Grounded=!move.Id.StartsWith("j_"),Y=move.Id.StartsWith("j_")?65000:0};
                        var mirror=new FighterRenderState{Id=id,X=468000,Facing=-1,Palette=1,MoveId=move.Id,ActionFrame=frame,Startup=move.Startup,Active=move.Active,Recovery=move.Recovery,Grounded=f.Grounded,Y=f.Y};
                        arena.SetState(new ArenaRenderState{Tick=tick+=6,Fighters=[f,mirror],TrainingGrid=false,ShakeScale=0});
                        title.Text=$"{id.ToUpperInvariant()} / {move.Name.ToUpperInvariant()}";
                        detail.Text=$"{move.Id} · {phase} · canonical frame {frame} / {move.TotalTicks} · P1 / P2 mirror palettes";
                        await UiFrames(4);
                        var names=atlas.RootElement.GetProperty("moves").GetProperty(move.Id).GetProperty(phase).EnumerateArray().Select(x=>x.GetString()!).ToArray();
                        string expected=names[Math.Clamp((frame-start)*names.Length/duration,0,names.Length-1)];
                        var observed=arena.RenderedCels;
                        bool ok=observed.Length==2&&observed.All(x=>x==expected);pass&=ok;
                        checks.Add(new{fighter=id,move=move.Id,phase,frame,expected,observed,passed=ok});
                        if(phase=="active"&&(move.Id is "s_lp" or "s_hp" or "c_hk" or "j_hk" or "throw_forward" or "throw_back"||move.Id.StartsWith("super")||move.Id.EndsWith("_ex")))
                        {
                            string name=$"{id}-{move.Id}-{phase}.png";await ArtCapture(name);captures.Add(name);
                        }
                    }
                }
                foreach(var state in atlas.RootElement.GetProperty("states").EnumerateObject())
                {
                    var f=new FighterRenderState{Id=id,State=state.Name,X=300000,Facing=1};
                    var mirror=new FighterRenderState{Id=id,State=state.Name,X=468000,Facing=-1,Palette=1};
                    arena.SetState(new ArenaRenderState{Tick=tick+=6,Fighters=[f,mirror],TrainingGrid=true,ShakeScale=0});
                    title.Text=$"{id.ToUpperInvariant()} / {state.Name.ToUpperInvariant()}";
                    detail.Text="Universal pose binding · mirrored root pivots · calibration room";
                    await UiFrames(4);
                    var valid=state.Value.EnumerateArray().Select(x=>x.GetString()).ToArray();
                    var observed=arena.RenderedCels;bool ok=observed.All(valid.Contains);pass&=ok;
                    checks.Add(new{fighter=id,state=state.Name,observed,passed=ok});
                    if(state.Name is "idle" or "walk" or "crouch" or "knockdown" or "parry" or "win")
                    {
                        string name=$"{id}-{state.Name}-mirrors.png";await ArtCapture(name);captures.Add(name);
                    }
                }
            }
        }
        catch(Exception e){pass=false;checks.Add(new{error=e.ToString(),passed=false});GD.PushError(e.ToString());}
        var result=new{utc=DateTime.UtcNow,passed=pass,scope="Native rendering fixtures: every move phase and atlas state, both mirror palettes. Gameplay validated separately through actual legal-input showcase and full-match smoke.",checks,captures};
        System.IO.File.WriteAllText(System.IO.Path.Combine(evidenceDir,"art-review.json"),JsonSerializer.Serialize(result,new JsonSerializerOptions{WriteIndented=true}));
        GD.Print($"ART REVIEW {(pass?"PASS":"FAIL")} / {checks.Count} bindings / {captures.Count} screenshots");QuitGame(pass?0:4);
    }
    async Task ArtCapture(string name)
    {
        await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
        using var shot=GetViewport().GetTexture().GetImage();shot.SavePng(System.IO.Path.Combine(evidenceDir,name));
    }
}
