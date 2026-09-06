from pathlib import Path
import hashlib,json,sys
root=Path(__file__).resolve().parents[1]
def verify():
    m=json.loads((root/'MANIFEST.json').read_text())
    for entry in m['files']:
        p=(root/entry['path']).resolve()
        if not p.is_relative_to(root.resolve()) or not p.is_file():raise ValueError('Missing/unsafe '+entry['path'])
        b=p.read_bytes()
        if len(b)!=entry['bytes'] or hashlib.sha256(b).hexdigest()!=entry['sha256']:raise ValueError('Changed '+entry['path'])
    return len(m['files'])
if __name__=='__main__':
    try:print('PASS:',verify(),'manifested files')
    except Exception as e:print('FAIL:',e,file=sys.stderr);sys.exit(1)
