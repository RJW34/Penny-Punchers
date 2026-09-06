#!/usr/bin/env python3
"""Dispatch the bounded acceptance scenarios to their real executable owners.

Examples:
  python tools/run_scenario.py --list
  python tools/run_scenario.py --scenario exact_credit_ex --seed 1
  python tools/run_scenario.py --scenario loopback_match --network-matrix --no-build
  python tools/run_scenario.py --scenario controller_menu_flow --headless

No unknown scenario, missing executable, missing result, failed assertion or timeout
is reported as a pass. Native full-match automation uses digital bot inputs; the
controller UI scenario uses explicitly labelled software device events. Neither
claims physical hardware or human feel acceptance.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import os
from pathlib import Path
import re
import shutil
import subprocess
import sys
import time
from datetime import datetime, timezone

ROOT = Path(__file__).resolve().parents[1]
CORE = {
    "input_motion_both_sides", "charge_crossup", "free_kit_round", "high_low_parry",
    "red_parry_boundary", "multihit_super_parry", "throw_kara_quickrise",
    "exact_credit_ex", "insufficient_credit_no_fallback", "paid_startup_interrupted",
    "reserve_two_ex_then_deny", "ex_cancel_super_costs", "atomic_plan_commit",
    "nine_round_draw", "all_rook_moves", "all_vale_moves", "all_lease_items",
}
CORE |= {"shop_registry_resource_contract", "shop_all_ex_repeat_without_bank", "shop_all_super_once_rollback", "shop_skill_caps_and_partial_receipts", "shop_actual_repeated_baits_and_partial_cap"}
APP = {
    "replay_wallet_seek", "bot_economic_match", "rollback_startup_debits",
    "rollback_ko_to_parry", "malformed_packet_replay",
}
APP |= {"shop_only_v2", "shop_timeout"}
BALANCE = {"zero_vs_full_wallet", "recovery_farming", "shop_only_pilot"}
NATIVE = {"whole_local_match", "controller_menu_flow", "native_export_smoke"}
SCENARIOS = CORE | APP | BALANCE | NATIVE | {"training_drills", "loopback_match"}


def route(scenario: str) -> str:
    if scenario in CORE:
        return "production core conformance"
    if scenario in APP:
        return "App integration executable"
    if scenario in BALANCE:
        return "counterbalanced full-match balance laboratory"
    if scenario in NATIVE:
        return "exported native Godot game"
    if scenario == "training_drills":
        return "core mechanics plus App drill/reset/checkpoint integration"
    if scenario == "loopback_match":
        return "two actual UDP peer processes"
    raise ValueError("Unknown scenario: " + scenario)


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def content_hash(directory: Path) -> str:
    digest = hashlib.sha256()
    for path in sorted(directory.rglob("*.json"), key=lambda p: p.relative_to(directory).as_posix()):
        digest.update(path.relative_to(directory).as_posix().encode("utf-8"))
        digest.update(path.read_bytes())
    return digest.hexdigest()


def source_identity() -> dict:
    fingerprint = hashlib.sha256()
    count = 0
    for folder in ("src", "game", "tools"):
        for path in sorted((ROOT / folder).rglob("*")):
            relative = path.relative_to(ROOT)
            if not path.is_file() or set(relative.parts) & {"bin", "obj", ".godot", "GeneratedData", "__pycache__"}:
                continue
            if path.suffix not in {".cs", ".csproj", ".py", ".godot", ".tscn", ".cfg"}:
                continue
            fingerprint.update(relative.as_posix().encode())
            fingerprint.update(path.read_bytes())
            count += 1
    git = subprocess.run(["git", "rev-parse", "HEAD"], cwd=ROOT, capture_output=True, text=True, check=False)
    return {"gitHead": git.stdout.strip() if git.returncode == 0 else None,
            "sourceFingerprintSha256": fingerprint.hexdigest(), "sourceFiles": count}


def find_tool(name: str, explicit: str | None) -> str:
    config_path = ROOT / ".local_tools.json"
    config = json.loads(config_path.read_text(encoding="utf-8")) if config_path.exists() else {}
    candidate = explicit or os.environ.get(name.upper() + "_BIN") or config.get(name) or shutil.which(name)
    if not candidate or not Path(candidate).is_file():
        raise FileNotFoundError(f"Set --{name}, {name.upper()}_BIN, or .local_tools.json to the installed {name} executable")
    return str(Path(candidate).resolve())


def native_game(explicit: str | None) -> Path:
    if explicit:
        path = Path(explicit).resolve()
    elif sys.platform == "win32":
        path = ROOT / "dist" / "StrikeLedger" / "windows" / "StrikeLedger.exe"
    elif sys.platform.startswith("linux"):
        path = ROOT / "dist" / "StrikeLedger" / "linux" / "StrikeLedger.x86_64"
    else:
        raise RuntimeError("This bounded release has Windows and Linux exports; provide --game for an explicitly prepared native executable")
    if not path.is_file():
        raise FileNotFoundError(f"Native export is missing: {path}. Run tools/build.py --export first.")
    return path


def run_process(command: list[str], output: Path, timeout: int, environment: dict) -> dict:
    print("RUN " + subprocess.list2cmdline(command), flush=True)
    started = time.monotonic()
    flags = subprocess.CREATE_NO_WINDOW if os.name == "nt" else 0
    with output.open("w", encoding="utf-8") as log:
        process = subprocess.Popen(command, cwd=ROOT, env=environment, stdout=log,
                                   stderr=subprocess.STDOUT, creationflags=flags)
        next_update = started + 15
        timed_out = False
        try:
            while process.poll() is None:
                now = time.monotonic()
                if now - started > timeout:
                    timed_out = True
                    process.kill()
                    break
                if now >= next_update:
                    print(f"RUNNING {output.name} / {int(now-started)} seconds", flush=True)
                    next_update = now + 15
                time.sleep(.1)
            code = process.wait(timeout=10)
        except BaseException:
            if process.poll() is None:
                process.kill()
                process.wait(timeout=10)
            raise
    # Godot can report script/import errors while returning zero; those remain failures.
    errors = []
    tail: list[str] = []
    with output.open(encoding="utf-8", errors="replace") as log:
        for line in log:
            if re.match(r"^(?:ERROR:|SCRIPT ERROR:|Unhandled exception|UI_SOFTWARE_FAIL)", line):
                errors.append(line.rstrip()[:1000])
            tail.append(line.rstrip())
            tail = tail[-8:]
    for line in tail:
        print(line, flush=True)
    result = {"command": command, "exitCode": code, "timeout": timed_out,
              "elapsedSeconds": time.monotonic()-started, "log": str(output), "reportedErrors": errors}
    if timed_out or code != 0 or errors:
        result["passed"] = False
    else:
        result["passed"] = True
    return result


def require_results(kind: str, directory: Path) -> dict:
    if kind == "core":
        path = directory / "core-conformance.json"
        result = json.loads(path.read_text(encoding="utf-8"))
        valid = result.get("failed") == 0 and result.get("passed", 0) > 0 and bool(result.get("results"))
    elif kind == "app":
        path = directory / "self-tests.json"
        result = json.loads(path.read_text(encoding="utf-8"))
        valid = bool(result.get("results")) and all(r.get("passed") is True for r in result["results"])
    elif kind == "balance":
        path = directory / "summary.json"
        result = json.loads(path.read_text(encoding="utf-8"))
        valid = result.get("samples", 0) > 0 and result.get("excluded") == 0
    elif kind == "network":
        path = directory / "matrix-result.json"
        result = json.loads(path.read_text(encoding="utf-8"))
        valid = result.get("actualTwoProcesses") is True and bool(result.get("results")) and all(
            r.get("passed") is True and r.get("matchingFinalHashWalletsScoresReceipts") is True for r in result["results"])
    elif kind == "ui":
        path = directory / "controller-menu-flow.json"
        result = json.loads(path.read_text(encoding="utf-8"))
        valid = result.get("success") is True and bool(result.get("checks")) and all(r.get("passed") is True for r in result["checks"])
    elif kind == "native":
        path = directory / "full-match.replay.json"
        result = json.loads(path.read_text(encoding="utf-8"))
        log = (directory / "gameplay.log").read_text(encoding="utf-8")
        valid = "MATCH COMPLETE " in log and bool(result.get("Commands")) and any(c.get("Kind") == "settlement" for c in result["Commands"])
    else:
        raise ValueError("Unknown result validator")
    if not valid:
        raise RuntimeError(f"Executable returned without a passing substantive result: {path}")
    return {"kind": kind, "path": str(path), "sha256": sha256(path)}


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--scenario")
    parser.add_argument("--seed", type=int, default=1)
    parser.add_argument("--seeds", type=int, default=1, help="balance-lab seed count, 1..20")
    parser.add_argument("--evidence-dir")
    parser.add_argument("--data", default=str(ROOT / "data"))
    parser.add_argument("--dotnet")
    parser.add_argument("--configuration", choices=["ExportRelease", "Release"], default="ExportRelease", help="managed build configuration; ExportRelease matches the shipped game")
    parser.add_argument("--game", help="explicit exported native executable")
    parser.add_argument("--headless", action="store_true", help="native software checks only; no viewport captures")
    parser.add_argument("--no-build", action="store_true", help="run CLI binaries already compiled in the selected configuration")
    parser.add_argument("--network-matrix", action="store_true", help="0/50/100/150ms RTT with jitter/loss/duplicate/reorder profiles")
    parser.add_argument("--timeout", type=int, default=900, help="bounded seconds per executable")
    parser.add_argument("--list", action="store_true")
    parser.add_argument("--dry-run", action="store_true", help="print plans only; does not write passing evidence")
    args = parser.parse_args()
    required = {s["id"] for s in json.loads((ROOT / "acceptance" / "scenarios.json").read_text())["scenarios"]}
    if required != SCENARIOS:
        raise RuntimeError(f"Scenario registry mismatch. Missing={sorted(required-SCENARIOS)}, extra={sorted(SCENARIOS-required)}")
    if args.list:
        print(json.dumps({"scenarios": [{"id": s, "runner": route(s)} for s in sorted(SCENARIOS)]}, indent=2))
        return 0
    if args.scenario not in SCENARIOS:
        parser.error("--scenario must identify an implemented scenario; use --list")
    if not 1 <= args.seed <= 100000 or not 1 <= args.seeds <= 20 or not 30 <= args.timeout <= 7200:
        parser.error("seed must be 1..100000, seeds 1..20, timeout 30..7200")
    if args.network_matrix and args.scenario != "loopback_match":
        parser.error("--network-matrix applies only to loopback_match")
    data = Path(args.data).resolve()
    if not (data / "rules.json").is_file():
        raise FileNotFoundError("Canonical --data directory is missing rules.json")
    if args.scenario in NATIVE and data != ROOT / "data":
        parser.error("Native scenarios read their packaged content. --data override is available for managed runners only.")
    if args.scenario == "loopback_match" and data != ROOT / "data":
        parser.error("The two-process coordinator currently uses this project's canonical data directory.")
    output = Path(args.evidence_dir or ROOT / "reports" / "evidence" / args.scenario).resolve()
    plan: list[tuple[str, list[str], Path, str | None, int]] = []
    dotnet = find_tool("dotnet", args.dotnet) if args.scenario not in NATIVE else None
    environment = dict(os.environ)
    if dotnet:
        environment["DOTNET_ROOT"] = str(Path(dotnet).parent)
        environment["PATH"] = environment["DOTNET_ROOT"] + os.pathsep + environment.get("PATH", "")
        environment["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1"
    built: set[str] = set()

    def managed(project_name: str, cli: list[str], directory: Path, validator: str):
        project = ROOT / "src" / project_name / (project_name + ".csproj")
        dll = project.parent / "bin" / args.configuration / "net8.0" / (project_name + ".dll")
        if not args.no_build and project_name not in built:
            plan.append(("build-" + project_name, [dotnet, "build", str(project), "-c", args.configuration, "--nologo"], output, None, args.timeout))
            built.add(project_name)
        elif args.no_build and not dll.is_file():
            raise FileNotFoundError("Compiled " + args.configuration + " executable is missing: " + str(dll))
        plan.append((validator, [dotnet, str(dll), "--data", str(data), *cli, "--evidence-dir", str(directory)], directory, validator, args.timeout))

    if args.scenario in CORE:
        managed("StrikeLedger.CoreTests", ["--scenario", args.scenario, "--seed", str(args.seed)], output, "core")
    elif args.scenario in APP:
        managed("StrikeLedger.NetworkLab", ["--scenario", args.scenario, "--seed", str(args.seed)], output, "app")
    elif args.scenario == "training_drills":
        managed("StrikeLedger.CoreTests", ["--seed", str(args.seed)], output / "core-mechanics", "core")
        managed("StrikeLedger.NetworkLab", ["--scenario", "training_drills", "--seed", str(args.seed)], output / "app-training", "app")
    elif args.scenario in BALANCE:
        # Preserve original scenario IDs as explicit v2 laboratory routes.
        # The actual command records the shop-only runner and its bounded scope.
        scope = "rentals" if args.scenario == "zero_vs_full_wallet" else "opening" if args.scenario == "recovery_farming" else "all"
        managed("StrikeLedger.BalanceLab", ["--scenario", "shop_only_pilot", "--scope", scope, "--seed", str(args.seed), "--seeds", str(args.seeds)], output, "balance")
    elif args.scenario == "loopback_match":
        command = [sys.executable, str(ROOT / "tools" / "run_network_lab.py"), "--dotnet", dotnet,
                   "--configuration", args.configuration,
                   "--output", str(output), "--seed", str(args.seed), "--max-seconds", str(args.timeout)]
        if args.no_build:
            command.append("--no-build")
        if args.network_matrix:
            command.append("--matrix")
        plan.append(("network", command, output, "network", (args.timeout + 60) * (4 if args.network_matrix else 1) + 120))
    else:
        executable = native_game(args.game)
        command = [str(executable)] + (["--headless"] if args.headless else [])
        command += ["--", "--scenario", args.scenario, "--seed", str(args.seed), "--evidence-dir", str(output)]
        plan.append(("native", command, output, "ui" if args.scenario == "controller_menu_flow" else "native", args.timeout))

    if args.dry_run:
        print(json.dumps({"status": "planned_only_not_executed", "scenario": args.scenario, "configuration": args.configuration,
                          "commands": [command for _, command, _, _, _ in plan]}, indent=2))
        return 0
    output.mkdir(parents=True, exist_ok=True)
    manifest = {"schemaVersion": 1, "scenario": args.scenario, "runner": route(args.scenario),
                "configuration": args.configuration,
                "seed": args.seed, "seedCount": args.seeds, "startedUtc": datetime.now(timezone.utc).isoformat(),
                "status": "running", "passed": False, "source": source_identity(), "contentHash": content_hash(data),
                "commands": [], "verifiedResults": [], "limitations": []}
    if args.scenario in NATIVE:
        manifest["nativeExecutable"] = {"path": str(executable), "sha256": sha256(executable)}
        manifest["limitations"] += ["Native automatic match scripts use fixed digital bot-input seeds; requested seed does not change their policy.",
                                    "Controller UI uses labelled software events; physical device and human feel gates are separate."]
        if args.headless:
            manifest["limitations"].append("Headless run produces no viewport captures and does not verify the physical display/audio path.")
    if args.scenario == "training_drills":
        manifest["limitations"].append("Verifies core drill mechanics and App reset/checkpoint/input recording; does not certify a human completed every drill.")
    if args.scenario in APP:
        manifest["limitations"].append("App conformance cases use their hand-authored fixture seeds; --seed is recorded, while live socket/balance runs use it for input generation.")
    result_path = output / "dispatch-result.json"
    try:
        for index, (label, command, directory, validator, timeout) in enumerate(plan):
            directory.mkdir(parents=True, exist_ok=True)
            result = run_process(command, output / f"dispatch-{index:02d}-{label}.log", timeout, environment)
            manifest["commands"].append(result)
            if not result["passed"]:
                raise RuntimeError(f"{label} failed; exit={result['exitCode']}; inspect {result['log']}")
            if validator:
                manifest["verifiedResults"].append(require_results(validator, directory))
        manifest["passed"] = True
        manifest["status"] = "passed"
    except Exception as error:
        manifest["status"] = "failed"
        manifest["error"] = str(error)
        print("SCENARIO FAILED: " + str(error), file=sys.stderr)
    finally:
        manifest["finishedUtc"] = datetime.now(timezone.utc).isoformat()
        manifest["artifacts"] = {p.relative_to(output).as_posix(): sha256(p) for p in sorted(output.rglob("*"))
                                 if p.is_file() and p != result_path}
        result_path.write_text(json.dumps(manifest, indent=2), encoding="utf-8")
    print(f"{manifest['status'].upper()} {args.scenario}: {result_path}", flush=True)
    return 0 if manifest["passed"] else 1


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except (OSError, ValueError, RuntimeError, KeyError) as error:
        print("Scenario runner error: " + str(error), file=sys.stderr)
        raise SystemExit(2)
