# Windows installer

The 1.0 installer wraps the previously verified Windows native package. It keeps
all 188 native files unchanged and adds current tester instructions and notices.
No Godot/.NET SDK or runtime download is required by testers.

To reproduce, install Inno Setup 6.7.3 from its official download page, then obtain
the pinned Windows ZIP from this repository's `shop-only-v2-2026-09-05` release:

```powershell
gh release download shop-only-v2-2026-09-05 --repo RJW34/Penny-Punchers --pattern Penny-Punchers-windows-x86_64.zip --dir dist
python tools/build_installer.py --iscc "C:/Path/To/Inno Setup 6/ISCC.exe"
python tools/verify_installer.py
```

`build_installer.py` checks the published ZIP's SHA-256 and every Windows native
file against the tracked candidate manifest before compiling. Each attempt uses
a fresh staging directory. Output is `dist/Penny-Punchers-1.0-Windows-Setup.exe`;
compilation, file inventories and execution evidence are under
`reports/evidence/installer-1.0/`.

`verify_installer.py` installs into an isolated test directory, verifies all
payload files and shortcuts, runs an installed native match and UI exercise,
checks reinstall, then uninstalls. It refuses to overwrite an existing registered
installation or existing Start-menu shortcuts. No game user data is deleted.

The setup uses a stable application ID, per-user registration, an optional desktop
shortcut and the standard Windows uninstaller. No signing certificate is
configured: the built installer is unsigned. The 1.0 release retains the original
gameplay evidence identity and its explicit physical-device/human limitations.
