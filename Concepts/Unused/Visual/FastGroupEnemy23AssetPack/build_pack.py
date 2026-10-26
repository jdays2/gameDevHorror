"""Finish the selected creature as a silent visual library, without a new enemy brain."""
from pathlib import Path
import hashlib
import json
import math
import sys
import time
import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parent
PROJECT = ROOT.parents[3]
sys.path.insert(0,str(ROOT.parent/'FastGroupEnemy21BodyMotion'))
import build_body as body
rig = body.rig
rig.ROOT = ROOT
COUNT = 24
STEP = .32
DUTY = .78
FRAME_MS = 65
ART = PROJECT/'Assets/EmergencyShift/Art/Enemies/ClawVisual'
CELL = 512


def write(path,value):
    path.parent.mkdir(parents=True,exist_ok=True)
    path.write_text(json.dumps(value,ensure_ascii=False,indent=2),encoding='utf-8',newline='\n')


def walk(t,index=None):
    shape = body.trunk(t)
    shape['yaw'] = (np.asarray(shape['yaw'])*.45).tolist()
    shape['roll'] = (np.asarray(shape['roll'])*.45).tolist()
    shape['pitch'] = (np.asarray(shape['pitch'])*.5).tolist()
    rest = body.REST_SPINE
    nodes = rest.copy()
    nodes[3] += [.006*math.cos(4*math.pi*t),0.,-.16+.003*math.cos(4*math.pi*t)]
    for index_node in [2,1,0]:
        owner = index_node+1
        nodes[index_node] = nodes[owner]+body.rotation(shape['yaw'][owner],shape['pitch'][owner])@(rest[index_node]-rest[owner])
    shape['spine3D'] = nodes.tolist()
    result = {'index':index,'body':shape,'spine':rig.project(nodes).tolist(),
              'rootOffset3D':(nodes[1]-rest[1]).tolist(),'limbs':{}}
    phases = {'FU':0.,'FL':.5,'RU':.75,'RL':.25}
    for ident,limb in rig.REST.items():
        q=(t+phases[ident])%1.
        dx,lift,state=rig.foot_trajectory(q,STEP,DUTY,-.15)
        joints=limb.copy()
        joints[0]=body.body_map(limb[0],shape)
        joints[2:,0]+=dx
        joints[2:,2]+=(.028 if ident[0]=='F' else .022)*lift
        if ident[0]=='R':
            joints[2:,1]+=(-1 if ident[1]=='U' else 1)*.035
        lengths=np.linalg.norm(np.diff(limb,axis=0),axis=1)
        joints[1]=rig.ik(joints[0],joints[2],body.body_map(limb[1],shape),*lengths[:2])
        result['limbs'][ident]={'phase':q,'state':state,'joints3D':joints.tolist(),'projected':rig.project(joints).tolist()}
    return result


def plan():
    poses=[walk(i/COUNT,i) for i in range(COUNT)]
    errors=[]
    contacts=[]
    for t in np.linspace(0,1,1000,endpoint=False):
        p=walk(float(t))
        contacts.append(sum(l['state']=='stance' for l in p['limbs'].values()))
        for ident,limb in p['limbs'].items():
            errors.extend(abs(np.linalg.norm(np.diff(limb['joints3D'],axis=0),axis=1)-np.linalg.norm(np.diff(rig.REST[ident],axis=0),axis=1)))
    report={'revision':'claw-visual-r1','count':COUNT,'durationsMs':[FRAME_MS]*COUNT,
            'stride':STEP,'duty':DUTY,'rearCyclesPerFrontCycle':1,
            'sourceReferenceSpeed':STEP/DUTY/(COUNT*FRAME_MS*.001),
            'minContacts':min(contacts),'maxBoneLengthError':float(max(errors)),
            'poses':poses,'artisticApproval':False}
    write(ROOT/'walk-plan.json',report)
    rig.contact_sheet([rig.bone_frame(p) for p in poses],ROOT/'01-walk-independent-bones.jpg')
    return poses


def render(indices):
    poses=plan()
    (ROOT/'walk').mkdir(exist_ok=True)
    surface=body.BodySurface(rig)
    for index in indices:
        start=time.perf_counter()
        image,stats=surface.render(poses[index])
        image.save(ROOT/'walk'/f'walk-{index:02d}.png')
        write(ROOT/'measurements'/f'{index:02d}.json',stats)
        print(json.dumps({'frame':index,'seconds':time.perf_counter()-start,**stats}),flush=True)


def atlas(name,paths,durations,loop,reference):
    ART.mkdir(parents=True,exist_ok=True)
    columns=6
    rows=math.ceil(len(paths)/columns)
    sheet=Image.new('RGBA',(CELL*columns,CELL*rows))
    for index,path in enumerate(paths):
        frame=Image.open(path).convert('RGBA').resize((CELL,CELL),Image.Resampling.LANCZOS)
        sheet.alpha_composite(frame,(index%columns*CELL,index//columns*CELL))
    destination=ART/(name+'.png')
    sheet.save(destination)
    return {'name':name,'asset':destination.relative_to(PROJECT).as_posix(),
            'columns':columns,'cell':CELL,'count':len(paths),'durationsMs':durations,
            'loop':loop,'referenceSpeed':reference,'pivotPixelsTopLeft':[301.1,255.9],
            'pixelsPerUnit':200,'sources':[p.relative_to(PROJECT).as_posix() for p in paths],
            'sourceHashes':[hashlib.sha256(p.read_bytes()).hexdigest() for p in paths]}


def pack():
    walk_plan=json.loads((ROOT/'walk-plan.json').read_text())
    run=ROOT.parent/'FastGroupEnemy21BodyMotion'
    attack=ROOT.parent/'FastGroupEnemy22BodyAttack'
    states=[atlas('Walk',sorted((ROOT/'walk').glob('walk-*.png')),[FRAME_MS]*COUNT,True,walk_plan['sourceReferenceSpeed']*270/400),
            atlas('Run',[run/'frames'/f'body-{i:02d}.png' for i in range(24)],[50]*24,True,.596590909),
            atlas('Sweep',[attack/'frames'/f'sweep-{i:02d}.png' for i in range(18)],
                  json.loads((attack/'new.json').read_text())['durationsMs'],False,0.),
            atlas('Hit',sorted((ROOT/'hit').glob('hit-*.png')),
                  json.loads((ROOT/'hit-plan.json').read_text())['durationsMs'],False,0.)]
    remains=[]
    for name in ['carcass-j','torn-j','heap-j']:
        source=ROOT/'remains'/(name+'.png')
        destination=ART/(name+'.png')
        original=Image.open(source).convert('RGBA')
        padded=Image.new('RGBA',(original.width+32,original.height+32))
        padded.paste(original,(16,16))
        padded.save(destination)
        remains.append({'name':name,'asset':destination.relative_to(PROJECT).as_posix(),
                        'pivotPixelsTopLeft':[776,681],'pixelsPerUnit':400/.72,'transparentPadding':16,
                        'source':source.relative_to(PROJECT).as_posix(),
                        'sourceSha256':hashlib.sha256(source.read_bytes()).hexdigest(),
                        'sha256':hashlib.sha256(destination.read_bytes()).hexdigest()})
    manifest={'task':'TASK-005','revision':'claw-visual-r1','role':'silent visual prefab, mechanics deliberately unassigned',
              'audio':False,'brain':False,'damageEvents':False,'states':states,'remains':remains,
              'selectedMaster':'../FastGroupEnemy19SpiderEyes/J-closed-mandibles.png',
              'approvedLocomotion':'../FastGroupEnemy21BodyMotion/acceptance.json'}
    write(ROOT/'asset-pack.json',manifest)
    print(json.dumps({'atlases':len(states),'sprites':sum(s['count'] for s in states),'remains':len(remains)}),flush=True)


if __name__=='__main__':
    ROOT.mkdir(exist_ok=True)
    if '--plan' in sys.argv:
        plan()
    elif '--pack' in sys.argv:
        pack()
    elif '--sample' in sys.argv:
        render([0,6,12,18])
    else:
        render(range(COUNT))
