"""Run exported game evidence with real process exits and optional native movie output."""
from pathlib import Path
import argparse,subprocess,json,time,datetime,hashlib,os
ROOT=Path(__file__).resolve().parents[1]
def wsl(path):return '/mnt/'+str(path)[0].lower()+str(path)[2:].replace('\\','/')
def main():
    p=argparse.ArgumentParser();p.add_argument('mode',choices=['match','free-kit','ui','showcase','art','network','linux','windows-headless','linux-headless','free-kit-headless']);p.add_argument('--movie',action='store_true');p.add_argument('--fps',type=int,default=30);p.add_argument('--label');p.add_argument('--ruleset',choices=['buyables_core','buyables_full'],default='buyables_core');p.add_argument('--profile',action='store_true');p.add_argument('--llvmpipe-threads',type=int,choices=range(1,9));a=p.parse_args()
    if a.llvmpipe_threads is not None and not a.mode.startswith('linux'):raise SystemExit('llvmpipe thread control applies only to a Linux process.')
    label=a.label or a.mode
    if not label or any(c not in 'abcdefghijklmnopqrstuvwxyz0123456789-_' for c in label):raise SystemExit('Invalid evidence label')
    out=ROOT/'reports/evidence'/('native-'+label);out.mkdir(parents=True,exist_ok=True)
    jobs=[]
    for seat in range(2 if a.mode=='network' else 1):
        folder=out/('peer'+str(seat)) if a.mode=='network' else out;folder.mkdir(exist_ok=True)
        linux=a.mode.startswith('linux');exe=ROOT/'dist/StrikeLedger'/('linux/StrikeLedger.x86_64' if linux else 'windows/StrikeLedger.exe')
        user=['--evidence-dir',wsl(folder) if linux else str(folder),'--ruleset',a.ruleset]
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
            environment=[]
            if a.llvmpipe_threads is not None:environment+=['LP_NUM_THREADS='+str(a.llvmpipe_threads)]
            if lib.exists():environment+=['LD_LIBRARY_PATH='+wsl(lib)+':'+wsl(lib/'pulseaudio')]
            if environment:prefix+=['env']+environment
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
        (folder/'process-result.json').write_text(json.dumps(result,indent=2));results.append(result);print(json.dump