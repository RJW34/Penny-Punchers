"""Fail-closed identity and actual-evidence audit for the AFTER HOURS art swap.

Never edits production, canonical data, requirements, the candidate, or the ledger.
Old media retains its original identity; unchanged executable gameplay evidence
may be inherited only after exact source, content, and binary comparisons.
"""
from __future__ import annotations
import argparse,datetime,hashlib,json,sys
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT=Path(__file__).resolve().parents[1]
BASE=ROOT/'reports/evidence/after-hours-baseline'
E=ROOT/'reports/evidence'
ALLOWED={
    'Directory.Build.props',
    'game/Main.ControlRouting.cs','game/Main.Menus.cs','game/Main.Rendering.cs',
    'game/Main.cs','game/Main.ArtReview.cs','game/Main.ArtSkin.cs',
    'game/Presentation/ArenaView.cs','game/Presentation/RenderState.cs',
    'game/Presentation/ArenaOverlay.cs','game/Presentation/FighterSprites.cs',
    'game/Presentation/FighterPalette.gdshader','game/export_presets.cfg',
}
MODULES={'StrikeLedger.Core.dll','StrikeLedger.App.dll'}
VISUAL_ROWS='CORE-005 MOVE-001 MOVE-002 COMBAT-002 COMBAT-004 COMBAT-008 PARRY-003 FEEL-001 ECO-002 SHOP-001 SHOP-002 MATCH-001 MATCH-004 ART-001 AUDIO-001 TRAIN-001 TRAIN-002 REPLAY-003 NET-004 UI-001 UI-002 UI-003 UI-004 UI-005 QA-001 QA-004 QA-005 BUILD-001 BUILD-002 DEVICE-001 DEVICE-002 FINAL-001 FINAL-002'.split()

def read(path):return json.loads(path.read_text(encoding='utf-8-sig'))
def relative(path):return path.relative_to(ROOT).as_posix()
def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--compatibility-only',action='store_true',help='Check identities/atlas metadata only; never claims native art or completed acceptance.')
    parser.add_argument('--output',type=Path,default=E/'after-hours-compatibility')
    args=parser.parse_args();checks=[];hashes={};artifacts={}
    def sha(path):
        path=path.resolve()
        if path not in hashes:
            h=hashlib.sha256()
            with path.open('rb') as stream:
                for part in iter(lambda:stream.read(1024*1024),b''):h.update(part)
            hashes[path]=h.hexdigest()
        return hashes[path]
    def check(name,passed,detail=None):
        checks.append({'check':name,'passed':bool(passed),'detail':detail})
        return bool(passed)
    def load(path):
        if not path.is_file():check('required artifact exists',False,relative(path));return None
        artifacts[relative(path)]=sha(path)
        try:return read(path)
        except (ValueError,OSError) as error:check('parse artifact',False,{'path':relative(path),'error':str(error)});return None

    old=load(BASE/'RELEASE_CANDIDATE.json');new=load(ROOT/'reports/RELEASE_CANDIDATE.json')
    ledger=load(BASE/'ACCEPTANCE_RESULTS.json');gate=load(BASE/'acceptance-software-gate.json')
    if not all([old,new,ledger,gate]):return 2
    check('preserved baseline really passed software gate',gate['status']=='PASS' and gate['verified_records']==76 and len(ledger['records'])==82)
    inherited_prefixes=('reports/evidence/core-exportrelease/','reports/app-exportrelease-final/','reports/network-lab-exportrelease/','reports/balance-lab-exportrelease-final/','reports/evidence/final-independent-action-verification/','reports/evidence/feel-measurements/','reports/evidence/settings-native-harness/','reports/evidence/input-settings')
    inherited={item['path']:item['sha256'] for record in ledger['records'] for item in record['artifacts'] if item['path'].startswith(inherited_prefixes)}
    inherited_changed=[p for p,digest in inherited.items() if not (ROOT/p).is_file() or sha(ROOT/p)!=digest]
    check('inherited executable evidence artifacts retain baseline hashes',bool(inherited) and not inherited_changed,{'verified_files':len(inherited),'changed_or_missing':inherited_changed})
    check('new art candidate identity differs',new['status']=='READY_FOR_VERIFICATION' and old['build_ref']!=new['build_ref'])
    a={p['path']:p['sha256'] for p in old['source_inputs']};b={p['path']:p['sha256'] for p in new['source_inputs']}
    check('current production files match candidate',all((ROOT/p).is_file() and sha(ROOT/p)==digest for p,digest in b.items()))
    check('candidate source fingerprint is reproducible',new['build_ref']=='source-sha256:'+hashlib.sha256(json.dumps(new['source_inputs'],sort_keys=True,separators=(',',':')).encode()).hexdigest())
    changed=[p for p in sorted(a.keys()|b.keys()) if a.get(p)!=b.get(p)]
    prohibited=[p for p in changed if p not in ALLOWED and not p.startswith('game/Assets/AfterHours/')]
    check('changes are within reviewed presentation scope',not prohibited,{'unexpected':prohibited,'note':'A filename allowlist is not proof of semantics. Actual shell interaction and legal gameplay are verified by the new native runs.'})
    frozen=[p for p in a.keys()|b.keys() if p.startswith(('src/StrikeLedger.Core/','src/StrikeLedger.App/'))]
    check('all Core and App source inputs unchanged',bool(frozen) and all(a.get(p)==b.get(p) for p in frozen),{'files':len(frozen)})
    props=ROOT/'Directory.Build.props'
    if props.is_file():
        values=[(node.tag,(node.text or '').strip()) for node in ET.parse(props).getroot().iter() if len(node)==0]
        check('root build metadata change only disables Git informational suffix',values==[('IncludeSourceRevisionInInformationalVersion','false')] and 'Directory.Build.props' in b,values)
    else:check('root build metadata property is recorded',False,'Directory.Build.props missing')
    data=[{'path':p.relative_to(ROOT/'data').as_posix(),'sha256':sha(p)} for p in sorted((ROOT/'data').rglob('*.json'))]
    datahash=hashlib.sha256(json.dumps(data,sort_keys=True,separators=(',',':')).encode()).hexdigest()
    check('canonical13 data files unchanged',len(data)==13 and old['content_sha256']==new['content_sha256']==datahash)
    oldbin={p['path']:p['sha256'] for p in old['native_binaries']};newbin={p['path']:p['sha256'] for p in new['native_binaries']}
    gameplay={p:digest for p,digest in newbin.items() if Path(p).name in MODULES}
    check('four actual shipped gameplay modules byte-identical',len(gameplay)==4 and all(oldbin.get(p)==digest==sha(ROOT/p) for p,digest in gameplay.items()),gameplay)
    check('every current exported binary matches candidate',bool(newbin) and all((ROOT/p).is_file() and sha(ROOT/p)==digest for p,digest in newbin.items()),{'files':len(newbin)})
    atlases={};expected_move_phases=set();expected_states=set()
    for fighter in ['rook','vale']:
        folder=ROOT/'game/Assets/AfterHours/Fighters'/fighter.title();path=folder/'atlas.json';atlas=load(path)
        canonical=read(ROOT/'data/fighters'/f'{fighter}.json')['moves']
        if atlas is None:continue
        atlases[fighter]=atlas
        check(f'{fighter} atlas49 canonical move mappings',len(atlas['moves'])==49 and set(atlas['moves'])=={m['id'] for m in canonical})
        refs=[name for ids in atlas['states'].values() for name in ids]+[name for move in atlas['moves'].values() for phase in ['startup','active','recovery'] for name in move[phase]]
        check(f'{fighter} all sprite references resolve',all(name in atlas['cells'] for name in refs))
        textures={c['texture'] for c in atlas['cells'].values()}
        paths=[relative(path)]+[relative(folder/t) for t in textures]
        check(f'{fighter} all atlas and image bytes fingerprinted',all(p in b and sha(ROOT/p)==b[p] for p in paths),paths)
        for move in canonical:
            expected_move_phases.update((fighter,move['id'],phase) for phase in ['startup','active','recovery'] if move[phase]>0)
        expected_states.update((fighter,state) for state in atlas['states'])
    required_art=['Stages/foundry.png','Stages/grid.png','Effects/combat-atlas.png','UI/hud-atlas.png','UI/interface-atlas.png','UI/title-backdrop.png']
    check('both stages effects and UI fingerprinted',all('game/Assets/AfterHours/'+p in b for p in required_art))

    def binding(folder,process,media,platform):
        bound={str(p).replace('\\','/'):digest for p,digest in {**media.get('sha256',{}),**process.get('runtime_sha256',{})}.items()}
        prefix='dist/StrikeLedger/'+platform+'/'
        required=[p for p in newbin if p.startswith(prefix) and Path(p).name in MODULES|{'StrikeLedger.dll','StrikeLedger.pck','StrikeLedger.exe','StrikeLedger.x86_64'}]
        check(folder+' evidence bound to exact current shell/PCK/Core/App',len(required)==5 and all(bound.get(p)==newbin[p] for p in required),{'paths':required})
        return bound

    def native(folder,resultname,kind):
        root=E/folder;process=load(root/'process-result.json');result=load(root/resultname)
        media=load(root/'media-validation.json') if kind!='art' or (root/'media-validation.json').is_file() else {}
        if not process or not result or (kind!='art' and not media):return None
        check(folder+' clean actual native process',process.get('passed') and process.get('exit_code')==0 and not process.get('errors'),process.get('command'))
        if kind!='art':check(folder+' fully decoded actual movie',media.get('passed') and media.get('complete_video_decode') and (root/'recording.mp4').is_file())
        else:check(folder+' actual presentation screenshots exist',bool(result.get('captures')) and all((root/p).is_file() and (root/p).stat().st_size>0 for p in result['captures']))
        bound=binding(folder,process,media,'linux' if kind=='linux' else 'windows')
        if kind!='art':
            movie=root/'recording.mp4'
            check(folder+' referenced movie hash matches',movie.is_file() and bound.get(relative(movie))==sha(movie))
            if movie.is_file():artifacts[relative(movie)]=sha(movie)
        return result

    native_summary={}
    if not args.compatibility_only:
        art=native('native-after-hours-art','art-review.json','art')
        if art:
            rows=art.get('checks',[]);actual={(c['fighter'],c['move'],c['phase']) for c in rows if 'move' in c};states={(c['fighter'],c['state']) for c in rows if 'state' in c}
            check('every actual native move phase binding passed',art.get('passed') and all(c.get('passed') for c in rows) and expected_move_phases==actual,{'expected':len(expected_move_phases),'actual':len(actual)})
            check('every actual native atlas state binding passed',expected_states==states,{'expected':len(expected_states),'actual':len(states)})
            native_summary['art']={'move_phases':len(actual),'states':len(states),'captures':len(art.get('captures',[])),'scope':'Actual static presentation fixtures and PNG captures; separate legal-input showcase/full-match movies verify motion in play.'}
        ui=native('native-after-hours-ui','controller-menu-flow.json','ui')
        if ui:check('actual native75 controller UI checks',ui.get('success') and len(ui.get('checks',[]))==75 and all(c.get('passed') for c in ui['checks']))
        show=native('native-after-hours-showcase','combat-visual-showcase.json','showcase')
        if show:check('actual legal-input13 showcase checks',show.get('success') and len(show.get('checks',[]))==13 and all(c.get('passed') for c in show['checks']))
        replay_hashes={}
        for label,folder in [('windows','native-after-hours-match'),('linux','native-after-hours-linux')]:
            match=native(folder,'runtime-result.json',label)
            if match:
                check(label+' completed economic match',match.get('complete') and match.get('ticks')==12789 and match.get('rounds')==6 and match.get('finalHash')=='c4d779b7d9ef1fbfa9b378e4b18fe422169b4b79b1b89d441492cf01e169c51d')
                native_summary[label]={k:match.get(k) for k in ['platform','display','gpu','cpu','ticks','finalHash','frameMs']}
                replay_path=E/folder/'full-match.replay.json';replay=load(replay_path)
                if replay:
                    commands=replay.get('Commands',[]);header=replay.get('Header',{})
                    check(label+' actual competitive replay matches completed runtime',bool(commands) and header.get('Build')==match.get('build') and header.get('ContentHash')==match.get('content') and header.get('Assist') is False and header.get('Config',{}).get('Training') is False and commands[-1].get('Hash')==match.get('finalHash') and [commands[-1].get('Wallet0'),commands[-1].get('Wallet1')]==match.get('wallets'))
                    replay_hashes[label]=sha(replay_path)
        check('new Windows and Linux complete replay bytes agree',len(replay_hashes)==2 and len(set(replay_hashes.values()))==1,replay_hashes)
        pacing=load(E/'native-after-hours-pacing/runtime-result.json');pacingprocess=load(E/'native-after-hours-pacing/process-result.json')
        if pacing and pacingprocess:
            check('new renderer actually profiled without movie capture',pacingprocess.get('passed') and pacingprocess.get('exit_code')==0 and pacing.get('complete') and pacing.get('display')!='headless' and all(k in pacing.get('frameMs',{}) for k in ['p50','p95','p99','worst']) and '--write-movie' not in pacingprocess.get('command',[]))
            binding('native-after-hours-pacing',pacingprocess,{},'windows')
            native_summary['pacing']={k:pacing.get(k) for k in ['platform','gpu','cpu','frameMs','frames','elapsedSeconds']}
        package=load(E/'package-after-hours-verification/result.json')
        if package:
            runs=package.get('results',[]);check('fresh native Windows and Linux package runs passed',package.get('passed') and len(runs)==2 and all(r.get('passed') and r.get('exit_code')==0 and r.get('zip_crc_valid') and r.get('runtime',{}).get('complete') for r in runs))
            for run in runs:
                extraction=Path(run.get('extraction','')).resolve();prefix='dist/StrikeLedger/'+run['platform']+'/'
                expected={p.removeprefix(prefix):digest for p,digest in newbin.items() if p.startswith(prefix)}
                within=extraction.is_relative_to(ROOT/'.tools/package-smoke')
                check(run['platform']+' extracted native bytes match current candidate',within and bool(expected) and all((extraction/p).is_file() and sha(extraction/p)==digest for p,digest in expected.items()),{'files':len(expected),'extraction':str(extraction)})

    passed=all(c['passed'] for c in checks)
    out={'schema_version':1,'utc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'command':[sys.executable]+sys.argv,'exit_code':0 if passed else 1,'passed':passed,'scope':'Gameplay identity compatibility only; no native/art acceptance claim' if args.compatibility_only else 'Actual current-candidate source/binary compatibility and new native art evidence. Not a substitute for visual inspection or unchanged release gate.','baseline_build_ref':old['build_ref'],'current_build_ref':new['build_ref'],'content_sha256':new['content_sha256'],'baseline_git_commit':'d1857f5b754fa049ab40588130f7f373f2a35c28','gameplay_assemblies_identical':gameplay,'checks':checks,'changed_inputs':[{'path':p,'before_sha256':a.get(p),'after_sha256':b.get(p)} for p in changed],'new_native_evidence':native_summary,'reviewed_artifacts':artifacts,'ledger_plan':{'ledger_mutated':False,'functional_inheritance':'Only unchanged exact Core/App/data behavior is inherited from the preserved76/76 baseline. Its commands and dates remain original.','affected_visual_rows':VISUAL_ROWS,'old_media_policy':'Old films remain explicitly pre-AFTER-HOURS historical observations. Replace current art/UI/showcase/full-match/platform presentation evidence with the new native captures. Preserve old free-kit and private-peer observations only as qualified unchanged-gameplay evidence.','external_requirements':['DEVICE-003','DEVICE-004','HUMAN-001'],'remaining_work':'Separate model visual review of actual captures, new candidate-specific ledger rebind after root authorization, exact final docs/package/gate audit.'}}
    args.output.mkdir(parents=True,exist_ok=True);target=args.output/('identity-only.json' if args.compatibility_only else 'result.json')
    target.write_text(json.dumps(out,indent=2)+'\n')
    print(json.dumps({'passed':passed,'checks':len(checks),'failures':[c for c in checks if not c['passed']],'output':relative(target)},indent=2))
    return 0 if passed else 1

if __name__=='__main__':raise SystemExit(main())
