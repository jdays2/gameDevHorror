"""TASK-010 fixed-material offline volume test. Does not repaint source pixels.

Stage 1: independent fixed-length 3D skeleton / evidence, before mesh authoring.
Stage 2 (later): a closed surface using one unchanged C00 image as rest UV.
Heights are an explicit proposal, NOT recovered Stalker anatomy.
"""
from pathlib import Path
import json, math, hashlib
import numpy as np
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parent
PROJECT = ROOT.parents[4]
VALID = PROJECT/'Docs/Validation/EnemyAnimationAlternativesTask010/TextureStable02'
PREV = ROOT.parent/'GenerationRound01'
VALID.mkdir(parents=True, exist_ok=True)
raw=Image.open(ROOT/'master-correction-r6-raw.png').convert('RGBA')
scaled=raw.resize((640,640),Image.Resampling.LANCZOS)
arr=np.asarray(scaled)
yy,xx=np.mgrid[:640,:640]
mask=(abs(xx-320)<24)&(abs(yy-320)<24)&(arr[:,:,:3].max(axis=2)<35)&(arr[:,:,3]>250)
cx,cy=float(xx[mask].mean()),float(yy[mask].mean())
shift=(round(320-cx),round(320-cy))
registered=Image.new('RGBA',(640,640));registered.alpha_composite(scaled,shift)
MASTER=ROOT/'master.png'
registered.save(MASTER)
PPU=224; SIZE=640; N=24

def dump(path,data):
    path.write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')

def world(xy,z): return np.array([(xy[0]-320)/PPU,(320-xy[1])/PPU,z],float)
def pixel(p): return [320+p[0]*PPU,320-p[1]*PPU]
def unit(a): return a/np.linalg.norm(a)
def smooth(x): return x*x*x*(10+x*(-15+6*x))

neutral=json.loads((PREV/'anatomy-reference.json').read_text(encoding='utf-8'))['points']
restguide=json.loads((PREV/'walk-plan.json').read_text(encoding='utf-8'))['frames'][0]['points']
rest={}
for neutral_limb,g in zip(neutral,restguide):
    name=g['id']
    h=world(np.array(g['hip'])+32,.13)
    k=world(np.array(g['knee'])+32,.10)
    t=world(np.array(g['tip'])+32,.015)
    rest[name]={'hip':h.tolist(),'knee':k.tolist(),'tip':t.tolist(),
                'neutralTipPixel':(np.array(neutral_limb['tip'])+32).tolist(),
                'lengths':[float(np.linalg.norm(k-h)),float(np.linalg.norm(t-k))]}

def pose(phase):
    points=[]
    for name,r in rest.items():
        h,k0,t0=map(np.array,(r['hip'],r['knee'],r['tip']))
        offset=0 if name in ('LF','RM','LR') else .5
        u=(phase+offset)%1
        planted=u<.6
        q=0 if planted else (u-.6)/.4
        dy=54*(u-.3-(0 if planted else smooth(q)))
        xy=np.array(r['neutralTipPixel'])+[0,dy]
        t=world(xy,.015+(0 if planted else .06*math.sin(math.pi*q)**2))
        a,b=r['lengths'];d=np.linalg.norm(t-h);axis=unit(t-h)
        if not abs(a-b)<d<a+b: raise ValueError(('unreachable',name,phase,d))
        c=(a*a-b*b+d*d)/(2*d)
        pole=k0-h;pole=unit(pole-axis*np.dot(pole,axis))
        k=h+c*axis+math.sqrt(max(0,a*a-c*c))*pole
        points.append({'id':name,'hip':h.tolist(),'knee':k.tolist(),'tip':t.tolist(),
                       'planted':planted,'phase':u})
    return points

frames=[{'frame':i,'phase':i/N,'points':pose(i/N)} for i in range(N)]
length_error=0;min_knee=1;stance_error=0
for phase in np.linspace(0,1,241):
    for p in pose(float(phase)):
        h,k,t=map(np.array,(p['hip'],p['knee'],p['tip']))
        length_error=max(length_error,abs(np.linalg.norm(k-h)-rest[p['id']]['lengths'][0]),abs(np.linalg.norm(t-k)-rest[p['id']]['lengths'][1]))
        min_knee=min(min_knee,k[2])
        if p['planted']:
            # Foot world Y = local Y + root travel, fixed during each stance.
            expected=(320-rest[p['id']]['neutralTipPixel'][1])/PPU+.3*54/PPU
            offset=0 if p['id'] in ('LF','RM','LR') else .5
            expected += (math.floor(float(phase)+offset)-offset)*54/PPU
            stance_error=max(stance_error,abs(t[1]+float(phase)*54/PPU-expected))
closure=max(abs(np.array([list(p.values())[1:4] for p in pose(0)],dtype=float)-np.array([list(p.values())[1:4] for p in pose(1)],dtype=float)).flat)
dump(ROOT/'skeleton.json',{'status':'PROPOSED HEIGHTS, fixed rest guide; not owner anatomy or runtime PASS',
    'master':str(MASTER.relative_to(PROJECT)),'masterSHA256':hashlib.sha256(MASTER.read_bytes()).hexdigest(),
    'canvas':[640,640],'origin':[320,320],'ppu':PPU,'frameCount':N,'cycleMs':750,
    'soleSkinThickness':.015,'rootHeight':.13,'kneeRestHeight':.10,'swingLift':.06,
    'stanceDuty':.6,'stridePixels':54,'rest':rest,'frames':frames})
dump(ROOT/'master-provenance.json',{'tool':'built-in image_gen','prompt':'master-correction-r6-prompt.txt',
    'raw':'master-correction-r6-raw.png','rawSHA256':hashlib.sha256((ROOT/'master-correction-r6-raw.png').read_bytes()).hexdigest(),
    'defaultCopySource':'C:/Users/Mark/.codex/generated_images/01a1a841-d038-78c1-a0f9-34060430b475/exec-1ce9834f-c6a0-4d2f-aca3-35e8a9c9547a.png',
    'wholeFrameScale':640/raw.width,'wholeFrameIntegerShift':shift,'sourcePupilCenterAfterScale':[cx,cy],
    'noLocalLimbTransforms':True,'masterSHA256':hashlib.sha256(MASTER.read_bytes()).hexdigest()})
dump(VALID/'skeleton-check.json',{'samples':241,'maxLengthError':length_error,'minKneeHeight':min_knee,
    'maxStanceWorldFootError':stance_error,'closureError':float(closure),'runtimeTested':False,
    'heightsRecoveredFromImage':False,'paired3DLengthsOwnerApproved':False})

master=Image.open(MASTER).convert('RGBA')
def composite(im,bg):
    out=Image.new('RGBA',im.size,bg);out.alpha_composite(im);return out.convert('RGB')

# Bone overlays retain the old independent guide, never fit it to the image.
bones=composite(master,(24,32,38,255));draw=ImageDraw.Draw(bones)
for r in rest.values():
    pts=[tuple(pixel(np.array(r[k]))) for k in ('hip','knee','tip')]
    draw.line(pts,fill=(50,240,255),width=2)
    for p in pts:draw.ellipse((p[0]-3,p[1]-3,p[0]+3,p[1]+3),fill=(245,230,40))
bones.save(VALID/'master-bones.png')
sheet=Image.new('RGB',(4*640,6*660),(24,32,38));sd=ImageDraw.Draw(sheet)
for f in frames:
    im=Image.new('RGB',(640,640),(24,32,38));d=ImageDraw.Draw(im)
    for p in f['points']:
        pts=[tuple(pixel(np.array(p[k]))) for k in ('hip','knee','tip')]
        d.line(pts,fill=(50,240,255) if p['planted'] else (240,170,60),width=3)
        for pt in pts:d.ellipse((pt[0]-4,pt[1]-4,pt[0]+4,pt[1]+4),fill=(240,235,230))
    x=f['frame']%4*640;y=f['frame']//4*660
    sheet.paste(im,(x,y+20));sd.text((x+10,y+3),str(f['frame']),fill='white')
sheet.save(VALID/'skeleton-all.png')
views=Image.new('RGB',(1000,600),(24,32,38));d=ImageDraw.Draw(views)
for index,axes in enumerate(((0,1),(1,2),(0,2))):
    cx=170+330*index;cy=300
    for r in rest.values():
        pts=[]
        for key in ('hip','knee','tip'):
            p=r[key];pts.append((cx+p[axes[0]]*130,cy-p[axes[1]]*250))
        d.line(pts,fill=(60,220,240),width=3)
        for x,y in pts:d.ellipse((x-4,y-4,x+4,y+4),fill='white')
    d.text((cx-80,20),('TOP','SIDE Y/Z','FRONT X/Z')[index],fill='white')
    if index:d.line((cx-150,300,cx+150,300),fill=(180,180,180))
views.save(VALID/'skeleton-projections.png')

# Static ROIs are fixed before seeing the new result. All are within stable body/eye.
rois={'eye':[309,309,331,331], 'forebody':[309,277,331,294], 'body_left':[287,319,299,337],
      'body_right':[341,319,353,337], 'body_back':[304,342,337,354]}
old=[np.asarray(Image.open(PREV/f'Selection/C-{i:02}.png').convert('RGBA')) for i in range(6)]
metrics={};crops=Image.new('RGB',(6*160,len(rois)*140),(24,32,38));cd=ImageDraw.Draw(crops)
for j,(name,box) in enumerate(rois.items()):
    x0,y0,x1,y1=box
    a=np.array([im[y0:y1,x0:x1,:3] for im in old],dtype=float)
    diffs=np.abs(a-np.roll(a,1,axis=0))
    metrics[name]={'box':box,'meanAdjacentRGBDifference_0_255':float(diffs.mean()),'maxAdjacentRGBDifference':float(diffs.max()),
                   'allPixelsOpaque':bool(all((im[y0:y1,x0:x1,3]>=250).all() for im in old))}
    for i,im in enumerate(old):
        crop=Image.fromarray(im).crop(box).resize((130,100),Image.Resampling.NEAREST)
        crops.paste(composite(crop,(24,32,38,255)),(i*160,j*140+35));cd.text((i*160+4,j*140+5),f'{name} C{i:02}',fill='white')
dump(VALID/'baseline-texture.json',{'method':'Fixed static torso ROIs, pupil-registered C00..05. Adjacent RGB MAE including loop. Not optical flow or anatomy proof.','rois':metrics})
crops.save(VALID/'before-fixed-body-crops.png')
print(json.dumps({'skeleton':json.loads((VALID/'skeleton-check.json').read_text()),'baseline':metrics},indent=2))
