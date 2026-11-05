from pathlib import Path
import json, hashlib
from collections import deque
import numpy as np
from PIL import Image,ImageDraw,ImageFont
R=Path(__file__).resolve().parent
V=R.parents[4]/'Docs/Validation/EnemyAnimationAlternativesTask010/GenerationRound01/Selection'
V.mkdir(parents=True,exist_ok=True)
OUT=R/'Selection';OUT.mkdir(exist_ok=True)
font=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',18)
plan=json.loads((R/'walk-plan.json').read_text())
def pupil(image):
    a=np.array(image);h,w=a.shape[:2];yy,xx=np.indices((h,w))
    mask=(a[:,:,:3].max(2)<70)&(a[:,:,3]>128)&(xx>w*.36)&(xx<w*.66)&(yy>h*.33)&(yy<h*.66)
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
        ps=np.array(pixels);dims=ps.max(0)-ps.min(0)+1;area=len(ps)
        if 30<=area<=600 and max(dims)<36 and min(dims)>5 and area/(dims[0]*dims[1])>.45:
            cy,cx=ps.mean(0);choices.append((abs(cx-w/2)+abs(cy-h*.48),cx,cy))
    assert choices
    return min(choices)[1:]
branches={};manifest={}
for branch in 'ABC':
    frames=[];entries=[]
    for i in range(6):
        single=R/f'{branch}-frame-{i:02}-r3.png'
        canvas=Image.new('RGBA',(640,640))
        if single.exists():
            raw=Image.open(single).convert('RGBA')
            # Uniform WHOLE canvas normalization, not anatomy fitting.
            im=raw.resize((576,576),Image.Resampling.LANCZOS)
            cx,cy=pupil(im);offset=(round(320-cx),round(320-cy));canvas.paste(im,offset)
            source=single;processing={'wholeCanvasScale':576/raw.width,'wholeCanvasTarget':[576,576],'translation':offset}
        else:
            source=R/f'{branch}-frame-{i:02}.png';im=Image.open(source).convert('RGBA');canvas.paste(im,(32,32))
            processing={'wholeCanvasScale':1,'translation':[32,32],'source':'exact r1 whole registered cell, no pixel painting'}
        canvas.save(OUT/f'{branch}-{i:02}.png');frames.append(canvas)
        entries.append({'frame':i,'source':source.name,'sourceSha256':hashlib.sha256(source.read_bytes()).hexdigest(),
                        'processing':processing,'noLimbCutOrUVWarp':True})
    frames[0].save(OUT/f'{branch}-walk.png',save_all=True,append_images=frames[1:],duration=125,loop=0,disposal=2)
    branches[branch]=frames;manifest[branch]=entries
    for bgname,bg in [('dark','#182027'),('light','#e8e5df'),('bones','#182027')]:
        sheet=Image.new('RGB',(1920,1340),bg)
        for i,im in enumerate(frames):
            comp=Image.new('RGBA',(640,640),bg);comp.alpha_composite(im);d=ImageDraw.Draw(comp)
            d.text((10,8),f'{branch} / {i:02}',font=font,fill='#5991b7')
            if bgname=='bones':
                for part in plan['frames'][i]['points']:
                    pts=[(part[k][0]+32,part[k][1]+32) for k in ('hip','knee','tip')]
                    d.line(pts,fill='#8edcff',width=2)
                    for x,y in pts:d.ellipse((x-3,y-3,x+3,y+3),fill='#f5d477')
            sheet.paste(comp.convert('RGB'),((i%3)*640,40+(i//3)*640))
        sheet.save(V/f'{branch}-all-{bgname}.png')
control=Image.open(R.parent/'control-stalker.png').convert('RGBA')
ref=Image.new('RGBA',(640,640));ref.paste(control,(32,32));ref.save(OUT/'original.png')
labels=['Исходный рисунок','A · по рисунку','B · по каркасу','C · по 3D-объёму']
preview=[]
for i in range(6):
    board=Image.new('RGB',(1280,720),'#182027');d=ImageDraw.Draw(board)
    d.text((20,15),'TASK-010 · три художественные пробы · один темп · не игровые серии',font=font,fill='white')
    for j,im in enumerate([ref,branches['A'][i],branches['B'][i],branches['C'][i]]):
        small=im.resize((320,320),Image.Resampling.LANCZOS)
        comp=Image.new('RGBA',(320,320),'#182027');comp.alpha_composite(small)
        board.paste(comp.convert('RGB'),(j*320,85))
        d.text((j*320+15,55),labels[j],font=font,fill='#d5dfe7')
    d.text((20,440),'6 ключевых поз в каждой ветке. Перерисованы проблемные кадры, сохранены старый дизайн и целое тело.',font=font,fill='#b7c9d3')
    d.text((20,475),'A — свободнее меняются блики; B — постановка по костям; C — объёмный ориентир и целая перерисовка.',font=font,fill='#b7c9d3')
    d.text((20,510),'Полноценные опоры, складывание и атака этим коротким тестом ещё не подтверждены.',font=font,fill='#e3bd85')
    preview.append(board)
preview[0].save(OUT/'compare-walk.png',save_all=True,append_images=preview[1:],duration=125,loop=0,disposal=2)
preview[0].save(OUT/'compare-poster.png')
(V/'sources-and-registration.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8')
print('Selection packaged; original source and r1/r2 evidence retained.')
