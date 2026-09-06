# Combat event ordering and migration field map

## Input → contact → income (production contract)
1. Sample both players at the fixed simulation tick; sample facing consistently. Record legal fresh parry edges and eligible-clock provenance without renderer state.
2. Recognize explicit intent against the character's complete command vocabulary, including locked enhanced families. Resolve command priority. Select the owned round super, or return an explicit locked-super intent; do not pick another art for free.
3. Validate transition/charge/phase/occupancy; validate ownership and available finite use, WITHOUT reading bank or pending earnings. Commit action/ordinal. For a super only, consume its use here. EX repeatable ownership is unchanged.
4. Apply the existing movement, freeze and collision rules. Gather a simultaneous pre-contact snapshot AFTER movement and BEFORE any hit changes health, airborne/ground state, hitstun, move ID, combo count or knockdown. Preserve attacker and defender facing/position used for actual collision.
5. Resolve actual rank, parry, block, throw, damage and contact arbitration. Build immutable accepted facts. A proposed collision removed by priority is not a reward.
6. Compute CH/AA/PP categories using those immutable facts, sort deterministic keys, then apply per-root/per-category/global caps. Append allowed receipts to pending round earnings. No bank change.
7. Determine the terminal outcome after the tick's accepted hits and reward candidates. Tick-of-KO rewards count; future events cannot arrive as spendable bonuses. Network correction can undo the complete tick normally.
8. Only after the final terminal inputs/state agree: apply outcome payout and allowed skill receipts once, bound by bank cap. Publish confirmed receipts to UI/replay. Next round clears equipment/uses/earnings, not saved bank.

## Stable provenance
RootAttackId is (match, round, original owner seat, originating action ordinal). It is not a move name, screenshot, projectile object ID alone or network packet sequence. A legal new attack after returning to neutral creates a new root. Multi-hit attacks, follow-up chains, delayed fields and all projectiles they create retain the originating root. Reflection changes effective owner, never origin/root/lifetime. Parry rewards against one's own reflected attack are excluded. The production serializer needs bounded current/relevant roots; do not grow a global cross-match receipt cache.

Counter-hit reward identifies OFFENSIVE startup by explicit damaging-strike/projectile-emission/throw capability metadata, not any nonempty ActionId. Pre-contact opponent in blockstun/hitstun/capture/dizzy/knockdown/combo is excluded. The first candidate policy deliberately limits the rewarding ATTACK itself to a direct strike even if the interrupted opponent was starting a projectile.

## Field migration
| Old active meaning | New active meaning |
|---|---|
| Move CreditCost, DebitOnStart | No runtime credit-price decision. V2 move debit zero/disabled; Product.ShopPrice owns cost. Legacy reader may preserve old schema separately. |
| ReserveFloor | Removed in v2. Saved bank itself is protected from combat spending. |
| Player SelectedSuper from MatchConfig | RoundLoadout.SuperArtId, optional, determined by cart. Fighter identity remains match-config locked. |
| SpendReceipt on EX/super startup | Activation identity and optional SuperUseConsumed receipt with zero credit movement. |
| CombatEvent.Detail='counter' alone | Typed prestate/provenance plus final accepted result for reward classification. Existing detail remains presentation only. |
| UI canEx = Credits>=300 | Owned EX family + actual input/transition legality. |
| canSuper = Credits>=1500 | Owned art, RemainingUses==1 + actual input/transition legality. |
| Any parry income forbidden | Narrow precision classification grants bounded next-shop receipt; normal legal parry unchanged. |
| Three slot-index arrays | Registry-based selected product IDs, explicit slot/cap/conflict contracts. |
| End-round credits only from outcome | Confirmed result grant and separately auditable earned-skill grant. |

Input suppression requires particular attention: old code keys some paid-input release suppression on `CreditCost>0`. Replace that with explicit EX/super/locked-intent category rules, or a now-zero-cost move can acquire unintended negative-edge behavior. Test PP/KK, held buttons across shop transitions and release after a rejected command.
