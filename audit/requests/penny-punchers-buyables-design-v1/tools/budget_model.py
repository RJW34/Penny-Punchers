"""Pure design-side budget arithmetic. Not the Penny-Punchers combat engine."""
from __future__ import annotations
import argparse
import itertools
import json
from pathlib import Path
from typing import Any

ROOT = Path(__file__).resolve().parents[1]


def read_catalog() -> dict[str, Any]:
    return json.loads((ROOT / "data/buyables_catalog.json").read_text(encoding="utf-8"))


def whole_nonnegative(value: int, name: str) -> None:
    if type(value) is not int or value < 0:
        raise ValueError(f"{name} must be a nonnegative integer")


def quote(catalog: dict[str, Any], fighter: str, wallet: int, item_ids: list[str],
          super_id: str, reserve: int = 0) -> dict[str, Any]:
    whole_nonnegative(wallet, "wallet")
    whole_nonnegative(reserve, "reserve")
    if wallet > 3600 or fighter not in ("rook", "vale"):
        raise ValueError("unsupported wallet or fighter")
    if not isinstance(item_ids, list) or not all(isinstance(i, str) for i in item_ids):
        raise ValueError("item IDs must be a list of strings")
    if len(item_ids) > 3 or len(set(item_ids)) != len(item_ids):
        raise ValueError("duplicate or excessive rentals")
    index = {e["id"]: e for e in catalog["entries"]}
    if super_id not in index:
        raise ValueError("unknown art")
    art = index[super_id]
    if art["kind"] != "super_activation" or art["fighter"] != fighter:
        raise ValueError("invalid selected art")
    cost = 0
    slots: set[str] = set()
    for item_id in item_ids:
        if item_id not in index:
            raise ValueError("unknown rental")
        item = index[item_id]
        if item["kind"] != "round_lease" or item["fighter"] != fighter:
            raise ValueError("ineligible rental")
        if item["slot"] in slots:
            raise ValueError("two choices in one slot")
        slots.add(item["slot"])
        cost += item["pricing"]["rental_credits"]
    if cost > 1800 or cost > wallet or reserve > wallet - cost:
        raise ValueError("plan exceeds rental cap, cash or protected floor")
    remaining = wallet - cost
    spendable = remaining - reserve
    return {
        "fighter": fighter, "opening_wallet": wallet, "rentals": item_ids,
        "rental_cost": cost, "remaining_wallet": remaining, "reserve_floor": reserve,
        "spendable": spendable, "selected_super": super_id,
        "super_cost": art["pricing"]["activation_credits"],
        "can_afford_super": spendable >= art["pricing"]["activation_credits"],
        "max_ex_instead": spendable // 300,
        "warning": "Affordability only: not prepaid uses, a legal combo or a winning strategy."
    }


def activation_budget(catalog: dict[str, Any], plan: dict[str, Any], action_ids: list[str]) -> int:
    """Check the arithmetic for sequential attempts, not combat-state legality."""
    index = {e["id"]: e for e in catalog["entries"]}
    credits = plan["remaining_wallet"]
    for action_id in action_ids:
        if action_id not in index:
            raise ValueError("unknown action")
        action = index[action_id]
        if action["fighter"] != plan["fighter"] or action["kind"] not in ("enhanced_activation", "super_activation"):
            raise ValueError("ineligible activation")
        if action["kind"] == "super_activation" and action_id != plan["selected_super"]:
            raise ValueError("unselected art")
        cost = action["pricing"]["activation_credits"]
        if credits - cost < plan["reserve_floor"]:
            raise ValueError("insufficient spendable money")
        credits -= cost
    return credits


def frontier(catalog: dict[str, Any], fighter: str, wallet: int, core_only: bool = False) -> list[dict[str, Any]]:
    groups = []
    for slot in ("signature", "technique", "gambit"):
        group = [e["id"] for e in catalog["entries"] if e["kind"] == "round_lease" and e["fighter"] == fighter and e["slot"] == slot and (not core_only or e["rollout"] == "core_trial")]
        groups.append([None] + group)
    result = []
    art = "R-A1" if fighter == "rook" else "V-A1"
    for choices in itertools.product(*groups):
        try:
            result.append(quote(catalog, fighter, wallet, [i for i in choices if i], art))
        except ValueError:
            pass
    return result


def report() -> dict[str, Any]:
    catalog = read_catalog()
    presets = json.loads((ROOT / "data/example_plans.json").read_text(encoding="utf-8"))["plans"]
    plans = []
    for p in presets:
        q = quote(catalog, p["fighter"], p["wallet"], p["rentals"], p["super"], p["reserve"])
        q.update({"id": p["id"], "intent": p["intent"], "illustrative_activations": p["activation_plan"],
                  "after_illustrative_activations": activation_budget(catalog, q, p["activation_plan"])})
        plans.append(q)
    return {
        "scope": "ACTUALLY_EXECUTED_DESIGN_ARITHMETIC_NOT_COMBAT",
        "presets": plans,
        "frontier_counts": [{"fighter": f, "wallet": w, "all_library": len(frontier(catalog, f, w)),
                             "core_trial": len(frontier(catalog, f, w, True))}
                            for f in ("rook", "vale") for w in (0, 300, 600, 900, 1200, 1800, 2400, 3600)],
        "super_capacity_without_rent_or_reserve": {str(p): {"activations": 3600 // p, "cash_left": 3600 % p} for p in (900, 1200, 1500)},
        "limitations": ["No engine was run.", "Fields and installations impose normal move-state constraints not modeled here.", "Counts are affordable loadouts, not viable strategies."]
    }


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--write-report", type=Path)
    args = parser.parse_args()
    data = report()
    text = json.dumps(data, indent=2)
    if args.write_report:
        args.write_report.parent.mkdir(parents=True, exist_ok=True)
        args.write_report.write_text(text + "\n", encoding="utf-8")
        print(f"Wrote design arithmetic report: {args.write_report}")
    else:
        print(text)
