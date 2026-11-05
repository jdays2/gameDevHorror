from pathlib import Path
import json,hashlib
from collections import deque
import numpy as np
from PIL import Image,ImageDraw,ImageFont
R=Path(__file__).resolve().parent
V=R.parents[4]/'Docs/Validation/EnemyAnimationAlternativesTask010/GenerationRound01'
V.mkdir(parents=True,exist_ok=True)
font=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',18)
plan=json.loads((R/'walk-plan.json').read_text())

def pupil(image):
    a=np.array(image);h,w=a.shape[:2]
    mask=(a[:,:,:3].max(2)<70)&(a[:,:,3]>128)
    yy,xx=np.indices(mask.shape)
    mask&=(xx>w*.36)&(xx<w*.66)&(yy>h*.33)&(yy<h*.66)
    seen=set();choices=[]
    for y,x in zip(*np.where(mask)):
        if (y,x) in seen:continue
        todo=deque([(int(y),int(x))]);seen.add((y,x));pixels=[]
        while todo:
            py,px=todo.popleft();pixels.append((py,px))
            for dy,dx in [(1,0),(-1,0),(0,1),(0,-1)]:
                qy,qx=py+dy,px+dx
                if 0<=qy<h and 0<=qx<w and mask[qy,qx] and (qy,qx) not in seen:
                    seen.add((qy,qx));todo.append((qy,qx))
        ps=np.array(pixels);lo=ps.min(0);hi=ps.max(0);dims=hi-lo+1;area=len(ps)
        if 30<=area<=600 and max(dims)<36 and min(dims)>5 and area/(dims[0]*dims[1])>.45:
            cy,cx=ps.mean(0);choices.append((abs(cx-w/2)+abs(cy-h*.48),cx,cy,area))
    assert choices,'Pupil landmark not found'
    return min(choices)[1:]

record={}
for branch in 'ABC':
    source=R/f'{branch}-walk-r1.png'
    raw=Image.open(source).convert('RGBA');cw,ch=raw.width//3,raw.height//2
    assert raw.width%3==0 and raw.height%2==0 and cw==ch,(source,raw.size)
    frames=[];entries=[]
    for i in range(6):
        rect=((i%3)*cw,(i//3)*ch,(i%3+1)*cw,(i//3+1)*ch)
        cell=raw.crop(rect);cx,cy,area=pupil(cell)
        x,y=round(288-cx),round(288-cy)
        assert x>=0 and y>=0 and x+cw<=640 and y+ch<=640
        canvas=Image.new('RGBA',(640,640));canvas.paste(cell,(x,y))
        assert np.array_equal(np.array(canvas.crop((x,y,x+cw,y+ch))),np.array(cell))
        canvas.save(R/f'{branch}-frame-{i:02}.png');frames.append(canvas)
        entries.append({'index':i,'sourceRect':rect,'pupil':[cx,cy],'pupilPixels':area,'integerTranslation':[x,y],
                        'pixelEdit':'NONE: whole-cell copy with integer translation only; no per-frame scaling/cutting limbs/UV/paint',
                        'sourceCellSize':[cw,ch]})
    frames[0].save(R/f'{branch}-walk-preview.png',save_all=True,append_images=frames[1:],duration=125,loop=0,disposal=2)
    for bgname,bg in [('dark','#182027'),('light','#e8e5df')]:
        sheet=Image.new('RGB',(1920,1340),bg)
        for i,im in enumerate(frames):
            comp=Image.new('RGBA',(640,640),bg);comp.alpha_composite(im)
            ImageDraw.Draw(comp).text((10,8),f'{branch} / {i:02}',font=font,fill='#5991b7')
            sheet.paste(comp.convert('RGB'),((i%3)*640,40+(i//3)*640))
        sheet.save(V/f'{branch}-all-{bgname}.png')
    bonepage=Image.new('RGB',(1920,1340),'#182027')
    for i,im in enumerate(frames):
        comp=Image.new('RGBA',(640,640),'#182027');comp.alpha_composite(im);d=ImageDraw.Draw(comp)
        for part in plan['frames'][i]['points']:
            pts=[tuple(part[k]) for k in ('hip','knee','tip')]
            d.line(pts,fill='#8edcff',width=2)
            for px,py in pts:d.ellipse((px-3,py-3,px+3,py+3),fill='#f5d477')
        d.text((10,8),f'{branch}/{i:02} fixed planned bones; NO fitting',font=font,fill='white')
        bonepage.paste(comp.convert('RGB'),((i%3)*640,40+(i//3)*640))
    bonepage.save(V/f'{branch}-all-bones.png')
    record[branch]={'source':source.name,'sha256':hashlib.sha256(source.read_bytes()).hexdigest(),'rawSize':raw.size,
                    'frames':entries,'registration':'Pupil-based integer whole-frame translation; no scaling; art drift remains visible.'}
(V/'registration.json').write_text(json.dumps(record,indent=2),encoding='utf-8')
print(json.dumps({k:[e['pupil'] for e in v['frames']] for k,v in record.items()},indent=2))
