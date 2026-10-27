"""Frozen59-pose actual-C# held-out study, without deliberate volume widening."""
import argparse
import json
import shutil
import numpy as np
from PIL import Image,ImageDraw,ImageFilter
from continuous_polar_painter import SpritePainter,field_with_metrics
from source_ownership_refined import owned_materials
from source_ownership_study import HERE,FAMILY,PROJECT,paint,metrics,digest
from compose_parts import encode
from attack_mouth import AttackMouth
from qa_parts import FONT
from native_uv_painter import _decode


def inspect(painter,pose,limbs,torso):
    checks=[]
    for leg,(limb,skin) in enumerate(zip(painter.anatomy['limbs'],limbs)):
        alpha=np.asarray(encode(skin))[...,3]
        yy,xx=np.nonzero(alpha>230)
        _,j=field_with_metrics(limb,pose,leg,np.column_stack([xx,yy]))
        inner=np.asarray(Image.fromarray((alpha>128).astype('uint8')*255).filter(ImageFilter.MinFilter(9)))>128
        values=alpha[inner]
        checks.append({'leg':leg,'opaquePixels':len(xx),'analyticalJacobianMinimum':float(np.nanmin(j['jacobian'])),
            'nonpositiveJacobian':int(np.count_nonzero(j['jacobian']<=0)),
            'polarCentreSamples':int(j['polarCentre'].sum()),
            'erode4AlphaMinimum':int(values.min()) if len(values) else None,
            'erode4AlphaBelow230':int(np.count_nonzero(values<230)),
            'borderPixelsAlphaOver1':int(np.count_nonzero(alpha[[0,-1],:]>1)+np.count_nonzero(alpha[:,[0,-1]]>1))})
    return {'surfaceChecks':checks,**metrics(limbs,torso)}


def main():
    parser=argparse.ArgumentParser()
    parser.add_argument('--out',default='ActualHeldOut02')
    parser.add_argument('--reuse-body')
    parser.add_argument('--extra-attack',default='')
    args=parser.parse_args()
    out=HERE/args.out
    if out.exists():
        raise FileExistsError('ActualHeldOut02 is frozen; use a new version')
    (out/'Raw').mkdir(parents=True)
    (out/'Masks').mkdir()
    body_bytes=(FAMILY/'poses.json').read_bytes()
    attack_bytes=(FAMILY/'attack-poses.json').read_bytes()
    body=json.loads(body_bytes)
    attack=json.loads(attack_bytes)
    bones_bytes=(FAMILY/'bones.json').read_bytes()
    bones=json.loads(bones_bytes)
    painter=SpritePainter(PROJECT)
    sources,torso,owner,native=owned_materials(painter)
    mouth=AttackMouth(PROJECT,torso)
    reuse=HERE/args.reuse_body if args.reuse_body else None
    if reuse:
        import hashlib
        cache=json.loads((reuse/'partial-source.json').read_text())
        assert cache['bodyPosesSha256']==hashlib.sha256(body_bytes).hexdigest()
        assert cache['continuousUvSha256']==digest(HERE/'continuous_polar_painter.py')
        assert cache['sourceOwnershipModuleSha256']==digest(HERE/'source_ownership_refined.py')
    records=[]
    chosen=[]
    for row in range(9):
        sheet=Image.new('RGB',(1728,1272),'#00ff00')
        d=ImageDraw.Draw(sheet)
        for index,phase in enumerate([0,1,12,24,25,36]):
            pose=next(p for p in body if p['row']==row and p['phase']==phase)
            name=f'body-{row}-{phase:02d}'
            cache_paths=[reuse/'Raw'/f'{name}.png',reuse/'Masks'/f'torso-{name}.png'] if reuse else []
            if reuse:
                cache_paths += [reuse/'Masks'/f'leg-{row}-{phase:02d}-{leg}.png' for leg in range(6)]
            if reuse and all(path.exists() for path in cache_paths):
                decoded=[]
                for path in cache_paths:
                    with Image.open(path) as cached:
                        decoded.append(_decode(cached.convert('RGBA')))
                whole,actual_torso=decoded[:2]
                limbs=decoded[2:]
            else:
                whole,limbs,actual_torso=paint(painter,pose,sources,torso)
            encode(whole).save(out/'Raw'/f'{name}.png')
            encode(actual_torso).save(out/'Masks'/f'torso-{name}.png')
            for leg,limb in enumerate(limbs):
                encode(limb).save(out/'Masks'/f'leg-{row}-{phase:02d}-{leg}.png')
            x,y=index%3*576,index//3*636
            sheet.paste(encode(whole,green=True),(x,y+60))
            d.text((x+8,y+8),f'{args.out} row{row}/phase{phase:02d} |native224PPU',fill='black',font=FONT)
            d.text((x+8,y+32),f'C{bones["cycleDistance"]} F{bones["frontSupportFraction"]}/H{bones["hindSupportFraction"]}/M{bones["middleSupportFraction"]} | no volume',fill='black',font=FONT)
            tagged={**pose,'name':name}
            chosen.append(tagged)
            records.append({'name':name,**inspect(painter,pose,limbs,actual_torso)})
        sheet.save(out/f'body-row-{row}-native-green.png')
        print(f'READY actualheldout row{row}/8 {out/f"body-row-{row}-native-green.png"}',flush=True)
    names=['entry-12-01','entry-12-07','strike-09','return-36-01','return-36-07']
    names += [name for name in args.extra_attack.split(',') if name]
    sheet=Image.new('RGB',(1728,636*((len(names)+2)//3)),'#00ff00');d=ImageDraw.Draw(sheet)
    (out/'AttackGreen').mkdir()
    for index,name in enumerate(names):
        pose=next(p for p in attack if p['name']==name)
        whole,limbs,actual_torso=paint(painter,pose,sources,torso)
        whole,actual_torso,_=mouth.apply(pose,whole,limbs,actual_torso)
        encode(whole).save(out/'Raw'/f'{name}.png')
        encode(whole,green=True).save(out/'AttackGreen'/f'{name}.png')
        encode(actual_torso).save(out/'Masks'/f'torso-{name}.png')
        for leg,limb in enumerate(limbs):
            encode(limb).save(out/'Masks'/f'leg-{name}-{leg}.png')
        x,y=index%3*576,index//3*636
        sheet.paste(encode(whole,green=True),(x,y+60))
        d.text((x+8,y+8),f'{args.out} {name} |native224PPU',fill='black',font=FONT)
        d.text((x+8,y+32),'Same whole-nativeUV/owner02; native mouth on strike',fill='black',font=FONT)
        chosen.append(pose)
        records.append({'name':name,**inspect(painter,pose,limbs,actual_torso)})
        print(f'READY actualheldout attack {name}',flush=True)
    sheet.save(out/'attack-five-native-green.png')
    (out/'poses.json').write_text(json.dumps(chosen,indent=2))
    report={'status':'Frozen held-out59 only. Numeric checks do not establish art/motion acceptance.',
        'frames':len(records),'deliberateVolumeWidening':False,'cases':records}
    (out/'proof.json').write_text(json.dumps(report,indent=2))
    import hashlib
    manifest={'version':args.out,'frames':len(chosen),
        'bodyPosesSha256':hashlib.sha256(body_bytes).hexdigest(),
        'attackPosesSha256':hashlib.sha256(attack_bytes).hexdigest(),
        'bonesMetadata':bones,'bonesMetadataSha256':hashlib.sha256(bones_bytes).hexdigest(),
        'continuousUvSha256':digest(HERE/'continuous_polar_painter.py'),
        'sourceOwnershipModuleSha256':digest(HERE/'source_ownership_refined.py'),
        'sourceCacheSha256':digest(HERE/'native_uv_painter.py'),
        'legacyUtilitySha256':digest(FAMILY/'sprite_painter.py'),
        'mouthModuleSha256':digest(FAMILY/'attack_mouth.py'),
        'rawPngSha256':{path.name:digest(path) for path in sorted((out/'Raw').glob('*.png'))}}
    for module in ['continuous_polar_painter.py','source_ownership_refined.py','native_uv_painter.py']:
        shutil.copy2(HERE/module,out/(module+'.txt'))
    (out/'manifest.json').write_text(json.dumps(manifest,indent=2))
    summary={'frames':len(records),
        'pairPixelsOutsideActualTorso':sum(pair['pixels'] for r in records for pair in r['all15Pairs']),
        'nonpositiveJacobian':sum(c['nonpositiveJacobian'] for r in records for c in r['surfaceChecks']),
        'holesAlphaOver4':sum(h['pixels'] for r in records for l in r['legs'] for group in l['holes'] if group['alphaOver']==4 for h in group['parts']),
        'holesAlphaOver128':sum(h['pixels'] for r in records for l in r['legs'] for group in l['holes'] if group['alphaOver']==128 for h in group['parts'])}
    print(json.dumps(summary,indent=2),flush=True)


if __name__=='__main__':
    main()
