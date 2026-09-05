#!/usr/bin/env python3
"""Read-only local prerequisite discovery. Does not install or certify game compatibility."""
import os,sys,shutil,subprocess,json,platform

def probe(exe,args):
    if not exe:return {'found':False,'version':'NOT_FOUND'}
    try:
        p=subprocess.run([exe,*args],capture_output=True,text=True,timeout=15)
        return {'found':True,'path':exe,'exit_code':p.returncode,'version':(p.stdout+p.stderr).strip()[:2000]}
    except Exception as e:return {'found':True,'path':exe,'error':str(e)}
if __name__=='__main__':
    dotnet=probe(shutil.which('dotnet'),['--info']);godot=probe(os.getenv('GODOT_BIN') or shutil.which('godot-mono') or shutil.which('godot4-mono') or shutil.which('godot4') or shutil.which('godot'),['--version'])
    print(json.dumps({'scope':'PREREQUISITE_DISCOVERY_ONLY','platform':platform.platform(),'python':sys.version,'dotnet':dotnet,'godot':godot,'note':'A found executable/version does not prove C# support or export success. Use matching .NET editor/templates and run actual builds.'},indent=2))
    raise SystemExit(0 if dotnet['found'] and godot['found'] else 1)
