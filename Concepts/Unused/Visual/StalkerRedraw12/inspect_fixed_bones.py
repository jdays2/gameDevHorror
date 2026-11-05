"""Overlay immutable controls FIRST. Local measurement is not an artwork fit."""
from pathlib import Path
from PIL import Image,ImageDraw
import json,numpy as np
OUT=Path(__file__).resolve().parent
controls=json.loads((OUT/'walk-fixed-controls.json').read_text())
colors=['#00c9e8','#f5cd00','#25d58b','#fa9c30','#a48cff','#ed71bb']
folder=OUT/'Review/walk00-volume-r1'
plate=Image.new('RGB',(1152,1152),'#e5e6e2')
for i in range(4):
    im=Image.open(folder/f'{i:02d}-matte.png').convert('RGB');d=ImageDraw.Draw(im)
    for b,color in zip(controls[i]['limbs'],colors):
        d.line([tuple(b[k]) for k in ['H','J','T']],fill=color,width=2)
        for key in ['H','J','T']:
            x,y=b[key];d.ellipse((x-4,y-4,x+4,y+4),outline=color,width=1)
    im.save(folder/f'{i:02d}-fixed-bones.png');plate.paste(im,((i%2)*576,(i//2)*576))
plate.save(folder/'fixed-bones-quad.png')
print('Fixed controls over actual complete registered sprites, unchanged per limb.')
