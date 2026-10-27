"""Native chronological cold attack/restore windows, current full RGB aliases only."""
from pathlib import Path
from functools import lru_cache
import hashlib,json
from PIL import Image,ImageDraw,ImageFont

HERE=Path(__file__).resolve().parent
OUT=HERE/'RuntimeSleeperFullFinal01'
CROP=(104,96,568,536)
SIZE=(1428,1452)
WINDOWS=[('normal-and-recovery',523,689),('restored-wake-and-stop',744,884),('near-attack-and-recovery',963,1116)]
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def write(p,d):
 t=p.with_suffix(p.suffix+'.tmp');t.write_text(json.dumps(d,indent=2),encoding='utf-8');t.replace(p)
def main():
 cp=OUT/'review-coverage.json';c=json.loads(cp.read_text(encoding='utf-8'));source=Path(c['sourceRoot'])
 p0=c['profiles'][0];rep=p0['key'];records={p['key']:{r['frame']:r for r in p['frames']} for p in c['profiles']}
 @lru_cache(maxsize=6)
 def image(key,i):return Image.open(source/key/f'frame-{i:05d}.png').convert('RGB')
 indices=sorted({i for _,a,b in WINDOWS for i in range(a,b+1)})
 assert len(indices)==462
 assert all(r['owner']=='native_whole_audit' for r in p0['frames'] if r['frame'] in indices and any(a<=r['frame']<=b for a,b in [(525,686),(747,881),(966,1113)]))
 targets=[];new_unique=[];seen={records[rep][i]['decodedRgbSha256']:(rep,i) for i in indices}
 for p in c['profiles']:
  key=p['key']
  for i in indices:
   r=records[key][i];path=source/key/f'frame-{i:05d}.png';assert sha(path)==r['sha256']
   bounds=r['foregroundBounds'];assert bounds and bounds[0]>=104 and bounds[1]>=96 and bounds[2]<=568 and bounds[3]<=536,(key,i,bounds)
   digest=hashlib.sha256(image(key,i).tobytes()).hexdigest();assert digest==r['decodedRgbSha256']
   t={'profile':key,'frame':i,'sourcePngSha256':r['sha256'],'decodedRgbSha256':digest,'owner':r['owner']}
   if digest in seen:
    other_key,other_i=seen[digest]
    assert image(key,i).tobytes()==image(other_key,other_i).tobytes()
    t['exactFullRgbRepresentative']={'profile':other_key,'frame':other_i}
   else:
    seen[digest]=(key,i);new_unique.append((key,i));t['newUnique']=True
   targets.append(t)
 pages=[];font=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',14)
 def render(name,keys):
  for first in range(0,len(keys),9):
   batch=keys[first:first+9];sheet=Image.new('RGB',SIZE,(20,24,28));draw=ImageDraw.Draw(sheet)
   for slot,(key,i) in enumerate(batch):
    f=records[key][i]['facts'];im=image(key,i).crop(CROP);x,y=slot%3*476+6,slot//3*484+40
    sheet.paste(im,(x,y));draw.text((x,y-36),f"{i} {f['scenario']} {f['state']}",font=font,fill='white')
    draw.text((x,y-20),f"{f['body']} F{f['fold']:.3f} E{f['closure']:.3f} P{f['phase']:.3f}",font=font,fill='white')
    assert sheet.crop((x,y,x+464,y+440)).tobytes()==im.tobytes()
   path=OUT/f'{name}-{first//9:03d}.png';sheet.save(path)
   pages.append({'kind':name,'page':first//9,'path':str(path),'sha256':sha(path),'sizePixels':list(SIZE),
    'frames':[{'profile':key,'frame':i,'sourcePngSha256':records[key][i]['sha256'],'decodedRgbSha256':records[key][i]['decodedRgbSha256']} for key,i in batch],
    'actuallyViewedOriginal':False})
 for name,a,b in WINDOWS:render(name,[(rep,i) for i in range(a,b+1)])
 if new_unique:render('additional-full-rgb-unique',new_unique)
 image.cache_clear()
 manifest={'status':'IMMUTABLE_NATIVE_PAGES_PREPARED_NOT_VIEWED','sourceRoot':c['sourceRoot'],'runSha256':c['runSha256'],
  'sourceCoverageSha256':sha(cp),'representativeProfile':rep,'fixedCrop':list(CROP),'nativeScale':1,'resampling':False,'pages':pages}
 write(OUT/'native-page-manifest.json',manifest)
 coverage={'status':'PREPARED_NOT_VIEWED','sourceRoot':c['sourceRoot'],'runSha256':c['runSha256'],
  'representativeProfile':rep,'profilesCovered':[p['key'] for p in c['profiles']],'ownedSourceFrames':1335,
  'contextSourceFrameInstances':len(targets),'representativeChronologicalFrames':len(indices),'additionalUniqueFullRgb':len(new_unique),
  'fixedCrop':list(CROP),'nativeScale':1,'resampling':False,'allFullRgbAliasesDirectByteCompared':True,
  'immutablePageManifestSha256':sha(OUT/'native-page-manifest.json'),'pages':pages,'sourceTargets':targets,
  'limits':'Current cold full capture only. No focused or warm inheritance. This preparation does not constitute actual page viewing.'}
 write(OUT/'own-native-coverage.json',coverage)
 print(json.dumps({'nativePages':len(pages),'chronologicalFrames':len(indices),'contextInstances':len(targets),'additionalUniqueRgb':len(new_unique),'manifestSha256':sha(OUT/'native-page-manifest.json')}))
if __name__=='__main__':main()
