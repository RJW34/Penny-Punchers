"""Validate this design companion. Does not validate production combat behavior."""
from __future__ import annotations
from collections import Counter
from pathlib import Path
import json
import sys
from budget_model import quote, activation_budget

ROOT = Path(__file__).resolve().parents[1]


def validate(root: Path = ROOT) -> dict:
    c = json.loads((root / "data/buyables_catalog.json").read_text(encoding="utf-8"))
    es = c["entries"]
    ids = {e["id"] for e in es}
    assert len(ids) == len(es) == 38, "unique catalog identity/count"
    assert Counter(e["kind"] for e in es) == {"round_lease": 24, "enhanced_activation": 8, "super_activation": 6}
    assert Counter(e["rollout"] for e in es) == {"core_trial": 24, "advanced_trial": 14}
    refs = {s["id"] for s in json.loads((root / "data/research_sources.json").read_text())["sources"]}
    for e in es:
        assert e["fighter"] in ("rook", "vale")
        assert e["validation_status"] == "DESIGN_PROPOSAL_NOT_NATIVE_VERIFIED"
        assert e["measured_frame_advantage"] is None and e["native_scenario_result"] is None
        assert not e["global_stat_modifiers"] and e["combat_income"] == 0 and not e["retained_after_round"]
        assert e["activation_count_limit"] is None
        p = e["pricing"]
        for key in ("rental_credits", "activation_credits"):
            assert type(p[key]) is int and p[key] >= 0 and p[key] % 300 == 0
        assert bool(p["rental_credits"]) != bool(p["activation_credits"]), "never both unlock and use fee"
        assert set(e["reference_ids"]) <= refs
        for key in ("name", "role", "command", "problem_solved", "mechanic_contract", "free_kit_comparison", "presentation_contract"):
            assert isinstance(e[key], str) and e[key].strip(), (e["id"], key)
        assert len(e["counterplay"]) >= 2 and len(e["specific_acceptance"]) >= 3
        for k in ("startup_ticks", "active_ticks", "recovery_ticks"):
            assert type(e["tuning_seed"][k]) is int and e["tuning_seed"][k] >= 0
        if e["kind"] == "round_lease":
            assert e["slot"] in ("signature", "technique", "gambit")
            assert p["activation_credits"] == 0
        else:
            assert e["slot"] is None and p["rental_credits"] == 0
    for f in ("rook", "vale"):
        assert sum(e["fighter"] == f and e["kind"] == "super_activation" for e in es) == 3
        assert sum(e["fighter"] == f and e["kind"] == "enhanced_activation" for e in es) == 4
        for slot in ("signature", "technique", "gambit"):
            ss = [e for e in es if e["fighter"] == f and e["slot"] == slot]
            assert len(ss) == 4 and sum(e["rollout"] == "core_trial" for e in ss) == 2
    for p in json.loads((root / "data/example_plans.json").read_text())["plans"]:
        q = quote(c, p["fighter"], p["wallet"], p["rentals"], p["super"], p["reserve"])
        activation_budget(c, q, p["activation_plan"])
    scenarios = json.loads((root / "data/acceptance_scenarios.json").read_text())["scenarios"]
    assert len({s["id"] for s in scenarios}) == len(scenarios) == 50
    assert all(s["status"] == "NOT_RUN" for s in scenarios)
    assert {s["catalog_entry"] for s in scenarios if "catalog_entry" in s} == ids
    migration = json.loads((root / "data/migration_map.json").read_text())
    assert len(migration["legacy_rentals"]) == 12
    assert len({m["legacy_item_id"] for m in migration["legacy_rentals"]}) == 12
    assert all(m["new_catalog_id"] in ids for m in migration["legacy_rentals"])
    future = json.loads((root / "data/future_research_candidates.json").read_text())
    assert len(future["candidates"]) == 8 and not future["included_in_38_entry_catalog"]
    for path in ("BUYABLES_DESIGN.md", "INTEGRATION_HANDOFF.md", "CATALOG.html", "docs/COMBAT_AND_RESOURCE_CONTRACTS.md", "docs/BALANCE_AND_RELEASE_GATES.md"):
        assert (root / path).is_file(), path
    book = (root / "BUYABLES_DESIGN.md").read_text()
    assert all(f"{e['id']} — {e['name']}" in book for e in es)
    return {"design_entries": len(es), "entry_and_system_scenario_plans": len(scenarios), "status": "DESIGN_STRUCTURE_PASS", "native_game_test": "NOT_RUN"}


if __name__ == "__main__":
    try:
        print(json.dumps(validate(), indent=2))
    except (AssertionError, KeyError, ValueError, OSError) as error:
        print(f"FAIL: {error}", file=sys.stderr)
        sys.exit(1)
