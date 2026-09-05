#!/usr/bin/env python3
"""Validate the delivered scaffold's data/contracts, never certify the future game."""
from __future__ import annotations
import argparse,ast,json,re,sys,xml.etree.ElementTree as ET
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]

def ensure(ok: bool,message: str)->None:
    if not ok:raise ValueError(message)

def validate(root:Path=ROOT,strict:bool=False)->dict:
    root=root.resolve();files=[p for p in root.rglob('*') if p.is_file() and not any(x in p.parts for x in ('.git','.venv','__pycache__','bin','obj','GeneratedData','.godot','.tools'))]
    parsed={}
    for p in files:
        ensure(not p.is_symlink() and p.resolve().is_relative_to(root),'Unsafe source path')
        rel=p.relative_to(root).as_posix()
        if p.suffix=='.json':parsed[rel]=json.loads(p.read_text(encoding='utf-8'))
        if p.suffix=='.py':ast.parse(p.read_text(encoding='utf-8'),filename=rel)
        if p.suffix=='.csproj':ET.parse(p)
    rules=parsed['data/rules.json'];e=parsed['data/economy.json'];network=parsed['data/network.json']
    ensure(rules['mode']=='singles_only' and rules['max_players']==2 and rules['arenas']==1,'Only1v1 single arena allowed')
    ensure(rules['target_half_points']==10 and rules['max_rounds']==9,'Unexpected match seed rules')
    ensure(network['max_peers']==network['max_seats']==2,'No four-seat/network team topology')
    ensure(network['economy_in_rollback_snapshot'] and not network['predicted_payouts'],'Rollback money/confirmed payout requirement')
    ensure(e['resource_model']=='one_persistent_scalar_credit_wallet','Single wallet required')
    ensure(e['combat_income']==e['passive_income']==0,'Combat/time resource income forbidden')
    ensure(e['starting_credits']==600 and e['wallet_cap']==3600,'Seed credit bounds changed without updates')
    ensure(e['loss_payouts']==[900,1200,1500] and e['max_recovery_tier']==2,'Recovery seed mismatch')
    ensure(sum(e['slot_prices'].values())==e['loadout_cap'],'Loadout cap/price mismatch')
    forbidden=set(parsed['data/resource_contract.json']['forbidden_resource_fields'])|{'crew','relay','team_id','duel_id','cash','sponsorship','teammates','stocks','blast_zones','ledge_catch'}
    def scan(o,path):
        if isinstance(o,dict):
            for k,v in o.items():
                ensure(k not in forbidden,f'Forbidden inherited resource/team/platform field {path}/{k}')
                scan(v,path+'/'+k)
        elif isinstance(o,list):
            for i,v in enumerate(o):scan(v,path+f'/{i}')
    for name,o in parsed.items():
        if name.startswith('data/') and name!='data/resource_contract.json':scan(o,name)
    items=parsed['data/items.json']['items'];ids=[x['id'] for x in items]
    ensure(len(items)==12 and len(set(ids))==12,'Expected12 unique leases')
    itemmap={x['id']:x for x in items};total=base=0;allmoves={}
    for fid in ('rook','vale'):
        f=parsed[f'data/fighters/{fid}.json'];moves=f['moves'];mids=[m['id'] for m in moves]
        ensure(f['id']==fid and len(moves)==49 and len(set(mids))==49,f'{fid} expected49 unique actions')
        mm={m['id']:m for m in moves};allmoves[fid]=mm;total+=len(moves)
        ensure(mm['command_fhp'].get('kara_throw_ticks')==parsed['data/inputs.json']['kara_throw_ticks'],'Kara window data mismatch')
        bc=sum(m['availability']=='base' for m in moves);ensure(bc==43,f'{fid} expected43 base-availability actions');base+=bc
        ensure(len(f['super_arts'])==3 and f['default_super'] in {a['id'] for a in f['super_arts']},'Bad art selection')
        for a in f['super_arts']:
            ensure(a['move_id'] in mm and a['credit_cost']==mm[a['move_id']]['credit_cost'],'Art price mismatch')
            ensure(a['credit_cost'] in e['super_activation_costs'],'Unknown super cost')
        for m in moves:
            start,active,rec=m['startup'],m['active'],m['recovery'];length=start+active+rec
            ensure(type(start) is int and start>=0 and type(active) is int and active>0 and type(rec) is int and rec>=0,'Bad timeline')
            cost=m['credit_cost'];ensure(type(cost) is int and 0<=cost<=e['wallet_cap'],'Bad action cost')
            ensure(m['debit_on_start']==(cost>0),'Debit flag mismatch')
            if m['kind']=='ex_special':ensure(cost==e['ex_activation_cost'] and not m['negative_edge'],'EX price/negative-edge mismatch')
            elif m['kind']!='super':ensure(cost==0,'Unspecified per-use charges on base/leased technique')
            if cost:ensure(not m['negative_edge'],'No paid negative edge')
            ensure(m['availability']=='base' or m['availability'] in itemmap,'Unknown lease availability')
            for b in m['hitboxes']:
                ensure(start<=b['start']<b['end']<=start+active,'Hitbox outside active interval')
                ensure(b['width']>0 and b['height']>0,'Nonpositive box')
            for group in ('movement','invulnerability','cancel_rules'):
                for x in m[group]:ensure(0<=x['start']<x['end']<=length,f'Bad {group} interval in {fid}:{m["id"]}')
            if m.get('projectile'):
                p=m['projectile'];ensure(0<=p['spawn_tick']<length and p['life_ticks']>0 and p['hits']>0,'Bad projectile lifetime')
            ensure(bool(m['hitboxes'] or m.get('projectile') or m.get('throw') or m['movement']),f'No defined effect for {fid}:{m["id"]}')
        for tc in f['target_combos']:ensure(tc['from'] in mm and tc['to'] in mm,'Bad target combo move reference')
    for x in items:
        ensure(x['slot'] in e['slot_prices'] and x['price']==e['slot_prices'][x['slot']],'Lease price/slot mismatch')
        ensure(x['duration']=='one_round' and x['use_limit'] is None and x['activation_credit_cost']==0,'No relay/activation-ticket lease')
        ensure(x['debit']=='preparation_commit' and not x['alters_system_defense'],'Unexpected lease effect/debit')
        ensure(len(x['eligible_fighters'])==1,'Character-specific lease required')
        fid=x['eligible_fighters'][0];ensure(fid in allmoves and x['move_id'] in allmoves[fid],'Item move missing')
        ensure(allmoves[fid][x['move_id']]['availability']==x['id'],'Item availability mismatch')
        if x['replaces'] is not None:ensure(x['replaces'] in allmoves[fid],'Bad replacement reference')
    req=parsed['acceptance/requirements.json']['requirements'];pkgs=parsed['orchestration/dag.json']['packages']
    ensure(len({r['id'] for r in req})==len(req),'Duplicate requirement ids');pm={p['id']:p for p in pkgs}
    ensure(len(pm)==len(pkgs),'Duplicate package ids');visiting=set();visited=set()
    def visit(pid):
        ensure(pid in pm,'Unknown dependency '+pid);ensure(pid not in visiting,'Dependency cycle')
        if pid in visited:return
        visiting.add(pid)
        for dep in pm[pid]['depends_on']:visit(dep)
        visiting.remove(pid);visited.add(pid)
    for p in pkgs:
        visit(p['id']);path=(root/p['path']).resolve();ensure(path.is_relative_to(root) and path.is_file(),'Missing package file')
        ensure(set(p['requirement_ids'])=={r['id'] for r in req if r['work_package']==p['id']},'DAG/acceptance drift')
    for r in req:
        ensure(r['work_package'] in pm and r['mandatory'] is True and r['required_evidence_kinds'],'Invalid mandatory gate')
        ensure(r['gate_class'] in ('software','target_device','human'),'Invalid gate class')
    for name in ['START_HERE.md','AGENTS.md','EXECUTOR_PROMPT.md','RESUME_PROMPT.md','game/project.godot','reports/STATE.json','docs/18_MIGRATION_FROM_PRIOR_PACK.md']:
        ensure((root/name).is_file(),'Missing entry/source file '+name)
    for s in parsed['acceptance/scenarios.json']['scenarios']:ensure(s['owner'] in pm and s['unimplemented_exit_must_be_nonzero'],'Scenario contract mismatch')
    if strict:
        from jsonschema import Draft202012Validator,FormatChecker
        for name,data in parsed.items():
            if name.startswith('schemas/'):Draft202012Validator.check_schema(data)
        pairs=[('data/economy.json','economy'),('data/items.json','items'),('data/fighters/rook.json','fighter'),('data/fighters/vale.json','fighter'),('acceptance/requirements.json','requirements'),('orchestration/dag.json','dag'),('reports/ACCEPTANCE_RESULTS.json','evidence')]
        for path,sch in pairs:
            errs=list(Draft202012Validator(parsed[f'schemas/{sch}.schema.json'],format_checker=FormatChecker()).iter_errors(parsed[path]))
            ensure(not errs,path+': '+'; '.join(x.message for x in errs[:5]))
        wallet=Draft202012Validator(parsed['schemas/wallet.schema.json'])
        for v in parsed['fixtures/economy_vectors.json']['payouts']:wallet.validate(v['before']);wallet.validate(v['expected'])
    return {'scope':'SCAFFOLD_ONLY','status':'PASS','json_files':len(parsed),'python_files':sum(p.suffix=='.py' for p in files),'csproj_xml_files':sum(p.suffix=='.csproj' for p in files),'requirements':len(req),'work_packages':len(pkgs),'fighters':2,'base_moves':base,'all_moves':total,'items':len(items),'schema_mode':'draft2020-12_and_semantics' if strict else 'semantics','game_implementation':'NOT_VERIFIED'}

def main()->int:
    ap=argparse.ArgumentParser(description=__doc__);ap.add_argument('--strict-schema',action='store_true');ap.add_argument('--root',type=Path,default=ROOT);a=ap.parse_args()
    try:print(json.dumps(validate(a.root,a.strict_schema),indent=2));return 0
    except Exception as e:print(f'SCAFFOLD VALIDATION FAILED: {type(e).__name__}: {e}',file=sys.stderr);return 1
if __name__=='__main__':raise SystemExit(main())
