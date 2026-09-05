$ErrorActionPreference = "Stop"
Push-Location (Join-Path $PSScriptRoot "..")
try {
    python tools/validate_pack.py --strict-schema
    if ($LASTEXITCODE -ne 0) { throw "Scaffold validation failed" }
    python -m unittest discover -s reference/tests -v
    if ($LASTEXITCODE -ne 0) { throw "Reference/tool tests failed" }
    python tools/build_move_tables.py --check
    if ($LASTEXITCODE -ne 0) { throw "Derived move table drift" }
    python tools/verify_manifest.py
    if ($LASTEXITCODE -ne 0) { throw "Delivery manifest differs; inspect legitimate development changes" }
} finally { Pop-Location }
