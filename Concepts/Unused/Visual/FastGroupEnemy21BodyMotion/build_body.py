"""Articulated trunk over the R2 contact gait, from the approved single painting."""
from pathlib import Path
import hashlib
import json
import math
import sys
import time
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

ROOT = Path(__file__).resolve().parent
sys.path.insert(0, str(ROOT.parent/'FastGroupEnemy20HeavyLocomotion'))
import build_heavy as heavy

rig = heavy.base
rig.ROOT = ROOT
REST_SPINE = np.asarray(rig.PLAN['spine'], dtype=float)
COUNT = heavy.COUNT


def rotation(yaw=0., pitch=0., roll=0.):
    cy, sy = math.cos(yaw), math.sin(yaw)
    cp, sp = math.cos(pitch), math.sin(pitch)
    cr, sr = math.cos(roll), math.sin(roll)
    rz = np.array([[cy,-sy,0.],[sy,cy,0.],[0.,0.,1.]])
    ry = np.array([[cp,0.,sp],[0.,1.,0.],[-sp,0.,cp]])
    rx = np.array([[1.,0.,0.],[0.,cr,-sr],[0.,sr,cr]])
    return rz@ry@rx


def trunk(t):
    load = math.sin(2*math.pi*(t-.08))
    rear_load = math.sin(2*math.pi*(t-.18))
    compress = math.sin(4*math.pi*t+.4)
    angles = np.radians([1.1*math.sin(2*math.pi*(t-.16)), 4.*load, -2.8*rear_load, .6*rear_load])
    pitches = np.radians([1.2*compress, 1.2*compress, -2.*compress, .4*compress])
    rolls = np.radians([0., 4.5*load, 1.*rear_load, -.8*rear_load])
    nodes = REST_SPINE.copy()
    # Build the fixed-length chain from the supporting pelvis toward the chest.
    # Most of the articulation belongs to the waist/shoulders, not a hip sway.
    nodes[3] += np.array([.018*math.cos(4*math.pi*t), 0., -.16+.006*math.cos(4*math.pi*t)])
    nodes[2] = nodes[3]+rotation(angles[3],pitches[3])@(REST_SPINE[2]-REST_SPINE[3])
    nodes[1] = nodes[2]+rotation(angles[2],pitches[2])@(REST_SPINE[1]-REST_SPINE[2])
    nodes[0] = nodes[1]+rotation(angles[1],pitches[1])@(REST_SPINE[0]-REST_SPINE[1])
    return {'spine3D':nodes.tolist(), 'yaw':angles.tolist(), 'pitch':pitches.tolist(), 'roll':rolls.tolist()}


def body_map(points, body):
    points = np.asarray(points, dtype=float)
    original_shape = points.shape
    points = points.reshape(-1,3)
    nodes = np.asarray(body['spine3D'])
    output = np.empty_like(points)
    for index, point in enumerate(points):
        x = point[0]
        if x <= REST_SPINE[0,0]:
            matrix = rotation(body['yaw'][0],body['pitch'][0])
            output[index] = nodes[0]+matrix@(point-REST_SPINE[0])
        elif x >= REST_SPINE[-1,0]:
            matrix = rotation(body['yaw'][-1],body['pitch'][-1],body['roll'][-1])
            output[index] = nodes[-1]+matrix@(point-REST_SPINE[-1])
        else:
            center = np.array([np.interp(x,REST_SPINE[:,0],nodes[:,axis]) for axis in range(3)])
            rest_center = np.array([x,0.,np.interp(x,REST_SPINE[:,0],REST_SPINE[:,2])])
            yaw = np.interp(x,REST_SPINE[:,0],body['yaw'])
            pitch = np.interp(x,REST_SPINE[:,0],body['pitch'])
            roll = np.interp(x,REST_SPINE[:,0],body['roll'])
            output[index] = center+rotation(yaw,pitch,roll)@(point-rest_center)
    return output.reshape(original_shape)


def pose(t,index=None):
    result = heavy.pose(t,index)
    body = trunk(t)
    result['body'] = body
    result['spine'] = rig.project(np.asarray(body['spine3D'])).tolist()
    result['rootOffset3D'] = (np.asarray(body['spine3D'][1])-REST_SPINE[1]).tolist()
    for ident,rest in rig.REST.items():
        joints = np.asarray(result['limbs'][ident]['joints3D'])
        joints[0] = body_map(rest[0],body)
        hint = body_map(rest[1],body)
        lengths = np.linalg.norm(np.diff(rest,axis=0),axis=1)
        joints[1] = rig.ik(joints[0],joints[2],hint,*lengths[:2])
        result['limbs'][ident]['joints3D'] = joints.tolist()
        result['limbs'][ident]['projected'] = rig.project(joints).tolist()
    return result


class BodySurface(heavy.SkinSurface):
    def classify(self):
        owners,parts = super().classify()
        for ident in ['FU','FL']:
            shoulder = self.rig.project(self.rig.REST[ident][0])
            distance = np.linalg.norm(self.source-shoulder,axis=1)
            # The shoulder skin can slide between the moving chest and arm.
            # Keep the attachment centre pinned; do not glue its whole mass.
            mobile = (parts=='body')&(self.source[:,0]>645)&(distance>22)&(distance<105)
            parts[mobile] = 'free'
        return owners,parts

    def __init__(self,base):
        super().__init__(base)
        xy = (self.source-np.array([760.,665.]))/375.
        center_z = np.interp(xy[:,0],REST_SPINE[:,0],REST_SPINE[:,2])
        # A prospective dorsal cross-section gives roll a small visible volume.
        # It is fixed across the clip, not inferred hidden anatomy or relighting.
        dome = .10*np.maximum(0.,1.-(xy[:,1]/.34)**2)
        self.body_points = np.column_stack([xy,center_z+dome])
        mask = self.body_image.getchannel('A')
        self.interior = np.zeros((1254,1254),dtype=float)
        for _ in range(16):
            mask = mask.filter(ImageFilter.MinFilter(3))
            self.interior += np.asarray(mask)>245

    def constraints(self,pose):
        target = super().constraints(pose)
        selected = self.parts=='body'
        target[selected] = self.rig.project(body_map(self.body_points[selected],pose['body']))
        return target

    def prevent_folds(self,q):
        triangles = self.visible_triangles
        for _ in range(2400):
            t = q[triangles]
            u,v = t[:,1]-t[:,0],t[:,2]-t[:,0]
            area = u[:,0]*v[:,1]-u[:,1]*v[:,0]
            bad = np.flatnonzero(area<self.step*self.step*.12)
            if len(bad)==0:
                break
            for k in bad:
                indices = triangles[k]
                a,b,c = q[indices]
                actual = (b[0]-a[0])*(c[1]-a[1])-(b[1]-a[1])*(c[0]-a[0])
                gradient = np.array([[b[1]-c[1],c[0]-b[0]],[c[1]-a[1],a[0]-c[0]],[a[1]-b[1],b[0]-a[0]]])
                gradient *= self.is_free[indices,None]
                denominator = np.sum(gradient*gradient)
                if denominator>1e-10:
                    move = gradient*(self.step*self.step*.125-actual)/denominator
                    q[indices] += np.clip(move,-3.,3.)
        return q

    def raster(self,q):
        r = self.rig
        dst = r.screen(q)
        uv = np.full((r.W,r.W,2),-1.,dtype=np.float32)
        priority = np.full((r.W,r.W),-1.,dtype=np.float32)
        minimum,minimum_skin = 10.,10.
        flipped,flipped_skin = 0,0
        overlaps = 0
        for indices in self.triangles:
            source = self.source[indices]
            a,b,c = dst[indices]
            determinant = (b[0]-a[0])*(c[1]-a[1])-(b[1]-a[1])*(c[0]-a[0])
            ratio = determinant/(r.SCALE*r.SCALE*self.step*self.step)
            minimum = min(minimum,ratio)
            center = np.clip(np.rint(source.mean(axis=0)).astype(int),0,1253)
            solid = self.art[center[1],center[0],3]>.5
            if solid:
                minimum_skin = min(minimum_skin,ratio)
            if ratio<=0:
                flipped += 1
                flipped_skin += int(solid)
            if abs(determinant)<1e-5:
                continue
            lo = np.maximum(0,np.floor(dst[indices].min(axis=0)).astype(int))
            hi = np.minimum(r.W-1,np.ceil(dst[indices].max(axis=0)).astype(int))
            if np.any(hi<lo):
                continue
            yy,xx = np.mgrid[lo[1]:hi[1]+1,lo[0]:hi[0]+1].astype(np.float32)
            u = ((xx-a[0])*(c[1]-a[1])-(yy-a[1])*(c[0]-a[0]))/determinant
            v = ((b[0]-a[0])*(yy-a[1])-(b[1]-a[1])*(xx-a[0]))/determinant
            inside = (u>=-.0001)&(v>=-.0001)&(u+v<=1.0001)
            coords = source[0]+u[...,None]*(source[1]-source[0])+v[...,None]*(source[2]-source[0])
            ax = np.clip(np.floor(coords[...,0]).astype(int),0,1252)
            ay = np.clip(np.floor(coords[...,1]).astype(int),0,1252)
            fx = np.clip(coords[...,0]-ax,0.,1.)
            fy = np.clip(coords[...,1]-ay,0.,1.)
            alpha = (self.art[ay,ax,3]*(1-fx)+self.art[ay,ax+1,3]*fx)*(1-fy)+(self.art[ay+1,ax,3]*(1-fx)+self.art[ay+1,ax+1,3]*fx)*fy
            depth = (self.interior[ay,ax]*(1-fx)+self.interior[ay,ax+1]*fx)*(1-fy)+(self.interior[ay+1,ax]*(1-fx)+self.interior[ay+1,ax+1]*fx)*fy
            # At a projected joint overlap, intact interior covers the painted
            # silhouette edge. Equal opaque alpha alone produced speckled ties.
            rank = np.minimum(alpha/.97,1.)*1000+depth
            old = priority[lo[1]:hi[1]+1,lo[0]:hi[0]+1]
            overlaps += int(np.count_nonzero(inside&(alpha>.5)&(old>500)))
            inside &= (alpha>.001)&(rank>=old)
            uv[lo[1]:hi[1]+1,lo[0]:hi[0]+1][inside] = coords[inside]
            old[inside] = rank[inside]
        x,y = uv[...,0],uv[...,1]
        ix = np.clip(np.floor(x).astype(int),0,1252)
        iy = np.clip(np.floor(y).astype(int),0,1252)
        fx = np.clip(x-ix,0.,1.)[...,None]
        fy = np.clip(y-iy,0.,1.)[...,None]
        color = (self.art[iy,ix]*(1-fx)+self.art[iy,ix+1]*fx)*(1-fy)+(self.art[iy+1,ix]*(1-fx)+self.art[iy+1,ix+1]*fx)*fy
        color *= (x>=0)[...,None]
        color[...,:3] /= np.maximum(color[...,3:4],1e-6)
        stats = {'minTriangleAreaRatio':float(minimum),'flippedTriangles':flipped,'minSolidTriangleAreaRatio':float(minimum_skin),'flippedSolidTriangles':flipped_skin,'overlapSamples':overlaps}
        return Image.fromarray(np.uint8(np.clip(color,0,1)*255)),stats


def write_json(name,data):
    (ROOT/name).write_text(json.dumps(data,indent=2),encoding='utf-8',newline='\n')


def plan():
    ROOT.mkdir(exist_ok=True)
    (ROOT/'frames').mkdir(exist_ok=True)
    (ROOT/'measurements').mkdir(exist_ok=True)
    (ROOT/'review').mkdir(exist_ok=True)
    poses = [pose(i/COUNT,i) for i in range(COUNT)]
    write_json('motion-plan.json',{
        'task':'TASK-005','revision':'body-motion-r2','count':COUNT,
        'master':'../FastGroupEnemy19SpiderEyes/J-closed-mandibles.png',
        'masterSha256':hashlib.sha256(heavy.MASTER.read_bytes()).hexdigest(),
        'durationsMs':heavy.DURATIONS,'canvas':[1024,1024],
        'sourceScale':rig.SCALE,'sourceOffset':rig.OFFSET.tolist(),
        'pivotPixelsTopLeft':rig.screen([760,665]).tolist(),
        'bodyParameters':{'chestYawDegrees':4.,'waistYawDegrees':2.8,'pelvisYawDegrees':.6,'chestRollDegrees':4.5,'pelvisRollDegrees':.8,'maximumHeadYawDegrees':1.1,'chainRoot':'supporting pelvis'},
        'frames':poses,'artisticApproval':False,'unityImport':False
    })
    rig.contact_sheet([rig.bone_frame(p) for p in poses],ROOT/'01-independent-bones.jpg')
    projections = Image.new('RGB',(1200,720),'#262b31')
    draw = ImageDraw.Draw(projections)
    for col,index in enumerate([0,6,12,18]):
        p = poses[index]
        for row,axes in enumerate([(0,1),(0,2),(1,2)]):
            def project(point):
                return (col*300+170+point[axes[0]]*90,row*240+150+point[axes[1]]*90*(-1 if axes[1]==2 else 1))
            draw.text((col*300+8,row*240+8),f'{index:02d} / {[("top"),("side"),("front")][row]}',fill='#dce4e9',font=rig.SMALL)
            for limb in p['limbs'].values():
                draw.line([project(v) for v in limb['joints3D']],fill='#7fe1be',width=2)
            draw.line([project(v) for v in p['body']['spine3D']],fill='#ffc673',width=3)
    projections.save(ROOT/'01-skeleton-projections.png')
    return poses


def render(indices):
    poses = plan()
    surface = BodySurface(rig)
    for index in indices:
        start = time.perf_counter()
        image,stats = surface.render(poses[index])
        image.save(ROOT/'frames'/f'body-{index:02d}.png')
        rig.bone_frame(poses[index],image).save(ROOT/'review'/f'bones-{index:02d}.png')
        stats.update(index=index,renderSeconds=time.perf_counter()-start)
        write_json(f'measurements/{index:02d}.json',stats)
        print(json.dumps(stats),flush=True)


if __name__=='__main__':
    if '--plan' in sys.argv:
        plan()
    else:
        if '--index' in sys.argv:
            indices = [int(sys.argv[sys.argv.index('--index')+1])]
        elif '--sample' in sys.argv:
            indices = [0,6,12,18]
        else:
            indices = range(COUNT)
        render(indices)
