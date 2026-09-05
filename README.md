# Strike Ledger

An original native six-button 1v1 fighter. Rook uses motion inputs; Vale uses charge and spacing. One credit wallet pays for EX attacks, the selected super, and optional one-round leases.

## Play

On this PC, open **Play Strike Ledger.cmd** or **dist/StrikeLedger/windows/StrikeLedger.exe**. The Windows and Linux ZIP packages are in **dist/**. Keep each extracted package together; the executable needs its PCK and application data directory.

Choose **Versus CPU** to play a complete match, **Local versus** for two players, or **Training lab** to practice. Keyboard: **WASD** movement, **U I O / J K L** punches/kicks, **P / semicolon** PP/KK chords. Menus use arrows, Enter and Esc. Controllers can be assigned and remapped in Settings.

See **release_docs/CONTROLS.md** for the full controls, **release_docs/README.md** for rules, and **release_docs/KNOWN_LIMITATIONS.md** for the precise verification boundaries.

## After Hours graphics

The completed 32-bit-style artwork now supplies Rook and Vale sprites, the foundry and calibration-room stages, combat effects, portraits, menus, lease icons and the live HUD. Production assets are in `game/Assets/AfterHours`; the original design boards remain in `design/after-hours-32bit`. See `reports/AFTER_HOURS_INTEGRATION.md` for the exact mapping, generation prompts and new native verification.

## Build and verify

Install matching Godot.NET 4.6.3/editor export templates and .NET SDK 8. Set DOTNET_BIN and GODOT_BIN, or create an untracked .local_tools.json containing their executable paths. Python 3.11+ and requirements-tools.txt support content validation and tooling.

```
python tools/build.py --export --test
python tools/run_scenario.py --list
python tools/run_scenario.py --scenario exact_credit_ex --seed 1
python tools/run_scenario.py --scenario loopback_match --network-matrix
python tools/release_gate.py --tier software
python tools/release_gate.py --tier all
```

The all-tier gate deliberately remains nonzero while actual device or human checks are pending. The gate checks evidence integrity; read the underlying reports to assess the game. The original PACK_MANIFEST.json describes the preserved input scaffold and is historical after development.

Production simulation is in src/StrikeLedger.Core, bots/replays/training/networking in src/StrikeLedger.App, and the native game, After Hours bitmap presentation and original synthesized audio in game. Canonical numeric data is under data. Tests use the production core and are separate from the initial Python arithmetic oracle.

Candidate identity, exact evidence and continuation state are in reports/RELEASE_CANDIDATE.json, reports/ACCEPTANCE_RESULTS.json and reports/RESUME_PACKET.md. No franchise assets or earlier game source were imported.
