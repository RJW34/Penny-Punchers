"""Check ZIP integrity, bind binaries, then run each freshly extracted native package."""
from pathlib import Path
import argparse,datetime,hashlib,json,os,subprocess,time,zipfile
ROOT=Path(__file__).resolve().parents[1]
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def linux(p):return '/mnt/'+str(p)[0].lower()+str(p)[2:].replace('\\','/')

def main():
    out=ROOT/'reports/evidence/package-verification';out.mkdir(parents=True,exist_ok=True)
    stamp=datetime.datetime.now(datetime.timezone.utc).strftime('%Y%m%dT%H%M%SZ')
    results=[]
    for platform,exe in [('windows','StrikeLedger.exe'),('linux','StrikeLedger.x86_64')]:
        archive=ROOT/'dist'/f'StrikeLedger-{platform}-x86_64.zip'
        target=ROOT/'.tools/package-smoke'/stamp/platform
        target.mkdir(parents=True,exist_ok=False)
        with zipfile.ZipFile(archive) as z:
            assert z.testzip() is None,'ZIP CRC failure'
            names=z.namelist()
            assert len(names)==len(set(names)),'Duplicate ZIP paths'
            for n in names:
                assert not Path(n).is_absolute() and '..' not in Path(n).parts and n.startswith('StrikeLedger/'),'Unsafe ZIP path'
            mode=z.getinfo('StrikeLedger/'+exe).external_attr>>16
            if platform=='linux':assert mode&0o111,'Linux executable permission missing'
            z.extractall(target)
        folder=target/'StrikeLedger'
        inventory=(folder/'SHA256SUMS.txt').read_text().splitlines()
        for line in inventory:
            digest,name=line.split('  ',1)
            assert sha(folder/name)==digest,'Extracted checksum mismatch: '+name
        for name in [exe,'StrikeLedger.pck']:
            assert sha(folder/name)==sha(ROOT/'dist/StrikeLedger'/platform/name),'Extracted binary differs from export'
        for name in ['StrikeLedger.dll','StrikeLedger.Core.dll','StrikeLedger.App.dll']:
            p=next(folder.rglob(name));source=next((ROOT/'dist/StrikeLedger'/platform).rglob(name));assert sha(p)==sha(source)
        runout=out/platform;runout.mkdir(exist_ok=True)
        command=[str(folder/exe),'--headless','--fixed-fps','60','--audio-driver','Dummy','--','--smoke','--no-screenshots','--evidence-dir',str(runout)]
        if platform=='linux':
            os.chmod(folder/exe,0o755)
            command=['wsl','-d','Ubuntu','--',linux(folder/exe),'--headless','--fixed-fps','60','--audio-driver','Dummy','--','--smoke','--no-screenshots','--evidence-dir',linux(runout)]
        start=time.monotonic()
        proc=subprocess.run(command,cwd=folder,stdout=subprocess.PIPE,stderr=subprocess.STDOUT,text=True,encoding='utf-8',errors='replace',timeout=240)
        (runout/'process.log').write_text('$ '+subprocess.list2cmdline(command)+'\n'+proc.stdout+'\nEXIT '+str(proc.returncode)+'\n')
        errors=[x for x in proc.stdout.splitlines() if x.startswith('ERROR:') or 'Unhandled exception' in x]
        runtime=json.loads((runout/'runtime-result.json').read_text()) if (runout/'runtime-result.json').exists() else {}
        passed=proc.returncode==0 and not errors and runtime.get('complete')
        results.append({'platform':platform,'passed':bool(passed),'command':command,'exit_code':proc.returncode,'elapsed_seconds':time.monotonic()-start,'archive':archive.relative_to(ROOT).as_posix(),'archive_sha256':sha(archive),'zip_crc_valid':True,'files':len(names),'checksums_verified':len(inventory),'executable_mode':oct(mode),'extraction':str(folder),'errors':errors,'runtime':runtime})
        print(json.dumps(results[-1]),flush=True)
    passed=all(r['passed'] for r in results) and len({r['runtime']['finalHash'] for r in results})==1
    (out/'result.json').write_text(json.dumps({'utc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'passed':passed,'scope':'Fresh ZIP extraction and actual native executable runs; runtime DLLs included, no development SDK command used. Linux uses this PC existing WSL Ubuntu with Dummy audio.','results':results},indent=2))
    raise SystemExit(0 if passed else 1)
if __name__=='__main__':main()
