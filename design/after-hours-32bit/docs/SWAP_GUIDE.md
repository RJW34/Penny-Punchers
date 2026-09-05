# Future visual swap guide

The images in this pack are original **design boards**, supplied alongside the traditional fighter so the prototype can keep developing. They are not a drop-in replacement for the current vector renderer. This guide describes the remaining steps; it does not apply them. Existing code, data, images, project configuration and reports stay untouched.

## Read the inventory correctly

- `VISUAL_COVERAGE.csv`: one row for every scoped visual component, including 98 moves, all current screens, universal states, UI, effects, projectiles, stage parts, glyph groups and 12 leases. `reference_art` is an assigned design reference; `production_outstanding` is required work, not a completed deliverable.
- `MOVE_COVERAGE.csv`: a compact row for each exact canonical fighter/move ID, command, family, strength, timings, active groups, availability, lease relation and price.
- `move-coverage.json`: every source move object retained in full, its phase intervals, associated lease and source SHA-256 fingerprints. Empty production frame rectangles, pivots and timelines intentionally mean “not authored.”
- `animation-contract.json`: proposed working dimensions, clocks, mirror policy, universal clip checklist and current presentation metadata gaps. Art cel estimates are planning targets; they are not simulation durations.
- Fighter `manifest.json` files: actual generated board cell assignments and known defects. Each now maps all 36 universal-state intents and 49 canonical moves across five selected boards, including supplemental. Those manifests take precedence over an assumed uniform crop grid. Mapped references do not imply complete animation.
- `inventory-summary.json`: row counts and assigned reference paths. The inventory script can regenerate the metadata after deliberate source changes; inspect source changes before treating new hashes as approved.

## Source-to-art map

| Existing source | Future art use |
|---|---|
| `game/Presentation/ArenaView.cs` / `DrawFoundry`, `DrawGrid`, machinery/corners | `stages/foundry.png`, `stages/grid.png`, `stages/environment-atlas.png`; separated parallax layers required |
| `ArenaView.cs` / `DrawFighter`, `PoseFor` | `fighters/rook/*` and `fighters/vale/*`; normalized cels and per-move timelines required |
| `ArenaView.cs` / `DrawProjectile`, `DrawImpact` | `effects/combat-atlas.png`; transparent animation frames and source-move mapping required |
| `game/Main.cs` / `_Draw`, `Main.PrepDetails.cs` | `ui/hud-atlas.png`, `ui/lease-icons.png`, `ui/typography.png`; live HUD values and state binding |
| `game/Main.Menus.cs` | `ui/title-backdrop.png`, `ui/interface-atlas.png`, `ui/menu-designs.png`, `ui/branding-announcements.png`; reusable widgets, wordmark and portrait crops |
| `game/Main.Network.cs`, `Main.PadText.cs` | `ui/system-designs.png`, interface tiles, connecting/wait/failure states and glyphs |
| `game/Main.Training.cs`, `Main.LabState.cs`, `Main.LabDiagnostics.cs` | `ui/system-designs.png`, training panels, input glyphs, diagnostic typography and grid |
| `game/Main.Replays.cs`, `Main.ReplayDetails.cs` | `ui/system-designs.png`, transport icons, archive panels, wallet chart and receipt layout |
| `game/Main.Rendering.cs`, `game/Presentation/RenderState.cs` | Read-only binding from simulation state/events to visual clips; no new combat authority |

All paths above are relative to `trad-fighter-variant/strike-ledger-astra-scaffold`; art paths are relative to `design/after-hours-32bit`.

## 1. Audit and normalize reference images

Work into a **new** production-art directory in the traditional variant when integration is commissioned. Keep these original reference boards intact. Verify each image's actual size, cell positions, pose completeness and labels with its manifest and visual inspection. Generated board sizes are commonly larger than 640 × 360 and do not establish a reliable pixel scale.

Remove board backgrounds and labels from copied working art. Some generated images can contain a painted checker pattern rather than alpha transparency; turning all matching colors transparent will damage clothing and leave halos. Inspect the alpha channel and hand-mask/redraw as needed. Repair cropped anatomy, inconsistent costume details, antialiased edges and stray particles before atlas packing. All five selected boards for each fighter are RGB with no alpha. Both supplemental sheets use opaque gray mattes and show complete bodies. Rook's original normals row 5, column 4 (`close_hp`) has cropped boots; its supplemental row 6, column 2 supplies a full-foot alternative. Vale's original close-MP/close-HP cells have incomplete lower legs; supplemental row 6, columns 1/2 supply whole-body alternatives. The new Close MP alternatives read as straight punches, so the intended silhouette still needs review. Preserve the original boards as provenance and consult each manifest's phase/facing notes. The failed alpha attempts did not solve transparency. Other pack images have individually inspected channel formats; use the root asset manifest rather than applying the fighter RGB status to stages, UI or effects.

Normalize a fighter's neutral silhouette first, then preserve its scale across every move and universal state. Never shrink a wide kick or tall jump independently to fit a standard cell. The proposed normal cell is 256 × 192 with a floor/root pivot at (128,172); 384 × 256 with pivot (128,224) can accommodate wider actions. These are proposed atlas containers, not promises that the source sheets already use these dimensions. Rook around 124 design pixels and Vale around 132 is a starting neutral-height target to verify against the game's boxes.

Export clean straight-alpha RGBA textures. Use nearest filtering and no mipmaps for pixel art; keep square pixels and an integer display scale. Define explicit sprite rectangles, root pivots, transparent gutters and effect attachment points after normalization. Do not infer production rectangles from generated grid lines.

## 2. Complete animation before replacing poses

Each of the 98 IDs needs an explicit clip record. The canonical action clock is 60 ticks/second. Startup is `[0,startup)`, active is `[startup,startup+active)`, recovery is `[startup+active,total)`. The inventory retains every hitbox interval, movement segment, cancel window, invulnerability segment, projectile and throw definition. Source numbers remain authoritative.

A cel may be held for several simulation ticks; simulation time must not be stretched to accommodate artwork. Zero-startup feints must not gain a new anticipation period. Produce readable anticipation, contact and recovery where those phases exist, with stronger normals distinguishable by their pose and weight shift. Special L/M/H/EX variants may share compatible cels but each needs a specific timeline and sufficient distinct extremes. EX accents and trails do not replace the correct attack pose.

Every authored hit group needs a readable contact moment. Supers need their own silhouette and multihit sequence. A projectile move releases once at its exact spawn tick, then the caster's recovery and projectile lifetime run independently. Vale's returning pulse reverses after its canonical 45 projectile ticks; the visual must not imply an extra permitted hit. Throw clips require matched attacker/victim poses through grip, rotation, release, floor impact and recovery for Rook/Rook, Rook/Vale, Vale/Rook and Vale/Vale, both directions.

Complete the universal state list in `animation-contract.json`: idle; forward/back walk; turning; crouch entry/hold/exit; dashes; takeoff/rise/apex/fall/super-jump/landing; high/low blocks; high/low/air/red parries; high/low/air hit reactions; thrown/tech; falling/grounded knockdown/wakeup; dizzy; taunt; intro/win/defeat/draw. All 36 intents per fighter now have manifest-mapped reference cells across the universal and supplemental sheets. The contract records those board/row/column bindings. A key-pose board may represent several states with a related pose; that relationship does not complete the missing animation or make a reference pose appropriate for every tick of the phase.

Lock facial features, costume construction, limb lengths and material ramps before in-betweening. Use a single palette map per alternate costume. Verify horizontal flip around the same root pivot and repair asymmetrical lettering/details separately. Keep limb motion in the sprite while root displacement comes only from the simulation.

## 3. Bind through a read-only presentation adapter

`ArenaRenderState` is presentation-only. Preserve that boundary. Art must not write health, credits, boxes, velocity, movement duration or collision state. `FighterRenderState.MoveId`, `ActionFrame`, `Startup`, `Active`, `Recovery`, `Facing` and `Palette` already offer much of the required binding. `Main.Rendering.cs` maps live state and de-duplicates presentation events.

The current snapshot collapses some animation distinctions. Future additive metadata or an adapter will be needed for:

- Signed walk direction and movement phase; vertical velocity/apex/fall; landing and turning.
- Parry subtype: the core has High, Low, Air, RedHigh and RedLow, but the current renderer selects only `parry` or `crouchparry`.
- Throw-victim link/age and knockdown/wakeup phases. The core tracks these more precisely than the existing presentation snapshot.
- Taunt: it emits `ActionStarted` with move `taunt` and currently uses 24 zero-velocity dash ticks, so a dedicated cosmetic state is needed for the new pose.
- Contact height for distinct hit reactions and actual freeze information for holding cels.
- Projectile stable ID, source move/fighter, age and exact variant. Current `ProjectileRenderState.IsSuper` groups EX and super together and does not carry a move ID or lifetime clock.

Expose already-authoritative facts without changing their semantics. Hold intended animation during hitstop and superfreeze. Cancel/interruption immediately changes the displayed action. On replay seeks, reset, simulation replacement or rollback correction, clear stale effects and reselect cels from state. Continue using confirmed/deduplicated presentation event delivery so rollback does not duplicate sparks or debit receipts.

## 4. Fit stage and HUD to the existing coordinate system

The current formula is `screenX = 640 + (worldX-cameraX) * 1280/480000` and `screenY = 604 - worldY * 1280/480000`. At design scale, use `320 + (worldX-cameraX) * 640/480000` and `302 - worldY * 640/480000`. Camera X clamps to 240000–528000. Stage walls remain world X=0 and X=768000; the arena has one flat floor and no platforms or hazards.

Separate stage layers and provide 1024-pixel-wide world coverage or deliberately seamless repeating parts. Test both camera extremes and center. Keep floor seam/pylon positions world-anchored. Decorative perspective does not change where characters stand. Align the grid from exact world coordinates rather than tracing the raster reference's grid.

Build text, timers, cash and diagnostics from verified glyph metrics. Preserve native resolution readability; dense UI needs reflow rather than a blind 50% reduction. Ensure the central contact area remains readable behind effects and overlaid receipts. Check preparation arithmetic, round points including halves, timer limits, all 12 lease states and both mirror palettes. The HUD must not add a second wallet or a super/EX meter. No combat action earns money; only confirmed round settlement does.

The planned lease-icon board order is six canonical Rook items across row 1; Rook arts 1/2/3 followed by signature/technique/gambit symbols on row 2; six canonical Vale items on row 3; Vale arts 1/2/3 followed by lock/check/slash on row 4. Verify the final board and its manifest before assigning crop coordinates. Canonical item names and IDs in `data/items.json` and the coverage files override any text artifact in a generated image. Layout boards demonstrate composition; never copy their sample currency values, reward labels or decorative tokens into gameplay rules.

## 5. Verify the future swap

These are future integration acceptance checks, not claims that the art reference delivery has already passed gameplay checks:

- All 98 moves and every universal state play the intended cels at canonical ticks; all strengths, selected supers, leases, cancels, throw pairings and projectile phases are covered.
- Feet align with y=302 at design scale through movement, turns, crouch, throws, knockdowns and jumps; debug boxes retain exact existing geometry.
- Both stage corners and training-grid positions remain correct with camera tracking; nothing resembling a platform or hazard is introduced.
- Title, select, local/private preparation, fight, results, pause, help, all move pages, controls, settings, replay screens, training screens, network states and controller text entry have complete visible/focus states.
- Wallet numerals, costs, reserve rules, rejection reasons and confirmed receipts match simulation. Only health is treated as a conventional combat HUD bar; stun remains labeled nonspendable.
- P1/P2 and mirror matches stay distinct in grayscale; contact feedback remains distinguishable with flashes and shake disabled.
- Keyboard/controller focus, remapping and disconnect states stay readable and reachable. Invalid network/replay fields and empty archives have finished states.
- Replay seeking and private rollback do not leave stale poses, duplicate effects or duplicated financial cues.
- Asset register/provenance entries are added only when assets actually ship; any external font has a verified compatible license and notice. Generated typography boards do not confer a font license.

## Source audit and regeneration

The initial read-only `python -B tools/validate_pack.py` semantic check passed with 2 fighters, 98 actions, 12 items, 82 requirements and 15 work packages. Its scope is **SCAFFOLD_ONLY** and `game_implementation` is **NOT_VERIFIED**. No existing report was rewritten.

The additive inventory generator is `docs/build_inventory.py` inside this art pack. It writes only its neighboring inventory files. It captures SHA-256 fingerprints of the canonical fighter, item, input, physics, resource and stage files. Because prototype work can continue in parallel, compare those hashes before a future integration and reconcile new moves or changed timings explicitly. There is no automatic link from these design records to runtime content.
