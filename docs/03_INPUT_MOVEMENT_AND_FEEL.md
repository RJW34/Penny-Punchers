# Digital six-button input and grounded movement
The resemblance target is deliberate arcade-fighter execution and grounded interaction. Numbers here are original seeds, not official Third Strike timings.

## Frame convention
An action starts at action-frame 0. `startup` is the count of pre-active ticks. With startup=5, active=3, recovery=12, hitboxes are active [5,8), the total action is 20 ticks and neutral resumes at 20. Displayed first active frame is 6. All intervals are inclusive start/exclusive end. Action-frame counters stop during relevant freeze; numbered input samples do not.

InputFrame contains seat 0 or 1, frame, screen-relative numpad direction 1..9 and held-button mask LP=1, MP=2, HP=4, LK=8, MK=16, HK=32. No analog shield, smash stick, jump button or dedicated parry button. Device adapters digitize sticks before the core. Both opposite directions resolve neutral on each axis. Sample six buttons together; disable OS repeat as an action source. A mapped same-tick PP/KK macro is permitted and disclosed; no motion macros or delayed multi-action sequences in competitive controls.

Configure two independent local devices. Keyboard is one seat; keyboard plus pad and two pads are required. Optional two-keyboard play must warn about rollover. Support pad, arcade-stick and leverless layouts through generic remapping/calibration, but claim hardware success only for devices actually tested.

## Recognizer and execution
Keep 120 ticks of bounded history. QCF=[2,3,6], QCB=[2,1,4], DP=[6,2,3], double-QCF=[2,3,6,2,3,6]. Allow consecutive duplicates, but not arbitrary missing directions or extra forward steps that synthesize a different motion. Total ordinary window 20, max step gap 8, final motion-to-button gap 5; double-motion total 30. Recognize required directional components: a neutral transition is not mandatory between quarter circles. Document each accepted/rejected shortcut in tests rather than growing accidental leniency.

Charge-back requires 45 consecutive ticks containing relative back (1/4/7), then forward within 10 ticks; charge-down requires 45 of 1/2/3, then up within 10. No charge partitioning in v1. Horizontal charge/motion tokens clear on facing-epoch changes; vertical charge survives a side switch. That is an explicit approximation, not claimed arcade parity. Collect charge during guarding, hitstop and superfreeze; do not age action timers during freeze. Motion timestamps remain simulation-tick based.

Single-button ordinary specials may use negative edge. Paid actions never execute from button release. PP/KK means a fresh simultaneous same-category pair on the same sampled tick; no implicit one-frame attack delay to guess a future chord. The training input display must explain mistimed pairs. Throw LP+LK, universal leap overhead MP+MK, and cosmetic taunt/gambit HP+HK use explicit mappings.

Contextual priority: throw tech where valid; super syntax; EX special syntax; ordinary special; throw; leap overhead; rented gambit/taunt; command normal; proximity normal; normal. In hitstun only legal buffering/defense applies. Recognize the complete EX/super vocabulary before checking owned capabilities and legal transitions. An unowned or exhausted command produces a small rejection cue without a free substitute, future execution, bank debit or artificial freeze. A licensed EX repeats after legal recovery; a prepaid super consumes its single use only on legal startup. Illegal cancels remain illegal. Saved bank and pending next-shop rewards never authorize an action.

Competitive ordinary links have zero universal action buffer. Default reversal buffer is 2 eligible wakeup/recovery ticks; cancel buffers are move-specific. Inputs during superfreeze may be remembered but not executed until an actionable tick. Exactly how a held direction plus fresh attack behaves must be consistent. Optional assist adds at most 3 link-buffer ticks, is marked in lobby/HUD/replays and excluded from default balance samples.

## Walking, dashes, jumps
Forward/back walks have distinct fixed speeds, stop immediately on release, and remain grounded. Forward/back dashes are discrete duration/movement profiles, not runs or dash dances. No universal invulnerable backdash. Double-tap directions within 10 ticks triggers dash, with a neutral/opposite release between taps. Dash's second forward may arm parry only if the parry state permits it; do not grant parry while the character is in an un-parryable committed dash.

Jump startup is 4 ticks; normal and down-then-up super jumps have separate launch velocity. No short-hop hold distinction, double jump, air steering, air dash or fast-fall. Horizontal launch is set at takeoff; gravity continues until a flat floor collision. A jump attack may change hurtboxes but not enable free air movement. Normal landing and attack landing have separate recovery. No air block. Passing over an opponent produces a cross-up and facing update when actionable; aerial attacks retain their startup-facing for their boxes until specified otherwise.

Fighters have solid pushboxes on the ground, not overlapping capsules with rigid-body bounce. In air, permit cross-ups when vertical boxes no longer overlap. Clamp feet and body to stage bounds. Camera movement cannot push a fighter or become an invisible wall. Mirrored equal-position ties use deterministic fair resolution and seat-swap tests.

## Kara throw
Only the explicitly tagged forward-heavy base move permits a cancel on action frames [0,2) to NORMAL throw, carrying its already-applied displacement. The source move applies its frame0 displacement before this contextual kara transition; remove its attack boxes and all later movement after the cancel. No arbitrary normal-to-anything kara engine. No paid-move kara bypass, refundable EX startup, extra priority, or retained hitbox. Leased replacements do not inherit kara eligibility unless explicitly tagged and tested. Provide a drill and a negative test for every disallowed case.
