# Independent implementation audit — all 76 software requirements

Audit date: 2026-09-05. Reviewer: combat/core agent, after freezing the production core and running its Release conformance executable. This is an implementation and evidence review, not the integrator's final acceptance ledger. UI, App and packaging work was still being completed during this review.

## Verified production core

`reports/evidence/core-conformance/core-conformance.json` records 36 passing production scenarios, the actual core assembly identifier, current content hash, seed, individual action effects and a real-host performance sample. All 98 commands are recognized through digital input. All 98 actions produce their declared hit, projectile, throw or feint displacement. Coverage includes the 12 leases, six selected supers, exact-credit startup, denied paid-input release suppression, contacted EX-to-super dual charging, 2-tick red parry boundaries, all five groups of Rook's super parried separately, actual air/projectile parries, throw tech/kara/quick rise/reversal, complete zero-credit free-kit round, hand-derived payout sequences, nine-round match draw, snapshot restoration and seat-mirrored execution.

The audit found and repaired production defects before freezing: a grounded leap's authored landing erased the move before it became active; the returning projectile ignored its turn event; a rejected paid chord could fall back to an ordinary special when its buttons were released; combat/input constants needed to be read from canonical JSON; throw stun needed the same authored threshold check. Two canonical EX cancel rules were added with integrator authorization, documented in `decisions/0003-contacted-ex-super-cancel.md`. Other authoring and price values remain unchanged.

## Concrete integration findings sent to owners

1. **Training reset/checkpoint determinism:** App initially restored the core without restoring the dummy's RNG, observation queue and queued commands. The App owner reports this repaired and adds repeated 90-tick input/hash tests. Verify the final App evidence uses the frozen assembly before closing TRAIN-002.
2. **Training drill compatibility:** motion training initially counted only Rook. App now accepts ordinary motions from either fighter. The charge drill requires Vale; the UI must choose the compatible fighter and refresh its selected-fighter display.
3. **Training tools:** the reviewed menu exposed credits, reset, frame step, dummy, boxes and checkpoints, but still lacked controller-accessible health/stun/position and slow-motion controls. The diagnostics lacked combo damage and computed frame advantage. These are real UI work, not an external hardware limitation.
4. **Presentation movement state:** core walking and dashes integrate X directly, so `PlayerState.Vx` is not a reliable walking/backdash indicator. The reviewed renderer selected idle during walks and forward-dash pose for backdashes. Derive presentation motion from successive X positions or authoritative directional history.
5. **Reserve selection:** reviewed local/network menus used modulo 1800, limiting the selectable floor to 1500 despite legal higher saved balances. Cycle through the actual affordable range and retain the same-wallet semantics.
6. **Replay assist identity:** the reviewed recorder accepted `Config.Assist=true` while writing `Header.Assist=false`; validation only checked the header. App must reject assisted competitive recording/import or faithfully label the configuration. Network configuration already rejects assist/training.
7. **Paused network heartbeat:** a healthy paused peer initially sent no traffic and would reach the 15-second disconnected timeout. Keep a bounded heartbeat while paused and verify no simulation or economic advance.
8. **Evidence hygiene:** the initial `reports/pack_checks/environment_discovery.log` describes the scaffold author's Linux packaging environment, not this Windows PC. `reports/BALANCE_NOTES.md` and the initially empty `reports/ACCEPTANCE_RESULTS.json` need replacement with final actual-run results. Historical reports must not be presented as current-candidate verification.

## Requirement-by-requirement review

“Core tested” means the named behavior is exercised by the production conformance executable. “Source reviewed” means concrete implementation was read, with final integrated evidence owned by the integrator. “Finalize evidence” does not grant a pass.

| ID | Review and concrete evidence/action |
|---|---|
| SCOPE-001 | Source reviewed: AGENTS, product contract, isolated extracted project; no earlier repository source imported. |
| ENV-001 | Core actually built with the located .NET SDK; integrator owns matching Godot import/export logs. |
| ENV-002 | `tools/build.py` runs content sync, build/import, production tests and both exports. Final clean-run log required. |
| CORE-001 | Core tested: fixed integer Step with explicit input tick validation; no Godot/OS clock/device dependency. |
| CORE-002 | Core tested: full canonical snapshots include buffers, charges, freeze, receipts, leases, floors and pending transaction state. |
| CORE-003 | Source reviewed: bounded roster, box/frame/action/item/reference/config validation; malformed import evidence can be broadened. |
| CORE-004 | Core tested: independently repeated seeded traces, snapshot round trips, 1200-tick mirrored seat/fighter comparisons. |
| CORE-005 | Source reviewed: shell advances core in `_PhysicsProcess`, render state reads snapshots. Final actual pacing trace required. |
| INPUT-001 | Source reviewed: per-device InputRouter, axis SOCD neutralization, six-button masks and binding quarantine. Physical device gate remains separate. |
| INPUT-002 | Core tested: QCF/QCB/DP/double QCF recognition, mirrored commands, missing diagonal and stale-step rejection. |
| INPUT-003 | Core tested: 44/45-tick charge boundary, vertical-charge side-switch retention, horizontal-charge invalidation. |
| INPUT-004 | Core tested: all 98 command syntaxes, six-button/chord priority, negative edge and paid-release rejection. |
| MOVE-001 | Core tested: walk speed, immediate stop, discrete dash, wall clamp. Presentation velocity finding sent to renderer owner. |
| MOVE-002 | Core tested: jump startup/landing, fixed airborne horizontal launch, no air steering, facing-epoch cross-up behavior. |
| MOVE-003 | Core tested: pushbox separation, stage bounds, mirrored seat simulation; camera has no core mutation path. |
| COMBAT-001 | Core tested: authored half-open startup/active/recovery and actual active contacts for every move. |
| COMBAT-002 | Core tested: normal/proximity/command recognition, high/low guard, chip, hitstop and blockstun. |
| COMBAT-003 | Core tested: target combo uses named `s_mp`, contact cancel succeeds, whiff cancel fails, reversal remains distinct from links. |
| COMBAT-004 | Core tested: forward/back/command throw effects, normal tech, jump-start immunity, whiff duration and normal-only kara. |
| COMBAT-005 | Core tested: one spawn, owner cap, swept projectile contact, rank clash, EX multi-contact cooldown and returning pulse turn. |
| COMBAT-006 | Core tested: reciprocal rank winner, equal-rank trade, double KO; timeout uses integer normalized-health comparison. |
| COMBAT-007 | Core tested: combo damage scaling, zero normal chip, special chip, actual dizzy threshold; juggle budget is bounded state. |
| COMBAT-008 | Core tested: soft/hard knockdown, quick-rise input, final-two-tick reversal, corner pushback and tagged kara. |
| PARRY-001 | Core tested: actual high/low contact, wrong-level failure, one-hit-group consumption and held-forward multihit failure. |
| PARRY-002 | Core tested: actual air parry and 2-tick red-parry boundary, with blockstun preserved on arm alone. |
| PARRY-003 | Core tested: five separately armed super parries, zero health loss, full attacker debit retained. |
| PARRY-004 | Core tested: projectile parry freezes surviving projectile rather than distant owner; superfreeze and buffers serialize. |
| FEEL-001 | Complete zero-credit round runs in core. Integrator's observed native free-kit play and human feel notes remain distinct evidence. |
| ECO-001 | Core tested/source reviewed: one scalar wallet, no combat payout path or second spendable meter. |
| ECO-002 | Core tested: exact 300 EX, both EX/super startup prices, whiff/interruption/parry retain debit. |
| ECO-003 | Core tested: illegal, 299-credit, reserve, unselected art and negative-edge paid failures; every post-denial release tick checked. |
| ECO-004 | Core tested: reserve is a locked floor on the same balance. Menu floor range finding sent to integrator. |
| ECO-005 | Core tested: restore removes disappearing debit/receipt and reproduces surviving debit exactly once. |
| ECO-006 | Core tested: actual round settlements reproduce 600 spent → 900 first-loss balance → 2100 saved second-loss balance and tiers. |
| SHOP-001 | Core tested/source reviewed: fighter/slot/affordability/duplicate validation; UI drafts remain uncharged. |
| SHOP-002 | Core tested: invalid opponent plan leaves both untouched; exact duplicate returns prior commit; round boundary expires leases/floor. |
| MATCH-001 | Source reviewed: native setup/prep/fight/results/rematch paths; integrator owns final complete interactive capture/replay. |
| MATCH-002 | Core tested: point conservation, first-to-five, nine draws produce 9–9 half points and honest draw; round reset preserves only carried state. |
| MATCH-003 | Core tested: unconfirmed terminal rejected, score/payout applied atomically once, exact repeat idempotent. |
| MATCH-004 | Source reviewed: pause skips Step, quit/rematch creates new match; paused network keepalive issue sent to App owner. |
| CONTENT-001 | Core tested: all 49 Rook commands and per-move declared effects, including 43 baseline definitions. |
| CONTENT-002 | Core tested: all 49 Vale commands and per-move declared effects, including returning projectile behavior. |
| CONTENT-003 | Core tested: all 12 lease effects and six selected arts, prices and unavailable replacement/base behavior. |
| ART-001 | Source reviewed: native presentation emits distinct fight states/boxes/effects and mirror palettes. Walk/backdash pose finding needs final visual check. |
| AUDIO-001 | Source reviewed: action/UI/contact/round cue integration and separate persisted volumes; actual sound observation owned by integrator. |
| BOT-001 | Source reviewed: bots emit InputFrames, use 12-tick observation delay and seedable captured RNG; preparation plans respect wallet/floor. |
| TRAIN-001 | Same production core and guarded training mutation API; remaining UI tools/diagnostics listed above must be integrated. |
| TRAIN-002 | Sixteen explicit drills/evaluators exist; dummy reset/checkpoint fix reported, fighter compatibility and final drill success traces need evidence. |
| REPLAY-001 | Source reviewed: input/transaction replay, periodic canonical hashes and per-step wallets; App actual full-match replay tests required on frozen build. |
| REPLAY-002 | Source reviewed: bounded import, strict identity, seek restores checkpoints; assist-identity hole sent to App owner. |
| REPLAY-003 | Source reviewed: per-round spend logs and JSON/CSV exports derived from replay events. Final network replay also must verify. |
| NET-001 | Source reviewed plus existing real two-process UDP results; final rerun after assembly freeze required. |
| NET-002 | Source reviewed: 2-tick delay, 8-tick prediction, 240 snapshots, bounded input history, hashes, actual fault-injection matrix. |
| NET-003 | Core tested and App rollback tests: spending and receipts are part of restored canonical state. |
| NET-004 | Core actual KO-to-parry correction; App emits only confirmed presentation events so false KO effects are withheld. |
| NET-005 | Source reviewed: SHA-256 commit/nonce reveal, plan validation, peer seat ownership and bounded controls; malformed socket tests exist. |
| NET-006 | Source reviewed: bounded desync dump/abort, timeout and no public configuration changes; paused heartbeat finding requires regression. |
| NET-007 | Existing matrix completes actual full two-process economic matches with matching final state/round receipts. Current-build rerun and saved replay comparison required. |
| UI-001 | Source reviewed: controller-focusable menus and independent prep routing. Integrator's native UI software assertions and physical gates are distinct. |
| UI-002 | Source reviewed: credit numerals, selected-art cost, floor, leases and reason-tagged cues; no earned meter. |
| UI-003 | Source reviewed: bindings, SOCD, chord macros, unplug policy and held-confirm suppression; per-device software tests exist. |
| UI-004 | Source reviewed: OS user-data settings, normalized bounds, atomic save, reduced flash/shake/audio only affect presentation. |
| UI-005 | Network rejects training/assist; replay assist mismatch was identified for repair. Root UI must clearly label training controls. |
| BAL-001 | Actual `reports/balance-lab/summary.json` records 84 full matches and Wilson intervals; final build/art/seat grouping must be retained. |
| BAL-002 | Balance lab contains funded/zero-wallet controlled policies; human dominance cannot be inferred from bots alone. |
| BAL-003 | Actual hand-derived payout regression plus balance lab farming/final-round policies; keep rational last-round spending distinct from a bug. |
| BAL-004 | EX cancel data change records old/new windows, rationale and regression; no undocumented tuning performed by core owner. |
| QA-001 | Core runner rejects unknown scenarios nonzero; all named core acceptance scenarios executable. Final dispatch/evidence ledger must cover remaining App/UI/export scenarios. |
| QA-002 | Core seeded/symmetry/round tests plus 84 full balance matches and repeated socket matches provide long-run coverage; no production credit underflow or projectile leak observed. |
| QA-003 | Source reviewed: bounded replay/content/network inputs and no embedded-code execution; final malformed-packet suite must use current build. |
| QA-004 | Core actual-host p50/p95/p99, allocations, snapshot size and 8/240-tick rollback timings recorded. Render pacing remains a separate measured artifact. |
| QA-005 | Core evidence records assembly/content/seed. Integrator must bind final native captures, playthrough and artifact hashes to the same final build. |
| BUILD-001 | Build script performs native Windows/Linux export; integrator owns final clean-package checks and external Linux execution status. |
| BUILD-002 | Final distribution must include controls, rules, asset/license notices, known limits and candidate manifest. |
| FINAL-001 | At review time acceptance records were initially empty. Run the real evidence gate after filling truthful candidate-bound records; this audit alone is not a gate pass. |
| FINAL-002 | Update STATE, RESUME_PACKET, balance/feel/build notes and final artifact paths after all fixes and final runs. |

## External and qualitative boundaries

Actual two-process loopback/fault tests establish socket and rollback behavior on this PC. They do not establish a second physical machine, Linux runtime determinism, two-controller/arcade-stick/leverless hardware operation, or human competitive feel. Those are separate device/human gates and do not excuse the concrete software fixes listed above.
