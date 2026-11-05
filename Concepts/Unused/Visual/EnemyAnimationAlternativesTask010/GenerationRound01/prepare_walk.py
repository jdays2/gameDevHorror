from pathlib import Path
import json, math
import numpy as np
from PIL import Image,ImageDraw,ImageFont
R=Path(__file__).resolve().parent
base=json.loads((R/'anatomy-reference.json').read_text())
frames=[]
L=54.0
def smooth(q):return q*q*q*(10+q*(-15+6*q))
for frame in range(6):
    parts=[]
    for limb in base['points']:
        hip,knee,tip=[np.array(limb[k],dtype=float) for k in ('hip','knee','tip')]
        tripod=0 if limb['id'] in ['LF','RM','LR'] else 1
        u=(frame/6+tripod*.5)%1
        q=(u-.6)/.4
        dy=L*(u-.3) if u<.6 else L*(u-.3-smooth(q))
        foot=tip+np.array([0,dy])
        a,b=np.linalg.norm(knee-hip),np.linalg.norm(tip-knee)
        delta=foot-hip;dist=np.linalg.norm(delta);axis=delta/dist
        along=(a*a-b*b+dist*dist)/(2*dist)
        perpendicular=np.array([-axis[1],axis[0]])
        sign=1 if np.dot(knee-hip,perpendicular)>0 else -1
        newknee=hip+along*axis+sign*math.sqrt(max(0,a*a-along*along))*perpendicular
        parts.append({'id':limb['id'],'hip':hip.tolist(),'knee':newknee.tolist(),'tip':foot.tolist(),
                      'planted':u<.6,'heightGuide':0 if u<.6 else .06*math.sin(math.pi*q)**2,
                      'projectedUpper':float(a),'projectedLower':float(b)})
    frames.append({'frame':frame,'phase':frame/6,'points':parts})
sheet=Image.new('RGB',(1728,1152),'#182027')
for f in frames:
    image=Image.new('RGB',(576,576),'#182027');d=ImageDraw.Draw(image)
    d.polygon([(237,328),(339,328),(288,216)],fill='#465462')
    for part in f['points']:
        chain=[tuple(part[k]) for k in ('hip','knee','tip')]
        d.line(chain,fill='#a9d8f5',width=3)
        for x,y in chain:d.ellipse((x-3,y-3,x+3,y+3),fill='#ffe391')
    image.save(R/f'walk-bones-{f["frame"]:02}.png')
    sheet.paste(image,((f['frame']%3)*576,(f['frame']//3)*576))
sheet.save(R/'walk-bones-six.png')
(R/'walk-plan.json').write_text(json.dumps({'status':'VISUAL_GENERATION_CONDITIONING_ONLY; projected 2D lengths, hidden true anatomy unknown',
 'frameCount':6,'frameDurationMs':125,'loop':True,'canvas':[576,576],'origin':[288,288],'stridePixels':54,
 'caveat':'Not a gait/physics/runtime PASS. Equal projected source chains are preserved per limb; true paired lengths are not recovered from a top image. No owner acceptance claimed.',
 'frames':frames},indent=2),encoding='utf-8')
print('Prepared six independent whole-frame pose guides.')
