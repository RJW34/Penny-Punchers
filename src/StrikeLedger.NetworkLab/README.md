# Reproducible App and socket tests

Build with `dotnet build src/StrikeLedger.NetworkLab/StrikeLedger.NetworkLab.csproj -c ExportRelease`. Run the resulting `bin/ExportRelease/net8.0/StrikeLedger.NetworkLab.dll --self-test --data data --output reports/app-exportrelease-final` from the repository root.

The ten suites exercise wallet-safe resimulation, the eight-tick prediction cap, late parry retracting a predicted paid-super KO, full bot-match replay and seek, invalid replay import, isolated training state, incompatible/forged-seat UDP controls, oversized datagrams, disconnects and a healthy 16-second pause/resume. All sixteen training evaluators also receive a real success sequence and a neutral-input timeout sequence.

Training success recordings contain normalized player/dummy inputs, emitted core events and per-frame canonical hashes. Explicit training position, health, dummy-action and checkpoint fixtures are stored separately as complete canonical snapshots with their recording index. This is executable evaluator conformance using the production core; it does not claim a human completed the drills with a physical controller.

`python tools/run_network_lab.py --configuration ExportRelease --matrix --output reports/network-lab-exportrelease` starts two actual UDP processes for each profile: RTT/jitter/loss 0/0/0, 50/0/0, 100/20/1%, and 150/20/3%. The last two also inject duplicates and reordering. This is the explicitly bounded four-profile matrix, not the Cartesian product of all values. Use `--dotnet <path>` for a private SDK and `--no-build` after a successful build.

Each process must complete a full competitive match, save its confirmed-input replay, and replay it to the exact final canonical state. The orchestrator requires peer agreement on final hash, wallets, scores and every round's settlement/startup receipts. It records commands, exit codes and SHA-256 hashes of raw logs, final snapshots, replay and economic exports. The simulation is allowed to run faster than the rendered 60 Hz game; prediction stalls are attempted advances, not rendered-frame smoothness measurements.

The handshake's build identity includes the actual App and Core module identities. Use the same configuration and source as the exported game, and compare DLL hashes when binding acceptance evidence. Loopback testing remains distinct from two physical machines, hardware-controller testing and human feel assessment.
