from pathlib import Path
import json,math
from PIL import Image,ImageDraw,ImageFont
R=Path(__file__).resolve().parent
points=json.loads((R/'anatomy-reference.json').read_text())['points']
font=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',12)
names=['A-direct-r2','B-bone-guided-r2','C-volume-guided-r1']
for name in names:
    image=Image.open(R/f'{name}.png').convert('RGBA').resize((576,576),Image.Resampling.LANCZOS)
    canvas=Image.new('RGBA',(576,576),'#182027');canvas.alpha_composite(image)
    d=ImageDraw.Draw(canvas,'RGBA')
    i=0
    for limb in points:
        h,k,t=[limb[key] for key in ('hip','knee','tip')]
        # Declared functional regions, not a medical atlas or inferred new muscles.
        centers=[[(h[0]+k[0])/2,(h[1]+k[1])/2],k,[k[0]*.78+t[0]*.22,k[1]*.78+t[1]*.22]]
        for j,(x,y) in enumerate(centers):
            i+=1;radius=[10,9,6][j]
            d.ellipse((x-radius,y-radius,x+radius,y+radius),outline=(119,224,182,210),width=2)
            d.text((x+7,y-7),str(i),font=font,fill=(255,230,162,255))
    canvas.convert('RGB').save(R/f'{name}-functional-regions.png')
print('18 corresponding functional regions retained for each neutral master; no literal muscle count claimed.')
