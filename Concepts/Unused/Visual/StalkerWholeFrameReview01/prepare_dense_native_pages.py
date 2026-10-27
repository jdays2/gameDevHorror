"""Native actual runtime pages; fixed crop, no scale/filter/fit."""
from pathlib import Path
from collections import defaultdict
import hashlib,json
import numpy as np
from PIL import Image,ImageDraw,ImageFont
HERE=Path(__file__).resolve().parent
OUT=HERE/'RuntimeDenseFinal01'
ROOT=HERE.parents[3]/'Concepts/Used/Stalker/Postures05/Scenarios/stalker-dense-final'
FONT=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',15)
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def write(p,d):
 t=p.with_suffix(p.suffix+'.tmp');t.write_text(json.dumps(d,indent=2),encoding='utf-8');t.replace(p)
c=json.loads((OUT/'review-coverage.json').read_text(encoding='utf-8'))
profiles={p['key']:p for p in c['profiles']}
pages=[]; equivalences=[]
def render(name,key,indices,members=None,full=False):
 indices=sorted(set(indices));step=4 if full else 6
 for start in range(0,len(indices),step):
  batch=indices[start:start+step]
  size=(1280,1360) if full else (1536,1104)
  sheet=Image.new('RGB',size,(22,24,28));d=ImageDraw.Draw(sheet)
  for slot,i in enumerate(batch):
   p=ROOT/key/f'frame-{i:05d}.png';im=Image.open(p).convert('RGB');f=profiles[key]['frames'][i]['facts']
   if full:x,y=slot%2*640,slot//2*680;tile=im
   else:
    a=np.asarray(im);mask=~((a[:,:,1]>220)&(a[:,:,0]<20)&(a[:,:,2]<20));ys,xs=np.where(mask)
    assert xs.min()>=64 and xs.max()<576 and ys.min()>=64 and ys.max()<576,(key,i,int(xs.min()),int(xs.max()),int(ys.min()),int(ys.max()))
    x,y=slot%3*512,slot//3*552;tile=im.crop((64,64,576,576))
   sheet.paste(tile,(x,y+40))
   d.text((x+3,y),f"{i} {f['body']} {f['state']}",font=FONT,fill='white')
   d.text((x+3,y+19),f"A{f['attack']:.3f} F{f['fold']:.3f} E{f['closure']:.3f} P{f['phase']:.3f}",font=FONT,fill='white')
  path=OUT/f'{name}-{start//step:03d}.png';sheet.save(path)
  pages.append({'name':name,'profile':key,'frames':batch,'path':str(path),'sha256':sha(path),'reviewed':False,'exactScenarioSequenceMembers':members or [key],'fullCanvas':full})
 print(json.dumps({'pageset':name,'profile':key,'frames':len(indices),'pages':(len(indices)+step-1)//step}),flush=True)

# Full new regular-attack runtime sequences; exact decoded-RGB sequence aliases only.
topics=[('normal','13-attack-windup-strike-recovery',824,875),('close','21c-natural-close-attack',1102,1154),('restore','22b-checkpoint-restored-attack',1225,1277)]
for topic,scenario,lo,hi in topics:
 gs=[g for g in c['scenarioGroups'] if g['scenario']==scenario]
 gs.sort(key=lambda g:not any('20hp' in k for k in g['members']))
 for gi,g in enumerate(gs):
  key=next((k for k in g['members'] if '20hp' in k),g['members'][0])
  render(f'{topic}-g{gi:02d}',key,range(lo,hi+1),g['members'])
  equivalences.append({'topic':topic,**g,'representative':key,'note':'Sequence equality covers scenario frames only; native neighbor frames are directly shown for representative, checked separately across all configurations.'})

# New RGB body families need current native checks; prior exact RGB families inherit
# only the previously reviewed scope. All configurations get actual new boundaries.
windows=[(42,49),(132,140),(223,231),(282,289),(342,349),(372,380),(402,414),(462,483),(537,558),(627,650),(687,698),(741,747),(765,785),(823,827),(870,878),(931,941),(987,998),(1036,1055),(1081,1104),(1148,1156),(1194,1205),(1223,1230),(1270,1278)]
for pi,(key,p) in enumerate(profiles.items()):
 if sum(r['exactRgbPrior'] for r in p['frames'])<100:
  indices=[]
  for lo,hi in windows:indices+=list(range(lo,hi+1))
  render(f'current-body-p{pi:02d}',key,indices)
 # Direct boundary neighbors for every source configuration, plus postinterruption
 # fear/body boundary and final current live pose before actual death.
 render(f'boundary-p{pi:02d}',key,[824,825,826,871,872,873,1102,1103,1104,1149,1150,1151,1225,1226,1227,1272,1273,1274,1379,1380,1381,1382,1383,1384,1385,1437,1438,1439,1440,1441,1442,1443,1444,1445,1446,1447,1448,1449])
 render(f'death-p{pi:02d}',key,[1449,1450,1457,1458,1459,1460,1470,1539],full=True)
write(OUT/'native-page-coverage.json',{'status':'PREPARED_NOT_VIEWED','crop':[64,64,576,576],'nativeScale':1,'pages':pages,'exactRgbScenarioEquivalences':equivalences,'limits':'Full regular attack sequences for each exact RGB family plus native transition neighbors; current body windows for two changed RGB families; all9 actual death handoffs. Interrupted attack return is separately reviewed by runtime_review.'})
print(json.dumps({'pages':len(pages),'instances':sum(len(p['frames']) for p in pages)}),flush=True)
