# Rook — AFTER HOURS
# Rook — AFTER HOURS

Original 32-bit-era arcade pixel-art design sources for the **traditional-fighter variant only**. No current game assets, gameplay data or renderer were replaced.

The identity is a compact powerful dark-skinned fighter with cropped square curls, an ochre sleeveless work jacket, slate wraps, loose indigo trousers and pale work boots. The P2 proposal uses ivory and brick red. All art was generated with the built-in image generation tool; the exact prompts are in `prompts/`.

| Sheet | Actual size | Design coverage |
| --- | --- | --- |
| `identity.png` | 1536 × 1024 | 3 × 2: neutral / versus / win / lose portraits, P1 and P2 full-body designs |
| `universal.png` | 1254 × 1254 | 4 × 4: 16 movement, guard, parry, reaction and recovery keys |
| `normals.png` | 1024 × 1536 | 4 × 6: 18 standard normals, 2 proximity normals, command hammer, leap overhead and 2 throws |
| `techniques.png` | 1254 × 1254 | 4 × 4: 4 special families, EX concepts, 3 supers and 6 leased techniques |
| `supplemental.png` | 1024 × 1536 | 4 × 6: 20 additional universal clip references, 2 complementary universal keys and 2 complete-foot proximity alternatives |

The selected five sheets contain **86 design panels**. `manifest.json` records row-major frame names, **all 49 canonical Rook move IDs**, **all 36 universal clip IDs** from the proposed animation contract, actual dimensions, checksums, verified alpha status and integration notes. Shared L/M/H references are explicitly mapped as shared keys. A reference for a clip is not proof of its complete animation.

These are **design key poses, not complete animations or runtime-ready atlases**. Every selected image is RGB with no alpha channel, verified through read-only image inspection. Universal and normals contain baked checkerboards; techniques and supplemental have opaque grey mattes. A targeted background-removal image edit also returned RGB; `universal-alpha-attempt.png` retains that unsuccessful attempt without replacing the chosen universal source.

The original normals row 5, column 4 (`close_hp`) clips its lower legs/boots. **Supplemental row 6, column 2 now supplies a complete-foot upward proximity heavy alternative**, preserving the original sheet. Supplemental row 6, column 1 has a complete body but reads as a straight punch, so the original `close_mp` remains the clearer elbow design. The supplemental jump-takeoff key is already airborne, and knockdown-fall reads as near-ground side landing; both need stronger transition poses. The original movement sheet's cell 9 reads as landing compression rather than descent; supplemental cell 5 now supplies descent.

All 36 universal clip IDs have source reference mappings. Remaining work includes actual startup/contact/recovery sequences, looping movement and expressive states, grounded jump anticipation/toe-off, a clearer suspended knockdown fall, paired throw alignment and timing, differentiated strength/EX frames, palette normalization, true-alpha cutouts and verified pivot/frame bounds. No air-block state was introduced; the supplemental airborne defense pose is a single-palm parry.

Before a future swap, select and clean real-alpha sprites, normalize the palette, proportions, foot pivots and frame bounds, and author the missing animation phases using canonical data. Separate embedded effects into their own sprite assets. Use nearest-neighbor integer scaling. No crop rectangles or timing values in this pack are approved runtime contracts.
Original 32-bit-era arcade pixel-art design sources for the **traditional-fighter variant only**. No current game assets, gameplay data or renderer were replaced.

The identity is a compact powerful dark-skinned fighter with cropped square curls, an ochre sleeveless work jacket, slate wraps, loose indigo trousers and pale work boots. The P2 proposal uses ivory and brick red. All art was generated with the built-in image generation tool; the exact prompts are in `prompts/`.

| Sheet | Actual size | Design coverage |
| --- | --- | --- |
| `identity.png` | 1536 × 1024 | 3 × 2: neutral / versus / win / lose portraits, P1 and P2 full-body designs |
| `universal.png` | 1254 × 1254 | 4 × 4: 16 movement, guard, parry, reaction and recovery keys |
| `normals.png` | 1024 × 1536 | 4 × 6: 18 standard normals, 2 proximity normals, command hammer, leap overhead and 2 throws |
| `techniques.png` | 1254 × 1254 | 4 × 4: 4 special families, EX concepts, 3 supers and 6 leased techniques |

`manifest.json` records row-major frame names, **all 49 canonical Rook move IDs**, actual dimensions, checksums, verified alpha status and integration notes. Shared L/M/H references are explicitly mapped as shared keys.

These are **design key poses, not complete animations or runtime-ready atlases**. Every selected image is RGB with no alpha channel, verified through read-only image inspection. Universal and normals contain baked checkerboards; techniques has an opaque grey matte. A targeted background-removal image edit also returned RGB; `universal-alpha-attempt.png` retains that unsuccessful attempt without replacing the chosen universal source.

Visual QA found one specific redraw issue: **normals row 5, column 4 (`close_hp`) clips the lower legs/boots at its panel edge**. The movement sheet's cell 9 reads as landing compression rather than descent. Character silhouettes and move silhouettes otherwise provide useful reference, with broad pixel clusters and a consistent original identity.

Before a future swap, extract and clean real-alpha sprites, redraw that cropped key, normalize the palette, proportions, foot pivots and frame bounds, and author startup/active/recovery and state transitions using the canonical data. Separate embedded effects into their own sprite assets. Use nearest-neighbor integer scaling. No crop rectangles or timing values in this pack are approved runtime contracts.
