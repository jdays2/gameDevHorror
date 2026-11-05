"""Build ONE closed rest-volume mesh, not cutout legs or per-frame painting.

Run only after the master bone overlay was actually reviewed. Mask determines
only the topology of the continuous rest volume. The full source RGBA is kept
unchanged as one material image; there are no per-limb textures or polar UV.
"""
from pathlib import Path
import json, hashlib
from collections import deque, Counter
import numpy as np
from PIL import Image, ImageFilter, ImageDraw

ROOT=Path(__file__).resolve().parent
PROJECT=ROOT.parents[4]
VALID=PROJECT/'Docs/Validation/EnemyAnimationAlternativesTask010/TextureStable02'
data=json.loads((ROOT/'skeleton.json').read_text(encoding='utf-8'))
master=Image.open(ROOT/'master.png').convert('RGBA')
assert hashlib.sha256((ROOT/'master.png').read_bytes()).hexdigest()==data['masterSHA256']
alpha=master.getchannel('A')
# Surface extends a few transparent texels beyond art; never alpha-trims the PNG.
mask=alpha.point(lambda a:255 if a>=32 else 0).filter(ImageFilter.MaxFilter(7))
small=np.array(mask.resize((320,320),Image.Resampling.NEAREST))>0
# Ignore isolated alpha dust; maintain one connected geometric component.
remaining=set(map(tuple,np.argwhere(small)))
components=[]
while remaining:
    start=remaining.pop();component={start};q=deque([start])
    while q:
        y,x=q.popleft()
        for n in ((y-1,x),(y+1,x),(y,x-1),(y,x+1)):
            if n in remaining:remaining.remove(n);component.add(n);q.append(n)
    components.append(component)
cells=max(components,key=len)
corners=sorted({(y+dy,x+dx) for y,x in cells for dy,dx in ((0,0),(0,1),(1,0),(1,1))})
index={p:i for i,p in enumerate(corners)}
px=np.array([(x*2,y*2) for y,x in corners],dtype=float)
xy=np.c_[(px[:,0]-320)/224,(320-px[:,1])/224]

def smoother(x):
    x=np.clip(x,0,1);return x*x*x*(10+x*(-15+6*x))

names=list(data['rest']);rest=data['rest']
segment_dist=[];segment_s=[];segment_height=[]
for name in names:
    h,k,t=map(np.array,(rest[name]['hip'],rest[name]['knee'],rest[name]['tip']))
    dist=[];param=[];height=[]
    for offset,(a,b) in enumerate(((h,k),(k,t))):
        v=b[:2]-a[:2]
        u=np.clip(((xy-a[:2])*v).sum(axis=1)/np.dot(v,v),0,1)
        p=a[:2]+u[:,None]*v
        dist.append(np.linalg.norm(xy-p,axis=1));param.append(offset+u)
        height.append(a[2]+u*(b[2]-a[2]))
    choice=np.argmin(np.stack(dist),axis=0);ix=np.arange(len(px))
    segment_dist.append(np.stack(dist)[choice,ix]);segment_s.append(np.stack(param)[choice,ix]);segment_height.append(np.stack(height)[choice,ix])
choice=np.argmin(np.stack(segment_dist),axis=0);ix=np.arange(len(px))
s=np.stack(segment_s)[choice,ix];center=np.stack(segment_height)[choice,ix]
rho=np.sqrt(((px[:,0]-320)/56)**2+((px[:,1]-310)/63)**2)
# Blend neighbouring chain influences continuously around shared roots. The
# previous hard nearest-limb owner plus s/.24 caused an LM contour tooth.
dist=np.stack(segment_dist);params=np.stack(segment_s)
soft=np.exp(-((dist-dist.min(axis=0))/.04)**2)
soft/=soft.sum(axis=0)
limb_move=soft*smoother((params-.10)/.45)*smoother((rho[None,:]-.9)/.35)
move=limb_move.sum(axis=0)
lower=smoother((params-.84)/.32)
weights=np.zeros((len(px),13),dtype=np.float32)
weights[:,0]=1-move
for i in range(6):
    weights[:,1+2*i]=limb_move[i]*(1-lower[i]);weights[:,2+2*i]=limb_move[i]*lower[i]
center=.13*(1-move)+center*move
# Closed lenticular soft volume. Thickness is a proposal derived from width,
# not unseen anatomy recovery. Raised core / narrower limbs / solid lower wall.
binary=Image.fromarray((np.array(mask)>0).astype('uint8')*255)
distance=np.zeros((640,640),dtype=float)
for n in range(1,65):
    inside=np.array(binary)>0
    if not inside.any():break
    distance[inside]+=1
    binary=binary.filter(ImageFilter.MinFilter(3))
nearest=np.clip(np.rint(px).astype(int),0,639)
depth=distance[nearest[:,1],nearest[:,0]]
half=np.minimum(.105,.014*np.sqrt(np.maximum(depth,1)))
top=np.c_[xy,center+half];bottom=np.c_[xy,np.maximum(.002,center-half)]
vertices=np.concatenate([top,bottom]);weights=np.concatenate([weights,weights])
uv0=np.c_[px[:,0]/640,1-px[:,1]/640];uv=np.concatenate([uv0,uv0])
n=len(corners);faces=[];edge_count=Counter()
for y,x in sorted(cells):
    q=[index[(y,x)],index[(y+1,x)],index[(y+1,x+1)],index[(y,x+1)]]
    faces.append(q);faces.append([v+n for v in q[::-1]])
    for a,b in zip(q,q[1:]+q[:1]):edge_count[(a,b)]+=1
for (a,b) in edge_count:
    if (b,a) not in edge_count:faces.append([b,a,a+n,b+n])
faces=np.array(faces,dtype=np.int32)
edge_usage=Counter(tuple(sorted((int(a),int(b)))) for f in faces for a,b in zip(f,np.roll(f,-1)))
assert all(v==2 for v in edge_usage.values()),'nonmanifold edge'
assert np.max(abs(weights.sum(axis=1)-1))<1e-6
np.savez_compressed(ROOT/'rest-surface.npz',vertices=vertices.astype(np.float32),faces=faces,uv=uv.astype(np.float32),weights=weights)
report={'oneClosedConnectedSurface':True,'vertexCount':len(vertices),'quadCount':len(faces),
    'surfaceRevision':2,'rootWeights':'continuous distance-blend among chains; proximal blend .10..55',
    'allEdgesTwoFaces':True,'largestAlphaComponentCells':len(cells),'discardedAlphaDustCells':sum(map(len,components))-len(cells),
    'materialImageCount':1,'uvInvariantAcrossFrames':True,'perFrameImageGeneration':False,'cutoutLimbTextures':False,
    'method':'Continuous closed lens volume, rest-projected whole-image UV, 13 weighted bones. Orthographic-only prototype, not sculpted all-angle anatomy.',
    'masterSHA256':data['masterSHA256'],'weightNames':['Torso']+[f'{name}_{part}' for name in names for part in ('upper','lower')],
    'heightRange':[float(vertices[:,2].min()),float(vertices[:,2].max())],
    'weightsRange':[float(weights.min()),float(weights.max())],'maxWeightSumError':float(abs(weights.sum(axis=1)-1).max()),
    'allAngleBacksideArtAuthored':False,'physicalBalanceOrMusclesValidated':False}
(VALID/'surface-structure.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
# Separate functional groups/attachments: three per limb, SAME normalized bounds
# and IDs every frame. Broad source shading remains intact; no atlas overlays.
groups=[]
over=Image.new('RGBA',(640,640),(24,32,38,255));over.alpha_composite(master)
d=ImageDraw.Draw(over)
colors=[(70,220,180,255),(250,180,60,255),(120,150,255,255)]
for i,name in enumerate(names):
    r=rest[name]
    pts=[np.array([320+r[k][0]*224,320-r[k][1]*224]) for k in ('hip','knee','tip')]
    h,k,t=pts
    for group,part,start,end in [(0,'upper_flesh',h,k),(1,'knee_sleeve',k,k+.32*(t-k)),(2,'distal_tendon_to_rigid_ivory',k+.32*(t-k),t)]:
        gid=f'{name}.{group+1}'
        d.line([tuple(start),tuple(end)],fill=colors[group],width=4)
        mid=(start+end)/2;d.text(tuple(mid+[3,3]),gid,fill=colors[group])
        groups.append({'id':gid,'limb':name,'aggregate':part,'origin':start.tolist(),'attachment':end.tolist(),
                       'fiberDirection':'origin->attachment','hiddenMuscleCountKnown':False,
                       'template':'3 functional regions per limb; not 3 asserted medical muscles',
                       'persistentNormalizedBounds':[[0,1],[0,.32],[.32,1]][group]})
over.convert('RGB').save(VALID/'master-functional-groups.png')
(ROOT/'functional-groups.json').write_text(json.dumps({'status':'Proposed functional checks, skin hides boundaries; not a medical muscle PASS',
    'groups':groups},indent=2)+'\n',encoding='utf-8')
print(json.dumps(report,indent=2))
