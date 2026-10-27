from pathlib import Path
from datetime import datetime, timezone
import json
from prepare_cold_adjacent_review import sha

HERE=Path(__file__).resolve().parent
PROJECT=HERE.parents[3]
WARM=PROJECT/'Concepts/Used/Stalker/Postures05'
COLD=PROJECT/'Concepts/Used/Sleeper/Postures05'
reports=[
 ('warmBodyAll3','native_whole_audit',HERE/'FullBodyFinal04-independent-completion.json',432,['green','dark','light']),
 ('warmBaseAttackAll3','visual_audit',WARM/'CandidateAudit/FinalAttack792Art01/review-progress.json',792,['green','dark','light']),
 ('warmOddAttackGreen','visual_audit',WARM/'AttackAdjacent01/ReviewNative/green-review.json',672,['green']),
 ('warmOddAttackDarkLight','native_whole_audit',HERE/'AttackAdjacentDarkLight01/independent-completion.json',672,['dark','light']),
 ('coldBodyGreen','sleeper_native',COLD/'body-native-green-review.json',432,['green']),
 ('coldBodyDarkLight','native_whole_audit',HERE/'ColdBodyFinal01/independent-completion.json',432,['dark','light']),
 ('coldBaseAttackGreen','sleeper_native',COLD/'base-attack-native-green-review.json',792,['green']),
 ('coldBaseAttackDarkLight','runtime_review',HERE/'ColdBaseAttackDarkLight01/completion-hashes.json',792,['dark','light']),
 ('coldOddAttackGreen','sleeper_native',COLD/'AttackAdjacent01/native-green-review.json',672,['green']),
 ('coldOddAttackDarkLight','native_whole_audit',HERE/'ColdAttackAdjacent01/independent-completion.json',672,['dark','light'])]
index=[]
for label,owner,path,count,bgs in reports:
    doc=json.loads(path.read_text())
    index.append({'id':label,'reviewer':owner,'path':str(path),'sha256':sha(path),'sourcePoses':count,'backgrounds':bgs,
                  'status':doc.get('status',doc.get('artVerdict','Completed factual coverage in source report'))})
# Final dense cold bank reuses historical even samples. Prove the reviewed source792 and odd672
# cover every final name with exact original PNG bytes, not merely identical geometry/alpha.
comp_path=COLD/'AttackAdjacent01/native-composition.json'
comp=json.loads(comp_path.read_text())
base=json.loads((HERE/'ColdBaseAttackDarkLight01/review-coverage.json').read_text())
odd=json.loads((HERE/'ColdAttackAdjacent01/review-coverage.json').read_text())
mapped={}
for rec in comp['reusedPoseMapping']:
    src,dst=rec['source'],rec['target']
    assert sha(COLD/'Raw'/(src+'.png'))==base['sourceRawPngSha256'][src]
    assert sha(COLD/'Raw'/(src+'.png'))==sha(COLD/'AttackAdjacent01/Raw'/(dst+'.png'))==comp['rawPngSha256'][dst]
    mapped[dst]={'historicalName':src,'pngSha256':comp['rawPngSha256'][dst]}
assert len(mapped)==792
assert len(odd['sourceRawPngSha256'])==672
assert not set(mapped).intersection(odd['sourceRawPngSha256'])
assert set(mapped).union(odd['sourceRawPngSha256'])==set(comp['rawPngSha256'])
assert len(comp['rawPngSha256'])==1464
for name,expected in odd['sourceRawPngSha256'].items():
    assert sha(COLD/'AttackAdjacent01/Raw'/(name+'.png'))==expected==comp['rawPngSha256'][name]
proof={'finalColdAttackPoses':1464,'reviewedHistoricalPosesMappedAndPngByteExact':792,
       'reviewedNewOddPosesDisjointAndPngByteExact':672,'everyFinalPoseCovered':True,
       'all24StrikeNamesCovered':all(f'strike-{n:02d}' in mapped for n in range(24)),
       'compositionPath':str(comp_path),'compositionSha256':sha(comp_path),'mappedRetainedPoses':mapped}
data={'status':'STATIC_SOURCE_ART_REVIEW_COVERAGE_COMPLETE; ACTUAL_DENSE_RUNTIME_GATE_OPEN',
      'createdAtUtc':datetime.now(timezone.utc).isoformat(),'reports':index,'finalColdAttackCoverageProof':proof,
      'scope':'Exact source raster art on green,dark,light. Reviewer credit separated. No owner artistic approval or automatic runtime/installation acceptance.',
      'historicalStudies':'Earlier fail reports and native-Recoil control are retained as history, not current canonical verdicts.',
      'nextReview':'Wait parent completion signal for Scenarios/stalker-dense-final; never inherit old actual body RGB without exact image byteproof.'}
(HERE/'static-review-index.json').write_text(json.dumps(data,indent=2))
def link(path,label): return f'[{label}]({path.as_posix()})'
rows=[
 ['Warm Stalker body432',link(HERE/'FullBodyFinal04-independent-art-review.md','all3 backgrounds'),'Same report','Same report','432actual frames ×3;216native pages; native_whole_audit'],
 ['Warm baseattack792',link(WARM/'CandidateAudit/FinalAttack792Art01/review-progress.json','green'),link(WARM/'CandidateAudit/FinalAttack792Art01/review-progress.json','dark'),link(WARM/'CandidateAudit/FinalAttack792Art01/review-progress.json','light'),'792×3;297native pages; visual_audit'],
 ['Warm newodd672',link(WARM/'AttackAdjacent01/ReviewNative/green-review.json','green'),link(HERE/'AttackAdjacentDarkLight01/review.md','dark'),link(HERE/'AttackAdjacentDarkLight01/review.md','light'),'672×3;84green+168dark/light pages; separate reviewers'],
 ['Cold Sleeper body432',link(COLD/'body-native-green-review.json','green'),link(HERE/'ColdBodyFinal01/review.md','dark'),link(HERE/'ColdBodyFinal01/review.md','light'),'385wholeRGBAunique+47exactaliases; green sleeper_native; dark/light native_whole_audit'],
 ['Cold baseattack792',link(COLD/'base-attack-native-green-review.json','green'),link(HERE/'ColdBaseAttackDarkLight01/independent-art-review.md','dark'),link(HERE/'ColdBaseAttackDarkLight01/independent-art-review.md','light'),'468wholeRGBAunique+324exactaliases; all24strikes explicitly viewed; green sleeper_native; dark/light runtime_review'],
 ['Cold newodd672',link(COLD/'AttackAdjacent01/native-green-review.json','green'),link(HERE/'ColdAttackAdjacent01/review.md','dark'),link(HERE/'ColdAttackAdjacent01/review.md','light'),'461wholeRGBAunique+211exactaliases; green sleeper_native; dark/light native_whole_audit']]
text='''# Current source art review index

Updated2026-10-27. The complete warm and cold static raster families have separate green, dark and light art evidence. No mandatory visible source-art blocker remains in these reviewed revisions. This is an agent static-art verdict, not owner artistic approval or a runtime acceptance.

| Source set | Green | Dark | Light | Actual coverage and reviewer |
|---|---|---|---|---|
'''
text+='\n'.join('| '+' | '.join(row)+' |' for row in rows)+'\n'
text+='''
Native means original224PPU source pixels, fixed crop, no per-frame auto-fit or resampling. Exact aliases are full decodedRGBA byte equality, including RGB; no cold RGB verdict is inherited from warm alpha or geometry. Individual reports record actual page views, hashes and source/alias maps. Coldbody dark/light98pages/770instances and coldodd dark/light116pages/922instances were independently viewed here; other reviewers are explicitly credited above.

The final dense attack bank has1464 poses: reviewed historical792 retained as even bridge samples plus672 new odd samples. The cold PNG mapping was independently rechecked for all792 retained names at index completion, including all24strikes; the672odd are disjoint and all1464final names are covered. Detailed current report hashes and this exact mapping are in '''+link(HERE/'static-review-index.json','static-review-index.json')+'''. Packing/installation checks are separate from the visual reports.

Anatomy, virtual support/contact math, source ownership, volume and visible skin were assessed separately. A zero-hole or opaque-alpha metric is not a visual PASS. The accepted baseline retains thin source Walk upper flesh; proposed extra-volume variants were rejected after actual intersections. Previously reported RF/RM contacts remain visible, source-traced shared proximal Y attachments rather than hidden distal intersections. The body mask-only points under torso are not whole-image holes. These classifications are documented in the frame reports.

Actual dense runtime remains open. The earlier '''+link(HERE/'RuntimeFullFinal01/review.md','body/fear/turn/death runtime report')+''' covers its recorded capture only; its old8bridge temporal verdict was superseded. '''+link(HERE/'RuntimeInterruptionAfter01/review.md','Old interruption evidence')+''' proves the first-pose hold repair only. Parent now captures stalker-dense-final after15sample/.24second bridges; no new-profile visual review begins before completion. Old body frames may be reused only after exact actual-image equality, and new attacks/transition neighbours need actual inspection.

The original native-keyframe measurements, failed prototypes and superseded still controls remain historical evidence. Their proposed8+4source pipeline and old failure findings are not descriptions of the final reviewed432+1464family.

---

'''
readme=HERE/'README.md'
history='''## Historical native-source evidence

The original whole drawings were a sharp visual starting point. Native Recoil03 was retained as a clean closed-pose control; original Recoil01/02 showed an extra lower appendage pair. Four fold stills were explicitly insufficient for compact stepping. The final whole-surface material pipeline was developed after that source audit; no approval of the rejected parts-over circular-joint assembly is implied.

The original corrected Sprite assets also differed substantially in physical core scale: the Recoil00 inner core was about74% of Walk00 width at its oldPPU. Manual homologous landmarks are approximately plus/minus3sourcepixels, not real3D anatomy. The unchanged347x325Recoil03 control proposesPPU384 with pivot(.501440922,.489230769), eye anchor crop(174,166). Its core width matchesWalk00 without raster resampling; the retained measurements and overlays document the limits.

Original extracts: four-recoil-two-walk-native-green.png; all-walk-native-{green,dark,light}.png; all-recoil-native-{green,dark,light}.png; all-attack-native-{green,dark,light}.png; unchanged *-exact-rgba.png. source-manifest.json records exact serialized rectangles, pivots,PPU and alpha. manual-core-measurements.json and *-core-landmarks-native.png record the manual source landmarks. closed-control-import-proposal.json describes the native control. extract_native_evidence.py and measure_core_landmarks.py reproduce only these own review files.

The source PNGs contain native partially transparent antialiasing and material. Alpha coverage alone never constituted an artpass; no blanket alpha fill was proposed. Early singleUV/ownership/volume controls and their fail reports remain in this folder or linked candidate families. Read the current source-art reports above for the final verdict.

This audit changed no production C#, Unity assets, scene resources or runtime state. Final scene authoring control is not applicable to this evidence-only subtask.
'''
temp=readme.with_suffix('.md.tmp')
temp.write_text(text+history,encoding='utf-8')
temp.replace(readme)
print(json.dumps({'reportsIndexed':len(index),'finalColdRetainedPngsIndependentlyExact':792,
                  'allFinalCold1464Covered':True,'indexSha256':sha(HERE/'static-review-index.json'),
                  'readmeSha256':sha(readme)}))
