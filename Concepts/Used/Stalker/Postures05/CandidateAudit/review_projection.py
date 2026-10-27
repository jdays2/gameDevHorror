"""Inspect the actual shortest distal projection, independently of its gate."""
from pathlib import Path
import hashlib
import json
import sys
import numpy as np
from PIL import Image, ImageDraw

HERE=Path(__file__).resolve().parent
FAMILY=HERE.parent
sys.path.insert(0,str(FAMILY))
from sprite_painter import SpritePainter, ORIGIN, FLIP, PPU
from compose_parts import encode
from qa_parts import FONT


def main():
    source=(FAMILY/'poses.json').read_bytes()
    poses=json.loads(source)
    painter=SpritePainter(FAMILY.parents[3])
    out=HERE/'ProjectionReview'
    out.mkdir(exist_ok=True)
    ranked=[]
    for pose in poses:
        for leg in range(6):
            knee,toe=[np.asarray(pose[key][leg]) for key in ('knees','feet')]
            ranked.append({'row':pose['row'],'phase':pose['phase'],'leg':leg,
                'distalXY':float(np.linalg.norm(toe[:2]-knee[:2])),
                'distal3D':float(np.linalg.norm(toe-knee)),
                'distalDepthDelta':float(toe[2]-knee[2])})
    ranked.sort(key=lambda record:record['distalXY'])
    chosen=[(6,0,3),(7,0,3),(7,1,3),(7,2,3),(7,23,2),(7,24,2),(7,25,2),(8,0,3)]
    sheet=Image.new('RGB',(1600,1000),(0,255,0))
    draw=ImageDraw.Draw(sheet)
    records=[]
    for index,(row,phase,leg) in enumerate(chosen):
        pose=next(pose for pose in poses if pose['row']==row and pose['phase']==phase)
        whole,limbs,torso=painter.paint(pose)
        whole_image=encode(whole,green=True)
        limb_image=encode(limbs[leg],green=True)
        knee,toe=[np.asarray(pose[key][leg]) for key in ('knees','feet')]
        kp,tp=[ORIGIN+FLIP@point[:2]*PPU for point in (knee,toe)]
        distance=float(np.linalg.norm(tp-kp))
        low=painter.anatomy['limbs'][leg]
        distal=painter._transform(painter._parts[leg][1],low['knee'],low['tip'],knee,toe)
        a=distal[...,3]
        straight=np.divide(distal[...,:3],a[...,None],out=np.zeros_like(distal[...,:3]),where=a[...,None]>.001)
        white=(np.min(straight,axis=-1)>.5)&(a>.75)&(torso[...,3]<=.5)
        record=next(record.copy() for record in ranked if (record['row'],record['phase'],record['leg'])==(row,phase,leg))
        record['axisPixels']=distance
        record['nativeDistalOpaqueLightPixelsOutsideBody']=int(white.sum())
        record['sourceLowerLength']=low['lowerLength']
        records.append(record)
        x,y=index%4*400,index//4*500
        sheet.paste(whole_image.resize((392,392),Image.Resampling.LANCZOS),(x+4,y+42))
        draw.text((x+6,y+5),f'row{row} phase{phase:02d} {low["id"]}',fill='black',font=FONT)
        draw.text((x+6,y+24),f'axis {distance:.2f}px / light tissue {white.sum()}px',fill='black',font=FONT)
        if (row,phase) in [(7,0),(7,24)]:
            name=f'body-{row}-{phase:02d}-{low["id"]}'
            panel=Image.new('RGB',(1152,1000),(0,255,0))
            panel.paste(whole_image,(0,60))
            panel.paste(limb_image,(576,60))
            pd=ImageDraw.Draw(panel)
            pd.text((10,8),f'{name}: whole / isolated complete limb, same576 canvas',fill='black',font=FONT)
            pd.text((10,32),f'Projected axis{distance:.3f}px; actual lower length{record["distal3D"]:.7f}',fill='black',font=FONT)
            cx,cy=((kp+tp)*.5).round().astype(int)
            box=(cx-40,cy-40,cx+40,cy+40)
            panel.paste(whole_image.crop(box).resize((320,320),Image.Resampling.NEAREST),(70,660))
            panel.paste(limb_image.crop(box).resize((320,320),Image.Resampling.NEAREST),(646,660))
            panel.save(out/f'{name}.png')
            encode(whole).save(out/f'{name}-raw.png')
    sheet.save(out/'shortest-distal-neighbors.png')
    report={'sourcePosesSha256':hashlib.sha256(source).hexdigest(),'minimum':ranked[0],
        'worst16':ranked[:16],'rendered':records,
        'interpretation':'Pixel counts describe the actual native light distal paint outside body, not a substituted capsule. A numerical lower bound alone cannot approve the silhouette.'}
    (out/'projection-check.json').write_text(json.dumps(report,indent=2))
    print(json.dumps(report,indent=2))


if __name__=='__main__':
    main()
