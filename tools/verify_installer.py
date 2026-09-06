"""Exercise the real 1.0 installer, installed game, reinstall and uninstaller."""
from pathlib import Path
from datetime import datetime, timezone
import hashlib, json, os, subprocess, time, traceback, winreg

ROOT = Path(__file__).resolve().parents[1]
EVIDENCE = ROOT / 'reports/evidence/installer-1.0'
REGISTRY = r'Software\Microsoft\Windows\CurrentVersion\Uninstall\{E20D99CC-399A-4C0C-B661-DA18FB522236}_is1'


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def registration():
    try:
        with winreg.OpenKey(winreg.HKEY_CURRENT_USER, REGISTRY, 0, winreg.KEY_READ | winreg.KEY_WOW64_64KEY) as key:
            return {name:winreg.QueryValueEx(key, name)[0] for name in ['DisplayName', 'DisplayVersion', 'InstallLocation']}
    except FileNotFoundError:
        return None


def ps(code):
    return json.loads(subprocess.check_output(['powershell.exe', '-NoProfile', '-Command', code], text=True, encoding='utf-8-sig'))


def main():
    report = json.loads((EVIDENCE / 'build-result.json').read_text())
    installer = ROOT / report['installer']['path']
    assert report['passed'] and sha(installer) == report['installer']['sha256']
    assert registration() is None, 'An existing Penny Punchers installation must not be replaced by this test'
    folders = ps("@{programs=[Environment]::GetFolderPath('Programs');desktop=[Environment]::GetFolderPath('Desktop');appdata=[Environment]::GetFolderPath('ApplicationData')} | ConvertTo-Json -Compress")
    group = Path(folders['programs']) / 'Penny Punchers'
    desktop = Path(folders['desktop']) / 'Penny Punchers.lnk'
    assert not group.exists() and not desktop.exists(), 'Existing shortcuts must not be overwritten'
    settings = Path(folders['appdata']) / 'Godot/app_userdata/Penny Punchers/settings.json'
    settings_before = sha(settings) if settings.exists() else None
    test_id = datetime.now(timezone.utc).strftime('%Y%m%dT%H%M%S%fZ')
    test_root = (ROOT / '.tools/installer-tests').resolve()
    install = (test_root / test_id / 'Penny Punchers').resolve()
    assert install.is_relative_to(test_root) and not install.exists()
    attempt = EVIDENCE / 'attempts' / test_id
    attempt.mkdir(parents=True, exist_ok=False)
    source_bytes = Path(__file__).read_bytes()
    source_sha256 = hashlib.sha256(source_bytes).hexdigest()
    (attempt / 'verify_installer.py').write_bytes(source_bytes)
    processes = []
    install_started = False
    failure = None
    cleanup = dict(attempted=False, reason='Normal verification includes uninstall.')

    def run(name, command, timeout=600):
        started = time.monotonic()
        entry = dict(name=name, command=command, exit_code=None, timed_out=False)
        with (attempt / (name+'.log')).open('x', encoding='utf-8') as log:
            log.write(subprocess.list2cmdline(command)+'\n'); log.flush()
            p = None
            try:
                # subprocess.run(timeout) kills/reaps only its direct child. Inno
                # can launch a temporary child, so retain the PID and terminate
                # this test-owned tree before reaping the parent on timeout.
                p = subprocess.Popen(command, cwd=install if install.exists() else ROOT,
                                     stdout=log, stderr=subprocess.STDOUT)
                entry['pid'] = p.pid
                try:
                    entry['exit_code'] = p.wait(timeout=timeout)
                except (subprocess.TimeoutExpired, KeyboardInterrupt):
                    entry['timed_out'] = time.monotonic()-started >= timeout
                    if p.poll() is None:
                        kill_command = ['taskkill.exe', '/PID', str(p.pid), '/T', '/F']
                        try:
                            killed = subprocess.run(kill_command, stdout=log, stderr=subprocess.STDOUT,
                                                    timeout=15, creationflags=subprocess.CREATE_NO_WINDOW)
                            entry['terminate_owned_tree'] = dict(command=kill_command, exit_code=killed.returncode)
                        except Exception as error:
                            entry['terminate_owned_tree'] = dict(command=kill_command, error=str(error))
                        if p.poll() is None:
                            p.kill()
                    entry['exit_code'] = p.wait(timeout=15)
                    raise
                assert entry['exit_code'] == 0, name + ' failed'
            finally:
                entry['elapsed_seconds'] = round(time.monotonic()-started, 3)
                processes.append(entry)
        return entry

    options = ['/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART','/SP-','/NOCLOSEAPPLICATIONS', '/DIR='+str(install), '/TASKS=desktopicon']
    links = [group/'Penny Punchers.lnk', group/'Tester guide.lnk', group/'Uninstall Penny Punchers.lnk', desktop]
    result = dict(passed=False, version='1.0', candidate=report['candidate'], installer=report['installer'],
                  attempt=test_id, source_sha256=source_sha256, install_directory=str(install), processes=processes,
                  evidence_directory=attempt.relative_to(ROOT).as_posix(),
                  limits='Windows 10 x64 on this PC; no clean second-PC, physical-controller or human-acceptance claim.')
    try:
        install_started = True
        run('install-process', [str(installer), *options, '/LOG='+str(attempt/'install.log')])
        reg = registration()
        assert reg and reg['DisplayVersion'] == '1.0' and Path(reg['InstallLocation']).resolve() == install
        assert 'Administrative install mode: No' in (attempt/'install.log').read_text(encoding='utf-8-sig')

        def verify_payload():
            for entry in report['payload_files']:
                assert sha(install / entry['path']) == entry['sha256'], entry['path']

        verify_payload()
        assert all(p.is_file() for p in links)
        link_details = []
        for link in [links[0], desktop]:
            literal = str(link).replace("'", "''")
            detail = ps("$w=New-Object -ComObject WScript.Shell; $s=$w.CreateShortcut('"+literal+"'); @{target=$s.TargetPath;workingDirectory=$s.WorkingDirectory} | ConvertTo-Json -Compress")
            assert Path(detail['target']).resolve() == install/'StrikeLedger.exe'
            assert Path(detail['workingDirectory']).resolve() == install
            link_details.append(dict(path=str(link), **detail))

        marker = install/'tester-created-file.txt'
        marker.write_text('Reinstall and uninstall must preserve files created by the player.\n')
        marker_hash = sha(marker)
        run('reinstall-process', [str(installer), *options, '/LOG='+str(attempt/'reinstall.log')])
        verify_payload()
        assert sha(marker) == marker_hash
        game = install/'StrikeLedger.exe'
        match_dir = attempt/'installed-match'; match_dir.mkdir(exist_ok=False)
        run('installed-match', [str(game), '--headless', '--fixed-fps', '60', '--', '--smoke', '--evidence-dir', str(match_dir), '--no-screenshots'])
        match = json.loads((match_dir/'runtime-result.json').read_text())
        assert match['complete'] and match['rounds'] == 7 and match['finalHash'] == '8ac7ae2504ef3a9bab9e4b2fbee2e1664abdb1b0e995278c8db87beff5bc337e'
        ui_dir = attempt/'installed-ui'; ui_dir.mkdir(exist_ok=False)
        run('installed-ui', [str(game), '--rendering-method', 'gl_compatibility', '--max-fps', '60', '--', '--ui-smoke', '--evidence-dir', str(ui_dir)])
        ui = json.loads((ui_dir/'controller-menu-flow.json').read_text())
        assert ui['success'] and len(ui['checks']) == 299 and all(c['passed'] for c in ui['checks'])
        for log in ['installed-match.log', 'installed-ui.log']:
            assert not any(line.startswith('ERROR:') or 'Unhandled exception' in line for line in (attempt/log).read_text(encoding='utf-8', errors='replace').splitlines()), log
        run('uninstall-process', [str(install/'unins000.exe'), '/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART','/LOG='+str(attempt/'uninstall.log')])
        assert registration() is None
        assert all(not (install/entry['path']).exists() for entry in report['payload_files'])
        assert all(not link.exists() for link in links)
        assert sha(marker) == marker_hash
        assert (sha(settings) if settings.exists() else None) == settings_before, 'User settings changed'
        result.update(passed=True, registered_per_user=True, payload_files_verified=len(report['payload_files']),
                      shortcuts=link_details, reinstall_preserved_user_file=True, installed_match_rounds=match['rounds'],
                      installed_match_hash=match['finalHash'], installed_ui_checks=len(ui['checks']),
                      uninstall_removed_payload_shortcuts_and_registration=True, uninstall_preserved_user_file=True,
                      user_settings_unchanged=True)
    except BaseException as error:
        failure = error
        result['failure'] = dict(type=type(error).__name__, message=str(error), traceback=traceback.format_exc())
    finally:
        if failure is not None and install_started:
            cleanup = dict(attempted=False, passed=False)
            try:
                # Resolve again immediately before executing any uninstaller. A
                # changed junction or another installation's registration fails
                # closed; no fallback directory/shortcut/registry deletion occurs.
                resolved = install.resolve()
                expected = test_root / test_id / 'Penny Punchers'
                if resolved != expected or not resolved.is_relative_to(test_root):
                    raise RuntimeError('Cleanup target is outside the exact test-owned installation')
                reg = registration()
                if reg and Path(reg['InstallLocation']).resolve() != resolved:
                    raise RuntimeError('Cleanup refused: registration belongs to another installation')
                uninstaller = resolved / 'unins000.exe'
                if uninstaller.is_file():
                    if uninstaller.resolve().parent != resolved:
                        raise RuntimeError('Cleanup uninstaller resolves outside the test-owned installation')
                    cleanup['attempted'] = True
                    run('failure-cleanup-process', [str(uninstaller), '/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART',
                                                   '/LOG='+str(attempt/'failure-cleanup.log')], timeout=180)
                    cleanup['passed'] = registration() is None and all(not link.exists() for link in links)
                    cleanup['remaining_payload_files'] = [entry['path'] for entry in report['payload_files'] if (resolved/entry['path']).exists()]
                    cleanup['passed'] = cleanup['passed'] and not cleanup['remaining_payload_files']
                else:
                    cleanup['reason'] = 'No test-owned uninstaller exists; partial files preserved for inspection.'
                    cleanup['passed'] = reg is None and not any(link.exists() for link in links)
                cleanup['user_settings_unchanged'] = (sha(settings) if settings.exists() else None) == settings_before
            except BaseException as cleanup_error:
                cleanup['error'] = dict(type=type(cleanup_error).__name__, message=str(cleanup_error))
        result.update(utc=datetime.now(timezone.utc).isoformat(), cleanup=cleanup)
        result_file = attempt / 'verification-result.json'
        result_file.write_text(json.dumps(result, indent=2)+'\n', encoding='utf-8')
        # The top-level result is an atomic summary/pointer, not reused run input.
        summary = dict(result, result_path=result_file.relative_to(ROOT).as_posix(), result_sha256=sha(result_file))
        pointer = EVIDENCE / ('verification-result.'+test_id+'.tmp')
        pointer.write_text(json.dumps(summary, indent=2)+'\n', encoding='utf-8')
        os.replace(pointer, EVIDENCE/'verification-result.json')
    print(json.dumps(result, indent=2))
    if failure is not None:
        raise failure


if __name__ == '__main__':
    main()
