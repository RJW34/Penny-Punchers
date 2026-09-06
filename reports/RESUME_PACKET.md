# Current release state

SOFTWARE_VERIFIED_EXTERNAL_CHECKS_PENDING. Candidate `source-sha256:327be61cf6003b56cbc01e0f9abfd8c9a811fa13a80b33175829c93dfc5f6989`; production Core/App/game/data remain frozen.

Read UPGRADE_STATUS.json, UPGRADE_REVIEW.md, SHOP_ONLY_V2_ACCEPTANCE.json and ACCEPTANCE_RESULTS.json before resuming. Completed checks are not rerun merely because the laptop restarted. Native hashes are preserved in RELEASE_CANDIDATE.json; interrupted evidence is marked separately.

- DEVICE-003: No physical controllers were available; software-injected controller inputs are separate evidence. Pending evidence: process_log, video
- DEVICE-004: Two processes and WSLg ran on this one PC; no second physical machine was tested. Pending evidence: process_log, replay, video
- HUMAN-001: Owner/player playtest and acceptance have not been recorded. Pending evidence: human_feedback
