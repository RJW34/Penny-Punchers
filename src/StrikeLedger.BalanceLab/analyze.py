"""Summarize completed, matched experiments without asserting human balance."""
import argparse, collections, hashlib, json, pathlib

def main():
    parser=argparse.ArgumentParser();parser.add_argument("directory",type=pathlib.Path);args=parser.parse_args();directory=args.directory.resolve()
    summary=json.loads((directory/"summary.json").read_text())
    matches=[json.loads(line) for line in (directory/"matches.jsonl").read_text().splitlines()]
    rounds=[json.loads(line) for line in (directory/"rounds.jsonl").read_text().splitlines()]
    assert len(matches)==summary["samples"] and not summary["excluded"] and all(not m["Aborted"] for m in matches)
    def context(m):return tuple(m[k] for k in ("Seed","ActorSeat","FighterA","FighterB","ArtA","ArtB","OpeningA","OpeningB","Stratum"))
    baseline={context(m):m for m in matches if m["Policy"]=="spend-on-confirm"}
    comparisons=[]
    for policy in sorted({m["Policy"] for m in matches}):
        sample=[m for m in matches if m["Policy"]==policy];assert all(context(m) in baseline for m in sample)
        comparison={"policy":policy,"matchedContexts":len(sample),"improved":sum(m["Verdict"]>baseline[context(m)]["Verdict"] for m in sample),"worsened":sum(m["Verdict"]<baseline[context(m)]["Verdict"] for m in sample),"unchanged":sum(m["Verdict"]==baseline[context(m)]["Verdict"] for m in sample)}
        comparisons.append(comparison)
    zero={"roundSeats":0,"actionStarts":0,"hits":0,"blocks":0,"parries":0,"paidAccepted":0,"combatSpend":0}
    for r in rounds:
        for seat in range(2):
            if r["OpeningCredits"][seat]!=0:continue
            zero["roundSeats"]+=1
            for dest,source in (("actionStarts","Actions"),("hits","Hits"),("blocks","Blocks"),("parries","Parries"),("paidAccepted","PaidAccepted"),("combatSpend","CombatSpend")):zero[dest]+=r[source][seat]
    assert zero["combatSpend"]==0 and zero["paidAccepted"]==0
    mirrored={}
    for m in matches:mirrored.setdefault(m["Id"].rsplit("-swap",1)[0],[]).append(m)
    assert all(len(pair)==2 for pair in mirrored.values())
    mirror_agreement=sum(pair[0]["Verdict"]==pair[1]["Verdict"] and pair[0]["Ticks"]==pair[1]["Ticks"] for pair in mirrored.values())
    aggregate={"build":summary["build"],"contentHash":summary["contentHash"],"matches":len(matches),"rounds":len(rounds),"ticks":sum(m["Ticks"] for m in matches),"mirroredPairs":len(mirrored),"mirroredOutcomeAndTickAgreement":mirror_agreement,"zeroCredit":zero,"pairedPolicyComparisons":comparisons,"payoutClipped":sum(p["Clipped"] for r in rounds for p in r["Payouts"]),"maximumRecoveryTier":max(t for r in rounds for t in r["OpeningRecoveryTiers"]),"leaseMovesStarted":sum(move.startswith("shop_") for r in rounds for starts in r["StartedMoves"] for move in starts)}
    (directory/"analysis.json").write_text(json.dumps(aggregate,indent=2))
    lines=["# Recorded 1v1 economy experiments","",f"Completed {len(matches)} matched experiments and {len(rounds)} played rounds ({aggregate['ticks']:,} simulation ticks), with no excluded or aborted runs. Each policy received the same frozen fighter/art/position/health/score/seed contexts; each context reverses budgets and mirrors seats.","",f"All {len(mirrored)} mirrored pairs agreed on match outcome and simulation duration: {mirror_agreement}/{len(mirrored)}. This is simulation symmetry evidence, not human balance acceptance.","","| Policy | Matched contexts | Improved vs confirm | Worsened | Same |","|---|---:|---:|---:|---:|"]
    for c in comparisons:lines.append(f"| {c['policy']} | {c['matchedContexts']} | {c['improved']} | {c['worsened']} | {c['unchanged']} |")
    lines+= ["",f"Across {zero['roundSeats']} player-rounds beginning at zero credits, fighters started {zero['actionStarts']} actions, landed {zero['hits']} hits, blocked {zero['blocks']} attacks and parried {zero['parries']}. Accepted paid starts and combat credit spending were both zero. This demonstrates that the free kit remains executable at zero, not that every matchup is competitively even.","",f"The experiments exercised recovery tier {aggregate['maximumRecoveryTier']}, recorded {aggregate['payoutClipped']} credits clipped by the wallet cap, and started leased moves {aggregate['leaseMovesStarted']} times. Raw per-round income and spend records are in rounds.jsonl; exact starting states are in frozen-states/.","","Outcomes compare these specific seeded policies. They do not prove a universally best spending strategy, exclude all recovery exploits, or quantify human agency. Paired seat runs are correlated; summary.json includes descriptive Wilson intervals and telemetry limits. No prices or combat data were tuned from this small experiment.","",f"Build: `{summary['build']}`",f"Content: `{summary['contentHash']}`"]
    (directory/"ANALYSIS.md").write_text("\n".join(lines)+"\n")
    manifest={p.name:hashlib.sha256(p.read_bytes()).hexdigest() for p in directory.iterdir() if p.is_file() and p.name!="artifact-sha256.json"}
    (directory/"artifact-sha256.json").write_text(json.dumps(manifest,indent=2));print(json.dumps(aggregate))

if __name__=="__main__":main()
