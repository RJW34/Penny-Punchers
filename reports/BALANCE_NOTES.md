The shop-only v2 candidate has completed a bounded, replay-verified pilot. These results describe the tested policies and seeds; they do not establish human competitive balance, final prices or a preferred economy.

A new match starts at 600 CR. The bank cap is 3600 and the shop limit is 2400. Up to three rentals, two repeatable EX licenses and one single-use super permit share that budget. EX licenses cost 600, or 900 for a vertical reversal; arts cost 900/1200/1500. The ordinary kit stays free. Combat never spends the bank: buying an EX grants repeated legal use, while a purchased art supplies one committed startup for that round.

Wins pay 1200, draws 900 and losses 1200/1200/1500 at the previous recovery tier. Eligible counter-hit, grounded anti-air and precision-parry receipts earn 50/75/100 for the next shop, at most twice per category and 300 total per round. Confirmed settlement applies the outcome grant first, then skill credits, with separate earned/granted/clipped amounts. Pending rewards cannot fund combat or the current round's purchases.

The completed current evidence consists of:

- **800 product rounds:** all 38 expanded products and 12 two-EX pairs, each compared with retaining its actual fee. Two policies and seeds 1/2 run both seats at equal opening bank 2400. Every product has actual starts; both licenses start in 94/96 purchased pair rounds. This uses a fixed Thomas opponent and does not exhaust mixed-cart, stage or matchup combinations.
- **48 fresh full matches:** four isolated payout/reward controls, three policies, two seeds and both seats. The reward-seeking policy reacts to public observations delayed by 12 ticks.
- **64 full continuations:** explicit round-two score 1-0 fixtures compare earned-gap and equal-bank states, holding assumed prior spending at 0 or 600. Skill-enabled fixtures explicitly assume prior receipts of 300/100; they are not represented as a played first round.
- **Reconstruction:** all 912 traces reproduce their recorded events and state. No run was excluded. Across 1,578 observed rounds, 3,036,953 fight steps preserve both banks, and all 3,156 settlement balance equations agree. The analysis excludes 112 terminal results from continuing-match liquidity.

The controls are isolated laboratory data copies, not selectable shipping economy modes:

| Control | Loss payouts | Skill rewards |
|---|---|---|
| A | 900/1200/1500 | Disabled |
| B | 900/1200/1500 | Capped |
| C — shipping v2 | 1200/1200/1500 | Capped |
| D | 1200/1200/1500 | Disabled |

Each control records exact file hashes; C is byte-identical to the shipping expanded data. These labels belong to the v2 experiment and supersede older experiment labels. Product comparisons all ran shipping C. A preserved harness mislabeled the 608 single-product rows as A in one reporting field. Raw samples and replay bytes remain untouched; the analysis publishes an explicit, hashed projection changing only that label. Future source output is corrected. No gameplay rerun is claimed for the projection.

The observed match effect of moving from B to C was +0.25 for pressure, -0.50 for reward-seeking and 0 for spacing, on a win=1/draw=0.5/loss=0 outcome scale. With only two selected seed blocks, every full-match contrast has a conditional interval spanning -1 to +1. Seats are averaged within seed blocks, not counted as independent players. The differing outcomes are evidence of policy dependence, not statistical support for better balance. Crossover policies consumed zero super uses, so these matches do not measure art-frequency effects; separate product and conformance runs exercise the arts.

The analytical score-only reference is **163/256 (63.671875%)** for a 1-0 leader in independent, fair, decisive future rounds of first-to-five. It is not a measured bot outcome or a target conversion rate. Score-preserving interventions are reported separately from that reference. Continuing states at 0-1 through 0-4 were observed, but only 1-0 receives paired economic interventions. Wallet recovery cannot restore conceded score.

Combat conformance separately covers all 100 core and 115 expanded action nodes, with real effects for branches, counters and objects rather than startup-only claims. The expanded suite passes 75 scenarios; the core suite passes 65 plus a new repeated-bait scenario. Both seats demonstrate capped CH/AA/precision baiting, a partial final award, fixed combat bank and every-tick restore/resimulation. This bounds the implemented reward ledger; it does not exclude an improved adversarial human strategy.

The 80 low-health, zero-bank EX corner-response cells retain failures: 64 defenders survive and 14/16 scripted parry attempts succeed. The Rook Pulse EX predictor misses its close spawn at both facings; this is not evidence that the move is impossible to parry. These predictive scripts are not human reaction measurements. Adaptive mixed pressure, all-art corner sequences, silhouette calibration, deterrence, long-term strategy and final price approval remain playtest work. Frequent activation alone is never treated as tactical value.


A separate strategy supplement completes **32 decisive-round match continuations per catalog (64 total)** from explicit round-nine, score-4-4 fixtures. It compares unused-super threat, draw-seeking, chip-out and final-round cash-out with matched save/free-policy controls, using seeds 1/2 and both seats. Every trace reconstructs. The unused permit remains unused; the cash-out policy consumes exactly one use. In these cells, draw-seeking loses at timeout, unused-permit and cash-out policies do not improve match outcomes, and both paid EX and free-projectile guard controls achieve chip KO (33 versus 105 ticks). The guard target starts at a declared nine HP. This does not establish an adapted human response, optimal play or eight previously played rounds. [Strategy process records](evidence/shop-v2-strategy-process-results.json) link both current catalog reports and the exact preserved harness.

Current evidence and reproduction:

- [Pilot analysis](evidence/shop-v2-pilot-analysis.json), [product summary](evidence/shop-v2-product-pilot-current/summary.json), [crossover summary](evidence/shop-v2-crossover-pilot-current/summary.json), and [actual process results](evidence/shop-v2-pilot-process-results.json).
- [Effect inventory](evidence/shop-v2-action-effects.json) and [Core/PP/SO crosswalk](SHOP_V2_CORE_FINDINGS.json).
- Core content: `0245f6813c2320a9081fd96fefb4fd600632c92ae8f2be8a56c049129f365734`.
- Expanded content: `e9c73b68e2cc1a128ea6215eb35fcce75036a66deca708dd59d7699efc8ae1f8`.
- Executed Core/App build: `pp-shop-only-v2/3aaa9b7c6de09ecb685e805b0edd641b35d36972f2a74f4a45700dcf9d0360f0`.

```text
dotnet run --project src/StrikeLedger.BalanceLab/StrikeLedger.BalanceLab.csproj -c ExportRelease -- --scenario shop_only_pilot --scope all --seed 1 --seeds 2 --data data/rulesets/buyables_full --evidence-dir reports/evidence/my-v2-pilot
```

Use a new output folder. `--scope smoke --seeds 1` is a short harness check, not a balance matrix. The preserved exact executed harness and commands are identified in the process report. `python tools/summarize_shop_v2_pilot.py` reproduces the current paired analysis. Evidence bundles and trace files are not implied to be included in a compact player download. Native recordings, physical devices and owner/friend feedback remain separate evidence.

[Historical pre-v2 appendix](history/BALANCE_NOTES_PRE_SHOP_V2.md) preserves the prior direct-spend experiment, original numbers and exact old identities. Its activation prices, protected reserves, loss schedule and commands apply only to that historical build. Neither its 252 matches nor the later pre-v2 492-sample checkpoint certify shop-only v2. Older v2 metadata runs also retain their original hashes and are clearly marked historical.
