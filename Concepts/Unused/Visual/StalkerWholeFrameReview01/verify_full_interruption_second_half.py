"""Freeze actual manual views032..062; never changes sibling manifest/pages."""
from pathlib import Path
from datetime import datetime, timezone
import hashlib,json
from PIL import Image,ImageChops

HERE=Path(__file__).resolve().parent
OUT=HERE/'RuntimeDenseFinal01/FullInterruptionSecondHalf01'
MANIFEST=HERE/'RuntimeDenseFinalIndependent01/FullInterruptedExtension01/page-manifest.json'
EXPECTED='3b20e7b004560ad370bab41537785ba4c07f20678b7138b369cf4b23f0e37136'
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def write(p,d):
 t=p.with_suffix(p.suffix+'.tmp');t.write_text(json.dumps(d,indent=2),encoding='utf-8');t.replace(p)
def main():
 assert sha(MANIFEST)==EXPECTED
 m=json.loads(MANIFEST.read_text(encoding='utf-8'));src=Path(m['sourceRoot'])
 c=json.loads((HERE/'RuntimeDenseFinal01/review-coverage.json').read_text())
 records={p['key']:{r['frame']:r for r in p['frames']} for p in c['profiles']}
 pages=[p for p in m['pages'] if p['type']=='unique' and 32<=p['page']<=62]
 assert [p['page'] for p in pages]==list(range(32,63))
 proof=[];count=0
 for pg in pages:
  assert sha(Path(pg['path']))==pg['sha256']
  sheet=Image.open(pg['path']).convert('RGB');assert sheet.size==(1392,1326)
  frames=[]
  for slot,t in enumerate(pg['frames']):
   key,i=t['key'],t['frame'];p=src/key/f'frame-{i:05d}.png';r=records[key][i]
   assert sha(p)==r['sha256']
   im=Image.open(p).convert('RGB');digest=hashlib.sha256(im.tobytes()).hexdigest()
   assert digest==r['decodedRgbSha256']==t['decodedRgbSha256']
   bounds=ImageChops.difference(im,Image.new('RGB',im.size,(0,255,0))).getbbox()
   assert bounds[0]>=168 and bounds[1]>=120 and bounds[2]<=504 and bounds[3]<=528
   expected=im.crop((168,120,504,528));x,y=slot%4*348+6,slot//4*442+30
   assert sheet.crop((x,y,x+336,y+408)).tobytes()==expected.tobytes()
   frames.append({'profile':key,'frame':i,'sourcePngSha256':r['sha256'],'decodedRgbSha256':digest,'payloadByteExact':True})
   count+=1
  proof.append({**pg,'actuallyViewedOriginal':True,'reviewer':'native_whole_audit','display':'view_image(detail=original); image(image_url,original); no resize_notice','shaAfterView':sha(Path(pg['path'])),'sourcePayloadsVerified':frames})
 assert count==365 and sha(MANIFEST)==EXPECTED
 OUT.mkdir(parents=True,exist_ok=True)
 result={'status':'PASS: assigned full-run interruption second-half actual native pixels',
  'reviewer':'native_whole_audit','recordedAtUtc':datetime.now(timezone.utc).isoformat(),
  'sourceCapture':str(src),'sourceRunSha256':m['runSha256'],'immutablePageManifest':str(MANIFEST),'pageManifestSha256':EXPECTED,
  'actualPagesViewed':31,'shownUniqueActualFrames':365,'pageRangeInclusive':[32,62],
  'manualViewingBatches':[[32,35],[36,39],[40,43],[44,47],[48,51],[52,55],[56,59],[60,62]],
  'nativeSize':[1392,1326],'fixedSourceCrop':[168,120,504,528],'nativeScale':1,'resampling':False,
  'allPageHashesUnchanged':True,'allSourcePngAndDecodedRgbHashesMatched':True,'allPayloadsByteExact':True,
  'findings':[],'observations':[
   'P3/P5 return samples follow changing walking phase and reach current body poses without detached skin or an observed fore-pair reset.',
   'Morgue P8 uses different phase columns; its return and phase wrap remain coherent in the actually viewed current pixels.',
   'The mouth closes before the return bridge; eye closure then progresses while fold is zero. Positive fear fold begins after the return endpoint.',
   'No actionable mouth pop, skin seam, transparent shaft, clipped tip, body-origin snap, or accidental extra spike was observed.'],
  'limits':['Only these31 actual unique pages/365 source images personally viewed in this extension split.',
   'Other extension unique pages and chronological contexts are independently reviewed by runtime_review.',
   'No focused-run RGB inheritance, no approximate dedup, no subjective real-time owner approval, no AI/scene/build/Sleeper-runtime acceptance.'],
  'pages':proof}
 write(OUT/'review-coverage.json',result)
 print(json.dumps({'status':result['status'],'actualPagesViewed':31,'frames':count,'path':str(OUT/'review-coverage.json'),'sha256':sha(OUT/'review-coverage.json')}))
if __name__=='__main__':main()
