"""Preserve exact source edits and repaint the captured pre-change 16 poses."""
from pathlib import Path
import json
import sys
import numpy as np
from PIL import Image, ImageDraw, ImageFont

HERE=Path(__file__).resolve().parent
FAMILY=HERE.parent
OUT=HERE/'SourceCleanupTrace'
sys.path.insert(0,str(FAMILY))
from sprite_painter import SpritePainter, _decode, _clean_source_root, ROOT_RADII
from compose_parts import encode

painter=SpritePainter(FAMILY.parents[3])
(OUT/'source-cleanup.json').write_text(json.dumps(painter.source_cleanup,indent=2))
atlas=Image.open(FAMILY.parents[3]/'Assets/EmergencyShift/Art/Enemies/Stalker/Locomotion02/rig-atlas.png').convert('RGBA')
sheet=Image.new('RGB',(3*500,500),'#00ff00')
draw=ImageDraw.Draw(sheet)
font=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',18)
for index,leg in enumerate([0,1,3]):
    layer=leg*2+1
    x,y=layer%4*358,layer//4*406
    before=_decode(atlas.crop((x,y,x+358,y+406)))
    hip=np.asarray(painter.anatomy['limbs'][leg]['hip'])
    after,trace=_clean_source_root(before,hip,ROOT_RADII[leg])
    removed=(before[...,3]>4/255)&(after[...,3]<=4/255)
    overlay=np.asarray(encode(before,green=True)).copy()
    overlay[removed]=(255,20,10)
    crop=(int(hip[0])-35,int(hip[1])-35,int(hip[0])+35,int(hip[1])+40)
    image=Image.fromarray(overlay).crop(crop).resize((420,450),Image.Resampling.NEAREST)
    sheet.paste(image,(index*500+40,50))
    draw.text((index*500+10,10),f'{painter.anatomy["limbs"][leg]["id"]}: {trace["removedPixelsAlphaOver4"]} source pixels removed',fill='black',font=font)
sheet.save(OUT/'source-dirt-removed-red.png')

poses=json.loads((OUT/'poses-before.json').read_text())
target=OUT/'Repaint'
(target/'Masks').mkdir(parents=True,exist_ok=True)
(target/'Raw').mkdir(exist_ok=True)
selected=[pose for pose in poses if pose['row'] in [0,4,6,8] and pose['phase']%12==0]
for pose in selected:
    whole,limbs,torso=painter.paint(pose)
    row,phase=pose['row'],pose['phase']
    encode(whole).save(target/'Raw'/f'body-{row}-{phase:02d}.png')
    for leg,mask in enumerate(limbs):
        encode(mask).save(target/'Masks'/f'leg-{row}-{phase:02d}-{leg}.png')
    encode(torso).save(target/'Masks/torso.png')
print('Exact source cleanup and 16 controlled pose repaints written')
