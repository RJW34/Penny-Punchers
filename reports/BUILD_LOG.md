# Actual build and verification

Updated 2026-09-05T11:39:58.076960+00:00. Frozen candidate: `source-sha256:5414744b92e51de04d4be67df1929929a1072e7d5b986d773b1459166217784d`. Canonical content inventory: `6fd0fe07da1f14d72754869b12cfe6cf21721d5055572c5b9f5b43dd72604c8b`.

AFTER HOURS bitmap fighters, stages, effects and interface are integrated into the Godot.NET game. Both exported Core DLLs remain `204653b031ca2700371d8c1df62c4b7b9eb131825fcaaba1ef5ee01676c2c7df`; both App DLLs remain `72e7b96bbd878126db96ba0be8e49bae49df1417bf3162f4aec4d1f325e6cebb`. The metadata-only build property prevents artwork commits from changing gameplay module identity. No canonical rules or gameplay source changed.

Current verification is **software 76/76, target device 3/5, human 0/1**. The unmodified software gate passed on the final evidence proposal. The all-tier gate verifies 79/82 records and remains FAIL solely for DEVICE-003, DEVICE-004 and HUMAN-001, which are NOT_RUN.

Gate evidence: `reports/evidence/after-hours-rebind/run-20260905T113513-945140Z/result.json`. This records an actual evidence-completeness/integrity gate execution, not a subjective play-quality judgment.

AFTER HOURS verification completed: 75 native UI checks; 415 art bindings with 38 PNG captures; 23 gameplay-boundary checks; direct visual review of 49 PNGs; 13 legal-input showcase checks with 14 PNGs; and 51 compatibility/native-evidence checks. Windows and Linux full-match movies completed, and freshly extracted platform packages passed actual native launches. Their 6-round, 12,789-tick competitive replays agree byte-for-byte: `371c2be65f805c377938b6eb74b551ceb2e3cc97a632b41f8d52fe47e9baac46`.

The separate normal-pacing Windows match completed 6 rounds/12,789 ticks with exit 0 in 232.75 seconds (230.32 seconds inside the runtime). Actual frame intervals were p50 16.666 ms, p95 16.802 ms, p99 17.316 ms and worst 557.276 ms. Movie capture and Linux WSL/llvmpipe runs are not substituted for normal Windows pacing or physical second-PC evidence.

Current artifacts are under `reports/evidence/native-after-hours-ui/`, `native-after-hours-art/`, `native-after-hours-showcase/`, `native-after-hours-pacing/`, the current Windows/Linux native movie directories and `package-after-hours-verification/`. `after-hours-compatibility/result.json` binds the complete 51-check verification to this candidate.

The pre-AFTER-HOURS baseline remains preserved in `reports/evidence/after-hours-baseline/`. Core/App source, both platform gameplay DLL pairs and all 13 canonical JSON files are unchanged. Earlier gameplay conformance, balance, training and network evidence is inherited only through this exact identity, retaining its original commands and dates. Old presentation movies remain historical; current graphics have their own native evidence.

Historical execution includes 15 contract checks, 36 Core scenarios, 10 App suites with 16 drill successes and 16 timeout failures, all 98 reconstructible action traces, 252 balance runs and four actual UDP fault profiles. These retain their original evidence identities.

`python tools/build.py --export --test` uses Godot.NET 4.6.3 and .NET SDK 8.0.424; exports include the runtime. `python tools/package_release.py` creates platform ZIPs and SHA256 inventories. `python tools/verify_packages.py --label package-after-hours-verification` performs fresh extraction and native launch verification. `python tools/package_verification.py` publishes the self-contained verification executables.

Only DEVICE-003 (physical local controller sessions), DEVICE-004 (two physical PCs) and HUMAN-001 (owner/player feedback) remain NOT_RUN.
