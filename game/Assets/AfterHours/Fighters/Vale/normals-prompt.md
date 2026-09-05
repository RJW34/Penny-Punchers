# normals runtime matte prompt

Built-in image_gen. Preserve original source; selected output uses shader keying after genuine-alpha extraction failed.

Use case: precise-object-edit
Asset type: shipping chroma-key sprite atlas for original Vale fighter.
Edit target: provided source board. Change background ONLY to perfectly uniform saturated opaque MAGENTA #FF00FF(RGB255,0,255). Remove every checker square/gray background pixel around and within silhouettes. NO transparency checkerboard, NO gray or white matte, NO gradient/texture/floor/shadow. This deliberate flat magenta is required for runtime shader. Preserve foreground ivory trousers, warm brown skin, teal one-sleeved cropped jacket, charcoal bodysuit, short silver-black hair, ink outlines, every full-body pose, exact layout and scale. Crisp original32-bit pixel-art figures. No text/borders/palette chips. Do not redraw or relight the fighter.
Canvas1086x1448, EXACT4 columns6 rows24 cells, preserve source pose order. In last row cells3 and4 ONLY, REMOVE the separate amber dummy opponent silhouettes and replace their pixels with the same flat magenta; keep Vale's complete body and throw pose intact. Do not add human silhouettes anywhere. Cropped close attacks in row5 remain compositionally unchanged; they will use supplemental full-body alternatives at runtime.
