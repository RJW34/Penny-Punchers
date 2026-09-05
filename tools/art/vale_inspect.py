"""Read-only raster inspection. Emits JSON metadata; never edits image pixels."""
from pathlib import Path
from collections import deque
import hashlib,json
import numpy as np
from PIL import Image
ROOT=Path(__file__).resolve().parents[2]
DEST=ROOT/'game/Assets/AfterHours/Fighters/Vale'
def inspect(path,cols,rows):
    im=Image.open(path);rgb=np.asarray(im.convert('RGB')).astype(np.int16)
    r,g,b=rgb[:,:,0],rgb[:,:,1],rgb[:,:,2]
    matte=(r>155)&(b>150)&(g<110)&((r-g)>75)&((b-g)>75)
    h,w=matte.shape;seen=bytearray((~matte).astype(np.uint8).tobytes());components=[]
    for i in range(w*h):
        if not seen[i]:continue
        seen[i]=0;q=deque([i]);count=0;sx=sy=0;left=right=i%w;top=bottom=i//w
        while q:
            p=q.popleft();x=p%w;y=p//w;count+=1;sx+=x;sy+=y;left=min(left,x);right=max(right,x);top=min(top,y);bottom=max(bottom,y)
            for n in ((p-1 if x else -1),(p+1 if x<w-1 else -1),p-w,p+w):
                if 0<=n<w*h and seen[n]:seen[n]=0;q.append(n)
        if count>=8:components.append({'pixels':count,'rect':[left,top,right-left+1,bottom-top+1],'center':[sx/count,sy/count]})
    groups=[[] for _ in range(cols*rows)];anchors=[]
    for c in (c for c in components if c['pixels']>=3000):
        cx=min(cols-1,int(c['center'][0]/w*cols));cy=min(rows-1,int(c['center'][1]/h*rows));groups[cy*cols+cx].append(c)
        anchors.append((cy*cols+cx,c))
    for c in (c for c in components if c['pixels']<3000):
        # A spark above a head may cross the nominal grid edge. Associate it to
        # the nearest actual body, not the nominal row containing the spark.
        def distance(anchor):
            x,y,cw,ch=anchor[1]['rect'];cx,cy=c['center']
            return max(x-cx,0,cx-(x+cw))**2+max(y-cy,0,cy-(y+ch))**2
        index,_=min(anchors,key=distance);groups[index].append(c)
    cells=[]
    for index,parts in enumerate(groups):
        # Tiny disconnected pigment specks are discarded, while limbs/sparks remain.
        parts=[p for p in parts if p['pixels']>=12]
        if not parts:raise ValueError((path,index,'empty cell'))
        left=min(p['rect'][0] for p in parts);top=min(p['rect'][1] for p in parts)
        right=max(p['rect'][0]+p['rect'][2] for p in parts);bottom=max(p['rect'][1]+p['rect'][3] for p in parts)
        cells.append({'index':index+1,'rect':[left,top,right-left,bottom-top],'parts':len(parts),'foregroundPixels':sum(p['pixels'] for p in parts)})
    empty=rgb[matte];return {'file':path.name,'sha256':hashlib.sha256(path.read_bytes()).hexdigest(),'mode':im.mode,'width':w,'height':h,'alphaExtrema':im.getchannel('A').getextrema() if 'A' in im.getbands() else None,'matteFraction':float(matte.mean()),'keyPixelMin':empty.min(axis=0).tolist(),'keyPixelMax':empty.max(axis=0).tolist(),'cells':cells}
def main():
    result={'method':'Read-only connected component pixel inspection; no raster data is modified. Saturated magenta shader key; RGBA was requested but tool returned RGB.','boards':[inspect(DEST/(name+'-matte.png'),4,rows) for name,rows in [('universal',4),('normals',6),('techniques',4),('supplemental',6)]]}
    (DEST/'pixel-inspection.json').write_text(json.dumps(result,indent=2)+'\n')
    print(json.dumps(result,indent=2))
if __name__=='__main__':main()
