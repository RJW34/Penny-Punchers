"""Verify final documentation repack without repeating unchanged native gameplay."""
from pathlib import Path
import argparse,hashlib,json,zipfile,datetime
ROOT=Path(__file__).resolve().parents[1]
def digest(data):return hashlib.sha256(data).hexdigest()
def main():
    parser=argparse.ArgumentParser();parser.add_argument('--prior-label',default='package-verification');parser.add_argument('--label',default='final-archive-integrity');args=parser.parse_args()
    for value in [args.prior_label,args.label]:
        assert value and all(c in 'abcdefghijklmnopqrstuvwxyz0123456789-_' for c in value),'Invalid evidence label'
    candidate=json.loads((ROOT/'reports/RELEASE_CANDIDATE.json').read_text())
    prior=json.loads((ROOT/'reports/evidence'/args.prior_label/'result.json').read_text())
    assert prior['passed'] and all(p['passed'] for p in prior['results'])
    results=[]
    for platform in ['windows','linux']:
        archive=ROOT/'dist'/f'Penny-Punchers-{platform}-x86_64.zip'
        prefix=f'dist/StrikeLedger/{platform}/'
        expected={p['path'].removeprefix(prefix):p['sha256'] for p in candidate['native_binaries'] if p['path'].startswith(prefix)}
        prior_run=next(p for p in prior['results'] if p['platform']==platform)
        extracted=Path(prior_run['extraction'])
        with zipfile.ZipFile(archive) as z:
            assert z.testzip() is None
            manifest=z.read('Penny-Punchers/SHA256SUMS.txt').decode().splitlines()
            for row in manifest:
                sha,name=row.split('  ',1);assert digest(z.read('Penny-Punchers/'+name))==sha,name
            for name,sha in expected.items():
                assert digest(z.read('Penny-Punchers/'+name))==sha and digest((extracted/name).read_bytes())==sha,name
            assert z.read('Penny-Punchers/RELEASE_CANDIDATE.json')==(ROOT/'reports/RELEASE_CANDIDATE.json').read_bytes()
            for file in (ROOT/'release_docs').rglob('*'):
                if file.is_file():assert z.read('Penny-Punchers/docs/'+file.relative_to(ROOT/'release_docs').as_posix())==file.read_bytes()
            assert z.read('Penny-Punchers/docs/ACCEPTANCE_RESULTS.json')==(ROOT/'reports/ACCEPTANCE_RESULTS.json').read_bytes()
            if platform=='linux':assert z.getinfo('Penny-Punchers/StrikeLedger.x86_64').external_attr>>16&0o111
            results.append({'platform':platform,'archive':archive.relative_to(ROOT).as_posix(),'sha256':digest(archive.read_bytes()),'bytes':archive.stat().st_size,'zip_crc_valid':True,'manifest_files_verified':len(manifest),'native_binaries_identical_to_candidate_and_actual_fresh_launch':len(expected),'documents_match_current_release_docs':True,'acceptance_ledger_matches':True})
    result={'utc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'passed':True,'candidate':candidate['build_ref'],'scope':'Final ZIPs after documentation refresh. All native bytes match the prior actual fresh-extraction full-match runs; every final manifest file, current player document and ledger is verified. This is an integrity audit, not an additional gameplay run.','results':results}
    (ROOT/'reports/evidence'/(args.label+'.json')).write_text(json.dumps(result,indent=2)+'\n');print(json.dumps(result))
if __name__=='__main__':main()
