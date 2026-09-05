#!/usr/bin/env python3
"""Launch two real UDP processes; validate both full-match final canonical states.
No router, firewall, account, LAN interface, or external server is configured.
"""
import argparse, hashlib, json, os, pathlib, socket, subprocess, sys, time

ROOT = pathlib.Path(__file__).resolve().parents[1]
def free_port():
    with socket.socket(socket.AF_INET, socket.SOCK_DGRAM) as sock:
        sock.bind(("127.0.0.1", 0))
        return sock.getsockname()[1]

def main():
    ap=argparse.ArgumentParser()
    ap.add_argument("--dotnet",default="dotnet")
    ap.add_argument("--configuration",default="Release",choices=["Debug","Release","ExportDebug","ExportRelease"])
    ap.add_argument("--output",default=str(ROOT/"reports"/"network-lab"))
    ap.add_argument("--matrix",action="store_true")
    ap.add_argument("--max-seconds",type=int,default=900)
    ap.add_argument("--seed",type=int,default=1)
    ap.add_argument("--rtt",type=int,default=0)
    ap.add_argument("--jitter",type=int,default=0)
    ap.add_argument("--loss",type=int,default=0)
    ap.add_argument("--duplicates",type=int,default=0)
    ap.add_argument("--reorder",type=int,default=0)
    ap.add_argument("--no-build",action="store_true")
    a=ap.parse_args()
    if not 1<=a.seed<=100000:ap.error("--seed must be1..100000")
    out=pathlib.Path(a.output).resolve();out.mkdir(parents=True,exist_ok=True)
    project=ROOT/"src"/"StrikeLedger.NetworkLab"/"StrikeLedger.NetworkLab.csproj"
    if not a.no_build:subprocess.run([a.dotnet,"build",str(project),"-c",a.configuration,"--nologo"],cwd=ROOT,check=True)
    dll=project.parent/"bin"/a.configuration/"net8.0"/"StrikeLedger.NetworkLab.dll"
    profiles=[(0,0,0,0,0),(50,0,0,0,0),(100,20,1,2,5),(150,20,3,3,10)] if a.matrix else [(a.rtt,a.jitter,a.loss,a.duplicates,a.reorder)]
    results=[]
    for n,(rtt,jitter,loss,duplicate,reorder) in enumerate(profiles):
        directory=out/f"rtt{rtt}-jitter{jitter}-loss{loss}";directory.mkdir(exist_ok=True)
        ports=[free_port(),free_port()]
        while ports[0]==ports[1]:ports[1]=free_port()
        session=f"socket-lab-{os.getpid()}-{n}";jobs=[];logs=[];commands=[];start=time.monotonic()
        for seat in range(2):
            cmd=[a.dotnet,str(dll),"--seat",str(seat),"--local-port",str(ports[seat]),"--remote-port",str(ports[1-seat]),"--session",session,"--data",str(ROOT/"data"),"--output",str(directory),"--rtt",str(rtt),"--jitter",str(jitter),"--loss",str(loss),"--duplicates",str(duplicate),"--reorder",str(reorder),"--max-seconds",str(a.max_seconds),"--seed",str(a.seed)]
            log=open(directory/f"peer{seat}.stdout.log","w",encoding="utf-8");logs.append(log);commands.append(cmd)
            jobs.append(subprocess.Popen(cmd,cwd=ROOT,stdout=log,stderr=subprocess.STDOUT))
        codes=[]
        try:
            for p in jobs:codes.append(p.wait(timeout=a.max_seconds+30))
        except subprocess.TimeoutExpired:
            for p in jobs:
                if p.poll() is None:p.kill()
            raise
        finally:
            for log in logs:log.close()
        peers=[json.loads((directory/f"peer{s}.result.json").read_text()) for s in range(2)] if all((directory/f"peer{s}.result.json").exists() for s in range(2)) else []
        matched=len(peers)==2 and peers[0]["finalHash"]==peers[1]["finalHash"] and peers[0]["wallets"]==peers[1]["wallets"] and peers[0]["scores"]==peers[1]["scores"] and peers[0]["roundReceipts"]==peers[1]["roundReceipts"]
        passed=codes==[0,0] and matched and all(p["completed"] for p in peers)
        files={p.name:hashlib.sha256(p.read_bytes()).hexdigest() for p in directory.iterdir() if p.is_file()}
        result={"passed":passed,"rtt":rtt,"jitter":jitter,"loss":loss,"duplicates":duplicate,"reorder":reorder,"exitCodes":codes,"matchingFinalHashWalletsScoresReceipts":matched,"elapsedSeconds":time.monotonic()-start,"commands":commands,"artifacts":files,"directory":str(directory)}
        results.append(result);print(json.dumps(result),flush=True)
        (out/"matrix-result.json").write_text(json.dumps({"actualTwoProcesses":True,"configuration":a.configuration,"transport":"UDP IPv4 loopback","physicalLanGate":"pending two-machine test","results":results},indent=2))
        if not passed:return 1
    return 0

if __name__=="__main__":sys.exit(main())
