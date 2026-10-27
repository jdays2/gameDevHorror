"""Temporary exact-alpha search; production C# remains the source of truth."""
from pathlib import Path
import argparse, copy, hashlib, json, math, sys, time
import numpy as np
from PIL import Image, ImageDraw

HERE=Path(__file__).resolve().parent
FAMILY=HERE.parent
sys.path.insert(0,str(FAMILY))
from sprite_painter import SpritePainter, ORIGIN, FLIP, PPU, SIZE, _small_enclosed_regions
from compose_parts import encode


def slerp(a,b,t):
    dot=float(np.clip(np.dot(a,b),-1,1))
    angle=math.acos(dot)
    if angle<1e-6:
        result=a+(b-a)*t
        return result/np.linalg.norm(result)
    return (a*math.sin((1-t)*angle)+b*math.sin(t*angle))/math.sin(angle)


def mirror(pose,anatomy,settings,hind_knee_y=-.17,hind_toe_y=-.08):
    nx,ny,gx,gy,fg,hg,mky,mty=settings
    fold=pose['row']/8
    walking=min(fold,.5)
    tuck=max((fold-.5)/.5,0)
    stride=(1-.4*walking/.5)*(1-tuck)
    result=copy.deepcopy(pose)
    for leg,limb in enumerate(anatomy['limbs']):
        hip=np.asarray(pose['hips'][leg],dtype='float64')
        side=-1 if leg%2==0 else 1
        support=.52 if 2<=leg<4 else .56
        amplitude=support*.5*stride
        phase=(pose['phase']/48+([0,.5,.5,0,0,.5][leg]))%1
        lift=0
        if phase<support:
            offset=amplitude-2*amplitude*phase/support
        else:
            swing=(phase-support)/(1-support)
            smooth=swing*swing*(3-2*swing)
            offset=-amplitude+2*amplitude*smooth
            lift=math.sin(swing*math.pi)*.075*(1-walking)*(1-tuck)
        if leg<2:
            extended=np.array([-side*.085,.61])
            guarded=np.array([-side*.075,fg])
        elif leg<4:
            extended=np.array([side*nx,ny])
            guarded=np.array([side*gx,gy])
        else:
            extended=np.array([-side*.11,-.61])
            guarded=np.array([-side*.09,hg])
        foot=np.r_[hip[:2]+extended*(1-walking)+guarded*walking+[0,offset],lift]
        upper_length,lower_length=limb['upperLength'],limb['lowerLength']
        kh=(hip[2]+foot[2])*.5
        up=math.sqrt(upper_length**2-(hip[2]-kh)**2)
        low=math.sqrt(lower_length**2-(foot[2]-kh)**2)
        delta=foot[:2]-hip[:2]
        distance=np.clip(np.linalg.norm(delta),abs(up-low)+.0001,up+low-.0001)
        axis=delta/np.linalg.norm(delta)
        foot[:2]=hip[:2]+axis*distance
        along=(up*up-low*low+distance*distance)/(2*distance)
        radius=math.sqrt(max(0,up*up-along*along))
        source_foot=np.asarray(limb['tip'])-np.asarray(limb['hip'])
        source_knee=np.asarray(limb['knee'])-np.asarray(limb['hip'])
        source_foot[1]*=-1
        source_knee[1]*=-1
        bend=np.sign(source_foot[0]*source_knee[1]-source_foot[1]*source_knee[0])
        knee=np.r_[hip[:2]+axis*along+np.array([-axis[1],axis[0]])*radius*bend,kh]
        if tuck>0:
            direction=1 if leg<2 else -1
            folded_knee=np.array([side*.14,direction*.17])
            folded_toe=np.array([-side*.12,direction*.08])
            if leg>=4:
                folded_knee[1]=hind_knee_y
                folded_toe[1]=hind_toe_y
            if 2<=leg<4:
                folded_knee=np.array([side*.16,mky])
                folded_toe=np.array([-side*.08,mty])
            upper=np.r_[folded_knee,math.sqrt(upper_length**2-np.dot(folded_knee,folded_knee))]/upper_length
            distal=folded_toe-folded_knee
            lower=np.r_[distal,-math.sqrt(lower_length**2-np.dot(distal,distal))]/lower_length
            old_upper=(knee-hip)/np.linalg.norm(knee-hip)
            old_lower=(foot-knee)/np.linalg.norm(foot-knee)
            knee=hip+slerp(old_upper,upper,tuck)*upper_length
            foot=knee+slerp(old_lower,lower,tuck)*lower_length
        result['knees'][leg]=knee.tolist()
        result['feet'][leg]=foot.tolist()
    return result


def alpha_transform(part,sa,sb,ta,tb):
    sa,sb=np.asarray(sa,dtype='float32'),np.asarray(sb,dtype='float32')
    ta,tb=np.asarray(ta[:2],dtype='float32'),np.asarray(tb[:2],dtype='float32')
    u=FLIP@(sb-sa)/PPU
    length=np.linalg.norm(u)
    u/=length
    v=tb-ta
    scale=np.linalg.norm(v)
    v/=scale
    matrix=np.outer(v,u)*scale/length+np.outer([-v[1],v[0]],[-u[1],u[0]])
    matrix=FLIP@matrix@FLIP
    translation=ORIGIN+FLIP@ta*PPU-matrix@sa
    inverse=np.linalg.inv(matrix)
    l,t,r,b=part.box
    corners=np.array([[l,t],[r,t],[l,b],[r,b]],dtype='float32')@matrix.T+translation
    x0=max(0,int(np.floor(corners[:,0].min()))-2)
    y0=max(0,int(np.floor(corners[:,1].min()))-2)
    x1=min(SIZE,int(np.ceil(corners[:,0].max()))+2)
    y1=min(SIZE,int(np.ceil(corners[:,1].max()))+2)
    result=np.zeros((SIZE,SIZE),dtype='float32')
    if x0>=x1 or y0>=y1:
        return result
    offset=inverse@(np.array([x0,y0],dtype='float32')-translation)-[l,t]
    coefficients=(inverse[0,0],inverse[0,1],offset[0],inverse[1,0],inverse[1,1],offset[1])
    region=np.asarray(part.channels[3].transform((x1-x0,y1-y0),Image.Transform.AFFINE,coefficients,Image.Resampling.BICUBIC))
    result[y0:y1,x0:x1]=np.clip(region,0,1)
    return result


def limb_alpha(painter,pose,leg):
    limb=painter.anatomy['limbs'][leg]
    upper,lower,joint,root=painter._parts[leg]
    hip,knee,toe=[np.asarray(pose[key][leg],dtype='float32') for key in ['hips','knees','feet']]
    a=alpha_transform(upper,limb['hip'],limb['knee'],hip,knee)
    b=alpha_transform(lower,limb['knee'],limb['tip'],knee,toe)
    sk=np.asarray(limb['knee'],dtype='float32')
    c=alpha_transform(joint,sk,sk+[1,0],knee,knee+[1/PPU,0,0])
    sh=np.asarray(limb['hip'],dtype='float32')
    d=alpha_transform(root,sh,sh+[1,0],hip,hip+[1/PPU,0,0])
    result=d+(c+(b+a*(1-b))*(1-c))*(1-d)
    yy,xx=np.nonzero(result>4/255)
    if len(xx):
        x0,y0=max(0,int(xx.min())-1),max(0,int(yy.min())-1)
        x1,y1=min(SIZE,int(xx.max())+2),min(SIZE,int(yy.max())+2)
        roi=result[y0:y1,x0:x1]
        for threshold in (128/255,4/255):
            for hole in _small_enclosed_regions(np.rint(np.clip(roi,0,1)*255)>round(threshold*255),6):
                hy,hx=zip(*hole)
                roi[hy,hx]=1 if threshold>.5 else 128/255
    return (np.clip(result,0,1)*255).round().astype('uint8')>128


def main():
    parser=argparse.ArgumentParser()
    parser.add_argument('--settings',nargs=8,type=float,default=[.54,-.28,.44,-.20,.35,-.35,-.03,-.16])
    parser.add_argument('--label',default='guard35')
    parser.add_argument('--rows',nargs='*',type=int)
    parser.add_argument('--fold-hind-knee-y',type=float,default=-.17)
    parser.add_argument('--fold-hind-toe-y',type=float,default=-.08)
    args=parser.parse_args()
    out=HERE/'GeometrySearch'/args.label
    out.mkdir(parents=True,exist_ok=True)
    source_bytes=(FAMILY/'poses.json').read_bytes()
    source=json.loads(source_bytes)
    painter=SpritePainter(FAMILY.parents[3])
    # Infer current C# lane settings from the two loaded support poses, before
    # changing anything. This detects a stale or incorrectly mirrored source.
    normal=next(p for p in source if p['row']==0 and p['phase']==0)
    guarded=next(p for p in source if p['row']==4 and p['phase']==0)
    fully_folded=next(p for p in source if p['row']==8 and p['phase']==0)
    nx=normal['feet'][3][0]-normal['hips'][3][0]
    ny=normal['feet'][3][1]-normal['hips'][3][1]-.26
    gx=2*(guarded['feet'][3][0]-guarded['hips'][3][0]) - nx
    gy=2*(guarded['feet'][3][1]-guarded['hips'][3][1]-.156) - ny
    fg=2*(guarded['feet'][0][1]-guarded['hips'][0][1]-.168)-.61
    hg=2*(guarded['feet'][4][1]-guarded['hips'][4][1]-.168)+.61
    mky=fully_folded['knees'][2][1]-fully_folded['hips'][2][1]
    mty=fully_folded['feet'][2][1]-fully_folded['hips'][2][1]
    hky=fully_folded['knees'][4][1]-fully_folded['hips'][4][1]
    hty=fully_folded['feet'][4][1]-fully_folded['hips'][4][1]
    baseline=[nx,ny,gx,gy,fg,hg,mky,mty]
    mirror_error=0
    for pose in source:
        replica=mirror(pose,painter.anatomy,baseline,hky,hty)
        mirror_error=max(mirror_error,float(np.max(np.abs(np.asarray(replica['knees'])-pose['knees']))),
            float(np.max(np.abs(np.asarray(replica['feet'])-pose['feet']))))
    if mirror_error>3e-5:
        raise ValueError(f'C# mirror does not match source: {mirror_error}; inferred {baseline}')
    candidates=[mirror(p,painter.anatomy,args.settings,args.fold_hind_knee_y,args.fold_hind_toe_y)
        for p in source if args.rows is None or p['row'] in args.rows]
    # Independently verify the alpha-only fast path against the full API before
    # using it to judge any candidate. It must be bit-identical at the threshold.
    _,reference_limbs,_=painter.paint(candidates[0])
    alpha_differences=[int(np.count_nonzero(limb_alpha(painter,candidates[0],leg) !=
        (np.rint(reference_limbs[leg][...,3]*255)>128))) for leg in range(6)]
    if any(alpha_differences):
        raise ValueError(f'Alpha fast path differs from canonical painter: {alpha_differences}')
    torso=(painter._torso[...,3]*255).round().astype('uint8')>128
    pair_records=[]
    start=time.perf_counter()
    for index,pose in enumerate(candidates):
        masks=[limb_alpha(painter,pose,leg) for leg in range(6)]
        for a in range(6):
            for b in range(a+1,6):
                overlap=masks[a]&masks[b]&~torso
                count=int(overlap.sum())
                if count:
                    yy,xx=np.nonzero(overlap)
                    pair_records.append({'row':pose['row'],'phase':pose['phase'],'pair':[a,b],'pixels':count,
                        'bbox':[int(xx.min()),int(yy.min()),int(xx.max()+1),int(yy.max()+1)]})
        if index%96==95:
            print(json.dumps({'checked':index+1,'maxPairSoFar':max([p['pixels'] for p in pair_records],default=0)}),flush=True)
    pair_records.sort(key=lambda item:-item['pixels'])
    (out/'poses.json').write_text(json.dumps(candidates))
    report={'settings':args.settings,'settingsOrder':['normalX','normalY','guardX','guardY','foreGuardY','hindGuardY','foldMiddleKneeY','foldMiddleToeY'],
        'sourcePosesSha256':hashlib.sha256(source_bytes).hexdigest(),
        'cSharpSourceInferredSettings':baseline,'mirrorMaxSourceError':mirror_error,'frames':len(candidates),
        'alphaFastPathThresholdDifferences':alpha_differences,'rows':args.rows,
        'foldHindKneeY':args.fold_hind_knee_y,'foldHindToeY':args.fold_hind_toe_y,
        'seconds':time.perf_counter()-start,'pairs':pair_records,'worstPairPixels':max([p['pixels'] for p in pair_records],default=0),
        'note':'Exact source alpha affine maps and actual torso only. No capsule or hip-disc exclusion. Candidate is temporary Python research until independently exported from C#.'}
    (out/'qa.json').write_text(json.dumps(report,indent=2))
    for record in pair_records[:4]:
        pose=next(p for p in candidates if p['row']==record['row'] and p['phase']==record['phase'])
        whole,limbs,torso_rgba=painter.paint(pose)
        a,b=record['pair']
        overlap=(np.rint(limbs[a][...,3]*255)>128)&(np.rint(limbs[b][...,3]*255)>128)&~torso
        image=np.asarray(encode(whole,green=True)).copy()
        image[overlap]=(255,20,10)
        Image.fromarray(image).save(out/f'worst-{record["row"]}-{record["phase"]:02d}-{a}-{b}.png')
    print(json.dumps({key:value for key,value in report.items() if key!='pairs'},indent=2))


if __name__=='__main__':
    main()
