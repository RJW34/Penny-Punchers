# Penny-Punchers — repository, game-loop and economy audit

**Audited revision:** `RJW34/Penny-Punchers`, `main`, `1623ee62c4f4b6d5574bbb9909eb6fe4e32f5441`  
**Date:** 5 September 2026  
**Mode:** read-only source and committed-evidence review. No repository changes.

## Verdict

This is an implemented playable candidate, not merely a scaffold. The match state machine closes: preparation → reveal → countdown → fight → confirmed terminal result → settlement → next round or match result. A real one-wallet economy, leased moves, paid startups, training, replay and private rollback exist.

It is not yet a finished player-facing fighting game, and the buy phase needs another design-and-implementation pass. The most useful next work is not a new engine, a large roster, teammate systems or a second resource gauge. Fix input/camera problems; make rentals understandable and practiceable; correct the CPU/experiment policies; then judge the economic decisions with real players and improve animation fidelity.

## Evidence boundary

The current tracked state reports 76/76 software requirements and 79/82 total, with actual controllers, two physical LAN computers and human feel still unverified. These are repository-reported runs, not tests executed during this audit. The audit did not launch Godot/.NET or independently watch the newest After Hours footage. Its visual findings derive from renderer code, content metadata and the candid art-integration report; potential matte/palette artifacts are inspection targets, not observed defects.

The audit inspected instructions/authority, root/history/branch metadata, key Core simulation/input/contact/serialization paths, App replay/rollback/private peer/bot/training paths, game menus/input/lifecycle/HUD/rendering/audio, canonical rules/economy/items, selected character tables, major test and balance harness paths, and current status/limits reports. It does not claim to have reviewed every byte of every helper, binary or asset.

The repository deliberately retains the internal Strike Ledger identity. That is not treated as a naming error. The original two-character scope is also not itself a defect. Historical evidence and original scaffold manifests must not be confused with the current source candidate.

## What to preserve

- Exactly two opposing fighters in one arena; no team finances or extra modes requiring team balance.
- One scalar wallet for rentals and committed EX/super startups; no attack/parry/whiff farming or hidden second gauge.
- A complete free kit and defense at zero credits.
- Atomic two-player preparation, idempotent receipts, deterministic snapshots/replays and confirmed-only settlement.
- Manual execution, expressive parry/throw/spacing systems and original art rather than copied reference-game assets.
- Honest separation among source checks, native software checks, physical-device checks and human acceptance.

## Release meaning

| Question | Audit verdict |
|---|---|
| Does the code implement a complete economic match? | Yes, structurally. |
| Are currencies merely decorative? | No: they affect real leases and move startups. |
| Is the current shop a mature tactical interface? | No. It remains largely a cycling purchase form. |
| Is the roster competitively balanced? | Not established. The recorded experiments are bounded and policy-limited. |
| Does art coverage prove Third Strike-like feel? | No. Key-pose coverage is not animation quality or human acceptance. |
| Is it ready to call finished? | No. Treat it as a playable candidate entering serious playtesting and refinement. |

## Highest-leverage fixes

1. **PP-001:** separate combat buttons from menu cancellation. Default B currently has conflicting MK/pause roles.
2. **PP-002:** prevent legal spacing from putting fighters outside the viewport.
3. **PP-003–006:** align buying, CPU execution and experiment coverage. Fix the lab's obsolete feint command before using its results to price feints.
4. **PP-007–017:** make every rental learnable, practiceable and meaningfully different. Explain reserve as protected future savings, not usable super funding.
5. **PP-018–020:** measure human decisions, comeback incentives and pacing; do not infer balance from successful accounting tests.
6. Improve move silhouettes, hurtbox authorship and actual animation timing before spending most art effort on larger backgrounds.

## Buy-phase redesign brief

Use one shared local/network view model, with explicit differences only for input ownership and the declared information policy. Display credits, selected super and its cost, previous-round income/spending, and an accurate timer. Show actual item cards rather than hiding alternatives behind cyclic text.

A card should answer: What problem does this solve? What move/input does it add or replace? What is the drawback? What cash remains? What EX/super options does that leave? Can I try it safely in training?

Example, based on current content:

> **Clinch Entry — 900 credits / this round only**  
> Adds an untechable command throw, `qcb+P`; does not replace your ordinary throw.  
> Startup 12; active 3; recovery 23. Committed close-range option; it is not free guaranteed pressure.  
> After purchase: show actual remaining wallet and selected-super/EX affordability.  
> **Try in Lab** / **Compare with base kit** / **Choose**.

Do not call a card “safe” or give a frame-advantage value until measured through the real simulation, including contact timing and hitstop. Do not describe Low Drive as strictly dominated by Low Turn: identical hit payloads do not erase differences in command, slot, replacement and cancel classification.

Full Try in Lab sessions belong in untimed catalog/pre-match practice, not in a way that freezes an opponent’s active competitive countdown. The timed shop can show concise read-only move previews.

Keep the underlying one-wallet design for the first redesign pass. Add transparent saved plans and advanced protected savings, not prepaid EX charges. Preserve the ability to choose no rental. On timeout, commit a clearly explained valid plan; never silently discard everything without a clear result.

### Proving that the shop belongs

Run a diagnostic comparison of the existing single-wallet fighter with rentals disabled versus the same game with improved rental cards and tools. This is an experiment, not a recommendation to remove the defining shop. Use matched players, alternate sides and fighter choices, and collect recordings. Ask whether a purchase changed the intended plan, whether the opponent noticed/adapted, and whether both players understood the cost of foregoing EX/super spending.

Track item selection and actual usage, but do not equate unused with useless: an understood threat can change behavior. The current basic CPU cannot assess that threat well, which is one reason human trials matter. Track forbidden negative outcomes too: universal purchases, obviously dead purchases, routine accidental empty plans, reserve confusion, and decisions made only to dismiss the screen.

## Visual fidelity direction

Aim for authored fighting-game motion and readability, not photorealism or upscaled static portraits. The current report honestly describes 79/80 source cells, shared phase poses and aliases. The next quality step is unique key actions with coherent pivots, anticipation, active frames and follow-through, aligned to deliberately authored hurtboxes.

A productive order is: silhouette/pose and motion consistency; contact and guard reactions; true alpha and palette masks; HUD and portrait proportions; menu type/layout; then stage depth, ambient motion and richer audio. Keep original assets and preserve reduced-flash controls. Cosmetic changes must not silently alter combat timing or replay identity.

## Prioritized implementation order

**A — Make a dependable duel:** fix B routing and camera bounds; add exported-GUI regression tests; reproduce the queued-throw/parry-transition hypotheses before changing semantics.

**B — Make spending understandable:** shared shop component, data-driven prices/slots, valid timeout behavior, rental practice, selected-super/EX affordability, and a plainly labeled savings floor. Repair both CPU and laboratory command coverage.

**C — Establish combat and economic value:** per-move hurt silhouettes, meaningful feint/rental roles, distinct fighter/super purposes, multi-policy experiments and observed human sets. Keep price changes versioned and do not tune solely to beat the current bots.

**D — Make the successful interactions look and sound good:** authored animation holds, clean alpha/palette assets, event-time VFX, responsive rollback presentation, stronger UI hierarchy and original sound variation.

**E — Verify and distribute:** real pad/keyboard/stick checks, two-machine matches, corrected docs, reproducible fresh exports, CI, evidence restoration and explicit human verdict. Preserve failures and limitations.

## Detailed findings

There are **77 findings and opportunities** below. They are not all bugs. Priority and evidence class are kept separate, and none is presented as a native reproduction performed here.

### PP-001 · Default medium kick also routes to pause

**P0 · source_defect · Controls**

**Basis.** DefaultPad assigns B to MK; ConfigureMenuActions assigns B to ui_cancel; Main._UnhandledInput treats ui_cancel as pause on the fight screen. RouteControlInput does not consume ordinary combat button events. This is a direct routing conflict; no native reproduction was performed here.

**Action.** Use a separate gameplay pause action, normally Start/Escape. Limit menu cancel to menu contexts and preserve all six attack bindings, including rebound B.

**Acceptance.** In the exported GUI, press and release every default attack and chord button in a live fight for each seat; none may change screen. B must produce MK, and Start alone must pause. Repeat with B rebound to LP.

**Source:** `game/GameSettings.cs`; `game/Main.ControlRouting.cs`; `game/Main.cs`; `game/Main.UiSmoke.cs`; `src/StrikeLedger.CoreTests/Program.cs`; `AUDIT_GUIDE.md`.

### PP-002 · Legal fighter separation exceeds visible arena width

**P1 · source_defect · Camera**

**Basis.** Foundry allows walls at 0 and 768000 with fighter centers clamped approximately to 16000..752000. ArenaView displays width 480000 at the midpoint. At opposite bounds, the root positions map to roughly -341 and 1621 in a 1280-wide viewport. No maximum-separation constraint appears in the reviewed pushbox path.

**Action.** Choose an explicit combat-space maximum separation independent of the camera, or a bounded camera fit rule. For a fixed-scale traditional fighter, a deterministic separation constraint is preferable to uncontrolled zoom. Record any geometry change and rebalance zoning.

**Acceptance.** Hold away with both players to the legal extremes and test corner knockback, projectiles and jumps. Both full fighting silhouettes must stay inside declared safe margins in all supported aspect ratios.

**Source:** `game/Presentation/ArenaView.cs`; `data/stages/foundry.json`; `src/StrikeLedger.Core/Simulation.Combat.cs`.

### PP-003 · Regular CPU purchases techniques its policy does not deliberately execute

**P1 · source_defect · CPU**

**Basis.** ChoosePreparation buys rook_high_hook or vale_heel_arc. The regular Next policy issues standing/crouching normals, throws, jumps and predefined special sequences, but contains no deliberate 6+HP lease branch. Buying is disconnected from the fighting policy.

**Action.** Make purchase choice and combat policy share a capability plan. Until a tool is supported, the CPU should not routinely spend on it. Evaluate threat value only against an opponent policy capable of responding to that threat.

**Acceptance.** For each CPU-selected item, produce seeded legal-input traces demonstrating intentional use in an appropriate situation. Verify that removing an unsupported item does not merely discard credits.

**Source:** `src/StrikeLedger.App/Bots.cs`; `data/economy.json`; `data/items.json`; `src/StrikeLedger.Core/Simulation.cs`.

### PP-004 · CPU super logic ignores actual selected-art affordability and Vale supers

**P1 · source_defect · CPU**

**Basis.** canSuper uses a fixed 1500-credit threshold although selected arts cost 900, 1200 or 1500. The super-input branch exists only in the Rook branch of the ordinary CPU policy.

**Action.** Read the selected move cost and command from content and support both fighters. Preserve observation delay and manual legal inputs.

**Acceptance.** At each selected art price, each fighter must have a seeded scenario that legally starts that art, with exactly one debit and no hidden extra funding.

**Source:** `src/StrikeLedger.App/Bots.cs`; `data/fighters/rook.json`; `data/items.json`; `data/fighters/vale.json`.

### PP-005 · Lease policy does not encode current gambit commands

**P1 · source_defect · Balance harness**

**Basis.** The lab handles a gambit command equal to HP+HK, while the authored feints use 4+HP+HK. Its other special-command branches do not handle that string. Starting a feint in a direct Core conformance fixture does not repair the lab policy.

**Action.** Use a shared data-driven command encoder for the laboratory and test every rented move through the same recognizer used by the game.

**Acceptance.** Every rental selected by the experiment must have a verified, legal-input policy route. Specifically observe shop_step_feint and shop_sway_feint activations with their true commands.

**Source:** `release_docs/BALANCE_NOTES.md`; `src/StrikeLedger.BalanceLab/Program.cs`; `data/fighters/rook.json`; `data/items.json`; `game/Main.UiSmoke.cs`; `src/StrikeLedger.CoreTests/Program.cs`; `AUDIT_GUIDE.md`.

### PP-006 · Fixed lexical rental selection does not evaluate all alternatives

**P1 · source_gap · Balance harness**

**Basis.** Lease-focused preparation sorts by price then ID and takes the first affordable item in each slot. Repeating this policy does not compare both alternatives within each slot.

**Action.** Create matched item-choice interventions and a capability-coverage report. Distinguish item availability, input reachability, actual selection, activation, and successful tactical use.

**Acceptance.** Each of the 12 items must appear in an explicit matched experiment or be marked untested. No broad claim of all-item balance may derive from one greedy selection policy.

**Source:** `release_docs/BALANCE_NOTES.md`; `src/StrikeLedger.BalanceLab/Program.cs`; `data/economy.json`; `data/items.json`; `src/StrikeLedger.Core/Simulation.cs`.

### PP-007 · Players cannot equip rentals through the reviewed training menus

**P1 · source_gap · Training**

**Basis.** TrainingReset clears loadouts; the lab menus expose health, stun, credits, position, recordings and speed, but no lease loadout editor or shop-to-lab route. Tests can equip items programmatically; players need an equivalent usable workflow.

**Action.** Add a training-only loadout selector and a Try in Lab action in the untimed catalog/pre-match practice flow, preserving the chosen fighter, opponent, art and credit context. It must not pause an opponent’s active competitive shop timer.

**Acceptance.** Every rental can be equipped, executed, compared with its replacement and removed entirely through normal controls. Returning from preview must not spend competitive credits.

**Source:** `src/StrikeLedger.App/Training.cs`; `game/Main.Training.cs`; `game/Main.LabState.cs`; `game/Main.Menus.cs`; `game/Main.PrepDetails.cs`; `game/Main.ControlRouting.cs`.

### PP-008 · Purchase cards omit their practical consequences

**P1 · source_gap · Shop UX**

**Basis.** The local LeasePreview shows command, one-round duration and free activation. Item descriptions/tradeoffs are in data but are not presented as a useful comparison with the base move.

**Action.** Use explicit cards: role, input, added/replaced move, what changes, limitation, remaining credits and a practice preview. Separate core information from optional frame data.

**Acceptance.** A first-time player can explain what they bought, what was replaced, how to execute it, and the opportunity cost without reading source or an external document.

**Source:** `game/Main.Menus.cs`; `game/Main.PrepDetails.cs`; `game/Main.ControlRouting.cs`; `data/economy.json`; `data/items.json`; `src/StrikeLedger.Core/Simulation.cs`.

### PP-009 · Online shop has less decision information than local shop

**P1 · source_gap · Shop UX**

**Basis.** NetworkPreparation presents slot/name cycling, reserve and a post-lock balance, but omits the local command previews and item prices on its rows, and does not visibly expose the server-side preparation deadline.

**Action.** Reuse the same shop component and card data for local and network modes, keeping only visibility/ownership rules different.

**Acceptance.** Identical legal plans display the same price, tool description, affordability, remaining time and projected wallet in both modes.

**Source:** `game/Main.Network.cs`; `src/StrikeLedger.App/PrivateMatchPeer.cs`; `src/StrikeLedger.App/RollbackSession.cs`; `game/Main.Menus.cs`; `game/Main.PrepDetails.cs`; `game/Main.ControlRouting.cs`.

### PP-010 · Unaffordable timeout plans are silently discarded

**P1 · source_defect · Shop UX**

**Basis.** The local lock path replaces an invalid draft and reserve with an empty plan at timeout. The user can be shown a selection that is not what is committed, without a specific explanation of the fallback.

**Action.** Keep drafts valid, or define a prominently communicated last-valid-plan policy. Always show the exact committed plan and explicit adjustments. Do not silently buy a different item.

**Acceptance.** Select an over-budget combination and let time expire. The user receives a deterministic, understandable result and a matching invoice/replay entry; no unexplained full wipe occurs.

**Source:** `game/Main.Menus.cs`; `game/Main.PrepDetails.cs`; `game/Main.ControlRouting.cs`.

### PP-011 · Unaffordable choices remain selectable with negative draft balances

**P2 · source_gap · Shop UX**

**Basis.** The cycling interface allows illegal combinations and relies on ReadyPlan validation or timeout cleanup rather than preventing or explaining conflicts when selected.

**Action.** Show disabled/replace-to-afford states, the conflicting purchase and the amount missing. Permit browsing unavailable items without making the committed draft invalid.

**Acceptance.** All selection paths, including reverse cycling, controller navigation and mouse clicks, maintain a valid plan or visibly separate browsing from committing.

**Source:** `game/Main.Menus.cs`; `game/Main.PrepDetails.cs`; `game/Main.ControlRouting.cs`.

### PP-012 · Reserve floor can be mistaken for money available for a super

**P1 · design_risk · Shop semantics**

**Basis.** A reserve is a minimum remaining balance, not earmarked usable cash. At wallet 1200 and floor 900, a 300 EX is possible but a 900 super is blocked. The CPU comment describing a reserve as keeping EX attempts available is misleading.

**Action.** Relabel it as protected savings for a future round and hide it under advanced planning initially. Explain blocked actions. Do not add a second wallet or super stock to solve the terminology.

**Acceptance.** Players correctly predict EX and super availability for several wallet/floor pairs and understand that current-round combat generates no replacement income.

**Source:** `data/economy.json`; `data/items.json`; `src/StrikeLedger.Core/Simulation.cs`; `src/StrikeLedger.App/Bots.cs`; `game/Main.Menus.cs`; `game/Main.PrepDetails.cs`; `game/Main.ControlRouting.cs`.

### PP-013 · Local visible drafts and online hidden commitments create different games

**P1 · design_risk · Shop semantics**

**Basis.** Local preparation explicitly shows both draft panels; a player can lock before the opponent finishes responding. Online uses commitment hashes and simultaneous reveal. This is a material information-rule difference, not just a layout difference.

**Action.** Decide and document the competitive information policy. Treat shared-screen local visibility as an explicit limitation or use a deliberately public shop policy consistently. Do not claim cryptographic privacy on a shared display.

**Acceptance.** Run a late-counterpick test in both modes. The resulting advantage and lock/unlock behavior must match the declared rules, including timeout cases.

**Source:** `game/Main.Menus.cs`; `game/Main.PrepDetails.cs`; `game/Main.ControlRouting.cs`; `game/Main.Network.cs`; `src/StrikeLedger.App/PrivateMatchPeer.cs`; `src/StrikeLedger.App/RollbackSession.cs`; `game/GameSettings.cs`; `game/Main.cs`.

### PP-014 · Fixed slot prices are not evidence of tactical value

**P1 · design_risk · Shop design**

**Basis.** Prices are 900/600/300 by slot, while rentals have unlimited free activation for one round and compete with EX/super spending. These are different value models, and the recorded development did not establish item-level price balance.

**Action.** Evaluate marginal matchup value over the free kit, rather than price by category alone. Include threat, punishability, execution, repeated use, and the cost of giving up paid conversions.

**Acceptance.** Each rental has an explicit tactical hypothesis and matched comparisons against a base-kit/no-rental alternative. Investigate near-zero use and universal-pick items without treating either statistic as conclusive alone.

**Source:** `data/economy.json`; `data/items.json`; `src/StrikeLedger.Core/Simulation.cs`; `release_docs/BALANCE_NOTES.md`; `src/StrikeLedger.BalanceLab/Program.cs`.

### PP-015 · Rook feints have weak documented mechanical value

**P1 · design_risk · Shop design**

**Basis.** Rook step feint moves 13600 units over its movement window and locks a 16-tick action; sway moves 12800 backward. Sixteen ticks of free walking cover 48000 forward or 33600 backward. The rentals have no hitbox, invulnerability or cancel rule. A deceptive pose may still have value, but that needs demonstration.

**Action.** Give each feint an explicit bait or spacing purpose, or replace it with a better-defined sidegrade. Do not make it a universal safe cancel or an invulnerable free escape. Test visual deception separately from movement distance.

**Acceptance.** Human trials must exhibit a repeatable situation in which the feint earns its cost versus a free movement option. Empty-space movement arithmetic is a baseline, not a complete balance verdict.

**Source:** `data/fighters/rook.json`; `data/items.json`.

### PP-016 · Low Drive and Low Turn need a meaningful comparison

**P2 · design_risk · Shop design**

**Basis.** These Rook moves share the inspected startup/active/recovery and hitbox payload. They differ in slot, price, command, special/unique classification and whether forward HP is replaced. Those differences can matter, so neither is automatically dominated.

**Action.** Explain the actual trade: preserving forward HP and special cancel access versus cheaper command-normal replacement. Differentiate roles further only if playtesting shows redundant decisions.

**Acceptance.** The card comparison and training examples make their input/cancel/slot differences clear. Both must have a plausible reason to be chosen in different plans.

**Source:** `data/fighters/rook.json`; `data/items.json`; `src/StrikeLedger.Core/Simulation.Input.cs`; `src/StrikeLedger.Core/SimulationState.cs`; `game/Main.Menus.cs`; `game/Main.PrepDetails.cs`; `game/Main.ControlRouting.cs`.

### PP-017 · Opening-round options depend heavily on the weakest purchases

**P2 · design_risk · Shop design**

**Basis.** With 600 starting credits, the player cannot rent a 900 signature; a 600 technique leaves no cash for EX, while a 300 gambit leaves one EX. If gambits lack value, the opening buy decision narrows substantially.

**Action.** Playtest the first round separately from the late economy. Tune starter choices or prices only after demonstrating why buy, light-buy and save each have a sensible use.

**Acceptance.** Experienced players should not converge immediately on one opening plan for reasons unrelated to opponent, character or intended playstyle.

**Source:** `data/economy.json`; `data/items.json`; `src/StrikeLedger.Core/Simulation.cs`; `data/fighters/rook.json`.

### PP-018 · One-seed bot results do not establish competitive balance

**P1 · verification_gap · Economy balance**

**Basis.** The committed report covers 252 matches, 1620 rounds and one common seed with correlated mirrored cases. It explicitly disclaims human balance. Only a contract-enabling EX cancel change is reported, not a broad price/timing tuning pass.

**Action.** Broaden seeds, strategies, matchups and human skill-matched pairs after fixing policy coverage. Keep descriptive statistics separate from causal or competitive claims.

**Acceptance.** Report paired outcomes, sample dependence, failed runs, confidence limitations and actual item coverage. Include multiple human pairs before accepting shop or combat feel.

**Source:** `release_docs/BALANCE_NOTES.md`; `src/StrikeLedger.BalanceLab/Program.cs`.

### PP-019 · Recovery, draws and wallet cap need adversarial incentives testing

**P2 · design_risk · Economy balance**

**Basis.** The current economy has rising loss payouts, a 900 draw payout, a finite nine-round match and a 3600 cap. The existing controlled-losing experiment is helpful but does not rule out stronger policies; aggregate cap clipping alone is not evidence of a bad cap.

**Action.** Test score-aware conserving, intentional ties, yielding, high-budget match point and comeback policies. Plot cap clipping by state and starting-budget stratum.

**Acceptance.** No policy should gain a broadly dominant match-winning advantage merely by refusing to fight. Clearly distinguish deliberate final-round spending from an exploit.

**Source:** `release_docs/BALANCE_NOTES.md`; `src/StrikeLedger.BalanceLab/Program.cs`; `data/economy.json`; `data/items.json`; `src/StrikeLedger.Core/Simulation.cs`; `data/rules.json`.

### PP-020 · Match and preparation pacing lack human acceptance

**P1 · design_risk · Pacing**

**Basis.** Fight time is 60 seconds, preparation 15, reveal 3 and countdown 2, with up to nine rounds. Recorded native timing runs are not a human decision-time study.

**Action.** Measure active play, reading, decisions, waiting and repeats. Provide a learn mode without shop pressure and efficient competitive preparation with early ready and saved plans.

**Acceptance.** Observe complete friend-versus-friend sets: no unexplained waiting, frequent timeout confusion or desire to skip the defining shop. Select final defaults from those observations.

**Source:** `data/rules.json`; `data/economy.json`; `data/items.json`; `src/StrikeLedger.Core/Simulation.cs`; `reports/STATE.json`; `reports/AFTER_HOURS_INTEGRATION.md`.

### PP-021 · Shop money displays and some limits bypass canonical content

**P1 · source_gap · Content plumbing**

**Basis.** DraftCost hardcodes 900/600/300; preparation timing, cap/income projections and HUD EX/art costs also contain literals. Current data agrees, but future tuning can make the interface lie while Core charges the real item price.

**Action.** Expose typed read-only rules and a single affordability/plan-preview service. Remove duplicated price formulas and content-sensitive labels from UI code.

**Acceptance.** Change one item price and an activation price in an isolated valid tuning fixture. Every preview, debit, HUD label, bot decision and replay must agree.

**Source:** `game/Main.Menus.cs`; `game/Main.PrepDetails.cs`; `game/Main.ControlRouting.cs`; `game/Main.ArtSkin.cs`; `game/Main.Rendering.cs`; `game/Main.cs`; `src/StrikeLedger.Core/GameContent.cs`; `data/economy.json`; `data/items.json`; `src/StrikeLedger.Core/Simulation.cs`.

### PP-022 · Item-array order silently defines shop slots

**P2 · source_gap · Content plumbing**

**Basis.** ItemsFor filters by fighter; draft access assumes each slot occupies exactly two consecutive positions. The semantic slot field is not used for grouping the UI.

**Action.** Build explicit slot-to-item collections with stable IDs and declared display order. Do not encode a menu selection as an index into a global content array.

**Acceptance.** Randomly reorder items.json without changing its meaning. The shop must retain the same slot membership, prices and moves.

**Source:** `game/Main.Menus.cs`; `game/Main.PrepDetails.cs`; `game/Main.ControlRouting.cs`; `src/StrikeLedger.Core/GameContent.cs`; `game/Main.ArtSkin.cs`.

### PP-023 · Regular CPU cannot meaningfully adapt to revealed equipment

**P2 · source_gap · CPU**

**Basis.** BotObservation lacks opponent leases, selected-art cost and detailed spending context. The policy reacts primarily to distance/action and uses fixed purchases.

**Action.** Add a bounded delayed observation of public loadout and wallet information and a few explicit, testable economic strategies. Do not read opponent input or future state.

**Acceptance.** Against different revealed tools, a bot can demonstrate relevant counterplay and budget behavior without violating the reaction/input contract.

**Source:** `src/StrikeLedger.App/Bots.cs`; `game/Main.Network.cs`; `src/StrikeLedger.App/PrivateMatchPeer.cs`; `src/StrikeLedger.App/RollbackSession.cs`.

### PP-024 · Link and hit-confirm drills can use weak success predicates

**P1 · test_required · Training correctness**

**Basis.** TrainingSession marks a link from a normal started while actionable within 40 ticks of an earlier hit, then a hit with that move. It does not explicitly require continuous defender hitstun. Similar recent-hit/contact predicates deserve negative examples.

**Action.** Track combo/action identity, defender actionable gaps and the precise contact that authorizes a cancel. Separate a link, a reset and a blocked cancel.

**Acceptance.** A delayed reset after the dummy recovers must fail the link drill; true links must pass. A blocked follow-up after a prior unrelated hit must not pass a hit-confirm test.

**Source:** `src/StrikeLedger.App/Training.cs`; `game/Main.Training.cs`; `game/Main.LabState.cs`.

### PP-025 · Advanced drills rely on generic sparring rather than precise practice feeds

**P2 · source_gap · Training UX**

**Basis.** Several parry, quick-rise and reversal drills select Conservative as their dummy behavior, rather than a deterministic attack script tailored to the skill.

**Action.** Provide recorded demonstrations, clear reset positions and a drill-specific repeatable opponent sequence with optional randomization after mastery.

**Acceptance.** A learner can repeat the exact target interaction immediately, see why an attempt failed and complete it on either facing without waiting for a lucky bot action.

**Source:** `src/StrikeLedger.App/Training.cs`; `game/Main.Training.cs`; `game/Main.LabState.cs`; `src/StrikeLedger.App/Bots.cs`.

### PP-026 · Training lineup is constrained to the opposite character

**P2 · source_gap · Training UX**

**Basis.** TrainingSession constructs the dummy as the other fighter and leaves its super at the default. StartLab overwrites fighter[1] with that choice. Mirror-match and arbitrary opponent-art lab setups are not exposed in this path.

**Action.** Allow independent fighter, art, palette and equipment selection for both training actors. Synchronize the HUD to authoritative selected supers.

**Acceptance.** Practice Rook/Rook, Vale/Vale and both mixed orientations with every selected super and loadout; labels must match the actual simulation.

**Source:** `src/StrikeLedger.App/Training.cs`; `game/Main.Training.cs`; `game/Main.LabState.cs`; `game/Main.ArtSkin.cs`; `game/Main.Rendering.cs`; `game/Main.cs`.

### PP-027 · Loading a replay from paused playback does not reset shell playback state

**P2 · source_defect · Replay UX**

**Basis.** ReplayControls sets paused=true. Its archive button can load another recording through ReplayMenu; that callback replaces the replay but does not clear paused or reset replayAccumulator. A fresh replay can inherit the prior paused/partial-step state.

**Action.** Use one replay-start lifecycle that initializes pause, accumulators, effects and input overlays explicitly.

**Acceptance.** Open replay A, pause, choose archive, load B, and verify B starts in its declared playback state with no inherited fractional step or old effect.

**Source:** `src/StrikeLedger.App/Replay.cs`; `game/Main.Replays.cs`; `game/Main.Menus.cs`; `game/Main.EconomyReport.cs`.

### PP-028 · Results claim a replay was saved even after a save error

**P2 · source_defect · Replay UX**

**Basis.** SaveReplay catches errors and returns void; FinishMatch uses an unconditional saved message. The success message can contradict an error toast.

**Action.** Return a structured save result and display its actual outcome and path. Keep the match result usable even when storage fails.

**Acceptance.** Use a read-only or full destination. Results must clearly say saving failed, retain the in-memory recording and offer retry without implying a file exists.

**Source:** `game/Main.Menus.cs`; `game/Main.PrepDetails.cs`; `game/Main.ControlRouting.cs`; `src/StrikeLedger.App/Replay.cs`; `game/Main.Replays.cs`; `game/Main.EconomyReport.cs`.

### PP-029 · Archive sorts filenames and only exposes eight entries

**P2 · source_gap · Replay UX**

**Basis.** ReplayMenu takes the first eight paths from lexicographic descending order. Prefixes such as private- and match- influence ordering before the timestamp, and there is no reviewed pagination path.

**Action.** Index replay metadata by creation time, matchup, result, version and completion status; add scrolling, filters and explicit import errors.

**Acceptance.** With more than eight mixed local/private recordings, the actual newest entry appears first and every valid recording remains reachable.

**Source:** `src/StrikeLedger.App/Replay.cs`; `game/Main.Replays.cs`; `game/Main.Menus.cs`; `game/Main.EconomyReport.cs`.

### PP-030 · Replay corruption during playback needs a safe error boundary

**P2 · test_required · Replay reliability**

**Basis.** The import callback catches header/load failures, but state/hash/debit mismatches can arise later in ReplayPlayer.Apply, called from playback ticks and seek callbacks.

**Action.** Catch playback verification failures at the shell boundary, pause playback, preserve diagnostics and return to the archive without crashing or falsely labeling the remainder verified.

**Acceptance.** Tamper a later command in an otherwise loadable recording. Playback must stop at the failing tick and show a useful mismatch report.

**Source:** `src/StrikeLedger.App/Replay.cs`; `game/Main.Replays.cs`; `game/Main.Menus.cs`; `game/Main.EconomyReport.cs`.

### PP-031 · Replay persistence lacks collision-proof names and atomic replacement

**P2 · source_gap · Replay reliability**

**Basis.** SaveReplay uses timestamps only to the second and ReplayFormat.Save writes directly to the destination. Rapid saves or interrupted writes can overwrite or damage an archive entry.

**Action.** Use unique immutable recording IDs and temporary-write/atomic-rename persistence. Separate user labels from identity.

**Acceptance.** Two saves in one second create two independently valid files; an interrupted write never destroys an existing verified replay.

**Source:** `src/StrikeLedger.App/Replay.cs`; `game/Main.Replays.cs`; `game/Main.Menus.cs`; `game/Main.EconomyReport.cs`.

### PP-032 · All presentation events wait for confirmed remote input

**P1 · design_risk · Netplay feel**

**Basis.** RollbackSession drains only confirmed events, including local ActionStarted. This protects irreversible receipts but can delay responsive local attack sounds and effects under network latency.

**Action.** Split reversible presentation from confirmed accounting/result events. Predict local reversible cues, key them by stable action IDs and reconcile without duplicates; keep payouts/results authoritative.

**Acceptance.** Under controlled latency, measure button-to-local-cue delay and reconcile corrections. No doubled sound, false permanent KO or incorrect debit is allowed.

**Source:** `game/Main.Rendering.cs`; `src/StrikeLedger.App/RollbackSession.cs`; `src/StrikeLedger.Core/SimulationState.cs`; `game/Main.Network.cs`; `src/StrikeLedger.App/PrivateMatchPeer.cs`.

### PP-033 · Delayed impacts use current positions rather than contact-time positions

**P1 · source_gap · Netplay presentation**

**Basis.** CombatEvent carries actors/move but not contact coordinates. ObserveChanges resolves victim/actor coordinates from the current simulation when the event is drained, which may be later than the contact in netplay.

**Action.** Attach canonical event-time contact data or recover it from the event snapshot. Age short effects correctly when confirmed late.

**Acceptance.** With artificial delay and fast moving actors/projectiles, hit/parry/block sparks appear at the original contact, not at a later fighter location.

**Source:** `game/Main.Rendering.cs`; `src/StrikeLedger.App/RollbackSession.cs`; `src/StrikeLedger.Core/SimulationState.cs`; `src/StrikeLedger.Core/Simulation.Combat.cs`; `src/StrikeLedger.Core/Simulation.cs`.

### PP-034 · Both peers must manually duplicate the entire lineup

**P2 · source_gap · Netplay UX**

**Basis.** The UI constructs a full MatchConfig independently on each machine and Hello rejects any mismatch. This is safe validation but a brittle connection workflow.

**Action.** Add a small private lobby with seat-owned selections, a visible rules/content fingerprint and one agreed ready transition.

**Acceptance.** Two users can choose their own fighters/arts without coordinating the other seat by voice; incompatible content still fails explicitly.

**Source:** `game/Main.Network.cs`; `src/StrikeLedger.App/PrivateMatchPeer.cs`; `src/StrikeLedger.App/RollbackSession.cs`; `game/Main.Menus.cs`; `game/Main.PrepDetails.cs`; `game/Main.ControlRouting.cs`.

### PP-035 · Some exit paths dispose the peer without sending disconnect

**P2 · source_defect · Netplay lifecycle**

**Basis.** Title/exit/controller-loss paths dispose the peer. PrivateMatchPeer.Dispose closes transport while Disconnect is a separate method. The other endpoint can wait for timeout instead of receiving a prompt clean departure.

**Action.** Centralize best-effort graceful leave before disposal, with bounded shutdown and an explicit incomplete-match result. Never award an unconfirmed round.

**Acceptance.** Quit from fight, menu and controller-loss paths on either peer. The other side promptly explains the departure without hanging or granting predicted income.

**Source:** `game/Main.Network.cs`; `src/StrikeLedger.App/PrivateMatchPeer.cs`; `src/StrikeLedger.App/RollbackSession.cs`; `game/GameSettings.cs`; `game/Main.ControlRouting.cs`; `game/Main.cs`.

### PP-036 · Pause/resume needs asymmetric-latency tests

**P1 · test_required · Netplay synchronization**

**Basis.** Pause and Resume switch local status immediately and send a round-only control message without an agreed execution tick. This can produce different pause boundaries under delay; the current effect on synchronization needs execution evidence.

**Action.** Define an agreed pause tick or verified pause barrier and an unpause countdown appropriate to private matches.

**Acceptance.** Inject asymmetric delay/reordering while pausing during input prediction, a super and a terminal event. Both simulations converge without stuck queues, accidental movement or duplicate settlement.

**Source:** `game/Main.Network.cs`; `src/StrikeLedger.App/PrivateMatchPeer.cs`; `src/StrikeLedger.App/RollbackSession.cs`.

### PP-037 · Private rematch returns to connection setup

**P2 · source_gap · Netplay UX**

**Basis.** The result path sends private players back to NetworkMenu instead of negotiating an in-session rematch.

**Action.** Add mutual rematch/return-to-lobby controls with a fresh session match ID and zero cross-match economic carryover.

**Acceptance.** Run repeated private rematches with swapped seats and character/art changes; old inputs, plans and money cannot leak forward.

**Source:** `game/Main.Network.cs`; `src/StrikeLedger.App/PrivateMatchPeer.cs`; `src/StrikeLedger.App/RollbackSession.cs`; `game/Main.Menus.cs`; `game/Main.PrepDetails.cs`; `game/Main.ControlRouting.cs`.

### PP-038 · Canonical serialization allocates repeatedly in the hot path

**P2 · test_required · Performance**

**Basis.** Hash, Capture, snapshot Bytes and Restore make repeated arrays/streams or new simulations; rollback stores full snapshots around steps. This is a profiling target, not proof it caused reported stalls.

**Action.** Profile allocations and frame-phase costs before optimizing. Cache or pool safe buffers, preserve canonical order, and retain replay/hash equivalence.

**Acceptance.** Compare allocation rate, GC pauses and worst frames under rollback stress before/after optimization; all deterministic fixtures stay byte-identical where semantics are unchanged.

**Source:** `src/StrikeLedger.Core/Simulation.Serialization.cs`; `src/StrikeLedger.App/RollbackSession.cs`; `release_docs/KNOWN_LIMITATIONS.md`; `game/Main.Network.cs`; `src/StrikeLedger.App/PrivateMatchPeer.cs`.

### PP-039 · The recorded half-second frame outlier is unattributed

**P1 · verification_gap · Performance**

**Basis.** The current Windows pacing report records p99 around 17.316 ms but a worst interval around 557.276 ms. It does not establish whether the outlier occurred in combat, a transition, loading or unrelated scheduling.

**Action.** Add phase markers, frame histograms, GC/asset/I/O tracing and repeated cold/warm runs. Do not describe the single outlier as a confirmed combat hitch without attribution.

**Acceptance.** Explain all substantial stalls and establish an explicit interactive-combat frame budget on the actual target machine; keep movie-capture timing separate.

**Source:** `src/StrikeLedger.Core/Simulation.Serialization.cs`; `src/StrikeLedger.App/RollbackSession.cs`; `release_docs/KNOWN_LIMITATIONS.md`; `reports/STATE.json`; `reports/AFTER_HOURS_INTEGRATION.md`.

### PP-040 · Simultaneous command throws lack an explicit second-pass arbitration

**P1 · test_required · Combat edges**

**Basis.** Throw attempts are collected before application. Mutual techable throws are special-cased, but queued untechable/mixed attempts are then applied after the first may already clear or knock down the second attacker.

**Action.** Specify mutual-command and mixed-throw outcomes, then arbitrate from one immutable contact snapshot with a documented symmetric rule.

**Acceptance.** Mirrored exact-frame command/command and command/normal throw cases produce the declared outcome without executing a stale invalidated attempt or introducing a seat-order advantage.

**Source:** `src/StrikeLedger.Core/Simulation.Combat.cs`; `src/StrikeLedger.Core/Simulation.cs`.

### PP-041 · Air parry arm can survive a landing transition

**P1 · test_required · Combat edges**

**Basis.** CanParry treats ParryKind.Air as valid for any parryable hit without checking Grounded. The reviewed landing path does not explicitly clear or convert an armed air parry.

**Action.** Define whether an air parry expires on landing; enforce the chosen high/low and landing-recovery semantics explicitly.

**Acceptance.** Tap an air parry immediately before landing into low and overhead attacks. The result must follow the declared grounded/air rule, not leftover state.

**Source:** `src/StrikeLedger.Core/Simulation.Combat.cs`; `src/StrikeLedger.Core/Simulation.cs`; `src/StrikeLedger.Core/Simulation.Input.cs`; `src/StrikeLedger.Core/SimulationState.cs`.

### PP-042 · Taunt bypasses the normal action-start parry cleanup

**P1 · test_required · Combat edges**

**Basis.** Taunt is implemented as a recognition-time DashTicks mutation rather than TryStartAction. The normal paid/free action-start path clears parry; this taunt branch does not visibly do so.

**Action.** Route taunts through a normal explicit action transition or duplicate all intentional commitment invariants with tests.

**Acceptance.** Arm forward parry and taunt on the same tick, then strike during the taunt. No unintended free defense survives a supposedly committed taunt.

**Source:** `src/StrikeLedger.Core/Simulation.Input.cs`; `src/StrikeLedger.Core/SimulationState.cs`; `src/StrikeLedger.Core/Simulation.Combat.cs`; `src/StrikeLedger.Core/Simulation.cs`.

### PP-043 · Hurt silhouettes are generic outside active hitbox extension

**P1 · source_gap · Combat feel**

**Basis.** Hurtboxes select standing/crouching/air rectangles and append reduced active-limb rectangles. They are not authored for each startup, active and recovery pose.

**Action.** Author combat-relevant per-phase/per-frame hurtboxes and move them with meaningful silhouette changes. Preserve intentional disjoint and punish windows instead of tracing every decorative pixel.

**Acceptance.** Use paused overlays and human whiff-punish tests for both facings. A visibly extended recovering limb behaves according to an explicit combat rule.

**Source:** `src/StrikeLedger.Core/Simulation.Combat.cs`; `src/StrikeLedger.Core/Simulation.cs`; `game/Presentation/FighterSprites.cs`; `game/Presentation/FighterPalette.gdshader`; `reports/AFTER_HOURS_INTEGRATION.md`.

### PP-044 · Midair facing and blocked posture transitions need feel validation

**P2 · test_required · Combat feel**

**Basis.** UpdateFacing follows the broad Actionable flag, which can include a neutral airborne fighter; crouch/guard presentation also follows stored posture. These policies should be intentional rather than incidental consequences of a shared flag.

**Action.** Test cross-ups, neutral air turns, landing guard switches and charge retention against the intended original rules. Document chosen deviations from Third Strike rather than claiming exact emulation.

**Acceptance.** Mirrored cross-up/landing cases produce predictable controls, sprite facing and hurtbox posture without unexplained charge loss or guard appearance.

**Source:** `src/StrikeLedger.Core/Simulation.Input.cs`; `src/StrikeLedger.Core/SimulationState.cs`; `src/StrikeLedger.Core/Simulation.Combat.cs`; `src/StrikeLedger.Core/Simulation.cs`; `game/Main.ArtSkin.cs`; `game/Main.Rendering.cs`; `game/Main.cs`.

### PP-045 · Strict command timing has not been accepted by actual players

**P1 · verification_gap · Controls feel**

**Basis.** The command UI documents same-tick fresh PP/KK edges and the rules use no ordinary link buffer. These are deliberate choices, not automatically defects, but software input checks do not establish comfortable hardware execution.

**Action.** Run physical pad/stick/leverless timing tests with input diagnostics. Tune narrowly and record differences; retain manual motions and a clear competitive preset.

**Acceptance.** Two experienced players can reproduce intended motions, chords, charge releases, reversals and links on both sides with explainable failures.

**Source:** `src/StrikeLedger.Core/Simulation.Input.cs`; `src/StrikeLedger.Core/SimulationState.cs`; `game/GameSettings.cs`; `game/Main.ControlRouting.cs`; `game/Main.cs`; `reports/STATE.json`; `reports/AFTER_HOURS_INTEGRATION.md`; `data/rules.json`.

### PP-046 · Analog triggers are not supported as action bindings

**P2 · source_gap · Controls breadth**

**Basis.** Pad mappings and capture accept JoyButton values, while the reviewed raw sampler reads only the left-stick axes for directions. Trigger axes do not have an action-binding path.

**Action.** Add typed button/axis bindings with press/release thresholds and hysteresis. Preserve dedicated menu/pause routing.

**Acceptance.** Map an EX chord or attack to either trigger and test hold, release, chatter, hotplug and simultaneous directions on physical hardware.

**Source:** `game/GameSettings.cs`; `game/Main.ControlRouting.cs`; `game/Main.cs`.

### PP-047 · Keyboard sampling combines physical and logical bindings unconditionally

**P2 · test_required · Controls portability**

**Basis.** ReadRaw uses IsPhysicalKeyPressed OR IsKeyPressed for each configured key. This may alias different physical keys on non-QWERTY layouts rather than behaving as a fallback only when scan codes are unavailable.

**Action.** Separate physical-position bindings from explicit logical-key accessibility fallback and test their interaction.

**Acceptance.** On alternate layouts and injected-event paths, each bound action has exactly the intended physical/logical source and no unintended duplicate control.

**Source:** `game/GameSettings.cs`; `game/Main.ControlRouting.cs`; `game/Main.cs`.

### PP-048 · Mapped move phases are not complete bespoke animations

**P1 · polish_opportunity · Art fidelity**

**Basis.** The integration report explicitly says the renderer composes moves from shared anticipation/recovery key poses. Its 415 selection checks establish coverage, not unique in-betweens or animation quality.

**Action.** Prioritize unique idle/walk/turn, high-frequency normals, hit/guard/parry, throw and recovery animations. Then refine EX/super sequences. Use original art consistent with the desired arcade feel.

**Acceptance.** Review complete legal-input matches at normal speed plus frame strips; every key move reads clearly and both fighters have distinct movement personality.

**Source:** `game/Presentation/FighterSprites.cs`; `game/Presentation/FighterPalette.gdshader`; `reports/AFTER_HOURS_INTEGRATION.md`; `reports/STATE.json`.

### PP-049 · Cel timing is uniformly distributed within broad action phases

**P2 · polish_opportunity · Art timing**

**Basis.** FighterSprites selects cels proportionally across startup/active/recovery; loop states use fixed tick intervals. This limits authored anticipation, impact holds and follow-through.

**Action.** Add explicit per-cel holds and event markers tied to the immutable combat timeline. Do not stretch active windows to fit a drawing.

**Acceptance.** Visual first-active, contact, recovery and cancellable frames line up with debug timelines, including hitstop, superfreeze, slow motion and rollback.

**Source:** `game/Presentation/FighterSprites.cs`; `game/Presentation/FighterPalette.gdshader`; `reports/AFTER_HOURS_INTEGRATION.md`; `game/Main.Rendering.cs`; `src/StrikeLedger.App/RollbackSession.cs`; `src/StrikeLedger.Core/SimulationState.cs`.

### PP-050 · Runtime magenta keying is a provisional asset pipeline

**P2 · polish_opportunity · Art pipeline**

**Basis.** Current fighter sheets are RGB matte images, and the shader removes magenta. The integration report is honest about this; no actual edge artifact was independently viewed in this audit.

**Action.** Prepare genuine alpha assets offline with edge/color cleanup and provenance preservation. Validate over light, dark and moving backgrounds.

**Acceptance.** Inspect every selected cel for opaque matte remnants, colored fringe, missing costume pixels and alpha consistency. Retain hashes and source boards.

**Source:** `game/Presentation/FighterSprites.cs`; `game/Presentation/FighterPalette.gdshader`; `reports/AFTER_HOURS_INTEGRATION.md`.

### PP-051 · Heuristic palette recoloring is fragile

**P2 · polish_opportunity · Art pipeline**

**Basis.** The palette shader identifies costume pixels with RGB threshold tests rather than authored masks. This can affect unrelated colors as art evolves.

**Action.** Use palette indices or explicit material masks and an accessible alternate costume with stable silhouette contrast.

**Acceptance.** Compare all cel states in both palettes; intended material colors change and skin, shadows, effects and edges remain correct.

**Source:** `game/Presentation/FighterSprites.cs`; `game/Presentation/FighterPalette.gdshader`; `reports/AFTER_HOURS_INTEGRATION.md`.

### PP-052 · Portraits are stretched into incompatible aspect ratios

**P2 · polish_opportunity · UI fidelity**

**Basis.** TextureRects use Scale, and face crops around 274x335 are displayed in square or near-square rectangles. This mathematically changes aspect ratio even without a screenshot review.

**Action.** Use aspect-preserving crop-to-fill or contain layouts with a consistent portrait camera/crop specification.

**Acceptance.** Portrait proportions remain consistent across select, HUD, results and shop, including mirrored matchups and supported resolutions.

**Source:** `game/Main.ArtSkin.cs`; `game/Main.Rendering.cs`; `game/Main.cs`.

### PP-053 · Menu layout and focus ownership depend on pixel coordinates

**P2 · source_gap · UI fidelity**

**Basis.** Controls are placed at fixed 1280x720 coordinates; preparation focus infers seat and row from X/Y thresholds. This is brittle under UI growth, font scaling and accessibility changes.

**Action.** Use reusable semantic panels, containers, focus neighbors and explicit seat/slot ownership. Provide safe scaling and scrolling.

**Acceptance.** Large text, long item names, keyboard/pad navigation and all supported resolutions preserve readable, reachable controls without overlap.

**Source:** `game/Main.Menus.cs`; `game/Main.PrepDetails.cs`; `game/Main.ControlRouting.cs`; `game/GameSettings.cs`; `game/Main.cs`; `game/Main.ArtSkin.cs`; `game/Main.Rendering.cs`.

### PP-054 · Typography and shop labels do not yet match the authored art direction

**P2 · polish_opportunity · UI fidelity**

**Basis.** The interface uses ThemeDB.FallbackFont while decoration is supplied by After Hours image boards. This is a source-based integration observation, not a visual verdict from current footage.

**Action.** Adopt a licensed, readable UI type system with heading/body/numeric styles, controller glyphs, alignment and contrast standards. Keep dynamic text as actual text.

**Acceptance.** Wallets, costs, commands and focus states are readable at the smallest target display and from couch distance; no essential instructions are baked into images.

**Source:** `game/Main.ArtSkin.cs`; `game/Main.Rendering.cs`; `game/Main.cs`; `game/Main.Menus.cs`; `game/Main.PrepDetails.cs`; `game/Main.ControlRouting.cs`; `AUDIT_GUIDE.md`; `README.md`; `docs/00_PRODUCT_CONTRACT.md`; `docs/01_DECISIONS_AND_AUTHORITY.md`.

### PP-055 · HUD assumes fixed health and art-price formulas

**P2 · source_gap · HUD fidelity**

**Basis.** Health is divided by 1000 instead of MaxHealth; EX and art prices are encoded as literals/formulas. These match current content but obstruct reliable tuning and roster growth.

**Action.** Render authoritative normalized health, selected-art data and affordability from the simulation/content model.

**Acceptance.** A controlled max-health/cost variation displays correctly without changing UI code; replays and training show their actual selected arts.

**Source:** `game/Main.ArtSkin.cs`; `game/Main.Rendering.cs`; `game/Main.cs`; `src/StrikeLedger.Core/GameContent.cs`; `game/Main.Menus.cs`.

### PP-056 · Resource readability can improve without restoring a combat meter

**P2 · polish_opportunity · HUD fidelity**

**Basis.** The HUD shows credits, EX readiness, art price, reserve and numeric stun, but the relationship among the wallet and currently affordable combinations remains mentally demanding.

**Action.** Show concise examples such as selected super plus one EX, or insufficient by 300. Keep a single wallet; do not turn the display into stored activation stocks. Add readable health damage trails and clear nonspendable stun treatment.

**Acceptance.** Players can infer available paid actions at a glance, including reserve effects, while never confusing displayed affordability with prepaid uses.

**Source:** `game/Main.ArtSkin.cs`; `game/Main.Rendering.cs`; `game/Main.cs`; `data/economy.json`; `data/items.json`; `src/StrikeLedger.Core/Simulation.cs`.

### PP-057 · Stage presentation is largely one scrolling image

**P2 · polish_opportunity · Stage fidelity**

**Basis.** ArenaView draws one Foundry bitmap with a small offset, plus separately drawn corners, shadows and effects.

**Action.** Add restrained far/mid/foreground depth, ambient movement and coherent fighter grounding. Keep foregrounds and flashes out of the tactical silhouette.

**Acceptance.** In-motion readability, corner visibility and frame pacing remain at least as good as the baseline in both palettes and reduced-flash mode.

**Source:** `game/Presentation/ArenaView.cs`; `data/stages/foundry.json`; `src/StrikeLedger.Core/Simulation.Combat.cs`; `game/Presentation/FighterSprites.cs`; `game/Presentation/FighterPalette.gdshader`; `reports/AFTER_HOURS_INTEGRATION.md`.

### PP-058 · Generic impact selection understates move identity

**P2 · polish_opportunity · Impact fidelity**

**Basis.** Effects and strength are largely selected through move-name suffixes and generic size/rank buckets. Projectiles also share a few cel ranges.

**Action.** Author per-move impact profile data: contact point, light/heavy/EX distinction, projectile geometry, limited camera response and character-specific sound layer.

**Acceptance.** Players distinguish hit, block, parry, counterhit, EX and super without reading debug text, and effects never conceal the next actionable interaction.

**Source:** `game/Main.Rendering.cs`; `src/StrikeLedger.App/RollbackSession.cs`; `src/StrikeLedger.Core/SimulationState.cs`; `game/Presentation/ArenaView.cs`; `data/stages/foundry.json`; `src/StrikeLedger.Core/Simulation.Combat.cs`; `game/Presentation/ArenaAudio.cs`.

### PP-059 · Cosmetic wall-clock aging needs pause and frame-advance review

**P2 · test_required · Presentation lifecycle**

**Basis.** ArenaView runs Always and ages impacts/wallet cues using process delta, while fighter clips follow simulation ticks. This can make effects disappear while training or replay is paused.

**Action.** Define which ambient effects may continue and which combat effects must follow the simulation/presentation timeline. Reset appropriately on seek and rollback.

**Acceptance.** Pause on contact, advance one frame at a time and seek backward. Relevant impact cues remain inspectable and do not accumulate stale effects.

**Source:** `game/Presentation/ArenaView.cs`; `data/stages/foundry.json`; `src/StrikeLedger.Core/Simulation.Combat.cs`; `game/Presentation/FighterSprites.cs`; `game/Presentation/FighterPalette.gdshader`; `reports/AFTER_HOURS_INTEGRATION.md`; `src/StrikeLedger.App/Replay.cs`; `game/Main.Replays.cs`; `game/Main.Menus.cs`; `game/Main.EconomyReport.cs`.

### PP-060 · Audio has a foundation but limited variation and character identity

**P2 · polish_opportunity · Audio fidelity**

**Basis.** ArenaAudio loads a small generic cue set and one Foundry loop, dispatches by name category and reuses ten voices. No claim about subjective mix quality is made without listening.

**Action.** Add controlled impact variations, character vocal/breath identity, distinctive parry/EX/super accents and restrained phase-aware music layers. Prioritize mix hierarchy over volume.

**Acceptance.** Blind listening can separate major combat outcomes; repeated common attacks do not sound identical or mask defense cues, and rapid events do not cut off critical audio.

**Source:** `game/Presentation/ArenaAudio.cs`; `game/Main.Rendering.cs`; `src/StrikeLedger.App/RollbackSession.cs`; `src/StrikeLedger.Core/SimulationState.cs`.

### PP-061 · Terminal feedback does not consistently distinguish result reasons

**P2 · source_gap · Match presentation**

**Basis.** The presentation maps RoundTerminal to a KO cue, and the draw path uses generic settlement/announcement handling. KO, timeout and double-KO should be rendered from the actual reason and winner state.

**Action.** Add distinct reason-aware end-of-round presentation, a concise transaction summary and clear match-point cues without extending combat freezes.

**Acceptance.** Normal KO, chip KO, timeout, double-KO, round draw and final match draw each produce accurate text, audio, portraits and accounting.

**Source:** `game/Main.ArtSkin.cs`; `game/Main.Rendering.cs`; `game/Main.cs`; `src/StrikeLedger.App/RollbackSession.cs`; `src/StrikeLedger.Core/SimulationState.cs`; `data/rules.json`.

### PP-062 · Linux verification status contradicts the final integration report

**P2 · source_defect · Documentation**

**Basis.** KNOWN_LIMITATIONS says new After Hours Linux movies/package checks are still being completed, while the final integration report and STATE say they passed. This is a real stale status inconsistency.

**Action.** Update status documents from one authoritative candidate record and preserve the remaining WSLg/llvmpipe/audio/hardware qualifications.

**Acceptance.** All player/release/audit docs agree on what was run, on which environment and for which candidate, without turning historical tests into new runs.

**Source:** `release_docs/KNOWN_LIMITATIONS.md`; `reports/AFTER_HOURS_INTEGRATION.md`; `AUDIT_GUIDE.md`; `reports/STATE.json`.

### PP-063 · Actual controllers, two-PC play and human feel remain open

**P1 · verification_gap · Verification**

**Basis.** The current ledger lists DEVICE-003, DEVICE-004 and HUMAN-001 as not run. Injected GUI events and same-machine network processes cannot substitute for these checks.

**Action.** Complete the real hardware and friend-versus-friend sessions after correcting routing and camera issues. Keep software, device and human verdicts separate.

**Acceptance.** Record real device models, machine roles, latency conditions, build hashes and unedited observations; do not auto-approve human acceptance.

**Source:** `reports/STATE.json`; `reports/AFTER_HOURS_INTEGRATION.md`; `release_docs/KNOWN_LIMITATIONS.md`; `AUDIT_GUIDE.md`; `game/Main.UiSmoke.cs`; `src/StrikeLedger.CoreTests/Program.cs`.

### PP-064 · Feature-count gates miss emergent player-facing failures

**P1 · source_gap · Verification**

**Basis.** All-action tests and UI smoke demonstrate valuable slices, but the inspected UI smoke reaches combat then tests Start; it does not demonstrate every combat button leaves the shell in fight. Coverage counts alone cannot establish quality.

**Action.** Add boundary-focused end-to-end regressions for combat inputs, maximum separation, actual rental practice, malformed saves and multi-round human-shaped traces.

**Acceptance.** The new regression set must fail on the identified source defects and pass only after real fixes, without weakening existing accounting or replay gates.

**Source:** `game/Main.UiSmoke.cs`; `src/StrikeLedger.CoreTests/Program.cs`; `AUDIT_GUIDE.md`; `game/GameSettings.cs`; `game/Main.ControlRouting.cs`; `game/Main.cs`; `game/Presentation/ArenaView.cs`; `data/stages/foundry.json`; `src/StrikeLedger.Core/Simulation.Combat.cs`.

### PP-065 · No checked-in GitHub Actions workflow at the audited root

**P2 · source_gap · Engineering**

**Basis.** The audited root tree has no .github directory. Local build/test tools exist; absence of a checked-in workflow is not a claim that no external automation exists.

**Action.** Add headless Core/App/schema/conformance CI plus a controlled native GUI/export job where supported. Cache tools with version verification and avoid fake hardware passes.

**Acceptance.** A fresh pull request runs the relevant checks and exposes logs; protected branches can require those checks once stable.

**Source:** `repository root tree at audited commit`; `repository branch and issue metadata at audited commit`; `AUDIT_GUIDE.md`; `README.md`; `docs/00_PRODUCT_CONTRACT.md`; `docs/01_DECISIONS_AND_AUTHORITY.md`.

### PP-066 · Roster and content shape are hardcoded across layers

**P2 · source_gap · Engineering**

**Basis.** The loader requires exactly Rook/Vale, 49 actions each, 12 items and two stages. GUI/art selection contains matching binary assumptions. This is valid bounded scope but expensive to extend.

**Action.** Before a third character, introduce registries and content-driven menus/rendering. Keep exactly two match seats while allowing a variable selectable roster.

**Acceptance.** A test-only third registry entry is selectable and validates without changing two-seat simulation rules; remove the fixture before claiming a shipped character.

**Source:** `src/StrikeLedger.Core/GameContent.cs`; `game/Main.Menus.cs`; `game/Main.ArtSkin.cs`; `game/Presentation/FighterSprites.cs`; `game/Presentation/FighterPalette.gdshader`; `reports/AFTER_HOURS_INTEGRATION.md`; `AUDIT_GUIDE.md`; `README.md`; `docs/00_PRODUCT_CONTRACT.md`; `docs/01_DECISIONS_AND_AUTHORITY.md`.

### PP-067 · Dense shell functions make behavior changes risky

**P2 · polish_opportunity · Engineering**

**Basis.** Many menu, lifecycle and input functions are compressed into long one-line statements and spread across partial Main files, with semantic control ownership inferred from layout.

**Action.** Format and separate flow coordination, shop view model, input context and presentation adapters. Refactor under existing golden replay and UI behavior tests, not as a rewrite.

**Acceptance.** Behavior-preserving refactoring maintains exact Core/App outcomes and leaves the new routing/transaction boundaries independently testable.

**Source:** `game/Main.Menus.cs`; `game/Main.PrepDetails.cs`; `game/Main.ControlRouting.cs`; `game/GameSettings.cs`; `game/Main.cs`; `game/Main.Network.cs`; `src/StrikeLedger.App/PrivateMatchPeer.cs`; `src/StrikeLedger.App/RollbackSession.cs`.

### PP-068 · Source-only checkout is not the complete historical evidence bundle

**P2 · verification_gap · Verification**

**Basis.** AUDIT_GUIDE explains that bulky evidence and exports are distributed separately and historical PACK_MANIFEST is not the current source manifest. A missing historical artifact in a fresh clone is not automatically a fake pass.

**Action.** Provide a simple, hash-verified evidence restoration/readme flow and distinguish historical candidate verification from a fresh rebuild.

**Acceptance.** A reviewer can restore the documented companion evidence and verify hashes, or run new tests with fresh IDs, without renaming old evidence as newly executed.

**Source:** `AUDIT_GUIDE.md`; `README.md`; `docs/00_PRODUCT_CONTRACT.md`; `docs/01_DECISIONS_AND_AUTHORITY.md`; `reports/STATE.json`; `reports/AFTER_HOURS_INTEGRATION.md`.

### PP-069 · Distribution rights and attribution need an explicit shipping checklist

**P2 · design_risk · Release**

**Basis.** The repository is private and the reviewed root does not contain a general code LICENSE. Original generated assets and tool dependencies still need an intentional distribution policy; this is not a finding of infringement.

**Action.** Before public distribution, document code licensing choice, engine/runtime notices, asset provenance and font/audio rights in the actual package.

**Acceptance.** A fresh export contains the intended notices and an asset-source manifest; no unlicensed reference-game assets are introduced to improve fidelity.

**Source:** `repository root tree at audited commit`; `repository branch and issue metadata at audited commit`; `AUDIT_GUIDE.md`; `README.md`; `docs/00_PRODUCT_CONTRACT.md`; `docs/01_DECISIONS_AND_AUTHORITY.md`; `game/Presentation/FighterSprites.cs`; `game/Presentation/FighterPalette.gdshader`; `reports/AFTER_HOURS_INTEGRATION.md`.

### PP-070 · Saved economic plans with transparent affordability

**P2 · optional_feature · Feature**

**Basis.** The current shop is a per-round cycling draft rather than a player-authored plan library.

**Action.** Add a few editable plans such as base kit/save, light tool plus EX, selected-super budget, and a rental counterplan. Reprice live; never silently downgrade the plan.

**Acceptance.** A saved plan adapts visibly to a changed wallet/cost version and still uses the same scalar wallet.

**Source:** `game/Main.Menus.cs`; `game/Main.PrepDetails.cs`; `game/Main.ControlRouting.cs`; `data/economy.json`; `data/items.json`; `src/StrikeLedger.Core/Simulation.cs`.

### PP-071 · A compact in-game round economy ledger

**P2 · optional_feature · Feature**

**Basis.** Economy export and replay wallet graphs already exist; the opportunity is to make the useful history available directly during preparation/results.

**Action.** Show previous opening cash, rental spend, EX/super spend, payout, cap clipping and closing cash, plus the next-round budget. Tie every number to existing receipts.

**Acceptance.** The in-game ledger agrees with replay JSON/CSV and authoritative receipts across retries, draws and rollbacks.

**Source:** `src/StrikeLedger.App/Replay.cs`; `game/Main.Replays.cs`; `game/Main.Menus.cs`; `game/Main.EconomyReport.cs`; `data/economy.json`; `data/items.json`; `src/StrikeLedger.Core/Simulation.cs`.

### PP-072 · Replay takeover for missed punish and spending decisions

**P2 · optional_feature · Feature**

**Basis.** Replay and training are implemented but the reviewed interfaces do not expose a takeover branch.

**Action.** Branch a replay moment into a clearly marked training session, retaining actual spacing, wallet, leases and selected arts. Keep the original replay immutable.

**Acceptance.** Players can retry a costly whiff or missed confirm; the branch can never be saved as the original competitive result.

**Source:** `src/StrikeLedger.App/Replay.cs`; `game/Main.Replays.cs`; `game/Main.Menus.cs`; `game/Main.EconomyReport.cs`; `src/StrikeLedger.App/Training.cs`; `game/Main.Training.cs`; `game/Main.LabState.cs`.

### PP-073 · Budget-aware combo and counterplay trials

**P2 · optional_feature · Feature**

**Basis.** The game already has drill and transaction foundations. They can teach the economic tradeoff rather than only execution mechanics.

**Action.** Author free, EX and super routes from the same starter, showing cost, damage, knockdown and remaining money. Add a drill for countering each rental.

**Acceptance.** Trials evaluate actual legal input and actual combo continuity, not canned animation or a recent-hit heuristic.

**Source:** `src/StrikeLedger.App/Training.cs`; `game/Main.Training.cs`; `game/Main.LabState.cs`; `data/economy.json`; `data/items.json`; `src/StrikeLedger.Core/Simulation.cs`.

### PP-074 · Multi-slot dummy recordings and randomized defense

**P3 · optional_feature · Feature**

**Basis.** The reviewed lab keeps one looping dummy-input sequence.

**Action.** Add several named slots, randomized playback with visible weights, guard-after-first-hit, reversal and wake-up action settings.

**Acceptance.** Dummy state, random seed and playback position survive training checkpoints and exported traces reproducibly.

**Source:** `src/StrikeLedger.App/Training.cs`; `game/Main.Training.cs`; `game/Main.LabState.cs`.

### PP-075 · Private-session connection diagnostics and lobby quality

**P3 · optional_feature · Feature**

**Basis.** The existing netplay scope is a private link, not public matchmaking. Better feedback can improve it without adding accounts or ranked services.

**Action.** Expose measured RTT/jitter/loss, input delay, rollback depth/stalls, content compatibility and a clear ready/rematch lobby.

**Acceptance.** Diagnostics reflect injected and real conditions, explain a stall honestly and never present the match phrase as strong public authentication.

**Source:** `game/Main.Network.cs`; `src/StrikeLedger.App/PrivateMatchPeer.cs`; `src/StrikeLedger.App/RollbackSession.cs`.

### PP-076 · Evidence-linked post-match coaching

**P3 · optional_feature · Feature**

**Basis.** Replay events and economy receipts provide factual inputs for a small post-match review; no model needs to invent opponent intent.

**Action.** Offer timestamped expensive whiffs, unused supported rentals, avoidable punish windows and missed affordable routes. Label inference separately from recorded events.

**Acceptance.** Every note jumps to the supporting moment, and an unused tool is not automatically called a mistake when its threat may have influenced play.

**Source:** `src/StrikeLedger.App/Replay.cs`; `game/Main.Replays.cs`; `game/Main.Menus.cs`; `game/Main.EconomyReport.cs`; `release_docs/BALANCE_NOTES.md`; `src/StrikeLedger.BalanceLab/Program.cs`.

### PP-077 · Compact arcade/rematch motivation after the duel works

**P3 · optional_feature · Feature**

**Basis.** The bounded prototype does not require a campaign, large roster or persistent power progression.

**Action.** Add a short arcade ladder, personal bests, cosmetic unlocks or local set tracking only after the primary loop succeeds. Keep competitive power and money reset between matches.

**Acceptance.** Optional progression never changes 1v1 combat statistics, cross-match credits or access to the complete free character kit.

**Source:** `AUDIT_GUIDE.md`; `README.md`; `docs/00_PRODUCT_CONTRACT.md`; `docs/01_DECISIONS_AND_AUTHORITY.md`; `data/rules.json`.

## Important non-findings

The absence of a public ranked backend, team mode, large roster or story campaign is not a failure of the bounded contract. Roster registries are preparation for future expansion, not a requirement to add characters now. A historical acceptance artifact missing from a fresh source-only clone is not proof of fabricated tests. A draw payout is not automatically an infinite farming exploit in a finite nine-round match. Identical per-hit numbers do not alone establish that one purchase dominates another. Runtime magenta keying is a pipeline weakness, not proof that this audit saw colored fringes. The Windows frame outlier is not yet attributed to active combat.

## Final acceptance should be an experience, not a percentage

A practical finish is two people launching a fresh build, using actual devices, completing several full economic matches without coaching around controls or menus, understanding their purchases, demonstrating reasons to buy and save, and reporting that offense, parries, throws, movement and feedback feel intentional. Tests support that verdict; they do not replace it.


---

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


---

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
