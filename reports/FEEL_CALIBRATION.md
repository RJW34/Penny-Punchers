# Measured feel calibration — shop-only v2

The frozen exported Core passed **24 actual movement, input, freeze and precision-parry measurements** on 2026-09-06T00:08:13.6293728Z. Reproduce with `python tools/measure_core_feel.py --evidence-dir reports/evidence/shop-v2-feel-candidate`. The standalone harness references the shipped Core DLL and does not rebuild production. Every before/after state, input, event and canonical snapshot is retained in `reports/evidence/shop-v2-feel-candidate/feel-measurements.json`; `process.log` binds both platform exports and the candidate.

| Measurement | Vincent (rook) | Thomas (vale) |
|---|---:|---:|
| Forward/backward walk, integer units per tick | 3000 / 2100 | 2700 / 2200 |
| Neutral stop, extra displacement | 0 | 0 |
| Forward dash displacement / movement ticks | 96000 / 16 | 91998 / 19 |
| Backdash displacement / movement ticks | 85000 / 20 | 85000 / 20 |
| Jump input-to-launch / airborne / landing recovery ticks | 4 / 30 / 3 | 4 / 30 / 3 |

The simulation runs at 60 ticks per second; 1000 authored integer units equal one logical rendering unit. Thomas’s forward dash preserves the measured integer rounding. A completed QCF starts on the same sampled simulation tick. Light and medium contacts hold both players for 8 advance calls; heavy contacts hold them for 11. A high parry holds the attacker for 14 and the defender for 8. The round timer advances during these player freezes while the held player clocks remain fixed.

Fresh manual parries at eligible defense ages 0 and 1 each produce 100 pending next-shop credits. Age 2 still successfully parries but earns no precision award. None changes the combat bank. Frozen-edge exclusions and root/cap arbitration have separate actual Core regression evidence; these three timing samples do not replace those tests.

All six core-catalog supers freeze for 20 calls including startup. Each fixture first wins a competitive funding round, confirms its payout, and atomically purchases the round permit for 900, 1200 or 1500 credits. Legal startup consumes its one use and changes the bank by zero. Timer, action, motion and hitstop clocks stay held through super freeze; input sampling continues, and the observed defender charge increases by 20. Full-catalog replacement arts have separate production action/effect tests; this 24-measurement report covers the default core catalog.

Candidate: `source-sha256:fc679e4ba18529de3e829e0f5b47aee05db45ffe746024f64fa7fb919324ed7f`.
Canonical packaged content digest: `0d9eec292cdfb3f23454487bef78e18b442cbf7f41b7d4b81a4832e295b63696`.
Loaded core-catalog content: `0245f6813c2320a9081fd96fefb4fd600632c92ae8f2be8a56c049129f365734`.
Core SHA-256: `e0c1812e39177786ca355e079009908f79fea330aed1e80f86dae57188253f5b`; MVID: `3ac20fff-bc62-40e0-ab5d-88a864f980f8`.

These are discrete simulation measurements. They do not measure rendered frame pacing, OS/controller input latency, display latency or human feel. Physical-device and owner/friend playtesting remain unperformed. Bot experiments do not establish expert competitive balance. The previous direct-spend report is preserved at `reports/history/FEEL_CALIBRATION_PRE_SHOP_V2.md` and is historical.

The display labels above reflect the final requested name swap. The original24 measurement artifacts preserve earlier labels/identities; the name-swap audit proves unchanged Core methods and mechanical data.
