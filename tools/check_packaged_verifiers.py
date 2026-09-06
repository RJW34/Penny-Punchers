"""Execute bounded self-contained verification tools on Windows and WSL Linux."""
from pathlib import Path
import hashlib,json,os,subprocess,time,sys,re
from datetime import datetime,timezone

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'reports/evidence/shop-v2-packaged-verifiers'

def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()
def load(p): return json.loads(p.read_text(encoding='utf-8-sig'))
def linux(p):
    p=p.resolve();return '/mnt/'+p.drive[0].lower()+'/'+p.as_posix().split(':/',1)[1]
def run(command,log,expected=0):
    if '--verify-existing' in sys.argv and log.is_file():
        text=log.read_text(encoding='utf-8');first=json.loads(text.splitlines()[0])
        assert first['command']==command,'Existing command differs: '+str(log)
        matches=re.findall(r'EXIT (\d+); elapsed ([0-9.]+)',text);assert matches,'Incomplete prior execution: '+str(log)
        code,elapsed=matches[-1];assert int(code)==expected
        return {'command':command,'exitCode':int(code),'elapsedSeconds':float(elapsed),'log':str(log.relative_to(ROOT)).replace('\\','/'),'logSha256':sha(log),'verifiedExistingCompletedRun':True}
    start=time.monotonic()
    with log.open('w',encoding='utf-8') as f:
        f.write(json.dumps({'utc':datetime.now(timezone.utc).isoformat(),'command':command})+'\n');f.flush()
        p=subprocess.run(command,cwd=ROOT,stdout=f,stderr=subprocess.STDOUT,timeout=300,creationflags=subprocess.CREATE_NO_WINDOW if os.name=='nt' else 0)
        f.write('\nEXIT '+str(p.returncode)+'; elapsed '+str(time.monotonic()-start)+'\n')
    assert (p.returncode==expected if expected==0 else p.returncode!=0),(command,p.returncode)
    return {'command':command,'exitCode':p.returncode,'elapsedSeconds':time.monotonic()-start,'log':str(log.relative_to(ROOT)).replace('\\','/'),'logSha256':sha(log)}

def main():
    OUT.mkdir(parents=True,exist_ok=True)
    package=ROOT/'dist/verification';manifest=load(package/'PACKAGE_MANIFEST.json');candidate=load(ROOT/'reports/RELEASE_CANDIDATE.json')
    assert len(manifest['available_tools'])==6
    for item in manifest['files']:
        assert sha(package/item['path'])==item['sha256'],item['path']
    native={Path(r['path']).name:r['sha256'] for r in candidate['native_binaries'] if r['path'].endswith(('/StrikeLedger.Core.dll','/StrikeLedger.App.dll'))}
    for name in ('Core','App'):assert manifest['production_binaries'][name]==native['StrikeLedger.'+name+'.dll']
    records=[]
    for platform in ('win-x64','linux-x64'):
        for tool in ('CoreTests','NetworkLab','BalanceLab'):
            exe=package/platform/tool/('StrikeLedger.'+tool+('.exe' if platform=='win-x64' else ''))
            if platform=='linux-x64':run(['wsl','-d','Ubuntu','--','chmod','+x',linux(exe)],OUT/(tool+'-chmod.log'))
            for catalog in ('core','full'):
                data=package/'data' if catalog=='core' else package/'data/rulesets/buyables_full'
                output=OUT/(platform+'-'+tool+'-'+catalog);output.mkdir(exist_ok=True)
                args={'CoreTests':['--scenario','shop_all_ex_repeat_without_bank'],'NetworkLab':['--scenario','shop_only_v2'],'BalanceLab':['--scenario','shop_only_pilot','--scope','smoke','--seeds','1']}[tool]
                if platform=='win-x64':command=[str(exe),*args,'--seed','1','--data',str(data),'--evidence-dir',str(output)]
                else:command=['wsl','-d','Ubuntu','--',linux(exe),*args,'--seed','1','--data',linux(data),'--evidence-dir',linux(output)]
                print('Running',platform,tool,catalog,flush=True)
                record=run(command,output/'process.log');record.update(platform=platform,tool=tool,catalog=catalog,executableSha256=sha(exe))
                report_name={'CoreTests':'core-conformance.json','NetworkLab':'self-tests.json','BalanceLab':'summary.json'}[tool]
                report=load(output/report_name)
                if tool=='CoreTests':assert report['failed']==0 and report['passed']==1
                elif tool=='NetworkLab':assert report['exitCode']==0 and all(x['passed'] for x in report['results'])
                else:assert report['excluded']==0 and report['completed']==8 and report['allTracesReconstructed']
                record['report']=str((output/report_name).relative_to(ROOT)).replace('\\','/');record['reportSha256']=sha(output/report_name);records.append(record)
    comparisons=[]
    for catalog in ('core','full'):
        for file in ('v2-full-match.json','v2-round-ledger.json','v2-round-ledger.csv'):
            paths=[OUT/(p+'-NetworkLab-'+catalog)/file for p in ('win-x64','linux-x64')]
            exact=paths[0].read_bytes()==paths[1].read_bytes()
            if file=='v2-round-ledger.json':
                assert load(paths[0])==load(paths[1]) and paths[0].read_text()==paths[1].read_text(),(catalog,file)
            else:assert exact,(catalog,file)
            comparisons.append({'catalog':catalog,'file':file,'windowsSha256':sha(paths[0]),'linuxSha256':sha(paths[1]),'byteIdentical':exact,'contentIdentical':True,'difference':'None' if exact else 'Only Windows CRLF versus Linux LF in formatted JSON; every parsed value and normalized text matches'})
    # The smoke sample is deliberately bounded; prior full matrices remain separate.
    result={'utc':datetime.now(timezone.utc).isoformat(),'passed':True,'candidate':candidate['build_ref'],'candidateManifestSha256':sha(ROOT/'reports/RELEASE_CANDIDATE.json'),'manifestSha256':sha(package/'PACKAGE_MANIFEST.json'),'verifiedManifestFiles':len(manifest['files']),'productionBinaries':manifest['production_binaries'],'processes':records,'crossPlatformComparisons':comparisons,'scope':'12 actual self-contained executable runs: Core all8 repeatable EX licenses, App v2 rollback/replay/wire suite, Balance8 paired smoke rounds per platform/catalog. No SDK installed or production rebuild. Linux uses existing WSL Ubuntu on this same PC. This is verifier packaging proof, not graphical native game ZIP launch or two physical PCs.'}
    (OUT/'result.json').write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
    print('PASS12 executable runs;6 content-identical comparisons (replay/CSV bytes exact; JSON ledger allows OS line endings)',flush=True)

if __name__=='__main__':main()
