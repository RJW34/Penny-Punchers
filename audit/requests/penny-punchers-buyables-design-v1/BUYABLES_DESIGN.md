# Penny-Punchers — full buyables design

**Proposal v1 · strictly 1v1 · one persistent credit wallet · Third Strike-inspired feel.**

## 1. The design thesis

**Buy a fighting plan, not a stronger health bar. Spend on a move when the opening is worth the money.**

A conventional spendable combat resource often makes the player choose between an enhanced special, a conversion, a reversal and a larger super. This game should retain those interesting choices and add a longer horizon: the same money could instead finance an additional technique for the next round. Merely replacing a gauge with a number while giving every player the same cash every few seconds would lose much of that new decision.

The current audit baseline already has direct credit spending and temporary techniques. Its weaknesses are purchase usefulness, communication, and validation, not a need to put every basic attack behind a fee. The existing audit reports no global purchasable stat upgrades. This proposal preserves that distinction and adds stronger move identities.

This is a completed design section, not a statement that these moves exist in the executable. The specific prices and target timings are proposals. The eight enhanced families and four conventional supers begin from audited baseline data for comparison; even their existing values are not assumed competitively settled. Every catalog card has a role, a free-kit comparison, an opponent response, a presentation requirement and a test.

## 2. Borrow the function, not every rule of the reference game

Street Fighter references are a vocabulary for what a move does. The project's actual numbers, animations, names and rules remain original. Reference properties differ between installments. In particular, Capcom's Omega Edition examples below are **not** represented as Third Strike frame data or ordinary SFIV behavior.

| Reference family | Useful design lesson | Economic expression here |
|---|---|---|
| Hadoken / Sonic Boom | A repeatable spatial threat and timing challenge | Keep the ordinary projectile free; pay for an enhanced activation or rent a distinct trajectory. |
| Shoryuken / Somersault Kick | Vertical interception and committed responses | Keep a competent free anti-air; pay for the specifically authored enhanced/reversal version. |
| Screw Piledriver-style command throws | Challenge defense differently from a strike | Character-specific round rental; long enough commitment and clear counters; ordinary throw stays free. |
| Dudley-style sway / duck / follow-up / feint families | Manipulate a particular attack path and the opponent's timing | Rent a bounded approach or bait, not passive dodge chance or universal invulnerability. |
| Rekka-style follow-ups | Stop, delay or commit farther through a sequence | Rent a finite authored branch sequence with measured gaps, not a universal cancel button. |
| Cannon Strike-style dive kick | Change jump timing and contact height | Advanced character-specific rental with a minimum height and punishable bad contact. |
| Urien's Aegis-type barrier | Spend to create a spatial problem, not necessarily immediate damage | Selected paid setup super with a finite object lifetime and hard interaction limits. |
| Yun's Genei Jin / install-type routing idea | A spend can open new execution paths instead of playing a canned damage animation | Selected timed HIT-only route super; no global damage/speed buff, no new bank of energy. |
| Oro's Tengu Stone / Rose's shadow follow-ups | Delayed objects can change conversions and pressure | Future archetype candidates with no random power and no second resource; not automatically added to both current fighters. |
| Yoga Teleport-type reposition | Position can be a tactical reward | A visible, constrained, vulnerable relocation—not a bought escape from every bad situation. |

Primary references: `data/research_sources.json`. Capcom's official command lists establish move families; its developer articles support the barrier, sway/feint, install, and object concepts. The detailed behavior of every proposed Penny-Punchers move below is original, not borrowed factual data.

## 3. Three economic operations, not three currencies

| Operation | When money leaves the wallet | What the player receives |
|---|---|---|
| **Round technique** | Once, when the preparation plan atomically locks | One available additional/replacement action or branch family, repeatable after recovery until the round ends. |
| **Enhanced action** | When the valid enhanced move enters committed startup | That one move attempt. No fee merely to unlock it in the shop. |
| **Selected super** | When that valid selected super enters committed startup | That one super attempt or its finite active state/object. Selecting it at match setup is free. |

There is no fourth operation that sells super tickets, EX ammunition, interest, passive meter gain or an extra resource bar. A projectile's remaining contacts, a throw's capture state, an armor flag local to one attack, and an install's expiration time are normal move state, not separately owned consumable currency. No such state is banked between rounds or earned by farming attacks.

A failed motion is not a purchase. An illegal transition or unaffordable valid command rejects without debit and without a surprise downgraded action. Once a move commits, whiff/block/parry/interruption never refunds it. Rolls back with corrected input? Restore the debit as part of the same snapshot, not as a second compensating deposit.

Every ordinary normal, special, movement action, ground throw, block, parry and system recovery remains available at zero money. Not every losing position must be escapable, but an opponent must not need to buy the one item that makes the basic matchup playable.

## 4. Loadout rules and presentation

Keep three optional rental slots:

- **Signature:** a substantial extra approach, sequence, throw or projectile identity.
- **Technique:** a particular command-normal replacement or limited offensive route.
- **Gambit:** a small read, spacing action, feint or narrow projectile response.

There are four proposed choices per slot per fighter. Equip at most one per slot. There is no cross-fighter shopping and no equipment retained by the winner. Rentals expire every round. The reference 1,800-credit total rental limit remains for comparability, but the price belongs to the item: a technique is not automatically 600 merely because of its slot.

**None** is a first-class choice. Do not call a filled loadout automatically stronger. Three techniques bought for 1,800 from a 1,800 wallet leave no EX/super money. The no-rental player has a different legitimate plan.

Use 300-credit increments for this first catalog, to remain legible beside the 300-credit EX baseline. This is a starting presentation and experimentation choice, not proof that future 450-credit pricing is inherently wrong. Do not change payout policy in the same experiment as these move prices.

Super choice remains match-locked, free to select and one art per fighter. The new install and barrier are proposed replacements for the third arts, not additional simultaneously available supers. Keep old arts in the actual build until replacement implementation and evidence are complete; test candidates under a new ruleset identity.

## 5. The complete candidate catalog

**38 entries: 24 round techniques, 8 enhanced actions, 6 supers.**

`core_trial` means the first controlled playtest pool, not "already implemented". `advanced_trial` means the design is specified here but requires the listed mechanics and interaction tests before it belongs in ordinary play. The first pool has 24 entries; the advanced pool has 14. This staging does not waive the project's required final functionality.

All startup/active/recovery values below use the intended 60-tick action timeline. They are seeds, not measured frame advantage. A move's "active" field may describe a counter/travel interval instead of a hitbox; the mechanic contract is authoritative. Air/hold/branch/field actions have explicitly different subphases in the combat contracts. Collision geometry and exact block advantage must be authored and tested in the engine, not inferred from the printed summary.


## Round-long techniques


### Rook

#### R-S1 — Clinch Entry

**Command throw · core_trial · 900 credits at preparation lock; then free activation.**  
**Command:** `qcb+P`. **Slot:** signature. 

**Why buy/spend:** An opponent relies on blocking, grounded parry and ordinary throw-tech timing at close range.

**Actual proposed behavior:** Add one grounded, untechable command throw. It cannot capture airborne/prejump, hitstunned, blockstunned, wakeup-immune or already captured targets. It ends in hard knockdown, not a free restand combo. No armor, travel, outgoing cancel or frame-zero grab. Fresh P is one punch.

**Compared with free play:** Normal throws remain free, faster and techable. The purchase pays for the different defense interaction, not for access to throwing or a damage multiplier.

**Counterplay:** Jump or preemptively back away before capture. Use a fast attack against the committed approach/startup. Bait the whiff and punish its recovery.

**Timing seed:** 12 / 3 / 23 startup / active / recovery ticks. Full seed fields are in the JSON; this is not an on-block/on-hit measurement.

**Presentation:** Distinct two-hand clinch anticipation and a short readable capture; do not reuse a strike pose. Show COMMAND THROW on the card; no cinematic that hides the next wakeup.

**Prove before release:** Prove capture cannot occur during blockstun or post-block throw immunity. Test both seat orders against normal/command throw on the same tick. Show a player response to tech versus jump/mash; a bot merely failing to jump is not sufficient.

#### R-S2 — Slipstream Entry

**Low-profile approach with branches · core_trial · 600 credits at preparation lock; then free activation.**  
**Command:** `qcb+P; then P or K`. **Slot:** signature. 

**Why buy/spend:** A high projectile or extended high poke makes an ordinary grounded approach predictable.

**Actual proposed behavior:** A 20-tick grounded ducking entry travels 42 world units during ticks [4,12). Use an authored low hurt silhouette in that interval, not blanket invulnerability. Fresh P or K during [8,13) starts respectively a mid straight (5/3/19, 65 damage) or a narrow anti-air upper (7/4/24, 80 damage, soft knockdown). No branch is automatic. Declining the branch completes entry recovery. Neither branch cancels on block or whiff.

**Compared with free play:** A free dash travels farther. A free jump bypasses some ground threats. This tool buys a particular hurtbox/path and the threat of two follow-ups, not a faster universal dash.

**Counterplay:** Low attacks still hit the ducking body. Wait, throw or punish the end of the entry. Block/parry the chosen follow-up; do not assume the upper hits crouching bodies.

**Timing seed:** 4 / 8 / 8 startup / active / recovery ticks. Full seed fields are in the JSON; this is not an on-block/on-hit measurement.

**Presentation:** Rook folds into a clearly grounded boxing weave. Keep feet and low hurt silhouette coherent. Straight and upper branches need genuinely different anticipation and impact poses.

**Prove before release:** Verify a high projectile misses by geometry and a low projectile/low strike still hits. Enumerate both branch inputs through the real parser, including delayed/no branch. Record a useful entry against zoning and a failed entry punished without paid defense.

#### R-S3 — Rivet Chain

**Rekka-style commitment sequence · advanced_trial · 600 credits at preparation lock; then free activation.**  
**Command:** `qcb+P; then P; then P`. **Slot:** signature. 

**Why buy/spend:** The player wants a pressure route with a deliberate stop/continue decision rather than one repeated special.

**Actual proposed behavior:** Three mid strikes have targets 10/3/18, 8/3/20 and 12/3/26 with damage 45/55/75. Each fresh follow-up P is accepted only during the preceding contact window [hit_start, hit_start+8). No follow-up on a parry or whiff. Stage two can be delayed within the window; stage three knocks down. Each continuation abandons previous recovery and commits to its own startup. No loop back, no alternate throw/overhead branch and no outgoing super cancel in this candidate.

**Compared with free play:** Free normal/special cancels already exist. This leases a varied sequence and an explicit commitment game, not a universal cancel permission.

**Counterplay:** After blocking, challenge a tested delayed gap rather than assuming every sequence is continuous. Parry a predicted follow-up. Block the final strike and take the measured punish.

**Timing seed:** 10 / 3 / 18 startup / active / recovery ticks. Full seed fields are in the JSON; this is not an on-block/on-hit measurement.

**Presentation:** Three related but readable body rhythms; a distinct heavy finish. Never hide a delay by freezing an unrelated idle cel.

**Prove before release:** Publish frame traces for earliest/latest follow-ups on hit and block. No infinite blockstring or phase-reset exploit. Stopping at stage one must be a deliberate viable choice in at least one tested context.

#### R-S4 — Cross Counter

**Specific attack read · advanced_trial · 600 credits at preparation lock; then free activation.**  
**Command:** `qcb+P`. **Slot:** signature. 

**Why buy/spend:** An opponent repeatedly commits to grounded mid/high strikes at a predictable timing.

**Actual proposed behavior:** Enter a counter stance: startup 5, counter window [5,11), whiff recovery 25. Only a frontal, direct, grounded mid/high strike can trigger it. Low, airborne, throw, projectile and super attacks bypass it. On trigger negate that single strike and start an independently colliding 6/3/22 mid riposte for 95 damage. The riposte is not a teleporting guaranteed hit; it can miss at range. Only this counter stance changes that interaction.

**Compared with free play:** A directional parry remains free and more broadly applicable. This option trades anticipation, narrower eligibility and major whiff commitment for an authored return attack.

**Counterplay:** Use a low or throw. Delay the anticipated strike until the stance expires. Bait at a distance where the return attack whiffs.

**Timing seed:** 5 / 6 / 25 startup / active / recovery ticks. Full seed fields are in the JSON; this is not an on-block/on-hit measurement.

**Presentation:** A provocative guarded lean, a sharp trigger flash, then a real punch with its own hurtbox. Never label it automatic parry.

**Prove before release:** Test every excluded attack class. Resolve simultaneous counters symmetrically. No credit reward, retained counter token or refund on failure.

#### R-T1 — High Hook

**Reach-oriented overhead replacement · core_trial · 300 credits at preparation lock; then free activation.**  
**Command:** `6+HP`. **Slot:** technique. **Replaces:** `command_fhp`.

**Why buy/spend:** The defender crouch-blocks at a spacing where the universal overhead lacks useful reach.

**Actual proposed behavior:** Replace command_fhp with a deliberately telegraphed overhead, 18/3/20 and 80 damage. No movement, armor or kara throw. Give it purposeful forward/head-level geometry; block/parry punishability is measured rather than inferred. On hit, only normal recovery/link rules apply.

**Compared with free play:** The universal overhead is still free. Forward HP loses its original moving/kara function; this is a situational replacement, not an extra universal overhead.

**Counterplay:** Stand-block or high-parry the visible windup. Interrupt before the active frame. Whiff-punish from outside the authored reach.

**Timing seed:** 18 / 3 / 20 startup / active / recovery ticks. Full seed fields are in the JSON; this is not an on-block/on-hit measurement.

**Presentation:** Lift the shoulder/elbow before a downward hook; distinguish it from Low Turn early enough for deliberate defense.

**Prove before release:** Compare with the free overhead and original forward HP. Prove no concealed kara carryover. Measure block and hit follow-ups at close/max range.

#### R-T2 — Low Turn

**Low knockdown replacement · advanced_trial · 300 credits at preparation lock; then free activation.**  
**Command:** `6+HP`. **Slot:** technique. **Replaces:** `command_fhp`.

**Why buy/spend:** The player prefers a deliberate low check from the command-normal slot rather than an extra special.

**Actual proposed behavior:** Replace command_fhp with a 14/3/23 low, 75 damage and soft knockdown. No travel, armor, kara or outgoing cancel. Do not reintroduce the old Low Drive payload unchanged at a second price.

**Compared with free play:** Free crouching lows and sweep remain. This is a different reach/input/knockdown choice while surrendering advancing forward HP.

**Counterplay:** Crouch-block or low-parry. Jump or space outside it. Punish the committed recovery after a measured block.

**Timing seed:** 14 / 3 / 23 startup / active / recovery ticks. Full seed fields are in the JSON; this is not an on-block/on-hit measurement.

**Presentation:** Low hip rotation and visible foot-level sweep; never use the overhead silhouette.

**Prove before release:** Find a niche compared with free sweep; remove/revise if none exists. Ensure its price and card do not imply a guaranteed mixup. Verify overhead/low replacement is revealed before fighting.

#### R-T3 — Rivet Lift

**Earned launcher conversion · core_trial · 600 credits at preparation lock; then free activation.**  
**Command:** `6+HP`. **Slot:** technique. **Replaces:** `command_fhp`.

**Why buy/spend:** The player wants a stronger conversion from a deliberate heavy read, not higher damage on every attack.

**Actual proposed behavior:** Replace command_fhp with a 12/3/24 mid launcher for 60 damage. A successful hit permits jump cancel only during [contact_frame, contact_frame+4); not on block, parry or whiff. Launch height/juggle cost are authored to permit a limited follow-up and then exhaust normal juggle rules. No reset of damage scaling or juggle budget.

**Compared with free play:** Free anti-airs and confirms remain. This sacrifices the original command normal and spends two EX worth of credits up front for repeatable hit-earned routes.

**Counterplay:** Block/parry; no jump cancel saves the blocked version. Whiff-punish its startup/recovery. Deny the spacing needed for the launcher.

**Timing seed:** 12 / 3 / 24 startup / active / recovery ticks. Full seed fields are in the JSON; this is not an on-block/on-hit measurement.

**Presentation:** A lifting body blow with a clear airborne trajectory; visually separate launch from standard hit reactions.

**Prove before release:** At least one real input route connects from launch and one blocked route is punished. No relaunch/ground-reset loop. Paid EX/super after launch still debits independently.

#### R-T4 — Tempered Blow

**Committed armored read · advanced_trial · 600 credits at preparation lock; then free activation.**  
**Command:** `6+HP; optional hold`. **Slot:** technique. **Replaces:** `command_fhp`.

**Why buy/spend:** A single predictable poke needs a read-based answer without granting passive armor.

**Actual proposed behavior:** Replace command_fhp. Minimum startup 18, active 3, recovery 27. Holding delays release up to startup 36 but does not increase damage beyond 100. One direct non-super strike can be absorbed during fixed action ticks [6,14); it still deals full health damage and can KO, but does not cause hitstun unless it causes dizzy. Throws, lows, projectiles, supers and a second strike interrupt. The armor interval never lengthens with hold. Always mid/blockable; no dash cancel.

**Compared with free play:** Blocking/parrying remain free. The buyer gains a risky particular attack, not damage reduction on the fighter or a universal Focus/Drive system.

**Counterplay:** Throw, low or use a multi-hit attack. Wait out the fixed absorption window. Block/parry the slow release and punish.

**Timing seed:** 18 / 3 / 27 startup / active / recovery ticks. Full seed fields are in the JSON; this is not an on-block/on-hit measurement.

**Presentation:** A visible windup brace with a brief armor cue only in its authored interval. Damage remains visible; do not show recoverable health that the game does not implement.

**Prove before release:** A lethal absorbed hit still ends the round. Hold cannot refresh armor or extend its interval. Dizzy/throw/multihit priorities tested, with no stored armor after action end.

#### R-G1 — False Start

**Attack feint · core_trial · 300 credits at preparation lock; then free activation.**  
**Command:** `4+HP+HK`. **Slot:** gambit. 

**Why buy/spend:** An opponent parries or swings early in response to Traveling Knee anticipation.

**Actual proposed behavior:** A standalone 12-tick feint copies the first six visible anticipation ticks and first 3.9 units of the free knee_h windup, then visibly aborts. No hitbox, armor, invulnerability or attack cancel; normal actions resume after tick 12. Cannot be entered out of another move. The round reveal tells the opponent this feint is available, not when it is being used.

**Compared with free play:** Walking is still better for ordinary travel. This purchases deception and reduced commitment compared with completing the real heavy knee.

**Counterplay:** Wait for the real active threat before committing. Use a low-risk space-controlling check. Throw/attack during the feint if already in range.

**Timing seed:** 6 / 0 / 6 startup / active / recovery ticks. Full seed fields are in the JSON; this is not an on-block/on-hit measurement.

**Presentation:** First six poses must match the real knee anticipation; the abort must then read clearly. It must not spoof a hit spark, super freeze or paid receipt.

**Prove before release:** Human A/B test whether the intended threat, not random confusion, changes decisions. Verify no attack transition before recovery end. No money gained from inducing a response.

#### R-G2 — Check Step

**Short retreat and punish spacing · core_trial · 300 credits at preparation lock; then free activation.**  
**Command:** `4+HP+HK`. **Slot:** gambit. 

**Why buy/spend:** A full backdash moves too far or commits too long for a close whiff punish.

**Actual proposed behavior:** A 10-tick grounded retreat travels 30 units in [0,6), then has 4 recovery ticks. It is fully strike/throw vulnerable and cannot block/parry/cancel during the action. It remains constrained by walls and maximum separation. No low-profile privilege.

**Compared with free play:** Free back walk is less committed; free backdash covers more distance. This purchases a short, fast-recovering fixed displacement with real vulnerability.

**Counterplay:** Follow with a longer poke. Walk forward and take space instead of chasing an immediate punish. Catch its recovery or corner it.

**Timing seed:** 0 / 6 / 4 startup / active / recovery ticks. Full seed fields are in the JSON; this is not an on-block/on-hit measurement.

**Presentation:** Short measured backward slide with stable feet/root; do not depict teleportation or invulnerability.

**Prove before release:** Compare action-to-next-normal time and distance with free walking/backdash. Confirm corners do not reverse movement or pass through the opponent. No input-buffer exploit grants guard/parry during commitment.

#### R-G3 — Vault Hop

**Predictable low/throw avoidance · advanced_trial · 300 credits at preparation lock; then free activation.**  
**Command:** `4+HP+HK`. **Slot:** gambit. 

**Why buy/spend:** An opponent overuses a grounded low/throw timing and a normal jump arc is too large for the desired reposition.

**Actual proposed behavior:** A 3-tick prejump followed by a fixed short hop; no attack may start before landing. Set a 40-unit apex, 20-unit forward displacement and 8 landing recovery ticks. It is not invincible: normal air hurtboxes apply. Prejump throw eligibility follows the unchanged system rule. Landing does not erase recovery or enable an instant cross-up.

**Compared with free play:** A normal jump is free and can attack; this is a smaller nonattacking trajectory, not an air dash.

**Counterplay:** Anti-air or air-to-air. Delay the low until landing. Take space and punish landing instead of throwing immediately.

**Timing seed:** 3 / 0 / 8 startup / active / recovery ticks. Full seed fields are in the JSON; this is not an on-block/on-hit measurement.

**Presentation:** Small grounded-to-air transfer; no wing/effect suggesting flight. Landing anticipation must match the vulnerable recovery.

**Prove before release:** Test every ascent/descent frame against lows and air hits. No aerial attacks or corner side bypass. Record useful low avoidance and a free punish.

#### R-G4 — Wire Cut

**Destroy an expected projectile · advanced_trial · 300 credits at preparation lock; then free activation.**  
**Command:** `4+HP+HK`. **Slot:** gambit. 

**Why buy/spend:** A player wants to remove a single approaching projectile rather than merely parry it and remain in place.

**Actual proposed behavior:** A 7/3/22 grounded swat has a frontal anti-projectile box but no fighter hitbox. It destroys one ordinary non-super projectile instance during [7,10). It cannot reflect, cancel its recovery, erase a persistent super field, or produce a refund. All ordinary fighter hurtboxes remain vulnerable.

**Compared with free play:** A free parry is quicker and protects the player at contact. This tool removes the object in a slightly forward region but requires a larger commitment.

**Counterplay:** Delay the projectile. Approach behind it and strike/throw the recovery. Use a projectile classification explicitly outside its ordinary-shot filter.

**Timing seed:** 7 / 3 / 22 startup / active / recovery ticks. Full seed fields are in the JSON; this is not an on-block/on-hit measurement.

**Presentation:** A slicing glove/forearm with a distinct projectile-dissipation spark, never a fighter hit spark.

**Prove before release:** Test ordinary, EX multi-hit instance, reflected, and super objects under an explicit filter. No fighter damage or meter gain. Simultaneous shot and melee resolve without universal invulnerability.


### Vale

#### V-S1 — Returning Pulse

**One-contact returning screen threat · core_trial · 900 credits at preparation lock; then free activation.**  
**Command:** `qcf+P`. **Slot:** signature. 

**Why buy/spend:** The opponent avoids the first projectile path and immediately re-enters the space it crossed.

**Actual proposed behavior:** Retain the one-contact motion-input projectile identity. Startup 22/active 3/recovery 23. Speed 2.2 units/tick, turn at projectile age 45, lifetime 110, one contact for 70 damage. Hit/block/parry consumes the object. It can threaten after a miss but never hits once outbound and again on return. Retain the documented one-projectile-instance owner restriction for ordinary projectile families.

**Compared with free play:** Free charge projectiles remain faster to access when charge is held. This rents different trajectory and charge-independent access, not universally better fireballs.

**Counterplay:** Parry/block to consume its contact. Avoid both legs of the known path. Punish the startup or exploit the owner projectile occupancy.

**Timing seed:** 22 / 3 / 23 startup / active / recovery ticks. Full seed fields are in the JSON; this is not an on-block/on-hit measurement.

**Presentation:** Visible turn and travel direction, an actual return tail, no misleading two-hit icon. Card warns that an existing shot can prevent another projectile activation.

**Prove before release:** Outbound miss then return hit; no outbound-plus-return double hit. Block/parry removes the projectile. Projectile-super attempts while occupancy forbids them reject before debit.

#### V-S2 — Low Palm

**Low special without charge · core_trial · 600 credits at preparation lock; then free activation.**  
**Command:** `qcf+P`. **Slot:** signature. 

**Why buy/spend:** Vale moved or crossed sides and wants a limited low-special conversion without stored directional charge.

**Actual proposed behavior:** A 14/3/24 low special for 85 damage and soft knockdown. It adds a motion-input move rather than replacing the free charge arsenal. Allow only explicitly authored normal-to-special incoming cancels. No outgoing super or EX conversion for this candidate. No invulnerability and no automatic approach.

**Compared with free play:** Free sweep/low normals are still the normal low options. This pays for the particular special-class route when charge is absent.

**Counterplay:** Crouch-block or low-parry. Keep outside the palm range. Punish a bad raw attempt or its block recovery.

**Timing seed:** 14 / 3 / 24 startup / active / recovery ticks. Full seed fields are in the JSON; this is not an on-block/on-hit measurement.

**Presentation:** Compact ground-level palm with a stable narrow collision silhouette; no implied long-range wave.

**Prove before release:** Demonstrate one charge-absent legal conversion. Compare with free sweep at identical credits. Confirm no low-to-super shortcut is implicitly added.

#### V-S3 — Arc Pulse

**Angle-specific projectile anti-air · advanced_trial · 600 credits at preparation lock; then free activation.**  
**Command:** `qcf+P`. **Slot:** signature. 

**Why buy/spend:** The opponent jumps the usual horizontal charge shot from a repeatable distance.

**Actual proposed behavior:** An 18/3/24 projectile launches upward-forward at a fixed angle. It has one contact, 65 damage and 60-tick lifetime. It is not homing, invincible or a ground-covering fireball. Author trajectory and body interaction to miss crouching/standing targets at typical close range. One ordinary projectile instance per owner.

**Compared with free play:** A free normal/charge anti-air is still available. This leases earlier spatial coverage at a distance in exchange for a vulnerable grounded commitment.

**Counterplay:** Stay grounded and approach. Change jump timing or trajectory. Parry the predictable airborne contact.

**Timing seed:** 18 / 3 / 24 startup / active / recovery ticks. Full seed fields are in the JSON; this is not an on-block/on-hit measurement.

**Presentation:** Distinct diagonal launch; the arc itself, not a full-screen glow, tells the opponent what space is occupied.

**Prove before release:** Anti-air succeeds at one intended range and fails at explicitly wrong ones. No unseen overhead guard property. No universal anti-air invulnerability added to the actor.

#### V-S4 — Pulse Anchor

**Destructible setup object · advanced_trial · 900 credits at preparation lock; then free activation.**  
**Command:** `qcf+P`. **Slot:** signature. 

**Why buy/spend:** The player wants to occupy a known location after earning setup time, rather than receive immediate damage.

**Actual proposed behavior:** A 26/1/25 placement creates one visible floor anchor 45 units forward. It arms 18 ticks after placement, expires at age 150 and triggers once when an enemy enters its 24-unit radius. Trigger has a visible 8-tick windup, then a mid/blockable 60-damage pulse. Any enemy normal/strike contacting the anchor before detonation destroys it. Owner hit/throw/dizzy removes it. Cannot coexist with the same owner's major super field or another anchor; reject illegal placement before any action commitment. No passive credit effects.

**Compared with free play:** An ordinary projectile is free and immediate but travels. This rents an interactable location and delayed timing, not guaranteed wakeup damage.

**Counterplay:** Destroy it with a spaced normal. Wait out the lifetime or stay outside the radius. Block/parry its single pulse or hit the setter.

**Timing seed:** 26 / 1 / 25 startup / active / recovery ticks. Full seed fields are in the JSON; this is not an on-block/on-hit measurement.

**Presentation:** Distinct unarmed/armed/trigger/destroyed states; strong ground marker without covering feet or hitboxes.

**Prove before release:** Every zero-credit character has a documented legal removal/avoidance response in representative setup states. No same-tick high/low or left/right impossibility when combined with owner attacks. Snapshots reproduce arming, trigger, destruction and expiry.

#### V-T1 — Heel Arc

**Ranged overhead replacement · core_trial · 300 credits at preparation lock; then free activation.**  
**Command:** `6+HP`. **Slot:** technique. **Replaces:** `command_fhp`.

**Why buy/spend:** A crouching opponent sits at Vale's command-normal range.

**Actual proposed behavior:** Replace command_fhp with an 18/3/20 overhead kick, 80 damage and 100 stun. No forward travel, kara or outgoing cancel. Its reach/hurtboxes must be authored to serve a visibly distinct role from the free universal overhead.

**Compared with free play:** The original advancing Long Heel and universal overhead remain alternative base-kit plans. Buying this surrenders the original forward-HP action.

**Counterplay:** Stand-block or high-parry the windup. Walk out and whiff-punish. Interrupt close startup.

**Timing seed:** 18 / 3 / 20 startup / active / recovery ticks. Full seed fields are in the JSON; this is not an on-block/on-hit measurement.

**Presentation:** A clear high knee lift followed by a heel arc. Keep foot extension vulnerable deliberately.

**Prove before release:** Compare actual ranges and block recovery with original Long Heel. No retained movement/kara from the replacement. Show a 300-credit opening choice that does not require explaining hidden modifiers.

#### V-T2 — Needle Check

**Precise anti-poke replacement · core_trial · 300 credits at preparation lock; then free activation.**  
**Command:** `6+HP`. **Slot:** technique. **Replaces:** `command_fhp`.

**Why buy/spend:** The opponent repeatedly exposes an extended limb at the edge of Vale's normal range.

**Actual proposed behavior:** Replace the questionable Long Check concept with a 12/3/18 mid poke for 55 damage and 70 stun. Author a thin, forward-reaching hit region (target farthest forward extent 72 units) and explicit extended-foot hurtboxes. No approach motion, knockdown, kara or outgoing cancel. It should touch an exposed limb at a narrow range, not outrange everything or gain blanket low invulnerability.

**Compared with free play:** Free Long Heel is faster, more damaging and moves forward. This leases precision at a specific spacing while sacrificing those benefits.

**Counterplay:** Retract or do not extend a limb into its thin region. Close in against the weaker committed check. Parry/block then punish according to measured spacing.

**Timing seed:** 12 / 3 / 18 startup / active / recovery ticks. Full seed fields are in the JSON; this is not an on-block/on-hit measurement.

**Presentation:** Long narrow toe/heel extension, not an effect bigger than its contact region. Explicitly draw the vulnerable recovering leg.

**Prove before release:** Require a paired limb-punish scenario where this beats the free option; otherwise redesign/delete. No inflated reach solely in invisible VFX. Show a case where free Long Heel is clearly preferable.

#### V-T3 — Descending Heel

**Dive-kick trajectory change · advanced_trial · 600 credits at preparation lock; then free activation.**  
**Command:** `air:2+MK`. **Slot:** technique. 

**Why buy/spend:** The opponent commits to one predictable anti-air timing against the normal forward-jump arc.

**Actual proposed behavior:** Add a downward-forward dive kick from a forward jump at height at least 50 units. It may start only before the player has used another aerial attack in that jump; one attack sequence per jump, no aerial reset. Startup 7, up to 16 active ticks until landing, 16 landing recovery ticks. Target 65 damage. Collision is mid (standing or crouching guard), not an instant overhead. Air trajectory and contact-height-dependent advantage must be measured; no universal plus-on-block claim.

**Compared with free play:** Normal jump attacks remain free. This buys a different committed trajectory, not a double jump, air dash or a guaranteed approach.

**Counterplay:** Space for a high contact and punish the landing. Anti-air earlier or air-to-air. Parry the altered approach.

**Timing seed:** 7 / 16 / 16 startup / active / recovery ticks. Full seed fields are in the JSON; this is not an on-block/on-hit measurement.

**Presentation:** Readable angle change, distinct heel-down pose and a clearly vulnerable landing. Avoid feet that visually hit far outside the box.

**Prove before release:** Minimum height boundary and instant-input rejection. Measure advantage at high, medium and low contact heights. No repeat dive, aerial cancel loop or ambiguous overhead property.

#### V-T4 — Skycatch

**Air-to-air throw · advanced_trial · 600 credits at preparation lock; then free activation.**  
**Command:** `air:LP+LK`. **Slot:** technique. 

**Why buy/spend:** The opponent repeatedly meets Vale in the air at close range and relies on parrying strikes.

**Actual proposed behavior:** Add an air-only throw, 5 startup and 3 capture-active ticks, 105 damage, no super cancel. Both actors must be airborne and the target must not be in hitstun, capture or juggle state. Capture is untechable but can be avoided before contact. On miss the actor falls normally and has 18 landing recovery ticks; on success use a fixed throw arc and the same landing recovery, without charge grants.

**Compared with free play:** Air normals are free and safer at distance. This rents a particular un-parryable aerial threat, not the ability to contest air space at all.

**Counterplay:** Stay outside the short capture region. Hit Vale before the throw becomes active. Stay grounded and punish landing.

**Timing seed:** 5 / 3 / 18 startup / active / recovery ticks. Full seed fields are in the JSON; this is not an on-block/on-hit measurement.

**Presentation:** A clear reach-and-capture pose; both fighter roots follow the throw arc. No teleport snap over a large distance.

**Prove before release:** Ground/prejump/hitstun captures rejected. Simultaneous air-throw arbitration is symmetric. Miss, landing and rollback do not refresh aerial actions.

#### V-G1 — False Pulse

**Projectile feint · core_trial · 300 credits at preparation lock; then free activation.**  
**Command:** `4+HP+HK`. **Slot:** gambit. 

**Why buy/spend:** An opponent jumps or parries early when they see Vale's horizontal-shot preparation.

**Actual proposed behavior:** A standalone 12-tick feint mimics the first six anticipation cels of pulse_h, then aborts without a projectile. It does not require stored charge, invent charge, launch a projectile, or cancel another action. Directional charge continues only through the same ordinary sampled-direction rules as any other action. No global freeze, armor, guard or parry during commitment.

**Compared with free play:** The real free charge shot remains the threat. This rents a false tell and shorter recovery, not stored fireball ammunition.

**Counterplay:** Wait for the actual projectile. Take space without committing to a jump. Attack/throw the feint when close.

**Timing seed:** 6 / 0 / 6 startup / active / recovery ticks. Full seed fields are in the JSON; this is not an on-block/on-hit measurement.

**Presentation:** Use the actual first six shot cels, then distinct recovery. Do not play a projectile-spawn cue on the fake.

**Prove before release:** Charge history is neither manufactured nor arbitrarily erased. Human test distinguishes plausible bait from an obviously unrelated animation. No fake debit or hit event.

#### V-G2 — Recoil Step

**Short retreat at zoning range · core_trial · 300 credits at preparation lock; then free activation.**  
**Command:** `4+HP+HK`. **Slot:** gambit. 

**Why buy/spend:** A full backdash overshoots the spacing for Vale's intended punish.

**Actual proposed behavior:** A fully vulnerable 10-tick retreat travels 28 units over [0,6) then recovers for 4. No attack/guard/parry/cancel during it. It grants no charge; held-back charge obeys the unchanged normal sampling rules. Stage and player-separation constraints apply.

**Compared with free play:** Free back walk is less committed and the full backdash covers more ground. This offers a deliberately short displacement and early return to neutral.

**Counterplay:** Pursue with a longer normal. Take space instead of whiffing a short poke. Corner Vale or punish recovery.

**Timing seed:** 0 / 6 / 4 startup / active / recovery ticks. Full seed fields are in the JSON; this is not an on-block/on-hit measurement.

**Presentation:** A short backward weight shift distinct from a full backdash and the attack feint.

**Prove before release:** Test cash-equivalent alternatives and normal back-walk charge. No corner escape through the opponent. No invulnerability or accidental action cancellation.

#### V-G3 — Shear Palm

**Active projectile reflection · advanced_trial · 300 credits at preparation lock; then free activation.**  
**Command:** `4+HP+HK`. **Slot:** gambit. 

**Why buy/spend:** An opponent repeatedly fires an ordinary shot on an obvious timing.

**Actual proposed behavior:** A 6/4/24 nonattacking palm can reflect one ordinary non-super projectile instance during [6,10). The returned instance retains its original remaining lifetime/hit count/credit origin but changes owner and direction. Reflect depth is capped at one; another interceptor dissipates an already-reflected object. No credit transfer, energy stock or fighter invulnerability. Paid EX and super-class projectiles are excluded by default.

**Compared with free play:** Free parry defends without the palm's recovery. Reflection returns a spatial threat but creates a larger baitable commitment.

**Counterplay:** Fake or delay the shot. Hit/throw the palm while approaching. Use an explicitly excluded projectile class or parry the return.

**Timing seed:** 6 / 4 / 24 startup / active / recovery ticks. Full seed fields are in the JSON; this is not an on-block/on-hit measurement.

**Presentation:** A sharp direction-change spark and clear ownership accent, without making the projectile indistinguishable from a super.

**Prove before release:** No infinite reflection loop or owner-count corruption. Original debit never moves to the defender and never refunds. Simultaneous reflection/destruction and rollback remain deterministic.

#### V-G4 — Slipgate

**Telegraphed reposition through a shot · advanced_trial · 600 credits at preparation lock; then free activation.**  
**Command:** `4+HP+HK`. **Slot:** gambit. 

**Why buy/spend:** A predictable projectile occupies a lane, but the player is willing to risk a slow, visible reposition.

**Actual proposed behavior:** An original grounded displacement, not unrestricted teleport. Startup 8, travel/phase interval [8,15), recovery 20. Move at most 80 units backward, clamped to stage and maximum separation. It cannot pass through the other fighter or switch sides. Only ordinary projectiles miss during the middle interval; direct strikes, throws, startup and recovery remain vulnerable. Never usable from hit/blockstun. No extra charge preservation beyond input rules.

**Compared with free play:** A jump or parry is free and a backdash may be quicker. This buys a particular projectile bypass at a highly visible destination.

**Counterplay:** Attack startup or the marked destination. Walk forward or throw instead of firing immediately. Exploit its long recovery or corner restriction.

**Timing seed:** 8 / 7 / 20 startup / active / recovery ticks. Full seed fields are in the JSON; this is not an on-block/on-hit measurement.

**Presentation:** Show destination marker on startup and a brief spatial streak; do not make the actor invisible for the whole action.

**Prove before release:** No strike/throw invincibility leaks. Zero-distance corner use still has recovery. No crossing, escape from stun, or ownership/charge anomaly.


## Enhanced moves: pay at startup


### Rook

#### R-E1 — Burst Pulse

**Projectile pressure · core_trial · 300 credits per committed activation; no unlock fee.**  
**Command:** `qcf+PP`.  

**Why buy/spend:** A committed enhanced projectile contest or conversion.

**Actual proposed behavior:** Use the original pulse_ex family as the first baseline, then tune one property at a time. Do not add blanket invulnerability or a full-screen guarantee. Debit once on accepted startup; failure, parry, block, whiff and interruption after startup never refund.

**Compared with free play:** The free shot remains the normal zoning tool.

**Counterplay:** Block/parry the contacts; jump or punish the startup. A zero-wallet opponent retains all ordinary guard/parry/movement responses.

**Timing seed:** 8 / 1 / 24 startup / active / recovery ticks. Full seed fields are in the JSON; this is not an on-block/on-hit measurement.

**Presentation:** Use distinct EX anticipation/impact and one concise 300-credit debit cue. Normal and EX need different readable contact rhythm, not just a color tint.

**Prove before release:** 299/300/301 exact-cost and reserve boundaries. Failed input is not a debit; post-start failure is not a refund. Legal-input both-facing contact/whiff/block/parry/replay/rollback traces.

#### R-E2 — Emergency Riser

**Reversal / anti-air · core_trial · 300 credits per committed activation; no unlock fee.**  
**Command:** `dp+PP`.  

**Why buy/spend:** A precisely timed, expensive escape or vertical punish.

**Actual proposed behavior:** Use the original rise_ex family as the first baseline, then tune one property at a time. Invulnerability is move-local and must have an explicit end; no post-block safety cancel. Debit once on accepted startup; failure, parry, block, whiff and interruption after startup never refund.

**Compared with free play:** The free anti-air still handles ordinary jumps.

**Counterplay:** Bait and block/parry; punish its measured recovery. A zero-wallet opponent retains all ordinary guard/parry/movement responses.

**Timing seed:** 3 / 5 / 24 startup / active / recovery ticks. Full seed fields are in the JSON; this is not an on-block/on-hit measurement.

**Presentation:** Use distinct EX anticipation/impact and one concise 300-credit debit cue. Normal and EX need different readable contact rhythm, not just a color tint.

**Prove before release:** 299/300/301 exact-cost and reserve boundaries. Failed input is not a debit; post-start failure is not a refund. Legal-input both-facing contact/whiff/block/parry/replay/rollback traces.

#### R-E3 — Driving Knee

**Grounded conversion · core_trial · 300 credits per committed activation; no unlock fee.**  
**Command:** `qcf+KK`.  

**Why buy/spend:** Spend after a real opening to improve travel/knockdown or a tested EX-to-super route.

**Actual proposed behavior:** Use the original knee_ex family as the first baseline, then tune one property at a time. Retain the recorded contact cancel provision only until a reviewed hit-only/block distinction is implemented; do not advertise a guaranteed combo. Debit once on accepted startup; failure, parry, block, whiff and interruption after startup never refund.

**Compared with free play:** Free knee variants retain their ordinary approach role.

**Counterplay:** Block/parry it; deny the confirm or punish a raw approach. A zero-wallet opponent retains all ordinary guard/parry/movement responses.

**Timing seed:** 8 / 5 / 24 startup / active / recovery ticks. Full seed fields are in the JSON; this is not an on-block/on-hit measurement.

**Presentation:** Use distinct EX anticipation/impact and one concise 300-credit debit cue. Normal and EX need different readable contact rhythm, not just a color tint.

**Prove before release:** 299/300/301 exact-cost and reserve boundaries. Failed input is not a debit; post-start failure is not a refund. Legal-input both-facing contact/whiff/block/parry/replay/rollback traces.

#### R-E4 — Recoil Palm

**Spacing punish · core_trial · 300 credits per committed activation; no unlock fee.**  
**Command:** `qcb+KK`.  

**Why buy/spend:** A paid retreating strike when the opponent extends into your space.

**Actual proposed behavior:** Use the original sway_ex family as the first baseline, then tune one property at a time. No automatic dodge of every attack, projectile or throw. Debit once on accepted startup; failure, parry, block, whiff and interruption after startup never refund.

**Compared with free play:** Free retreat palm and walking remain alternatives.

**Counterplay:** Do not chase with a short poke; take space or whiff-punish. A zero-wallet opponent retains all ordinary guard/parry/movement responses.

**Timing seed:** 8 / 5 / 24 startup / active / recovery ticks. Full seed fields are in the JSON; this is not an on-block/on-hit measurement.

**Presentation:** Use distinct EX anticipation/impact and one concise 300-credit debit cue. Normal and EX need different readable contact rhythm, not just a color tint.

**Prove before release:** 299/300/301 exact-cost and reserve boundaries. Failed input is not a debit; post-start failure is not a refund. Legal-input both-facing contact/whiff/block/parry/replay/rollback traces.


### Vale

#### V-E1 — Twin Pulse

**Charge projectile conversion · core_trial · 300 credits per committed activation; no unlock fee.**  
**Command:** `charge_back+PP`.  

**Why buy/spend:** A charge-required enhanced shot, not a fee to use ordinary fireballs.

**Actual proposed behavior:** Use the original pulse_ex family as the first baseline, then tune one property at a time. Paying does not waive directional charge or owner projectile occupancy. Debit once on accepted startup; failure, parry, block, whiff and interruption after startup never refund.

**Compared with free play:** Free charge shot remains central.

**Counterplay:** Jump/parry/block at the tested timing; punish setup. A zero-wallet opponent retains all ordinary guard/parry/movement responses.

**Timing seed:** 8 / 1 / 24 startup / active / recovery ticks. Full seed fields are in the JSON; this is not an on-block/on-hit measurement.

**Presentation:** Use distinct EX anticipation/impact and one concise 300-credit debit cue. Normal and EX need different readable contact rhythm, not just a color tint.

**Prove before release:** 299/300/301 exact-cost and reserve boundaries. Failed input is not a debit; post-start failure is not a refund. Legal-input both-facing contact/whiff/block/parry/replay/rollback traces.

#### V-E2 — Emergency Arc

**Charge reversal / anti-air · core_trial · 300 credits per committed activation; no unlock fee.**  
**Command:** `charge_down+KK`.  

**Why buy/spend:** A paid vertical response after actual stored down charge.

**Actual proposed behavior:** Use the original rise_ex family as the first baseline, then tune one property at a time. No purchase of instant charge or unlimited invulnerability. Debit once on accepted startup; failure, parry, block, whiff and interruption after startup never refund.

**Compared with free play:** Free charge anti-air and air normals remain.

**Counterplay:** Bait the reversal and punish; exploit absence of charge. A zero-wallet opponent retains all ordinary guard/parry/movement responses.

**Timing seed:** 3 / 5 / 24 startup / active / recovery ticks. Full seed fields are in the JSON; this is not an on-block/on-hit measurement.

**Presentation:** Use distinct EX anticipation/impact and one concise 300-credit debit cue. Normal and EX need different readable contact rhythm, not just a color tint.

**Prove before release:** 299/300/301 exact-cost and reserve boundaries. Failed input is not a debit; post-start failure is not a refund. Legal-input both-facing contact/whiff/block/parry/replay/rollback traces.

#### V-E3 — Piercing Palm

**Confirmed ground conversion · core_trial · 300 credits per committed activation; no unlock fee.**  
**Command:** `qcb+PP`.  

**Why buy/spend:** Spend on a real palm opening, with a specifically tested selected-super continuation.

**Actual proposed behavior:** Use the original palm_ex family as the first baseline, then tune one property at a time. Each EX and subsequent super has its own debit, never a bundled free super. Debit once on accepted startup; failure, parry, block, whiff and interruption after startup never refund.

**Compared with free play:** Free palm routes remain usable at zero.

**Counterplay:** Block/parry or deny contact; punish careless raw use. A zero-wallet opponent retains all ordinary guard/parry/movement responses.

**Timing seed:** 8 / 5 / 24 startup / active / recovery ticks. Full seed fields are in the JSON; this is not an on-block/on-hit measurement.

**Presentation:** Use distinct EX anticipation/impact and one concise 300-credit debit cue. Normal and EX need different readable contact rhythm, not just a color tint.

**Prove before release:** 299/300/301 exact-cost and reserve boundaries. Failed input is not a debit; post-start failure is not a refund. Legal-input both-facing contact/whiff/block/parry/replay/rollback traces.

#### V-E4 — Burst Heel

**Alternate trajectory / knockdown · core_trial · 300 credits per committed activation; no unlock fee.**  
**Command:** `qcf+KK`.  

**Why buy/spend:** A distinct enhanced heel conversion that is not merely another reversal.

**Actual proposed behavior:** Use the original heel_ex family as the first baseline, then tune one property at a time. Classify collision height and vulnerability from data, not VFX or the EX name. Debit once on accepted startup; failure, parry, block, whiff and interruption after startup never refund.

**Compared with free play:** Free heel variants remain.

**Counterplay:** Space, block/parry or anti-air according to the authored path. A zero-wallet opponent retains all ordinary guard/parry/movement responses.

**Timing seed:** 8 / 5 / 24 startup / active / recovery ticks. Full seed fields are in the JSON; this is not an on-block/on-hit measurement.

**Presentation:** Use distinct EX anticipation/impact and one concise 300-credit debit cue. Normal and EX need different readable contact rhythm, not just a color tint.

**Prove before release:** 299/300/301 exact-cost and reserve boundaries. Failed input is not a debit; post-start failure is not a refund. Legal-input both-facing contact/whiff/block/parry/replay/rollback traces.


## Supers: select freely, pay to activate


### Rook

#### R-A1 — Circuit Break

**Reliable close confirm · core_trial · 900 credits per committed activation; no unlock fee.**  
**Command:** `double_qcf+P`.  

**Why buy/spend:** A distinct high-commitment use of the same saved credits.

**Actual proposed behavior:** Preserve the close-range strike super as the economical cash-out art. Its role is reliable conversion from specifically measured contacts, not universal reversal safety.

**Compared with free play:** All ordinary fighting stays intact. Selecting this art excludes the other arts for the match; the fee buys an activation, not automatic damage.

**Counterplay:** Whiff or block/parry the committed sequence; keep outside its intended start range. A whiff, parry, blocked or interrupted committed super never refunds its purchase.

**Timing seed:** 5 / 20 / 35 startup / active / recovery ticks. Full seed fields are in the JSON; this is not an on-block/on-hit measurement.

**Presentation:** A distinct art identity, concise paid-startup receipt, readable threat and recovery. Install uses an expiring status icon, not an earnable/refillable meter; field uses visible geometry and lifetime cues.

**Prove before release:** Exact cost and cost minus one, with full and reduced-flash cues. Test raw start, correct confirm, wrong confirm, reversal, whiff, block and parry. Test maximum-bank repetitions and zero-credit defense; illegal active-state reactivation has zero debit.

#### R-A2 — Forge Impact

**Vertical reversal / punish · core_trial · 1200 credits per committed activation; no unlock fee.**  
**Command:** `double_qcf+P`.  

**Why buy/spend:** A distinct high-commitment use of the same saved credits.

**Actual proposed behavior:** Preserve the more specialized vertical super. Test strike/projectile/throw vulnerability windows separately. Its justification is vertical coverage and timing, not a requirement to do more damage than every cheaper art.

**Compared with free play:** All ordinary fighting stays intact. Selecting this art excludes the other arts for the match; the fee buys an activation, not automatic damage.

**Counterplay:** Bait the paid reversal, block/parry, then punish recovery. A whiff, parry, blocked or interrupted committed super never refunds its purchase.

**Timing seed:** 6 / 8 / 35 startup / active / recovery ticks. Full seed fields are in the JSON; this is not an on-block/on-hit measurement.

**Presentation:** A distinct art identity, concise paid-startup receipt, readable threat and recovery. Install uses an expiring status icon, not an earnable/refillable meter; field uses visible geometry and lifetime cues.

**Prove before release:** Exact cost and cost minus one, with full and reduced-flash cues. Test raw start, correct confirm, wrong confirm, reversal, whiff, block and parry. Test maximum-bank repetitions and zero-credit defense; illegal active-state reactivation has zero debit.

#### R-A3 — Overtime

**Temporary hit-confirm routing · advanced_trial · 1500 credits per committed activation; no unlock fee.**  
**Command:** `double_qcf+P`.  **Replaces:** `super_3`.

**Why buy/spend:** A distinct high-commitment use of the same saved credits.

**Actual proposed behavior:** Proposed replacement for Rush Cascade, not an automatic migration. Commit 1500 at startup; after 12 vulnerable activation ticks, enable a 240-unfrozen-tick state. Only listed HIT-only normal-to-normal edges become legal; no speed/damage/defense buff. Block/parry/whiff never grants the extra edges. Throws, EX and supers are excluded from the extra chain graph; ordinary explicitly allowed EX still costs 300. State ends on knockdown, capture, dizzy, round end or expiry. No refresh while active, no refund and no duration earned from hits.

**Compared with free play:** All ordinary fighting stays intact. Selecting this art excludes the other arts for the match; the fee buys an activation, not automatic damage.

**Counterplay:** Block/parry the routes to deny the hit-only extensions, disengage during the fixed lifetime, or punish activation. A knockdown/capture ends the state. A whiff, parry, blocked or interrupted committed super never refunds its purchase.

**Timing seed:** 12 / 0 / 0 startup / active / recovery ticks. Full seed fields are in the JSON; this is not an on-block/on-hit measurement.

**Presentation:** A distinct art identity, concise paid-startup receipt, readable threat and recovery. Install uses an expiring status icon, not an earnable/refillable meter; field uses visible geometry and lifetime cues.

**Prove before release:** Exact cost and cost minus one, with full and reduced-flash cues. Test raw start, correct confirm, wrong confirm, reversal, whiff, block and parry. Test maximum-bank repetitions and zero-credit defense; illegal active-state reactivation has zero debit.


### Vale

#### V-A1 — Skybreak

**Vertical interception · core_trial · 900 credits per committed activation; no unlock fee.**  
**Command:** `double_qcf+P`.  

**Why buy/spend:** A distinct high-commitment use of the same saved credits.

**Actual proposed behavior:** Preserve the existing first vertical art as a defined anti-air/punish option. Affordability alone does not guarantee every hit connects; compare to the free charge anti-air and EX before tuning price.

**Compared with free play:** All ordinary fighting stays intact. Selecting this art excludes the other arts for the match; the fee buys an activation, not automatic damage.

**Counterplay:** Stay grounded, bait the ascent, or parry/block its contacts. A whiff, parry, blocked or interrupted committed super never refunds its purchase.

**Timing seed:** 5 / 20 / 35 startup / active / recovery ticks. Full seed fields are in the JSON; this is not an on-block/on-hit measurement.

**Presentation:** A distinct art identity, concise paid-startup receipt, readable threat and recovery. Install uses an expiring status icon, not an earnable/refillable meter; field uses visible geometry and lifetime cues.

**Prove before release:** Exact cost and cost minus one, with full and reduced-flash cues. Test raw start, correct confirm, wrong confirm, reversal, whiff, block and parry. Test maximum-bank repetitions and zero-credit defense; illegal active-state reactivation has zero debit.

#### V-A2 — Crosswind

**Projectile confirm / lane cash-out · core_trial · 1200 credits per committed activation; no unlock fee.**  
**Command:** `double_qcf+P`.  

**Why buy/spend:** A distinct high-commitment use of the same saved credits.

**Actual proposed behavior:** Preserve the projectile super as a committed cash-out for a verified lane or confirm. Existing projectile occupancy must reject startup before debit, not spend then fail to spawn.

**Compared with free play:** All ordinary fighting stays intact. Selecting this art excludes the other arts for the match; the fee buys an activation, not automatic damage.

**Counterplay:** Jump or parry/block the path according to actual startup; punish bad placement or owner recovery. A whiff, parry, blocked or interrupted committed super never refunds its purchase.

**Timing seed:** 6 / 8 / 35 startup / active / recovery ticks. Full seed fields are in the JSON; this is not an on-block/on-hit measurement.

**Presentation:** A distinct art identity, concise paid-startup receipt, readable threat and recovery. Install uses an expiring status icon, not an earnable/refillable meter; field uses visible geometry and lifetime cues.

**Prove before release:** Exact cost and cost minus one, with full and reduced-flash cues. Test raw start, correct confirm, wrong confirm, reversal, whiff, block and parry. Test maximum-bank repetitions and zero-credit defense; illegal active-state reactivation has zero debit.

#### V-A3 — Prism Lattice

**Paid spatial setup · advanced_trial · 1500 credits per committed activation; no unlock fee.**  
**Command:** `double_qcf+P`.  **Replaces:** `super_3`.

**Why buy/spend:** A distinct high-commitment use of the same saved credits.

**Actual proposed behavior:** Proposed replacement for Tidal Step. After 18 vulnerable startup ticks place one stationary, mid/blockable barrier, 45 or 90 units in front based on the initiating P strength. It has a 180-unfrozen-tick life and two contacts at most, 45 raw damage per fighter contact, minimum 18 ticks between contacts. An eligible normal projectile reflection consumes a contact, retains lifetime and has reflection depth one. Enemy direct strikes can destroy it; owner knockdown/capture/dizzy removes it. No physical wall, refresh, stacking with another major field, random effect or money generation. See field arbitration contract before implementation.

**Compared with free play:** All ordinary fighting stays intact. Selecting this art excludes the other arts for the match; the fee buys an activation, not automatic damage.

**Counterplay:** Interrupt setup, destroy the field with a spaced normal, stay outside it, or block/parry its mid contacts. Zero credits must not require buying a counter-item. A whiff, parry, blocked or interrupted committed super never refunds its purchase.

**Timing seed:** 18 / 1 / 24 startup / active / recovery ticks. Full seed fields are in the JSON; this is not an on-block/on-hit measurement.

**Presentation:** A distinct art identity, concise paid-startup receipt, readable threat and recovery. Install uses an expiring status icon, not an earnable/refillable meter; field uses visible geometry and lifetime cues.

**Prove before release:** Exact cost and cost minus one, with full and reduced-flash cues. Test raw start, correct confirm, wrong confirm, reversal, whiff, block and parry. Test maximum-bank repetitions and zero-credit defense; illegal active-state reactivation has zero debit.


## 6. Why these supers make the resource idea more interesting

A good economy should fund different kinds of advantage. Circuit Break buys a tested close conversion. Forge Impact buys a particular vertical response. Crosswind buys a projectile cash-out. Overtime buys a temporary set of execution routes. Prism Lattice buys a contested location. "Every expensive art is a longer cinematic for more damage" is deliberately not the target.

The point of Overtime is not "+20% damage for four seconds." It makes a specific HIT-only cancel graph available while normal speed, health, ordinary defense and raw damage remain unchanged. The player still has to obtain an opening and execute the route. The opponent can deny the opening or remove the state with a knockdown/capture. See the exact graph and expiry rules in the combat contract.

The point of Prism Lattice is not "pay to make the opponent guess an impossible left/right." It creates a visible, destructible, finite barrier. A single guard orientation rule, no stacked major fields, limited contacts and zero-credit defensive fixtures are prerequisites. Aegis is a creative reference for buying space, not permission to copy every extreme setup from another game.

## 7. Example economic plans

These are legal budgeting examples, not optimal builds or guaranteed combo sequences. Full machine-readable plans are in `data/example_plans.json`.

| Situation | Rentals | Cash after rent | What that cash can finance |
|---|---|---:|---|
| Rook starts with 600 | False Start for 300 | 300 | One EX attempt, or simply saved money. |
| Rook starts with 600 | Slipstream Entry for 600 | 0 | Repeatable entry/branches plus the free kit; no paid activation this round. |
| Rook has 1,800 | Clinch 900 + High Hook 300 | 600 | Two EX attempts, not an additional 900-credit super. |
| Rook has 1,800 | Rivet Lift 600 + Check Step 300 | 900 | One selected Circuit Break, or three EX attempts. |
| Vale has 1,800 | Returning Pulse 900 + Needle Check 300 | 600 | Two EX opportunities, subject to projectile occupancy and legal states. |
| Vale has 2,400 | Recoil Step 300 | 2,100 | Prism Lattice 1,500 plus two EX activations; advanced concurrency tests still apply. |
| Rook has 1,800 | None | 1,800 | Circuit Break plus three EX activations, or save. Not necessarily one combo. |

A 900-credit rental and three EX activations have the same opportunity cost, not the same gameplay value. A rental can be used repeatedly or change the opponent's behavior without being used. Compare equally funded players where the no-rental player actually retains and may use the saved cash; do not refund the rent inside the experiment.

## 8. No blanket stat shop

Do not fill weak catalog slots with global +health, +damage, faster normals, wider parry windows, cheaper supers or increased income. Those changes amplify many situations at once and complicate the opening-win feedback already being tested.

Move-local properties are allowed: a particular attack can have armor during a fixed windup, a particular movement can evade ordinary projectiles in one interval, a counter can answer one class of strike, or a super can temporarily allow a specific cancel route. Each has a visible action, limited eligibility, recovery and counterplay. That is different from a passive always-on stat increase.

A future character may have distinctive fixed health or movement statistics. A separate explicitly approved stat-sidegrade experiment is possible, but it is not part of this catalog and must not be slipped in as a numerical tuning fix.

## 9. Purchase decisions should remain readable

A card must say: the problem it addresses, its input, whether it adds or replaces an action, its actual price/duration, its main downside, and what combat options remain affordable after buying it. Show the selected super's name and cost, not just "Art 3." Show why an active object prevents another move before the player mistakes rejection for dropped input.

There is no button to "buy two EX charges." A preview may say **600 left = two EX attempts OR a saved 600**, but that is arithmetic, not a separate inventory. A reserve is labeled **Do not spend below X this round**, lives under advanced planning and defaults to zero. It is not money reserved for a usable super.

Untimed catalog practice can demonstrate the move on hit/block/parry and compare it to the free kit. During a competitive countdown, only concise prerecorded/read-only previews are allowed; do not pause an opponent to run a training session.

## 10. Release means demonstrated tactical value

Every candidate needs a fixture that shows its intended use, a free-kit comparison, and a free opponent response. It also needs actual input, spend, contact, expiry and rollback checks. Then people need to understand it and choose it for a reason. Coverage of the move ID is not evidence that the purchase is good.

Keep the earlier comeback experiment separate. At equal first-round spending the current payout schedule creates a 300-credit opening gap, which can cross a super/EX threshold. This catalog does not secretly increase loser payouts or add compensating stat boosts. Test the catalog first under a frozen payout schedule; compare the audit's payout candidates in a distinct experiment.

Complete source/UI/physical/human verification independently. This document proposes a full content direction; it does not declare the game, the economy, or any matchups balanced.
