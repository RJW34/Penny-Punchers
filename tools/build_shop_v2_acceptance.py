"""Bind reviewed v2 requirement evidence; missing executions remain NOT_RUN."""
import hashlib
import json
from collections import Counter
from datetime import datetime, timezone
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
REQ = 'audit/requests/penny-punchers-shop-only-economy-rework-v2/penny-punchers-shop-only-rework-v2/acceptance/requirements.json'
E = 'reports/evidence/'
CORE = E+'shop-v2-core-core-final/core-conformance.json'
FULL = E+'shop-v2-core-full-current/core-conformance.json'
APP = E+'shop-v2-app-candidate-core/'
APPF = E+'shop-v2-app-candidate2-full/'
UI = E+'native-shop-v2-candidate-ui/'
NET = E+'shop-v2-network-candidate/matrix-result.json'
NETF = E+'shop-v2-network-candidate-full/matrix-result.json'
REVIEW = E+'shop-v2-independent-review.json'
FEEL = E+'shop-v2-feel-candidate/feel-measurements.json'
IDENTITY = E+'shop-v2-final-candidate-binding.json'
BUILD = 'pp-shop-only-v2/3aaa9b7c6de09ecb685e805b0edd641b35d36972f2a74f4a45700dcf9d0360f0'
CONTENT = {'buyables_core':'0245f6813c2320a9081fd96fefb4fd600632c92ae8f2be8a56c049129f365734', 'buyables_full':'e9c73b68e2cc1a128ea6215eb35fcce75036a66deca708dd59d7699efc8ae1f8'}

def read(p): return json.loads((ROOT/p).read_text(encoding='utf-8-sig'))
def sha(p): return hashlib.sha256((ROOT/p).read_bytes()).hexdigest()
def write(p, value): (ROOT/p).write_text(json.dumps(value, indent=2, ensure_ascii=False)+'\n', encoding='utf-8')
def artifact(p):
    p=p.replace('\\','/')
    assert (ROOT/p).is_file() and (ROOT/p).stat().st_size, p
    return {'path':p,'sha256':sha(p),'bytes':(ROOT/p).stat().st_size}

def main():
    now=datetime.now(timezone.utc).isoformat()
    candidate=read('reports/RELEASE_CANDIDATE.json')
    source=candidate['build_ref'].split(':',1)[1]
    assert len(source)==64
    for entry in candidate['source_inputs']:
        assert sha(entry['path'])==entry['sha256'], 'Source changed: '+entry['path']
    native={r['path']:r['sha256'] for r in candidate['native_binaries']}
    identities=read(E+'shop-v2-candidate-assembly-identities.json')
    assembly_proof=[]
    for name in ('StrikeLedger.Core.dll','StrikeLedger.App.dll'):
        tested=next(x for x in identities if x['path'].endswith('/'+name))
        shipped=[p for p in native if p.endswith('/'+name)]
        assert len(shipped)==2
        assert sha(tested['path'])==tested['sha256']
        for path in shipped:
            assert sha(path)==native[path]==tested['sha256'], 'Tested/shipped mismatch: '+path
            assembly_proof.append(dict(path=path,sha256=native[path],mvid=tested['mvid'],tested_path=tested['path']))
    for path, expected in [(CORE,CONTENT['buyables_core']),(FULL,CONTENT['buyables_full'])]:
        report=read(path); assert report['content_hash']==expected and report['failed']==0
        assert report['core_assembly_id']==identities[0]['mvid']
    for path, expected in [(APP+'self-tests.json',CONTENT['buyables_core']),(APPF+'self-tests.json',CONTENT['buyables_full'])]:
        report=read(path); assert report['build']==BUILD and report['contentHash']==expected
        assert report['exitCode']==0 and all(x['passed'] for x in report['results'])
    for path in (NET,NETF): assert all(r['passed'] and r['exitCodes']==[0,0] and r['byteIdenticalCanonicalStateAndReplay'] for r in read(path)['results'])
    projection={'candidate_id':candidate['build_ref'],'source_sha256':source,'content_sha256':candidate['content_sha256'],'build_id':BUILD,'loaded_content_hashes':CONTENT,'candidate_manifest':artifact('reports/RELEASE_CANDIDATE.json')}
    write('reports/SHOP_ONLY_V2_CANDIDATE.json',projection)
    binding={'utc':now,'passed':True,'scope':'Executed source SHA checks and tested-to-shipped Core/App byte identity; does not inherit old graphical claims. Each runtime result must separately match current shell/PCK hashes.',**projection,'verified_source_inputs':len(candidate['source_inputs']),'assemblies':assembly_proof,'command':'python tools/build_shop_v2_acceptance.py'}
    write(IDENTITY,binding)

    core_map={r['id']:r for r in read('reports/SHOP_V2_CORE_FINDINGS.json')['ownedV2Requirements']}
    passed_scenarios={r['id'] for r in read(FULL)['results'] if r['pass']}
    specs={}
    def put(n, note, paths, scenarios=(), pending=''):
        specs[f'SO-{n:03}']={'note':note,'paths':list(paths),'scenarios':list(scenarios),'pending':pending}
    for n in range(4,35):
        r=core_map[f'SO-{n:03}']; scenarios=list(dict.fromkeys(r['scenarios']))
        assert set(scenarios)<=passed_scenarios, (n,set(scenarios)-passed_scenarios)
        put(n,'Actual production Core scenario execution: '+', '.join(scenarios)+'. '+r['remaining'],r['evidence'],scenarios)
    put(1,'Actual dirty-checkout intake, installed catalogs, ownership and preserved pre-v2 checkpoint are recorded; the pinned historical checkout was not restored.',['audit/upgrade-2026-09-05/SHOP_V2_INTAKE.json','audit/upgrade-2026-09-05/AUDIT_INTAKE.json'])
    put(2,'Read current authority and canonical metadata. Shop-only ownership, one-use permits, pending rewards and confirmed settlement supersede active direct-spend/no-income rules; historical files are explicitly labeled.',[REVIEW,'AGENTS.md','docs/07_DIRECT_SPEND_EDGE_CASES.md','docs/18_MIGRATION_FROM_PRIOR_PACK.md'])
    crosswalk=read('reports/AUDIT_CROSSWALK.json')
    assert len(crosswalk['rows'])==91 and len({r['id'] for r in crosswalk['rows']})==91
    put(3,'All 91 PP IDs retain dispositions and owner evidence, including partial, optional and external findings. Reconciled does not mean all 91 fixed or human approved. Final owner refresh is required before release closure.',['reports/AUDIT_CROSSWALK.json'],pending='Await final owner crosswalk refresh against the complete candidate evidence.')
    specs['SO-012']['paths'].append(REVIEW)
    specs['SO-019']['paths'].append(REVIEW)
    specs['SO-022']['paths'] += [FEEL,E+'shop-v2-feel-candidate/process.log']
    specs['SO-026']['note']='Actual competitive anti-air earns75 pending credits while bank remains600; exported GUI labels NEXT SHOP and confirmed settlement later deposits the receipt. Core also tests50/100 categories.'
    specs['SO-026']['paths'] += [UI+'shop-v2-presentation.json',UI+'shop-v2-01-actual-pending-antiair.png']
    specs['SO-034']['paths'] += [APP+'false-prepaid-startup.json',APP+'false-counter-reward.json',APP+'late-parry.json',REVIEW]
    specs['SO-034']['note']+=' App rollback retracts false prepaid use and false counter award; stable Add/Confirm/Cancel presentation keys prevent repeated confirmation from replaying cues. This is software cue behavior, not a physical speaker measurement.'
    put(35,'Actual two-peer commit/reveal validates mixed quoted carts and selected permits against content; rewards derive from input resimulation. Mismatched versions/content and forged seats fail closed.',[APP+'self-tests.json',NET,NETF])
    put(36,'Replay format4/protocol3 explicitly refuse legacy semantics. Malformed identity/size tests and legacy refusal preserve original files; current full matches replay exactly.',[APP+'legacy-refused.json',APP+'self-tests.json',APPF+'self-tests.json'])
    put(37,'Four actual independent-process complete matches cover RTT0/50/100/150ms, jitter0/20ms, loss0/1/3%, duplicates0/2/3%, reorder0/5/10%; one additional full-catalog match. Final canonical states and replay files are byte-identical. Both processes run on this PC, not two physical PCs.',[NET,NETF])
    put(38,'Actual oversized/malformed/forged UDP and replay tests reject hostile payloads; prediction is bounded to8 frames and history pruning is exercised through complete multi-round matches. This is bounded fuzz/regression coverage, not a proof against all hostile traffic.',[APP+'self-tests.json',APP+'upgrade-regressions.json',NET,FULL,REVIEW])
    put(39,'Exported GUI routes real controller events through registry-priced EX/super/rental cards, independent focus, save/load/repeat/ready, lineup and catalog. Software joypad events do not certify physical devices.',[UI+'controller-menu-flow.json',UI+'shop-v2-04-mixed-super-cart.png',UI+'buyables-04-ex-catalog.png'])
    put(40,'Actual GUI checks reject other-seat edits and retain visible valid drafts. Actual15-second UDP timeout commits the previous valid cart after an invalid replacement; it neither wipes the cart nor charges twice.',[UI+'controller-menu-flow.json',APP+'shop-timeout.json',REVIEW])
    put(41,'Public facts come only from confirmed events: actual jump, counter, parry, purchase/use and skill counts. Actual late terminal UDP reward is drained before result transition and recorded once. No behavioral prediction is invented.',[UI+'shop-v2-presentation.json',UI+'late-terminal-history.json',UI+'shop-v2-02-confirmed-prior-facts.png',REVIEW])
    put(42,'Actual purchased super is shown READY, becomes USED after one real command and rejects the second without a bank change. Licenses and future-shop awards are displayed; health-aperture evidence is separately included.',[UI+'shop-v2-presentation.json',UI+'shop-v2-05-super-ready.png',UI+'shop-v2-06-super-used.png',UI+'health-frame-evidence.json'])
    put(43,'Typed round-ledger equations reproduce opening bank, cart, saved bank, outcome grant, skill grant, clipping and closing bank. Atomic replay storage returns a structured success/failure result; actual result GUI opens only after replay verification.',[APP+'v2-round-ledger.json',APP+'upgrade-regressions.json',UI+'controller-menu-flow.json',UI+'ui-08-verified-match-result.png'])
    put(44,'Actual exported GUI software input tests cover default B/MK and rebound action buttons without combat pause, plus Start/Escape pause and release quarantine. These tests do not claim a plugged-in controller.',[UI+'controller-menu-flow.json'])
    put(45,'Actual legal input moves fighters to opposite corners, jumps and contact on all four stages; production camera contains the source-cell envelopes in the viewport.',[UI+'stage-camera-evidence.json',UI+'stage-foundry-opposite-corners.png',UI+'stage-marist_green-corner-jumps.png'])
    put(46,'Actual GUI opens the catalog before competitive preparation and routes Try into isolated training practice; real selected-item and free comparator inputs run there. Competitive timed shop is not paused by preview.',[UI+'controller-menu-flow.json',UI+'buyables-06-practice-menu.png',UI+'buyables-07-free-comparison.png'])
    put(47,'Production shared CommandEncoder demonstrates executable purchased capability:21 core/25 full policy option witnesses,196/220 legal demonstration cases. Bots inspect ownership and remaining uses, not combat-bank affordability. Action effects and derived routes are separately mapped.',[APP+'cpu-capability-traces.json',APPF+'cpu-capability-traces.json',APP+'training-demonstrations.json',APPF+'training-demonstrations.json',E+'shop-v2-action-effects.json'])
    put(48,'Actual App tests cover21 drill evaluators with no-input failures and genuine success traces, independent seat loadouts, owned/locked/used cases, checkpoint/reset/dummy RNG and recorded inputs. Exported GUI reaches equipment, training diagnostics and real practice. No human completion is claimed.',[APP+'self-tests.json',APPF+'self-tests.json',APP+'training-inputs.json',UI+'controller-menu-flow.json'])
    put(49,'Typed CSV/JSON round ledgers and native pilot traces include purchases, licenses, super uses, pending rewards, actual outcome/skill grants and next bank. Raw pilot mislabeled A payout headers have a preserved explicit corrected projection validated against input traces; no silent rewrite.',[APP+'v2-round-ledger.json',APP+'v2-round-ledger.csv',E+'shop-v2-pilot-analysis.json',E+'shop-v2-pilot-analysis-product-samples.corrected.jsonl'])
    put(50,'Current core26/full38 products and100/115 action nodes are implemented with actual effect/route witnesses. Development validation and human counterplay/price approval remain distinct; full-catalog selection is explicit.',[E+'shop-v2-action-effects.json',FULL,UI+'controller-menu-flow.json','reports/SHOP_V2_CORE_FINDINGS.json'])
    for n in (51,52,53):
        r=core_map[f'SO-{n:03}']; put(n,r['title']+': '+r['remaining'],r['evidence'],r['scenarios'])
    put(54,'Both-seat actual repeated CH/AA/precision bait and partial caps are recorded.64 additional legal-input strategy continuations across both catalogs compare unused permit, draw-seeking, chip-out and final cash-out from declared round9score4-4, seeds1/2 and both seats. Failed strategies remain in results; no human deterrence or optimal-play claim.',[FULL,E+'shop-v2-core-bait-current/core-conformance.json',E+'shop-v2-strategy-process-results.json',E+'shop-v2-strategy-current/strategy-summary.json',E+'shop-v2-strategy-core-current/strategy-summary.json',E+'shop-v2-pilot-analysis.json'])
    put(55,'No owner/friend play session or comprehension/feel/rematch verdict has been recorded.',[],pending='Requires actual human playtesting and recorded verdict.')
    put(56,'The exported software GUI traverses title/lineup/cart/fight, an actual earned reward and next cart, and real completed-match result/rematch callbacks. Competitive replay fixtures reconstruct actual inputs. These staged software sessions are not a continuous human playtest.',[UI+'controller-menu-flow.json',UI+'shop-v2-presentation.json',UI+'ui-completed-match.replay.json',UI+'ui-09-rematch-fresh-wallet.png'])
    put(57,'Current production Core65/core plus75/full, App15+15, objects11, complete socket matches, native GUI299 and24 frozen-export timing checks pass with explicit source/content/binary identities. Historical source-identical evidence is used only where current tested-to-shipped bytes are proved; no old visual result is relabeled.',[CORE,FULL,APP+'self-tests.json',APPF+'self-tests.json',E+'shop-v2-objects-candidate2/buyable-object-tests.json',NET,NETF,UI+'controller-menu-flow.json',FEEL,REVIEW])
    put(58,'Fresh exports and actual scoped Windows/Linux launches require final package extraction/hash and execution reports. WSL/WSLg is the same physical PC.',[],pending='Await current candidate package verification completion.')
    put(59,'Software joypad injection and mapping tests are available; two actual controllers and keyboard+controller were not physically exercised.',[],pending='Requires physical input devices and actual local input session.')
    put(60,'Independent UDP processes run on one PC. No actual two-physical-PC match was performed.',[],pending='Requires two physical computers and an actual private match.')
    put(61,'Current rules, controls, limits, identities and open external gates are documented. Final documentation audit and package inclusion are still being completed.',['reports/FEEL_CALIBRATION.md','reports/SHOP_ONLY_APP_REVIEW.md','docs/18_MIGRATION_FROM_PRIOR_PACK.md'],pending='Await final document audit and release package documentation binding.')

    ui_ready=False
    if (ROOT/(UI+'process-result.json')).exists():
        process=read(UI+'process-result.json'); flow=read(UI+'controller-menu-flow.json')
        ui_ready=process.get('passed') is True and process['exit_code']==0 and not process['errors'] and flow['success'] and all(x['passed'] for x in flow['checks'])
        if ui_ready:
            for path, value in process['runtime_sha256'].items(): assert native[path]==value==sha(path), path
    # Promote only explicit owner-reviewed final records, never mere file existence.
    extra_path=ROOT/'reports/evidence/shop-v2-acceptance-final-supplements.json'
    extra=read(extra_path.relative_to(ROOT)) if extra_path.exists() else {}
    if extra: assert extra['candidate_id']==candidate['build_ref']
    for reqid, supplement in extra.get('records',{}).items():
        assert reqid in ('SO-003','SO-058','SO-061')
        assert supplement['status']=='PASS' and supplement['reviewed'] is True
        specs[reqid].update(paths=supplement['paths'],pending='',note=supplement['note'])
    rows=[]
    requirements=read(REQ)['requirements']
    for req in requirements:
        spec=specs[req['id']]
        paths=list(dict.fromkeys(spec['paths']))
        missing=[p for p in paths if not (ROOT/p).is_file()]
        pending=spec['pending']
        if any(p.startswith(UI) for p in paths) and not ui_ready: pending='Await clean current-candidate native GUI process and runtime binding.'
        if missing: pending='Missing actual artifacts: '+', '.join(missing)
        passed=not pending
        paths=[p for p in paths if (ROOT/p).is_file()]
        if passed: paths += [IDENTITY]
        row={'id':req['id'],'title':req['title'],'acceptance':req['acceptance'],'work_package':req['work_package'],'status':'PASS' if passed else 'NOT_RUN','evidence_scope':req['layer'],**{k:projection[k] for k in ('source_sha256','content_sha256','build_id')},'command_or_session':'Reviewed actual executions and commands in the cited reports; binding: python tools/build_shop_v2_acceptance.py','observed_utc':now,'exit_code':0 if passed else None,'observation':spec['note'],'scenario_ids':spec['scenarios'],'remaining':pending,'artifacts':[artifact(p) for p in dict.fromkeys(paths)]}
        rows.append(row)
    assert len(rows)==61 and len({r['id'] for r in rows})==61
    summary={layer:dict(Counter(r['status'] for r in rows if r['evidence_scope']==layer)) for layer in ('software','device','human')}
    write('reports/SHOP_ONLY_V2_ACCEPTANCE.json',{'schema_version':1,'candidate_id':projection['candidate_id'],'utc':now,'scope':'61 original v2 requirements. PASS is scoped software evidence, never human feel, physical hardware, or expert balance approval. Original requirement text is preserved.','requirements':artifact(REQ),'candidate':projection,'binding':artifact(IDENTITY),'summary':summary,'records':rows})
    print(json.dumps(summary)); print('NOT_RUN:', ', '.join(r['id'] for r in rows if r['status']!='PASS'))

if __name__=='__main__': main()
