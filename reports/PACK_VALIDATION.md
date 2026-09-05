# Pack validation — Strike Ledger 1v1 / single-wallet edition

**2026-09-05 • SCAFFOLD VALIDATED. GAME NOT BUILT OR VERIFIED.**

This replaces the platform/team scaffold and transitional drafts. No earlier game's acceptance evidence is imported. The receiving Astra must implement the game.

| Check | Actual result |
|---|---|
| Canonical data, cost/slot/availability/timeline and single-wallet semantics | PASS |
| JSON Schema Draft 2020-12 definitions and supplied instances | PASS |
| Scope regression: team fields, third seat, sponsored/provenance wallet, earned meter and combat income rejected | PASS |
| Independent Python arithmetic/reference and packaging-tool tests | **114 tests passed** |
| Dependency/acceptance cross-references | **15 packages, 82 requirements: 76 software / 5 target-device / 1 human** |
| Content inventory | **2 original fighter data sets, 49 actions each (43 base availability), 12 leases, 2 flat stage layouts** |
| Derived original move-table CSVs | PASS: 98 rows |
| Python source syntax and C# project XML structure | PASS; XML parsing is NOT a C# compilation |
| Canonical-content copy | Dry-run PASS; apply behavior tested only in isolated temporary fixtures |
| Release evidence gate | Expected FAIL in software/device/human/all tiers: game evidence is empty |
| Native tools on packaging host | Neither dotnet nor Godot found |
| C# harness compilation/execution | **NOT RUN** |
| Godot import/gameplay/export | **NOT RUN; game is not implemented by this scaffold** |
| Actual combat, paid move transitions, rollback, controllers and target OS launches | **NOT RUN / NOT VERIFIED** |
| Human Third Strike–inspired feel verdict and balance | **NOT RUN / NOT CLAIMED** |

The 114 tests include hand-specified payout/startup/score/math vectors; 10,000 randomized payout cases and 2,000 randomized spending sequences; preparation atomicity/idempotence and canonical plan order; immutable-spend restoration; exact-match result accounting; malformed scope/schema rejection; content-copy safety; and evidence integrity negative cases. These are tests of the oracle/tools, not production combat tests. Paid interruption/parry/KO rollback still require the actual engine, as specified by the acceptance inventory.

## Reproduce
Use Python3.11+ and install requirements-tools.txt in a local environment, then:
```
python tools/validate_pack.py --strict-schema
python -m unittest discover -s reference/tests -v
python tools/build_move_tables.py --check
python tools/verify_manifest.py
python tools/doctor.py
python tools/release_gate.py --tier all
```
The last command MUST initially return nonzero. Doctor may return nonzero for missing tools; that describes the local environment, not game readiness. Captured commands/exits are in reports/pack_checks/. Their successful negative tests are not release passes.

## Archive integrity and uncertainty
PACK_MANIFEST.json lists delivered files except itself, with byte sizes and SHA-256. Delivery verification extracts the final ZIP to a fresh directory and reruns strict validation, all tests, move-table checks and the manifest. The final external delivery verification log records those actual results after the ZIP is built; it is not gameplay evidence.

Intentional later development changes legitimately invalidate this original delivery manifest. Do not revert working code to satisfy it. Actual gameplay releases need their own source/content/candidate identity and new evidence.

All frame windows, action values, prices and match rules are original experimental proposals. Sources document the qualitative inspiration and toolchain, not exact arcade parity or proven balance. C# source has been authored but not compiled in this packaging host. The Godot scene explicitly says SCAFFOLD ONLY and exits nonzero for unimplemented smoke/scenario requests.
