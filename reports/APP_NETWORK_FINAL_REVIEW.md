# Final App and private-network review

The frozen exported candidate passed all ten App integration suites, all sixteen training evaluator success and timeout cases, four actual two-process UDP fault profiles and the 252-match controlled economy experiment. These results use the exact App and Core DLL bytes included in both exported platforms. Production source remained unchanged during these final runs.

## Defects and integration gaps repaired

| Area | Final behavior and exercised evidence |
|---|---|
| Late input and terminal rollback | Resimulation restores the complete core snapshot, including wallets and startup receipts. A corrected earlier KO truncates obsolete future snapshots and events. A late parry can retract a predicted paid-super KO without duplicating a debit or applying a payout. The rollback and late-parry App suites exercise these cases. |
| Economic settlement | Settlement requires confirmed terminal input and matching peer terminal hashes. A separate confirmed-input replay must also reach the same terminal state. One idempotent settlement is then recorded. Per-round immutable receipt records prevent fast phase transitions from disappearing from integration logs. |
| Presentation during rollback | Only confirmed combat events are released, once. Invalidated speculative events are removed rather than leaving a false KO or repeating a startup-debit presentation event. |
| Preparation and round start | Protocol, build, content, configuration, session and seat are checked before private play. Plans use nonce-backed SHA-256 commit/reveal and atomic core validation. Matching starts preserve the deterministic 180-tick reveal and 120-tick countdown. |
| Healthy paused connections | Reliable pause/resume retains one-second keepalives during paused and preparation states. A real UDP integration test pauses for sixteen seconds, exceeding the fifteen-second disconnect threshold, then resumes successfully. |
| Replay integrity | Complete normalized input history and preparation/settlement/next-round transactions are recorded with wallet checks, hashes and exact startup receipts. Seeking restores complete checkpoints and resimulates. Training and assisted configurations are rejected; the hidden `Config.Assist`/header mismatch path is closed. Import is bounded by size, command count, JSON depth and schema. |
| Training reset and checkpoints | Reset restores the dummy's initial RNG, queued commands and relative clock, clears recording/playback flags and restarts dummy playback. Checkpoints restore the dummy state alongside the core state. A stalled repeated bot query returns the same input without advancing its RNG. |
| Training evaluator correctness | Motion drills recognize compatible free motions for both fighters; the charge drill requires Vale. Defensive throw techs are recognized when player 0 is the event target. Actual parry type, quick-rise and recovery-buffered reversal events drive their milestones. All sixteen evaluators have real success traces and neutral-input timeout failures. |
| Malformed and disconnected peers | Forged-seat controls, incompatible handshakes, oversized datagrams and bounded protocol violations are rejected. Aborted sessions cannot be revived by later handshake traffic. Disconnects during preparation or paid startup do not apply speculative round income. |

The core also repaired paid-chord release fallback and narrowed reversal event emission to genuine recovery buffering; those core conformance details are maintained by the separate Core review. The only intentional combat-table tuning was the contacted EX-to-selected-super route in [ADR 0003](../decisions/0003-contacted-ex-super-cancel.md), described in [Balance notes](BALANCE_NOTES.md).

## Actual socket results

Each row launched two independent processes using IPv4 UDP loopback. Both peers completed a full match with exit code 0. The orchestrator compared final canonical hashes, wallets, scores and every round's receipts. Each peer saved its confirmed-input replay and replayed it to the exact final live state; peer replay and economy files were byte-identical within each profile.

| RTT | Jitter | Loss | Duplicates | Reordering | Result |
|---:|---:|---:|---:|---:|---|
| 0 ms | 0 ms | 0% | 0% | 0% | Both processes passed |
| 50 ms | 0 ms | 0% | 0% | 0% | Both processes passed |
| 100 ms | 20 ms | 1% | 2% | 5% | Both processes passed |
| 150 ms | 20 ms | 3% | 3% | 10% | Both processes passed |

This is the declared bounded four-profile matrix, not the full Cartesian product. The laboratory advances faster than the rendered 60 Hz game; its stall count measures blocked advance attempts and is not a rendered-frame smoothness metric. The final 150 ms profile completed seven rounds and 14,708 ticks, ending with wallets 1,200/1,200 and score 5–2. Its matching final state hash is `b6213df1747b9d20b4b6f23c8609b0cd2a486b2bdcddfc99be19cd0e7aac476f`.

## Final artifact map

| Evidence | Final path and scope |
|---|---|
| App assertions | [app-exportrelease-final/self-tests.json](app-exportrelease-final/self-tests.json): 10/10 passed suites, exact command, platform, timestamp, build and content identity. |
| Training evaluator traces | [app-exportrelease-final/drills/results.json](app-exportrelease-final/drills/results.json): 16 success checks and 16 timeout failures. Each drill also has `*-success.json`, `*-timeout.json` and `*-fixtures.json`. Fixtures explicitly capture canonical training positions, health, dummy action and checkpoint states with recording indices. These are executable evaluator traces, not human controller sessions. |
| Full local replay | [app-exportrelease-final/full-match.slreplay](app-exportrelease-final/full-match.slreplay), [bot-match-result.json](app-exportrelease-final/bot-match-result.json), [economy.json](app-exportrelease-final/economy.json) and [economy.csv](app-exportrelease-final/economy.csv): completed bot match, validated playback and seek. |
| Late paid-super correction | [app-exportrelease-final/late-parry.json](app-exportrelease-final/late-parry.json): canonical correction and wallet/receipt assertions. |
| Actual socket matrix | [network-lab-exportrelease/matrix-result.json](network-lab-exportrelease/matrix-result.json): four profiles, eight processes, commands, exit codes and artifact SHA-256 values. Each profile retains both peers' JSONL logs, result JSON, final binary snapshot, `.slreplay`, economic JSON/CSV and stdout. |
| Controlled balance evidence | [balance-lab-exportrelease-final/summary.json](balance-lab-exportrelease-final/summary.json) and [ANALYSIS.md](balance-lab-exportrelease-final/ANALYSIS.md): 252 completed matches, 1,620 rounds, 126/126 mirrored outcome/duration agreements, zero aborted/excluded runs. Raw matches, rounds, starting snapshots and checksums are retained alongside them. |
| Shipped assembly binding | [app-exportrelease-final/shipped-assembly-binding.json](app-exportrelease-final/shipped-assembly-binding.json): SHA-256 comparison of actual Windows/Linux exported App/Core DLLs and both executable labs. |

Older App, network and balance folders contain `STATUS.md` identifying them as historical development evidence. Their previous passes do not substitute for these final build-bound results.

## Exact shipped identity

Configuration is `ExportRelease`. The module IDs below were read from the final laboratory DLLs using `Assembly.ManifestModule.ModuleVersionId`. Their SHA-256 values equal the corresponding actual exported DLLs on both platforms.

| Assembly | MVID | SHA-256 |
|---|---|---|
| `StrikeLedger.App.dll` | `ff842841-04a6-4441-942b-9e916edeccca` | `72e7b96bbd878126db96ba0be8e49bae49df1417bf3162f4aec4d1f325e6cebb` |
| `StrikeLedger.Core.dll` | `03804be8-8358-4d7e-9ea4-ac2504a00609` | `204653b031ca2700371d8c1df62c4b7b9eb131825fcaaba1ef5ee01676c2c7df` |

Verified export payload directories are `dist/StrikeLedger/windows/data_StrikeLedger_windows_x86_64/` and `dist/StrikeLedger/linux/data_StrikeLedger_linuxbsd_x86_64/`. The comparison uses their shipped files, not an intermediate publish directory. The final lab dependencies are under `src/StrikeLedger.NetworkLab/bin/ExportRelease/net8.0/` and `src/StrikeLedger.BalanceLab/bin/ExportRelease/net8.0/`.

- Replay/private-session build identity: `strike-ledger-native-1/c75fa1b44d9da6f03682753fb9fde812a944c34be05051697a1954b4813babbc`
- Canonical content SHA-256: `ec249786c06cb4e5cc4a0ea2dd84fe8c818d8360a11977d85f1b9aa66c293e63`

The build suffix binds the App and Core module identities. Exact DLL equality establishes that these final executable tests exercised the same application/combat code bytes included in both native exports; it does not establish that every platform's hardware and presentation path has been exercised.

## Reproduction and acceptance limits

Build `src/StrikeLedger.NetworkLab/StrikeLedger.NetworkLab.csproj -c ExportRelease`, then run its `bin/ExportRelease/net8.0/StrikeLedger.NetworkLab.dll --self-test --data data --output reports/app-exportrelease-final` from the repository root. Run `python tools/run_network_lab.py --configuration ExportRelease --matrix --seed 1 --output reports/network-lab-exportrelease` for actual two-process profiles. The tool accepts `--dotnet <path>` and `--no-build` for an already-built private SDK setup. Exact historical commands and timestamps are retained in the result JSON.

Loopback transport, scripted input and shared assembly identity are the scope of this review. Two physical machines, target-platform hardware/controller behavior, human perception of rollback and competitive feel remain separate acceptance gates. Native rendered-game evidence is maintained in the release review by the shell owner. This report makes no public matchmaking, router/NAT traversal, spectator, account-service or anti-cheat claim.
