"""Record the reviewed capture, its loop seam, and actual active skin masks."""
from pathlib import Path
from collections import deque
import json
import hashlib
import numpy as np
from PIL import Image, ImageDraw, ImageFont

ROOT=Path(__file__).resolve().parent
FAMILY=ROOT.parent
PROJECT=FAMILY.parents[3]
atlas_path=PROJECT/'Assets/EmergencyShift/Art/Enemies/Stalker/Locomotion02/rig-atlas.png'
atlas=Image.open(atlas_path).convert('RGBA')
parts=[]
assembled=Image.new('RGBA',(358,406))
for layer in [1,3,5,7,9,11,0]:
    skin=atlas.crop((layer%4*358,layer//4*406,(layer%4+1)*358,(layer//4+1)*406))
    assembled.alpha_composite(skin)
    if layer==0:
        continue
    alpha=np.array(skin)[...,3]>20
    visited=np.zeros(alpha.shape,dtype=bool)
    sizes=[]
    for y,x in zip(*np.where(alpha)):
        if visited[y,x]:
            continue
        queue=deque([(int(y),int(x))])
        visited[y,x]=True
        count=0
        while queue:
            yy,xx=queue.popleft()
            count+=1
            for dy in (-1,0,1):
                for dx in (-1,0,1):
                    ny,nx=yy+dy,xx+dx
                    if 0<=ny<406 and 0<=nx<358 and alpha[ny,nx] and not visited[ny,nx]:
                        visited[ny,nx]=True
                        queue.append((ny,nx))
        sizes.append(count)
    parts.append(dict(layer=layer,components=len(sizes),opaquePixelCounts=sorted(sizes,reverse=True)))
source=Image.open(PROJECT/'Assets/EmergencyShift/Art/Enemies/Stalker/PaleFlesh/walk-sheet.png').convert('RGBA').crop((30,84,388,490))
missing=int(np.sum((np.array(source)[...,3]>220)&(np.array(assembled)[...,3]<220)))
box=(700,70,1240,670)
first=Image.open(FAMILY/'PreviewFrames/close-100.png').crop(box)
repeated=Image.open(FAMILY/'PreviewFrames/close-150.png').crop(box)
difference=np.array(first).astype(float)-np.array(repeated).astype(float)
strip=Image.new('RGB',(1320,265),(22,27,31))
draw=ImageDraw.Draw(strip)
font=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',16)
for index,frame in enumerate([147,148,149,100,101,102]):
    shot=Image.open(FAMILY/'PreviewFrames'/f'close-{frame:03d}.png').crop(box).resize((220,240))
    strip.paste(shot,(index*220,25))
    draw.text((index*220+8,4),f'#{frame}',fill='white',font=font)
strip.save(ROOT/'walk-loop-join.jpg',quality=95)
report=dict(captureSha256=hashlib.sha256((FAMILY/'close.mp4').read_bytes()).hexdigest(),
    framesVisuallyReviewed=[0,479],contactSheets=[f'after-{page:02d}.jpg' for page in range(20)],
    reviewed='All 480 poses on numbered sheets; knees/roots at full size; gameplay captures and 1x viewer.',
    activeLegMasks=parts,missingOpaquePixelsInActualActiveAssembly=missing,
    walkLoop=dict(firstFrame=100,exclusiveEndFrame=150,sourceFps=60,fullCycles=1,
        repeatedPoseRgbRms=float(np.sqrt(np.mean(difference*difference))),
        notes='Compare full-period frames 100/150 after camera translation by 1 unit; inspect join 149->100 separately.'),
    fixes=['extra recovery steps during straight travel','duplicated knee contours','disconnected torso pixels in leg masks',
        'folded skin triangles','unstable knee plane shortening spikes','unsafe random reset phase'],
    artisticAcceptance='Pending owner review; technical checks do not grant acceptance.')
(ROOT/'visual-review.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
before=json.loads((ROOT/'cadence-before.json').read_text())
before['implementation']='f426876, measured before this correction'
before['results']=[row for row in before['results'] if abs(row['distance']-.68)<.001]
(ROOT/'cadence-before.json').write_text(json.dumps(before,indent=2),encoding='utf-8')
print(json.dumps(dict(missingOpaquePixels=missing,masks=[row['components'] for row in parts],loopRms=report['walkLoop']['repeatedPoseRgbRms'])))
