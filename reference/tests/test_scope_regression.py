"""Negative contract tests against accidentally carrying over team/earned-meter fields."""
import unittest,json,tempfile,shutil
from pathlib import Path
from jsonschema import Draft202012Validator,ValidationError
from tools.validate_pack import validate,ROOT

class ScopeSchemaTests(unittest.TestCase):
    def schema(self,name):return Draft202012Validator(json.loads((ROOT/f'schemas/{name}.schema.json').read_text()))
    def test_scalar_wallet_valid(self):self.schema('wallet').validate({'credits':600,'recovery_tier':0})
    def test_old_recovery_bucket_rejected(self):
        with self.assertRaises(ValidationError):self.schema('wallet').validate({'credits':600,'recovery_tier':0,'recovery':300})
    def test_sponsor_rejected(self):
        with self.assertRaises(ValidationError):self.schema('wallet').validate({'credits':600,'recovery_tier':0,'sponsored':300})
    def test_bool_balance_rejected(self):
        with self.assertRaises(ValidationError):self.schema('wallet').validate({'credits':True,'recovery_tier':0})
    def test_third_seat_rejected(self):
        with self.assertRaises(ValidationError):self.schema('input_frame').validate({'seat_id':2,'frame':0,'direction':5,'held':0})
    def test_dual_arena_packet_rejected(self):
        with self.assertRaises(ValidationError):self.schema('input_frame').validate({'seat_id':0,'frame':0,'direction':5,'held':0,'duel_id':1})
    def test_transfer_command_rejected(self):
        with self.assertRaises(ValidationError):self.schema('preparation_plan').validate({'round_id':1,'seat_id':0,'plan_version':0,'lease_ids':[],'reserve_floor':0,'locked':False,'transfer':600})
    def test_client_balance_in_plan_rejected(self):
        with self.assertRaises(ValidationError):self.schema('preparation_plan').validate({'round_id':1,'seat_id':0,'plan_version':0,'lease_ids':[],'reserve_floor':0,'locked':False,'credits':3600})

class ContentMutationTests(unittest.TestCase):
    def mutate(self,path,fn):
        with tempfile.TemporaryDirectory() as t:
            dest=Path(t)/'pack';shutil.copytree(ROOT,dest,ignore=shutil.ignore_patterns('__pycache__'))
            p=dest/path;d=json.loads(p.read_text());fn(d);p.write_text(json.dumps(d))
            with self.assertRaises(ValueError):validate(dest)
    def test_added_meter_is_rejected(self):self.mutate('data/fighters/rook.json',lambda d:d.update(super_meter=0))
    def test_added_team_mode_is_rejected(self):self.mutate('data/rules.json',lambda d:d.update(crew={'enabled':True}))
    def test_four_peers_is_rejected(self):self.mutate('data/network.json',lambda d:d.update(max_peers=4))
    def test_combat_income_is_rejected(self):self.mutate('data/economy.json',lambda d:d.update(combat_income=10))
    def test_kara_window_drift_is_rejected(self):self.mutate('data/inputs.json',lambda d:d.update(kara_throw_ticks=1))
    def test_move_without_effect_rejected(self):
        def remove(d):d['moves'][0].update(hitboxes=[],movement=[],projectile=None)
        self.mutate('data/fighters/rook.json',remove)
if __name__=='__main__':unittest.main()
