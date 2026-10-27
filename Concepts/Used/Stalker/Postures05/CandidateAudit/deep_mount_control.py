"""Exactly two root-authored deep-mount controls, using fixed owned materials."""
from pathlib import Path
import json
import numpy as np
from PIL import Image, ImageDraw

from continuous_polar_painter import SpritePainter, field_with_metrics
from source_ownership_study import owned_materials, paint, metrics, digest, HERE, FAMILY, PROJECT
from compose_parts import encode
from qa_parts import FONT


def main():
    out=HERE/'DeepMountOwnedStudy01'
    if out.exists():
        raise FileExistsError('Frozen mount controls exist; choose another version')
    (out/'Raw').mkdir(parents=True)
    (out/'Masks').mkdir()
    painter=SpritePainter(PROJECT)
    sources,torso,owner,native=owned_materials(painter)
    baseline=json.loads((HERE/'SourceOwnershipStudy01/proof.json').read_text())['cases']
    before=[case for case in baseline if case['name'].endswith('tracedownership')]
    after=json.loads((FAMILY/'DeepMountStudy/poses.json').read_text())
    sheet=Image.new('RGB',(1152,1272),'#00ff00')
    d=ImageDraw.Draw(sheet)
    records=[]
    for row,(old,new) in enumerate(zip(before,after)):
        for col,(label,pose) in enumerate([('body-owned oldmount',old['pose']),('body-owned deepmount',new)]):
            name=f'{row}-{col}'
            whole,limbs,actual_torso=paint(painter,pose,sources,torso)
            encode(whole).save(out/'Raw'/f'{name}.png')
            encode(actual_torso).save(out/'Masks'/f'torso-{name}.png')
            jacobians=[]
            for leg,limb in enumerate(limbs):
                image=encode(limb)
                image.save(out/'Masks'/f'leg-{name}-{leg}.png')
                yy,xx=np.nonzero(np.asarray(image)[...,3]>230)
                _,check=field_with_metrics(painter.anatomy['limbs'][leg],pose,leg,np.column_stack([xx,yy]))
                jacobians.append({'leg':leg,'minimum':float(np.nanmin(check['jacobian'])),
                    'nonpositive':int(np.count_nonzero(check['jacobian']<=0))})
            sheet.paste(encode(whole,green=True),(col*576,row*636+60))
            d.text((col*576+8,row*636+8),f'row{pose["row"]}/phase{pose["phase"]} | {label}',fill='black',font=FONT)
            d.text((col*576+8,row*636+32),'Same source ownership / sameUV / native224PPU',fill='black',font=FONT)
            records.append({'name':name,'label':label,'pose':pose,'jacobian':jacobians,**metrics(limbs,actual_torso)})
    sheet.save(out/'mount-before-after-native-green.png')
    report={'status':'Two study controls only. No art acceptance or animation claim.',
        'sourceOwnershipManifestSha256':digest(HERE/'SourceOwnershipStudy01/manifest.json'),'cases':records}
    (out/'proof.json').write_text(json.dumps(report,indent=2))
    manifest={'continuousUvSha256':digest(HERE/'continuous_polar_painter.py'),
        'sourceOwnershipModuleSha256':digest(HERE/'source_ownership_study.py'),
        'deepMountSourcePosesSha256':digest(FAMILY/'DeepMountStudy/poses.json'),
        'rawPngSha256':{path.name:digest(path) for path in sorted((out/'Raw').glob('*.png'))}}
    (out/'manifest.json').write_text(json.dumps(manifest,indent=2))
    for case in records:
        print(case['name'],case['label'],'overlaps',[(p['pair'],p['pixels'],p['bbox']) for p in case['all15Pairs'] if p['pixels']],
            'holes',sum(h['pixels'] for leg in case['legs'] for group in leg['holes'] for h in group['parts']),flush=True)


if __name__=='__main__':
    main()
