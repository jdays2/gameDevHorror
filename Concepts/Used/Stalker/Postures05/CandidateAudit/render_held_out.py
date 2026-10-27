"""Frozen21-pose study only, native224PPU; no production/mass writes."""
from pathlib import Path
import hashlib
import json
import shutil
import sys
import numpy as np
from PIL import Image, ImageDraw

HERE=Path(__file__).resolve().parent
FAMILY=HERE.parent
PROJECT=FAMILY.parents[3]
sys.path.insert(0,str(FAMILY))
from native_uv_painter import SpritePainter
from compose_parts import encode
from attack_mouth import AttackMouth
from qa_parts import FONT


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    out=HERE/'HeldOut01'
    if out.exists():
        raise FileExistsError('HeldOut01 is frozen. Use a new version for later candidates.')
    (out/'Raw').mkdir(parents=True)
    (out/'Masks').mkdir()
    body=json.loads((FAMILY/'poses.json').read_text())
    attack=json.loads((FAMILY/'attack-poses.json').read_text())
    painter=SpritePainter(PROJECT)
    mouth=AttackMouth(PROJECT,painter._torso)
    chosen=[]
    images={}
    for row in [8,4,0,7]:
        sheet=Image.new('RGB',(1152,1272),'#00ff00')
        draw=ImageDraw.Draw(sheet)
        for index,phase in enumerate([0,12,24,36]):
            pose=next(pose for pose in body if pose['row']==row and pose['phase']==phase)
            name=f'body-{row}-{phase:02d}'
            whole,limbs,torso=painter.paint(pose)
            encode(whole).save(out/'Raw'/f'{name}.png')
            encode(torso).save(out/'Masks'/f'torso-{name}.png')
            for leg,limb in enumerate(limbs):
                encode(limb).save(out/'Masks'/f'leg-{row}-{phase:02d}-{leg}.png')
            x,y=index%2*576,index//2*636
            sheet.paste(encode(whole,green=True),(x,y+60))
            draw.text((x+8,y+8),f'H01 row{row} phase{phase:02d} |224PPU native576',fill='black',font=FONT)
            draw.text((x+8,y+31),'Study only. Form review; no acceptance.',fill='black',font=FONT)
            chosen.append({**pose,'name':name})
            images[name]=whole
        sheet.save(out/f'body-row-{row}-native-green.png')
        print(f'READY body row{row}: {out/f"body-row-{row}-native-green.png"}',flush=True)
    names=['entry-12-01','entry-12-07','strike-09','return-36-01','return-36-07']
    sheet=Image.new('RGB',(1728,1272),'#00ff00')
    draw=ImageDraw.Draw(sheet)
    for index,name in enumerate(names):
        pose=next(pose for pose in attack if pose['name']==name)
        whole,limbs,torso=painter.paint(pose)
        whole,torso,_=mouth.apply(pose,whole,limbs,torso)
        encode(whole).save(out/'Raw'/f'{name}.png')
        encode(torso).save(out/'Masks'/f'torso-{name}.png')
        for leg,limb in enumerate(limbs):
            encode(limb).save(out/'Masks'/f'leg-{name}-{leg}.png')
        x,y=index%3*576,index//3*636
        sheet.paste(encode(whole,green=True),(x,y+60))
        draw.text((x+8,y+8),f'H01 {name} |224PPU native576',fill='black',font=FONT)
        draw.text((x+8,y+31),'Study only. Native head overlay on strike.',fill='black',font=FONT)
        chosen.append(pose)
    sheet.save(out/'attack-five-native-green.png')
    (out/'poses.json').write_text(json.dumps(chosen,indent=2))
    shutil.copy2(HERE/'native_uv_painter.py',out/'renderer-snapshot.py.txt')
    manifest={'version':'HeldOut01','status':'Study only; closed poses fail organic-form review. Alpha/geometry checks cannot approve art.',
        'sourceBodyPosesSha256':digest(FAMILY/'poses.json'),'sourceAttackPosesSha256':digest(FAMILY/'attack-poses.json'),
        'sourceAtlasSha256':digest(PROJECT/'Assets/EmergencyShift/Art/Enemies/Stalker/Locomotion02/rig-atlas.png'),
        'rendererSha256':digest(HERE/'native_uv_painter.py'),'legacyUtilitySha256':digest(FAMILY/'sprite_painter.py'),
        'mouthProcessorSha256':digest(FAMILY/'attack_mouth.py'),'frames':len(chosen),
        'sourceCleanup':painter.source_cleanup,
        'rawPngSha256':{path.name:digest(path) for path in sorted((out/'Raw').glob('*.png'))}}
    (out/'manifest.json').write_text(json.dumps(manifest,indent=2))
    print('READY frozen21-pose HeldOut01',flush=True)


if __name__=='__main__':
    main()
