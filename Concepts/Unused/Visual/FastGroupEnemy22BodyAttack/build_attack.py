"""Transfer the broad primary sweep onto the approved design and articulated body."""
from pathlib import Path
import hashlib
import json
import math
import sys
import time
import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parent
sys.path.insert(0,str(ROOT.parent/'FastGroupEnemy21BodyMotion'))
import build_body as body
sys.path.insert(0,str(ROOT.parent/'FastGroupEnemy17SimplifiedMotion/alternatives/heavy'))
import build_wide_attacks as broad

rig = body.rig
rig.ROOT = ROOT
TIMES = broad.CONFIG['sweep']['times']
DURATIONS = broad.CONFIG['sweep']['durationsMs']


def write_json(name,value):
    (ROOT/name).write_text(json.dumps(value,ensure_ascii=False,indent=2),encoding='utf-8',newline='\n')


def trunk(t,motion):
    wind,drive,_ = broad.runtime.action(t,0.,.4,.5,1.)
    yaw = np.radians([-.9*wind+.9*drive,-4.5*wind+4.5*drive,-2.*wind+2.*drive,.4*wind-.4*drive])
    pitch = np.radians([.6*wind-.6*drive,2.*wind-2.*drive,1.*wind-1.*drive,.2*wind-.2*drive])
    roll = np.radians([0.,-3.*wind+2.5*drive,-.6*wind+.6*drive,.3*wind-.3*drive])
    rest = body.REST_SPINE
    nodes = rest.copy()
    nodes[3] += motion
    nodes[2] = nodes[3]+body.rotation(yaw[3],pitch[3])@(rest[2]-rest[3])
    nodes[1] = nodes[2]+body.rotation(yaw[2],pitch[2])@(rest[1]-rest[2])
    nodes[0] = nodes[1]+body.rotation(yaw[1],pitch[1])@(rest[0]-rest[1])
    return {'spine3D':nodes.tolist(),'yaw':yaw.tolist(),'pitch':pitch.tolist(),'roll':roll.tolist()}


def pose(t,index=None):
    result = broad.pose('sweep',t,index)
    shape = trunk(t,np.asarray(result['rootOffset3D']))
    result['body'] = shape
    result['spine'] = rig.project(np.asarray(shape['spine3D'])).tolist()
    result['rootOffset3D'] = (np.asarray(shape['spine3D'][1])-body.REST_SPINE[1]).tolist()
    for ident,rest in rig.REST.items():
        joints = np.asarray(result['limbs'][ident]['joints3D'])
        joints[0] = body.body_map(rest[0],shape)
        hint = body.body_map(rest[1],shape)
        lengths = np.linalg.norm(np.diff(rest,axis=0),axis=1)
        joints[1] = rig.ik(joints[0],joints[2],hint,*lengths[:2])
        result['limbs'][ident]['joints3D'] = joints.tolist()
        result['limbs'][ident]['projected'] = rig.project(joints).tolist()
    return result


class AttackSurface(body.BodySurface):
    def constraints(self,pose):
        target = super().constraints(pose)
        for ident in ['FU','FL']:
            angle = pose['limbs'][ident]['handAngleRad']
            selected = (self.owners==ident)&(self.parts=='hand')
            rest = rig.project(rig.REST[ident][2])
            posed = np.asarray(pose['limbs'][ident]['projected'][2])
            target[selected] = (self.source[selected]-rest)@broad.rotation(angle).T+posed
        return target

    def render(self,pose):
        image,stats = self.raster(self.deform(pose))
        for ident,piece in self.feet.items():
            rest = rig.project(rig.REST[ident][2])
            posed = np.asarray(pose['limbs'][ident]['projected'][2])
            matrix = broad.rotation(pose['limbs'][ident].get('handAngleRad',0.)).T
            affine = matrix/rig.SCALE
            offset = rest-matrix@posed-affine@rig.OFFSET
            coefficients = (*affine[0],offset[0],*affine[1],offset[1])
            image.alpha_composite(piece.transform((1024,1024),Image.Transform.AFFINE,coefficients,Image.Resampling.BICUBIC))
        return image,stats


def plan():
    for name in ['frames','measurements','review']:
        (ROOT/name).mkdir(parents=True,exist_ok=True)
    sequence = [pose(t,i) for i,t in enumerate(TIMES)]
    write_json('motion-plan.json',{
        'task':'TASK-005','revision':'body-sweep-r1','state':'sweep',
        'master':'../FastGroupEnemy19SpiderEyes/J-closed-mandibles.png',
        'masterSha256':hashlib.sha256(body.heavy.MASTER.read_bytes()).hexdigest(),
        'locomotionReference':'../FastGroupEnemy21BodyMotion/acceptance.json',
        'anatomy':'../FastGroupEnemy13AssetInspired/anatomy-plan-v4.json',
        'canvas':[1024,1024],'sourceScale':rig.SCALE,'sourceOffset':rig.OFFSET.tolist(),
        'pivotPixelsTopLeft':rig.screen([760,665]).tolist(),
        'frameCount':len(sequence),'durationsMs':DURATIONS,'durationMs':sum(DURATIONS),
        'loop':False,'visualStrikeFrames':[[8,9]],'frames':sequence,
        'artisticApproval':False,'unityImport':False
    })
    rig.contact_sheet([rig.bone_frame(p) for p in sequence],ROOT/'01-independent-bones.jpg')
    return sequence


def render(indices):
    sequence = plan()
    surface = AttackSurface(rig)
    for index in indices:
        start = time.perf_counter()
        image,stats = surface.render(sequence[index])
        image.save(ROOT/'frames'/f'sweep-{index:02d}.png')
        rig.bone_frame(sequence[index],image).save(ROOT/'review'/f'bones-{index:02d}.png')
        stats.update(index=index,renderSeconds=time.perf_counter()-start)
        write_json(f'measurements/{index:02d}.json',stats)
        print(json.dumps(stats),flush=True)


if __name__=='__main__':
    if '--plan' in sys.argv:
        plan()
    else:
        render([0,7,9,17] if '--sample' in sys.argv else range(len(TIMES)))
