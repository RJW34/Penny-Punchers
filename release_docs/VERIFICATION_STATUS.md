# Verification status

Updated 2026-09-05T11:39:58.076960+00:00. AFTER HOURS graphics integration and software verification are complete.

Candidate: `source-sha256:5414744b92e51de04d4be67df1929929a1072e7d5b986d773b1459166217784d`. Content: `6fd0fe07da1f14d72754869b12cfe6cf21721d5055572c5b9f5b43dd72604c8b`.

Current verification is **software 76/76, target device 3/5, human 0/1**. The unmodified software gate passed on the final evidence proposal. The all-tier gate verifies 79/82 records and remains FAIL solely for DEVICE-003, DEVICE-004 and HUMAN-001, which are NOT_RUN.

Gate evidence: `reports/evidence/after-hours-rebind/run-20260905T113513-945140Z/result.json`. This records an actual evidence-completeness/integrity gate execution, not a subjective play-quality judgment.

AFTER HOURS verification completed: 75 native UI checks; 415 art bindings with 38 PNG captures; 23 gameplay-boundary checks; direct visual review of 49 PNGs; 13 legal-input showcase checks with 14 PNGs; and 51 compatibility/native-evidence checks. Windows and Linux full-match movies completed, and freshly extracted platform packages passed actual native launches. Their 6-round, 12,789-tick competitive replays agree byte-for-byte: `371c2be65f805c377938b6eb74b551ceb2e3cc97a632b41f8d52fe47e9baac46`.

The pre-AFTER-HOURS baseline remains preserved in `reports/evidence/after-hours-baseline/`. Core/App source, both platform gameplay DLL pairs and all 13 canonical JSON files are unchanged. Earlier gameplay conformance, balance, training and network evidence is inherited only through this exact identity, retaining its original commands and dates. Old presentation movies remain historical; current graphics have their own native evidence.

- **DEVICE-003:** Two actual local controllers and a physical keyboard-plus-controller session have not been observed. Software-injected device routing does not satisfy this check.
- **DEVICE-004:** No two-physical-machine LAN match has been observed. Actual UDP processes on this PC do not establish that result.
- **HUMAN-001:** No owner/player acceptance of fighting feel or wallet decisions has been supplied. Agent review and bot runs do not replace human feedback.

The acceptance gate checks evidence completeness and file integrity. Actual commands, recordings, source reviews and replay traces remain in the source workspace `reports/` directory. No physical-controller or human-acceptance claim is made.
