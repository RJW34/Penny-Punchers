# Penny-Punchers

The complete source repository for the native fighting game currently named **Strike Ledger** in its executable, namespaces and artwork. This repository starts from the implemented After Hours graphics build and preserves its development history.

Start with [AUDIT_GUIDE.md](AUDIT_GUIDE.md) for architecture, review priorities, character expansion, UI/settings entry points and verification boundaries. [Fresh-clone verification](audit/import-2026-09-05/verification.json) records a successful game build/import, 15 seed checks, 36 production Core scenarios and 10 application suites, with actual logs.

The repository contains the C# simulation and application layers, Godot project, all runtime artwork/audio, original art boards and generation prompts, canonical data, tests, build tools and design documents. A fresh clone contains source; build outputs and machine-specific tool installations are generated locally.

An original native six-button 1v1 fighter. Rook uses motion inputs; Vale uses charge and spacing. One credit wallet pays for EX attacks, the selected super, and optional one-round leases.

## Play

After exporting a build, open **Play Strike Ledger.cmd** or **dist/StrikeLedger/windows/StrikeLedger.exe**. The Linux executable is **dist/StrikeLedger/linux/StrikeLedger.x86_64**. Keep each exported directory together; the executable needs its PCK and application data directory. `python tools/package_release.py` packages existing exports into ZIPs in `dist/`.

Choose **Versus CPU** to play a complete match, **Local versus** for two players, or **Training lab** to practice. Keyboard: **WASD** movement, **U I O / J K L** punches/kicks, **P / semicolon** PP/KK chords. Menus use arrows, Enter and Esc. Controllers can be assigned and remapped in Settings.

See **release_docs/CONTROLS.md** for the full controls, **release_docs/README.md** for rules, and **release_docs/KNOWN_LIMITATIONS.md** for the precise verification boundaries.

## After Hours graphics

The completed 32-bit-style artwork now supplies Rook and Vale sprites, the foundry and calibration-room stages, combat effects, portraits, menus, lease icons and the live HUD. Production assets are in `game/Assets/AfterHours`; the original design boards remain in `design/after-hours-32bit`. See `reports/AFTER_HOURS_INTEGRATION.md` for the exact mapping, generation prompts and new native verification.

## Build and verify

Use Godot **.NET 4.6.3**, .NET SDK **8**, and Python **3.11+**. Exporting additionally requires matching Godot .NET export templates. The original build used SDK 8.0.424; these are reproducibility pins, not claims about the latest supported versions. Initial NuGet restore needs network access.

Put `dotnet` and `godot` on PATH, set `DOTNET_BIN` and `GODOT_BIN`, or create an untracked `.local_tools.json` containing their full executable paths:

```json
{
  "dotnet": "C:/Tools/dotnet/dotnet.exe",
  "godot": "C:/Tools/Godot/Godot_v4.6.3-stable_mono_win64_console.exe"
}
```

From the repository root:

```
python -m pip install -r requirements-tools.txt
python tools/build.py --test
python tools/build.py --export
python tools/run_scenario.py --list
python tools/run_scenario.py --scenario exact_credit_ex --seed 1
python tools/run_scenario.py --scenario loopback_match --network-matrix
```

`build.py` regenerates `game/GeneratedData` from tracked `data/`, builds the game, imports resources, and runs the C# suites when `--test` is supplied. For direct Core/App test commands and optional art/media dependencies, see the audit guide. Preserve `.gitattributes` so canonical content bytes remain stable across checkouts.

## Audit evidence and history

The imported playable baseline is commit `ab951b790fdf657d7e32fe6f9c16ede31855a77c`. Its source, art and canonical data remain intact. Repository onboarding documentation is newer; filenames and reports referring to Strike Ledger or the initial scaffold retain their historical context.

The original verification passed 76/76 software requirements. Physical local-controller sessions, a match across two physical LAN computers, and owner/player feedback remain unperformed. These are historical results for the recorded candidate, not automatic certification of future changes.

The [repository releases](https://github.com/RJW34/Penny-Punchers/releases) provide `Penny-Punchers-audit-evidence.zip`, containing the exact files referenced by the baseline acceptance ledger, with a hash inventory and reproduction instructions. Large recordings, runtime files and generated evidence are kept out of the source Git tree. A source-only clone cannot pass that historical evidence gate until the companion artifacts are restored. To inspect the frozen baseline independently of later changes:

```text
git worktree add ../Penny-Punchers-baseline ab951b790fdf657d7e32fe6f9c16ede31855a77c
```

Extract the evidence ZIP into that worktree, then run `python tools/release_gate.py --tier software`. The `--tier all` command remains nonzero for the three external checks. Build/test commands above can run without this evidence download.

The original PACK_MANIFEST.json describes the preserved input scaffold and is historical after development.

Production simulation is in src/StrikeLedger.Core, bots/replays/training/networking in src/StrikeLedger.App, and the native game, After Hours bitmap presentation and original synthesized audio in game. Canonical numeric data is under data. Tests use the production core and are separate from the initial Python arithmetic oracle.

Candidate identity, exact evidence and continuation state are in reports/RELEASE_CANDIDATE.json, reports/ACCEPTANCE_RESULTS.json and reports/RESUME_PACKET.md. No franchise assets or earlier game source were imported.
