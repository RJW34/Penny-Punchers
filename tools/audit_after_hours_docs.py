"""Audit concrete player/source documentation after the native graphics verification."""
from pathlib import Path
import datetime,hashlib,json
ROOT=Path(__file__).resolve().parents[1]
def read(name):return (ROOT/name).read_text(encoding='utf-8-sig')
def main():
    candidate=json.loads(read('reports/RELEASE_CANDIDATE.json'));checks=[]
    def check(name,value):checks.append({'check':name,'passed':bool(value)})
    check('root launch script targets actual Windows executable','dist\\StrikeLedger\\windows' in read('Play Strike Ledger.cmd') and (ROOT/'dist/StrikeLedger/windows/StrikeLedger.exe').is_file())
    check('player rules and controls included',all((ROOT/'release_docs'/name).is_file() for name in ['README.md','CONTROLS.md','KNOWN_LIMITATIONS.md','ASSET_NOTICES.md','AFTER_HOURS.md','VERIFICATION_STATUS.md']))
    check('current source candidate stated in player verification',candidate['build_ref'] in read('release_docs/VERIFICATION_STATUS.md'))
    check('three external checks remain explicit',all(name in read('release_docs/VERIFICATION_STATUS.md') for name in ['DEVICE-003','DEVICE-004','HUMAN-001']))
    check('art animation and matte scope described',all(word in read('release_docs/AFTER_HOURS.md') for word in ['19 bitmap','159','98 moves','in-between','magenta','unchanged']))
    check('runtime font and software notices preserved',all((ROOT/'release_docs/licenses'/name).is_file() for name in ['NOTO_OFL.txt','GODOT_LICENSE.txt','GODOT_COPYRIGHT.txt','DOTNET_LICENSE.txt','DOTNET_THIRD_PARTY_NOTICES.txt']))
    check('current renderer is described as bitmap art','original vector animation' not in read('release_docs/KNOWN_LIMITATIONS.md') and 'original vector characters' not in read('release_docs/README.md'))
    manifest=json.loads(read('assets/AFTER_HOURS_ASSETS.json'))
    check('all19 shipped bitmap hashes match asset register',manifest['count']==19 and all(hashlib.sha256((ROOT/a['path']).read_bytes()).hexdigest()==a['sha256'] and a['path'] in read('assets/ASSET_REGISTER.csv') for a in manifest['assets']))
    originals=json.loads(read('design/after-hours-32bit/ASSET_MANIFEST.json'))
    check('all22 original design board bytes preserved',len(originals['assets'])==22 and all(hashlib.sha256((ROOT/'design/after-hours-32bit'/a['path']).read_bytes()).hexdigest()==a['sha256'] for a in originals['assets']))
    check('current source continuation record included',candidate['build_ref'] in read('reports/RESUME_PACKET.md') and candidate['build_ref'] in read('reports/AFTER_HOURS_INTEGRATION.md'))
    check('new pacing and Linux scope accurately disclosed',all(word in read('release_docs/KNOWN_LIMITATIONS.md') for word in ['After Hours','llvmpipe','human']))
    paths=[ROOT/'README.md',ROOT/'assets/ASSET_REGISTER.csv',ROOT/'assets/AFTER_HOURS_ASSETS.json']
    paths+=sorted(p for p in (ROOT/'release_docs').rglob('*') if p.is_file())
    paths+=[ROOT/'reports'/name for name in ['AFTER_HOURS_INTEGRATION.md','AFTER_HOURS_UI_AND_RENDER_REVIEW.md','AFTER_HOURS_GAMEPLAY_BOUNDARY_REVIEW.md','RESUME_PACKET.md','STATE.json','BLOCKERS.md','BUILD_LOG.md']]
    hashes={p.relative_to(ROOT).as_posix():hashlib.sha256(p.read_bytes()).hexdigest() for p in paths}
    passed=all(c['passed'] for c in checks)
    result={'utc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'passed':passed,'build_ref':candidate['build_ref'],'reviewer':'Codex root documentation and artifact audit','checks':checks,'document_sha256':hashes}
    (ROOT/'reports/evidence/after-hours-documentation-audit.json').write_text(json.dumps(result,indent=2)+'\n')
    print(json.dumps({'passed':passed,'checks':len(checks),'failed':[c for c in checks if not c['passed']]}))
    return 0 if passed else 1
if __name__=='__main__':raise SystemExit(main())
