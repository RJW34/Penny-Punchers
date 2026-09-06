# Auditing Penny-Punchers

The active game uses Shop-Only Economy Rework v2. Start with [docs/21_SHOP_ONLY_V2.md](docs/21_SHOP_ONLY_V2.md), [README.md](README.md) and [reports/STATE.json](reports/STATE.json). Original audit and design ZIPs, their extracted contents, source assets and migration records are retained. Earlier direct-spend reports describe their own historical builds.

## Reproduce the game

Install matching Godot.NET4.6.3 editor/export templates and .NET SDK8.0.424, then configure `DOTNET_BIN`/`GODOT_BIN` or ignored `.local_tools.json`. Python3.12 and the pinned `requirements-tools.txt` support data and packaging tools.

```sh
python -m pip install -r requirements-tools.txt
python tools/validate_pack.py --strict-schema
python tools/build.py --test --export
```

The build copies canonical content, compiles/imports the Godot game, runs executable C# ContractTests/CoreTests/NetworkLab and exports Windows/Linux. These harnesses use `dotnet run`, not `dotnet test`. Core and expanded catalogs must both be checked:

```sh
dotnet run --project src/StrikeLedger.CoreTests -c ExportRelease -- --data data --self-test --evidence-dir reports/evidence/audit-core
dotnet run --project src/StrikeLedger.CoreTests -c ExportRelease -- --data data/rulesets/buyables_full --self-test --evidence-dir reports/evidence/audit-full
dotnet run --project src/StrikeLedger.NetworkLab -c ExportRelease -- --data data --self-test --evidence-dir reports/evidence/audit-app
```

`tools/compile_shop_only.py --publish` reproduces active catalogs from the preserved buyables recipe. `compile_buyables.py` by itself reproduces the historical direct-spend design and must not publish over the active game. Runtime IDs remain `rook` and `vale`; displayed names are Thomas and Vincent. Core has100 action nodes and26 products; expanded has115 and38. Both include four stages.

## Evidence and release integrity

`reports/RELEASE_CANDIDATE.json` binds production inputs, canonical data and native exports. `reports/ACCEPTANCE_RESULTS.json` retains the original82 requirement IDs with explicitly superseded v2 resource descriptions. The separate v2 ledger retains all61 new requirements and the audit crosswalk retains all91 PP findings. Partial work and external checks stay explicit.

`tools/release_gate.py` checks completeness and file hashes. It cannot establish that a result is truthful or the game feels good. `reports/evidence/` and `dist/` are ignored to keep generated binaries and long traces outside source Git; release companion archives provide audit artifacts. Missing evidence on a source-only clone is expected to fail the evidence gate. Run current tests or obtain the matching companion archive, rather than changing hashes to manufacture a pass.

Use `run_native_evidence.py` for exported UI/match/showcase evidence and `verify_packages.py` to extract and launch packaged binaries. `compare_native_replays.py` compares the exact v4 command/event/receipt streams and checks frozen fight banks. Windows and Linux on this PC are distinct OS runtimes; Linux uses WSL, not a second physical machine. Controller-injected software tests are distinct from physical pad tests.

Historical `rebind_after_hours_ledger.py`, `run_independent_review.py`, `verify_after_hours.py`, `update_release_status.py` and their fixed baseline identities are preserved historical tooling. Do not use them to turn old recordings into a v2 pass. The original scaffold Python reference and replay-header schema are also historical; active production replay version4 is defined and bounded in `src/StrikeLedger.App/Replay.cs`.

## Code map and extension work

- `src/StrikeLedger.Core/`: fixed60 integer simulation, collision, input, capabilities, immutable contact facts, rewards and canonical snapshots.
- `src/StrikeLedger.App/`: sessions, legal-input bots, training, commit/reveal, UDP rollback and v4 replay/round receipts.
- `game/Main.*.cs`: controller menus, independent carts, settings, training and results; presentation is in `game/Presentation/`.
- `data/`: canonical100-node core catalog; `data/rulesets/buyables_full/`: implemented115-node expanded catalog.
- `game/Assets/AfterHours/` and `design/after-hours-32bit/`: supplied32-bit art and provenance. Barlow Condensed is bundled under its OFL.
- `audit/requests/`: preserved supplied packages. Current review records identify which finding has actual evidence and which remains open.

A new fighter requires a roster registry, asset bindings, command/CPU/training integration and both-facing combat/replay tests. Retain exactly two match seats while expanding selectable characters. A JSON file alone is insufficient. New product recipes must preserve charge/recovery/cancel rules, explicit access policy, per-round ownership and bank conservation.

Add settings to normalization, persistence, their UI and runtime consumer together. Match input mappings by their real physical role; B/MK must never pause fighting, and menu confirm must be released before combat. Inspect native screenshots as well as assertions. Preserve original replay bytes and deliberately reject incompatible versions.

Bot samples provide bounded tactical and economic observations. They do not establish optimal play, expert fairness, physical-controller compatibility or an owner/friend verdict.
