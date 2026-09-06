"""Keep original acceptance IDs while recording the user's v2 resource supersession."""
from pathlib import Path
import json
ROOT=Path(__file__).resolve().parents[1]
changes={
"SCOPE-001":"Inspect actual workspace/Git, preserve owner work and record the newest strictly 1v1 shop-only bank scope with nonspendable next-shop skill receipts.",
"CORE-002":"Full canonical snapshot/restore includes input buffers, action/parry state, frozen bank, round capabilities, super-use receipts, root provenance, eligible-defense clocks and bounded skill ledger.",
"INPUT-004":"Negative-edge free specials, same-tick EX chords, throw/overhead chords and capability-command priority are tested without illegal fallback.",
"COMBAT-007":"Damage scaling, chip/KO, stun/dizzy and juggle budgets enforce their data. Damage does not automatically grant currency; typed skill contacts follow the bounded v2 contract.",
"PARRY-003":"Multihit parries require distinct arms and contacts. Parry prevents damage/chip/stun; only eligible fresh manual precision parries grant bounded next-shop receipts once per original attack root.",
"ECO-001":"Exactly one shop-only bank per player, frozen throughout Fight; nonspendable pending skill receipts and round capabilities cannot authorize currency spending in combat.",
"ECO-002":"An owned EX family is repeatable for the round at zero activation cost; a prepaid super consumes its sole use on legal startup, with no whiff/block/parry/interruption refund.",
"ECO-003":"Unowned, exhausted, illegal or merely buffered capability commands never consume a super use, debit bank, or trigger an unintended free fallback.",
"ECO-004":"Saved bank is protected throughout Fight. Mixed carts have a 2400-credit limit and explicit slot caps; nonzero legacy reserve-floor requests are rejected.",
"ECO-005":"Rollback restores capabilities, super uses, provenance, skill receipts and pending rewards together; disappearing, repeated or changed inputs give canonical state and frozen bank.",
"ECO-006":"Actual C# v2 payouts (win1200, draw900, loss1200/1200/1500), cap3600, tier progression and outcome-first skill settlement clipping conform to independent fixtures.",
"SHOP-001":"Optional mixed carts validate fighter, implementation, unique IDs, replacements, price and limits (1signature, 1technique, 1gambit, 2EX, 1super); editing a draft is reversible.",
"CONTENT-003":"Core catalog provides 12 rentals, 8 EX licenses and 6 super permits; the expanded catalog provides 24 rentals, 8 EX licenses and 6 permits, all with legal entrypoints and counterplay.",
"REPLAY-003":"Replays retain confirmed cart, super-use and skill-reward receipts; verification reproduces exact commands, typed events, bank and canonical hashes.",
"NET-003":"Late input correction restores capabilities, super-use state, pending skill rewards and confirmed event presentation without duplicate receipts.",
"NET-004":"A predicted prepaid-super use, reward and terminal result can disappear on correction; only the confirmed canonical result and receipts are published.",
"UI-002":"Fight HUD shows static saved bank, owned EX families, super NONE/READY/USED and nonspendable NEXT SHOP rewards; shop explains prices, cart limits and remaining bank.",
"BAL-001":"Matched v2 pilots compare product purchase versus saving, score0-0 and score1-0 openings with equal banks, and shop-only A/B/C/D payouts crossed with rewards on/off, reporting samples and uncertainty.",
"BAL-002":"Zero-bank/full-bank and low-health corner probes exercise repeatable EX pressure, chip, manual parry, movement and product interactions, reporting tactical failures honestly.",
"BAL-003":"Matched spending and reward-seeking policies probe caps, recovery incentives and full-match outcomes; limited bots are distinguished from exhaustive anti-farming or competitive-balance proof.",
"DEVICE-004":"Two physical machines complete a private 1v1 LAN match with matching confirmed result, cart, super-use and skill-reward logs."
}
p=ROOT/'acceptance/requirements.json';data=json.loads(p.read_text(encoding='utf-8'))
history=ROOT/'audit/upgrade-2026-09-05/acceptance-v2-supersession.json'
if not history.exists():
    history.write_text(json.dumps({"authority":"User requested Shop-Only Economy Rework v2; original acceptance IDs and evidence gates retained.","changes":[{"id":r["id"],"before":r["description"],"after":changes[r["id"]]} for r in data["requirements"] if r["id"] in changes]},indent=2)+"\n",encoding="utf-8")
for r in data["requirements"]:
    if r["id"] in changes:r["title"]=r["description"]=changes[r["id"]]
p.write_text(json.dumps(data,indent=2)+"\n",encoding="utf-8")
print(f"Updated {len(changes)} acceptance descriptions; preserved all IDs and gates.")
