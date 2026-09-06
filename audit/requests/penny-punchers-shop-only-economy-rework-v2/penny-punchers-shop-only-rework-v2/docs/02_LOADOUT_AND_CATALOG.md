# Purchased capability contracts
`data/catalog.v2.json` is a migration proposal for existing runtime content. `data/candidate_library.v2.json` adapts the earlier 38 design entries without asserting implementation. Prices are numeric seeds; do not overwrite revised local prices blindly. Record adopted values in live canonical data and a migration ledger.

## Purchase semantics
- Each technique licenses an added/replacement move for one round; one per signature/technique/gambit slot.
- Each EX license grants exactly one enhanced family, preserving free normal variants; maximum two distinct EX licenses. Default price 600; enhanced vertical reversal family 900. No activation fee, finite-use EX stock, recharge meter or invented cooldown. Ordinary recovery, charge, projectile limits and legal cancels still apply.
- Each super permit grants one legal startup of that art for one round, price 900/1200/1500 initially. Only one permit total, no duplicate packs. Choose it during each shop, including changing from the last round. No purchased super means no executable super.
- Total shop spend ≤ 2400 and ≤ current bank. All categories compete for that budget. Unspent money is the sole future savings; remove reserve-floor UI/state from v2. These limits are starting design levers.

## Baseline conversion
All 12 live rental records remain, subject to their unresolved audit value findings. All 8 EX moves move from automatic base availability/direct fee to explicit licenses. All 6 live arts become optional prepaid products; legacy third arts stay until any designed replacement is actually implemented and verified. Runtime counts come from registries, not hardcoded 12/26/38 assumptions.

Price translation is not `300 per EX` → `300 for unlimited EX`. Repeatable invincible reversals, projectile pressure and self-cancel loops require sustained-use tests. Raise cost/restrict equipped families or retune the offending move through a decision; do not silently return to startup payments. No automatic per-round EX use cap in this candidate.

## Atomic cart
Use stable product IDs and live data, not array positions. Validate character eligibility, distinct IDs, one action replacement, slot constraints, conflicts, total spend, implementation status and ruleset identity before mutating either player. Price and content hash are quoted in the cart. A duplicate commit with the same identity/payload is idempotent; changed payload with the same key is rejected. Draft edits are free; nothing is charged until lock. No post-lock refund. Maintain an explicit last-valid draft; if time expires, show and commit it (or the visibly announced empty plan if none). Never silently wipe a valid cart because the most recent edit was invalid.

Missing licenses must not let a parser downgrade a PP motion to an ordinary special or normal. Recognize explicit EX/super intent before entitlement gating. Invalid recognition is distinct from a recognized-but-locked move. Neither costs money. Preserve fresh-chord and negative-edge suppression by move category, not by nonzero CreditCost.
