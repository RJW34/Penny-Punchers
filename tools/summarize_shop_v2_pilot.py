"""Analyze recorded shop-v2 pilot samples; no synthetic gameplay or revised source outcomes."""
import argparse,datetime,hashlib,json,math
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def interval(values):
 mean=sum(values)/len(values);n=len(values)
 if n<2:return dict(mean=mean,seedBlocks=n,lower=None,upper=None,method='Insufficient independent seed blocks')
 half=math.sqrt(2*math.log(40)/n)
 return dict(mean=mean,seedBlocks=n,lower=max(-1,mean-half),upper=min(1,mean+half),method='95% Hoeffding bound conditional on independent seed blocks with differences bounded[-1,1]; exploratory and no multiple-comparison guarantee')
def main():
 parser=argparse.ArgumentParser();parser.add_argument('--products',type=Path,default=ROOT/'reports/evidence/shop-v2-product-pilot-current');parser.add_argument('--crossover',type=Path,default=ROOT/'reports/evidence/shop-v2-crossover-pilot-current');parser.add_argument('--output',type=Path,default=ROOT/'reports/evidence/shop-v2-pilot-analysis.json');a=parser.parse_args();a.products=a.products.resolve();a.crossover=a.crossover.resolve();a.output=a.output.resolve()
 paths=[a.products/'summary.json',a.products/'samples.jsonl',a.crossover/'summary.json',a.crossover/'samples.jsonl']
 productSummary=json.loads(paths[0].read_text(encoding='utf-8'));crossSummary=json.loads(paths[2].read_text(encoding='utf-8'));products=[json.loads(l) for l in paths[1].read_text(encoding='utf-8').splitlines()];cross=[json.loads(l) for l in paths[3].read_text(encoding='utf-8').splitlines()]
 assert productSummary['excluded']==crossSummary['excluded']==0 and all(s['Reconstructed'] for s in products+cross)
 assert productSummary['build']==crossSummary['build'] and productSummary['contentHash']==crossSummary['contentHash']
 candidate=next(v for v in crossSummary['isolatedPayoutVariants'] if v['name']=='C')
 assert candidate['contentHash']==productSummary['contentHash']
 corrections=[]
 for sample in products:
  if sample['Kind']=='rental-round' and sample['Payout']=='A':
   corrections.append(dict(sample=sample['Id'],field='Payout',raw='A',corrected='C'));sample['Payout']='C'
 projection=a.output.with_name(a.output.stem+'-product-samples.corrected.jsonl')
 a.output.parent.mkdir(parents=True,exist_ok=True)
 projection.write_text(''.join(json.dumps(s,separators=(',',':'))+'\n' for s in products),encoding='utf-8')
 effects=[]
 for policy in sorted({s['Policy'] for s in cross if s['Kind']=='crossover-full-match'}):
  for treatment,control,meaning in [('B','A','skill income with historical payouts'),('C','B','earlier payout parity with skills enabled'),('C','D','skill income with candidate payouts'),('D','A','earlier payout parity without skills')]:
   blocks=[]
   for seed in sorted({s['Seed'] for s in cross}):
    t=[s for s in cross if s['Kind']=='crossover-full-match' and s['Policy']==policy and s['Payout']==treatment and s['Seed']==seed]
    c=[s for s in cross if s['Kind']=='crossover-full-match' and s['Policy']==policy and s['Payout']==control and s['Seed']==seed]
    assert len(t)==len(c)==2 and {s['Seat'] for s in t}=={0,1}
    blocks.append(dict(seed=seed,delta=sum(s['ActorResult'] for s in t)/2-sum(s['ActorResult'] for s in c)/2,treatedSamples=[s['Id'] for s in t],controlSamples=[s['Id'] for s in c]))
   effects.append(dict(policy=policy,treatment=treatment,control=control,meaning=meaning,blocks=blocks,pairedInterval=interval([b['delta'] for b in blocks])))
 single={s['Item'] for s in products if '|' not in s['Item']};pairs={s['Item'] for s in products if '|' in s['Item']};starts={}
 for sample in products:
  for product,count in sample['ProductStarts'].items():starts[product]=starts.get(product,0)+count
 pairCoverage=[]
 for pair in sorted(pairs):
  cases=[s for s in products if s['Item']==pair and s['Rental']]
  pairCoverage.append(dict(products=pair.split('|'),purchasedSamples=len(cases),bothProductsActuallyStartedSamples=sum(all(s['ProductStarts'].get(p,0)>0 for p in pair.split('|')) for s in cases),sampleIds=[s['Id'] for s in cases]))
 earnings=[]
 for policy in sorted({s['Policy'] for s in cross if s['Kind']=='crossover-full-match'}):
  for control in ['A','B','C','D']:
   samples=[s for s in cross if s['Kind']=='crossover-full-match' and s['Policy']==policy and s['Payout']==control]
   earnings.append(dict(policy=policy,control=control,matches=len(samples),earned=sum(sum(s['SkillEarned']) for s in samples),granted=sum(sum(s['SkillGranted']) for s in samples),clipped=sum(sum(s['SkillClipped']) for s in samples),consumedSuperUses=sum(sum(s['SuperUsesConsumed']) for s in samples)))
 result=dict(format='shop-only-v2-observed-paired-analysis',utc=datetime.datetime.now(datetime.timezone.utc).isoformat(),command=['python','tools/summarize_shop_v2_pilot.py','--products',str(a.products),'--crossover',str(a.crossover),'--output',str(a.output)],build=productSummary['build'],contentHash=productSummary['contentHash'],inputs=[dict(path=str(p.relative_to(ROOT)),sha256=sha(p),bytes=p.stat().st_size) for p in paths],completedProductRounds=len(products),completedCrossoverMatches=sum(s['Kind']=='crossover-full-match' for s in cross),completedScoreControlledMatches=sum(s['Kind']=='opening-full-match' for s in cross),productAlternatives=len(single),pairAlternatives=len(pairs),productsWithActualStarts=len(starts),productStarts=starts,pairActivationCoverage=pairCoverage,fullMatchPairedEffects=effects,earningsByPolicy=earnings,scoreControlledEffects=crossSummary['gapEffects'],scoreOnlyBenchmark=crossSummary['scoreOnlyBenchmark'],limits=['These are chosen deterministic seed1/2 policy runs. Wide conditional intervals cannot establish population human balance or justify prices.','Counterbalanced seats are averaged inside seed blocks; individual seat samples are not treated as independent.','All38 products and12 two-EX pairs use a fixed Thomas opponent and two scripted policies in single rounds; full art/stage/opponent/loadout factorial is untested.','Crossover uses Thomas/Vincent with adaptive shop decisions; reward-seeking reacts to12-tick-old public observations. It is not an optimal farming policy.','Round2 fixtures assume300/100 previous skill receipts when enabled and preserve1-0 score; they do not claim that a first round was played to generate those assumptions.','No tactical meaning is inferred from activation frequency alone. Low-health corner response conformance and human adapted play remain separate evidence.'])
 result['reportingCorrection']=dict(reason='The executed preserved harness mislabeled single-product Payout as A although it loaded the shipping C content hash. This projection changes only that reporting field; actual inputs, commands, scores, events, receipts, hashes and trace bytes remain untouched. Source is fixed for future runs. This is not a claimed simulation rerun.',correctedRows=len(corrections),changes=corrections,projection=dict(path=str(projection.relative_to(ROOT)),sha256=sha(projection),bytes=projection.stat().st_size),tracePathBase=str(a.products.relative_to(ROOT)),rawSamplesSha256=sha(paths[1]),actualShippingContentHash=productSummary['contentHash'])
 raw=[json.loads(l) for l in paths[1].read_text(encoding='utf-8').splitlines()]
 assert len(raw)==len(products) and all({k:v for k,v in x.items() if k!='Payout'}=={k:v for k,v in y.items() if k!='Payout'} for x,y in zip(raw,products))
 rounds=[]
 for directory in [a.products,a.crossover]:
  ledger=directory/'round-economy.jsonl';result['inputs'].append(dict(path=str(ledger.relative_to(ROOT)),sha256=sha(ledger),bytes=ledger.stat().st_size))
  for line in ledger.read_text(encoding='utf-8').splitlines():
   row=json.loads(line);settlement=row['settlement']
   for seat in [0,1]:
    assert row['afterPayout'][seat]==row['beforePayout'][seat]+settlement[f'Payout{seat}']['Granted']+settlement[f'SkillPayout{seat}']['Granted']
   rounds.append(row)
 continuing=[]
 for deficit in range(1,5):
  observed=[(r,s) for r in rounds if r['usableInContinuingMatch'] for s in [0,1] if r['score'][s]==0 and r['score'][1-s]==deficit*2]
  continuing.append(dict(trailerScore=0,leaderScore=deficit,observedContinuingEntries=len(observed),trailerBankRange=[min(r['afterPayout'][s] for r,s in observed),max(r['afterPayout'][s] for r,s in observed)] if observed else None,scope='Observed continuing states only, not a randomized causal intervention at this deficit.'))
 result['roundEconomyReconstruction']=dict(verifiedRoundReceipts=len(rounds),seatBalanceEquations=len(rounds)*2,terminalStatesExcludedFromContinuingLiquidity=sum(not r['usableInContinuingMatch'] for r in rounds),scoreDeficitWitnesses=continuing)
 a.output.write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8');print(json.dumps({k:result[k] for k in ['completedProductRounds','completedCrossoverMatches','completedScoreControlledMatches','productAlternatives','pairAlternatives','productsWithActualStarts']},indent=2))
if __name__=='__main__':main()
