# Verification boundaries

Penny Punchers contains two fighters, four stages and two versioned move libraries using Shop-Only Economy Rework v2. Current candidate identity, test scope and remaining gates belong to the packaged `RELEASE_CANDIDATE.json`, `docs/UPGRADE_STATUS.json` and `docs/UPGRADE_REVIEW.md`, or their `reports/` counterparts in source. Historical After Hours and direct-spend evidence does not certify the changed simulation, UI or economy.

The [v1.0 tester release](https://github.com/RJW34/Penny-Punchers/releases/tag/v1.0) adds **Penny-Punchers-1.0-Windows-Setup.exe** around byte-identical game binaries from verified candidate `327be61cf6003b56cbc01e0f9abfd8c9a811fa13a80b33175829c93dfc5f6989`. The installer and updated source documentation do not create a new gameplay certification. Setup is per-user, includes Godot/.NET, creates a Start-menu entry and offers a desktop shortcut. Windows Apps uninstall preserves the game's Godot user settings and replays. The installer is unsigned; no code-signing certificate has been selected.

## Play and balance

The Core trial retains Rush Cascade and Tidal Step. Expanded replaces those third supers with Overtime and Prism Lattice and adds advanced rentals. Both use purchased EX licenses, one-use super permits and confirmed next-shop skill earnings. Bot experiments and deterministic tests establish only their stated conditions; human competitive balance, readable decision making and every matchup-specific punish remain separate questions. Experimental economy controls are isolated laboratory variants, not additional playable economy modes.

Two physical controllers, a physical keyboard-plus-controller session, two separate LAN computers and owner/friend acceptance remain unverified without actual recorded participation. Software-injected button events, two socket processes on one PC and bot matches do not substitute for these checks. Local shop drafts are public on the shared screen; private preparation uses hidden commit/reveal, an intentional format difference that affects shop decisions.

## Platform and presentation

Available Linux development runs use Ubuntu through WSL/WSLg. Dummy audio and software rendering do not verify physical speakers, an independent Linux GPU or input-to-display latency. Performance profiles apply only to the named binary, device, renderer and run conditions. Earlier frame-rate measurements are not inherited by a new export.

The fighter sheets provide existing key poses and phase-aware reuse. Branches, dives and air throws reuse compatible artwork rather than receiving newly authored full animation sheets. Transparent-alpha attempts failed validation and were excluded. Shipped RGB sheets still use runtime chroma key and heuristic palette masks, with edge and recoloring limitations. Missing presentation packs use a visibly labelled development placeholder; this is not a claim of another finished fighter.

Marist panoramas retain the original supplied images and preserve aspect through runtime crop/cover. Background ambience is decorative. The four stages are not fully reconstructed layered scenes; their scenery does not add collision obstacles or change legal movement.

## Compatibility and rights

Replays require compatible content/build identities; old move IDs and resource semantics are not silently substituted. Original request archives, the pre-Buyables checkpoint and the pre-shop-v2 checkpoint preserve the history. The current shop may reject a saved product set when prices, trial content or available funds change; it keeps the last valid current cart instead.

The owner expressly authorizes the public source repository and tester-build distribution. No project-wide license has been selected for the original code/art; public availability does not apply an open-source license. Third-party notices remain included under their existing terms. `RIGHTS.md` in source records the publication and licensing status. Marist reference photographs are excluded from runtime packages, and no institutional endorsement is claimed.
