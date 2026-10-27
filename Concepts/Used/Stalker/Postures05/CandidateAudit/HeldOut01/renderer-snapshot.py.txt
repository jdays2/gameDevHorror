"""Study-only whole-native-limb inverse UV painter. No import/write side effects.

This module is a held-out candidate, not the production painter. Source arrays
and returned torso are linear premultiplied float32. Signed RGB-delta layers
with alpha0 are accepted by warp_rgba without clamping or color conversion.
"""
from pathlib import Path
import json
import sys
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

FAMILY = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(FAMILY))
from sprite_painter import ORIGIN, FLIP, PPU, SIZE, JOINT_RADII, ROOT_RADII
from sprite_painter import _decode, _over, _clean_source_root

def inverse_locked_shaft(points,source_joint,source_end,target_joint,target_end,locked_radius):
    source_axis=source_end-source_joint
    source_length=np.linalg.norm(source_axis)
    source_axis/=source_length
    target_axis=target_end-target_joint
    target_length=np.linalg.norm(target_axis)
    target_axis/=target_length
    source_normal=np.array([-source_axis[1],source_axis[0]])
    target_normal=np.array([-target_axis[1],target_axis[0]])
    if min(source_length,target_length)<=locked_radius+1:
        raise ValueError('The native joint zone does not fit this projected shaft')
    relative=points-target_joint
    axial=relative@target_axis
    transverse=relative@target_normal
    source_axial=np.where(axial<=locked_radius,axial,
        locked_radius+(axial-locked_radius)*(source_length-locked_radius)/(target_length-locked_radius))
    return source_joint+source_axial[...,None]*source_axis+transverse[...,None]*source_normal


def segment_distance(points,start,end):
    delta=end-start
    phase=np.clip(((points-start)*delta).sum(axis=-1)/np.dot(delta,delta),0,1)
    return np.linalg.norm(points-(start+phase[...,None]*delta),axis=-1)


def bilinear(source,uv):
    # Arithmetic noise at an exact source pose must not filter native pixels.
    uv=np.where(np.abs(uv-np.rint(uv))<1e-4,np.rint(uv),uv)
    x,y=uv[...,0],uv[...,1]
    x0,y0=np.floor(x).astype(int),np.floor(y).astype(int)
    fx,fy=x-x0,y-y0
    result=np.zeros((*x.shape,4),dtype='float32')
    for dx,dy,weight in [(0,0,(1-fx)*(1-fy)),(1,0,fx*(1-fy)),
                         (0,1,(1-fx)*fy),(1,1,fx*fy)]:
        xx,yy=x0+dx,y0+dy
        valid=(xx>=0)&(yy>=0)&(xx<source.shape[1])&(yy<source.shape[0])
        result[valid]+=source[yy[valid],xx[valid]]*weight[valid,None]
    return result


def uv_field(limb,pose,leg,inner=None,outer=None,points=None):
    inner=JOINT_RADII[leg] if inner is None else inner
    outer=inner+8 if outer is None else outer
    if points is None:
        yy,xx=np.mgrid[:SIZE,:SIZE]
        pixels=np.stack([xx,yy],axis=-1).astype('float64')
    else:
        pixels=np.asarray(points,dtype='float64')
    source_hip,source_knee,source_toe=[np.asarray(limb[key],dtype='float64') for key in ('hip','knee','tip')]
    target_hip,target_knee,target_toe=[ORIGIN+FLIP@np.asarray(pose[key][leg][:2])*PPU for key in ('hips','knees','feet')]
    upper_uv=inverse_locked_shaft(pixels,source_knee,source_hip,target_knee,target_hip,outer)
    lower_uv=inverse_locked_shaft(pixels,source_knee,source_toe,target_knee,target_toe,outer)
    relative=pixels-target_knee
    radius=np.linalg.norm(relative,axis=-1)
    target_upper_axis=(target_hip-target_knee)/np.linalg.norm(target_hip-target_knee)
    target_lower_axis=(target_toe-target_knee)/np.linalg.norm(target_toe-target_knee)
    target_upper_owner=(relative@target_upper_axis)>=(relative@target_lower_axis)
    outside_uv=np.where(target_upper_owner[...,None],upper_uv,lower_uv)
    target_upper_angle=np.arctan2(*(target_hip-target_knee)[::-1])
    target_lower_angle=np.arctan2(*(target_toe-target_knee)[::-1])
    source_upper_angle=np.arctan2(*(source_hip-source_knee)[::-1])
    source_lower_angle=np.arctan2(*(source_toe-source_knee)[::-1])
    target_arc=(target_lower_angle-target_upper_angle)%(2*np.pi)
    source_arc=(source_lower_angle-source_upper_angle)%(2*np.pi)
    angle=(np.arctan2(relative[...,1],relative[...,0])-target_upper_angle)%(2*np.pi)
    mapped_angle=np.where(angle<=target_arc,angle*source_arc/target_arc,
        source_arc+(angle-target_arc)*(2*np.pi-source_arc)/(2*np.pi-target_arc))+source_upper_angle
    upper_radial_scale=(np.linalg.norm(source_hip-source_knee)-outer)/(np.linalg.norm(target_hip-target_knee)-outer)
    lower_radial_scale=(np.linalg.norm(source_toe-source_knee)-outer)/(np.linalg.norm(target_toe-target_knee)-outer)
    radial_scale=np.where(target_upper_owner,upper_radial_scale,lower_radial_scale)
    mapped_radius=np.where(radius<=outer,radius,outer+(radius-outer)*radial_scale)
    polar_uv=source_knee+mapped_radius[...,None]*np.stack([np.cos(mapped_angle),np.sin(mapped_angle)],axis=-1)
    t=np.clip((radius-inner)/(outer-inner),0,1)
    t=t*t*(3-2*t)
    # Far outside both bone rays an affine extension can sample the opposite
    # source branch and create a duplicate skin needle. Keep the polar chart
    # in that empty exterior sector; interpolate UVs only, never alpha/RGBA.
    upper_axial=np.maximum(relative@target_upper_axis,0)
    lower_axial=np.maximum(relative@target_lower_axis,0)
    upper_ray_distance=np.linalg.norm(relative-upper_axial[...,None]*target_upper_axis,axis=-1)
    lower_ray_distance=np.linalg.norm(relative-lower_axial[...,None]*target_lower_axis,axis=-1)
    distance=np.minimum(upper_ray_distance,lower_ray_distance)
    lateral=np.clip((distance-14)/8,0,1)
    lateral=1-lateral*lateral*(3-2*lateral)
    weight=t*lateral
    uv=polar_uv*(1-weight[...,None])+outside_uv*weight[...,None]
    return uv



def warp_rgba(source, limb, pose, leg, inner=None, outer=None, points=None):
    """Sample one full native RGBA/delta exactly once using this candidate UV."""
    return bilinear(source, uv_field(limb, pose, leg, inner, outer, points))


class SpritePainter:
    """API-compatible study painter with read-only original and cleaned sources."""
    def __init__(self, project_path):
        self.project_path = Path(project_path).resolve()
        source_folder = self.project_path / 'Concepts/Used/Stalker/Locomotion02'
        self.anatomy = json.loads((source_folder/'anatomy.json').read_text())
        path = self.project_path/'Assets/EmergencyShift/Art/Enemies/Stalker/Locomotion02/rig-atlas.png'
        with Image.open(path) as image:
            atlas = image.convert('RGBA')
        def layer(slot):
            x, y = slot % 4 * 358, slot // 4 * 406
            return _decode(atlas.crop((x, y, x+358, y+406)))
        raw_sources, sources, trace = [], [], []
        for leg, limb in enumerate(self.anatomy['limbs']):
            raw = layer(leg*2+1)
            source, cleanup = _clean_source_root(raw, np.asarray(limb['hip']), ROOT_RADII[leg])
            raw.flags.writeable = False
            source.flags.writeable = False
            raw_sources.append(raw)
            sources.append(source)
            trace.append({'leg': leg, 'id': limb['id'], **cleanup})
        self.raw_sources = tuple(raw_sources)
        self.sources = tuple(sources)
        self.source_cleanup = tuple(trace)
        # The held-out comparison intentionally retains the one-control torso.
        # No extra cap or target-space alpha surgery is introduced here.
        mask = Image.new('L', (358, 406))
        ImageDraw.Draw(mask).polygon(((187,124),(120,247),(250,247)), fill=255)
        mask = mask.filter(ImageFilter.MaxFilter(9)).filter(ImageFilter.GaussianBlur(.65))
        core = layer(0) * (np.asarray(mask,dtype='float32')/255)[...,None]
        self._torso = np.zeros((SIZE,SIZE,4),dtype='float32')
        x, y = (ORIGIN-np.asarray(self.anatomy['pivot'])).astype(int)
        self._torso[y:y+406,x:x+358] = core
        self._torso.flags.writeable = False
        yy, xx = np.mgrid[:SIZE,:SIZE]
        self._grid = np.stack([xx,yy],axis=-1).astype('float64')
        self._grid.flags.writeable = False

    def paint(self, pose):
        limbs = []
        whole = np.zeros((SIZE,SIZE,4),dtype='float32')
        for leg, limb in enumerate(self.anatomy['limbs']):
            skin = warp_rgba(self.sources[leg], limb, pose, leg, points=self._grid)
            limbs.append(skin)
            whole = _over(skin, whole)
        return _over(self._torso, whole), limbs, self._torso
