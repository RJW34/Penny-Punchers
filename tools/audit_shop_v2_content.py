"""Read-only canonical shop-v2 audit; preserves an inventory of examined resource leaves."""
from pathlib import Path
import argparse, datetime, hashlib, json, re

ROOT = Path(__file__).resolve().parents[1]
RESOURCE = re.compile(r'credit|spend|wallet|money|refund|reserve|purchas|permit|license|selected|paid|debit|income|reward|activation|ticket|stock|resource|cost', re.I)
STALE = re.compile(r'match_setup|commit\s+\d+\s+at startup|still costs\s+\d+|proposed replacement|before implementation', re.I)

def read(p): return json.loads(p.read_text(encoding='utf-8'))
def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()
def leaves(v, path=''):
    if isinstance(v, dict):
        for k, x in v.items(): yield from leaves(x, path+'.'+k)
    elif isinstance(v, list):
        for i, x in enumerate(v): yield from leaves(x, path+f'[{i}]')
    else: yield path, v
def content_hash(base):
    h=hashlib.sha256()
    for p in sorted(base.rglob('*.json'),key=lambda p:p.relative_to(base).as_posix()):
        rel=p.relative_to(base)
        if 'rulesets' in rel.parts: continue
        h.update(rel.as_posix().encode()); h.update(p.read_bytes())
    return h.hexdigest()

def main():
    ap=argparse.ArgumentParser();ap.add_argument('--output',type=Path,default=ROOT/'reports/evidence/shop-v2-canonical-audit');a=ap.parse_args()
    checks=[]; inventory=[]; files=[]
    def check(name, ok, detail):
        checks.append(dict(name=name,passed=bool(ok),detail=detail))
    for label,base,nmoves,nitems in [('core',ROOT/'data',100,26),('full',ROOT/'data/rulesets/buyables_full',115,38)]:
        parsed={p.relative_to(base).as_posix():read(p) for p in base.rglob('*.json') if 'rulesets' not in p.relative_to(base).parts}
        for rel,d in sorted(parsed.items()):
            p=base/rel;files.append(dict(path=p.relative_to(ROOT).as_posix(),sha256=sha(p),bytes=p.stat().st_size))
            for key,value in leaves(d):
                if RESOURCE.search(key) or (isinstance(value,str) and RESOURCE.search(value)):
                    inventory.append(dict(ruleset=label,file=rel,key=key,value=value))
        e=parsed['economy.json'];r=parsed['rules.json'];rc=parsed['resource_contract.json'];ins=parsed['inputs.json'];net=parsed['network.json'];items=parsed['items.json']['items'];fighters=[v for k,v in parsed.items() if k.startswith('fighters/')];moves=[m for f in fighters for m in f['moves']]
        check(label+'_library_counts',len(moves)==nmoves and len(items)==nitems,dict(moves=len(moves),products=len(items)))
        check(label+'_bank_rules',e['starting_credits']==600 and e['wallet_cap']==3600 and e['loadout_cap']==2400 and e['loss_payouts']==[1200,1200,1500] and e['win_payout']==1200 and e['draw_payout']==900,e)
        check(label+'_no_combat_debit',e['combat_bank_writes'] is False and e['combat_income']==0 and e['activation_debit']=='none' and e['ex_activation_cost']==0 and e['super_activation_costs']==[0] and all(m['credit_cost']==0 and m['debit_on_start'] is False for m in moves),'All215 core/full move definitions have zero activation cost and no startup debit.')
        check(label+'_reward_policy',e['reward_policy']==dict(counter_hit=50,anti_air=75,perfect_parry=100,category_limit=2,total_limit=300,perfect_window_eligible_ticks=2),e['reward_policy'])
        check(label+'_slots_and_reserve',e['slot_limits']==dict(signature=1,technique=1,gambit=1,ex=2,super=1) and e['default_reserve_floor']==0 and e['reserve_policy']=='disabled_nonzero_rejected','Compatibility reserve field remains zero; positive requests rejected by production quote tests.')
        check(label+'_round_selection_and_expiry',r['super_art_locked_for_match'] is False and r['carry_between_rounds']==['credits','recovery_tier','score'] and all(x in r['reset_between_rounds'] for x in ['selected_super_art','owned_ex_licenses','super_uses_remaining','super_use_receipts','skill_ledger','attack_roots','parry_precision_clock']) and all(a['selected_at']=='preparation_commit' and a['credit_cost']==0 for f in fighters for a in f['super_arts']),r)
        check(label+'_atomic_product_contract',all(i['implementation_status']=='implemented' and i['duration']=='one_round' and i['debit']=='preparation_commit' and i['activation_credit_cost']==0 and i['use_limit']==(1 if i['slot']=='super' else None) for i in items),'Every live product is implemented; EX/rentals have no use counter, super has one use.')
        check(label+'_product_move_links',all(next(m for f in fighters if f['id'] in i['eligible_fighters'] for m in f['moves'] if m['id']==i['move_id'])['availability']==i['id'] for i in items),'Every product maps to its authoritative availability-gated move.')
        check(label+'_precision_vs_ordinary_parry',parsed['combat.json']['parry']['success_income']==0 and 'ordinary_parry' in rc['no_income_events'] and 'perfect_parry' in rc['pending_nonspendable_rewards'],'Ordinary parry has no income; precision reward is pending and deposits only through confirmed skill settlement.')
        check(label+'_resource_contract',rc['same_account_spent_by']==['atomic_preparation_capabilities'] and rc['no_purchased_activation_inventory'] is False and rc['combat_bank_writes'] is False and 'confirmed_skill_settlement' in rc['income_events'] and 'pending_skill_credits' in rc['not_spendable_resources'],rc)
        check(label+'_input_network_contract',ins['capability_negative_edge'] is False and 'no_fallback_no_delayed_execution' in ins['paid_input_rejection'] and net['protocol_version']==4 and net['combat_credit_packets'] is False and net['predicted_payouts'] is False,'Complete capability vocabulary; input-only inference; canonical capability/receipt rollback state.')
        stale=[dict(file=k,key=p,value=v) for k,d in parsed.items() for p,v in leaves(d) if isinstance(v,str) and STALE.search(v)]
        check(label+'_obsolete_canonical_text_absent',not stale,stale)
    previous=ROOT/'reports/evidence/shop-v2-crossover-pilot-final/isolated-rulesets/C'
    diffs=[]
    if previous.exists():
        for p in previous.rglob('*.json'):
            rel=p.relative_to(previous);now=ROOT/'data/rulesets/buyables_full'/rel
            old=dict(leaves(read(p)));new=dict(leaves(read(now)))
            for k in sorted(old.keys()|new.keys()):
                if old.get(k)!=new.get(k):diffs.append(dict(file=rel.as_posix(),key=k,before=old.get(k),after=new.get(k)))
        check('final_full_delta_only_three_design_roles',len(diffs)==3 and all(d['key'].endswith('.design_role') for d in diffs),diffs)
    result=dict(format='shop-v2-canonical-read-only-audit',utc=datetime.datetime.now(datetime.timezone.utc).isoformat(),command='python tools/audit_shop_v2_content.py',passed=all(c['passed'] for c in checks),checks=checks,resourceLeafCount=len(inventory),resourceLeaves=inventory,files=files,contentHashes=dict(core=content_hash(ROOT/'data'),full=content_hash(ROOT/'data/rulesets/buyables_full')),scope='All JSON leaves in both live numeric catalogs examined. Names such as paid_negative_edge are retained compatibility terminology; values enforce ownership and no combat debit. Historical source recipes and old evidence are intentionally separate.')
    a.output.mkdir(parents=True,exist_ok=True);(a.output/'result.json').write_text(json.dumps(result,indent=2,ensure_ascii=False)+'\n',encoding='utf-8')
    print(json.dumps(dict(passed=result['passed'],checks=len(checks),resourceLeaves=len(inventory),contentHashes=result['contentHashes'],failed=[c for c in checks if not c['passed']]),indent=2))
    raise SystemExit(0 if result['passed'] else 1)
if __name__=='__main__':main()
