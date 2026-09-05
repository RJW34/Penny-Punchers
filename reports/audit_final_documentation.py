"""Read-only audit of final delivery instructions and their exact status scope."""
from pathlib import Path
import datetime,hashlib,json,sys
ROOT=Path(__file__).resolve().parents[1]
def read(p):return (ROOT/p).read_text(encoding='utf-8-sig')
def obj(p):return json.loads(read(p))
def main():
    requirements=obj('acceptance/requirements.json')['requirements']
    candidate=obj('reports/RELEASE_CANDIDATE.json')
    records={r['requirement_id']:r for r in obj('reports/ACCEPTANCE_RESULTS.json')['records']}
    state=obj('reports/STATE.json')
    pending=[r['id'] for r in requirements if records[r['id']]['status']!='PASS']
    counts={tier:{'passed':sum(r['gate_class']==tier and records[r['id']]['status']=='PASS' for r in requirements),'total':sum(r['gate_class']==tier for r in requirements)} for tier in ['software','target_device','human']}
    checks=[]
    def check(name,value):checks.append({'name':name,'passed':bool(value)})
    check('state_matches_actual_ledger',state['blocked_requirements']==pending and state['verification']==counts)
    check('state_candidate_matches',state['game_build_ref']==candidate['build_ref'] and state['game_content_sha256']==candidate['content_sha256'])
    summary='; '.join(f"{tier}: {v['passed']}/{v['total']}" for tier,v in counts.items())
    for p in ['reports/BLOCKERS.md','reports/RESUME_PACKET.md','release_docs/VERIFICATION_STATUS.md']:
        text=read(p);check(p+' exact counts and remaining ids',summary in text and all(i in text for i in pending))
    check('manifest_identity_in_player_status',candidate['build_ref'] in read('release_docs/VERIFICATION_STATUS.md') and candidate['content_sha256'] in read('release_docs/VERIFICATION_STATUS.md'))
    check('root_launch_build_and_gate_instructions',all(x in read('README.md') for x in ['Play Strike Ledger.cmd','tools/build.py --export --test','tools/release_gate.py --tier software','tools/release_gate.py --tier all']))
    check('reproduction_entry_points_exist',all((ROOT/p).is_file() for p in ['Play Strike Ledger.cmd','tools/build.py','tools/run_scenario.py','tools/release_gate.py','dist/verification/README.md']))
    player=read('release_docs/README.md');check('player_native_launch_rules_and_data_instructions',all(x in player for x in ['StrikeLedger.exe','StrikeLedger.x86_64','600 CR','300 CR','3600 CR','per-user','private IP']))
    limits=read('release_docs/KNOWN_LIMITATIONS.md');check('explicit_hardware_human_linux_and_pacing_limits',all(x in limits for x in ['physical controllers','physical computers','No human player','llvmpipe','Dummy audio','21.09ms','168.27ms','492.36ms']))
    check('remaining_physical_and_human_not_inferred',all(records[x]['status']=='NOT_RUN' for x in ['DEVICE-003','DEVICE-004','HUMAN-001']))
    check('all82_requirements_accounted_for',set(records)=={r['id'] for r in requirements} and len(records)==82)
    check('balance_report_player_copy_exact',read('reports/BALANCE_NOTES.md')==read('release_docs/BALANCE_NOTES.md'))
    docs=['README.md','reports/STATE.json','reports/BLOCKERS.md','reports/RESUME_PACKET.md','reports/FEEL_CALIBRATION.md','reports/BUILD_LOG.md']+[p.relative_to(ROOT).as_posix() for p in (ROOT/'release_docs').rglob('*') if p.is_file()]
    check('player_controls_notices_and_licenses_exist',all((ROOT/p).is_file() and (ROOT/p).stat().st_size>100 for p in ['release_docs/CONTROLS.md','release_docs/ASSET_NOTICES.md','release_docs/licenses/GODOT_LICENSE.txt','release_docs/licenses/DOTNET_LICENSE.txt','release_docs/licenses/NOTO_OFL.txt']))
    result={'schema_version':1,'utc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'command':[sys.executable,'reports/audit_final_documentation.py'],'exit_code':0 if all(c['passed'] for c in checks) else 1,'passed':all(c['passed'] for c in checks),'scope':'Actual read-only document/content/status audit. No gameplay or hardware claims are created. The ledger itself is not hashed, preventing a recursive artifact hash.','build_ref':candidate['build_ref'],'content_sha256':candidate['content_sha256'],'verification':counts,'remaining_requirements':pending,'checks':checks,'document_sha256':{p:hashlib.sha256((ROOT/p).read_bytes()).hexdigest() for p in docs}}
    (ROOT/'reports/evidence/final-documentation-audit.json').write_text(json.dumps(result,indent=2)+'\n')
    print(json.dumps(result,indent=2));return result['exit_code']
if __name__=='__main__':raise SystemExit(main())
