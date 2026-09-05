#!/usr/bin/env python3
"""Preview or copy canonical JSON into the engine's generated content folder.

Run validate_pack.py first. Never writes source JSON. --apply only replaces a folder with
this tool's ownership marker; preserves unknown existing files by refusing overwrite.
"""
from __future__ import annotations
import argparse,hashlib,json,shutil,tempfile
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
MARKER='.strike-ledger-generated-content'

def copy_content(root:Path,apply:bool=False)->dict:
    root=root.resolve();src=root/'data';dest=root/'game/GeneratedData'
    if src.is_symlink() or dest.is_symlink() or not dest.resolve().is_relative_to(root):raise ValueError('Unsafe/symlink content path')
    files=sorted(p for p in src.rglob('*.json') if p.is_file())
    if not files:raise ValueError('No canonical content')
    entries=[]
    for p in files:
        if p.is_symlink() or not p.resolve().is_relative_to(src):raise ValueError('Unsafe source file')
        entries.append({'path':p.relative_to(src).as_posix(),'sha256':hashlib.sha256(p.read_bytes()).hexdigest()})
    digest=hashlib.sha256(json.dumps(entries,separators=(',',':'),sort_keys=True).encode()).hexdigest()
    result={'scope':'CONTENT_COPY_ONLY','mode':'APPLY' if apply else 'DRY_RUN','files':len(entries),'content_sha256':digest,'destination':'game/GeneratedData'}
    if not apply:return result
    if dest.exists() and not (dest/MARKER).is_file():raise ValueError('Existing destination has no ownership marker; refusing overwrite')
    if dest.exists():
        old=json.loads((dest/'content_manifest.json').read_text(encoding='utf-8'))
        allowed={e['path'] for e in old['files']}|{MARKER,'content_manifest.json'}
        for p in dest.rglob('*'):
            if p.is_symlink() or (p.is_file() and p.relative_to(dest).as_posix() not in allowed):raise ValueError('Unknown existing generated file; refusing overwrite')
    dest.parent.mkdir(parents=True,exist_ok=True)
    temp=Path(tempfile.mkdtemp(prefix='.content-staging-',dir=dest.parent))
    try:
        for item in entries:
            target=temp/item['path'];target.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(src/item['path'],target)
        (temp/MARKER).write_text('Strike Ledger generated content v1\n',encoding='utf-8')
        (temp/'content_manifest.json').write_text(json.dumps({'version':1,'content_sha256':digest,'files':entries},indent=2)+'\n',encoding='utf-8')
        if dest.exists():shutil.rmtree(dest)
        temp.rename(dest)
    finally:
        if temp.exists():shutil.rmtree(temp)
    return result
if __name__=='__main__':
    ap=argparse.ArgumentParser(description=__doc__);ap.add_argument('--apply',action='store_true');args=ap.parse_args()
    try:print(json.dumps(copy_content(ROOT,args.apply),indent=2))
    except Exception as ex:ap.exit(1,f'CONTENT COPY FAILED: {ex}\n')
