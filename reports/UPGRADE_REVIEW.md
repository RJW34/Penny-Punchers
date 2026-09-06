# Penny Punchers — shop-only v2 upgrade

Candidate `source-sha256:327be61cf6003b56cbc01e0f9abfd8c9a811fa13a80b33175829c93dfc5f6989`. Canonical JSON inventory `91ec2da0d0a9473018def6630da5733289ec769b7dcae621de2b6fc701fc4de3`. Updated 2026-09-06T01:20:39.298158+00:00.

The game integrates the expanded September audit, Buyables v1 and the superseding Shop-Only Economy Rework v2. Thomas and Vincent use the supplied 32-bit presentation, with Foundry Ring, Training Grid and both Marist stages. Core offers 26 products/100 action nodes; Expanded offers 38 products/115 nodes, including Overtime and Prism Lattice. Counts include derived action branches, not extra fighters.

The final requested swap names the orange-outfit motion fighter **Vincent** (rook) and the teal-outfit charge fighter **Thomas** (vale). Their art, moves and numerical rules are unchanged. The name-swap audit verifies every production source delta, all mechanical JSON values and every Core method body/signature. Earlier executions retain their original names, assembly/content identities and unmodified replay files; the renamed candidate has fresh 66 Core scenarios, 15 App suites, 299 GUI checks and native package runs. Historical replay files are not silently converted to a different content identity.

The bank starts at 600 and is frozen during Fight. Shop carts combine rentals, up to 2 EX licenses and one optional super permit within 2400 credits. EX licenses repeat for the round; a super consumes its one use on legal startup. Confirmed counter-hit, anti-air and fresh precision-parry receipts settle at the next shop with category/round caps. Preparation, rollback, replays, results, training and public history use the same resource contract.

Health fills and damage trails now clip to each frame's angled opening, with a mirrored drain direction for player 2. Actual full/475/90 HP checks and replay reconstruction passed for both seats. Shop detail cards, prior-round facts, capability indicators, camera framing and controller navigation received native UI checks. A late network terminal receipt is drained before result handling, preserving the final counter/reward in round history.

## Verification

| Original acceptance layer | Verified records |
|---|---:|
| software | 76/76 |
| target device | 3/5 |
| human | 0/1 |

The unmodified software evidence-integrity gate is **PASS**; the all-layer gate is **FAIL**. These gates validate evidence completeness and hashes; they do not establish subjective game quality. The separate 61-row v2 ledger is `SHOP_ONLY_V2_ACCEPTANCE.json`. All 91 original PP findings retain explicit implemented, partial, optional or external dispositions in `AUDIT_CROSSWALK.json`.

The validated mechanical corpus includes 75 Expanded Core scenarios, App suites 15 each, and 15 independent payout/cart fixtures; fresh renamed Core/App checks are distinguished above. The native UI completed 299 checks. Renamed Windows and Linux native headless replays agree across 16140 input ticks, 16160 commands and 7 completed rounds. Timing measurements cover 24 movement/input/freeze/defense checks under the documented cosmetic bridge. Matched pilots contain 800 product/pair rounds and 112 full matches, plus 64 bounded strategy cells; their confidence intervals and limitations remain in `BALANCE_NOTES.md`.

Native process results, recordings, frame reviews, package extraction tests and exact hashes are indexed by `UPGRADE_STATUS.json` and the acceptance ledgers. A recorded movie is not a real-time performance test. Linux graphical runs use this PC's WSLg software renderer; reduced recording rate is explicitly identified in their report.

The isolated Windows graphical profile completed seven rounds on the GTX 1050 Ti / i7-7700HQ. Across 17,180 actual process callbacks, frame intervals were 16.67 ms median, 17.13 ms at the 95th percentile and 17.74 ms at the 99th percentile. The worst interval was 498.93 ms; this run does not attribute that isolated stall to a specific cause or prove stall-free pacing.

The laptop restarted during recording/verification packaging. All 574 production source/export files matched the pre-restart candidate hashes. Interrupted movies and damaged derived/cache files were preserved and regenerated where needed; incomplete runs never count as passes.

## Remaining checks and limits

- DEVICE-003: No physical controllers were available; software-injected controller inputs are separate evidence. Pending evidence: process_log, video
- DEVICE-004: Two processes and WSLg ran on this one PC; no second physical machine was tested. Pending evidence: process_log, replay, video
- HUMAN-001: Owner/player playtest and acceptance have not been recorded. Pending evidence: human_feedback

Only two fighters are complete. Some rare moves reuse supplied poses; RGB chroma-key edges and palette masks retain limitations. Marist stages preserve the supplied panoramas rather than claiming fully layered reconstruction. Pricing, adaptive human strategy and competitive balance need playtesting. Physical-controller, two-computer LAN and owner/friend verdicts are never inferred from bots, injected events or two processes on this PC.

## Build and audit

Run `Play Penny Punchers.cmd`, or extract the Windows/Linux package and launch its native executable while keeping the directory together. Player controls, rules, asset notices and known limitations are in `release_docs/` and packaged `docs/`.

The private repository is https://github.com/RJW34/Penny-Punchers. It contains the full source, supplied request packages, canonical data, assets and reproduction tools. Large native evidence is distributed separately with exact manifests. `AUDIT_GUIDE.md` describes source reproduction and evidence boundaries. The GitHub Actions workflow is retained at `ci/verify.yml`; activating it requires an account with workflow permission. Local verification has already run independently of that service.
