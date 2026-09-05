# Build and release
The seed targets Godot .NET 4.6.3 and net8.0 as a conservative reproducible starting point, not a latest/supported-forever claim [SRC-03..05]. Use matching editor and export templates. Recheck official support at WP-000 and release, lock actual versions and upgrade coherently where necessary. The standard non-.NET Godot editor is not enough for C# [SRC-04].

From project root, initial scaffold-only checks:
```
python -m pip install -r requirements-tools.txt
python tools/validate_pack.py --strict-schema
python -m unittest discover -s reference/tests -v
python tools/verify_manifest.py
python tools/doctor.py
```
Use a local virtual environment rather than modifying system Python. Doctor reports only, does not install. Safe official per-user provisioning is within the build mission; admin changes/publishing are not. Record real commands and results, including missing tools.

Then compile seed and actual implementation:
```
dotnet run --project src/StrikeLedger.ContractTests/StrikeLedger.ContractTests.csproj
python tools/copy_content.py --apply
python tools/build_move_tables.py --check
dotnet build game/StrikeLedger.csproj
```
`GODOT_BIN` points to the actual installed .NET editor. Platform-aware scripts must resolve that path without hardcoded user folders. Import with headless editor, run the game and headless scenarios, then export named Windows Desktop and Linux presets. Create output directories first. Do not claim a `.csproj` XML parse is compilation or export success is target-machine launch.

Required release layout: `dist/<candidate>/windows/`, `linux/`, player README, controls, single-wallet rules, known limitations, test/candidate manifest, original asset/dependency notices and SHA256SUMS. Distribute native game files, not the user-specific toolchain or font files. Export all required managed/runtime dependencies; verify on a clean user account or machine without development tools when available.

Targets: Windows x86_64 primary player environment and Linux x86_64. An available host may cross-export; actual Windows/Linux launches and two-machine LAN/controller verification are distinct device gates. Implement and package both targets even if one physical OS is unavailable, then report which launch gate remains pending.

Final commands include `tools/release_gate.py --tier software`, `--tier target_device`, `--tier human`, and `--tier all`. Before game implementation the gate must fail. At completion attach reproducible actual artifact paths and results, not a staged status-only PASS.
