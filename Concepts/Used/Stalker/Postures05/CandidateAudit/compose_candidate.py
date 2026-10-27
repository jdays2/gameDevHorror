"""Independent local experiment: source-chain split + full-volume joint patch.

Reads the original Locomotion02 atlas and root StudyTarget pose JSON. Writes
only CandidateAudit; no changes to production, root compositor, or anatomy.
"""
from pathlib import Path
import json
import math
import numpy as np
from PIL import Image, ImageDraw, ImageFont, ImageFilter

OUT=Path(__file__).resolve().parent
FAMILY=OUT.parent
PROJECT=FAMILY.parents[3]
ORIGIN=np.array([288.,288.],dtype='float32')
FLIP=np.diag([1.,-1.]).astype('float32')
PPU=224
SIZE=576
FONT=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',15)
anatomy=json.loads((FAMILY.parent/'Locomotion02/anatomy.json').read_text())
atlas=Image.open(PROJECT/'Assets/EmergencyShift/Art/Enemies/Stalker/Locomotion02/rig-atlas.png').convert('RGBA')


def decode(image):
    rgba=np.asarray(image).astype('float32')/255
    rgb=rgba[...,:3]
    rgb=np.where(rgb<=.04045,rgb/12.92,((rgb+.055)/1.055)**2.4)
    return np.concatenate([rgb*rgba[...,3:4],rgba[...,3:4]],axis=-1)


def encode(rgba,background=None):
    alpha=rgba[...,3:4]
    rgb=rgba[...,:3]
    if background is not None:
        rgb=rgb+np.asarray(background,dtype='float32')*(1-alpha)
    else:
        rgb=np.divide(rgb,alpha,out=np.zeros_like(rgb),where=alpha>1e-6)
    rgb=np.where(rgb<=.0031308,rgb*12.92,1.055*np.maximum(rgb,0)**(1/2.4)-.055)
    if background is None:
        rgb=np.concatenate([rgb,alpha],axis=-1)
    return Image.fromarray((np.clip(rgb,0,1)*255).round().astype('uint8'))


def layer(index):
    x,y=index%4*358,index//4*406
    return decode(atlas.crop((x,y,x+358,y+406)))


def over(top,bottom):
    return top+bottom*(1-top[...,3:4])


def transform(paint,source_a,source_b,target_a,target_b,transverse_scale=1):
    source_a=np.asarray(source_a,dtype='float32')
    source_b=np.asarray(source_b,dtype='float32')
    target_a=np.asarray(target_a[:2],dtype='float32')
    target_b=np.asarray(target_b[:2],dtype='float32')
    source_axis=FLIP@(source_b-source_a)/PPU
    source_length=np.linalg.norm(source_axis)
    source_axis/=source_length
    target_axis=target_b-target_a
    target_length=np.linalg.norm(target_axis)
    target_axis/=target_length
    sn=np.array([-source_axis[1],source_axis[0]])
    tn=np.array([-target_axis[1],target_axis[0]])
    matrix=np.outer(target_axis,source_axis)*target_length/source_length+np.outer(tn,sn)*transverse_scale
    pixel_matrix=FLIP@matrix@FLIP
    translation=ORIGIN+FLIP@target_a*PPU-pixel_matrix@source_a
    inverse=np.linalg.inv(pixel_matrix)
    offset=-inverse@translation
    coefficients=(inverse[0,0],inverse[0,1],offset[0],inverse[1,0],inverse[1,1],offset[1])
    result=np.stack([np.asarray(Image.fromarray(paint[...,channel]).transform((SIZE,SIZE),
        Image.Transform.AFFINE,coefficients,Image.Resampling.BICUBIC)) for channel in range(4)],axis=-1)
    result=np.clip(result,0,1)
    result[...,:3]=np.minimum(result[...,:3],result[...,3:4])
    return result


sy,sx=np.mgrid[:406,:358]
source_points=np.stack([sx,sy],axis=-1).astype('float32')
ty,tx=np.mgrid[:SIZE,:SIZE]
target_points=np.stack([tx,ty],axis=-1).astype('float32')


def distance_segment(points,a,b):
    delta=b-a
    t=np.clip(np.sum((points-a)*delta,axis=-1)/np.dot(delta,delta),0,1)
    return np.linalg.norm(points-(a+t[...,None]*delta),axis=-1)


def sample(paint,x,y):
    x=np.clip(x,0,paint.shape[1]-1.001)
    y=np.clip(y,0,paint.shape[0]-1.001)
    x0,y0=np.floor(x).astype('int32'),np.floor(y).astype('int32')
    dx,dy=(x-x0)[...,None],(y-y0)[...,None]
    return ((paint[y0,x0]*(1-dx)+paint[y0,x0+1]*dx)*(1-dy)+
        (paint[y0+1,x0]*(1-dx)+paint[y0+1,x0+1]*dx)*dy)


parts=[]
source_sheet=Image.new('RGB',(6*230,4*265),(0,255,0))
source_draw=ImageDraw.Draw(source_sheet)
for leg,limb in enumerate(anatomy['limbs']):
    full=layer(leg*2+1)
    hip,knee,tip=[np.asarray(limb[k],dtype='float32') for k in ['hip','knee','tip']]
    du=distance_segment(source_points,hip,knee)
    dl=distance_segment(source_points,knee,tip)
    radius=np.linalg.norm(source_points-knee,axis=-1)
    upper_axis=(knee-hip)/np.linalg.norm(knee-hip)
    along=(source_points-hip)@upper_axis
    # Only segmentation of shared original pixels, not target capsule clipping.
    upper_keep=np.clip((dl-du+2)/4,0,1)*np.clip((along+9)/3,0,1)
    lower_keep=np.clip((du-dl+2)/4,0,1)
    # Remove compressed copies of the ball. The unchanged source patch restores
    # the actual cross-section at the target knee at its original pixel radius.
    joint_radius=[12.5,12.,10.,10.5,12.,11.5][leg]
    without_joint=np.clip((radius-(joint_radius-3))/3,0,1)
    upper=full*(upper_keep*without_joint)[...,None]
    lower=full*(lower_keep*without_joint)[...,None]
    cap=full*np.clip(joint_radius+1-radius,0,1)[...,None]
    root_radius=[13.,12.,9.,10.,12.,11.5][leg]
    root_distance=np.linalg.norm(source_points-hip,axis=-1)
    root=full*np.clip(root_radius+1-root_distance,0,1)[...,None]
    without_root=np.clip((root_distance-(root_radius-3))/3,0,1)
    parts.append({'full':full,'upper':upper,'upper_rootless':upper*without_root[...,None],
        'lower':lower,'cap':cap,'radius':joint_radius,'root':root,'root_radius':root_radius})
    for row,(name,paint) in enumerate([('full',full),('upper',upper),('lower',lower),('cap',cap)]):
        image=encode(paint,[0,1,0]).resize((230,261),Image.Resampling.LANCZOS)
        source_sheet.paste(image,(leg*230,row*265+4))
        source_draw.text((leg*230+5,row*265+3),f'{limb["id"]} / {name}',fill='black',font=FONT)
source_sheet.save(OUT/'source-parts.png')

torso=layer(0)
torso_target=np.zeros((SIZE,SIZE,4),dtype='float32')
x,y=(ORIGIN-np.asarray(anatomy['pivot'])).astype('int32')
torso_target[y:y+406,x:x+358]=torso


def polar_cap(paint,limb,pose,leg,radius):
    """Local joint-only angular warp with unchanged radial volume."""
    source_hip,source_knee,source_tip=[np.asarray(limb[key],dtype='float32') for key in ['hip','knee','tip']]
    target_hip,target_knee,target_tip=[ORIGIN+FLIP@np.asarray(pose[key][leg][:2])*PPU for key in ['hips','knees','feet']]
    sa=math.atan2(*(source_hip-source_knee)[::-1])
    sb=math.atan2(*(source_tip-source_knee)[::-1])
    ta=math.atan2(*(target_hip-target_knee)[::-1])
    tb=math.atan2(*(target_tip-target_knee)[::-1])
    sd=(sb-sa+math.pi)%(2*math.pi)-math.pi
    td=(tb-ta+math.pi)%(2*math.pi)-math.pi
    sign=1 if sd>0 else -1
    if td*sd<=0:
        raise ValueError('Projected bend changed sides')
    delta=target_points-target_knee
    angle=np.arctan2(delta[...,1],delta[...,0])
    theta=((angle-ta)*sign)%(2*math.pi)
    inner=theta<abs(td)
    source_theta=np.where(inner,theta*abs(sd)/abs(td),
        abs(sd)+(theta-abs(td))*(2*math.pi-abs(sd))/(2*math.pi-abs(td)))
    source_angle=sa+source_theta*sign
    distance=np.linalg.norm(delta,axis=-1)
    x=source_knee[0]+np.cos(source_angle)*distance
    y=source_knee[1]+np.sin(source_angle)*distance
    cap=sample(paint,x,y)
    cap*=np.clip(radius+1-distance,0,1)[...,None]
    return cap


def compose(pose,kind,show_layers=False,body_override=None,root_scale=1):
    canvas=np.zeros_like(torso_target)
    for leg,limb in enumerate(anatomy['limbs']):
        part=parts[leg]
        hip,knee,tip=pose['hips'][leg],pose['knees'][leg],pose['feet'][leg]
        upper_source=part['upper_rootless'] if kind in ['rootcap','rootless'] else part['upper']
        upper=transform(upper_source,limb['hip'],limb['knee'],hip,knee)
        lower=transform(part['lower'],limb['knee'],limb['tip'],knee,tip)
        if kind=='polar':
            joint=polar_cap(part['full'],limb,pose,leg,part['radius'])
        else:
            sk=np.asarray(limb['knee'],dtype='float32')
            tk=np.asarray(knee,dtype='float32')
            joint=transform(part['cap'],sk,sk+[1,0],tk,tk+[1/PPU,0,0])
        paint=over(joint,over(lower,upper))
        if kind=='rootcap':
            sh=np.asarray(limb['hip'],dtype='float32')
            th=np.asarray(hip,dtype='float32')
            root=transform(part['root'],sh,sh+[1,0],th,th+[root_scale/PPU,0,0],root_scale)
            paint=over(root,paint)
        if show_layers:
            colors=np.asarray([[1,.2,.2],[1,.7,.7],[.2,.8,1],[.2,.3,1],[1,1,.1],[1,.6,.1]],dtype='float32')
            paint[...,:3]=paint[...,3:4]*colors[leg]
        canvas=over(paint,canvas)
    body=torso_target.copy() if body_override is None else body_override.copy()
    if show_layers:
        body[...,:3]=body[...,3:4]*.7
    return over(body,canvas)


def main():
    poses=json.loads((FAMILY/'StudyTarget/poses.json').read_text())
    encode(torso_target,[0,1,0]).save(OUT/'original-torso-alone.png')
    encode(compose(poses[2],'fixed',True),[0,1,0]).save(OUT/'layer-ownership.png')
    mounted=json.loads(json.dumps(poses[2]))
    mounted['mountCorrection']='05 authoring hypothesis: hips and whole chains inward, fixed source paint unchanged'
    for leg in range(6):
        factor=[.65,.65,.70,.70,.72,.72][leg]
        hip=np.asarray(mounted['hips'][leg],dtype='float32')
        delta=np.array([hip[0]*(factor-1),hip[1]*(factor-1),0],dtype='float32')
        for key in ['hips','knees','feet']:
            mounted[key][leg]=(np.asarray(mounted[key][leg])+delta).tolist()
    (OUT/'mounted-pose.json').write_text(json.dumps(mounted,indent=2))
    mounted_sheet=Image.new('RGB',(3*576,2*636),(0,255,0))
    mounted_draw=ImageDraw.Draw(mounted_sheet)
    for index,padding in enumerate([0,2,4,6,8,10]):
        core_mask=Image.new('L',(358,406))
        ImageDraw.Draw(core_mask).polygon([(187,124),(120,247),(250,247)],fill=255)
        if padding:
            core_mask=core_mask.filter(ImageFilter.MaxFilter(padding*2+1))
        core_mask=core_mask.filter(ImageFilter.GaussianBlur(.65))
        mask=np.asarray(core_mask).astype('float32')/255
        core=torso*mask[...,None]
        core_target=np.zeros_like(torso_target)
        torso_x,torso_y=(ORIGIN-np.asarray(anatomy['pivot'])).astype('int32')
        core_target[torso_y:torso_y+406,torso_x:torso_x+358]=core
        canvas=compose(mounted,'rootcap',body_override=core_target)
        encode(canvas).save(OUT/f'mounted-pad-{padding}.png')
        encode(core_target,[0,1,0]).save(OUT/f'body-core-pad-{padding}.png')
        image=encode(canvas,[0,1,0])
        if padding==4:
            image.save(OUT/'mounted-pad-4-green.png')
            image.crop((160,160,416,448)).resize((768,864),Image.Resampling.LANCZOS).save(OUT/'mounted-pad-4-close.png')
        column,row=index%3,index//3
        px,py=column*576,row*636
        mounted_sheet.paste(image,(px,py+60))
        mounted_draw.text((px+10,py+8),f'Mount .65/.70/.72 | torso triangle padding {padding}px',fill='black',font=FONT)
        mounted_draw.text((px+10,py+31),'Same chain lengths / angles; only XY attachment correction',fill='black',font=FONT)
    mounted_sheet.save(OUT/'mounted-comparison.png')
    refinements=[('baseline','rootcap',1,0),('rootless','rootless',1,0),('root70','rootcap',.7,0),
        ('root70curve4','rootcap',.7,4),('rootlesscurve4','rootless',1,4),('root70curve8','rootcap',.7,8)]
    refinement_sheet=Image.new('RGB',(3*576,2*636),(0,255,0))
    refinement_draw=ImageDraw.Draw(refinement_sheet)
    for index,(label,kind,root_scale,base_curve) in enumerate(refinements):
        mask=Image.new('L',(358,406))
        ImageDraw.Draw(mask).polygon([(187,124),(120,247),(250,247)],fill=255)
        mask=mask.filter(ImageFilter.MaxFilter(9)).filter(ImageFilter.GaussianBlur(.65))
        mask=np.asarray(mask).astype('float32')/255
        if base_curve:
            # Local static torso correction only: follow existing bottom rim,
            # remove the old attachment flesh below it, preserve the eye/rim.
            bottom=251-base_curve*np.clip(1-((sx-185)/65)**2,0,1)
            mask*=np.clip(bottom-sy+.5,0,1)
        core=torso*mask[...,None]
        core_target=np.zeros_like(torso_target)
        torso_x,torso_y=(ORIGIN-np.asarray(anatomy['pivot'])).astype('int32')
        core_target[torso_y:torso_y+406,torso_x:torso_x+358]=core
        canvas=compose(mounted,kind,body_override=core_target,root_scale=root_scale)
        encode(canvas).save(OUT/f'refined-{label}.png')
        green=encode(canvas,[0,1,0])
        green.crop((160,160,416,448)).resize((768,864),Image.Resampling.LANCZOS).save(OUT/f'refined-{label}-close.png')
        px,py=index%3*576,index//3*636
        refinement_sheet.paste(green,(px,py+60))
        refinement_draw.text((px+10,py+8),label,fill='black',font=FONT)
        refinement_draw.text((px+10,py+31),f'Root volume factor {root_scale} | lower torso cleanup {base_curve}px',fill='black',font=FONT)
    refinement_sheet.save(OUT/'refinement-comparison.png')
    for kind in ['fixed','polar','rootcap']:
        (OUT/kind).mkdir(exist_ok=True)
        sheet=Image.new('RGB',(3*400,2*425),(0,255,0))
        draw=ImageDraw.Draw(sheet)
        for index,pose in enumerate(poses):
            canvas=compose(pose,kind)
            encode(canvas).save(OUT/kind/f'fold-{index}.png')
            green=encode(canvas,[0,1,0]).crop((88,88,488,488)).resize((360,360),Image.Resampling.LANCZOS)
            x,y=index%3*400,index//3*425
            sheet.paste(green,(x+20,y+55))
            draw.text((x+10,y+8),f'{kind} | spread {pose["spread"]}',fill='black',font=FONT)
            draw.text((x+10,y+29),'Nearest-chain split + restored full joint radius',fill='black',font=FONT)
        sheet.save(OUT/f'{kind}-green.png')
    print('Eighteen local joint-composition candidates and six mounted-body comparisons written; production untouched')


if __name__=='__main__':
    main()
