# Penny-Punchers audit — expanded economy edition

**Date:** 5 September 2026. **Revision:** `1623ee62c4f4b6d5574bbb9909eb6fe4e32f5441`.

This is the original audit plus an appended purchase/stat/comeback analysis. It does not change the GitHub repository, implement a new rule, or certify the game balanced.

## Read first

- `Economy_and_Purchase_Balance_Addendum.md`: all 12 rentals, six-art price screening, stat-upgrade policy, opening-wallet arithmetic, recovery timing, and the correct score-only fairness benchmark.
- `Penny-Punchers_Audit.md`: original 77-finding audit followed by the full supplement and 14 new findings.
- `Penny-Punchers_Audit_Backlog.json`: combined **91** prioritized findings, with original IDs preserved.
- `Economy_Playtest_Protocol.md`: proposed controlled experiments; these gameplay trials have not been run here.
- `Regression_and_Playtest_Plan.md`: original plan with the economy protocol appended.
- `economy_appendix/`: actually executed independent Python calculations/tests and exact-hash-verified source snapshots. These are not the production engine.

## Actual supplement checks

Fourteen source files match current repository blob identities (13 canonical JSON files and EconomySeed.cs). Thirty-seven analytical/content unittest methods passed. Nine opening aggregate-spend combinations and a bounded equal-spend ledger-state enumeration were calculated. Native C#/Godot, controllers, human play, and economic win-rate effects were not tested here.

The initially mistaken auditor-test expectation for Rook's 1500-cost all-hit static payload was corrected from 201 to 202. Initial/final logs are retained. No game code was changed as a result.

## Preservation and integrity

`historical/Penny-Punchers_Audit_2026-09-05.zip` is the original archive unchanged. The old `audit_calculations.json` remains the original audit's calculations; new results live under `economy_appendix/calculations/`. `FILE_SHA256.json` inventories the expanded delivery, excluding itself. `VALIDATION.md` records the new package checks and fresh extraction rehearsal.

## Reproduce the supplemental arithmetic

```sh
cd economy_appendix
python economy_analysis.py --data verified_source/data --output calculations
python -m unittest test_economy_analysis -v
```

Python 3.11+; standard library only. Read the limitations in the results before using any number as gameplay evidence.
