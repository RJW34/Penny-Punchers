# After Hours UI and renderer review

Final review completed 2026-09-05 against source candidate `5414744b92e51de04d4be67df1929929a1072e7d5b986d773b1459166217784d`. The UI and captured art pass this visual review with the minor observations listed below. This is an independent source and native-image review; it does not claim physical controller testing, player feel, or that a rendering fixture executes combat.

## Final candidate and evidence

The machine-readable result is `reports/evidence/after-hours-visual-review.json`. It rehashed all 133 production inputs in the current candidate, recomputed the source fingerprint, checked both process reports against the exported binaries and candidate manifest, validated every PNG CRC, and bound all 49 final PNGs to the directly inspected image bytes. All 11 UI images and all 38 art images are byte-identical to the inspected snapshots preserved under the `-pre-interruption` folders. The final title, selection, Rook idle mirror, and Vale Crosswind images were also opened again after rebinding. This is an explicit image-identity check; the old executable is not being relabeled as the final build.

| Final artifact | SHA-256 |
| --- | --- |
| Windows executable | `f01e9eec6f8259c4c755614f325313dffa6c7a90619c7d38a5bf76604df91060` |
| Windows PCK | `471731809ef120516c33cef6171cf1c8e289e8695277f47b4fa64f7befbc5b6b` |
| Shell DLL | `e19abeacdf69aa0dce26161e2f0589e6f7c4c49f550eb559b8e4c66b511107db` |
| Core DLL | `204653b031ca2700371d8c1df62c4b7b9eb131825fcaaba1ef5ee01676c2c7df` |
| App DLL | `72e7b96bbd878126db96ba0be8e49bae49df1417bf3162f4aec4d1f325e6cebb` |
| UI MP4 | `3b6a7d77de9707a9bb52c403427c4415a18af73a2a484609b9950771c5a0f2bb` |

`native-after-hours-ui` completes 75 software controller-menu checks with 11 PNGs and exit 0, without engine errors or leak messages. Its MP4 is 16.716667 seconds at 1280×720/60 fps with successful complete decode. `native-after-hours-art` completes 415 binding checks covering all 98 moves and 125 atlas-state entries, with 38 PNGs and exit 0. Every selected native-runtime hash matches the current candidate. The machine report contains individual hashes and actual command lines; no GPU process or encoder was launched by this final review.

## UI integration

`game/Main.ArtSkin.cs` binds seven unchanged source PNGs through cached Godot `AtlasTexture` crops, `StyleBoxTexture` frames, and native GUI controls. `game/Assets/AfterHours/UI/manifest.json` records source paths and matching SHA-256 values. No source bitmap was rewritten for these UI bindings.

The title uses the supplied foundry backdrop and wordmark. Menu families use the supplied blank interface panel, icons, and portrait crops. Their labels and callbacks remain native controls. Selection and preparation retain their existing button text prefixes, independent seat handling, and focus behavior; `UiRefreshPreparationSkin` only changes frame decoration. The two unsupported `Button.IconMaxWidth` assignments were replaced by the supported `icon_max_width` theme constant.

The HUD reads health, credits, score, reserve, stun, timer, and round from the current simulation. It crops empty health frames, plain fill strips, and wallet icons from the artboard; it does not display the board's example health, currency, or timer values. The super slot and cost use the existing selected-art data convention. P1 and P2 remain explicit text labels. Numeric values remain normal Godot text for readability.

`UiArtBackdrop` hides the arena while menu backgrounds are visible. `Clear("fight")` and `Clear("art-review")` make it visible again. The UI CanvasLayer remains above the world; the root HUD remains above the arena at Z=-1. The skin does not change input routing, Core, App, collision boxes, or simulation timing.

## Renderer findings and responses

| Finding | Source review status |
| --- | --- |
| Arena child overlay with relative Z=2 would put effects above the root HUD. | Corrected by the owning agent: `ArenaOverlay` is added without a positive relative Z override. Its later child order keeps it above fighter cels while remaining below the HUD. |
| Static atlas texture wrappers and palette materials needed explicit release. | Corrected by the owning agent: `FighterSprites.ReleaseAtlases`, `FighterSprites._ExitTree`, and `ArenaView._ExitTree` now release the owned wrappers. UI wrappers are released in `Main._ExitTree`. Native clean-exit validation remains the authoritative resource check. |
| The first Rook alternate shader selected amber colors also found in skin. | Corrected by the owning agent: the current shader selects navy cloth instead. The initial source predicate matched 68/884 pixels in a face region and 6/936 in an arm region of the production universal sheet, so this was a concrete skin-tone collision. Current native images must be checked after the correction. |
| The adapter initially selected zero-health knockdown ahead of terminal result poses. | Corrected by the owning agent: settled result states and intro states now have priority in `Main.Rendering.cs`. |
| A nonempty terminal action could override universal result/intro state in the cel selector. | Corrected by the owning agent: `FighterSprites` now gives win, defeat, draw, and intro universal states precedence over the retained action. Final result and win captures reviewed. |
| The raster measurement room is decorative when debug boxes are disabled. | Explicit limitation: the canonical 20-unit world-coordinate overlay currently appears with both TrainingGrid and DebugBoxes. Decorative perspective grid lines must not be interpreted as collision measurements. |

The cel renderer uses per-cell region, root pivot, scale, and authored-facing metadata. It does not retime simulation actions. Phase selection uses authoritative action frame and canonical startup/active/recovery durations. Actor facing and per-cell authored facing are applied separately. Freeze pauses universal state age. Tick rewind resets the universal state timeline. Cosmetic event buffers reset when simulation identity changes or rewinds.

Current mirror-match selection sets alternate colors only for seat 2 when both fighter IDs match. Character ID, seat labels, position markers, and live HUD labels remain independent of palette color.

## Native draft evidence inspected

`reports/evidence/native-after-hours-art-draft/art-review.json` reports 383 successful bindings and 34 viewport screenshots. `process-result.json` records exit 0 and no errors; `process.log` identifies native Godot 4.6.3 OpenGL on NVIDIA GTX 1050 Ti. This run predates the final palette correction and is draft evidence only.

Directly inspected `rook-idle-mirrors.png`, `vale-idle-mirrors.png`, and `rook-s_hp-active.png`. Fighters have coherent visible silhouettes, readable roots at approximately y=604, no visible rectangular matte background, and distinct facing. The foundry and calibration room fill the viewport. The old mirror images exhibit the known overly broad recoloring; do not use them as proof of the corrected palette.

## Native UI draft evidence inspected

Directly inspected all eleven PNGs under `reports/evidence/native-after-hours-ui-draft`: title, CPU selection, independent preparation, local fight, two-device remapping, replay controls, training grid and boxes, private setup, controller text entry, verified completed-match result, and rematch preparation. The controller report records 75 successful software checks; its process result records exit 0 and no errors.

This visual inspection caught a real minimum-size bug in `UiArtImage`: `TextureRect.Size` was assigned before `ExpandMode.IgnoreSize`, so crop dimensions overrode intended title-logo, portrait, and icon sizes. The initializer now configures IgnoreSize first, then the texture and final rectangle size. A second collision between training input history and the measurement-room subtitle was fixed by moving history from (24,198) to (42,224). Both fixes are confirmed in the final native screenshots.

The remaining menu layouts and dynamic HUD fields in the inspected draft are legible; preparation has distinct independent seat highlights and results show the verified match score before the rematch callback. The software-test banner intentionally covers part of P2's header and is absent during normal play. No physical controller claim is made.

Action-fixture screenshots validate rendered bindings; actual legal-input showcase, full-match, and controller-menu runs remain separate evidence.

## Final visual findings

All eleven final menu/HUD screenshots have readable live text, contained decoration, visible focus, and consistent metal panel styling. The title wordmark clears the tagline and menu; both selection portraits and the screen icons fit their assigned rectangles. Preparation shows distinct P1/P2 highlights. The verified result shows Rook 5 / Vale 4, followed by the actual controller rematch callback and both reset 600-CR wallets. The training input history clears the measurement-floor subtitle. Normal HUD values are drawn live; the source board's example numbers are never shown.

All 38 art captures were reviewed: both fighters' idle, walk, crouch, knockdown, parry and win mirrors; all four throws; all six supers; every captured EX pose; and the captured standing, crouching and airborne normals. Roots, facing and complete limb silhouettes remain coherent. Rook's alternate trousers become warm stone/tan while the skin and amber jacket remain intact. Vale's alternate jacket is purple and trousers remain cream, without the draft shader's patchy trouser recoloring.

Rook Pulse EX, Vale Pulse EX, and Vale Crosswind have no baked flying projectile in their no-projectile fixture snapshots. Crosswind retains both hands and the complete body after its cleanup. Authored close-contact arcs and super afterimages remain decorative on other cels. They do not create simulation objects or hitboxes.

There are no blocking visual faults in the inspected scope. Minor observations are retained honestly: a few pink edge pixels remain around extended fingertips in some keyed poses, and authored EX trails may extend below the body root in static fixtures. The labeled software-test banner obscures part of P2's heading during testing and is absent from normal play. These image checks do not establish every animation tick's quality or physical-controller behavior.
