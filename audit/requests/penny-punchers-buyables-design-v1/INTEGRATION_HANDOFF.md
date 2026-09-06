# Integration handoff — add the buyables proposal without disrupting audit repairs

You are the integrating coding agent. This is a design companion, not a request to overwrite the current branch with an older scaffold. The user wants a complete traditional 1v1 economic fighter with Third Strike-inspired feel; one wallet replaces all spendable combat meter. The expanded audit is being addressed concurrently.

## First action

Inspect current local Git status, branch, agent instructions, accepted decisions, current data and the audit work ledger. Record current commit/content identity and dirty files without discarding anything. The design reference commit is `1623ee62c4f4b6d5574bbb9909eb6fe4e32f5441`; it is not a restore target. Coordinate ownership with the active integrator. Do not pull/reset/rebase/stash/delete or publish merely to make the proposal fit.

Read `BUYABLES_DESIGN.md`, `docs/COMBAT_AND_RESOURCE_CONTRACTS.md`, `data/buyables_catalog.json`, `data/migration_map.json`, and the current expanded-audit findings. New explicit user decisions outrank this proposal. Audit repairs and game truth outrank historical scaffold assumptions. Record conflicts rather than silently rolling back a finished fix.

## Integration sequence

1. Add this under an isolated `design/buyables-v1/` or another agreed documentation path. It has no runtime autoload and no effect on existing builds.
2. Write a short decision record accepting/modifying the three-operation resource model and selecting the first candidate pool. Payout changes are outside this proposal. Preserve one wallet and free baseline defense.
3. Implement registry-driven item lookup/pricing/commands/roster art bindings and the shared shop view model required by the audit. Exact historical counts must not be replaced by a new magic number such as 38.
4. Map existing items by IDs using `migration_map.json`. Separate rename, retune and new-mechanic changes. Keep old replay records/content identity untouched. Generate new move IDs/version IDs where semantics changed; do not silently reinterpret an old recording as the new move.
5. Implement rental practice, plan previews, no-rental plans and transparent timeout behavior. Then implement the first 12 candidate rentals and validate all eight existing EX families/four conventional arts as comparison baselines. Keep tested legacy third arts in production until advanced replacements pass.
6. Add advanced capability packages one at a time: branch state, authored hurtboxes, counters, move-local armor, air commands/throws, projectile interception/reflection, persistent fields, then the HIT-only install graph. Each passes an isolated scenario before combinations.
7. Implement Overtime and Prism Lattice in an explicitly versioned experimental ruleset. The full final candidate catalog has three selected arts per fighter, not six simultaneously usable supers. No placeholder super counts as completed content.
8. Run per-entry scenarios and targeted interaction coverage; publish what was actually exercised. Then run matched spending comparisons and human sessions. Change prices from evidence, retaining original/proposed/accepted values and rationale.
9. Integrate only compatible accepted candidates into the actual release path, regenerate proper evidence, export and launch. A rejected candidate is documented as rejected—not hidden as a finished feature.

## Required state and evidence

For every entry record design ID, runtime move/item ID, status (proposed/implemented/software-tested/human-tested/accepted/rejected), selected ruleset, source/content/build hash, tests, actual gameplay evidence and unresolved observations. Do not claim the 38 design entries are 38 implemented moves. Keep current audit PP IDs unchanged; link this work using `BUY-` scenario IDs rather than inventing PP-092 fixes without review.

## Boundary

This companion does not authorize new paid services, public networking, accounts, monetization, copied Capcom assets, team modes or a game-engine rewrite. Do not change damage globally, add stored charges or introduce income farming to make a candidate feel worthwhile. Numerical seeds can change through an explicit tested decision; do not advertise guessed frame advantage or an untested combo.

At a genuine session boundary, leave current commit/content identity, executed commands, evidence paths, unresolved items and the next concrete task. Continue implementation from the real repository state, not from another regenerated scaffold.
