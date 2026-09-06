"""Archive already tested self-contained verification tools; no test results synthesized."""
from pathlib import Path
import hashlib, json, zipfile

ROOT = Path(__file__).resolve().parents[1]


def main():
    folder = ROOT / 'dist/verification'
    proof = json.loads((ROOT / 'reports/evidence/shop-v2-packaged-verifiers/result.json').read_text())
    candidate = json.loads((ROOT / 'reports/RELEASE_CANDIDATE.json').read_text())
    assert proof['passed'] and proof['candidate'] == candidate['build_ref']
    output = ROOT / 'dist/Penny-Punchers-verification-tools.zip'
    files = sorted(p for p in folder.rglob('*') if p.is_file())
    assert files
    manifest = []
    with zipfile.ZipFile(output, 'w', zipfile.ZIP_DEFLATED, compresslevel=5) as archive:
        for path in files:
            rel = path.relative_to(folder).as_posix()
            payload = path.read_bytes()
            manifest.append(dict(path=rel, bytes=len(payload), sha256=hashlib.sha256(payload).hexdigest()))
            info = zipfile.ZipInfo.from_file(path, 'Penny-Punchers-verification/' + rel)
            info.compress_type = zipfile.ZIP_DEFLATED
            if 'linux' in rel.lower():
                info.create_system = 3
                info.external_attr = (0o100755 if not path.suffix else 0o100644) << 16
            archive.writestr(info, payload)
        archive.writestr('Penny-Punchers-verification/ARCHIVE_MANIFEST.json', json.dumps(dict(candidate=candidate['build_ref'], files=manifest), indent=2)+'\n')
    with zipfile.ZipFile(output) as archive:
        assert archive.testzip() is None
        for item in manifest:
            assert hashlib.sha256(archive.read('Penny-Punchers-verification/'+item['path'])).hexdigest() == item['sha256']
    result = dict(passed=True, candidate=candidate['build_ref'], archive=output.relative_to(ROOT).as_posix(), bytes=output.stat().st_size,
                  sha256=hashlib.sha256(output.read_bytes()).hexdigest(), files=len(files), crc_verified=True,
                  scope='Exact-byte archive of the six self-contained verifiers that completed the twelve recorded packaged checks; no additional gameplay execution claimed.')
    (ROOT / 'reports/evidence/shop-v2-verification-archive.json').write_text(json.dumps(result, indent=2)+'\n')
    print(json.dumps(result))


if __name__ == '__main__':
    main()
