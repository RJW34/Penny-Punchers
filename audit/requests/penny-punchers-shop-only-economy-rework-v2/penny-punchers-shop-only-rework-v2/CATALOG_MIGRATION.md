# Existing-content purchase migration catalog

These prices are new-rule candidate seeds, not a balance certification. Every activation costs zero credits. All products expire after the round. This document maps currently known live actions; unbuilt design candidates remain separate.

| Product | Fighter | Slot | Shop price | Use policy | Existing runtime action |
|---|---|---|---:|---|---|
| Clinch entry | rook | signature | 900 | Repeat after legal recovery | shop_clinch |
| Low drive | rook | signature | 900 | Repeat after legal recovery | shop_low_drive |
| High hook | rook | technique | 600 | Repeat after legal recovery | shop_high_hook |
| Low turn | rook | technique | 600 | Repeat after legal recovery | shop_low_turn |
| Step feint | rook | gambit | 300 | Repeat after legal recovery | shop_step_feint |
| Sway feint | rook | gambit | 300 | Repeat after legal recovery | shop_sway_feint |
| Returning pulse | vale | signature | 900 | Repeat after legal recovery | shop_return_pulse |
| Low palm | vale | signature | 900 | Repeat after legal recovery | shop_low_palm |
| Heel arc | vale | technique | 600 | Repeat after legal recovery | shop_heel_arc |
| Long check | vale | technique | 600 | Repeat after legal recovery | shop_long_check |
| Step feint | vale | gambit | 300 | Repeat after legal recovery | shop_step_feint |
| Sway feint | vale | gambit | 300 | Repeat after legal recovery | shop_sway_feint |
| Pulse shot EX | rook | ex | 600 | Repeat after legal recovery | pulse_ex |
| Rising elbow EX | rook | ex | 900 | Repeat after legal recovery | rise_ex |
| Traveling knee EX | rook | ex | 600 | Repeat after legal recovery | knee_ex |
| Retreat palm EX | rook | ex | 600 | Repeat after legal recovery | sway_ex |
| Circuit Break | rook | super | 900 | One startup | super_1 |
| Forge Impact | rook | super | 1200 | One startup | super_2 |
| Rush Cascade | rook | super | 1500 | One startup | super_3 |
| Charge pulse EX | vale | ex | 600 | Repeat after legal recovery | pulse_ex |
| Rising heel EX | vale | ex | 900 | Repeat after legal recovery | rise_ex |
| Turn palm EX | vale | ex | 600 | Repeat after legal recovery | palm_ex |
| Retreat heel EX | vale | ex | 600 | Repeat after legal recovery | heel_ex |
| Rising Current | vale | super | 900 | One startup | super_1 |
| Crosswind | vale | super | 1200 | One startup | super_2 |
| Tidal Step | vale | super | 1500 | One startup | super_3 |

EX products are four per fighter and limited to two equipped families. Super products are three per fighter and limited to one selected use. Ordinary base versions remain free. Existing weak rentals retain their audit flags; migrating them is not an endorsement.

Prior 38-entry candidate library: `data/candidate_library.v2.json`. Its EX entries now specify round licenses and supers prepaid use, including the proposed Overtime/Prism. New moves are not created merely by an economy conversion.
