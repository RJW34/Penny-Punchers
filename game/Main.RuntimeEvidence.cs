using Godot;
using StrikeLedger.App;
using System.Diagnostics;
using System.Text.Json;
public partial class Main
{
    readonly Stopwatch runtimeWatch=Stopwatch.StartNew();readonly List<double> renderMs=[];double previousRenderMs;
    void RecordRenderedFrame(double delta){if(!smoke&&!networkEvidence)return;double now=runtimeWatch.Elapsed.TotalMilliseconds;if(previousRenderMs>0)renderMs.Add(now-previousRenderMs);previousRenderMs=now;}
    void WriteRuntimeEvidence(){if(!smoke&&!networkEvidence)return;renderMs.Sort();double Percent(double q)=>renderMs.Count==0?0:renderMs[(int)Math.Min(renderMs.Count-1,Math.Floor(q*renderMs.Count))];var r=new{utc=DateTime.UtcNow,engine=Engine.GetVersionInfo()["string"].AsString(),platform=OS.GetName(),display=DisplayServer.GetName(),gpu=RenderingServer.GetVideoAdapterName(),cpu=OS.GetProcessorName(),logicalCpus=OS.GetProcessorCount(),build=ReplayFormat.Build,content=content.ContentHash,complete=sim?.Phase==StrikeLedger.Core.MatchPhase.MatchOver,freeKit=freeKitEvidence,ticks=sim?.Tick,finalHash=sim?.Hash(),wallets=sim?.Players.Select(p=>p.Credits),scores=sim?.Players.Select(p=>p.ScoreHalfPoints),rounds=sim?.CompletedRounds,frames=renderMs.Count,elapsedSeconds=runtimeWatch.Elapsed.TotalSeconds,frameMs=new{p50=Percent(.5),p95=Percent(.95),p99=Percent(.99),worst=Percent(1)},commands=OS.GetCmdlineArgs(),pacingNote=DisplayServer.GetName()=="headless"?"Headless accelerated simulation; no visual pacing claim":"Actual wall-clock intervals between Godot Process callbacks; movie/fixed-fps flags if present are recorded above"};System.IO.File.WriteAllText(System.IO.Path.Combine(evidenceDir,"runtime-result.json"),JsonSerializer.Serialize(r,new JsonSerializerOptions{WriteIndented=true}));}
}
