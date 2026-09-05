# AFTER HOURS gameplay boundary review

Reviewed 2026-09-05T11:11:10.538053+00:00 by `/root/combat_core`.

The bounded review passes: no unresolved gameplay-boundary defect was found in the reviewed source. Four presentation timing/priority defects found during review are now corrected. The exact reviewed source hashes are recorded in the companion JSON. All 13 reviewed production inputs are now bound to the completed candidate `source-sha256:5414744b92e51de04d4be67df1929929a1072e7d5b986d773b1459166217784d`. Fresh native presentation verification remains a separate parent task.

## Scope and method

Read the new renderer, DTO adapter, shader, atlas metadata and supporting frozen Core timing/contact code. Ran actual SHA256 comparisons against the preserved baseline, checked canonical content and move references, and parsed the build property. Manual source conclusions are distinguished from executed checks in the JSON. The reviewer authored Core/Rook assets earlier but did not implement the shared renderer corrections; this is code review, not external playtesting.

## Corrections verified

- Authoritative result, health, knockdown, dizzy, hitstun and blockstun states precede cosmetic event timers. A stale tech cue cannot hide a later hit or block. Guard interrupts action art; delayed cosmetic parry/tech cues cannot hide a fresh action.
- Parry art survives global superfreeze while actual defender hitstop remains. Taunt art follows the committed DashTicks duration rather than an absolute Tick deadline.
- Frozen locomotion retains its previously observed direction, keeping backdash art stable when Tick advances without motion.

## Boundary checks

Action cels use canonical ActionFrame and half-open startup/active/recovery intervals. Cel counts never change move timing. Universal animation ages stop while frozen. Backward timeline resets clear cosmetic deadlines, effects and animation ages.

Actor roots, facing, foot pivots and the fixed camera projection read simulation state. Runtime projectiles originate only from sim.Projectiles; their visual age derives from remaining canonical life and their direction from actual velocity. Debug boxes retain canonical geometry. FX sizing, shake and particles are cosmetic.

Both atlases map all 49 canonical moves with nonempty phase arrays and resolving cell references. Rook uses 79 selected cells; Vale uses 80. Vale EX pulse explicitly shares the clean caster-release pose; super2 selects the targeted cleaned image. Rook detached projectile rings remain outside runtime crop rectangles. The actual Core projectile supplies each gameplay orb.

Matte removal precedes palette changes. The Rook alternate targets navy clothing, preserving warm skin and jacket ramps; Vale changes teal clothing to violet. Generated RGB matte sheets are honestly documented and originals remain preserved.

## Executed identity evidence

- 18 Core/App source inputs match the preserved baseline byte-for-byte.
- All 13 canonical JSON files produce `6fd0fe07da1f14d72754869b12cfe6cf21721d5055572c5b9f5b43dd72604c8b`.
- Both Windows and Linux Core DLLs: `204653b031ca2700371d8c1df62c4b7b9eb131825fcaaba1ef5ee01676c2c7df`.
- Both Windows and Linux App DLLs: `72e7b96bbd878126db96ba0be8e49bae49df1417bf3162f4aec4d1f325e6cebb`.
- Directory.Build.props contains only IncludeSourceRevisionInInformationalVersion=false, a metadata change.

The machine report records 23 checks, all passing, with their methods and details.

## Remaining evidence boundary

The final candidate was created at 2026-09-05T11:10:24.207758+00:00. All 13 reviewed production inputs match its recorded hashes and the current actual files. The metadata helper is also hashed separately. Fresh native art/UI/match/platform evidence remains the parent task. This review makes no frame-rate, controller, two-PC or human-playtest claim. Old gameplay tests are inherited only through unchanged Core/App/data identity.

[Machine-readable checks](evidence/after-hours-boundary-review.json)
