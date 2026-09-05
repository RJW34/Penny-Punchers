"""Bind reviewed, existing executable evidence to the immutable candidate.

This records an evidence audit, not new gameplay. It never edits requirements,
the release gate, candidate identity, production code, or earlier run results.
Pending requirements retain the precise missing observation. Every referenced
artifact must exist and is hashed from its actual bytes.
"""
from __future__ import annotations
import argparse, datetime, hashlib, json, platform, subprocess, sys
from pathlib import Path

ROOT=Path(__file__).resolve().parents[1]
E='reports/evidence/'
CORE=E+'core-exportrelease/core-conformance.json'
APP='reports/app-exportrelease-final/self-tests.json'
BAL='reports/balance-lab-exportrelease-final/'
UI=E+'native-ui-final/'
SHOW=E+'native-showcase-final/'
MATCH=E+'native-match-final/'
FREE=E+'native-free-kit/'
NET=E+'native-network/'
AUDIT=E+'acceptance-ledger-audit.json'

def read(path):return json.loads((ROOT/path).read_text(encoding='utf-8-sig'))
def sha(path):
    h=hashlib.sha256()
    with (ROOT/path).open('rb') as f:
        for block in iter(lambda:f.read(1024*1024),b''):h.update(block)
    return h.hexdigest()
def exists(path):return (ROOT/path).is_file() and (ROOT/path).stat().st_size>0
def a(kind,path):return (kind,path)

B={
'core':[a('test_log',CORE)],
'app':[a('test_log',APP)],
'actions':[a('replay',E+'core-exportrelease/action-replays.training.json'),a('test_log',E+'final-independent-action-verification/action-replay-verification.json')],
'feel':[a('metrics',E+'feel-measurements/feel-measurements.json'),a('process_log',E+'feel-measurements/process.log')],
'show':[a('test_log',SHOW+'combat-visual-showcase.json'),a('video',SHOW+'recording.mp4'),a('process_log',SHOW+'process-result.json'),a('process_log',E+'showcase-final-media-review.json')],
'ui':[a('test_log',UI+'controller-menu-flow.json'),a('video',UI+'recording.mp4'),a('process_log',UI+'process-result.json'),a('process_log',E+'ui-final-media-review.json')],
'match':[a('test_log',MATCH+'runtime-result.json'),a('process_log',MATCH+'process-result.json'),a('video',MATCH+'recording.mp4'),a('metrics',MATCH+'media-validation.json'),a('process_log',E+'match-final-media-review.json')],
'free':[a('metrics',FREE+'runtime-result.json'),a('process_log',FREE+'process-result.json'),a('video',FREE+'recording.mp4')],
'native-net':[a('process_log',NET+'peer0/process-result.json'),a('test_log',NET+'peer0/native-network-result.json'),a('test_log',NET+'peer1/native-network-result.json'),a('video',NET+'peer0/recording.mp4'),a('replay',NET+'peer0/native-network.replay.json')],
'socket':[a('metrics','reports/network-lab-exportrelease/matrix-result.json'),a('process_log','reports/network-lab-exportrelease/rtt150-jitter20-loss3/peer0.result.json'),a('process_log','reports/network-lab-exportrelease/rtt150-jitter20-loss3/peer1.result.json')],
'replay':[a('replay','reports/app-exportrelease-final/full-match.slreplay'),a('test_log','reports/app-exportrelease-final/bot-match-result.json')],
'economy':[a('metrics','reports/app-exportrelease-final/economy.json'),a('metrics',NET+'peer0/native-network.replay.economy.json')],
'drills':[a('test_log','reports/app-exportrelease-final/drills/results.json'),a('replay','reports/app-exportrelease-final/drills/rollback_spend-success.json')],
'balance':[a('metrics',BAL+'summary.json'),a('metrics',BAL+'analysis.json'),a('metrics',BAL+'rounds.jsonl'),a('process_log','reports/BALANCE_NOTES.md')],
'build':[a('build_log',E+'final-export-console.log'),a('build_log',E+'root-test-console.log'),a('binary','dist/StrikeLedger/windows/StrikeLedger.exe'),a('binary','dist/StrikeLedger/linux/StrikeLedger.x86_64')],
'cross':[a('test_log',E+'cross-platform-replay/result.json'),a('process_log',E+'cross-platform-replay/process.log'),a('replay',E+'native-linux-headless/full-match.replay.json'),a('replay',E+'native-windows-headless/full-match.replay.json')],
'linux':[a('process_log',E+'native-linux/process-result.json'),a('test_log',E+'native-linux/runtime-result.json'),a('video',E+'native-linux/recording.mp4')],
'review':[a('process_log',E+'final-independent-review.json'),a('process_log',E+'final-independent-review.log'),a('process_log','reports/FINAL_INDEPENDENT_REVIEW.md')],
'input':[a('test_log',E+'input-settings-checks.log'),a('process_log',E+'input-settings-review.json'),a('process_log','assets/presentation_checks/inputchecks/Program.cs'),a('process_log','assets/presentation_checks/inputchecks/InputChecks.csproj')],
'package':[a('process_log',E+'package-verification/result.json'),a('process_log',E+'package-verification/final-candidate-binding.json'),a('process_log',E+'package-verification/windows/process.log'),a('process_log',E+'package-verification/linux/process.log')],
'settings-native':[a('test_log',E+'settings-native-harness/result.json'),a('process_log',E+'settings-native-harness/process-result.json'),a('process_log',E+'settings-native-harness/native.log')],
}

# Each note is the scope reviewed against the substantive requirement, rather
# than treating the presence of a correctly named file as proof of completion.
M={}
def put(ids,bundles,note,extra=()):
    for rid in ids.split():M[rid]={'bundles':bundles.split(),'note':note,'extra':list(extra)}

put('SCOPE-001','', 'Actual workspace/Git/tool inspection records the preserved source ZIP, strictly two fighters, one scalar wallet and reuse of toolchain binaries only.',[a('process_log',E+'workspace-inspection.log')])
put('ENV-001','build','Matched Godot4.6.3 .NET/editor/templates and SDK8.0.424 actually restore/build/import/export. Native completion is evidenced separately.')
put('ENV-002','','The actual root command runs content copying, compile/import,15 scaffold-seed checks,36 production core suites and10 App suites. Seed validation is explicitly labeled as insufficient to establish gameplay.',[a('test_log',E+'root-test-console.log'),a('test_log','reports/pack_checks/strict_validation.log')])
put('CORE-001 CORE-002 CORE-003 CORE-004','core','Production core36-suite conformance covers canonical integer state, full snapshot roundtrips, deterministic resimulation, bounded content and input state, and mirrored seat traces; the tested Core DLL matches the exports.')
put('CORE-005','core cross match','The graphical and accelerated headless native runs produce the identical12789-tick match hash and replay. Graphical callback intervals vary in the recorded runtime metrics while core outcomes remain fixed60.',[a('screenshot',MATCH+'match-result.png')])
put('INPUT-001','core ui','Core input tests plus actual GUI device remapping establish part of the requirement; explicit SOCD/six-button/unplug/held-input software assertions are tracked separately.')
put('INPUT-002 INPUT-003 INPUT-004','core','Current production tests exercise both-facings motions,45-tick charge/facing reset, timing rejection, all98 command recognitions, negative-edge paid rejection and compound button priorities.')
put('MOVE-001 MOVE-002','core feel show','Actual movement/stop/dash/jump/contact traces and exported legal-input showcase are recorded. Feel measurements report discrete core timings and displacement, not OS/display latency.')
put('MOVE-003','core','Movement/wall/pushbox tests and seeded seat symmetry cover floor/wall, crossing and equal-position behavior without default engine physics.')
put('COMBAT-001 COMBAT-003 COMBAT-005 COMBAT-006 COMBAT-007','core','Production action/contact tests cover half-open action clocks, contact-only cancels, target/link/reversal paths, projectile count/clash, simultaneous rank/trades, chip, dizzy and bounded juggle state.')
put('COMBAT-002 COMBAT-004 COMBAT-008','core show drills','Current core assertions cover guard, proximity/command actions, throws/tech/immunity, quick rise/reversal and selected kara rules. Exported showcase visibly exercises real legal contact and throw state; drill traces separately exercise recovery.')
put('PARRY-001 PARRY-002 PARRY-004','core drills','Real high/low/air/red timing and source-freeze tests plus actual drill input traces establish fresh-edge arms, retry rules, blockstun boundaries and snapshot-safe parry clocks.')
put('PARRY-003','core show','Exported showcase uses five fresh parry inputs to defend every paid super hit with one startup debit; core multihit tests assert no damage/chip/stun/credit reward.')
put('FEEL-001','feel free','Complete observed free-kit native match and21 measured production-core sequences cover motion, stop, input response, freeze and authored timing. Reported native frame pacing is capture-loaded, not normal-play60FPS acceptance.',[a('process_log','reports/FEEL_CALIBRATION.md')])
put('ECO-001 ECO-003 ECO-004 ECO-006','core','Hand-derived payout/cap/tier fixtures, negative-edge/unaffordable/reserve denials and exactly-priced startups execute the production one-wallet economy. No combat/time meter or paid-use inventory is introduced.')
put('ECO-002','core show','Exact300-credit EX and selected900-credit super starts visibly debit once. Core interruption/contact/whiff/parry and two-startup cancel regressions verify no refunds.')
put('ECO-005','core app','Snapshot and actual late-input rollback tests restore wallets/floors/receipts with world state, retract obsolete paid/terminal branches, and retain one accepted debit.')
put('SHOP-001','core ui','Atomic-plan, leased-slot/replacement and selected-super production tests validate eligibility/prices; native two-seat draft editing changes only its own draft before locking.')
put('SHOP-002','core ui match native-net','Native local and two-process private matches exercise separate draft locks, simultaneous commit/reveal, full reveal/countdown and next-round lease expiry with persistent credits.')
put('MATCH-001','core app ui match','Full exported six-round economic match reaches MatchOver and replay agreement. Controller result/rematch callback evidence is tracked separately from that full-play proof.')
put('MATCH-002 MATCH-003','core app native-net','Production tests cover half-point first-to-five/cap9 draws, normalized terminal resolution, confirmed/idempotent settlement and ordered round transitions; both native peers finish nine rounds with identical result and replay hashes.')
put('MATCH-004','core app ui','Native controller pause holds Tick stable and exit/replay transitions run through actual callbacks. Actual UDP preparation/startup disconnect and16-second pause/resume tests retain economic invariants. No reset grants speculative income.')
put('CONTENT-001 CONTENT-002','core actions','Current exported Core produced49 action traces per fighter with distinct observed declared effects and all98 command recognitions. The packaged verifier independently replayed98/98 final traces on exact ExportRelease Core.')
put('CONTENT-003','core','All12 lease effects and six selected super choices are exercised by production all-action/lease availability tests with direct scalar-credit startup prices.')
put('ART-001','show ui','Final exported showcase/UI footage and inspected PNGs show both original animated fighters, foundry/grid presentations, actual hit/block/parry/throw/super states and readable colors.',[a('screenshot',SHOW+'combat-04-parry.png'),a('screenshot',UI+'ui-06-training-grid-and-boxes.png'),a('process_log',E+'presentation-review.json')])
put('AUDIO-001','show ui','Real exported MP4s include AAC stereo action/ambient output; GUI test adjusts actual audio slider. The asset register records original generated/procedural sources and licenses.',[a('process_log',E+'presentation-review.json'),a('process_log','release_docs/ASSET_NOTICES.md'),a('process_log',E+'audio-assets.log')])
put('BOT-001','app match balance','Seeded delayed-observation bots drive actual normalized input through the core for full local/native and252 balance matches. Counterbalanced fixture initialization occurs before play and is explicitly recorded.')
put('TRAIN-001 TRAIN-002','app drills ui show','All16 real-core drill evaluators have actual input success and neutral-timeout failures, recorded fixtures and resets. Native controller footage exercises drill selection, frame advance, collision overlays, checkpoint and dummy record/playback. Training resource controls remain isolated.')
put('REPLAY-001','app replay cross','Full match records include normalized inputs, preparation, startup receipts, settlements and next-round commands. Seeking and complete playback reproduce canonical world/wallet hashes; Windows/Linux replay bytes also match.')
put('REPLAY-002','app cross','Replay seek/reset and bounded malformed/version/content/assist rejection are actually asserted. Cross-platform mismatch rejection separately proves incompatible input is not accepted.',[a('test_log',E+'cross-platform-replay/mismatch-rejection.json')])
put('REPLAY-003','replay economy ui','Confirmed per-round economic JSON/CSV and wallet history are derived from recorded transaction/startup data and agree with replay. Actual archive GUI opens replay controls and wallet-history display.')
put('NET-001','socket native-net','Both CLI and two actual exported game processes use UDP sockets with exactly two seats and matching protocol/build/content/configuration. These are loopback processes on one physical PC.')
put('NET-002','app socket','Four declared profiles pass with0/50/100/150ms RTT,0/20ms jitter,0/1/3%loss and injected duplicates/reordering. Actual rollback/stall/receipt and canonical hash metrics are retained; this is a bounded matrix, not all combinations.')
put('NET-003','core app','Real EX and paid-super input/rollback tests restore one-wallet startup receipts and reserve floor together. Full two-peer logs and confirmed replays retain accepted startup costs across resimulation.')
put('NET-004','app show','Actual App rollback retracts a predicted paid-super KO on late parry without settlement. Exported showcase segment11 visibly removes speculative KO after restoring the corrected branch with one debit/no score; the showcase is explicitly a deterministic training correction, supplemented by separate real socket transport tests.')
put('NET-005','app socket','Real UDP forged-seat/incompatible/malformed/oversized controls reject; commit/reveal plans validate atomically, with immutable authoritative inputs and no network wallet-setting command.')
put('NET-006','app socket','Actual UDP tests cover disconnect in preparation and paid startup, bounded keepalive/pause/resume and malformed peer states. Final process logs retain diagnostics and unchanged incomplete-round economy; no router/firewall/public service was changed.')
put('NET-007','native-net socket','Two actual rendered native processes complete nine rounds score5–4 and wallets1200/900 with identical confirmed final/replay hash1c5375ff722c8cae222adeb184a99034b1b76d0c90787a93bef72fda3802fd15; full peer logs/replays/receipts and videos are retained.')
put('UI-001','ui match','Final exported UI footage exercises controller-only setup, independent preparation, settings/remapping, replay, training, help and private text entry through actual callbacks. Results/rematch coverage is separately tracked; software-injected pads are not claimed as physical devices.')
put('UI-002','ui show match','Inspected exported HUD and preparation screenshots expose a single wallet, selected costs/reserve/leases and affordability/rejection. Actual paid-startup showcase ties visible debit to one receipt.',[a('screenshot',UI+'ui-02-independent-preparation.png'),a('screenshot',SHOW+'combat-03-projectilespawn.png')])
put('UI-003','ui core','Independent per-device button capture and conflict swapping run through actual GUI events. Explicit SOCD/chord/unplug/held-confirm neutralization evidence is separately tracked, with physical pads remaining a distinct device gate.')
put('UI-004','ui','The native UI toggles actual flash/audio preferences, but intentionally disables persistence of user settings. An isolated settings save/load test is required before claiming persistence.')
put('UI-005','app ui','Training flags/dummy/resource controls are visibly isolated; replay and network reject Training/Assist configurations including mismatched embedded assist flags. Default competitive configuration cannot import assisted state.')
put('BAL-001 BAL-003','balance','Final252 matched experiments share nine frozen contexts across seven policies, reversed budgets and mirrored seats.1620rounds/3387240ticks,126/126mirror agreement, zero exclusions. Notes interpret cap/tier2/final-round/farming outcomes and correlated Wilson summaries without a human-balance claim.')
put('BAL-002','core balance','Actual zero-credit/funded/corner/lease policies plus core chip/parry/cancel bounds probe paid pressure.84 zero-credit player-rounds retain2268freeactions838hits220blocks38parries with zero paid starts/spend; limits are documented.')
put('BAL-004','core balance','ADR0003 records original empty versus contacted[8,13) EX super-cancel arrays for two moves, rationale and no price changes. Current final content hash and regression reruns bind the retained change; no speculative tuning was added.',[a('process_log','decisions/0003-contacted-ex-super-cancel.md')])
put('QA-001','core app drills balance ui show match native-net','Actual15 seed checks,36 Core suites,10 App suites,16 drill success/timeout paths,252 controlled matches, four UDP profiles and native GUI/full-match runs cover the29 registered scenario families. Dispatcher checks verify exact registry and construction branches, with actual unsupported-ID exit2. Registry dry runs are not represented as29 new gameplay subprocess executions.',[a('test_log',E+'root-test-console.log'),a('test_log',E+'dispatcher-final-check.json'),a('process_log',E+'final-independent-review.json')])
put('QA-002','core app balance socket','Seeded snapshot/symmetry/round reset bounds plus252 full matches and repeated complete UDP matches record no underflow, duplicated payout, leaked projectile or unbounded stun/juggle loop in these runs. No unexecuted soak duration is claimed.')
put('QA-003','core app cross','Actual bounded malformed replay/UDP, incompatible identity, snapshot and strict content checks reject invalid imports. No executable imported payload exists.')
put('QA-004','feel','Core profiles include p50/p95/p99, allocations, snapshots and8/240tick resimulation. Actual final Windows NVIDIA1050Ti/i7-7700HQ native full-match profiling completed12789ticks without movie capture: frame p50=16.7013ms,p95=21.0911ms,p99=168.2692ms,worst=492.3552ms. The isolated first minute showed59-61FPS, with substantial tail stalls during concurrent UI recording. This establishes measurement and known limits, not perfectly steady60FPS or physical input latency.',[a('metrics',CORE),a('metrics',E+'native-pacing-final/runtime-result.json'),a('metrics',E+'native-pacing-final/scope-profile.json'),a('process_log',E+'native-pacing-final/process-result.json'),a('process_log',E+'native-pacing-final/process.log'),a('metrics',E+'replay-step-profile/result.json')])
put('QA-005','match native-net','The actual final-candidate Windows full-match movie completes six rounds with exact runtime identity and matching replay; final UI/showcase films separately exercise the changed renderer. Prior two-peer/free-kit/Linux films retain explicit pre-polish presentation identity with unchanged exact App/Core/content, compatibility inventory and independent semantic review.',[a('process_log',E+'match-final-media-review.json'),a('process_log','reports/app-exportrelease-final/shipped-assembly-binding.json')])
put('BUILD-001','build','Recorded root restore/build/import and final Windows/Linux self-contained exports include engine/runtime dependencies; candidate manifest hashes actual exported native payloads.')
put('BUILD-002','package','Both actual fresh ZIP extractions pass CRC and208 file checksums and include player controls/rules/help/notices/known limits plus the exact candidate manifest. Native Windows/Linux executables from those extractions each complete12789tick matches with exit0 and byte-identical replay. All188 Windows/187 Linux candidate native files match. Later documentation-only repacks must preserve these binary hashes.',[a('process_log',E+'final-packaging.log')])
put('DEVICE-001','match','Actual Windows NVIDIA native package launches and finishes a six-round local economic match; full recording, process exit0, actual GPU metadata and replay are retained.')
put('DEVICE-002','linux','Actual Linux runtime already passes headless deterministic comparison. Graphical native full-match recording/media verification remains pending until the running capture finishes.')
put('DEVICE-003','ui','Software-injected controller GUI routing is tested. No two actual physical controllers or physical keyboard-plus-pad session has been observed.')
put('DEVICE-004','native-net','Two actual processes on one PC are demonstrated. No two-physical-machine LAN match has been observed.')
put('DEVICE-005','cross','Actual Windows and Linux native OS runtime runs produce byte-identical completed12789tick replay, wallets/debits/checkpoints/transactions and canonical result. These runs are headless and are not a graphical-device claim.')
put('FINAL-001','review','Independent substantive audit and unchanged software evidence gate are required. The ledger is not its own proof of game completion.')
put('FINAL-002','','The final documentation audit checks source/build/launch instructions, recorded limits, exact remaining external requirements, current verification counts and resume state. Hashes in that audit cover the actual final document bytes.',[a('process_log',E+'final-documentation-audit.json')])
put('HUMAN-001','','No owner/player review of prototype feel or wallet decisions has been supplied; agent/script observations are not substituted for human feedback.')

for rid in ('INPUT-001','UI-003','UI-004'):
    M[rid]['bundles'].append('input')
M['INPUT-001']['note']='Actual production-linked44-check harness covers all16 SOCD combinations, invalid/conflicting per-device bindings, disconnected assignment eligibility and held-confirm quarantine. Native75-check GUI trace separately exercises device-specific remapping/callbacks; core98-command tests cover six-button input semantics. Physical pads are a separate gate.'
M['UI-003']['note']='Production-linked harness and native GUI trace jointly test SOCD, remapping/conflict swaps, disconnected device eligibility and held-confirm quarantine/release. Runtime source guards absent devices and pauses on device-loss callback; physical unplug hardware has not been claimed.'
M['UI-004']['bundles'].append('settings-native')
M['UI-004']['note']='Actual headless Godot application linked unchanged production GameSettings.cs runs14 assertions on real Save()/Load(), OS roaming user-data resolution, every settings family, atomic replacement and cleanup. A distinct custom application directory isolates the player profile. The44-check pure/input harness and native GUI preferences provide complementary coverage.'
M['MATCH-001']['note']='Complete six-round native economic match reaches MatchOver with replay agreement. The final75-check exported controller UI replays a complete legal competitive match with all hash/wallet checks, opens actual results, and invokes Run it back to verify fresh preparation, both600CR, no points/leases and round1.'
M['UI-001']['note']='Final exported75-check controller-only GUI trace exercises setup, independent preparation, settings/remapping, replay, training, help/private text entry, verified completed results and actual controller rematch callback. It resets wallets/points/leases/round correctly; software devices are explicitly distinguished from physical pads.'
M['DEVICE-002']['note']='Actual native Linux package completes the same six-round economic match under this PC WSLg X11/llvmpipe with validated video and clean exit0. Root temporarily suspended the process during Windows profiling, so3128s wall/callback metrics are not normal-play performance. This is not a second physical machine claim.'
M['FEEL-001']['extra'].extend([a('process_log',E+'free-kit-media-review.json'),a('metrics',FREE+'media-validation.json')])
M['DEVICE-002']['extra'].append(a('metrics',E+'native-linux/media-validation.json'))

PENDING={
'DEVICE-003':'No two real controllers or real keyboard-plus-controller exercise.',
'DEVICE-004':'No two physical machines exercised; same-PC native socket evidence is not a substitute.',
'FINAL-001':'Independent audit and final unchanged software gate have not yet completed against this ledger.',
'FINAL-002':'Integrating task is updating exact final docs/state/artifact instructions.',
'HUMAN-001':'No actual owner/player feedback or reviewer supplied.'}

def main():
    ap=argparse.ArgumentParser();ap.add_argument('--gate',action='store_true');ap.add_argument('--finalize',action='store_true',help='Publish final software status only after an actual passing gate against the proposed ledger');args=ap.parse_args()
    candidate=read('reports/RELEASE_CANDIDATE.json');requirements=read('acceptance/requirements.json')['requirements']
    assert set(M)=={r['id'] for r in requirements},'Every requirement needs explicit reviewed mapping'
    assert candidate['status']=='READY_FOR_VERIFICATION'
    assert all(sha(p['path'])==p['sha256'] for p in candidate['source_inputs']),'Candidate production source changed'
    data=[{'path':p.relative_to(ROOT/'data').as_posix(),'sha256':sha(p.relative_to(ROOT).as_posix())} for p in sorted((ROOT/'data').rglob('*.json'))]
    manifest_hash=hashlib.sha256(json.dumps(data,separators=(',',':'),sort_keys=True).encode()).hexdigest()
    assert manifest_hash==candidate['content_sha256'],'Candidate content manifest changed'
    core=read(CORE);app=read(APP);balance=read(BAL+'summary.json');matrix=read('reports/network-lab-exportrelease/matrix-result.json')
    assert core['passed']==36 and core['failed']==0 and all(r['pass'] for r in core['results'])
    assert app['exitCode']==0 and len(app['results'])==10 and all(r['passed'] for r in app['results'])
    assert balance['samples']==252 and balance['excluded']==0 and balance['exitCode']==0 and balance['build']==app['build']
    assert len(matrix['results'])==4 and all(r['passed'] and r['exitCodes']==[0,0] for r in matrix['results'])
    assert read(UI+'controller-menu-flow.json')['success'] and read(SHOW+'combat-visual-showcase.json')['success']
    assert len(read(UI+'controller-menu-flow.json')['checks'])==75 and read(UI+'media-validation.json')['passed']
    settings=read(E+'settings-native-harness/result.json')
    assert settings['passed'] and len(settings['checks'])==14 and settings['cleanedOwnedSettingsFile'] and settings['sourceSha256']==sha('game/GameSettings.cs')
    assert read(E+'settings-native-harness/process-result.json')['passed']
    compatibility=read(E+'candidate-compatibility/result.json')
    assert compatibility['passed'] and compatibility['current_build_ref']==candidate['build_ref'] and compatibility['content_sha256']==candidate['content_sha256']
    for module,digest in compatibility['gameplay_assemblies_identical'].items():
        assert sha('dist/StrikeLedger/windows/data_StrikeLedger_windows_x86_64/'+module)==digest
        assert sha('dist/StrikeLedger/linux/data_StrikeLedger_linuxbsd_x86_64/'+module)==digest
    assert read(E+'native-linux/process-result.json')['passed'] and read(E+'native-linux/runtime-result.json')['complete'] and read(E+'native-linux/media-validation.json')['passed']
    assert read(MATCH+'runtime-result.json')['complete'] and read(MATCH+'process-result.json')['passed'] and read(MATCH+'media-validation.json')['passed']
    peer0=read(NET+'peer0/native-network-result.json');peer1=read(NET+'peer1/native-network-result.json')
    assert peer0['passed'] and peer1['passed'] and peer0['finalHash']==peer1['finalHash']==peer0['replayHash']==peer1['replayHash']
    assert read('reports/app-exportrelease-final/shipped-assembly-binding.json')['allAppIdentical']
    assert read(SHOW+'media-validation.json')['passed'] and read(SHOW+'process-result.json')['passed']
    dispatch=read(E+'dispatcher-final-check.json')
    assert dispatch['passed'] and dispatch['registryCount']==29 and dispatch['unknownScenarioExitCode']==2
    assert read(E+'native-pacing-final/runtime-result.json')['complete'] and read(E+'native-pacing-final/process-result.json')['passed']
    package=read(E+'package-verification/result.json');binding=read(E+'package-verification/final-candidate-binding.json')
    assert package['passed'] and len(package['results'])==2 and all(r['passed'] and r['exit_code']==0 and r['runtime']['complete'] for r in package['results'])
    assert binding['passed'] and binding['sourceCandidate']==candidate['build_ref'] and binding['fullReplayBytesIdentical']
    if args.finalize:
        review=read(E+'final-independent-review.json');docs=read(E+'final-documentation-audit.json')
        assert review['review_completed'] and review['checksFailed']==0 and review['sourceCandidateAtReview']['build_ref']==candidate['build_ref']
        assert docs['passed'] and docs['build_ref']==candidate['build_ref'] and all(sha(p)==digest for p,digest in docs['document_sha256'].items())
        PENDING.pop('FINAL-001',None);PENDING.pop('FINAL-002',None)
        M['FINAL-001']['note']='Independent separate-agent substantive review passed32 checks on this exact source candidate. Final software status is published only after the unmodified gate actually passes the proposed complete ledger; the actual gate output and process exit are retained.'
    for entry in M.values():entry['extra'].extend([a('process_log',E+'candidate-compatibility/result.json'),a('process_log',E+'final-source-semantics.json')])
    used={path for entry in M.values() for kind,path in sum([B[b] for b in entry['bundles']],[])+entry['extra'] if exists(path)}
    hash_cache={p:sha(p) for p in used}
    now=datetime.datetime.now(datetime.timezone.utc).isoformat()
    audit={'schema_version':1,'scope':'Actual read-only audit of final existing executable evidence; not new gameplay or hardware testing','utc':now,'command':'python reports/build_acceptance_ledger.py'+(' --finalize' if args.finalize else '')+(' --gate' if args.gate else ''),'exit_code':0,'build_ref':candidate['build_ref'],'content_sha256':manifest_hash,'core_canonical_content_hash':core['content_hash'],'identity_note':'The release gate hashes the canonical file-hash manifest; Core hashes canonical relative paths and bytes. These are distinct algorithms over the same verified13 JSON files. They are not silently relabeled.','app_build':app['build'],'checks':{'candidate_source_matches':True,'candidate_data_manifest_matches':True,'core36_passed':True,'app10_passed':True,'balance252_no_exclusions':True,'actual_socket_four_profiles_passed':True,'native_two_peer_final_replay_agreement':True,'native_ui75_and_showcase13_passed':True,'fresh_native_packages_passed':True,'actual_normal_native_pacing_measured':True},'reviewed_artifacts':hash_cache,'open_requirements':PENDING}
    (ROOT/AUDIT).write_text(json.dumps(audit,indent=2)+'\n',encoding='utf-8');hash_cache[AUDIT]=sha(AUDIT)
    records=[]
    for req in requirements:
        rid=req['id'];entry=M[rid];artifacts=[];seen=set();missing=[]
        for kind,path in sum([B[b] for b in entry['bundles']],[])+entry['extra']:
            if path in seen:continue
            seen.add(path)
            if not exists(path):missing.append(path);continue
            artifacts.append({'kind':kind,'path':path,'sha256':hash_cache[path]})
        artifacts.append({'kind':'process_log','path':AUDIT,'sha256':hash_cache[AUDIT]})
        missing_kinds=sorted(set(req['required_evidence_kinds'])-{p['kind'] for p in artifacts})
        pending=PENDING.get(rid,'')
        if missing:pending+=' Missing final artifacts: '+', '.join(missing)+'.'
        if missing_kinds:pending+=' Unavailable required evidence kinds: '+', '.join(missing_kinds)+'.'
        records.append({'requirement_id':rid,'build_ref':candidate['build_ref'],'content_sha256':manifest_hash,'status':'NOT_RUN' if pending else 'PASS','platform':platform.platform()+'; actual Windows/Linux or loopback scope identified in artifacts','command':audit['command'],'exit_code':0,'timestamp_utc':now,'reviewer':'Codex session_network evidence audit' if req['gate_class']!='human' else '', 'notes':entry['note'],'pending_reason':pending,'artifacts':artifacts})
    ledger={'schema_version':1,'records':records}
    if args.finalize:
        proposed=ROOT/E/'acceptance-finalization-proposed.json'
        proposed.write_text(json.dumps(ledger,indent=2)+'\n',encoding='utf-8')
        gate=subprocess.run([sys.executable,str(ROOT/'tools/release_gate.py'),'--tier','software','--ledger',str(proposed)],cwd=ROOT,text=True,capture_output=True)
        gate_path=E+'acceptance-software-preflight.json';process_path=E+'acceptance-software-preflight-process.json'
        (ROOT/gate_path).write_text(gate.stdout,encoding='utf-8')
        (ROOT/process_path).write_text(json.dumps({'utc':now,'command':gate.args,'exit_code':gate.returncode,'stderr':gate.stderr},indent=2)+'\n',encoding='utf-8')
        assert gate.returncode==0,'Final software gate failed; authoritative ledger not promoted: '+gate.stdout+gate.stderr
        final=next(r for r in records if r['requirement_id']=='FINAL-001')
        for path in [gate_path,process_path]:final['artifacts'].append({'kind':'process_log','path':path,'sha256':sha(path)})
    (ROOT/'reports/ACCEPTANCE_RESULTS.json').write_text(json.dumps(ledger,indent=2)+'\n',encoding='utf-8')
    summary={'records':len(records),'pass':sum(r['status']=='PASS' for r in records),'pending':[{'id':r['requirement_id'],'reason':r['pending_reason']} for r in records if r['status']!='PASS']}
    print(json.dumps(summary,indent=2))
    if args.gate or args.finalize:
        result=subprocess.run([sys.executable,str(ROOT/'tools/release_gate.py'),'--tier','software'],cwd=ROOT,text=True,capture_output=True)
        (ROOT/E/'acceptance-software-gate.json').write_text(result.stdout,encoding='utf-8')
        (ROOT/E/'acceptance-software-gate-process.json').write_text(json.dumps({'utc':now,'command':result.args,'exit_code':result.returncode,'stderr':result.stderr},indent=2)+'\n',encoding='utf-8')
        print('SOFTWARE_GATE_EXIT',result.returncode);print(result.stdout)
        return result.returncode
    return 0

if __name__=='__main__':raise SystemExit(main())
