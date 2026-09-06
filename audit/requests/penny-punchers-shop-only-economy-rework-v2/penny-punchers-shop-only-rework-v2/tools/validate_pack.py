"""Validate the authoring package. Does not validate or execute the game."""
from pathlib import Path
from collections import Counter
import json, sys, ast
ROOT=Path(__file__).resolve().parents[1]
def read(p):return json.loads((ROOT/p).read_text())
def validate():
    rules=read('data/rules.v2.json');cat=read('data/catalog.v2.json')['entries'];future=read('data/candidate_library.v2.json')['entries']
    req=read('acceptance/requirements.json')['requirements'];wps=read('work_packages/index.json')['work_packages']
    assert rules['players']==2 and rules['arenas']==1 and not rules['team_systems']
    assert not rules['combat_bank_writes'] and rules['combat_debit']==0
    assert rules['reward_deposit']=='confirmed_round_settlement'
    assert rules['super_policy']=='one_prepaid_use_total_per_round'
    assert rules['slot_limits']['ex']==2 and rules['slot_limits']['super']==1
    assert rules['reward_policy']['total_cap_per_player_per_round']==300
    assert rules['loss_payouts']==[1200,1200,1500]
    for collection in (cat,future):
        assert len({e['id'] for e in collection})==len(collection)
        for e in collection:
            assert e['activation_price']==0 and e['shop_price']>0
            assert e['slot'] in rules['slot_limits']
            assert e['fighter'] in ('rook','vale')
            assert e['uses_per_round']==(1 if e['kind']=='super_permit' else None)
            assert e['kind'] in ('super_permit','ex_license','technique_license')
    assert Counter(e['kind'] for e in cat)=={'technique_license':12,'ex_license':8,'super_permit':6}
    assert Counter(e['kind'] for e in future)=={'technique_license':24,'ex_license':8,'super_permit':6}
    # Baseline reference counts are assertions about this pack, not constraints to copy into runtime.
    for f in ('rook','vale'):
        moves={m['id']:m for m in read(f'provenance/baseline/fighters/{f}.json')['moves']}
        for e in (x for x in cat if x['fighter']==f):
            for mid in e['move_ids']:assert mid in moves
            if e['replaces_move_id']:assert e['replaces_move_id'] in moves
    by={p['id']:p for p in wps};assert len(by)==len(wps)
    done=set()
    while len(done)<len(by):
        ready={i for i,p in by.items() if i not in done and set(p['dependencies'])<=done}
        assert ready,'cyclic/unknown dependency';done|=ready
    assert len({r['id'] for r in req})==len(req)
    all_ids={r['id'] for r in req}
    assert {rid for p in wps for rid in p['requirements']}==all_ids
    for r in req:assert r['work_package'] in by and r['layer'] in ('software','device','human')
    for p in wps:assert (ROOT/p['path']).is_file()
    for s in read('acceptance/scenarios.json')['scenarios']:assert s['work_package'] in by
    for p in ROOT.rglob('*.py'):ast.parse(p.read_text(),filename=str(p))
    # Active library is rewritten, not merely covered by a contradictory top-level disclaimer.
    text=json.dumps(future).lower()
    for obsolete in ['debit once on accepted startup','300-credit debit cue','direct_spend','paid_action_receipts','still costs 300']:
        assert obsolete not in text,obsolete
    schema_checks=0
    try:
        import jsonschema
        for instance,schema in [('data/catalog.v2.json','schemas/catalog.schema.json'),('data/rules.v2.json','schemas/rules.schema.json')]:
            sd=read(schema);jsonschema.Draft202012Validator.check_schema(sd);jsonschema.validate(read(instance),sd);schema_checks+=1
    except ImportError:
        raise RuntimeError('Install requirements-tools.txt to run schema validation; do not silently skip it')
    return {'package_validation':'PASS','baseline_products':len(cat),'design_candidates':len(future),'requirements':len(req),'layers':dict(Counter(r['layer'] for r in req)),'work_packages':len(wps),'native_scenarios_planned':len(read('acceptance/scenarios.json')['scenarios']),'schemas_validated':schema_checks,'native_game_executed':False}
if __name__=='__main__':
    try:print(json.dumps(validate(),indent=2))
    except Exception as e:print('FAIL:',e,file=sys.stderr);sys.exit(1)
