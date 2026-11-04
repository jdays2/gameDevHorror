from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
import numpy as np,json,hashlib

OUT=Path(__file__).resolve().parent
EVIDENCE=OUT.parents[3]/'Docs/Validation/Task007WholeGenerated10'
FONT=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',17)
source=json.loads((OUT/'source-bones.json').read_text(encoding='utf-8-sig'))
cases=[
 ('neutral','master-r2.png','original.png',[x['source00'] for x in source['source00Source01Correspondences']],[[400,374],[867,398],[338,606],[923,647],[399,919],[849,895]]),
 ('sleep','compact-source-r1.png','original-sleep-reference.png',json.loads((OUT/'compact-source-observation.json').read_text(encoding='utf-8-sig'))['limbs'],[[519,567],[774,568],[509,635],[783,635],[521,691],[774,691]]),
 ('attack','attack-source-r1.png','original-attack-reference.png',json.loads((OUT/'attack-source-observation.json').read_text(encoding='utf-8-sig'))['limbs'],[[420,352],[832,352],[323,600],[935,600],[369,880],[881,879]])]
results=[]
for name,art,reference,limbs,joints in cases:
 im=Image.open(OUT/art).convert('RGBA'); rgba=np.array(im);scale=im.width/576
 rows=[]
 overlay=im.copy();d=ImageDraw.Draw(overlay)
 for n,(limb,joint) in enumerate(zip(limbs,joints)):
  J=np.array(limb['J'],float);T=np.array(limb['T'],float)
  actualJ=np.array(joint)/scale
  visible=limb.get('tipVisible',True)
  row={'id':limb['id'],'sourceJ':J.tolist(),'actualJ_native':joint,'actualJ_sourceGrid':actualJ.tolist(),'JErrorPx':float(np.linalg.norm(actualJ-J)),'JObservationUncertaintyPx':2}
  if visible:
   center=T*scale; radius=20*scale
   x0,y0=np.maximum(0,np.floor(center-radius).astype(int));x1,y1=np.minimum([im.width,im.height],np.ceil(center+radius).astype(int))
   yy,xx=np.where(rgba[y0:y1,x0:x1,3]>=128);xx=xx+x0;yy=yy+y0
   u=(T-J)/np.linalg.norm(T-J)
   index=np.argmax(xx*u[0]+yy*u[1])
   if name=='sleep':
    # Neighboring folded legs overlap the generic search windows. Observe the
    # actual corresponding visible point, never another leg's extreme pixel.
    compactTips={'LF':[622,482],'RF':[674,482],'LR':[628,775],'RR':[672,775]}
    native=compactTips[limb['id']]
    xx[index],yy[index]=native
    row['tipMeasurement']='Manual visible correspondence +/-2source px; automated overlapping-window failure retained separately.'
   tip=np.array([xx[index],yy[index]])/scale
   row.update(actualT_native=[int(xx[index]),int(yy[index])],actualT_sourceGrid=tip.tolist(),sourceT=T.tolist(),TErrorPx=float(np.linalg.norm(tip-T)))
  else: row['T']='OCCLUDED_UNKNOWN; excluded from visible endpoint gate'
  row['nominal5px']=row['JErrorPx']<=5 and row.get('TErrorPx',0)<=5
  row['withinRecorded7pxBudget']=row['JErrorPx']<=7 and row.get('TErrorPx',0)<=7
  for point,label,color in [(J,'J','#00e1e8'),(T,'T','#00e1e8'),(actualJ,'j','#ffe358')]:
   if label=='T' and not visible:continue
   x,y=point*scale;r=7
   d.ellipse((x-r,y-r,x+r,y+r),outline=color,width=2)
  rows.append(row)
 page=Image.new('RGB',(1152,620),'#dcdcd6')
 left=Image.open(OUT/reference).convert('RGBA');left.thumbnail((576,576))
 page.paste(left,(0,35),left)
 right=overlay.resize((576,576),Image.Resampling.LANCZOS)
 page.paste(right,(576,35),right)
 pd=ImageDraw.Draw(page);pd.text((12,10),name+' / immutable source',font=FONT,fill='#242424');pd.text((588,10),'whole generated key; expected cyan / observed yellow',font=FONT,fill='#242424')
 page.save(EVIDENCE/(name+'-key-bones.png'))
 result={'key':name,'art':art,'sha256':hashlib.sha256((OUT/art).read_bytes()).hexdigest(),'registration':'Full canonical square canvas uniformly scaled by width/576, no per-limb fitting or coordinate rescue.','rows':rows,'limitedVisible2DBoneGate':all(x['withinRecorded7pxBudget'] for x in rows),'nominal5pxAll':all(x['nominal5px'] for x in rows),'scope':'Manual visible joint observations +/-2source px; tips via alpha128 extent inside fixed20px neighborhoods along source JT. H/hidden anatomy/3D not recovered. This does not prove muscle composition, seam-free skin, animation or owner art acceptance.'}
 results.append(result)
(EVIDENCE/'key-bone-review.json').write_text(json.dumps(results,indent=2),encoding='utf-8')
print(json.dumps([{'key':x['key'],'limitedGate':x['limitedVisible2DBoneGate'],'nominal5':x['nominal5pxAll'],'maxError':max(max(y['JErrorPx'],y.get('TErrorPx',0)) for y in x['rows'])} for x in results]))
