# Verification boundary and test strategy
The included Python oracle tests the NEW proposed ledger/capability semantics only. It consumes already-classified contact facts; it cannot establish that the real engine extracts those facts correctly. The C# file is a compile-intended API/authorizer seed, not a full game patch. Neither constitutes native gameplay evidence.

Required production tiers:
1. Core unit/conformance: invoke current C# production classes, not a reimplementation of the oracle. Cross-check generated vectors. Test legal input parsing separately from direct action-start setup.
2. Combat classification: prestate snapshots, timelines, timings, exact counter/AA/precision cases and denied cases. Mirror both seats. One early CH then AA from same root cannot pay twice. Include lethal trades and rank-rejected contacts.
3. App integration: full input-only matches with real shopping, results and changed kit; record all bank writes and phase. No bank mutation between cart lock and confirmed settlement. No authorization reads bank.
4. Native GUI: actual buttons/controllers or clearly labeled software events, all attack buttons, EX purchase, no-purchase rejection, reward receipt, next-shop funds, art changes, retry/rematch and replay. Fake headless screenshots cannot prove this.
5. Rollback: delayed/lost/duplicated/reordered input correcting bought super use, anti-air launch classification, false perfect parry and earlier terminal. Final native traces agree without duplicate bonus or use consumption.
6. Exports/device/human: clean Windows/Linux builds and launches, actual controller/two-PC checks, and owner/friend judgment. Keep physical and human pending if not available, never fabricate approvals.

Use `acceptance/scenarios.json` for concrete required scenarios. Restore original controller/camera audit regressions first; broken gameplay corrupts balance samples. Do not require every unique combination to have identical win rate. Test rare boundaries systematically, then measure representative adaptable policies with multiple seeds.

`tools/check_release_evidence.py` is a conservative structural check only. It confirms manifests/artifact hashes for a supplied current candidate; it does NOT independently determine that a log is truthful, that code is complete, or that play is balanced. Actual execution and review remain required. Initial ledger is deliberately NOT_RUN and fails. Never write dummy artifacts to turn requirements green.
