from pathlib import Path
from collections import defaultdict
import json,hashlib,math
import numpy as np
from PIL import Image,ImageDraw,ImageFont
HERE=Path(__file__).resolve().parent;OUT=HERE/'RuntimeFullFinal01'
ROOT=HERE.parents[3]/'Concepts/Used/Stalker/Postures05/Scenarios/stalker-final'
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
c=json.loads((OUT/'review-coverage.json').read_text())
profiles={p['key']:p for p in c['profiles']}
default=c['profiles'][0]['key'];morgue=next(k for k in profiles if 'b8ea8741' in k)
selection=defaultdict(set)
windows=[(42,49),(132,143),(223,232),(282,290),(342,349),(372,380),(402,410),(432,440),(462,483),(537,558),(627,650),(687,698),(741,747),(765,772),(777,785),(823,836),(863,877),(931,941),(987,998),(1036,1055),(1081,1114),(1141,1155),(1194,1215),(1223,1237),(1264,1278),(1337,1385),(1404,1449)]
for key in (default,morgue):
 for lo,hi in windows:selection[key].update(range(lo,hi+1))
 # actual direction/stop/teleport events, not guessed scenario middle points
 fs=profiles[key]['frames']
 for i in range(1,1450):
  f=fs[i]['facts'];b=fs[i-1]['facts']
  if f['Teleport'] or f['RestoreRotation'] or f['velocity']!=b['velocity'] and f['scenario'] in ['05-turn-90-and-180','06-start-stop-teleport','16-stagger-hit-stop-and-resume']:
   selection[key].update(range(max(0,i-2),min(1450,i+4)))
for key in profiles:
 # Direct current native checks for every configuration, including worst ranked cuts.
 selection[key].update([0,474,475,476,1225,1226,1227,1340,1341,1342,1352,1353,1354,1407,1408,1409,1420,1421,1422])
font=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',15)
pages=[]
for pi,(key,indices) in enumerate(selection.items()):
 indices=sorted(indices)
 for start in range(0,len(indices),6):
  batch=indices[start:start+6]
  sheet=Image.new('RGB',(1536,1104),(22,24,28));d=ImageDraw.Draw(sheet)
  for slot,i in enumerate(batch):
   path=ROOT/key/f'frame-{i:05d}.png';im=Image.open(path).convert('RGB');a=np.array(im)
   mask=~((a[:,:,1]>220)&(a[:,:,0]<20)&(a[:,:,2]<20));ys,xs=np.where(mask)
   assert xs.min()>=64 and xs.max()<576 and ys.min()>=64 and ys.max()<576,(key,i,int(xs.min()),int(xs.max()),int(ys.min()),int(ys.max()))
   x,y=slot%3*512,slot//3*552;sheet.paste(im.crop((64,64,576,576)),(x,y+40));f=profiles[key]['frames'][i]['facts']
   d.text((x+3,y),f"P{pi} {i} {f['state']} / {f['body']}",font=font,fill='white')
   d.text((x+3,y+19),f"fold{f['fold']:.3f} eye{f['closure']:.3f} phase{f['phase']:.3f} rot{f['visualRotation']:.1f}",font=font,fill='white')
  p=OUT/f'p{pi:02d}-page-{start//6:03d}-native.png';sheet.save(p)
  pages.append({'profile':key,'profileIndex':pi,'page':start//6,'frames':batch,'path':str(p),'sha256':sha(p),'reviewed':False})
 # actual live→dead source pose, hide boundary and torn remains; full640 camera canvas
 for part,indices_dead in enumerate([[1449,1450,1457,1458],[1459,1460,1470,1539]]):
  sheet=Image.new('RGB',(1280,1360),(22,24,28));d=ImageDraw.Draw(sheet)
  for slot,i in enumerate(indices_dead):
   x,y=slot%2*640,slot//2*680;sheet.paste(Image.open(ROOT/key/f'frame-{i:05d}.png').convert('RGB'),(x,y+40));f=profiles[key]['frames'][i]['facts']
   d.text((x+3,y),f"P{pi} {i} {f['state']} / {f['body']} enabled{f['bodyEnabled']}",font=font,fill='white')
   d.text((x+3,y+19),f"death{f['death']:.3f} remains{f['remainsRoots']} kill{f['killCalls']} ortho{f['orthographicSize']}",font=font,fill='white')
  p=OUT/f'p{pi:02d}-death-{part}-native.png';sheet.save(p)
  pages.append({'profile':key,'profileIndex':pi,'page':'death'+str(part),'frames':indices_dead,'path':str(p),'sha256':sha(p),'reviewed':False})
(OUT/'native-page-coverage.json').write_text(json.dumps({'scope':'Native selected transition windows for two distinct gait/timing families, current configuration/ranked-cut checks for all9, full-canvas actual death handoff for all9. Exact presentation-fact equivalence separate from RGB identity.','crop':[64,64,576,576],'scale':1,'pages':pages,'selection':{k:sorted(v) for k,v in selection.items()}},indent=2))
print(json.dumps({'pages':len(pages),'byProfile':{k:len(v) for k,v in selection.items()},'pageGroups':{str(pi):{'key':k,'pages':sum(p['profileIndex']==pi for p in pages)} for pi,k in enumerate(selection)}},indent=2))
