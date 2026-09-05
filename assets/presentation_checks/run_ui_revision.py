"""Run the revised controller GUI test from the final exported candidate."""
from pathlib import Path
import subprocess, json, time, hashlib, datetime

root=Path(__file__).resolve().parents[2]
runtime=root/'dist/StrikeLedger/windows'
exe=runtime/'StrikeLedger.exe'
folder=root/'reports/evidence/native-ui-final';folder.mkdir(parents=True,exist_ok=True)
files=[exe,runtime/'StrikeLedger.pck']+[runtime/'data_StrikeLedger_windows_x86_64'/(n+'.dll') for n in ['StrikeLedger','StrikeLedger.Core','StrikeLedger.App']]
def hashes():return {str(p.relative_to(root)):hashlib.sha256(p.read_bytes()).hexdigest() for p in files}
before=hashes()
cmd=[str(exe),'--rendering-method','gl_compatibility','--write-movie',str(folder/'recording.avi'),'--fixed-fps','60','--disable-vsync','--','--ui-smoke','--evidence-dir',str(folder)]
started=time.monotonic()
with (folder/'process.log').open('w',encoding='utf-8') as log:
    log.write('$ '+subprocess.list2cmdline(cmd)+'\n');log.flush()
    child=subprocess.Popen(cmd,cwd=root,stdout=log,stderr=subprocess.STDOUT)
    try:code=child.wait(timeout=600)
    except subprocess.TimeoutExpired:
        child.kill();code=124
    log.write('\nEXIT '+str(code)+'\n')
text=(folder/'process.log').read_text(encoding='utf-8',errors='replace')
errors=[l for l in text.splitlines() if l.startswith('ERROR:') or 'Unhandled exception' in l or 'instances leaked' in l or 'resources still in use' in l]
result=json.loads((folder/'controller-menu-flow.json').read_text()) if (folder/'controller-menu-flow.json').exists() else {}
after=hashes()
passed=code==0 and not errors and before==after and result.get('success') and len(result.get('checks',[]))==75
report={'utc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'mode':'ui','command':cmd,'exit_code':code,'elapsed_seconds':time.monotonic()-started,'passed':bool(passed),'errors':errors,'exe_sha256':before[str(exe.relative_to(root))],'runtime_sha256':before,'runtime_unchanged_during_run':before==after,'result':str((folder/'controller-menu-flow.json').relative_to(root))}
(folder/'process-result.json').write_text(json.dumps(report,indent=2))
print(json.dumps(report))
raise SystemExit(0 if passed else 1)
