"""Write current release/resume documentation from actual evidence ledgers."""
from pathlib import Path
from datetime import datetime,timezone
from collections import Counter
import json,hashlib,subprocess
import release_gate

ROOT=Path(__file__).resolve().parents[1]
def read(path):
 p=ROOT/path
 return json.loads(p.read_text(encoding='utf-8')) if p.is_file() else None
def write(path,value):
 (ROOT/path).write_text(value,encoding='utf-8')
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()

def main():
 candidate=read('reports/RELEASE_CANDIDATE.json');original=read('reports/ACCEPTANCE_RESULTS.json');v2=read('reports/SHOP_ONLY_V2_ACCEPTANCE.json')
 assert candidate and original and v2
 requirements=read('acceptance/requirements.json')['requirements'];lookup={r['requirement_id']:r for r in original['records']}
 counts={layer:{'passed':sum(lookup[r['id']]['status']=='PASS' for r in requirements if r['gate_class']==layer),'total':sum(r['gate_class']==layer for r in requirements)} for layer in ['software','target_device','human']}
 # Keep the supplied v2 ledger's own layer and status vocabulary intact.
 rows=v2.get('records',v2.get('requirements',[]));v2counts={}
 for row in rows:
  layer=row.get('layer',row.get('evidence_scope',row.get('gate_class','software')));cell=v2counts.setdefault(layer,{'passed':0,'total':0});cell['total']+=1;cell['passed']+=row.get('status')=='PASS'
 gates={tier:release_gate.evaluate(ROOT,tier) for tier in ['software','all']}
 paths={
  'coreExpanded':'reports/evidence/shop-v2-core-full-current/core-conformance.json',
  'contract':'reports/evidence/build-test/StrikeLedger.ContractTests/contract-conformance.json',
  'nativeUi':'reports/evidence/native-shop-v2-renamed-ui/controller-menu-flow.json',
  'windowsMatch':'reports/evidence/native-shop-v2-candidate-match/process-result.json',
  'windowsFreeKit':'reports/evidence/native-shop-v2-candidate-free/process-result.json',
  'nativeNetwork':'reports/evidence/native-shop-v2-candidate-network/peer0/process-result.json',
  'linuxGraphical':'reports/evidence/native-shop-v2-linux-candidate-5fps/process-result.json',
  'crossPlatform':'reports/evidence/shop-v2-renamed-cross-platform/result.json',
  'packages':'reports/evidence/shop-v2-renamed-package/result.json',
  'timing':'reports/evidence/shop-v2-feel-candidate/feel-measurements.json',
  'pilot':'reports/evidence/shop-v2-pilot-analysis.json',
  'manual':'reports/evidence/shop-v2-manual-native/result.json',
  'restartIntegrity':'reports/evidence/shop-v2-restart-recovery/integrity.json',
  'nameSwap':'reports/evidence/name-swap-audit/result.json',
  'profile':'reports/evidence/native-shop-v2-candidate-profile/runtime-result.json',
 }
 evidence={k:dict(path=p,sha256=sha(ROOT/p)) for k,p in paths.items() if (ROOT/p).is_file()}
 pending=[dict(id=r['id'],description=r['description'],status=lookup[r['id']]['status'],limitations=lookup[r['id']]['limitations']) for r in requirements if lookup[r['id']]['status']!='PASS']
 now=datetime.now(timezone.utc).isoformat();crosswalk=read('reports/AUDIT_CROSSWALK.json')
 state='SOFTWARE_VERIFIED_EXTERNAL_CHECKS_PENDING' if gates['software']['status']=='PASS' else 'RELEASE_VERIFICATION_IN_PROGRESS'
 result=dict(schema_version=1,project='Penny-Punchers',status=state,utc=now,candidate=candidate['build_ref'],canonicalContent=candidate['content_sha256'],gitCommit=subprocess.check_output(['git','rev-parse','HEAD'],cwd=ROOT,text=True).strip(),originalAcceptance=counts,shopOnlyAcceptance=v2counts,integrityGates=gates,originalAuditFindings=len(crosswalk['rows']),auditDispositions=dict(Counter(r['disposition'] for r in crosswalk['rows'])),evidence=evidence,pending=pending,scope='Two playable fighters, four stages, core/expanded shop-only v2 catalogs; bounded software verification, no physical-controller/second-machine/human verdict.',externalChecks=['Physical local controller sessions','Two physical LAN PCs','Owner/friend feel and balance acceptance'],presentationLimits=['Rare actions reuse compatible supplied poses','RGB sheets use runtime chroma key and palette masks','Marist panoramas are aspect-preserving backgrounds, not fully layered scenery'],ci='Pinned workflow is retained at ci/verify.yml; current OAuth login lacks workflow-install scope.')
 write('reports/UPGRADE_STATUS.json',json.dumps(result,indent=2)+'\n')
 gateRows='\n'.join(f'| {k.replace("_"," ")} | {v["passed"]}/{v["total"]} |' for k,v in counts.items())
 pendingText='\n'.join('- '+p['id']+': '+p['limitations'] for p in pending) or 'No remaining records in the original acceptance ledger.'
 text=f'''# Penny Punchers — shop-only v2 upgrade

Candidate `{candidate['build_ref']}`. Canonical JSON inventory `{candidate['content_sha256']}`. Updated {now}.

The game integrates the expanded September audit, Buyables v1 and the superseding Shop-Only Economy Rework v2. Thomas and Vincent use the supplied 32-bit presentation, with Foundry Ring, Training Grid and both Marist stages. Core offers 26 products/100 action nodes; Expanded offers 38 products/115 nodes, including Overtime and Prism Lattice. Counts include derived action branches, not extra fighters.

The final requested swap names the orange-outfit motion fighter **Vincent** (rook) and the teal-outfit charge fighter **Thomas** (vale). Their art, moves and numerical rules are unchanged. The name-swap audit verifies every production source delta, all mechanical JSON values and every Core method body/signature. Earlier executions retain their original names, assembly/content identities and unmodified replay files; the renamed candidate has fresh 66 Core scenarios, 15 App suites, 299 GUI checks and native package runs. Historical replay files are not silently converted to a different content identity.

The bank starts at 600 and is frozen during Fight. Shop carts combine rentals, up to 2 EX licenses and one optional super permit within 2400 credits. EX licenses repeat for the round; a super consumes its one use on legal startup. Confirmed counter-hit, anti-air and fresh precision-parry receipts settle at the next shop with category/round caps. Preparation, rollback, replays, results, training and public history use the same resource contract.

Health fills and damage trails now clip to each frame's angled opening, with a mirrored drain direction for player 2. Actual full/475/90 HP checks and replay reconstruction passed for both seats. Shop detail cards, prior-round facts, capability indicators, camera framing and controller navigation received native UI checks. A late network terminal receipt is drained before result handling, preserving the final counter/reward in round history.

## Verification

| Original acceptance layer | Verified records |
|---|---:|
{gateRows}

The unmodified software evidence-integrity gate is **{gates['software']['status']}**; the all-layer gate is **{gates['all']['status']}**. These gates validate evidence completeness and hashes; they do not establish subjective game quality. The separate 61-row v2 ledger is `SHOP_ONLY_V2_ACCEPTANCE.json`. All 91 original PP findings retain explicit implemented, partial, optional or external dispositions in `AUDIT_CROSSWALK.json`.

The validated mechanical corpus includes 75 Expanded Core scenarios, App suites 15 each, and 15 independent payout/cart fixtures; fresh renamed Core/App checks are distinguished above. The native UI completed 299 checks. Renamed Windows and Linux native headless replays agree across 16140 input ticks, 16160 commands and 7 completed rounds. Timing measurements cover 24 movement/input/freeze/defense checks under the documented cosmetic bridge. Matched pilots contain 800 product/pair rounds and 112 full matches, plus 64 bounded strategy cells; their confidence intervals and limitations remain in `BALANCE_NOTES.md`.

Native process results, recordings, frame reviews, package extraction tests and exact hashes are indexed by `UPGRADE_STATUS.json` and the acceptance ledgers. A recorded movie is not a real-time performance test. Linux graphical runs use this PC's WSLg software renderer; reduced recording rate is explicitly identified in their report.

The isolated Windows graphical profile completed seven rounds on the GTX 1050 Ti / i7-7700HQ. Across 17,180 actual process callbacks, frame intervals were 16.67 ms median, 17.13 ms at the 95th percentile and 17.74 ms at the 99th percentile. The worst interval was 498.93 ms; this run does not attribute that isolated stall to a specific cause or prove stall-free pacing.

The laptop restarted during recording/verification packaging. All 574 production source/export files matched the pre-restart candidate hashes. Interrupted movies and damaged derived/cache files were preserved and regenerated where needed; incomplete runs never count as passes.

## Remaining checks and limits

{pendingText}

Only two fighters are complete. Some rare moves reuse supplied poses; RGB chroma-key edges and palette masks retain limitations. Marist stages preserve the supplied panoramas rather than claiming fully layered reconstruction. Pricing, adaptive human strategy and competitive balance need playtesting. Physical-controller, two-computer LAN and owner/friend verdicts are never inferred from bots, injected events or two processes on this PC.

## Build and audit

Run `Play Penny Punchers.cmd`, or extract the Windows/Linux package and launch its native executable while keeping the directory together. Player controls, rules, asset notices and known limitations are in `release_docs/` and packaged `docs/`.

The repository is https://github.com/RJW34/Penny-Punchers. It contains the full source, supplied request packages, canonical data, assets and reproduction tools. Large native evidence is distributed separately with exact manifests. `AUDIT_GUIDE.md` describes source reproduction and evidence boundaries. The GitHub Actions workflow is retained at `ci/verify.yml`; activating it requires an account with workflow permission. Local verification has already run independently of that service.
'''
 write('reports/UPGRADE_REVIEW.md',text)
 write('reports/BLOCKERS.md','# Current remaining verification\n\n'+pendingText+'\n\nSee UPGRADE_REVIEW.md for presentation, platform and balance boundaries. Historical After Hours results remain in reports/history.\n')
 write('reports/BUILD_LOG.md','# Current build and verification\n\n'+f'Candidate `{candidate["build_ref"]}` was exported successfully for Windows and Linux using Godot.NET4.6.3 and .NET8.0.424. The actual command output is `reports/evidence/build.log`. Both native exports survived the restart with exact saved hashes.\n\n'+f'Current software gate: {gates["software"]["status"]}. The full evidence index and pending gates are in UPGRADE_STATUS.json and UPGRADE_REVIEW.md. Historical After Hours results are preserved separately and do not certify v2.\n')
 write('reports/RESUME_PACKET.md','# Current release state\n\n'+f'{state}. Candidate `{candidate["build_ref"]}`; production Core/App/game/data remain frozen.\n\nRead UPGRADE_STATUS.json, UPGRADE_REVIEW.md, SHOP_ONLY_V2_ACCEPTANCE.json and ACCEPTANCE_RESULTS.json before resuming. Completed checks are not rerun merely because the laptop restarted. Native hashes are preserved in RELEASE_CANDIDATE.json; interrupted evidence is marked separately.\n\n'+pendingText+'\n')
 write('reports/STATE.json',json.dumps(dict(schema_version=1,project='Penny-Punchers',scope='shop_only_v2_1v1_traditional_fighter',status=state,updated_utc=now,game_build_ref=candidate['build_ref'],game_content_sha256=candidate['content_sha256'],verification=counts,shop_only_verification=v2counts,next_action='Complete remaining current evidence and publish artifacts.' if gates['software']['status']!='PASS' else 'Run actual physical-controller/two-PC/owner playtests; use the release and audit companion.',blocked_requirements=[p['id'] for p in pending]),indent=2)+'\n')
 print(json.dumps(dict(status=state,acceptance=counts,v2=v2counts)))

if __name__=='__main__':main()
