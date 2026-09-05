# Architecture and integration contracts
## Layers
`StrikeLedger.Core`: deterministic integers, input history and command recognizer, fighter/action state machines, collisions, projectiles, parry/guard/throws, credit debits, round lifecycle, content loading, immutable snapshots and stable serialization. No Godot types, OS clock, filesystem writes, audio, HTTP, unordered iteration, floating-point collision, or device polling.

`StrikeLedger.App` (can be an assembly or explicit namespace): lobby/session flow, preparation commit, records, replay container, bot inputs, rollback session, reliable control protocol, local/remote seat assignment. It calls the same core in tests, bots, training and shipped play.

`game/`: Godot scene, rendering, original animation/audio, menus, input adapters, camera, debug display and export. It interpolates presentation, not outcomes. Animation never determines a hitbox's active frame or the moment credits are charged.

`StrikeLedger.Tests`: real core conformance, headless scenarios and integrations. `reference/` is a Python oracle only. An agreement between two duplicated bugs is not gameplay validation; retain hand-derived cases, adversarial tests, recordings and human feedback.

## Core surface to implement
- `Step(InputFrame seat0, InputFrame seat1)` advances exactly one numbered simulation tick and returns state + typed events.
- `Capture()/Restore(snapshot)` round-trip every future-affecting field.
- `SerializeCanonical()/Hash()` use a versioned little-endian canonical order, explicit collection order and schema bounds. A debug SHA-256 hash is acceptable; no language-randomized hash codes.
- `TryStartAction(candidate)` validates state/cancel/availability/credit floor, then atomically changes action state and debits its cost. A failed transition changes neither.
- `CommitPreparation(plan0, plan1)` validates both plans against the immutable post-settlement balances, then applies both once or neither.
- `SettleRound(confirmedTerminalState)` updates BOTH wallets and score exactly once. It cannot run from speculative KO.

## Tick order
Sample and retain normalized inputs; derive edges and facing context; update eligible command/parry/reversal buffers; resolve actions for both players from the same pre-contact snapshot; apply valid startup costs; advance unfrozen actor clocks/movement; update projectile motion; resolve floor/walls/pushboxes; build all collision candidates; resolve throw-tech/defense/strike priority from a common snapshot; commit all effects together; update stun/knockdown/timer; compute terminal result; emit deterministic events/hash. Never make seat0's mutation the precondition for seat1's otherwise simultaneous strike. Exact boundary order is tested and documented.

## Snapshot inventory
Round id and phase, score, both scalar wallets and recovery tiers, selected supers and locked leases/floors; tick counters and input histories; position/velocity/facing/facing-epoch; grounded state/action frame/cancel eligibility; freeze and stun counters; health/stun/combo/juggle and throw-immunity state; parry arm/retry/consumption; charge and motion buffers; projectile ids/state/hit registries; deterministic random state if any; spend receipts and action ordinals; pending terminal result and confirmed frame. Audio handles, texture references and render interpolation are excluded.

The rollback timeline includes combat spending; the match transaction log contains confirmed preparation and settlement. Restoring a snapshot restores its wallet and spend log together. Do not put irreversible combat debits in an external singleton/database.

## Time and load
60 fixed simulation ticks per second. A full-screen freeze and a per-actor hitstop are distinct. All wall-clock accumulation belongs to the shell; do not speed combat up to catch up indefinitely. Bounded catch-up and explicit desync/pause diagnostics, never dropped simulation state. Profile actual available hardware and record p95/p99 frame cost, replay resimulation budget and render pacing; no invented performance numbers.
