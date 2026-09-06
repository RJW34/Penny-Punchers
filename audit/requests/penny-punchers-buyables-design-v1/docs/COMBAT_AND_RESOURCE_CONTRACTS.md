# Combat, lifecycle and single-wallet contracts

**Design semantics for implementation; all numbers are candidate seeds.** This file resolves behavior that a price/name list cannot. It does not claim the current engine already supports the new capabilities.

## A. Transaction boundaries

Preparation is a draft until both plans lock under the mode's declared information policy. Validate character, item ID, one-per-slot, total limit, wallet, reserve and conflicts before mutating either player's balance. Commit once and record the final ordered item IDs and prices. A failed plan does not partly spend. Refund/replacement in the uncommitted draft is simply a new draft, not a compensating credit grant. The UI must disclose timeout behavior rather than silently discarding a plan.

A paid action must pass input/availability/transition/object/active-state/funds/reserve checks before a single atomic startup debit. Its identity binds session, round, seat and action ordinal. Corrected prediction restores the same snapshot; do not add a refund transaction on top of a restored wallet. An actual committed whiff, blocked move, parried move or interrupted move retains the expense. A recognized paid input must not silently execute an ordinary attack when it fails funds/legality.

Rentals charge only at preparation commit. Their subsequent legal moves and branch continuations cost zero. EX and supers charge only on activation. No catalog entry charges an unlock fee and an activation fee simultaneously. No refunded or transferable installed state exists.

## B. Commands and availability

Use explicit fighter/move IDs, not string guesses from display names. `P` denotes one fresh punch for rented signature actions; `PP` and `KK` refer to the existing explicit chord recognizer. Preserve current underlying input policy unless a separately logged input decision changes it. Do not casually widen the same-tick chord requirement while implementing buyables.

Exactly one signature per fighter resolves its shared input: Rook uses qcb+P, Vale qcf+P. All gambits use the exclusive 4+HP+HK slot. Technique replacements use 6+HP and remove the original `command_fhp` only while equipped. Descending Heel and Skycatch add explicit air commands rather than removing the grounded forward-HP action; their cost and slot are their opportunity cost.

The new air command must win over an ordinary air normal only when fully recognized and legal under its documented height/previous-action rules. An equipped, deliberately recognized special that then fails its own legality check must give a clear diagnostic; do not make implementation accidental fallback the balance rule. Test both choices against intended ordinary aerial execution and settle any usability issue explicitly.

Super identity is selected freely for the match, one of that fighter's configured arts. Inputs for unselected supers are not simultaneous additional choices. All eight EX families remain baseline capabilities; no shop item is required to unlock them.

## C. Clock convention

Action frames are zero-indexed. A regular active interval is `[startup, startup+active)`; end points are exclusive. Actor hitstop and global freeze halt the actor's action frame. Ordinary hitstun, blockstun, cancel and reversal timing follow the authoritative combat core, not cel count.

An install/field lifetime uses **unfrozen simulation ticks**, excluding global superfreeze but including ordinary actor hitstop/hitstun/blockstun. This prevents repeated contact hitstop from replenishing an install. Its timer is part of snapshots/replay. Pause halts simulation normally. Round end immediately clears every rental/temporary field/install. No timer is earned, stockpiled or converted back to cash.

`recovery_is_landing=true` means the listed recovery begins at landing, not a fixed air recovery that can be erased by touching the floor. For Vault Hop, a proposed complete trajectory is 3 prejump ticks, 20 flight ticks, 8 landing ticks: initial vertical velocity 7.6 units/tick, gravity .8, horizontal velocity 1.0. With position advanced before gravity, the apex is 40 units and the flight returns to floor after 20 ticks. Clamp collisions normally. These are original target kinematics, not current engine measurements.

Arc Pulse's target trajectory is `vx=2.6, vy=3.2` world units/tick from `(28,42)` relative to the actor, no gravity, 60 ticks maximum. It naturally misses low close targets; terminate on offstage bounds. Reevaluate geometry in production, recording any seed changes.

Slipgate computes a clamped backward destination at startup. Travel to that fixed destination over 7 ticks using deterministic integer interpolation; do not re-aim at the moving opponent or clip through a fighter. If blocked, stop early and retain full recovery. Projectile evasion applies only during the middle interval and never grants strike/throw invulnerability.

## D. Collision authorship and special states

Hit, hurt, push, projectile, counter, interception and field boxes are separate semantic types. Rendering never chooses a box. Every new move requires explicit startup/active/recovery hurt silhouettes; do not automatically construct them from half a hitbox and call the art aligned.

A low-profile entry avoids an attack because hurt and hit geometry do not intersect, not because the move is secretly invincible to every projectile. Cross Counter's eligible direct strikes are grounded mid/overhead guard classes, excluding low, aerial, projectile, throw and super. Its riposte is a separate action; it can miss at range. The negated first strike cannot also damage the defender via a second branch of resolution. Decide simultaneous trigger cases before implementation and mirror the tests.

Tempered Blow's one-contact armor is local to one startup. It takes full damage; lethal damage ends the round and dizzy interrupts. Low, throw, projectile, super or subsequent contact bypasses the armor. That flag is not a stored token or a regenerating shield. Holding the attack never restarts the window or raises its contact allowance.

A branch follows a real fresh input and a specific source state/window. No recognized generic special cancel is a substitute for that branch. Branches never wrap back to their entry action. Rehit groups, juggle consumption, damage scaling, throw eligibility and wakeup immunity remain active.

## E. Overtime route graph

The proposed extra graph is HIT-only and grounded at source. Use actual move IDs and authored contact windows for these edges:

```text
s_lp -> s_mp
s_lk -> s_mk
c_lp -> c_mp
c_lk -> c_mk
s_mp -> s_hp
s_mk -> s_hk
c_mp -> c_hp
c_mk -> c_hk
```

Each edge is accepted only after this source action actually hit, while its ordinary contact-cancel window is open. Where a source move lacks such a window, author and test one explicitly instead of treating the whole animation as cancellable. The normal source action frame/first-contact ordinal governs it. No reverse strength edges, no same-normal self-cancel, no close-normal substitution that bypasses the graph, no throw/air/lease/EX/super nodes. Normal existing special/super cancels are retained only where authored; an EX is still 300 and a different super is not selected.

The graph is acyclic within the cancel sequence. Returning to neutral starts a new ordinary sequence; it does not restore stun, damage scaling or juggle budget mid-combo. The 240-tick timer begins when activation completes. Incoming normal hits do not refresh the timer; knockdown, capture and dizzy terminate the state. Active reactivation is illegal and costs nothing. Repeated use after expiry requires paying 1,500 again, with no artificial round casting quota.

Avoid the false claim that four seconds means four seconds of guaranteed pressure. The user bought a chance to exploit a later hit, and can lose the investment completely.

## F. Fields, shots and guard direction

For the advanced field ruleset, implement explicit categories rather than the old universal owner-count shortcut:

- At most one **ordinary projectile instance** per owner, including normal, EX or projectile super as assigned by the registry; a multihit shot is one instance.
- At most one **major field** per owner: Pulse Anchor OR Prism Lattice. They never overlap and cannot refresh while active.
- One ordinary projectile may coexist with one major field only in the advanced ruleset after the combined-pressure gates pass. Baseline behavior stays unchanged outside that ruleset.

An ordinary/free-special projectile is `normal` tier for Wire Cut/Shear Palm eligibility. EX and super tiers are excluded by default. A rental projectile with zero activation fee is still explicitly tiered in content; Returning Pulse is normal tier in this proposal. Do not infer tier from color or credit cost alone.

Prism Lattice can reflect normal-tier shots only, uses one of its two contacts to do so, and never resets an object's lifetime/hit count. Reflected objects have depth 1; any later reflection attempt dissipates instead. The original credit receipt remains with the original paid action; deflection is not a transaction.

A barrier is mid/blockable and never a physical push wall. Opposing direct strikes contacting it destroy it. To avoid an ambiguous same-tick interaction, use this ordered intent resolution: (1) collect simultaneous contacts; (2) direct fighter strikes that reach a field destroy it; (3) destroyed fields cannot create new pulses/reflections on that tick; (4) resolve surviving fields/projectiles against actors under the ordinary simultaneous-contact policy. Both seats use the same ordering. If the core already has a stronger symmetric resolver, express equivalent results there rather than adding a seat-ordered side path.

There is a single defense-facing reference for all hostile owned objects on one tick: the opposing fighter's current side relative to the defender, sampled for that tick. A shot/field behind the defender does not independently require the opposite guard direction. Field contacts are all mid, so simultaneous owner/field contact cannot require both low and high guards. This is an explicit proposal for the advanced ruleset and needs cross-up testing across all projectiles. Do not silently mix it with a different existing orientation convention.

Destroying a field is not a guarantee that every owner-plus-field situation is safe to challenge. Require representative free defensive routes, clear prior strategic mistakes and no mechanically impossible contradictory guard inputs. Parry consumes one eligible field contact; block also consumes one. No credit gain. Owner knockdown/capture/dizzy removes anchor/barrier; plain blocking does not.

Prism placement: LP puts it 45 units ahead; MP or HP puts it 90 ahead. Clamp to stage; do not place intersecting the defender's current pushbox—shift toward the owner to the nearest legal non-overlap point. If none exists, reject before the 1,500 debit. The setter is vulnerable through startup and 24-tick post-placement recovery. A field does not refund if the owner is immediately interrupted after creation.

## G. Art and networking

Contact events contain event-time actor/target/world positions, action ordinal and effect identity. Cosmetic local startup cues may be predicted/reconciled according to the audit's netplay fix, but confirmed score/payouts never are. Suppress duplicate debit audio on resimulation. State must include lease IDs, branch stage/window, actor charge, pending throw, armor consumed flag, counter phase, previous aerial attack flag, field owner/position/age/contact/reflect data and Overtime timer/graph context.

No discarded predicted object should remain visually or financially. No sprite-driven collider. No random cosmetic number may enter a state hash. Canonical IDs are not display names.

## H. What remains engine-authored

This section specifies behavior and seeds, not final frame-data files. Exact boxes, per-contact hitstun/blockstun, launch velocities beyond the explicit examples, contact pauses and cancel edges require the production move compiler and measured traces. Mandatory per-card acceptance covers that work. Never fill an unknown on-block value with a plausible-looking number, copy reference-game frames or call a table of move IDs a finished implementation.
