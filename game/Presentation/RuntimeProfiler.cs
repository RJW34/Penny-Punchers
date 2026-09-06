using Godot;
using System.Diagnostics;
using System.Text.Json;

namespace StrikeLedger.Presentation;

/// <summary>Opt-in diagnostics; reads elapsed time without changing simulation inputs.</summary>
public static class RuntimeProfiler
{
    public static readonly bool Enabled=OS.GetCmdlineUserArgs().Contains("--profile");
    static readonly Dictionary<string,List<double>> samples=new();
    static readonly List<object> stalls=new();
    static readonly Dictionary<string,List<double>> frames=new();
    static long lastFrame;static string lastPhase="startup";static int[] lastGc=[0,0,0];
    public static void MarkFrame(string phase,long tick)
    {
        if(!Enabled)return;
        long now=Stopwatch.GetTimestamp();int[] gc=[GC.CollectionCount(0),GC.CollectionCount(1),GC.CollectionCount(2)];
        if(lastFrame!=0)
        {
            double ms=(now-lastFrame)*1000d/Stopwatch.Frequency;
            if(!frames.TryGetValue(lastPhase,out var values))frames[lastPhase]=values=new();
            if(values.Count<100000)values.Add(ms);
            if(ms>=40&&stalls.Count<1000)stalls.Add(new{phase=lastPhase,nextPhase=phase,tick,milliseconds=ms,gcCollections=gc.Zip(lastGc,(a,b)=>a-b).ToArray(),managedBytes=GC.GetTotalMemory(false),movieCapture=OS.GetCmdlineArgs().Contains("--write-movie")});
        }
        lastFrame=now;lastPhase=phase;lastGc=gc;
    }
    public readonly struct Scope(string? name,long start):IDisposable
    {
        public void Dispose()
        {
            if(name==null)return;
            if(!samples.TryGetValue(name,out var values))samples[name]=values=new();
            if(values.Count<100000)values.Add((Stopwatch.GetTimestamp()-start)*1000d/Stopwatch.Frequency);
        }
    }
    public static Scope Measure(string name)=>Enabled?new(name,Stopwatch.GetTimestamp()):default;
    public static void Save(string folder)
    {
        if(!Enabled)return;
        var result=samples.ToDictionary(p=>p.Key,p=>{
            var a=p.Value.Order().ToArray();double Q(double q)=>a.Length==0?0:a[Math.Clamp((int)Math.Ceiling(a.Length*q)-1,0,a.Length-1)];
            return new {count=a.Length,meanMs=a.Length==0?0:a.Average(),p50Ms=Q(.5),p95Ms=Q(.95),p99Ms=Q(.99),maxMs=Q(1)};
        });
        System.IO.Directory.CreateDirectory(folder);
        var phaseFrames=frames.ToDictionary(p=>p.Key,p=>new{count=p.Value.Count,under20=p.Value.Count(v=>v<20),ms20to40=p.Value.Count(v=>v>=20&&v<40),ms40to100=p.Value.Count(v=>v>=40&&v<100),over100=p.Value.Count(v=>v>=100),maxMs=p.Value.DefaultIfEmpty().Max()});
        System.IO.File.WriteAllText(System.IO.Path.Combine(folder,"scope-profile.json"),JsonSerializer.Serialize(new{utc=DateTime.UtcNow,scope="Wall time inside actual native C# callbacks; includes OS scheduling, excludes GPU work after draw command submission. Phase transitions and GC deltas locate stalls; absence of GC is not proof of GPU/OS cause.",measurements=result,phaseFrames,stalls},new JsonSerializerOptions{WriteIndented=true}));
    }
}
