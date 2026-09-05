# Vale image-generation prompts

All images were made with the built-in `image_gen` tool. Exact prompts are preserved below. `identity.png` was generated from text only; the remaining original boards used it as the identity/style reference. The final alpha attempt edited `universal.png` after local visual inspection.

## identity.png

```text
Use case: stylized-concept
Asset type: original 32-bit arcade fighter identity, portrait and palette design sheet for the traditional fighting game AFTER HOURS.
Primary request: Create one polished 1536x1024 landscape pixel-art identity board for VALE, an original adult unarmed human fighter. Homage only to late-1990s 32-bit arcade craft: confident expressive anatomy, carefully placed crisp pixel clusters, strong dark 2-pixel outlines, dramatic but clean 4-tone shading, colorful reflected highlights. Do not resemble or copy any existing commercial fighting-game character.
Identity invariant: tall athletic medium-brown-skinned woman with long legs; short swept silver-black hair and angular focused face. Asymmetric cropped teal utility jacket with one folded collar over fitted charcoal sleeveless body suit, ivory wide-leg trousers made of distinctive rectangular articulated fabric panels, teal ankle wraps, flat dark lightweight footwear. No armor, no weapon, no gloves, no logo. Graceful spacing specialist with open palms, rooted feet and angular elbows.
Composition: quiet solid ink-navy (#111923) background, no scenic objects. Divide entire board into equal 4 columns by 2 rows, eight precisely separated cells, no drawn cell borders, all art centered with ample gutters. Top-left cell is entire full-body primary P1 design in relaxed open-palm stance facing right, top-second cell entire full-body alternative P2 with muted violet jacket, cream trim, slate-blue trousers and amber ankle wraps, otherwise identical. Top-third cell large neutral bust portrait facing camera at three-quarter angle. Top-fourth cell large intense versus bust portrait with open hand raised.
Bottom-left cell large win portrait with small composed smile and fist gently against heart; bottom-second cell large lose portrait with eyes lowered, fatigued yet dignified, a minor dust mark only, no blood; bottom-third cell full-body rear three-quarter clothing turnaround P1; bottom-fourth cell two neat rows of flat square palette chips, one teal/ivory/navy/warm skin palette, one violet/cream/slate/skin palette.
Keep face, hair, body proportions and costume details consistent across all depictions. Each full body fully visible head to toe and sharply resolved; portraits visibly expressive and useful for game interfaces. Render visible intentional large pixel clusters at a coherent arcade sprite scale. Opaque navy board is intentional here.
Color palette: ink navy #111923, amber #EDAA54, cream #F4E5C5, teal #69B8AC; warm walnut skin and desaturated violet alternate.
Text: none at all. No letters, captions, signatures, grids, watermark, logos, interface, mockup frame.
Avoid: smooth digital painting, vector look, 3D, blur, soft antialiasing, gradients, paper grain, noisy pixels, tiny crowded figures, repeated indistinguishable expressions.
```

## universal.png

```text
Use case: stylized-concept
Asset type: original fighting-game universal movement and defense sprite DESIGN KEY POSE atlas, not final animation.
Input image: reference image is the Vale identity board. Use it to preserve exact face, hair, costume, body proportions and rendering language. Generate a new action atlas.
Create one 1536x1536 square atlas with exactly FOUR COLUMNS by FOUR ROWS, exactly sixteen separate full-body poses of the SAME adult female fighter. Strict evenly spaced invisible 4x4 grid, ample empty gutters, center each figure within its own equal cell; never let body parts or effects cross cell boundaries. Entire head/hands/feet always visible. Cell sizes equal; consistent head size and coherent scale throughout. Absolutely no text, labels, numbers, lines or palette swatches.
VALE identity: tall athletic medium-brown-skinned woman, long powerful legs, short swept silver-black hair. Teal asymmetric cropped utility jacket with one long sleeve and other bare arm, fitted charcoal body suit, ivory segmented wide-leg fabric trousers, teal ankle wraps, flat dark footwear. Original unarmed human fighter. Facial expression composed and alert. Except for tumbling, she faces screen RIGHT. Open-palm spacing and redirection martial-arts posture.
Rows and columns are in this EXACT left-to-right top-to-bottom order:
Row 1: (1) grounded neutral stance, left open palm forward and rear palm at chest, knees soft; (2) walking forward, lead foot stepping right; (3) walking backward, rear foot stepping left while eyes and palms still face right; (4) low forward dash, body leaning sharply right, both arms close.
Row 2: (5) retreating backdash, torso leaning left and front hand guarding right, feet briefly off floor; (6) compact deep crouch with both open hands guarding; (7) jump takeoff, compressed body pushing toes off ground; (8) ascending jump apex with legs tucked and open palms poised.
Row 3: (9) descending jump with both legs reaching toward landing; (10) low landing compression with one open hand balancing beside knee; (11) standing high block with forearms crossed above upper chest; (12) crouching low block with one palm guarding shin and other guarding face.
Row 4: (13) high parry, one decisive extended palm at forehead height with tiny crisp teal square spark; (14) low parry in wide low lunge, palm pushed down-forward with tiny teal spark; (15) midsection hit reaction, torso arched back with one elbow raised, no blood; (16) fully grounded knockdown on side, legs folded naturally and head visibly turned toward viewer, no blood.
Style: exquisite 32-bit late-1990s arcade pixel sprites. Deliberate visible square pixel clusters, crisp 2px ink-navy outlines, readable 4-tone shading and selective amber highlights, no smooth painting. Use muted teal #69B8AC, cream #F4E5C5, ink-navy #111923 and warm brown skin. Return genuinely transparent background with alpha channel. Do not draw checkerboard, solid background, shadows, grid or staging floor. Only sixteen characters and two tiny parry sparks. No weapons, no copied existing franchise characters, no logos, no gradients, no blur, no antialiasing.
```

## normals.png

```text
Use case: stylized-concept
Asset type: original 32-bit traditional fighting-game normal attacks and throws DESIGN KEY POSE atlas.
Input image: Vale identity board is a character and palette reference, preserve the same original adult woman's face, hair, long-limbed proportions, cropped asymmetric jacket and segmented trousers. Generate a NEW action atlas.
Create one 1536x2048 portrait atlas. EXACTLY FOUR COLUMNS by SIX ROWS. Exactly TWENTY-FOUR distinct full-body fighter key poses, left-to-right row-major order below. The invisible grid cells are equal and regularly spaced; generous blank gutters; all fingers, feet, hair and clothing stay inside their own cell. No overlap between cells. Even head size across poses. Character faces RIGHT in all strikes. Grounded poses align to their own cell baseline; airborne poses float centrally.
Vale: athletic tall medium-brown-skinned adult woman, short swept silver-black hair, asymmetric teal cropped utility jacket with ONE full sleeve and other bare arm over charcoal fitted body suit, ivory wide-leg trousers with rectangular fabric panels, teal ankle wraps, dark flat footwear. Graceful open-palm spacing specialist, unarmed, composed angular face. Maintain exact outfit and identity from reference. Distinct attack silhouette in EVERY cell, especially clearly distinguish punches/palms from kicks.
EXACT row-major pose sequence:
Row 1: standing light punch: short forward lead open-hand jab at chest; standing medium punch: stronger straight rear-palm thrust with forward shoulder; standing heavy punch: fully extended long palm lunge; standing light kick: low quick front-foot toe tap.
Row 2: standing medium kick: straight waist-height front side kick; standing heavy kick: fully extended high roundhouse with sweeping torso; crouching light punch: compact crouch and short chest-height palm jab; crouching medium punch: deep crouch and longer low open-palm thrust.
Row 3: crouching heavy punch: low crouch driving one palm upward as an anti-air; crouching light kick: bent supporting knee, quick ankle-height toe kick; crouching medium kick: one hand near ground, long horizontal shin-level side kick; crouching heavy kick: near-ground wide sweeping heel with rotated torso.
Row 4: jumping light punch: tucked knees and small forward palm jab; jumping medium punch: forward-leaning airborne straight palm thrust; jumping heavy punch: airborne doubled downward palm strike; jumping light kick: tucked rear knee and compact forward knee-height toe kick.
Row 5: jumping medium kick: airborne long diagonal-down side kick; jumping heavy kick: airborne full horizontal scissor-shaped heavy heel kick; proximity medium punch: close compact forward elbow from bent arm; proximity heavy punch: turning upward palm with rear shoulder rotating forcefully.
Row 6: command Long heel: deeply planted front foot and extremely long chest-height heel extension right; universal Leap strike overhead: short forward hop with downward chopping open palm and raised knee; forward throw release: Vale turning right and releasing an amber single-color faceless training silhouette over her forward hip to right; back throw release: Vale twisting leftward and releasing an amber single-color faceless training silhouette over her rear hip to left, clearly distinct direction.
Throw silhouettes only in final two cells, simplified amber opponent with no costume details, all contained inside cells. No opponents in other cells.
Style: beautiful late-1990s 32-bit arcade pixel art, visible coarse pixel clusters and crisp angular edges, selective navy 2px contours, small disciplined 4-tone ramps, warm reflected light, no antialiasing or painterly softness. Palette teal #69B8AC, cream #F4E5C5, navy #111923, amber #EDAA54, warm brown skin.
Background: true transparent alpha if supported; NEVER paint checkerboard squares. If transparent output cannot be created, use completely uniform flat #F0A0D0 matte with NO texture or checkerboard, so later manual cutout is possible. Do not draw shadows or ground or cell borders.
Text: none. No letters, captions, numbers, signatures, watermarks, logos. No weapons, no franchise character references, no duplicates, no blurred motion trails. These are separate useful pose designs, not a looping animation strip.
```

## techniques.png

```text
Use case: stylized-concept
Asset type: original traditional fighting-game special-move, super-art and lease-technique DESIGN KEY POSE atlas.
Input reference: use Vale identity board to preserve exact fighter identity and costume, especially long-legged adult build and ONE long jacket sleeve, other arm bare. Generate a new action sheet.
Make one 1536x1536 square image, STRICT FOUR COLUMNS by FOUR ROWS. Exactly sixteen clearly separated full-body poses/effect studies, equal invisible grid cells and large empty gutters. Center every entire figure inside its cell; every head, finger, shoe and effect visible, no cropping at knees or ankles, no overlaps. Consistent figure head size. All attacks face RIGHT; she may move left while continuing to face right. No text at all, no labels, no numbers, no borders.
Vale: original tall athletic medium-brown-skinned adult woman, short swept silver-black hair, composed face, asymmetric cropped teal utility jacket with one long sleeve, charcoal fitted body suit, ivory wide-leg segmented fabric trousers with rectangular seams, teal ankle wraps and dark flat footwear. Unarmed open-palm and heel-based martial artist, controls teal pressure currents; no actual water, wind-fantasy effects rendered as angular pixel strokes.
EXACT LEFT-TO-RIGHT TOP-TO-BOTTOM CONTENT:
Row 1: (1) Charge pulse: planted low stance pushing both open palms right, one small angular teal ring projectile detached from hands; (2) Charge pulse EX: forceful forward step and double-palm release with amber-white core inside twin teal angular rings; (3) Rising heel: airborne vertical anti-air heel, one leg nearly vertical and other leg tucked with narrow teal ascending slash; (4) Rising heel EX: twisting upward split kick with angular amber outline and doubled teal rising slashes.
Row 2: (5) Turn palm: pivoting advancing open-palm strike, one heel turning on ground, extended palm and broad teal short arc; (6) Retreat heel: torso recoiling left while long raised heel thrusts right, a short teal heel trail; (7) Rising Current super: corkscrew airborne upward heel, three separated angular teal current spirals rise vertically around full-body figure; (8) Crosswind super: deep rooted two-palm push releasing large compact white-cored teal multi-ring projectile contained within own cell.
Row 3: (9) Tidal Step super: aggressive long forward stepping palm, two narrow staggered teal afterimage outlines behind body, angular groundward pressure arc; (10) Returning pulse lease: standing one-palm release of small teal ring with hook-shaped returning trail arcing back towards wrist; (11) Low palm lease: deep crouching forward open-palm strike at opponent shin height, small low teal contact wedge; (12) Heel arc lease: upright high overhead crescent heel dropping diagonally down-right with thin teal crescent.
Row 4: (13) Long check lease: exceptionally long extended horizontal open-palm poke with stretched lead shoulder and heel rooted; (14) Step feint lease: leaning forward into a convincing step, open palms withdrawn, one thin teal dash mark behind feet, NO hit spark; (15) Sway feint lease: shoulders leaning backward left, front foot planted and palms guarding, one thin teal reverse dash mark in front, NO hit spark; (16) dramatic super-freeze activation stance: wide planted feet and open hand raised near face, hard-edged small halo of floating amber ledger-like rectangles, full body visible, no written symbols.
Deliberate authentic late-1990s 32-bit arcade pixel craft, visibly squared pixel clusters, crisp 2-pixel ink-navy outlines, readable 4-tone shading, no smooth antialias. Teal #69B8AC, cream #F4E5C5, navy #111923, amber #EDAA54 and warm brown skin.
Background: genuine transparent alpha channel; do not paint checkerboard squares. If real alpha cannot be emitted use one perfectly uniform flat magenta #F0A0D0 background instead. No paper or texture. No floor, no shadows, no grid lines, no interface mockup, no brands or franchise references. Effects are compact, sharp and secondary to readable original character silhouettes.
```

## universal-alpha-attempt.png

```text
Use case: background-extraction
Asset type: existing 4x4 sixteen-pose Vale fighter sprite design atlas.
Edit target: the provided universal.png image. Make ONE precise change only: remove the entire baked white/light-gray checkerboard background and output genuine PNG transparency with an alpha channel. Every background checker square must become fully transparent alpha=0. Do not draw a replacement checkerboard. The image must composite cleanly over any chosen background.
Preserve invariants absolutely: all sixteen character poses, position of each pixel-art character within the 4x4 layout, entire framing, every body part, costume, face, silver-black hair, warm brown skin, teal jacket and ankle wraps, ivory segmented trousers, dark shoes, dark outlines, both teal parry sparks, image width and height, spacing, colors and pixel-art style. Remove only the background surrounding and between figure silhouettes including enclosed holes between limbs; keep foreground ivory clothing opaque. Do not redraw, relight, retouch or add details. No labels, no text, no floor, no shadows.
Return the edited atlas as a genuine transparent-alpha PNG cutout, not an image that visually simulates transparency.
```


