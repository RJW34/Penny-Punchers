# Penny-Punchers — buyables design companion

**Design proposal v1 · 5 September 2026. Not a game patch or balance certification.**

This companion completes the proposed buyables section while the expanded audit is being implemented. It does not overwrite the audit, renumber PP-001–PP-091, change the payouts, modify the remote repository, or certify untested moves. Read `BUYABLES_DESIGN.md` first; every entry also exists in `data/buyables_catalog.json` and the searchable `CATALOG.html`.

The complete candidate library has **38 entries: 24 round-long techniques, 8 enhanced paid activations and 6 selected supers**. All content is character-specific. Three optional rental slots remain, but price belongs to the individual item rather than being hardcoded by slot. Twelve rentals, eight EX families and four conventional arts form the first controlled trial. Twelve additional rentals and two mechanically different supers are fully specified advanced candidates, not a reason to dump all 38 choices into an untested build at once.

One wallet pays everything. Baseline attacks and defense stay free. An EX costs credits at startup, not an unlock fee. Selecting one super for the match is free; using it costs credits. A round technique costs once at preparation lock and then has no activation fee. There are no global stat products, super tickets, team finances or earned combat gauges.

## Read order

`BUYABLES_DESIGN.md` → `docs/COMBAT_AND_RESOURCE_CONTRACTS.md` → `docs/SHOP_AND_PRESENTATION.md` → `docs/BALANCE_AND_RELEASE_GATES.md` → `INTEGRATION_HANDOFF.md`.

The earlier 91-item audit remains the repair backlog. This is a separate design proposal. An integrator must reconcile it against the latest working tree and recorded decisions, not reset work to the cited baseline. Preserve tested old supers until replacements are truly implemented and verified. Do not copy the catalog JSON over the game's current `data/items.json`: it is a design schema, not that runtime schema.

## Reproduce companion checks

From this directory, Python 3.11+ (standard library only):

```text
python tools/validate_catalog.py
python -m unittest discover -s tests -v
python tools/budget_model.py --write-report reports/budget_examples.json
python tools/verify_manifest.py
```

These commands validate this companion's structure, budget arithmetic and documentation inventory. They do not compile or execute Penny-Punchers, prove a combo, establish frame advantage, or measure player preferences. See `reports/VALIDATION.md` for the actual results recorded during delivery.
