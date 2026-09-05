# Rook runtime sprites

`atlas.json` selects 79 full-body cells from four generated pose sheets, maps all 49 canonical Rook moves to startup/active/recovery cel arrays, and provides 60 universal state keys and renderer aliases. Coordinates are explicit rectangles with local foot pivots. The original boards remain unchanged under `design/after-hours-32bit/fighters/rook`.

These PNGs use a **magenta chroma matte**, removed by the runtime sprite shader. They are opaque RGB files, not transparent PNGs. A built-in imagegen extraction request returned another baked checkerboard without an alpha channel; that unsuccessful output was preserved outside the runtime assets at `tools/art/rook-provenance/universal-alpha-failed.png`. The authorized fallback uses generated magenta backgrounds and records `chroma_key: [1, 0, 1]` in the atlas. The generated matte varies slightly, so key magenta hue/dominance or use the declared tolerance instead of exact RGB equality. Keep nearest-neighbor texture filtering.

The runtime selection excludes the cropped `normals.close_hp` reference and uses the complete-foot `supplemental.close_hp_alternative`. Tight pulse, EX pulse and first-super rectangles exclude detached projectile rings; actual moving projectiles belong to the renderer. Attached uppercut trails and defensive sparks remain cosmetic parts of their active cels.

Animation uses explicit authored key poses selected by gameplay phase. Strength variants share family poses and anticipation/recovery poses are deliberately reused. The sheets do not contain independently drawn inbetweens for every simulation tick. The Core supplies every gameplay duration; this asset pack changes no timing, collision, economy or input rule.

All four sheets were edited through the built-in imagegen tool. Their exact prompt set is in `prompts/`, with source/generated hashes and channel verification in `provenance.json`. No API/CLI fallback or raster postprocessing was used. Generated PNGs were copied byte-for-byte into the workspace.

Run `python tools/art/rook_metadata.py` from the repository root to reproduce the atlas and validation record. This tool authors JSON and reads PNG pixels only. It checks all move IDs, cell references, pivots, bounds, nonempty foreground and clear crop borders. It never edits or resamples a PNG.
