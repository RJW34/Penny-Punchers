#!/usr/bin/env python3
"""Derive reviewable CSVs from canonical moves; no gameplay claim."""
from pathlib import Path
import argparse,csv,io,json,sys
ROOT=Path(__file__).resolve().parents[1]
def tables(root=ROOT,check=False):
    count=0
    for file in sorted((root/'data/fighters').glob('*.json')):
        f=json.loads(file.read_text(encoding='utf-8'));s=io.StringIO(newline='');wr=csv.writer(s,lineterminator='\n')
        wr.writerow(['id','name','command','kind','preactive_ticks','first_active_display_frame','active_ticks','recovery_ticks','credit_cost','availability'])
        for m in f['moves']:wr.writerow([m['id'],m['name'],m['command'],m['kind'],m['startup'],m['startup']+1,m['active'],m['recovery'],m['credit_cost'],m['availability']]);count+=1
        dest=root/'design'/f"{f['id']}_move_table.csv";text=s.getvalue()
        if check:
            if not dest.is_file() or dest.read_text(encoding='utf-8')!=text:raise ValueError('Stale or missing derived table: '+str(dest))
        else:dest.parent.mkdir(parents=True,exist_ok=True);dest.write_text(text,encoding='utf-8',newline='')
    return count
if __name__=='__main__':
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('--check',action='store_true');a=p.parse_args()
    try:print(f'Derived table {"check" if a.check else "write"} PASS: {tables(check=a.check)} actions')
    except Exception as ex:print(ex,file=sys.stderr);raise SystemExit(1)
