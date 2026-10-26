"""Extract the existing Walk00 paint once. No generated/inpainted animation frames."""
from pathlib import Path
import json, hashlib
import numpy as np
from PIL import Image, ImageDraw, ImageFont, ImageFilter
from collections import deque

ROOT = Path(__file__).resolve().parent
PROJECT = ROOT.parents[3]
SOURCE = PROJECT/'Assets/EmergencyShift/Art/Enemies/Stalker/PaleFlesh/walk-sheet.png'
ART = Image.open(SOURCE).convert('RGBA').crop((30,84,388,490))
PIVOT = np.array([187.,200.])  # serialized sprite uvTransform: sheet (217, 740), top-left crop
PPU = 224.
LIMBS = [
    ('LF', [147,156], [84,81], [150,7], .46,.46, 1),
    ('RF', [226,158], [299,94], [244,15], .46,.46, -1),
    ('LM', [134,201], [53,189], [8,265], .40,.41, -1),
    ('RM', [241,206], [323,210], [349,287], .40,.41, 1),
    ('LR', [141,252], [83,333], [138,399], .48,.43, -1),
    ('RR', [232,252], [287,326], [246,394], .48,.43, 1),
]

def connected_skin(rgba, knee):
    """Keep the leg attached to its knee, with its antialiased outer edge.

    Voronoi ownership can steal a few disconnected forehead/torso pixels. They
    belong to the stationary torso, not to a moving appendage.
    """
    alpha=rgba[...,3]>8
    keep=np.zeros(alpha.shape,dtype=bool)
    y,x=int(knee[1]),int(knee[0])
    queue=deque([(y,x)])
    keep[y,x]=True
    while queue:
        y,x=queue.popleft()
        for dy,dx in [(a,b) for a in (-1,0,1) for b in (-1,0,1) if a or b]:
            ny,nx=y+dy,x+dx
            if 0<=ny<alpha.shape[0] and 0<=nx<alpha.shape[1] and alpha[ny,nx] and not keep[ny,nx]:
                keep[ny,nx]=True
                queue.append((ny,nx))
    edge=np.array(Image.fromarray(keep.astype('uint8')*255).filter(ImageFilter.MaxFilter(3)))>0
    result=rgba.copy()
    result[~edge,3]=0
    return result

def plan():
    data = dict(species='Stalker: six paired appendages, one knee and rigid distal spike per limb',
                source=str(SOURCE.relative_to(PROJECT)), sourceSha256=hashlib.sha256(SOURCE.read_bytes()).hexdigest(),
                crop=[30,84,388,490],pivot=PIVOT.tolist(),ppu=PPU,
                projection='strict overhead XY; Z is height above floor, not depth in the image',
                caveat='Heights/true lengths are explicit rig design parameters; a painting does not measure hidden anatomy.',
                bodyHeight=.13, kneePlane='stable XY bend side from source chain; Z interpolates hip/ground height',
                limbs=[dict(id=n,hip=h,knee=k,tip=t,upperLength=u,lowerLength=l,pole=p,
                            muscleGroups=[n+' proximal actuator',n+' joint stabilizer',n+' distal tendon sleeve']) for n,h,k,t,u,l,p in LIMBS])
    (ROOT/'anatomy.json').write_text(json.dumps(data,indent=2))
    canvas=Image.new('RGBA',ART.size,(29,33,38,255)); canvas.alpha_composite(ART)
    d=ImageDraw.Draw(canvas)
    for n,h,k,t,u,l,p in LIMBS:
        d.line([tuple(h),tuple(k),tuple(t)],fill='#52e9ff',width=2)
        for q in (h,k,t):
            d.ellipse((q[0]-4,q[1]-4,q[0]+4,q[1]+4),fill='#ffdf66')
        d.text((k[0]+5,k[1]),n,fill='white')
    canvas.convert('RGB').save(ROOT/'skeleton-review.png')

def extract():
    # One connected painted surface per leg. The former two rigid pieces copied
    # the knee twice; their rotating outlines produced visible seams in motion.
    yy,xx=np.mgrid[:ART.height,:ART.width]; pixels=np.stack([xx,yy],axis=-1)
    rgba=np.asarray(ART).copy()
    lines=[]
    for n,h,k,t,u,l,p in LIMBS:
        for a,b in [(h,k),(k,t)]:
            a=np.array(a); b=np.array(b); v=b-a
            f=np.clip(np.sum((pixels-a)*v,axis=-1)/np.dot(v,v),0,1)
            lines.append(np.linalg.norm(pixels-(a+f[...,None]*v),axis=-1))
    distances=np.stack(lines,axis=-1)
    owner=np.argmin(distances,axis=-1)
    # The triangular torso covers root joints; keep its original painted contour.
    bodyMask=Image.new('L',ART.size); bd=ImageDraw.Draw(bodyMask)
    bd.polygon([(179,125),(202,125),(220,165),(252,229),(240,249),(224,256),(141,256),(120,243),(113,222),(147,160)],fill=255)
    body=np.array(bodyMask)>0
    layers=[]
    base=rgba.copy();base[~body,3]=0;layers.append(Image.fromarray(base))
    muscle=Image.new('RGBA',ART.size,(29,33,38,255));muscle.alpha_composite(ART); md=ImageDraw.Draw(muscle,'RGBA')
    for index,(n,h,k,t,u,l,p) in enumerate(LIMBS):
        h=np.array(h);k=np.array(k);t=np.array(t)
        for part in range(2):
            own=owner==index*2+part
            a,b=(h,k) if part==0 else (k,t)
            axis=(b-a)/np.linalg.norm(b-a)
            own &= np.sum((pixels-a)*axis,axis=-1)>-8
            # Both pieces contain the round knee cap, hiding the cut in rotations.
            own |= np.linalg.norm(pixels-k,axis=-1)<11
            if part==0:
                own |= np.linalg.norm(pixels-h,axis=-1)<14
            cut=rgba.copy();cut[~own|body,3]=0
            # Root artwork must overlap the torso by 10px, without a gap.
            if part==0:
                root=np.linalg.norm(pixels-h,axis=-1)<15
                cut[root]=rgba[root]
            layers.append(Image.fromarray(cut))
        whole=rgba.copy()
        own=(owner//2==index)&~body
        own |= np.linalg.norm(pixels-h,axis=-1)<15
        whole[~own,3]=0
        layers[index*2+1]=Image.fromarray(connected_skin(whole,k))
        md.line([tuple(h),tuple(k)],fill=(250,112,90,120),width=15)
        md.ellipse((k[0]-10,k[1]-10,k[0]+10,k[1]+10),fill=(245,192,60,120))
        md.line([tuple(k),tuple(t)],fill=(84,167,245,100),width=9)
        md.text(tuple((h+k)*.5),n+' 1/2/3',fill='white')
    muscle.convert('RGB').save(ROOT/'muscle-review.png')
    # Pixels removed by the proximal half-plane belong to the torso attachment.
    active=[layers[0]]+layers[1::2]
    covered=np.max(np.stack([np.asarray(layer)[...,3] for layer in active]),axis=0)
    missing=(rgba[...,3]>0)&(covered==0)
    torso=np.array(layers[0]);torso[missing]=rgba[missing];layers[0]=Image.fromarray(torso)
    atlas=Image.new('RGBA',(ART.width*4,ART.height*4))
    for i,layer in enumerate(layers):atlas.alpha_composite(layer,(i%4*ART.width,i//4*ART.height))
    out=PROJECT/'Assets/EmergencyShift/Art/Enemies/Stalker/Locomotion02'
    out.mkdir(parents=True,exist_ok=True)
    atlas.save(out/'rig-atlas.png')
    # Reassemble neutral source to check masks have neither holes nor stray parts.
    check=Image.new('RGBA',ART.size)
    for layer in layers[1::2]+layers[:1]:check.alpha_composite(layer)
    bg=Image.new('RGBA',ART.size,(29,33,38,255));bg.alpha_composite(check);bg.convert('RGB').save(ROOT/'surface-review.png')
    (ROOT/'extraction.json').write_text(json.dumps(dict(layers=13,atlasSize=atlas.size,
       bounds=[list(layer.getbbox()) for layer in layers],
       notes='Six continuous leg surfaces in odd atlas cells; even cells retained but unused. One knee painting per leg. Torso last. No independent frame generation.',
       missingOpaquePixels=int(np.sum((rgba[...,3]>220)&(np.array(check)[...,3]<220)))),indent=2))

if __name__=='__main__':
    import sys
    plan()
    if '--extract' in sys.argv:extract()
