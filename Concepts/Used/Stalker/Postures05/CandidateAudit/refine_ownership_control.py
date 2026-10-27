"""Exactly two deep-mount poses, comparing old vs corrected source tracing."""
import json
import numpy as np
from PIL import Image,ImageDraw
from continuous_polar_painter import SpritePainter
from source_ownership_study import HERE,FAMILY,PROJECT,paint,metrics,digest
from source_ownership_refined import owned_materials,BODY_OWNER
from compose_parts import encode
from qa_parts import FONT


def main():
    out=HERE/'SourceOwnershipStudy02'
    if out.exists():
        raise FileExistsError('SourceOwnershipStudy02 is frozen')
    (out/'Raw').mkdir(parents=True)
    (out/'Masks').mkdir()
    painter=SpritePainter(PROJECT)
    sources,torso,owner,native=owned_materials(painter)
    poses=json.loads((FAMILY/'DeepMountStudy/poses.json').read_text())
    sheet=Image.new('RGB',(1152,1272),'#00ff00');d=ImageDraw.Draw(sheet)
    records=[]
    for row,pose in enumerate(poses):
        baseline=Image.open(HERE/'DeepMountOwnedStudy01/Raw'/f'{row}-1.png').convert('RGBA')
        from native_uv_painter import _decode
        sheet.paste(encode(_decode(baseline),green=True),(0,row*636+60))
        whole,limbs,actual_torso=paint(painter,pose,sources,torso)
        name=f'deep-owned02-{row}'
        encode(whole).save(out/'Raw'/f'{name}.png')
        encode(actual_torso).save(out/'Masks'/f'torso-{name}.png')
        for leg,limb in enumerate(limbs):
            encode(limb).save(out/'Masks'/f'leg-{name}-{leg}.png')
        sheet.paste(encode(whole,green=True),(576,row*636+60))
        for col,label in enumerate(['owner01 underbelly cut','owner02 native underbelly edge']):
            d.text((col*576+8,row*636+8),label,fill='black',font=FONT)
            d.text((col*576+8,row*636+32),f'row{pose["row"]}/phase{pose["phase"]} same deepmount/native224PPU',fill='black',font=FONT)
        records.append({'name':name,'pose':pose,**metrics(limbs,actual_torso)})
    sheet.save(out/'underbelly-trace-before-after-native-green.png')
    proof=Image.new('RGB',(1074,466),'#00ff00');pd=ImageDraw.Draw(proof)
    traced=encode(native,green=True)
    ImageDraw.Draw(traced).line(BODY_OWNER+[BODY_OWNER[0]],fill='blue',width=1)
    proof.paste(traced,(0,60));proof.paste(encode(native*owner[...,None],green=True),(358,60))
    a=np.asarray(encode(native))[...,3]
    profile=[]
    for x in range(165,211):
        empty=np.flatnonzero(a[235:270,x]<=4)
        if len(empty):
            y=235+int(empty[0]);profile.append({'x':x,'lastNativeAlphaOver4Y':y-1,
                'nativeEdgePreservedByOwnership':bool(np.all(owner[235:y,x]))})
    native_edge=np.asarray(encode(native,green=True)).copy()
    for item in profile:
        x,y=item['x'],item['lastNativeAlphaOver4Y']
        native_edge[y,x]=(255,0,0)
    crop=Image.fromarray(native_edge).crop((155,230,220,260)).resize((325,150),Image.Resampling.NEAREST)
    proof.paste(crop,(730,100))
    for col,label in enumerate(['Binary source trace02','Original bodyRGBA, no new alpha','Nativealpha edge red, 5xnearest']):
        pd.text((col*358+5,10),label,fill='black',font=FONT)
    proof.save(out/'source-owner-underbelly-evidence.png')
    source_checks=[]
    for leg,(before,after) in enumerate(zip(painter.sources,sources)):
        source_checks.append({'leg':leg,'unownedSourceExact':bool(np.array_equal(before[~owner],after[~owner])),
            'removedAlphaOver4':int(((before[...,3]>4/255)&owner).sum())})
    report={'status':'Source tracing refinement only. Real closed leg crossings remain blockers.',
        'sourceOwnerPolygon':BODY_OWNER,'nativeUnderbellyAlphaProfile':profile,'sourceChecks':source_checks,'cases':records}
    (out/'proof.json').write_text(json.dumps(report,indent=2))
    manifest={'continuousUvSha256':digest(HERE/'continuous_polar_painter.py'),
        'sourceOwnershipModuleSha256':digest(HERE/'source_ownership_refined.py'),
        'deepMountSourcePosesSha256':digest(FAMILY/'DeepMountStudy/poses.json'),
        'rawPngSha256':{path.name:digest(path) for path in (out/'Raw').glob('*.png')}}
    (out/'manifest.json').write_text(json.dumps(manifest,indent=2))
    print('Nativeunderbelly preserved all:',all(p['nativeEdgePreservedByOwnership'] for p in profile))
    for case in records:
        print(case['name'],'crossings',[(p['pair'],p['pixels']) for p in case['all15Pairs'] if p['pixels']],flush=True)


if __name__=='__main__':
    main()
