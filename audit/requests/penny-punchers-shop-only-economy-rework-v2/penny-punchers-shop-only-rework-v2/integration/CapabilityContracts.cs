// Compile-intended seed; NOT compiled in the packaging host. Adapt into current Core.
// Does not implement contacts, rollback, shop UI or the existing game's full legality rules.
using System;
using System.Collections.Generic;
namespace PennyPunchers.Rework.Seed;

public enum AccessKind { Base, RoundLicense, PrepaidSuper }
public enum StartDecision { Illegal, Locked, Exhausted, Base, Licensed, SuperUse, Duplicate }
public sealed record Product(string Id, string Fighter, string Slot, int ShopPrice,
    string[] MoveIds, AccessKind Access, string? Replaces = null);
public sealed record OwnedMove(string MoveId, AccessKind Access);
public sealed record SkillReceipt(string Id, long Tick, string Category, int Nominal, int Allowed);
public sealed record PurchaseReceipt(string Id, int Round, int OpeningBank, int ShopCost, int ClosingBank);
public sealed record RoundSettlement(string Id, int BaseNominal, int BaseGranted,
    int SkillEarned, int SkillGranted, int Clipped, int ClosingBank);

/// Authorization deliberately has NO bank, credit price or earnings argument.
public sealed class CapabilityAuthorizer
{
    private readonly Dictionary<string, OwnedMove> owned;
    private readonly Dictionary<string, string> starts = new(StringComparer.Ordinal);
    public int SuperUsesRemaining { get; private set; }
    public CapabilityAuthorizer(IEnumerable<OwnedMove> moves, int superUses)
    {
        if(superUses is <0 or >1) throw new ArgumentOutOfRangeException(nameof(superUses));
        owned = new(StringComparer.Ordinal);
        int supers = 0;
        foreach(var move in moves)
        {
            if(string.IsNullOrWhiteSpace(move.MoveId) || !owned.TryAdd(move.MoveId, move))
                throw new ArgumentException("Duplicate/empty owned move");
            if(move.Access == AccessKind.PrepaidSuper) supers++;
        }
        if(supers>1 || superUses>0 && supers!=1) throw new ArgumentException("One selected super maximum");
        SuperUsesRemaining=superUses;
    }
    public StartDecision Commit(string moveId, string actionId, bool fightPhase, bool legalTransition)
    {
        if(string.IsNullOrWhiteSpace(actionId)) throw new ArgumentException("Action identity required");
        if(starts.TryGetValue(actionId,out string? prior))
        {
            if(prior != moveId) throw new InvalidOperationException("Conflicting startup identity");
            return StartDecision.Duplicate;
        }
        if(!fightPhase || !legalTransition) return StartDecision.Illegal;
        if(!owned.TryGetValue(moveId,out var move)) return StartDecision.Locked;
        if(move.Access==AccessKind.PrepaidSuper && SuperUsesRemaining==0) return StartDecision.Exhausted;
        if(move.Access==AccessKind.PrepaidSuper) SuperUsesRemaining--;
        starts.Add(actionId,moveId);
        return move.Access switch { AccessKind.Base=>StartDecision.Base,
            AccessKind.RoundLicense=>StartDecision.Licensed, _=>StartDecision.SuperUse };
    }
    // Production snapshots must serialize ownership, starts/ordinals and remaining uses.
    // Do not embed this unbounded illustrative dictionary without the Core's history bounds.
}

public sealed record ContactFacts(bool Accepted, bool Training, string Outcome,
    string AttackKind, int Earner, int OriginOwner, int EffectiveOwner, int Damage, int ThreatDamage,
    bool AttackerGrounded, bool AttackerPrejump, bool DefenderAirborne, bool VoluntaryAir,
    bool DefenderDisabled, bool DefenderInCombo, bool OffensiveStartup,
    bool FreshManualParry, bool FrozenParryEdge, int ParryEligibleAge);

public static class SkillClassification
{
    // Result only selects a candidate category. Separate canonical ledger handles caps/dedupe.
    public static string? Classify(ContactFacts f, int precisionWindow=2)
    {
        if(precisionWindow<1) throw new ArgumentOutOfRangeException(nameof(precisionWindow));
        if(!f.Accepted || f.Training) return null;
        if(f.Outcome=="parry")
            return f.ThreatDamage>0 && (f.AttackKind is "direct" or "projectile" or "field") &&
                f.OriginOwner!=f.Earner && f.EffectiveOwner==1-f.Earner &&
                f.FreshManualParry && !f.FrozenParryEdge && f.ParryEligibleAge>=0 &&
                f.ParryEligibleAge<precisionWindow ? "perfect_parry" : null;
        if(f.Outcome!="hit" || f.Damage<=0 || f.AttackKind!="direct" ||
            f.EffectiveOwner!=f.Earner || f.OriginOwner!=f.Earner ||
            f.DefenderDisabled || f.DefenderInCombo) return null;
        if(f.AttackerGrounded && !f.AttackerPrejump && f.DefenderAirborne && f.VoluntaryAir)
            return "anti_air";
        return f.OffensiveStartup ? "counter_hit" : null;
    }
}
