# Contacted EX to selected super

Date: 2026-09-05. Authorized by the integrating task after a real-core conformance test identified a conflict.

The contract explicitly requires a legal EX startup followed by a legal selected-super cancel, with both startup prices retained. Every initial EX definition contained `"cancel_rules": []`, so the production core correctly rejected every such transition. No executable case could satisfy that requirement.

The canonical `rook/knee_ex` and `vale/palm_ex` now each have one cancel rule: action frames `[8,13)`, `requires_contact: true`, targets `selected_super`. Both moves already have startup 8 and active duration 5. The old value for each was the empty cancel array. All other EX cancel tables are unchanged. A successful hit or block opens the authored active window; a parry, whiff, insufficient wallet, reserve restriction, airborne/disabled actor or unselected art still cannot trigger the transition. EX spends 300 at its own committed startup; the selected art spends its independent price on the later committed startup.

These grounded options make the required mechanic available to both fighters and introduce a paid confirm route. This increases the value of these two EX moves when a player retains enough credits for their selected art. It does not grant a global cancel system or change prices, damage, frame timings, defensive rules or credit generation. Balance and human feel remain separate gates.

The derived move-table CSV columns do not contain cancel rules and require no change. No oracle arithmetic fixtures change. The real-production `ex_cancel_super_costs` scenario exercises the legal two-debit transition, while `cancel_contact_not_whiff`, `reserve_two_ex_then_deny`, `leased_replacement_and_selected_super`, `negative_edge_paid_forbidden`, and rollback receipt tests retain rejection behavior. Current content hash and executable results are written by `StrikeLedger.CoreTests` into `reports/evidence/core-conformance/core-conformance.json`.
