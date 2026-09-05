"""Audit exactly which presentation changes followed the frozen gameplay evidence."""
from pathlib import Path
import datetime,difflib,hashlib,json
ROOT=Path(__file__).resolve().parents[1]
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def main():
    old=json.loads((ROOT/'reports/evidence/pre-ui-polish-candidate.json').read_text())
    new=json.loads((ROOT/'reports/RELEASE_CANDIDATE.json').read_text())
    a={v['path']:v['sha256'] for v in old['source_inputs']};b={v['path']:v['sha256'] for v in new['source_inputs']}
    changed=[p for p in sorted(a.keys()|b.keys()) if a.get(p)!=b.get(p)]
    allowed={'game/Main.cs','game/Main.Menus.cs','game/Main.UiSmoke.cs','game/Main.Rendering.cs','game/Main.Shutdown.cs','game/Presentation/ArenaView.cs','game/Presentation/FoundryBackdrop.cs','game/Presentation/RuntimeProfiler.cs'}
    assert set(changed)<=allowed,changed
    assert old['content_sha256']==new['content_sha256']
    assert all(a.get(p)==b.get(p) for p in a.keys()|b.keys() if p.startswith('src/')),'Gameplay source changed'
    verified=[]
    for p in changed:
        if p in a:
            before=ROOT/'reports/evidence/pre-ui-polish-sources'/p.removeprefix('game/')
            original_available=before.exists() and sha(before)==a[p]
        else:original_available=False
        after=ROOT/p
        assert sha(after)==b[p]
        verified.append({'path':p,'before_sha256':a.get(p),'after_sha256':b.get(p),'previous_source_bytes_available':original_available})
    def assemblies(c):return {Path(v['path']).name:v['sha256'] for v in c['native_binaries'] if Path(v['path']).name in ['StrikeLedger.Core.dll','StrikeLedger.App.dll']}
    assert assemblies(old)==assemblies(new),'Gameplay assembly changed'
    out=ROOT/'reports/evidence/candidate-compatibility';out.mkdir(exist_ok=True)
    result={'utc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'passed':True,'previous_build_ref':old['build_ref'],'current_build_ref':new['build_ref'],'content_sha256':new['content_sha256'],'gameplay_assemblies_identical':assemblies(new),'changes':verified,'source_comparison_limit':'The previous PCK strips C# source to newline stubs. This audit compares the archived source hash inventory and exact Core/App binaries; it does not claim a recovered textual source diff. The independent review checks the changed rendering, footer and diagnostic code, supplemented by fresh native UI/full-match runs.','scope':'Prior action, economy, bot, training, network and competitive replay evidence uses byte-identical Core/App assemblies and canonical data. Prior native movies show the pre-polish presentation, explicitly retained as such. Final UI, performance and native smoke captures verify the updated presentation. Changes are confined to the listed presentation/diagnostic source files; no competitive Core/App input, rules or state source changed.'}
    (out/'result.json').write_text(json.dumps(result,indent=2)+'\n');print(json.dumps(result))
if __name__=='__main__':main()
