# Verification boundaries

Strike Ledger implements the bounded two-fighter game in the supplied project, with original vector animation and synthesized sound. VERIFICATION_STATUS.md records completed and pending checks. The source-workspace acceptance ledger records exact evidence and artifact hashes.

Two physical controllers and keyboard-plus-controller play have not been validated on this PC because no connected controllers have been detected. Software controller events exercise the menu and mapping flows; they do not certify controller hardware.

Private matches have been tested between two real processes on this PC, including simulated delay, jitter, loss, duplication and reordering. A match between two physical computers remains pending. No router forwarding or firewall configuration was changed.

Linux completed a full native graphical match in the existing Ubuntu WSL environment using the llvmpipe software OpenGL renderer and Dummy audio driver. Movie encoding was slow and temporarily suspended during a separate Windows performance investigation. Its wall time does not measure normal Linux play performance. Audio output through Linux desktop speakers and an independent Linux GPU remain unmeasured. Native Windows and Linux also produced byte-identical competitive replays.

On this PC (i7-7700HQ, GTX1050Ti), the optimized Windows build completed a normal full match with a 60 FPS cap. Actual frame intervals were median16.70ms,95th percentile21.09ms,99th percentile168.27ms, worst492.36ms. Most one-second samples were59–61FPS; concurrent automated UI recording caused visible stalls. These results do not promise a perfectly steady60FPS. The arena background is cached to reduce rendering cost. VSync can be changed in Preferences; disabling it improved the initial comparison on this Windows driver.

No human player has yet accepted the fighting feel or competitive balance. Counterbalanced bot experiments, command timing and deterministic tests provide measured evidence, with the uncertainty recorded in BALANCE_NOTES. They cannot establish expert tournament balance.

Replays are deliberately tied to their exact compatible build and content. Older development recordings may be rejected after a gameplay change. Training traces are separate from competitive replays.

A full inventory of completed and pending checks is in the accompanying candidate/acceptance records. The all-tier release gate remains nonzero until the outstanding actual-device and human checks are completed.
