"""Whole-sprite edit references. Never cuts/transforms individual body parts."""
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
import json,hashlib,shutil,math
import numpy as np

OUT=Path(__file__).resolve().parent
ROOT=OUT.parents[3]
EVIDENCE=ROOT/'Docs/Validation/Task007Redraw12'
EVIDENCE.mkdir(parents=True,exist_ok=True)
PLAYER=ROOT/'Concepts/Unused/Visual/StalkerSpritePlayer11'
layout=json.loads((PLAYER/'animations.json').read_text(encoding='utf-8-sig'))
reference=json.loads((ROOT/'Concepts/Unused/Visual/StalkerWholeGenerated10/source-bones.json').read_text(encoding='utf-8-sig'))
observed=json.loads((ROOT/'Docs/Validation/Task007WholeFrames07/Visual/SourceKeys02to07/manual-landmarks.json').read_text(encoding='utf-8-sig'))
walk=[None]*8
for n in [0,1]:walk[n]=[dict(x['source00' if n==0 else 'source01']) for x in reference['source00Source01Correspondences']]
for f in observed['frames']:walk[f['frame']]=f['limbs']
assert all(walk)
sources=[]
for state,s in layout['baseline'].items():
    if state=='awakening':continue
    for i,frame in enumerate(s['frames']):
        path=PLAYER/frame['file'];im=Image.open(path).convert('RGBA')
        # Reference rendering uses a single uniform world scale, like SpriteRenderer.
        scale=224/frame['ppu'];size=(round(im.width*scale),round(im.height*scale))
        rendered=im.resize(size,Image.Resampling.LANCZOS) if scale!=1 else im
        canvas=Image.new('RGBA',(576,576));canvas.alpha_composite(rendered,((576-rendered.width)//2,(576-rendered.height)//2))
        target=OUT/'anchors'/state/f'{i:02d}.png';target.parent.mkdir(parents=True,exist_ok=True);canvas.save(target)
        sources.append({'state':state,'index':i,'nativeExport':str(path.relative_to(ROOT)),'nativeSha256':hashlib.sha256(path.read_bytes()).hexdigest(),'nativePPU':frame['ppu'],'wholeRenderScale':scale,'file':target.relative_to(OUT).as_posix(),'sha256':hashlib.sha256(target.read_bytes()).hexdigest()})
        matte=Image.new('RGBA',(576,576),'#e5e6e2');matte.alpha_composite(canvas);matte.convert('RGB').save(target.with_name(target.stem+'-matte.png'))

palette=['#10bccd','#e5b829','#38bd83','#e68e46','#a49bff','#e48fd1']
poses=[]
plate=Image.new('RGB',(1152,1152),'#e5e6e2')
guide=Image.new('RGB',(1152,1152),'#e5e6e2')
for i,t in enumerate([0,1/3,2/3,1]):
    anchor=0 if i<2 else 1
    plate.paste(Image.open(OUT/f'anchors/movement/{anchor:02d}-matte.png'),((i%2)*576,(i//2)*576))
    limbs=[]
    for a,b in zip(walk[0],walk[1]):limbs.append({'id':a['id'],**{k:((1-t)*np.array(a[k])+t*np.array(b[k])).tolist() for k in ['H','J','T']}})
    poses.append({'cell':i,'fraction':t,'limbs':limbs})
    d=ImageDraw.Draw(guide);ox=(i%2)*576;oy=(i//2)*576
    d.polygon([(ox+289,oy+222),(ox+231,oy+322),(ox+344,oy+324)],outline='#71797b',width=2)
    d.ellipse((ox+266,oy+265,ox+310,oy+310),outline='#71797b',width=2)
    for j,b in enumerate(limbs):
        points=[(ox+b[k][0],oy+b[k][1]) for k in ['H','J','T']]
        d.line(points,fill=palette[j],width=2)
        for x,y in points:d.ellipse((x-3,y-3,x+3,y+3),outline=palette[j],width=1)
plate.save(OUT/'walk00-01-edit-target.png');guide.save(OUT/'walk00-01-fixed-bones.png')
muscles=ROOT/'Docs/Validation/Task007WholeFrames07/Anatomy00/m02v2-muscles-light.png'
shutil.copy2(muscles,OUT/'source-muscles-reference.png')
shutil.copy2(ROOT/'Concepts/Unused/Visual/StalkerWholeGenerated10/source-muscles.json',OUT/'source-muscles.json')
counts=[]
for i in range(8):
    distance=max(float(np.linalg.norm(np.array(a[k])-b[k])) for a,b in zip(walk[i],walk[(i+1)%8]) for k in ['J','T'])
    counts.append(max(3,math.ceil(distance/10)))
(OUT/'plan.json').write_text(json.dumps({'scope':'Full replacement motion, sleep entry, awakening and attack. No video, old rejected sequences or body-part assembly.','sourceIdentity':'Original pre-rig Stalker; existing original drawings are anchors, not new drawings.','method':'AI edits complete pose plates. Only entire sprites are placed in reference layouts. No body pixels cut/rearranged/deformed or painted by code. Flat matte references show actual RGBA, not hidden alpha-zero RGB.','pilot':poses,'walkOriginalControls':walk,'walkMinimumSubsteps':counts,'qualityGatesBeforeArt':{'wholeCanvas':[576,576],'maxAdjacentVisibleJointOrTipDeltaPx':10,'maxUnintendedBodyAnchorDriftPx':2,'maxBodyScaleDriftPercent':2,'bodyPatchMAE_max':0.04,'bodyPatchSSIM_min':0.96,'unchangedAnatomy':'Six limb identities/attachments, one outer joint and one rigid pointed distal each; stable22functional groups/root/shaft/tendon relationships. Covered anatomy UNKNOWN, not recovered3D.','validationOrder':'Actual whole-sprite bones, then volumes, composition/OI, skin and all neighbor transitions/loop. Failure requires whole AI edit/regeneration; never pixel/part repair.'},'sources':sources,'status':'All4states required. Pilot has no acceptance yet.'},ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps({'anchors':len(sources),'pilotCells':4,'minimumWalkFrames':sum(counts),'walkSubsteps':counts}))
