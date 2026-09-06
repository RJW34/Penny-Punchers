"""Invalidate stale current-release claims after the user-approved semantic migration.

Historical ledgers are preserved, never relabeled as current v2 evidence. A later
candidate freeze and real execution must replace these NOT_RUN records.
"""
from pathlib import Path
import hashlib,json,datetime,subprocess
ROOT=Path(__file__).resolve().parents[1]
def read(p):return json.loads(p.read_text(encoding='utf-8'))
def write(p,v):p.parent.mkdir(parents=True,exist_ok=True);p.write_text(json.dumps(v,indent=2)+'\n',encoding='utf-8')
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def main():
 archive=ROOT/'audit/upgrade-2026-09-05/baseline-status'
 for name in ('STATE.json','ACCEPTANCE_RESULTS.json','RELEASE_CANDIDATE.json'):
  source=ROOT/'reports'/name;target=archive/name
  if not target.exists():target.parent.mkdir(parents=True,exist_ok=True);target.write_bytes(source.read_bytes())
 now=datetime.datetime.now(datetime.timezone.utc).isoformat()
 sources=[{'path':p.relative_to(ROOT).as_posix(),'sha256':sha(p)} for base in ('game','src/StrikeLedger.Core','src/StrikeLedger.App') for p in sorted((ROOT/base).rglob('*')) if p.is_file() and p.suffix in ('.cs','.csproj','.json','.godot','.cfg','.png','.wav','.ttf','.otf','.tscn','.import') and not any(part in ('bin','obj','.godot','GeneratedData') for part in p.parts)]
 content=[{'path':p.relative_to(ROOT/'data').as_posix(),'sha256':sha(p)} for p in sorted((ROOT/'data').rglob('*.json'))]
 source_hash=hashlib.sha256(json.dumps(sorted(sources,key=lambda x:x['path']),sort_keys=True,separators=(',',':')).encode()).hexdigest()
 content_hash=hashlib.sha256(json.dumps(content,sort_keys=True,separators=(',',':')).encode()).hexdigest()
 build='integration-source-sha256:'+source_hash
 write(ROOT/'reports/RELEASE_CANDIDATE.json',dict(schema_version=1,status='INTEGRATION_IN_PROGRESS',build_ref=build,content_sha256=content_hash,created_utc=now,source_inputs=sources,native_binaries=[],note='No current v2 release candidate is frozen. Existing export binaries are stale and are not associated with these source hashes. Run fresh build/export/candidate identity and verification before release.'))
 records=[]
 for requirement in read(ROOT/'acceptance/requirements.json')['requirements']:
  records.append(dict(requirement_id=requirement['id'],status='NOT_RUN',build_ref=build,content_sha256=content_hash,timestamp_utc=now,platform='Windows host; v2 integration in progress',command='No current-candidate execution bound yet',exit_code=-1,artifacts=[],limitations='Historical results preserved under audit/upgrade-2026-09-05/baseline-status. Current semantics require fresh production/native evidence; focused in-progress results live separately.'))
 write(ROOT/'reports/ACCEPTANCE_RESULTS.json',dict(schema_version=1,records=records))
 write(ROOT/'reports/STATE.json',dict(schema_version=1,project='Penny-Punchers',scope='shop_only_v2_1v1_traditional_fighter',status='UPGRADE_IN_PROGRESS',current_package='RW-002 through RW-007 integration',completed_packages=[],blocked_requirements=[],game_build_ref=build,game_content_sha256=content_hash,updated_utc=now,next_action='Complete current v2 Core/App/UI migrations, actual collision/reward tests, native GUI, network faults, balance controls, fresh exports and source audit before candidate promotion.',verification=dict(software=dict(status='IN_PROGRESS',passed=0,total=58),target_device=dict(status='NOT_RUN',passed=0,total=2),human=dict(status='NOT_RUN',passed=0,total=1)),historical_checkpoints=['audit/upgrade-2026-09-05/baseline-status','reports/evidence/pre-shop-v2-checkpoint/pre-shop-v2-checkpoint.zip'],external_checks=['Physical local controllers','Two physical PCs','Owner/friend feel verdict']))
 intake=ROOT/'audit/upgrade-2026-09-05/SHOP_V2_INTAKE.json'
 write(intake,dict(user_instruction='Please also integrate Penny-Punchers_Shop-Only_Economy_Rework_v2',received_package='Penny-Punchers_Shop-Only_Economy_Rework_v2.zip',archive_sha256=sha(ROOT/'Penny-Punchers_Shop-Only_Economy_Rework_v2.zip'),manifested_files_verified=76,reference_tool_tests=dict(passed=95,failed=0,native_game_executed=False),package_requirements=dict(software=58,device=2,human=1),live_git_head=subprocess.check_output(['git','rev-parse','HEAD'],cwd=ROOT,text=True).strip(),dirty_work_preserved=True,pre_migration_checkpoint=dict(path='reports/evidence/pre-shop-v2-checkpoint/pre-shop-v2-checkpoint.zip',sha256='2ab5ac246facea4480bad36ff56050979342a0ac2e60ec62359c7dcd8322b36a',files=689),catalog_preserved=dict(core_actions=100,expanded_actions=115,core_rentals=12,expanded_rentals=24,ex_products=8,super_products=6),ownership=dict(shared_core='combat_core',reward_ledger_and_tests='root',app_replay_network_training='session_network',shop_hud_presentation='presentation'),active_contract='docs/21_SHOP_ONLY_V2.md',current_candidate_frozen=False,observed_utc=now))
 print('Historical release evidence preserved; current v2 ledger correctly awaits current candidate execution.')
if __name__=='__main__':main()
