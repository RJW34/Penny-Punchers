#!/usr/bin/env python3
"""Auditor-authored arithmetic/content checks. NOT the C# game or a balance simulation.

Usage: python economy_analysis.py --data verified_source/data --output calculations
All combat outcomes supplied to this model are hypothetical. This program never
infers a round win probability from a wallet, an item, or authored damage numbers.
"""
from __future__ import annotations
import argparse
from dataclasses import asdict, dataclass
from fractions import Fraction
from functools import lru_cache
from itertools import product
from pathlib import Path
import hashlib
import json

@dataclass(frozen=True)
class Wallet:
    credits: int
    tier: int = 0

@dataclass(frozen=True)
class Rules:
    start: int = 600
    cap: int = 3600
    win: int = 1200
    draw: int = 900
    losses: tuple[int, ...] = (900,1200,1500)

    @classmethod
    def load(cls, data: Path) -> Rules:
        e = json.loads((data/'economy.json').read_text())
        return cls(e['starting_credits'],e['wallet_cap'],e['win_payout'],e['draw_payout'],tuple(e['loss_payouts']))

def settle(wallet: Wallet, spend: int, outcome: str, rules: Rules = Rules()) -> tuple[Wallet,dict]:
    if not (0 <= wallet.credits <= rules.cap and 0 <= wallet.tier < len(rules.losses)):
        raise ValueError('Invalid wallet')
    if isinstance(spend,bool) or not isinstance(spend,int) or not 0 <= spend <= wallet.credits:
        raise ValueError('Spend must be a legal nonnegative integer aggregate')
    if outcome == 'W': nominal, tier = rules.win, max(0,wallet.tier-1)
    elif outcome == 'L': nominal, tier = rules.losses[wallet.tier], min(len(rules.losses)-1,wallet.tier+1)
    elif outcome == 'D': nominal, tier = rules.draw, wallet.tier
    else: raise ValueError('Unknown outcome')
    retained=wallet.credits-spend
    granted=min(nominal,rules.cap-retained)
    return Wallet(retained+granted,tier),dict(opening=wallet.credits,spend=spend,retained=retained,nominal=nominal,granted=granted,clipped=nominal-granted,closing=retained+granted,old_tier=wallet.tier,new_tier=tier)

def sequence(outcomes: str, spend_policy: str, rules: Rules = Rules()) -> list[dict]:
    a,b=Wallet(rules.start),Wallet(rules.start)
    pa=pb=0;rows=[]
    for r,outcome in enumerate(outcomes,1):
        if r>9 or max(pa,pb)>=10: break
        if spend_policy=='save': sa=sb=0
        elif spend_policy=='equal_600': sa=sb=min(600,a.credits,b.credits)
        elif spend_policy=='leader_saves_opponent_spends_all': sa,sb=0,b.credits
        else: raise ValueError('Unknown policy')
        other={'W':'L','L':'W','D':'D'}[outcome]
        a,ra=settle(a,sa,outcome,rules);b,rb=settle(b,sb,other,rules)
        pa += 2 if outcome=='W' else 1 if outcome=='D' else 0
        pb += 2 if outcome=='L' else 1 if outcome=='D' else 0
        rows.append({'round':r,'A_result':outcome,'A':ra,'B':rb,'score_half_points':[pa,pb], 'A_minus_B':a.credits-b.credits,'match_over':max(pa,pb)>=10 or r==9})
    return rows

@lru_cache(None)
def fair_match_win(a: int, b: int, target: int=5) -> Fraction:
    if a>=target:return Fraction(1)
    if b>=target:return Fraction(0)
    return (fair_match_win(a+1,b,target)+fair_match_win(a,b+1,target))/2

@lru_cache(None)
def fixed_p_win(a:int,b:int,p:Fraction,target:int=5) -> Fraction:
    if a>=target:return Fraction(1)
    if b>=target:return Fraction(0)
    return p*fixed_p_win(a+1,b,p,target)+(1-p)*fixed_p_win(a,b+1,p,target)

def feasible_budgets(credits:int, art_cost:int, items:list[dict],fighter:str) -> list[dict]:
    choices=[]
    for slot in ['signature','technique','gambit']:
        choices.append([None]+[i for i in items if i['slot']==slot and fighter in i['eligible_fighters']])
    result=[]
    for plan in product(*choices):
        selected=[i for i in plan if i];cost=sum(i['price'] for i in selected)
        if cost>credits or cost>1800:continue
        left=credits-cost
        result.append({'items':[i['id'] for i in selected],'rental_cost':cost,'remaining':left,'max_ex_if_no_other_spend':left//300,'can_selected_super':left>=art_cost,'max_ex_after_one_super':(left-art_cost)//300 if left>=art_cost else None})
    return result

def source_identity(data:Path)->list[dict]:
    manifest=json.loads((data.parent/'SOURCE_IDENTITY.json').read_text())
    checks=[]
    for entry in manifest['files']:
        raw=(data.parent/entry['path']).read_bytes()
        actual=hashlib.sha1(b'blob '+str(len(raw)).encode()+b'\0'+raw).hexdigest()
        if actual!=entry['git_blob_sha1']:raise ValueError('Changed source: '+entry['path'])
        checks.append({'path':entry['path'],'git_blob_sha1':actual,'verified':True})
    return checks

def move_summary(m:dict)->dict:
    return {key:m.get(key) for key in ['id','name','kind','command','startup','active','recovery','credit_cost','hitboxes','throw','projectile','movement','cancel_rules','invulnerability','kara_throw_eligible','kara_throw_ticks']}

def nominal_damage(m:dict,combat:dict)->int:
    """All-hit payload sum, no counterhit, starting combo index zero; NOT tested damage."""
    hits=[h['damage'] for h in sorted(m['hitboxes'],key=lambda h:(h['start'],h['hit_group']))]
    if m.get('projectile'):hits += [m['projectile']['damage']]*m['projectile']['hits']
    if m.get('throw'):hits += [m['throw']['damage']]
    c=combat['damage']
    return sum(d*max(c['combo_floor_percent'],100-c['combo_step_percent']*i)//100 for i,d in enumerate(hits))

def equal_spend_reachability(rules:Rules)->dict:
    """Enumerate ledger states, not fights. First outcome W; all later W/L/D.
    Equal aggregate spend in both seats, multiples of 300 up to poorer wallet.
    Keeping spend equal isolates most expenditure differences, but clipping and
    recovery history still matter. State counts are NOT samples or win rates.
    """
    states={(0,0,0,0,rules.start,rules.start)}
    witnesses={next(iter(states)):[]}
    extreme_witnesses=[]
    rows=[];terminal_total=0;global_gap=0
    for r in range(1,10):
        nxt=set();terminal=set();next_witnesses={};outcomes='W' if r==1 else 'WLD'
        for pa,pb,ta,tb,wa,wb in sorted(states):
            for spend in range(0,min(wa,wb)+1,300):
                for outcome in outcomes:
                    a,_=settle(Wallet(wa,ta),spend,outcome,rules)
                    b,_=settle(Wallet(wb,tb),spend,{'W':'L','L':'W','D':'D'}[outcome],rules)
                    qa=pa+(2 if outcome=='W' else 1 if outcome=='D' else 0)
                    qb=pb+(2 if outcome=='L' else 1 if outcome=='D' else 0)
                    st=(qa,qb,a.tier,b.tier,a.credits,b.credits)
                    (terminal if max(qa,qb)>=10 or r==9 else nxt).add(st)
                    next_witnesses.setdefault(st,witnesses[(pa,pb,ta,tb,wa,wb)]+[{"round":r,"A_result":outcome,"equal_spend":spend,"closing_credits":[a.credits,b.credits],"score_half_points":[qa,qb]}])
        all_states=nxt|terminal
        terminal_total+=len(terminal)
        gaps=[s[4]-s[5] for s in all_states]
        global_gap=max(global_gap,max(map(abs,gaps),default=0))
        rows.append({'round':r,'continuing_unique_states':len(nxt),'terminal_unique_states':len(terminal),'A_minus_B_min':min(gaps,default=0),'A_minus_B_max':max(gaps,default=0)})
        if nxt:
            hi=max(sorted(nxt),key=lambda s:s[4]-s[5]);lo=min(sorted(nxt),key=lambda s:s[4]-s[5])
            extreme_witnesses.append({"round":r,"max_A_gap":next_witnesses[hi],"min_A_gap":next_witnesses[lo]})
        states=nxt;witnesses={st:next_witnesses[st] for st in nxt}
    return {'scope':'Exhaustive for equal aggregate spend in 300-credit steps, decisive A opening win, later W/L/D. Not exhaustive of unequal spend or gameplay.','rows':rows,'maximum_absolute_wallet_gap':global_gap,'terminal_state_count_across_rounds':terminal_total,'continuing_state_extreme_witnesses':extreme_witnesses}

def run(data:Path,output:Path)->dict:
    output.mkdir(parents=True,exist_ok=True)
    identity=source_identity(data)
    rules=Rules.load(data);items=json.loads((data/'items.json').read_text())['items'];combat=json.loads((data/'combat.json').read_text())
    fighters={f:json.loads((data/'fighters'/f'{f}.json').read_text()) for f in ['rook','vale']}
    r1=[]
    for sa,sb in product([0,300,600],repeat=2):
        a,ra=settle(Wallet(rules.start),sa,'W',rules);b,rb=settle(Wallet(rules.start),sb,'L',rules)
        r1.append({'winner_round1_spend':sa,'loser_round1_spend':sb,'winner_next_credits':a.credits,'loser_next_credits':b.credits,'gap':a.credits-b.credits,'reward_only_gap':rules.win-rules.losses[0],'retained_spending_gap':sb-sa})
    variants={'current':rules,'equal_first_loss':Rules(losses=(1200,1200,1500)),'equal_all_win_loss_income':Rules(losses=(1200,1200,1200))}
    scenarios={name:{p:sequence('WWWWW',p,r) for p in ['save','equal_600','leader_saves_opponent_spends_all']} for name,r in variants.items()}
    probabilities={'assumptions':'Independent equally likely decisive rounds; identical strength, no economic effect, no draws. Pure score arithmetic, not measured game outcomes.', 'scores':[{'lead':[i,0],'leader_fraction':str(fair_match_win(i,0)),'leader_probability':float(fair_match_win(i,0)),'trailing_probability':float(1-fair_match_win(i,0))} for i in range(1,5)],'toy_sensitivity':[]}
    for p in [Fraction(1,2),Fraction(11,20),Fraction(3,5)]:
        one=p*fair_match_win(2,0)+(1-p)*fair_match_win(1,1)
        probabilities['toy_sensitivity'].append({'ASSUMED_future_round_win_probability':float(p),'match_win_if_only_round2_uses_p':float(one),'match_win_if_all_later_rounds_use_p':float(fixed_p_win(1,0,p)),'not_an_estimate_of_this_game':True})
    matrices={};supers=[];feints=[]
    for fid,f in fighters.items():
        moves={m['id']:m for m in f['moves']}
        for item in [i for i in items if fid in i['eligible_fighters']]:
            matrices[item['id']]={'item':item,'move':move_summary(moves[item['move_id']]),'base_replacement':move_summary(moves[item['replaces']]) if item['replaces'] else None,'opportunity_cost_ex_activations':item['price']//300}
        for art in f['super_arts']:
            m=moves[art['move_id']]
            supers.append({'fighter':fid,'art':art,'move':move_summary(m),'nominal_all_hit_scaled_damage_zero_combo_index_NOT_ENGINE_TEST':nominal_damage(m,combat),'max_activations_at_cap_if_no_other_spend':rules.cap//art['credit_cost'],'remainder_at_cap':rules.cap%art['credit_cost']})
        for id,direction in [('shop_step_feint','forward'),('shop_sway_feint','back')]:
            m=moves[id];duration=m['startup']+m['active']+m['recovery']
            displacement=sum((x['end']-x['start'])*x['vx'] for x in m['movement'])
            walk=f['physics']['walk_'+direction]*duration
            feints.append({'fighter':fid,'move':id,'action_ticks':duration,'authored_displacement_world_units':displacement/1000,'free_walk_absolute_distance_over_same_ticks':walk/1000,'does_not_measure_visual_deception':True})
    budgets={fid:{str(cr):{str(sc):feasible_budgets(cr,sc,items,fid) for sc in [900,1200,1500]} for cr in [600,900,1200,1500,1800,3600]} for fid in fighters}
    summary={'repository':'RJW34/Penny-Punchers','commit':'1623ee62c4f4b6d5574bbb9909eb6fe4e32f5441','mode':'AUDITOR_PYTHON_ARITHMETIC_AND_STATIC_CONTENT_ONLY','native_game_executed':False,'human_balance_proven':False,'identity_checks':identity,'rules':asdict(rules),'first_round_all_aggregate_spend_pairs':r1,'streak_scenarios':scenarios,'score_only_probabilities':probabilities,'equal_spend_reachable_ledgers':equal_spend_reachability(rules),'feint_motion_comparison':feints,'global_stat_purchase_records':[],'stat_purchase_scope':'The inspected canonical 12-item catalog leases individual actions; no global character-stat product is implemented by its item model.'}
    for name,obj in [('economy_results.json',summary),('purchase_move_matrix.json',matrices),('super_cost_payload_screen.json',supers),('affordability_frontiers.json',budgets)]:
        (output/name).write_text(json.dumps(obj,indent=2)+'\n')
    return summary

if __name__=='__main__':
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--data',type=Path,default=Path(__file__).resolve().parent/'verified_source'/'data')
    parser.add_argument('--output',type=Path,default=Path(__file__).resolve().parent/'calculations')
    args=parser.parse_args();s=run(args.data,args.output)
    print(json.dumps({'mode':s['mode'],'source_files_verified':len(s['identity_checks']),'first_round_spend_pairs':len(s['first_round_all_aggregate_spend_pairs']),'reachable_ledger_states':s['equal_spend_reachable_ledgers'],'native_game_executed':False},indent=2))
