"""Convert and fully decode an actual exported-game recording; never claims render pacing."""
from pathlib import Path
import argparse,datetime,hashlib,json,shutil,subprocess,sys

ROOT=Path(__file__).resolve().parents[1]
def sha(path):
 h=hashlib.sha256()
 with path.open('rb') as f:
  for block in iter(lambda:f.read(1024*1024),b''):h.update(block)
 return h.hexdigest()
def main():
 p=argparse.ArgumentParser();p.add_argument('folder',type=Path);a=p.parse_args();folder=a.folder.resolve()
 if not folder.is_relative_to(ROOT/'reports/evidence'):raise SystemExit('Movie folder must stay inside this repository evidence directory.')
 avi=folder/'recording.avi';mp4=folder/'recording.mp4';process=folder/'process-result.json'
 if not process.is_file():raise SystemExit('Actual native process evidence required.')
 native=json.loads(process.read_text(encoding='utf-8'))
 runtime=(ROOT/native['result']).resolve()
 if not runtime.is_relative_to(folder):raise SystemExit('Runtime result must belong to this movie directory.')
 if not all(x.is_file() for x in [avi,process,runtime]):raise SystemExit('Complete actual native recording/process/runtime evidence required.')
 if mp4.exists():raise SystemExit('Preserve existing movie: choose a fresh native evidence folder.')
 ffmpeg=shutil.which('ffmpeg');ffprobe=shutil.which('ffprobe')
 if not ffmpeg or not ffprobe:raise SystemExit('Existing ffmpeg and ffprobe must be available.')
 calls=[]
 def run(args,name):
  target=folder/name;result=subprocess.run(args,cwd=ROOT,stdout=subprocess.PIPE,stderr=subprocess.STDOUT,text=True,encoding='utf-8',errors='replace');target.write_text(result.stdout,encoding='utf-8');calls.append(dict(command=args,exitCode=result.returncode,log=target.relative_to(ROOT).as_posix()))
  if result.returncode:raise RuntimeError(f'{name}: exit {result.returncode}')
  return result.stdout
 state=json.loads(runtime.read_text(encoding='utf-8'))
 complete=state.get('complete',state.get('success',state.get('passed',False)))
 if native.get('passed') is not True or native.get('exit_code')!=0 or complete is not True:raise SystemExit('Native process or selected scenario did not complete.')
 if state.get('display')=='headless' or '--write-movie' not in native.get('command',[]):raise SystemExit('Graphical movie process evidence required.')
 run([ffmpeg,'-hide_banner','-threads','2','-i',str(avi),'-c:v','libx264','-threads','2','-preset','fast','-crf','18','-pix_fmt','yuv420p','-c:a','aac','-b:a','160k','-movflags','+faststart',str(mp4)],'movie-conversion.log')
 metadata=json.loads(run([ffprobe,'-v','error','-show_format','-show_streams','-of','json',str(mp4)],'movie-probe.json'))
 videos=[s for s in metadata['streams'] if s['codec_type']=='video'];audios=[s for s in metadata['streams'] if s['codec_type']=='audio'];duration=float(metadata['format']['duration'])
 if len(videos)!=1 or duration<=1 or videos[0]['width']<320 or videos[0]['height']<180:raise RuntimeError('Unexpected actual movie dimensions/duration.')
 run([ffmpeg,'-v','error','-threads','2','-i',str(mp4),'-threads','2','-f','null','-'],'movie-full-decode.log')
 captures=[]
 for index,fraction in enumerate([.08,.28,.52,.76,.97]):
  capture=folder/f'movie-review-{index+1}.png';run([ffmpeg,'-v','error','-ss',str(duration*fraction),'-i',str(mp4),'-frames:v','1',str(capture)],f'movie-frame-{index+1}.log');captures.append(capture)
 tracked=[avi,mp4,process,runtime,*captures]
 hashes={p.relative_to(ROOT).as_posix():sha(p) for p in tracked};hashes.update(native.get('runtime_sha256',{}))
 result=dict(format='native-game-movie-validation',utc=datetime.datetime.now(datetime.timezone.utc).isoformat(),passed=True,mode=native['mode'],complete_video_decode=True,exit_code=0,platform=state.get('platform'),display=state.get('display'),gpu=state.get('gpu'),build=state.get('build'),content=state.get('content'),completedRounds=state.get('rounds'),completedTicks=state.get('ticks'),durationSeconds=duration,video=videos[0],audioStreams=audios,sha256=hashes,captures=[p.name for p in captures],commands=calls,boundary='Actual graphical native export completed the selected scenario and produced these recorded frames. Full-match claims apply only to match/free-kit/network/Linux modes with complete match results. Movie fixed-FPS timing is not a render-performance measurement. Linux under WSLg is the same physical PC; no second-machine or physical-controller claim. Captures require separate visual inspection.')
 (folder/'media-validation.json').write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8');print(json.dumps({k:result[k] for k in ['passed','platform','display','gpu','completedRounds','completedTicks','durationSeconds','captures']},indent=2))
if __name__=='__main__':main()
