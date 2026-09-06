import unittest,sys,json,random,copy
from pathlib import Path
from dataclasses import replace
ROOT=Path(__file__).resolve().parents[1];sys.path.insert(0,str(ROOT))
from reference.model import Model,Plan,Fact,classify,load_inputs

def hit(n=1,seat=0,**kw):
    return Fact(f'h{seat}-{n}',n,seat,f'attack-{seat}-{n}',seat,seat,**kw)
def parry(n=1,seat=0,**kw):
    options=dict(outcome='parry',damage=0,fresh_manual_parry=True,parry_age=0)
    options.update(kw)
    return Fact(f'p{seat}-{n}',n,seat,f'enemy-{1-seat}-{n}',1-seat,1-seat,**options)
def funded(bank=3600,fighters=('rook','vale')):
    m=Model(fighters=fighters);m.bank=[bank,bank];return m
class OwnershipTests(unittest.TestCase):
    def test_zero_bank_ex_repeats(self):
        m=Model();m.commit(Plan(('rook_pulse_ex_license',)))
        for i in range(10):self.assertEqual(m.activate(0,'pulse_ex',f'a{i}'),'licensed')
        self.assertEqual(m.bank[0],0)
    def test_rich_unowned_ex_locked(self):
        m=funded();m.commit();self.assertEqual(m.activate(0,'pulse_ex','a'),'not_owned');self.assertEqual(m.bank[0],3600)
    def test_wrong_ex_family_locked(self):
        m=Model();m.commit(Plan(('rook_pulse_ex_license',)));self.assertEqual(m.activate(0,'rise_ex','a'),'not_owned')
    def test_free_kit_zero_bank(self):
        m=funded(0);m.commit()
        for s in (0,1):
            for move in m.base[m.fighters[s]]:self.assertEqual(m.activate(s,move,f'{s}-{move}'),'base')
        self.assertEqual(m.bank,[0,0])
    def test_super_one_use(self):
        m=funded(900);m.commit(Plan(('rook_super_1_permit',)))
        self.assertEqual(m.activate(0,'super_1','a'),'super_use');self.assertEqual(m.activate(0,'super_1','b'),'exhausted');self.assertEqual(m.bank[0],0)
    def test_super_wrong_art_locked(self):
        m=funded(900);m.commit(Plan(('rook_super_1_permit',)));self.assertEqual(m.activate(0,'super_2','a'),'not_owned')
    def test_super_failed_legality_keeps_use(self):
        m=funded(900);m.commit(Plan(('rook_super_1_permit',)));self.assertEqual(m.activate(0,'super_1','a',False),'illegal');self.assertEqual(m.super_uses[0],1)
    def test_super_failed_after_start_no_refund(self):
        m=funded(900);m.commit(Plan(('rook_super_1_permit',)));m.activate(0,'super_1','a');self.assertEqual(m.super_uses[0],0);self.assertEqual(m.bank[0],0)
    def test_duplicate_start_no_second_use(self):
        m=funded(900);m.commit(Plan(('rook_super_1_permit',)));m.activate(0,'super_1','a');self.assertEqual(m.activate(0,'super_1','a'),'duplicate')
    def test_conflicting_start_rejected(self):
        m=funded(900);m.commit(Plan(('rook_super_1_permit',)));m.activate(0,'super_1','a')
        with self.assertRaises(ValueError):m.activate(0,'s_lp','a')
    def test_expiry_both_seats(self):
        m=funded(900);m.commit(Plan(('rook_super_1_permit',)),Plan(('vale_pulse_ex_license',)));m.mark_terminal(0,3);m.settle(3);old=m.bank.copy();m.next_round();self.assertEqual(m.owned,[{},{}]);self.assertEqual(m.super_uses,[0,0]);self.assertEqual(m.bank,old)
    def test_art_changes_next_round(self):
        m=funded(1200);m.commit(Plan(('rook_super_1_permit',)));m.mark_terminal(0,3);m.settle(3);m.next_round();m.commit(Plan(('rook_super_2_permit',)));self.assertEqual(m.activate(0,'super_2','a'),'super_use');self.assertEqual(m.activate(0,'super_1','b'),'not_owned')
    def test_replace_original_move(self):
        m=Model();m.commit(Plan(('rook_high_hook',)));self.assertEqual(m.activate(0,'command_fhp','a'),'replaced');self.assertEqual(m.activate(0,'shop_high_hook','b'),'licensed')
    def test_pre_fight_activation_illegal(self):self.assertEqual(Model().activate(0,'s_lp','a'),'illegal')
    def test_licenses_independent_of_earnings(self):
        m=funded(0);m.commit();m.reward(hit(defender_offensive_startup=True));self.assertEqual(m.activate(0,'pulse_ex','a'),'not_owned');self.assertEqual(m.bank[0],0)
class CartTests(unittest.TestCase):
    def test_invalid_other_player_atomic(self):
        m=Model();before=m.snapshot()
        with self.assertRaises(ValueError):m.commit(Plan(('rook_pulse_ex_license',)),Plan(('vale_super_3_permit',)))
        self.assertEqual(m.snapshot(),before)
    def test_duplicate_product(self):
        m=funded()
        with self.assertRaises(ValueError):m.commit(Plan(('rook_pulse_ex_license',)*2))
    def test_wrong_fighter(self):
        with self.assertRaises(ValueError):Model().commit(Plan(('vale_pulse_ex_license',)))
    def test_unknown_product(self):
        with self.assertRaises(ValueError):Model().commit(Plan(('unknown',)))
    def test_two_ex_allowed(self):
        m=funded();m.commit(Plan(('rook_pulse_ex_license','rook_knee_ex_license')));self.assertEqual(len(m.owned[0]),2)
    def test_three_ex_rejected(self):
        with self.assertRaises(ValueError):funded().commit(Plan(('rook_pulse_ex_license','rook_knee_ex_license','rook_sway_ex_license')))
    def test_two_supers_rejected(self):
        with self.assertRaises(ValueError):funded().commit(Plan(('rook_super_1_permit','rook_super_2_permit')))
    def test_same_technique_slot_rejected(self):
        with self.assertRaises(ValueError):funded().commit(Plan(('rook_high_hook','rook_low_turn')))
    def test_spend_cap(self):
        with self.assertRaises(ValueError):funded().commit(Plan(('rook_clinch','rook_rise_ex_license','rook_super_1_permit')))
    def test_exact_price(self):
        m=funded(600);m.commit(Plan(('rook_pulse_ex_license',)));self.assertEqual(m.bank[0],0)
    def test_below_price(self):
        with self.assertRaises(ValueError):funded(599).commit(Plan(('rook_pulse_ex_license',)))
    def test_duplicate_commit(self):
        m=Model();a=Plan(('rook_pulse_ex_license',));first=m.commit(a);self.assertEqual(m.commit(a),first);self.assertEqual(m.bank[0],0)
    def test_changed_commit(self):
        m=Model();m.commit()
        with self.assertRaises(ValueError):m.commit(Plan(('rook_pulse_ex_license',)))
    def test_order_independent_commit(self):
        m=funded();a=('rook_pulse_ex_license','rook_knee_ex_license');first=m.commit(Plan(a));self.assertEqual(m.commit(Plan(tuple(reversed(a)))),first)
    def test_no_buy_valid(self):
        m=Model();m.commit();self.assertEqual(m.bank,[600,600]);self.assertEqual(m.owned,[{},{}])
class RewardTests(unittest.TestCase):
    def setUp(self):self.m=Model();self.m.commit()
    def test_counter(self):self.assertEqual(self.m.reward(hit(defender_offensive_startup=True))['amount'],50)
    def test_non_counter(self):self.assertEqual(self.m.reward(hit())['amount'],0)
    def test_anti_air(self):self.assertEqual(self.m.reward(hit(defender_airborne=True,defender_voluntary_air=True))['amount'],75)
    def test_overlap_max_only(self):self.assertEqual(self.m.reward(hit(defender_airborne=True,defender_voluntary_air=True,defender_offensive_startup=True))['amount'],75)
    def test_juggle_excluded(self):self.assertEqual(self.m.reward(hit(defender_airborne=True,defender_in_combo=True,defender_offensive_startup=True))['amount'],0)
    def test_disabled_excluded(self):self.assertEqual(self.m.reward(hit(defender_offensive_startup=True,defender_disabled=True))['amount'],0)
    def test_launch_is_not_anti_air(self):self.assertEqual(self.m.reward(hit(defender_airborne=False,defender_voluntary_air=False))['amount'],0)
    def test_air_to_air_excluded(self):self.assertEqual(self.m.reward(hit(attacker_grounded=False,defender_airborne=True,defender_voluntary_air=True))['amount'],0)
    def test_prejump_attacker_excluded(self):self.assertEqual(self.m.reward(hit(attacker_prejump=True,defender_airborne=True,defender_voluntary_air=True))['amount'],0)
    def test_projectile_ch_and_aa_excluded(self):self.assertEqual(self.m.reward(hit(attack_kind='projectile',defender_offensive_startup=True,defender_airborne=True,defender_voluntary_air=True))['amount'],0)
    def test_damage_zero_excluded(self):self.assertEqual(self.m.reward(hit(damage=0,defender_offensive_startup=True))['amount'],0)
    def test_discarded_contact_excluded(self):self.assertEqual(self.m.reward(hit(accepted=False,defender_offensive_startup=True))['amount'],0)
    def test_block_excluded(self):self.assertEqual(self.m.reward(hit(outcome='block',defender_offensive_startup=True))['amount'],0)
    def test_perfect_age_zero(self):self.assertEqual(self.m.reward(parry())['amount'],100)
    def test_perfect_age_one(self):self.assertEqual(self.m.reward(parry(parry_age=1))['amount'],100)
    def test_normal_age_two(self):self.assertEqual(self.m.reward(parry(parry_age=2))['amount'],0)
    def test_negative_age(self):self.assertEqual(self.m.reward(parry(parry_age=-1))['amount'],0)
    def test_frozen_edge_excluded(self):self.assertEqual(self.m.reward(parry(parry_edge_frozen=True))['amount'],0)
    def test_auto_counter_excluded(self):self.assertEqual(self.m.reward(parry(fresh_manual_parry=False))['amount'],0)
    def test_non_damaging_fake_excluded(self):self.assertEqual(self.m.reward(parry(threat_damage=0))['amount'],0)
    def test_enemy_projectile_parry(self):self.assertEqual(self.m.reward(parry(attack_kind='projectile'))['amount'],100)
    def test_own_reflected_projectile(self):self.assertEqual(self.m.reward(replace(parry(attack_kind='projectile'),origin_owner=0))['amount'],0)
    def test_multihit_root_once(self):
        p=parry();self.m.reward(p);self.assertEqual(self.m.reward(replace(p,event_id='p2',tick=2,contact_ordinal=1))['amount'],0)
    def test_duplicate_event(self):
        p=parry();self.m.reward(p);self.assertEqual(self.m.reward(p)['reason'],'duplicate_event');self.assertEqual(self.m.ledger[0].pending,100)
    def test_conflicting_event(self):
        p=parry();self.m.reward(p)
        with self.assertRaises(ValueError):self.m.reward(replace(p,parry_age=1))
    def test_later_offense_root_not_upgrade(self):
        h=hit(defender_offensive_startup=True);self.m.reward(h)
        self.assertEqual(self.m.reward(replace(h,tick=2,event_id='h2',defender_airborne=True,defender_voluntary_air=True))['amount'],0)
    def test_pertype_cap(self):
        for n in range(1,4):self.m.reward(hit(n,defender_offensive_startup=True))
        self.assertEqual(self.m.ledger[0].pending,100)
    def test_partial_global_cap(self):
        for n in (1,2):self.m.reward(hit(n,defender_offensive_startup=True))
        self.m.reward(hit(3,defender_airborne=True,defender_voluntary_air=True));self.m.reward(parry(4));result=self.m.reward(parry(5))
        self.assertEqual(result['amount'],25);self.assertEqual(result['nominal'],100);self.assertEqual(self.m.ledger[0].pending,300)
    def test_highest_capped_no_fallback(self):
        for n in (1,2,3):self.m.reward(hit(n,defender_airborne=True,defender_voluntary_air=True,defender_offensive_startup=True))
        self.assertEqual(self.m.ledger[0].pending,150);self.assertEqual(self.m.ledger[0].counts['counter_hit'],0)
    def test_separate_player_caps(self):
        for s in (0,1):self.m.reward(parry(1,s))
        self.assertEqual([l.pending for l in self.m.ledger],[100,100])
    def test_training_no_income(self):self.assertEqual(self.m.reward(hit(training=True,defender_offensive_startup=True))['amount'],0)
    def test_bank_frozen_with_rewards(self):
        before=self.m.bank.copy();self.m.reward(parry());self.assertEqual(self.m.bank,before)
    def test_wrong_order_rejected(self):
        self.m.reward(parry(2))
        with self.assertRaises(ValueError):self.m.reward(parry(1))
    def test_after_terminal_no_income(self):
        self.m.mark_terminal(0,5);self.assertEqual(self.m.reward(parry(6))['amount'],0)
    def test_lethal_tick_can_earn(self):
        self.m.reward(hit(5,defender_offensive_startup=True));self.m.mark_terminal(0,5);self.m.settle(5);self.assertEqual(self.m.bank[0],1850)
class SettlementTests(unittest.TestCase):
    def test_unconfirmed_blocked(self):
        m=Model();m.commit();m.mark_terminal(0,10)
        with self.assertRaises(ValueError):m.settle(9)
        self.assertEqual(m.bank,[600,600])
    def test_bonus_settles_between_rounds(self):
        m=Model();m.commit();m.reward(parry());m.mark_terminal(0,4);m.settle(4);self.assertEqual(m.bank,[1900,1800])
    def test_base_then_bonus_clipping(self):
        m=funded(2500);m.commit();m.reward(parry());m.mark_terminal(0,4);r=m.settle(4)['rows'][0]
        self.assertEqual((r['base_granted'],r['skill_granted'],r['clipped'],r['closing']),(1100,0,200,3600))
    def test_duplicate_settlement(self):
        m=Model();m.commit();m.mark_terminal(0,4);a=m.settle(4);b=m.settle(4);self.assertEqual(a,b);self.assertEqual(m.bank,[1800,1800])
    def test_conflicting_settlement_id(self):
        m=Model();m.commit();m.mark_terminal(0,4);m.settle(4)
        with self.assertRaises(ValueError):m.settle(4,'changed')
    def test_draw_income_and_points(self):
        m=Model();m.commit();m.reward(parry());m.mark_terminal(-1,4);m.settle(4);self.assertEqual(m.bank,[1600,1500]);self.assertEqual(m.score,[1,1]);self.assertEqual(m.tiers,[0,0])
    def test_tier_old_value(self):
        m=funded(0)
        for round,n in enumerate((1200,1200,1500),1):
            m.bank=[0,0];m.commit();m.mark_terminal(0,4);r=m.settle(4);self.assertEqual(r['rows'][1]['base_nominal'],n)
            if round<3:m.next_round()
    def test_equal_spend_no_initial_payout_gap(self):
        m=Model();m.commit(Plan(('rook_pulse_ex_license',)),Plan(('vale_pulse_ex_license',)));m.mark_terminal(0,4);m.settle(4);self.assertEqual(m.bank,[1200,1200])
    def test_five_wins_end(self):
        m=Model()
        for i in range(5):
            m.commit();m.mark_terminal(0,4);m.settle(4)
            if i<4:m.next_round()
        self.assertEqual(m.phase,'match_over')
        with self.assertRaises(ValueError):m.next_round()
    def test_nine_draws_end(self):
        m=Model()
        for i in range(9):
            m.commit();m.mark_terminal(-1,4);m.settle(4)
            if i<8:m.next_round()
        self.assertEqual(m.phase,'match_over');self.assertEqual(m.score,[9,9])
    def test_new_match_reset(self):
        m=funded();fresh=Model();self.assertEqual(fresh.bank,[600,600]);self.assertNotEqual(m.bank,fresh.bank)
class SnapshotTests(unittest.TestCase):
    def test_roundtrip(self):
        m=funded();m.commit(Plan(('rook_super_1_permit','rook_pulse_ex_license')));m.reward(parry());before=m.hash();s=m.snapshot();m.activate(0,'super_1','a');m.restore(s);self.assertEqual(m.hash(),before)
    def test_false_reward_removed(self):
        m=Model();m.commit();s=m.snapshot();m.reward(parry());m.restore(s);self.assertEqual(m.ledger[0].pending,0);m.reward(hit(defender_offensive_startup=True));self.assertEqual(m.ledger[0].pending,50)
    def test_false_super_use_removed(self):
        m=funded(900);m.commit(Plan(('rook_super_1_permit',)));s=m.snapshot();m.activate(0,'super_1','a');m.restore(s);self.assertEqual(m.super_uses[0],1);self.assertEqual(m.bank[0],0)
    def test_fresh_replay_exact(self):
        m=Model();m.commit();m.reward(parry());m.mark_terminal(0,4);m.settle(4);s=m.snapshot();other=Model();other.restore(s);self.assertEqual(other.hash(),m.hash());self.assertEqual(other.settle(4),m.settlement_receipt)
    def test_wrong_identity_rejected(self):
        m=Model();s=m.snapshot();r,c,b=load_inputs();r['win_payout']=1300
        with self.assertRaises(ValueError):Model(r,c,b).restore(s)
    def test_wrong_session_rejected(self):
        with self.assertRaises(ValueError):Model(session='other').restore(Model().snapshot())
    def test_randomized_bank_invariant_and_reward_caps(self):
        rng=random.Random(2002)
        for run in range(150):
            m=funded(rng.randrange(0,3601));m.commit();bank=m.bank.copy()
            for tick in range(1,81):
                s=rng.randrange(2);kind=rng.randrange(4)
                f=hit(tick,s,defender_offensive_startup=kind==0,defender_airborne=kind==1,defender_voluntary_air=kind==1) if kind<3 else parry(tick,s)
                m.reward(f);m.activate(s,'s_lp',f'{s}/{tick}')
                self.assertEqual(m.bank,bank);self.assertTrue(all(0<=l.pending<=300 for l in m.ledger))
                self.assertTrue(all(n<=2 for l in m.ledger for n in l.counts.values()))
            m.mark_terminal(rng.choice([-1,0,1]),81);m.settle(81);self.assertTrue(all(0<=b<=3600 for b in m.bank))
if __name__=='__main__':unittest.main()
