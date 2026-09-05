# Asset and software notices

## Original project material

Rook, Vale, their costumes, the foundry and calibration room, combat effects, portraits and interface art are original project material. The After Hours source boards were created with the built-in OpenAI ImageGen tool, with art direction by Codex for this project. Eight working fighter sheets were edited with the same built-in tool to replace baked backgrounds with a magenta matte; a runtime shader removes that matte. Stage, effect, portrait and interface crops preserve the supplied source image bytes. No commercial game sprites, characters, stages, recordings, samples or ROM material were supplied as sources or included.

The original boards are preserved in the source package under `design/after-hours-32bit`. Shipped bitmap resources and explicit animation mappings are under `game/Assets/AfterHours`. Each fighter folder records prompts, selected output fingerprints, measured rectangles and root pivots. The artwork is a 32-bit arcade visual style; this does not imply all source PNGs have an alpha channel. Startup/contact/recovery timelines reuse compatible authored key poses where the boards do not contain separate in-between cels. The simulation alone owns move timing, hitboxes and spending.

The 15 effect cues and the 20-second Foundry music loop are original deterministic oscillator/noise synthesis. Their reproducible source is `assets/synthesize_audio.py`; shipped WAV files are under `game/Presentation/Audio/`. There are no sampled voices or external music recordings.

The source package's `assets/ASSET_REGISTER.csv` identifies each shipped art/audio resource and its method. Synthetic presentation fixtures under `assets/presentation_checks` are developer render checks, not gameplay evidence or shipped game artwork.

## Godot Engine 4.6.3

Godot Engine is distributed under the MIT license. Copyright (c) 2014-present Godot Engine contributors. Copyright (c) 2007-2014 Juan Linietsky and Ariel Manzur.

The unmodified license and third-party notices from the exact `4.6.3-stable` source tag are included in `licenses/GODOT_LICENSE.txt` and `licenses/GODOT_COPYRIGHT.txt`. Primary source: [Godot's tagged license](https://github.com/godotengine/godot/blob/4.6.3-stable/LICENSE.txt) and [third-party copyright list](https://github.com/godotengine/godot/blob/4.6.3-stable/COPYRIGHT.txt).

## Embedded default font

The game uses Godot's `ThemeDB.FallbackFont`, with no separately copied system font. Godot's Noto Sans font material is copyright 2012 Google Inc. and licensed under SIL Open Font License 1.1. The unmodified font license is included in `licenses/NOTO_OFL.txt`; font attribution is also preserved in `GODOT_COPYRIGHT.txt`. Primary source: [Godot's tagged Noto license](https://github.com/godotengine/godot/blob/4.6.3-stable/thirdparty/fonts/LICENSE.Noto.txt).

## .NET runtime

The application uses .NET. Its upstream Microsoft/.NET Foundation license and third-party notices are included in `licenses/DOTNET_LICENSE.txt` and `licenses/DOTNET_THIRD_PARTY_NOTICES.txt`. Preserve the runtime's accompanying notices if redistributing the application package.

These notices do not imply endorsement of Strike Ledger by the engine, font or runtime authors.
