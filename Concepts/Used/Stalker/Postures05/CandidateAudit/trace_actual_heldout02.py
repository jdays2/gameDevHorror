"""Frozen actual59: visible contact proof and native source UV per contact."""
import json
import numpy as np
from PIL import Image,ImageDraw
from continuous_polar_painter import SpritePainter,uv_field,field_with_metrics
from native_uv_painter import bilinear
from source_ownership_refined import owned_materials
from source_ownership_study import HERE,FAMILY,PROJECT
from compose_parts import encode
from qa_parts import FONT,green,bounded_holes


def main():
    folder=HERE/'ActualHeldOut02'
    painter=SpritePainter(PROJECT)
    sources,torso,owner,native=owned_materials(painter)
    poses={pose['name']:pose for pose in json.loads((folder/'poses.json').read_text())}
    cases=[('body-6-00',[3,5]),('body-6-24',[2,4]),('entry-12-07',[0,1]),
           ('return-36-01',[0,1]),('body-2-25',[1,3])]
    sheet=Image.new('RGB',(1152,len(cases)*350),'#00ff00');d=ImageDraw.Draw(sheet)
    records=[]
    for row,(name,pair) in enumerate(cases):
        pose=poses[name]
        stem=f'{pose["row"]}-{pose["phase"]:02d}' if 'row' in pose else name
        legs=[np.asarray(Image.open(folder/'Masks'/f'leg-{stem}-{leg}.png').convert('RGBA')) for leg in pair]
        body=np.asarray(Image.open(folder/'Masks'/f'torso-{name}.png').convert('RGBA'))
        overlap=(legs[0][...,3]>128)&(legs[1][...,3]>128)&(body[...,3]<=128)
        yy,xx=np.nonzero(overlap)
        cx,cy=int(np.rint(xx.mean())),int(np.rint(yy.mean()))
        index=int(np.argmin((xx-cx)**2+(yy-cy)**2))
        point=[int(xx[index]),int(yy[index])]
        raw=green(np.asarray(Image.open(folder/'Raw'/f'{name}.png').convert('RGBA')))
        marked=np.asarray(raw).copy();marked[overlap]=(255,0,0)
        y=row*350
        # Fixed 70px crops at4x; no fit changes between original/marked/masks.
        box=(cx-35,cy-35,cx+35,cy+35)
        sheet.paste(raw.crop(box).resize((280,280),Image.Resampling.NEAREST),(0,y+60))
        sheet.paste(Image.fromarray(marked).crop(box).resize((280,280),Image.Resampling.NEAREST),(290,y+60))
        colored=np.zeros((576,576,3),dtype='uint8');colored[:]=(0,255,0)
        colored[legs[0][...,3]>128]=(255,120,150)
        colored[legs[1][...,3]>128]=(70,100,255)
        colored[body[...,3]>128]=(120,120,120)
        colored[overlap]=(255,0,0)
        sheet.paste(Image.fromarray(colored).crop(box).resize((280,280),Image.Resampling.NEAREST),(580,y+60))
        d.text((8,y+8),f'{name} {pair} {int(overlap.sum())}px | whole / red contact / independent masks',fill='black',font=FONT)
        source_records=[]
        for column,leg in enumerate(pair):
            limb=painter.anatomy['limbs'][leg]
            uv=uv_field(limb,pose,leg,points=[point])
            hip,knee,tip=[np.asarray(limb[key]) for key in ['hip','knee','tip']]
            source_records.append({'leg':leg,'id':limb['id'],'sourceUV':uv[0].tolist(),
                'distanceToSourceHip':float(np.linalg.norm(uv[0]-hip)),
                'distanceToSourceKnee':float(np.linalg.norm(uv[0]-knee)),
                'nativeAlphaByte':float(bilinear(sources[leg],uv)[0,3]*255)})
            source_image=encode(sources[leg],green=True)
            sx,sy=np.rint(uv[0]).astype(int)
            cropped=source_image.crop((sx-14,sy-14,sx+14,sy+14)).resize((112,112),Image.Resampling.NEAREST)
            sheet.paste(cropped,(875+column*130,y+90))
            d.text((875+column*130,y+65),limb['id']+' native',fill='black',font=FONT)
        records.append({'name':name,'pair':pair,'contactPixels':int(overlap.sum()),
            'targetProbe':point,'actualTorsoAlphaByte':int(body[point[1],point[0],3]),'source':source_records})
    sheet.save(folder/'contact-source-contexts-4x-nearest.png')
    # Trace the remaining threshold4 single pixel using the frozen pose.
    pose=poses['body-1-25'];leg=5;point=np.array([338.,412.])
    uv,j=field_with_metrics(painter.anatomy['limbs'][leg],pose,leg,[point])
    yy,xx=np.mgrid[-80:81,-80:81]
    points=np.stack([point[0]+xx/8,point[1]+yy/8],axis=-1)
    subalpha=bilinear(sources[leg],uv_field(painter.anatomy['limbs'][leg],pose,leg,points=points))[...,3]
    holes=bounded_holes(subalpha>4/255)
    trace={'name':'body-1-25','leg':5,'targetProbe':point.tolist(),'sourceUV':uv[0].tolist(),
        'nativeOwnedAlphaByte':float(bilinear(sources[leg],uv)[0,3]*255),
        'nativeRawAlphaByte':float(bilinear(painter.raw_sources[leg],uv)[0,3]*255),
        'analyticalJacobian':float(j['jacobian'][0]),'subpixelLatticeScale':8,
        'subpixelLatticeBoundedHolesAlphaOver4':holes}
    report={'contacts':records,'lowAlphaOnePixelTrace':trace}
    (folder/'contact-source-trace.json').write_text(json.dumps(report,indent=2))
    print(json.dumps(report,indent=2))


if __name__=='__main__':
    main()
