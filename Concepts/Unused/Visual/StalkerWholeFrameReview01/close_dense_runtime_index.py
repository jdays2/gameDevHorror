"""Join completed independent review evidence; no source/cache mutations."""
from pathlib import Path
from datetime import datetime,timezone
import hashlib,json

HERE=Path(__file__).resolve().parent
OUT=HERE/'RuntimeDenseFinal01'
EXT=HERE/'RuntimeDenseFinalIndependent01/FullInterruptedExtension01'
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def read(p):return json.loads(p.read_text(encoding='utf-8-sig'))
def write(p,d):
 t=p.with_suffix(p.suffix+'.tmp');t.write_text(json.dumps(d,indent=2,ensure_ascii=False),encoding='utf-8');t.replace(p)
def textwrite(p,t):
 q=p.with_suffix(p.suffix+'.tmp');q.write_text(t,encoding='utf-8');q.replace(p)
def main():
 c=read(OUT/'review-coverage.json');done=read(OUT/'independent-completion.json')
 ext=read(EXT/'review-coverage.json');manifest=read(EXT/'page-manifest.json')
 assert ext['status'].startswith('PASS'),ext['status']
 assert ext['sourceInstances']==1179 and ext['uniqueFullRgb']==749 and ext['exactDuplicateInstances']==430
 assert ext['sourceRoot']==c['sourceRoot'] and ext['runSha256']==c['runSha256']
 assert sha(EXT/'page-manifest.json')=='3b20e7b004560ad370bab41537785ba4c07f20678b7138b369cf4b23f0e37136'
 assert len(manifest['pages'])==79
 assert sorted(p['page'] for p in manifest['pages'] if p['type']=='unique')==list(range(63))
 assert sorted(p['page'] for p in manifest['pages'] if p['type']=='temporal')==list(range(16))
 for p in manifest['pages']:assert sha(Path(p['path']))==p['sha256']
 current={p['key']:{r['frame']:r for r in p['frames']} for p in c['profiles']}
 for p in ext['profiles']:
  assert [r['frame'] for r in p['records']]==list(range(1319,1450))
  for r in p['records']:
   q=current[p['key']][r['frame']]
   assert r['pngSha256']==q['sha256'] and r['decodedRgbSha256']==q['decodedRgbSha256']
 proof=OUT/'FullInterruptionSecondHalf01/review-coverage.json'
 assert sha(proof)=='5969f604af97a9011a1c093483e980df04ff08a95e550b012c5099f93c01567a'
 assert read(proof)['actualPagesViewed']==31
 extension_evidence={'path':str(EXT/'review-coverage.json'),'sha256':sha(EXT/'review-coverage.json'),
  'actualCurrentSourceInstancesMapped':1179,'actualFullRgbUniqueViewed':749,'exactRgbAliases':430,
  'actualNativeUniquePages':63,'actualChronologicalContextPages':16,'chronologicalContextFrameInstances':189,
  'ownSecondHalfPages':31,'ownSecondHalfUniqueImages':365,'ownSecondHalfProof':str(proof),'ownSecondHalfProofSha256':sha(proof),
  'reviewerCredit':'native_whole_audit viewed unique032..062; runtime_review viewed unique000..031 and all16 chronological context pages; exact430duplicates directly compared by runtime_review.',
  'focusedRunInheritanceUsed':False}
 done.update(status='PASS_WITHIN_RECORDED_ACTUAL_NATIVE_PRESENTATION_SCOPE; NO_ACTIONABLE_ART_OR_TEMPORAL_BLOCKER',
  fullInterruptedExtension=extension_evidence,openScope=[],sourcePngCachePruneEligible=True,
  allCurrentPassesActualNativePagesViewed=347,allCurrentPassesDisplayedFrameInstances=2488,
  personallyViewedCurrentPages_native_whole_audit=183,personallyViewedCurrentFrameInstances_native_whole_audit=1261,
  finalEvidenceJoinedAtUtc=datetime.now(timezone.utc).isoformat())
 done['acceptedScope'] += ['current full-run cases27/28 across all9, including all749 full RGB unique images and separately viewed chronological cuts/bridge endpoints/fold contexts']
 done['limits']=[v for v in done['limits'] if not v.startswith('Sibling focused cases27/28')]
 done['limits'].append('The focused-run edge PASS was not reused for different full-run phase columns; its full-run extension has separate actual image evidence.')
 write(OUT/'independent-completion.json',done)
 c['status']=done['status'];c['fullInterruptedExtensionEvidence']=extension_evidence
 write(OUT/'review-coverage.json',c)
 report=(OUT/'review.md').read_text(encoding='utf-8')
 report=report.replace('The completed native review has found no actionable art defect in the normal attack, close attack, checkpoint attack, body/fear/wait/turn windows or death handoff. The current full-run interrupted attacks are receiving a separate extension review; the earlier focused-run PASS is not inherited because its actual phase columns differ.',
  'PASS within the recorded animator presentation scope. No actionable art or temporal blocker remains in the reviewed normal, close and checkpoint attacks, actual full-run interrupted attacks, body/fear/wait/turn windows or death handoffs. The different full-run interruption phase columns received their own actual-image review; no focused-run PASS was inherited.')
 old='The current full-run cases 27/28 must be closed by the independent extension before the full-run gate and source-cache pruning eligibility are marked complete. The focused [dense-edge review](../RuntimeDenseEdges01/independent-art-review.md) remains valid for its own capture only. For example, full F1348 uses `return-17-00` at phase .367400885 while focused F074 uses `return-13-00` at phase .290039063; these are different actual images.'
 new='The independent [full-run interruption extension](../RuntimeDenseFinalIndependent01/FullInterruptedExtension01/review-coverage.json) is complete: all1,179 current cases27/28 source instances are mapped, all749 full-RGB unique images were actually viewed, and the remaining430 instances are exact RGB duplicates. Another16 native pages show189 ordered cut, bridge and fold contexts. Of the63 unique pages, native_whole_audit personally viewed pages032–062 (31pages/365images), and runtime_review viewed pages000–031 plus all16 context pages. All79 page hashes, all1,179 PNGs, exact aliases, run and manifests were rechecked by runtime_review. The current P8 Morgue phase columns and wrap remain coherent. First cuts retain the real extended strike, the mouth retracts, return samples00–13 reach the current walking pose, then the fear body begins folding. No reset, detached skin, transparency, cropped tip or new mouth pop was found. The current review totals347 actually viewed native pages and2,488 displayed frame instances across credited reviewers; these totals include repeated chronological contexts and are not a unique-frame count. Source-cache pruning is eligible only for this completed run\u0027s ignored frame-*.png cache; the273 retained originals, nativepages, videos, manifests and ordered source hashes must remain. The focused [dense-edge report](../RuntimeDenseEdges01/independent-art-review.md) remains separate evidence for its own capture.'
 assert old in report
 report=report.replace(old,new)
 textwrite(OUT/'review.md',report)
 index=read(HERE/'static-review-index.json')
 index['status']='STATIC_SOURCE_ART_REVIEW_COVERAGE_COMPLETE; STALKER_ACTUAL_DENSE_PRESENTATION_REVIEW_COMPLETE'
 index['updatedAtUtc']=datetime.now(timezone.utc).isoformat()
 index['stalkerActualDenseRuntimeEvidence']={'scope':done['status'],'capture':c['sourceRoot'],'sourcePngHashesLocked':13860,
  'review':str(OUT/'review.md'),'reviewSha256':sha(OUT/'review.md'),
  'completion':str(OUT/'independent-completion.json'),'completionSha256':sha(OUT/'independent-completion.json'),
  'coverage':str(OUT/'review-coverage.json'),'coverageSha256':sha(OUT/'review-coverage.json'),
  'extension':extension_evidence,'personalViewsAndIndependentReviewerCreditsSeparated':True,
  'notClaimed':['owner artistic approval','all13860 PNGs personally viewed','all possible AI transitions','build','Sleeper runtime']}
 index['nextReview']='Sleeper actual runtime and build/scene validation are separate root-owned work; no acceptance is inferred from warm geometry or this Stalker runtime report.'
 write(HERE/'static-review-index.json',index)
 readme=(HERE/'README.md').read_text(encoding='utf-8')
 paragraphs=readme.split('\n\n')
 replacements=0
 for i,p in enumerate(paragraphs):
  if p.startswith('Actual dense runtime remains open.'):
   paragraphs[i]=('The [actual dense Stalker runtime review]('+str(OUT/'review.md').replace('\\','/')+') is complete within its recorded animator presentation scope: nine profiles,25 cases/profile,13,860 source PNG hashes locked.347 native pages were actually viewed across credited reviewers, with exact RGB aliases and explicit restore/camera fixtures separated from continuous animation. Normal/close/checkpoint attack, actual full-run interrupted return/fear, body/fold/wait/turn and all9 death handoffs have no remaining actionable art or temporal blocker in the reviewed scope. The different full-run interruption phase columns received a separate749-image/79-page extension; the earlier focused run was not assumed equivalent. This is not a claim to personally view all13,860PNG, owner real-time tempo approval, scene/build acceptance or Sleeper runtime acceptance.273 original PNGs, nativepages, ordered source hashes and independent reports are retained. The earlier eight-sample runtime report remains historical.')
   replacements+=1
 assert replacements==1
 textwrite(HERE/'README.md','\n\n'.join(paragraphs))
 paths=[OUT/'review.md',OUT/'independent-completion.json',OUT/'review-coverage.json',OUT/'native-page-coverage.json',
  OUT/'unique-pending-coverage.json',OUT/'ordered-source-hashes.json',OUT/'selected-source-png.json',OUT/'planned-instance-resolution.json',
  EXT/'review-coverage.json',EXT/'page-manifest.json',proof,HERE/'README.md',HERE/'static-review-index.json']
 hashes={str(p):sha(p) for p in paths}
 write(OUT/'completion-hashes.json',{'status':done['status'],'sha256':hashes})
 print(json.dumps({'status':done['status'],'cachePruneEligible':True,'reviewSha256':sha(OUT/'review.md'),
  'completionSha256':sha(OUT/'independent-completion.json'),'coverageSha256':sha(OUT/'review-coverage.json'),
  'indexSha256':sha(HERE/'static-review-index.json'),'readmeSha256':sha(HERE/'README.md'),
  'completionHashesSha256':sha(OUT/'completion-hashes.json')},indent=2))
if __name__=='__main__':main()
