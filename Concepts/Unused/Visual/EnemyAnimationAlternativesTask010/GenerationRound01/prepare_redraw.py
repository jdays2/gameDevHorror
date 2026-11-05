from pathlib import Path
import json
from PIL import Image,ImageDraw
R=Path(__file__).resolve().parent
plan=json.loads((R/'walk-plan.json').read_text())
for branch in 'ABC':
    target=Image.new('RGBA',(1920,1280));guide=Image.new('RGB',(1920,1280),'#182027')
    for i in range(6):
        image=Image.open(R/f'{branch}-frame-{i:02}.png').convert('RGBA')
        target.paste(image,((i%3)*640,(i//3)*640))
        comp=Image.new('RGBA',(640,640),'#182027');comp.alpha_composite(image)
        d=ImageDraw.Draw(comp)
        for part in plan['frames'][i]['points']:
            pts=[tuple(part[k]) for k in ('hip','knee','tip')]
            d.line(pts,fill='#45d4fb',width=4)
            for x,y in pts:d.ellipse((x-5,y-5,x+5,y+5),fill='#ffdf76')
        guide.paste(comp.convert('RGB'),((i%3)*640,(i//3)*640))
    target.save(R/f'{branch}-walk-r2-edit-target.png')
    guide.save(R/f'{branch}-walk-r2-fixed-bones.png')
