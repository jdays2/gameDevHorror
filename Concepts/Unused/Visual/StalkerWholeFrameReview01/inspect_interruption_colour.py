from pathlib import Path
import json
import numpy as np
from PIL import Image,ImageDraw

HERE=Path(__file__).resolve().parent
ROOT=HERE.parents[3]/'Concepts/Used/Stalker/Postures05/Scenarios/stalker-interruption-after'
OUT=HERE/'RuntimeInterruptionAfter01'
rows=[]
for key in ['MechanicsSandbox-stalker-20hp-default-stationary-990b0376','MechanicsSandbox-stalker-2hp-default-stationary-89db3ee7','MorgueRouteTest-stalker-morgueroutetest-default-live-b8ea8741']:
    m=json.loads((ROOT/key/'manifest.json').read_text(encoding='utf-8-sig'))
    a=np.array(Image.open(ROOT/key/'frame-00066.png').convert('RGB'))
    b=np.array(Image.open(ROOT/key/'frame-00067.png').convert('RGB'))
    different=np.any(a!=b,2)
    ys,xs=np.where(different)
    rows.append({'profile':key,'changed':int(different.sum()),'bbox':[int(xs.min()),int(ys.min()),int(xs.max()+1),int(ys.max()+1)],'maxChannelDelta':int(np.abs(a.astype(int)-b.astype(int)).max()),'factsChanged':{k:[m['frames'][66].get(k),m['frames'][67].get(k)] for k in m['frames'][66] if m['frames'][66].get(k)!=m['frames'][67].get(k)}})
    if '2hp-' in key:
        sheet=Image.new('RGB',(1280,680))
        sheet.paste(Image.fromarray(a),(0,40));sheet.paste(Image.fromarray(b),(640,40))
        d=ImageDraw.Draw(sheet);d.text((5,5),'2HP frame66 strike10 before hit',(255,255,255));d.text((645,5),'2HP frame67 strike10 at hit',(255,255,255))
        sheet.save(OUT/'2hp-first-cut-native.png')
(OUT/'first-cut-colour-differences.json').write_text(json.dumps(rows,indent=2))
print(json.dumps(rows,indent=2))
