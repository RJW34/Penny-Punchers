#!/usr/bin/env python3
"""Publish self-contained verification tools against the frozen game binaries.

Production projects are never rebuilt. Generated staging projects compile only
the verification sources and reference the already exported Core/App assemblies.
"""
from __future__ import annotations

import argparse
from datetime import datetime, timezone
import hashlib
import json
import os
from pathlib import Path
import shutil
import subprocess
import time
from xml.sax.saxutils import escape

ROOT = Path(__file__).resolve().parents[1]
TOOLS = ("CoreTests", "NetworkLab", "BalanceLab")
PLATFORMS = ("win-x64", "linux-x64")
EXPECTED: dict[str, str] = {}


def sha(path: Path) -> str:
    h = hashlib.sha256()
    with path.open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            h.update(block)
    return h.hexdigest()


def run(command: list[str], log: Path, timeout: int = 600) -> None:
    log.parent.mkdir(parents=True, exist_ok=True)
    start = time.monotonic()
    with log.open("w", encoding="utf-8") as output:
        output.write(json.dumps({"utc": datetime.now(timezone.utc).isoformat(), "command": command}) + "\n")
        output.flush()
        process = subprocess.Popen(command, cwd=ROOT, stdout=output, stderr=subprocess.STDOUT,
                                   creationflags=subprocess.CREATE_NO_WINDOW if os.name == "nt" else 0)
        last_progress = start
        while process.poll() is None:
            time.sleep(0.2)
            now = time.monotonic()
            if now - last_progress >= 15:
                print(f"{log.name}: still running ({now-start:.0f}s)", flush=True)
                last_progress = now
            if now - start > timeout:
                process.kill()
                process.wait()
                raise RuntimeError(f"Timeout: {log}")
        output.write(f"\nEXIT {process.returncode}; elapsed {time.monotonic()-start:.3f}s\n")
    if process.returncode:
        raise RuntimeError(f"Command failed ({process.returncode}); see {log}")


def project(tool: str, directory: Path, assemblies: dict[str, Path]) -> Path:
    sources = sorted(p for p in (ROOT / "src" / f"StrikeLedger.{tool}").rglob("*.cs")
                     if not any(part in {"bin", "obj"} for part in p.parts))
    if not sources:
        raise RuntimeError(f"No verification source files: {tool}")
    references = ["Core"] + ([] if tool == "CoreTests" else ["App"])
    xml = ['<Project Sdk="Microsoft.NET.Sdk">', '<PropertyGroup>',
           '<OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework>',
           f'<AssemblyName>StrikeLedger.{tool}</AssemblyName>',
           '<EnableDefaultCompileItems>false</EnableDefaultCompileItems>',
           '<ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable>',
           '<TreatWarningsAsErrors>true</TreatWarningsAsErrors><Optimize>true</Optimize>',
           '<PublishSingleFile>false</PublishSingleFile><PublishTrimmed>false</PublishTrimmed>',
           '<SelfContained>true</SelfContained><Deterministic>true</Deterministic>',
           '</PropertyGroup><ItemGroup>']
    xml.extend(f'<Compile Include="{escape(str(p), {chr(34): "&quot;"})}" />' for p in sources)
    for name in references:
        path = escape(str(assemblies[name]))
        xml.append(f'<Reference Include="StrikeLedger.{name}"><HintPath>{path}</HintPath><Private>true</Private></Reference>')
    xml.append('</ItemGroup></Project>')
    directory.mkdir(parents=True, exist_ok=True)
    path = directory / f"StrikeLedger.{tool}.csproj"
    path.write_text("\n".join(xml) + "\n", encoding="utf-8")
    return path


def readme() -> str:
    return r"""# Penny Punchers verification tools — shop-only v2

These self-contained Windows x64 and Linux x64 executables use the exact frozen
Core/App binaries shipped in the native game. No .NET SDK is required to run them.
Keep each tool directory intact. Canonical data is in `data/` alongside this
README. `PACKAGE_MANIFEST.json` records production hashes and every shipped file.

Both playable trials use the v2 shop-only economy. Core has 100 runtime action
nodes and Expanded has 115, including branches across both fighters; the historical
98-action scenario is not current-catalog evidence. Use `data/rulesets/buyables_full`
for Expanded and retain separate evidence directories for the two content hashes.
The stable internal executable/namespace names remain StrikeLedger.

Run examples from this verification directory. Replace `win-x64` with `linux-x64`
and omit `.exe` on Linux; run `chmod +x linux-x64/*/StrikeLedger.*` after extracting
on a filesystem that does not preserve executable permissions.

```powershell
.\win-x64\CoreTests\StrikeLedger.CoreTests.exe --data .\data --evidence-dir .\results\core
.\win-x64\CoreTests\StrikeLedger.CoreTests.exe --scenario shop_registry_resource_contract --data .\data\rulesets\buyables_full --evidence-dir .\results\expanded-registry
.\win-x64\CoreTests\StrikeLedger.CoreTests.exe --scenario shop_all_ex_repeat_without_bank --seed 1 --data .\data --evidence-dir .\results\ex
.\win-x64\CoreTests\StrikeLedger.CoreTests.exe --scenario shop_all_super_once_rollback --seed 1 --data .\data --evidence-dir .\results\super
.\win-x64\CoreTests\StrikeLedger.CoreTests.exe --scenario shop_skill_caps_and_partial_receipts --data .\data --evidence-dir .\results\skill
.\win-x64\NetworkLab\StrikeLedger.NetworkLab.exe --self-test --data .\data --evidence-dir .\results\app
.\win-x64\NetworkLab\StrikeLedger.NetworkLab.exe --scenario shop_only_v2 --data .\data --evidence-dir .\results\shop-app
.\win-x64\BalanceLab\StrikeLedger.BalanceLab.exe --scenario shop_only_pilot --scope smoke --seed 1 --seeds 1 --data .\data --evidence-dir .\results\balance-smoke
```

For an actual two-process UDP loopback match, launch these in separate terminals:

```powershell
.\win-x64\NetworkLab\StrikeLedger.NetworkLab.exe --seat 0 --local-port 27961 --remote-port 27962 --session verification-match --seed 1 --rtt 100 --jitter 20 --loss 1 --duplicates 1 --reorder 2 --max-seconds 900 --data .\data --evidence-dir .\results\peer0
.\win-x64\NetworkLab\StrikeLedger.NetworkLab.exe --seat 1 --local-port 27962 --remote-port 27961 --session verification-match --seed 1 --rtt 100 --jitter 20 --loss 1 --duplicates 1 --reorder 2 --max-seconds 900 --data .\data --evidence-dir .\results\peer1
```

Each peer must complete with exit code zero; compare final state hashes, banks,
scores and confirmed round receipts. Each peer reconstructs its recorded replay
to the same final state. This is two processes on one computer, not evidence of
two physical machines or physical controllers.

On Linux, for example:

```sh
./linux-x64/CoreTests/StrikeLedger.CoreTests --scenario shop_actual_counter_and_parry_rewards --data ./data --evidence-dir ./results/linux-rewards
```

CoreTests without `--scenario` runs the production Core suite appropriate to the
selected content. NetworkLab `--self-test` runs App/network/replay/training checks.
BalanceLab `shop_only_pilot --scope smoke` checks a short sample; `--scope all` runs
the broader current pilot and may take much longer. Neither is a human balance
verdict. Unknown scenarios and failed assertions exit nonzero. Historical scenario
names marked not applicable do not count as passing current action coverage.
Generated action traces are explicitly training conformance, not competitive film.

To reproduce packaging from the source project:
`python tools/package_verification.py --configuration ExportRelease`
The script verifies frozen Core/App hashes before and after publishing. It never
rebuilds production projects or changes their source. Native shop/HUD/camera,
physical-device and owner/friend acceptance are separate from these checks.
"""


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--dotnet")
    parser.add_argument("--configuration", choices=["ExportRelease"], default="ExportRelease")
    parser.add_argument("--platform", choices=PLATFORMS, action="append")
    parser.add_argument("--tool", choices=TOOLS, action="append")
    args = parser.parse_args()
    local = json.loads((ROOT / ".local_tools.json").read_text(encoding="utf-8")) if (ROOT / ".local_tools.json").exists() else {}
    dotnet = args.dotnet or os.environ.get("DOTNET_BIN") or local.get("dotnet") or shutil.which("dotnet")
    if not dotnet:
        raise RuntimeError("Pass --dotnet with the supported .NET SDK executable")
    # A new gameplay candidate cannot inherit the art-only baseline's constants.
    # Bind to the actual exported assemblies listed in the frozen candidate.
    candidate = json.loads((ROOT / "reports/RELEASE_CANDIDATE.json").read_text(encoding="utf-8"))
    for entry in candidate["source_inputs"]:
        if sha(ROOT / entry["path"]) != entry["sha256"]:
            raise RuntimeError("Candidate source changed: " + entry["path"])
    assemblies = {}
    for name in ("Core", "App"):
        entries = [e for e in candidate["native_binaries"] if e["path"].endswith(f"/StrikeLedger.{name}.dll")]
        if len(entries) != 2 or len({e["sha256"] for e in entries}) != 1:
            raise RuntimeError(f"Both platform exports must contain the same frozen {name} assembly")
        for entry in entries:
            if sha(ROOT / entry["path"]) != entry["sha256"]:
                raise RuntimeError("Candidate binary changed: " + entry["path"])
        assemblies[name] = ROOT / entries[0]["path"]
        EXPECTED[name] = entries[0]["sha256"]
    for name, path in assemblies.items():
        if not path.is_file() or sha(path) != EXPECTED[name]:
            raise RuntimeError(f"Frozen {name} binary identity differs: {path}")
    evidence = ROOT / "reports" / "evidence" / "verification-build"
    destination = ROOT / "dist" / "verification"
    published = []
    for platform in args.platform or PLATFORMS:
        for tool in args.tool or TOOLS:
            staging = evidence / "projects" / platform / tool
            csproj = project(tool, staging, assemblies)
            output = destination / platform / tool
            log = evidence / f"publish-{platform}-{tool}.log"
            command = [str(dotnet), "publish", str(csproj), "--configuration", args.configuration,
                       "--runtime", platform, "--self-contained", "true", "--output", str(output),
                       "--source", "https://api.nuget.org/v3/index.json", "--nologo", "--verbosity", "minimal"]
            print(f"Publishing {platform}/{tool}", flush=True)
            run(command, log)
            for name in ["Core"] + ([] if tool == "CoreTests" else ["App"]):
                if sha(output / f"StrikeLedger.{name}.dll") != EXPECTED[name]:
                    raise RuntimeError(f"Published {tool} has incompatible {name} binary")
            executable = output / f"StrikeLedger.{tool}{'.exe' if platform == 'win-x64' else ''}"
            if not executable.is_file():
                raise RuntimeError(f"Missing self-contained executable: {executable}")
            runtime = output / ("coreclr.dll" if platform == "win-x64" else "libcoreclr.so")
            if not runtime.is_file():
                raise RuntimeError(f"Missing self-contained .NET runtime: {runtime}")
            published.append({"platform": platform, "tool": tool, "executable": executable.relative_to(destination).as_posix(), "log": log.relative_to(ROOT).as_posix()})
    shutil.copytree(ROOT / "data", destination / "data", dirs_exist_ok=True)
    (destination / "README.md").write_text(readme(), encoding="utf-8")
    for name, path in assemblies.items():
        if sha(path) != EXPECTED[name]:
            raise RuntimeError(f"Production {name} changed during packaging")
    available = []
    for platform in PLATFORMS:
        for tool in TOOLS:
            folder = destination / platform / tool
            executable = folder / f"StrikeLedger.{tool}{'.exe' if platform == 'win-x64' else ''}"
            if not executable.is_file():
                continue
            for name in ["Core"] + ([] if tool == "CoreTests" else ["App"]):
                if sha(folder / f"StrikeLedger.{name}.dll") != EXPECTED[name]:
                    raise RuntimeError(f"Existing {platform}/{tool} has incompatible {name} binary")
            available.append({"platform": platform, "tool": tool, "executable": executable.relative_to(destination).as_posix()})
    entries = [{"path": p.relative_to(destination).as_posix(), "bytes": p.stat().st_size, "sha256": sha(p)}
               for p in sorted(destination.rglob("*")) if p.is_file() and p.name not in {"PACKAGE_MANIFEST.json", "SHA256SUMS.txt"}]
    manifest = {"schema_version": 1, "utc": datetime.now(timezone.utc).isoformat(),
                "configuration": args.configuration, "production_binaries": EXPECTED,
                "self_contained": True, "available_tools": available, "published_this_run": published, "files": entries}
    (destination / "PACKAGE_MANIFEST.json").write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")
    (destination / "SHA256SUMS.txt").write_text("".join(e["sha256"] + "  " + e["path"] + "\n" for e in entries), encoding="utf-8")
    evidence.mkdir(parents=True, exist_ok=True)
    (evidence / "package-result.json").write_text(json.dumps({"passed": True, "published": published, "available_tools": available, "production_binaries": EXPECTED, "manifest_sha256": sha(destination / "PACKAGE_MANIFEST.json"), "files": len(entries), "bytes": sum(e["bytes"] for e in entries)}, indent=2) + "\n", encoding="utf-8")
    print(f"Published {len(published)} self-contained tools; {len(entries)} files, {sum(e['bytes'] for e in entries):,} bytes", flush=True)


if __name__ == "__main__":
    main()
