# Delivery continuation

Updated 2026-09-05T11:39:58.076960+00:00. Frozen candidate: `source-sha256:5414744b92e51de04d4be67df1929929a1072e7d5b986d773b1459166217784d`. Content: `6fd0fe07da1f14d72754869b12cfe6cf21721d5055572c5b9f5b43dd72604c8b`.

The completed native game uses AFTER HOURS bitmap fighters, stages, effects and interface. Explicit atlas mappings cover all 98 canonical actions and universal poses across 159 selected fighter cells. Matte removal, palette alternatives and foot pivots remain presentation-only. The original ZIP and design boards are preserved. Launch `Play Strike Ledger.cmd`, or extract a platform ZIP from `dist/` and keep its executable, PCK and runtime directory together.

Current verification is **software 76/76, target device 3/5, human 0/1**. The unmodified software gate passed on the final evidence proposal. The all-tier gate verifies 79/82 records and remains FAIL solely for DEVICE-003, DEVICE-004 and HUMAN-001, which are NOT_RUN.

Gate evidence: `reports/evidence/after-hours-rebind/run-20260905T113513-945140Z/result.json`. This records an actual evidence-completeness/integrity gate execution, not a subjective play-quality judgment.

AFTER HOURS verification completed: 75 native UI checks; 415 art bindings with 38 PNG captures; 23 gameplay-boundary checks; direct visual review of 49 PNGs; 13 legal-input showcase checks with 14 PNGs; and 51 compatibility/native-evidence checks. Windows and Linux full-match movies completed, and freshly extracted platform packages passed actual native launches. Their 6-round, 12,789-tick competitive replays agree byte-for-byte: `371c2be65f805c377938b6eb74b551ceb2e3cc97a632b41f8d52fe47e9baac46`.

The pre-AFTER-HOURS baseline remains preserved in `reports/evidence/after-hours-baseline/`. Core/App source, both platform gameplay DLL pairs and all 13 canonical JSON files are unchanged. Earlier gameplay conformance, balance, training and network evidence is inherited only through this exact identity, retaining its original commands and dates. Old presentation movies remain historical; current graphics have their own native evidence.

The deterministic Core/App retain 12 leases, 16 training drills, one persistent credit wallet, replay, bots, UDP rollback and complete match/result/rematch flows. Retained gameplay evidence includes 36 Core scenarios, 10 App suites, all 16 drill successes/timeouts, 252 matched balance runs and four UDP fault profiles. Current source/native inventories are in `reports/RELEASE_CANDIDATE.json`.

Build with `python tools/build.py --export --test` using Godot.NET 4.6.3 and .NET SDK 8.0.424. The sibling cache supplied toolchain binaries only. Self-contained verification executables are under `dist/verification/`; `python tools/run_scenario.py --list` lists 29 routes. Run `python tools/release_gate.py --tier software` or `--tier all` to inspect the acceptance ledger.

Only these external checks remain:

- **DEVICE-003:** Two actual local controllers and a physical keyboard-plus-controller session have not been observed. Software-injected device routing does not satisfy this check.
- **DEVICE-004:** No two-physical-machine LAN match has been observed. Actual UDP processes on this PC do not establish that result.
- **HUMAN-001:** No owner/player acceptance of fighting feel or wallet decisions has been supplied. Agent review and bot runs do not replace human feedback.
