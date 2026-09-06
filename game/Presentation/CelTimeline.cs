namespace StrikeLedger.Presentation;

public readonly record struct CelHold(string Cel,int Ticks);
public static class CelTimeline
{
    public static string Select(IReadOnlyList<CelHold> clip,int tick,bool loop=false)
    {
        if(clip.Count==0)throw new ArgumentException("Empty animation clip");
        int duration=clip.Sum(c=>Math.Max(1,c.Ticks));
        tick=loop?Math.Max(0,tick)%duration:Math.Clamp(tick,0,duration-1);
        foreach(var cel in clip){if(tick<Math.Max(1,cel.Ticks))return cel.Cel;tick-=Math.Max(1,cel.Ticks);}
        return clip[^1].Cel;
    }
}
