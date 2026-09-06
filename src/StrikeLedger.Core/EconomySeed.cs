using System.Text.Json;
namespace StrikeLedger.Core;

// Arithmetic seed only. No actual fight state, command recognition or network coordinator.
public sealed record EconomyRules(int StartingCredits,int Cap,int WinPayout,int DrawPayout,int[] LossPayouts)
{
    public static EconomyRules Load(string path)
    {
        using var doc=JsonDocument.Parse(File.ReadAllText(path)); var j=doc.RootElement;
        var r=new EconomyRules(j.GetProperty("starting_credits").GetInt32(),j.GetProperty("wallet_cap").GetInt32(),
            j.GetProperty("win_payout").GetInt32(),j.GetProperty("draw_payout").GetInt32(),
            j.GetProperty("loss_payouts").EnumerateArray().Select(x=>x.GetInt32()).ToArray());
        if(r.Cap<=0 || r.Cap>1000000 || r.StartingCredits<0 || r.StartingCredits>r.Cap || r.WinPayout<0 || r.DrawPayout<0 || r.LossPayouts.Length!=3 || r.LossPayouts.Any(x=>x<0))
            throw new InvalidDataException("Invalid economic rules");
        return r;
    }
}
public readonly record struct Wallet
{
    public int Credits { get; }
    public int RecoveryTier { get; }
    public Wallet(int credits,int recoveryTier,EconomyRules rules)
    {
        if(credits<0 || credits>rules.Cap || recoveryTier is <0 or >2) throw new ArgumentOutOfRangeException(nameof(credits));
        Credits=credits; RecoveryTier=recoveryTier;
    }
}
public enum Outcome { Win, Loss, Draw }
public enum ActivationStatus { Paid, Free, Illegal, Insufficient, Reserve, Duplicate, Licensed, SuperUse, Locked, Exhausted }
public sealed record SpendReceipt(string Key,string MoveId,int Cost);
public sealed record PayoutReceipt(int Nominal,int Granted,int Clipped,int OldTier,int NewTier);
public static class EconomySeed
{
    public static (Wallet Wallet,PayoutReceipt Receipt) Payout(Wallet wallet,Outcome result,EconomyRules rules)
    {
        int amount,tier;
        switch(result)
        {
            case Outcome.Win: amount=rules.WinPayout;tier=Math.Max(0,wallet.RecoveryTier-1);break;
            case Outcome.Loss: amount=rules.LossPayouts[wallet.RecoveryTier];tier=Math.Min(2,wallet.RecoveryTier+1);break;
            case Outcome.Draw: amount=rules.DrawPayout;tier=wallet.RecoveryTier;break;
            default: throw new ArgumentOutOfRangeException(nameof(result));
        }
        int grant=Math.Min(amount,rules.Cap-wallet.Credits);
        return (new Wallet(checked(wallet.Credits+grant),tier,rules),new PayoutReceipt(amount,grant,amount-grant,wallet.RecoveryTier,tier));
    }
    public static string SinglesDecision(int a,int b,int rounds)
    {
        if(rounds is <0 or >9 || a<0 || b<0 || (long)a+b!=2*rounds)throw new ArgumentException("Point conservation failed");
        if(a>=10 && a>b)return "A";
        if(b>=10 && b>a)return "B";
        return rounds==9 ? (a>b?"A":b>a?"B":"DRAW") : "CONTINUE";
    }
}
public sealed class SpendSnapshot
{
    public Wallet Wallet { get; }
    public int ReserveFloor { get; }
    private readonly SpendReceipt[] _receipts;
    public IReadOnlyList<SpendReceipt> Receipts => Array.AsReadOnly(_receipts);
    public SpendSnapshot(Wallet wallet,int floor,IEnumerable<SpendReceipt>? receipts=null)
    {
        if(floor<0 || floor>wallet.Credits)throw new ArgumentOutOfRangeException(nameof(floor));
        Wallet=wallet;ReserveFloor=floor;_receipts=receipts?.ToArray()??Array.Empty<SpendReceipt>();
        if(_receipts.Any(x=>x is null || x.Cost<=0 || string.IsNullOrWhiteSpace(x.Key) || x.Key.Length>128 || string.IsNullOrWhiteSpace(x.MoveId) || x.MoveId.Length>128)) throw new ArgumentException("Invalid receipt");
        if(_receipts.Select(x=>x.Key).Distinct().Count()!=_receipts.Length)throw new ArgumentException("Duplicate receipts");
    }
    public (SpendSnapshot State,ActivationStatus Status) Activate(string moveId,int cost,string key,bool legal,EconomyRules rules)
    {
        if(string.IsNullOrWhiteSpace(moveId)||moveId.Length>128||string.IsNullOrWhiteSpace(key)||key.Length>128||cost<0||cost>rules.Cap)
            throw new ArgumentException("Bad action cost/id");
        var existing=_receipts.FirstOrDefault(x=>x.Key==key);
        if(existing is not null)
        {
            if(existing.MoveId!=moveId||existing.Cost!=cost)throw new InvalidOperationException("Receipt id conflict");
            return(this,ActivationStatus.Duplicate);
        }
        if(!legal)return(this,ActivationStatus.Illegal);
        if(cost>Wallet.Credits)return(this,ActivationStatus.Insufficient);
        if(Wallet.Credits-cost<ReserveFloor)return(this,ActivationStatus.Reserve);
        if(cost==0)return(this,ActivationStatus.Free);
        var updated=new Wallet(Wallet.Credits-cost,Wallet.RecoveryTier,rules);
        var receipts=_receipts.Append(new SpendReceipt(key,moveId,cost));
        return(new SpendSnapshot(updated,ReserveFloor,receipts),ActivationStatus.Paid);
    }
}
