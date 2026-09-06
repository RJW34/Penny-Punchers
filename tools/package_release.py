"""Package the already verified native exports, player documentation and SHA-256 inventory."""
from pathlib import Path
import hashlib,json,shutil,zipfile
ROOT=Path(__file__).resolve().parents[1]
def sha(path):
    h=hashlib.sha256()
    with path.open('rb') as f:
        for b in iter(lambda:f.read(1024*1024),b''):h.update(b)
    return h.hexdigest()
def main():
    candidate=json.loads((ROOT/'reports/RELEASE_CANDIDATE.json').read_text())
    if candidate['status']!='READY_FOR_VERIFICATION':raise SystemExit('Build candidate identity first')
    dist=ROOT/'dist/StrikeLedger'
    for platform,exe in [('windows','StrikeLedger.exe'),('linux','StrikeLedger.x86_64')]:
        folder=dist/platform
        if not (folder/exe).is_file():raise SystemExit('Missing native export: '+str(folder/exe))
        for file in (ROOT/'release_docs').rglob('*'):
            if file.is_file():
                target=folder/'docs'/file.relative_to(ROOT/'release_docs');target.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(file,target)
        shutil.copyfile(ROOT/'reports/RELEASE_CANDIDATE.json',folder/'RELEASE_CANDIDATE.json')
        for name in ['UPGRADE_STATUS.json','UPGRADE_REVIEW.md']:
            source=ROOT/'reports'/name
            if source.is_file():shutil.copyfile(source,folder/'docs'/name)
        (folder/'README.txt').write_text('PENNY PUNCHERS\n\nLaunch '+exe+'. Keep all files in this directory together.\nRead docs/README.md and docs/CONTROLS.md for controls and play modes.\nRead docs/KNOWN_LIMITATIONS.md for the exact verification boundaries.\n',encoding='utf-8')
        entries=[{'path':p.relative_to(folder).as_posix(),'bytes':p.stat().st_size,'sha256':sha(p)} for p in sorted(folder.rglob('*')) if p.is_file() and p.name!='SHA256SUMS.txt']
        (folder/'SHA256SUMS.txt').write_text(''.join(e['sha256']+'  '+e['path']+'\n' for e in entries),encoding='utf-8')
        target=ROOT/'dist'/f'Penny-Punchers-{platform}-x86_64.zip'
        with zipfile.ZipFile(target,'w',zipfile.ZIP_DEFLATED,compresslevel=5) as archive:
            for p in sorted(folder.rglob('*')):
                if p.is_file():
                    info=zipfile.ZipInfo.from_file(p,Path('Penny-Punchers')/p.relative_to(folder))
                    info.compress_type=zipfile.ZIP_DEFLATED
                    if platform=='linux':info.create_system=3;info.external_attr=((0o100755 if p.name==exe else 0o100644)<<16)
                    archive.writestr(info,p.read_bytes())
        print(json.dumps({'platform':platform,'archive':str(target.relative_to(ROOT)),'sha256':sha(target),'files':len(entries)+1,'uncompressed_bytes':sum(e['bytes'] for e in entries)}))
if __name__=='__main__':main()
