# Penny-Punchers

An original native six-button 1v1 fighting game. **Thomas** uses motion inputs; **Vincent** uses charge and spacing. The round shop sells optional rentals, repeatable EX licenses and one-use super permits. Your saved bank stays fixed during combat; capped skill awards deposit at the next settlement. The application is Penny Punchers; legacy executable and C# namespace names remain StrikeLedger for build compatibility.

Start with [AUDIT_GUIDE.md](AUDIT_GUIDE.md) for architecture, review priorities, character expansion, UI/settings entry points and verification boundaries. [Fresh-clone verification](audit/import-2026-09-05/verification.json) records the earlier imported baseline build and its tests. Those logs are historical; the v2 release requires its own current-source checks.

The repository contains the C# simulation and application layers, Godot project, all runtime artwork/audio, original art boards and generation prompts, canonical data, tests, build tools and design documents. A fresh clone contains source; build outputs and machine-specific tool installations are generated locally.

The active rules are [Shop-Only Economy Rework v2](docs/21_SHOP_ONLY_V2.md), incorporating the earlier audit and Buyables mechanics. See [UPGRADE_STATUS.md](reports/UPGRADE_STATUS.md) for current evidence and remaining gates. Both playable trials use the same shop-only economy; neither restores combat spending.

| Library | Rentals | EX products | Super permits | Runtime action nodes, including branches |
|---|---:|---:|---:|---:|
| Core trial | 12 | 8 | 6 | 100 |
| Expanded trial | 24 | 8 | 6 | 115 |

Counts span both fighters. Each fighter has four EX choices and three supers; Expanded replaces the third super with Thomas's Overtime or Vincent's Prism Lattice. Choose fighters, stage and trial in the lineup, then purchase the round's kit in the shop. The catalog and free-kit practice comparisons are untimed.

A match starts at **600 CR**. A cart can contain three optional rental slots, up to two EX licenses and at most one super permit, spending at most **2400 CR**. EX licenses cost **600 CR**, or **900 CR** for the vertical reversal, and repeat after ordinary recovery. Supers cost **900/1200/1500 CR** by registry and grant one legal startup. All purchases expire after the round; buying nothing keeps the complete free base kit. There is no reserve floor or combat debit.

Confirmed counter-hits, grounded anti-airs and precision parries can earn **50/75/100 CR for the next shop**, subject to eligibility, twice-per-category and 300-total round caps. Settlement adds result income and skill income with explicit clipping at the **3600 CR** bank cap. See the player rules for timing and payout details.

## Play

After exporting a build, open **Play Strike Ledger.cmd** or **dist/StrikeLedger/windows/StrikeLedger.exe**. The Linux executable is **dist/StrikeLedger/linux/StrikeLedger.x86_64**. Keep each exported directory together; the executable needs its PCK and application data directory. `python tools/package_release.py` packages existing exports into ZIPs in `dist/`.

Choose **Versus CPU** to play a complete match, **Local versus** for two players, or **Training lab** to practice. Keyboard: **WASD** movement, **U I O / J K L** punches/kicks, **P / semicolon** PP/KK chords. Menus use arrows, Enter and Esc. Controllers can be assigned and remapped in Settings.

See **release_docs/CONTROLS.md** for the full controls, **release_docs/README.md** for rules, and **release_docs/KNOWN_LIMITATIONS.md** for the precise verification boundaries.

## After Hours graphics

The supplied 32-bit-style artwork supplies Thomas and Vincent sprites, portraits, effects, menus and the live HUD. Four stages are available: Foundry Ring, Calibration Grid, Marist Green — Golden Hour and Marist Gates — Blue Hour. Production assets are in `game/Assets/AfterHours`; the original design boards remain in `design/after-hours-32bit`. See `assets/PRESENTATION_UPGRADE.md` for current mappings and art limitations; `reports/AFTER_HOURS_INTEGRATION.md` describes the historical import.

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
dotnet run --project src/StrikeLedger.CoreTests/StrikeLedger.CoreTests.csproj -c ExportRelease -- --data data --evidence-dir reports/evidence/current-core

dotnet run --project src/StrikeLedger.CoreTests/StrikeLedger.CoreTests.csproj -c ExportRelease -- --scenario shop_all_ex_repeat_without_bank --data data --evidence-dir reports/evidence/current-ex

dotnet run --project src/StrikeLedger.NetworkLab/StrikeLedger.NetworkLab.csproj -c ExportRelease -- --self-test --data data --evidence-dir reports/evidence/current-app
```

`build.py` regenerates `game/GeneratedData` from tracked `data/`, builds the game, imports resources, and runs the C# suites when `--test` is supplied. Repeat the direct Core/App commands with `--data data/rulesets/buyables_full` and a different evidence directory for Expanded. `shop_registry_resource_contract`, `shop_all_super_once_rollback` and App `--scenario shop_only_v2` provide focused v2 checks. Legacy scenario names in older runners and reports are not proof of current catalog coverage. For optional art/media dependencies, see the audit guide. Preserve `.gitattributes` so canonical content bytes remain stable across checkouts.

## Audit evidence and history

The imported playable baseline is commit `ab951b790fdf657d7e32fe6f9c16ede31855a77c`. Its historical source, art and canonical data remain available in Git history; the current checkout has subsequently changed. Repository onboarding documentation is newer; filenames and reports referring to Strike Ledger or the initial scaffold retain their historical context.

The original verification passed 76/76 software requirements. Physical local-controller sessions, a match across two physical LAN computers, and owner/player feedback remain unperformed. These are historical results for the recorded candidate, not automatic certification of future changes.

The [repository releases](https://github.com/RJW34/Penny-Punchers/releases) provide `Penny-Punchers-audit-evidence.zip`, containing the exact files referenced by the baseline acceptance ledger, with a hash inventory and reproduction instructions. Large recordings, runtime files and generated evidence are kept out of the source Git tree. A source-only clone cannot pass that historical evidence gate until the companion artifacts are restored. To inspect the frozen baseline independently of later changes:

```text
git worktree add ../Penny-Punchers-baseline ab951b790fdf657d7e32fe6f9c16ede31855a77c
```

Extract the evidence ZIP into that worktree, then run `python tools/release_gate.py --tier software`. The `--tier all` command remains nonzero for the three external checks. Build/test commands above can run without this evidence download.

The original PACK_MANIFEST.json describes the preserved input scaffold and is historical after development.

Production simulation is in src/StrikeLedger.Core, bots/replays/training/networking in src/StrikeLedger.App, and the native game, After Hours bitmap presentation and original synthesized audio in game. Canonical numeric data is under data. Tests use the production core and are separate from the initial Python arithmetic oracle.

Candidate identity, exact evidence and continuation state are in reports/RELEASE_CANDIDATE.json, reports/ACCEPTANCE_RESULTS.json and reports/RESUME_PACKET.md. No franchise assets or earlier game source were imported. Physical controllers, two physical PCs and owner/friend acceptance remain separate checks. Existing sprite poses, RGB chroma-key cleanup and heuristic palette masks have documented limitations; no new fully authored animation set or genuine RGBA conversion is claimed.
