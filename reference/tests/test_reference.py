"""Oracle unit tests, NOT evidence that the fighting game is implemented."""
import unittest,json,random,dataclasses
from pathlib import Path
from reference.model import *

class WalletTests(unittest.TestCase):
    def test_default(self):self.assertEqual(Account(),Account(600,0))
    def test_negative(self):
        with self.assertRaises(ValueError):Account(-1)
    def test_over_cap(self):
        with self.assertRaises(ValueError):Account(3601)
    def test_bool(self):
        with self.assertRaises(ValueError):Account(True)
    def test_float(self):
        with self.assertRaises(ValueError):Account(300.0)
    def test_bad_tier(self):
        with self.assertRaises(ValueError):Account(0,3)
    def test_immutable(self):
        with self.assertRaises(dataclasses.FrozenInstanceError):Account().credits=1000
    def test_only_scalar_balance(self):self.assertEqual({f.name for f in dataclasses.fields(Account)},{'credits','recovery_tier'})
    def test_vectors(self):
        for v in json.loads((ROOT/'fixtures/economy_vectors.json').read_text())['payouts']:
            with self.subTest(v=v):
                a,r=payout(Account(**v['before']),v['outcome']);self.assertEqual(asdict(a),v['expected']);self.assertEqual(r['clipped'],v['clipped'])
    def test_three_losses(self):
        a=Account(0)
        for expected in [900,2100,3600]:a,_=payout(a,'loss');self.assertEqual(a.credits,expected)
    def test_win_decreases_one(self):self.assertEqual(payout(Account(0,2),'win')[0].recovery_tier,1)
    def test_draw_preserves_tier(self):self.assertEqual(payout(Account(0,2),'draw')[0].recovery_tier,2)
    def test_cap_receipt(self):self.assertEqual(payout(Account(3600),'win')[1]['clipped'],1200)
    def test_unknown_outcome(self):
        with self.assertRaises(ValueError):payout(Account(),'parry')
    def test_random_payout_invariants(self):
        rng=random.Random(617)
        for _ in range(10000):
            a=Account(rng.randrange(3601),rng.randrange(3));o=rng.choice(['win','loss','draw']);b,r=payout(a,o)
            self.assertEqual(b.credits-a.credits,r['granted']);self.assertEqual(r['granted']+r['clipped'],r['nominal']);self.assertLessEqual(b.credits,3600)

class PlanTests(unittest.TestCase):
    def test_empty(self):self.assertEqual(quote_plan(Account(),Plan('rook'))['credits_after'],600)
    def test_technique(self):self.assertEqual(quote_plan(Account(),Plan('rook',('rook_high_hook',)))['credits_after'],0)
    def test_full(self):self.assertEqual(quote_plan(Account(1800),Plan('rook',('rook_clinch','rook_high_hook','rook_step_feint')))['cost'],1800)
    def test_wrong_character(self):
        with self.assertRaises(ValueError):quote_plan(Account(3600),Plan('vale',('rook_clinch',)))
    def test_duplicate_slot(self):
        with self.assertRaises(ValueError):quote_plan(Account(3600),Plan('rook',('rook_clinch','rook_low_drive')))
    def test_duplicate_item(self):
        with self.assertRaises(ValueError):quote_plan(Account(3600),Plan('rook',('rook_clinch','rook_clinch')))
    def test_unknown_item(self):
        with self.assertRaises(ValueError):quote_plan(Account(3600),Plan('rook',('missing',)))
    def test_unaffordable(self):
        with self.assertRaises(ValueError):quote_plan(Account(),Plan('rook',('rook_clinch',)))
    def test_floor_too_high(self):
        with self.assertRaises(ValueError):quote_plan(Account(),Plan('rook',('rook_high_hook',),1))
    def test_floor_projection(self):self.assertEqual(quote_plan(Account(1500),Plan('rook',('rook_high_hook',),900))['credits_after'],900)
    def test_draft_not_mutation(self):
        a=Account();quote_plan(a,Plan('rook',('rook_high_hook',)));self.assertEqual(a.credits,600)
    def test_atomic_pair(self):
        ledger=PreparationLedger((Account(),Account()),1);r=ledger.commit((Plan('rook',('rook_high_hook',)),Plan('vale')),'prepare1');self.assertEqual([x['credits'] for x in r['accounts']],[0,600])
    def test_bad_second_rolls_back_first(self):
        l=PreparationLedger((Account(),Account()),1)
        with self.assertRaises(ValueError):l.commit((Plan('rook',('rook_high_hook',)),Plan('vale',('vale_return_pulse',))),'bad')
        self.assertEqual(l.accounts,(Account(),Account()))
    def test_duplicate_commit(self):
        l=PreparationLedger((Account(),Account()),1);plans=(Plan('rook'),Plan('vale'));a=l.commit(plans,'p');b=l.commit(plans,'p');self.assertEqual(a,b)
    def test_conflicting_commit(self):
        l=PreparationLedger((Account(),Account()),1);l.commit((Plan('rook'),Plan('vale')),'p')
        with self.assertRaises(ValueError):l.commit((Plan('rook',('rook_high_hook',)),Plan('vale')),'p')
    def test_canonical_lease_order_idempotent(self):
        l=PreparationLedger((Account(1800),Account(1800)),1)
        a=(Plan('rook',('rook_clinch','rook_high_hook')),Plan('vale'))
        b=(Plan('rook',('rook_high_hook','rook_clinch')),Plan('vale'))
        self.assertEqual(l.commit(a,'p'),l.commit(b,'p'))
    def test_new_id_cannot_recommit(self):
        l=PreparationLedger((Account(),Account()),1);p=(Plan('rook'),Plan('vale'));l.commit(p,'p')
        with self.assertRaises(ValueError):l.commit(p,'p2')
    def test_receipt_deep_copy(self):
        l=PreparationLedger((Account(),Account()),1);p=(Plan('rook'),Plan('vale'));r=l.commit(p,'p');r['accounts'][0]['credits']=99999;self.assertEqual(l.commit(p,'p')['accounts'][0]['credits'],600)
    def test_exactly_two(self):
        with self.assertRaises(ValueError):PreparationLedger((Account(),Account(),Account()),1)

class SpendTests(unittest.TestCase):
    def test_vectors(self):
        for v in json.loads((ROOT/'fixtures/economy_vectors.json').read_text())['spends']:
            with self.subTest(v=v):
                s=SpendState(Account(v['credits']),v['floor']);after,status=s.activate('test',v['cost'],'e',v['legal']);self.assertEqual(after.account.credits,v['expected_credits']);self.assertEqual(status,v['status'])
    def test_two_ex_then_empty(self):
        s=SpendState(Account());s,_=s.activate('ex',300,'a');s,_=s.activate('ex',300,'b');self.assertEqual(s.account.credits,0);self.assertEqual(s.activate('ex',300,'c')[1],'INSUFFICIENT')
    def test_floor_two_then_deny(self):
        s=SpendState(Account(1500),900);s,_=s.activate('ex',300,'a');s,_=s.activate('ex',300,'b');self.assertEqual(s.account.credits,900);self.assertEqual(s.activate('ex',300,'c')[1],'RESERVE')
    def test_idempotence(self):
        s,_=SpendState(Account()).activate('ex',300,'a');r,status=s.activate('ex',300,'a');self.assertEqual(status,'DUPLICATE');self.assertEqual(len(r.receipts),1);self.assertEqual(r.account.credits,300)
    def test_event_payload_conflict(self):
        s,_=SpendState(Account()).activate('ex',300,'a')
        with self.assertRaises(ValueError):s.activate('other',300,'a')
    def test_restore_then_replay(self):
        before=SpendState(Account(1200));after,_=before.activate('super',900,'f10');restored=before;again,_=restored.activate('super',900,'f10');self.assertEqual(after,again)
    def test_restore_removed_move(self):
        before=SpendState(Account(1200));after,_=before.activate('super',900,'f10');self.assertEqual(after.account.credits,300);self.assertEqual(before.account.credits,1200);self.assertFalse(before.receipts)
    def test_double_paid_cancel(self):
        s=SpendState(Account(1200));s,_=s.activate('ex',300,'f10');s,_=s.activate('super',900,'f15');self.assertEqual(s.account.credits,0);self.assertEqual(sum(r.cost for r in s.receipts),1200)
    def test_free_at_zero(self):self.assertEqual(SpendState(Account(0)).activate('parry',0,'p')[1],'FREE')
    def test_illegal_no_spend(self):
        s=SpendState(Account(300));after,status=s.activate('ex',300,'x',False);self.assertIs(s,after);self.assertEqual(status,'ILLEGAL')
    def test_bad_floor(self):
        with self.assertRaises(ValueError):SpendState(Account(300),301)
    def test_negative_cost(self):
        with self.assertRaises(ValueError):SpendState(Account()).activate('ex',-300,'x')
    def test_bool_cost(self):
        with self.assertRaises(ValueError):SpendState(Account()).activate('ex',True,'x')
    def test_no_gameplay_refund_api(self):self.assertFalse(hasattr(SpendState,'refund'))
    def test_random_spends_conserve(self):
        rng=random.Random(1523)
        for _ in range(2000):
            start=rng.randrange(3601);floor=rng.randrange(start+1);s=SpendState(Account(start),floor)
            for n in range(12):s,_=s.activate('action',rng.choice([0,300,900,1200,1500]),str(n),rng.choice([True,False]))
            self.assertEqual(start-s.account.credits,sum(r.cost for r in s.receipts));self.assertGreaterEqual(s.account.credits,floor)

class MatchTests(unittest.TestCase):
    def test_vectors(self):
        for v in json.loads((ROOT/'fixtures/scoring_vectors.json').read_text())['vectors']:self.assertEqual(singles_decision(v['a'],v['b'],v['rounds']),v['expected'])
    def test_points_conservation(self):
        with self.assertRaises(ValueError):singles_decision(1,1,2)
    def test_five_wins(self):
        l=MatchLedger()
        for r in range(1,6):l.settle(r,'A',str(r))
        self.assertEqual(l.decision,'A');self.assertEqual(l.scores,(10,0))
    def test_nine_draws(self):
        l=MatchLedger()
        for r in range(1,10):l.settle(r,'DRAW',str(r))
        self.assertEqual(l.decision,'DRAW');self.assertEqual(l.scores,(9,9))
    def test_no_speculative_result(self):
        l=MatchLedger()
        with self.assertRaises(ValueError):l.settle(1,'A','1',False)
        self.assertEqual(l.accounts,(Account(),Account()));self.assertEqual(l.rounds,0)
    def test_duplicate_payout(self):
        l=MatchLedger();a=l.settle(1,'A','1');self.assertEqual(l.settle(1,'A','1'),a);self.assertEqual(l.accounts[0].credits,1800)
    def test_changed_same_id(self):
        l=MatchLedger();l.settle(1,'A','1')
        with self.assertRaises(ValueError):l.settle(1,'B','1')
    def test_new_id_same_round(self):
        l=MatchLedger();l.settle(1,'A','1')
        with self.assertRaises(ValueError):l.settle(1,'A','other')
    def test_out_of_order(self):
        with self.assertRaises(ValueError):MatchLedger().settle(2,'A','2')
    def test_invalid_result_atomic(self):
        l=MatchLedger()
        with self.assertRaises(ValueError):l.settle(1,'BAD','1')
        self.assertEqual(l.scores,(0,0));self.assertEqual(l.accounts,(Account(),Account()))
    def test_post_terminal(self):
        l=MatchLedger()
        for r in range(1,6):l.settle(r,'B',str(r))
        with self.assertRaises(ValueError):l.settle(6,'A','6')
    def test_result_copy(self):
        l=MatchLedger();r=l.settle(1,'A','1');r['scores'][0]=999;self.assertEqual(l.settle(1,'A','1')['scores'],[2,0])
    def test_no_legacy_functions(self):
        import reference.model as m
        for name in ('transfer','crew_decision','relay_step','pairings'):self.assertFalse(hasattr(m,name))

class MathInputTableTests(unittest.TestCase):
    def test_math_vectors(self):
        d=json.loads((ROOT/'fixtures/math_vectors.json').read_text())
        for x in d['trunc_div']:self.assertEqual(trunc_div(x['n'],x['d']),x['expected'])
        for x in d['damage']:self.assertEqual(scaled_damage(x['base'],x['combo_index'],x['counterhit']),x['expected'])
        for x in d['advantage']:self.assertEqual(contact_advantage(x['stun'],x['active'],x['recovery'],x['active_offset']),x['expected'])
    def test_div_zero(self):
        with self.assertRaises(ValueError):trunc_div(1,0)
    def test_normalized_health(self):self.assertEqual(health_verdict(500,1000,450,900),'DRAW')
    def test_health_winner(self):self.assertEqual(health_verdict(501,1000,450,900),'A')
    def test_bad_health(self):
        with self.assertRaises(ValueError):health_verdict(1001,1000,0,1000)
    def test_facing_involution(self):
        for d in range(1,10):self.assertEqual(relative_direction(relative_direction(d,-1),-1),d)
    def test_facing_qcf(self):self.assertEqual([relative_direction(x,-1) for x in [2,3,6]],[2,1,4])
    def test_no_new_held_edge(self):self.assertEqual(button_edges(3,3),(0,0))
    def test_new_ex_chord_edge(self):self.assertEqual(button_edges(0,3),(3,0))
    def test_release_edges(self):self.assertEqual(button_edges(3,0),(0,3))
    def test_parry_truth_table(self):
        self.assertTrue(parry_covers('high','mid'));self.assertFalse(parry_covers('high','low'));self.assertTrue(parry_covers('low','low'));self.assertFalse(parry_covers('low','overhead'));self.assertTrue(parry_covers('air','air'))
    def test_throw_unparryable(self):
        for k in ['high','low','air']:self.assertFalse(parry_covers(k,'mid',True))
if __name__=='__main__':unittest.main()
