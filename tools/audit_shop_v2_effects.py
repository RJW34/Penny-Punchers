"""Bind all100/115 shop-v2 action nodes to actual current effect evidence.

This inventory combines distinct mechanisms; it does not call every action a strike,
nor treat a successful startup/demonstration alone as proof of the action's effect.
"""
from pathlib import Path
import hashlib,json,gzip
from audit_shop_v2_content import content_hash
ROOT=Path(__file__).resolve().parents[1]
def read(p):return json.loads((ROOT/p).read_text(encoding='utf-8'))
def sha(p):return hashlib.sha256((ROOT/p).read_bytes()).hexdigest()
ROUTES={
'buy_r_s2_slipstream':('buy_route_slipstream_fresh_edges','Exact42-unit displacement; fresh branch edges accepted and early/late edges denied.'),
'buy_r_s2_straight':('buy_route_slipstream_fresh_edges','Independent straight strike hit observed after fresh punch branch.'),
'buy_r_s2_upper':('buy_route_slipstream_fresh_edges','Independent upper strike hit observed after fresh kick branch.'),
'buy_r_s3_chain1':('buy_route_rivet_contact_chain','First hit/block permits next input; parry/whiff denies it.'),
'buy_r_s3_chain2':('buy_route_rivet_contact_chain','Second independent contact enables final fresh branch.'),
'buy_r_s3_chain3':('buy_route_rivet_contact_chain','Final hit and block are observed; finite sequence consumes no activation credits.'),
'buy_r_g1_false_start':('buy_route_mimic_exact_commitment','12-tick commitment,3900 source translation, no hit/projectile/spend.'),
'buy_v_g1_false_pulse':('buy_route_mimic_exact_commitment','12-tick commitment,zero translation, no hit/projectile/spend.'),
'buy_r_a3_overtime':('buy_route_overtime_wallet_timer','One prepaid permit use at committed startup,240-tick install, all8 HIT-only graph edges; separate freeze/interruption suite.')}
ACTORS={
'buy_r_s4_counter':('buy_actor_counter_classes','Front grounded mid catches; low/projectile/super/rear cases bypass.'),
'buy_r_s4_riposte':('buy_actor_counter_classes','Independent riposte after catch, with actual damage events additionally recorded in matched pilot.'),
'buy_r_t4_tempered':('buy_actor_hold_armor','36-tick maximum held startup; armor takes full damage, preserves action once, lethal hit ends round.'),
'buy_r_g3_vault_hop':('buy_actor_vault_and_slipgate','Measured40-unit apex,20-unit horizontal travel,8 landing ticks and rejected air attacks.'),
'buy_v_t3_descending_heel':('buy_actor_air_input_and_landing','Real jump height gating, no illegal fallback,16 landing recovery; actual damage additionally recorded in pilot.'),
'buy_v_t4_skycatch':('buy_actor_air_input_and_landing','Actual eligible airborne capture deals105 and retains18 landing ticks.'),
'buy_v_g4_slipgate':('buy_actor_vault_and_slipgate','Cached80-unit retreat and zero-distance wall use with unchanged commitment.')}
OBJECTS={
'buy_r_g4_wire_cut':('interception_and_reflection_are_finite','Actual normal-shot interception; paid tiers excluded by separate fixture.'),
'buy_v_s3_arc_pulse':('arc_projectile_moves_vertically_and_restores','Actual rising diagonal projectile position, lifetime and snapshot reconstruction.'),
'buy_v_s4_anchor':('anchor_arming_windup_block_consumption','Actual arming,trigger windup,block consumption and owner-linked interruption arbitration.'),
'buy_v_g3_shear_palm':('interception_and_reflection_are_finite','Actual one-depth ownership/direction transfer; original source and finite lifetime retained.'),
'buy_v_a3_prism':('prism_owned_zero_bank_occupancy_snapshot_expiry','Owned zero-bank permit,field occupancy/lifetime/snapshot; parry/reflection/destruction and exhausted recast fixtures.')}
def main():
 out=[];object_path='reports/evidence/shop-v2-objects-candidate2/buyable-object-tests.json';objects=read(object_path);object_pass={x['name'] for x in objects['results'] if x['passed']}
 for name,directory in [('core','data'),('full','data/rulesets/buyables_full')]:
  reportdir='reports/evidence/shop-v2-core-core-final' if name=='core' else 'reports/evidence/shop-v2-core-full-current';reportpath=reportdir+'/core-conformance.json';report=read(reportpath);passed={x['id'] for x in report['results'] if x['pass']};direct={(x['fighter'],x['move']):x for x in report['actionCoverage']};nodes=[]
  assert report['failed']==0 and report['content_hash']==content_hash(ROOT/directory)
  for fp in sorted((ROOT/directory/'fighters').glob('*.json')):
   fighter=json.loads(fp.read_text(encoding='utf-8'))
   for move in fighter['moves']:
    key=(fighter['id'],move['id']);sources=[];description='';kind=''
    if key in direct:
     d=direct[key];assert d['effectObserved'] or d['displacement']!=0
     sources=[reportpath,reportdir+'/action-replays.training.json'];kind='reconstructed training action effect';description=f"Observed damage={d['damage']}, displacement={d['displacement']}; exact startup/input/event/final-state reconstruction."
    elif move['id'] in ROUTES or move['id'] in ACTORS:
     case,description=(ROUTES|ACTORS)[move['id']];assert case in passed,(name,move['id'],case)
     sources=[reportpath,'src/StrikeLedger.CoreTests/'+('BuyableRouteTests.cs' if move['id'] in ROUTES else 'BuyableActorTests.cs')];kind='passed mechanism-specific assertions with snapshot reconstruction'
     if move['id'] in ROUTES:sources.append(reportdir+'/audit-legal-traces.json')
     if move['id'] in ('buy_r_s4_riposte','buy_v_t3_descending_heel'):
      item='buy_r_s4_counter' if 'riposte' in move['id'] else move['id'];trace=next((ROOT/'reports/evidence/shop-v2-product-pilot-current/traces').glob(f'rental-{item}-pressure-s1-seat0-lease.json.gz'))
      with gzip.open(trace,'rt',encoding='utf-8') as f:record=json.load(f)
      assert record['ContentHash']==report['content_hash'];hits=[e for c in record['Commands'] for e in c.get('Events') or [] if e['Kind']==3 and e['MoveId']==move['id']];assert hits
      sources.append(trace.relative_to(ROOT).as_posix());description+=f' This referenced legal pilot trace records{len(hits)} actual hit events.'
    elif move['id'] in OBJECTS:
     case,description=OBJECTS[move['id']];assert case in object_pass;assert objects['contentHash']==report['content_hash'];sources=[object_path,'reports/evidence/shop-v2-objects-candidate2/buyable-object-traces.jsonl','src/StrikeLedger.NetworkLab/BuyableObjectTests.cs'];kind='passed object arbitration/lifecycle assertions and per-tick event trace'
    else:raise AssertionError(f'Uncovered action node: {name}:{key}')
    nodes.append(dict(fighter=fighter['id'],move=move['id'],catalog_id=move.get('catalog_id'),derived_from=move.get('derived_from'),effect_verification=kind,what_was_verified=description,evidence=sources))
  out.append(dict(ruleset=name,content_hash=report['content_hash'],core_assembly_id=report['core_assembly_id'],action_nodes=len(nodes),covered_nodes=len(nodes),direct_reconstructed_effects=len(direct),mechanism_specific_effects=len(nodes)-len(direct),startup_only_used_as_effect_proof=False,nodes=nodes,not_applicable_historical_cases=report['notApplicable']))
 paths=sorted({p for trial in out for node in trial['nodes'] for p in node['evidence']});result=dict(format='penny-shop-only-v2-action-effect-coverage',trials=out,artifacts=[dict(path=p,sha256=sha(p)) for p in paths],limits=['Coverage proves the specified mechanical effects in bounded fixtures; it does not prove every defense interaction, optimal purchase value, native sprite alignment or human competitive fairness.','A neutral-entry demonstration is not used as sole effect proof. Derived nodes are verified through their owning route or catch.','Core and full trials are different current content identities. Historical98-action/v1 evidence remains separate. Standalone startup demonstration counts are not used for effect coverage.'])
 destination=ROOT/'reports/evidence/shop-v2-action-effects.json';destination.write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8');print(json.dumps([dict(ruleset=x['ruleset'],covered=x['covered_nodes'],total=x['action_nodes']) for x in out]))
if __name__=='__main__':main()
