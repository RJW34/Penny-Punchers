"""Bind catalog-v1 runtime actions to inspected existing cels, never fabricate art.

Run after the canonical buyable data compiler. Fail before writes if any required
move/cel is absent. Old action bindings remain untouched. All emitted holds use
canonical phase durations; feints copy the exact first six source ticks.
"""
import copy
import argparse
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]

# These are explicit, reviewed reuse decisions, not names guessed at runtime.
# Each tuple is startup / active / recovery. Commas mean successive key poses.
BINDINGS = {
    "rook": {
        "buy_r_s1_clinch": ("u_walk_forward,t_shop_clinch", "t_shop_clinch", "u_walk_back,u_idle"),
        "buy_r_s2_slipstream": ("u_crouch", "t_shop_low_drive", "u_landing,s_crouch_up"),
        "buy_r_s2_straight": ("u_crouch,n_close_mp", "n_s_mp", "n_s_mp,u_walk_back"),
        "buy_r_s2_upper": ("u_crouch,n_c_mp", "n_c_hp", "n_c_hp,s_crouch_up"),
        "buy_r_s3_chain1": ("u_walk_forward", "n_s_lp", "n_s_lp,u_block_stand"),
        "buy_r_s3_chain2": ("n_close_mp", "n_s_mp", "n_s_mp,u_block_stand"),
        "buy_r_s3_chain3": ("n_close_mp", "n_s_hp", "n_s_hp,u_walk_back,u_idle"),
        "buy_r_s4_counter": ("u_walk_back", "t_shop_sway_feint", "u_walk_back,u_idle"),
        "buy_r_s4_riposte": ("u_walk_back", "n_s_hp", "n_s_hp,u_walk_back,u_idle"),
        "buy_r_t1_high_hook": ("n_close_mp", "t_shop_high_hook", "u_block_stand,u_idle"),
        "buy_r_t2_low_turn": ("u_crouch", "t_shop_low_turn", "u_landing,s_crouch_up"),
        "buy_r_t3_rivet_lift": ("u_crouch,n_c_mp", "n_c_hp", "n_c_hp,s_crouch_up"),
        "buy_r_t4_tempered": ("u_block_stand,n_close_mp", "n_s_hp", "n_s_hp,u_walk_back,u_idle"),
        "buy_r_g1_false_start": ("u_idle", "u_idle", "t_shop_step_feint,u_idle"),
        "buy_r_g2_check_step": ("u_walk_back", "u_walk_back,s_walk_back_second_key", "s_walk_back_second_key,u_idle"),
        "buy_r_g3_vault_hop": ("s_jump_takeoff", "u_jump_ascent,u_jump_apex,s_jump_fall", "u_landing,u_idle"),
        "buy_r_g4_wire_cut": ("u_walk_back", "n_s_mp", "u_walk_back,u_idle"),
        "buy_r_a3_overtime": ("u_block_stand,s_taunt", "s_taunt", "u_idle"),
    },
    "vale": {
        "buy_v_s1_return_pulse": ("u_walk_back,t_shop_return_pulse", "t_pulse", "t_shop_return_pulse,u_idle"),
        "buy_v_s2_low_palm": ("u_crouch", "t_shop_low_palm", "u_land,s_crouch_up"),
        "buy_v_s3_arc_pulse": ("u_crouch,u_walk_forward", "t_pulse", "t_shop_return_pulse,u_idle"),
        "buy_v_s4_anchor": ("u_crouch,n_c_mp", "t_shop_return_pulse", "u_walk_back,u_idle"),
        "buy_v_t1_heel_arc": ("n_s_mk", "t_shop_heel_arc", "u_land,u_idle"),
        "buy_v_t2_needle_check": ("u_walk_forward,n_s_lk", "n_s_mk", "n_s_mk,u_walk_back,u_idle"),
        "buy_v_t3_descending_heel": ("u_jump_apex", "n_j_hp", "u_land,s_crouch_up,u_idle"),
        "buy_v_t4_skycatch": ("n_j_lp", "n_throw_forward", "u_land,u_idle"),
        "buy_v_g1_false_pulse": ("u_idle", "u_idle", "t_shop_sway_feint,u_idle"),
        "buy_v_g2_recoil_step": ("u_walk_back", "t_shop_sway_feint", "u_walk_back,u_idle"),
        "buy_v_g3_shear_palm": ("u_walk_back", "t_shop_long_check", "u_walk_back,u_idle"),
        "buy_v_g4_slipgate": ("u_walk_back", "t_shop_step_feint", "u_walk_back,u_idle"),
        "buy_v_a3_prism": ("u_walk_back,t_shop_return_pulse", "t_pulse", "t_shop_return_pulse,u_idle"),
    },
}


def phase_holds(cels, ticks, phase):
    names = cels.split(",")
    if ticks <= 0:
        return [{"cel": names[-1], "ticks": 1}]
    names = names[:ticks]
    # A two-tick recovery follow-through; the remainder is the explicit settle.
    if phase == "recovery" and len(names) > 1 and ticks >= len(names) + 2:
        holds = [{"cel": names[0], "ticks": 2}]
        names, ticks = names[1:], ticks - 2
    else:
        holds = []
    quotient, remainder = divmod(ticks, len(names))
    return holds + [{"cel": name, "ticks": quotient + (index < remainder)} for index, name in enumerate(names)]


def first_ticks(move_timing, move, count):
    result = []
    for phase in ("startup", "active", "recovery"):
        if move[phase] == 0:
            continue
        for hold in move_timing[phase]:
            take = min(count, hold["ticks"])
            if take:
                result.append({"cel": hold["cel"], "ticks": take})
                count -= take
            if count == 0:
                return result
    raise ValueError("Mimic source shorter than required shared anticipation")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--fighters", type=Path, default=ROOT / "data/rulesets/buyables_full/fighters", help="Canonical full catalog fighter definitions, or an explicitly selected compiler staging directory")
    args = parser.parse_args()
    timing_path = ROOT / "game/Presentation/animation-timing.json"
    impact_path = ROOT / "game/Presentation/impact-profiles.json"
    timings = json.loads(timing_path.read_text())
    impacts = json.loads(impact_path.read_text())
    register = {"schema_version": 1, "scope": "Catalog-v1 bindings reuse original After Hours poses. No new authored in-betweens, dedicated air-throw sheet, or material masks are claimed.", "source_inputs": [], "fighters": {}}
    for fighter, bindings in BINDINGS.items():
        data_path = args.fighters / f"{fighter}.json"
        data = json.loads(data_path.read_text())
        register["source_inputs"].append({"path": str(data_path.resolve().relative_to(ROOT)).replace("\\", "/"), "sha256": hashlib.sha256(data_path.read_bytes()).hexdigest()})
        moves = {move["id"]: move for move in data["moves"]}
        atlas = json.loads((ROOT / f"game/Assets/AfterHours/Fighters/{fighter.title()}/atlas.json").read_text())
        target = timings["fighters"][fighter]["moves"]
        register["fighters"][fighter] = {}
        for move_id, phases in bindings.items():
            move = moves[move_id]  # Fail closed; do not guess upcoming frame data.
            for cels in phases:
                for cel in cels.split(","):
                    assert cel in atlas["cells"], f"Unknown {fighter}/{cel}"
            entry = {phase: phase_holds(cels, move[phase], phase) for phase, cels in zip(("startup", "active", "recovery"), phases)}
            if mimic := move.get("mimic"):
                source = mimic["source_move_id"]
                assert move["startup"] == mimic["shared_ticks"]
                entry["startup"] = first_ticks(target[source], moves[source], mimic["shared_ticks"])
            target[move_id] = entry
            hitboxes = move.get("hitboxes", [])
            shot = move.get("projectile") or (move.get("object_rules") or {}).get("contact") or {}
            rank = max([h.get("rank", 1) for h in hitboxes] + [shot.get("rank", 1)])
            super_move = move["kind"] == "super"
            impacts["fighters"][fighter][move_id] = {"size": 185 if super_move else 156 if rank >= 3 else 116, "shake": 6 if super_move else 4.5 if rank >= 3 else 2.6, "sound": "super" if super_move else "heavy" if rank >= 3 else "hit", "contact_cel": 27 if super_move else 6 if rank >= 3 else 0, "frames": 3 if super_move else 6, "lifetime_ticks": 30 if super_move else 20 if rank >= 3 else 16}
            register["fighters"][fighter][move_id] = {"catalog_id": move.get("catalog_id", ""), "method": "Existing key-pose reuse with explicit tick holds; Core owns movement and contact timing.", "cels": sorted({hold["cel"] for phase in entry.values() for hold in phase}), "mimic": copy.deepcopy(move.get("mimic"))}
    for path, data in ((timing_path, timings), (impact_path, impacts), (ROOT / "assets/BUYABLE_PRESENTATION_BINDINGS.json", register)):
        path.write_text(json.dumps(data, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"passed": True, "bindings": sum(map(len, BINDINGS.values())), "scope": register["scope"]}))


if __name__ == "__main__":
    main()
