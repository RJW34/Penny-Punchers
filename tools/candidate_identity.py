"""Bind source inputs and canonical data to the current native build candidate."""
from pathlib import Path
import hashlib,json,datetime
ROOT=Path(__file__).resolve().parents[1]
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def main():
    sources=[]
    for name in ['Directory.Build.props','Directory.Build.targets']:
        p=ROOT/name
        if p.is_file():sources.append({'path':name,'sha256':sha(p)})
    for base in ['game','src/StrikeLedger.Core','src/StrikeLedger.App']:
        for p in sorted((ROOT/base).rglob('*')):
            if p.is_file() and not any(x in p.parts for x in ['bin','obj','.godot','GeneratedData']) and p.suffix in ['.cs','.csproj','.tscn','.godot','.cfg','.sln','.wav','.ogg','.png','.svg','.import','.gdshader','.json']:
                sources.append({'path':p.relative_to(ROOT).as_posix(),'sha256':sha(p)})
    sources.sort(key=lambda x:x['path'])
    sourcehash=hashlib.sha256(json.dumps(sources,sort_keys=True,separators=(',',':')).encode()).hexdigest()
    content=[{'path':p.relative_to(ROOT/'data').as_posix(),'sha256':sha(p)} for p in sorted((ROOT/'data').rglob('*.json'))]
    contenthash=hashlib.sha256(json.dumps(content,sort_keys=True,separators=(',',':')).encode()).hexdigest()
    binaries=[{'path':p.relative_to(ROOT).as_posix(),'sha256':sha(p),'bytes':p.stat().st_size} for p in sorted((ROOT/'dist/StrikeLedger').rglob('*')) if p.is_file() and p.suffix in ['.exe','.x86_64','.pck','.dll','.so']]
    if not any(x['path'].endswith('.exe') for x in binaries) or not any(x['path'].endswith('.x86_64') for x in binaries):raise SystemExit('Both exports required')
    result={'schema_version':1,'status':'READY_FOR_VERIFICATION','build_ref':'source-sha256:'+sourcehash,'content_sha256':contenthash,'created_utc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'source_inputs':sources,'native_binaries':binaries,'note':'Source fingerprint covers production C# and engine project inputs. Assembly/build identities are also recorded in each executable run; different build configurations may have different deterministic MVID identities.'}
    (ROOT/'reports/RELEASE_CANDIDATE.json').write_text(json.dumps(result,indent=2)+'\n')
    print(json.dumps({k:result[k] for k in ['build_ref','content_sha256','created_utc']}))
if __name__=='__main__':main()
