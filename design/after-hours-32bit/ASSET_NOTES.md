# Selected artwork and production notes

All 22 selected PNGs are original source designs. Their actual dimensions and channel formats are in `ASSET_MANIFEST.json`. Requested generation dimensions often differ from returned dimensions. A 32-bit arcade aesthetic is the art direction, not a claim that every PNG uses a 32-bit color format or is already quantized to a console palette.

## Main sheets: observed reading order

Use these orders for review only. They are not verified crop rectangles. Re-author exact rectangles, gutters, baseline and pivot metadata during sprite production.

| File | Observed arrangement and use |
| --- | --- |
| stages/foundry.png | Full-bleed foundry reference with flat combat floor, teal pipes left, amber boiler right, clock/gallery/trusses behind. Raster is one flattened composition; parallax layers and extended camera coverage remain to be separated/painted. |
| stages/grid.png | Calibration-room composition. Rebuild measurement lines from authoritative world coordinates for the runtime overlay; the painted grid is not a measurement instrument. |
| stages/environment-atlas.png | 6×4: row 1 pipes, column, truss with hooks, railing, boiler, clock; row 2 lamp dim/lit, hook, blank plaque, vent, decorative wheel; row 3 cheering worker ×3 and clapping worker ×3; row 4 furnace ×3 and steam ×3. Actual RGBA source; verify alpha fringes and effects before extracting. The decorative wheel replaced a requested floor-inlay cell; floor treatment is visible in the stage background. |
| effects/combat-atlas.png | 6×6: row 1 small hit ×6; row 2 heavy hit ×6; row 3 block brackets ×3 then parry diamond ×3; row 4 Rook pulse ×3 then Vale pulse ×3; row 5 EX accents ×3 then super bursts ×3; row 6 air dust, dash dust, landing dust, throw-tech rings, dizzy stars, floor shadow. Actual RGBA source; some particles approach cell boundaries. Review each crop before packing. |
| ui/hud-atlas.png | Opaque dark-matte component study: health and stun frames/fills, timer, score pips, numeral wallets, EX/super/reserve chips, reason-tagged wallet feedback, lease sockets and player markers. Currency bars are absent. Extract shapes; render all changing numbers and labels at runtime. |
| ui/interface-atlas.png | Irregular row layout of panels, buttons, sliders, toggles, menu icons, status chips, prompt strip, scrollbar, fields and spinner. Intended shape/style source for sliced UI panels, not an automatic equal-grid atlas. |
| ui/lease-icons.png | 6×4. Row 1 canonical Rook leases: Clinch entry, Low drive, High hook, Low turn, Step feint, Sway feint. Row 2 Circuit Break, Forge Impact, Rush Cascade, signature/technique/gambit categories. Row 3 canonical Vale leases: Returning pulse, Low palm, Heel arc, Long check, Step feint, Sway feint. Row 4 Rising Current, Crosswind, Tidal Step, locked/equipped/unavailable. Glyphs convey the action; reconcile miniature hair/costume simplification against each fighter identity sheet before shipping. |
| ui/menu-designs.png | 2×2 title, lineup, preparation and results compositions. Corrected preparation shows one signature selected and no stock row. Results display point/wallet examples only. Sample values are layout examples, not game data. |
| ui/system-designs.png | 2×3 settings, input test, training, replays, private match, dialog family. Corrected to canonical roster names and abstract fighter silhouettes. Full runtime menu text and focus behavior remain data/code driven. |
| ui/typography.png | Alphabet, digits, punctuation, direction/button glyphs and numeric samples. This is a visual font study, not an installable font. Rebuild small-body text and verify every glyph, advance, baseline and motion-arrow sequence; don't crop ambiguous motion symbols blindly. |
| ui/branding-announcements.png | Wordmark/emblem/app-icon concept, round/fight/KO/time/draw/win/perfect banners and combat status chips. Dynamic round numbers and combo counts need glyph rendering. App icon still needs dedicated size exports. |
| ui/title-backdrop.png | Opaque title/menu background with empty left wall for live text. No built-in menu behavior. |

## Fighters

Each fighter has five selected sheets and a manifest with observed cell order and exact move references. RGB sheets contain opaque mattes; a checker pattern is painted pixels, not real transparency. The extra `universal-alpha-attempt.png` files are unsuccessful background-extraction attempts, retained for provenance and excluded from the selected gallery/catalog.

The supplemental boards add separate concepts for previously missing universal state intents and complete-foot proximity alternatives. The main sheets remain unchanged. Supplemental alternatives still require pose judgment: a straight punch is not a close elbow, a late airborne pose is not the whole jump takeoff, and a near-floor pose does not complete a falling animation. Follow the latest fighter README/manifest instead of assuming every generated instruction was rendered literally.

Sprite scale, face/costume consistency, exact attack silhouettes, foreground readability, alternate-palette animation and matched attacker/victim throw choreography need a production pass. The move mapping is a design handoff; startup/contact/recovery cels, roots and timelines are deliberately not fabricated.

## Originality and scope

No commercial game sprite, stage, logo, font or ROM was supplied as a generation input. Generated boards were corrected when they invented off-roster names, misleading economy details or inconsistent icon identity. No existing project register, code, asset or data file is changed by this pack. `ASSET_REGISTER.csv` is local to this optional design pack; merge entries into a future release register only when selected exports actually ship.
