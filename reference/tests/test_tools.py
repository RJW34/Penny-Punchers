"""Tests of the scaffold tools themselves, explicitly not gameplay evidence."""
from __future__ import annotations
import hashlib,json,shutil,tempfile,unittest
from pathlib import Path
from tools.release_gate import evaluate
from tools.copy_content import copy_content
from tools.validate_pack import validate,ROOT

class GateTests(unittest.TestCase):
    def setUp(self):
        self.tmp=tempfile.TemporaryDirectory();self.root=Path(self.tmp.name)
        (self.root/'acceptance').mkdir();(self.root/'reports').mkdir()
        self.req={'id':'TEST-001','mandatory':True,'gate_class':'software','required_evidence_kinds':['test_log']}
        (self.root/'acceptance/requirements.json').write_text(json.dumps({'requirements':[self.req]}))
        self.art=self.root/'reports/actual.log';self.art.write_text('TEST FIXTURE ONLY, NOT GAME EVIDENCE\n')
        (self.root/'data').mkdir();(self.root/'data/test.json').write_text('{}\n')
        digest=copy_content(self.root)['content_sha256']
        (self.root/'reports/RELEASE_CANDIDATE.json').write_text(json.dumps({'schema_version':1,'status':'READY_FOR_VERIFICATION','build_ref':'fixture-only','content_sha256':digest}))
        self.rec={'requirement_id':'TEST-001','status':'PASS','build_ref':'fixture-only','content_sha256':digest,'timestamp_utc':'2026-09-04T00:00:00Z','platform':'fixture','command':'unit fixture command','exit_code':0,'artifacts':[{'kind':'test_log','path':'reports/actual.log','sha256':hashlib.sha256(self.art.read_bytes()).hexdigest()}]}
    def tearDown(self):self.tmp.cleanup()
    def run_gate(self,records):
        (self.root/'reports/ACCEPTANCE_RESULTS.json').write_text(json.dumps({'schema_version':1,'records':records}));return evaluate(self.root)
    def test_empty_fails(self):self.assertEqual(self.run_gate([])['status'],'FAIL')
    def test_complete_fixture_integrity_passes_not_game(self):self.assertEqual(self.run_gate([self.rec])['status'],'PASS')
    def test_tampered_file_fails(self):self.art.write_text('tampered');self.assertEqual(self.run_gate([self.rec])['status'],'FAIL')
    def test_empty_file_fails(self):self.art.write_text('');self.assertEqual(self.run_gate([self.rec])['status'],'FAIL')
    def test_missing_file_fails(self):self.art.unlink();self.assertEqual(self.run_gate([self.rec])['status'],'FAIL')
    def test_wrong_kind_fails(self):self.rec['artifacts'][0]['kind']='screenshot';self.assertEqual(self.run_gate([self.rec])['status'],'FAIL')
    def test_path_escape_fails(self):self.rec['artifacts'][0]['path']='../outside.log';self.assertEqual(self.run_gate([self.rec])['status'],'FAIL')
    def test_duplicate_fails(self):self.assertEqual(self.run_gate([self.rec,self.rec])['status'],'FAIL')
    def test_unknown_fails(self):self.rec['requirement_id']='TEST-999';self.assertEqual(self.run_gate([self.rec])['status'],'FAIL')
    def test_nonzero_exit_fails(self):self.rec['exit_code']=2;self.assertEqual(self.run_gate([self.rec])['status'],'FAIL')
    def test_false_exit_fails(self):self.rec['exit_code']=False;self.assertEqual(self.run_gate([self.rec])['status'],'FAIL')
    def test_nonutc_fails(self):self.rec['timestamp_utc']='2026-09-04T00:00:00';self.assertEqual(self.run_gate([self.rec])['status'],'FAIL')
    def test_missing_build_fails(self):self.rec['build_ref']='';self.assertEqual(self.run_gate([self.rec])['status'],'FAIL')
    def test_blocked_fails(self):self.rec['status']='BLOCKED';self.assertEqual(self.run_gate([self.rec])['status'],'FAIL')
    def test_old_build_fails(self):self.rec['build_ref']='old-build';self.assertEqual(self.run_gate([self.rec])['status'],'FAIL')
    def test_old_content_fails(self):self.rec['content_sha256']='f'*64;self.assertEqual(self.run_gate([self.rec])['status'],'FAIL')
    def test_changed_canonical_data_fails(self):
        (self.root/'data/test.json').write_text('{"changed":true}');self.assertEqual(self.run_gate([self.rec])['status'],'FAIL')
    def test_missing_candidate_fails(self):
        (self.root/'reports/RELEASE_CANDIDATE.json').unlink();self.assertEqual(self.run_gate([self.rec])['status'],'FAIL')
    def test_initial_real_game_ledger_fails(self):self.assertEqual(evaluate(ROOT)['status'],'FAIL')

class ContentToolTests(unittest.TestCase):
    def setUp(self):
        self.tmp=tempfile.TemporaryDirectory();self.root=Path(self.tmp.name);(self.root/'data').mkdir();(self.root/'game').mkdir();(self.root/'data/sample.json').write_text('{"value":1}\n')
    def tearDown(self):self.tmp.cleanup()
    def test_dry_run_does_not_write(self):
        self.assertEqual(copy_content(self.root)['mode'],'DRY_RUN');self.assertFalse((self.root/'game/GeneratedData').exists())
    def test_apply_copies_original_bytes(self):
        r=copy_content(self.root,True);self.assertEqual(r['files'],1);self.assertEqual((self.root/'game/GeneratedData/sample.json').read_bytes(),(self.root/'data/sample.json').read_bytes())
    def test_repeat_stable_hash(self):self.assertEqual(copy_content(self.root,True)['content_sha256'],copy_content(self.root,True)['content_sha256'])
    def test_source_mutation_changes_hash(self):
        a=copy_content(self.root)['content_sha256'];(self.root/'data/sample.json').write_text('{"value":2}');self.assertNotEqual(a,copy_content(self.root)['content_sha256'])
    def test_unowned_destination_refused(self):
        (self.root/'game/GeneratedData').mkdir()
        with self.assertRaises(ValueError):copy_content(self.root,True)
    def test_unknown_extra_file_refused(self):
        copy_content(self.root,True);(self.root/'game/GeneratedData/user.txt').write_text('keep me')
        with self.assertRaises(ValueError):copy_content(self.root,True)
        self.assertEqual((self.root/'game/GeneratedData/user.txt').read_text(),'keep me')

class PackValidationTests(unittest.TestCase):
    def test_seed_semantic_validation(self):
        r=validate(ROOT);self.assertEqual(r['requirements'],82);self.assertEqual(r['base_moves'],86);self.assertEqual(r['scope'],'SCAFFOLD_ONLY')
if __name__=='__main__':unittest.main()
