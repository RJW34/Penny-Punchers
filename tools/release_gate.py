#!/usr/bin/env python3
"""Fail-closed evidence completeness/integrity gate, NOT an oracle that evidence is truthful.

Review actual artifacts and reproduce commands. A fabricated log with a valid hash is still fabricated.
Initial scaffold delivery MUST fail this gate because no game has been built or verified.
"""
from __future__ import annotations
import argparse,datetime,hashlib,json,re,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
KINDS={'test_log','build_log','process_log','video','screenshot','replay','metrics','binary','human_feedback'}
HEX=re.compile(r'^[0-9a-f]{64}$')

def evaluate(root:Path,tier:str='software',ledger:Path|None=None)->dict:
    root=root.resolve();requirements=json.loads((root/'acceptance/requirements.json').read_text(encoding='utf-8'))['requirements']
    allids={r['id'] for r in requirements};selected={r['id']:r for r in requirements if r['mandatory'] and (tier=='all' or r['gate_class']==tier)}
    problems=[];records={}
    candidate_path=root/'reports/RELEASE_CANDIDATE.json'
    candidate={}
    if not candidate_path.is_file():problems.append('Missing release candidate identity')
    else:
        candidate=json.loads(candidate_path.read_text(encoding='utf-8'))
        if candidate.get('schema_version')!=1 or candidate.get('status')!='READY_FOR_VERIFICATION':
            problems.append('Release candidate has not been built and marked READY_FOR_VERIFICATION')
        if not isinstance(candidate.get('build_ref'),str) or not candidate['build_ref'].strip():
            problems.append('Release candidate build_ref missing')
        digest=candidate.get('content_sha256')
        if not isinstance(digest,str) or not HEX.fullmatch(digest):problems.append('Release candidate content hash missing')
        else:
            content=[]
            for path in sorted((root/'data').rglob('*.json')):
                if path.is_symlink() or not path.resolve().is_relative_to(root/'data'):
                    problems.append('Unsafe canonical data path');continue
                content.append({'path':path.relative_to(root/'data').as_posix(),'sha256':hashlib.sha256(path.read_bytes()).hexdigest()})
            actual=hashlib.sha256(json.dumps(content,separators=(',',':'),sort_keys=True).encode()).hexdigest()
            if not content or actual!=digest:problems.append('Release candidate does not match current canonical content')
    if not selected:problems.append('No requirements selected; refusing vacuous pass')
    data=json.loads((ledger or root/'reports/ACCEPTANCE_RESULTS.json').read_text(encoding='utf-8'))
    if data.get('schema_version')!=1:problems.append('Unsupported ledger version')
    for rec in data.get('records',[]):
        rid=rec.get('requirement_id')
        if rid not in allids:problems.append(f'Unknown requirement: {rid}');continue
        if rid in records:problems.append(f'Duplicate evidence record: {rid}');continue
        records[rid]=rec
    verified=[]
    for rid,req in selected.items():
        rec=records.get(rid)
        if rec is None:problems.append(f'{rid}: missing evidence');continue
        before=len(problems)
        if rec.get('build_ref')!=candidate.get('build_ref'):problems.append(f'{rid}: evidence belongs to a different build')
        if rec.get('content_sha256')!=candidate.get('content_sha256'):problems.append(f'{rid}: evidence belongs to different content')
        if rec.get('status')!='PASS':problems.append(f'{rid}: status {rec.get("status")}')
        for key in ['build_ref','platform','command']:
            if not isinstance(rec.get(key),str) or not rec[key].strip() or rec[key].upper() in ['NOT_RUN','UNKNOWN','SCAFFOLD_ONLY']:problems.append(f'{rid}: invalid {key}')
        if type(rec.get('exit_code')) is not int or rec['exit_code']!=0:problems.append(f'{rid}: no successful actual command exit')
        if not isinstance(rec.get('content_sha256'),str) or not HEX.fullmatch(rec['content_sha256']):problems.append(f'{rid}: invalid content hash')
        try:
            when=datetime.datetime.fromisoformat(rec.get('timestamp_utc','').replace('Z','+00:00'))
            if when.tzinfo is None or when.utcoffset()!=datetime.timedelta(0):raise ValueError('UTC timestamp required')
        except (ValueError,TypeError,AttributeError):problems.append(f'{rid}: invalid UTC timestamp')
        if req['gate_class']=='human' and not str(rec.get('reviewer','')).strip():problems.append(f'{rid}: missing actual human reviewer')
        kinds=set();paths=set()
        artifacts=rec.get('artifacts',[])
        if not isinstance(artifacts,list):problems.append(f'{rid}: artifacts must be array');artifacts=[]
        for a in artifacts:
            if not isinstance(a,dict):problems.append(f'{rid}: invalid artifact');continue
            kind=a.get('kind');relative=a.get('path');digest=a.get('sha256')
            if kind not in KINDS:problems.append(f'{rid}: unknown artifact kind');continue
            if not isinstance(relative,str) or not relative or '\\' in relative or ':' in relative:
                problems.append(f'{rid}: invalid artifact path');continue
            path=(root/relative).resolve()
            if Path(relative).is_absolute() or '..' in Path(relative).parts or not path.is_relative_to(root):
                problems.append(f'{rid}: unsafe artifact path');continue
            if relative in paths:problems.append(f'{rid}: duplicate artifact path');continue
            paths.add(relative)
            if not path.is_file() or path.stat().st_size==0:problems.append(f'{rid}: missing/empty artifact {relative}');continue
            if not isinstance(digest,str) or not HEX.fullmatch(digest) or hashlib.sha256(path.read_bytes()).hexdigest()!=digest:
                problems.append(f'{rid}: artifact hash mismatch {relative}');continue
            kinds.add(kind)
        missing=set(req['required_evidence_kinds'])-kinds
        if missing:problems.append(f'{rid}: missing artifact kinds {sorted(missing)}')
        if len(problems)==before:verified.append(rid)
    return {'scope':'EVIDENCE_COMPLETENESS_AND_INTEGRITY_ONLY','tier':tier,'status':'FAIL' if problems else 'PASS','required':len(selected),'verified_records':len(verified),'problems':problems,'truth_review':'Reproduce commands and inspect actual gameplay; file/hash validation alone cannot establish truth or feel.'}

def main()->int:
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('--tier',choices=['software','target_device','human','all'],default='software');p.add_argument('--root',type=Path,default=ROOT);p.add_argument('--ledger',type=Path);a=p.parse_args()
    try:r=evaluate(a.root,a.tier,a.ledger);print(json.dumps(r,indent=2));return 0 if r['status']=='PASS' else 1
    except Exception as ex:print(f'RELEASE GATE ERROR (fail closed): {type(ex).__name__}: {ex}',file=sys.stderr);return 2
if __name__=='__main__':raise SystemExit(main())
