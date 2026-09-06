# Addendum — purchasable moves, stat upgrades, and comeback fairness

**Repository:** `RJW34/Penny-Punchers`  
**Pinned head:** `1623ee62c4f4b6d5574bbb9909eb6fe4e32f5441` (`main`, rechecked for this follow-up)  
**Date:** 5 September 2026  
**Scope:** traditional 1v1; one persistent credit wallet; no team economy. This extends, rather than replaces, the original 77-finding audit.

## Verdict

The resource architecture has sensible anti-snowball safeguards, but the current evidence does **not** establish fair competitive balance. Winning round one does not grant a permanent character upgrade, retained rental, or an enormous automatic purse advantage. With equal first-round spending, it grants **300 more credits** than losing. That difference can nevertheless cross an EX/super/rental affordability threshold and change a round's most important interaction.

The rental catalog is mixed. Command-grab access, alternate low/overhead options, and a returning projectile can support meaningful plans. Several purchases need a much clearer role or a value adjustment. Vale's Long Check, both fighters' feints, and the relative value of the six supers deserve specific scrutiny before adding more products.

**No global character-stat increase products are implemented in the pinned catalog.** Some rented moves have different damage, stun, height, range, or timing; that is not a buff to the fighter's entire kit. Buying health, global damage, speed, defense windows, income multipliers, or permanent upgrades would be a new design change. My recommendation is to leave global purchasable stats out of the first balanced version.

## Evidence and execution boundary

This supplement reread the current branch and economic source through the GitHub connector. It additionally analyzed **all 13 canonical data JSON files**, plus the current `EconomySeed.cs`. Those 14 files match their pinned Git blob hashes. The container could not download repository files directly, so already attached seed bytes were reused only after matching the current repository blobs; two character files were reconstructed with the inspected EX-to-super cancel additions, and their **complete resulting byte sequences** also match the current blob hashes. This is recorded in `economy_appendix/verified_source/SOURCE_IDENTITY.json`.

The included Python program actually ran the arithmetic and data analyses. It did not run Godot, compile C#, execute a battle, or infer real win rates. **37 analytical/content test methods passed**. An initial audit-test expected value for one nominal super payload was corrected from 201 to 202 after the integer arithmetic exposed the author-side mistake; the initial and final logs are preserved. This is not a game-code defect.

The final archive includes reproducible calculations, complete purchase/move snapshots, budget frontiers, a source-identity manifest, and a controlled playtest protocol. Native and human balance acceptance remain open.

## 1. What the game actually sells

The 12 items are six choices per fighter: two alternatives in each of three slots. Signature costs 900, technique 600, and gambit 300. One option per slot may be leased, up to 1,800 in rental spending. Leases last one round, activate for zero additional credits, and have no use-count limit. The same wallet separately funds 300-credit EX activations and a match-selected 900/1,200/1,500-credit super. Ordinary attacks, movement, guard, parries, and normal throws remain free.

Sources: `data/items.json`; `data/economy.json`; `src/StrikeLedger.Core/GameContent.cs::ItemDefinition`; `Simulation.cs::CommitPreparation/NextRound`; `Simulation.Input.cs::Available/TryStartAction`.

At sufficient funding, each fighter has **27** rental combinations (none or either choice in each of three slots), or **81** combinations when including three selected supers. Across the four ordered character pairings, that is **26,244 nominal paired configurations** before spacing, remaining cash, score, skill, or timing are considered. These are configuration counts, not independent balance trials or proof that all configurations are equally meaningful. At the initial 600 credits, only **5** rental combinations per fighter are affordable.

A full 1,800-credit rental purchase from an 1,800-credit wallet leaves **zero combat credit**. “Full buy” should not be presented as automatically optimal: the player may rationally rent nothing and preserve enhanced attacks. If every matchup recommends filling every slot, the shop has probably lost its intended opportunity cost.

## 2. Item-by-item judgment

The costs below are compared with EX purchasing power only as a budget translation. A command throw is not interchangeable with three EX attacks merely because both cost 900.

### Rook — Clinch Entry, 900

**Promising role:** add an untechable command throw to challenge passive defense and parry habits. It costs three EX activations and does not replace the ordinary throw.

The authored command throw is `qcb+P`, 12 startup / 3 active / 23 recovery, 135 damage, 90 stun, and 27,000 throw-range units. The free normal throw is 3/2/24, 120 damage, 80 stun, and 24,000 range, but is techable. The 900-credit purchase is principally for a different defensive interaction, not for the 15 extra damage.

**Risk:** the paid option is substantially slower. It must threaten a believable strike/throw sequence without becoming a guaranteed capture. The current throw rules prohibit capture during hitstun/blockstun and impose post-stun immunity, so do not advertise a guaranteed command-throw combo from a blocked normal.

**Required proof:** legal-input setplay against tech, jump, mash, parry, and patient defense; mirror the tests; establish intended arbitration for simultaneous command throws. Keep the role if it genuinely changes defense; do not simply accelerate it until the CPU loses.

### Rook — Low Drive, 900

**Plausible but insufficiently distinguished:** `qcb+P`, a 14/3/24 low for 85 damage, 110 stun, and hard knockdown. It adds a special-class option while retaining forward HP.

Its hit payload closely matches the cheaper Low Turn. That does not establish dominance: the input, slot, special-cancel eligibility, and preservation of forward HP differ. Its advantage must come from a verified route or situation rather than the name “signature.”

**Required proof:** compare it with free sweep, crouching medium kick routes, and Low Turn. Demonstrate a cancel or spacing situation where the extra 300 is worth paying. Do not infer safety from the printed recovery count alone.

### Rook — High Hook, 600

**Coherent role:** an overhead replacement for forward HP, `6+HP`, 18/3/19, 85 damage, 110 stun. It exchanges the original forward-HP option for a different guard check.

**Risk:** every character already has the free universal overhead, and the replacement loses the original move's advancing/kara behavior. Its extra reach or payoff must justify two EX activations. Do not frame this as buying access to overhead attacks at all.

**Required proof:** compare reach, crouching contact, post-hit advantage, punishability, and intended pressure against the free overhead and original forward HP.

### Rook — Low Turn, 600

**Coherent slot tradeoff, currently muddy presentation:** changes forward HP to a 14/3/24 low for 85 damage, 110 stun, and hard knockdown.

**Risk:** the free kit already contains low normals and a hard-knockdown sweep; the player loses forward HP. Its price needs a reason grounded in input access, reach, or a new pressure choice. It is not the same product as the special-class Low Drive, even with matching hit payloads.

**Required proof:** make at least one “choose this over Low Drive” and one “choose Low Drive over this” situation reproducible, or revise the pair.

### Rook — Step Feint, 300

**Highest concern:** 16 total action ticks, 13.6 world units of authored forward travel, no hitbox, no invulnerability, and no cancel. Free forward walking over the same 16 unobstructed ticks covers 48 units. It is not competitive as a straightforward movement purchase.

**Possible justification:** a convincingly shared startup or silhouette can bait a reaction. That is a human deception hypothesis, not something its displacement proves. A generic obvious step is not enough.

**Required proof:** identify exactly which threat it imitates, the opponent response it induces, and the punish it enables. Redesign or remove it if that hypothesis fails; do not add invisible armor merely to rescue the price.

### Rook — Sway Feint, 300

The same issue applies: 12.8 units backward over a 16-tick action versus 33.6 units of free backward walking. It needs an actual bait/recovery purpose. Its existence does not establish a viable 300-credit starter purchase.

### Vale — Returning Pulse, 900

**Distinctive idea, narrower actual behavior:** a 22/3/23 motion-input projectile that turns after 45 projectile-age ticks and can hit **once**. It is not an outbound-plus-return two-hit attack. Hit, block, or successful parry consumes its contact; a returning threat mainly matters after an initial miss. Its availability through a motion input is itself a meaningful change to a charge-oriented fighter.

**Important restriction:** the current owner-wide projectile check counts the fighter's active projectiles when starting other projectile moves. A persistent Returning Pulse can prevent another projectile, including a selected projectile super, from starting. Do not present it as unrestricted layered projectile pressure.

**Required proof:** whiff-return timing, jump avoidance, turn positioning, opponent crossing, block/parry removal, and the tradeoff with ordinary/EX/super projectile access. Preserve the restriction if it is deliberate, but explain it on the card.

### Vale — Low Palm, 900

**Plausible role:** a motion-input low special, `qcf+P`, 14/3/24, 85 damage, 110 stun, hard knockdown. Relative to free charge options it could enable a low threat without stored charge.

**Risk:** free low normals and sweep already exist, and it has no outgoing cancel rules. Its value is not “low attacks cost money”; it is the particular route, range, and charge-independent threat.

**Required proof:** compare against charge loss after movement/cross-up, free sweep, crouching medium kick conversions, and the Returning Pulse alternative.

### Vale — Heel Arc, 600

The role mirrors High Hook: an 18/3/19 overhead replacement for forward HP, with Vale-specific reach. Compare against the free overhead and lost forward-HP/kara tools. Require a demonstrated pressure/spacing benefit, not simply a different animation.

### Vale — Long Check, 600

**Most concerning replacement on the table:** it replaces the free Long Heel/forward HP but has worse startup, damage, recovery, authored width, hitstun, and blockstun. It also loses advancing movement and kara eligibility.

| Property | Free Long Heel / forward HP | Purchased Long Check |
|---|---:|---:|
| Startup ticks | 10 | 14 |
| Active ticks | 3 | 3 |
| Recovery ticks | 17 | 19 |
| Damage | 100 | 85 |
| Stun inflicted | 80 | 110 |
| Hitstun | 24 | 23 |
| Blockstun | 14 | 13 |
| Hitbox width, world units | 70 | 63 |
| Authored forward travel, world units | 3 | 0 |
| Kara throw eligibility | Yes | No |

Long Check does have more stun and a lower hitbox, so “strictly dominated” is not established. The lower contact geometry could matter. Nevertheless, paying 600 for this trade needs an unusually clear purpose, and its “long” label is misleading as a simple range comparison.

**Recommendation:** redesign around a verified anti-poke, low-profile contact, stun-conversion, or spacing niche; otherwise replace the option. Do not buff every stat simultaneously or assume an increase in raw range alone creates a good choice.

### Vale — Step Feint and Sway Feint, 300 each

These use the same authored feint movement as Rook. Vale can freely walk 43.2 units forward or 35.2 backward over 16 ticks, compared with 13.6 and 12.8 for the feints. Test character-specific deception rather than assuming copied feints have equivalent value. The current lab's obsolete `HP+HK` branch must be repaired to actually exercise `4+HP+HK`.

### Overall catalog recommendation

Keep the concept of situational leases, but require every card to name a problem, a base-kit alternative, an opportunity cost, and a counterplay example. Three fixed price tiers are presentation scaffolding, not established balance. First repair practice and policy coverage, then change prices or move designs from measured outcomes. Do not fill weak slots with passive stat bonuses to make purchases feel valuable.

Sources for this section: all `data/items.json` records; all `data/fighters/{rook,vale}.json` moves; `Simulation.Input.cs::Recognize/Available/CanTransition`; `Simulation.Combat.cs::ResolveThrows/AdvanceProjectiles/ResolveContacts`. Exact content snapshots and machine-readable comparisons are in `economy_appendix/calculations/purchase_move_matrix.json`.

## 3. Super pricing is part of shop balance

The 900/1,200/1,500 art prices change both opening affordability and repeated access. With a 3,600 wallet and no rentals or other spending, a player can finance **four**, **three**, or **two** activations respectively (the last leaves 600). This is purchasing capacity, not a prediction that every activation can land.

The authored raw payloads are not monotone in price. For example, Rook's five-hit 900-credit art sums to 240 scaled damage if every authored hit lands from combo index zero; the two-hit 1,200 art sums to 266; the three-hit 1,500 art sums to 202. These are **static all-hit arithmetic**, not production damage traces. Movement, invulnerability, actual connection, pushback, range, chip, conversion access, and knockdown can change value enormously.

A more expensive art may reasonably do less damage if it buys a superior tactical function. But the function must be tested. Never infer that a 1,500 price is justified because it is art number three. Compare every selected art at exact credit thresholds and within realistic confirms, reversals, spacing, and parry situations.

One cap on the shared wallet does not impose a separate per-round casting limit; that is intentional. Do not conceal poor super pricing with a second meter or prepaid charges. Adjust costs, move properties, or the shared economy only through a recorded design change.

## 4. Stat increases: distinguish character identity from purchased power

The current items do not globally raise health, damage, speed, parry windows, or income. A rented move dealing 110 stun instead of 80 is **move-specific tuning**, not +30 stun on everything the fighter does.

My recommendation for the first balanced prototype:

| Proposal | Recommendation | Reason |
|---|---|---|
| Permanent match-long stacking health/damage/speed | Do not add | Earlier funding buys benefits that repeatedly help preserve future funding. |
| Per-round global health/damage buffs | Avoid initially | They help virtually every plan and change kill/confirm thresholds, even when they expire. |
| Faster startup, shorter recovery, larger parry windows | Do not sell | They change the trusted execution/interaction rules across the entire character. |
| Interest, payout multipliers, cheaper EX/supers | Do not add initially | They directly strengthen the positive financial feedback the audit is trying to measure. |
| A temporary additional/replacement move | Keep, with a tested role | It creates a visible plan and an explicit slot/opportunity cost. |
| Fixed character-stat differences | Reasonable | They belong in character identity and normal matchup testing, not wallet snowball. |
| Future publicly visible stat sidegrades | Separate experimental decision | Require meaningful drawbacks and controlled tests; not a shortcut to make the current shop exciting. |
| Cosmetic changes | Economically neutral | They must not alter competitive power or persist credits between matches. |

A hypothetical +10% damage change can turn a 240-damage sequence from five clean openings to four against 1,000 health (264 damage after the buff). A hypothetical +10% maximum health changes the number of clean 260-damage openings required from four to five. These examples assume repeated identical complete damage sequences and demonstrate thresholds only; they are not measurements of current combos or real win rates.

No arbitrary small percentage is guaranteed safe. Speed, timing, hitbox, and defense changes are especially difficult to compare because their effect depends on specific interactions. If the goal is build variety, improve distinct techniques and super roles first.

## 5. Exactly what a round-one win buys

Let `sW` and `sL` be each player's total first-round spending (rentals plus accepted paid activations). Both start at 600. There is no first-round cap clipping:

- Winner's round-two wallet: `600 - sW + 1200`.
- Loser's round-two wallet: `600 - sL + 900`.
- Difference: **`300 + sL - sW`**.

Therefore the award difference itself is 300. A larger observed wallet gap can also come from different expenditure, not from the win payout alone.

| Both spend in round one | Winner entering round two | Loser entering round two | Illustrative threshold |
|---|---:|---:|---|
| 0 | 1,800 | 1,500 | A 1,500 super plus one EX is affordable only to the winner. |
| 300 | 1,500 | 1,200 | A 1,500 selected super is available only to the winner. |
| 600 | 1,200 | 900 | A 900 super plus one EX is affordable only to the winner; a 1,200 selected super is also a threshold. |

The examples are conditional on the relevant selected art and on no additional rentals/reserve. “Super plus EX” here means aggregate affordability, not a guaranteed valid combo route for every art.

A 300 difference is 25% of a 1,200 wallet and 33.3% of a 900 wallet. More importantly, it can change the existence of a reversal, confirm, or anti-chip option. Its importance cannot be evaluated from percentage alone.

Unequal-spending examples:

| Winner spends | Loser spends | Round-two wallets (winner / loser) |
|---:|---:|---:|
| 0 | 600 | 1,800 / 900 |
| 600 | 0 | 1,200 / 1,500 |

The first is a two-to-one wallet difference, but 600 of that 900 gap comes from spending. In the second the loser is richer. This is why “who won round one?” and “who preserved their money?” must be separated.

### What already restrains snowballing

Health and combat state reset; rentals expire regardless of who won; ordinary actions and defense stay free; repeated losses raise the payout; the wallet is capped; and money does not persist into another match. There is no winner-retained equipment or permanent purchased-stat stack.

Those safeguards are meaningful. They are not proof of adequate comeback strength, because the richer player can still have more funded threats, and free execution does not imply equal matchup power.

### Where positive feedback can remain

A successful player might win using cheaper confirms or fewer failed paid attacks. The opponent may repeatedly spend trying to escape, whiff, or get parried. The leader then preserves cash as well as winning score. Paid move strength can reinforce that cycle even without a large winner payout.

The package includes a deliberately extreme hypothetical ledger where player A saves while B spends its entire wallet. It is not a feasible-strategy proof or an engine match. It shows why a 300 award gap does not bound the total economic difference.

## 6. Recovery catches up, but potentially late

The old tier determines the loss payout, and only then advances. Consecutive losses pay **900, then 1,200, then 1,500**. Winning decreases a recovery tier by one rather than resetting a high tier immediately.

Under a simple hypothetical sequence where A wins repeatedly and both spend 600 every round:

| After round | Score A–B | A credits | B credits | A's wallet lead |
|---|---:|---:|---:|---:|
| 1 | 1–0 | 1,200 | 900 | +300 |
| 2 | 2–0 | 1,800 | 1,500 | +300 |
| 3 | 3–0 | 2,400 | 2,400 | 0 |
| 4 | 4–0 | 3,000 | 3,300 | −300 |

This disproves a simple claim that win payouts keep increasing the leader's wallet advantage forever. But first reaching cash parity while 0–3 down is not necessarily enough to create a satisfying comeback. Score losses remain real.

The program also enumerated all reachable **ledger states** with an A opening win, later W/L/D outcomes, and equal aggregate spending in 300-credit increments. It reached 1,186 distinct terminal ledger states across the possible ending rounds. This count is not a sample of played matches. Later mixed streaks, clipping, and retained recovery tiers can produce 900-credit differences in this bounded model; the 300 opening gap is not a universal invariant. Reproducible continuing-state witnesses are included in the JSON.

Do not count payouts at match-over as comeback assistance. They cannot buy anything before the match ends, and credits reset on rematch. Report liquidity on continuing-round entries instead.

## 7. The correct fairness baseline is not 50% after a 1–0 lead

In a first-to-five with independent 50/50 decisive future rounds and no economy advantage, the round-one winner already wins the match with probability:

`P(at least four wins in the next eight rounds) = 163/256 = 63.671875%`.

| Score | Leader's fair-model match chance | Trailer's fair-model match chance |
|---|---:|---:|
| 1–0 | 63.67% | 36.33% |
| 2–0 | 77.34% | 22.66% |
| 3–0 | 89.06% | 10.94% |
| 4–0 | 96.88% | 3.13% |

These are mathematical score-only benchmarks, not observed game win rates. Real players are not necessarily equal, and conditioning on an actual first-round win can select the stronger player. Draws and the half-point cap also require their own model when present.

Thus a measured 64% conversion rate after winning round one does not by itself demonstrate an economy problem. Nor would a high unadjusted rate prove that the economy caused it.

A toy sensitivity calculation illustrates why duration matters: if a hypothetical first-round winner has a 55% chance in round two only and every other future round is 50/50, match conversion becomes about 65.04%. If that same assumed 55% advantage persists in every later round, it becomes about 73.96%. **No mapping from 300 credits to 55% is asserted.** The game needs controlled measurement to determine the actual effects.

## 8. What would establish whether the opening win is too valuable?

Fix PP-001/002 and the CPU/lab coverage issues before collecting balance data. Then start matched experiments from **the same confirmed round-two state with score 1–0**, changing only the intended economic treatment.

For every equal-spend opening (0, 300, 600) compare:

1. Current wallets and recovery state.
2. Add 300 to the trailer to remove the immediate award gap.
3. Remove 300 from the leader to remove the gap without increasing total liquidity.
4. Optionally swap wallets as a diagnostic; it is not a shipping rule.

Keep score, health, fighter, selected art, stage, and future payout rules fixed within each comparison. Retain differences in chosen policies as responses to the intervention rather than replaying a rigid input script that never adapts to its funds. Mirror seats and counterbalance human order. Use player-pair/seed blocks and analyze correlation rather than counting mirrored duplicates as independent evidence. This follows the general randomized-block principle of isolating a treatment while controlling nuisance variation; see NIST's randomized-block guidance listed below.

Primary outcomes: effect on round-two win probability and eventual match conversion relative to the paired control, with uncertainty. Secondary outcomes: useful affordability thresholds, spending efficiency, rental use and threat response, paid-reversal/confirm access, chipped KOs at zero funds, and how long a trailer lacks a meaningful spending choice. Do not force every choice to 50% win rate or treat an unused threat as automatically wasted money.

The implementation-ready procedures, metrics, and exploratory guardrails are in `Economy_Playtest_Protocol.md`. Small owner/friend sessions can identify confusion and dead items; they cannot certify a narrow percentage-point balance margin.

## 9. Payout variants worth testing — not silently shipping

| Variant | Win payout | Loss payouts | Purpose |
|---|---:|---|---|
| A: current | 1,200 | 900 / 1,200 / 1,500 | Establish the actual baseline. |
| B: equal first-loss award | 1,200 | **1,200 / 1,200 / 1,500** | Remove the immediate outcome-driven gap while retaining later recovery. |
| C: equal win/loss income | 1,200 | 1,200 / 1,200 / 1,200 | Diagnostic: score rewards winning; cash advantage comes from expenditure efficiency. |

Draw payout remains 900 for this isolated comparison. Variant B changes the first recovery tier for **every** loss at tier zero, not only the literal first round. It increases monetary supply and can affect cap clipping, super frequency, and average shop choices. Variant C still allows unequal retained wallets and therefore does not remove all positive feedback.

**My first candidate to test is B**, because it directly addresses the user's concern with one changed payout value. I would not call it balanced until it survives the controls above. Keep the existing baseline and store candidate data hashes separately. Test deliberate yielding and drawing, but do not assume paying losses equally makes losing profitable: losing still costs a point, while spending less can change fight strength.

## 10. What should change in the buy menu because of this audit?

Show the selected super's actual cost, each rental's added/replaced action, remaining combat funds, affordable EX/super combinations, and a base-kit comparison. Display the prior round's rental spending, combat debits, nominal/granted/clipped income, and remaining wallet separately. A player needs to understand whether a cash deficit came from the payout schedule or their own unsuccessful expenditure.

Rename the reserve control to **“Do not spend below ___ credits this round.”** With 1,200 credits and a 900 floor, a player cannot activate a 900 super; doing so would leave 300. The protected balance is savings, not available super money. It should be advanced/opt-in, and the final-round UI should clearly explain that saved credits have no next-match value. Do not automatically override an intentional reserve without a rules decision.

A rental card should identify a practical matchup problem and its counterplay. Do not insert a vague “+damage” option to compensate for an unconvincing technique. Make the next round's intended plan understandable enough that the player can explain it in one sentence.

## Added backlog and implementation priority

This supplement adds **PP-078–PP-091**, bringing the combined backlog to **91**. Existing findings remain intact. The new records extend PP-014–020 and the CPU/training/UX work; they are design risks, source gaps, or verification requirements, not 14 newly reproduced engine bugs.

Prioritize: fix fundamental play blockers → repair item execution/practice coverage → redesign Long Check and validate feints/Returning Pulse → implement affordability/ledger UI → run opening-gap and payout comparisons → obtain human feel and comeback feedback. Leave global stat products out until the economic duel itself is convincing.

## Sources and reproduction

All repository references are pinned to the audited head. Key paths:

- `data/economy.json`, `data/items.json`, `data/rules.json`, `data/combat.json`, and both full fighter JSON files.
- `src/StrikeLedger.Core/EconomySeed.cs`; `Simulation.cs`; `Simulation.Input.cs`; `Simulation.Combat.cs`; `GameContent.cs`.
- `src/StrikeLedger.App/Bots.cs`; `src/StrikeLedger.BalanceLab/Program.cs`; `release_docs/BALANCE_NOTES.md`.
- `docs/00_PRODUCT_CONTRACT.md`; `game/Main.Menus.cs`; `game/Main.PrepDetails.cs`; `game/Main.Network.cs`.
- NIST/SEMATECH, **Randomized block designs**: https://www.itl.nist.gov/div898/handbook/pri/section3/pri332.htm — methodology only, not evidence about this game's balance.
- NIST/SEMATECH, **Confidence intervals**: https://www.itl.nist.gov/div898/handbook/prc/section2/prc241.htm — uncertainty guidance only.

From `economy_appendix/`, with Python 3.11+ and no external packages:

```sh
python economy_analysis.py --data verified_source/data --output calculations
python -m unittest test_economy_analysis -v
```

These commands reproduce only the supplemental static/arithmetic work. They do not replace the game's C# suites, actual device tests, or human playtesting.
