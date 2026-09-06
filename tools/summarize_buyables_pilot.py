"""Recompute bounded paired uncertainty from actual pilot sample rows; never synthesizes a run."""
from pathlib import Path
import argparse, hashlib, json, math
def bound(values):
 n=len(values);mean=sum(values)/n;half=math.sqrt(2*math.log(40)/n)
 return dict(mean=mean,independent_seed_blocks=n,lower=max(-1,mean-half),upper=min(1,mean+half),method='95% Hoeffding bound for independently sampled seed-block deltas bounded [-1,1]; conditional sampling model, no familywise guarantee')
def main():
 p=argparse.ArgumentParser();p.add_argument('directory',type=Path);a=p.parse_args();path=a.directory/'samples.jsonl';rows=[json.loads(line) for line in path.read_text(encoding='utf-8').splitlines() if line.strip()];summary=json.loads((a.directory/'summary.json').read_text(encoding='utf-8'));groups=[]
 if summary['scope']=='rentals':
  for item,policy in sorted({(r['Item'],r['Policy']) for r in rows}):
   selected=[r for r in rows if r['Item']==item and r['Policy']==policy];blocks=[]
   for seed in sorted({r['Seed'] for r in selected}):
    leased=[r for r in selected if r['Seed']==seed and r['Rental']];saved=[r for r in selected if r['Seed']==seed and not r['Rental']]
    assert len(leased)==len(saved)==2 and {r['Seat'] for r in leased}=={0,1}
    blocks.append(dict(seed=seed,delta=sum(r['ActorResult'] for r in leased)/2-sum(r['ActorResult'] for r in saved)/2))
   groups.append(dict(item=item,policy=policy,blocks=blocks,bounded_interval=bound([b['delta'] for b in blocks])))
 else:
  for payout,spend,gap in sorted({(r['Payout'],r['OpeningSpend'],r['Gap']) for r in rows}):
   blocks=[]
   for seed in sorted({r['Seed'] for r in rows}):
    treated=[r for r in rows if (r['Payout'],r['OpeningSpend'],r['Gap'],r['Seed'])==(payout,spend,gap,seed)];base=[r for r in rows if (r['Payout'],r['OpeningSpend'],r['Gap'],r['Seed'])==(payout,spend,'current',seed)];assert len(treated)==len(base)==2
    blocks.append(dict(seed=seed,match_delta=sum(r['ActorResult'] for r in treated)/2-sum(r['ActorResult'] for r in base)/2,round2_delta=sum(r['OpeningRoundResult'] for r in treated)/2-sum(r['OpeningRoundResult'] for r in base)/2))
   groups.append(dict(payout=payout,opening_spend=spend,gap=gap,blocks=blocks,bounded_match_interval=bound([b['match_delta'] for b in blocks]),bounded_round2_interval=bound([b['round2_delta'] for b in blocks])))
 result=dict(format='penny-buyables-pilot-bounded-uncertainty-v1',source_samples_sha256=hashlib.sha256(path.read_bytes()).hexdigest(),source_summary_sha256=hashlib.sha256((a.directory/'summary.json').read_bytes()).hexdigest(),actual_samples=len(rows),groups=groups,limitations=['Two chosen seeds are a descriptive pilot. Population confidence requires an independent random-seed sampling model that this convenience sample does not establish.','Mirror seats are averaged inside each seed. They never double the independent sample size.','This bounded supplement supersedes any narrow t interval in the first-run summary when observed deltas happen to coincide. It changes statistical reporting only, not measured inputs or outcomes.','No conclusion of human competitive fairness or optimal purchase value follows from these samples.'])
 out=a.directory/'bounded-uncertainty.json';out.write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8');print(json.dumps(dict(path=str(out),actual_samples=len(rows),groups=len(groups),independent_seeds=len({r['Seed'] for r in rows}))))
if __name__=='__main__':main()
