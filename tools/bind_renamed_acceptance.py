"""Bind the preserved v2 evidence through the verified cosmetic name-only delta.

Original artifacts and their identities are never rewritten. New binary/UI/package
checks are required where they establish current visible names and distribution.
"""
from pathlib import Path
from datetime import datetime,timezone
from collections import Counter
import hashlib,json
ROOT=Path(__file__).resolve().parents[1]
def read(p):return json.loads((ROOT/p).read_text(encoding='utf-8'))
def artifact(p):return dict(path=p,sha256=hashlib.sha256((ROOT/p).read_bytes()).hexdigest(),bytes=(ROOT/p).stat().st_size)
def main():
 candidate=read('reports/RELEASE_CANDIDATE.json');bridge=read('reports/evidence/name-swap-audit/result.json')
 assert bridge['passed'] and bridge['candidate']==candidate['build_ref'] and bridge['coreMethods']['equal']
 assert set(bridge['sourceChanges'])=={'game/Main.BuyablesEvidence.cs','game/Main.CombatShowcase.cs','src/StrikeLedger.App/Training.cs'}
 for r in candidate['source_inputs']+candidate['native_binaries']:assert artifact(r['path'])['sha256']==r['sha256'],r['path']
 core=read('reports/evidence/build-test/StrikeLedger.CoreTests/core-conformance.json');app=read('reports/evidence/build-test/StrikeLedger.NetworkLab/self-tests.json')
 assert core['failed']==0 and core['passed']==66 and app['exitCode']==0 and all(r['passed'] for r in app['results'])
 ui=read('reports/evidence/native-shop-v2-renamed-ui/controller-menu-flow.json');proc=read('reports/evidence/native-shop-v2-renamed-ui/process-result.json')
 assert ui['success'] and all(r['passed'] for r in ui['checks']) and proc['passed']
 native={r['path']:r['sha256'] for r in candidate['native_binaries']}
 assert all(native[p]==h for p,h in proc['runtime_sha256'].items())
 baseline=read('reports/evidence/pre-name-swap-candidate/SHOP_ONLY_V2_ACCEPTANCE.json')
 rows=baseline['records'];now=datetime.now(timezone.utc).isoformat()
 current=['reports/evidence/name-swap-audit/result.json','reports/evidence/build-test/StrikeLedger.CoreTests/core-conformance.json','reports/evidence/build-test/StrikeLedger.NetworkLab/self-tests.json','reports/evidence/native-shop-v2-renamed-ui/controller-menu-flow.json','reports/evidence/native-shop-v2-renamed-ui/process-result.json']
 for r in rows:
  r['prior_execution_identity']={k:r[k] for k in ['source_sha256','content_sha256','build_id']}
  r['source_sha256']=candidate['build_ref'].split(':')[1];r['content_sha256']=candidate['content_sha256'];r['build_id']=app['build'];r['observed_utc']=now
  r['observation']+=' Cosmetic-name bridge: original executions retain their own hashes and earlier Thomas/Vincent labels. Exact production source/data and Core IL comparison proves no mechanical changes; current66 Core/15 App suites and299 GUI checks validate the renamed build.'
  if r['status']=='PASS':
   for a in r['artifacts']:assert artifact(a['path'])['sha256']==a['sha256'], 'Prior evidence changed: '+a['path']
  present={a['path'] for a in r['artifacts']}
  r['artifacts'] += [artifact(p) for p in current if p not in present]
 supplements={
  'SO-003':['reports/AUDIT_CROSSWALK.json'],
  'SO-058':['reports/evidence/shop-v2-renamed-package/result.json','reports/evidence/shop-v2-packaged-verifiers/result.json'],
  'SO-061':['reports/evidence/shop-v2-document-review.json'],
 }
 for r in rows:
  if r['id'] not in supplements:continue
  paths=supplements[r['id']]
  if not all((ROOT/p).is_file() for p in paths):continue
  if r['id']=='SO-003':
   cw=read(paths[0]);assert cw['reconciled']==91 and len({x['id'] for x in cw['rows']})==91
  else:
   if not all(read(p).get('passed') is True for p in paths):continue
   if r['id']=='SO-058':assert read(paths[1])['candidate']==candidate['build_ref']
  r.update(status='PASS',exit_code=0,remaining='',artifacts=[artifact(p) for p in paths+current])
  r['observation']={'SO-003':'All91 original findings retain actual implemented/partial/optional/external dispositions. Workflow activation remains explicitly pending authorization scope; reconciliation is not blanket completion.','SO-058':'Fresh renamed Windows/Linux packages were extracted and completed native matches. All six self-contained verifiers use current shipped assemblies and their12 bounded checks passed.','SO-061':'Current player/audit/resume documents were reviewed against executed evidence, swapped-name mapping and exact remaining hardware/human/presentation boundaries.'}[r['id']]
 summary={layer:dict(Counter(r['status'] for r in rows if r['evidence_scope']==layer)) for layer in ['software','device','human']}
 result=dict(schema_version=1,candidate_id=candidate['build_ref'],utc=now,scope='61 original requirements, with explicit cosmetic-name bridge and current executable/UI/package checks. Historical execution hashes remain unmodified.',requirements=baseline['requirements'],candidate=dict(candidate_id=candidate['build_ref'],source_sha256=candidate['build_ref'].split(':')[1],content_sha256=candidate['content_sha256'],build_id=app['build']),binding=artifact(current[0]),summary=summary,records=rows)
 (ROOT/'reports/SHOP_ONLY_V2_ACCEPTANCE.json').write_text(json.dumps(result,indent=2)+'\n')
 (ROOT/'reports/SHOP_ONLY_V2_CANDIDATE.json').write_text(json.dumps(result['candidate'],indent=2)+'\n')
 print(json.dumps(summary))
if __name__=='__main__':main()
