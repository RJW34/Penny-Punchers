"""Bind the Core/economy crosswalk to completed current evidence, without editing a release ledger."""
from pathlib import Path
import datetime, hashlib, json
ROOT=Path(__file__).resolve().parents[1]
def read(p):return json.loads((ROOT/p).read_text(encoding='utf-8'))
def entry(p):
 f=ROOT/p;return dict(path=p,sha256=hashlib.sha256(f.read_bytes()).hexdigest(),bytes=f.stat().st_size)
def main():
 report='reports/SHOP_V2_CORE_FINDINGS.json';d=read(report)
 audit='reports/evidence/shop-v2-canonical-audit/result.json';full='reports/evidence/shop-v2-core-full-current/core-conformance.json';core='reports/evidence/shop-v2-core-core-final/core-conformance.json';bait='reports/evidence/shop-v2-core-bait-current/core-conformance.json';analysis='reports/evidence/shop-v2-pilot-analysis.json';products='reports/evidence/shop-v2-product-pilot-current/summary.json';crossover='reports/evidence/shop-v2-crossover-pilot-current/summary.json';objects='reports/evidence/shop-v2-objects-candidate2/buyable-object-tests.json';reflection='reports/evidence/shop-v2-objects-candidate2/reflected-origin-precision.json';app='reports/evidence/shop-v2-app-candidate2-full/self-tests.json'
 a=read(audit);f=read(full);c=read(core);b=read(bait);p=read(products);x=read(crossover);measured=read(analysis)
 assert a['passed'] and f['failed']==c['failed']==b['failed']==0 and (f['passed'],c['passed'],b['passed'])==(75,65,1)
 assert f['content_hash']==p['contentHash']==x['contentHash']==measured['contentHash']==a['contentHashes']['full'] and c['content_hash']==b['content_hash']==a['contentHashes']['core']
 assert p['completed']==800 and x['completed']==112 and p['excluded']==x['excluded']==0 and p['allTracesReconstructed'] and x['allTracesReconstructed']
 d['utc']=datetime.datetime.now(datetime.timezone.utc).isoformat();d['productionState']='Core/App and both numeric catalogs frozen. Current tests and pilots below ran against their recorded exact content and assembly identities; native presentation and release integration are independently owned.'
 for label in ['core','full']:d['rulesets'][label]['contentHash']=a['contentHashes'][label]
 sources=sorted({v['path'] for v in d['sourceFiles']}|{'tools/audit_shop_v2_content.py','tools/summarize_shop_v2_pilot.py','tools/finalize_shop_v2_findings.py','tools/audit_shop_v2_effects.py','src/StrikeLedger.BalanceLab/ShopStrategyCells.cs','src/StrikeLedger.BalanceLab/Program.cs'})
 d['sourceFiles']=[entry(s) for s in sources]
 artifacts=[audit,full,core,bait,analysis,products,crossover,app,objects,reflection,'reports/evidence/shop-v2-product-pilot-current/samples.jsonl','reports/evidence/shop-v2-crossover-pilot-current/samples.jsonl','reports/evidence/shop-v2-product-pilot-current/round-economy.jsonl','reports/evidence/shop-v2-crossover-pilot-current/round-economy.jsonl','reports/evidence/shop-v2-core-full-current/audit-legal-traces.json','reports/evidence/shop-v2-core-full-current/action-replays.training.json','reports/evidence/shop-v2-core-full-current-console.log','reports/evidence/shop-v2-product-pilot-current-console.log','reports/evidence/shop-v2-crossover-pilot-current-console.log']
 strategies=['reports/evidence/shop-v2-strategy-current/strategy-summary.json','reports/evidence/shop-v2-strategy-core-current/strategy-summary.json']
 for path,label in zip(strategies,['full','core']):
  s=read(path);assert s['passed'] and s['completed']==32 and s['contentHash']==a['contentHashes'][label]
 artifacts+=['reports/evidence/shop-v2-action-effects.json','reports/evidence/shop-v2-pilot-process-results.json',*strategies,'reports/evidence/shop-v2-strategy-process-results.json','reports/evidence/shop-v2-strategy-current/strategy-samples.jsonl','reports/evidence/shop-v2-strategy-core-current/strategy-samples.jsonl','reports/evidence/shop-v2-strategy-harness/source-binding.json']
 d['evidenceFiles']=[entry(s) for s in artifacts]
 d['pilotHarness']=[entry(v['path']) for v in d['pilotHarness']]
 d['currentVerification']=dict(fullCoreScenarios=75,coreScenarios=65,coreSupplementalBaitScenario=1,canonicalChecks=25,productRounds=800,crossoverFullMatches=48,scoreControlledContinuations=64,allReconstructed=True,excluded=0,coreAssemblyId=f['core_assembly_id'],appBuild=measured['build'],fightBankEqualityAssertions=p['fightBankEqualityAssertions']+x['fightBankEqualityAssertions'],pressureCells=80,pressureParrySuccesses=14,pressureParryAttempts=16,pressureDefenderSurvivals=64,pressureScope='Scripted legal responses at a100HP already-cornered defender. Rook Pulse EX predictive parry timing missed at both facings; recorded failures are retained. No all-EX human escape claim.',performanceMetadataNote='The legacy test report calls all non-DEBUG builds Release. These executions used ExportRelease CoreTests DLL and the recorded exact Core assembly ID. Core step profiling is not native render pacing evidence.',pilotReportingCorrection=measured['reportingCorrection'],roundEconomyReconstruction=measured['roundEconomyReconstruction'])
 pilot_ids={'PP-082','PP-083','PP-084','PP-085','PP-086','PP-088','PP-089','PP-091'}
 details={
 'PP-082':('The approved v2 schedule removes the old300 award gap; pending skill and retained spend can still differ.64score1-0 continuations pair earned-gap versus equal-bank states with0/600 prior-spend fixtures across four isolated controls. Round2 and eventual-match deltas retain seed-block uncertainty.','The assumed round1 skill receipts300/100 are explicit fixture inputs. All-art/loadout factorial and trailer-top-up versus leader-reduction equivalence are not established.'),
 'PP-083':('48full matches plus64continuations record every settlement and flag whether the next shop is usable. Analysis separately inventories observed0-1,0-2,0-3,0-4 continuing states and excludes terminal wallets from comeback liquidity.','Only1-0 has paired economic interventions; deeper-deficit states are observations, not causal comeback experiments. Human value of liquidity remains open.'),
 'PP-084':('Every trace starts from an explicit bank/score snapshot and contains atomic preparation plans, actual frozen-bank fight inputs, and confirmed outcome/skill grants. Buy/save pairs retain the exact fee; score1-0 treatments hold prior spending fixed.','The bounded pilot does not model a complete taxonomy of efficient winners, failed human reversals, or asymmetric spending histories.'),
 'PP-085':('800actual rounds compare all38single products and12two-EX pairs to buying nothing at equal opening bank2400. Every product has an actual startup witness and paired seed-block outcome estimate; retained cash stays in the same bank.','Full mixed rental/license/art cart factorial and human opportunity-cost decisions are not exhausted. Single rounds cannot establish match-level purchase value.'),
 'PP-086':('Observed uses, contact outcomes, round lengths and full command traces are retained. Repeatable EX is additionally exercised at zero bank with ordinary recovery/charge rules and80corner response cells.','Short/long contextual value, deterrence and adapted repeated pressure remain human balance questions; frequent activation alone is not treated as value.'),
 'PP-088':('Reports separate the analytical score-only163/256 benchmark from measured results and paired score-preserving interventions. Counterbalanced seats are averaged within seed blocks; with two chosen seeds the conditional intervals remain broad.','No50percent first-winner conversion target or population human balance conclusion. Stage, art and player-pair blocking is incomplete.'),
 'PP-089':('Approved v2 uses four isolated controls: A historical payouts/no skills, B historical/capped skills, C shipping parity/capped skills, D parity/no skills. C is byte-identical to shipping; all variant files and hashes are retained.48fresh matches plus64continuations are reconstructed.','This is a representative pilot, not final balance approval. Crossover policies consumed zero super uses, so those matches do not measure art-frequency effects; separate product rounds and conformance cover actual arts.'),
 'PP-091':('Every observed round exports pre-settlement bank, nominal/granted/clipped result payout, old/new recovery tier, separate earned/granted/clipped skills, score and continuing-state usability. Analysis verifies both seats\' settlement equations; full command traces reconstruct preparation debits and every bank-frozen fight tick.','Owned-threat utility is distinct from bank totals. UI ledger rendering is independently verified; mathematical state counts are never presented as played matches.')}
 for q in d['originalAudit']:
  q['evidence']=[full]
  if q['id'] in pilot_ids:
   q['implementationState']='V2_PILOT_COMPLETED_LIMITED';q['detail'],q['remaining']=details[q['id']];q['evidence']=[analysis,products,crossover]
  if q['id']=='PP-087':q['detail']='All 8 EX across 2 facings and 5 real responses at a 100 HP zero-bank cornered defender. 80 reconstructed cells retain damage, survival and failed responses: 64 survivals, 14/16 scripted parry successes. Rook Pulse EX predictor missed its close spawn at both facings; this is not labeled impossible to parry.';q['remaining']='Predictive scripts are not human reaction measurements. All-art low-health corner sequences and adaptive mixed rental/EX pressure are not exhausted by these EX cells; conventional art defense tests are separate.'
  if q['id']=='PP-079':q['evidence']+=[analysis,products]
 for q in d['ownedV2Requirements']:
  q['evidence']=[full]
  if q['id'] in {'SO-049','SO-052','SO-053','SO-054'}:
   q['scopeState']='PILOT_COMPLETED_LIMITED';q['evidence']=[analysis,products,crossover];q['remaining']='Two selected seeds and scripted policies; complete matchup/stage/cart factorial and adapted human competitive balance remain open.'
  if q['id']=='SO-054':q['scenarios']=['shop_actual_repeated_baits_and_partial_cap','shop_strategy_cells'];q['evidence']+=[full,bait,*strategies];q['remaining']='Both seats complete repeated actual CH/AA/precision baits and partial caps. A separate32-cell supplement per catalog records unused-permit threat, draw-seeking, chip-out and decisive cash-out against matched control policies; all64 decisive-round match continuations reconstruct. Draw-seeking loses by timeout in these cells, both free/EX chip controls KO, and the purchased permit is actually used or deliberately retained as declared. Scripted policy outcomes do not establish optimal or human farming, deterrence or price fairness.'
  if q['id']=='SO-051':
   both=sum(v['bothProductsActuallyStartedSamples'] for v in measured['pairActivationCoverage']);total=sum(v['purchasedSamples'] for v in measured['pairActivationCoverage']);q['evidence']=[full,analysis,products];q['remaining']=f'All 12 two-EX pairs run against retained-cash controls; {both}/{total} purchased pair rounds actually start both licenses. 80 corner responses retain failures. No human reaction or universal escape claim.'
  if q['id']=='SO-004':q['evidence']+=['reports/evidence/shop-v2-action-effects.json']
  if q['id']=='SO-024':q['evidence']+=[objects,reflection];q['remaining']='Actual reflected own-origin precision parry retains the root and awards no opportunity; enemy-origin control awards100practice opportunity. Competitive root classifier and settlement are covered separately.'
  if q['id']=='SO-028':q['scenarios']+=['shop_actual_repeated_baits_and_partial_cap']
  if q['id']=='SO-031':q['evidence']+=[app];q['remaining']='App confirmation/abort and native results rendering have independently owned current evidence.'
  if q['id']=='SO-038':q['evidence']+=[app];q['remaining']='Hostile wire validation is independently owned by App; no unbounded network-payload claim from Core tests alone.'
 d['historicalEvidence']='The first v2 catalogs707a/f29b and later full5169 metadata runs are preserved in their original folders with STATUS.md. Current evidence uses0245/e9c73; no old artifact was silently rebound. Pre-v2 direct-spend checkpoint remains historical.'
 # Keep the dossier legible where an older terminal encoded a multiplication glyph incorrectly.
 def clean(v):
  if isinstance(v,str):return v.replace('\ufffd',' x ')
  if isinstance(v,list):return [clean(x) for x in v]
  if isinstance(v,dict):return {k:clean(x) for k,x in v.items()}
  return v
 (ROOT/report).write_text(json.dumps(clean(d),indent=2,ensure_ascii=False)+'\n',encoding='utf-8')
 print(json.dumps(dict(report=report,full=f['passed'],core=c['passed'],coreSupplement=b['passed'],products=p['completed'],crossover=x['completed'],contentHashes=a['contentHashes']),indent=2))
if __name__=='__main__':main()
