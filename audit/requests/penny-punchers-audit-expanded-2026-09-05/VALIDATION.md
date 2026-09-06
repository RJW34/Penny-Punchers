# Expanded audit validation

**Audit artifact, not a game release.** Source revision remains `1623ee62c4f4b6d5574bbb9909eb6fe4e32f5441`.

## Checks actually executed

| Check | Result |
|---|---|
| Pinned current head rechecked through GitHub | Same revision |
| Original archive bytes and original five-file manifest | PASS |
| Original report and regression plan preserved as prefixes | PASS |
| Original 77 JSON findings preserved unchanged | PASS |
| Added PP-078–PP-091, unique combined IDs/count | 91 total |
| All delivered JSON parses | PASS |
| Pinned source blob identities | 14/14 verified |
| Auditor Python analytical/content tests | 37 passed |
| Opening aggregate-spend pair enumeration | 9 pairs |
| Bounded equal-spend ledger-state enumeration | Completed; 1,186 distinct terminal states across ending rounds, not played matches |
| Canonical output generation using delivered scripts | PASS |
| Native C# or Godot execution | NOT RUN |
| Economic win-rate effects, controllers, human feel | NOT VERIFIED |

The initial analytical test expectation for a nominal all-hit super payload was an author arithmetic error (201 versus 202) and was corrected; initial and final logs are preserved. No game code changed.

The final ZIP is checked by CRC, extraction, every file-manifest hash, the delivered tests, and regeneration of calculation outputs in a fresh temporary directory. Its exact final result and archive SHA-256 are recorded in the companion `Penny-Punchers_Audit_Expanded_Verification.json`. The companion avoids a self-referential archive hash inside the ZIP.

Passing these artifact checks establishes reproducibility of the supplement, not fighting-game balance. The protocol's gameplay interventions and human sessions remain proposed work.
