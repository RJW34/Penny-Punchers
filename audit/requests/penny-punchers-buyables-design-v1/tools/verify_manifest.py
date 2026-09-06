"""Verify the delivery files; a changed design legitimately needs a new manifest."""
import hashlib
import json
from pathlib import Path
import sys
root=Path(__file__).resolve().parents[1]
manifest=json.loads((root/"MANIFEST.json").read_text(encoding="utf-8"))
failures=[]
for entry in manifest["files"]:
    rel=Path(entry["path"])
    if rel.is_absolute() or ".." in rel.parts:
        failures.append("unsafe path"); continue
    p=root/rel
    if not p.is_file(): failures.append(f"missing: {rel}"); continue
    b=p.read_bytes()
    if len(b)!=entry["bytes"] or hashlib.sha256(b).hexdigest()!=entry["sha256"]:
        failures.append(f"changed: {rel}")
if failures:
    print("\n".join(failures));sys.exit(1)
print(f"PASS: {len(manifest['files'])} delivery files match SHA-256/size.")
