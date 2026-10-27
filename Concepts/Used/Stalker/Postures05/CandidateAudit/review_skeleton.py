"""Separate top/side bone pass and registered skin overlay for latest 05 poses."""
from pathlib import Path
import json
import math
import argparse
import numpy as np
from PIL import Image, ImageDraw, ImageFont

HERE=Path(__file__).resolve().parent
FAMILY=HERE.parent
parser=argparse.ArgumentParser()
parser.add_argument('--raw-folder',type=Path,default=FAMILY/'Raw')
parser.add_argument('--mask-folder',type=Path,default=FAMILY/'Masks')
parser.add_argument('--out',type=Path,default=HERE/'SkeletonReviewV2')
args=parser.parse_args()
OUT=args.out
OUT.mkdir(exist_ok=True)
poses=json.loads((FAMILY/'poses.json').read_text())
anatomy=json.loads((FAMILY.parent/'Locomotion02/anatomy.json').read_text())
selected=[next(pose for pose in poses if pose['row']==row and pose['phase']==0) for row in [0,4,6,8]]
COLORS=['#ff6060','#ffc0c0','#30cfff','#6060ff','#ffff30','#ffb020']
FONT=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',19)
SMALL=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',15)
dark='#14212a'
sheet=Image.new('RGB',(4*448,2*448),dark)
draw=ImageDraw.Draw(sheet)
records=[]
torso=np.asarray(Image.open(args.mask_folder/'torso.png').getchannel('A'))


def top(p,origin):
    return origin[0]+p[0]*180,origin[1]-p[1]*180


def side(p,origin):
    return origin[0]+p[1]*180,origin[1]-p[2]*180


def node(draw,point,color,radius=4):
    x,y=point
    draw.ellipse((x-radius,y-radius,x+radius,y+radius),fill=color,outline='black',width=1)


for index,pose in enumerate(selected):
    row=pose['row']
    left=index*448
    draw.text((left+10,8),f'fold {row}/8 | phase 00 | top XY',fill='white',font=FONT)
    draw.text((left+10,34),'front up; source chains unchanged',fill='white',font=SMALL)
    draw.text((left+10,456),f'fold {row}/8 | side YZ',fill='white',font=FONT)
    draw.text((left+10,482),'front right; ground Z=0 in green',fill='white',font=SMALL)
    top_origin=(left+224,245)
    side_origin=(left+224,822)
    draw.line((left+12,822,left+436,822),fill='#50bb50',width=2)
    for z in [.2,.4,.6]:
        y=822-z*180
        draw.line((left+12,y,left+436,y),fill='#31424b')
        draw.text((left+15,y-19),f'{z:.1f}',fill='#80909a',font=SMALL)
    skin=Image.open(args.raw_folder/f'body-{row}-00.png').convert('RGBA')
    alpha=np.asarray(skin)[...,3:4].astype('float32')/255
    rgb=np.asarray(skin)[...,:3]*alpha+np.array([0,255,0])*(1-alpha)
    overlay=Image.fromarray(np.clip(rgb,0,255).round().astype('uint8'))
    skin_draw=ImageDraw.Draw(overlay)
    leg_data=[]
    for leg,limb in enumerate(anatomy['limbs']):
        h,k,f=[np.asarray(pose[key][leg]) for key in ['hips','knees','feet']]
        color=COLORS[leg]
        for mapping,origin in [(top,top_origin),(side,side_origin)]:
            points=[mapping(point,origin) for point in [h,k,f]]
            draw.line(points,fill=color,width=3)
            for point in points:
                node(draw,point,color)
            draw.text((points[1][0]+4,points[1][1]-21),limb['id'],fill=color,font=SMALL)
        pixels=[(288+point[0]*224,288-point[1]*224) for point in [h,k,f]]
        skin_draw.line(pixels,fill=color,width=2)
        for point in pixels:
            node(skin_draw,point,color,3)
        skin_draw.text((pixels[1][0]+5,pixels[1][1]-19),limb['id'],fill='black',font=SMALL)
        a,b=h[:2]-k[:2],f[:2]-k[:2]
        angle=math.degrees(math.acos(float(np.clip(np.dot(a,b)/(np.linalg.norm(a)*np.linalg.norm(b)),-1,1))))
        hx,hy=round(pixels[0][0]),round(pixels[0][1])
        leg_data.append({'id':limb['id'],'upperLength':float(np.linalg.norm(k-h)),
            'upperLengthError':abs(float(np.linalg.norm(k-h))-limb['upperLength']),
            'lowerLength':float(np.linalg.norm(f-k)),
            'lowerLengthError':abs(float(np.linalg.norm(f-k))-limb['lowerLength']),
            'projectedKneeInteriorDegrees':angle,'hipHeight':float(h[2]),'kneeHeight':float(k[2]),
            'footHeight':float(f[2]),'actualTorsoAlphaAtHip':int(torso[hy,hx])})
    panel=Image.new('RGB',(576,636),'#00ff00')
    panel.paste(overlay,(0,60))
    pd=ImageDraw.Draw(panel)
    pd.text((10,8),f'05 fold {row}/8, phase00 | registered bones on skin',fill='black',font=FONT)
    pd.text((10,34),'Same 224 px/unit and pivot; no individual part alignment',fill='black',font=SMALL)
    panel.save(OUT/f'skin-bones-{row}.png')
    records.append({'row':row,'phase':0,'legs':leg_data,
        'feetAtGround':sum(abs(leg['footHeight'])<.005 for leg in leg_data)})
sheet.save(OUT/'bones-top-and-side.png')
(OUT/'skeleton-check.json').write_text(json.dumps({'poses':records,
    'source':'Independent projections from latest C# poses.json. Top/side bones reviewed before registered skin overlay.',
    'supportCaveat':'Fully folded toes are lifted above Z=0. The pose needs body/belly support; no claim of foot-supported standing is made.'},indent=2))
print(json.dumps({'rows':[record['row'] for record in records],
    'worstBoneLengthError':max(max(leg['upperLengthError'],leg['lowerLengthError']) for record in records for leg in record['legs']),
    'feetAtGround':[record['feetAtGround'] for record in records],
    'foldedKneeAngles':[leg['projectedKneeInteriorDegrees'] for leg in records[-1]['legs']],
    'hipCoverage':[leg['actualTorsoAlphaAtHip'] for leg in records[-1]['legs']]},indent=2))
