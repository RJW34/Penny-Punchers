#!/usr/bin/env sh
set -eu
cd "$(dirname "$0")/.."
python tools/validate_pack.py --strict-schema
python -m unittest discover -s reference/tests -v
python tools/build_move_tables.py --check
python tools/verify_manifest.py
