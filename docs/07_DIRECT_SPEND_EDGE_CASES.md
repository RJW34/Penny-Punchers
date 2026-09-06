# Shop-only ownership, reward and rollback edge cases

This retained filename is a documentation compatibility path. Its former direct-spend contract is superseded by the user-approved shop-only v2 rules in `21_SHOP_ONLY_V2.md`. Tests must execute the production Core, not only reference arithmetic.

| Case | Required behavior |
|---|---|
| Owned EX with zero bank | Legal startup is repeatable; bank stays zero and no ammunition is consumed |
| Unowned EX with a full bank | Reject startup without a weaker fallback or automatic purchase |
| Purchased unused super | Legal startup consumes exactly one use and changes no bank credits |
| Absent or exhausted super permit | Reject startup without another art substituting for it |
| Command during ineligible state | Only a legal buffer may later start; buffer expiry consumes nothing |
| Button release completes EX/super input | No negative-edge paid-category startup |
| Accepted super is interrupted, whiffs, is blocked or parried | The use stays consumed; no refund |
| Owned EX canceled to owned unused super | The EX license persists; the super consumes its one use |
| Rental replaces a base action | Resolve the equipped replacement with no duplicate or unavailable fallback |
| Cart has a duplicate, wrong fighter, wrong price/hash, excess slot/count/cost or nonzero reserve | Reject before mutation; both players' commit remains atomic |
| Duplicate preparation/result key | Return the original receipt without charging or paying twice |
| Shop deadline follows an invalid edit | Commit the preceding valid cart, including empty if no valid edit was made |
| Rollback removes a predicted startup | Restore ownership/use receipts and command state together; bank remains fixed |
| Rollback removes a predicted skill contact | Restore pending ledger and root/category caps; no confirmed reward cue or cash adjustment |
| Late parry prevents predicted KO | No score or settlement income before the corrected terminal tick is confirmed |
| Counter-hit/anti-air/precision-parry opportunity | Classify actual pre-contact facts, root provenance and eligible clocks; append capped pending reward only |
| Same root or own-origin reflected projectile | Enforce root deduplication and own-origin reward exclusion through reflection/branches |
| Category or round skill cap reached | Record nominal, allowed and capped amounts; no excess pending credit |
| Bank cap clips settlement | Record earned/granted/clipped amounts with no overflow |
| Final settlement | Record final bank truthfully; label it unused because no next shop exists |
| Client sends a bonus, balance, refund or transfer command | Reject; only inputs and validated coordinator operations are accepted |
| Same-tick actions/contact rewards | Resolve from the common contact snapshot, with no seat-order economic advantage |

Action roots include session, round, origin seat and action ordinal. Super-use and skill receipts are deterministic, bounded and rollback-restorable. Reflected projectiles retain their original root even when their current owner changes. Presentation may show provisional movement cues, but use/reward/contact/result facts enter public history only after confirmation.

Only preparation/result transactions change banks. Peers derive all combat capabilities, pending rewards and receipt amounts through the same Core. A client never submits a trusted “I earned 100” or resulting wallet. Historical direct-spend evidence is preserved separately and cannot prove these rules.
