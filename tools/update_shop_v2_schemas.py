"""Migrate strict authoring schemas to the shop-only resource contracts."""
from pathlib import Path
import json
ROOT=Path(__file__).resolve().parents[1]
def load(name):return json.loads((ROOT/'schemas'/name).read_text(encoding='utf-8'))
def save(name,data):(ROOT/'schemas'/name).write_text(json.dumps(data,indent=2)+'\n',encoding='utf-8')
def main():
 economy=load('economy.schema.json');p=economy['properties'];economy['title']='Shop-only bank and round capabilities'
 p['resource_model']={'const':'shop_only_bank_round_capabilities'}
 for field,value in [('ex_activation_cost',0),('activation_debit','none'),('default_reserve_floor',0),('reserve_policy','disabled_nonzero_rejected'),('combat_bank_writes',False),('loadout_cap',2400)]:p[field]={'const':value}
 p['super_activation_costs']={'const':[0]}
 for slot in ('ex','super'):p['slot_prices']['properties'][slot]={'type':'integer','minimum':1,'maximum':2400}
 p['slot_prices']['required']=['signature','technique','gambit','ex','super']
 p['slot_limits']={'type':'object','properties':{k:{'const':v} for k,v in dict(signature=1,technique=1,gambit=1,ex=2,super=1).items()},'required':['signature','technique','gambit','ex','super'],'additionalProperties':False}
 limits=dict(counter_hit=1000,anti_air=1000,perfect_parry=1000,category_limit=10,total_limit=3000,perfect_window_eligible_ticks=10)
 p['reward_policy']={'type':'object','properties':{k:{'type':'integer','minimum':1 if k=='perfect_window_eligible_ticks' else 0,'maximum':v} for k,v in limits.items()},'required':list(limits),'additionalProperties':False}
 economy['required']=list(dict.fromkeys(economy['required']+['slot_limits','combat_bank_writes','reward_policy']));save('economy.schema.json',economy)
 items=load('items.schema.json');items['title']='Round rentals, repeatable EX licenses and one-use super permits';entry=items['properties']['items']['items'];p=entry['properties']
 p['slot']['enum']=['signature','technique','gambit','ex','super'];p['price']['maximum']=2400;p['use_limit']={'enum':[None,1]}
 p['access_policy']={'enum':['round_license','prepaid_super']};p['implementation_status']={'const':'implemented'};p['conflicts_with']={'type':'array','maxItems':255,'uniqueItems':True,'items':{'type':'string','minLength':1,'maxLength':128}}
 entry['required']=list(dict.fromkeys(entry['required']+['access_policy','implementation_status','conflicts_with']))
 entry['allOf']=[{'if':{'properties':{'slot':{'const':'super'}}},'then':{'properties':{'use_limit':{'const':1},'access_policy':{'const':'prepaid_super'}}},'else':{'properties':{'use_limit':{'type':'null'},'access_policy':{'const':'round_license'}}}}]
 save('items.schema.json',items)
 fighters=load('fighter.schema.json');move=fighters['properties']['moves']['items'];move['properties']['credit_cost']={'const':0};move['properties']['debit_on_start']={'const':False};move['properties']['access_policy']={'enum':['base','round_license','prepaid_super']};move['required']=list(dict.fromkeys(move['required']+['access_policy']));fighters['properties']['super_arts']['items']['properties']['credit_cost']={'const':0};save('fighter.schema.json',fighters)
 fighters['properties']['super_arts']['items']['properties']['selected_at']={'const':'preparation_commit'};save('fighter.schema.json',fighters)
 print('Updated strict economy, product and fighter schemas for shop-only v2.')
if __name__=='__main__':main()
