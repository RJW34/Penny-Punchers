# techniques runtime matte prompt

Built-in image_gen. Preserve original source; selected output uses shader keying after genuine-alpha extraction failed.

Use case: precise-object-edit
Asset type: shipping chroma-key sprite atlas for original Vale fighter.
Edit target: provided source board. Change background ONLY to perfectly uniform saturated opaque MAGENTA #FF00FF(RGB255,0,255). Remove every checker square/gray background pixel around and within silhouettes. NO transparency checkerboard, NO gray or white matte, NO gradient/texture/floor/shadow. This deliberate flat magenta is required for runtime shader. Preserve foreground ivory trousers, warm brown skin, teal one-sleeved cropped jacket, charcoal bodysuit, short silver-black hair, ink outlines, every full-body pose, exact layout and scale. Crisp original32-bit pixel-art figures. No text/borders/palette chips. Do not redraw or relight the fighter.
Canvas1254x1254, EXACT4 columns4 rows16 cells, preserve source pose order. Preserve all attached teal motion arcs and compact super auras. Remove ONLY detached standalone projectile rings floating separately beyond the hands in row1cell1, row1cell2, row2cell4, row3cell2, replacing those disconnected projectiles with flat magenta. Actual runtime projectile effects are rendered separately. Keep palms, body and attached motion trail shapes undisturbed.
