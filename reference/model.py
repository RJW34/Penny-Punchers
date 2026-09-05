"""Independent single-wallet arithmetic oracle, NOT a fighting-game engine.

No movement, action recognizer, Godot, sockets or real controller execution is implemented
here. Production must independently implement/verify those systems and consume fixtures.
"""
from __future__ import annotations
from dataclasses import dataclass, replace, asdict
from pathlib import Path
import copy, json

ROOT = Path(__file__).resolve().parents[1]
E = json.loads((ROOT/'data/economy.json').read_text(encoding='utf-8'))
ITEMS = {x['id']: x for x in json.loads((ROOT/'data/items.json').read_text(encoding='utf-8'))['items']}

def integer(v: int, minimum: int = 0, maximum: int = 2_147_483_647) -> int:
    if type(v) is not int or not minimum <= v <= maximum:
        raise ValueError(f'Invalid integer {v!r}, expected [{minimum},{maximum}]')
    return v

def token(value: str) -> str:
    if not isinstance(value,str) or not value.strip() or len(value)>128:
        raise ValueError('Nonempty bounded string required')
    return value

@dataclass(frozen=True)
class Account:
    credits: int = 600
    recovery_tier: int = 0
    def __post_init__(self) -> None:
        integer(self.credits,0,E['wallet_cap'])
        integer(self.recovery_tier,0,E['max_recovery_tier'])


def payout(account: Account, outcome: str) -> tuple[Account,dict]:
    tier=account.recovery_tier
    if outcome=='win':
        amount=E['win_payout'];next_tier=max(0,tier-1)
    elif outcome=='loss':
        amount=E['loss_payouts'][tier];next_tier=min(E['max_recovery_tier'],tier+1)
    elif outcome=='draw':
        amount=E['draw_payout'];next_tier=tier
    else: raise ValueError('Unknown outcome')
    grant=min(amount,E['wallet_cap']-account.credits)
    return Account(account.credits+grant,next_tier), {'outcome':outcome,'nominal':amount,'granted':grant,'clipped':amount-grant,'old_tier':tier,'new_tier':next_tier}

@dataclass(frozen=True)
class Plan:
    fighter_id: str
    lease_ids: tuple[str,...] = ()
    reserve_floor: int = 0
    def __post_init__(self) -> None:
        if self.fighter_id not in ('rook','vale'): raise ValueError('Unknown fighter')
        if type(self.lease_ids) is not tuple: raise ValueError('Immutable lease id tuple required')
        integer(self.reserve_floor,0,E['wallet_cap'])

def quote_plan(account: Account,plan: Plan) -> dict:
    if len(plan.lease_ids)>3 or len(set(plan.lease_ids)) != len(plan.lease_ids):
        raise ValueError('Too many or duplicate leases')
    slots=set();cost=0
    for iid in plan.lease_ids:
        if iid not in ITEMS: raise ValueError('Unknown lease')
        item=ITEMS[iid]
        if plan.fighter_id not in item['eligible_fighters']: raise ValueError('Wrong fighter')
        if item['slot'] in slots: raise ValueError('Duplicate slot')
        slots.add(item['slot']);cost+=item['price']
    if cost>E['loadout_cap'] or cost>account.credits: raise ValueError('Unaffordable plan')
    remaining=account.credits-cost
    if plan.reserve_floor>remaining: raise ValueError('Reserve exceeds post-purchase balance')
    return {'lease_ids':sorted(plan.lease_ids),'cost':cost,'credits_after':remaining,'reserve_floor':plan.reserve_floor}

class PreparationLedger:
    """Atomic/idempotent arithmetic model; not a live lobby or authentication service."""
    def __init__(self,accounts: tuple[Account,Account],round_id: int):
        if type(accounts) is not tuple or len(accounts)!=2: raise ValueError('Exactly two accounts')
        integer(round_id,1,9)
        self.accounts=accounts;self.round_id=round_id;self._key=None;self._payload=None;self._receipt=None
    def commit(self,plans: tuple[Plan,Plan],command_id: str)->dict:
        token(command_id)
        if type(plans) is not tuple or len(plans)!=2: raise ValueError('Exactly two plans')
        payload=json.dumps([{'fighter_id':x.fighter_id,'lease_ids':sorted(x.lease_ids),'reserve_floor':x.reserve_floor} for x in plans],sort_keys=True)
        if self._key is not None:
            if self._key!=command_id or self._payload!=payload: raise ValueError('Already committed or id conflict')
            return copy.deepcopy(self._receipt)
        quotes=[quote_plan(a,p) for a,p in zip(self.accounts,plans)] # validate both before mutation
        next_accounts=tuple(Account(q['credits_after'],a.recovery_tier) for a,q in zip(self.accounts,quotes))
        receipt={'round_id':self.round_id,'plans':quotes,'accounts':[asdict(a) for a in next_accounts]}
        self.accounts=next_accounts;self._key=command_id;self._payload=payload;self._receipt=copy.deepcopy(receipt)
        return copy.deepcopy(receipt)

@dataclass(frozen=True)
class Receipt:
    key: str
    move_id: str
    cost: int
    def __post_init__(self) -> None:
        token(self.key);token(self.move_id);integer(self.cost,1,E['wallet_cap'])

@dataclass(frozen=True)
class SpendState:
    account: Account
    reserve_floor: int = 0
    receipts: tuple[Receipt,...] = ()
    def __post_init__(self) -> None:
        integer(self.reserve_floor,0,self.account.credits)
        if type(self.receipts) is not tuple or len({r.key for r in self.receipts})!=len(self.receipts):
            raise ValueError('Unique immutable receipts required')
    def activate(self,move_id: str,cost: int,key: str,legal: bool=True)->tuple['SpendState',str]:
        token(move_id);token(key);integer(cost,0,E['wallet_cap'])
        if type(legal) is not bool: raise ValueError('Boolean legal flag required')
        prior=next((r for r in self.receipts if r.key==key),None)
        if prior:
            if prior.move_id!=move_id or prior.cost!=cost: raise ValueError('Event id content conflict')
            return self,'DUPLICATE'
        if not legal: return self,'ILLEGAL'
        if cost>self.account.credits:return self,'INSUFFICIENT'
        if self.account.credits-cost<self.reserve_floor:return self,'RESERVE'
        if cost==0:return self,'FREE'
        updated=Account(self.account.credits-cost,self.account.recovery_tier)
        return SpendState(updated,self.reserve_floor,self.receipts+(Receipt(key,move_id,cost),)),'PAID'


def singles_decision(a: int,b: int,rounds: int)->str:
    integer(rounds,0,9);integer(a);integer(b)
    if a+b!=2*rounds:raise ValueError('Point conservation failed')
    if a>=10 and a>b:return 'A'
    if b>=10 and b>a:return 'B'
    if rounds==9:return 'A' if a>b else 'B' if b>a else 'DRAW'
    return 'CONTINUE'

class MatchLedger:
    """Confirmed result arithmetic only; real terminal input confirmation is engine work."""
    def __init__(self,accounts: tuple[Account,Account]=(Account(),Account())):
        if type(accounts) is not tuple or len(accounts)!=2: raise ValueError('Exactly two accounts')
        self.accounts=accounts;self.scores=(0,0);self.rounds=0;self.decision='CONTINUE';self._records={}
    def settle(self,round_id: int,result: str,command_id: str,confirmed: bool=True)->dict:
        integer(round_id,1,9);token(command_id)
        if type(confirmed) is not bool or not confirmed:raise ValueError('Confirmed result required')
        payload=(round_id,result)
        if command_id in self._records:
            old,receipt=self._records[command_id]
            if old!=payload:raise ValueError('Command id conflict')
            return copy.deepcopy(receipt)
        if self.decision!='CONTINUE' or round_id!=self.rounds+1:raise ValueError('Terminal or out-of-order round')
        if result=='A':outcomes=('win','loss');points=(2,0)
        elif result=='B':outcomes=('loss','win');points=(0,2)
        elif result=='DRAW':outcomes=('draw','draw');points=(1,1)
        else:raise ValueError('Unknown round result')
        paid=[payout(a,o) for a,o in zip(self.accounts,outcomes)]
        accounts=tuple(x[0] for x in paid);scores=tuple(a+b for a,b in zip(self.scores,points))
        decision=singles_decision(*scores,round_id)
        receipt={'round_id':round_id,'result':result,'accounts':[asdict(a) for a in accounts],'payouts':[x[1] for x in paid],'scores':list(scores),'decision':decision}
        self.accounts=accounts;self.scores=scores;self.rounds=round_id;self.decision=decision
        self._records[command_id]=(payload,copy.deepcopy(receipt))
        return copy.deepcopy(receipt)


def trunc_div(n: int,d: int)->int:
    if type(n) is not int or type(d) is not int or d==0: raise ValueError('Integer nonzero divisor required')
    return abs(n)//abs(d)*(-1 if (n<0)!=(d<0) else 1)

def scaled_damage(base: int,combo_index: int,counterhit: bool=False)->int:
    integer(base,0,10000);integer(combo_index,0,1000)
    if type(counterhit) is not bool:raise ValueError('Boolean counterhit required')
    return trunc_div(trunc_div(base*(110 if counterhit else 100),100)*max(30,100-10*combo_index),100)

def contact_advantage(stun: int,active: int,recovery: int,active_offset: int=0)->int:
    integer(stun);integer(active,1);integer(recovery);integer(active_offset,0,active-1)
    return stun-(active-1-active_offset+recovery)

def health_verdict(a: int,a_max: int,b: int,b_max: int)->str:
    integer(a_max,1);integer(b_max,1);integer(a,0,a_max);integer(b,0,b_max)
    l,r=a*b_max,b*a_max
    return 'A' if l>r else 'B' if r>l else 'DRAW'

def relative_direction(raw: int,facing: int)->int:
    integer(raw,1,9)
    if type(facing) is not int or facing not in (-1,1):raise ValueError('Facing must be ±1')
    return raw if facing==1 else {1:3,2:2,3:1,4:6,5:5,6:4,7:9,8:8,9:7}[raw]

def button_edges(previous: int,current: int)->tuple[int,int]:
    integer(previous,0,63);integer(current,0,63)
    return current & ~previous,previous & ~current

def parry_covers(kind: str,level: str,is_throw: bool=False)->bool:
    if kind not in ('high','low','air') or level not in ('mid','low','overhead','air'):raise ValueError('Unknown kind/level')
    if type(is_throw) is not bool:raise ValueError('Boolean throw flag required')
    return not is_throw and (kind=='air' or level in ({'mid','overhead','air'} if kind=='high' else {'mid','low'}))
