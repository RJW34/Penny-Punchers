# Build, audit and evidence tools

`build.py --test` regenerates content, compiles the Godot C# game, imports resources and runs the executable C# verification suites. Add `--export` to create self-contained Windows/Linux exports using matching installed templates. `package_release.py` packages those exports.

`run_scenario.py --list` enumerates scenario routes. `run_native_evidence.py` drives actual exported games; some platform routes depend on this project's original Windows/WSL setup. `release_gate.py` validates the referenced evidence and hashes rather than running the game. The historical companion evidence archive is available in repository releases; it is not included in a source-only clone.

The tools below retain narrower scopes:
validate_pack.py: source/data/schema/DAG/legacy-field consistency, Python AST and project XML only.
verify_manifest.py: delivered file hashes; legitimate future edits change the delivery manifest.
copy_content.py: safe generated-content copy/digest, dry-run by default; refuses unowned destinations.
build_move_tables.py: regenerate or check derived original action CSVs.
doctor.py: read-only local prerequisite/version discovery.
release_gate.py: fail-closed evidence inventory/integrity; it cannot prove honesty or human feel.
check_scaffold.sh / .ps1: packaging checks only.

The initial scaffold validators do not establish gameplay quality. Production simulation tests, native runs and direct review are separate. Some release/rebind helpers deliberately require exact historical binary hashes; consult [AUDIT_GUIDE.md](../AUDIT_GUIDE.md) before using them on a modified build. Never rewrite old results into a fresh PASS.
