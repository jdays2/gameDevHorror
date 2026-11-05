from pathlib import Path
import json
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parent
PROJECT = ROOT.parents[4]
anatomy = json.loads((PROJECT/'Concepts/Used/Stalker/Locomotion02/anatomy.json').read_text(encoding='utf-8-sig'))
ROOT.mkdir(parents=True,exist_ok=True)
points = []
for limb in anatomy['limbs']:
    p = {key:[limb[key][0]+101,limb[key][1]+88] for key in ('hip','knee','tip')}
    points.append({'id':limb['id'],**p})
image=Image.new('RGB',(576,576),'#182027')
d=ImageDraw.Draw(image)
d.polygon([(237,328),(339,328),(288,216)],fill='#465462',outline='#9eaebd',width=2)
font=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',14)
for limb in points:
    p=[tuple(limb[k]) for k in ('hip','knee','tip')]
    d.line(p,fill='#a9d8f5',width=3)
    for q in p:
        d.ellipse((q[0]-4,q[1]-4,q[0]+4,q[1]+4),fill='#ffe391')
    d.text((p[1][0]+8,p[1][1]),limb['id'],font=font,fill='white')
d.ellipse((268,268,308,308),outline='#b9c8d9',width=2)
image.save(ROOT/'neutral-bones.png')
(ROOT/'anatomy-reference.json').write_text(json.dumps({'status':'independent reference; not owner-approved new anatomy','canvas':[576,576],'origin':[288,288],'points':points,'boneSource':'Locomotion02 anatomical landmark coordinates; no per-limb fitting to generated art','unknown':'true hidden heights; muscle atlas not inferred from this PNG'},indent=2),encoding='utf-8')
print(ROOT)
