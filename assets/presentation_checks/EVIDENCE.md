# Presentation and input evidence

These artifacts distinguish rendered gameplay, software-driven UI, and isolated art fixtures.

## Rendered combat showcase

The final exported-build acceptance artifacts are in `reports/evidence/native-showcase-final`: `process-result.json`, `combat-visual-showcase.json`, the PNG captures, and `recording.mp4`. Reproduce them using `python tools/run_native_evidence.py showcase --label showcase-final --movie --fps 60`. The 11 segments advance legal `InputFrame` values through the production simulation. The report contains every input, event, resulting state hash, segment setup, and 13 outcome assertions. `native-showcase` retains the previous presentation candidate with its original runtime hashes.

The earlier `combat-final` and `combat-output` directories are development/source-run evidence. The earlier MP4 precedes final shutdown hardening and is supplemental only; final exported process results take precedence.

The test explicitly enables training. Positions, wallet balances, and the one-HP rollback setup are changed only at segment boundaries through `SetTrainingState`. It does not directly start actions or invent poses. Footwork, free and EX projectiles, high/low/projectile parries, throw and throw tech, five-hit super parry, and Vale's charge special all come from recorded directional/button input.

The final segment shows an explicitly unconfirmed paid K.O., restores the pre-action snapshot, and replays corrected late parry inputs. The unconfirmed branch releases no presentation events and never settles. The corrected branch must leave one debit, one receipt, no K.O., no points, and no payout. This is supplemental visual evidence of snapshot correction, not a substitute for the independent rollback/network-transport tests.

## Software controller UI

`reports/evidence/native-ui-final/controller-menu-flow.json` records 75 assertions from actual Godot GUI focus, input events and callbacks in the final exported game. `process-result.json` checks process exit and shutdown errors and snapshots the runtime hashes before and after. Eleven PNGs show title, the CPU-lineup footer, independent preparation, local fight, two-device remapping, replay controls, training tools, controller text entry, a verified complete result and its fresh rematch. Reproduce the exported test using `python assets/presentation_checks/run_ui_revision.py`. The simulated devices are visibly labeled. This verifies software routing and controller-accessible flows; it makes no physical-controller or two-pad hardware claim. Test settings do not overwrite player preferences. The temporary archive-selection replay is removed at completion; the completed-match replay remains as an evidence artifact. `native-ui` retains the earlier63-assertion exported candidate, and `ui-output` is older source-run evidence.

`inputchecks` runs assertions against the production settings and input-routing source, including duplicate swaps, malformed preferences, all SOCD combinations, independent maps and logical/physical key compatibility.

The UI revision's source-run proof is `reports/evidence/ui-rematch-debug` (64 assertions without screenshots). Do not treat source-run evidence as proof of a later exported candidate.

The durable isolated input run is `reports/evidence/input-settings-checks.log` (44 assertions: the original 38 plus six JSON/temporary-disk roundtrip checks). `input-settings-review.json` binds the unchanged production `GameSettings.cs` hash to the original candidate. The roundtrip uses production JSON properties and normalization but does not invoke the live user-path Save/Load methods. Physical controller connection/unplug remains a separate hardware gate.

## Complete native matches

`native-match-final` contains the complete final-renderer Windows match, with a clean process result, 54 verified PNGs and a fully decoded 229-second movie. `match-final-media-review.json` records its selected-frame inspection. Earlier candidate folders `native-match`, `native-free-kit`, both `native-network/peer*` and `native-linux` also contain complete exported-game movies, clean process results and full media-validation reports with their original runtime hashes. `full-match-media-review.json`, `free-kit-media-review.json` and `linux-media-review.json` record selected-frame inspection and the scope of those runs. The free-kit replay has eight empty preparations, no paid debit or lease, and no wallet decreases. The Linux recording uses WSLg/X11 with llvmpipe; intentional process suspension during Windows diagnostics makes its wall-time frame metrics unsuitable for normal-play performance claims.

## Art and audio checks

`output/*.png` are the separate, visibly labeled renderer pose fixtures. They test original silhouettes and animation drawing, and are not gameplay evidence. `assets/check_audio.py` checks the original generated PCM WAV files for format, duration, clipping, continuity at edges and distinct content. Asset provenance is listed in `assets/ASSET_REGISTER.csv` and player-facing third-party notices in `release_docs/ASSET_NOTICES.md`.

## Shutdown

`quit-verbose.log` reproduces headless looping-WAV playback remaining alive at shutdown. Stopping/detaching in `_ExitTree` cleaned up a short Debug title probe (`quit-verbose-fixed.log`) but did not consistently clean up Release full-match audio, so it was not sufficient for acceptance. The final shutdown path calls `ArenaView.ShutdownAudio()` before quitting, blocks additional cues, and lets the audio server drain across frames for at least 350 ms of real wall time. The Debug full-match probe in `reports/evidence/audio-drain-debug.log` verifies this path. Exported acceptance requires complete results, exit 0 and no shutdown ERROR lines through `tools/run_native_evidence.py`.
