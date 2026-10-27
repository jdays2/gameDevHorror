"""Read-only all-pose native-frame QA, gated by completed composition hashes.

Use explicit pose manifests rather than globbing stale earlier PNGs. Body
uses the new shared torso mask; attack always uses its current per-pose mask.
Every pair stays in the report. No hip spheres or size tolerances hide it.
"""
import argparse
from pathlib import Path
import hashlib
import json
import sys
import numpy as np
from PIL import Image,ImageFilter

HERE=Path(__file__).resolve().parent
FAMILY=HERE.parent
sys.path.insert(0,str(FAMILY))
from native_frame_painter import SpritePainter,field_with_metrics,uv_field
from qa_parts import components,bounded_holes


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def local_components(binary):
    yy,xx=np.nonzero(binary)
    if not len(xx):
        return []
    x0,y0,x1,y1=int(xx.min()),int(yy.min()),int(xx.max())+1,int(yy.max())+1
    found=components(binary[y0:y1,x0:x1])
    for part in found:
        a,b,c,d=part['bbox']
        part['bbox']=[a+x0,b+y0,c+x0,d+y0]
    return found


def validate_composition(kind):
    path=FAMILY/f'{kind}-composition.json'
    composition=json.loads(path.read_text())
    assert composition['algorithm']=='whole-native-continuous-polar',path
    assert composition['painterSha256']==digest(FAMILY/'native_frame_painter.py')
    assert composition['colorsSha256']==digest(FAMILY/'frame_colors.py')
    pose_path=FAMILY/('attack-poses.json' if kind=='attack' else 'poses.json')
    assert composition['posesSha256']==digest(pose_path)
    if kind=='attack':
        assert composition['mouthOverlayApplied']
        assert composition['mouthSha256']==digest(FAMILY/'attack_mouth.py')
    poses=json.loads(pose_path.read_text())
    assert composition['poses']==len(poses)==(792 if kind=='attack' else 432)
    return poses,composition,digest(path)


def main():
    parser=argparse.ArgumentParser()
    parser.add_argument('--kind',choices=['body','attack','all'],default='all')
    parser.add_argument('--out',required=True)
    args=parser.parse_args()
    out=HERE/args.out
    if out.exists():
        raise FileExistsError('Keep numeric review versions immutable')
    kinds=['body','attack'] if args.kind=='all' else [args.kind]
    inputs={kind:validate_composition(kind) for kind in kinds}
    out.mkdir(parents=True)
    painter=SpritePainter(FAMILY.parents[3])
    all_records=[]
    mask_digest=hashlib.sha256()
    raw_digest=hashlib.sha256()
    for kind,(poses,composition,composition_digest) in inputs.items():
        records=[]
        for index,pose in enumerate(poses):
            name=pose['name'] if kind=='attack' else f'body-{pose["row"]}-{pose["phase"]:02d}'
            stem=name if kind=='attack' else f'{pose["row"]}-{pose["phase"]:02d}'
            body_path=FAMILY/'Masks'/(f'torso-{name}.png' if kind=='attack' else 'torso.png')
            body=np.asarray(Image.open(body_path).convert('RGBA'))[...,3]
            paths=[FAMILY/'Masks'/f'leg-{stem}-{leg}.png' for leg in range(6)]
            alphas=[]
            limbs=[]
            for leg,path in enumerate(paths):
                mask_digest.update(path.name.encode())
                mask_digest.update(path.read_bytes())
                alpha=np.asarray(Image.open(path).convert('RGBA'))[...,3]
                alphas.append(alpha)
                yy,xx=np.nonzero(alpha>230)
                _,uv_check=field_with_metrics(painter.anatomy['limbs'][leg],pose,leg,np.column_stack([xx,yy]))
                jacobian=uv_check['jacobian']
                eroded=np.asarray(Image.fromarray((alpha>128).astype('uint8')*255).filter(ImageFilter.MinFilter(9)))>128
                inside=alpha[eroded]
                weak=(alpha>0)&(alpha<=6)
                dilation=np.asarray(Image.fromarray((alpha>128).astype('uint8')*255).filter(ImageFilter.MaxFilter(5)))>128
                far_weak=weak&~dilation
                yyw,xxw=np.nonzero(far_weak)
                parts4=local_components(alpha>4)
                parts0=local_components(alpha>0)
                limbs.append({'leg':leg,'id':painter.anatomy['limbs'][leg]['id'],
                    'componentsAlphaOver4':parts4,'componentsAlphaPositive':parts0,
                    'holes':[{'alphaOver':threshold,'parts':bounded_holes(alpha>threshold)} for threshold in [4,128]],
                    'borderPixelsAlphaPositive':int(np.count_nonzero(alpha[[0,-1],:])+np.count_nonzero(alpha[:,[0,-1]])),
                    'opaqueSamples':len(xx),'jacobianMinimum':float(np.nanmin(jacobian)) if len(jacobian) else None,
                    'nonpositiveJacobian':int(np.count_nonzero(jacobian<=0)),
                    'polarCentreSamples':int(uv_check['polarCentre'].sum()),
                    'erode4AlphaMinimum':int(inside.min()) if len(inside) else None,
                    'erode4AlphaBelow230':int(np.count_nonzero(inside<230)),
                    'weakAlphaUpTo6Outside2pxSolidDilation':int(far_weak.sum()),
                    'weakOutside2pxBbox':[int(xxw.min()),int(yyw.min()),int(xxw.max())+1,int(yyw.max())+1] if len(xxw) else None})
            pairs=[]
            for a in range(6):
                for b in range(a+1,6):
                    contact=(alphas[a]>128)&(alphas[b]>128)&(body<=128)
                    yy,xx=np.nonzero(contact)
                    record={'pair':[a,b],'pixels':int(contact.sum())}
                    if len(xx):
                        record['bbox']=[int(xx.min()),int(yy.min()),int(xx.max())+1,int(yy.max())+1]
                        points=np.column_stack([xx,yy])
                        traces=[]
                        for leg in [a,b]:
                            limb=painter.anatomy['limbs'][leg]
                            uv=uv_field(limb,pose,leg,points=points)
                            hip_distance=np.linalg.norm(uv-np.asarray(limb['hip']),axis=-1)
                            knee_distance=np.linalg.norm(uv-np.asarray(limb['knee']),axis=-1)
                            traces.append({'leg':leg,'sourceUvFirst':uv[0].tolist(),
                                'sourceHipDistanceRange':[float(hip_distance.min()),float(hip_distance.max())],
                                'sourceKneeDistanceRange':[float(knee_distance.min()),float(knee_distance.max())]})
                        record['sourceMaterialTrace']=traces
                        record['reviewStatus']='Kept in metrics; needs actual context. Proximal Y contacts are not excluded by a mask or tolerance.'
                    pairs.append(record)
            raw_path=FAMILY/'Raw'/f'{name}.png'
            raw_digest.update(raw_path.name.encode())
            raw_digest.update(raw_path.read_bytes())
            whole=np.asarray(Image.open(raw_path).convert('RGBA'))[...,3]
            record={'name':name,'kind':kind,'maskStem':stem,'torsoMask':body_path.name,
                'limbs':limbs,'all15Pairs':pairs,'wholeComponentsAlphaOver4':local_components(whole>4),
                'wholeComponentsAlphaPositive':local_components(whole>0),
                'wholeBorderPixelsAlphaPositive':int(np.count_nonzero(whole[[0,-1],:])+np.count_nonzero(whole[:,[0,-1]]))}
            records.append(record)
            if (index+1)%48==0:
                print(f'Native QA {kind}: {index+1}/{len(poses)}',flush=True)
        assert digest(FAMILY/f'{kind}-composition.json')==composition_digest,'Composition changed during read-only audit'
        (out/f'{kind}-checks.json').write_text(json.dumps(records,indent=2))
        all_records.extend(records)
    pairs=[{'name':r['name'],**p} for r in all_records for p in r['all15Pairs'] if p['pixels']]
    holes=[{'name':r['name'],'leg':leg['leg'],**h} for r in all_records for leg in r['limbs'] for h in leg['holes'] if h['parts']]
    summary={'frames':len(all_records),'compositionManifestHashes':{k:v[2] for k,v in inputs.items()},
        'painterSha256':digest(FAMILY/'native_frame_painter.py'),'masksSha256':mask_digest.hexdigest(),
        'rawImagesSha256':raw_digest.hexdigest(),'allPairsTested':len(all_records)*15,
        'pairCandidates':sorted(pairs,key=lambda p:-p['pixels']),
        'boundedHoleCandidates':holes,
        'nonpositiveJacobianPixels':sum(l['nonpositiveJacobian'] for r in all_records for l in r['limbs']),
        'borderAlphaPositivePixels':sum(r['wholeBorderPixelsAlphaPositive'] for r in all_records),
        'erode4AlphaBelow230Pixels':sum(l['erode4AlphaBelow230'] for r in all_records for l in r['limbs']),
        'interpretation':'Numeric QA only. Every contact and low-alpha fragment is retained for native-scale art review; no author acceptance is inferred.'}
    (out/'summary.json').write_text(json.dumps(summary,indent=2))
    print(json.dumps({k:v for k,v in summary.items() if k not in ['pairCandidates','boundedHoleCandidates']},indent=2),flush=True)


if __name__=='__main__':
    main()
