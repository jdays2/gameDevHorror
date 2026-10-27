from pathlib import Path
from collections import defaultdict
import json,hashlib
HERE=Path(__file__).resolve().parent
OUT=HERE/'RuntimeFullFinal01'
c=json.loads((OUT/'review-coverage.json').read_text())
fields=['state','body','bodyAsset','spritePivotPixels','spriteRect','spritePixelsPerUnit','bodyLocalPosition','bodyLocalScale','bodyLocalRotation','closure','eyeOpen','eye','fold','visualRotation','scale','bodyEnabled','orthographicSize','Teleport','RestoreRotation']
groups=defaultdict(list)
for p in c['profiles']:
 for s in p['scenarios']:
  fs=[r['facts'] for r in p['frames'][s['first']:s['last']+1]]
  sig=[{k:f[k] for k in fields} for f in fs]
  h=hashlib.sha256(json.dumps(sig,sort_keys=True).encode()).hexdigest()
  groups[(s['scenario'],h)].append(p['key'])
out=[{'scenario':s,'exactPresentationFactsSha256':h,'members':m} for(s,h),m in groups.items()]
(OUT/'presentation-sequence-equivalence.json').write_text(json.dumps({'fields':fields,'groups':out,'limits':'Exact selected renderer state/shape/timing facts, not decoded RGB identity or world-camera subpixel identity.'},indent=2))
print(json.dumps({'uniquePresentationSequences':len(out),'perScenario':{s:sum(1 for g in out if g['scenario']==s) for s in dict.fromkeys(g['scenario'] for g in out)},'firstGroups':out[:4]},indent=2))
r=json.loads((HERE.parents[3]/'Concepts/Used/Stalker/Postures05/Scenarios/stalker-final/transition-review.json').read_text())
print(json.dumps({'exactDiagnosticsPass':r['exactDiagnosticsPass'],'setupFailures':r['setupFailures'],'profiles':[{'key':p['profile'],'importSamples':p['spriteImportSamplesChecked'],'exactFailures':len(p['exactFailures']),'candidates':len(p['reviewCandidates']),'ranks':[(q['frame'],q['silhouetteXorOverUnion']) for q in p['largestSilhouetteChanges'][:12]]} for p in r['profiles']]},indent=2))
