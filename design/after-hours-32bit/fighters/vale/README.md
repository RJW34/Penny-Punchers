# Vale — AFTER HOURS 32-bit source art

Five original design boards provide 88 design cells covering Vale's identity, portraits, colorways, universal movement/defense, attacks, throws, specials, supers and leases. The mapping in `manifest.json` references all 49 current canonical Vale move IDs and all 36 proposed universal clip intents. These are named pose references, not finished motion clips.

These are pixel-art design key poses and expression references. They are not completed animation or drop-in runtime sprite sheets. All files are new and confined to the traditional fighter's design folder; no prototype asset has been replaced.

| Board | Actual size | Grid | Content |
| --- | --- | --- | --- |
| identity.png | 1536 × 1024 | 4 × 2 | P1, P2, neutral, versus / win, lose, rear turnaround, palette chips |
| universal.png | 1254 × 1254 | 4 × 4 | Movement, jump, defense, parry, hit reaction, knockdown |
| normals.png | 1086 × 1448 | 4 × 6 | 18 standing/crouching/jumping normals, proximity normals, command/overhead, throws |
| techniques.png | 1254 × 1254 | 4 × 4 | All special families, three supers, six leases, activation |
| supplemental.png | 1086 × 1448 | 4 × 6 | 20 additional universal states, two whole-body proximity fixes, high hit, quickrise |

## Visual direction

Vale uses open palms, long heel strikes and teal pressure-current effects. Her one-sleeved teal cropped jacket, charcoal body suit, ivory segmented wide trousers and flat dark footwear form an original silhouette. The P2 reference changes jacket to muted violet, trousers to slate-blue and ankle wraps to amber. The identity board fixes the face, costume seams and palette intent for subsequent production work.

## Verified limitations

The three action boards contain **baked checkerboard backgrounds**. Read-only PNG inspection reports `Format24bppRgb` for every image; no image has a real alpha channel. A targeted built-in background-extraction edit also failed and is retained separately as `universal-alpha-attempt.png`. The original universal board remains preferred.

The normals board's proximity cells 19 (`close_mp`) and 20 (`close_hp`) crop the lower legs. Supplemental cells 21 and 22 now supply complete-body alternatives with both feet visible. The new close-MP is a straight punch rather than the prompted elbow, so its final silhouette still needs choosing. Original cell 11 (`c_mk`) extends the heel left, and back-throw direction needs clearer choreography. The cell names document intended design coverage; they do not certify finished motion or hitbox alignment.

The supplemental sheet deliberately uses an opaque gray matte, with minor pixel variation and no checkerboard. It still needs an alpha mask. Its 24 distinct cells were reviewed in order: turn, crouch down/up, jump rise, super jump, air parry, red high/low parry, low/air hit, throw victim/tech, knockdown fall, wakeup, dizzy, taunt, intro, win, defeat, draw, close MP/HP, high hit and quickrise. All complete figures fit their cells. The short swept hair, teal jacket and ivory trousers remain consistent with Vale's identity board.

Before swapping into the prototype, create alpha masks, normalize the pixel grid/scale/pivots, redraw incomplete silhouettes, split body and effects, produce timing-correct startup/active/recovery and common-state animation, and derive the P2 palette. Canonical move numbers and behavior remain authoritative.

## Provenance

Built-in image generation only. No code or Python was used to draw or modify raster art. Files were copied from the generated-image directory without modifying their pixels. Every image was visually reviewed; dimensions and RGB format were inspected through System.Drawing. Exact original prompts, including the failed alpha edit, are in `prompts.md`; the later additive sheet's exact prompt is in `supplemental-prompt.md`.
