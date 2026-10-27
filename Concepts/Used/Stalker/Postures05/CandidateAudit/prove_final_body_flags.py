"""Exact current-mask evidence for all-body numeric flags; no art mutation."""
from pathlib import Path
import json,sys
import numpy as np
from PIL import Image,ImageDraw,ImageFont
HERE=Path(__file__).resolve().parent
FAMILY=HERE.parent
sys.path.insert(0,str(FAMILY))
from native_frame_painter import SpritePainter,uv_field,field_with_metrics,bilinear
from make_native_attack_pages import composite

def main():
    out=HERE/'FinalBody432QA01'
    poses={(p['row'],p['phase']):p for p in json.loads((FAMILY/'poses.json').read_text())}
    painter=SpritePainter(FAMILY.parents[3])
    cases=[('body-4-31',(304,244,351,290)),('body-3-31',(304,244,351,290)),
           ('body-3-28',(306,279,341,314)),('body-4-29',(306,279,341,314))]
    font=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',14)
    sheet=Image.new('RGB',(1010,260*len(cases)),(70,70,70))
    draw=ImageDraw.Draw(sheet)
    trace=[]
    torso=np.asarray(Image.open(FAMILY/'Masks/torso.png').convert('RGBA'))[...,3]
    for row,(name,box) in enumerate(cases):
        _,r,p=name.split('-'); pose=poses[(int(r),int(p))]
        raw=Image.open(FAMILY/'Raw'/f'{name}.png').convert('RGBA')
        draw.text((6,row*260+5),name+' | exact same crop, nearest x4; G / dark / light / masks RF:red RM:blue torso:white',font=font,fill='white')
        for col,bg in enumerate(([0,1,0],[.015,.015,.015],[.88,.88,.88])):
            crop=composite(raw,bg).crop(box).resize(((box[2]-box[0])*4,(box[3]-box[1])*4),Image.Resampling.NEAREST)
            sheet.paste(crop,(6+col*245,row*260+35))
        a=[np.asarray(Image.open(FAMILY/'Masks'/f'leg-{r}-{p}-{leg}.png').convert('RGBA'))[...,3] for leg in [1,3]]
        mask=np.zeros((576,576,3),dtype='uint8')
        mask[...,0]=a[0];mask[...,2]=a[1]
        mask[torso>128]=[255,255,255]
        sheet.paste(Image.fromarray(mask).crop(box).resize(((box[2]-box[0])*4,(box[3]-box[1])*4),Image.Resampling.NEAREST),(741,row*260+35))
        if row>=2:
            point=np.array([[323.,296.]])
            uv,metrics=field_with_metrics(painter.anatomy['limbs'][3],pose,3,point)
            s=bilinear(painter.sources[3],uv)[0]
            x,y=323,296
            trace.append({'name':name,'pixel':[x,y],'limbAlphaByte':int(a[1][y,x]),
                'torsoAlphaByte':int(torso[y,x]),'wholeAlphaByte':int(np.asarray(raw)[y,x,3]),
                'sourceUV':uv[0].tolist(),'sourceSampleAlpha':float(s[3]),
                'jacobian':float(metrics['jacobian'][0])})
    sheet.save(out/'body-contact-and-hole-contexts-4x.png')
    (out/'body-hole-trace.json').write_text(json.dumps(trace,indent=2))
    print(json.dumps(trace,indent=2))

if __name__=='__main__': main()
