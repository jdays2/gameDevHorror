from pathlib import Path
from collections import Counter
from datetime import datetime, timezone
import json
from prepare_cold_adjacent_review import sha

HERE=Path(__file__).resolve().parent
OUT=HERE/'ColdBodyFinal01'
p=OUT/'review-coverage.json'
d=json.loads(p.read_text())
SOURCE=Path(d['source'])
assert sha(SOURCE/'body-composition.json')==d['compositionSha256']
assert sha(SOURCE/'body-native-unique-map.json')==d['mapSha256']
assert len(d['pages'])==98 and all(q['viewed'] for q in d['pages'])
assert Counter(q['background'] for q in d['pages'])=={'dark':49,'light':49}
for name,expected in d['sourceRawPngSha256'].items():
    assert sha(SOURCE/'Raw'/(name+'.png'))==expected
for q in d['pages']:
    assert sha(Path(q['path']))==q['sha256']
for bg in ['dark','light']:
    reps=[n for q in d['pages'] if q['background']==bg for n in q['representatives']]
    aliases=[n for q in d['pages'] if q['background']==bg for n in q['aliases']]
    assert len(reps)==len(set(reps))==385
    assert len(aliases)==len(set(aliases))==432
refs={}
for label,relative in [('coldBodyGreen','body-native-green-review.json'),('coldOddGreen','AttackAdjacent01/native-green-review.json')]:
    path=SOURCE/relative
    evidence=json.loads(path.read_text())
    refs[label]={'path':str(path),'sha256':sha(path),'status':evidence['status'],
                 'sourcePoses':evidence['sourcePoses'],'reviewedUniqueFrames':evidence['reviewedUniqueFrames'],
                 'credit':'sleeper_native actual green review; not claimed as my own page views'}
assert refs['coldBodyGreen']['sourcePoses']==432 and refs['coldOddGreen']['sourcePoses']==672
d['status']='COMPLETE_COLD_BODY_STATIC_MATERIAL_DARK_LIGHT_PASS; RUNTIME_OPEN'
d['completedAtUtc']=datetime.now(timezone.utc).isoformat()
d['mandatoryMaterialBlockers']=[]
d['separateGreenEvidence']=refs
d['scope']='Independent cold RGB material/palette/blood/alpha-edge and occlusion art; all385 wholeRGBA representatives on dark/light;47 aliases independently byteexact. No transfer from warm RGB verdict.'
d['limits']=['Green views credited separately to sleeper_native.',
             'Historical cold base792 dark/light belongs to runtime_review, not this report.',
             'Installation and actual gameplay/Bridge15 runtime remain separate parent-owned checks.',
             'Alpha or geometry equality alone is not an art acceptance.']
p.write_text(json.dumps(d,indent=2))
result={'status':d['status'],'completedAtUtc':d['completedAtUtc'],'posesCovered':432,
        'uniqueWholeRgbaFramesActuallyViewedPerBackground':385,'exactAliasCount':47,
        'nativePagesActuallyViewedAndRehashed':98,'nativeFrameInstancesActuallyViewed':770,
        'sourceRawHashesRechecked':432,'compositionSha256':d['compositionSha256'],
        'mapSha256':d['mapSha256'],'coverageSha256':sha(p),'mandatoryMaterialBlockers':[],
        'fixedCropPixels':d['fixedCropPixels'],'nativePpu':224,'resampling':False,
        'separateGreenEvidence':refs,'remainingGates':['cold historical base792 dark/light by runtime_review','actual dense runtime review']}
(OUT/'independent-completion.json').write_text(json.dumps(result,indent=2))
(OUT/'review.md').write_text(f'''Cold Sleeper body432: static material PASS on dark and light.

All385 unique wholeRGBA body images were actually inspected at native1x on both backgrounds (98 own fixed-crop pages,770 viewed frame instances). All47 aliases were independently compared byte for byte with their representative, covering432 poses. The source432 PNG hashes and98 viewed page hashes were rechecked at completion. Native224PPU pixels were composited independently without filtering or auto-fit; fixed crop[132,63,446,520] contains every nonzero alpha pixel.

The grey-green body, six roots, knee skin and blades keep a coherent material through walk, intermediate folds, compact rows6/7 and closed8. Blood stays attached to blades. No warm remnant, mismatched knee patch, detached blood or skin, visible transparent bite, matte halo or cropped contour was found. Largest proximal RF/RM root contact body4-31 remains a coherent Y attachment in the cold palette. Old mask points body3-28 and body4-29 show no whole-image gap. No mandatory material blocker remains in this static body set.

Coverage and byte proofs: [review-coverage.json]({p.as_posix()}). This cold RGB verdict comes from actual cold pixels; it does not inherit the warm body RGB art approval.

Separate green evidence by sleeper_native: [cold body432 green]({(SOURCE/'body-native-green-review.json').as_posix()}) (385 unique,97 native pages) and [cold newodd672 green]({(SOURCE/'AttackAdjacent01/native-green-review.json').as_posix()}) (461 unique,116 native pages). Their report hashes are recorded in independent-completion.json; these are credited agent reviews, not my own green page views.

My separate cold newodd672 dark/light review is [ColdAttackAdjacent01/review.md]({(HERE/'ColdAttackAdjacent01/review.md').as_posix()}). Historical cold base792 dark/light belongs to runtime_review, and base792 green to sleeper_native. Actual dense runtime smoothness and installation are separate checks; no overall temporal or gameplay PASS is claimed here.
''')
print(json.dumps(result,indent=2))
