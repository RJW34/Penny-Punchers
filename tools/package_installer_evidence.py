"""Archive actual installer verification reports and captures with a hash inventory."""
from pathlib import Path
import hashlib, json, zipfile

ROOT = Path(__file__).resolve().parents[1]


def main():
    folder = ROOT / 'reports/evidence/installer-1.0'
    result = json.loads((folder/'verification-result.json').read_text())
    assert result['passed'] and result['installed_ui_checks'] == 299
    actual = ROOT / result['result_path']
    assert hashlib.sha256(actual.read_bytes()).hexdigest() == result['result_sha256']
    source = ROOT/'tools/verify_installer.py'
    assert hashlib.sha256(source.read_bytes()).hexdigest() == result['source_sha256']
    output = ROOT/'dist/Penny-Punchers-1.0-Installer-Verification.zip'
    entries = []
    with zipfile.ZipFile(output, 'w', zipfile.ZIP_DEFLATED, compresslevel=5) as archive:
        for path in sorted(folder.rglob('*')):
            if not path.is_file() or path.suffix == '.tmp':
                continue
            assert not path.is_symlink() and path.resolve().is_relative_to(folder)
            payload = path.read_bytes()
            rel = path.relative_to(ROOT).as_posix()
            entries.append(dict(path=rel, bytes=len(payload), sha256=hashlib.sha256(payload).hexdigest()))
            archive.writestr(rel, payload)
        manifest = dict(format='penny-punchers-installer-verification-v1', candidate=result['candidate'],
                        installer=result['installer'], final_result=result['result_path'],
                        scope='Actual compiler, installation, installed-game and uninstall evidence. Earlier first-run artifacts are retained by their distinct paths; the final result points to an isolated verified attempt.', files=entries)
        archive.writestr('INSTALLER_EVIDENCE_MANIFEST.json', json.dumps(manifest, indent=2)+'\n')
    with zipfile.ZipFile(output) as archive:
        assert archive.testzip() is None
        for item in entries:
            assert hashlib.sha256(archive.read(item['path'])).hexdigest() == item['sha256']
    print(json.dumps(dict(passed=True, archive=output.relative_to(ROOT).as_posix(), bytes=output.stat().st_size,
                         sha256=hashlib.sha256(output.read_bytes()).hexdigest(), files=len(entries), crc_verified=True)))


if __name__ == '__main__':
    main()
