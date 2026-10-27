"""One-pose inverse-UV study; no cap overlays, no production or mass writes."""
from pathlib import Path
import json
import sys
import numpy as np
from PIL import Image, ImageDraw

HERE=Path(__file__).resolve().parent
FAMILY=HERE.parent
PROJECT=FAMILY.parents[3]
sys.path.insert(0,str(FAMILY))
from sprite_painter import SpritePainter, ORIGIN, FLIP, PPU, SIZE, _decode, _over, _clean_source_root, ROOT_RADII, JOINT_RADII
from compose_parts import encode
from qa_parts import FONT, bounded_holes, components


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


def warp(source,limb,pose,leg,inner=None,outer=None,debug=False,points=None):
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
    paint=bilinear(source,uv)
    return (paint,uv) if debug else paint


def crop(image,center,size=64):
    x,y=np.rint(center).astype(int)
    return image.crop((x-size//2,y-size//2,x+size//2,y+size//2))


def main():
    out=HERE/'SingleUVStudy'
    out.mkdir(exist_ok=True)
    painter=SpritePainter(PROJECT)
    anatomy=painter.anatomy
    atlas=Image.open(PROJECT/'Assets/EmergencyShift/Art/Enemies/Stalker/Locomotion02/rig-atlas.png').convert('RGBA')
    originals=[]
    for leg in range(6):
        slot=leg*2+1
        originals.append(_decode(atlas.crop((slot%4*358,slot//4*406,slot%4*358+358,slot//4*406+406))))
    identity={'hips':[],'knees':[],'feet':[]}
    for limb in anatomy['limbs']:
        for key,point in [('hips','hip'),('knees','knee'),('feet','tip')]:
            xy=FLIP@(np.asarray(limb[point])-np.asarray(anatomy['pivot']))/PPU
            identity[key].append([float(xy[0]),float(xy[1]),0.0])
    offset=(ORIGIN-np.asarray(anatomy['pivot'])).astype(int)
    identity_records=[]
    for leg,limb in enumerate(anatomy['limbs']):
        reconstructed=warp(originals[leg],limb,identity,leg)
        expected=np.zeros((SIZE,SIZE,4),dtype='float32')
        expected[offset[1]:offset[1]+406,offset[0]:offset[0]+358]=originals[leg]
        before,after=np.asarray(encode(expected)),np.asarray(encode(reconstructed))
        identity_records.append({'id':limb['id'],'changedRGBABytes':int(np.count_nonzero(before!=after)),
            'maxByteError':int(np.abs(before.astype(int)-after.astype(int)).max())})
    pose=next(pose for pose in json.loads((FAMILY/'poses.json').read_text()) if pose['row']==7 and pose['phase']==0)
    old_whole,old_limbs,torso=painter.paint(pose)
    limbs=[]
    whole=np.zeros((SIZE,SIZE,4),dtype='float32')
    actual_records=[]
    for leg,limb in enumerate(anatomy['limbs']):
        clean_source,source_trace=_clean_source_root(originals[leg],np.asarray(limb['hip']),ROOT_RADII[leg])
        painted,uv=warp(clean_source,limb,pose,leg,debug=True)
        limbs.append(painted)
        whole=_over(painted,whole)
        alpha=np.rint(painted[...,3]*255).astype('uint8')
        holes4=bounded_holes(alpha>4)
        holes128=bounded_holes(alpha>128)
        detail=[]
        for hole in holes128:
            x0,y0,x1,y1=hole['bbox']
            for y,x in zip(*np.nonzero(alpha[y0:y1,x0:x1]<=128)):
                y,x=y+y0,x+x0
                sx,sy=uv[y,x]
                detail.append({'xy':[int(x),int(y)],'sourceUV':[float(sx),float(sy)],
                    'sourceAlpha4':clean_source[int(np.floor(sy)):int(np.floor(sy))+2,int(np.floor(sx)):int(np.floor(sx))+2,3].tolist()})
        dux,duy=np.gradient(uv[...,0],axis=1),np.gradient(uv[...,0],axis=0)
        dvx,dvy=np.gradient(uv[...,1],axis=1),np.gradient(uv[...,1],axis=0)
        determinant=dux*dvy-duy*dvx
        fy,fx=np.nonzero((determinant<0)&(alpha>230))
        fine_negative=[]
        for y,x in zip(fy,fx):
            e=.001
            points=[[x+e,y],[x-e,y],[x,y+e],[x,y-e]]
            _,small_uv=warp(clean_source,limb,pose,leg,debug=True,points=points)
            du=(small_uv[0]-small_uv[1])/(2*e)
            dv=(small_uv[2]-small_uv[3])/(2*e)
            jac=du[0]*dv[1]-du[1]*dv[0]
            if jac<0:
                fine_negative.append({'xy':[int(x),int(y)],'jacobian':float(jac)})
        actual_records.append({'id':limb['id'],
            'sourceCleanup':source_trace,
            'boundedHolePixelsOver4':sum(hole['pixels'] for hole in holes4),
            'boundedHolePixelsOver128':sum(hole['pixels'] for hole in holes128),
            'opaqueUvFoldoverPixelsFineEpsilon':len(fine_negative),
            'fineNegativeJacobian':fine_negative,
            'holes128':detail})
        encode(painted).save(out/f'leg-{limb["id"]}.png')
    whole=_over(torso,whole)
    pair_checks=[]
    for a in range(6):
        for b in range(a+1,6):
            overlap=(np.rint(limbs[a][...,3]*255)>128)&(np.rint(limbs[b][...,3]*255)>128)&(np.rint(torso[...,3]*255)<=128)
            if overlap.any():
                yy,xx=np.nonzero(overlap)
                pair_checks.append({'pair':[a,b],'pixels':int(overlap.sum()),
                    'bbox':[int(xx.min()),int(yy.min()),int(xx.max()+1),int(yy.max()+1)]})
    whole_alpha=np.rint(whole[...,3]*255).astype('uint8')
    whole_components=components(whole_alpha>4)
    encode(whole).save(out/'single-control-raw.png')
    encode(whole,green=True).save(out/'single-control-green.png')
    native=Image.new('RGB',(4*200,2*160+70),'#00ff00')
    draw=ImageDraw.Draw(native)
    titles=['native source','old parts-over','single UV','single UV alpha']
    for col,title in enumerate(titles):
        draw.text((col*200+8,10),title,fill='black',font=FONT)
    zoom=Image.new('RGB',(4*320,2*360),'#00ff00')
    zd=ImageDraw.Draw(zoom)
    for row,leg in enumerate([0,3]):
        limb=anatomy['limbs'][leg]
        sk=np.asarray(limb['knee'])
        tk=ORIGIN+FLIP@np.asarray(pose['knees'][leg][:2])*PPU
        alpha=np.rint(limbs[leg][...,3]*255).astype('uint8')
        images=[crop(encode(originals[leg],green=True),sk),crop(encode(old_limbs[leg],green=True),tk),
            crop(encode(limbs[leg],green=True),tk),crop(Image.fromarray(np.repeat(alpha[...,None],3,axis=-1)),tk)]
        for col,image in enumerate(images):
            native.paste(image,(col*200+60,row*160+70))
            draw.text((col*200+8,row*160+140),f'{limb["id"]} 64px at224PPU',fill='black',font=FONT)
            zoom.paste(image.resize((256,256),Image.Resampling.NEAREST),(col*320+20,row*360+70))
            zd.text((col*320+8,row*360+10),f'{limb["id"]}: {titles[col]}',fill='black',font=FONT)
            zd.text((col*320+8,row*360+34),'4x nearest; same source scale',fill='black',font=FONT)
    native.save(out/'two-joints-native-scale.png')
    zoom.save(out/'two-joints-4x-nearest.png')
    report={'status':'One-frame prototype only. Not production and not accepted art.',
        'identity':identity_records,'pose':{'row':7,'phase':0},'actual':actual_records,
        'wholeChecks':{'componentsAlphaOver4':whole_components,'pairIntersectionsOutsideActualTorso':pair_checks,
            'borderPixelsAlphaOver1':int((whole_alpha[0]>1).sum()+(whole_alpha[-1]>1).sum()+(whole_alpha[:,0]>1).sum()+(whole_alpha[:,-1]>1).sum())},
        'method':'One full native RGBA per limb; radius-preserving polar UV and rigid UV transition over8px. The complete joint zone is axially locked; only the shaft beyond jointRadius+8 compresses to its native endpoint. Angular ray ownership selects the shaft UV chart. In the empty exterior sector beyond14..22px from both rays, the polar chart remains, avoiding opposite-branch ghost samples. Only UV coordinates are interpolated; exactly one bilinear source RGBA sample. Explicit traced proximal source dirt cleanup. No alpha feathers, no circular cap layers, no target alpha clipping and no hole fill.'}
    (out/'prototype-check.json').write_text(json.dumps(report,indent=2))
    print(json.dumps(report,indent=2))


if __name__=='__main__':
    main()
