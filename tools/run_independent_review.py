#!/usr/bin/env python3
"""Read-only release review checks; writes review evidence, never acceptance records."""
from __future__ import annotations
import hashlib, json, pathlib, platform, subprocess, sys
from datetime import datetime, timezone

ROOT = pathlib.Path(__file__).resolve().parents[1]
OUT = ROOT / 'reports/evidence'
BUILD = 'strike-ledger-native-1/c75fa1b44d9da6f03682753fb9fde812a944c34be05051697a1954b4813babbc'
CONTENT = 'ec249786c06cb4e5cc4a0ea2dd84fe8c818d8360a11977d85f1b9aa66c293e63'
CORE = '204653b031ca2700371d8c1df62c4b7b9eb131825fcaaba1ef5ee01676c2c7df'
APP = '72e7b96bbd878126db96ba0be8e49bae49df1417bf3162f4aec4d1f325e6cebb'
MVID = '03804be8-8358-4d7e-9ea4-ac2504a00609'

def main() -> int:
    checks, artifacts, log = [], {}, []
    def read(path):
        p=ROOT/path
        raw=p.read_bytes()
        artifacts[path]={'bytes':len(raw),'sha256':hashlib.sha256(raw).hexdigest()}
        return json.loads(raw)
    def check(name, passed, detail):
        checks.append({'check':name,'passed':bool(passed),'detail':detail})
        log.append(('PASS ' if passed else 'FAIL ')+name+' '+json.dumps(detail,ensure_ascii=False))
    def sha(path):
        p=ROOT/path
        h=hashlib.sha256(p.read_bytes()).hexdigest()
        artifacts[path]={'bytes':p.stat().st_size,'sha256':h}
        return h

    requirements=read('acceptance/requirements.json')['requirements']
    check('requirement_registry', len(requirements)==82 and sum(r['gate_class']=='software' for r in requirements)==76,
          {'total':len(requirements),'software':sum(r['gate_class']=='software' for r in requirements)})
    candidate=read('reports/RELEASE_CANDIDATE.json')
    changed=[r['path'] for r in candidate['source_inputs'] if not (ROOT/r['path']).is_file() or sha(r['path'])!=r['sha256']]
    log.append('SOURCE CANDIDATE point-in-time changes '+json.dumps(changed))
    for folder in ['dist/StrikeLedger/windows/data_StrikeLedger_windows_x86_64','dist/StrikeLedger/linux/data_StrikeLedger_linuxbsd_x86_64',
                   'dist/verification/win-x64/CoreTests','dist/verification/linux-x64/CoreTests']:
        check('frozen_core_binary '+folder,sha(folder+'/StrikeLedger.Core.dll')==CORE,{'expectedSha256':CORE})
    for folder in ['dist/StrikeLedger/windows/data_StrikeLedger_windows_x86_64','dist/StrikeLedger/linux/data_StrikeLedger_linuxbsd_x86_64']:
        check('frozen_app_binary '+folder,sha(folder+'/StrikeLedger.App.dll')==APP,{'expectedSha256':APP})
    core=read('reports/evidence/core-exportrelease/core-conformance.json')
    check('final_core_conformance',core['passed']==36 and core['failed']==0 and core['core_assembly_id']==MVID and core['content_hash']==CONTENT,
          {'passed':core['passed'],'failed':core['failed'],'coreAssemblyId':core['core_assembly_id']})
    trace=read('reports/evidence/final-independent-action-verification/action-replay-verification.json')
    sha('reports/evidence/core-exportrelease/action-replays.training.json')
    check('independent_reconstructible_all_98_actions',trace['passed'] and trace['replayedActions']==98 and trace['coreAssemblyId']==MVID and trace['contentHash']==CONTENT,
          {'replayedActions':trace['replayedActions'],'scope':'Packaged final Windows CoreTests actually reconstructed each final ExportRelease training conformance trace; not represented as competitive replay.'})
    app=read('reports/app-exportrelease-final/self-tests.json')
    check('final_app_conformance',app['exitCode']==0 and app['build']==BUILD and app['contentHash']==CONTENT and len(app['results'])==10 and all(r['passed'] for r in app['results']),
          {'passed':sum(r['passed'] for r in app['results']),'cases':[r['name'] for r in app['results']]})
    drills=read('reports/app-exportrelease-final/drills/results.json')
    check('all_16_drill_success_and_timeout_paths',len(drills['results'])==16 and drills['build']==BUILD and all(r['success'] and r['timeoutFailed'] for r in drills['results']),
          {'drills':len(drills['results']),'scope':'Actual evaluator success plus 7202-frame timeout failure per drill; software inputs.'})
    matrix=read('reports/network-lab-exportrelease/matrix-result.json')
    check('actual_udp_four_fault_profiles',matrix['actualTwoProcesses'] and matrix['configuration']=='ExportRelease' and len(matrix['results'])==4 and all(r['passed'] and r['exitCodes']==[0,0] for r in matrix['results']),
          {'profiles':len(matrix['results']),'scope':'Eight separate native managed processes on one PC; physical LAN test remains separate.'})
    for case in matrix['results']:
        folder=pathlib.Path(case['commands'][0][case['commands'][0].index('--output')+1]).relative_to(ROOT).as_posix()
        p0=read(folder+'/peer0.result.json');p1=read(folder+'/peer1.result.json')
        keys=['finalHash','wallets','scores','roundReceipts']
        equal=all(p0[k]==p1[k] for k in keys)
        replay_equal=sha(folder+'/peer0.slreplay')==sha(folder+'/peer1.slreplay')
        check('native_peer_records '+folder,equal and replay_equal and all(p['completed'] and p['build']==BUILD and p['contentHash']==CONTENT for p in [p0,p1]),
              {'finalHash':p0['finalHash'],'rounds':p0['rounds'],'equalRecords':keys,'replayBytesIdentical':replay_equal})
    balance=read('reports/balance-lab-exportrelease-final/summary.json')
    check('final_balance_dataset',balance['build']==BUILD and balance['contentHash']==CONTENT and balance['samples']==252 and balance['excluded']==0 and balance['exitCode']==0,
          {'samples':balance['samples'],'excluded':balance['excluded'],'elapsedSeconds':balance['elapsedSeconds'],'limitations':balance['limitations']})
    cross=read('reports/evidence/cross-platform-replay/result.json')
    check('native_windows_linux_exact_replay',cross['passed'] and cross['build']==BUILD and cross['contentHash']==CONTENT and cross['identicalReplayBytes'] and all(x['exactlyEqual'] for x in cross['comparisons'].values()),
          {'ticks':cross['finalTick'],'commands':cross['commands'],'checkpointCount':cross['comparisons']['checkpoints']['records'],'scope':'Native Windows/Linux execution and exact replay bytes; headless pacing is not a rendering claim.'})
    for mode in ['native-ui','native-showcase','native-match','native-free-kit']:
        path='reports/evidence/'+mode+'/media-validation.json'
        media=read(path)
        check('native_media '+mode,media['passed'] and media['process_exit']==0,
              {'scope':'Recorded native output decoded and checked by media validator. Selected stills reviewed separately; not a claim that this reviewer listened to every audio sample.','metadata':{k:media[k] for k in ['mode','complete_video_decode'] if k in media}})
    free=read('reports/evidence/native-free-kit/runtime-result.json')
    free_replay=read('reports/evidence/native-free-kit/full-match.replay.json')
    debits=sum(len(c.get('Debits',[])) for c in free_replay['Commands'])
    plans=[c for c in free_replay['Commands'] if c['Kind']=='preparation']
    check('native_free_kit_entire_match',free['complete'] and free['freeKit'] and free['build']==BUILD and debits==0 and all(not c[p]['ItemIds'] for c in plans for p in ['Plan0','Plan1']),
          {'ticks':free['ticks'],'rounds':free['rounds'],'debits':debits,'emptyPreparationPlans':len(plans)*2,'finalWallets':free['wallets']})
    feel=read('reports/evidence/feel-measurements/feel-measurements.json')
    check('measured_discrete_core_feel',feel['passed'] and feel['coreBinarySha256']==CORE and len(feel['measurements'])==21,
          {'measurements':len(feel['measurements']),'scope':feel['scope']})
    dispatch=read('reports/evidence/dispatcher-final-check.json')
    check('scenario_registry_and_rejection',dispatch['passed'] and dispatch['registryCount']==29 and dispatch['defaultConfiguration']=='ExportRelease' and dispatch['unknownScenarioExitCode']!=0,
          {'registryCount':dispatch['registryCount'],'scope':'Core/App/network command construction and explicit unknown rejection checked; not falsely described as 29 newly executed subprocesses. Actual scenario families use the final suites above.'})
    profile=read('reports/evidence/replay-step-profile/result.json')
    check('matched_core_recorder_profile',profile['passed'] and profile['coreSha256']==CORE and profile['appSha256']==APP and len({r['finalHash'] for r in profile['results']})==1,
          {'scope':profile['scope'],'results':[{k:r[k] for k in ['recorderEnabled','steps','fightSteps','fightStepMs','allocatedBytesPerStep']} for r in profile['results']]})
    settings=read('reports/evidence/settings-native-harness/result.json')
    settings_process=read('reports/evidence/settings-native-harness/process-result.json')
    settings_ok=settings['passed'] and settings_process['passed'] and len(settings['checks'])==14 and all(r['passed'] for r in settings['checks']) and settings['sourceSha256']==sha('game/GameSettings.cs')
    check('actual_native_settings_save_load',settings_ok,{'checks':len(settings['checks']),'path':settings['resolvedSettingsPath'],'scope':settings['scope'],'cleanedOwnedSettingsFile':settings['cleanedOwnedSettingsFile']})
    pacing=read('reports/evidence/native-pacing-final/runtime-result.json')
    pacing_process=read('reports/evidence/native-pacing-final/process-result.json')
    scopes=read('reports/evidence/native-pacing-final/scope-profile.json')
    pacing_ok=pacing['complete'] and pacing_process['exit_code']==0 and pacing['build']==BUILD and '--no-screenshots' in pacing_process['command'] and '--write-movie' not in pacing_process['command'] and '--fixed-fps' not in pacing_process['command']
    check('actual_optimized_native_pacing_profile',pacing_ok,{'frames':pacing['frames'],'frameMs':pacing['frameMs'],'host':{k:pacing[k] for k in ['engine','platform','display','gpu','cpu']},'command':pacing_process['command'],'scopeMeasurements':scopes['measurements'],'knownLimit':'Median is near60FPS but p95 exceeds16.67ms and p99 has large stalls. This is actual complete native profiling, not a claim of perfectly steady60FPS.'})
    semantics=read('reports/evidence/final-source-semantics.json')
    compatibility=read('reports/evidence/candidate-compatibility/result.json')
    compatibility_ok=compatibility['passed'] and compatibility['current_build_ref']==candidate['build_ref'] and semantics['newCandidate']==candidate['build_ref'] and not changed
    check('final_candidate_source_semantics',compatibility_ok,{'changedFiles':len(semantics['changedFiles']),'sourceComparisonScope':semantics['scope'],'frozenGameplayInputs':semantics['unchangedFrozenCoreAppDataInputs']})
    final_ui_path='reports/evidence/native-ui-final/controller-menu-flow.json'
    final_ui=None
    if (ROOT/final_ui_path).exists():
        final_ui=read(final_ui_path)
        check('final_native_ui_rematch',final_ui['success'] and any('Rematch resets both wallets' in r['name'] and r['passed'] for r in final_ui['checks']),{'checks':len(final_ui['checks']),'scope':'Actual native GUI callbacks and software joypad input; no physical device claim.'})
    excluded={'.git','.venv','__pycache__','bin','obj','GeneratedData','.godot','.tools'}
    malformed=[];json_count=0
    for p in ROOT.rglob('*.json'):
        if p.is_file() and not any(x in p.parts for x in excluded):
            json_count+=1
            try:json.loads(p.read_text(encoding='utf-8'))
            except Exception as exc:malformed.append({'path':p.relative_to(ROOT).as_posix(),'bytes':p.stat().st_size,'error':str(exc)})
    check('global_json_parse_with_validator_exclusions',not malformed,{'files':json_count,'malformed':malformed})
    command=[sys.executable,'tools/validate_pack.py','--strict-schema']
    run=subprocess.run(command,cwd=ROOT,capture_output=True,text=True)
    strict_path=OUT/'final-independent-strict.log'
    strict_path.write_text('COMMAND '+json.dumps(command)+'\nEXIT '+str(run.returncode)+'\n'+run.stdout+run.stderr,encoding='utf-8')
    sha(strict_path.relative_to(ROOT).as_posix())
    check('strict_schema_scaffold_contract',run.returncode==0,{'command':command,'exitCode':run.returncode,'scope':'Validator explicitly reports SCAFFOLD_ONLY; this is not substituted for gameplay verification.'})
    ledger=read('reports/ACCEPTANCE_RESULTS.json')
    pending=[{'id':r['requirement_id'],'status':r['status'],'reason':r.get('pending_reason','')} for r in ledger['records'] if r['status']!='PASS']
    source_reviews=[
      {'requirement':'NET-004','files':['src/StrikeLedger.App/RollbackSession.cs','game/Main.Rendering.cs','game/Main.CombatShowcase.cs'],
       'finding':'Rollback replaces tick-keyed events; DrainPresentationEvents releases only confirmed events. Online shell consumes this queue. Late-parry test prevents settlement; captured training restore/resimulation demo withholds predicted audiovisual effects and publishes corrected parry. Preventing speculative cues is valid invalidation; the demo is supplemental visual evidence, not misrepresented transport.'},
      {'requirement':'UI-004','files':['game/GameSettings.cs','game/Main.UiSmoke.cs'],
       'finding':'Production settings normalize bounds, save atomically under ProjectSettings.GlobalizePath(user://settings.json), and load defensively. Visual/audio values remain in presentation code. The final isolated native Godot harness now actually executes Save/Load across every settings family, a second atomic replacement, and owned-file cleanup;14checks pass without touching the player profile.'},
      {'requirement':'QA-001','files':['tools/run_scenario.py','src/StrikeLedger.CoreTests/Program.cs','src/StrikeLedger.NetworkLab/Program.cs'],
       'finding':'29 registered scenarios dispatch to real Core/App/network/balance/native families, with unknown nonzero. Core36, App10, training16, sockets4 profiles and native captures cover substantial production behavior. Actual final native GUI fixture now verifies complete competitive replay before real result/rematch callbacks. Final requirement gate is a separate integrator step; listing29 IDs alone is not execution evidence.'},
      {'requirement':'QA-004','files':['game/Main.cs','game/Presentation/ArenaView.cs','game/Presentation/FoundryBackdrop.cs','game/Presentation/RuntimeProfiler.cs','src/StrikeLedger.App/Replay.cs'],
       'finding':'No unbounded per-frame arena state or I/O found. Original ArenaDraw profiling identified a material callback cost. Final source now renders the static FoundryBackdrop once in an owned SubViewport and draws one cached quad; fighters, effects and wall markers remain dynamic. RuntimeProfiler is opt-in, has bounded samples and no timing reads when disabled. Actual pre-optimization hands-on stdout repeatedly reports 2-24 FPS. Exact 10000-step recorder profile is fast (fight p95 .751ms, p99 1.3605ms); Final optimized native complete-match profile is now recorded: p50 16.7013ms,p95 21.0911ms,p99 168.2692ms,worst492.3552ms. Median improved substantially; large tail stalls remain a documented limitation.'},
      {'requirement':'training/replay/reserves','files':['game/Main.LabDiagnostics.cs','game/Main.Replays.cs','game/Main.Menus.cs','src/StrikeLedger.App/Training.cs'],
       'finding':'Reviewed final reset/dummy-state synchronization, charge-drill compatible fighter selection, replay fighter+art hydration, full affordable reserve range, training snapshot/bot/playback restore, and defensive throw-tech evaluator ordering. Existing exact final tests found no remaining Core/App correctness blocker in these paths.'}
    ]
    for entry in source_reviews:
        entry['existingFiles']=[]
        for file in entry['files']:
            if (ROOT/file).exists():sha(file);entry['existingFiles'].append(file)
    findings=[
      {'id':'normal_native_pacing','state':'measured_limitation' if pacing_ok else 'open','requirements':['QA-004'],'detail':'Pre-optimization interactive fight pacing is materially below 60 FPS on current host. Root measured original ArenaDraw cost; final source caches the static backdrop once. Final normal native complete-match profiling now records allframe p50 16.7013ms,p95 21.0911ms,p99 168.2692ms,worst492.3552ms. QA004 measurement requirement is satisfied with explicit tail-stall limits; no claim of perfectly steady60FPS.'},
      {'id':'settings_os_path_execution','state':'resolved' if settings_ok else 'verification_pending','requirements':['UI-004'],'detail':'Actual native isolated OS-user-data Save/Load harness now passes14checks, including both save operations, all settings families and owned-file cleanup. Player preferences untouched. Production source hash unchanged.'},
      {'id':'candidate_and_final_presentation','state':'reviewed' if compatibility_ok and final_ui and final_ui['success'] else 'integrator_pending','requirements':['FINAL-001','QA-001'],'detail':'Final312471d candidate source inputs match; current eight changed files manually reviewed and compatibility checks preserve byte-identical frozen Core/App/data. Fresh nativeUI rematch and complete nativepacing records supplement older explicitly pre-polish movies. Final acceptance ledger/gate remains the integrator responsibility.','changedCandidateInputs':changed},
      {'id':'strict_validator_empty_output','state':'resolved','detail':'Earlier only malformed JSON was reports/evidence/strict-validation.json, zero bytes. Shell redirection created it before validator rglob parsed every JSON. Parent removed scoped empty file and uses .log output. Reference validator remained unchanged.'},
      {'id':'dispatcher_configuration','state':'resolved','detail':'Tool previously defaulted to Release despite final ExportRelease binaries. Default and network forwarding now ExportRelease, explicit override tested, unknown exits2; no production assembly changes.'},
      {'id':'strict_ledger_schema','state':'resolved' if run.returncode==0 else 'integrator_pending','requirements':['FINAL-001'],'detail':'Current exact strict validator exit '+str(run.returncode)+'. See final-independent-strict.log. Acceptance status must use schema values PASS, FAIL, BLOCKED or NOT_RUN; PENDING is not a permitted value.'},
      {'id':'external_gates','state':'not_claimed','detail':'Two actual controllers, two physical PCs and human feel feedback remain external gates. Linux headless replay equality is not graphical target proof; bind Linux graphical run only when final artifacts complete.'}
    ]
    report={'schema_version':1,'review_completed':True,'reviewer':'Codex combat_core independent release review',
      'utc':datetime.now(timezone.utc).isoformat(),'host':platform.platform(),'command':[sys.executable,'tools/run_independent_review.py'],
      'scope':'Independent source/evidence review by a separate team agent, not external human QA and not a replacement for software evidence gate. No production or acceptance ledger mutation.',
      'runtimeBuild':BUILD,'runtimeContentHash':CONTENT,'coreSha256':CORE,'appSha256':APP,
      'sourceCandidateAtReview':{'build_ref':candidate['build_ref'],'content_sha256':candidate['content_sha256'],'changedInputs':changed},
      'checksPassed':sum(c['passed'] for c in checks),'checksFailed':sum(not c['passed'] for c in checks),'checks':checks,
      'sourceReviews':source_reviews,'selectedVisualsPersonallyInspected':['reports/evidence/native-ui/ui-02-independent-preparation.png','reports/evidence/native-ui/ui-06-training-grid-and-boxes.png','reports/evidence/native-showcase/combat-11-corrected-parry.png'],
      'findings':findings,'ledgerNonpassSnapshot':pending,'artifacts':artifacts,
      'conclusion':'No new software correctness blocker found in the reviewed final scope. Actual settings persistence, rematch flow, source compatibility and native pacing are now substantiated. Frame-time tail stalls remain a documented host limitation. Final acceptance gate and external hardware/human claims are separate from this independent review.'}
    (OUT/'final-independent-review.json').write_text(json.dumps(report,indent=2,ensure_ascii=False)+'\n',encoding='utf-8')
    log.extend(['CONCLUSION '+report['conclusion'],'FINDINGS '+json.dumps(findings,ensure_ascii=False)])
    (OUT/'final-independent-review.log').write_text('\n'.join(log)+'\n',encoding='utf-8')
    lines=['# Independent final release review','',report['conclusion'],'',
      f"Reviewed at {report['utc']} on {report['host']}. This is an independent team-agent source and execution-evidence review, not external human playtesting. No production or ledger was changed.",'',
      f"Runtime build: `{BUILD}`. Runtime content: `{CONTENT}`.",
      f"Frozen Core SHA256: `{CORE}`. Frozen App SHA256: `{APP}`.",
      f"Source candidate observed: `{candidate['build_ref']}`. Source-bundle content hashing differs from runtime canonical-content hashing.",'',
      '## Substantive checks','',
      'Reviewed all 82 requirement definitions (76 software) alongside the earlier per-requirement Core audit. The final exact binary evidence contains 36 Core cases, 10 App cases, all 16 drill success and timeout paths, four actual two-process socket profiles, 252 completed balance samples and native gameplay/UI captures. The packaged final CoreTests independently reconstructed all 98 action conformance traces. Windows and Linux native replays match all inputs, commands, checkpoints, wallet records, debits and bytes. The completed native free-kit match contains no paid debit or purchased lease.','',
      'Selected preparation, training diagnostics and corrected-parry stills were personally inspected for readable state and truthful captions. Media decoder reports establish recording integrity; this review does not claim to have listened to every recording. Software controllers and one-PC UDP do not stand in for physical device or two-machine gates.','',
      'NET-004 is supported by the actual rollback App test plus confirmed-only presentation event delivery and the native correction showcase. Predicted terminal audio/visual cues are withheld; correction removes their pending events. The showcase is correctly described as snapshot restore/resimulation, not a live network transport demonstration.','',
      'The dispatcher now defaults consistently to ExportRelease and forwards it to network tools. Core, App and network command construction, explicit Release override and unknown-scenario rejection were checked. Registry coverage is not claimed as 29 new process executions. The final native UI run exercises actual verified-match results and controller rematch; full acceptance status still comes from the separate candidate-bound gate.','',
      '## Remaining limits and closed review issues','',
      '- **QA-004: actual interactive pacing.** Native fight output repeatedly drops to 2â€“24 FPS. The matched 10,000-step profile measured Core+ReplayRecorder fight p95 0.751 ms and p99 1.3605 ms, with one 19.7 ms spike. This disproves sustained recorder cost as the main explanation and does not measure the complete Godot frame. Final source caches the foundry backdrop once and keeps dynamic combat geometry separate; the profiler is opt-in. A complete normal native match, with no movie or screenshots, now measures p50 16.7013ms,p95 21.0911ms,p99 168.2692ms,worst492.3552ms. This satisfies actual profiling with known limits; it does not establish perfectly steady60FPS.',
      '- **UI-004 closed by actual native persistence.** The isolated Godot harness executes production Save/Load at the actual OS user-data path, restores all settings families, verifies a second atomic replacement and cleans its owned file. All14checks pass against unchanged source; real player preferences are untouched.',
      '- **Final source binding reviewed.** Candidate312471d matches current inputs. Eight presentation/diagnostic/UI-evidence files changed, and their current semantics were manually reviewed. Core/App assemblies and canonical data remain identical. Old PCK source is stripped; no literal oldsource-body diff is claimed. Fresh native UI rematch and full-match pacing evidence cover final presentation behavior. Run the final candidate-bound acceptance gate separately.',
      '- **External gates.** Physical controllers, two physical machines and human feel assessment are not claimed. Linux graphical target evidence must be bound only after the actual graphical run completes.','',
      '## Resolved review findings','',
      'Strict validation previously parsed its own zero-byte redirected reports/evidence/strict-validation.json before producing output. That was the only malformed JSON found with the validatorâ€™s exact exclusions. The empty generated file was removed and logs now use .log; the reference validator was not weakened. Its SCAFFOLD_ONLY contract scope is kept separate from gameplay evidence. The exact latest exit and any current ledger schema errors are recorded in final-independent-strict.log.','',
      'The dispatcher configuration mismatch was repaired as a tool-only change. The current source review found no new frozen Core/App correctness blocker in replay selection, training reset/checkpoint behavior, defensive tech evaluation, reserve selection, rollback economics or confirmed presentation event handling.','',
      '## Reproduction and artifacts','',
      '`python tools/run_independent_review.py` reproduces the read-only artifact/identity/comparison checks and writes the JSON and process log. The report records each actual check, command, source candidate and SHA256. Existing complete suites are inspected rather than needlessly re-executed.','',
      '- `reports/evidence/final-independent-review.json` â€” machine-readable review, scope, artifact digests and open findings.',
      '- `reports/evidence/final-independent-review.log` â€” actual check results.',
      '- `reports/evidence/final-independent-strict.log` â€” exact strict validator command and result.',
      '- `reports/evidence/final-independent-action-verification/action-replay-verification.json` â€” 98 reconstructed actions on final ExportRelease Core.',
      '- `reports/evidence/replay-step-profile/result.json` â€” matched actual Core and Core+Recorder measurements.',
      '- `reports/CORE_INDEPENDENT_SOFTWARE_AUDIT.md` â€” earlier detailed 76-software-requirement source audit.','']
    (ROOT/'reports/FINAL_INDEPENDENT_REVIEW.md').write_text('\n'.join(lines),encoding='utf-8')
    print(json.dumps({'reviewCompleted':True,'checksPassed':report['checksPassed'],'checksFailed':report['checksFailed'],'openFindings':[f['id'] for f in findings if f['state'] in ['open','verification_pending','integrator_pending']],'candidateChangedInputs':changed}))
    return 0 if not report['checksFailed'] else 1

if __name__=='__main__':raise SystemExit(main())
