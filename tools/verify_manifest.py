#!/usr/bin/env python3
"""Check delivered scaffold bytes. Intentional development changes will invalidate this original manifest."""
from pathlib import Path
import hashlib,json,sys
ROOT=Path(__file__).resolve().parents[1]
def verify(root:Path)->int:
    root=root.resolve();manifest=json.loads((root/'PACK_MANIFEST.json').read_text(encoding='utf-8'));seen=set()
    for item in manifest['files']:
        rel=item['path'];p=(root/rel).resolve()
        if rel in seen or not p.is_relative_to(root) or Path(rel).is_absolute():raise ValueError(f'Unsafe/duplicate manifest path: {rel}')
        seen.add(rel)
        if not p.is_file():raise ValueError(f'Missing file: {rel}')
        raw=p.read_bytes()
        if len(raw)!=item['bytes'] or hashlib.sha256(raw).hexdigest()!=item['sha256']:raise ValueError(f'Modified/corrupt file: {rel}')
    return len(seen)
if __name__=='__main__':
    try:print(f'PASS: original scaffold manifest matches {verify(ROOT)} files. This does not verify the game.');raise SystemExit(0)
    except (OSError,ValueError,KeyError) as ex:print(f'MANIFEST FAILED: {ex}',file=sys.stderr);raise SystemExit(1)
