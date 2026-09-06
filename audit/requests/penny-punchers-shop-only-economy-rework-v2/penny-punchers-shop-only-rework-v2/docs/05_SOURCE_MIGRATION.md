# Live-code migration map (read current code first)
Baseline remote ref:1623ee62c4f4b6d5574bbb9909eb6fe4e32f5441. File names below were inspected in the audit and selected key paths reread for this rework. Local work may differ. No patch assumes byte-for-byte source identity.

| Current seam | Required change |
|---|---|
| `src/StrikeLedger.Core/GameContent.cs` / move/item definitions | Explicit AccessPolicy/Base/License/FiniteUse; item price only in shop. Read registries, not 12 items/49 moves/three-slot-only assumptions. Add EX/super categories, implementations and conflicts. Reject nonzero combat debit in v2 data. |
| `SimulationState.cs` | BankCredits, immutable round ownership, SuperUsesRemaining, RoundSkillLedger, reward roots, pre-contact facts, precision-arm clocks, settlement receipts. Remove active reserve floor and per-start spend receipts. |
| `Simulation.cs::CommitPreparation` | New mixed cart, two-player validation and atomic debit; super selection part of plan, not match setup; freeze bank through fight. |
| `Simulation.Input.cs::Recognize` | Recognize all syntactically valid special intent before filtering ownership; explicit locked EX/super rejects without downgrading to a normal. Move credit cost must no longer distinguish negative-edge/chord eligibility. |
| `Simulation.Input.cs::Available/CanTransition/TryStartAction` | Gate by license/remaining super, not bank; preserve phase/input/charge/cancel/occupancy legality. On valid startup decrement only finite super use. EX infinite after legal recovery. |
| `Simulation.Combat.cs::ResolveContacts` | Build authoritative immutable PRE-CONTACT classification, then allocate rewards after accepted collision arbitration. Current projectile candidates force `CounterHit=false` and current Hit events lack full air/parry timing facts; do not claim existing logs are enough. Initially no projectile CH/AA reward. |
| `Simulation.Input.cs::UpdateParryEdge` | Track eligible-clock arm age and frozen provenance; keep normal defense timings. PP is new derived reward event, not a purchased input. |
| `Simulation.Serialization.cs` | Snapshot/version/hash every capability, finite-use, root, dedupe, reward and parry clock; mutation-safe restore. Include immutable sorted cart identity. |
| `src/StrikeLedger.App/Replay.cs` | New build/rules/content schema; purchases/capabilities/earnings/settlements instead of startup debits. Refuse legacy incompatible replay explicitly or run isolated legacy interpreter. Do not mutate original replays. |
| `RollbackSession.cs`, `PrivateMatchPeer.cs`, `UdpTransport.cs` | Extend commit/reveal payload to all purchases and round-selected art; same hash on both; verify contact receipts from input simulation, never trust client bonus messages. Confirm settlement before spendable deposits. |
| `src/StrikeLedger.App/Bots.cs`, `BalanceLab/` | Buy supported kit from prior public habits; only issue licensed EX or unspent selected super; remove canEx bank check and fixed1500 canSuper; observe bounded skill facts through legal inputs. |
| `src/StrikeLedger.App/Training.cs`, `game/Main.Training.cs`, `Main.LabState.cs` | Equip any implemented license; drill unpaid/locked vs owned EX; restore/replenish only explicit training uses; no competitive payout. Add CH/AA/PP and reward-window diagnostics. |
| `game/Main.Menus.cs`, `Main.PrepDetails.cs`, `Main.Network.cs` | Shared registry cart UI, bank budget, EX/super purchase cards, prior-round facts, explicit ready/last-valid-timeout. Replace three-index DraftItems/DraftCost magic. |
| `Main.ArtSkin.cs`, `Main.Rendering.cs` | Live equipped-kit icons, super READY/USED, next-shop-only reward popups; no 300-credit EX debit, LOW FUNDS, reserve floor or unbought super READY during fight. |
| `Main.EconomyReport.cs`, replay/results | Opening bank → purchases → frozen fight bank → result grant + skill grant → cap loss → closing bank; show individual earned receipts. |
| `schemas/`, `data/`, `tools/validate_pack.py` | New canonical versions, access/payment constraints, dynamic catalog counts, bounded packets and hostile payload tests. |
| `CoreTests`, `NetworkLab`, native GUI evidence | Replace resource-specific tests under a new ruleset while keeping old historical evidence and unrelated mechanical regressions. New gate does not waive original gameplay functionality. |

## Known cross-cutting traps
Setting all move CreditCost values to zero while leaving Available untouched silently grants every EX to everybody. Removing paid gating can also change negative-edge suppression and chord priority. Removing the HUD gauge alone changes nothing. Giving every player a funded wallet while gating on it is still the old model. Putting reward mutations into Main or ArenaView is nondeterministic and rollback-unsafe. Parsing “counter” strings or post-launch Y cannot establish requested rewards.

Existing installed Overtime: requires one shop permit, then can use only independently licensed EX, without extra credit fee. Existing Prism/field: one permit; normal field contacts/lifetime not currency; whole field inherits one reward root. Missing designed versions stay proposals. Do not rename live art3 to a nonexistent implementation.
