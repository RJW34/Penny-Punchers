# Balance notes — recorded candidate experiments

The final candidate completed 252 controlled 1v1 matches, 1,620 rounds and 3,387,240 simulation ticks on 5 September 2026. All runs completed; none were excluded. These results provide reproducible evidence about the implemented economy and these particular bot policies. Human competitive balance and feel have not been established.

## Experiment design

Seven policies each played the same nine frozen contexts with both opening-budget assignments and both mirrored seats: 7 × 9 × 2 × 2 = 252 matches. Each policy therefore received 36 matched cases. The contexts cover both fighters and mirror matchups, all nine selected-art pairings across contexts, opening credits of 0/300/600/900/1800/3600, neutral and corner positions, health leads, tier-2 recovery, the wallet cap and a decisive final round. This is a bounded stratified design, not every combination of these factors.

The common combat seed starts at 1. Bots act through the production input recognizer, use a minimum 12-tick observation delay and do not read the opponent's input. The separate laboratory initializes explicit trusted fixtures before play and saves their canonical snapshots. Running policies cannot mutate the competitive simulation's wallet or health. All 126 mirrored pairs agreed on policy-relative outcome and simulation duration, providing evidence of symmetry under the tested inputs.

## Observed policy outcomes

Each row contains 36 matches against the common baseline; no match was a draw. The interval column is a nominal 95% Wilson interval calculated from the raw win count. Mirrored seats and common seeded contexts are correlated, so these intervals are descriptive summaries, not calibrated confidence intervals for independent players or evidence of statistically significant policy differences.

| Policy | Wins–losses | Win share | Nominal Wilson interval | Improved / worse / same versus confirm in matched cases |
|---|---:|---:|---:|---:|
| Spend on confirm | 22–14 | 61.1% | 44.9–75.2% | 0 / 0 / 36 |
| Conservative reserve | 14–22 | 38.9% | 24.8–55.1% | 6 / 14 / 16 |
| Force available EX | 10–26 | 27.8% | 15.8–44.0% | 6 / 18 / 12 |
| Bank for selected super | 10–26 | 27.8% | 15.8–44.0% | 2 / 14 / 20 |
| Lease focused | 18–18 | 50.0% | 34.5–65.5% | 6 / 10 / 20 |
| Zero-spend defense | 10–26 | 27.8% | 15.8–44.0% | 2 / 14 / 20 |
| Controlled losing | 10–26 | 27.8% | 15.8–44.0% | 4 / 16 / 16 |

Spending on confirmed contact performed best in this bounded sample. Forcing EX or banking for a super did not reliably improve outcomes, and leasing helped some contexts while worsening others. This does not identify a universally best strategy: the single seed, fixed context selection and specific input policies limit generalization. The paired comparison preserves the same fighter, art, starting state, budget assignment and seat for each policy comparison.

## Zero credits, recovery and the cap

Across 84 player-rounds starting at zero credits, fighters started 2,268 actions, landed 838 hits, blocked 220 attacks and parried 38. They had zero accepted paid startups and spent zero combat credits. The free kit remained executable and capable of both offense and defense at zero. These counts do not establish equal matchup strength or measure a human player's sense of agency.

The experiments reached recovery tier 2, recorded 215,400 credits clipped at the 3,600-credit wallet cap and started leased moves 1,678 times. Cap clipping is the sum across all observed payouts, not a typical per-match amount or an income exploit. Opening balances, purchase costs, combat debits, nominal/granted/clipped payouts and closing balances are retained per round.

The controlled-losing policy deliberately yielded its early rounds to exercise recovery incentives. It won 10 of 36 matches and improved on spend-on-confirm in four matched cases, worsened in sixteen and tied the outcome in sixteen. This sample does not show deliberate losing to be generally advantageous, but it also does not exclude a more effective farming strategy. Recovery grants credits while conceding score, and the finite match and wallet cap constrain retained income. Wider seeds, adversarial policies and human play are still needed before treating the recovery schedule as competitively settled.

## Authored change and remaining limits

The only intentional combat-table tuning during implementation was [ADR 0003](../decisions/0003-contacted-ex-super-cancel.md): Rook's `knee_ex` and Vale's `palm_ex` gained a contacted selected-super cancel window at action frames `[8,13)`. Their previous cancel arrays were empty, making the required EX-to-super mechanic impossible. The EX retains its 300-credit startup debit and the selected art pays its separate startup price. Whiffs, parries, insufficient credits, reserve restrictions and invalid actor/art states do not grant that route. Prices, damage, move frame timings, other EX cancel tables and credit generation were not tuned from these experiments.

This change makes two paid confirm routes available and can increase their value when sufficient credits remain. It is documented as a contract-enabling design decision, not as a statistically proven balance improvement. No additional price or combat-data changes were inferred from the 252-match results.

Telemetry counts paid attempts that reach the core's accepted/rejected transition checks; inputs entered while unactionable are outside that count. Melee whiffs and interruptions are distinguished, while projectile startups and contacts are separate. Positive observed stun changes can undercount a dizzy reset occurring on the same tick. Human reaction, readability, matchup knowledge, physical controller feel and longer-term strategy are outside this experiment.

## Reproduction and evidence

Build `src/StrikeLedger.BalanceLab/StrikeLedger.BalanceLab.csproj` with configuration `ExportRelease`, then run its `bin/ExportRelease/net8.0/StrikeLedger.BalanceLab.dll` with `--data data --output reports/balance-lab-exportrelease-final --seed 1 --seeds 1` from the source repository root. Run `python src/StrikeLedger.BalanceLab/analyze.py reports/balance-lab-exportrelease-final` to derive the comparison tables and artifact checksums.

The final [summary](../reports/balance-lab-exportrelease-final/summary.json), [analysis](../reports/balance-lab-exportrelease-final/analysis.json), [match records](../reports/balance-lab-exportrelease-final/matches.jsonl), [round records](../reports/balance-lab-exportrelease-final/rounds.jsonl), [starting snapshots](../reports/balance-lab-exportrelease-final/frozen-states/) and [artifact checksums](../reports/balance-lab-exportrelease-final/artifact-sha256.json) belong to the source repository's evidence bundle. They are not implied to be included in a compact player-only download. Historical experiment folders are marked superseded.

- Build: `strike-ledger-native-1/c75fa1b44d9da6f03682753fb9fde812a944c34be05051697a1954b4813babbc`
- Canonical content SHA-256: `ec249786c06cb4e5cc4a0ea2dd84fe8c818d8360a11977d85f1b9aa66c293e63`
- The actual Windows/Linux App and Core DLLs match both final laboratory binaries byte for byte; see the [assembly binding](../reports/app-exportrelease-final/shipped-assembly-binding.json).
