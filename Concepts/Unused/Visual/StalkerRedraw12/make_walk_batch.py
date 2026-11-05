"""Whole untextured pose sheets, anatomy controls only; no final skin pixels."""
from pathlib import Path
import json,math
import numpy as np
from PIL import Image
from render_volume_guide import render

OUT=Path(__file__).resolve().parent
plan=json.loads((OUT/'plan.json').read_text(encoding='utf-8-sig'))
keys=plan['walkOriginalControls'];counts=plan['walkMinimumSubsteps']
poses=[]
for i,n in enumerate(counts):
    for k in range(n):
        t=k/n
        poses.append({'key':i,'fraction':t,'limbs':[{'id':a['id'],**{p:((1-t)*np.array(a[p])+t*np.array(b[p])).tolist() for p in ['H','J','T']}} for a,b in zip(keys[i],keys[(i+1)%8])]})
(OUT/'walk-fixed-controls.json').write_text(json.dumps(poses,indent=2),encoding='utf-8')
for start in range(0,len(poses),4):
    batch=poses[start:start+4]
    if len(batch)<4:batch+=poses[:4-len(batch)]
    plate=Image.new('RGB',(1152,1152),'#e5e6e2')
    bones=plate.copy()
    for k,p in enumerate(batch):
        for overlay,target in [(False,plate),(True,bones)]:
            f=OUT/'guide-temp.png'
            render(p['limbs'],f,overlay)
            target.paste(Image.open(f),((k%2)*576,(k//2)*576))
    plate.save(OUT/f'walk-{start:02d}-volume-quad.png')
    bones.save(OUT/f'walk-{start:02d}-bones-quad.png')
print(json.dumps({'walkPoses':len(poses),'quads':math.ceil(len(poses)/4)}))
