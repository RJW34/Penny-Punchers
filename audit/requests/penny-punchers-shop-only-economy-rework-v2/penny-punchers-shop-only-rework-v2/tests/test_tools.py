import unittest,sys,json,tempfile,hashlib,copy
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1];sys.path.insert(0,str(ROOT/'tools'))
from validate_pack import validate
from check_release_evidence import check
class PackageTests(unittest.TestCase):
    def test_structure(self):self.assertEqual(validate()['package_validation'],'PASS')
    def test_baseline_sources_hash_match_record(self):
        data=json.loads((ROOT/'provenance/SOURCE_IDENTITY.json').read_text())
        for name,hashes in data['baseline_files'].items():
            b=(ROOT/'provenance/baseline'/name).read_bytes();self.assertEqual(hashlib.sha1(f'blob {len(b)}\0'.encode()+b).hexdigest(),hashes['git_blob_sha1'])
    def test_original_audit_ids_preserved(self):
        d=json.loads((ROOT/'data/audit_crosswalk.json').read_text());self.assertEqual([r['id'] for r in d['rows']],[f'PP-{i:03}' for i in range(1,92)])
    def test_all_futures_no_activation_debit(self):
        d=json.loads((ROOT/'data/candidate_library.v2.json').read_text())
        self.assertTrue(all(e['activation_price']==0 for e in d['entries']))
    def test_initial_evidence_rejected(self):
        candidate=json.loads((ROOT/'reports/CANDIDATE.template.json').read_text());ledger=json.loads((ROOT/'reports/evidence_ledger.json').read_text());req=json.loads((ROOT/'acceptance/requirements.json').read_text())
        self.assertTrue(check(candidate,ledger,req,ROOT))
    def synthetic(self,path):
        # SYNTHETIC TOOL FIXTURE ONLY, never entered into the real game's evidence ledger.
        c={'candidate_id':'SYNTHETIC','build_id':'synthetic','source_sha256':'a'*64,'content_sha256':'b'*64}
        r={'id':'T','status':'PASS','source_sha256':'a'*64,'content_sha256':'b'*64,'build_id':'synthetic','command_or_session':'synthetic validator fixture','observed_utc':'2026-09-05T00:00:00Z','exit_code':0,'evidence_scope':'software','artifacts':[{'path':path.name,'sha256':hashlib.sha256(path.read_bytes()).hexdigest()}]}
        return c,{'candidate_id':'SYNTHETIC','records':[r]},{'requirements':[{'id':'T','layer':'software'}]}
    def test_structural_validator_happy_fixture(self):
        with tempfile.TemporaryDirectory() as d:
            p=Path(d)/'fixture.txt';p.write_text('SYNTHETIC VALIDATOR FIXTURE - NOT GAMEPLAY');c,l,r=self.synthetic(p);self.assertEqual(check(c,l,r,Path(d)),[])
    def test_validator_rejects_stale(self):
        with tempfile.TemporaryDirectory() as d:
            p=Path(d)/'fixture.txt';p.write_text('synthetic');c,l,r=self.synthetic(p);l['records'][0]['content_sha256']='c'*64;self.assertTrue(check(c,l,r,Path(d)))
    def test_validator_rejects_hash_mismatch(self):
        with tempfile.TemporaryDirectory() as d:
            p=Path(d)/'fixture.txt';p.write_text('synthetic');c,l,r=self.synthetic(p);p.write_text('changed');self.assertTrue(check(c,l,r,Path(d)))
    def test_validator_rejects_path_traversal(self):
        with tempfile.TemporaryDirectory() as d:
            p=Path(d)/'fixture.txt';p.write_text('synthetic');c,l,r=self.synthetic(p);l['records'][0]['artifacts'][0]['path']='../elsewhere';self.assertTrue(check(c,l,r,Path(d)))
    def test_validator_rejects_oracle_as_game_evidence(self):
        with tempfile.TemporaryDirectory() as d:
            p=Path(d)/'fixture.txt';p.write_text('synthetic');c,l,r=self.synthetic(p);l['records'][0]['evidence_scope']='oracle';self.assertTrue(check(c,l,r,Path(d)))
if __name__=='__main__':unittest.main()

class VectorTests(unittest.TestCase):
    def test_vectors_regenerate_identically(self):
        from generate_vectors import vectors
        p=ROOT/'data/vectors/conformance.json'
        if not p.exists():self.skipTest('Generate vectors first')
        self.assertEqual(vectors(),json.loads(p.read_text()))
    def test_budget_examples_never_spend_in_fight(self):
        from generate_vectors import budgets
        for e in budgets()['examples']:
            self.assertEqual(e['opening']-e['shop_cost'],e['saved_bank'])
            self.assertLessEqual(e['shop_cost'],2400)
            self.assertLessEqual(e['super_uses'],1)
