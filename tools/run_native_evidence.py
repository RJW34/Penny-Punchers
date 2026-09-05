"""Run exported game evidence with real process exits and optional native movie output."""
from pathlib import Path
import argparse,subprocess,json,time,datetime,hashlib,os
ROOT=Path(__file__).resolve().parents[1]
def wsl(path):return '/mnt/'+str(path)[0].lower()+str(path)[2:].replace('\\','/')
def main():
    p=argparse.ArgumentParser();p.add_argument('mode',choices=['match','free-kit','ui','showcase','art','network','linux','windows-headless','linux-headless','free-kit-headless']);p.add_argument('--movie',action='store_true');p.add_argument('--fps',type=int,default=30);p.add_argument('--label');p.add_argument('--profile',action='store_true');a=p.parse_args()
    label=a.label or a.mode
    if not label or any(c not in 'abcdefghijklmnopqrstuvwxyz0123456789-_' for c in label):raise SystemExit('Invalid evidence label')
    out=ROOT/'reports/evidence'/('native-'+label);out.mkdir(parents=True,exist_ok=True)
    jobs=[]
    for seat in range(2 if a.mode=='network' else 1):
        folder=out/('peer'+str(seat)) if a.mode=='network' else out;folder.mkdir(exist_ok=True)
        linux=a.mode.startswith('linux');exe=ROOT/'dist/StrikeLedger'/('linux/StrikeLedger.x86_64' if linux else 'windows/StrikeLedger.exe')
        user=['--evidence-dir',wsl(folder) if linux else str(folder)]
        if a.profile:user+=['--profile','--no-screenshots']
        if a.mode=='art':user+=['--art-review']
        elif a.mode=='ui':user+=['--ui-smoke']
        elif a.mode=='showcase':user+=['--combat-showcase']
        elif a.mode=='network':user+=['--network-evidence','--seat',str(seat)]
        else:user+=['--smoke']+(['--free-kit'] if a.mode.startswith('free-kit') else [])
        engine=['--rendering-method','gl_compatibility']
        if a.profile:engine+=['--print-fps']
        if linux:engine+=['--audio-driver','Dummy']
        if a.movie and a.mode not in ['ui','showcase','art']:engine+=['--resolution','960x540']
        if a.mode.endswith('headless'):engine+=['--headless','--fixed-fps','60']
        elif a.movie:engine+=['--write-movie',wsl(folder/'recording.avi') if linux else str(folder/'recording.avi'),'--fixed-fps',str(a.fps),'--disable-vsync']
        else:engine+=['--max-fps','60']
        prefix=[str(exe)]
        if linux:
            lib=ROOT/'.tools/linux-audio/unpacked/usr/lib/x86_64-linux-gnu'
            prefix=['wsl','-d','Ubuntu','--']
            if lib.exists():prefix+=['env','LD_LIBRARY_PATH='+wsl(lib)+':'+wsl(lib/'pulseaudio')]
            prefix+=[wsl(exe)]
        cmd=prefix+engine+['--']+user
        log=(folder/'process.log').open('w',encoding='utf-8');log.write('$ '+subprocess.list2cmdline(cmd)+'\n');log.flush()
        start=time.monotonic();child=subprocess.Popen(cmd,cwd=ROOT,stdout=log,stderr=subprocess.STDOUT);jobs.append((child,log,folder,start,cmd))
    results=[]
    for child,log,folder,start,cmd in jobs:
        try:code=child.wait(timeout=3600)
        except subprocess.TimeoutExpired:child.kill();code=124
        log.write('\nEXIT '+str(code)+'\n');log.close()
        name={'art':'art-review.json','ui':'controller-menu-flow.json','showcase':'combat-visual-showcase.json','network':'native-network-result.json'}.get(a.mode,'runtime-result.json')
        resultfile=folder/name
        payload=json.loads(resultfile.read_text()) if resultfile.exists() else {}
        complete=payload.get('success',payload.get('passed',payload.get('complete',False)))
        text=(folder/'process.log').read_text(encoding='utf-8',errors='replace')
        errors=[line for line in text.splitlines() if line.startswith('ERROR:') or 'Unhandled exception' in line]
        result={'utc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'mode':a.mode,'command':cmd,'exit_code':code,'elapsed_seconds':time.monotonic()-start,'passed':code==0 and complete and not errors,'errors':errors,'result':str(resultfile.relative_to(ROOT)),'exe_sha256':hashlib.sha256((ROOT/'dist/StrikeLedger'/('linux/StrikeLedger.x86_64' if a.mode.startswith('linux') else 'windows/StrikeLedger.exe')).read_bytes()).hexdigest()}
        runtime_root=ROOT/'dist/StrikeLedger'/('linux' if a.mode.startswith('linux') else 'windows')
        runtime_files=[p for p in runtime_root.rglob('*') if p.is_file() and p.name in ['StrikeLedger.exe','StrikeLedger.x86_64','StrikeLedger.pck','StrikeLedger.dll','StrikeLedger.Core.dll','StrikeLedger.App.dll']]
        result['runtime_sha256']={p.relative_to(ROOT).as_posix():hashlib.sha256(p.read_bytes()).hexdigest() for p in runtime_files}
        (folder/'process-result.json').write_text(json.dumps(result,indent=2));results.append(result);print(json.dumps(result),flush=True)
    if a.mode=='network' and all(x['passed'] for x in results):
        states=[json.loads((folder/'native-network-result.json').read_text()) for _,_,folder,_,_ in jobs]
        if states[0]['finalHash']!=states[1]['finalHash']:raise SystemExit('Native peers diverged')
    raise SystemExit(0 if all(x['passed'] for x in results) else 1)
if __name__=='__main__':main()
