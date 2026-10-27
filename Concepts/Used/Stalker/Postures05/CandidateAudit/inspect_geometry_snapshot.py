"""Canonical painter evidence for the selected geometry, including tiny mounts."""
from pathlib import Path
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
    source=HERE/'GeometrySearch/guard20-fold12-10/poses.json'
    poses=json.loads(source.read_text())
    out=HERE/'GeometryDecision'
    (out/'Raw').mkdir(parents=True,exist_ok=True)
    (out/'Masks').mkdir(exist_ok=True)
    (out/'poses.json').write_text(json.dumps(poses))
    painter=SpritePainter(FAMILY.parents[3])
    chosen=[(0,0),(0,24),(4,0),(7,25),(8,0)]
    panel=Image.new('RGB',(1500,980),(0,255,0))
    draw=ImageDraw.Draw(panel)
    mounts=[]
    for index,(row,phase) in enumerate(chosen):
        pose=next(pose for pose in poses if pose['row']==row and pose['phase']==phase)
        whole,limbs,torso=painter.paint(pose)
        image=encode(whole,green=True)
        name=f'body-{row}-{phase:02d}'
        encode(whole).save(out/'Raw'/f'{name}.png')
        encode(torso).save(out/'Masks/torso.png')
        for leg,limb in enumerate(limbs):
            encode(limb).save(out/'Masks'/f'leg-{row}-{phase:02d}-{leg}.png')
        x=index%3*500
        y=index//3*490
        panel.paste(image.resize((460,460),Image.Resampling.LANCZOS),(x+20,y+28))
        draw.text((x+12,y+5),f'{name}: fixed canvas / six unchanged bone chains',fill='black',font=FONT)
        if row==8:
            image.save(out/'clean-fullfold.png')
        for a in range(6):
            for b in range(a+1,6):
                mask=(np.rint(limbs[a][...,3]*255)>128)&(np.rint(limbs[b][...,3]*255)>128)&(np.rint(torso[...,3]*255)<=128)
                if not mask.any():
                    continue
                yy,xx=np.nonzero(mask)
                center=np.array([xx.mean(),yy.mean()])
                starts=[ORIGIN+FLIP@np.asarray(pose['hips'][leg][:2])*PPU for leg in (a,b)]
                mounts.append({'name':name,'pair':[a,b],'pixels':int(mask.sum()),
                    'bbox':[int(xx.min()),int(yy.min()),int(xx.max()+1),int(yy.max()+1)],
                    'hipsPixels':[start.tolist() for start in starts],
                    'centerDistancesToMountPixels':[float(np.linalg.norm(center-start)) for start in starts]})
                if len(mounts)<=3:
                    cx,cy=center.round().astype(int)
                    crop=np.asarray(image).copy()
                    crop[mask]=(255,20,10)
                    Image.fromarray(crop).crop((cx-40,cy-40,cx+40,cy+40)).resize((640,640),Image.Resampling.NEAREST).save(out/f'mount-{name}-{a}-{b}.png')
    panel.save(out/'chosen-five-green.png')
    (out/'mount-context.json').write_text(json.dumps(mounts,indent=2))
    print(json.dumps(mounts,indent=2))


if __name__=='__main__':
    main()
