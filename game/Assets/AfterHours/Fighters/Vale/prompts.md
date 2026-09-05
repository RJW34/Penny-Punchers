# Vale runtime asset prompts

Built-in image_gen only. Original source boards are preserved unchanged.

## Universal alpha retry (failed: RGB, checkerboard remains)

Use case: background-extraction
Asset type: shipping runtime sprite atlas for original fighter Vale, exact16 separate full-body sprites in4x4 grid.
Edit target: attached universal.png. Remove ALL baked white/light-gray checkerboard from around and inside the16 silhouettes. Output a genuine RGBA PNG with alpha=0 for every background pixel. The final asset must composite against black, magenta, or arbitrary game scenery without a checkerboard rectangle. DO NOT draw a checkerboard to represent transparency. Transparent background required, no replacement background.
Preserve exactly all16 poses and their row-major4x4 order, placement/framing, all head/hair/hands/feet, athletic adult female identity, medium brown skin, short swept silver-black hair, one-sleeved teal utility jacket, charcoal bodysuit, segmented ivory wide-leg trousers, teal ankle wraps, dark shoes, crisp outlines, deliberate32-bit pixel clusters. Keep opaque ivory trousers; remove background visible through gaps between legs/arms and around teal parry sparks. No floor or drop shadows, no text. Do not add opponents or change stance. Preserve original dimensions1254x1254 if possible. Foreground only, transparent-alpha file, no rendered matte.

## Universal flat matte (selected runtime image)

Use case: precise-object-edit
Asset type: production chroma-key sprite atlas, original Vale4x4 universal poses.
Edit target: universal.png. Change ONLY the entire background. Replace every light white/gray checkerboard square with one flat, highly saturated solid MAGENTA color #FF00FF (RGB255,0,255). Every surrounding/background pixel including enclosed holes between limbs must be this exact SAME MAGENTA. This opaque magenta is deliberate and required for the runtime key shader. NO transparency checkerboard, NO gray, NO white matte, NO texture, NO gradient, NO floor, NO shadows.
Keep all16 original character poses, row-major grid order, canvas1254x1254, positions, scale, full body boundaries, face, short silver-black hair, brown skin, one-sleeved teal jacket, charcoal bodysuit, ivory segmented trousers, teal ankle wraps and shoes EXACTLY as source. Keep foreground white/ivory clothing and black contours intact. Preserve tiny teal parry sparks. Do not redraw or improve or alter poses. NO extra characters, labels or borders. Flat opaque #FF00FF background behind the existing sixteen clean pixel-art figures.
