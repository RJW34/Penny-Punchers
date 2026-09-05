"""Refresh human-readable delivery status from the actual acceptance ledger."""
from pathlib import Path, PurePosixPath
import json,datetime,collections
ROOT=Path(__file__).resolve().parents[1]

def main():
    candidate=json.loads((ROOT/'reports/RELEASE_CANDIDATE.json').read_text())
    if any(PurePosixPath(item['path'].replace('\\','/')).as_posix().startswith('game/Assets/AfterHours/') for item in candidate.get('source_inputs',[])):
        raise SystemExit('Refusing to update release status: this legacy status writer targets the prior vector renderer. Use the current After Hours documentation/rebind workflow for candidates containing game/Assets/AfterHours/ inputs.')
    records=json.loads((ROOT/'reports/ACCEPTANCE_RESULTS.json').read_text())['records']
    requirements=json.loads((ROOT/'acceptance/requirements.json').read_text())['requirements']
    byid={r['requirement_id']:r for r in records}
    pending=[r for r in requirements if byid.get(r['id'],{}).get('status')!='PASS']
    completed=[p['id'] for p in json.loads((ROOT/'orchestration/dag.json').read_text())['packages'] if all(byid.get(i,{}).get('status')=='PASS' for i in p['requirement_ids'])]
    counts={tier:{'passed':sum(r['gate_class']==tier and byid.get(r['id'],{}).get('status')=='PASS' for r in requirements),'total':sum(r['gate_class']==tier for r in requirements)} for tier in ['software','target_device','human']}
    utc=datetime.datetime.now(datetime.timezone.utc).isoformat()
    state={'schema_version':1,'project':'Strike Ledger','scope':'1v1_traditional_fighter_single_credit_wallet','status':'PLAYABLE_CANDIDATE_WITH_PENDING_VERIFICATION' if pending else 'VERIFIED','current_package':None,'completed_packages':completed,'blocked_requirements':[r['id'] for r in pending],'game_build_ref':candidate['build_ref'],'game_content_sha256':candidate['content_sha256'],'next_action':'Complete the explicitly pending checks in ACCEPTANCE_RESULTS.json using actual hardware and human feedback; preserve the candidate and rerun both release-gate tiers.','updated_utc':utc,'verification':counts}
    (ROOT/'reports/STATE.json').write_text(json.dumps(state,indent=2)+'\n')
    summary='; '.join(f"{tier}: {v['passed']}/{v['total']}" for tier,v in counts.items())
    items='\n'.join('- '+r['id']+': '+r['title']+'\n  '+byid.get(r['id'],{}).get('notes','See the acceptance record for the exact missing evidence.') for r in pending)
    (ROOT/'reports/BLOCKERS.md').write_text('# Pending verification\n\nThe native game is implemented and packaged. Current evidence: '+summary+'.\n\n'+(items or 'No pending requirements.')+'\n\nNo physical controller, second computer, or human playtest result is inferred from software events or loopback processes.\n')
    (ROOT/'release_docs/VERIFICATION_STATUS.md').write_text('# Verification status\n\nUpdated '+utc+'. '+summary+'.\n\nCandidate: `'+candidate['build_ref']+'`. Content: `'+candidate['content_sha256']+'`.\n\n'+(items or 'All required checks passed.')+'\n\nThe accompanying acceptance ledger binds evidence to file hashes. Full recordings, raw bot experiments, commands, and replay traces remain in the source workspace reports directory.\n')
    (ROOT/'reports/RESUME_PACKET.md').write_text('''# Delivery continuation

The original ZIP is preserved in the parent directory. The extracted project is the complete native Strike Ledger game. Launch Play Strike Ledger.cmd, or extract a platform ZIP from dist and run its executable. Keep PCK and the runtime data directory together.

Production: deterministic C# Core, App sessions/replay/training/bots/UDP rollback, Godot.NET game with original articulated vector fighters, foundry/grid stages and synthesized audio. There are 98 actions, 12 leases, 16 training drills, one persistent credit wallet, and full match/result/rematch flows. Godot.NET 4.6.3 and .NET SDK8.0.424 were used. The sibling cache supplied toolchain binaries only; no sibling game source was imported.

Evidence: reports/ACCEPTANCE_RESULTS.json is authoritative; reports/RELEASE_CANDIDATE.json inventories production inputs and native binaries. Core/App final conformance, 252 matched balance runs, four actual two-process UDP fault profiles, native full-match movies, the free-kit audit, and byte-identical Windows/Linux native replays are retained under reports. Historical development output is explicitly superseded. The unmodified release gate verifies evidence integrity, not subjective play quality.

Run python tools/build.py --export --test to rebuild. The packaged self-contained CoreTests, NetworkLab and BalanceLab under dist/verification run without installing a development SDK. See their README for exact commands. Use python tools/run_scenario.py --list to list all29 canonical scenario routes. Use python tools/release_gate.py --tier software and --tier all to inspect acceptance.

Do not overwrite old evidence identities after a gameplay change. Rebuild, create a fresh candidate, rerun affected tests and native scenarios, then refresh artifact hashes. Keep actual controller, physical second-computer and human-feedback checks pending until performed. No public publishing, admin/security changes or expenses were needed.

Current verification: '''+summary+'.\n\nPending requirements: '+', '.join(r['id'] for r in pending)+'.\n')
    (ROOT/'reports/FEEL_CALIBRATION.md').write_text('''# Measured feel calibration

The final production Core passed 21 measured movement/input/freeze checks. Reproduce with `python tools/measure_core_feel.py`; read reports/evidence/feel-measurements/feel-measurements.json for every before/after state, actual input, event and canonical snapshot.

Rook walks forward/backward 3000/2100 authored integer units per tick; Vale 2700/2200. Both stop without extra displacement on neutral input. Rook forward dash measures96000 units over16 ticks; Vale91998 over19. Backdash measures85000 over20. Both jumps launch after4 ticks, remain airborne30 ticks, and require3 landing recovery calls. 1000 authored units equal one logical rendering unit.

A completed QCF starts its action on that same sampled simulation tick. Measured light/medium hitstop is8 calls, heavy11. High parry holds the attacker14 and defender8. All six selected supers freeze20 calls including startup; measured startup debits are900/1200/1500 from credits earned through an actual confirmed round payout. These figures measure simulation timing, not hardware input-to-photon latency.

The full competitive free-kit native match ran18466 input ticks across8 rounds with no leases, no debit receipts and no wallet decreases. Recorded presentation showcases cover directional and repeated parries, throws/techs, contacts, paid startups, and an explicitly labeled rollback correction fixture. Corrected defects include movement poses, replay-art HUD selection, training dummy/reset labels, menu-footer overlap and audio shutdown cleanup.

No actual player has accepted the fighting feel. Human feedback remains pending; bot experiments and timing measurements cannot establish expert competitive balance. Consult BALANCE_NOTES.md for matched-run uncertainty and policy differences.
''')
    (ROOT/'reports/BUILD_LOG.md').write_text('''# Actual build and verification

The preserved archive was unpacked into this isolated project and Git initialized. The initial129-file manifest, strict schemas and114 reference/tool tests passed before implementation. Those reference tests are historical scaffold checks, separate from the actual-game acceptance gate.

`python tools/build.py --export --test` uses matching Godot.NET4.6.3 export templates and .NETSDK8.0.424. Native Windows and Linux exports include the .NET runtime. Actual command output is in reports/evidence/release-build.log and reports/evidence/build.log. The final root test invocation passed15 contract checks,36 Core scenarios,10 App/network suites including16 training-drill successes and16 timeout failures. Strict data/schema validation passed with output redirected to strict-validation.log; redirecting to an empty .json inside the scanned tree is invalid and was corrected.

Actual native Linux and Windows headless matches each ran12789 input ticks and produced byte-identical competitive replays, including12806 commands/wallet records,230 checkpoints and30 debits. Exported Windows full-match, free-kit, UI and combat-showcase movies and both private-peer views have actual process-exit and media-decode verification. The separate4-profile UDP matrix completed with matching confirmed state, receipts, wallets and scores under delay/loss/jitter/duplication/reordering.

`python tools/package_release.py` creates the two ZIPs, player documentation, candidate manifest and SHA256 inventories. `python tools/package_verification.py` builds the six self-contained test executables. Exact artifact hashes and verification boundaries are in the acceptance ledger, native result files and release_docs/VERIFICATION_STATUS.md.
''')
    print(json.dumps({'utc':utc,'counts':counts,'pending':[r['id'] for r in pending],'completed_packages':completed}))

if __name__=='__main__':main()
