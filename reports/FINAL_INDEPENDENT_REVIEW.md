# Independent final release review

No new software correctness blocker found in the reviewed final scope. Actual settings persistence, rematch flow, source compatibility and native pacing are now substantiated. Frame-time tail stalls remain a documented host limitation. Final acceptance gate and external hardware/human claims are separate from this independent review.

Reviewed at 2026-09-05T09:37:47.941792+00:00 on Windows-10-10.0.19045-SP0. This is an independent team-agent source and execution-evidence review, not external human playtesting. No production or ledger was changed.

Runtime build: `strike-ledger-native-1/c75fa1b44d9da6f03682753fb9fde812a944c34be05051697a1954b4813babbc`. Runtime content: `ec249786c06cb4e5cc4a0ea2dd84fe8c818d8360a11977d85f1b9aa66c293e63`.
Frozen Core SHA256: `204653b031ca2700371d8c1df62c4b7b9eb131825fcaaba1ef5ee01676c2c7df`. Frozen App SHA256: `72e7b96bbd878126db96ba0be8e49bae49df1417bf3162f4aec4d1f325e6cebb`.
Source candidate observed: `source-sha256:312471d950685ecc88b89d64a88af35b2b57cabfc1ab9c2b44273191559995fa`. Source-bundle content hashing differs from runtime canonical-content hashing.

## Substantive checks

Reviewed all 82 requirement definitions (76 software) alongside the earlier per-requirement Core audit. The final exact binary evidence contains 36 Core cases, 10 App cases, all 16 drill success and timeout paths, four actual two-process socket profiles, 252 completed balance samples and native gameplay/UI captures. The packaged final CoreTests independently reconstructed all 98 action conformance traces. Windows and Linux native replays match all inputs, commands, checkpoints, wallet records, debits and bytes. The completed native free-kit match contains no paid debit or purchased lease.

Selected preparation, training diagnostics and corrected-parry stills were personally inspected for readable state and truthful captions. Media decoder reports establish recording integrity; this review does not claim to have listened to every recording. Software controllers and one-PC UDP do not stand in for physical device or two-machine gates.

NET-004 is supported by the actual rollback App test plus confirmed-only presentation event delivery and the native correction showcase. Predicted terminal audio/visual cues are withheld; correction removes their pending events. The showcase is correctly described as snapshot restore/resimulation, not a live network transport demonstration.

The dispatcher now defaults consistently to ExportRelease and forwards it to network tools. Core, App and network command construction, explicit Release override and unknown-scenario rejection were checked. Registry coverage is not claimed as 29 new process executions. The final native UI run exercises actual verified-match results and controller rematch; full acceptance status still comes from the separate candidate-bound gate.

## Remaining limits and closed review issues

- **QA-004: actual interactive pacing.** Native fight output repeatedly drops to 2â€“24 FPS. The matched 10,000-step profile measured Core+ReplayRecorder fight p95 0.751 ms and p99 1.3605 ms, with one 19.7 ms spike. This disproves sustained recorder cost as the main explanation and does not measure the complete Godot frame. Final source caches the foundry backdrop once and keeps dynamic combat geometry separate; the profiler is opt-in. A complete normal native match, with no movie or screenshots, now measures p50 16.7013ms,p95 21.0911ms,p99 168.2692ms,worst492.3552ms. This satisfies actual profiling with known limits; it does not establish perfectly steady60FPS.
- **UI-004 closed by actual native persistence.** The isolated Godot harness executes production Save/Load at the actual OS user-data path, restores all settings families, verifies a second atomic replacement and cleans its owned file. All14checks pass against unchanged source; real player preferences are untouched.
- **Final source binding reviewed.** Candidate312471d matches current inputs. Eight presentation/diagnostic/UI-evidence files changed, and their current semantics were manually reviewed. Core/App assemblies and canonical data remain identical. Old PCK source is stripped; no literal oldsource-body diff is claimed. Fresh native UI rematch and full-match pacing evidence cover final presentation behavior. Run the final candidate-bound acceptance gate separately.
- **External gates.** Physical controllers, two physical machines and human feel assessment are not claimed. Linux graphical target evidence must be bound only after the actual graphical run completes.

## Resolved review findings

Strict validation previously parsed its own zero-byte redirected reports/evidence/strict-validation.json before producing output. That was the only malformed JSON found with the validatorâ€™s exact exclusions. The empty generated file was removed and logs now use .log; the reference validator was not weakened. Its SCAFFOLD_ONLY contract scope is kept separate from gameplay evidence. The exact latest exit and any current ledger schema errors are recorded in final-independent-strict.log.

The dispatcher configuration mismatch was repaired as a tool-only change. The current source review found no new frozen Core/App correctness blocker in replay selection, training reset/checkpoint behavior, defensive tech evaluation, reserve selection, rollback economics or confirmed presentation event handling.

## Reproduction and artifacts

`python tools/run_independent_review.py` reproduces the read-only artifact/identity/comparison checks and writes the JSON and process log. The report records each actual check, command, source candidate and SHA256. Existing complete suites are inspected rather than needlessly re-executed.

- `reports/evidence/final-independent-review.json` â€” machine-readable review, scope, artifact digests and open findings.
- `reports/evidence/final-independent-review.log` â€” actual check results.
- `reports/evidence/final-independent-strict.log` â€” exact strict validator command and result.
- `reports/evidence/final-independent-action-verification/action-replay-verification.json` â€” 98 reconstructed actions on final ExportRelease Core.
- `reports/evidence/replay-step-profile/result.json` â€” matched actual Core and Core+Recorder measurements.
- `reports/CORE_INDEPENDENT_SOFTWARE_AUDIT.md` â€” earlier detailed 76-software-requirement source audit.
