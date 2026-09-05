# Verification strategy
Pack tests exercise arithmetic, invariants, fixtures, schema and packaging tools. They do not run fighting-game combat. Production C# core tests must consume/compare the fixtures AND independently cover the real game. Godot scene tests must drive actual menus/controls through an exported candidate, not use fabricated snapshots as gameplay proof.

## Required suites
Content parser/schema/reference checks; signed integer math; input/motion/charge and negative-edge boundaries; frame/cancel/parry/throw rules; every move's reachable effect; collision/guard/projectile and seat symmetry; credit preflight/startup/idempotence; rollback wallet+input restoration; preparation commit and match lifecycle; replay determinism; two-process socket play; malformed message/replay rejection; local control assignment; headless scenarios; full-match visual/audio playthrough and exports. Add property/randomized tests for no negative wallets, no unauthenticated money mutation, monotonic result settlement and bounded move/projectile counts.

`acceptance/requirements.json` is the mandatory inventory. `orchestration/dag.json` maps each requirement once to its owning package. Every accepted record points to real artifact paths, SHA-256, candidate build/content identity, command/exit code, UTC time, platform and actual reviewer where applicable. No prior-pack validation or reference test log can satisfy combat/network/feel gates.

Release tiers: software; target_device; human. Initial reports contain no successful evidence and release_gate must fail. It checks completeness/identity/integrity, not honesty or perceptual quality. Reproduce commands and inspect actual artifacts. A valid hash of a fabricated video does not make it true. Do not edit requirements or gate code to manufacture completion.

## Scenario interface to implement
The shipped game/test runner supports `--scenario <id> --seed <n> --evidence-dir <path>` for deterministic scenarios listed in acceptance/scenarios.json. An unknown/unimplemented scenario exits nonzero. It must drive real core and, for UI scenarios, the real shell. `--smoke` imports/launches a playable scene and exercises at least input→attack→paid debit→round result, not merely opening a label. Record stdout/stderr and actual exit.

Visual review: captures for neutral/guard/parry/throw/projectile/super, both characters, preparation/reveal, actual full local match, training controls and both network clients. No composite renders passed off as played frames. Keyboard inputs must visibly correspond to state/move/debit logs. All 98 move definitions require coverage, with live representative family evidence and per-move deterministic traces.

## Candidate binding
A candidate records commit or source-tree digest plus canonical content digest, exported hashes and command manifest. Any gameplay/content change invalidates affected evidence. Refresh only after rebuild/retest; don't attach old recordings to new balancing values. Evidence-completeness tools cannot detect every stale binary; manual review and reproducible builds remain required.
