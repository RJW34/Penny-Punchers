# AFTER HOURS / original 32-bit art direction

This is an additive art development pack for **Strike Ledger's traditional-fighter variant only**. It sits beside the prototype for a later visual swap. It does not replace code, gameplay data, existing graphics, project settings or assets in the platform fighter. “AFTER HOURS” names this original visual treatment; the game's working title remains STRIKE LEDGER.

The aesthetic uses the late-1990s 32-bit fighting-game vocabulary: expressive human silhouettes, deliberately placed pixel clusters, strong contact poses, deep shadows, limited ramps, restrained dithering and assertive arcade typography. “32-bit” describes the art direction, not a promise that a generated PNG already has the resolution, palette, transparency or animation structure required by the game. The image boards are original design references. Full normalized sprite animation and runtime import remain production work.

## World and visual identity

After the industrial shift ends, a foundry exhibition hall becomes a small fighting venue. Steel beams, warm clerestory windows, maintenance machinery, distant spectators and painted ring markings establish the place. The mood is skilled local competition under work lights: warm amber highlights against cooled teal steel, weathered paint, practical clothing and paper-ledger graphics. No stage hazards, platforms, ledges, destructible machinery or extra actors affect combat. The foreground keeps the flat floor and solid corners obvious.

The homage is expressed through pace, composition, silhouette readability and pixel craft. Character designs, costumes, signs, logos, poses, move graphics and environmental arrangements are original. Do not trace commercial sprites, recreate a recognizable franchise costume or stage, copy a franchise logo, or import ripped artwork. Numeric move data remains this game's own canonical data.

## Core palette

| Role | Color | Use |
|---|---|---|
| Ink | `#111923` | Deepest outline, UI ground, silhouette separation |
| Cream | `#F4E5C5` | Main labels, brightest controlled accents |
| Amber | `#EDAA54` | Rook identity, warm foundry light, focus cues |
| Teal | `#69B8AC` | Vale identity, cool structural accents |

Build material ramps around these anchors rather than tinting every surface uniformly. Aim for four to six deliberate values per main material, with hue shifts in shadow. Keep the darkest skin shadows separate from ink and preserve skin identity across palette variants. Skin, hair, brass, fabric and steel need distinct response to light. Neutral cloth highlights can share cream, but avoid making every bright surface equally prominent.

Use one-pixel interior clusters at the final working resolution and one- to two-pixel broken exterior outlines. Put the darkest contour on the underside/back of a limb; allow warm or cool edge light to replace outline on lit planes. Use dither only for large low-contrast haze/light transitions. Faces and hands should rely on readable clusters rather than soft blur or dense speckle. Generated raster detail may need hand cleanup to reach this standard.

## Fighters

**Rook** is compact and powerful, with dark skin, short curls and amber workwear. Broad shoulders, wrapped or cuffed forearms, stout footwear and a contained guard express close-range pressure. His pose language compresses before it strikes: elbows, knees, short explosive steps and hard angled pulse shapes. Distinguish the short checks, stronger weight transfers and long recovery poses even when they belong to the same family.

**Vale** is taller and athletic, with silver-black swept hair, a teal utility jacket and ivory trousers. Her silhouette emphasizes long limbs, a higher guard and a contrasting lower-body shape. Her pose language uses pivots, heel lines, controlled retreat and segmented crescent pulse effects. Preserve her recognizable jacket and trouser blocks through extended kicks and airborne poses.

Each fighter's `identity.png` and accompanying `manifest.json` govern exact board cells and proposed alternate palette. Each has five selected boards: identity, universal, normals, techniques and supplemental. The manifests map all 49 move IDs and all 36 universal-state intents per fighter to reference cells. Supplemental boards expand those state references and provide complete-foot alternatives for cropped proximity attacks; they do not complete the animation phases. P1 and P2 identity is also carried by permanent corner shapes, portrait frames and floor markers. A mirror match must remain readable in grayscale; hue changes alone are insufficient. Do not recolor skin merely to distinguish players. Do not mirror baked lettering on clothing or UI.

The roster has **49 data-defined moves per fighter**: 18 standing/crouching/jumping normals, two proximity normals, forward-heavy command, leap overhead, two throws, four special families with L/M/H/EX variants, three selectable supers and six leased moves. Every ID is listed in `MOVE_COVERAGE.csv` and `move-coverage.json`. Shared family art is an initial direction reference, not permission to substitute one complete animation for every strength. Only one super is selected for a match; EX and the selected super use the same numeric wallet as preparation leases.

## Stage and camera composition

The target design canvas is **640 × 360**, displayed at **2×** in the existing **1280 × 720** presentation. The existing floor is y=604, so the design floor is **y=302**. Use this as the foot-contact line. The upper HUD occupies approximately y=0–77; the normal footer begins around y=336; the training footer begins around y=305. A pose, effect or stage object must respect those live UI regions.

The full stage is 768,000 milliunits wide while the camera sees 480,000. At design scale the visible viewport is 640 pixels and the complete stage spans **1024 pixels**. A production background needs enough horizontal content or repeatable layers for the camera's clamped range, not merely a stretched viewport painting. Current camera movement is cosmetic to geometry. Ground line, world-space floor seams and corner pylons must track the existing world-to-screen transform.

Suggested layer stack: far hall and ceiling; windows/columns/cable races; hanging sign; machinery; quiet gallery crowd; railing; faint haze/light shafts; world-anchored fighting floor and decal; fighter shadows; fighters; projectiles/contact effects; corner markers; HUD. Reserve atmospheric embers and low trim for areas where they cannot be confused with hitboxes or hide a foot.

The measurement grid uses the same floor and solid walls. Draw 20-world-unit minor spacing with emphasized 100-unit lines and readable labels; do not assume that a decorative grid drawn in a reference image already matches those world positions. Grid visibility and hit/hurt/push boxes are diagnostic presentation layers.

## UI, icons and lettering

Treat the interface like well-made workshop signage and printed event receipts: rectangular panels with occasional stepped corners, narrow rules, clear columns and bold condensed headings. Keep text and numerals live. A raster typography board demonstrates letterform direction; it is not a font file or verified full glyph set.

Use a readable body font, preferably at least 8–10 design pixels for dense labels after reflow, with tabular numerals for money, time and frame data. The current prototype contains smaller labels; simply halving those sizes would be illegible. The future swap must reflow affected tables, training panels and preparation rows. Maintain full keyboard and controller focus treatment on every screen.

Health may use a conventional length-based bar. **Credits are numeric**, with CR suffix, exact costs and discrete ready/unavailable/reserve-protected shapes. Do not add a filling super, EX, drive, stock or substitute combat-earnings gauge. Stun is nonspendable and explicitly labeled. Reserve is a floor on the same wallet, not another account. Preparation previews state their no-further-combat-spending assumption; online commitments do not expose the opponent's private draft before reveal.

Signature, technique and gambit slots use distinct silhouettes so 12 lease pictograms can stay compact. Selected, empty, locked and unaffordable states combine a frame/mark with a text label. Never use the same diamond or sparkle to imply both “enough money” and “a charge/stock has been earned.”

## Contact effects and accessibility

Hits use angular stars; blocks use hard shield arcs and square fragments; parries use hollow diamonds; red parries add a double-diamond outline; throw tech uses opposed break brackets. Color supplements these shapes. Rook's angular amber pulse and Vale's segmented teal crescent pulse retain separate ordinary, EX, super and returning variants where data defines them. Subtle dust, sparks and trails support contact without becoming foreground noise.

Every animated effect needs entry, peak and decay phases, transparent padding and a stable anchor. Reduced flashes substitutes outlined/local cues and lowers intensity; it does not change hitstop, superfreeze, action clocks or timing. Camera shake can be disabled independently. Super art should feel decisive through silhouette and composition even with flashes and shake disabled.

## Delivery boundary

`VISUAL_COVERAGE.csv` covers the complete visible source scope and marks a few small presentation extensions explicitly. Its assigned art path is the design destination for that component. It does **not** certify that every listed animation frame, glyph, crop or widget state has already been painted on that board. Each image's own manifest describes its actual contents. `production_outstanding` records the remaining extraction, redraw, animation, typography and integration work.

No prototype source, canonical data or existing asset is altered by this design package. `SWAP_GUIDE.md` provides a future integration order and acceptance checklist; `animation-contract.json` contains proposed coordinate, timing and animation metadata conventions. Integrate only through a separately reviewed presentation change once the source boards have become verified production assets.
