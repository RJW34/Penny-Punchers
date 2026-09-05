namespace StrikeLedger.Core;
public static class CoreMath
{
    public static long TruncDiv(long numerator,long denominator)
    {
        if(denominator==0) throw new DivideByZeroException();
        return checked(numerator/denominator); // C# signed integer division truncates toward zero.
    }
    public static int ScaledDamage(int damage,int index,bool counterHit=false)
    {
        if(damage is <0 or >10000 || index is <0 or >1000) throw new ArgumentOutOfRangeException(nameof(damage));
        long baseDamage=TruncDiv((long)damage*(counterHit?110:100),100);
        return checked((int)TruncDiv(baseDamage*Math.Max(30,100-10*index),100));
    }
    public static int HealthVerdict(int a,int aMax,int b,int bMax)
    {
        if(aMax<=0 || bMax<=0 || a<0 || a>aMax || b<0 || b>bMax) throw new ArgumentOutOfRangeException(nameof(a));
        return ((long)a*bMax).CompareTo((long)b*aMax); // positive=A, negative=B, zero=draw
    }
    public static byte RelativeDirection(byte raw,int facing)
    {
        if(raw is <1 or >9 || facing is not (1 or -1)) throw new ArgumentOutOfRangeException(nameof(raw));
        if(facing==1)return raw;
        return raw switch {1=>3,3=>1,4=>6,6=>4,7=>9,9=>7,_=>raw};
    }
}
