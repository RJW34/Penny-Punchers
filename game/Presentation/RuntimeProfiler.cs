using Godot;
using System.Diagnostics;
using System.Text.Json;

namespace StrikeLedger.Presentation;

/// <summary>Opt-in diagnostics; reads elapsed time without changing simulation inputs.</summary>
public static class RuntimeProfiler
{
    public static readonly bool Enabled=OS.GetCmdlineUserArgs().Contains("--profile");
    static readonly Dictionary<string,List<double>> samples=new();
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
        System.IO.File.WriteAllText(System.IO.Path.Combine(folder,"scope-profile.json"),JsonSerializer.Serialize(new{utc=DateTime.UtcNow,scope="Wall time inside actual native C# callbacks; includes OS scheduling, excludes GPU work after draw command submission.",measurements=result},new JsonSerializerOptions{WriteIndented=true}));
    }
}
