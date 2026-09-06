"""Reproducible shop-v2 migration over the preserved 100/115-action recipe libraries.

The v2 package is a resource migration, not a reversion to its older illustrative
26-product catalog. Existing implemented move IDs, collision rules and trial split survive.
"""
from pathlib import Path
import argparse,hashlib,json,shutil
import compile_buyables as legacy
ROOT=Path(__file__).resolve().parents[1]
def read(p):return json.loads(p.read_text(encoding='utf-8'))
def write(p,v):p.write_text(json.dumps(v,indent=2,ensure_ascii=False)+'\n',encoding='utf-8')
def migrate(name,out):
 legacy.compile_set(name,out)
 items=read(out/'items.json')['items']
 for i in items:i.update(access_policy='round_license',implementation_status='implemented',conflicts_with=[])
 for path in sorted((out/'fighters').glob('*.json')):
  f=read(path);fid=f['id'];f['display_name']={'rook':'Vincent','vale':'Thomas'}[fid]
  for m in f['moves']:
   oldcost=m['credit_cost'];m.update(credit_cost=0,debit_on_start=False,access_policy='base' if m['availability']=='base' else 'round_license')
   if m.get('install'):
    m['design_role']='Full-trial replacement for Rush Cascade. A purchased permit supplies one committed startup this round; after 12 vulnerable activation ticks, enable a 240-unfrozen-tick state. Only listed HIT-only normal-to-normal edges become legal; no speed, damage or defense buff. Block, parry and whiff never grant extra edges. Throws, EX and supers are excluded from the extra chain graph; licensed EX retains ordinary legal cancels with no activation fee. State ends on knockdown, capture, dizzy, round end or expiry. No refresh, restored use or duration earned from hits.'
   if m.get('object_rules',{}).get('kind')=='reflect':
    m['design_role']=m['design_role'].replace('credit origin','root attack origin').replace('Paid EX and super-class projectiles','EX and super-class projectiles')
   if m.get('object_rules',{}).get('kind')=='prism':
    m['design_role']=m['design_role'].replace('Proposed replacement for Tidal Step.','Full-trial replacement for Tidal Step.').replace(' See field arbitration contract before implementation.',' Field contacts follow simultaneous combat arbitration.')
   if m['kind'] not in ('ex_special','super'):continue
   super_=m['kind']=='super';product=fid+('_permit_' if super_ else '_license_')+m['id'];price=oldcost if super_ else 900 if m['id']=='rise_ex' else 600
   m.update(availability=product,access_policy='prepaid_super' if super_ else 'round_license',negative_edge=False)
   items.append(dict(id=product,name=m['name'],slot='super' if super_ else 'ex',price=price,eligible_fighters=[fid],move_id=m['id'],replaces=None,duration='one_round',commit_frame=0,refund_after_lock=False,alters_system_defense=False,activation_credit_cost=0,use_limit=1 if super_ else None,debit='preparation_commit',catalog_id=m.get('catalog_id',''),legacy_item_id=None,access_policy=m['access_policy'],implementation_status='implemented',conflicts_with=[],description='One prepaid committed startup this round.' if super_ else 'Repeatable EX access for this round after legal recovery.',tradeoff='Whiff, block, parry or interruption still uses the permit.' if super_ else 'Ordinary charge, recovery, cancel and projectile limits still apply.'))
  for a in f['super_arts']:a.update(credit_cost=0,selected_at='preparation_commit')
  f['resource_notes']='Bank changes only at atomic preparation and confirmed settlement. EX licenses repeat after legal recovery; a purchased art has one prepaid use. Skill credits are nonspendable until the next shop.'
  write(path,f)
 write(out/'items.json',dict(version=1,items=items))
 econ=read(out/'economy.json');econ.update(resource_model='shop_only_bank_round_capabilities',loss_payouts=[1200,1200,1500],slot_prices=dict(signature=900,technique=600,gambit=300,ex=600,super=900),slot_limits=dict(signature=1,technique=1,gambit=1,ex=2,super=1),loadout_cap=2400,ex_activation_cost=0,super_activation_costs=[0],activation_debit='none',reserve_policy='disabled_nonzero_rejected',combat_bank_writes=False,reward_policy=dict(counter_hit=50,anti_air=75,perfect_parry=100,category_limit=2,total_limit=300,perfect_window_eligible_ticks=2));write(out/'economy.json',econ)
 rules=read(out/'rules.json');rules['ruleset_id']='pp-shop-only-v2-'+('full' if name.endswith('full') else 'core');rules['super_art_locked_for_match']=False;rules['carry_between_rounds']=['credits','recovery_tier','score'];rules['reset_between_rounds']=[x for x in rules['reset_between_rounds'] if x not in ('reserve_floor','spend_receipts')]+['selected_super_art','owned_ex_licenses','super_uses_remaining','super_use_receipts','skill_ledger','attack_roots','parry_precision_clock'];write(out/'rules.json',rules)
 inputs=read(out/'inputs.json');inputs.update(paid_input_rejection='recognize_complete_capability_vocabulary_then_gate_no_fallback_no_delayed_execution',capability_negative_edge=False);write(out/'inputs.json',inputs)
 network=read(out/'network.json');network.update(protocol_version=4,paid_moves_inferred_from_inputs_only=True,capabilities_and_skill_receipts_in_rollback_snapshot=True,combat_credit_packets=False);write(out/'network.json',network)
 contract=read(out/'resource_contract.json');contract.update(wallet_fields=['credits','recovery_tier'],income_events=['confirmed_round_win','confirmed_round_loss','confirmed_round_draw','confirmed_skill_settlement'],same_account_spent_by=['atomic_preparation_capabilities'],no_purchased_activation_inventory=False,combat_bank_writes=False,capabilities=['round_licenses','one_prepaid_super_use'],pending_nonspendable_rewards=['counter_hit','anti_air','perfect_parry']);contract['no_income_events']=['ordinary_hit','ordinary_block','ordinary_parry','whiff','damage_taken','elapsed_time','taunt','combo','knockdown'];contract['forbidden_resource_fields']=[x for x in contract['forbidden_resource_fields'] if x not in ('super_stock','finisher_tickets')];contract['not_spendable_resources']+=['pending_skill_credits','owned_capabilities'];write(out/'resource_contract.json',contract)
 h=hashlib.sha256()
 for p in sorted(out.rglob('*.json'),key=lambda x:x.relative_to(out).as_posix()):
  if 'rulesets' in p.relative_to(out).parts:continue
  h.update(p.relative_to(out).as_posix().encode());h.update(p.read_bytes())
 return dict(ruleset=rules['ruleset_id'],products=len(items),moves=sum(len(read(p)['moves']) for p in (out/'fighters').glob('*.json')),content_hash=h.hexdigest())
def main():
 p=argparse.ArgumentParser();p.add_argument('--output',type=Path,default=ROOT/'.tools/shop-v2-compiled');p.add_argument('--publish',action='store_true');a=p.parse_args();results=[]
 for name,target in [('buyables_core',ROOT/'data'),('buyables_full',ROOT/'data/rulesets/buyables_full')]:
  output=a.output/name;results.append(migrate(name,output))
  if a.publish:
   for source in output.rglob('*.json'):
    destination=target/source.relative_to(output);destination.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(source,destination)
 write(a.output/'compilation.json',dict(format='shop-only-v2-compiler',scope='Migration seeds; economic playtest approval remains separate.',results=results));print(json.dumps(results,indent=2))
if __name__=='__main__':main()
