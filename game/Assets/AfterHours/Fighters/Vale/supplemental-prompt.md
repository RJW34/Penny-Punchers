# supplemental runtime matte prompt

Built-in image_gen. Preserve original source; selected output uses shader keying after genuine-alpha extraction failed.

Use case: precise-object-edit
Asset type: shipping chroma-key sprite atlas for original Vale fighter.
Edit target: provided source board. Change background ONLY to perfectly uniform saturated opaque MAGENTA #FF00FF(RGB255,0,255). Remove every checker square/gray background pixel around and within silhouettes. NO transparency checkerboard, NO gray or white matte, NO gradient/texture/floor/shadow. This deliberate flat magenta is required for runtime shader. Preserve foreground ivory trousers, warm brown skin, teal one-sleeved cropped jacket, charcoal bodysuit, short silver-black hair, ink outlines, every full-body pose, exact layout and scale. Crisp original32-bit pixel-art figures. No text/borders/palette chips. Do not redraw or relight the fighter.
Canvas1086x1448, EXACT4 columns6 rows24 cells, preserve all source supplemental pose order and complete heads/feet. Source has gray background; every gray background area must become pure flat magenta including gaps. Keep tiny teal/red/amber actual parry/launch/dizzy sparks. Keep all corrected full-body close attacks in final row. No opponents.
