"""Reproducible native build. Set DOTNET_BIN / GODOT_BIN or .local_tools.json."""
import argparse,json,os,pathlib,shutil,subprocess,sys,time,re
ROOT=pathlib.Path(__file__).resolve().parents[1]
cfg=json.loads((ROOT/'.local_tools.json').read_text()) if (ROOT/'.local_tools.json').exists() else {}
def tool(name):
    result=os.environ.get(name.upper()+'_BIN') or cfg.get(name) or shutil.which(name)
    if not result: raise RuntimeError(f'Set {name.upper()}_BIN to the installed {name} executable')
    return str(pathlib.Path(result).resolve())
def run(args,log):
    print('RUN',subprocess.list2cmdline([str(x) for x in args]),flush=True)
    env=os.environ.copy(); env['DOTNET_ROOT']=str(pathlib.Path(tool('dotnet')).parent);env['PATH']=env['DOTNET_ROOT']+os.pathsep+env.get('PATH','');env['DOTNET_CLI_TELEMETRY_OPTOUT']='1'
    p=subprocess.run([str(x) for x in args],cwd=ROOT,env=env,stdout=subprocess.PIPE,stderr=subprocess.STDOUT,text=True,encoding='utf-8',errors='replace')
    log.write('$ '+subprocess.list2cmdline([str(x) for x in args])+'\n'+p.stdout+f'\nEXIT {p.returncode}\n');log.flush();print(p.stdout[-5000:],flush=True)
    if p.returncode: raise SystemExit(p.returncode)
    if re.search(r'^ERROR:',p.stdout,re.MULTILINE):raise SystemExit('Engine reported ERROR despite exit 0; inspect build.log')
def main():
    parser=argparse.ArgumentParser();parser.add_argument('--export',action='store_true');parser.add_argument('--test',action='store_true');a=parser.parse_args()
    out=ROOT/'reports/evidence';out.mkdir(parents=True,exist_ok=True)
    with (out/'build.log').open('w',encoding='utf-8') as log:
        run([sys.executable,'tools/copy_content.py','--apply'],log)
        run([tool('dotnet'),'build','game/StrikeLedger.csproj','-c','Debug'],log)
        run([tool('godot'),'--headless','--path',ROOT/'game','--editor','--import','--quit'],log)
        if a.test:
            for proj in ['StrikeLedger.ContractTests','StrikeLedger.CoreTests','StrikeLedger.NetworkLab']:
                path=ROOT/f'src/{proj}/{proj}.csproj'
                if path.exists():run([tool('dotnet'),'run','--project',path,'-c','ExportRelease','--','--data',ROOT/'data','--self-test','--evidence-dir',out/'build-test'/proj],log)
        if a.export:
            for preset,folder,name in [('Windows Desktop','windows','StrikeLedger.exe'),('Linux','linux','StrikeLedger.x86_64')]:
                dest=ROOT/'dist/StrikeLedger'/folder;dest.mkdir(parents=True,exist_ok=True)
                run([tool('godot'),'--headless','--path',ROOT/'game','--export-release',preset,dest/name],log)
if __name__=='__main__':main()
