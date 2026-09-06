# Feel calibration: honest reference, independent game
The Capcom interview describes directional parry and reading/execution as central to Street Fighter III [SRC-01]. That supports the qualitative target, not the numeric windows of our original game. Do not claim arcade-exact timing, animation, collision, priority or input parser equivalence.

## Machine-verifiable first
Measure key-to-simulated-action and render latency with recorded methodology; fixed 60 behavior across render rates; walking stop/start and dash profiles; arc duration and landing; timing of input history/cancel/reversal; action-frame/hitbox synchronization; freeze and collision symmetry; minimum link execution; high/low/multihit parry and red-parry boundaries; legal prepaid-super consumption and eligible defense-clock precision ages. Banks must remain unchanged during combat, including startup/freeze. Save input traces, state hashes, hitbox videos and results at each candidate/content revision.

`tools/measure_core_feel.py` compiles only its measurement harness against the frozen exported Core, verifies the candidate's source/content inputs and matching Windows/Linux Core bytes, and checks the assembly remains unchanged afterward. Its simulated input/movement/freeze/precision measurements do not measure physical key latency, rendered FPS or human feel. Those need separate native pacing/device/human evidence.

Use original synthetic drills, not downloaded arcade input traces or copied move tables. Real reference play is optional only when the user lawfully has the game; it is never a build prerequisite or reason to obtain ROMs/assets. Ask testers for feel observations rather than asking an LLM to declare a clone faithful.

## Actual human pass
The owner and a friend should play at least: two free-kit rounds, repeated parry drills, both character archetypes, an economy match with shops skipped, one with leases, and a real private-network match where available. Ask whether movement feels deliberate, motion recognition fair, hitstop informative, normals distinct, spacing readable, throws/parries consequential, and credit-spending understandable. Ask exactly where controls feel mushy or unexpectedly strict. Record device/OS/build/content/pacing/assist settings.

Separate “a mechanic works” from “the game feels right.” The software agent may produce evidence that a parry succeeds at a boundary; only a real player can supply the actual owner feel verdict. No fake reviewer, invented session or auto-PASS. A pending human response must remain HUMAN_PENDING while all unrelated software work continues.

## Tuning order
Fix determinism/timing bugs → input recognizer/cancel correctness → free-kit walking/jump/normal spacing → defensive response and hitstop → character move roles → shop prices, skill rewards and opportunity costs → cosmetic polish. Do not hide sluggish input by expanding parry windows or give the losing player invisible combat buffs.

Known deliberate deviations: original moves/health/frame data; explicit priority/trade rule; no charge partitioning; simple pose boxes; configurable bounded input approximation; simplified dizzy/quick-rise details; no earned combat gauge. Document any added deviation with a test. “Inspired by Third Strike” is the claim; “Third Strike but reskinned” is not.
