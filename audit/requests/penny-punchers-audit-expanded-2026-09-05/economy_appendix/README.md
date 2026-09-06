# Auditor arithmetic and source snapshots

These files check arithmetic and data, not actual fighting-game balance. No credits-to-win-probability model is fitted or assumed. Outcome sequences are hypothetical inputs.

`verified_source/` includes all 13 canonical JSON files plus EconomySeed.cs, verified against current Git blobs. It is not a complete game repository. The source manifest explains that matching attachment bytes were reused because direct container network downloads were unavailable. The two fighter byte sequences were reconstructed with the inspected cancel additions and then verified in full against current Git blob hashes.

`calculations/economy_results.json` contains opening wallets, streak examples, payout variants, score-only probabilities, and a bounded exhaustive equal-spend state enumeration with witnesses. Those states are not played matches.

`calculations/purchase_move_matrix.json` contains every rental and the actual replaced base action where applicable. `super_cost_payload_screen.json` contains authored all-hit damage sums, not tested connection/damage. `affordability_frontiers.json` contains possible rental plans and retained combat budgets with zero reserve.

Reproduce with the commands in the parent README. Native balance experiments are specified separately and remain unexecuted.
