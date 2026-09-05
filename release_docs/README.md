# Strike Ledger

An original six-button 1v1 fighting game set in the Foundry Ring. Rook is a motion-input pressure fighter. Vale uses charge inputs, long kicks and spacing. One credit wallet pays for both round preparation and powerful attacks.

## Start playing

On Windows, open `StrikeLedger.exe` in the Windows package. Keep the `.pck` file and the accompanying application data folder beside the executable. On Linux, the executable is `StrikeLedger.x86_64` in the Linux package; keep its accompanying files together as well.

Choose **Versus CPU** for a match or **Training lab** to learn inputs. Select a fighter and one super for the match, then start. Training includes a measurement floor, input display, active collision boxes, frame stepping, dummy recording, checkpoints and focused drills.

The full controls are in `CONTROLS.md` and the in-game **Field manual**. **Settings → Controls & remapping** shows live input and lets you assign devices and change controls without a mouse.

## The wallet

- A match starts with **600 CR**. Movement, normals, throws, defense and ordinary specials remain free.
- EX attacks cost **300 CR**. The selected super costs **900, 1200 or 1500 CR**. Payment happens when the action starts, even if it misses or is interrupted.
- Wins award **1200 CR**. Losses award **900 / 1200 / 1500 CR**, depending on recovery tier. Draws award **900 CR**. The wallet caps at **3600 CR**.
- Preparation leases last one round. Choosing a reserve prevents a paid action from crossing that floor. Combat does not earn credits.
- Preparation balance projections assume no further combat spending. A new match resets credits.

During local preparation, each assigned input controls its own panel. Up/down selects an option; left/right changes it; confirm chooses it. Move down to **Ready** to lock the plan. Both drafts are visible on the shared screen.

## Local and private play

Local versus uses a keyboard and controller, or two controllers. Assign each device to one corner on the lineup or controls screen. The same physical device cannot occupy both corners.

Private play connects two peers on a trusted LAN or an existing private link. Both players need the same build, content, lineup, port and match phrase. Enter the other computer's private IP. The host uses the base UDP port; the joining peer uses base port + 1. The game does not configure router forwarding or publish a public server.

The private connection uses a two-frame input delay and bounded rollback. Paid-action and contact presentation events wait for confirmation. A disconnect does not award a speculative round payout.

## Saved data

Settings, replays, training traces and diagnostic captures stay in Godot's normal per-user application data folder for **Strike Ledger**. On Windows this is normally `%APPDATA%\Godot\app_userdata\Strike Ledger`. On Linux it is normally `~/.local/share/godot/app_userdata/Strike Ledger`.

Completed matches save a local replay. The replay menu checks build/content identity, state hashes and wallet history during playback. Training traces are separate from competitive replays.

## Hardware notes

The Windows renderer has been exercised on this PC. Two physical controllers and a second LAN computer still need device-level validation; deterministic tests and a same-machine network test do not establish those hardware results. Review the release's evidence/status notes for the exact checks completed.

Controller mappings use a driver identity plus controller slot. Drivers without a persistent slot use equal-model connection order. After swapping identical controllers between connection slots, check the live input page and reassign the corners if necessary.

The game uses the original After Hours bitmap characters, stages, effects and interface artwork, with procedurally synthesized audio. `ASSET_NOTICES.md` identifies the engine and embedded-font notices.
