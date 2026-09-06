"""Preserve the implemented direct-spend checkpoint before the shop-only semantic migration."""
from pathlib import Path
from datetime import datetime,timezone
import hashlib,json,zipfile
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'reports/evidence/pre-shop-v2-checkpoint';OUT.mkdir(parents=True,exist_ok=True)
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def main():
 paths=set()
 for directory in ['src/StrikeLedger.Core','src/StrikeLedger.CoreTests','src/StrikeLedger.App','src/StrikeLedger.NetworkLab','src/StrikeLedger.BalanceLab','data','design/buyables-v1/runtime-baseline']:
  for p in (ROOT/directory).rglob('*'):
   if p.is_file() and not any(part in ('bin','obj') for part in p.relative_to(ROOT/directory).parts):paths.add(p)
 for name in ['Directory.Build.props','tools/compile_buyables.py','tools/audit_buyables_coverage.py','tools/summarize_buyables_pilot.py','tools/archive_pre_shop_v2.py']:
  paths.add(ROOT/name)
 for name in ['CoreTests','NetworkLab']:
  for p in (ROOT/f'src/StrikeLedger.{name}/bin/Release/net8.0').glob('*'):
   if p.is_file():paths.add(p)
 for directory in ['pre-shop-v2-core','pre-shop-v2-full','upgrade-pre-v2-regressions','buyables-rental-pilot','buyables-opening-pilot','buyable-objects-final']:
  for p in (ROOT/'reports/evidence'/directory).rglob('*'):
   if p.is_file():paths.add(p)
 for name in ['pre-shop-v2-core-console.log','pre-shop-v2-full-console.log','buyables-rental-pilot-console.log','buyables-opening-pilot-console.log','buyables-action-coverage.json']:
  p=ROOT/'reports/evidence'/name
  if p.exists():paths.add(p)
 records=[dict(path=p.relative_to(ROOT).as_posix(),size=p.stat().st_size,sha256=sha(p)) for p in sorted(paths)]
 core=json.loads((ROOT/'reports/evidence/pre-shop-v2-core/core-conformance.json').read_text());full=json.loads((ROOT/'reports/evidence/pre-shop-v2-full/core-conformance.json').read_text())
 assert core['failed']==full['failed']==0
 manifest=dict(format='penny-pre-shop-only-v2-checkpoint-v1',utc=datetime.now(timezone.utc).isoformat(),scope='Exact implemented direct-spend core/full trial source, numeric data, current short conformance and runnable verifier assemblies; historical completed pilots retain their original identities.',current_core_assembly_id=core['core_assembly_id'],current_core_dll_sha256=sha(ROOT/'src/StrikeLedger.CoreTests/bin/Release/net8.0/StrikeLedger.Core.dll'),core_content_hash=core['content_hash'],full_content_hash=full['content_hash'],current_core_tests=core['passed'],current_full_tests=full['passed'],files=records,file_count=len(records),uncompressed_bytes=sum(r['size'] for r in records),fixes_included=['PP040 mutual throw arbitration','PP041 air parry landing','PP042 taunt cleanup','PP043 authored phase hurtboxes','PP044 air facing and block posture','All38 buyable library recipes with100core/115full action nodes','Actor end-of-tick marker cleanup and vault prejump flag','Explicit illegal actor command rejection without fallback','App ledger uses actual terminal tick'],evidence_limits=['The384 rental and108 opening pilot runs precede final actor-marker/prejump/diagnostic cleanup. Their stored source/build/content identities are preserved; they are not claimed to execute the final archived core DLL.','Existing earlier network matrix and object evidence retain their earlier build identity. The new shop-only resource model requires fresh tests and cannot inherit their resource conclusions.','No v2 shop-only, reward, capability or zero-bank license semantics are implemented or certified by this archive.'])
 target=OUT/'pre-shop-v2-checkpoint.zip'
 with zipfile.ZipFile(target,'w',compression=zipfile.ZIP_DEFLATED,compresslevel=6) as z:
  for p in sorted(paths):z.write(p,p.relative_to(ROOT).as_posix())
  z.writestr('CHECKPOINT_MANIFEST.json',json.dumps(manifest,indent=2)+'\n')
  z.writestr('CHECKPOINT_README.md','# Pre-shop-only-v2 checkpoint\n\nThis is the implemented direct-spend buyables trial, preserved before the new user-directed shop-only rewrite. Extract into an isolated directory. Run the archived CoreTests DLL with --data data or --data data/rulesets/buyables_full. The included exact source and per-file manifest explain scope; historical pilot identities are not final-core certification. Do not restore these files over active v2 work.\n')
 with zipfile.ZipFile(target) as z:
  assert z.testzip() is None
  for r in records:assert hashlib.sha256(z.read(r['path'])).hexdigest()==r['sha256']
 result=dict(path=target.relative_to(ROOT).as_posix(),size=target.stat().st_size,sha256=sha(target),file_count=len(records),verified_crc_and_every_member_hash=True,current_core_tests=core['passed'],current_full_tests=full['passed'],core_assembly_id=core['core_assembly_id'],core_content_hash=core['content_hash'],full_content_hash=full['content_hash'])
 (OUT/'result.json').write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8');print(json.dumps(result,indent=2))
if __name__=='__main__':main()
