# Audit and development guide

**Penny-Punchers is the repository name.** The existing game, C# namespaces, executable names, save paths and internal project identity remain **Strike Ledger**. The current artwork is the After Hours raster revision. This guide describes existing code and proposed follow-up work; it does not announce new UI, characters or settings.

Read [AGENTS.md](AGENTS.md), [the product contract](docs/00_PRODUCT_CONTRACT.md), and [decisions and authority](docs/01_DECISIONS_AND_AUTHORITY.md) before changing behavior. Canonical numbers live in `data/`; the intended game remains strictly 1v1 with one credit wallet per player funding both EX and supers.

## Architecture map

| Area | Actual entry points | Audit focus |
| --- | --- | --- |
| Canonical content | `data/fighters/*.json`, `data/items.json`, `data/combat.json`, `data/physics.json`, `data/economy.json`, `schemas/` | Commands, move phases, boxes, prices, settlement and schema constraints. |
| Deterministic simulation | `src/StrikeLedger.Core/Simulation.cs`, `Simulation.Input.cs`, `Simulation.Combat.cs` | Fixed input ticks, action recognition, simultaneous contacts, defense, spending and round transitions. |
| State and serialization | `src/StrikeLedger.Core/SimulationState.cs`, `Simulation.Serialization.cs`, `GameContent.cs` | Every future-affecting field must survive snapshot/restore; content identity and ordering must stay stable. |
| Sessions and records | `src/StrikeLedger.App/Replay.cs`, `RollbackSession.cs`, `PrivateMatchPeer.cs`, `UdpTransport.cs` | Replay validation, corrected inputs, confirmed presentation events, reliable control messages and confirmed settlement. |
| Training and bots | `src/StrikeLedger.App/Training.cs`, `Bots.cs`; `src/StrikeLedger.BalanceLab/` | Training-only setup boundaries, actual input generation and measured bot experiments. |
| Godot shell | `game/Main.cs`, `Main.Menus.cs`, other `Main.*.cs` partials | Screen transitions, callbacks, preparation, lab/replay/network controls and lifecycle cleanup. |
| Input and preferences | `game/GameSettings.cs`, `Main.ControlRouting.cs` | Normalization, per-device mappings, seat assignment, remapping and held-button suppression. |
| Rendering and audio | `game/Main.Rendering.cs`, `Main.ArtSkin.cs`, `game/Presentation/` | Read-only render DTOs, cel phase selection, HUD, camera, effects, audio and resource shutdown. |
| Art provenance | `game/Assets/AfterHours/`, `design/after-hours-32bit/`, `assets/ASSET_REGISTER.csv` | Runtime atlases and pivots versus preserved source boards; see `release_docs/ASSET_NOTICES.md`. |
| Verification | `src/StrikeLedger.ContractTests/`, `src/StrikeLedger.CoreTests/`, `src/StrikeLedger.NetworkLab/`; `acceptance/`; `tools/` | Executable tests, scenario routing and evidence integrity. `reference/` is a Python oracle/structural harness, not the shipped combat engine. |

Trace a gameplay change through `InputRouter.Sample` → `Simulation.Step` → typed `StepResult` events → `Main.ObserveChanges`/`UpdateArena` → `ArenaView`/`FighterSprites`. A render cel must never decide damage, debit timing or a hitbox's active frame. See [architecture contracts](docs/02_ARCHITECTURE.md) and [direct-spend edge cases](docs/07_DIRECT_SPEND_EDGE_CASES.md).

## Build and test from source

Use the toolchain setup in [README.md](README.md): Python 3.11+, .NET 8 SDK, and the **Godot .NET 4.6.3** editor. Exports require matching .NET export templates. `game/StrikeLedger.csproj` targets `net8.0` with `Godot.NET.Sdk/4.6.3`; this is a project pin, not a claim about the latest release.

Optional art-inspection helpers use Pillow and NumPy (`python -m pip install Pillow numpy`). Movie conversion/validation needs `ffmpeg` and `ffprobe` on `PATH`; these are not prerequisites for the Core/App tests. Some native evidence orchestration assumes Windows process handling, a desktop session or WSL/WSLg. Inspect each script's arguments and host paths before reproducing those captures on another machine.

From the repository root, after configuring `DOTNET_BIN` and `GODOT_BIN` or the ignored `.local_tools.json`:

```sh
python -m pip install -r requirements-tools.txt
python tools/validate_pack.py --strict-schema
python tools/build.py --test
python tools/run_scenario.py --list
python tools/run_scenario.py --scenario exact_credit_ex --seed 1
```

`build.py --test` copies canonical content into ignored `game/GeneratedData/`, builds the Godot C# shell, imports assets, and runs the ContractTests, CoreTests and NetworkLab console harnesses. These are executable tests; `dotnet test` is not the entry point for these projects. The fresh import verification under `audit/import-2026-09-05/` records **15 contract checks, 36 Core scenarios and 10 App suites** passing. Re-run tests after changes rather than carrying that result forward.

To run the production Core and App suites independently, with `dotnet` on `PATH`:

```sh
dotnet run --project src/StrikeLedger.CoreTests/StrikeLedger.CoreTests.csproj -c ExportRelease -- --data data --self-test --evidence-dir reports/evidence/audit-core
dotnet run --project src/StrikeLedger.NetworkLab/StrikeLedger.NetworkLab.csproj -c ExportRelease -- --data data --self-test --evidence-dir reports/evidence/audit-app
```

```sh
python tools/build.py --export --test
python tools/run_scenario.py --scenario controller_menu_flow
python tools/run_scenario.py --scenario whole_local_match
```

The exporter writes `dist/StrikeLedger/windows/` and `dist/StrikeLedger/linux/`. Native scenario routes require an export; graphical scenarios need a desktop renderer. The controller-menu driver injects software events and does not certify physical pads. Build logs and new evidence go under ignored `reports/evidence/`. `python -m unittest discover -s reference/tests -v` is an additional structural/oracle check, not a replacement for C# or native tests.

## Tests versus the historical release gate

The committed acceptance ledger describes a particular historical candidate. `dist/` and `reports/evidence/` are ignored, so a clone alone does **not** contain all artifacts required to reproduce its 76 software evidence checks. `tools/release_gate.py` verifies candidate/content identity, artifact existence, hashes and required evidence kinds. It does not run the gameplay tests or establish human feel.

For that historical audit, use a **separate checkout at commit `ab951b7`**, obtain `Penny-Punchers-audit-evidence.zip` using the release instructions in [README.md](README.md), and extract its files at that checkout's root. The separate archive contains the 126 directly referenced ledger artifacts and a manifest. Then run the unmodified gate:

```sh
python tools/release_gate.py --tier software
python tools/release_gate.py --tier all
```

Missing evidence on a clean clone is expected to fail closed. Physical-device and human gates remain distinct from software proof. For a new build, generate new evidence and a new candidate; do not change old hashes just to make a gate pass or label old recordings as a changed build. `tools/candidate_identity.py` and `tools/rebind_after_hours_ledger.py` belong to the release workflow. The latter requires reviewed compatibility evidence; inspect it before use. `tools/update_release_status.py` intentionally refuses After Hours candidates because its legacy prose describes the previous vector renderer.

## UI and settings changes

- `Main.Menus.cs` constructs menus in code with fixed coordinates. Begin with `Button`, `Panel`, `Heading`, `Back`, and the relevant screen method; retain keyboard/controller focus and the existing callbacks. Specialized lab, replay and network screens live in their corresponding `Main.*.cs` partials.
- `Main.ArtSkin.cs` supplies cached texture crops, frames, portraits and live HUD drawing. Use source art as decoration; health, credits, timer and selections must remain live values. The supplied typography board is not an installed font; dynamic text uses Godot's bundled fallback font.
- `GameSettings.Load`, `Normalize`, `Save` and `Apply` own `user://settings.json`. Current width choices are explicitly constrained to 1280/1600/1920. Add a preference to the model and normalization, its menu control, and its runtime consumer together. Keep old settings files readable.
- `Main.ControlRouting.cs` handles remap capture before GUI actions and exposes `BuildPadRemapControls`. `InputRouter` in `GameSettings.cs` handles actual assignment and sampling. Extend `assets/presentation_checks/inputchecks/Program.cs` for settings/input logic and `Main.UiSmoke.cs` for real menu callbacks; inspect screenshots as well as assertions.

## Adding a fighter is a roster refactor

Adding one JSON file and one sprite sheet is insufficient. Keep **two match seats** while expanding the selectable roster; two-player arrays are not automatically roster bugs.

| Constraint to review | Existing location |
| --- | --- |
| Loader requires exactly Rook/Vale, 12 items and two stages; each fighter requires 49 moves and three supers. | `src/StrikeLedger.Core/GameContent.cs` / `Load()` |
| Fighter and replay schemas enumerate Rook/Vale; validation lists the pair explicitly. | `schemas/fighter.schema.json`, `items.schema.json`, `replay_header.schema.json`; `tools/validate_pack.py` |
| Initial selections determine cached fighter JSON; local/network selectors toggle the pair; title says two fighters; move-list pagination is fixed. | `game/Main.cs` / `_Ready()`; `Main.Menus.cs` / `Select`, `NetworkMenu`, `Title`, `MoveList` |
| Preparation assumes three slots with two ordered lease choices each. | `Main.Menus.cs` / `ItemsFor`, `DraftItems`, `Preparation`; `Main.PrepDetails.cs` |
| Atlas, portrait, icon and projectile selection falls back through Rook/Vale branches. | `Presentation/FighterSprites.cs` / `LoadAtlas`; `Main.ArtSkin.cs` / `UiPortrait`, `UiLeaseArt`; `Presentation/ArenaView.cs` / `DrawOverlay`; `FighterPalette.gdshader` |
| Bots, opponent selection and charge drills contain fighter-specific branches. | `src/StrikeLedger.App/Bots.cs`, `Training.cs`; shell training menus |
| Input recognition expects common IDs such as `throw_forward`, `leap_overhead`, `s_*`, `c_*`, `j_*` and `close_*`; tests encode 98 actions/12 items. | `Simulation.Input.cs` / `Recognize`; `src/StrikeLedger.CoreTests/Program.cs` |

Introduce an explicit roster/asset registry before removing those constraints. Add command recognition, action-effect, lease, both-facing, replay/rollback and art-binding coverage for the new fighter. Update content identity and private-match compatibility deliberately. Read [content and presentation](docs/08_CONTENT_AND_PRESENTATION.md), [input contracts](docs/03_INPUT_MOVEMENT_AND_FEEL.md), and [balance methodology](docs/14_1V1_BALANCE_AND_EXPERIMENTS.md).

## Suggested next work — not implemented

1. Improve menu layout reuse and scaling while retaining controller-only navigation, input testing and live economy information.
2. Build the roster/asset registry and remove paired-name assumptions, then add one fully tested fighter through that path.
3. Expand settings only with validation, backward-compatible persistence and native UI evidence; follow with actual pad and human playtesting.

Preserve the deterministic Core/App boundary and existing replay evidence during each step. A repository rename alone should not silently migrate game saves, protocol identities or the internal Strike Ledger brand.
