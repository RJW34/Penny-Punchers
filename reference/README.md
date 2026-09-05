# What this reference does and does not prove
`model.py` is a small independent Python arithmetic oracle: scalar wallets, payout tiers/caps, draft/atomic preparation, paid-startup spend receipts and snapshot-style restoration, singles scoring, signed damage/advantage math, direction mirroring and parry-coverage truth table. Unit tests include hand-specified vectors and randomized invariants.

It does NOT recognize complete gameplay inputs, simulate movement/collision/freeze, verify human execution, run Godot, handle actual sockets, authenticate players, record played matches, or prove balance. Production C# tests must compare these fixtures AND implement all actual game interactions. `command_cases.json` specifies production parser cases; merely reading that file is not a passing parser.

Run from root: `python -m unittest discover -s reference/tests -v`. Passing this suite never satisfies a game release on its own.
