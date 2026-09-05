# Start here — Strike Ledger, GPT6-Astra build pack
**Version 1.0 • 2026-09-05 • working title. This is scaffolding, not a finished executable.**

Extract into a NEW folder. Open your GPT6-Astra coding session in the directory containing this file and AGENTS.md. Paste EXECUTOR_PROMPT.md. It authorizes the entire bounded implementation, not another plan or the first work package. No undocumented `/goal` command is assumed.

Latest requirements are binding: original traditional 2D fighter with Third Strike–inspired feel; ONE persistent credit balance entirely replaces super/EX meter; exclusively 1v1, one arena, no teammate systems. Between-round leases and direct per-use EX/super spending compete for the same credits. Base offense/defense remains complete at zero credits.

Read order: AGENTS.md → docs/00_PRODUCT_CONTRACT.md → docs/01_DECISIONS_AND_AUTHORITY.md → docs/02_ARCHITECTURE.md → system docs → work_packages/INDEX.md → acceptance/requirements.json. Canonical numbers/content live in data/. Reference Python is an independent arithmetic oracle, not gameplay code.

Initial scaffold checks:
```
python -m pip install -r requirements-tools.txt
python tools/validate_pack.py --strict-schema
python -m unittest discover -s reference/tests -v
python tools/verify_manifest.py
python tools/doctor.py
```
Use a local environment for tool dependencies. Doctor is read-only. Native tools missing on this machine do not authorize stopping at planning; provision safely as the build docs describe. The initial release evidence gate correctly fails until real game evidence exists.

The finish includes two original fully playable fighters, single-wallet economy, entire local match flow, private 1v1 rollback, bots, training, deterministic replays, original visuals/audio, controls/settings and Windows/Linux distributions. Software, target-machine and actual human feel verification are separate gates. Never fabricate the latter two.

On an execution/context boundary, the agent writes reports/RESUME_PACKET.md and updates reports/STATE.json. Continue in the same directory using RESUME_PROMPT.md. For an already-started old prototype, follow docs/18_MIGRATION_FROM_PRIOR_PACK.md; do not overlay blindly.
