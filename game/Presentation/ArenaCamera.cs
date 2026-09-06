namespace StrikeLedger.Presentation;

/// <summary>Pure presentation fit. Camera never constrains legal simulation positions.</summary>
public readonly record struct CameraSubject(int X,int Y,float Left,float Right,float Top,float Bottom);
public readonly record struct CameraFrame(float WorldX,float Zoom)
{
    public const float BaseUnits=1280f/480000f,Floor=604,SafeLeft=40,SafeRight=1240,SafeTop=174,SafeBottom=664;
    public float Units=>BaseUnits*Zoom;
    public float ScreenX(float x)=>640+(x-WorldX)*Units;
    public float ScreenY(float y)=>Floor-y*Units;
    public static CameraFrame Fit(IReadOnlyList<CameraSubject> subjects)
    {
        if(subjects.Count==0)return new(384000,1);
        float left=float.MaxValue,right=float.MinValue,top=0,bottom=0;
        foreach(var s in subjects)
        {
            left=Math.Min(left,s.X*BaseUnits+s.Left);right=Math.Max(right,s.X*BaseUnits+s.Right);
            top=Math.Max(top,s.Y*BaseUnits+s.Top);bottom=Math.Max(bottom,s.Bottom-s.Y*BaseUnits);
        }
        float zoom=Math.Min(1,(SafeRight-SafeLeft)/Math.Max(1,right-left));
        zoom=Math.Min(zoom,(Floor-SafeTop)/Math.Max(1,top));
        if(bottom>0)zoom=Math.Min(zoom,(SafeBottom-Floor)/bottom);
        // Legal content fits above .35; continue fitting malformed/outlying visual
        // inputs instead of clipping them. No history, interpolation or wall time.
        zoom=Math.Clamp(zoom,.1f,1);
        float midpoint=(float)subjects.Average(s=>(double)s.X);
        float nominal=Math.Clamp(midpoint,240000,528000)*BaseUnits;
        float minCenter=right-(SafeRight-640)/zoom,maxCenter=left+(640-SafeLeft)/zoom;
        float center=Math.Clamp(nominal,Math.Min(minCenter,maxCenter),Math.Max(minCenter,maxCenter));
        return new(center/BaseUnits,zoom);
    }
}
