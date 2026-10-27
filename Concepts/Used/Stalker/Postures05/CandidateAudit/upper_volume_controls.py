"""Exactly two proximal-volume controls, with explicit lower-paint audit."""
import json
import numpy as np
from PIL import Image,ImageDraw
from continuous_polar_painter import SpritePainter,uv_field
from upper_volume_painter import warp_rgba,field_with_metrics
from source_ownership_refined import owned_materials
from source_ownership_study import HERE,FAMILY,PROJECT,metrics,digest
from native_uv_painter import _over,ORIGIN,FLIP,PPU
from prove_continuous_polar import source_pose
from compose_parts import encode
from qa_parts import FONT,green


def segment_distance(points,start,end):
    axis=end-start
    phase=np.clip(((points-start)*axis).sum(axis=-1)/(axis@axis),0,1)
    return np.linalg.norm(points-start-phase[...,None]*axis,axis=-1)


def main():
    out=HERE/'UpperVolumeStudy01'
    if out.exists():
        raise FileExistsError('UpperVolumeStudy01 is frozen')
    (out/'Raw').mkdir(parents=True)
    (out/'Masks').mkdir()
    painter=SpritePainter(PROJECT)
    sources,torso,owner,native=owned_materials(painter)
    before_folder=HERE/'ClosedMiddleOwnedStudy02'
    before_record=json.loads((before_folder/'proof.json').read_text())['cases'][1]
    pose=before_record['pose']
    base=[np.asarray(Image.open(before_folder/'Masks'/f'leg-{pose["name"]}-{leg}.png').convert('RGBA')) for leg in range(6)]
    canvas=Image.new('RGB',(1728,636),'#00ff00');draw=ImageDraw.Draw(canvas)
    baseline=Image.open(before_folder/'Raw'/f'{pose["name"]}.png').convert('RGBA')
    canvas.paste(green(np.asarray(baseline)),(0,60))
    draw.text((8,10),'Frozen samepose baseline',fill='black',font=FONT)
    draw.text((8,32),'No deliberate volume; native224PPU',fill='black',font=FONT)
    joints=Image.new('RGB',(1296,720),'#00ff00');jd=ImageDraw.Draw(joints)
    records=[]
    neutral=source_pose(painter)
    offset=(ORIGIN-np.asarray(painter.anatomy['pivot'])).astype(int)
    identity=[]
    for strength in [1.3,1.5]:
        for leg,limb in enumerate(painter.anatomy['limbs']):
            got=np.asarray(encode(warp_rgba(painter.raw_sources[leg],limb,neutral,leg,strength)))
            expected=np.zeros((576,576,4),dtype='uint8')
            expected[offset[1]:offset[1]+406,offset[0]:offset[0]+358]=np.asarray(encode(painter.raw_sources[leg]))
            identity.append({'peak':strength,'leg':leg,'neutralVolumeFold':0,
                'byteDifferences':int(np.count_nonzero(got!=expected))})
    for column,strength in enumerate([1.3,1.5],1):
        whole=np.zeros((576,576,4),dtype='float32');limbs=[];checks=[]
        for leg,limb in enumerate(painter.anatomy['limbs']):
            material=warp_rgba(sources[leg],limb,pose,leg,strength,painter._grid)
            limbs.append(material);whole=_over(material,whole)
            image=encode(material)
            image.save(out/'Masks'/f'leg-volume{strength}-{leg}.png')
            old_uv=uv_field(limb,pose,leg,points=painter._grid)
            new_uv,j=field_with_metrics(limb,pose,leg,painter._grid,strength)
            sk,sh,st=[np.asarray(limb[key]) for key in ['knee','hip','tip']]
            lower_owner=segment_distance(old_uv,sk,st)<segment_distance(old_uv,sk,sh)
            lower_owner &= np.linalg.norm(old_uv-sk,axis=-1)>25
            lower_owner &= base[leg][...,3]>128
            shift=np.linalg.norm(new_uv-old_uv,axis=-1)
            rgba=np.asarray(image)
            change=np.max(np.abs(rgba.astype('int16')-base[leg].astype('int16')),axis=-1)
            anchors=np.stack([ORIGIN+FLIP@np.asarray(pose[key][leg][:2])*PPU for key in ['hips','knees','feet']])
            anchor_uv,_=field_with_metrics(limb,pose,leg,anchors,strength)
            expected_anchor=np.stack([sh,sk,st])
            opaque=rgba[...,3]>230
            checks.append({'leg':leg,'minimumJacobianOnOpaque':float(np.nanmin(j['jacobian'][opaque])),
                'nonpositiveJacobianOnOpaque':int(np.count_nonzero(j['jacobian'][opaque]<=0)),
                'minimumPrewarpJacobian':float(j['prewarpJacobian'].min()),
                'maximumAnchorSourceUvErrorPixels':float(np.max(np.linalg.norm(anchor_uv-expected_anchor,axis=-1))),
                'lowerOpaquePixelsChecked':int(lower_owner.sum()),
                'maximumLowerSourceUvShiftPixels':float(shift[lower_owner].max()) if lower_owner.any() else 0,
                'lowerPixelsAnyByteChanged':int(np.count_nonzero(change[lower_owner])),
                'maximumLowerRgbaByteDifference':int(change[lower_owner].max()) if lower_owner.any() else 0})
            cx,cy=np.rint(anchors[1]).astype(int)
            for row,im in [(0,Image.fromarray(base[leg])),(column,image)]:
                crop=green(np.asarray(im)).crop((cx-42,cy-42,cx+42,cy+42)).resize((168,168),Image.Resampling.NEAREST)
                joints.paste(crop,(leg*216,row*240+50))
                jd.text((leg*216+5,row*240+10),f'{limb["id"]} '+('baseline' if row==0 else f'peakx{strength}'),fill='black',font=FONT)
        whole=_over(torso,whole)
        encode(whole).save(out/'Raw'/f'volume{strength}.png')
        encode(torso).save(out/'Masks'/f'torso-volume{strength}.png')
        canvas.paste(encode(whole,green=True),(column*576,60))
        draw.text((column*576+8,10),f'Midshaft peakx{strength}; same sourceRGBA',fill='black',font=FONT)
        draw.text((column*576+8,32),'One continuousQ ->oneUV ->one material sample',fill='black',font=FONT)
        records.append({'peak':strength,'pose':pose,'surfaceChecks':checks,**metrics(limbs,torso)})
    canvas.save(out/'two-volume-controls-native-green.png')
    joints.save(out/'all-six-joints-volume-2x-nearest.png')
    report={'status':'Two control-only soft-envelope proposals. Await actual-art inspection; no production or mass.',
        'note':'Peak parameter is normal expansion at shaft axis; the lower-side compact band limits bulge to protect distal paint.',
        'identity':identity,'cases':records}
    (out/'proof.json').write_text(json.dumps(report,indent=2))
    manifest={'upperVolumeModuleSha256':digest(HERE/'upper_volume_painter.py'),
        'continuousUvSha256':digest(HERE/'continuous_polar_painter.py'),
        'sourceOwnershipModuleSha256':digest(HERE/'source_ownership_refined.py'),
        'baselineRawSha256':digest(before_folder/'Raw'/f'{pose["name"]}.png'),
        'rawPngSha256':{path.name:digest(path) for path in (out/'Raw').glob('*.png')}}
    (out/'manifest.json').write_text(json.dumps(manifest,indent=2))
    for case in records:
        print('peak',case['peak'],'pairs',[(p['pair'],p['pixels']) for p in case['all15Pairs'] if p['pixels']],
            'holes',sum(h['pixels'] for l in case['legs'] for group in l['holes'] for h in group['parts']),
            'lowerbytes',[(r['leg'],r['lowerPixelsAnyByteChanged'],r['maximumLowerSourceUvShiftPixels']) for r in case['surfaceChecks']],flush=True)
    print('Identity differences:',sum(row['byteDifferences'] for row in identity),flush=True)


if __name__=='__main__':
    main()
