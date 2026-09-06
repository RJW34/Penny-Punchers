# Penny-Punchers — shop-only economy rework v2
**Implementation scaffolding, not a finished patch or verified game.** Latest user feedback supersedes the prior direct-spend designs.

Use this INSIDE the current `RJW34/Penny-Punchers` checkout as `rework/shop-only-v2/`. Do not extract over source, restore an old commit, or replace ongoing audit repairs. Open the coding agent in the repository ROOT and paste this folder's `EXECUTOR_PROMPT.md`.

## The new game
Money is spent only when a buy-period plan locks. Ordinary moves and defense remain free. EX families must be purchased as round-long licenses; two may be equipped and they repeat after legal recovery without extra money or ammo. A purchased super supplies one use for the round. One super can be chosen anew in each buy period. All acquired capabilities expire after the round; unspent cash carries. No winner-retained upgrades, global stat purchases, team finances, or earned combat gauge.

Counter-hit, anti-air and precision-parry bonuses are generated from actual combat. They accumulate as nonspendable round earnings and deposit only with confirmed settlement. They cannot buy a mid-round move. See the exact anti-farming rules before implementation.

The license duration, two-EX cap, one-use super policy and numbers are explicit design defaults, not quotations from the user. They are resolved so implementation can begin; tune through recorded evidence without restoring in-fight payments.

## Read order
`docs/00_AUTHORITY_AND_DECISIONS.md` → `docs/01_PRODUCT_AND_LOOP.md` → `docs/02_LOADOUT_AND_CATALOG.md` → `docs/03_REWARD_SEMANTICS.md` → `docs/05_SOURCE_MIGRATION.md` → `work_packages/INDEX.md` → `acceptance/requirements.json`.

Run from this pack directory:
```
python tools/validate_pack.py
python -m unittest discover -s tests -v
python tools/generate_vectors.py
python tools/verify_manifest.py
```
These are reference/design checks, not gameplay certification. After integration, actual C# production tests, running Godot menus, full matches, rollback, exports, and player sessions are required. Source seed compilation status is recorded honestly in `reports/VALIDATION.md`.

## Current integration boundary
The remote head inspected was `1623ee62c4f4b6d5574bbb9909eb6fe4e32f5441`; it is a comparison baseline, NEVER a reset target. Local/concurrent audit changes may be newer. Preserve all implemented moves and repaired controls/camera. The 26-entry baseline migration catalog covers the current 12 rentals, 8 EX families and 6 supers. A separately adapted 38-entry prior design library remains proposal material; do not falsely label its unbuilt mechanics finished or block the resource migration on unrelated new moves.
