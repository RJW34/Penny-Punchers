"""Read-only integration census. Reports candidates, not proven bugs or completed fixes."""
from pathlib import Path
import argparse,json,subprocess,hashlib,re
p=argparse.ArgumentParser();p.add_argument('--repo',type=Path,required=True);p.add_argument('--output',type=Path,required=True);a=p.parse_args()
root=a.repo.resolve()
def git(*args):
    r=subprocess.run(['git','-C',str(root),*args],capture_output=True,text=True,timeout=15)
    return {'exit_code':r.returncode,'stdout':r.stdout.strip(),'stderr':r.stderr.strip()}
head=git('rev-parse','HEAD')
if head['exit_code']:raise SystemExit('Not a readable Git checkout; do not invent repository state')
patterns=['ReserveFloor','CreditCost','SpendReceipt','canEx','canSuper','paid_action','no combat income','combat_income','SelectedSuper','success_income']
hits=[]
for base in ('src','game','data','docs','schemas'):
    folder=root/base
    if not folder.is_dir():continue
    for f in folder.rglob('*'):
        if not f.is_file() or any(n in ('bin','obj','.godot','GeneratedData') for n in f.parts) or f.suffix not in ('.cs','.json','.md'):continue
        if f.stat().st_size>2_000_000:continue
        text=f.read_text(encoding='utf-8-sig',errors='replace')
        found={term:[i+1 for i,line in enumerate(text.splitlines()) if term.lower() in line.lower()] for term in patterns}
        found={k:v for k,v in found.items() if v}
        if found:hits.append({'path':str(f.relative_to(root)),'sha256':hashlib.sha256(f.read_bytes()).hexdigest(),'review_occurrences':found})
out={'status':'READ_ONLY_RECONCILIATION_NOT_A_TEST','head':head,'branch':git('branch','--show-current'),'dirty':git('status','--short'),'hits':hits,'note':'Historical/unchanged values may be legitimate. Manually classify; never blanket-delete or overwrite source from a regex.'}
a.output.parent.mkdir(parents=True,exist_ok=True);a.output.write_text(json.dumps(out,indent=2));print('Census saved:',a.output)
