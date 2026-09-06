# Shop-Only Economy Rework v2

The user requested this package after the expanded audit and Buyables Design v1. These rules supersede conflicting resource semantics in the original scaffold and prior design packages. Mechanical moves, defensive rules, original artwork, deterministic simulation, controller and camera repairs remain in scope.

The complete pre-v2 working source, registries, runnable test harnesses and their evidence are preserved at `reports/evidence/pre-shop-v2-checkpoint/pre-shop-v2-checkpoint.zip` (SHA-256 `2ab5ac246facea4480bad36ff56050979342a0ac2e60ec62359c7dcd8322b36a`). Its direct-spend measurements are historical evidence and cannot establish v2 balance.

## Active contract

Each player starts with600 credits. The bank cap is3600 and one shop can spend at most2400. Players may select one signature, one technique and one gambit rental; up to two specific EX family licenses; and zero or one super permit. All equipment expires at the next round regardless of winner. Fighters remain selected for the match; super choice comes from each round's purchased permit. No purchase is valid and the ordinary six-button kit and free defense remain available.

EX licenses cost600, or900 for the vertical reversal. They permit repeat use after ordinary input, state, charge, recovery, cancel and object-occupancy checks. They add no ammunition, special cooldown or extra input assistance. A super costs900/1200/1500 by registry and has one legal startup. Whiff, block, parry or interruption after that startup never restores its use. Illegal or merely buffered input consumes nothing.

The bank remains unchanged throughout combat. Neither bank nor pending earnings participates in action authorization. Input recognition includes locked EX and super syntax before checking ownership, so a rejected enhanced command cannot fall through to a normal or acquire a release-edge attack. Both carts validate before either bank or capability is mutated. Timeout uses the explicit last-valid cart; duplicate commits are idempotent and altered payloads are rejected.

## Skill receipts

Counter-hit grants50, grounded anti-air75 and precision parry100, each at most twice per player per round and300 combined. Partial final grants are recorded exactly. The production collision resolver captures both actors' pre-contact state after movement, resolves arbitration, then supplies immutable accepted facts to the production reward ledger. A direct damaging opener must interrupt offensive startup for CH, or hit a voluntarily airborne opponent from grounded nonprejump state for AA. Pre-existing combo, stun, capture, knockdown and dizzy exclude both. AA takes priority without a fallback if capped. Armor absorption, projectiles, fields and throws cannot produce offensive awards.

Precision parry classifies the existing manual defense at eligible defense-clock ages0/1. Age2 may still parry normally without income. An input edge entered during freeze remains bonus-ineligible on thaw. Genuine enemy damaging projectiles and fields may be manually parried; automatic counters, interception, reflection and parrying one's own reflected original attack do not earn precision income.

Root attack identity is session, round, original owner and originating action ordinal. All branches and spawned objects retain the root across reflection. Offensive and defensive decisions consume separate per-root keys even when capped. The first actual direct damaging opener also consumes its opportunity when ineligible; a later branch cannot manufacture a new opener. Identical fact duplicates are ignored, conflicting identities fail closed, and tick/earner/root/contact order is deterministic. These stricter first-opener and capped-root details follow the written package contract where its Python oracle omits storage. Round-local facts, roots, clocks, counts and receipts serialize and roll back. Training reports opportunities without competitive earnings or an ever-growing ledger.

Only confirmed settlement deposits outcome and skill income, with result payout first and explicit clipping. Win1200, draw900, and losses1200/1200/1500 use the old recovery tier before it changes. The final-round receipt identifies unused postmatch funds; a rematch starts again at600. Aborts have no unsettled payout. Skill rewards are an original balance proposal, not a claim about another game's exact parry timing.

## Verification boundary

The supplied archive's76 manifested files and95 reference/tool tests passed. They establish package integrity and oracle arithmetic, not native gameplay completion. Current production implementation, replay/network tests, native UI, export, balance and audit dispositions are tracked separately in `audit/upgrade-2026-09-05` and `reports/evidence`. Physical controllers, a second physical PC, and owner/friend playtest approval require actual evidence and must remain pending when unavailable.

The source archive is retained unchanged under `audit/requests/penny-punchers-shop-only-economy-rework-v2`. Historical audit conclusions keep their PP identifiers; only conflicting resource expectations are superseded. The shipping path must enable v2, including all installed buyables, rather than leaving the requested rules behind an inactive flag.
