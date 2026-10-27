"""Verify the canonical painter against a full-canvas affine reference."""
from pathlib import Path
import json
import sys
import time
import numpy as np
from PIL import Image

OUT=Path(__file__).resolve().parent
FAMILY=OUT.parent
sys.path.insert(0,str(FAMILY))
from sprite_painter import SpritePainter

sys.path.insert(0,str(OUT))
import compose_candidate as reference

start=time.perf_counter()
painter=SpritePainter(FAMILY.parents[3])
mounted=json.loads((OUT/'mounted-pose.json').read_text())
load_seconds=time.perf_counter()-start
start=time.perf_counter()
whole,limbs,torso=painter.paint(mounted)
paint_seconds=time.perf_counter()-start
assert whole.shape==(576,576,4)
assert whole.dtype==np.float32
assert len(limbs)==6
assert np.isfinite(whole).all()
assert whole.min()>=0 and whole.max()<=1.000001
assert np.all(whole[...,:3]<=whole[...,3:4]+1e-6)
reference.encode(whole).save(OUT/'canonical-mounted.png')
green=reference.encode(whole,[0,1,0])
green.save(OUT/'canonical-mounted-green.png')
green.crop((160,160,416,448)).resize((768,864),Image.Resampling.LANCZOS).save(OUT/'canonical-mounted-close.png')

errors=[]
for leg,limb in enumerate(painter.anatomy['limbs']):
    for kind,part in zip(['upper','lower','joint','root'],painter._parts[leg]):
        source=np.zeros((406,358,4),dtype=np.float32)
        left,top,right,bottom=part.box
        source[top:bottom,left:right]=np.stack([np.asarray(channel) for channel in part.channels],axis=-1)
        if kind=='upper':
            a,b=limb['hip'],limb['knee']
            ta,tb=mounted['hips'][leg],mounted['knees'][leg]
        elif kind=='lower':
            a,b=limb['knee'],limb['tip']
            ta,tb=mounted['knees'][leg],mounted['feet'][leg]
        else:
            key='knee' if kind=='joint' else 'hip'
            pose_key='knees' if kind=='joint' else 'hips'
            a=np.asarray(limb[key],dtype=np.float32)
            b=a+[1,0]
            ta=np.asarray(mounted[pose_key][leg],dtype=np.float32)
            tb=ta+[1/224,0,0]
        full=reference.transform(source,a,b,ta,tb)
        roi=painter._transform(part,a,b,ta,tb)
        difference=np.abs(full-roi)
        errors.append({'leg':leg,'part':kind,'maxFloatError':float(difference.max()),
            'meanFloatError':float(difference.mean()),'pixelsWithAlphaErrorOver01':int((difference[...,3]>.01).sum())})
assert max(item['maxFloatError'] for item in errors)<.001

report={'loadSeconds':load_seconds,'paintSeconds':paint_seconds,'api':{'shape':list(whole.shape),
    'dtype':str(whole.dtype),'limbs':len(limbs),'finite':bool(np.isfinite(whole).all()),
    'maxPremultiplicationError':float(np.maximum(whole[...,:3]-whole[...,3:4],0).max())},
    'roiReferenceComparison':errors,'worstRoiReferenceError':max(item['maxFloatError'] for item in errors)}
(OUT/'canonical-check.json').write_text(json.dumps(report,indent=2))
print(json.dumps({key:value for key,value in report.items() if key!='roiReferenceComparison'},indent=2))
