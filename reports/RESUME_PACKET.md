# Delivery continuation

The original ZIP is preserved in the parent directory. The extracted project is the complete native Strike Ledger game. Launch Play Strike Ledger.cmd, or extract a platform ZIP from dist and run its executable. Keep PCK and the runtime data directory together.

Production: deterministic C# Core, App sessions/replay/training/bots/UDP rollback, Godot.NET game with original articulated vector fighters, foundry/grid stages and synthesized audio. There are 98 actions, 12 leases, 16 training drills, one persistent credit wallet, and full match/result/rematch flows. Godot.NET 4.6.3 and .NET SDK8.0.424 were used. The sibling cache supplied toolchain binaries only; no sibling game source was imported.

Evidence: reports/ACCEPTANCE_RESULTS.json is authoritative; reports/RELEASE_CANDIDATE.json inventories production inputs and native binaries. Core/App final conformance, 252 matched balance runs, four actual two-process UDP fault profiles, native full-match movies, the free-kit audit, and byte-identical Windows/Linux native replays are retained under reports. Historical development output is explicitly superseded. The unmodified release gate verifies evidence integrity, not subjective play quality.

Run python tools/build.py --export --test to rebuild. The packaged self-contained CoreTests, NetworkLab and BalanceLab under dist/verification run without installing a development SDK. See their README for exact commands. Use python tools/run_scenario.py --list to list all29 canonical scenario routes. Use python tools/release_gate.py --tier software and --tier all to inspect acceptance.

Do not overwrite old evidence identities after a gameplay change. Rebuild, create a fresh candidate, rerun affected tests and native scenarios, then refresh artifact hashes. Keep actual controller, physical second-computer and human-feedback checks pending until performed. No public publishing, admin/security changes or expenses were needed.

Current verification: software: 76/76; target_device: 3/5; human: 0/1.

Pending requirements: DEVICE-003, DEVICE-004, HUMAN-001.
