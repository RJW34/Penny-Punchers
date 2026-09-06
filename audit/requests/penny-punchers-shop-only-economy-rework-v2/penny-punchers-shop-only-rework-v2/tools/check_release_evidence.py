"""Structural evidence binding only; actual test execution/review is still required."""
from pathlib import Path
import argparse,hashlib,json,re,sys
ROOT=Path(__file__).resolve().parents[1]
def check(candidate,ledger,requirements,artifact_root,tier='all'):
    failures=[]
    def fail(msg):failures.append(msg)
    for k in ('source_sha256','content_sha256'):
        if not isinstance(candidate.get(k),str) or not re.fullmatch('[a-f0-9]{64}',candidate[k]):fail('Missing/invalid '+k)
    if not candidate.get('candidate_id') or not candidate.get('build_id'):fail('No actual candidate/build ID')
    if ledger.get('candidate_id')!=candidate.get('candidate_id'):fail('Ledger/candidate mismatch')
    records=ledger.get('records',[]);by={r.get('id'):r for r in records}
    if len(by)!=len(records):fail('Duplicate requirement records')
    for req in requirements['requirements']:
        if tier!='all' and req['layer']!=tier:continue
        row=by.get(req['id'])
        if not row or row.get('status')!='PASS':fail(req['id']+': NOT_RUN or not passed');continue
        for k in ('source_sha256','content_sha256','build_id'):
            if row.get(k)!=candidate.get(k):fail(req['id']+': stale '+k)
        if not row.get('command_or_session') or not row.get('observed_utc') or row.get('exit_code')!=0:fail(req['id']+': missing observation metadata')
        if row.get('evidence_scope')!=req['layer']:fail(req['id']+': wrong scope (oracle/seed is not native evidence)')
        artifacts=row.get('artifacts',[])
        if not artifacts:fail(req['id']+': no artifacts')
        for art in artifacts:
            raw=Path(art.get('path',''));path=(artifact_root/raw).resolve()
            if raw.is_absolute() or not path.is_relative_to(artifact_root.resolve()) or not path.is_file():fail(req['id']+': missing/unsafe artifact');continue
            b=path.read_bytes()
            if not b or art.get('sha256')!=hashlib.sha256(b).hexdigest():fail(req['id']+': empty/hash mismatch')
    return failures
if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('--candidate',type=Path,default=ROOT/'reports/CANDIDATE.template.json');p.add_argument('--ledger',type=Path,default=ROOT/'reports/evidence_ledger.json');p.add_argument('--artifact-root',type=Path,default=ROOT);p.add_argument('--tier',choices=['all','software','device','human'],default='all');a=p.parse_args()
    failures=check(json.loads(a.candidate.read_text()),json.loads(a.ledger.read_text()),json.loads((ROOT/'acceptance/requirements.json').read_text()),a.artifact_root,a.tier)
    print(json.dumps({'structural_evidence_gate':'FAIL' if failures else 'PASS','tier':a.tier,'failures':failures,'note':'This is not an independent game execution or balance certification.'},indent=2));sys.exit(bool(failures))
