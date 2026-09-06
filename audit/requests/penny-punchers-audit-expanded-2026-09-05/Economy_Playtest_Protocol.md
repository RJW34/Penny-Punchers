# Economy validation protocol — implementation handoff

**Status: proposed experiment, not an executed playtest.**
Pinned baseline: `RJW34/Penny-Punchers@1623ee62c4f4b6d5574bbb9909eb6fe4e32f5441`.
Keep strictly 1v1 and one wallet. Do not add team systems or a second combat resource.

## Prerequisites

Complete the combat/menu input and camera regressions. Repair ordinary CPU use of purchased techniques and actual selected-super prices. Repair the laboratory command encoder for `4+HP+HK`, deliberately cover all rental alternatives, and expose rentals in player-operable training. A bot that cannot use a purchase or respond to its threat cannot estimate that purchase's strategic value.

Run the existing C# suites and retain both baseline and experiment source/content identities. Do not alter the old acceptance ledger. Use explicit lab-only fixtures for controlled state changes; never expose competitive wallet mutation or label a trusted fixture adjustment as an ordinary replay input.

## Experiment E1 — distinguish score from money

Question: How much does the outcome-derived 300-credit gap after round one increase the already-leading player's chance to win?

Create verified round-two starting states with score 1–0, full reset health/combat state, current recovery tiers (winner 0, loser 1), exact fighter/art selection, no retained rentals, no reserve, and current stage geometry. Record the full state hash and treatment.

Within each cell compare:

| Cell | Baseline wallets | Trailer top-up control | Leader subtraction control |
|---|---|---|---|
| Neither spent in round one | 1800 / 1500 | 1800 / 1800 | 1500 / 1500 |
| Both spent 300 | 1500 / 1200 | 1500 / 1500 | 1200 / 1200 |
| Both spent 600 | 1200 / 900 | 1200 / 1200 | 900 / 900 |

Order is winner/trailer, not permanently P1/P2. Keep score and recovery tiers fixed across each comparison, and use the existing future payout rules. The top-up and subtraction controls isolate the gap at different overall liquidity levels; neither on its own explains every effect. An optional reversed-wallet control is diagnostic only.

Cover all ordered character pairings, all selected-art combinations, and both actor seats. Mirror-correlated cases are not independent replications. Begin with a small documented representative pilot, then expand to the complete matrix where feasible. Use multiple seeds and multiple supported combat policies. Record unsupported cells as untested.

For bots, use reproducible per-actor/per-round random streams and label which inputs are driven by delayed observations. A common seed with different numbers of random calls does not guarantee perfectly matched combat decisions. Allow policies to respond to changed affordability; replaying a fixed unaffordable input script is not a strategic experiment.

For people, block by player pair, fighter/art matchup, and hardware; randomize/counterbalance treatment order. Include equally skilled practice partners where possible, but do not assume an actual round-one winner is equally skilled simply because the sample is large. Obtain outcomes without leading participants to expect a particular result.

Primary metrics:

- Round-two win-probability difference relative to each paired control.
- Eventual match-conversion difference, with score held at 1–0 at entry.
- Confidence/uncertainty intervals and effective independent block counts.

Secondary metrics:

- Affordable EX/super/lease combinations on each continuing-round entry.
- Time spent unable to afford a supported punish, reversal, or escape.
- Paid attempts, accepted debits, hits, blocks, parries, whiffs, interruptions, and lost conversions.
- Rent selection, actual execution, documented opponent adaptation, and remaining cash.
- Payout nominal/granted/clipped amounts and recovery tier before/after each result.
- Match length, shop length, accidental reserves/empty plans, and desire to rematch.

A first-to-five 1–0 lead already converts at 163/256 under fair independent decisive rounds. This is a reference calculation, not the null expectation for every asymmetric matchup. Do not compare raw conditional conversion with 50% and call the difference snowballing.

## Experiment E2 — measure the whole payout policy

Compare baseline A against candidate B and diagnostic C, with character/move data unchanged:

- A: win 1200, loss 900/1200/1500.
- B: win 1200, loss 1200/1200/1500.
- C: win 1200, loss 1200/1200/1200.

Keep draw payout 900 for this experiment. B changes every tier-zero loss, not just the literal opening round; disclose higher income and cap effects. Run full matches from the ordinary starting state as well as controlled continuations. Use original candidate manifests, receipts, replays/traces, and analysis output per ruleset. Never relabel an old candidate's results.

Test saving, efficient confirming, repeated failed EX, rental-focused play, super banking, repeated losses, alternating wins/losses, intentional yielding, draws, zero cash, and capped cash. Test a player who wins cheaply while the other spends heavily; equal spending alone cannot bound real wallet gaps.

Exclude terminal-match payouts from helpful-comeback-liquidity measures. Do not interpret a 0–5 player's postmatch wallet as a resource that could have aided the finished match.

## Experiment E3 — purchase usefulness, not feature count

For each of the 12 rentals, document one intended problem, the free-kit alternative, the lost slot/action, the cost in foregone combat spending, and at least one opponent response. Execute legal commands, not only direct `TryStartAction` calls.

Use matched states with the same initial total credits. Compare the paid rental to a no-rental player who keeps the saved cash for legal combat use. A comparison that refunds the rental to its buyer does not evaluate the actual purchase.

Specific mandatory checks:

- Clinch Entry: strike/throw situations, jump/tech/mash/parry defense, post-block immunity, simultaneous throw arbitration.
- Low Drive versus Low Turn: special-class cancel access and retaining forward HP versus cheaper command-normal replacement; verify any claimed route.
- High Hook/Heel Arc: reach, post-hit options and punishability versus universal overhead and original forward HP; lost kara behavior.
- Long Check: increased stun/lower geometry versus slower startup, weaker damage, narrower width, lost movement/kara; find a reproducible niche or redesign.
- Both feint pairs: intended visual deception, time-to-recovery, actual bait/punish. Displacement alone is worse than walking. Do not invent a benefit from an unsupported bot reaction.
- Returning Pulse: one contact only, turn after a miss, removal on block/parry, owner-wide active-projectile restriction, no unproven two-hit or layered-super claim.
- Low Palm: charge-independent low-special access versus free sweep and charge moves.

Do not require all choices to have equal aggregate pick rates. Matchup-specific specialization is acceptable. Do flag universal autopicks, purchases repeatedly bought but not usable by the policy, strictly misleading labels, and actions that have no demonstrated payoff.

## Experiment E4 — six selected supers

At exact affordability and one-credit-below/above boundaries, test all six arts. Include no-rental, one-rental and high-bank situations; normal/special confirms, EX-to-super only where actually allowed, raw reversal, whiff punishment, parry, block, and escape.

Record actual landed damage, chip, stun, position, knockdown, safety, connection rate, remaining cash, and next-round consequences. All-hit payload sums in this audit are not engine damage evidence. Four cheap activations versus two expensive ones is a capacity difference that requires a tactical justification, not a requirement that the prices become equal.

## Experiment E5 — zero-wallet agency and paid chip

Create representative corner/midscreen low-health states against each expensive offense sequence. Give the defender zero combat funds but the intact base kit. Verify legal free guard/parry/jump/movement/throw options and their timing. Record cases where a prior strategic choice has legitimately created a losing position separately from a system-level lack of counterplay.

Free action execution is necessary but not sufficient: an input may be legal yet fail to contest the funded attack. Do not conclude “comebacks are fair” from simply observing a free normal start.

## Experiment E6 — stat proposal review

No global stat purchases are currently shipped. Before any implementation, obtain an explicit design decision stating duration, stacking, tradeoff, visibility, affected interactions, price, and how the change survives rollback/replay.

Default recommendation: do not add global paid health/damage/speed/parry/income upgrades. Prefer distinct move tools or fixed character identity. If stat sidegrades are deliberately explored, separate their results from the original one-wallet/no-stat baseline and test kill thresholds, blockstrings, spacing, defense windows, and economic feedback. Never add a supposedly harmless percentage without a testable consequence.

## Exploratory guardrails, not universal truths

Before data collection, the owner should choose the tolerable economy-attributable advantage. One reasonable *provisional* target to discuss is no more than roughly 3 percentage points of additional match conversion due specifically to the opening payout difference, on the representative equal-skill/equal-spend comparison; effects above 5 points merit immediate investigation. These are proposed product tolerances, not statistical laws or accepted results.

Analyze matchup cells as well as pooled means. A favorable average can hide one severe art-specific threshold. Treat wide confidence intervals as inconclusive, not as evidence of no imbalance. Use player/seed-block resampling or an appropriate paired analysis rather than pretending every mirrored run is an independent Bernoulli sample. Small owner/friend pilots serve usability discovery first.

Do not force a trailing player's overall match chance to 50%; that would erase earned score advantage. Do not eliminate all benefit from saving; that would erase the economy. The goal is to avoid an excessive additional advantage from the payout rule or purchasable universal power.

## Suggested output contract

Every run should save:

- Source/build/content hashes and an explicit competitive/lab-only trace flag.
- Experiment ID, treatment, player/policy IDs, seed block, seat, fighter, selected art, opening score, wallet, and recovery tier.
- Initial snapshot and every input/economic receipt or compatible event trace.
- Per-round spending, payout components, cash available at the next *continuing* round, outcome, and duration.
- Per-item/per-art choice, activation, result, and interpretation limits.
- Abort/exclusion reasons, hardware/input/network conditions, and subjective answers kept separate from measured facts.

The release verdict must distinguish: accounting invariants verified; command and combat behavior verified; experiment suggests acceptable balance within tested conditions; actual human feel accepted. None implies the others.

## References

NIST randomized-block designs: https://www.itl.nist.gov/div898/handbook/pri/section3/pri332.htm
NIST confidence-interval guidance: https://www.itl.nist.gov/div898/handbook/prc/section2/prc241.htm
These inform experimental method, not a claim that NIST evaluated this game.
