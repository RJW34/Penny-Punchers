# Penny-Punchers: current source and build

This is the working native game repository. Continue in this checkout and preserve uncommitted work. The latest user request integrates Shop-Only Economy Rework v2 after the expanded audit and Buyables Design v1.

Read AGENTS.md, docs/21_SHOP_ONLY_V2.md, docs/01_DECISIONS_AND_AUTHORITY.md, and reports/RESUME_PACKET.md. Current code and registries are authoritative implementation inputs; archived design packs and pre-v2 evidence retain historical identity. Never restore the package's pinned baseline over the upgraded moves or fixes.

The finish includes Thomas and Vincent, the 32-bit art set, Foundry/Grid and both Marist stages, complete local and CPU matches, untimed training, deterministic replay, private rollback, settings and Windows/Linux builds. All competitive new matches use shop-only purchases, repeatable licensed EX, one purchased super startup per round, and bounded next-shop skill receipts. No per-action bank spending or active reserve floor remains.

Run `python tools/doctor.py`, `python tools/validate_pack.py --strict-schema`, and `python tools/build.py --test` for the current production checks. Run `python tools/build.py --export` for native packages after checks pass. Release evidence must match the exact candidate source/content/binaries; legacy oracle passes are not current gameplay proof.

Keep software, physical-controller, two-physical-PC and actual-human verdicts separate. Do all available implementation and verification without fabricating unavailable external evidence. Update reports/STATE.json and reports/RESUME_PACKET.md at meaningful boundaries.
