"""Actual tests of proposal arithmetic/structure, not the native game."""
from __future__ import annotations
from collections import Counter
import json
from pathlib import Path
import sys
import unittest
ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "tools"))
from budget_model import read_catalog, quote, activation_budget, frontier, report
from validate_catalog import validate

class CatalogTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.c = read_catalog()
        cls.es = cls.c["entries"]
        cls.index = {e["id"]: e for e in cls.es}

    def test_complete_structural_validation(self):
        self.assertEqual(validate()["design_entries"], 38)

    def test_one_wallet_only(self):
        self.assertEqual(self.c["scope"]["spendable_resources"], ["credits"])
        self.assertFalse(self.c["scope"]["team_systems"])

    def test_no_stats_or_income_or_retained_power(self):
        self.assertTrue(all(not e["global_stat_modifiers"] and not e["combat_income"] and not e["retained_after_round"] for e in self.es))

    def test_rent_and_activation_not_double_tax(self):
        for e in self.es:
            p=e["pricing"]
            self.assertNotEqual(p["rental_credits"]>0,p["activation_credits"]>0)

    def test_zero_wallet_free_plan(self):
        q=quote(self.c,"rook",0,[],"R-A1")
        self.assertEqual(q["spendable"],0)
        self.assertFalse(q["can_afford_super"])

    def test_none_is_valid_at_every_wallet(self):
        for wallet in range(0,3601):
            self.assertEqual(quote(self.c,"rook",wallet,[],"R-A1")["remaining_wallet"],wallet)

    def test_opening_feint_leaves_ex(self):
        q=quote(self.c,"rook",600,["R-G1"],"R-A1")
        self.assertEqual(activation_budget(self.c,q,["R-E1"]),0)

    def test_opening_signature_leaves_free_kit_only(self):
        q=quote(self.c,"rook",600,["R-S2"],"R-A1")
        with self.assertRaises(ValueError): activation_budget(self.c,q,["R-E1"])

    def test_two_in_one_slot_rejected(self):
        with self.assertRaises(ValueError): quote(self.c,"rook",3600,["R-S1","R-S2"],"R-A1")

    def test_duplicate_rejected(self):
        with self.assertRaises(ValueError): quote(self.c,"rook",3600,["R-G1","R-G1"],"R-A1")

    def test_cross_character_purchase_rejected(self):
        with self.assertRaises(ValueError): quote(self.c,"rook",3600,["V-G1"],"R-A1")

    def test_ex_is_not_a_rental(self):
        with self.assertRaises(ValueError): quote(self.c,"rook",3600,["R-E1"],"R-A1")

    def test_unknown_id_rejected(self):
        with self.assertRaises(ValueError): quote(self.c,"rook",600,["invented"],"R-A1")

    def test_wrong_selected_art_rejected(self):
        with self.assertRaises(ValueError): quote(self.c,"rook",600,[],"V-A1")

    def test_unselected_art_activation_rejected(self):
        q=quote(self.c,"rook",3600,[],"R-A1")
        with self.assertRaises(ValueError): activation_budget(self.c,q,["R-A2"])

    def test_cross_character_activation_rejected(self):
        q=quote(self.c,"rook",3600,[],"R-A1")
        with self.assertRaises(ValueError): activation_budget(self.c,q,["V-E1"])

    def test_reserve_not_a_second_available_pool(self):
        q=quote(self.c,"rook",1200,[],"R-A1",900)
        self.assertEqual(q["spendable"],300)
        self.assertFalse(q["can_afford_super"])
        self.assertEqual(activation_budget(self.c,q,["R-E1"]),900)
        with self.assertRaises(ValueError): activation_budget(self.c,q,["R-A1"])

    def test_floor_above_remaining_rejected(self):
        with self.assertRaises(ValueError): quote(self.c,"rook",600,["R-G1"],"R-A1",600)

    def test_negative_and_noninteger_money_rejected(self):
        for w in (-1,3601,0.5,True,"600"):
            with self.assertRaises((ValueError,TypeError)): quote(self.c,"rook",w,[],"R-A1")

    def test_exact_rental_boundaries(self):
        for e in self.es:
            if e["kind"]!="round_lease": continue
            price=e["pricing"]["rental_credits"]
            art="R-A1" if e["fighter"]=="rook" else "V-A1"
            with self.assertRaises(ValueError): quote(self.c,e["fighter"],price-1,[e["id"]],art)
            self.assertEqual(quote(self.c,e["fighter"],price,[e["id"]],art)["remaining_wallet"],0)

    def test_exact_activation_boundaries(self):
        for e in self.es:
            if e["kind"]=="round_lease": continue
            price=e["pricing"]["activation_credits"]
            art=e["id"] if e["kind"]=="super_activation" else ("R-A1" if e["fighter"]=="rook" else "V-A1")
            q=quote(self.c,e["fighter"],price-1,[],art)
            with self.assertRaises(ValueError): activation_budget(self.c,q,[e["id"]])
            q=quote(self.c,e["fighter"],price,[],art)
            self.assertEqual(activation_budget(self.c,q,[e["id"]]),0)

    def test_all_example_plans_are_arithmetically_legal(self):
        r=report()
        self.assertEqual(len(r["presets"]),10)
        self.assertTrue(all(q["after_illustrative_activations"]>=q["reserve_floor"] for q in r["presets"]))

    def test_super_capacity(self):
        self.assertEqual(report()["super_capacity_without_rent_or_reserve"],{"900":{"activations":4,"cash_left":0},"1200":{"activations":3,"cash_left":0},"1500":{"activations":2,"cash_left":600}})

    def test_zero_wallet_frontier_one_plan(self):
        self.assertEqual(len(frontier(self.c,"rook",0)),1)
        self.assertEqual(len(frontier(self.c,"vale",0)),1)

    def test_core_frontier_at_cap_27(self):
        # None + two choices per slot, all obey the 1800 aggregate cap.
        for f in ("rook","vale"): self.assertEqual(len(frontier(self.c,f,3600,True)),27)

    def test_full_frontier_respects_rental_cap(self):
        for f in ("rook","vale"):
            qs=frontier(self.c,f,3600)
            self.assertGreater(len(qs),27)
            self.assertLessEqual(len(qs),125)
            self.assertTrue(all(q["rental_cost"]<=1800 for q in qs))

    def test_new_opening_choices_exist(self):
        for f in ("rook","vale"):
            self.assertGreater(len(frontier(self.c,f,600,True)),5)

    def test_complete_migration_has_all_old_rentals(self):
        m=json.loads((ROOT/"data/migration_map.json").read_text())["legacy_rentals"]
        old=json.loads((ROOT/"data/baseline_purchase_reference.json").read_text())["items"]
        self.assertEqual({x["legacy_item_id"] for x in m},{x["id"] for x in old})

    def test_no_untested_native_claims(self):
        self.assertTrue(all(e["native_scenario_result"] is None and e["measured_frame_advantage"] is None for e in self.es))
        s=json.loads((ROOT/"data/acceptance_scenarios.json").read_text())["scenarios"]
        self.assertTrue(all(x["status"]=="NOT_RUN" for x in s))

    def test_overtime_graph_documented_hit_only(self):
        text=(ROOT/"docs/COMBAT_AND_RESOURCE_CONTRACTS.md").read_text()
        self.assertIn("HIT-only",text)
        edges=[("s_lp","s_mp"),("s_lk","s_mk"),("c_lp","c_mp"),("c_lk","c_mk"),("s_mp","s_hp"),("s_mk","s_hk"),("c_mp","c_hp"),("c_mk","c_hk")]
        ranks={"l":0,"m":1,"h":2}
        for a,b in edges:
            self.assertIn(a+" -> "+b,text)
            self.assertLess(ranks[a[-2]],ranks[b[-2]])

    def test_hop_kinematic_seed(self):
        y=0;v=7600;maximum=0
        for _ in range(20): y+=v;v-=800;maximum=max(maximum,y)
        self.assertEqual(y,0)
        self.assertEqual(maximum,40000)

    def test_advanced_staging_not_silent_live_enable(self):
        self.assertEqual(self.index["R-A3"]["rollout"],"advanced_trial")
        self.assertEqual(self.index["V-A3"]["rollout"],"advanced_trial")
        self.assertEqual(sum(e["rollout"]=="advanced_trial" for e in self.es),14)

if __name__=="__main__": unittest.main()
