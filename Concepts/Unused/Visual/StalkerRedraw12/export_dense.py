"""Whole-cell exports only. No part cuts/paint/deformation or invented frames."""
from pathlib import Path
from PIL import Image,ImageFilter,ImageDraw
import json,hashlib,numpy as np,sys
OUT=Path(__file__).resolve().parent
VARIANT=sys.argv[1] if len(sys.argv)>1 else 'r1'
SPECS={'movement':(6,6),'sleep_entry':(5,4),'awakening':(5,4),'attack':(8,4)}
records={}
def complete_creature_rects(im,cols,rows):
    a=np.array(im)[:,:,3];mask=a>128;seen=np.zeros(mask.shape,bool);components=[]
    h,w=mask.shape
    for y,x in zip(*np.nonzero(mask)):
        if seen[y,x]:continue
        q=[(int(y),int(x))];seen[y,x]=1;points=[]
        while q:
            py,px=q.pop();points.append((px,py))
            for dy,dx in [(1,0),(-1,0),(0,1),(0,-1)]:
                ny,nx=py+dy,px+dx
                if 0<=ny<h and 0<=nx<w and mask[ny,nx] and not seen[ny,nx]:seen[ny,nx]=1;q.append((ny,nx))
        if len(points)>500:
            pts=np.array(points);mean=pts.mean(0)
            index=min(rows-1,int(mean[1]/h*rows))*cols+min(cols-1,int(mean[0]/w*cols))
            lo=pts.min(0);hi=pts.max(0)
            rect=(max(0,int(lo[0])-4),max(0,int(lo[1])-4),min(w,int(hi[0])+5),min(h,int(hi[1])+5))
            components.append((index,rect,points))
    assert len(components)==cols*rows,(len(components),cols*rows)
    assert len(set(x[0] for x in components))==len(components)
    for index,rect,pts in components:
        x0,y0,x1,y1=rect
        for other,_,otherpts in components:
            if other!=index:assert not any(x0<=x<x1 and y0<=y<y1 for x,y in otherpts),(index,other,'whole rectangles overlap another creature')
    return {i:rect for i,rect,_ in components}
for state,(cols,rows) in SPECS.items():
    if VARIANT=='r2' and state=='awakening':continue
    path=OUT/'DenseTrials'/f'{state}-{VARIANT}.png';im=Image.open(path).convert('RGBA');frames=[]
    complete=complete_creature_rects(im,cols,rows)
    for i in range(cols*rows):
        col,row=i%cols,i//cols
        # Generated sheets do not have mathematically exact grid placement.
        # Take one rectangle around one COMPLETE connected creature. Never
        # separates a limb, assembles skin, moves pixels or changes RGB.
        bounds=complete[i]
        cell=im.crop(bounds);ar=np.array(cell);a=ar[:,:,3]
        near=np.array(Image.fromarray((a>2).astype('uint8')*255).filter(ImageFilter.MaxFilter(9)))
        removed=(a>0)&(a<=2)&(near==0);ar[removed,3]=0;cell=Image.fromarray(ar)
        folder=OUT/('DenseTrials/frames' if VARIANT=='r1' else f'DenseTrials/{VARIANT}/frames')/state;folder.mkdir(parents=True,exist_ok=True)
        file=folder/f'{i:02d}.png';cell.save(file)
        matte=Image.new('RGBA',cell.size,'#e5e6e2');matte.alpha_composite(cell)
        matte.convert('RGB').save(folder/f'{i:02d}-matte.png')
        edge=np.r_[a[0,:],a[-1,:],a[:,0],a[:,-1]]
        raw=np.array(im.crop(bounds));now=np.array(cell)
        assert np.array_equal(raw[:,:,:3],now[:,:,:3])
        assert np.array_equal(raw[:,:,3][raw[:,:,3]>=3],now[:,:,3][raw[:,:,3]>=3])
        frames.append({'index':i,'file':file.relative_to(OUT).as_posix(),'sourceWholeCreatureRect':list(bounds),'nativeSize':list(cell.size),'sha256':hashlib.sha256(file.read_bytes()).hexdigest(),'removedDetachedAlpha1or2':int(removed.sum()),'opaquePixelsOnCellEdge':int((edge>128).sum()),'rgbExactToWholeSourceRect':True,'alphaAtLeast3ExactToWholeSourceRect':True})
    records[state]={'sheet':str(path.relative_to(OUT)).replace('\\','/'),'sheetSize':list(im.size),'grid':[cols,rows],'frames':frames}
if VARIANT=='r2':records['awakening']={'sharedWithSleepEntry':True,'note':'Reversed complete20new PNG poses, not20additional unique drawings.','frames':list(reversed(records['sleep_entry']['frames']))}
(OUT/f'DenseTrials/native-frames-{VARIANT}.json').write_text(json.dumps(records,indent=2),encoding='utf-8')
if VARIANT=='r1':(OUT/'DenseTrials/native-frames.json').write_text(json.dumps(records,indent=2),encoding='utf-8')
print(json.dumps({s:{'frames':len(v['frames']),'sheetSize':v.get('sheetSize'),'nativeSize':v['frames'][0]['nativeSize'],'clippedCells':[f['index'] for f in v['frames'] if f['opaquePixelsOnCellEdge']]} for s,v in records.items()}))
