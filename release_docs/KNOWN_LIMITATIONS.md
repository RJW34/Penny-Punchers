# Verification boundaries

Strike Ledger implements the bounded two-fighter game in the supplied project, with the After Hours bitmap graphics and synthesized sound. VERIFICATION_STATUS.md records completed and pending checks. The source-workspace acceptance ledger records exact evidence and artifact hashes.

Two physical controllers and keyboard-plus-controller play have not been validated on this PC because no connected controllers have been detected. Software controller events exercise the menu and mapping flows; they do not certify controller hardware.

Private matches have been tested between two real processes on this PC, including simulated delay, jitter, loss, duplication and reordering. A match between two physical computers remains pending. No router forwarding or firewall configuration was changed.

The preceding release completed a full native Linux graphical match in the existing Ubuntu WSL environment using the llvmpipe software OpenGL renderer and Dummy audio driver. Its slow movie capture and temporary process suspension do not measure normal Linux play performance. The new After Hours Linux graphical movie and fresh platform-package checks are still being completed. Audio through Linux desktop speakers and an independent Linux GPU remain unmeasured. The preceding native Windows/Linux competitive replays agreed byte for byte, and the new exports retain those exact Core/App binaries and canonical content.

The current After Hours Windows build was profiled on this PC's i7-7700HQ and GTX 1050 Ti through a complete six-round, 12,789-tick native match, with a 60 FPS cap and no movie capture or screenshot writes. Across 13,665 measured frame intervals, the median was 16.666ms, the 95th percentile 16.802ms, the 99th percentile 17.316ms, and the worst 557.276ms. Of 228 logged FPS samples, 224 read 60; the others read 29, 34, 50 and 57. This shows mostly consistent pacing with occasional substantial stalls, not a guarantee of perfectly steady 60 FPS. The run's measured interval was 230.321 seconds; total process time including launch and shutdown was 232.750 seconds. It exited cleanly and reproduced the verified final match hash and wallets.

These are wall-clock intervals between actual Godot callbacks, not physical input-to-display latency. The separate callback profiler includes OS scheduling delays and excludes GPU work after draw submission; its 95th-percentile times were TickGame 0.258ms, UpdateArena 0.043ms, ArenaDraw 0.140ms and HudDraw 0.225ms. The cause of the worst isolated frame was not established by this run. Full logs and runtime hashes are in `reports/evidence/native-after-hours-pacing/` in the source workspace. The earlier vector-renderer measurements remain historical evidence. VSync can be changed in Preferences.

No human player has yet accepted the fighting feel or competitive balance. Counterbalanced bot experiments, command timing and deterministic tests provide measured evidence, with the uncertainty recorded in BALANCE_NOTES. They cannot establish expert tournament balance.

Replays are deliberately tied to their exact compatible build and content. Older development recordings may be rejected after a gameplay change. Training traces are separate from competitive replays.

The fighter sheets supply distinct action and universal key poses, with phase-aware reuse where no separate in-between cels exist. The original design boards remain preserved in the source package. The runtime removes their edited magenta matte and applies the alternate costume colors in a shader.

A full inventory of completed and pending checks is in the accompanying candidate/acceptance records. The all-tier release gate remains nonzero until the outstanding actual-device and human checks are completed.
