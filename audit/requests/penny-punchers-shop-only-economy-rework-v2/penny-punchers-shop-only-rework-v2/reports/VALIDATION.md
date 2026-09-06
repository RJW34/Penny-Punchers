# Actual packaging validation
**SCAFFOLD VALIDATED. NOT AN IMPLEMENTED OR VERIFIED GAME REWORK.**

95 Python reference/tool test methods passed. The seeded reference stress test submitted 12,000 supplied contact facts and checked frozen bank and reward caps. These were NOT game matches or detected collisions. Catalog/schema/cross-reference/DAG checks passed for26 migration products,38 candidate designs,10 work packages,61 requirements and45 PLANNED native scenarios. Nine independent-oracle conformance cases and10 budget examples were generated deterministically. Source baseline byte hashes match their attached-audit identity records; the remote head and selected runtime paths were reread through GitHub.

The initial release-evidence validator fails as intended: no actual new game candidate or native/device/human evidence exists. Synthetic validator unit fixtures are temporary files clearly labeled synthetic and are never included as game evidence. The validator binds identity/metadata/artifact hashes only; it cannot establish truthful gameplay or balanced feel by itself.

Packaging host has no `dotnet` or Godot executable on PATH. C# API seed compilation, current-game build, native contact classification, controller play, private-network runtime and human feel have NOT been executed here. Python success cannot substitute for those.

Reproduce from pack directory:
```
python tools/validate_pack.py
python -m unittest discover -s tests -v
python tools/generate_vectors.py
python tools/verify_manifest.py
python tools/check_release_evidence.py
```
The last command should return nonzero initially. `inspect_live_repo.py --repo PATH --output PATH` is a read-only receiving-agent census, not a test or automatic fixer. Do not overwrite in-progress audit work.

Final archive rehearsal: the ZIP is extracted to a fresh temporary directory; its schema validator,95-test suite, generated-vector equality checks and manifest verifier are rerun. The actual output is provided in the delivery-side archive verification JSON. Native verification remains unperformed regardless of archive integrity.
