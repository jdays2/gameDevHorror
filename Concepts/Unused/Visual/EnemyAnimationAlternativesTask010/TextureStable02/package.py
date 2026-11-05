"""Whole-frame preview packaging and texture/motion measurements; no art paint."""
from pathlib import Path
import json,hashlib
import numpy as np
from PIL import Image,ImageDraw,ImageFont

ROOT=Path(__file__).resolve().parent;PROJECT=ROOT.parents[4]
VALID=PROJECT/'Docs/Validation/EnemyAnimationAlternativesTask010/TextureStable02'
PREV=ROOT.parent/'GenerationRound01'
plan=json.loads((ROOT/'skeleton.json').read_text(encoding='utf-8'))
new=[Image.open(ROOT/f'Frames/walk-{i:02}.png').convert('RGBA') for i in range(24)]
old=[Image.open(PREV/f'Selection/C-{i:02}.png').convert('RGBA') for i in range(6)]
durations=[31,31,32,31]*6
font=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',23)
small=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',17)
def comp(im,bg=(24,32,38,255)):
    out=Image.new('RGBA',im.size,bg);out.alpha_composite(im);return out
def apng(path,frames):
    frames[0].save(path,save_all=True,append_images=frames[1:],duration=durations,loop=0,disposal=0,blend=0)
apng(ROOT/'C-stable-walk.png',new)
compare=[];focus=[]
for i,im in enumerate(new):
    before=old[i//4]
    out=Image.new('RGBA',(1280,720),(24,32,38,255));out.alpha_composite(before,(0,55));out.alpha_composite(im,(640,55))
    d=ImageDraw.Draw(out);d.text((22,15),'Раньше: отдельный рисунок каждого кадра',font=font,fill='white')
    d.text((662,15),'Теперь: одна постоянная текстура',font=font,fill='white')
    d.text((22,690),'6 ключей · цикл 0,75 с',font=small,fill=(160,175,190));d.text((662,690),'24 позы · цикл 0,75 с · внеигровая проба',font=small,fill=(160,175,190))
    compare.append(out)
    box=(275,270,366,359)
    crop=Image.new('RGBA',(640,370),(24,32,38,255));cd=ImageDraw.Draw(crop)
    for x,source,title in ((0,before,'Раньше'),(320,im,'Постоянная текстура')):
        piece=comp(source.crop(box)).resize((300,294),Image.Resampling.NEAREST)
        crop.alpha_composite(piece,(x+10,50));cd.text((x+12,12),title,font=font,fill='white')
    focus.append(crop)
apng(ROOT/'compare-texture-walk.png',compare)
apng(ROOT/'compare-body-closeup.png',focus)
compare[0].save(ROOT/'compare-poster.png')

for start in (0,6,12,18):
    for name,bg in [('dark',(24,32,38,255)),('light',(235,234,229,255))]:
        sheet=Image.new('RGB',(1920,1340),bg[:3]);d=ImageDraw.Draw(sheet)
        bones=sheet.copy();bd=ImageDraw.Draw(bones)
        for j in range(6):
            i=start+j;x=j%3*640;y=j//3*670
            base=comp(new[i],bg).convert('RGB');sheet.paste(base,(x,y+30));d.text((x+12,y+6),f'{i:02} / 24',font=small,fill=(60,160,200))
            b=base.copy();bdr=ImageDraw.Draw(b)
            for p in plan['frames'][i]['points']:
                pts=[(320+p[k][0]*224,320-p[k][1]*224) for k in ('hip','knee','tip')]
                bdr.line(pts,fill=(40,220,255),width=2)
                for xx,yy in pts:bdr.ellipse((xx-3,yy-3,xx+3,yy+3),fill=(245,215,30))
            bones.paste(b,(x,y+30));bd.text((x+12,y+6),f'{i:02} / 24',font=small,fill=(60,160,200))
        sheet.save(VALID/f'all-{name}-{start//6}.png')
        if name=='dark':bones.save(VALID/f'all-bones-{start//6}.png')

rois=json.loads((VALID/'baseline-texture.json').read_text(encoding='utf-8'))['rois']
arr=np.array([np.asarray(im) for im in new]);measured={}
for name,r in rois.items():
    x0,y0,x1,y1=r['box'];a=arr[:,y0:y1,x0:x1,:3].astype(float)
    dif=np.abs(a-np.roll(a,1,axis=0));a6=a[::4];dif6=abs(a6-np.roll(a6,1,axis=0))
    measured[name]={'box':r['box'],'old6AdjacentRGB_MAE':r['meanAdjacentRGBDifference_0_255'],
        'new24AdjacentRGB_MAE':float(dif.mean()),'new6MatchedPhaseAdjacentRGB_MAE':float(dif6.mean()),
        'newMaxRGBChange':float(dif.max()),'newAllPixelsOpaque':bool((arr[:,y0:y1,x0:x1,3]==255).all()),
        'newMaxAlphaChange':int(np.abs(arr[:,y0:y1,x0:x1,3].astype(int)-np.roll(arr[:,y0:y1,x0:x1,3].astype(int),1,axis=0)).max()),
        'newAllAlphaAtLeast250':bool((arr[:,y0:y1,x0:x1,3]>=250).all()),
        'newAlphaRange':[int(arr[:,y0:y1,x0:x1,3].min()),int(arr[:,y0:y1,x0:x1,3].max())]}
invariants=json.loads((VALID.parent/'source-preservation.json').read_text(encoding='utf-8'))
preserved={path:hashlib.sha256((PROJECT/path).read_bytes()).hexdigest()==sha for path,sha in invariants['inputs'].items()}
assert all(preserved.values()),'Selected preexisting input changed; stop for actual source audit'
report={'status':'Temporal fixed-surface proof, not production or owner acceptance','frameCount':24,'uniqueRGBA':len({hashlib.sha256(im.tobytes()).hexdigest() for im in new}),
    'canvas':[640,640],'origin':[320,320],'cycleMs':sum(durations),'durationsMs':durations,'ROIs':measured,
    'masterSHA256Before':plan['masterSHA256'],'masterSHA256After':hashlib.sha256((ROOT/'master.png').read_bytes()).hexdigest(),
    'frameHashes':{f'walk-{i:02}.png':hashlib.sha256((ROOT/f'Frames/walk-{i:02}.png').read_bytes()).hexdigest() for i in range(24)},
    'selected17InputsPreserved':preserved,
    'APNGFramesExactRGBA':all(np.array_equal(np.asarray(Image.open(ROOT/'C-stable-walk.png').convert('RGBA')),arr[0]) for _ in [0]),
    'UnityInvoked':False,'ownerArtAcceptance':False,'gameBugFixed':False}
check=Image.open(ROOT/'C-stable-walk.png');assert check.n_frames==24
for i in range(24):check.seek(i);assert np.array_equal(np.array(check.convert('RGBA')),arr[i])
report['APNGFramesExactRGBA']=True
(VALID/'texture-verification.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
html='''<!doctype html><html lang="ru"><meta charset="utf-8"><title>Stalker · постоянная текстура</title><style>body{font:18px Arial;background:#182026;color:#eee;max-width:1280px;margin:24px auto}img{max-width:100%;height:auto}a{color:#8ed2f5}</style><h1>Сталкер: проверка постоянной текстуры</h1><p>Слева прежняя C, справа один мастер на непрерывной объёмной поверхности. Оба цикла 0,75 с. Результат вне игры; художественная приёмка не отмечена.</p><img src="compare-texture-walk.png"><h2>Корпус крупно</h2><img src="compare-body-closeup.png"><p><a href="C-stable-walk.png">Новый цикл на альфе</a> · <a href="fixed-material-volume.blend">Объёмная проба Blender</a> · <a href="README.md">Метод и ограничения</a></p></html>'''
(ROOT/'comparison.html').write_text(html,encoding='utf-8')
print(json.dumps({k:report[k] for k in ('uniqueRGBA','cycleMs','ROIs')},ensure_ascii=False,indent=2))
