using StrikeLedger.Core;

namespace StrikeLedger.App;

public readonly record struct CommandInput(byte Direction,Buttons Held);

/// <summary>Canonical command grammar to ordinary held input samples; never starts an action directly.</summary>
public static class CommandEncoder
{
    public static IReadOnlyList<CommandInput> Encode(GameContent content,string fighterId,string moveId,int facing=1)
        =>Encode(content,content.Fighters[fighterId].Move(moveId),facing);

    public static IReadOnlyList<CommandInput> Encode(GameContent content,MoveDefinition move,int facing=1)
    {
        if(facing is not(-1 or 1))throw new ArgumentOutOfRangeException(nameof(facing));
        var output=new List<CommandInput>();
        void Add(byte direction,Buttons held=Buttons.None)=>output.Add(new(CoreMath.RelativeDirection(direction,facing),held));
        Add(5); // A fresh release makes chords deterministic when the previous action held buttons.
        var parts=move.Command.Split('+');string prefix=parts[0];
        byte? airDirection=null;if(prefix.StartsWith("air:",StringComparison.Ordinal)){string suffix=prefix[4..];if(byte.TryParse(suffix,out byte air)&&air is >=1 and <=9){airDirection=air;prefix="J";}else{parts=(suffix+"+"+string.Join("+",parts.Skip(1))).TrimEnd('+').Split('+');prefix=parts[0];airDirection=5;}}Buttons buttons=Buttons.None;
        int firstButton=prefix is "LP" or "MP" or "HP" or "LK" or "MK" or "HK"?0:1;
        foreach(string token in parts.Skip(firstButton))buttons|=token switch
        {
            "P"=>Buttons.LP,"K"=>Buttons.LK,"PP"=>Buttons.LP|Buttons.MP,"KK"=>Buttons.LK|Buttons.MK,
            _=>Enum.TryParse<Buttons>(token,false,out var b)&&Enum.IsDefined(b)&&b!=Buttons.None?b:throw new InvalidDataException("Unsupported command button: "+token)
        };
        byte final=5;
        if(content.Inputs.MotionPatterns.TryGetValue(prefix,out var pattern))
        {foreach(var d in pattern.Take(pattern.Length-1))Add((byte)d);final=(byte)pattern[^1];}
        else if(prefix is "charge_back" or "charge_down")
        {for(int i=0;i<content.Inputs.ChargeHoldTicks;i++)Add(prefix=="charge_back"?(byte)4:(byte)2);final=prefix=="charge_back"?(byte)6:(byte)8;}
        else if(prefix=="J")final=5; // Caller must provide an airborne actor; this does not manufacture jumping state.
        else if(prefix=="C")final=2;
        else if(prefix=="S"||prefix=="CLOSE"||prefix=="5"||firstButton==0)final=5;
        else if(byte.TryParse(prefix,out byte direction)&&direction is >=1 and <=9)final=direction;
        else throw new InvalidDataException("Unsupported canonical command: "+move.Command);
        if(airDirection.HasValue)final=airDirection.Value;
        Add(final,buttons);
        return output;
    }
}
