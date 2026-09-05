# Actual build and verification

The preserved archive was unpacked into this isolated project and Git initialized. The initial129-file manifest, strict schemas and114 reference/tool tests passed before implementation. Those reference tests are historical scaffold checks, separate from the actual-game acceptance gate.

`python tools/build.py --export --test` uses matching Godot.NET4.6.3 export templates and .NETSDK8.0.424. Native Windows and Linux exports include the .NET runtime. Actual command output is in reports/evidence/release-build.log and reports/evidence/build.log. The final root test invocation passed15 contract checks,36 Core scenarios,10 App/network suites including16 training-drill successes and16 timeout failures. Strict data/schema validation passed with output redirected to strict-validation.log; redirecting to an empty .json inside the scanned tree is invalid and was corrected.

Actual native Linux and Windows headless matches each ran12789 input ticks and produced byte-identical competitive replays, including12806 commands/wallet records,230 checkpoints and30 debits. Exported Windows full-match, free-kit, UI and combat-showcase movies and both private-peer views have actual process-exit and media-decode verification. The separate4-profile UDP matrix completed with matching confirmed state, receipts, wallets and scores under delay/loss/jitter/duplication/reordering.

`python tools/package_release.py` creates the two ZIPs, player documentation, candidate manifest and SHA256 inventories. `python tools/package_verification.py` builds the six self-contained test executables. Exact artifact hashes and verification boundaries are in the acceptance ledger, native result files and release_docs/VERIFICATION_STATUS.md.
