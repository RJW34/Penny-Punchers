"""Bind an existing successful isolated input-check run to its production source."""
from pathlib import Path
import hashlib, json

root = Path(__file__).resolve().parents[2]
log = root/'reports/evidence/input-settings-checks.log'
text = log.read_text(encoding='utf-8-sig')
assert 'BASELINE PASS 38:' in text and 'PASS 44 input/settings assertions' in text
def sha(path): return hashlib.sha256(path.read_bytes()).hexdigest()
candidate = json.loads((root/'reports/RELEASE_CANDIDATE.json').read_text())
source = 'game/GameSettings.cs'
expected = next(i['sha256'] for i in candidate['source_inputs'] if i['path'] == source)
assert sha(root/source) == expected, 'Production settings source changed from candidate'
paths = [source,'assets/presentation_checks/inputchecks/Program.cs','assets/presentation_checks/inputchecks/InputChecks.csproj','reports/evidence/input-settings-checks.log']
report = {
    'passed': True, 'exit_code': 0, 'assertions': 44, 'baseline_assertions': 38,
    'command': r'D:\cs-fighting-game\split-circuit-scaffold\tools\.cache\dotnet\dotnet.exe run --project assets/presentation_checks/inputchecks/InputChecks.csproj',
    'candidate_build_ref': candidate['build_ref'], 'production_source_unchanged': True,
    'baseline_coverage': [
        'All 16 simultaneous directional combinations, including neutral left/right and neutral up/down SOCD',
        'Finite/clamped audio and deadzone values; supported render resolution',
        'Malformed/null/duplicate key and pad arrays; conflict swaps; reserved-button refusal',
        'Independent pad arrays and eligible independent seat assignments; reject duplicate and disconnected assignments',
        'Injected logical key fallback, preserved physical layout and actual Pause key',
        'Held menu-confirm quarantine, unaffected fresh independent button, release and subsequent new press'
    ],
    'six_roundtrip_assertions': [
        'Audio and deadzone', 'Display and accessibility preferences', 'Remapped keyboard',
        'Two independent saved pad maps', 'Assigned device profiles', 'Transient fields excluded from persistence'
    ],
    'scope': 'The harness links production GameSettings.cs directly and uses its InputRouter static functions, JSON properties and Normalize. Round trip writes and reads one uniquely named temporary file and removes it. It does not invoke the live user-path Save/Load methods or overwrite user settings.',
    'physical_controller_tested': False, 'physical_unplug_tested': False,
    'sha256': {p: sha(root/p) for p in paths}
}
(root/'reports/evidence/input-settings-review.json').write_text(json.dumps(report,indent=2))
print(json.dumps({'passed':True,'assertions':44,'production_source_unchanged':True,'source_sha256':expected}))
