"""Trace frozen H01 root fragments and holes to actual source coordinates."""
from pathlib import Path
import json
import numpy as np
from PIL import Image, ImageDraw
from native_uv_painter import SpritePainter, uv_field, bilinear, ORIGIN, FLIP, PPU, JOINT_RADII
from qa_parts import FONT

HERE=Path(__file__).resolve().parent
FAMILY=HERE.parent


def main():
    out=HERE/'SourceUVTrace'
    out.mkdir(exist_ok=True)
    painter=SpritePainter(FAMILY.parents[3])
    atlas=np.asarray(Image.open(FAMILY.parents[3]/'Assets/EmergencyShift/Art/Enemies/Stalker/Locomotion02/rig-atlas.png').convert('RGBA'))
    body_alpha=atlas[:406,:358,3].astype('float32')/255
    body_rgba=np.repeat(body_alpha[...,None],4,axis=-1)
    poses={pose['name']:pose for pose in json.loads((HERE/'HeldOut01/poses.json').read_text())}
    qa=json.loads((HERE/'HeldOut01/QA/qa.json').read_text())
    probes=[('body-0-12',347,315,'right root needle'),('body-0-36',347,315,'right root needle'),
        ('body-0-12',224,310,'left root needle'),('body-0-24',350,309,'RM/RR intersection'),
        ('body-4-00',333,274,'RF/RM collar')]
    for entry in qa['perLegBoundedHoles']:
        if entry['name'].startswith('body-8') and entry['name']!='body-8-00':
            continue
        for hole in entry['holes']:
            x0,y0,x1,y1=hole['bbox']
            probes.append((entry['name'],(x0+x1-1)*.5,(y0+y1-1)*.5,f'hole a>{entry["alphaOver"]} leg{entry["leg"]}'))
    records=[]
    for name,x,y,label in probes:
        pose=poses[name]
        for leg,limb in enumerate(painter.anatomy['limbs']):
            uv=uv_field(limb,pose,leg,points=[[x,y]])
            rgba=bilinear(painter.sources[leg],uv)[0]
            if rgba[3]<=4/255 and not label.endswith(f'leg{leg}'):
                continue
            source_body=bilinear(body_rgba,uv)[0,3]
            hip,knee,toe=[ORIGIN+FLIP@np.asarray(pose[key][leg][:2])*PPU for key in ('hips','knees','feet')]
            upper=(hip-knee)/np.linalg.norm(hip-knee)
            lower=(toe-knee)/np.linalg.norm(toe-knee)
            normal=(upper-lower)/np.linalg.norm(upper-lower)
            relative=np.array([x,y])-knee
            signed=float(relative@normal)
            projected=np.array([x,y])-signed*normal
            epsilon=.001
            around=uv_field(limb,pose,leg,points=[projected-epsilon*normal,projected+epsilon*normal])
            around_alpha=bilinear(painter.sources[leg],around)[...,3]
            records.append({'name':name,'label':label,'leg':leg,'id':limb['id'],'targetXY':[x,y],
                'sourceUV':uv[0].tolist(),'nativeLegAlpha':float(rgba[3]),'originalTorsoAlphaAtSameUV':float(source_body),
                'sharedOpaqueSourceBodyAndLeg':bool(rgba[3]>.9 and source_body>.9),
                'radiusFromTargetKnee':float(np.linalg.norm(relative)),'lockedJointRadius':JOINT_RADII[leg]+8,
                'distanceToAngularBisectorPixels':abs(signed),
                'sourceUVAcrossExactBisector':around.tolist(),'sourceUvJumpFor002TargetPixel':float(np.linalg.norm(around[1]-around[0])),
                'nativeLegAlphaAcrossBisector':around_alpha.tolist()})
    sheet=Image.new('RGB',(4*360,2*480),'#00ff00')
    draw=ImageDraw.Draw(sheet)
    seen=set()
    shown=0
    for record in records:
        if 'root' not in record['label'] and 'intersection' not in record['label'] and 'collar' not in record['label']:
            continue
        key=(record['label'],record['leg'])
        if key in seen or shown>=8:
            continue
        seen.add(key)
        raw=np.asarray(Image.open(FAMILY.parents[3]/'Assets/EmergencyShift/Art/Enemies/Stalker/PaleFlesh/walk-sheet.png').convert('RGBA').crop((30,84,388,490)))
        alpha=raw[...,3:4]/255
        rgb=np.rint(raw[...,:3]*alpha+np.array([0,255,0])*(1-alpha)).astype('uint8')
        image=Image.fromarray(rgb)
        d=ImageDraw.Draw(image)
        sx,sy=record['sourceUV']
        d.ellipse((sx-3,sy-3,sx+3,sy+3),outline='red',width=1)
        # Original torso mask is shown by a white contour-like sparse lattice.
        a=np.asarray(image).copy()
        yy,xx=np.mgrid[:406,:358]
        mark=(body_alpha>.5)&(xx%5==0)&(yy%5==0)
        a[mark]=(255,255,255)
        x,y=shown%4*360,shown//4*480
        sheet.paste(Image.fromarray(a),(x,y+60))
        draw.text((x+5,y+5),f'{record["id"]} {record["label"]}',fill='black',font=FONT)
        draw.text((x+5,y+28),f'bodyA {record["originalTorsoAlphaAtSameUV"]:.2f}',fill='black',font=FONT)
        shown+=1
    sheet.save(out/'root-pixels-on-source.png')
    (out/'source-uv-trace.json').write_text(json.dumps(records,indent=2))
    print(json.dumps(records,indent=2))


if __name__=='__main__':
    main()
