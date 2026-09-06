"""Prove the late requested name swap is a presentation-only delta.

Historical executions keep their original identities. This report never relabels
or modifies their replay bytes; fresh renamed build/UI checks remain separate.
"""
from pathlib import Path
import datetime,hashlib,json,subprocess,shutil
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'reports/evidence/name-swap-audit'
BEFORE=ROOT/'reports/evidence/pre-name-swap-candidate'
def read(p):return json.loads(p.read_text(encoding='utf-8'))
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def main():
 OUT.mkdir(exist_ok=True)
 old=read(BEFORE/'RELEASE_CANDIDATE.json');new=read(ROOT/'reports/RELEASE_CANDIDATE.json')
 delta=read(BEFORE/'rename-delta.json');allowed={r['path'] for r in delta['changes']}
 oldsrc={r['path']:r['sha256'] for r in old['source_inputs']};newsrc={r['path']:r['sha256'] for r in new['source_inputs']}
 assert oldsrc.keys()==newsrc.keys(),'Unexpected production source additions/removals'
 changed=[p for p in oldsrc if oldsrc[p]!=newsrc[p]]
 assert set(changed)<=allowed,changed
 for path in changed:
  a=(BEFORE/'source-before'/path).read_text();b=(ROOT/path).read_text()
  assert a.replace('Thomas','__SWAP__').replace('Vincent','Thomas').replace('__SWAP__','Vincent')==b,path
 for r in new['source_inputs']+new['native_binaries']:assert sha(ROOT/r['path'])==r['sha256'],r['path']
 olddata=[];newdata=[];datachanges=[]
 for p in sorted((ROOT/'data').rglob('*.json')):
  rel=p.relative_to(ROOT).as_posix();before=subprocess.check_output(['git','show','9193c77:'+rel],cwd=ROOT)
  a=json.loads(before);b=read(p)
  if a!=b:
   assert rel in allowed and p.stem in ['rook','vale'],rel
   assert b['display_name']=={'rook':'Vincent','vale':'Thomas'}[p.stem]
   datachanges.append(rel);a.pop('display_name');b.pop('display_name')
  assert a==b,rel
  olddata.append([rel,a]);newdata.append([rel,b])
 canonical=lambda x:hashlib.sha256(json.dumps(x,sort_keys=True,separators=(',',':')).encode()).hexdigest()
 assert canonical(olddata)==canonical(newdata)
 # MVID/PDB/source-link metadata can change after a Git commit. Compare every
 # Core method body and signature independently of those non-executable IDs.
 code='''using System.Reflection.Metadata;using System.Reflection.PortableExecutable;using System.Security.Cryptography;using System.Text;using System.Text.Json;
static string Digest(string path){using var stream=File.OpenRead(path);using var pe=new PEReader(stream);var md=pe.GetMetadataReader();using var output=new MemoryStream();using var w=new BinaryWriter(output,Encoding.UTF8,true);foreach(var h in md.MethodDefinitions){var m=md.GetMethodDefinition(h);w.Write(md.GetString(m.Name));w.Write((int)m.Attributes);w.Write((int)m.ImplAttributes);var sig=md.GetBlobBytes(m.Signature);w.Write(sig.Length);w.Write(sig);if(m.RelativeVirtualAddress==0){w.Write(0);continue;}var body=pe.GetMethodBody(m.RelativeVirtualAddress);var il=body.GetILBytes()!;w.Write(il.Length);w.Write(il);w.Write(body.MaxStack);w.Write(body.LocalVariablesInitialized);foreach(var e in body.ExceptionRegions){w.Write((int)e.Kind);w.Write(e.TryOffset);w.Write(e.TryLength);w.Write(e.HandlerOffset);w.Write(e.HandlerLength);w.Write(e.FilterOffset);}}w.Flush();return Convert.ToHexString(SHA256.HashData(output.ToArray())).ToLowerInvariant();}
var before=Digest(args[0]);var after=Digest(args[1]);Console.WriteLine(JsonSerializer.Serialize(new{before,after,equal=before==after}));return before==after?0:1;
'''
 project=OUT/'il-probe';project.mkdir(exist_ok=True)
 (project/'Program.cs').write_text(code)
 (project/'Probe.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable></PropertyGroup></Project>')
 before=next((BEFORE/'native/windows').rglob('StrikeLedger.Core.dll'));after=next((ROOT/'dist/StrikeLedger/windows').rglob('StrikeLedger.Core.dll'))
 dotnet=read(ROOT/'.local_tools.json')['dotnet'];cmd=[dotnet,'run','--project',str(project/'Probe.csproj'),'-c','Release','--',str(before),str(after)]
 run=subprocess.run(cmd,cwd=ROOT,text=True,encoding='utf-8',stdout=subprocess.PIPE,stderr=subprocess.STDOUT)
 (OUT/'core-il.log').write_text('$ '+subprocess.list2cmdline(cmd)+'\n'+run.stdout+'\nEXIT '+str(run.returncode)+'\n')
 assert run.returncode==0,run.stdout
 il=json.loads(run.stdout.strip().splitlines()[-1]);assert il['equal']
 result=dict(utc=datetime.datetime.now(datetime.timezone.utc).isoformat(),passed=True,priorCandidate=old['build_ref'],candidate=new['build_ref'],oldCanonicalContent=old['content_sha256'],newCanonicalContent=new['content_sha256'],sourceChanges=changed,dataChanges=datachanges,mechanicalDataProjectionSha256=canonical(newdata),coreMethods=il,priorManifestSha256=sha(BEFORE/'RELEASE_CANDIDATE.json'),currentManifestSha256=sha(ROOT/'reports/RELEASE_CANDIDATE.json'),scope='Only requested Thomas/Vincent text swaps in four fighter display labels, one training message, showcase captions and UI expectation. All production source changes restricted to exact name replacement; every other source/resource input unchanged. All canonical mechanical data and Core method bodies/signatures match. Current renamed executable/GUI tests are separate; historical replay/content/build IDs are preserved and incompatible old replay files are not rewritten.',mapping={'rook':'Vincent','vale':'Thomas'},artifacts={'delta':str((BEFORE/'rename-delta.json').relative_to(ROOT)).replace('\\','/'),'coreIL':'reports/evidence/name-swap-audit/core-il.log'})
 (OUT/'result.json').write_text(json.dumps(result,indent=2)+'\n');print(json.dumps(result,indent=2))
if __name__=='__main__':main()
