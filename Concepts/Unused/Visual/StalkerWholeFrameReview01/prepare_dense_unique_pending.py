"""Exact decoded-RGB aliases of actual viewed evidence; no perceptual aliases."""
from pathlib import Path
import hashlib,json
from PIL import Image,ImageDraw,ImageFont
HERE=Path(__file__).resolve().parent;OUT=HERE/'RuntimeDenseFinal01'
ROOT=HERE.parents[3]/'Concepts/Used/Stalker/Postures05/Scenarios/stalker-dense-final'
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def write(p,d):
 t=p.with_suffix('.json.tmp');t.write_text(json.dumps(d,indent=2),encoding='utf-8');t.replace(p)
c=json.loads((OUT/'review-coverage.json').read_text(encoding='utf-8'))
p=json.loads((OUT/'native-page-coverage.json').read_text(encoding='utf-8'))
old=json.loads((HERE/'RuntimeFullFinal01/review-coverage.json').read_text(encoding='utf-8'))
oldpages=json.loads((HERE/'RuntimeFullFinal01/native-page-coverage.json').read_text(encoding='utf-8'))
oldrecords={x['key']:{r['frame']:r for r in x['frames']} for x in old['profiles']}
records={x['key']:{r['frame']:r for r in x['frames']} for x in c['profiles']}
seen={};aliases=[]
for pg in oldpages['pages']:
 if not pg['reviewed']:continue
 for i in pg['frames']:
  r=oldrecords[pg['profile']][i]
  seen.setdefault(r['decodedRgbSha256'],{'kind':'PRIOR_ACTUAL_NATIVE_VIEW','profile':pg['profile'],'frame':i,'priorPage':Path(pg['path']).name})
for pg in p['pages']:
 if not pg['reviewed']:continue
 for i in pg['frames']:
  r=records[pg['profile']][i]
  seen[r['decodedRgbSha256']]={'kind':'CURRENT_ACTUAL_NATIVE_VIEW','profile':pg['profile'],'frame':i,'page':Path(pg['path']).name}
pending=[]
for pg in p['pages']:
 if pg['reviewed']:continue
 if pg['name'].startswith(('current-body-','death-')):continue
 for i in pg['frames']:
  r=records[pg['profile']][i];h=r['decodedRgbSha256']
  target={'profile':pg['profile'],'frame':i,'plannedPage':Path(pg['path']).name,'decodedRgbSha256':h}
  if h in seen:aliases.append({**target,'exactAliasOf':seen[h]});continue
  target['sourcePngSha256']=r['sha256'];pending.append(target)
  seen[h]={'kind':'CURRENT_PENDING_UNIQUE','profile':pg['profile'],'frame':i}
font=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',15)
pages=[]
for start in range(0,len(pending),6):
 batch=pending[start:start+6];sheet=Image.new('RGB',(1536,1360),(22,24,28));d=ImageDraw.Draw(sheet)
 # Full640 camera images use center512 crop only for live, full canvas scaled
 # never; death uses640 cells on separate existing pages, excluded here.
 # To keep every actual pixel, these compact pages use fixed512 for live and
 # crop only empty background for death if the creature/gore lies in its center.
 for slot,t in enumerate(batch):
  im=Image.open(ROOT/t['profile']/f"frame-{t['frame']:05d}.png").convert('RGB')
  r=records[t['profile']][t['frame']]['facts'];x=slot%3*512;y=slot//3*680
  sheet.paste(im.crop((64,0,576,640)),(x,y+40))
  pi=next(i for i,q in enumerate(c['profiles']) if q['key']==t['profile'])
  d.text((x+3,y),f"P{pi} {t['frame']} {r['body']} {r['state']}",font=font,fill='white')
  d.text((x+3,y+19),f"A{r['attack']:.3f} F{r['fold']:.3f} E{r['closure']:.3f} P{r['phase']:.3f}",font=font,fill='white')
 path=OUT/f'unique-pending-{start//6:03d}.png';sheet.save(path)
 pages.append({'page':start//6,'path':str(path),'sha256':sha(path),'frames':batch,'reviewed':False})
out={'status':'PREPARED; NOT_VIEWED','exactRgbAliases':aliases,'uniquePending':pending,'pages':pages,
 'delegatedOriginalPages':['current-body-p01-000..048 to runtime_review','current-body-p05-000..048 to sleeper_native','death-p00..08 each0/1 to runtime_review'],
 'limits':'Only byte-exact decoded RGB identities alias prior/current actual views. Original planned temporal ordering remains in native-page-coverage; aliases prove image identity, not subjective motion tempo. Delegated body/death pages are excluded; no credit inferred until their independent completion reports arrive.'}
write(OUT/'unique-pending-coverage.json',out)
print(json.dumps({'plannedPages':len(p['pages']),'viewedOriginal':sum(pg['reviewed'] for pg in p['pages']),'pendingUniqueImages':len(pending),'pendingPages':len(pages),'exactAliases':len(aliases)}))
