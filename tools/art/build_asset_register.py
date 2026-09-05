"""Inventory shipped After Hours source files without altering raster pixels."""
from pathlib import Path
import csv, hashlib, json
from PIL import Image
ROOT=Path(__file__).resolve().parents[2]
def main():
    path=ROOT/'assets/ASSET_REGISTER.csv'
    with path.open(newline='',encoding='utf-8') as f:
        reader=csv.DictReader(f);fields=reader.fieldnames;rows=list(reader)
    rows=[r for r in rows if not r['path'].startswith('game/Assets/AfterHours/')]
    for row in rows:
        if row['path']=='game/Presentation/ArenaView.cs':
            row.update(creation_method='Godot raster presentation adapter and live diagnostic overlay',modifications='After Hours stage/effect textures and explicit fighter cel selection; original vector fighter draw path replaced',verified_by='Native artwork review; legal-input showcase; software GUI flow',notes='Simulation coordinates and event delivery remain authoritative')
        elif row['path']=='game/Presentation/FoundryBackdrop.cs':
            row.update(notes='Historical prototype source retained; no longer instantiated by the game renderer',verified_by='Source reference search')
        elif row['path']=='game/Main.cs;game/Main.Menus.cs':
            row.update(creation_method='Live Godot HUD and controls with supplied bitmap frame/portrait/icon crops',modifications='After Hours interface skin; native default font and live currency/health text retained',verified_by='Native software controller flow and visual inspection')
    assets=[]
    for png in sorted((ROOT/'game/Assets/AfterHours').rglob('*.png')):
        rel=png.relative_to(ROOT).as_posix()
        fighter='/Fighters/' in rel
        with Image.open(png) as im:
            im.verify()
        with Image.open(png) as im:
            entry={'path':rel,'sha256':hashlib.sha256(png.read_bytes()).hexdigest(),'bytes':png.stat().st_size,'size':list(im.size),'mode':im.mode,'has_alpha':'A' in im.getbands(),'method':'built-in image_gen matte edit of supplied original fighter board' if fighter else 'byte-identical supplied original artwork; runtime rectangular crops'}
        assets.append(entry)
        rows.append(dict(zip(fields,[rel,'OpenAI ImageGen; art direction by Codex for this project','Original generated project artwork; no third-party source artwork supplied',entry['method'],'Runtime magenta key and costume palette shader; explicit rectangles/pivots' if fighter else 'Nearest texture rendering; live UI values are separate from decorative crops','Native After Hours render/UI/real-combat review','Prompts and source provenance preserved in design/after-hours-32bit and production fighter folders'])))
    with path.open('w',newline='',encoding='utf-8') as f:
        writer=csv.DictWriter(f,fields);writer.writeheader();writer.writerows(rows)
    result={'scope':'Actually shipped After Hours image resources only','assets':assets,'count':len(assets)}
    (ROOT/'assets/AFTER_HOURS_ASSETS.json').write_text(json.dumps(result,indent=2)+'\n')
    print(json.dumps({'count':len(assets),'bytes':sum(a['bytes'] for a in assets)}))
if __name__=='__main__':main()
