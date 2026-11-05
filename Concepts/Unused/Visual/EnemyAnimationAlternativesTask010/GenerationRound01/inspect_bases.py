from pathlib import Path
import json
from PIL import Image,ImageDraw,ImageFont
R=Path(__file__).resolve().parent
points=json.loads((R/'anatomy-reference.json').read_text())['points']
font=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',16)
for name in ['A-direct-r1','B-bone-guided-r1','C-volume-guided-r1']:
    if not (R/f'{name}.png').exists():continue
    source=Image.open(R/f'{name}.png').convert('RGBA')
    print(name,source.size,source.getchannel('A').getbbox())
    # One uniform whole-canvas scale, fixed before checking any limb.
    target=source.resize((576,576),Image.Resampling.LANCZOS)
    page=Image.new('RGB',(1152,620),'#182027')
    for i,bg in enumerate(['#182027','#e8e5df']):
        image=Image.new('RGBA',(576,576),bg);image.alpha_composite(target)
        d=ImageDraw.Draw(image)
        for limb in points:
            chain=[tuple(limb[k]) for k in ('hip','knee','tip')]
            d.line(chain,fill='#5bc9ed',width=2)
            for x,y in chain:d.ellipse((x-3,y-3,x+3,y+3),fill='#f8d864')
        page.paste(image.convert('RGB'),(i*576,40))
    ImageDraw.Draw(page).text((14,10),name+' · fixed reference bones, no limb fitting',font=font,fill='white')
    page.save(R/f'{name}-bones-review.png')
