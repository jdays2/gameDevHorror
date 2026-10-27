"""Trace the two low-alpha discrete holes; no changed RGBA or filled pixels."""
import json
import numpy as np
from PIL import Image,ImageDraw
from continuous_polar_painter import SpritePainter,uv_field,field_with_metrics
from native_uv_painter import bilinear
from source_ownership_refined import owned_materials
from source_ownership_study import HERE,FAMILY,PROJECT
from compose_parts import encode
from qa_parts import bounded_holes,components,FONT


def main():
    folder=HERE/'ClosedMiddleOwnedStudy01'
    painter=SpritePainter(PROJECT)
    sources,torso,owner,native=owned_materials(painter)
    pose=json.loads((folder/'proof.json').read_text())['cases'][0]['pose']
    probes=[(2,221,280),(3,356,280)]
    records=[]
    sheet=Image.new('RGB',(1200,900),'#00ff00');d=ImageDraw.Draw(sheet)
    for row,(leg,x,y) in enumerate(probes):
        limb=painter.anatomy['limbs'][leg]
        uv,j=field_with_metrics(limb,pose,leg,[[x,y]])
        native_sample=bilinear(sources[leg],uv)[0]
        raw_sample=bilinear(painter.raw_sources[leg],uv)[0]
        # A subpixel lattice tests whether the same low-alpha V-space still
        # connects to surrounding empty space, rather than filling it away.
        sub=8
        yy,xx=np.mgrid[-10*sub:10*sub+1,-10*sub:10*sub+1]
        points=np.stack([x+xx/sub,y+yy/sub],axis=-1)
        sub_uv=uv_field(limb,pose,leg,points=points)
        sub_alpha=bilinear(sources[leg],sub_uv)[...,3]
        empty=sub_alpha<=4/255
        centre=(10*sub,10*sub)
        reached=np.zeros_like(empty)
        pending=[centre]
        while pending:
            py,px=pending.pop()
            if py<0 or px<0 or py>=empty.shape[0] or px>=empty.shape[1] or reached[py,px] or not empty[py,px]:
                continue
            reached[py,px]=True
            pending.extend((py+dy,px+dx) for dy in [-1,0,1] for dx in [-1,0,1] if dy or dx)
        outside=bool(reached[0,:].any() or reached[-1,:].any() or reached[:,0].any() or reached[:,-1].any())
        raw_image=Image.open(folder/'Masks'/f'leg-{pose["name"]}-{leg}.png').convert('RGBA')
        alpha=np.asarray(raw_image)[...,3]
        record={'leg':leg,'id':limb['id'],'targetXY':[x,y],'sourceUV':uv[0].tolist(),
            'nativeSampleAlphaByte':float(native_sample[3]*255),'rawOriginalSampleAlphaByte':float(raw_sample[3]*255),
            'sameSourceAlphaBeforeOwnership':bool(np.isclose(native_sample[3],raw_sample[3])),
            'analyticJacobian':float(j['jacobian'][0]),
            'targetAlpha5x5':alpha[y-2:y+3,x-2:x+3].tolist(),
            'nativeSourceBoundedHolesAlphaOver4':bounded_holes(painter.raw_sources[leg][...,3]>4/255),
            'lowAlphaRegionConnectsToOuterAtSubpixels':outside,'subpixelGridPerNativePixel':sub,
            'subpixelLatticeHolesOver4':bounded_holes(sub_alpha>4/255)}
        records.append(record)
        sx,sy=uv[0];cx,cy=np.rint([sx,sy]).astype(int)
        src=encode(sources[leg],green=True).crop((cx-20,cy-20,cx+20,cy+20)).resize((320,320),Image.Resampling.NEAREST)
        td=ImageDraw.Draw(src);td.ellipse((157,157,163,163),outline='red',width=1)
        # Preserve original alpha as a grayscale diagnostic beside nativegreen.
        crop=alpha[y-10:y+11,x-10:x+11]
        target_alpha=Image.fromarray(crop).resize((336,336),Image.Resampling.NEAREST).convert('RGB')
        sub_rgb=np.stack([sub_alpha*255,sub_alpha*255,sub_alpha*255],axis=-1).clip(0,255).astype('uint8')
        sub_rgb[reached]=[0,200,255]
        sub_image=Image.fromarray(sub_rgb).resize((322,322),Image.Resampling.NEAREST)
        y0=row*450
        sheet.paste(src,(0,y0+80));sheet.paste(target_alpha,(370,y0+80));sheet.paste(sub_image,(780,y0+80))
        d.text((8,y0+8),f'{limb["id"]} target({x},{y}); source native, target A, A at8x subpixel with reached gap cyan',fill='black',font=FONT)
        d.text((8,y0+35),f'native A={native_sample[3]*255:.3f}/255; continuous outside path:{outside}; no RGBA changed',fill='black',font=FONT)
    (folder/'microholes-source-trace.json').write_text(json.dumps(records,indent=2))
    sheet.save(folder/'microholes-native-source-alpha-subpixel.png')
    print(json.dumps(records,indent=2))


if __name__=='__main__':
    main()
