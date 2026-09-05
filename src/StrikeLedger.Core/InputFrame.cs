namespace StrikeLedger.Core;
[Flags]
public enum Buttons : byte { None=0, LP=1, MP=2, HP=4, LK=8, MK=16, HK=32 }
// No duel/team id, analog shield, meter command, or device polling in the core.
public readonly record struct InputFrame
{
    public int SeatId { get; }
    public long Frame { get; }
    public byte Direction { get; }
    public Buttons Held { get; }
    public InputFrame(int seatId, long frame, byte direction, Buttons held)
    {
        if (seatId is <0 or >1 || frame<0 || direction is <1 or >9 || ((byte)held & ~63)!=0)
            throw new ArgumentOutOfRangeException(nameof(seatId), "Invalid two-seat digital input");
        SeatId=seatId; Frame=frame; Direction=direction; Held=held;
    }
    public Buttons PressedSince(InputFrame previous) => Held & ~previous.Held;
    public Buttons ReleasedSince(InputFrame previous) => previous.Held & ~Held;
}
