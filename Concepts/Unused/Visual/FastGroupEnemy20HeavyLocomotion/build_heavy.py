"""A new keyed gait for the existing fixed-length creature rig.

Animation rendering reuses the project's established continuous skin renderer.
The approved J bitmap is the single source; no per-frame image generation.
"""
from pathlib import Path
import hashlib,json,math,sys,time
import numpy as np
from PIL import Image,ImageDraw

ROOT=Path(__file__).resolve().parent
PARENT=ROOT.parent/'FastGroupEnemy17SimplifiedMotion'
sys.path.insert(0,str(PARENT))
import build_motions as base
from skin_surface import SkinSurface

MASTER=ROOT.parent/'FastGroupEnemy19SpiderEyes/J-closed-mandibles.png'
base.ROOT=ROOT
base.ART=Image.open(MASTER).convert('RGBA')
COUNT=24
DURATIONS=[50]*COUNT
STRIDE=.70
DUTY=.66
REAR_DUTY=.65
REAR_CYCLES=2
REAR_STRIDE=STRIDE/DUTY*REAR_DUTY/REAR_CYCLES

def pose(t,index=None):
    # Lower support stance, small fore/aft load transfer, no lateral sway or hop.
    motion=np.array([.018*math.cos(4*math.pi*t),0.,-.16+.006*math.cos(4*math.pi*t)])
    result={'index':index,'rootOffset3D':motion.tolist(),'limbs':{}}
    for ident,rest in base.REST.items():
        front=ident[0]=='F'
        side=-1 if ident[1]=='U' else 1
        phase=0 if ident[1]=='U' else .5
        if front:
            q=(t+phase)%1
            dx,lift,state=base.foot_trajectory(q,STRIDE,DUTY,-.22)
        else:
            q=(REAR_CYCLES*t+phase+.23)%1
            dx,lift,state=base.foot_trajectory(q,REAR_STRIDE,REAR_DUTY,-.20)
        joints=rest.copy()
        joints[0]+=motion
        joints[2:,0]+=dx
        # Keep the claw silhouette forward. Lateral motion is a tiny clearance.
        joints[2:,1]+=side*(.008*lift if front else .035+.008*lift)
        joints[2:,2]+=(.045 if front else .035)*lift
        lengths=np.linalg.norm(np.diff(rest,axis=0),axis=1)
        joints[1]=base.ik(joints[0],joints[2],rest[1]+motion,*lengths[:2])
        result['limbs'][ident]={'phase':q,'state':state,'joints3D':joints.tolist(),'projected':base.project(joints).tolist()}
    result['spine']=base.project(np.asarray(base.PLAN['spine'])+motion).tolist()
    return result

def plan():
    ROOT.mkdir(exist_ok=True)
    sequence=[pose(i/COUNT,i) for i in range(COUNT)]
    lengths=[]
    support=[]
    previous=[]
    for t in np.arange(0,1,.001):
        p=pose(t)
        old=base.locomotion_pose('run',t)
        support.append(sum(l['state']=='stance' for l in p['limbs'].values()))
        previous.append(sum(l['state']=='stance' for l in old['limbs'].values()))
        for ident,limb in p['limbs'].items():
            actual=np.linalg.norm(np.diff(limb['joints3D'],axis=0),axis=1)
            expected=np.linalg.norm(np.diff(base.REST[ident],axis=0),axis=1)
            lengths.extend(abs(actual-expected))
    report={'old':{'loopMs':480,'minContacts':min(previous),'zeroContactFraction':previous.count(0)/len(previous),'frontStride':.34,'frontDuty':.43,'frontLift':.18},
            'new':{'loopMs':sum(DURATIONS),'frontStride':STRIDE,'frontDuty':DUTY,'frontLift':.045,'rearCyclesPerFrontCycle':REAR_CYCLES,'rearStride':REAR_STRIDE,'rearDuty':REAR_DUTY,'minContacts':min(support),'zeroContactFraction':support.count(0)/len(support),'maxBoneLengthError':float(max(lengths))},
            'note':'Contact count validates the declared rig, not hidden medical anatomy or artistic menace.'}
    (ROOT/'gait-comparison.json').write_text(json.dumps(report,indent=2),newline="\n")
    data={'task':'TASK-005','revision':'heavy-locomotion-r2','master':'../FastGroupEnemy19SpiderEyes/J-closed-mandibles.png','masterSha256':hashlib.sha256(MASTER.read_bytes()).hexdigest(),'anatomy':'../FastGroupEnemy13AssetInspired/anatomy-plan-v4.json','canvas':[1024,1024],'sourceScale':base.SCALE,'sourceOffset':base.OFFSET.tolist(),'pivotPixelsTopLeft':base.screen([760,665]).tolist(),'count':COUNT,'durationsMs':DURATIONS,'loop':True,'frames':sequence,'artisticApproval':False}
    (ROOT/'motion-plan.json').write_text(json.dumps(data,indent=2),newline="\n")
    base.contact_sheet([base.bone_frame(p) for p in sequence],ROOT/'01-independent-bones.jpg')
    base.master_check()
    print(json.dumps(report),flush=True)
    return sequence

def render(indices):
    sequence=plan()
    folder=ROOT/'frames';folder.mkdir(exist_ok=True)
    measurements=ROOT/'measurements';measurements.mkdir(exist_ok=True)
    surface=SkinSurface(base)
    for index in indices:
        start=time.perf_counter()
        image,stats=surface.render(sequence[index])
        image.save(folder/f'heavy-{index:02d}.png')
        base.bone_frame(sequence[index],image).save(ROOT/f'sample-bones-{index:02d}.png')
        stats.update(index=index,renderSeconds=time.perf_counter()-start)
        (measurements/f'{index:02d}.json').write_text(json.dumps(stats,indent=2),newline="\n")
        print(json.dumps(stats),flush=True)

if __name__=='__main__':
    if '--plan' in sys.argv:plan()
    else:render([0,6,12,18] if '--sample' in sys.argv else range(COUNT))
