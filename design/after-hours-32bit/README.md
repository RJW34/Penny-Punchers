# AFTER HOURS

Original 32-bit arcade art direction for **Strike Ledger**, the traditional-fighter variant. A nocturnal foundry exhibition in ink-blue steel, furnace amber, oxidized teal and warm ivory; grounded workwear silhouettes and expressive arcade poses.

**[Open the art gallery](review/index.html)** — 22 selected PNG backgrounds and design sheets. The gallery works locally, without a server or internet connection; filter categories, inspect individual files, zoom and change the preview matte.

This is a separate design/source-art pack for a future visual swap. No existing prototype asset, code, data, configuration or platform-fighter file was replaced. The repository here is an unborn `master` checkout inside `trad-fighter-variant`; this task neither switched nor created Git branches.

## The artwork

| Collection | Included |
| --- | --- |
| Rook | Identity, portraits and alternate palette; movement/defense; 24 normal/unique/throw keys; special/super/lease keys; 24 supplemental state/repair poses |
| Vale | Identity, portraits and alternate palette; movement/defense; 24 normal/unique/throw keys; special/super/lease keys; 24 supplemental state/repair poses |
| Arena | Foundry exhibition background, calibration-grid training room, 24-cell prop/spectator/ambient sheet |
| Combat | 36-cell hit, heavy hit, block, parry, pulse, EX/super, dust, throw-tech, dizzy and shadow effect sheet |
| Interface | Title backdrop, HUD parts, panel/control/icon skin, canonical lease/super icon board, four main-screen compositions, six utility-screen compositions, type/input-glyph study, branding/announcements |

The [visual inventory](docs/VISUAL_COVERAGE.csv) assigns references to **352 scoped visual requirements**. The [move inventory](docs/MOVE_COVERAGE.csv) maps all **98 canonical moves**, and the fighter manifests map **36 universal state intents per fighter**. These counts describe coverage and design intent, not finished animation frames or 352 individual image files.

## What is ready, and what comes later

All selected raster artwork is saved here and can be reviewed independently of the running prototype. Full move timing, source IDs, palette direction, presentation coordinates and swap requirements are documented.

The source sheets still require production preparation: clean fighter cutouts, consistent pixel scale and root pivots, explicit sprite rectangles, pose refinement, in-between animation and timing, bitmap-font metrics and presentation integration. Some cell drawings serve as closely related references rather than exact action extremes. The original fighter sheets have baked mattes/checkerboards; the supplemental sheets provide flat-matte alternatives and complete-foot proximity references. No source sheet is advertised as a drop-in animation atlas.

Start with [ASSET_NOTES.md](ASSET_NOTES.md), the [art direction](docs/ART_DIRECTION.md), and the [future swap guide](docs/SWAP_GUIDE.md). Per-fighter details: [Rook](fighters/rook/README.md), [Vale](fighters/vale/README.md). The existing 60 Hz simulation, hitboxes and numeric economy remain authoritative; the art does not introduce super meters or combat income.

## Provenance and verification

Art was generated and iterated with the **built-in image_gen tool**, with original prompts and no supplied franchise artwork. It is a homage to arcade pixel-art craftsmanship, with original characters, setting and branding.

- [Main prompt set, including revisions](docs/main-prompts.json)
- [Rook prompts and board metadata](fighters/rook/README.md) / [Vale prompts and board metadata](fighters/vale/README.md)
- [Asset manifest: dimensions, format and hashes](ASSET_MANIFEST.json)
- [Pack-local asset register](ASSET_REGISTER.csv)
- [Integrity, coverage and existing-file comparison](VERIFICATION.json)

`tools/verify_art_pack.py` uses only the Python standard library. Its default mode checks image structure, PNG chunk CRCs, decompressed payload size, all catalog hashes, referenced files and canonical move coverage. `--refresh` regenerates only this pack's manifest, register and verification report. It never modifies images or game files. This artifact check is not gameplay certification.

The baseline comparison detected existing prototype files changing during the parallel build. Their paths are recorded in `VERIFICATION.json`; those changes were left in place. The art task wrote only within this new pack, so the report does not claim that the entire developing project stayed byte-for-byte unchanged.
