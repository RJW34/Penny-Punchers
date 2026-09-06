"""Independent shop-only authoring oracle. NOT the game's combat/rollback implementation.

Contact facts must be produced by an actual simulator in production. This oracle checks
ledger, ownership and reward arithmetic on supplied facts; it does not detect collisions.
"""
from __future__ import annotations
from dataclasses import dataclass, asdict, field
from pathlib import Path
from typing import Optional
import copy
import hashlib
import json

ROOT = Path(__file__).resolve().parents[1]

def load_inputs(root: Path = ROOT):
    rules = json.loads((root / 'data/rules.v2.json').read_text())
    catalog = json.loads((root / 'data/catalog.v2.json').read_text())['entries']
    base = {}
    for fighter in ('rook', 'vale'):
        f = json.loads((root / f'provenance/baseline/fighters/{fighter}.json').read_text())
        base[fighter] = [m['id'] for m in f['moves'] if m['availability'] == 'base' and m['kind'] not in ('ex_special','super')]
    return rules, catalog, base

@dataclass(frozen=True)
class Plan:
    product_ids: tuple[str, ...] = ()

@dataclass(frozen=True)
class Fact:
    event_id: str
    tick: int
    earner: int
    root: str
    origin_owner: int
    effective_owner: int
    outcome: str = 'hit'
    attack_kind: str = 'direct'
    contact_ordinal: int = 0
    damage: int = 35
    threat_damage: int = 35
    accepted: bool = True
    attacker_grounded: bool = True
    attacker_prejump: bool = False
    defender_airborne: bool = False
    defender_voluntary_air: bool = False
    defender_disabled: bool = False
    defender_in_combo: bool = False
    defender_offensive_startup: bool = False
    parry_age: int = -1
    fresh_manual_parry: bool = False
    parry_edge_frozen: bool = False
    training: bool = False

@dataclass
class Ledger:
    pending: int = 0
    counts: dict[str,int] = field(default_factory=lambda: {'counter_hit':0,'anti_air':0,'perfect_parry':0})
    roots: dict[str,str] = field(default_factory=dict)
    facts: dict[str,dict] = field(default_factory=dict)
    receipts: list[dict] = field(default_factory=list)
    last_order: Optional[tuple] = None


def classify(f: Fact, rules: dict) -> Optional[str]:
    if not f.accepted or f.training:
        return None
    if f.outcome == 'parry':
        if (f.threat_damage > 0 and f.attack_kind in ('direct','projectile','field') and f.origin_owner != f.earner
            and f.effective_owner == 1-f.earner and f.fresh_manual_parry and not f.parry_edge_frozen
            and 0 <= f.parry_age < rules['reward_policy']['perfect_window_eligible_ticks']):
            return 'perfect_parry'
        return None
    if (f.outcome != 'hit' or f.damage <= 0 or f.attack_kind != 'direct'
        or f.effective_owner != f.earner or f.origin_owner != f.earner
        or f.defender_disabled or f.defender_in_combo):
        return None
    if (f.attacker_grounded and not f.attacker_prejump and f.defender_airborne
        and f.defender_voluntary_air):
        return 'anti_air'
    if f.defender_offensive_startup:
        return 'counter_hit'
    return None

class Model:
    def __init__(self, rules=None, catalog=None, base=None, fighters=('rook','vale'), session='reference'):
        dr,dc,db = load_inputs()
        self.rules = copy.deepcopy(dr if rules is None else rules)
        entries = dc if catalog is None else catalog
        self.catalog = {e['id']:copy.deepcopy(e) for e in entries}
        if len(self.catalog) != len(entries): raise ValueError('duplicate catalog IDs')
        self.base = db if base is None else copy.deepcopy(base)
        if len(fighters) != 2 or any(f not in self.base for f in fighters): raise ValueError('two known fighters required')
        if not session or len(session)>80: raise ValueError('invalid session')
        self.fighters = tuple(fighters); self.session = session
        self.bank = [self.rules['starting_credits']]*2
        self.tiers = [0,0]; self.score = [0,0]; self.round = 1; self.completed = 0
        self.phase = 'shop'; self.plans = [[],[]]; self.owned = [dict(),dict()]
        self.super_uses = [0,0]; self.ledger = [Ledger(),Ledger()]
        self.activation_receipts = {}
        self.prep_key = None; self.prep_payload = None; self.prep_receipt = None
        self.settlement_key = None; self.settlement_receipt = None
        self.terminal = None; self.frozen_bank = None

    def _seat(self, seat):
        if type(seat) is not int or seat not in (0,1): raise ValueError('seat')

    def validate_plan(self, seat: int, plan: Plan):
        self._seat(seat)
        if not isinstance(plan,Plan) or len(plan.product_ids)>6: raise ValueError('malformed plan')
        ids = tuple(sorted(plan.product_ids))
        if len(set(ids)) != len(ids): raise ValueError('duplicate product')
        counts = {}; total=0; owns={}; replaced=set()
        for product in ids:
            if product not in self.catalog: raise ValueError('unknown product')
            e=self.catalog[product]
            if e['fighter'] != self.fighters[seat]: raise ValueError('wrong fighter')
            if e['activation_price'] != 0: raise ValueError('v2 forbids activation prices')
            slot=e['slot']; counts[slot]=counts.get(slot,0)+1
            if counts[slot]>self.rules['slot_limits'][slot]: raise ValueError('slot limit')
            if any(c in ids for c in e['conflicts_with']): raise ValueError('product conflict')
            if e['replaces_move_id']:
                if e['replaces_move_id'] in replaced: raise ValueError('replacement collision')
                replaced.add(e['replaces_move_id'])
            for move in e['move_ids']:
                if move in owns: raise ValueError('duplicate move ownership')
                owns[move]=e['kind']
            total+=e['shop_price']
        if total>self.bank[seat] or total>self.rules['round_shop_spend_cap']: raise ValueError('unaffordable plan')
        return total, owns, list(ids)

    def commit(self, a=Plan(), b=Plan(), key=None):
        key=key or f'{self.session}:prep:{self.round}'
        payload=[sorted(a.product_ids),sorted(b.product_ids)]
        if self.prep_receipt is not None:
            if key==self.prep_key and payload==self.prep_payload: return copy.deepcopy(self.prep_receipt)
            raise ValueError('conflicting commit')
        if self.phase!='shop': raise ValueError('shop closed')
        # Preflight BOTH players before any mutation.
        validated=[self.validate_plan(0,a),self.validate_plan(1,b)]
        before=self.bank.copy()
        for s,(cost,owns,ids) in enumerate(validated):
            self.bank[s]-=cost; self.owned[s]=owns; self.plans[s]=ids
            self.super_uses[s]=int(any(k=='super_permit' for k in owns.values()))
        self.frozen_bank=self.bank.copy();self.phase='fight'
        self.prep_key=key;self.prep_payload=payload
        self.prep_receipt={'key':key,'round':self.round,'opening':before,'costs':[v[0] for v in validated],'after':self.bank.copy(),'products':payload}
        return copy.deepcopy(self.prep_receipt)

    def activate(self,seat:int,move:str,key:str,legal:bool=True):
        self._seat(seat)
        if not key or len(key)>128: raise ValueError('bad attempt key')
        signature=(seat,move)
        if key in self.activation_receipts:
            if tuple(self.activation_receipts[key]) != signature: raise ValueError('conflicting activation ID')
            return 'duplicate'
        if self.phase!='fight' or not legal:return 'illegal'
        replaced={self.catalog[p]['replaces_move_id'] for p in self.plans[seat]}
        if move in replaced:return 'replaced'
        if move in self.base[self.fighters[seat]]: status='base'
        elif move in self.owned[seat]:
            if self.owned[seat][move]=='super_permit':
                if self.super_uses[seat]==0:return 'exhausted'
                self.super_uses[seat]-=1; status='super_use'
            else:status='licensed'
        else:return 'not_owned'
        self.activation_receipts[key]=signature
        # This assertion checks the model, not the C# engine.
        assert self.bank==self.frozen_bank
        return status

    def reward(self,f:Fact):
        self._seat(f.earner)
        if (f.tick<0 or not f.root or not f.event_id or len(f.root)>128 or len(f.event_id)>128
            or f.origin_owner not in (0,1) or f.effective_owner not in (0,1) or f.contact_ordinal<0):
            raise ValueError('malformed fact')
        if self.phase!='fight':return {'amount':0,'reason':'outside_fight'}
        l=self.ledger[f.earner];payload=asdict(f)
        if f.event_id in l.facts:
            if l.facts[f.event_id]!=payload:raise ValueError('conflicting reward event')
            return {'amount':0,'reason':'duplicate_event'}
        category=classify(f,self.rules)
        if category is None:return {'amount':0,'reason':'ineligible'}
        order=(f.tick,f.root,f.contact_ordinal)
        if l.last_order is not None and order<tuple(l.last_order): raise ValueError('unsorted authoritative facts')
        l.last_order=order
        root_key=('defense:' if category=='perfect_parry' else 'offense:')+f.root
        if root_key in l.roots:return {'amount':0,'reason':'duplicate_root'}
        rp=self.rules['reward_policy'];rule=rp[category]
        # Once globally capped, no new fact/root history is needed for reward computation.
        if l.pending>=rp['total_cap_per_player_per_round']:return {'amount':0,'reason':'global_cap'}
        l.roots[root_key]=category;l.facts[f.event_id]=payload
        if l.counts[category]>=rule['max_awards_per_round']:
            return {'amount':0,'reason':'category_cap','category':category}
        amount=min(rule['amount'],rp['total_cap_per_player_per_round']-l.pending)
        l.counts[category]+=1;l.pending+=amount
        receipt={'id':f'{self.session}:{self.round}:{f.earner}:{root_key}', 'tick':f.tick,'category':category,'nominal':rule['amount'],'amount':amount}
        l.receipts.append(receipt)
        assert self.bank==self.frozen_bank
        return copy.deepcopy(receipt)

    def mark_terminal(self,winner:int,tick:int):
        if winner not in (-1,0,1) or tick<0:raise ValueError('invalid terminal')
        if self.phase!='fight':raise ValueError('not fighting')
        if any(l.last_order and tick<l.last_order[0] for l in self.ledger):raise ValueError('terminal precedes contact')
        self.terminal={'winner':winner,'tick':tick};self.phase='terminal'

    def settle(self,confirmed_tick:int,key=None):
        key=key or f'{self.session}:settle:{self.round}'
        if self.settlement_receipt is not None:
            if key==self.settlement_key:return copy.deepcopy(self.settlement_receipt)
            raise ValueError('conflicting settlement')
        if self.phase!='terminal' or self.terminal is None or confirmed_tick<self.terminal['tick']:raise ValueError('unconfirmed terminal')
        rows=[];winner=self.terminal['winner']
        for s in (0,1):
            tier=self.tiers[s]
            if winner==-1:base=self.rules['draw_payout'];new_tier=tier;self.score[s]+=1
            elif s==winner:base=self.rules['win_payout'];new_tier=max(0,tier-1);self.score[s]+=2
            else:base=self.rules['loss_payouts'][tier];new_tier=min(2,tier+1)
            room=self.rules['wallet_cap']-self.bank[s]
            base_grant=min(base,room);bonus=self.ledger[s].pending;bonus_grant=min(bonus,room-base_grant)
            old=self.bank[s];self.bank[s]+=base_grant+bonus_grant;self.tiers[s]=new_tier
            rows.append({'seat':s,'before':old,'base_nominal':base,'base_granted':base_grant,'skill_earned':bonus,'skill_granted':bonus_grant,'clipped':base+bonus-base_grant-bonus_grant,'closing':self.bank[s],'old_tier':tier,'new_tier':new_tier})
        self.completed+=1
        over=max(self.score)>=self.rules['target_half_points'] or self.completed>=self.rules['max_rounds']
        self.phase='match_over' if over else 'result'
        self.settlement_key=key;self.settlement_receipt={'key':key,'round':self.round,'rows':rows,'score_half_points':self.score.copy(),'match_over':over}
        return copy.deepcopy(self.settlement_receipt)

    def next_round(self):
        if self.phase!='result':raise ValueError('no next round')
        self.round+=1;self.phase='shop';self.plans=[[],[]];self.owned=[{},{}];self.super_uses=[0,0]
        self.ledger=[Ledger(),Ledger()];self.activation_receipts={};self.terminal=None;self.frozen_bank=None
        self.prep_key=self.prep_payload=self.prep_receipt=self.settlement_key=self.settlement_receipt=None

    def snapshot(self)->bytes:
        state={k:copy.deepcopy(v) for k,v in self.__dict__.items() if k not in ('rules','catalog','base','ledger')}
        state['ledger']=[asdict(l) for l in self.ledger]
        state['identity']=self.identity()
        return json.dumps(state,sort_keys=True,separators=(',',':')).encode()

    def identity(self):
        return hashlib.sha256(json.dumps([self.rules,self.catalog,self.base],sort_keys=True,separators=(',',':')).encode()).hexdigest()

    def restore(self,snapshot:bytes):
        state=json.loads(snapshot)
        if state.pop('identity')!=self.identity():raise ValueError('wrong reference rules/content')
        if tuple(state['fighters'])!=self.fighters or state['session']!=self.session:raise ValueError('wrong session/config')
        # Trusted oracle snapshot, not the production hostile-input deserializer.
        state['fighters']=tuple(state['fighters']);state['ledger']=[Ledger(**l) for l in state['ledger']]
        self.__dict__.update(state)

    def hash(self):return hashlib.sha256(self.snapshot()).hexdigest()
