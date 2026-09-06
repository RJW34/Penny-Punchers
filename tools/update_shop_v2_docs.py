"""One-time migration of active entry-point contracts; preserve the exact prior prose for audit."""
from pathlib import Path
import hashlib,json

ROOT=Path(__file__).resolve().parents[1]
ARCHIVE=ROOT/'audit/upgrade-2026-09-05/pre-shop-v2-docs'
REPLACEMENTS={
'START_HERE.md':'''# Penny-Punchers: current source and build

This is the working native game repository. Continue in this checkout and preserve uncommitted work. The latest user request integrates Shop-Only Economy Rework v2 after the expanded audit and Buyables Design v1.

Read AGENTS.md, docs/21_SHOP_ONLY_V2.md, docs/01_DECISIONS_AND_AUTHORITY.md, and reports/RESUME_PACKET.md. Current code and registries are authoritative implementation inputs; archived design packs and pre-v2 evidence retain historical identity. Never restore the package's pinned baseline over the upgraded moves or fixes.

The finish includes Thomas and Vincent, the 32-bit art set, Foundry/Grid and both Marist stages, complete local and CPU matches, untimed training, deterministic replay, private rollback, settings and Windows/Linux builds. All competitive new matches use shop-only purchases, repeatable licensed EX, one purchased super startup per round, and bounded next-shop skill receipts. No per-action bank spending or active reserve floor remains.

Run `python tools/doctor.py`, `python tools/validate_pack.py --strict-schema`, and `python tools/build.py --test` for the current production checks. Run `python tools/build.py --export` for native packages after checks pass. Release evidence must match the exact candidate source/content/binaries; legacy oracle passes are not current gameplay proof.

Keep software, physical-controller, two-physical-PC and actual-human verdicts separate. Do all available implementation and verification without fabricating unavailable external evidence. Update reports/STATE.json and reports/RESUME_PACKET.md at meaningful boundaries.
''',
'EXECUTOR_PROMPT.md':'''Implement and verify the complete Penny-Punchers upgrade in this existing checkout. The user authorized local game/computer work and the Penny-Punchers Git repository. Preserve the installed move libraries, controller/camera/actor repairs, 32-bit artwork, Marist stages and Thomas/Vincent naming.

Read AGENTS.md and docs/21_SHOP_ONLY_V2.md first. Shop-Only Economy Rework v2 supersedes the old direct-startup debit, automatically owned EX, reserve floor, no skill income and free match-locked super rules. All purchases occur atomically in the between-round shop; EX licenses repeat under normal combat legality; a purchased super has one legal startup; pending CH/AA/precision-parry receipts deposit only at confirmed settlement. Keep ordinary offense/defense free and preserve exactly two opposing players in one arena.

One integrator owns shared simulation, registry, serialization and protocol contracts. Specialists may modify agreed independent files. Production facts must use simultaneous pre-contact state and accepted collision outcomes; roots, clocks, receipts and super uses roll back together. Input intent recognition cannot downgrade a rejected EX/super command. Bank stays unchanged in combat, and no activation check reads it.

Execute implementation, production tests, real native GUI interactions, two-process network fault tests, fresh exports and reproducible package checks. Complete agent-solvable work; do not claim completion from documentation or an independent Python oracle. Report unavailable physical devices, second physical PC and human feel judgments honestly. Preserve historical evidence and replace stale current-candidate status claims with evidence tied to the actual build.
''',
'docs/00_PRODUCT_CONTRACT.md':'''# Penny-Punchers product contract

The current user-approved resource contract is docs/21_SHOP_ONLY_V2.md. It supersedes earlier economy restrictions in this repository and the supplied v1 archives.

Build an original native traditional 2D fighting game for exactly two opponents in one arena. Third Strike is a qualitative reference for grounded six-button feel; no franchise engine, ROM, art, audio or exact frame tables are reused. Thomas is the motion-input pressure fighter (stable internal ID rook); Vincent is the charge-oriented spacing fighter (vale).

Both fighters retain complete ordinary normals, specials, movement, free high/low/air/red parries, blocking, throws and techs, confirms, cancels, knockdowns and stun. The installed100-move core and115-move expanded registries retain their implemented rentals, branches, actors, projectiles, fields and installs. The shop licenses selected EX families and optionally one super each round. The bank is static during combat; skill receipts are pending until confirmed result.

Local1v1, CPU matches, frame-step training and drills, recorded/replayed matches, private1v1 direct-IP rollback, controllers/remapping, settings, help, audio and Windows/Linux packages form the complete loop. Foundry, Grid and two Marist stages share combat geometry. Five points clinch within nine rounds, draws grant half a point each, and a final tied score is a match draw. Clocks:60-second fight,15-second shop,3-second reveal and2-second countdown.

No teams, transfers, assists, accounts, purchased currency, microtransactions, persistent progression or public matchmaking. These exclusions do not excuse incomplete private networking or presentation. Software execution, physical hardware and actual human feel are distinct verification gates; retain evidence and honest limits for each.
''',
'docs/05_ECONOMY_AND_PREPARATION.md':'''# Shop-only round economy

The full active semantics and provenance are in docs/21_SHOP_ONLY_V2.md. Prices and rules are loaded from current data, with data-driven catalog validation and a new incompatible replay/content identity.

Starting bank600; cap3600; shop cap2400. One optional signature, technique and gambit, up to two EX licenses and at most one super permit. Equipment expires for both players each round; unspent bank carries. EX costs600 or900 for the vertical reversal and repeats under ordinary legality. Super products cost900/1200/1500 and buy one legal startup. A new shop may select a different art. The ordinary complete kit remains free.

Validate both players' complete product-ID carts before mutation, including fighter eligibility, duplicate IDs, slot limits, replacement/conflict rules, budget, bank and implemented content identity. Same commit key/payload is idempotent; changed payload fails closed. Draft highlighting never spends. Each accepted edit updates the last-valid cart; timeout commits it. A saved plan is re-priced and validated against current content. Local drafts may be visible on the shared screen; private network plans use commit/reveal and must not permit a last-mover counterpurchase.

No combat bank writes, bank affordability reads, reserve floor, per-use EX price, emergency buying or cash refund. Recognize explicit locked EX/super syntax before ownership filters, preserving motion/charge/cancel/recovery rules and chord release suppression. Legal super startup consumes its use even on whiff, block, parry or interruption; illegal and buffered attempts do not. Rollback restores the original capability/use/reward state and re-simulates corrected inputs.

CH50, AA75 and precision parry100 are nonspendable receipts, capped at two paid awards per category and300 combined each round. Precise definitions, origin roots, simultaneous prestate, frozen-edge exclusions, deterministic order and no-farming rules are in the active v2 contract. Training is diagnostic only.

Confirmed outcome payout uses the old recovery tier: win1200, loss1200/1200/1500, draw900. Update tier after calculating the result. Grant result income first, then skill receipts within remaining wallet space, recording both clipping amounts. Next shop exposes opening bank, cart, frozen saved bank, outcome and skill grants, clipping and closing bank. A match's final income is unused postmatch balance, not comeback liquidity; rematch resets600 and aborts do not settle unresolved rounds.
''',
'docs/14_1V1_BALANCE_AND_EXPERIMENTS.md':'''# Shop-only1v1 balance experiments

Current rules: docs/21_SHOP_ONLY_V2.md. Historical direct-spend pilots remain labeled pre-v2. Their sample counts and outcome conclusions cannot be inherited as repeatable-license balance proof.

Measure repeatable EX pressure, projectile occupancy, reversal recovery, charge constraints, chip, corner defense, confirms, one/two-license kits, free counterplay and zero saved-bank use. Compare each purchase with no-buy at equal opening cash, preserving its exact price as the no-buy player's saving. Test rentals and finite supers as useful threats, not merely successful action starts.

Run four versioned laboratory controls, all shop-only: A historical payouts/no skill, B historical payouts/skill, C candidate equal initial payouts/skill, D candidate payouts/no skill. The shipping default is C. Use multiple legal-input policies and seeds, swap seats/fighters, preserve traces and replay exact states. Disabled rewards are experimental controls, not an alternate unrequested shipping economy.

For comeback causality hold round-two score1-0 and matchup constant, compare earned-bank and equal-bank states, and account for different cart spending. A fair independent decisive first-to-five score leader converts163/256 before any income effect;50% is not the correct null. Mirrored trials are dependent blocks, not independent human samples. Record sample sizes, uncertainty, exclusions and actual execution limits.

Probe CH/AA/parry feeding, root reuse, multihit/reflection, early cap races, reward-seeking versus winning policies, draw/chip/final-round incentives, zero-bank ordinary defense and unused super threats. Stronger players may earn more, so capped income is not inherently a comeback mechanism. No passive interest, ordinary-hit farming, extra EX ammunition, paid defense, global stat boosts or hidden rubber-banding.

Human pairs must judge shop comprehension, execution, clarity, counterplay and desire to rematch. Software policy samples are exploratory evidence, not a substitute for that verdict or a claim of finished competitive balance.
'''
}
def main():
 ARCHIVE.mkdir(parents=True,exist_ok=True)
 records=[]
 for rel,text in REPLACEMENTS.items():
  path=ROOT/rel;original=path.read_bytes();backup=ARCHIVE/rel
  if not backup.exists():backup.parent.mkdir(parents=True,exist_ok=True);backup.write_bytes(original)
  path.write_text(text,encoding='utf-8')
  records.append({'path':rel,'original_sha256':hashlib.sha256(backup.read_bytes()).hexdigest(),'v2_sha256':hashlib.sha256(path.read_bytes()).hexdigest()})
 (ARCHIVE/'MIGRATION.json').write_text(json.dumps({'historical_only':True,'active_contract':'docs/21_SHOP_ONLY_V2.md','files':records},indent=2)+'\n',encoding='utf-8')
 print(f'Updated {len(records)} active documents; exact prior prose preserved.')
if __name__=='__main__':main()
