"""Build the 1.0 Windows installer from a verified native ZIP, preserving game bytes."""
from pathlib import Path
from datetime import datetime, timezone
import argparse, hashlib, json, shutil, subprocess, zipfile

ROOT = Path(__file__).resolve().parents[1]
SOURCE_ARCHIVE = 'dist/Penny-Punchers-windows-x86_64.zip'
SOURCE_SHA256 = '8c67034ac169558370bb8b6f4b9add6937400ac3171bc60caae20b6ebfca14ff'


def sha(path):
    h = hashlib.sha256()
    with path.open('rb') as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b''):
            h.update(chunk)
    return h.hexdigest()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--iscc', type=Path, required=True)
    args = parser.parse_args()
    compiler = args.iscc.resolve()
    assert compiler.is_file(), compiler
    candidate = json.loads((ROOT / 'reports/RELEASE_CANDIDATE.json').read_text())
    source = ROOT / SOURCE_ARCHIVE
    assert sha(source) == SOURCE_SHA256, 'Verified player ZIP changed; obtain the pinned shop-only-v2 release asset'
    # Each attempt gets its own payload, avoiding stale files and recursive cleanup.
    attempt = datetime.now(timezone.utc).strftime('%Y%m%dT%H%M%S%fZ')
    stage = ROOT / '.tools/installer-1.0' / attempt / 'payload'
    stage.mkdir(parents=True)
    with zipfile.ZipFile(source) as archive:
        assert archive.testzip() is None
        for entry in archive.infolist():
            rel = Path(entry.filename)
            assert rel.parts[0] == 'Penny-Punchers' and '..' not in rel.parts
            if entry.is_dir():
                continue
            target = stage.joinpath(*rel.parts[1:]).resolve()
            assert target.is_relative_to(stage)
            target.parent.mkdir(parents=True, exist_ok=True)
            target.write_bytes(archive.read(entry))
    prefix = 'dist/StrikeLedger/windows/'
    native = [r for r in candidate['native_binaries'] if r['path'].startswith(prefix)]
    assert native
    for entry in native:
        assert sha(stage / entry['path'].removeprefix(prefix)) == entry['sha256']
    # Refresh only distribution instructions and notices; retain original evidence.
    for name in ['README.md', 'CONTROLS.md', 'KNOWN_LIMITATIONS.md', 'ASSET_NOTICES.md']:
        shutil.copyfile(ROOT / 'release_docs' / name, stage / 'docs' / name)
    shutil.copyfile(ROOT / 'RIGHTS.md', stage / 'docs/RIGHTS.md')
    shutil.copyfile(ROOT / 'installer/TESTER_GUIDE.txt', stage / 'TESTER_GUIDE.txt')
    (stage / 'VERSION.txt').write_text('Penny Punchers 1.0\n' + candidate['build_ref'] + '\n', encoding='utf-8')
    # Rebuild the package checksum inventory after these documentation additions.
    entries = [dict(path=p.relative_to(stage).as_posix(), bytes=p.stat().st_size, sha256=sha(p))
               for p in sorted(stage.rglob('*')) if p.is_file() and p.name != 'SHA256SUMS.txt']
    (stage / 'SHA256SUMS.txt').write_text(''.join(x['sha256']+'  '+x['path']+'\n' for x in entries), encoding='utf-8')
    entries.append(dict(path='SHA256SUMS.txt', bytes=(stage/'SHA256SUMS.txt').stat().st_size, sha256=sha(stage/'SHA256SUMS.txt')))
    evidence = ROOT / 'reports/evidence/installer-1.0'
    evidence.mkdir(parents=True, exist_ok=True)
    command = [str(compiler), '/DPayloadDir='+str(stage), '/DInstallerOutputDir='+str(ROOT/'dist'), str(ROOT/'installer/Penny-Punchers.iss')]
    with (evidence / 'compile.log').open('w', encoding='utf-8') as log:
        log.write(subprocess.list2cmdline(command)+'\n'); log.flush()
        result = subprocess.run(command, cwd=ROOT, stdout=log, stderr=subprocess.STDOUT)
    assert result.returncode == 0, 'Installer compilation failed; inspect compile.log'
    installer = ROOT / 'dist/Penny-Punchers-1.0-Windows-Setup.exe'
    report = dict(passed=True, utc=datetime.now(timezone.utc).isoformat(), version='1.0', candidate=candidate['build_ref'],
                  verified_source_zip=dict(path=SOURCE_ARCHIVE, sha256=SOURCE_SHA256),
                  installer=dict(path=installer.relative_to(ROOT).as_posix(), sha256=sha(installer), bytes=installer.stat().st_size),
                  compiler=dict(path=str(compiler), sha256=sha(compiler)), command=command, exit_code=result.returncode,
                  payload=str(stage), payload_files=entries, unchanged_native_files=len(native),
                  scope='Distribution wrapper and tester instructions only; all native candidate files retained byte-for-byte.')
    (evidence / 'build-result.json').write_text(json.dumps(report, indent=2)+'\n', encoding='utf-8')
    print(json.dumps({k:report[k] for k in ['passed','candidate','installer','unchanged_native_files']}))


if __name__ == '__main__':
    main()
