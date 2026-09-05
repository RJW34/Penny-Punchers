"""Propose, verify, and explicitly promote the AFTER HOURS evidence ledger.

Default execution writes only a separate proposal and audit. --promote is the
only operation that replaces the authoritative ledger, and requires successful
strict compatibility, actual current reviews, original inherited hashes, and the
unmodified software gate. No production, candidate, or requirement file is edited.
"""
from __future__ import annotations

import argparse
import copy
import datetime
import hashlib
import json
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
E = 'reports/evidence/'
BASE = E + 'after-hours-baseline/'
OUT = E + 'after-hours-rebind/'
COMPAT = E + 'after-hours-compatibility/result.json'
EXTERNAL = {'DEVICE-003', 'DEVICE-004', 'HUMAN-001'}

# Existing filenames inside these new native directories are intentional. The
# native drivers preserve diagnostic names; the directory records run identity.
PREFIX_MAP = {
    E+'native-ui-final/': E+'native-after-hours-ui/',
    E+'native-showcase-final/': E+'native-after-hours-showcase/',
    E+'native-match-final/': E+'native-after-hours-match/',
    E+'native-linux/': E+'native-after-hours-linux/',
    E+'native-pacing-final/': E+'native-after-hours-pacing/',
    E+'package-verification/': E+'package-after-hours-verification/',
}
REPLACED_AUDITS = {
    E+x for x in [
        'acceptance-ledger-audit.json', 'acceptance-software-preflight.json',
        'acceptance-software-preflight-process.json', 'candidate-compatibility/result.json',
        'final-source-semantics.json', 'final-independent-review.json',
        'final-independent-review.log', 'final-documentation-audit.json',
        'presentation-review.json', 'ui-final-media-review.json',
        'showcase-final-media-review.json', 'match-final-media-review.json',
    ]
} | {'reports/FINAL_INDEPENDENT_REVIEW.md'}
MUTABLE_DOCS = {'reports/FEEL_CALIBRATION.md', 'release_docs/ASSET_NOTICES.md'}

UI_ROWS = set('INPUT-001 SHOP-001 SHOP-002 MATCH-001 MATCH-004 TRAIN-001 TRAIN-002 REPLAY-003 UI-001 UI-002 UI-003 UI-004 UI-005 QA-001 ART-001 AUDIO-001'.split())
SHOW_ROWS = set('MOVE-001 MOVE-002 COMBAT-002 COMBAT-004 COMBAT-008 PARRY-003 ECO-002 ART-001 AUDIO-001 TRAIN-001 TRAIN-002 NET-004 UI-002 QA-001'.split())
MATCH_ROWS = set('CORE-005 SHOP-002 MATCH-001 BOT-001 UI-001 QA-001 QA-005 DEVICE-001'.split())


def read(path):
    return json.loads((ROOT/path).read_text(encoding='utf-8-sig'))


def write(path, obj):
    target = ROOT/path
    target.parent.mkdir(parents=True, exist_ok=True)
    target.write_text(json.dumps(obj, indent=2)+'\n', encoding='utf-8')


def sha(path):
    result = hashlib.sha256()
    with (ROOT/path).open('rb') as stream:
        for part in iter(lambda: stream.read(1024*1024), b''):
            result.update(part)
    return result.hexdigest()


def local(path):
    resolved = (ROOT/path).resolve()
    if not resolved.is_relative_to(ROOT) or not resolved.is_file() or not resolved.stat().st_size:
        raise ValueError('Missing/empty/outside-project evidence: '+str(path))
    return resolved.relative_to(ROOT).as_posix()


def validate_review(path, candidate, *, documents=False, independent=False):
    """Accept actual review formats, never generate a successful review itself."""
    path = local(path)
    review = read(path)
    binding = review.get('build_ref', review.get('current_build_ref', review.get('candidate')))
    if binding is None:
        binding = review.get('sourceCandidateAtReview', {}).get('build_ref')
    if binding is None:
        binding = review.get('candidate_binding', {}).get('bound_build_ref')
    if binding != candidate['build_ref']:
        raise ValueError('Review belongs to another candidate: '+path)
    passed = review.get('passed') is True or (
        review.get('review_completed') is True and review.get('checksFailed') == 0)
    checks = review.get('checks', [])
    if not passed or (checks and not all(c.get('passed') is True for c in checks)):
        raise ValueError('Review is incomplete or failed: '+path)
    if independent and (not review.get('reviewer') or not checks):
        raise ValueError('Independent review must identify the reviewer and actual checks: '+path)
    if not documents and not independent and not (
        review.get('visually_inspected') or review.get('visual_reviews') or review.get('visual_finding') or
        (review.get('images') and review.get('observations') and all(o.get('passed') is True for o in review['observations']))):
        raise ValueError('Visual review must record actual inspected frames/findings: '+path)
    digests = review.get('document_sha256' if documents else 'reviewed_artifacts', {})
    if not documents and not independent and not digests:
        digests = review.get('artifact_sha256', {})
    if independent and not digests:
        digests = review.get('reviewed_files', {})
    if documents and not digests:
        raise ValueError('Documentation review must hash the actual final documents: '+path)
    for artifact, digest in digests.items():
        if sha(local(artifact)) != digest:
            raise ValueError('Review artifact changed after inspection: '+artifact)
    return path


def run_gate(ledger, suffix, tier='software'):
    command = [sys.executable, str(ROOT/'tools/release_gate.py'), '--tier', tier, '--ledger', str(ROOT/ledger)]
    run = subprocess.run(command, cwd=ROOT, text=True, capture_output=True)
    result_path = OUT+suffix+'.json'
    process_path = OUT+suffix+'-process.json'
    try:
        result = json.loads(run.stdout)
    except ValueError:
        result = {'status': 'ERROR', 'stdout': run.stdout}
    write(result_path, result)
    write(process_path, {'utc': datetime.datetime.now(datetime.timezone.utc).isoformat(),
                         'command': command, 'exit_code': run.returncode, 'stderr': run.stderr})
    return run.returncode, result, result_path, process_path


def main():
    global OUT, COMPAT
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--plan', action='store_true', help='Print the proposed mapping only; write nothing and run no acceptance command.')
    parser.add_argument('--promote', action='store_true', help='Replace ACCEPTANCE_RESULTS.json only after all current evidence and the software gate pass.')
    parser.add_argument('--visual-review', action='append', default=[], help='Current-candidate JSON recording inspected frames and visual findings. Repeat for separate reviews.')
    parser.add_argument('--independent-review', default=E+'after-hours-independent-review.json')
    parser.add_argument('--documentation-review', default=E+'after-hours-documentation-audit.json')
    args = parser.parse_args()
    if args.plan and args.promote:
        parser.error('--plan cannot promote')
    baseline = read(BASE+'ACCEPTANCE_RESULTS.json')
    requirements = read('acceptance/requirements.json')['requirements']
    ids = {r['id'] for r in requirements}
    if len(baseline['records']) != 82 or {r['requirement_id'] for r in baseline['records']} != ids:
        raise ValueError('Preserved baseline/requirements do not contain the exact 82 records')
    if args.plan:
        print(json.dumps({'authoritative_ledger_mutated': False, 'records': len(ids),
                          'prefix_replacements': PREFIX_MAP,
                          'new_review_paths': {'visual': args.visual_review or [E+'after-hours-visual-review.json'],
                                               'independent': args.independent_review,
                                               'documentation': args.documentation_review},
                          'external_requirements_remain_not_run': sorted(EXTERNAL),
                          'sequence': ['strict actual compatibility', 'current visual/independent/document reviews',
                                       'original inherited artifact SHA checks', 'separate proposed ledger',
                                       'unmodified software gate', 'optional explicit promotion', 'all-tier status report']}, indent=2))
        return 0

    # Each attempt owns an immutable evidence directory. A failed later attempt
    # must not rewrite reports already hashed by an accepted earlier ledger.
    run_name = datetime.datetime.now(datetime.timezone.utc).strftime('run-%Y%m%dT%H%M%S-%fZ')
    OUT += run_name+'/'
    COMPAT = OUT+'compatibility/result.json'
    initial_ledger_hash = sha('reports/ACCEPTANCE_RESULTS.json')
    preservation = read(BASE+'preservation.json')
    for item in preservation['files']:
        if sha(item['preserved']) != item['sha256']:
            raise ValueError('Preserved baseline bytes changed: '+item['preserved'])
    # Rerun instead of trusting a stale PASS on an earlier candidate or artifact.
    command = [sys.executable, str(ROOT/'tools/verify_after_hours.py'), '--output', str(ROOT/(OUT+'compatibility'))]
    compatibility = subprocess.run(command, cwd=ROOT, text=True, capture_output=True)
    write(OUT+'compatibility-process.json', {'utc': datetime.datetime.now(datetime.timezone.utc).isoformat(),
          'command': command, 'exit_code': compatibility.returncode,
          'stdout': compatibility.stdout, 'stderr': compatibility.stderr})
    if compatibility.returncode:
        raise ValueError('Strict actual compatibility is not complete; no ledger proposed. '+compatibility.stdout)
    candidate = read('reports/RELEASE_CANDIDATE.json')
    compat = read(COMPAT)
    if not compat['passed'] or compat['current_build_ref'] != candidate['build_ref'] or not compat['new_native_evidence']:
        raise ValueError('A full current native compatibility report is required')
    visuals = [validate_review(p, candidate) for p in args.visual_review or [E+'after-hours-visual-review.json']]
    independent = validate_review(args.independent_review, candidate, independent=True)
    documents = validate_review(args.documentation_review, candidate, documents=True)
    now = datetime.datetime.now(datetime.timezone.utc).isoformat()
    hashes = {}

    def artifact(kind, path, original_digest=None):
        path = local(path)
        if path not in hashes:
            hashes[path] = sha(path)
        if original_digest and hashes[path] != original_digest:
            raise ValueError('Inherited artifact changed; refusing silent rehash: '+path)
        return {'kind': kind, 'path': path, 'sha256': hashes[path]}

    def bundle(folder, result, kind='test_log', video=True):
        paths = [(kind, E+folder+'/'+result), ('process_log', E+folder+'/process-result.json')]
        if video:
            paths += [('video', E+folder+'/recording.mp4'), ('metrics', E+folder+'/media-validation.json')]
        return paths

    bundles = {
        'ui': bundle('native-after-hours-ui', 'controller-menu-flow.json'),
        'show': bundle('native-after-hours-showcase', 'combat-visual-showcase.json'),
        'match': bundle('native-after-hours-match', 'runtime-result.json'),
        'linux': bundle('native-after-hours-linux', 'runtime-result.json'),
        'art': bundle('native-after-hours-art', 'art-review.json', video=False),
        'pacing': bundle('native-after-hours-pacing', 'runtime-result.json', 'metrics', video=False),
        'package': [('process_log', E+'package-after-hours-verification/result.json')],
    }
    replacements = []
    provenance = []
    records = []
    pacing = read(E+'native-after-hours-pacing/runtime-result.json')
    for old in baseline['records']:
        rid = old['requirement_id']
        record = copy.deepcopy(old)
        record.update(build_ref=candidate['build_ref'], content_sha256=candidate['content_sha256'])
        # The current timestamp belongs to this evidence audit. The original
        # execution/audit metadata is retained verbatim in the linked provenance.
        provenance.append({key: copy.deepcopy(old.get(key)) for key in [
            'requirement_id', 'build_ref', 'content_sha256', 'timestamp_utc',
            'platform', 'command', 'exit_code', 'notes', 'artifacts']})
        record['timestamp_utc'] = now
        record['command'] = 'python tools/rebind_after_hours_ledger.py'+(' --promote' if args.promote else '')
        record['exit_code'] = 0
        record['reviewer'] = '' if rid == 'HUMAN-001' else 'Codex AFTER HOURS evidence binding audit'
        record['artifacts'] = []
        inherited = []
        for original in old['artifacts']:
            path = original['path']
            if path in REPLACED_AUDITS or path in MUTABLE_DOCS:
                continue
            # These reports are superseded by actual new fresh-package evidence;
            # a final archive-only repack can add its own separate integrity log.
            if path == E+'final-packaging.log' or path.startswith(E+'package-verification/'):
                continue
            if path.startswith(E+'native-pacing-final/'):
                continue
            mapped = path
            for previous, current in PREFIX_MAP.items():
                if path.startswith(previous):
                    mapped = current+path[len(previous):]
                    break
            if path == E+'final-export-console.log':
                mapped = E+'after-hours-build-console.log'
            if mapped != path or path.startswith('dist/StrikeLedger/'):
                record['artifacts'].append(artifact(original['kind'], mapped))
                replacements.append({'requirement_id': rid, 'old': path, 'current': mapped})
            else:
                record['artifacts'].append(artifact(original['kind'], path, original['sha256']))
                inherited.append(path)

        added = []
        if rid in UI_ROWS: added += bundles['ui']
        if rid in SHOW_ROWS: added += bundles['show']
        if rid in MATCH_ROWS: added += bundles['match']
        if rid in MATCH_ROWS or rid in {'DEVICE-005', 'REPLAY-001'}:
            added += [('replay', E+'native-after-hours-match/full-match.replay.json')]
        if rid in {'DEVICE-002', 'DEVICE-005'}:
            added += [('replay', E+'native-after-hours-linux/full-match.replay.json')]
        if rid == 'ART-001':
            added += bundles['art']
            art = read(E+'native-after-hours-art/art-review.json')
            added += [('screenshot', E+'native-after-hours-art/'+p) for p in art['captures']]
        if rid == 'DEVICE-002': added += bundles['linux']
        if rid == 'QA-004': added += bundles['pacing']
        if rid == 'BUILD-002': added += bundles['package']
        if rid in {'ENV-001', 'ENV-002', 'BUILD-001', 'QA-001'}:
            added += [('build_log' if rid in {'ENV-001', 'BUILD-001'} else 'test_log', E+'after-hours-build-console.log')]
        if rid in {'ART-001', 'AUDIO-001', 'UI-001', 'UI-002', 'QA-005', 'FINAL-001'}:
            added += [('process_log', p) for p in visuals]
        if rid == 'FINAL-001': added += [('process_log', independent)]
        if rid in {'FINAL-002', 'BUILD-002', 'AUDIO-001'}:
            added += [('process_log', documents)]
        if rid == 'CORE-005':
            added += [('screenshot', E+'native-after-hours-match/match-result.png')]
        added += [('process_log', COMPAT), ('process_log', BASE+'ACCEPTANCE_RESULTS.json')]
        record['artifacts'] += [artifact(kind, path) for kind, path in added]
        record['artifacts'] = list({a['path']: a for a in record['artifacts']}.values())
        record['notes'] = (
            'AFTER HOURS current-candidate binding audit. Exact unchanged Core/App binaries, sources and canonical data '
            'permit the specifically retained baseline behavior observations; their original commands, dates and artifact hashes '
            'remain in the linked baseline ledger and binding audit. Baseline observation: '+old['notes'])
        if any(p.startswith((E+'native-free-kit/', E+'native-network/')) for p in inherited):
            record['notes'] += ' The retained free-kit/private-peer films show the prior artwork; they are historical observations of byte-identical gameplay, not new-art recordings.'
        current_notes = {
            'ART-001': 'Actual new native art fixtures inspect every mapped nonempty move phase and all atlas state aliases; new UI/showcase/match recordings and separate visual findings establish the new sprite, stage, effect, HUD and mirror presentation. Phase-composed source key poses do not imply unique drawn in-betweens.',
            'AUDIO-001': 'Current exported UI/showcase recordings exercise the unchanged action/ambient audio and volume controls alongside the new sprites/interface. The current documentation audit hashes the expanded image-generation provenance and asset notices.',
            'UI-001': 'The new exported shell passes all 75 software-injected controller-event GUI checks, including legal completed replay/result/rematch, and has a decoded native movie. This does not certify physical controllers.',
            'MATCH-001': 'The new Windows native economic match completes 12789 ticks and six rounds with canonical hash c4d779b7d9ef1fbfa9b378e4b18fe422169b4b79b1b89d441492cf01e169c51d. The new 75-check UI run exercises actual result/rematch callbacks with clean reset wallets and scores.',
            'QA-004': 'The AFTER HOURS renderer was actually profiled through a complete native match without movie capture. Recorded host, frame percentiles and limitations are in the new pacing report. Frame milliseconds: '+json.dumps(pacing.get('frameMs', {}), sort_keys=True)+'. Prior renderer measurements are historical; encoding FPS is not normal-play pacing.',
            'QA-005': 'New candidate-specific Windows/Linux full-match, UI and legal-input showcase movies and art PNGs are bound to actual current shell/PCK/Core/App hashes. Old free-kit/private-peer recordings retain their baseline identities and prove only explicitly unchanged gameplay.',
            'BUILD-001': 'The recorded final AFTER HOURS command exported both native platforms; it was an export-only invocation. An earlier graphics --export --test invocation actually ran 15 contract checks, 36 Core suites and 10 App suites. Those gameplay tests are retained through exact unchanged Core/App binaries, sources and canonical content. The recorded Git informational-version property preserves the prior gameplay DLL identity; current exported binaries and fingerprinted runtime art match the final candidate inventory.',
            'BUILD-002': 'Fresh Windows/Linux ZIP extractions pass recorded CRC/checksums and actual native complete matches. Extracted runtime binaries match the current candidate; current player documentation/notices are separately hashed by the final documentation audit. A later docs-only repack must retain those exact binary hashes.',
            'DEVICE-001': 'The current AFTER HOURS Windows NVIDIA native executable actually completes the six-round economic match with clean process exit, decoded movie, full runtime binding and canonical replay agreement.',
            'DEVICE-002': 'The current AFTER HOURS Linux native executable actually completes the same six-round economic match with a decoded graphical movie and current runtime binding. This PC uses WSLg/llvmpipe; capture timing is not ordinary gameplay FPS and this is not a second physical machine.',
            'FINAL-001': 'An actual independent current-candidate substantive review and separate visual review accompany exact gameplay inheritance and new native evidence. The unmodified software gate must pass the complete separate proposal before this record can be promoted; its actual output/process are appended afterward.',
            'FINAL-002': 'The current documentation audit verifies actual source/build/launch instructions, new-art provenance, measured limitations, exact three remaining external checks and current resume/status documents, with hashes of the reviewed document bytes.',
        }
        if rid in current_notes:
            record['notes'] = current_notes[rid]
        if rid in EXTERNAL:
            if old['status'] != 'NOT_RUN':
                raise ValueError('External baseline status unexpectedly changed: '+rid)
            record['status'] = 'NOT_RUN'
            record['notes'] = old['notes']
            record['pending_reason'] = old['pending_reason']
        records.append(record)

    audit_path = OUT+'binding-audit.json'
    write(audit_path, {'schema_version': 1, 'utc': now, 'command': [sys.executable]+sys.argv,
          'exit_code': 0, 'passed': True, 'scope': 'Evidence binding audit, not new gameplay or physical/human testing.',
          'build_ref': candidate['build_ref'], 'content_sha256': candidate['content_sha256'],
          'baseline_record_provenance': provenance, 'replaced_artifacts': replacements,
          'reviewed_artifacts': hashes, 'external_requirements': sorted(EXTERNAL)})
    for record in records:
        record['artifacts'].append(artifact('process_log', audit_path))
    proposal = OUT+'proposed-ledger.json'
    write(proposal, {'schema_version': 1, 'records': records})
    code, gate, gate_path, process_path = run_gate(proposal, 'software-preflight')
    if code:
        raise ValueError('Proposed ledger failed the unmodified software gate: '+json.dumps(gate))
    final = next(r for r in records if r['requirement_id'] == 'FINAL-001')
    final['artifacts'] += [artifact('process_log', gate_path), artifact('process_log', process_path)]
    write(proposal, {'schema_version': 1, 'records': records})
    code, gate, _, _ = run_gate(proposal, 'software-gate')
    if code:
        raise ValueError('Final proposed gate failed: '+json.dumps(gate))
    if sha('reports/ACCEPTANCE_RESULTS.json') != initial_ledger_hash:
        raise ValueError('Authoritative ledger changed concurrently; refusing overwrite')
    if args.promote:
        # Replace only the named ledger after the complete evidence audit passes.
        (ROOT/'reports/ACCEPTANCE_RESULTS.json').write_bytes((ROOT/proposal).read_bytes())
    _, all_gate, _, _ = run_gate(proposal, 'all-tier-gate', 'all')
    summary = {'passed': True, 'promoted': args.promote, 'proposal': proposal,
               'build_ref': candidate['build_ref'], 'software': gate,
               'all_tier_status': all_gate['status'], 'external_requirements': sorted(EXTERNAL)}
    write(OUT+'result.json', summary)
    print(json.dumps(summary, indent=2))
    return 0


if __name__ == '__main__':
    try:
        raise SystemExit(main())
    except (OSError, ValueError, KeyError) as error:
        print('AFTER HOURS ledger update stopped without promotion: '+str(error), file=sys.stderr)
        raise SystemExit(1)
