"""Freeze the actual dense Stalker review. Own evidence only; no Unity calls.

Selected native windows and exact actual-RGB aliases are deliberately distinguished
from the all-frame provenance/metadata check. No claim to have viewed 13,860 PNGs.
"""
from pathlib import Path
from collections import Counter
from concurrent.futures import ThreadPoolExecutor
from datetime import datetime, timezone
from functools import lru_cache
import hashlib
import json
import shutil
import statistics
from PIL import Image

HERE = Path(__file__).resolve().parent
OUT = HERE / 'RuntimeDenseFinal01'
ROOT = HERE.parents[3]


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


def write(path, data):
    tmp = path.with_suffix(path.suffix + '.tmp')
    tmp.write_text(json.dumps(data, indent=2, ensure_ascii=False), encoding='utf-8')
    tmp.replace(path)


def fixed(f, lo, hi, key):
    return all(x[key] == f[lo][key] for x in f[lo:hi + 1])


def main():
    c = read(OUT / 'review-coverage.json')
    capture = Path(c['sourceRoot'])
    run = read(capture / 'run.json')
    assert run['completedAll'] and not run['captureErrors']
    assert len(run['completed']) == len(c['profiles']) == 9
    assert c['actualFrameCount'] == 13860
    run_sha = sha(capture / 'run.json')
    records = {p['key']: {r['frame']: r for r in p['frames']} for p in c['profiles']}
    old = read(HERE / 'RuntimeFullFinal01/review-coverage.json')
    old_pages = read(HERE / 'RuntimeFullFinal01/native-page-coverage.json')
    old_records = {p['key']: {r['frame']: r for r in p['frames']} for p in old['profiles']}
    old_seen = {(pg['profile'], i) for pg in old_pages['pages'] if pg['reviewed'] for i in pg['frames']}
    native = read(OUT / 'native-page-coverage.json')
    pending = read(OUT / 'unique-pending-coverage.json')
    assert len(native['pages']) == 341 and len(pending['pages']) == 84
    assert all(p['reviewed'] for p in pending['pages'])
    assert len(pending['uniquePending']) == 502 and len(pending['exactRgbAliases']) == 394

    rr_path = HERE / 'RuntimeDenseFinalIndependent01/review-coverage.json'
    sn_path = OUT / 'BodyP05SleeperNative/actual-body-p05-review.json'
    rr, sn = read(rr_path), read(sn_path)
    assert sha(rr_path) == '6efe215085916ea5c253e160e05cfc4e0dd4c6a35ff13c84977b8e0ba19695a2'
    assert sha(sn_path) == 'd5378317eeba4845a7e0ae60f34110bd63c8ad112b7427804ee8153c9261511f'
    assert rr['completion']['actuallyViewedPagesOriginal'] == 67
    assert sn['actualPagesViewed'] == 49
    assert rr['status'].startswith('PASS') and sn['status'].startswith('PASS')
    assert rr['findings'] == ['No actionable visual regression in the viewed selection.', 'Restore cuts and death diagnostic framing cuts are explicit and are not claimed as continuous animation.']
    assert not sn['findings']

    by_name = {Path(p['path']).name: p for p in native['pages']}
    direct_pages = [p for p in native['pages'] if p['reviewed'] and p.get('reviewer', 'native_whole_audit') == 'native_whole_audit']
    assert len(direct_pages) == 68
    current_seen = {(p['profile'], i) for p in direct_pages for i in p['frames']}
    for reviewer, report_path, pages in [('runtime_review', rr_path, rr['pages']), ('sleeper_native', sn_path, sn['pages'])]:
        for dp in pages:
            pg = by_name[Path(dp['path']).name]
            assert pg['sha256'] == dp['sha256'] and pg['frames'] == dp['frames']
            assert dp.get('reviewed', dp.get('actuallyViewed', False))
            pg.update(reviewed=True, reviewer=reviewer,
                      reviewStatus='INDEPENDENTLY_ACTUALLY_VIEWED_ORIGINAL_1X_NO_RESIZE',
                      reviewerEvidence=str(report_path), reviewerEvidenceSha256=sha(report_path))
            current_seen.update((pg['profile'], i) for i in pg['frames'])

    unique_seen = {(t['profile'], t['frame']) for pg in pending['pages'] for t in pg['frames']}
    assert len(unique_seen) == 502
    current_seen |= unique_seen
    pending_by_target = {(t['plannedPage'], t['profile'], t['frame']): t for t in pending['uniquePending']}
    alias_by_target = {(t['plannedPage'], t['profile'], t['frame']): t for t in pending['exactRgbAliases']}
    alias_counts = Counter()
    for a in pending['exactRgbAliases']:
        ref = a['exactAliasOf']; key = (ref['profile'], ref['frame'])
        r = records[a['profile']][a['frame']]
        assert r['decodedRgbSha256'] == a['decodedRgbSha256']
        if ref['kind'] == 'PRIOR_ACTUAL_NATIVE_VIEW':
            assert key in old_seen
            rr0 = old_records[key[0]][key[1]]
            assert any(p['reviewed'] and Path(p['path']).name == ref['priorPage'] and p['profile'] == key[0] and key[1] in p['frames'] for p in old_pages['pages'])
        else:
            assert key in current_seen
            rr0 = records[key[0]][key[1]]
        assert r['decodedRgbSha256'] == rr0['decodedRgbSha256']
        alias_counts[ref['kind']] += 1

    resolved_instances = []
    for pg in native['pages']:
        pg['resolved'] = True
        if pg['reviewed']:
            pg.setdefault('reviewer', 'native_whole_audit')
            pg['resolution'] = 'ACTUAL_NATIVE_PAGE_VIEW'
        else:
            pg['resolution'] = 'ALL_PAYLOADS_VIEWED_ON_COMPACT_NATIVE_PAGES_OR_BYTE_EXACT_RGB_ALIAS'
            pg['resolutionEvidence'] = str(OUT / 'unique-pending-coverage.json')
        for i in pg['frames']:
            key = (Path(pg['path']).name, pg['profile'], i)
            if pg['reviewed']:
                kind = 'NATIVE_PAGE_VIEW_' + pg['reviewer']
            elif key in pending_by_target:
                kind = 'COMPACT_NATIVE_PAGE_VIEW_native_whole_audit'
            else:
                assert key in alias_by_target, key
                kind = 'EXACT_RGB_ALIAS_' + alias_by_target[key]['exactAliasOf']['kind']
            resolved_instances.append({'page': key[0], 'profile': key[1], 'frame': i, 'resolution': kind})
    assert len(resolved_instances) == 1944

    # Re-hash the complete immutable runtime PNG set, all manifests, and all pages.
    ordered = []
    profile_results = []
    for p in c['profiles']:
        key = p['key']; mp = capture / key / 'manifest.json'
        assert sha(mp) == p['manifestSha256']
        assert len(p['frames']) == 1540
        ordered += [{'profile': key, 'frame': r['frame'], 'pngSha256': r['sha256'],
                     'decodedRgbSha256': r['decodedRgbSha256']} for r in p['frames']]
        f = [r['facts'] for r in p['frames']]
        wd = [(f[i]['phase'] - f[i-1]['phase']) % 1 for i in range(46, 135)]
        rd = [(f[i]['phase'] - f[i-1]['phase']) % 1 for i in range(490, 540)]
        walk_hz = statistics.mean(wd) * p['sampleHz']
        retreat_hz = statistics.mean(rd) * p['sampleHz']
        checks = {
            'walkMatchesSpeedOver1_25Cycle': abs(walk_hz - p['speed'] / 1.25) < .002,
            'retreatMatches3_8Over1_0Cycle': abs(retreat_hz - 3.8 / (1.25 * .8)) < .002,
            'stoppedPhaseHeld375to404': fixed(f,375,404,'phase'),
            'staggerPhaseHeld933to938': fixed(f,933,938,'phase'),
            'targetDeadPhaseHeld993to1037': fixed(f,993,1037,'phase'),
            'teleport410PreservesPhase': f[410]['Teleport'] and f[409]['phase'] == f[410]['phase'],
            'normalFearPhaseHeld465to476': fixed(f,465,476,'phase'),
            'normalFearBodyBeforeEye': f[465]['fold'] > 0 and f[473]['closure'] == 0 and f[474]['closure'] > 0,
            'fullFoldWait547to629': all(x['fold'] == 1 and x['closure'] == 1 for x in f[547:630]),
            'fullFoldWaitPhaseHeld': fixed(f,547,629,'phase'),
            'unfoldBeforeFullyOpenEye': f[644]['fold'] == 0 and f[644]['closure'] > 0 and f[650]['closure'] == 0,
            'explicitRestoreFlags690and1226': [x['frame'] for x in f if x['RestoreRotation']] == [690,1226],
            'hitFirstCutHeld1340to1341': f[1340]['body'] == f[1341]['body'] == 'strike-10' and f[1340]['phase'] == f[1341]['phase'],
            'fearFirstCutHeld1407to1408': f[1407]['body'] == f[1408]['body'] == 'strike-10' and f[1407]['phase'] == f[1408]['phase'],
            'bodyLocalTransformFixed': all(x['bodyLocalPosition'] == [0,0,0] and x['bodyLocalScale'] == [1,1,1] and x['bodyLocalRotation'] == 0 for x in f),
            'bodyPpu224': all(x['spritePixelsPerUnit'] == 224 for x in f),
            'deathPriorPoseHeld': f[1449]['body'] == f[1450]['body'] and f[1449]['phase'] == f[1450]['phase'],
            'deathCameraChangeExplicit': f[1449]['orthographicSize'] == 1.65 and f[1450]['orthographicSize'] == 4.5,
            'deathBodyVisibleThrough1457': all(x['bodyEnabled'] for x in f[1450:1458]),
            'deathBodyDisabled1458Onward': all(not x['bodyEnabled'] for x in f[1458:]),
            'deathOneRemainsRoot': all(x['remainsRoots'] == 1 for x in f[1450:]),
            'deathOneEventDespiteTwoKillCalls': f[-1]['killCalls'] == 2 and all(x['deathEvents'] == 1 for x in f[1450:]),
        }
        attack_checks = []
        for start, end in [(825,872),(1103,1150),(1226,1273)]:
            entry = [int(x['body'].rsplit('-',1)[1]) for x in f[start:end+1] if x['body'].startswith('entry-')]
            ret = [int(x['body'].rsplit('-',1)[1]) for x in f[start:end+1] if x['body'].startswith('return-')]
            detail = {'first':start,'last':end,'phaseHeld':fixed(f,start,end,'phase'),
                      'startsAndEndsOnSameBody':f[start]['body'] == f[end]['body'],
                      'entrySamples':entry,'returnSamples':ret,
                      'entryEveryIndex1to14':entry == list(range(1,15)),
                      'returnNoIndexSkip':ret[0] == 1 and ret[-1] == 13 and all(0 <= b-a <= 1 for a,b in zip(ret,ret[1:])),
                      'walkResumesWithOneDelta':0 < (f[end+1]['phase']-f[end]['phase'])%1 < .025}
            assert all(detail[k] for k in ['phaseHeld','startsAndEndsOnSameBody','entryEveryIndex1to14','returnNoIndexSkip','walkResumesWithOneDelta']), (key,detail)
            attack_checks.append(detail)
        assert all(checks.values()), (key,{k:v for k,v in checks.items() if not v})
        profile_results.append({'profile':key,'manifestSha256':p['manifestSha256'],
            'walkSpeed':p['speed'],'walkExpectedHz':p['speed']/1.25,'walkObservedHz':walk_hz,
            'retreatObservedHz':retreat_hz,'attackDuration':p['attackDuration'],'strikeAt':p['strikeAt'],
            'checks':checks,'regularAttacks':attack_checks,
            'exactSameFrameRgbToPrior':sum(r['exactRgbPrior'] for r in p['frames']),
            'exactPriorCountsByScenario':{s['scenario']:s['exactRgbPrior'] for s in p['scenarios']}})
    with ThreadPoolExecutor(max_workers=4) as pool:
        matches = list(pool.map(lambda r: sha(capture/r['profile']/f"frame-{r['frame']:05d}.png") == r['pngSha256'], ordered))
    assert all(matches)

    @lru_cache(maxsize=32)
    def rgb(key, i):
        return Image.open(capture/key/f'frame-{i:05d}.png').convert('RGB')
    payload_count = 0
    for pg in native['pages']:
        assert sha(Path(pg['path'])) == pg['sha256']
        sheet = Image.open(pg['path']).convert('RGB')
        for slot,i in enumerate(pg['frames']):
            im = rgb(pg['profile'],i)
            if pg['fullCanvas']:
                x,y=slot%2*640,slot//2*680; expected=im
            else:
                x,y=slot%3*512,slot//3*552; expected=im.crop((64,64,576,576))
            assert sheet.crop((x,y+40,x+expected.width,y+40+expected.height)).tobytes() == expected.tobytes()
            payload_count += 1
    for pg in pending['pages']:
        assert sha(Path(pg['path'])) == pg['sha256']
        sheet = Image.open(pg['path']).convert('RGB')
        for slot,t in enumerate(pg['frames']):
            x,y=slot%3*512,slot//3*680
            expected=rgb(t['profile'],t['frame']).crop((64,0,576,640))
            assert sheet.crop((x,y+40,x+512,y+680)).tobytes() == expected.tobytes()
            payload_count += 1
    rgb.cache_clear()
    assert sha(capture/'run.json') == run_sha

    # Small original-PNG checkpoint set retained before parent prunes reproducible
    # capture PNG cache. Complete native pages and ordered hashes remain as well.
    selected = set()
    for p in c['profiles']:
        for i in [824,825,872,873,1102,1103,1150,1151,1225,1226,1273,1274,1340,1341,1407,1408,1449,1450,1457,1458,1539]:
            selected.add((p['key'],i))
    representative=next(p['key'] for p in c['profiles'] if '20hp' in p['key'])
    selected.update((representative,i) for i in range(824,876))
    selected.update((representative,i) for i in range(465,477))
    selected.update((representative,i) for i in range(627,651))
    selected_rows=[]
    for key,i in sorted(selected):
        dest=OUT/'SelectedSourcePNG'/key/f'frame-{i:05d}.png'
        dest.parent.mkdir(exist_ok=True,parents=True)
        shutil.copyfile(capture/key/dest.name,dest)
        expected=records[key][i]['sha256'];assert sha(dest)==expected
        selected_rows.append({'profile':key,'frame':i,'path':str(dest),'sha256':expected})
    write(OUT/'selected-source-png.json',{'count':len(selected_rows),'frames':selected_rows})
    write(OUT/'ordered-source-hashes.json',{'capture':str(capture),'runSha256':run_sha,'frames':ordered})
    write(OUT/'planned-instance-resolution.json',{'allPlannedNativeInstancesResolved':True,'count':len(resolved_instances),'instances':resolved_instances})
    native.update(status='ALL_341_PLANNED_PAGES_RESOLVED; ACTUAL_VIEWS_AND_EXACT_RGB_ALIASES_SEPARATE',
                  actuallyViewedPages=sum(p['reviewed'] for p in native['pages']),
                  actuallyViewedFrameInstances=sum(len(p['frames']) for p in native['pages'] if p['reviewed']),
                  resolutionEvidence=str(OUT/'planned-instance-resolution.json'))
    write(OUT/'native-page-coverage.json',native)
    pending['status']='ALL_84_COMPACT_NATIVE_PAGES_ACTUALLY_VIEWED; EXACT_RGB_ALIASES_INDEPENDENTLY_RECHECKED'
    write(OUT/'unique-pending-coverage.json',pending)
    result={'status':'OWN_NATIVE_SCOPE_PASS; FULL_RUN_INTERRUPTION_RGB_EXTENSION_PENDING',
        'recordedAtUtc':datetime.now(timezone.utc).isoformat(),'sourceCapture':str(capture),
        'sourceRunSha256':run_sha,'completedProfiles':9,'scenariosPerProfile':25,
        'allSourcePngHashesIndependentlyLocked':13860,'allSourceManifestHashesUnchanged':True,
        'originalPlannedPages':341,'originalPlannedFrameInstances':1944,'allPlannedInstancesResolved':True,
        'ownOriginalPagesActuallyViewed':68,'ownOriginalFrameInstances':394,
        'ownCompactPagesActuallyViewed':84,'ownCompactUniqueFramesActuallyViewed':502,
        'delegatedPagesActuallyViewed':116,'delegatedFrameInstances':654,
        'allReviewersCurrentPagesActuallyViewed':268,'allReviewersDisplayedCurrentFrameInstances':1550,
        'remainingOriginalPlannedInstancesByteExactRgbAliases':394,'aliasClasses':dict(alias_counts),
        'nativePagesRehashed':425,'nativePayloadsByteExactToSource':payload_count,
        'selectedOriginalPngRetained':len(selected_rows),'profiles':profile_results,
        'delegatedEvidence':[{'reviewer':'runtime_review','path':str(rr_path),'sha256':sha(rr_path)},
                             {'reviewer':'sleeper_native','path':str(sn_path),'sha256':sha(sn_path)}],
        'acceptedScope':['all six actual RGB families of normal/close/checkpoint attack entry, strike, return and resume',
            'all9 actual profile boundary neighbors, prior-image inheritance only by decoded RGB identity',
            'changed P01/P05 body windows personally inspected by credited independent reviewers',
            'normal fear body-before-eye, full-fold wait, unfold, stop, turn and teleport phase handling',
            'explicit restored snapshot cuts classified separately from continuous animation',
            'all9 actual death handoffs and stable remains, camera-size change classified separately',
            'observed walk/retreat phase cadence arithmetic and monotone dense regular bridge index advance'],
        'findings':[],
        'openScope':['Actual full-run cases27/28 current RGB sequences require separate native extension; focused-run pixels are not equal and are not inherited.'],
        'limits':['Selected native transition windows, not personal visual inspection of every one of13860 source PNGs.',
            'Exact RGB aliases prove the same image; temporal ordering is checked separately using actual metadata and chronological native windows.',
            'Sibling focused cases27/28 report supplies its own independently viewed full interruption sequences.',
            'Native adjacent-frame review and phase arithmetic do not claim owner approval of subjective real-time x1 feel.',
            'Diagnostic fixtures force states; this is presentation review, not validation of every gameplay AI state path.',
            'No Sleeper runtime, scene, build, or physical3D-anatomy acceptance is implied.'],
        'sourcePngCachePruneEligible':False,
        'pruneScope':'Only this completed run\u0027s ignored frame-*.png capture cache after parent verifies retained selected PNG/native pages/videos/manifests/ordered hashes; this script deletes nothing.'}
    write(OUT/'independent-completion.json',result)
    c['status']=result['status'];c['runSha256']=run_sha;c['completionEvidence']=str(OUT/'independent-completion.json')
    c['limits']='Selected actual native transition review plus independent all-frame provenance/metadata; see completion limits and exact reviewer credits.'
    write(OUT/'review-coverage.json',c)
    print(json.dumps({k:v for k,v in result.items() if k not in ['profiles','acceptedScope','limits','pruneScope']},indent=2))


if __name__=='__main__':
    main()
