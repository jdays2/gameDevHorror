"""Freeze the three completed actual cold profiles; no warm/focused inheritance."""
from pathlib import Path
from collections import defaultdict,Counter
from concurrent.futures import ThreadPoolExecutor
from datetime import datetime,timezone
import hashlib,json
import numpy as np
from PIL import Image

HERE=Path(__file__).resolve().parent
OUT=HERE/'RuntimeSleeperFullFinal01'
ROOT=HERE.parents[3]/'Concepts/Used/Stalker/Postures05/Scenarios/sleeper-final'
OWN={13,14,15,17,18,19,23,24,25,26}
BODY={1,2,3,4,5,6,16,21,22}
EDGE={27,29,20}
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def write(p,d):
 t=p.with_suffix(p.suffix+'.tmp');t.write_text(json.dumps(d,indent=2,ensure_ascii=False),encoding='utf-8');t.replace(p)
def owner(s):
 n=int(s.split('-')[0]);assert sum(n in q for q in [OWN,BODY,EDGE])==1,s
 return 'native_whole_audit' if n in OWN else 'sleeper_native' if n in BODY else 'runtime_review'
def main():
 OUT.mkdir(exist_ok=True)
 run=json.loads((ROOT/'run.json').read_text(encoding='utf-8-sig'));run_sha=sha(ROOT/'run.json')
 assert run['completedAll'] and len(run['completed'])==3 and not run['captureErrors']
 profiles=[];groups=defaultdict(list);assignments=[]
 for mp in sorted(ROOT.glob('*/manifest.json')):
  key=mp.parent.name;msha=sha(mp);m=json.loads(mp.read_text(encoding='utf-8-sig'))
  assert m['species']=='sleeper' and len(m['frames'])==1365 and len(m['spans'])==22
  def inspect(f):
   p=mp.parent/f"frame-{f['frame']:05d}.png";s=sha(p)
   with Image.open(p) as im:
    assert im.size==(640,640);rgb=im.convert('RGB')
   digest=hashlib.sha256(rgb.tobytes()).hexdigest();a=np.asarray(rgb)
   ys,xs=np.nonzero(np.any(a!=(0,255,0),axis=2))
   bounds=[int(xs.min()),int(ys.min()),int(xs.max())+1,int(ys.max())+1] if len(xs) else None
   return {'frame':f['frame'],'sha256':s,'decodedRgbSha256':digest,'foregroundBounds':bounds,'owner':owner(f['scenario']),'facts':f}
  with ThreadPoolExecutor(max_workers=4) as pool:records=list(pool.map(inspect,m['frames']))
  assert [r['frame'] for r in records]==list(range(1365))
  assert sha(mp)==msha
  scenarios=[]
  for span in m['spans']:
   rows=records[span['first']:span['last']+1];assert len(rows)==span['count']
   h=hashlib.sha256(''.join(r['decodedRgbSha256'] for r in rows).encode()).hexdigest()
   q={**span,'owner':owner(span['scenario']),'sequenceRgbSha256':h};scenarios.append(q)
   groups[(span['scenario'],h)].append(key)
   assignments.append({'profile':key,**q})
  p={'key':key,'manifestSha256':msha,'sourceHierarchy':m['sourceHierarchy'],'sourceGlobalId':m['sourceGlobalId'],
   'sourceVariantKey':m['sourceVariantKey'],'sourceVariantConfiguration':m['sourceVariantConfiguration'],
   'sourceVariantMembers':m['sourceVariantMembers'],'level':m['level'],'species':m['species'],
   'speed':m['speed'],'attackDuration':m['AttackDuration'],'strikeAt':m['StrikeAt'],'lungeStart':m['LungeStart'],'lungeEnd':m['LungeEnd'],
   'sampleHz':m['sampleHz'],'captureNote':m['note'],'routeScope':m['routeScope'],'scenarios':scenarios,'frames':records}
  counts=Counter(r['owner'] for r in records)
  assert counts=={'native_whole_audit':445,'sleeper_native':669,'runtime_review':251},counts
  profiles.append(p);write(OUT/(key+'-provenance.json'),p)
  print(json.dumps({'profile':key,'frames':len(records),'ownerCounts':dict(counts)}),flush=True)
 assert len(profiles)==3 and sha(ROOT/'run.json')==run_sha
 c={'status':'ALL3_COMPLETED_CAPTURE_PROVENANCE_FROZEN; ACTUAL_VISUAL_REVIEW_PENDING','recordedAtUtc':datetime.now(timezone.utc).isoformat(),
  'sourceRoot':str(ROOT),'runSha256':run_sha,'sourceProfileCount':3,'sourceFrameCount':4095,'scenariosPerProfile':22,
  'captureErrors':run['captureErrors'],'profiles':profiles,
  'scenarioGroups':[{'scenario':s,'sequenceRgbSha256':h,'members':v} for (s,h),v in groups.items()],
  'limits':'Current cold full PNGs only. No warm, focused or approximate RGB art inheritance. All source file/decoded RGB hashes frozen; visual page review remains separate.'}
 write(OUT/'review-coverage.json',c)
 a={'status':'ALL4095_CURRENT_FRAME_OWNERS_ASSIGNED_WITHOUT_GAPS_OR_OVERLAP',
  'owners':{'native_whole_audit':{'scenarioIds':sorted(OWN),'frames':1335},'sleeper_native':{'scenarioIds':sorted(BODY),'frames':2007},'runtime_review':{'scenarioIds':sorted(EDGE),'frames':753}},
  'neighborContextMayOverlap':True,'assignments':assignments}
 write(OUT/'scenario-assignments.json',a)
 print(json.dumps({'status':c['status'],'coverageSha256':sha(OUT/'review-coverage.json'),'assignmentsSha256':sha(OUT/'scenario-assignments.json'),'exactRgbScenarioGroups':len(groups)}),flush=True)
if __name__=='__main__':main()
