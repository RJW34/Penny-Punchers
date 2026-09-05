"""Validate completed exported game evidence; never substitutes for visual review."""
from pathlib import Path
import argparse, hashlib, json, struct, subprocess, zlib

ROOT = Path(__file__).resolve().parents[2]

def digest(path):
    h = hashlib.sha256()
    with path.open('rb') as f:
        while chunk := f.read(1024 * 1024): h.update(chunk)
    return h.hexdigest()

def png(path):
    data = path.read_bytes()
    assert data[:8] == b'\x89PNG\r\n\x1a\n', path
    pos, compressed, dims, ended = 8, bytearray(), None, False
    while pos < len(data):
        size = struct.unpack('>I', data[pos:pos+4])[0]
        kind, body = data[pos+4:pos+8], data[pos+8:pos+8+size]
        crc = struct.unpack('>I', data[pos+8+size:pos+12+size])[0]
        assert zlib.crc32(kind+body) & 0xffffffff == crc, path
        if kind == b'IHDR': dims = struct.unpack('>II', body[:8])
        if kind == b'IDAT': compressed.extend(body)
        pos += 12 + size
        if kind == b'IEND': ended = True; break
    assert ended and dims in ((1280,720),(960,540)), (path, dims)
    decoded = zlib.decompress(compressed)
    assert len(set(decoded)) > 32, 'Unexpected blank/low-content screenshot: '+str(path)
    return dict(path=str(path.relative_to(ROOT)), width=dims[0], height=dims[1], sha256=digest(path), crc_valid=True)

def main():
    parser = argparse.ArgumentParser(); parser.add_argument('mode', choices=['ui','showcase','art','match','free-kit','network','linux']); parser.add_argument('--peer',type=int,choices=[0,1]); parser.add_argument('--convert',action='store_true'); parser.add_argument('--label'); parser.add_argument('--runtime-root',type=Path); args = parser.parse_args()
    label = args.label or args.mode
    assert label and all(c in 'abcdefghijklmnopqrstuvwxyz0123456789-_' for c in label), 'Invalid evidence label'
    folder = ROOT/'reports/evidence'/('native-'+label)
    if args.mode == 'network':
        assert args.peer is not None, 'Network evidence requires --peer 0 or 1'
        folder /= 'peer'+str(args.peer)
    process = json.loads((folder/'process-result.json').read_text())
    result_name = {'art':'art-review.json','ui':'controller-menu-flow.json','showcase':'combat-visual-showcase.json','network':'native-network-result.json'}.get(args.mode,'runtime-result.json')
    result = json.loads((folder/result_name).read_text())
    assert process['passed'] and process['exit_code'] == 0 and not process.get('errors',[]), process
    if args.mode == 'art':
        assert result['passed'] and all(c['passed'] for c in result['checks'])
        assert len({(c['fighter'],c['move']) for c in result['checks'] if 'move' in c}) == 98
        assertion_count=len(result['checks'])
    elif args.mode in ('ui','showcase'):
        assert result['success'] and len(result['checks']) in ((63,75) if args.mode == 'ui' else (13,))
        assert all(c['passed'] for c in result['checks'])
        assertion_count = len(result['checks'])
        if args.mode == 'ui' and assertion_count == 75:
            names = {c['name'] for c in result['checks']}
            assert {'CPU selector and navigation hint do not overlap', 'Completed replay verifies every hash and wallet before result display', 'Controller result rematch creates a fresh match', 'Rematch resets both wallets to 600 CR', 'Rematch resets points, leases and round number'} <= names
            assert (folder/'ui-completed-match.replay.json').exists()
    else:
        assert result.get('passed',result.get('complete',False)), result
        assert 1 <= result['rounds'] <= 9 and (max(result['scores']) >= 10 or result['rounds'] == 9)
        assert len(result['wallets']) == 2 and all(0 <= x <= 3600 for x in result['wallets'])
        assertion_count = 3
        if args.mode == 'network':
            assert result['finalHash'] == result['replayHash'] and result['seat'] == args.peer
            other = folder.parent/('peer'+str(1-args.peer))
            other_process = json.loads((other/'process-result.json').read_text())
            other_result = json.loads((other/'native-network-result.json').read_text())
            assert other_process['passed'] and other_process['exit_code'] == 0 and not other_process.get('errors',[])
            assert other_result['passed'] and all(result[k] == other_result[k] for k in ['finalHash','replayHash','wallets','scores','rounds','build','content'])
            assertion_count += 2
        if args.mode == 'free-kit':
            assert result['freeKit']
            replay = json.loads((folder/'full-match.replay.json').read_text()); commands = replay['Commands']
            assert not replay['Header']['Config']['Training'] and not replay['Header']['Config']['Assist']
            plans = [c for c in commands if c['Kind'] == 'preparation']
            assert len(plans) == result['rounds'] and all(not c['Plan0']['ItemIds'] and not c['Plan1']['ItemIds'] for c in plans)
            assert all(not c.get('Debits',[]) for c in commands)
            assert all(all(b[k] >= a[k] for k in ('Wallet0','Wallet1')) for a,b in zip(commands,commands[1:]))
            assert commands[-1]['Hash'] == result['finalHash']
            assertion_count += 5
    log = (folder/'process.log').read_text(encoding='utf-8', errors='replace')
    assert not any(s in log for s in ['ERROR:', 'Unhandled exception', 'Leaked instance:', 'instances leaked', 'resources still in use'])
    screenshots = [png(p) for p in sorted(folder.glob('*.png'))]
    if args.mode=='art': assert len(screenshots)==len(result['captures']) and len(screenshots)>=32
    elif args.mode in ('ui','showcase'): assert len(screenshots) == ((11 if assertion_count == 75 else 8) if args.mode == 'ui' else 14), len(screenshots)
    else:
        assert len(screenshots) >= 4
        assert (folder/('native-network-result.png' if args.mode == 'network' else 'match-result.png')).exists()
    movie = folder/'recording.mp4'
    if args.convert:
        subprocess.run(['ffmpeg','-nostdin','-hide_banner','-loglevel','warning','-y','-threads','2','-i',str(folder/'recording.avi'),'-c:v','libx264','-threads','2','-preset','fast','-crf','20','-pix_fmt','yuv420p','-c:a','aac','-b:a','160k','-movflags','+faststart',str(movie)],check=True)
    def probe(path):
        return json.loads(subprocess.check_output(['ffprobe','-v','error','-show_entries','format=duration,size:stream=codec_name,codec_type,width,height,r_frame_rate,sample_rate,channels,nb_frames','-of','json',str(path)]))
    source = probe(folder/'recording.avi'); metadata = probe(movie)
    video = next(s for s in metadata['streams'] if s.get('codec_name') == 'h264')
    original_video = next(s for s in source['streams'] if s.get('codec_type') == 'video')
    assert all(video[k] == original_video[k] for k in ['width','height','r_frame_rate','nb_frames']), (source, metadata)
    assert all((p['width'],p['height']) == (video['width'],video['height']) for p in screenshots)
    if args.mode in ('ui','showcase','art'): assert (video['width'],video['height'],video['r_frame_rate']) == (1280,720,'60/1')
    assert any(s.get('codec_name') == 'aac' and s.get('channels') == 2 for s in metadata['streams'])
    duration = float(metadata['format']['duration'])
    if args.mode=='art': assert duration>10 and abs(duration-float(source['format']['duration']))<.1
    elif args.mode in ('ui','showcase'): assert (10 < duration < 40) if args.mode == 'ui' else (56 < duration < 60)
    else: assert duration > 30 and abs(duration-float(source['format']['duration'])) < .1
    subprocess.run(['ffmpeg','-v','error','-threads','2','-i',str(movie),'-map','0:v:0','-f','null','-'], check=True)
    windows = args.runtime_root.resolve() if args.runtime_root else ROOT/'dist/StrikeLedger'/('linux' if args.mode == 'linux' else 'windows')
    managed = windows/('data_StrikeLedger_linuxbsd_x86_64' if args.mode == 'linux' else 'data_StrikeLedger_windows_x86_64')
    executable = windows/('StrikeLedger.x86_64' if args.mode == 'linux' else 'StrikeLedger.exe')
    runtime = [executable,windows/'StrikeLedger.pck']+[managed/(s+'.dll') for s in ['StrikeLedger','StrikeLedger.Core','StrikeLedger.App']]
    hashes = {str(p.relative_to(ROOT)):digest(p) for p in runtime+[movie,folder/'process.log',folder/'process-result.json',folder/result_name]}
    if 'exe_sha256' in process: assert hashes[str(executable.relative_to(ROOT))] == process['exe_sha256']
    else:
        assert str(windows/'StrikeLedger.exe') in log
        baseline = json.loads((ROOT/'reports/evidence/native-ui/media-validation.json').read_text())['sha256']
        assert all(hashes[str(p.relative_to(ROOT))] == baseline[str(p.relative_to(ROOT))] for p in runtime), 'Candidate changed since UI acceptance'
    report = dict(passed=True,mode=args.mode,peer=args.peer,process_exit=process['exit_code'],process_supervision=process.get('supervision_note','Original process supervisor'),assertions=assertion_count,pngs=screenshots,movie=metadata,source_movie=source,complete_video_decode=True,sha256=hashes,physical_device_claim=False,visual_review='Separate human/model image inspection; CRC/decode checks alone do not establish visual correctness.')
    (folder/'media-validation.json').write_text(json.dumps(report,indent=2))
    print(json.dumps(dict(passed=True,mode=args.mode,peer=args.peer,assertions=assertion_count,pngs=len(screenshots),seconds=duration,mp4_sha256=digest(movie))))

if __name__ == '__main__': main()
