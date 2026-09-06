# Penny Punchers

An original six-button 1v1 fighting game. **Vincent** uses motion inputs and close pressure; **Thomas** uses charge inputs, long kicks and spacing. Buy the round's kit in the shop, then fight with a fixed saved bank.

## Start playing

On Windows, open `StrikeLedger.exe`. On Linux, launch `StrikeLedger.x86_64`. These executable names remain for build compatibility; the game is Penny Punchers. Keep the executable, PCK and accompanying application directory together.

Choose **Versus CPU**, **Local versus**, **Training lab** or **Private match**. Select fighters, a stage and a trial. Foundry Ring, Calibration Grid, **Marist Green — Golden Hour** and **Marist Gates — Blue Hour** share the same combat geometry. Super selection happens in each round's shop, not in the lineup.

**Move catalog / try** opens an untimed catalog with rental, EX license, super permit and all-action pages. Read the command, replacement, purpose and tradeoff, or open the full card and practice. Branch entries identify their required starting move. Practice equips an explicit lab kit and does not pause a competitive opponent's shop clock.

## Build your round

A match starts with **600 CR**. The ordinary six-button kit, ordinary specials and defense remain free. During each buy period, spend no more than **2400 CR**, within your available bank:

| Product | Round limit | What the purchase grants |
|---|---:|---|
| Signature, technique, gambit rentals | One per slot | Repeatable use or a stated replacement for this round |
| EX family licenses | Two | Repeatable EX after ordinary recovery; 600 CR, or 900 CR for the vertical reversal |
| Super permit | Zero or one | One legal startup; 900/1200/1500 CR according to the selected product |

Prices come from the live catalog. **All equipment expires after the round.** A super's use is consumed on legal startup even if it whiffs, is blocked, parried or interrupted. An illegal or merely buffered input consumes no use. Licenses do not bypass motions, charge, recovery, cancel windows or object limits. Buying nothing keeps the complete free base kit.

Your bank stays fixed during combat. There is no per-attack payment, EX ammunition or protected reserve floor. The HUD shows owned EX, **SUPER READY / USED / NONE**, the small saved-bank value and pending **NEXT SHOP** awards.

Each assigned device navigates its own panel. Up/Down selects a row; Left/Right attempts a rental change or toggles a product; confirm opens a rental choice or invokes the focused action. An invalid edit shows its reason and leaves the last valid cart unchanged. **Ready** locks the plan; timeout locks the last valid cart displayed. Local drafts are visible to both players. Private drafts use commitment/reveal and remain hidden until both lock.

**Save** stores stable product IDs, with the quoted price and content identity. **Load** and **Repeat last** recheck current prices, trial, category limits and bank; neither buys automatically. The prior-round facts popup shows the revealed opponent kit, super use and confirmed public counts. Counts of observed jumps, counter contacts and parries are not predictions of the next plan.

## Settlement and skill awards

A win grants **1200 CR**. A loss grants **1200 / 1200 / 1500 CR** using the old recovery tier. A draw grants **900 CR**. Eligible direct counter-hits, grounded anti-airs and precision parries earn **50 / 75 / 100 CR** for the next shop, at most twice per category and **300 CR total per round**. Not every ordinary parry earns a reward; normal parry feedback still applies.

Pending skill money is not spendable during the fight. Confirmed settlement deposits outcome income first, then skill income, with amounts clipped at the **3600 CR** bank cap. The receipt distinguishes earned, granted and clipped credits. An aborted unsettled round pays nothing. Final-match funds have no further shop; rematches reset to 600 CR.

## Controls and private play

Keyboard defaults: A/D walk, W jump, S crouch; U/I/O punches and J/K/L kicks. LP+LK throws; MP+MK overheads. P and semicolon provide PP and KK chords, without performing motions. **Escape or controller Start pauses. B remains MK during combat and Cancel in menus.**

Settings includes two-device remapping, analog triggers, deadzone, physical/logical keyboard layout, audio, display, reduced flashes and shake. Physical mode follows key positions; logical mode follows the selected language layout. See `CONTROLS.md` and the live input test.

Local versus uses a keyboard and controller, or two controllers. Shared-screen and private commit/reveal drafts are different information formats; do not treat them as the same shop experiment.

Private play is for trusted LANs or an existing private link. Both peers need matching builds and content. Enter the opponent's private IP, base port and phrase; host uses the base port and join uses base port + 1. The phrase distinguishes the session; it is not strong public authentication. The game does not configure routers or publish a public server.

## Recordings and saved data

Settings, replays, training traces and diagnostics use Godot's **Penny Punchers** user folder: normally `%APPDATA%\Godot\app_userdata\Penny Punchers` on Windows or `~/.local/share/godot/app_userdata/Penny Punchers` on Linux. Existing Strike Ledger preferences are read when no new settings file exists; the original file is preserved.

Replay saves report success or the actual failure. Playback checks compatible commands, state hashes and round receipts. Historical recordings with different rules are rejected rather than reinterpreted. Source replays remain immutable when used for training. Training awards are labelled **PRACTICE ONLY** and do not accumulate competitive earnings.

## Versioned trials and verification

Core has 12 rentals, eight EX products and six super permits across both fighters, with 100 runtime action nodes including branches. Expanded has 24 rentals, eight EX products and six permits, with 115 nodes. Expanded replaces Rush Cascade/Tidal Step with Overtime/Prism Lattice. Both trials use this v2 shop-only economy.

Balance remains provisional. `BALANCE_NOTES.md`, `KNOWN_LIMITATIONS.md` and `VERIFICATION_STATUS.md` distinguish software evidence from physical-device and human acceptance. The packaged candidate identity and status files identify the actual binaries; old results do not certify this build. Original artwork and software notices are in `ASSET_NOTICES.md`.
