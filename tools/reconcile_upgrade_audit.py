"""Reconcile all91 original audit findings without inventing completion."""
from pathlib import Path
from datetime import datetime,timezone
import json,hashlib
ROOT=Path(__file__).resolve().parents[1]
def read(p):return json.loads((ROOT/p).read_text(encoding="utf-8"))
def sha(p):return hashlib.sha256((ROOT/p).read_bytes()).hexdigest()
sources=[
 ("reports/SHOP_V2_CORE_FINDINGS.json","originalAudit"),
 ("audit/upgrade-2026-09-05/SHOP_ONLY_APP_FINDINGS.json","pp_finding_updates"),
 ("audit/upgrade-2026-09-05/PRESENTATION_FINDINGS.json","findings")]
owned={}
for path,key in sources:
 for record in read(path)[key]:owned.setdefault(record["id"],[]).append({"review":path,"record":record})
root={
"PP-001":("IMPLEMENTED","Combat pause uses Start/Escape; default B/MK and rebound action roles are covered by native software input tests.","game/Main.ControlRouting.cs"),
"PP-005":("IMPLEMENTED","The shared CommandEncoder and actual purchased-option CPU witnesses cover gambit entrypoints, including derived branches.","src/StrikeLedger.App/CommandEncoder.cs"),
"PP-006":("PARTIAL","PurchasePlanning selects executable weighted alternatives from actual content. Bot preferences are limited policies, not a solved valuation of every matchup.","src/StrikeLedger.App/PurchasePlanning.cs"),
"PP-008":("IMPLEMENTED","Canonical cards expose input, purpose, tradeoff, replacement, price and expiry; long detail and practice wrapping receive native visual checks.","game/Main.Purchase.cs"),
"PP-009":("IMPLEMENTED","Local and online use the same canonical card/preparation details; online retains private commit/reveal.","game/Main.Purchase.cs"),
"PP-010":("IMPLEMENTED","Invalid edits preserve the visible last valid plan; real15-second timeout regression commits that plan.","src/StrikeLedger.NetworkLab/ShopV2AppTests.cs"),
"PP-011":("IMPLEMENTED","Invalid/unaffordable choices are rejected before replacing the draft; both-player commit preflight remains atomic.","src/StrikeLedger.Core/PreparationRules.cs"),
"PP-012":("SUPERSEDED_IMPLEMENTED","V2 removes reserve floors and combat affordability. Explicit owned EX/super state replaces the old floor display.","docs/21_SHOP_ONLY_V2.md"),
"PP-013":("DOCUMENTED_DESIGN_BOUNDARY","Shared-screen local drafts remain visible; remote drafts commit privately. Simultaneous final reveal is preserved and no local secrecy is claimed.","docs/11_UI_CONTROLS_AND_ACCESSIBILITY.md"),
"PP-014":("PARTIAL_BALANCE","Canonical slot prices are implemented. Matched purchase/save and EX-pair pilots measure bounded value; prices are not asserted competitively optimal.","src/StrikeLedger.BalanceLab/ShopOnlyPilot.cs"),
"PP-015":("IMPLEMENTED_WITH_BALANCE_LIMIT","False Start/False Pulse retain authored finite commitment and exact displacement, no damage and no false counter-hit income; strategic bluff value needs human play.","src/StrikeLedger.CoreTests/ActualSkillContactTests.cs"),
"PP-016":("PARTIAL_BALANCE","Canonical action effects and mirrored purchase/save pilots provide concrete comparisons. Optimal Low Drive/Low Turn matchup value remains a tuning question.","src/StrikeLedger.BalanceLab/ShopOnlyPilot.cs"),
"PP-017":("SUPERSEDED_WITH_BALANCE_LIMIT","Opening600 now also buys specific EX licenses. Opening bank/score-controlled policy comparisons are measured; human purchase preference remains open.","docs/21_SHOP_ONLY_V2.md"),
"PP-018":("PARTIAL_BALANCE","New pilots use two seeds, paired controls, seat mirrors and multiple policies. Results disclose uncertainty and do not establish expert balance.","src/StrikeLedger.BalanceLab/ShopOnlyPilot.cs"),
"PP-019":("PARTIAL_BALANCE","A/B/C/D payout/reward controls, exact caps and real repeated CH/AA/parry bait tests are present. Exhaustive adversarial strategy search remains open.","src/StrikeLedger.CoreTests/ActualSkillContactTests.cs"),
"PP-020":("HUMAN_PENDING","No owner/friend acceptance of match/shop pacing is invented.","release_docs/KNOWN_LIMITATIONS.md"),
"PP-022":("IMPLEMENTED","Preparation uses stable product IDs, explicit semantic rows and canonical slot types instead of item-array positions.","game/Main.Purchase.cs"),
"PP-045":("HUMAN_PENDING","Execution windows have actual measured/negative tests, but player acceptance of their strictness remains open.","release_docs/KNOWN_LIMITATIONS.md"),
"PP-046":("IMPLEMENTED_HARDWARE_PENDING","Analog triggers support threshold action bindings and software regressions; actual pad compatibility remains unverified.","game/GameSettings.cs"),
"PP-047":("IMPLEMENTED","Physical/logical keyboard behavior is explicit and tested without unconditional double sampling.","game/GameSettings.cs"),
"PP-062":("CURRENT_EVIDENCE_REQUIRED","Linux is tested as a native export under this PC's WSL Ubuntu; final candidate launch/comparison evidence is identified separately from historical runs.","tools/compare_native_replays.py"),
"PP-063":("EXTERNAL_PENDING","Two physical controllers, two physical PCs and owner/friend feel are still separate unperformed gates.","release_docs/KNOWN_LIMITATIONS.md"),
"PP-064":("IMPLEMENTED_WITH_LIMITS","Mechanical effect assertions, real contact provenance, rollback corrections, native flows and actual screenshot review complement counts. No count alone establishes polish.","src/StrikeLedger.CoreTests/ActualSkillContactTests.cs"),
"PP-065":("IMPLEMENTED","Checked-in GitHub Actions builds/validates Core and App with pinned tool versions and action references.",".github/workflows/verify.yml"),
"PP-067":("PARTIAL_REFACTOR","Shop, catalog, training, replay, network, camera and typography have focused partials/classes. Some Main functions remain dense; further layout/component refactoring is open.","game/Main.Purchase.cs"),
"PP-068":("DOCUMENTED_DELIVERY","Source Git contains code/data/design/asset provenance. Large native builds and evidence are separate release archives with manifests, not falsely claimed inside a source-only clone.","AUDIT_GUIDE.md"),
"PP-069":("DOCUMENTED_DELIVERY","Rights/provenance and bundled font OFL are explicit. No new legal ownership claim is inferred from the supplied artwork.","RIGHTS.md"),
"PP-070":("IMPLEMENTED","Save/Load/Repeat preserve stable mixed-product IDs and revalidate price/content/bank, with explicit rejected/stale messages and temp-file persistence checks.","game/GameSettings.cs"),
"PP-076":("PARTIAL","Post-match economy receipts and prior public facts are evidence-linked. A full prescriptive coaching system remains unimplemented.","game/Main.EconomyReport.cs"),
"PP-077":("OPEN_OPTIONAL","Local/private mutual rematch is implemented; a distinct arcade campaign/progression mode remains an optional unimplemented extension.","game/Main.ArchiveLobby.cs")}
original=read("audit/requests/penny-punchers-shop-only-economy-rework-v2/penny-punchers-shop-only-rework-v2/data/audit_crosswalk.json")["rows"]
rows=[]
for finding in original:
 rid=finding["id"];row={"id":rid,"title":finding["title"],"owner_reviews":owned.get(rid,[])}
 if rid in root:
  state,detail,path=root[rid];row.update(disposition=state,detail=detail,source=path,source_sha256=sha(path))
 elif rid in owned:
  row.update(disposition="IMPLEMENTATION_REVIEW_RECORDED",detail="Read the exact owned disposition and limitations below. This is not an automatic release PASS.")
 else:raise RuntimeError("Unreconciled finding "+rid)
 rows.append(row)
out={"schema_version":1,"utc":datetime.now(timezone.utc).isoformat(),"scope":"Every original PP finding preserved with implementation/partial/external disposition; individual review evidence identities and limits remain authoritative.","original_findings":91,"reconciled":len(rows),"review_sha256":{p:sha(p) for p,_ in sources},"rows":rows,"note":"Current candidate execution is recorded in SHOP_V2_ACCEPTANCE.json and ACCEPTANCE_RESULTS.json. Historical owner reviews are retained as history; final native reports establish current graphical behavior."}
assert len(rows)==91 and len({r["id"] for r in rows})==91
(ROOT/"reports/AUDIT_CROSSWALK.json").write_text(json.dumps(out,indent=2)+"\n",encoding="utf-8")
print("Reconciled all91 findings; no blanket resolved status.")
