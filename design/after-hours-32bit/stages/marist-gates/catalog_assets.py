"""Catalog this additive stage. Reads images; writes only new-stage metadata."""
from pathlib import Path
import csv
from datetime import datetime, timezone
import hashlib
import json
import runpy

root = Path(__file__).resolve().parent
png_info = runpy.run_path(str(root.parents[1] / 'tools/verify_art_pack.py'))['png_info']
records = []
for name in ('background.png', 'panorama.png', 'select-card.png'):
    path = root / name
    info = png_info(path)
    records.append({'path': name, **info, 'sha256': hashlib.sha256(path.read_bytes()).hexdigest(),
                    'bytes': path.stat().st_size,
                    'source': 'Original built-in image_gen generation/edit; architectural guidance credited in SOURCES.md',
                    'author': 'OpenAI ImageGen; art direction by Codex for the project',
                    'status': 'source_art_for_future_stage_integration',
                    'prompts': 'prompts.json', 'runtime_integrated': False})
integration_path = root / 'integration.json'
integration = json.loads(integration_path.read_text(encoding='utf-8-sig'))
for entry in integration['art']['files']:
    record = next(x for x in records if x['path'] == entry['path'])
    entry['actual_native_dimensions'] = [record['width'], record['height']]
    entry['actual_format'] = record['format']
    entry['sha256'] = record['sha256']
integration_path.write_text(json.dumps(integration, indent=2) + '\n', encoding='utf-8')
proposal = json.loads((root / 'stage.proposed.json').read_text(encoding='utf-8-sig'))
foundry = json.loads((root.parents[3] / 'data/stages/foundry.json').read_text(encoding='utf-8-sig'))
geometry_keys = ('geometry','left','right','floor','spawn_x','camera_width','camera_tracking','camera_affects_collision','hazards','platforms','same_competitive_geometry')
assert all(proposal[k] == foundry[k] for k in geometry_keys), 'Proposed geometry differs from Foundry'
assert len(records) == 3
manifest = {'stage_id': 'marist_gates', 'display_name': 'Marist Gates — Blue Hour',
            'scope': 'traditional_fighter_only_additive_stage_pack', 'selected_pngs': 3,
            'generation_method': 'built-in image_gen', 'assets': records,
            'sources': 'SOURCES.md', 'runtime_integrated': False,
            'normalization_pending': True, 'separate_parallax_layers': False}
(root / 'ASSET_MANIFEST.json').write_text(json.dumps(manifest, indent=2) + '\n', encoding='utf-8')
with (root / 'ASSET_REGISTER.csv').open('w', encoding='utf-8', newline='') as out:
    keys = ('path','width','height','format','sha256','source','author','status','prompts')
    writer = csv.DictWriter(out, fieldnames=keys, extrasaction='ignore')
    writer.writeheader()
    writer.writerows(records)
report = {'verified_at_utc': datetime.now(timezone.utc).isoformat(), 'png_integrity_pass': True,
          'selected_pngs': 3, 'proposed_geometry_matches_foundry': True,
          'actual_dimensions': {r['path']: [r['width'],r['height']] for r in records},
          'observed_camera_crops': 'Panorama reviewed as full-width composition; normalized crop contract in integration.json. No runtime camera test claimed.',
          'existing_game_files_written': False, 'runtime_integrated': False}
(root / 'VERIFICATION.json').write_text(json.dumps(report, indent=2) + '\n', encoding='utf-8')
print(json.dumps(report, indent=2))
