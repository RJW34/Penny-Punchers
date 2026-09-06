"""Compile original, versioned buyable recipes over an immutable numeric baseline.

No sprite extraction or source-code timing inference is used. New collision shapes below
are explicit gameplay-authored seeds, requiring native silhouette review and playtests.
"""
from __future__ import annotations
import argparse, copy, hashlib, json, shutil
from pathlib import Path

ROOT=Path(__file__).resolve().parents[1]
BASE=ROOT/'design/buyables-v1/runtime-baseline'
CATALOG=ROOT/'audit/requests/penny-punchers-buyables-design-v1/data/buyables_catalog.json'
IDS={
'R-S1':'buy_r_s1_clinch','R-S2':'buy_r_s2_slipstream','R-S3':'buy_r_s3_chain1','R-S4':'buy_r_s4_counter',
'R-T1':'buy_r_t1_high_hook','R-T2':'buy_r_t2_low_turn','R-T3':'buy_r_t3_rivet_lift','R-T4':'buy_r_t4_tempered',
'R-G1':'buy_r_g1_false_start','R-G2':'buy_r_g2_check_step','R-G3':'buy_r_g3_vault_hop','R-G4':'buy_r_g4_wire_cut',
'V-S1':'buy_v_s1_return_pulse','V-S2':'buy_v_s2_low_palm','V-S3':'buy_v_s3_arc_pulse','V-S4':'buy_v_s4_anchor',
'V-T1':'buy_v_t1_heel_arc','V-T2':'buy_v_t2_needle_check','V-T3':'buy_v_t3_descending_heel','V-T4':'buy_v_t4_skycatch',
'V-G1':'buy_v_g1_false_pulse','V-G2':'buy_v_g2_recoil_step','V-G3':'buy_v_g3_shear_palm','V-G4':'buy_v_g4_slipgate',
'R-A3':'buy_r_a3_overtime','V-A3':'buy_v_a3_prism'}
CARDS={
'R-S1':('Slow untechable grab for grounded defense.','Jump or strike during its 12-tick startup; no armor or travel.'),
'R-S2':('Duck forward, then choose a fresh punch or kick branch.','Lows and throws beat entry; missed or late input leaves an exposed stop.'),
'R-S3':('Three fresh-punch stages after hit or block.','Whiff or parry ends the route; the final committed strike is punishable.'),
'R-S4':('Catch one front grounded mid or overhead, then riposte.','Low, air, projectile, throw and super attacks bypass it; the riposte can miss.'),
'R-T1':('Replaces forward HP with an overhead hook.','Slow standing commitment; no cancel or kara throw.'),
'R-T2':('Replaces forward HP with a low soft knockdown.','Guard low or punish recovery; no travel or outgoing cancel.'),
'R-T3':('Launcher permits a fresh jump cancel only after hit.','Block, parry and whiff deny the jump; combo scaling and juggle limits remain.'),
'R-T4':('Hold HP for a delayed strike with one early armor contact.','Armor takes full damage; lows, throws, projectiles, supers and a second hit interrupt.'),
'R-G1':('Shares six ticks of heavy-knee anticipation, then aborts.','No attack or defensive privilege; the full 12-tick feint is committed.'),
'R-G2':('Retreats 30 units in six ticks, then recovers for four.','Fully vulnerable throughout; walls limit distance without reducing recovery.'),
'R-G3':('A short forward hop with fixed flight and landing recovery.','No air attack; throws catch startup and anti-air geometry catches flight.'),
'R-G4':('A timed palm destroys one ordinary normal-tier shot.','Cannot hit fighters, reflect shots or erase fields; EX and super shots bypass it.'),
'V-S1':('A slow ordinary pulse reverses once if it misses.','One contact consumes it; parry removes it and the shared shot slot stays occupied.'),
'V-S2':('A low palm converts ground contact to soft knockdown.','Low guard and recovery punishes answer it; no outgoing EX or super cancel.'),
'V-S3':('A rising diagonal pulse checks forward jumps.','Often misses grounded targets; shares the ordinary shot slot and is parryable.'),
'V-S4':('Places an arming marker with one proximity-triggered pulse.','Strike the marker or hit its owner; visible arming and windup delay the threat.'),
'V-T1':('Replaces forward HP with an overhead heel.','Stand guard or interrupt startup; no cancel, travel or kara throw.'),
'V-T2':('A long thin mid checks grounded approaches.','No low profile or knockdown; the extended recovering foot is punishable.'),
'V-T3':('A descending mid from a forward jump above 50 units.','One air sequence only; requires 16 ticks of landing recovery even when it misses.'),
'V-T4':('An untechable grab against a nearby airborne opponent.','Cannot catch grounded or already-juggled targets; a miss carries landing recovery.'),
'V-G1':('Shares six ticks of heavy-pulse anticipation, then aborts.','No projectile, movement or charge grant; the full 12-tick feint is committed.'),
'V-G2':('Retreats 28 units in six ticks, then recovers for four.','Fully vulnerable throughout; walls limit distance without reducing recovery.'),
'V-G3':('Reflects one ordinary normal-tier shot during a short window.','EX, supers and fields bypass it; a second reflection dissipates the shot.'),
'V-G4':('Withdraws toward a cached point 80 units behind.','Only normal shots miss during travel; strikes, throws, walls and recovery still matter.')}

def box(x,y,w,h):return dict(x=x,y=y,width=w,height=h)
STAND=box(0,47500,30000,95000)
LOW=box(0,18000,34000,36000)
def phase(start,end,shapes,intent,replace=False):
 return dict(start=start,end=end,replace_body=replace,boxes=shapes,intent=intent)
def strike(m,damage,stun,level='mid',shape=None,hitstun=23,blockstun=14,knockdown='none',launch=0,rank=3):
 m['hitboxes']=[dict(id='h0',start=m['startup'],end=m['startup']+m['active'],**(shape or box(26000,48000,42000,24000)),level=level,damage=damage,stun=stun,rank=rank,hitstun=hitstun,blockstun=blockstun,push_x=10000,launch_y=launch,knockdown=knockdown,hit_group=0,juggle_cost=2,parryable=True)]
def authored(m,profile='arm'):
 """Each named body/limb profile is independent of attack extents; intentional disjoints remain explicit."""
 s,a,total=m['startup'],m['startup']+m['active'],m['startup']+m['active']+m['recovery']
 profiles={
 'arm':(box(16000,47000,22000,22000),box(28000,47000,28000,18000),box(26000,45000,28000,20000)),
 'overhead':(box(10000,71000,26000,22000),box(34000,64000,32000,22000),box(29000,56000,30000,24000)),
 'low':(box(8000,22000,26000,22000),box(32000,18000,32000,20000),box(30000,20000,32000,22000)),
 'needle':(box(12000,33000,30000,20000),box(46000,38000,38000,16000),box(44000,38000,38000,18000)),
 'upper':(box(12000,60000,24000,28000),box(24000,87000,26000,50000),box(20000,69000,28000,44000)),
 'reach':(box(10000,48000,24000,28000),box(26000,50000,24000,40000),box(22000,46000,26000,34000)),
 'body':(STAND,STAND,STAND)}
 first,active,last=profiles[profile];windows=[]
 if s:windows.append(phase(0,s,[first],'Authored anticipation silhouette; no sprite-alignment claim.',profile=='body'))
 if a>s:windows.append(phase(s,a,[active],'Authored committed body/limb; geometry is independent of effect art.',profile=='body'))
 if total>a:windows.append(phase(a,total,[last],'Authored recovering limb/body remains punishable.',profile=='body'))
 m['hurtbox_windows']=windows
def entry(e):
 t=e['tuning_seed'];k=e['id'];command=e['command'].split(';')[0].strip();command=command.replace('air:','air:')
 m=dict(id=IDS[k],name=e['name'],kind='special' if e['slot']=='signature' else 'unique' if e['slot']=='technique' else 'gambit',command=command,
        startup=t['startup_ticks'],active=t['active_ticks'],recovery=t['recovery_ticks'],hitboxes=[],cancel_rules=[],negative_edge=False,
        availability=IDS[k],pose=IDS[k],invulnerability=[],movement=[],projectile=None,credit_cost=0,debit_on_start=False,kara_throw_eligible=False,catalog_id=k,events=[],
        design_role=e['mechanic_contract'])
 authored(m,'body');return m
def branch(m,id,name,timing,damage,stun,profile='arm',knockdown='none',launch=0):
 n=copy.deepcopy(m);n.update(id=id,name=name,command='derived:P',startup=timing[0],active=timing[1],recovery=timing[2],movement=[],branches=[],derived_from=m['id']);n.pop('actor_rules',None)
 strike(n,damage,stun,knockdown=knockdown,launch=launch,shape=box(26000,87000,30000,55000) if profile=='upper' else None);authored(n,profile);return n

def recipes(catalog,fighter):
 moves=[]
 for e in catalog:
  if e['kind']!='round_lease' or e['fighter']!=fighter:continue
  k=e['id'];m=entry(e);t=e['tuning_seed'];extra=[]
  if k=='R-S1':m['throw']=dict(range=27000,damage=135,stun=90,techable=False,swap_sides=False,knockdown='hard');authored(m,'reach')
  elif k=='R-S2':
   m['movement']=[dict(start=4,end=12,vx=5250,vy=0)]
   m['hurtbox_windows']=[phase(0,4,[STAND],'Standing commitment before the duck.',True),phase(4,12,[LOW],'Low entry avoids high geometry only; lows and throws remain live.',True),phase(12,20,[STAND],'Standing recovery is fully exposed.',True)]
   m['branches']=[dict(button=b,target=target,start=8,end=13,contact='none',relative_to_contact=False) for b,target in [('P','buy_r_s2_straight'),('K','buy_r_s2_upper')]]
   extra=[branch(m,'buy_r_s2_straight','Slipstream Straight',(5,3,19),65,65),branch(m,'buy_r_s2_upper','Slipstream Upper',(7,4,24),80,85,'upper','soft',8000)]
   extra[1]['command']='derived:K'
  elif k=='R-S3':
   strike(m,45,55,hitstun=21,blockstun=12);authored(m)
   second=branch(m,'buy_r_s3_chain2','Rivet Chain II',(8,3,20),55,60);third=branch(m,'buy_r_s3_chain3','Rivet Chain III',(12,3,26),75,85,knockdown='soft',launch=6500)
   m['branches']=[dict(button='P',target=second['id'],start=0,end=8,contact='contact',relative_to_contact=True)]
   second['branches']=[dict(button='P',target=third['id'],start=0,end=8,contact='contact',relative_to_contact=True)];extra=[second,third]
  elif k=='R-S4':
   m['actor_rules']=dict(type='counter',counter_start=5,counter_end=11,riposte='buy_r_s4_riposte');authored(m,'reach');extra=[branch(m,'buy_r_s4_riposte','Cross Counter Riposte',(6,3,22),95,100)]
  elif k in ('R-T1','V-T1'):
   strike(m,80,100,'overhead',box(32000,63000,56000,26000),hitstun=23,blockstun=14);authored(m,'overhead')
  elif k=='R-T2':strike(m,75,95,'low',box(30000,17000,50000,20000),knockdown='soft',launch=5000);authored(m,'low')
  elif k=='R-T3':
   strike(m,60,70,shape=box(22000,58000,42000,50000),hitstun=30,blockstun=14,launch=10000);m['jump_cancel']=dict(start=0,end=4);authored(m,'upper')
  elif k=='R-T4':
   strike(m,100,110,shape=box(30000,50000,50000,30000),hitstun=25,blockstun=15);authored(m);m['actor_rules']=dict(type='armor_hold',armor_start=6,armor_end=14,max_hold_startup=36,trigger_button='HP')
  elif k in ('R-G1','V-G1'):
   m['mimic']=dict(source_move_id='knee_h' if fighter=='rook' else 'pulse_h',shared_ticks=6,abort_ticks=6)
   if fighter=='rook':m['movement']=[dict(start=3,end=6,vx=1300,vy=0)]
   authored(m,'body')
  elif k in ('R-G2','V-G2'):
   m['movement']=[dict(start=0,end=6,vx=-5000,vy=0)] if fighter=='rook' else [dict(start=0,end=4,vx=-4667,vy=0),dict(start=4,end=6,vx=-4666,vy=0)];authored(m,'body')
  elif k=='R-G3':m['actor_rules']=dict(type='vault',flight_ticks=20,vertical_velocity=7600,gravity=800,horizontal_velocity=1000,landing_recovery=8,apex_height=40000);authored(m,'body')
  elif k in ('R-G4','V-G3'):
   m['object_rules']=dict(kind='intercept' if k=='R-G4' else 'reflect',active_start=m['startup'],active_end=m['startup']+m['active'],interaction_box=box(30000,42000,40000,60000));authored(m,'reach')
  elif k=='V-S1':
   m['projectile']=dict(spawn_tick=22,spawn_x=28000,spawn_y=42000,vx=2200,vy=0,life_ticks=110,turn_after_ticks=45,hits=1,rehit_ticks=8,damage=70,stun=70,hitstun=22,blockstun=14,width=24000,height=24000,level='mid',max_active_per_owner=1,rank=4,juggle_cost=1,parryable=True,tier='normal');authored(m,'reach')
  elif k=='V-S2':strike(m,85,100,'low',box(26000,18000,50000,23000),knockdown='soft',launch=5000);authored(m,'low')
  elif k=='V-S3':
   m['projectile']=dict(spawn_tick=18,spawn_x=28000,spawn_y=42000,vx=2600,vy=3200,life_ticks=60,hits=1,rehit_ticks=8,damage=65,stun=65,hitstun=22,blockstun=14,width=18000,height=20000,level='mid',max_active_per_owner=1,rank=3,juggle_cost=2,parryable=True,tier='normal');authored(m,'upper')
  elif k=='V-S4':
   m['object_rules']=dict(kind='anchor',placement_near=45000,placement_far=45000,life_ticks=150,arming_ticks=18,trigger_radius=24000,trigger_windup=8,marker_width=24000,marker_height=20000,contact=dict(width=48000,height=70000,hits=1,rehit_ticks=18,damage=60,stun=60,hitstun=22,blockstun=15,level='mid',rank=2,juggle_cost=2,parryable=True,tier='normal'));authored(m,'reach')
  elif k=='V-T2':strike(m,55,70,shape=box(58000,40000,28000,12000),hitstun=20,blockstun=12);authored(m,'needle')
  elif k=='V-T3':
   strike(m,65,70,shape=box(18000,15000,36000,30000),hitstun=20,blockstun=14);authored(m,'low');m['actor_rules']=dict(type='dive',minimum_height=50000,vertical_velocity=6000,horizontal_velocity=1800,landing_recovery=16,trigger_button='MK')
  elif k=='V-T4':
   m['throw']=dict(range=24000,damage=105,stun=80,techable=False,swap_sides=False,knockdown='hard');m['actor_rules']=dict(type='air_throw',landing_recovery=18);authored(m,'reach')
  elif k=='V-G4':m['actor_rules']=dict(type='slipgate',travel_start=8,travel_end=15,travel_distance=80000);authored(m,'body')
  else:raise ValueError(k)
  moves.extend([m]+extra)
 return moves

def compile_set(ruleset,out):
 catalog=json.loads(CATALOG.read_text(encoding='utf-8'))['entries'];full=ruleset=='buyables_full'
 entries=[e for e in catalog if full or e['rollout']=='core_trial'];out.mkdir(parents=True,exist_ok=True)
 for p in BASE.rglob('*.json'):
  target=out/p.relative_to(BASE);target.parent.mkdir(parents=True,exist_ok=True);target.write_bytes(p.read_bytes())
 items=[];mapping=[]
 for fpath in sorted((out/'fighters').glob('*.json')):
  f=json.loads(fpath.read_text(encoding='utf-8'));fid=f['id'];f['moves']=[m for m in f['moves'] if m['availability']=='base'];new=recipes(entries,fid);f['moves'].extend(new)
  for e in entries:
   if e['fighter']!=fid:continue
   if e['kind']=='round_lease':
    item_id=IDS[e['id']];description,tradeoff=CARDS[e['id']];items.append(dict(id=item_id,name=e['name'],slot=e['slot'],price=e['pricing']['rental_credits'],eligible_fighters=[fid],move_id=item_id,replaces=e['replaces_move_id'],duration='one_round',commit_frame=0,refund_after_lock=False,alters_system_defense=False,activation_credit_cost=0,use_limit=None,debit='preparation_commit',catalog_id=e['id'],legacy_item_id=e['legacy_item_id'],description=description,tradeoff=tradeoff))
   elif e['kind']=='enhanced_activation':
    m=next(m for m in f['moves'] if m['id']==e['tuning_seed']['baseline_move_id']);m['catalog_id']=e['id'];m['name']=e['name']
   elif e['id'].endswith(('A1','A2')):
    mid=e['tuning_seed']['baseline_move_id'];m=next(m for m in f['moves'] if m['id']==mid);m['catalog_id']=e['id'];m['name']=e['name'];next(a for a in f['super_arts'] if a['move_id']==mid)['name']=e['name']
  if full:
   k='R-A3' if fid=='rook' else 'V-A3';e=next(e for e in entries if e['id']==k);old=next(m for m in f['moves'] if m['id']=='super_3');m=copy.deepcopy(old)
   m.update(id=IDS[k],name=e['name'],pose=IDS[k],command=e['command'].split(';')[0].strip(),startup=e['tuning_seed']['startup_ticks'],active=e['tuning_seed']['active_ticks'],recovery=e['tuning_seed']['recovery_ticks'],hitboxes=[],movement=[],invulnerability=[],cancel_rules=[],projectile=None,super_freeze=0,catalog_id=k,design_role=e['mechanic_contract'])
   if fid=='rook':
    edges=[]
    for a,b in [('s_lp','s_mp'),('s_lk','s_mk'),('c_lp','c_mp'),('c_lk','c_mk'),('s_mp','s_hp'),('s_mk','s_hk'),('c_mp','c_hp'),('c_mk','c_hk')]:
     source=next(n for n in f['moves'] if n['id']==a);edges.append(dict(from_=a,to=b,start=source['startup'],end=source['startup']+source['active']+4))
    m['install']=dict(duration_ticks=240,edges=[{'from':e.pop('from_'),**e} for e in edges])
   else:m['object_rules']=dict(kind='prism',placement_near=45000,placement_far=90000,life_ticks=180,arming_ticks=0,marker_width=18000,marker_height=70000,contact=dict(width=18000,height=70000,hits=2,rehit_ticks=18,damage=45,stun=45,hitstun=20,blockstun=16,level='mid',rank=2,juggle_cost=2,parryable=True,tier='super'))
   authored(m,'body');f['moves'].remove(old);f['moves'].append(m);art=next(a for a in f['super_arts'] if a['move_id']=='super_3');art.update(move_id=m['id'],name=m['name'],role=e['role'])
  for m in f['moves']:
   if m.get('projectile'):m['projectile']['tier']='super' if m['kind']=='super' else 'ex' if m['kind']=='ex_special' else 'normal'
  fpath.write_text(json.dumps(f,indent=2)+'\n',encoding='utf-8')
  for e in entries:
   if e['fighter']==fid:mapping.append(dict(catalog_id=e['id'],runtime_move_id=IDS.get(e['id'],e['tuning_seed'].get('baseline_move_id')),legacy_item_id=e['legacy_item_id'],status='implemented_candidate_requires_current_tests'))
 (out/'items.json').write_text(json.dumps(dict(version=1,items=items),indent=2)+'\n',encoding='utf-8')
 rules=json.loads((out/'rules.json').read_text());rules['ruleset_id']='penny-punchers-'+ruleset.replace('_','-')+'-v1';(out/'rules.json').write_text(json.dumps(rules,indent=2)+'\n',encoding='utf-8')
 h=hashlib.sha256()
 for p in sorted(out.rglob('*.json'),key=lambda p:p.relative_to(out).as_posix()):
  if 'rulesets' in p.relative_to(out).parts:continue
  h.update(p.relative_to(out).as_posix().encode());h.update(p.read_bytes())
 return dict(ruleset=ruleset,content_hash=h.hexdigest(),rental_count=len(items),moves=sum(len(json.loads(p.read_text())['moves']) for p in (out/'fighters').glob('*.json')),mapping=mapping)

def main():
 parser=argparse.ArgumentParser();parser.add_argument('--output',type=Path,default=ROOT/'.tools/buyables-compiled');parser.add_argument('--publish',action='store_true',help='Copy validated recipe outputs to default core and explicit full data directories.');args=parser.parse_args()
 results=[compile_set(name,args.output/name) for name in ['buyables_core','buyables_full']]
 (args.output/'compilation.json').write_text(json.dumps(dict(format='penny-buyables-compiler-v1',baseline=str(BASE.relative_to(ROOT)),catalog_sha256=hashlib.sha256(CATALOG.read_bytes()).hexdigest(),results=results),indent=2)+'\n',encoding='utf-8')
 if args.publish:
  for name,target in [('buyables_core',ROOT/'data'),('buyables_full',ROOT/'data/rulesets/buyables_full')]:
   for source in (args.output/name).rglob('*.json'):
    dest=target/source.relative_to(args.output/name);dest.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(source,dest)
 print(json.dumps([{k:v for k,v in result.items() if k!='mapping'} for result in results],indent=2))
if __name__=='__main__':main()
