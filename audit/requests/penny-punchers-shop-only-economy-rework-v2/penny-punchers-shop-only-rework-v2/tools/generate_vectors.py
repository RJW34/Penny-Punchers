"""Emit deterministic authoring-oracle vectors, never native gameplay evidence."""
from pathlib import Path
import json,sys,hashlib
from dataclasses import asdict
ROOT=Path(__file__).resolve().parents[1];sys.path.insert(0,str(ROOT))
from reference.model import Model,Plan,Fact

def vectors():
    out=[]
    def make(name,banks,products,attempts):
        m=Model();m.bank=list(banks);inputs={'banks':list(banks),'fighters':list(m.fighters),'plans':[list(p) for p in products],'attempts':attempts}
        m.commit(Plan(tuple(products[0])),Plan(tuple(products[1])))
        result=[]
        for a in attempts:result.append(m.activate(a['seat'],a['move'],a['id'],a.get('legal',True)))
        out.append({'id':name,'input':inputs,'expected':{'activation_results':result,'bank':m.bank,'super_uses':m.super_uses,'owned':m.owned}})
    make('zero-bank-licensed-ex',(600,600),[['rook_pulse_ex_license'],[]],[{'seat':0,'move':'pulse_ex','id':str(i)} for i in range(4)])
    make('rich-unowned',(3600,3600),[[],[]],[{'seat':0,'move':'pulse_ex','id':'a'},{'seat':0,'move':'super_1','id':'b'}])
    make('one-super-use',(900,900),[['rook_super_1_permit'],[]],[{'seat':0,'move':'super_1','id':'a'},{'seat':0,'move':'super_1','id':'b'}])
    make('illegal-super-kept',(900,900),[['rook_super_1_permit'],[]],[{'seat':0,'move':'super_1','id':'a','legal':False}])
    make('mixed-ex-plus-super',(2400,2400),[['rook_pulse_ex_license','rook_rise_ex_license','rook_super_1_permit'],[]],[{'seat':0,'move':'pulse_ex','id':'a'},{'seat':0,'move':'rise_ex','id':'b'},{'seat':0,'move':'super_1','id':'c'}])
    make('p2-license-independent',(600,600),[[],['vale_pulse_ex_license']],[{'seat':1,'move':'pulse_ex','id':'a'},{'seat':0,'move':'pulse_ex','id':'b'}])
    facts=[
        Fact('counter',1,0,'root1',0,0,defender_offensive_startup=True),
        Fact('anti',2,0,'root2',0,0,defender_airborne=True,defender_voluntary_air=True,defender_offensive_startup=True),
        Fact('perfect',3,0,'enemy1',1,1,outcome='parry',damage=0,parry_age=1,fresh_manual_parry=True),
        Fact('normal',4,0,'enemy2',1,1,outcome='parry',damage=0,parry_age=2,fresh_manual_parry=True),
    ]
    m=Model();m.commit(Plan(('rook_pulse_ex_license',)))
    reward_results=[m.reward(f) for f in facts]
    before=m.bank.copy();m.mark_terminal(1,6);settled=m.settle(6)
    out.append({'id':'skill-rewards-on-loss','input':{'banks':[600,600],'plans':[['rook_pulse_ex_license'],[]],'facts':[asdict(f) for f in facts],'winner':1,'terminal_tick':6},'expected':{'reward_results':reward_results,'fight_bank':before,'settlement':settled,'closing_bank':m.bank}})
    # Category denial, no highest-to-lowest fallback.
    m=Model();m.commit();facts=[]
    for i in range(1,4):facts.append(Fact(str(i),i,0,'aa'+str(i),0,0,defender_airborne=True,defender_voluntary_air=True,defender_offensive_startup=True))
    amounts=[m.reward(f)['amount'] for f in facts]
    out.append({'id':'capped-aa-no-counter-fallback','input':{'facts':[asdict(f) for f in facts]},'expected':{'amounts':amounts,'pending':m.ledger[0].pending,'counts':m.ledger[0].counts}})
    # Reference snapshot roundtrip and deliberate prediction correction.
    m=Model();m.bank=[900,900];m.commit(Plan(('rook_super_1_permit',)));s=m.snapshot();m.activate(0,'super_1','predicted');m.reward(Fact('p',1,0,'enemy',1,1,outcome='parry',damage=0,parry_age=0,fresh_manual_parry=True));m.restore(s)
    out.append({'id':'corrected-prediction','input':{'purchased':'rook_super_1_permit','then_restore_pre_start':True},'expected':{'bank':m.bank,'super_uses':m.super_uses,'pending':[l.pending for l in m.ledger]}})
    return {'format':'PP_SHOP_V2_ORACLE_VECTORS_1','native_executed':False,'note':'No collision/input recognition is executed. Normalize expected contract fields in production; Python snapshot hashes are not C# binary hashes.','cases':out}

def budgets():
    rows=[]
    for fighter,bank,ids in [
        ('rook',600,['rook_pulse_ex_license']),('rook',600,['rook_step_feint']),
        ('rook',1200,['rook_pulse_ex_license','rook_knee_ex_license']),
        ('rook',1800,['rook_clinch','rook_pulse_ex_license']),
        ('rook',2400,['rook_rise_ex_license','rook_pulse_ex_license','rook_super_1_permit']),
        ('vale',600,['vale_pulse_ex_license']),('vale',1800,['vale_return_pulse','vale_pulse_ex_license']),
        ('vale',2400,['vale_rise_ex_license','vale_pulse_ex_license','vale_super_1_permit']),
        ('rook',1425,['rook_rise_ex_license','rook_step_feint']),('rook',600,[])]:
        m=Model(fighters=(fighter,'vale' if fighter=='rook' else 'rook'));m.bank=[bank,600];r=m.commit(Plan(tuple(ids)))
        rows.append({'fighter':fighter,'opening':bank,'products':ids,'shop_cost':r['costs'][0],'saved_bank':m.bank[0],'repeatable':[k for k,v in m.owned[0].items() if v!='super_permit'],'super':[k for k,v in m.owned[0].items() if v=='super_permit'],'super_uses':m.super_uses[0]})
    return {'all_prices_unvalidated':True,'combat_spend':0,'examples':rows}
if __name__=='__main__':
    directory=ROOT/'data/vectors';directory.mkdir(exist_ok=True)
    for name,d in [('conformance.json',vectors()),('budgets.json',budgets())]:(directory/name).write_text(json.dumps(d,indent=2)+'\n')
    print(json.dumps({'oracle_cases':len(vectors()['cases']),'budget_examples':len(budgets()['examples']),'native_execution':False},indent=2))
