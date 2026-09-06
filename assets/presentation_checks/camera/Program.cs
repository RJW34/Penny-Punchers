using System.Text.Json;
using StrikeLedger.Presentation;

var root=Path.GetFullPath(args.Length>0?args[0]:".");int checks=0;
var fighterData=args.Length>1?Path.GetFullPath(args[1]):Path.Combine(root,"data","fighters");
void Require(bool value,string message){checks++;if(!value)throw new Exception(message);}
var all=new List<(string Name,float Left,float Right,float Top,float Bottom)>();
foreach(string id in new[]{"Rook","Vale"})
{
    using var d=JsonDocument.Parse(File.ReadAllText(Path.Combine(root,"game","Assets","AfterHours","Fighters",id,"atlas.json")));
    foreach(var p in d.RootElement.GetProperty("cells").EnumerateObject())
    {
        var c=p.Value;var r=c.GetProperty("rect").EnumerateArray().Select(x=>x.GetSingle()).ToArray();var pivot=c.GetProperty("pivot").EnumerateArray().Select(x=>x.GetSingle()).ToArray();float scale=c.GetProperty("scale").GetSingle();
        all.Add((id+"/"+p.Name,-pivot[0]*scale,(r[2]-pivot[0])*scale,pivot[1]*scale,(r[3]-pivot[1])*scale));
    }
}
foreach(var cel in all)foreach(int facing in new[]{-1,1})foreach(int y in new[]{0,130000,300000})foreach(var roots in new[]{(16000,752000),(16000,65000),(703000,752000),(288000,480000)})
{
    var a=new CameraSubject(roots.Item1,y,facing<0?-cel.Right:cel.Left,facing<0?-cel.Left:cel.Right,cel.Top,cel.Bottom);
    var b=new CameraSubject(roots.Item2,0,-155,155,280,10);var subjects=new[]{a,b};var camera=CameraFrame.Fit(subjects);
    foreach(var s in subjects)
    {
        Require(camera.ScreenX(s.X)+s.Left*camera.Zoom>=CameraFrame.SafeLeft-.1f,cel.Name+" left");
        Require(camera.ScreenX(s.X)+s.Right*camera.Zoom<=CameraFrame.SafeRight+.1f,cel.Name+" right");
        Require(camera.ScreenY(s.Y)-s.Top*camera.Zoom>=CameraFrame.SafeTop-.1f,cel.Name+" top");
        Require(camera.ScreenY(s.Y)+s.Bottom*camera.Zoom<=CameraFrame.SafeBottom+.1f,cel.Name+" bottom");
    }
    Require(camera==CameraFrame.Fit(subjects),"Camera depends only on snapshot");
}
CelHold[] clip=[new("anticipation",2),new("hold",7),new("settle",1)];
Require(CelTimeline.Select(clip,0)=="anticipation"&&CelTimeline.Select(clip,1)=="anticipation","First hold has exact two ticks");
Require(CelTimeline.Select(clip,2)=="hold"&&CelTimeline.Select(clip,8)=="hold","Unequal middle hold stays seven ticks");
Require(CelTimeline.Select(clip,9)=="settle"&&CelTimeline.Select(clip,100)=="settle","Non-looping final pose holds");
Require(CelTimeline.Select(clip,10,true)=="anticipation","Loop wraps at declared total");
using var timings=JsonDocument.Parse(File.ReadAllText(Path.Combine(root,"game","Presentation","animation-timing.json")));
foreach(string id in new[]{"rook","vale"})
{
    using var data=JsonDocument.Parse(File.ReadAllText(Path.Combine(fighterData,id+".json")));
    using var atlas=JsonDocument.Parse(File.ReadAllText(Path.Combine(root,"game","Assets","AfterHours","Fighters",id=="rook"?"Rook":"Vale","atlas.json")));
    foreach(var move in data.RootElement.GetProperty("moves").EnumerateArray())
    {
        string name=move.GetProperty("id").GetString()!;var timing=timings.RootElement.GetProperty("fighters").GetProperty(id).GetProperty("moves").GetProperty(name);
        foreach(string phase in new[]{"startup","active","recovery"})
        {
            var c=timing.GetProperty(phase).EnumerateArray().Select(x=>new CelHold(x.GetProperty("cel").GetString()!,x.GetProperty("ticks").GetInt32())).ToArray();
            Require(c.All(x=>x.Ticks>0&&atlas.RootElement.GetProperty("cells").TryGetProperty(x.Cel,out _)),id+" "+name+" valid named holds");
            int duration=move.GetProperty(phase).GetInt32();if(duration==0)continue;
            Require(c.Sum(x=>x.Ticks)==duration,id+" "+name+" "+phase+" holds match canonical interval");
            if(phase=="active"&&atlas.RootElement.GetProperty("moves").TryGetProperty(name,out var authored))Require(c[0].Cel==authored.GetProperty("active")[0].GetString(),id+" "+name+" first active starts exactly with contact cel");
        }
        if(move.TryGetProperty("mimic",out var mimic))
        {
            string source=mimic.GetProperty("source_move_id").GetString()!;
            var real=data.RootElement.GetProperty("moves").EnumerateArray().Single(m=>m.GetProperty("id").GetString()==source);
            string At(JsonElement m,int tick)
            {
                var phases=timings.RootElement.GetProperty("fighters").GetProperty(id).GetProperty("moves").GetProperty(m.GetProperty("id").GetString()!);
                foreach(string phase in new[]{"startup","active","recovery"})
                {
                    int duration=m.GetProperty(phase).GetInt32();if(tick>=duration){tick-=duration;continue;}
                    var holds=phases.GetProperty(phase).EnumerateArray().Select(h=>new CelHold(h.GetProperty("cel").GetString()!,h.GetProperty("ticks").GetInt32())).ToArray();
                    return CelTimeline.Select(holds,tick);
                }
                throw new Exception("Frame outside action");
            }
            int shared=mimic.GetProperty("shared_ticks").GetInt32();
            for(int tick=0;tick<shared;tick++)Require(At(move,tick)==At(real,tick),id+" "+name+" exact threat anticipation tick "+tick);
            Require(At(move,shared)!=At(real,shared),id+" "+name+" abort visibly diverges after shared interval");
        }
    }
}
Console.WriteLine(JsonSerializer.Serialize(new{passed=true,checks,scope="Production camera and explicit cel hold selectors; all source cel envelopes across opposite bounds, near corners, both facings and legal/training heights. This does not claim native visuals or human animation quality."}));
