"""Three root-authored closed middle-sector controls; fixed source/UV02."""
import argparse
import json
import numpy as np
from PIL import Image,ImageDraw,ImageFilter
from continuous_polar_painter import SpritePainter,field_with_metrics
from source_ownership_refined import owned_materials
from source_ownership_study import HERE,FAMILY,PROJECT,paint,metrics,digest
from native_uv_painter import ORIGIN,FLIP,PPU
from compose_parts import encode
from qa_parts import FONT


def main():
    parser=argparse.ArgumentParser()
    parser.add_argument('--source',default='ClosedMiddleStudy/poses.json')
    parser.add_argument('--out',default='ClosedMiddleOwnedStudy01')
    args=parser.parse_args()
    out=HERE/args.out
    if out.exists():
        raise FileExistsError('ClosedMiddleOwnedStudy01 is frozen')
    (out/'Raw').mkdir(parents=True)
    (out/'Masks').mkdir()
    painter=SpritePainter(PROJECT)
    sources,torso,owner,native=owned_materials(painter)
    poses=json.loads((FAMILY/args.source).read_text())
    sheet=Image.new('RGB',(len(poses)*576,636),'#00ff00');draw=ImageDraw.Draw(sheet)
    joints=Image.new('RGB',(1296,(len(poses)+1)*240),'#00ff00');jd=ImageDraw.Draw(joints)
    records=[]
    for leg,limb in enumerate(painter.anatomy['limbs']):
        cx,cy=np.rint(limb['knee']).astype(int)
        crop=encode(sources[leg],green=True).crop((cx-42,cy-42,cx+42,cy+42))
        joints.paste(crop.resize((168,168),Image.Resampling.NEAREST),(leg*216,50))
        jd.text((leg*216+5,10),limb['id']+' native source |2x',fill='black',font=FONT)
    for index,pose in enumerate(poses):
        name=pose.get('name',f'closed-middle-{index}')
        whole,limbs,actual_torso=paint(painter,pose,sources,torso)
        encode(whole).save(out/'Raw'/f'{name}.png')
        encode(actual_torso).save(out/'Masks'/f'torso-{name}.png')
        sheet.paste(encode(whole,green=True),(index*576,60))
        draw.text((index*576+6,10),name,fill='black',font=FONT)
        draw.text((index*576+6,32),'sourceowner02 /sameUV /native224PPU',fill='black',font=FONT)
        checks=[]
        for leg,(limb,material) in enumerate(zip(painter.anatomy['limbs'],limbs)):
            image=encode(material)
            image.save(out/'Masks'/f'leg-{name}-{leg}.png')
            alpha=np.asarray(image)[...,3]
            yy,xx=np.nonzero(alpha>230)
            points=np.column_stack([xx,yy])
            _,j=field_with_metrics(limb,pose,leg,points)
            eroded=np.asarray(Image.fromarray((alpha>128).astype('uint8')*255).filter(ImageFilter.MinFilter(9)))>128
            interior=alpha[eroded]
            checks.append({'leg':leg,'opaqueSamples':len(points),'jacobianMinimum':float(np.nanmin(j['jacobian'])),
                'nonpositiveJacobian':int(np.count_nonzero(j['jacobian']<=0)),
                'polarCentreSamples':int(j['polarCentre'].sum()),
                'erode4AlphaMinimum':int(interior.min()) if len(interior) else None,
                'erode4AlphaBelow230':int(np.count_nonzero(interior<230)),
                'borderPixelsAlphaOver1':int(np.count_nonzero(alpha[[0,-1],:]>1)+np.count_nonzero(alpha[:,[0,-1]]>1))})
            cx,cy=np.rint(ORIGIN+FLIP@np.asarray(pose['knees'][leg][:2])*PPU).astype(int)
            crop=encode(material,green=True).crop((cx-42,cy-42,cx+42,cy+42))
            y=(index+1)*240
            joints.paste(crop.resize((168,168),Image.Resampling.NEAREST),(leg*216,y+50))
            jd.text((leg*216+5,y+10),f'{index+1}: {limb["id"]} |same2x',fill='black',font=FONT)
        records.append({'name':name,'pose':pose,'jacobianAlpha':checks,**metrics(limbs,actual_torso)})
    sheet.save(out/'controls-native-green.png')
    joints.save(out/'all-six-joints-source-target-2x-nearest.png')
    report={'scope':f'Exactly {len(poses)} authoring proposals; no runtime/production/mass writes',
        'status':'Await independent visual inspection; hidden middle tips are proposals, not native measurements.',
        'cases':records}
    (out/'proof.json').write_text(json.dumps(report,indent=2))
    manifest={'continuousUvSha256':digest(HERE/'continuous_polar_painter.py'),
        'sourceOwnershipModuleSha256':digest(HERE/'source_ownership_refined.py'),
        'sourcePosePath':str(FAMILY/args.source),'sourcePoseSha256':digest(FAMILY/args.source),
        'rawPngSha256':{path.name:digest(path) for path in (out/'Raw').glob('*.png')}}
    (out/'manifest.json').write_text(json.dumps(manifest,indent=2))
    for r in records:
        print(r['name'],'pairs',[(p['pair'],p['pixels']) for p in r['all15Pairs'] if p['pixels']],
            'holes',sum(h['pixels'] for l in r['legs'] for b in l['holes'] for h in b['parts']),
            'negativeJ',sum(x['nonpositiveJacobian'] for x in r['jacobianAlpha']),flush=True)


if __name__=='__main__':
    main()
