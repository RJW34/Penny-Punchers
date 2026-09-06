# Buyables design companion — actual validation

**DESIGN COMPANION VALIDATED. NOT A BUILT OR BALANCED GAME.**

## Checks executed

| Check | Actual result |
|---|---|
| Custom structural validator | PASS: 38 unique entries, required fields, references, migration IDs, per-character slots and trial pools |
| Python design/arithmetic tests | **32 passed, 0 failed** |
| Budget examples | 10 checked; arithmetic output under `budget_examples.json` |
| Native scenario inventory | 50 planned, **0 executed**; every result remains `NOT_RUN` |
| HTML reference browser | 11 checks passed in Chromium; filters, search, expansion, count and desktop/mobile overflow |
| HTML screenshots | Desktop and mobile rendering directly inspected; this is not a game screenshot review |
| Native C#/Godot build, combat, networking | **NOT RUN** |
| Human feel and competitive balance | **NOT RUN** |
| Repository / earlier audit changes | None |

The first file-URL navigation attempt was blocked by the host Chromium policy. The browser subsequently rendered the exact HTML bytes using Playwright `set_content`. That verifies reference-browser rendering and scripts, not the host's file-URL access. This delivery uses no web runtime dependency, external font or bundled proprietary asset.

The structure checks are a custom Python validator, not formal JSON Schema validation. The test suite validates design metadata and arithmetic, including cost boundaries, slot conflicts, none-as-a-valid-plan, one-wallet budgets, trial filtering, all ten examples, acyclic install routing, and the authored hop trajectory. These tests do **not** execute moves in the production combat engine.

## Reproduce

```text
python tools/validate_catalog.py
python -m unittest discover -s tests -v
python tools/budget_model.py --write-report reports/budget_examples.json
python tools/verify_manifest.py
```

Use Python 3.11 or later; these four commands require only the standard library. Optional HTML inspection needs a browser, not Godot.

## Completion meaning

The authored section is complete as a **candidate design specification**: 24 round rentals, 8 paid enhanced families, 6 selected supers, cost and lifecycle rules, per-card purpose/counterplay/presentation, migration mapping, and an implementation/acceptance plan. Eight further ideas are explicitly research-only and excluded from 38.

Several paid actions already exist in the audited baseline; the catalog's names/roles do not certify current behavior or imply they are all newly invented. Exact combat boxes, contact timing and native effects must still be authored or reconciled with current code, then tested. No new price, combo, on-block value, comeback probability or human preference is claimed proven.

`MANIFEST.json` records the final delivered bytes. The final ZIP is rehearsed by extraction to a clean directory, running the manifest checker, custom validator and all 32 tests. The separate delivery verification file records those actual archive results without causing a circular manifest.
