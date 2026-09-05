# Tool boundaries
validate_pack.py: source/data/schema/DAG/legacy-field consistency, Python AST and project XML only.
verify_manifest.py: delivered file hashes; legitimate future edits change the delivery manifest.
copy_content.py: safe generated-content copy/digest, dry-run by default; refuses unowned destinations.
build_move_tables.py: regenerate or check derived original action CSVs.
doctor.py: read-only local prerequisite/version discovery.
release_gate.py: fail-closed evidence inventory/integrity; it cannot prove honesty or human feel.
check_scaffold.sh / .ps1: packaging checks only.

Nothing here makes the scaffold a built game. The initial evidence gate must fail. Real implementation must add actual core/Godot/process tests and candidate evidence, not rename these tools as gameplay verification.
