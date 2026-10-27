"""Render only explicit high-risk actual-C# cases into a new frozen folder."""
import argparse
import hashlib
import json
from PIL import Image,ImageDraw
from continuous_polar_painter import SpritePainter
from source_ownership_refined import owned_materials
from source_ownership_study import HERE,FAMILY,PROJECT,paint,digest
from render_actual_heldout02 import inspect
from compose_parts import encode
from attack_mouth import AttackMouth
from qa_parts import FONT


def main():
    parser=argparse.ArgumentParser()
    parser.add_argument('--out',required=True)
    parser.add_argument('--body',default='6:0,6:1,6:24,6:25,7:0,1:25')
    parser.add_argument('--attack',default='entry-12-07,return-36-01,strike-09')
    args=parser.parse_args()
    out=HERE/args.out
    if out.exists():
        raise FileExistsError('A targeted proof is immutable; use a new version')
    (out/'Raw').mkdir(parents=True)
    (out/'Masks').mkdir()
    body_bytes=(FAMILY/'poses.json').read_bytes()
    attack_bytes=(FAMILY/'attack-poses.json').read_bytes()
    body=json.loads(body_bytes)
    attack=json.loads(attack_bytes)
    selected=[]
    for item in args.body.split(','):
        if item:
            row,phase=map(int,item.split(':'))
            selected.append(({**next(p for p in body if p['row']==row and p['phase']==phase),
                              'name':f'body-{row}-{phase:02d}'},False))
    for name in args.attack.split(','):
        if name:
            selected.append((next(p for p in attack if p['name']==name),True))
    painter=SpritePainter(PROJECT)
    sources,torso,owner,native=owned_materials(painter)
    mouth=AttackMouth(PROJECT,torso)
    sheet=Image.new('RGB',(1728,636*((len(selected)+2)//3)),'#00ff00')
    draw=ImageDraw.Draw(sheet)
    records=[]
    for index,(pose,is_attack) in enumerate(selected):
        name=pose['name']
        whole,limbs,actual_torso=paint(painter,pose,sources,torso)
        if is_attack:
            whole,actual_torso,_=mouth.apply(pose,whole,limbs,actual_torso)
        encode(whole).save(out/'Raw'/f'{name}.png')
        encode(actual_torso).save(out/'Masks'/f'torso-{name}.png')
        stem=name if is_attack else f'{pose["row"]}-{pose["phase"]:02d}'
        for leg,limb in enumerate(limbs):
            encode(limb).save(out/'Masks'/f'leg-{stem}-{leg}.png')
        x,y=index%3*576,index//3*636
        sheet.paste(encode(whole,green=True),(x,y+60))
        draw.text((x+8,y+8),name+' actualC# targeted',fill='black',font=FONT)
        draw.text((x+8,y+32),'Same baselineUV/owner02/native224PPU',fill='black',font=FONT)
        records.append({'name':name,**inspect(painter,pose,limbs,actual_torso)})
        print('READY',name,flush=True)
    sheet.save(out/'targeted-native-green.png')
    (out/'poses.json').write_text(json.dumps([pose for pose,_ in selected],indent=2))
    (out/'proof.json').write_text(json.dumps({'cases':records},indent=2))
    manifest={'bodyPosesSha256':hashlib.sha256(body_bytes).hexdigest(),
        'attackPosesSha256':hashlib.sha256(attack_bytes).hexdigest(),
        'continuousUvSha256':digest(HERE/'continuous_polar_painter.py'),
        'sourceOwnershipModuleSha256':digest(HERE/'source_ownership_refined.py'),
        'rawPngSha256':{path.name:digest(path) for path in (out/'Raw').glob('*.png')}}
    (out/'manifest.json').write_text(json.dumps(manifest,indent=2))
    print(json.dumps({'cases':[(r['name'],[(p['pair'],p['pixels']) for p in r['all15Pairs'] if p['pixels']]) for r in records],
        'nonpositiveJacobian':sum(s['nonpositiveJacobian'] for r in records for s in r['surfaceChecks']),
        'holesOver4':sum(h['pixels'] for r in records for l in r['legs'] for g in l['holes'] if g['alphaOver']==4 for h in g['parts']),
        'holesOver128':sum(h['pixels'] for r in records for l in r['legs'] for g in l['holes'] if g['alphaOver']==128 for h in g['parts'])},indent=2),flush=True)


if __name__=='__main__':
    main()
