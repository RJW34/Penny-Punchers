#!/usr/bin/env python3
"""Compare completed native Windows/Linux simulation evidence, never render pacing."""
from __future__ import annotations

import argparse
from collections import Counter
from datetime import datetime, timezone
import hashlib
import json
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
HASH = re.compile(r"[0-9a-f]{64}\Z")


class ComparisonError(ValueError):
    pass


def require(condition: bool, message: str) -> None:
    if not condition:
        raise ComparisonError(message)


def digest(value: object) -> str:
    return hashlib.sha256(json.dumps(value, sort_keys=True, separators=(",", ":"), ensure_ascii=False).encode("utf-8")).hexdigest()


def load(path: Path, maximum: int) -> tuple[dict, dict, bytes]:
    require(path.is_file(), f"Missing actual-run artifact: {path}")
    size = path.stat().st_size
    require(0 < size <= maximum, f"Artifact size outside bound: {path} ({size})")
    raw = path.read_bytes()
    value = json.loads(raw)
    require(isinstance(value, dict), f"Artifact root must be an object: {path}")
    return value, {"path": str(path), "bytes": size, "sha256": hashlib.sha256(raw).hexdigest()}, raw


def inspect(runtime: dict, replay: dict, platform: str) -> dict:
    require(runtime.get("platform") == platform, f"Expected actual {platform} runtime evidence")
    require(runtime.get("complete") is True, f"{platform} native match did not complete")
    ticks, rounds = runtime.get("ticks"), runtime.get("rounds")
    require(type(ticks) is int and 0 < ticks <= 100000, f"{platform} runtime tick bound")
    require(type(rounds) is int and 1 <= rounds <= 9, f"{platform} completed round bound")
    require(isinstance(runtime.get("finalHash"), str) and HASH.fullmatch(runtime["finalHash"]), f"{platform} final canonical hash missing")
    require(isinstance(runtime.get("content"), str) and HASH.fullmatch(runtime["content"]), f"{platform} content identity missing")
    header, commands = replay.get("Header"), replay.get("Commands")
    require(isinstance(header, dict) and isinstance(commands, list), f"{platform} replay schema missing")
    require(header.get("Version") == 4, f"{platform} expected shop-only replay format4")
    require(header.get("Build") == runtime.get("build") and header.get("ContentHash") == runtime.get("content"), f"{platform} runtime/replay build or content identity mismatch")
    config = header.get("Config")
    require(isinstance(config, dict) and config.get("Training") is False and config.get("Assist") is False and header.get("Assist") is False, f"{platform} replay must be an unassisted competitive match")
    require(0 < len(commands) <= 120000, f"{platform} replay command count bound")
    require(isinstance(commands[0], dict) and commands[0].get("Kind") == "preparation" and commands[0].get("Tick") == 0, f"{platform} replay does not start at match preparation")
    inputs, checkpoints, wallets, receipts, transactions = [], [], [], [], []
    previous_balance = None
    expected_tick = 0
    counts = Counter()
    for index, command in enumerate(commands):
        require(isinstance(command, dict), f"{platform} command {index} is not an object")
        kind, tick = command.get("Kind"), command.get("Tick")
        require(kind in {"preparation", "step", "fight", "settlement", "next"}, f"{platform} unknown command at {index}")
        require(type(tick) is int and tick == expected_tick, f"{platform} command tick discontinuity at {index}: {tick} != {expected_tick}")
        counts[kind] += 1
        balance = [command.get("Wallet0"), command.get("Wallet1")]
        require(all(type(v) is int and 0 <= v <= 3600 for v in balance), f"{platform} wallet outside canonical bounds at {index}")
        wallets.append([index, tick, kind, *balance])
        require("Debits" not in command, f"{platform} legacy combat debit field at {index}")
        typed = {key: command.get(key) for key in ["Events", "Skills", "Uses0", "Uses1", "Preparation", "Settlement"]}
        for key, maximum in [("Events",256),("Skills",64),("Uses0",1),("Uses1",1)]:
            require(isinstance(typed[key], list) and len(typed[key]) <= maximum, f"{platform} invalid {key} at {index}")
        for receipt in typed["Skills"]:
            require(isinstance(receipt,dict) and type(receipt.get("Tick")) is int and 0<=receipt["Tick"]<=tick and receipt.get("EarnerSeat") in (0,1) and type(receipt.get("Allowed")) is int and 0<=receipt["Allowed"]<=100, f"{platform} invalid skill receipt at {index}")
        if kind == "step":
            require(previous_balance == balance, f"{platform} bank changed during Fight at {index}")
        previous_balance = balance
        receipts.append([index, typed])
        checkpoint = command.get("Hash")
        require(isinstance(checkpoint, str) and (checkpoint == "" or HASH.fullmatch(checkpoint)), f"{platform} invalid canonical checkpoint at {index}")
        if checkpoint:
            checkpoints.append([index, tick, kind, checkpoint, *balance, typed])
        if kind == "step":
            directions = [command.get("Direction0"), command.get("Direction1")]
            buttons = [command.get("Buttons0"), command.get("Buttons1")]
            require(all(type(d) is int and 1 <= d <= 9 for d in directions), f"{platform} invalid input direction at {index}")
            require(all(type(b) is int and 0 <= b <= 63 for b in buttons), f"{platform} invalid six-button input mask at {index}")
            inputs.append([tick, directions[0], buttons[0], directions[1], buttons[1]])
            expected_tick += 1
            if expected_tick % 60 == 0:
                require(bool(checkpoint), f"{platform} missing periodic canonical hash at tick {expected_tick}")
        else:
            require(bool(checkpoint), f"{platform} transaction lacks canonical hash at {index}")
            transactions.append(command)
    require(expected_tick == ticks, f"{platform} replay/runtime final tick mismatch")
    require(counts["settlement"] == rounds and counts["preparation"] == rounds and counts["next"] == rounds - 1, f"{platform} incomplete round lifecycle in replay")
    final = commands[-1]
    require(final["Kind"] == "settlement" and final["Tick"] == ticks, f"{platform} final command is not completed match settlement")
    require(final["Hash"] == runtime["finalHash"], f"{platform} final replay/runtime canonical state differs")
    require([final["Wallet0"], final["Wallet1"]] == runtime.get("wallets"), f"{platform} final replay/runtime wallets differ")
    scores = runtime.get("scores")
    require(isinstance(scores, list) and len(scores) == 2 and all(type(v) is int and 0 <= v <= 18 for v in scores) and sum(scores) == 2 * rounds, f"{platform} scores violate completed-round point conservation")
    require(max(scores) >= 10 or rounds == 9, f"{platform} match stopped before its completion condition")
    return {"counts": dict(counts), "inputs": inputs, "checkpoints": checkpoints, "wallets": wallets, "receipts": receipts, "transactions": transactions}


def compare(windows: Path, linux: Path) -> dict:
    artifacts, runs = {}, {}
    for platform, folder in [("Windows", windows), ("Linux", linux)]:
        runtime, runtime_file, _ = load(folder / "runtime-result.json", 1024 * 1024)
        replay, replay_file, replay_bytes = load(folder / "full-match.replay.json", 64 * 1024 * 1024)
        artifacts[platform] = {"runtime": runtime_file, "replay": replay_file}
        runs[platform] = (runtime, replay, replay_bytes, inspect(runtime, replay, platform))
    wr, wp, wb, wc = runs["Windows"]
    lr, lp, lb, lc = runs["Linux"]
    for field in ["engine", "build", "content", "ticks", "finalHash", "wallets", "scores", "rounds", "freeKit"]:
        require(wr.get(field) == lr.get(field), f"Native runtime {field} differs: {wr.get(field)!r} != {lr.get(field)!r}")
    require(wp["Header"] == lp["Header"], "Native replay headers/configurations differ")
    require(wc["counts"] == lc["counts"], "Native replay command counts differ")
    for field in ["inputs", "wallets", "receipts", "checkpoints", "transactions"]:
        require(wc[field] == lc[field], f"Native replay {field} differ")
    require(wp["Commands"] == lp["Commands"], "Native replay full command records differ")
    require(wb == lb, "Native replay byte streams differ despite structural comparison")
    return {
        "passed": True, "comparison": "Completed exported native Windows versus native Linux simulation",
        "build": wr["build"], "contentHash": wr["content"], "seed": wp["Header"]["Seed"],
        "finalTick": wr["ticks"], "completedRounds": wr["rounds"], "finalCanonicalHash": wr["finalHash"],
        "finalWallets": wr["wallets"], "finalScoreHalfPoints": wr["scores"],
        "identicalReplayBytes": True, "replaySha256": hashlib.sha256(wb).hexdigest(), "replayBytes": len(wb),
        "commands": len(wp["Commands"]), "commandCounts": wc["counts"],
        "comparisons": {field: {"exactlyEqual": True, "records": len(wc[field]), "canonicalProjectionSha256": digest(wc[field])}
                        for field in ["inputs", "wallets", "receipts", "checkpoints", "transactions"]},
        "artifacts": artifacts,
        "boundary": "Both native exports completed this match using their respective OS runtimes. These runs are headless and accelerated; no render pacing, graphical target, physical controller, or second physical machine claim is made.",
        "displayModes": {"Windows": wr.get("display"), "Linux": lr.get("display")},
    }


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--windows", type=Path, default=ROOT / "reports/evidence/native-windows-headless")
    parser.add_argument("--linux", type=Path, default=ROOT / "reports/evidence/native-linux-headless")
    parser.add_argument("--evidence-dir", type=Path, default=ROOT / "reports/evidence/cross-platform-replay")
    args = parser.parse_args()
    args.evidence_dir.mkdir(parents=True, exist_ok=True)
    started = datetime.now(timezone.utc).isoformat()
    log = ["Native cross-platform replay comparison", "UTC " + started,
           "COMMAND " + json.dumps(sys.argv), "WINDOWS " + str(args.windows.resolve()), "LINUX " + str(args.linux.resolve())]
    try:
        result = compare(args.windows.resolve(), args.linux.resolve())
        for platform, files in result["artifacts"].items():
            for kind, artifact in files.items():
                log.append(f"{platform} {kind}: {artifact['path']} / {artifact['bytes']} bytes / SHA256 {artifact['sha256']}")
        for field, compared in result["comparisons"].items():
            log.append(f"MATCH {field}: {compared['records']} records / SHA256 {compared['canonicalProjectionSha256']}")
        log.append(f"PASS exact {result['commands']} commands, {result['finalTick']} input ticks, {result['completedRounds']} rounds; final state {result['finalCanonicalHash']}")
        log.append(result["boundary"])
        exit_code = 0
    except (ComparisonError, OSError, ValueError, KeyError, TypeError) as error:
        result = {"passed": False, "error": str(error), "windows": str(args.windows.resolve()), "linux": str(args.linux.resolve())}
        log.append("FAIL " + str(error))
        exit_code = 1
    result["utc"] = started
    result["exitCode"] = exit_code
    (args.evidence_dir / "result.json").write_text(json.dumps(result, indent=2) + "\n", encoding="utf-8")
    (args.evidence_dir / "process.log").write_text("\n".join(log) + "\n", encoding="utf-8")
    print("\n".join(log[-2:] if exit_code == 0 else log[-1:]))
    return exit_code


if __name__ == "__main__":
    raise SystemExit(main())
