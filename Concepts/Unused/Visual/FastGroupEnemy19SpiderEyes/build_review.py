"""Diagnostic plates only: crops/compositing/unchanged source anatomy, no asset edits."""
from pathlib import Path
import json, hashlib
from PIL import Image, ImageDraw, ImageFont

ROOT=Path(__file__).resolve().parent
SOURCE=ROOT.parent/'FastGroupEnemy13AssetInspired'
PLAN=json.loads((SOURCE/'anatomy-plan-v4.json').read_text(encoding='utf-8-sig'))
MUSCLES=json.loads((SOURCE/'anatomy-muscle-plan-v4.json').read_text(encoding='utf-8-sig'))
OUT=ROOT/'qa'
OUT.mkdir(exist_ok=True)
def project(p): return (760+375*p[0],665+375*p[1])
def base(name):
    im=Image.open(ROOT/name).convert('RGBA')
    return Image.alpha_composite(Image.new('RGBA',im.size,'#242b30'),im)
def save_bones(name):
    im=base(name);d=ImageDraw.Draw(im)
    d.line([project(PLAN['head']['center'])]+[project(p) for p in PLAN['spine']],fill='#66e6ff',width=3)
    for family,keys in [('fronts',['shoulder','elbow','wrist','palm']),('rears',['hip','knee','hock','palm'])]:
        for limb in PLAN[family]:
            points=[project(limb[k]) for k in keys]
            d.line(points,fill='#ffe681',width=4)
            for p in points:d.ellipse((p[0]-5,p[1]-5,p[0]+5,p[1]+5),fill='#ffffff')
    im.save(OUT/(Path(name).stem+'-01-source-bones.png'))
def save_muscles(name,ids=False):
    im=base(name);layer=Image.new('RGBA',im.size);d=ImageDraw.Draw(layer)
    for g in MUSCLES['groups']:
        a,b=project(g['from']),project(g['to'])
        d.line([a,b],fill=g['color']+('d0' if ids else '70'),width=max(2,round(g['radius']*375)))
        if ids:
            d.line([a,b],fill='white',width=2)
            d.text(a,g['id'],fill='white',stroke_width=2,stroke_fill='#101316')
    Image.alpha_composite(im,layer).save(OUT/(Path(name).stem+('-03-source-IDs.png' if ids else '-02-source-volumes.png')))
def compare(items,file):
    plate=Image.new('RGB',(420*len(items),420),'#242b30');d=ImageDraw.Draw(plate)
    for i,(name,label) in enumerate(items):
        # Identical head crop/window for all options; final PNGs remain untouched.
        head=base(name).crop((290,525,690,805)).resize((400,280))
        plate.paste(head.convert('RGB'),(i*420+10,60));d.text((i*420+20,20),label,fill='white')
    plate.save(ROOT/file)
if __name__=='__main__':
    finals=['lateral-v1.png','D-side-rows.png','E-irregular.png','F-wide-head.png','G-narrow-wedge.png','H-broad-spade.png','I-closed-mouth.png','J-closed-mandibles.png']
    for name in finals:save_bones(name)
    # 02/03 use unchanged body plan; G/H head volumes are separate prospective choices.
    for name in finals:save_muscles(name);save_muscles(name,True)
    compare([('lateral-v1.png','C - reference'),('G-narrow-wedge.png','G - narrow wedge'),('H-broad-spade.png','H - broad spade')],'head-shape-comparison.png')
    compare([('D-side-rows.png','D - more side eyes'),('E-irregular.png','E - irregular'),('F-wide-head.png','F - wider head')],'eye-options-comparison.png')
    compare([('H-broad-spade.png','H - reference'),('I-closed-mouth.png','I - closed mouth'),('J-closed-mandibles.png','J - main design')],'mouth-mandible-comparison.png')
    jaw=base('J-closed-mandibles.png');d=ImageDraw.Draw(jaw)
    for a,b in [((450,610),(383,660)),((450,720),(383,670))]:
        d.line([a,b],fill='#ffe681',width=3)
        for p in [a,b]:d.ellipse((p[0]-4,p[1]-4,p[0]+4,p[1]+4),fill='white')
    jaw.save(OUT/'J-head-01-prospective-jaws.png')
    hashes={p.name:hashlib.sha256(p.read_bytes()).hexdigest() for p in ROOT.glob('*.png')}
    (ROOT/'art-sha256.json').write_text(json.dumps(hashes,indent=2)+'\n',encoding='utf-8')
    print(json.dumps({'files':len(finals),'muscleIDs':len(MUSCLES['groups']),'finalAssetsModified':False}))
