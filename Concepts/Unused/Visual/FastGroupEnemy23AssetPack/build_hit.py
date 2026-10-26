"""Brief braced hit reaction; same source, anatomy and articulated surface."""
import sys
import math
import json
import numpy as np
from PIL import Image
import build_pack as pack

ROOT=pack.ROOT
body=pack.body
rig=pack.rig
PULSES=[0.,.6,1.,.65,.2,0.]
DURATIONS=[20,25,30,35,30,30]


def pose(amount,index):
    rest=body.REST_SPINE
    shape={'yaw':np.radians([.4,-2.,1.,.2])*amount,
           'pitch':np.radians([1.5,1.,-.5,0.])*amount,
           'roll':np.radians([0.,1.,-.3,0.])*amount}
    nodes=rest.copy()
    nodes[3]+=np.array([.012*amount,0.,-.004*amount])
    for node in [2,1,0]:
        owner=node+1
        nodes[node]=nodes[owner]+body.rotation(shape['yaw'][owner],shape['pitch'][owner])@(rest[node]-rest[owner])
    shape={key:value.tolist() for key,value in shape.items()}
    shape['spine3D']=nodes.tolist()
    result={'index':index,'body':shape,'spine':rig.project(nodes).tolist(),
            'rootOffset3D':(nodes[1]-rest[1]).tolist(),'limbs':{}}
    for ident,chain in rig.REST.items():
        joints=chain.copy()
        joints[0]=body.body_map(chain[0],shape)
        lengths=np.linalg.norm(np.diff(chain,axis=0),axis=1)
        joints[1]=rig.ik(joints[0],joints[2],body.body_map(chain[1],shape),*lengths[:2])
        result['limbs'][ident]={'state':'stance','joints3D':joints.tolist(),'projected':rig.project(joints).tolist()}
    return result


def plan():
    poses=[pose(amount,index) for index,amount in enumerate(PULSES)]
    pack.write(ROOT/'hit-plan.json',{'poses':poses,'durationsMs':DURATIONS,'loop':False,'damageEvent':False})
    rig.contact_sheet([rig.bone_frame(p) for p in poses],ROOT/'01-hit-independent-bones.jpg')
    return poses


def render():
    poses=plan()
    (ROOT/'hit').mkdir(exist_ok=True)
    surface=body.BodySurface(rig)
    images=[]
    for index,p in enumerate(poses):
        image,stats=surface.render(p)
        image.save(ROOT/'hit'/f'hit-{index:02d}.png')
        images.append(rig.bone_frame(p,image))
        pack.write(ROOT/'measurements/hit'/f'{index:02d}.json',stats)
        assert stats['flippedTriangles']==0
        print(json.dumps({'frame':index,**stats}),flush=True)
    rig.contact_sheet(images,ROOT/'review/hit-bones.jpg')


if __name__=='__main__':
    if '--plan' in sys.argv:
        plan()
    else:
        render()
