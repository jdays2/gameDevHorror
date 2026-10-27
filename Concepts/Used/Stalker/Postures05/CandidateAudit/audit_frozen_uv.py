"""Fine UV Jacobian and PNG interior alpha on immutable HeldOut01."""
from pathlib import Path
import hashlib
import json
import numpy as np
from PIL import Image, ImageFilter
from native_uv_painter import SpritePainter, uv_field

HERE=Path(__file__).resolve().parent
FAMILY=HERE.parent


def interior(alpha):
    records=[]
    for radius in [2,4]:
        mask=np.asarray(Image.fromarray((alpha>128).astype('uint8')*255).filter(ImageFilter.MinFilter(radius*2+1)))>128
        values=alpha[mask]
        records.append({'erosionRadiusPixels':radius,'pixels':int(mask.sum()),
            'minimumAlpha':int(values.min()) if len(values) else None,
            'pixelsAlphaBelow230':int((values<230).sum()),'pixelsAlphaBelow240':int((values<240).sum())})
    return records


def main():
    folder=HERE/'HeldOut01'
    manifest=json.loads((folder/'manifest.json').read_text())
    assert hashlib.sha256((HERE/'native_uv_painter.py').read_bytes()).hexdigest()==manifest['rendererSha256']
    for name,digest in manifest['rawPngSha256'].items():
        assert hashlib.sha256((folder/'Raw'/name).read_bytes()).hexdigest()==digest,name
    poses=json.loads((folder/'poses.json').read_text())
    painter=SpritePainter(FAMILY.parents[3])
    records=[]
    for index,pose in enumerate(poses):
        name=pose['name']
        whole=np.asarray(Image.open(folder/'Raw'/f'{name}.png').convert('RGBA'))[...,3]
        torso=np.asarray(Image.open(folder/'Masks'/f'torso-{name}.png').convert('RGBA'))[...,3]
        leg_records=[]
        for leg,limb in enumerate(painter.anatomy['limbs']):
            stem=f'{pose["row"]}-{pose["phase"]:02d}' if 'row' in pose else name
            alpha=np.asarray(Image.open(folder/'Masks'/f'leg-{stem}-{leg}.png').convert('RGBA'))[...,3]
            yy,xx=np.nonzero(alpha>230)
            points=np.column_stack([xx,yy]).astype('float64')
            e=.001
            all_points=np.concatenate([points+[e,0],points-[e,0],points+[0,e],points-[0,e]])
            all_uv=uv_field(limb,pose,leg,points=all_points)
            uxp,uxm,uyp,uym=np.split(all_uv,4)
            du=(uxp-uxm)/(2*e)
            dv=(uyp-uym)/(2*e)
            determinant=du[:,0]*dv[:,1]-du[:,1]*dv[:,0]
            negative=determinant < -1e-5
            bad=[]
            for x,y,jac in zip(xx[negative],yy[negative],determinant[negative]):
                bad.append({'xy':[int(x),int(y)],'jacobian':float(jac),'actualTorsoAlpha':int(torso[y,x])})
            leg_records.append({'leg':leg,'id':limb['id'],'interior':interior(alpha),
                'jacobianTestedOpaquePixels':len(points),'jacobianMinimum':float(determinant.min()) if len(points) else None,
                'negativeJacobianPixels':len(bad),'negativeJacobianPixelsOutsideActualTorso':sum(item['actualTorsoAlpha']<=128 for item in bad),
                'negativeJacobianExamples':bad[:20]})
        records.append({'name':name,'wholeInterior':interior(whole),'legs':leg_records})
        if index%4==3:
            print(f'Fine UV/alpha checks {index+1}/21',flush=True)
    report={'version':'HeldOut01','epsilonTargetPixel':.001,'frames':len(records),
        'rawPngManifestVerified':True,'rendererSha256':manifest['rendererSha256'],'checks':records,
        'negativeJacobianPixels':sum(leg['negativeJacobianPixels'] for item in records for leg in item['legs']),
        'negativeJacobianPixelsOutsideActualTorso':sum(leg['negativeJacobianPixelsOutsideActualTorso'] for item in records for leg in item['legs']),
        'status':'HeldOut01 remains a form/root FAIL; these metrics do not approve the artwork.'}
    (folder/'QA/uv-alpha-interior.json').write_text(json.dumps(report,indent=2))
    print(json.dumps({key:value for key,value in report.items() if key!='checks'},indent=2))


if __name__=='__main__':
    main()
