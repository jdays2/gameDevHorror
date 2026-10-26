"""Native modular scorpion surfaces driven by a declared rig, not generated poses."""
from pathlib import Path
import copy
import hashlib
import json
import math
import sys
import numpy as np
from PIL import Image, ImageDraw, ImageFont, ImageFilter
import build_anatomy as anatomy
import scorpion_parts_r5 as parts

ROOT=Path(__file__).resolve().parent
PROJECT=ROOT.parents[3]
SIZE=1024
SCALE=340.
ORIGIN=np.array([512.,548.])
FONT=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',16)
SOURCES=ROOT/'sources'
ART=PROJECT/'Assets/EmergencyShift/Art/Enemies/Scorpion'
COUNT=24
STRIDE=.40
DUTY=.64
NATIVE_SECONDS=.36
ATTACK_MS=[18,17]*16
ATTACK_SECONDS=sum(ATTACK_MS)/1000
ATTACK_CYCLES=2


def write(path,value):
    path.parent.mkdir(parents=True,exist_ok=True)
    path.write_text(json.dumps(value,ensure_ascii=False,indent=2),encoding='utf-8',newline='\n')


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def project(points):
    p=np.asarray(points,dtype=float)
    return p[...,:2]*np.array([SCALE,-SCALE])+ORIGIN


def plan():
    p=anatomy.make_plan()
    p['revision']='scorpion-native-r5-compact'
    p['sourceStrategy']='Native modular artwork. Whole generated concepts v1-v4 failed the separate skeleton gate and are not approved masters.'
    p['changesFromR1']={'kneeHeight':[.20,.32],
                       'reason':'Reserve true 3D reach for a fast grounded gait without flattening the legs or extending the image-plane bones.',
                       'topProjectionUnchanged':True,'limbCountsUnchanged':True}
    for chain in p['walkingLegs'].values():
        chain[1][2]=.32
    for side,sign in [('L',-1),('R',1)]:
        p['pedipalps'][side]={'arm':[[sign*.13,.36,.14],[sign*.40,.70,.15],[sign*.46,.98,.08]],
                              'hinge':[sign*.46,1.08,.08],
                              'fixedTip':[sign*.28,1.38,.05],
                              'movingTip':[sign*.72,1.34,.05]}
    p['counts']['primaryEyes']=10
    p['eyePositions']=[[sign*x,y,.15] for sign in [-1,1] for x,y in [(.055,.375),(.12,.32),(.075,.255),(.15,.23),(.10,.18)]]
    p['changesFromR2']={'pincers':'Larger two-finger pincers held forward; paired close during the strike and alternate clacking during run.',
                       'tailSurfaceWidthMultiplier':1.65,'eyes':10,'head':'Broader blunt anterior crown; same body axis and attachment sockets.',
                       'countsAndWalkingLegLengthsUnchanged':True}
    p['gait']={'stride':STRIDE,'duty':DUTY,'frameCount':COUNT,'frameMs':15,
               'nativeReferenceSpeed':STRIDE/DUTY/NATIVE_SECONDS,
               'alternatingTetrapods':[['L1','R2','L3','R4'],['R1','L2','R3','L4']]}
    parts.update_plan(p)
    p['changesFromR4']={'pincers':'Short pedipalps, bulbous manus, short dark grasping tips; stable nearly closed running grip.',
                        'attack':'Two continuing gait cycles during a moving front claw capture.',
                        'surface':'Quiet ash/olive with deliberate larger blood smears, ten red eyes.'}
    write(ROOT/'native-plan.json',p)
    anatomy.projections(p).save(ROOT/'02-native-independent-projections.jpg',quality=93)
    anatomy.top(p).save(ROOT/'02-native-independent-bones.png')
    write(ROOT/'native-geometry.json',anatomy.validate(p))
    return p


def fixed_plan():
    return json.loads((ROOT/'native-plan.json').read_text())


def ik(root,end,hint,a,b):
    delta=end-root
    distance=np.linalg.norm(delta)
    if not abs(a-b)<distance<a+b:
        raise ValueError(f'Unreachable {distance}: {abs(a-b)}..{a+b}')
    axis=delta/distance
    along=(a*a-b*b+distance*distance)/(2*distance)
    pole=hint-root
    pole-=np.dot(pole,axis)*axis
    pole/=np.linalg.norm(pole)
    return root+axis*along+pole*math.sqrt(max(0,a*a-along*along))


def foot(q):
    q%=1
    if q<DUTY:
        return STRIDE*(.5-q/DUTY),0.,True
    u=(q-DUTY)/(1-DUTY)
    smooth=u*u*u*(10+u*(-15+6*u))
    return STRIDE*(smooth-.5),math.sin(math.pi*u)**2,False


def rotate(points,angle,center):
    c,s=math.cos(angle),math.sin(angle)
    matrix=np.array([[c,-s],[s,c]])
    points=np.asarray(points,dtype=float).copy()
    points[...,:2]=(points[...,:2]-center[:2])@matrix.T+center[:2]
    return points


def pose(t,kind='run'):
    p=fixed_plan()
    legs={}
    moving=kind in ('run','attack')
    gait=t*ATTACK_CYCLES if kind=='attack' else t
    yaw=math.radians(.6)*math.sin(2*math.pi*gait) if moving else 0.
    root_offset=np.array([.006*math.sin(2*math.pi*gait),0.,.003*math.cos(4*math.pi*gait)]) if moving else np.zeros(3)
    tetrapod=set(p['gait']['alternatingTetrapods'][0])
    for ident,chain in p['walkingLegs'].items():
        rest=np.asarray(chain,dtype=float)
        joints=rest.copy()
        contact=True
        if moving:
            q=(gait+(0 if ident in tetrapod else .5))%1
            dy,lift,contact=foot(q)
            joints[0]=rotate(rest[0],yaw,np.array([0.,0.,0.]))+root_offset
            joints[2,1]+=dy
            joints[2,2]+=.045*lift
            lengths=np.linalg.norm(np.diff(rest,axis=0),axis=1)
            joints[1]=ik(joints[0],joints[2],rest[1]+root_offset,*lengths)
        legs[ident]={'joints3D':joints.tolist(),'contact':contact}
    palps=copy.deepcopy(p['pedipalps'])
    tail=np.asarray(p['tail'],dtype=float)
    if kind=='attack':
        if t<.22:
            wind=t/.22
            drive=0.
        elif t<.40:
            wind=1-(t-.22)/.18
            drive=(t-.22)/.18
        else:
            wind=0.
            drive=1-(t-.40)/.60
        for ident,palp in palps.items():
            side=-1 if ident=='L' else 1
            origin=np.asarray(palp['arm'][0],dtype=float)
            angle=-side*math.radians(8)*wind+side*math.radians(8)*drive
            palp['arm']=rotate(palp['arm'],angle,origin).tolist()
            for key in parts.CHELA_KEYS:
                palp[key]=rotate(palp[key],angle,origin).tolist()
            parts.close_chela(palp,side,.70+.30*drive-.70*wind,rotate)
        tail=rotate(tail,math.radians(-4)*wind+math.radians(2)*drive,tail[0])
    elif kind=='run':
        for ident,palp in palps.items():
            side=-1 if ident=='L' else 1
            origin=np.asarray(palp['arm'][0],dtype=float)
            angle=side*math.radians(.5)*math.sin(2*math.pi*t)
            palp['arm']=rotate(palp['arm'],angle,origin).tolist()
            for key in parts.CHELA_KEYS:
                palp[key]=rotate(palp[key],angle,origin).tolist()
            parts.close_chela(palp,side,.70,rotate)
        tail=rotate(tail,math.radians(1.2)*math.sin(2*math.pi*(t-.18)),tail[0])
    if moving:
        if kind=='attack':
            tail=rotate(tail,math.radians(1.2)*math.sin(2*math.pi*(gait-.18)),tail[0])
        for palp in palps.values():
            palp['arm']=(rotate(palp['arm'],yaw,np.zeros(3))+root_offset).tolist()
            for key in parts.CHELA_KEYS:
                palp[key]=(rotate(palp[key],yaw,np.zeros(3))+root_offset).tolist()
        tail=rotate(tail,yaw,np.zeros(3))+root_offset
    return {'kind':kind,'phase':t,'legs':legs,'palps':palps,'tail':tail.tolist(),
            'bodyYaw':yaw,'bodyOffset3D':root_offset.tolist()}


def distance_to_chain(pixels,chain):
    distances=[]
    for a,b in zip(chain,chain[1:]):
        a,b=np.asarray(a),np.asarray(b)
        v=b-a
        f=np.clip(np.sum((pixels-a)*v,axis=-1)/np.dot(v,v),0,1)
        distances.append(np.linalg.norm(pixels-(a+f[...,None]*v),axis=-1))
    return np.min(distances,axis=0)


def prepare_sources():
    SOURCES.mkdir(exist_ok=True)
    atlas=PROJECT/'Assets/EmergencyShift/Art/Enemies/Stalker/Locomotion02/rig-atlas.png'
    if not (SOURCES/'stalker-atlas.png').exists():
        (SOURCES/'stalker-atlas.png').write_bytes(atlas.read_bytes())
    whole=Image.open(ROOT/'concept-v3.png').convert('RGBA')
    yy,xx=np.mgrid[:whole.height,:whole.width]
    pixels=np.stack([xx,yy],axis=-1)
    sources={}
    for side,sign in [('L',-1),('R',1)]:
        def mir(x,y):
            return [x if sign==-1 else 1254-x,y]
        chain=[mir(561,481),mir(483,360),mir(394,258),mir(380,195)]
        fingers=[mir(408,79),mir(290,93)]
        shape=np.asarray(whole).copy()
        distance=distance_to_chain(pixels,chain)
        for tip in fingers:
            distance=np.minimum(distance,distance_to_chain(pixels,[chain[-1],tip]))
        shape[distance>38,3]=0
        shape[yy>525,3]=0
        start=np.asarray(chain[0],dtype=float)
        axis=np.asarray(chain[1],dtype=float)-start
        axis/=np.linalg.norm(axis)
        behind=np.sum((pixels-start)*axis,axis=-1)<0
        shape[behind&(np.linalg.norm(pixels-start,axis=-1)>10),3]=0
        image=Image.fromarray(shape)
        image.save(SOURCES/(side+'-palp.png'))
        sources[side]={'chainPixels':chain,'fixedTipPixels':fingers[0],'movingTipPixels':fingers[1]}
    chain=[[627,868],[633,964],[661,1046],[730,1104],[805,1089],[847,1022],[797,948]]
    shape=np.asarray(whole).copy()
    shape[distance_to_chain(pixels,chain)>47,3]=0
    shape[yy<852,3]=0
    shape[(yy<890)&(np.abs(xx-627)>25),3]=0
    shape[yy<860,3]=0
    Image.fromarray(shape).save(SOURCES/'tail.png')
    sources['tail']={'chainPixels':chain}
    sources['legs']={'LF':[[147,156],[84,81],[150,7]],'RF':[[226,158],[299,94],[244,15]],
                     'LM':[[134,201],[53,189],[8,265]],'RM':[[241,206],[323,210],[349,287]],
                     'LR':[[141,252],[83,333],[138,399]],'RR':[[232,252],[287,326],[246,394]]}
    sources['artworkUVOnly']='Source pixel anchors are skinning UV calibration, not a retrospective pass of rejected whole concepts. Native bones are declared independently for each revision.'
    write(ROOT/'source-attachments.json',sources)


def map_segment(points,a,b,c,d):
    a,b,c,d=map(np.asarray,(a,b,c,d))
    x=(b-a)/np.linalg.norm(b-a)
    y=np.array([-x[1],x[0]])
    u=(d-c)/np.linalg.norm(d-c)
    v=np.array([-u[1],u[0]])
    local=points-a
    ratio=np.linalg.norm(d-c)/np.linalg.norm(b-a)
    return c+np.sum(local*x,axis=-1)[...,None]*ratio*u+np.sum(local*y,axis=-1)[...,None]*ratio*v


class Surface:
    def __init__(self,image,chain,edges=None,width_scale=1.):
        self.image=image.convert('RGBA')
        self.chain=np.asarray(chain,dtype=float)
        self.edges=edges or list(zip(range(len(chain)-1),range(1,len(chain))))
        self.width_scale=width_scale
        self.pixels=np.asarray(self.image).astype(float)/255.
        self.pixels[...,:3]*=self.pixels[...,3:4]
        bounds=self.image.getbbox()
        x0,y0,x1,y1=bounds
        self.xs=np.linspace(x0,x1,math.ceil((x1-x0)/9)+1)
        self.ys=np.linspace(y0,y1,math.ceil((y1-y0)/9)+1)
        yy,xx=np.meshgrid(self.ys,self.xs,indexing='ij')
        self.vertices=np.column_stack([xx.ravel(),yy.ravel()])
        ny,nx=len(self.ys),len(self.xs)
        self.triangles=[]
        alpha=np.asarray(self.image)[...,3]
        for y in range(ny-1):
            for x in range(nx-1):
                lo=np.floor([self.xs[x],self.ys[y]]).astype(int)
                hi=np.ceil([self.xs[x+1],self.ys[y+1]]).astype(int)
                if not np.any(alpha[max(0,lo[1]):hi[1]+1,max(0,lo[0]):hi[0]+1]>4):
                    continue
                a=y*nx+x
                self.triangles.extend([[a,a+1,a+nx],[a+1,a+nx+1,a+nx]])
        self.weights=[]
        for ia,ib in self.edges:
            a,b=self.chain[ia],self.chain[ib]
            v=b-a
            f=np.clip(np.sum((self.vertices-a)*v,axis=1)/np.dot(v,v),0,1)
            dist=np.linalg.norm(self.vertices-(a+f[:,None]*v),axis=1)
            self.weights.append(1/np.maximum(dist,5.)**4)
        self.weights=np.asarray(self.weights).T
        self.weights/=np.sum(self.weights,axis=1)[:,None]

    def deform(self,target):
        target=np.asarray(target,dtype=float)
        mapped=[]
        for a,b in self.edges:
            points=map_segment(self.vertices,self.chain[a],self.chain[b],target[a],target[b])
            axis=target[b]-target[a]
            axis/=np.linalg.norm(axis)
            normal=np.array([-axis[1],axis[0]])
            across=np.sum((points-target[a])*normal,axis=-1)
            points+=(self.width_scale-1)*across[:,None]*normal
            mapped.append(points)
        mapped=np.stack(mapped,axis=1)
        return np.sum(mapped*self.weights[...,None],axis=1)

    def render(self,target):
        posed=self.deform(target)
        initial=posed.copy()
        triangles=np.asarray(self.triangles,dtype=int)
        centers=np.min(np.linalg.norm(self.vertices[:,None,:]-self.chain[None,:,:],axis=-1),axis=1)
        free=centers>5.
        source_t=self.vertices[triangles]
        first,second=source_t[:,1]-source_t[:,0],source_t[:,2]-source_t[:,0]
        source_area=first[:,0]*second[:,1]-first[:,1]*second[:,0]
        minimum=source_area*.035
        original_bad=0
        for iteration in range(2400):
            t=posed[triangles]
            first,second=t[:,1]-t[:,0],t[:,2]-t[:,0]
            area=first[:,0]*second[:,1]-first[:,1]*second[:,0]
            bad=np.flatnonzero(area<minimum)
            if iteration==0:
                original_bad=len(bad)
            if len(bad)==0:
                break
            for k in bad:
                indices=triangles[k]
                a,b,c=posed[indices]
                actual=(b[0]-a[0])*(c[1]-a[1])-(b[1]-a[1])*(c[0]-a[0])
                gradient=np.array([[b[1]-c[1],c[0]-b[0]],[c[1]-a[1],a[0]-c[0]],[a[1]-b[1],b[0]-a[0]]])
                gradient*=free[indices,None]
                denominator=np.sum(gradient*gradient)
                if denominator>1e-10:
                    posed[indices]+=np.clip(gradient*(minimum[k]*1.04-actual)/denominator,-1.5,1.5)
        canvas=np.zeros((SIZE,SIZE,4),dtype=float)
        priority=np.zeros((SIZE,SIZE),dtype=float)
        flipped=0
        for triangle in self.triangles:
            source=self.vertices[triangle]
            dest=posed[triangle]
            a,b,c=dest
            ab,ac=b-a,c-a
            determinant=ab[0]*ac[1]-ab[1]*ac[0]
            sb,sc=source[1]-source[0],source[2]-source[0]
            source_det=sb[0]*sc[1]-sb[1]*sc[0]
            if determinant*source_det<=0:
                flipped+=1
                continue
            lo=np.maximum(np.floor(dest.min(axis=0)).astype(int),0)
            hi=np.minimum(np.ceil(dest.max(axis=0)).astype(int),SIZE-1)
            if np.any(hi<lo):
                continue
            yy,xx=np.mgrid[lo[1]:hi[1]+1,lo[0]:hi[0]+1]
            u=((xx-a[0])*(c[1]-a[1])-(yy-a[1])*(c[0]-a[0]))/determinant
            v=((b[0]-a[0])*(yy-a[1])-(b[1]-a[1])*(xx-a[0]))/determinant
            inside=(u>=-.0001)&(v>=-.0001)&(u+v<=1.0001)
            uv=source[0]+u[...,None]*(source[1]-source[0])+v[...,None]*(source[2]-source[0])
            sx=np.clip(np.floor(uv[...,0]).astype(int),0,self.image.width-2)
            sy=np.clip(np.floor(uv[...,1]).astype(int),0,self.image.height-2)
            fx=(uv[...,0]-sx)[...,None]
            fy=(uv[...,1]-sy)[...,None]
            color=(self.pixels[sy,sx]*(1-fx)+self.pixels[sy,sx+1]*fx)*(1-fy)+(self.pixels[sy+1,sx]*(1-fx)+self.pixels[sy+1,sx+1]*fx)*fy
            old=priority[lo[1]:hi[1]+1,lo[0]:hi[0]+1]
            inside&=color[...,3]>=old
            canvas[lo[1]:hi[1]+1,lo[0]:hi[0]+1][inside]=color[inside]
            old[inside]=color[...,3][inside]
        canvas[...,:3]/=np.maximum(canvas[...,3:4],1e-6)
        return Image.fromarray(np.uint8(np.clip(canvas,0,1)*255)),{'flippedTriangles':flipped,
                'correctedSurfaceTriangles':original_bad,'maximumSurfaceCorrectionPx':float(np.max(np.linalg.norm(posed-initial,axis=1))),
                'jointPinErrorPx':float(np.max(np.linalg.norm(posed[~free]-initial[~free],axis=1))) if np.any(~free) else 0.}


def clean_part_pixels(rgba):
    """Remove detached atlas residue and single-pixel frays, retaining soft edges."""
    from collections import deque
    alpha=rgba[...,3]
    solid=alpha>24
    visited=np.zeros(solid.shape,dtype=bool)
    largest=[]
    for y,x in np.argwhere(solid):
        if visited[y,x]:
            continue
        queue=deque([(y,x)])
        visited[y,x]=True
        component=[]
        while queue:
            cy,cx=queue.popleft()
            component.append((cy,cx))
            for dy,dx in [(0,1),(0,-1),(1,0),(-1,0)]:
                ny,nx=cy+dy,cx+dx
                if 0<=ny<solid.shape[0] and 0<=nx<solid.shape[1] and solid[ny,nx] and not visited[ny,nx]:
                    visited[ny,nx]=True
                    queue.append((ny,nx))
        if len(component)>len(largest):
            largest=component
    mask=np.zeros(solid.shape,dtype=np.uint8)
    if largest:
        y,x=np.asarray(largest).T
        mask[y,x]=255
    mask=Image.fromarray(mask).filter(ImageFilter.MinFilter(3)).filter(ImageFilter.MaxFilter(3))
    mask=mask.filter(ImageFilter.GaussianBlur(.55))
    rgba[...,3]=np.minimum(alpha,np.asarray(mask))
    return rgba


def surfaces():
    anchors=json.loads((ROOT/'source-attachments.json').read_text())
    atlas=Image.open(SOURCES/'sage-atlas.png').convert('RGBA')
    ids=['LF','RF','LM','RM','LR','RR']
    data={}
    for index,ident in enumerate(ids):
        layer=1+index*2
        xy=(layer%4*358,layer//4*406)
        image=atlas.crop((xy[0],xy[1],xy[0]+358,xy[1]+406))
        rgba=np.asarray(image).copy()
        yy,xx=np.mgrid[:406,:358]
        points=np.stack([xx,yy],axis=-1)
        chain=anchors['legs'][ident]
        proximal=distance_to_chain(points,chain[:2])
        distal=distance_to_chain(points,chain[1:])
        # Native part ownership: omit neighbouring torso/limb pixels in donor
        # atlas cells, while retaining a full joint cap and root overlap.
        rgba[(proximal>24)&(distal>17),3]=0
        root=np.asarray(chain[0],dtype=float)
        axis=np.asarray(chain[1],dtype=float)-root
        axis/=np.linalg.norm(axis)
        behind=np.sum((points-root)*axis,axis=-1)<-4
        rgba[behind&(np.linalg.norm(points-root,axis=-1)>6),3]=0
        along=np.sum((points-root)*axis,axis=-1)
        across=np.abs((points[...,0]-root[0])*axis[1]-(points[...,1]-root[1])*axis[0])
        rgba[(along<19)&(across>9),3]=0
        image=Image.fromarray(clean_part_pixels(rgba))
        data[ident]=Surface(image,anchors['legs'][ident])
    # Pedipalp shafts reuse one quiet proximal painted segment, without its talon.
    cell=5
    xy=(cell%4*358,cell//4*406)
    image=atlas.crop((xy[0],xy[1],xy[0]+358,xy[1]+406))
    rgba=np.asarray(image).copy()
    yy,xx=np.mgrid[:406,:358]
    pixels=np.stack([xx,yy],axis=-1)
    a,b=map(np.asarray,anchors['legs']['LM'][:2])
    axis=(b-a)/np.linalg.norm(b-a)
    along=np.sum((pixels-a)*axis,axis=-1)
    length=np.linalg.norm(b-a)
    rgba[distance_to_chain(pixels,[a+axis*20,b-axis*26])>9,3]=0
    data['arm']=Surface(Image.fromarray(rgba),[a+axis*12,b-axis*18],width_scale=.65)
    data['chela']=parts.chela_layers()
    data['tail']=Surface(Image.open(ROOT/'tail-source-r4.png'),parts.TAIL)
    return data


def render(p,surface):
    canvas=Image.new('RGBA',(SIZE,SIZE))
    metrics=[]
    leg_masks=[]
    chela_masks=[]
    for ident,limb in p['legs'].items():
        number=int(ident[1])
        template=ident[0]+('F' if number==1 else 'R' if number==4 else 'M')
        image,m=surface[template].render(project(limb['joints3D']))
        canvas.alpha_composite(image)
        leg_masks.append(np.asarray(image.getchannel('A'))>48)
        metrics.append(m)
    image,m=surface['tail'].render(project(p['tail']))
    canvas.alpha_composite(image)
    metrics.append(m)
    for side,palp in p['palps'].items():
        for first,last in zip(palp['arm'],palp['arm'][1:]):
            image,m=surface['arm'].render(project([first,last]))
            canvas.alpha_composite(image)
            metrics.append(m)
        chela=parts.render_chela(palp,side,project,surface['chela'])
        chela_masks.append(np.asarray(chela.getchannel('A'))>48)
        canvas.alpha_composite(chela)
    # Reusable painted attachment caps belong to the declared hip/root nodes.
    # They cover ownership cuts under the torso; they add no new bone or hinge.
    atlas=Image.open(SOURCES/'sage-atlas.png').convert('RGBA')
    cell=5
    x,y=cell%4*358+53,cell//4*406+189
    cap=atlas.crop((x-12,y-12,x+12,y+12))
    cap_mask=Image.new('L',(24,24))
    ImageDraw.Draw(cap_mask).ellipse((1,1,22,22),fill=255)
    original=np.asarray(cap).copy()
    original[...,3]=np.minimum(original[...,3],np.asarray(cap_mask))
    cap=Image.fromarray(original).resize((26,26),Image.Resampling.LANCZOS)
    roots=[l['joints3D'][0] for l in p['legs'].values()]
    roots.extend(palp['arm'][0] for palp in p['palps'].values())
    for root in roots:
        x,y=project(root)
        canvas.alpha_composite(cap,(round(x)-13,round(y)-13))
    elbow_cap=cap.resize((36,36),Image.Resampling.LANCZOS)
    for palp in p['palps'].values():
        x,y=project(palp['arm'][1])
        canvas.alpha_composite(elbow_cap,(round(x)-18,round(y)-18))
    canvas.alpha_composite(torso_surface(p))
    return canvas,{'flippedTriangles':sum(m['flippedTriangles'] for m in metrics),
                   'correctedSurfaceTriangles':sum(m['correctedSurfaceTriangles'] for m in metrics),
                   'maximumSurfaceCorrectionPx':max(m['maximumSurfaceCorrectionPx'] for m in metrics),
                   'jointPinErrorPx':max(m['jointPinErrorPx'] for m in metrics),
                   'chelaWalkingOverlapPixels':sum(int(np.count_nonzero(chela&leg)) for chela in chela_masks for leg in leg_masks),
                   'chelaPairOverlapPixels':int(np.count_nonzero(chela_masks[0]&chela_masks[1]))}


def torso_surface(p,emission=False):
    name='eyes-emission-source.png' if emission else 'body-source-r5-blood.png'
    torso=Image.open(ROOT/name).convert('RGBA')
    # A uniform body registration, anchored by anterior crown and tail socket.
    # All native bone coordinates remain those of the independent plan.
    scale=(704.4-381.4)/(1500.-30.)*1.06
    offset=np.array([512.-512.*scale,548.-(30.+1500.)*.5*scale])
    theta=p['bodyYaw']
    c,s=math.cos(theta),math.sin(theta)
    matrix=np.array([[c,s],[-s,c]])
    shift=project(p['bodyOffset3D'])-ORIGIN
    inv=matrix.T/scale
    translation=(ORIGIN-matrix.T@(ORIGIN+shift)-offset)/scale
    coefficients=(*inv[0],translation[0],*inv[1],translation[1])
    return torso.transform((SIZE,SIZE),Image.Transform.AFFINE,coefficients,Image.Resampling.BICUBIC)


def bones(image,p):
    out=Image.new('RGBA',image.size,'#282e33')
    out.alpha_composite(image)
    d=ImageDraw.Draw(out)
    for ident,limb in p['legs'].items():
        points=project(limb['joints3D'])
        d.line([tuple(q) for q in points],fill='#4fe5df',width=3)
        for x,y in points:
            d.ellipse((x-4,y-4,x+4,y+4),fill='#f6e0a6')
        d.text(tuple(points[1]+[8,0]),ident,fill='white',font=FONT)
    for palp in p['palps'].values():
        points=project(palp['arm']+[palp['palm'],palp['fixedBase'],palp['fixedMid'],palp['fixedTip']])
        d.line([tuple(q) for q in points],fill='#e6aef2',width=3)
        for x,y in points:
            d.ellipse((x-4,y-4,x+4,y+4),outline='white',width=2)
        d.line([tuple(q) for q in project([palp['hinge'],palp['movingMid'],palp['movingTip']])],fill='#e6aef2',width=2)
    d.line([tuple(q) for q in project(p['tail'])],fill='#f0cd70',width=3)
    return out.convert('RGB')


def neutral():
    prepare_sources()
    p=pose(0,'neutral')
    image,metrics=render(p,surfaces())
    image.save(ROOT/'native-neutral.png')
    torso_surface(p,True).save(ROOT/'neutral-eyes.png')
    bones(image,p).save(ROOT/'03-native-bones-on-skin.jpg',quality=94)
    write(ROOT/'native-neutral-metrics.json',metrics)
    print(json.dumps(metrics),flush=True)


def diagnostic(kind):
    inspection=json.loads((ROOT/'inspection.json').read_text())
    if not inspection['nativeSkeletonVisualPass']:
        raise ValueError('Skeleton review must pass first.')
    image=Image.open(ROOT/'native-neutral.png').convert('RGBA')
    out=Image.new('RGBA',image.size,'#282e33')
    out.alpha_composite(image)
    layer=Image.new('RGBA',image.size)
    d=ImageDraw.Draw(layer)
    p=fixed_plan()
    groups=[]
    for ident,chain in p['walkingLegs'].items():
        root,knee,tip=map(np.asarray,chain)
        groups.extend([
            {'id':ident+'-P','origin':(root+(knee-root)*.08).tolist(),'insertion':(root+(knee-root)*.85).tolist(),'width':.040,'function':'proximal protraction/retraction aggregate'},
            {'id':ident+'-J','origin':(root+(knee-root)*.72).tolist(),'insertion':(knee+(tip-knee)*.12).tolist(),'width':.035,'function':'joint flexion/extension and stabilizer aggregate'},
            {'id':ident+'-T','origin':(knee+(tip-knee)*.08).tolist(),'insertion':(knee+(tip-knee)*.78).tolist(),'width':.018,'function':'distal tendon/sheath, bone tip has no muscle belly'}])
    for side,palp in p['pedipalps'].items():
        chain=palp['arm']+[palp['palm']]
        for index,(a,b) in enumerate(zip(chain,chain[1:])):
            groups.append({'id':side+'P-'+str(index+1),'origin':a,'insertion':b,'width':.060 if index<2 else .050,'function':'pedipalp segment motor/stabilizer'})
        groups.append({'id':side+'P-C','origin':palp['palm'],'insertion':palp['hinge'],'width':.055,'function':'manus adductor aggregate, tendon reaches movable finger hinge; fixed finger is rigid cuticle'})
    for index,(a,b) in enumerate(zip(p['tail'][:-2],p['tail'][1:-1])):
        groups.append({'id':'T-'+str(index+1),'origin':a,'insertion':b,'width':.058,'function':'segment flexion/extension aggregate; final ivory sting has no muscle belly'})
    groups.extend([{'id':'B-F','origin':p['bodyAxis'][0],'insertion':p['bodyAxis'][1],'width':.11,'function':'cephalothoracic attachment mass'},
                   {'id':'B-A','origin':p['bodyAxis'][1],'insertion':p['bodyAxis'][-1],'width':.10,'function':'abdominal segment support aggregate'}])
    palette=['#e7806d','#efcd70','#76b7e4']
    for index,group in enumerate(groups):
        a,b=project([group['origin'],group['insertion']])
        color=palette[index%3]
        rgba=tuple(int(color[i:i+2],16) for i in (1,3,5))+(110,)
        width=max(5,int(group['width']*SCALE*2))
        d.line([tuple(a),tuple(b)],fill=rgba,width=width)
        if kind=='muscles':
            d.line([tuple(a),tuple(b)],fill=(255,245,220,240),width=1)
            vector=b-a
            vector/=max(np.linalg.norm(vector),1e-6)
            side=np.array([-vector[1],vector[0]])
            d.polygon([tuple(b),tuple(b-vector*8+side*3),tuple(b-vector*8-side*3)],fill='white')
            d.text(tuple((a+b)*.5+[4,-8]),group['id'],font=FONT,fill='white')
    out.alpha_composite(layer)
    filename='04-native-volumes.jpg' if kind=='volumes' else '05-native-muscle-ids.jpg'
    out.convert('RGB').save(ROOT/filename,quality=94)
    write(ROOT/'functional-groups.json',{'revision':p['revision'],'count':len(groups),'groups':groups,
          'scope':'Prospective functional aggregates of this fictional arthropod, not a medical atlas or a copied count from another species.'})
    print(json.dumps({'pass':kind,'groups':len(groups)}),flush=True)


def frames(kind):
    inspection=json.loads((ROOT/'inspection.json').read_text())
    if not all(inspection.get(key,False) for key in ['nativeSkeletonVisualPass','nativeVolumesVisualPass','nativeMuscleAttachmentsVisualPass','cleanSkinVisualPass']):
        raise ValueError('Complete separate anatomy gates first.')
    surface=surfaces()
    count=24 if kind=='run' else len(ATTACK_MS)
    folder=ROOT/'frames'/kind
    folder.mkdir(parents=True,exist_ok=True)
    maximum=0.
    supports=[]
    for index in range(count):
        t=index/count if kind=='run' else sum(ATTACK_MS[:index])/sum(ATTACK_MS)
        p=pose(t,kind)
        image,stats=render(p,surface)
        image.save(folder/f'{kind}-{index:02d}.png')
        torso_surface(p,True).save(folder/f'{kind}-{index:02d}-eyes.png')
        bones(image,p).save(folder/f'{kind}-{index:02d}-bones.jpg',quality=80)
        maximum=max(maximum,stats['flippedTriangles'])
        supports.append(sum(l['contact'] for l in p['legs'].values()))
        write(ROOT/'measurements'/kind/f'{index:02d}.json',{'pose':p,**stats})
        print(json.dumps({'kind':kind,'frame':index,**stats}),flush=True)
    write(ROOT/(kind+'-export.json'),{'kind':kind,'count':count,'maxFlippedTriangles':maximum,'minContacts':min(supports),
          'durationsMs':[15]*count if kind=='run' else ATTACK_MS,
          'loop':kind=='run','referenceSpeed':STRIDE/DUTY/(NATIVE_SECONDS if kind=='run' else ATTACK_SECONDS/ATTACK_CYCLES)})


if __name__=='__main__':
    if '--prepare-r5' in sys.argv:
        parts.prepare(Surface,write)
    elif '--plan' in sys.argv:
        plan()
    elif '--volumes' in sys.argv:
        diagnostic('volumes')
    elif '--muscles' in sys.argv:
        diagnostic('muscles')
    elif '--frames' in sys.argv:
        frames(sys.argv[sys.argv.index('--frames')+1])
    else:
        neutral()
