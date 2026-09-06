"""Tests of auditor math/data only. No Godot or production C# gameplay is executed."""
from fractions import Fraction
from pathlib import Path
import json
import unittest
from economy_analysis import Rules, Wallet, settle, sequence, fair_match_win, fixed_p_win, feasible_budgets, source_identity, nominal_damage, equal_spend_reachability

ROOT=Path(__file__).resolve().parent
DATA=ROOT/'verified_source'/'data'
if not DATA.exists(): DATA=ROOT/'source'/'data'

class AuditChecks(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.rules=Rules.load(DATA)
        cls.items=json.loads((DATA/'items.json').read_text())['items']
        cls.fighters={fid:json.loads((DATA/'fighters'/f'{fid}.json').read_text()) for fid in ['rook','vale']}
        cls.combat=json.loads((DATA/'combat.json').read_text())
    def test_pinned_source_hashes(self):
        self.assertEqual(len(source_identity(DATA)),14)
    def test_baseline_rules(self):
        self.assertEqual(self.rules,Rules())
    def test_first_loss_uses_old_tier(self):
        w,r=settle(Wallet(0),0,'L');self.assertEqual((w.credits,w.tier,r['nominal']),(900,1,900))
    def test_second_loss_payout(self):
        w,r=settle(Wallet(900,1),900,'L');self.assertEqual((w.credits,w.tier),(1200,2))
    def test_third_loss_payout(self):
        w,r=settle(Wallet(1200,2),1200,'L');self.assertEqual((w.credits,w.tier),(1500,2))
    def test_win_decreases_tier_one_step(self):
        w,_=settle(Wallet(0,2),0,'W');self.assertEqual(w,Wallet(1200,1))
    def test_draw_does_not_change_tier(self):
        w,_=settle(Wallet(0,1),0,'D');self.assertEqual(w,Wallet(900,1))
    def test_clipping(self):
        w,r=settle(Wallet(3300),0,'W');self.assertEqual(w.credits,3600);self.assertEqual((r['granted'],r['clipped']),(300,900))
    def test_illegal_spends_rejected(self):
        for spend in [-1,601,True,0.5]:
            with self.assertRaises(ValueError):settle(Wallet(600),spend,'W')
    def test_first_round_equal_spend_advantage(self):
        for s in [0,300,600]:
            a,_=settle(Wallet(600),s,'W');b,_=settle(Wallet(600),s,'L');self.assertEqual(a.credits-b.credits,300)
    def test_first_round_all_pairs_decomposition(self):
        for sa in [0,300,600]:
            for sb in [0,300,600]:
                a,_=settle(Wallet(600),sa,'W');b,_=settle(Wallet(600),sb,'L');self.assertEqual(a.credits-b.credits,300+sb-sa)
    def test_first_round_extremes(self):
        a,_=settle(Wallet(600),0,'W');b,_=settle(Wallet(600),600,'L');self.assertEqual((a.credits,b.credits),(1800,900))
    def test_loser_can_have_more_money(self):
        a,_=settle(Wallet(600),600,'W');b,_=settle(Wallet(600),0,'L');self.assertGreater(b.credits,a.credits)
    def test_consecutive_loss_equal_spending_trace(self):
        rows=sequence('WWWW','equal_600');self.assertEqual([(r['A']['closing'],r['B']['closing']) for r in rows],[(1200,900),(1800,1500),(2400,2400),(3000,3300)])
    def test_match_stops_at_fifth_win(self):
        rows=sequence('WWWWWWWWW','save');self.assertEqual(len(rows),5);self.assertTrue(rows[-1]['match_over'])
    def test_nine_draw_limit(self):
        rows=sequence('DDDDDDDDDD','save');self.assertEqual(len(rows),9);self.assertEqual(rows[-1]['score_half_points'],[9,9])
    def test_reward_neutral_first_loss_variant(self):
        rows=sequence('W','equal_600',Rules(losses=(1200,1200,1500)));self.assertEqual(rows[0]['A']['closing'],rows[0]['B']['closing'])
    def test_equal_all_income_equal_spending(self):
        rows=sequence('WWLWLDWLD','equal_600',Rules(losses=(1200,1200,1200)));self.assertTrue(all(r['A_minus_B']==0 for r in rows))
    def test_first_win_score_only_probability(self):
        self.assertEqual(fair_match_win(1,0),Fraction(163,256))
    def test_three_zero_score_only_probability(self):
        self.assertEqual(1-fair_match_win(3,0),Fraction(7,64))
    def test_symmetric_score_probability(self):
        self.assertEqual(fair_match_win(1,1),Fraction(1,2))
    def test_sensitivity_reduces_to_fair(self):
        self.assertEqual(fixed_p_win(1,0,Fraction(1,2)),fair_match_win(1,0))
    def test_one_round_advantage_only(self):
        self.assertEqual(Fraction(11,20)*fair_match_win(2,0)+Fraction(9,20)*fair_match_win(1,1),Fraction(333,512))
    def test_item_count_and_no_global_defense_modification(self):
        self.assertEqual(len(self.items),12);self.assertTrue(all(not i['alters_system_defense'] for i in self.items))
    def test_item_expiry_and_free_repeat_activation(self):
        for i in self.items:
            self.assertEqual((i['duration'],i['activation_credit_cost'],i['use_limit']),('one_round',0,None))
    def test_base_kit_is_free(self):
        for f in self.fighters.values():
            for m in f['moves']:
                if m['availability']=='base' and m['kind'] not in ['ex_special','super']:self.assertEqual(m['credit_cost'],0)
    def test_affordability_no_rental_1200_vs_900(self):
        for c in [900,1200]:
            n=next(p for p in feasible_budgets(c,900,self.items,'rook') if not p['items'])
            self.assertEqual(n['max_ex_after_one_super'],(c-900)//300)
    def test_wallet_cap_is_not_the_loadout_cap(self):
        plans=feasible_budgets(1800,900,self.items,'rook')
        full=[p for p in plans if p['rental_cost']==1800]
        self.assertEqual(len(full),8);self.assertTrue(all(p['remaining']==0 for p in full))
    def test_number_of_affordable_plans(self):
        self.assertEqual([len(feasible_budgets(c,900,self.items,'rook')) for c in [600,900,1200,1500,1800]],[5,11,15,19,27])
    def test_long_check_tradeoff_not_strict_dominance_proof(self):
        m={m['id']:m for m in self.fighters['vale']['moves']};a=m['command_fhp'];b=m['shop_long_check']
        self.assertLess(a['startup'],b['startup']);self.assertGreater(a['hitboxes'][0]['damage'],b['hitboxes'][0]['damage']);self.assertLess(a['hitboxes'][0]['stun'],b['hitboxes'][0]['stun'])
    def test_returning_pulse_one_hit_not_two(self):
        m=next(m for m in self.fighters['vale']['moves'] if m['id']=='shop_return_pulse')
        self.assertEqual((m['projectile']['hits'],m['projectile']['max_active_per_owner']),(1,1));self.assertEqual(m['projectile']['turn_after_ticks'],45)
    def test_feint_motion_arithmetic(self):
        f=self.fighters['rook'];m=next(m for m in f['moves'] if m['id']=='shop_step_feint');travel=sum((x['end']-x['start'])*x['vx'] for x in m['movement'])
        self.assertEqual(travel,13600);self.assertEqual(sum(m[k] for k in ['startup','active','recovery'])*f['physics']['walk_forward'],48000)
    def test_super_cap_counts(self):
        self.assertEqual([3600//c for c in [900,1200,1500]],[4,3,2])
    def test_nominal_super_payload_is_not_monotone_price(self):
        m={m['id']:m for m in self.fighters['rook']['moves']}
        self.assertEqual([nominal_damage(m[x],self.combat) for x in ['super_1','super_2','super_3']],[240,266,202])
    def test_ledger_bounds_exhaustive_single_round(self):
        for c in range(0,3601,300):
            for t in range(3):
                for spend in range(0,c+1,300):
                    for outcome in 'WLD':
                        w,r=settle(Wallet(c,t),spend,outcome)
                        self.assertTrue(0<=w.credits<=3600 and 0<=w.tier<=2)
                        self.assertEqual(w.credits,c-spend+r['nominal']-r['clipped'])
    def test_equal_spend_global_gap_not_just_300(self):
        out=equal_spend_reachability(self.rules);self.assertEqual(out['maximum_absolute_wallet_gap'],900)
        self.assertEqual(out['terminal_state_count_across_rounds'],1186)
    def test_hypothetical_stat_breakpoints(self):
        import math
        self.assertEqual((math.ceil(1000/240),math.ceil(1000/264)),(5,4))
        self.assertEqual((math.ceil(1000/260),math.ceil(1100/260)),(4,5))

if __name__=='__main__':unittest.main(verbosity=2)
