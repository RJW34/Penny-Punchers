# Collision, defense, hit resolution and combat rules
## Geometry and action data
Use integer world units, positive Y up, mirrored local axis-aligned boxes and swept collision for fast displacement/projectiles. Pushboxes, hurtboxes, hitboxes and throw ranges are independent. A box's x/y is its local center from fighter feet; widths/heights are full dimensions. Validate positive extents and mirror center X, not width. Standing/crouching/air base hurtboxes are authored; add pose-specific hurtboxes during implementation, with visible motion and no accidental invisible weapon immunity.

Move tables contain 49 action definitions per fighter. Every listed move must be recognizable, reach startup/active/recovery, show an appropriate animation, and perform its declared projectile/throw/movement effect. An empty hitbox array is correct for a projectile, throw or feint only when that corresponding effect really exists. The CSV is derived documentation, not another numeric authority.

A hit-group contacts a target at most once per action instance. Different groups allow deliberate multihits. A parry consumes that group for that defender too; it must not rehit every active tick. Projectile ranks and contact registries are explicit. Equal-rank projectiles cancel; higher rank survives with bounded remaining contacts. No unbounded free projectile duplication.

## Resolution and guard
Build candidates for both sides before mutation. Prototype clash rule: at a same-tick reciprocal strike contact, higher attack rank wins; equal rank trades. It applies only to actual simultaneous reciprocal contact, not every attack across the screen. Damage/invulnerability and guard are evaluated before final effects. A same-tick active strike defeats a new throw attempt; throw-tech has priority for a valid existing normal throw. Verify symmetry by swapping seats and mirroring state.

Holding relative back guards mids/overheads; down-back guards mids/lows. Overheads and jump attacks defeat crouch guard; lows defeat standing guard. No air block. Blocking requires an eligible grounded state and does not interrupt one's attack. Guard animation, blockstop, blockstun and pushback must not be conflated. Do not permit walking while blockstunned or generate credits for guarding.

Normal chip is zero. Special/super chip begins at 10% of scaled damage, integer floor, and can KO; successful parry prevents damage, chip and stun. Chip is not an additional attack hit or credit event. This makes a last-health parry meaningful, but chip-pressure viability must be tested under wealthy/poor budgets. No guard gauge or purchasable guard break.

## Parries: execution and risks
A fresh neutral-to-forward edge arms high parry; neutral-to-down arms low parry; neutral-to-forward while airborne arms air parry. Cardinal edges are required in v1 (diagonal rolling does not automatically grant both). At least one neutral tick re-arms a fresh attempt. Ground high window 10 eligible ticks, low 10, air 7; unsuccessful attempts incur a 20-tick retry lockout. A successful contact consumes one arm, clears the miss lockout, and requires a new edge for the next hit. No held-button automatic multihit parry.

High parry catches mids/overheads/jump strikes, not lows. Low catches lows/mids, not overheads/air strikes. Air catches parryable strikes/projectiles, not throws. Parry does not reflect projectiles. When a parry succeeds, defender freezes 8 ticks and attacker 14; projectiles use explicit source handling rather than remotely freezing the owner regardless of distance. Set projectile-only success to projectile freeze plus defender freeze unless the originating close attack is directly involved. This approximation needs real multihit drills.

Red parry is a fresh correct-direction parry edge from grounded blockstun, with a 2-tick arm window; it is not an automatic reward for holding forward out of block. It may interrupt blockstun only on a successful eligible contact. Failed attempts do not erase remaining blockstun. No red parry while being thrown/hit. Superfreeze does not age a parry window or create repeated edges; an input made during it may arm at the first eligible postfreeze tick exactly once. Record the input/eligible-clock rule in traces.

Parries cost zero and award zero credits. They remain available to a zero-credit player. They are a defensive/tactical skill, not another economy generator or a paid consumable.

## Throws, knockdown and stun
Normal throw LP+LK has startup, short active range and whiff recovery. Back throw swaps sides only on success. Normal throws can be teched inside the configured five-tick window, ending in deterministic separation. Command throws cannot be teched/parried; ordinary jump/spacing/strike decisions remain answers. V1 jump startup is throw-invulnerable; post-wakeup/hitstun/blockstun immunity is two ticks. Throws cannot connect through solid separation or invulnerable state.

Soft knockdown permits quick rise on down within the configured impact window. Hard knockdown does not. Normal/quick/hard timings are 42/24/55 ticks as initial proposals. Wakeup actions use the explicit reversal buffer. Corners retain pushback rules; no wallbounce or groundbounce by default. Juggle budget starts at 10, decreases per contact and resets only on a neutral landing/new combo. Prevent loops by group registries, combo scaling and juggle limits, not nondeterministic bailout logic.

Stun is a non-spendable buildup state. Attacks add authored stun, delay decay 120 ticks, then decay 5 per eligible tick. Reaching the fighter threshold enters 90-tick dizzy; no mash shortening in v1. A combo cannot inflict a second dizzy before the defender has regained neutral. Blocking/chip does not add stun. It resets each round and cannot buy moves or exchange for credits.

## Damage, combos and clocks
For hit index n starting at 0: counterhit base is floor(base*1.10), otherwise base; then multiply by max(30,100-10*n)% and floor. Counterhits do not add hitstun in v1. Throw damage participates in declared scaling. Super multihits each increment combo index; no unscaled duplicate final blow. Only damaging strikes/throws increment it, not block/parry/whiff.

Cancels require their data window and contact condition. `requires_contact` means hit OR block, never parry/whiff. Confirmation is the player's input decision, not an engine auto-confirm. A target combo selects its named next move rather than ordinary proximity dispatch. A link waits until neutral; do not secretly use cancel logic. Hitstop pauses both contacting actors' action frames, not input sampling. Superfreeze begins once after a successfully paid startup; two same-tick supers cause one shared max-duration freeze, not seat-biased sequential debits/freezes.

At zero health, process all simultaneous contacts before result. Double KO is draw. Timeout compares normalized remaining health by cross multiplication, never floats. Combat timer pauses during full freeze, local pause and confirmed network stall; otherwise advances during ordinary movement and per-actor hitstop. Do not count loading/menus as combat time.
