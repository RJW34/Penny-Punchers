# Measured feel calibration

The final production Core passed 21 measured movement/input/freeze checks. Reproduce with `python tools/measure_core_feel.py`; read reports/evidence/feel-measurements/feel-measurements.json for every before/after state, actual input, event and canonical snapshot.

Rook walks forward/backward 3000/2100 authored integer units per tick; Vale 2700/2200. Both stop without extra displacement on neutral input. Rook forward dash measures96000 units over16 ticks; Vale91998 over19. Backdash measures85000 over20. Both jumps launch after4 ticks, remain airborne30 ticks, and require3 landing recovery calls. 1000 authored units equal one logical rendering unit.

A completed QCF starts its action on that same sampled simulation tick. Measured light/medium hitstop is8 calls, heavy11. High parry holds the attacker14 and defender8. All six selected supers freeze20 calls including startup; measured startup debits are900/1200/1500 from credits earned through an actual confirmed round payout. These figures measure simulation timing, not hardware input-to-photon latency.

The full competitive free-kit native match ran18466 input ticks across8 rounds with no leases, no debit receipts and no wallet decreases. Recorded presentation showcases cover directional and repeated parries, throws/techs, contacts, paid startups, and an explicitly labeled rollback correction fixture. Corrected defects include movement poses, replay-art HUD selection, training dummy/reset labels, menu-footer overlap and audio shutdown cleanup.

No actual player has accepted the fighting feel. Human feedback remains pending; bot experiments and timing measurements cannot establish expert competitive balance. Consult BALANCE_NOTES.md for matched-run uncertainty and policy differences.
