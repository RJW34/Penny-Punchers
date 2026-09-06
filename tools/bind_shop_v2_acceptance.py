"""Bind reviewed v2 evidence to the retained 82-item acceptance ledger.

This is an evidence index, not a gameplay oracle. Missing evidence stays NOT_RUN.
The independent review and the unchanged release gate remain separate checks.
"""
from pathlib import Path
from datetime import datetime, timezone
import hashlib,json

ROOT=Path(__file__).resolve().parents[1]
E='reports/evidence/'

def sha(path):
 h=hashlib.sha256()
 with path.open('rb') as f:
  for block in iter(lambda:f.read(1024*1024),b''):h.update(block)
 return h.hexdigest()

def main():
 candidate=json.loads((ROOT/'reports/RELEASE_CANDIDATE.json').read_text())
 assert candidate['status']=='READY_FOR_VERIFICATION'
 for item in candidate['source_inputs']+candidate['native_binaries']:
  assert sha(ROOT/item['path'])==item['sha256'], 'Candidate input changed: '+item['path']
 groups={
  'core':[('test_log',E+'shop-v2-core-full-current/core-conformance.json')],
  'app':[('test_log',E+'shop-v2-app-candidate-core/self-tests.json'),('test_log',E+'shop-v2-app-candidate2-full/self-tests.json')],
  'objects':[('test_log',E+'shop-v2-objects-candidate2/buyable-object-tests.json')],
  'content':[('test_log',E+'shop-v2-action-effects.json'),('replay',E+'shop-v2-core-full-current/action-replays.training.json')],
  'contract':[('test_log',E+'shop-v2-contract-candidate/contract-conformance.json')],
  'schema':[('test_log',E+'shop-v2-strict-candidate.log')],
  'build':[('build_log',E+'build.log'),('binary','dist/StrikeLedger/windows/StrikeLedger.exe'),('binary','dist/StrikeLedger/linux/StrikeLedger.x86_64')],
  'ui':[('test_log',E+'native-shop-v2-candidate-ui/controller-menu-flow.json'),('process_log',E+'native-shop-v2-candidate-ui/process.log'),('video',E+'native-shop-v2-candidate-ui/recording.mp4'),('screenshot',E+'native-shop-v2-candidate-ui/health-frame-02-partial.png')],
  'showcase':[('test_log',E+'native-shop-v2-candidate-showcase/combat-visual-showcase.json'),('video',E+'native-shop-v2-candidate-showcase/recording.mp4')],
  'match':[('process_log',E+'native-shop-v2-candidate-match/process.log'),('metrics',E+'native-shop-v2-candidate-match/runtime-result.json'),('video',E+'native-shop-v2-candidate-match/recording.mp4'),('replay',E+'native-shop-v2-candidate-match/full-match.replay.json')],
  'free':[('metrics',E+'native-shop-v2-candidate-free/runtime-result.json'),('video',E+'native-shop-v2-candidate-free/recording.mp4')],
  'network':[('test_log',E+'shop-v2-network-candidate/matrix-result.json'),('metrics',E+'shop-v2-network-candidate/rtt150-jitter20-loss3/peer0.result.json')],
  'native-network':[('test_log',E+'native-shop-v2-candidate-network/peer0/native-network-result.json'),('process_log',E+'native-shop-v2-candidate-network/peer0/process.log'),('video',E+'native-shop-v2-candidate-network/peer0/recording.mp4'),('replay',E+'native-shop-v2-candidate-network/peer0/network-match.replay.json')],
  'linux':[('process_log',E+'native-shop-v2-linux-current/process.log'),('video',E+'native-shop-v2-linux-current/recording.mp4')],
  'cross-platform':[('test_log',E+'shop-v2-cross-platform-candidate/process.log'),('replay',E+'native-shop-v2-candidate-windows-headless/full-match.replay.json')],
  'feel':[('metrics',E+'shop-v2-feel-candidate/feel-measurements.json')],
  'profile':[('metrics',E+'native-shop-v2-candidate-profile/runtime-result.json')],
  'pilot':[('metrics',E+'shop-v2-pilot-analysis.json'),('process_log',E+'shop-v2-pilot-process-results.json')],
  'pressure':[('test_log',E+'shop-v2-core-core-final/core-conformance.json'),('metrics',E+'shop-v2-core-full-current/core-conformance.json')],
  'tuning':[('metrics',E+'shop-v2-canonical-audit/result.json'),('process_log',E+'shop-v2-canonical-audit/process.log')],
  'package':[('process_log',E+'shop-v2-package-candidate/result.json')],
  'review':[('process_log',E+'shop-v2-independent-review.json')],
  'scope':[('process_log','audit/upgrade-2026-09-05/acceptance-v2-supersession.json')],
  'dispatch':[('test_log',E+'shop-v2-dispatch-candidate/dispatch-result.json')],
  'docs':[('process_log',E+'shop-v2-document-review.json')],
 }
 mapping={}
 def assign(ids,*keys):
  for rid in ids.split():mapping[rid]=list(keys)
 assign('SCOPE-001','scope')
 assign('ENV-001 BUILD-001','build')
 assign('ENV-002','schema')
 assign('CORE-001 CORE-002 CORE-003 CORE-004 INPUT-001 INPUT-002 INPUT-003 INPUT-004 MOVE-003 COMBAT-001 COMBAT-003 COMBAT-005 COMBAT-006 COMBAT-007 PARRY-001 PARRY-002 PARRY-004 ECO-001 ECO-003 ECO-004 ECO-005 SHOP-001 MATCH-002 MATCH-003','core')
 assign('CORE-005','core','ui')
 assign('MOVE-001 MOVE-002 COMBAT-002 COMBAT-004 COMBAT-008 PARRY-003 ECO-002','core','showcase')
 assign('FEEL-001','feel','free')
 assign('ECO-006','core','contract')
 assign('SHOP-002 MATCH-001','core','ui','match')
 assign('MATCH-004','app','ui')
 assign('CONTENT-001 CONTENT-002','content')
 assign('CONTENT-003','content','objects')
 assign('ART-001','ui','showcase')
 assign('AUDIO-001','ui')
 assign('BOT-001','app','match')
 assign('TRAIN-001 TRAIN-002','app','ui')
 assign('REPLAY-001','app','match')
 assign('REPLAY-002','app')
 assign('REPLAY-003','app','match')
 assign('NET-001 NET-007','native-network')
 assign('NET-002','network')
 assign('NET-003 NET-005','app')
 assign('NET-004','app','native-network')
 assign('NET-006','app','native-network')
 assign('UI-001 UI-002','ui')
 assign('UI-003 UI-004 UI-005','app','ui')
 assign('BAL-001 BAL-003','pilot')
 assign('BAL-002','pressure','pilot')
 assign('BAL-004','tuning')
 assign('QA-001','core','app','dispatch')
 assign('QA-002','core','pilot')
 assign('QA-003','core','app')
 assign('QA-004','profile','network')
 assign('QA-005','match')
 assign('BUILD-002','package')
 assign('DEVICE-001','match')
 assign('DEVICE-002','linux')
 assign('DEVICE-005','cross-platform')
 assign('FINAL-001','review')
 assign('FINAL-002','docs')
 requirements=json.loads((ROOT/'acceptance/requirements.json').read_text())['requirements']
 records=[];now=datetime.now(timezone.utc).isoformat()
 for req in requirements:
  rid=req['id'];artifacts=[];seen=set();missing=[]
  for group in mapping.get(rid,[]):
   for kind,path in groups[group]:
    if path in seen:continue
    seen.add(path);file=ROOT/path
    if not file.is_file() or not file.stat().st_size:missing.append(path);continue
    artifacts.append(dict(kind=kind,path=path,sha256=sha(file)))
  kinds={a['kind'] for a in artifacts}
  absent=set(req['required_evidence_kinds'])-kinds
  external=rid in ['DEVICE-003','DEVICE-004','HUMAN-001']
  status='NOT_RUN' if external or missing or absent else 'PASS'
  limitation='Evidence applies to the bounded described scenario; no physical-controller, second physical PC or human balance claim.'
  if rid in ['CONTENT-001','CONTENT-002']:limitation+=' Action traces are real-core training conformance and mechanism-specific route witnesses, not competitive films of every node.'
  if rid in ['BAL-001','BAL-002','BAL-003']:limitation+=' Deterministic bots and a bounded pressure/policy matrix do not establish exhaustive balance or human adaptation.'
  if rid=='ART-001':limitation+=' Supplied artwork is integrated; some rare moves reuse authored poses, as documented in known limitations.'
  if external:limitation={'DEVICE-003':'No physical controllers were available; software-injected controller inputs are separate evidence.','DEVICE-004':'Two processes and WSLg ran on this one PC; no second physical machine was tested.','HUMAN-001':'Owner/player playtest and acceptance have not been recorded.'}[rid]
  if missing or absent:limitation+=' Pending evidence: '+', '.join(missing+sorted(absent))
  records.append(dict(requirement_id=rid,status=status,build_ref=candidate['build_ref'],content_sha256=candidate['content_sha256'],timestamp_utc=now,platform='Windows x64 / native Linux x64 under WSL where identified',command='See actual commands and outcomes in linked artifacts; indexed by tools/bind_shop_v2_acceptance.py',exit_code=0 if status=='PASS' else -1,artifacts=artifacts,limitations=limitation))
 result=dict(schema_version=1,scope='Reviewed bounded current-v2 evidence; exact production identities and native source binding checked separately',records=records)
 (ROOT/'reports/ACCEPTANCE_RESULTS.json').write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
 print(json.dumps({s:sum(r['status']==s for r in records) for s in ['PASS','NOT_RUN']}))

if __name__=='__main__':main()
