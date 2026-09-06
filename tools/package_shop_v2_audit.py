"""Package current v2 evidence, preserving measured bytes and explicit exclusions.

Run --plan while reports are being finalized. Archive creation requires all software
requirements to pass and the completed Windows/UI/showcase/Linux movies. This is an
evidence companion to the source checkout and player packages, not a new test run.
"""
from __future__ import annotations

import argparse
from collections import Counter, defaultdict
from datetime import datetime, timezone
import hashlib
import json
from pathlib import Path, PurePosixPath
import re
import sys
import zipfile

ROOT = Path(__file__).resolve().parents[1]
EVIDENCE = "reports/evidence/"
LEDGER = "reports/SHOP_ONLY_V2_ACCEPTANCE.json"
HEX = re.compile(r"^[0-9a-fA-F]{64}$")
MAX_ASSET_BYTES = 2_000_000_000
CURRENT_DIRS = (
    "shop-v2-core-core-final", "shop-v2-core-full-current", "shop-v2-core-bait-current",
    "shop-v2-app-candidate-core", "shop-v2-app-candidate2-full", "shop-v2-objects-candidate2",
    "shop-v2-network-candidate", "shop-v2-network-candidate-full", "shop-v2-feel-candidate",
    "shop-v2-canonical-audit", "shop-v2-contract-candidate", "shop-v2-dispatch-candidate",
    "shop-v2-product-pilot-current", "shop-v2-crossover-pilot-current", "shop-v2-pilot-harness",
    "shop-v2-strategy-current", "shop-v2-strategy-core-current", "shop-v2-strategy-harness",
    "shop-v2-cross-platform-candidate", "shop-v2-package-candidate",
    "native-shop-v2-candidate-windows-headless", "native-shop-v2-candidate-linux-headless",
    "native-shop-v2-candidate-ui", "native-shop-v2-candidate-match",
    "native-shop-v2-candidate-showcase", "native-shop-v2-linux-candidate-5fps",
    "native-shop-v2-renamed-ui", "native-shop-v2-renamed-windows-headless", "native-shop-v2-renamed-linux-headless",
    "native-shop-v2-candidate-free", "native-shop-v2-candidate-network", "native-shop-v2-candidate-profile",
    "shop-v2-renamed-cross-platform", "shop-v2-renamed-package", "shop-v2-packaged-verifiers",
    "name-swap-audit", "pre-name-swap-candidate", "build-test", "shop-v2-manual-native", "shop-v2-restart-recovery",
)
MOVIES = (
    "native-shop-v2-renamed-ui", "native-shop-v2-candidate-match",
    "native-shop-v2-candidate-showcase", "native-shop-v2-linux-candidate-5fps",
)
REPORTS = (
    LEDGER, "reports/SHOP_ONLY_V2_CANDIDATE.json", "reports/RELEASE_CANDIDATE.json",
    "reports/ACCEPTANCE_RESULTS.json", "reports/UPGRADE_STATUS.json", "reports/UPGRADE_REVIEW.md",
    "reports/AUDIT_CROSSWALK.json", "reports/SHOP_V2_CORE_FINDINGS.json",
    "reports/SHOP_V2_CORE_FINDINGS.md", "reports/SHOP_ONLY_APP_REVIEW.md",
    "reports/CORE_INDEPENDENT_SOFTWARE_AUDIT.md", "reports/FEEL_CALIBRATION.md",
    "reports/BALANCE_NOTES.md", "reports/STATE.json", "reports/BLOCKERS.md",
    "reports/RESUME_PACKET.md", "reports/BUILD_LOG.md",
)
SIDECARS = (
    "shop-v2-action-effects.json", "shop-v2-action-effects-process.log",
    "shop-v2-candidate-assembly-identities.json", "shop-v2-final-candidate-binding.json",
    "shop-v2-independent-review.json", "shop-v2-independent-review.log",
    "shop-v2-pilot-analysis.json", "shop-v2-pilot-analysis-product-samples.corrected.jsonl",
    "shop-v2-pilot-process-results.json", "shop-v2-strategy-process-results.json",
    "shop-v2-strategy-restart-audit.json", "shop-v2-visual-review.json",
    "shop-v2-core-core-final-console.log", "shop-v2-core-full-current-console.log",
    "shop-v2-core-bait-current-console.log", "shop-v2-product-pilot-current-console.log",
    "shop-v2-crossover-pilot-current-console.log", "shop-v2-strategy-current-console.log",
    "shop-v2-strategy-core-current-console.log", "shop-v2-strict-candidate.log",
    "shop-v2-completed-movies-visual-review.json", "shop-v2-final-archive-integrity.json",
    "shop-v2-verification-archive.json", "shop-v2-final-software-gate.json", "shop-v2-final-all-gate.json",
)


def read(path: Path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def sha(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest()


class Inventory:
    def __init__(self, directories: list[str]):
        self.directories = set(directories)
        self.files: dict[str, dict] = {}
        self.origins: dict[str, set] = defaultdict(set)
        self.excluded: dict[str, dict] = {}
        self.queue: list[str] = []
        self.checked: set[str] = set()
        self.errors: list[str] = []
        self.references = 0
        self.resolved_references: list[dict] = []
        self.hashes: dict[Path, str] = {}

    def hash(self, path: Path) -> str:
        if path not in self.hashes:
            self.hashes[path] = sha(path)
        return self.hashes[path]

    def resolve_reference(self, value: str, origin: str, digest: str) -> str | None:
        """Locate exact bytes for relative manifest paths and retained snapshots."""
        before = ROOT / EVIDENCE / 'pre-name-swap-candidate'
        candidates = [(ROOT / origin).parent / value, ROOT / 'data' / value,
                      before / 'source-before' / value, before / 'source-before/data' / value,
                      before / Path(value).name]
        if origin.startswith(EVIDENCE) and '/' in origin[len(EVIDENCE):]:
            candidates.extend((ROOT / origin).parent.rglob(Path(value).name))
        if Path(value).suffix.lower() == '.dll':
            for directory in ('pre-name-swap-candidate', 'shop-v2-pilot-harness', 'shop-v2-strategy-harness'):
                candidates.extend((ROOT / EVIDENCE / directory).rglob(Path(value).name))
        for candidate in candidates:
            candidate = candidate.resolve()
            if candidate.is_relative_to(ROOT) and candidate.is_file() and self.hash(candidate) == digest.lower():
                resolved = candidate.relative_to(ROOT).as_posix()
                self.resolved_references.append(dict(recorded_path=value, resolved_path=resolved, sha256=digest, origin=origin))
                return resolved
        return None

    def historical_source(self, rel: str, origin: str) -> bool:
        # Only these already retained earlier reviews may describe older source.
        # Current ledger artifacts and current execution bytes never use this rule.
        origins = {'audit/upgrade-2026-09-05/PRESENTATION_FINDINGS.json',
                   'reports/SHOP_V2_CORE_FINDINGS.json',
                   'reports/evidence/shop-v2-independent-review.json',
                   'reports/evidence/shop-v2-canonical-audit/result.json',
                   'audit/upgrade-2026-09-05/SHOP_ONLY_APP_FINDINGS.json'}
        return origin in origins and rel.startswith(('assets/', 'data/', 'game/', 'src/', 'tools/'))

    def relative(self, value: str) -> str:
        value = value.replace("\\", "/")
        if "\x00" in value or ".." in PurePosixPath(value).parts:
            raise ValueError("Unsafe path: " + value)
        path = Path(value)
        target = (path if path.is_absolute() else ROOT / path).resolve()
        if not target.is_relative_to(ROOT):
            raise ValueError("Path leaves checkout: " + value)
        rel = target.relative_to(ROOT).as_posix()
        parts = PurePosixPath(rel).parts
        if any(p.lower() in {".git", ".ssh", ".aws", ".azure", "node_modules", ".cache", "__pycache__"} for p in parts):
            raise ValueError("Credential/cache path refused: " + rel)
        if target.suffix.lower() in {".pem", ".key", ".pfx"} or target.name.lower().startswith(".env"):
            raise ValueError("Secret-like path refused: " + rel)
        return rel

    def exclusion(self, rel: str) -> str | None:
        path = PurePosixPath(rel)
        if any('interrupted' in part or part in {'bin','obj','__pycache__'} for part in path.parts):
            return "Interrupted forensic files and generated build caches are preserved locally, not promoted into current verification."
        if path.suffix.lower() in {".avi", ".wav"}:
            return "Raw recording excluded; completed MP4 and decode/inspection evidence are archived separately."
        if rel.startswith("dist/"):
            return "Native player binaries belong to the separately published player ZIPs; recorded hashes retained."
        if rel.startswith("reports/history/") or rel.startswith("release_docs/history/"):
            return "Historical appendix is preserved in Git; not current v2 execution evidence."
        if rel.startswith(EVIDENCE):
            tail = rel[len(EVIDENCE):]
            if "/" in tail and tail.split("/", 1)[0] not in self.directories:
                return "Unselected historical, development, interrupted, or duplicate evidence; not current execution proof."
        return None

    def exclude(self, rel: str, reason: str, origin: str, expected: str | None = None):
        item = self.excluded.setdefault(rel, dict(path=rel, reason=reason, referenced_by=[]))
        if origin not in item["referenced_by"]:
            item["referenced_by"].append(origin)
        path = ROOT / rel
        if path.resolve().is_relative_to(ROOT) and path.is_file():
            item["bytes"] = path.stat().st_size
        if expected:
            item["recorded_sha256"] = expected

    def add(self, value: str, origin: str, digest=None, size=None, direct=False):
        try:
            if not direct and origin.startswith(EVIDENCE + 'pre-name-swap-candidate/'):
                if digest:
                    resolved = self.resolve_reference(value, origin, digest)
                    if resolved:
                        self.add(resolved, 'Exact retained snapshot referenced by ' + origin, digest, size)
                        return
                self.exclude(value, 'Nested reference from a preserved earlier-candidate report; not a current ledger assertion. Original report bytes and recorded identity are retained.', origin, digest)
                return
            if not direct and Path(value).is_absolute() and not Path(value).resolve().is_relative_to(ROOT):
                self.exclude(value, 'Historical external asset reference; external bytes are not read or bundled.', origin, digest)
                return
            rel = self.relative(value)
            path = ROOT / rel
            reason = self.exclusion(rel)
            if direct and rel.startswith('dist/'):
                if not path.is_file() or self.hash(path) != digest:
                    raise ValueError('Current separately published binary SHA-256 mismatch: ' + rel)
                self.references += 1
                self.exclude(rel, reason, origin, digest)
                return
            if reason and not direct:
                self.exclude(rel, reason, origin, digest)
                return
            if reason:
                raise ValueError("Current ledger directly references excluded material: " + rel)
            if digest and not direct and (not path.is_file() or self.hash(path) != digest.lower()):
                resolved = self.resolve_reference(value, origin, digest)
                if resolved:
                    rel, path = resolved, ROOT / resolved
                elif self.historical_source(rel, origin):
                    self.exclude(rel, 'Earlier source hash retained in this historical review; current source is in Git. This mismatching historical source is not promoted to current execution evidence.', origin, digest)
                    return
            if not path.is_file():
                raise ValueError("Missing referenced file: " + rel + ' from ' + origin)
            if path.is_symlink():
                raise ValueError("Symlinks are not archived: " + rel)
            if rel not in self.files:
                self.files[rel] = dict(path=rel, bytes=path.stat().st_size, sha256=self.hash(path))
                if path.suffix.lower() == ".json":
                    self.queue.append(rel)
            entry = self.files[rel]
            if digest is not None:
                if not isinstance(digest, str) or not HEX.fullmatch(digest) or entry["sha256"] != digest.lower():
                    raise ValueError("Referenced SHA-256 mismatch: " + rel + " from " + origin)
                self.references += 1
            if size is not None and entry["bytes"] != size:
                raise ValueError("Referenced byte size mismatch: " + rel + " from " + origin)
            self.origins[rel].add(origin)
        except (ValueError, OSError) as error:
            self.errors.append(str(error))

    def structured(self, value, origin: str, branch=""):
        if isinstance(value, dict):
            # Candidate source/binary inventories are checksums of the separate
            # source checkout and player artifacts, not a request to bundle them.
            if branch in {"source_inputs", "native_binaries"}:
                return
            p = value.get("path")
            digest = value.get("sha256")
            if isinstance(p, str) and isinstance(digest, str) and HEX.fullmatch(digest):
                self.add(p, origin, digest, value.get("bytes"))
            if isinstance(value.get("source"), str) and isinstance(value.get("source_sha256"), str):
                self.add(value["source"], origin, value["source_sha256"])
            for key, child in value.items():
                if key in {"source_inputs", "native_binaries"}:
                    if isinstance(child, list):
                        for entry in child:
                            if isinstance(entry, dict) and "path" in entry:
                                rel = self.relative(entry["path"])
                                self.exclude(rel, "Source/player inventory retained in candidate manifest; obtain full bytes from Git/player package.", origin, entry.get("sha256"))
                    continue
                # Native media uses {repository/path: digest}, while network
                # matrix reports use {basename: digest} in nested profile dirs.
                if isinstance(child, str) and HEX.fullmatch(child) and ("/" in key or "." in key):
                    if "/" in key or "\\" in key:
                        self.add(key, origin, child)
                    else:
                        folder = (ROOT / origin).parent
                        matches = [f for f in folder.rglob(key) if f.is_file() and self.hash(f) == child.lower()]
                        if matches:
                            for found in matches:
                                self.add(found.relative_to(ROOT).as_posix(), origin, child)
                        elif branch in {"artifacts", "sha256", "hashes"}:
                            self.errors.append("Unresolved hashed artifact " + key + " in " + origin)
                else:
                    self.structured(child, origin, key)
        elif isinstance(value, list):
            for child in value:
                self.structured(child, origin, branch)
        elif isinstance(value, str) and branch in {"evidence", "artifacts", "trace", "traces", "report", "log"}:
            normalized = value.replace("\\", "/")
            if normalized.startswith(("reports/", "audit/", "tools/", "src/", "data/", "docs/", "release_docs/")):
                self.add(normalized, origin)

    def expand(self):
        while self.queue:
            rel = self.queue.pop()
            if rel in self.checked:
                continue
            self.checked.add(rel)
            try:
                self.structured(read(ROOT / rel), rel)
            except (ValueError, OSError) as error:
                self.errors.append(str(error) + " in " + rel)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--plan", action="store_true", help="Audit prospective contents; do not create a ZIP or claim final readiness.")
    parser.add_argument("--include-evidence-dir", action="append", default=[], help="Additional explicitly reviewed current folder, relative to reports/evidence.")
    parser.add_argument("--output", default="dist/Penny-Punchers-shop-v2-audit-evidence.zip")
    parser.add_argument("--result", default="reports/evidence/shop-v2-audit-package-result.json")
    args = parser.parse_args()
    inventory = Inventory(list(CURRENT_DIRS) + args.include_evidence_dir)
    output = ROOT / inventory.relative(args.output)
    result_path = ROOT / inventory.relative(args.result)
    ledger = read(ROOT / LEDGER)
    candidate = read(ROOT / "reports/RELEASE_CANDIDATE.json")
    blockers = []
    if ledger["candidate_id"] != candidate["build_ref"]:
        blockers.append("Ledger/source candidate mismatch")
    records = ledger["records"]
    software = [r for r in records if r["evidence_scope"] == "software"]
    original = read(ROOT / 'reports/ACCEPTANCE_RESULTS.json')['records']
    original_requirements = read(ROOT / 'acceptance/requirements.json')['requirements']
    original_software = {r['id'] for r in original_requirements if r['gate_class'] == 'software'}
    if len(original) != 82 or len({r['requirement_id'] for r in original}) != 82 or len(original_software) != 76:
        blockers.append('Expected the 82 original requirements including 76 software records')
    if any(r['status'] != 'PASS' for r in original if r['requirement_id'] in original_software):
        blockers.append('Original software acceptance is incomplete')
    if len(records) != 61 or len({r["id"] for r in records}) != 61 or len(software) != 58:
        blockers.append("Expected 61 original requirements including 58 software records")
    if any(r["status"] != "PASS" for r in software):
        blockers.append("Current software evidence is incomplete: " + ", ".join(r["id"] for r in software if r["status"] != "PASS"))
    for path in REPORTS:
        if (ROOT / path).is_file():
            inventory.add(path, "current review entry point")
        else:
            blockers.append("Missing current review entry point: " + path)
    inventory.add("tools/package_shop_v2_audit.py", "reproducible packager")
    for path in sorted((ROOT / "release_docs").glob("*.md")):
        inventory.add(path.relative_to(ROOT).as_posix(), "current player documentation")
    inventory.structured(ledger["requirements"], LEDGER)
    for row in records:
        for artifact in row.get("artifacts", []):
            inventory.add(artifact["path"], row["id"], artifact["sha256"], artifact.get("bytes"), direct=True)
    for row in original:
        for artifact in row.get('artifacts', []):
            inventory.add(artifact['path'], row['requirement_id'], artifact['sha256'], artifact.get('bytes'), direct=True)
    for name in SIDECARS:
        if (ROOT / EVIDENCE / name).is_file():
            inventory.add(EVIDENCE + name, "current result sidecar")
    for directory in sorted(inventory.directories):
        if "/" in directory or "\\" in directory or directory in {".", ".."}:
            raise ValueError("Evidence directory must be a single explicit folder name: " + directory)
        for path in sorted((ROOT / EVIDENCE / directory).rglob("*")):
            if path.is_file():
                inventory.add(path.relative_to(ROOT).as_posix(), "complete current evidence directory")
    # Record unselected folders at directory scope, without reading/compressing
    # huge interrupted AVI files or pretending those attempts completed.
    for folder in sorted((ROOT / EVIDENCE).iterdir()):
        if folder.is_dir() and "shop-v2" in folder.name and folder.name not in inventory.directories:
            inventory.exclude(folder.relative_to(ROOT).as_posix() + "/", "Historical/development/interrupted/duplicate directory intentionally outside current companion scope.", "selection policy")
    for movie in MOVIES:
        folder = ROOT / EVIDENCE / movie
        validation = folder / "media-validation.json"
        if not validation.is_file():
            blockers.append("Await completed validated movie: " + movie)
            continue
        media = read(validation)
        if media.get("passed") is not True or media.get("complete_video_decode") is not True or not (folder / "recording.mp4").is_file():
            blockers.append("Incomplete/undecoded movie: " + movie)
    inventory.expand()
    files = [dict(v, included_because=sorted(inventory.origins[k])) for k, v in sorted(inventory.files.items())]
    # No generated plan/result may self-ingest on the next invocation.
    if args.result in inventory.files or args.output in inventory.files:
        inventory.errors.append("Archive/result recursively selected itself")
    now = datetime.now(timezone.utc).isoformat()
    manifest = dict(format="penny-punchers-shop-v2-audit-evidence-v1", created_utc=now,
                    candidate_id=candidate["build_ref"], canonical_sha256=candidate["content_sha256"],
                    ledger=dict(path=LEDGER, sha256=sha(ROOT / LEDGER), summary=ledger["summary"]),
                    scope="Current exact-byte evidence companion. No tests or media are synthesized by packaging. Source and native player packages are published separately.",
                    command=[sys.executable, "tools/package_shop_v2_audit.py", *sys.argv[1:]],
                    file_count=len(files), uncompressed_bytes=sum(f["bytes"] for f in files),
                    checked_structured_hash_references=inventory.references, files=files,
                    resolved_historical_or_relative_references=inventory.resolved_references,
                    exclusions=list(inventory.excluded.values()),
                    external_limits="Human balance/feel, physical controllers, and two physical PCs remain external gates. Linux WSLg is one physical PC; 5fps recording is platform smoke, not a pacing benchmark.")
    result = dict(format="shop-v2-audit-package-result", utc=now, plan_only=args.plan,
                  ready=not blockers and not inventory.errors, blockers=blockers,
                  errors=sorted(set(inventory.errors)), candidate_id=candidate["build_ref"],
                  ledger_sha256=manifest["ledger"]["sha256"], file_count=len(files),
                  uncompressed_bytes=manifest["uncompressed_bytes"],
                  checked_structured_hash_references=inventory.references,
                  exclusion_count=len(inventory.excluded), archive_created=False)
    if not args.plan and result["ready"]:
        if output.exists():
            raise ValueError("Refusing to overwrite an existing evidence archive: " + str(output))
        output.parent.mkdir(parents=True, exist_ok=True)
        readme = """# Penny Punchers — current shop-only v2 audit evidence

This companion preserves actual evidence bytes. It does not run a test or turn an
interrupted run into a result. See AUDIT_EVIDENCE_MANIFEST.json for the exact source
candidate, canonical data identity, ledger SHA, files and deliberate exclusions.

Extract into a separate checkout of the matching published source, retaining the
repository-relative paths. Do not overwrite a working checkout with newer evidence.
Begin with reports/SHOP_ONLY_V2_ACCEPTANCE.json (61 original SO requirements), then
reports/AUDIT_CROSSWALK.json (91 PP dispositions) and reports/SHOP_V2_CORE_FINDINGS.md.
PASS records are scoped software results; read their remaining/observation fields.

The final display-name swap has fresh executable/UI/package checks and an explicit
source, mechanical-data and Core-method comparison in reports/evidence/name-swap-audit.
Earlier v2 pilots and recordings keep their original names and build/content/replay
identities. They are not silently rewritten as new runs. The archive manifest records
exact snapshot aliases and unavailable historical source references separately;
current ledger artifact hashes are always checked strictly.

Validate every archived file against the manifest before relying on it. The source
candidate manifest records source/player hashes; full Git source and Windows/Linux
player ZIPs are separate release assets. This companion is not a replacement for
those binaries and does not reproduce machine-dependent native graphics by itself.

The full product, paired-payout and strategy traces are under the current pilot and
strategy directories. Read shop-v2-pilot-analysis.json before interpreting the raw
product rows: an A/C reporting-label correction is explicit, hashed and separate;
raw executions were not rewritten. Preserved managed pilot/strategy harnesses and
recorded commands permit independent replay in the matching .NET environment.

Completed MP4s include process results, full decode logs and actual frame captures.
Raw AVI/WAV and interrupted/development/historical duplicate recordings are excluded
and listed. Linux WSLg/llvmpipe 5fps is platform smoke on the same PC, not a rendering
performance or two-computer claim. Human playtest, physical-controller and actual
two-physical-PC gates remain explicitly unperformed unless the ledger says otherwise.
"""
        with zipfile.ZipFile(output, "w", compression=zipfile.ZIP_DEFLATED, compresslevel=5, allowZip64=True) as archive:
            for entry in files:
                path = ROOT / entry["path"]
                if path.stat().st_size != entry["bytes"] or sha(path) != entry["sha256"]:
                    raise ValueError("Evidence changed during packaging: " + entry["path"])
                archive.write(path, entry["path"])
            archive.writestr("AUDIT_EVIDENCE_MANIFEST.json", json.dumps(manifest, indent=2, ensure_ascii=False) + "\n")
            archive.writestr("AUDIT_EVIDENCE_README.md", readme)
        with zipfile.ZipFile(output) as archive:
            bad = archive.testzip()
            if bad:
                raise ValueError("ZIP CRC failure: " + bad)
            names = archive.namelist()
            if len(names) != len(set(names)) or len(names) != len(files) + 2:
                raise ValueError("Duplicate/missing archive entries")
        if output.stat().st_size >= MAX_ASSET_BYTES:
            raise ValueError("Archive exceeds the requested under-2GB release-asset budget")
        result.update(archive_created=True, archive=output.relative_to(ROOT).as_posix(),
                      archive_bytes=output.stat().st_size, archive_sha256=sha(output),
                      crc_verified=True, manifest_sha256=hashlib.sha256((json.dumps(manifest, indent=2, ensure_ascii=False) + "\n").encode()).hexdigest())
    result_path.parent.mkdir(parents=True, exist_ok=True)
    result_path.write_text(json.dumps(result, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    print(json.dumps(result, indent=2, ensure_ascii=False))
    return 0 if args.plan and not inventory.errors or result["archive_created"] else 1


if __name__ == "__main__":
    raise SystemExit(main())
