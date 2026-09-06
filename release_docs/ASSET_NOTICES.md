# Asset and software notices

## Original project material

Thomas and Vincent, their costumes, Foundry Ring, Calibration Grid, combat effects, portraits and interface artwork are original project material. Older source filenames use `rook` and `vale`. The supplied After Hours boards were created with the built-in OpenAI image-generation tool. Working fighter sheets were edited with that tool to create a removable magenta matte; the runtime chroma-key shader removes it. These sheets remain RGB rather than technically validated transparent RGBA. Compatible key poses are reused where independently authored in-betweens do not exist.

Marist Green — Golden Hour and Marist Gates — Blue Hour use the supplied original generated panoramas and selection cards. The artwork is an illustrated interpretation of campus architecture, not an architectural survey or historical record. Reference photographs remain owned by their rightsholders and are excluded from this source import and runtime package. Marist names identify the depicted places; no university, architect or artist endorsement is claimed. Source attribution remains in each stage's `SOURCES.md` under `design/after-hours-32bit/stages/`.

Original boards, prompts, fingerprints and rejected experiments are preserved in `design/after-hours-32bit`. Runtime images, measured rectangles, pivots and animation mappings are in `game/Assets/AfterHours`. `assets/MARIST_STAGES.json` records source/runtime hashes; `assets/BUYABLE_PRESENTATION_BINDINGS.json` distinguishes reused poses from new mechanics. Runtime cropping preserves the supplied image bytes and aspect. Simulation alone owns movement, contacts, timing, product ownership and settlement.

The audio is original deterministic oscillator/noise synthesis: 24 PCM files, including effect variations and stage loops. Reproduction source is `assets/synthesize_audio.py`; shipped WAV files are under `game/Presentation/Audio`. Synthesized breaths are noise cues, not recorded human performances. No external music recordings, sampled voices, commercial game sprites, franchise stages or ROM material are included.

`assets/ASSET_REGISTER.csv` and the more recent specialized asset manifests record sources and methods. Synthetic presentation fixtures under `assets/presentation_checks` are development checks; they do not by themselves prove gameplay or physical playback quality.

## Barlow UI fonts

The UI uses unmodified **Barlow Medium** and **Barlow Condensed SemiBold**. Copyright 2017 The Barlow Project Authors. Both are distributed under the **SIL Open Font License 1.1**; the unmodified license is included as `licenses/BARLOW_OFL.txt` and alongside the source fonts in `game/Presentation/Fonts/OFL.txt`. Source URLs and exact hashes are recorded in `assets/UI_FONT_SOURCES.json`. These are openly licensed fonts, not copied proprietary system fonts.

## Godot Engine 4.6.3

Godot Engine is distributed under the MIT license. Copyright (c) 2014-present Godot Engine contributors; copyright (c) 2007-2014 Juan Linietsky and Ariel Manzur.

The unmodified license and third-party notices from the exact `4.6.3-stable` source tag are included in `licenses/GODOT_LICENSE.txt` and `licenses/GODOT_COPYRIGHT.txt`. Primary sources: [Godot's tagged license](https://github.com/godotengine/godot/blob/4.6.3-stable/LICENSE.txt) and [copyright list](https://github.com/godotengine/godot/blob/4.6.3-stable/COPYRIGHT.txt).

Godot's embedded Noto Sans fallback material is copyright 2012 Google Inc. and distributed under SIL OFL 1.1. Its license remains in `licenses/NOTO_OFL.txt` and the engine copyright notice. The current game UI primarily uses the Barlow fonts described above.

## .NET runtime and distribution

The application uses .NET. Upstream Microsoft/.NET Foundation license and third-party notices remain in `licenses/DOTNET_LICENSE.txt` and `licenses/DOTNET_THIRD_PARTY_NOTICES.txt`. Preserve runtime notices when redistributing an authorized package.

No public license for the original game code or artwork is selected by these third-party notices. The source repository's `RIGHTS.md` records its private development/audit status. These notices imply no endorsement of Penny Punchers by the engine, font, runtime or depicted-location authors.
