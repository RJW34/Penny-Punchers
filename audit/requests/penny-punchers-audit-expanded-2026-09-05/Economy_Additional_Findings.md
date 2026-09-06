# New economy findings — PP-078–PP-091

These supplement the original backlog. Source review, arithmetic and design risks are not native game reproductions.

## PP-078 — Global character-stat purchases are absent and should not be silently introduced

**P1 · design_risk · Stat-upgrade policy**

**Basis.** The complete current 12-item catalog leases actions, not health, global damage, movement speed, defense windows, or income. Different move-specific values are not fighter-wide stat buffs. Adding passive products changes the original no-stat-boost contract.

**Action.** Keep global paid stats out of the first balanced prototype. Treat any future stat sidegrade as a separately approved rules experiment with an explicit downside, duration, visibility, stacking policy, and replay identity.

**Acceptance.** Catalog/runtime validation distinguishes action properties from global modifiers. No hidden upgrade stacks, permanent rental effects, or altered core defense is introduced. Any approved variant has separate balance results.

**Related original findings:** PP-014, PP-019.

**Source paths:** `data/items.json`; `src/StrikeLedger.Core/GameContent.cs`; `src/StrikeLedger.Core/Simulation.cs`; `docs/00_PRODUCT_CONTRACT.md`.

## PP-079 — Vale Long Check needs a demonstrated advantage over free Long Heel

**P1 · design_risk · Purchase value**

**Basis.** The 600-credit replacement has 14/3/19 timing, 85 damage and 63-unit hitbox width versus the free move's 10/3/17, 100 damage and 70 width. It loses advancing movement and kara eligibility. It gains 30 stun and lower contact geometry, so strict dominance is not proved.

**Action.** Give it a reproducible anti-poke, geometry, stun-conversion, or spacing niche; redesign or replace it if that niche fails. Explain its tradeoff and do not sell it as simply longer range.

**Acceptance.** Matched legal-input trials show where the replacement earns its 600 cost, where the free move is better, and how an opponent responds. Include the lost 6+HP/kara role.

**Related original findings:** PP-008, PP-014.

**Source paths:** `data/fighters/vale.json`; `data/items.json`; `src/StrikeLedger.Core/Simulation.Input.cs`.

## PP-080 — Returning Pulse is a one-contact returning threat with owner-wide projectile constraints

**P1 · design_risk · Purchase value**

**Basis.** Returning Pulse has hits=1 and turn_after_ticks=45. Contact is consumed on hit, block, or parry. Its owner's active-projectile check can also prevent ordinary/EX/super projectile starts; it is not a guaranteed two-hit boomerang or unrestricted layering tool.

**Action.** Validate whiff-return setup value, charge-independent access, and the opportunity cost of blocking other projectile moves. Describe the exact behavior on the card before repricing it.

**Acceptance.** Legal traces cover outbound hit/block/parry removal, return after a miss, crossings, and attempted ordinary/EX/selected-super projectiles while it remains active. Claimed tactical benefits are actually reproduced.

**Related original findings:** PP-008, PP-014.

**Source paths:** `data/fighters/vale.json`; `src/StrikeLedger.Core/Simulation.Input.cs`; `src/StrikeLedger.Core/Simulation.Combat.cs`.

## PP-081 — Selected-super prices create major access differences not justified by slot order

**P1 · design_risk · Super economics**

**Basis.** At the 3600 wallet cap, 900/1200/1500 prices finance 4/3/2 activations if there is no other spending. Static damage is not monotone with price: Rook's all-hit zero-index scaled payload screen gives 240/266/202. This arithmetic is not engine connection/damage evidence.

**Action.** Measure every art's actual confirm, reversal, reach, chip, positioning, and counterplay value at real budgets. Do not require identical damage or assume higher-numbered art means greater value.

**Acceptance.** All six arts have exact-threshold, paid-confirm, block/parry, reversal, whiff and late-round bank tests. Price changes cite outcomes and preserve one-wallet direct spending.

**Related original findings:** PP-004, PP-014, PP-018.

**Source paths:** `data/fighters/rook.json`; `data/fighters/vale.json`; `data/economy.json`; `data/combat.json`.

## PP-082 — A 300-credit opening award gap can cross a decisive affordability threshold

**P1 · verification_gap · Round-one fairness**

**Basis.** Equal first-round expenditure produces wallets 1800/1500, 1500/1200 or 1200/900. The extra 300 can permit a selected super or a super-plus-EX budget that the trailer lacks. No measured conversion effect is available.

**Action.** Run score-preserving round-two interventions with current wallets, trailer +300 and leader -300, across all selected arts and relevant rental plans.

**Acceptance.** Report paired changes in round-two and eventual-match win probability with uncertainty. An accounting pass or a small nominal money difference is not accepted as proof of competitive fairness.

**Related original findings:** PP-018, PP-019.

**Source paths:** `data/economy.json`; `src/StrikeLedger.Core/EconomySeed.cs`; `src/StrikeLedger.Core/Simulation.cs`.

## PP-083 — Recovery may restore liquidity only after severe score disadvantage

**P1 · design_risk · Comeback timing**

**Basis.** Consecutive losses use old-tier payouts 900,1200,1500. In the explicit equal-600-spending repeated-win ledger, cash becomes equal only after the trailer is 0-3 down. Later income does not restore lost points; terminal payouts cannot help the completed match.

**Action.** Measure time to useful comeback liquidity on continuing-round entries. Test earlier parity without forcing a 50% match chance after a earned score deficit.

**Acceptance.** Analyze 0-1, 0-2, 0-3 and 0-4 situations separately, excluding match-over payouts from resource availability. Use the score-only benchmark and matchup-specific controls.

**Related original findings:** PP-019, PP-020.

**Source paths:** `src/StrikeLedger.Core/EconomySeed.cs`; `src/StrikeLedger.Core/Simulation.cs`; `data/rules.json`.

## PP-084 — Residual spending differences can snowball independently of the win bonus

**P1 · design_risk · Comeback incentives**

**Basis.** Round-two gap equals 300 + loser spend - winner spend. A winner spending 0 and loser spending 600 yields 1800/900; reversed expenditure yields 1200/1500. Efficient offense and expensive failed defense can preserve a much larger bank across rounds.

**Action.** Separate award differences from retained-money differences in telemetry. Test efficient winners against costly failed reversals, parried paid attacks, and repeated losing expenditure.

**Acceptance.** Counterfactuals hold score and spending history fixed where appropriate. Report outcomes, purchased-threat value, and next-round money components instead of attributing all wallet gaps to winning.

**Related original findings:** PP-019, PP-071.

**Source paths:** `data/economy.json`; `src/StrikeLedger.Core/EconomySeed.cs`; `src/StrikeLedger.Core/Simulation.Input.cs`.

## PP-085 — Full-rental plans and combat budgets need explicit opportunity-cost comparisons

**P1 · source_gap · Shop decisions**

**Basis.** Three rental slots cost 1800 in total, which can consume the entire round-two winner wallet after a no-spend opening. The full-rental player then has no funds for enhanced combat despite owning all slots. This can be rational or bad; fullness is not a balance target.

**Action.** Show actual remaining EX/super affordability and compare against a no-rental plan retaining its cash. Keep buying nothing a first-class choice and do not automatically fill slots.

**Acceptance.** Each of 27 sufficient-budget loadouts can be valued against retention of its actual fee. UI reveals when the last purchase removes a reversal or selected-super budget; no separate resource pool is introduced.

**Related original findings:** PP-008, PP-056, PP-070.

**Source paths:** `data/items.json`; `data/economy.json`; `game/Main.Menus.cs`; `game/Main.PrepDetails.cs`.

## PP-086 — Unlimited per-round rental use makes duration and repeat pressure central to value

**P1 · design_risk · Purchase value**

**Basis.** All current rentals have zero activation cost and no use-count limit after their upfront fee. Their value changes with round length, neutral repetitions, and whether possession deters an opponent. Raw damage divided by price is insufficient.

**Action.** Evaluate rental value across short/long rounds and neutral/pressure/defense situations. Keep the same-wallet identity; do not introduce hidden charges or prepaid EX stocks as a balancing shortcut.

**Acceptance.** Record use counts, legal repeated pressure, threat responses, forfeited combat spending, and match pacing. No item is judged good merely because it was frequently activated.

**Related original findings:** PP-014, PP-018, PP-020.

**Source paths:** `data/items.json`; `src/StrikeLedger.Core/Simulation.Input.cs`; `src/StrikeLedger.BalanceLab/Program.cs`.

## PP-087 — Zero-credit legal actions do not prove adequate answers to funded chip and pressure

**P1 · test_required · Combat-economy interaction**

**Basis.** The free kit remains executable and chip can KO. That does not prove the zero-wallet defender has a meaningful answer in each funded pressure situation, especially corners or selected-super threats.

**Action.** Create low-health zero-wallet pressure fixtures for all arts and common EX/rental sequences. Distinguish legitimate losing positions from systemic lack of counterplay.

**Acceptance.** Test real guard/parry/jump/throw/spacing responses with legal inputs and reaction constraints. Document fail cases and their prior causes. Do not require escape from every earned checkmate.

**Related original findings:** PP-018, PP-019, PP-063.

**Source paths:** `data/combat.json`; `src/StrikeLedger.Core/Simulation.Combat.cs`; `data/fighters/rook.json`; `data/fighters/vale.json`.

## PP-088 — Raw first-round-winner conversion is confounded by score and player strength

**P1 · verification_gap · Balance methodology**

**Basis.** A 1-0 leader in an independent 50/50 decisive-round first-to-five converts 163/256 = 63.671875% without any economic advantage. Actual winners may also be stronger players. A 50% conditional target is incorrect.

**Action.** Use paired score-preserving economic interventions and block by player pair, matchup, art and seed. Analyze mirrored correlation and confidence intervals.

**Acceptance.** Reports separate score-only mathematical baselines, observed rates and causal treatment estimates. No balance conclusion uses raw P(match win | round-one win) alone.

**Related original findings:** PP-018, PP-019.

**Source paths:** `data/rules.json`; `src/StrikeLedger.Core/EconomySeed.cs`; `src/StrikeLedger.BalanceLab/Program.cs`; `release_docs/BALANCE_NOTES.md`.

## PP-089 — Earlier parity payout variants need isolated versioned experiments

**P1 · verification_gap · Rules experiments**

**Basis.** The current loss tiers are 900/1200/1500. Candidate 1200/1200/1500 removes the opening award gap at equal spending, but also changes every tier-zero loss and increases currency supply. Equal all-win/loss income is a separate diagnostic, not a proven solution.

**Action.** Keep baseline A, compare candidate B and diagnostic C under unchanged combat data, and record candidate hashes. Test cap clipping, art frequency, deliberate losses/draws and score-aware saving.

**Acceptance.** Complete paired continuations and full-match trials with explicit uncertainty. Do not silently replace shipped payouts or label the new schedule balanced based on arithmetic alone.

**Related original findings:** PP-018, PP-019.

**Source paths:** `data/economy.json`; `src/StrikeLedger.Core/EconomySeed.cs`; `src/StrikeLedger.BalanceLab/Program.cs`.

## PP-090 — A locked reserve can suppress a comeback or final-round cash-out unintentionally

**P1 · design_risk · Reserve usability**

**Basis.** With 1200 credits and a 900 reserve, a 900 super is rejected because only 300 would remain. The saved floor is not money set aside for an available super. It resets per round, but has no future-match value at the decisive end.

**Action.** Label the floor as protected savings, keep it advanced/opt-in, and show executable budget. Warn about remaining protected funds in a decisive round without silently overriding a deliberate choice.

**Acceptance.** Player tests distinguish reserve rejection from insufficient total cash. Exact-threshold tests cover no-rental and rental plans, final-round warnings, replay restoration and explicit unlock policies only if approved.

**Related original findings:** PP-012, PP-056.

**Source paths:** `src/StrikeLedger.Core/Simulation.Input.cs`; `src/StrikeLedger.Core/Simulation.cs`; `game/Main.Menus.cs`.

## PP-091 — Economic telemetry should expose affordability and payout history rather than wallet totals alone

**P1 · source_gap · Balance instrumentation**

**Basis.** A single balance hides whether funds came from saving, nominal recovery, clipping, or an opening award. Later mixed streaks in the analytical equal-spending model can produce a 900 gap, so the opening 300 difference is not a global invariant.

**Action.** Add per-round nominal/granted/clipped payout, old/new tier, lease debit, combat debit, affordable action combinations, and next-continuing-round balance to the analysis and in-game ledger.

**Acceptance.** Reconstruct every ending balance from opening cash, debits and actual grant; preserve source/result hashes. Show continuing-state witnesses for mathematical claims and never present state-count enumeration as played matches.

**Related original findings:** PP-019, PP-071.

**Source paths:** `src/StrikeLedger.Core/EconomySeed.cs`; `src/StrikeLedger.Core/Simulation.cs`; `src/StrikeLedger.App/Replay.cs`; `game/Main.EconomyReport.cs`.
