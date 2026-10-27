"""Close unchanged body runtime review; deliberately leave bridge tempo open.

Reads completed captures only; writes independent audit evidence beside this script.
No Unity, production asset, scene, or canonical painter mutations.
"""
from pathlib import Path
from collections import Counter
from concurrent.futures import ThreadPoolExecutor
from datetime import datetime, timezone
import hashlib
import json
import statistics

HERE = Path(__file__).resolve().parent
OUT = HERE / 'RuntimeFullFinal01'
ROOT = HERE.parents[3] / 'Concepts/Used/Stalker/Postures05/Scenarios/stalker-final'


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def constant(frames, first, last, field):
    return all(f[field] == frames[first][field] for f in frames[first:last + 1])


def main():
    c = json.loads((OUT / 'review-coverage.json').read_text())
    pages = json.loads((OUT / 'native-page-coverage.json').read_text())
    run = json.loads((ROOT / 'run.json').read_text(encoding='utf-8-sig'))
    assert run['completedAll'] and not run['captureErrors']
    assert sha(ROOT / 'run.json') == c['runSha256']
    assert len(c['profiles']) == len(run['completed']) == 9

    hash_inputs = []
    profile_results = []
    for p in c['profiles']:
        key = p['key']
        assert sha(ROOT / key / 'manifest.json') == p['manifestSha256'], key
        assert len(p['frames']) == 1540
        hash_inputs += [(ROOT / key / f"frame-{r['frame']:05d}.png", r['sha256']) for r in p['frames']]
        f = [r['facts'] for r in p['frames']]
        walk_deltas = [(f[i]['phase'] - f[i - 1]['phase']) % 1 for i in range(46, 135)]
        retreat_deltas = [(f[i]['phase'] - f[i - 1]['phase']) % 1 for i in range(490, 540)]
        walk_hz = statistics.mean(walk_deltas) * p['sampleHz']
        retreat_hz = statistics.mean(retreat_deltas) * p['sampleHz']
        assert abs(walk_hz - p['speed'] / 1.25) < .002
        assert abs(retreat_hz - 3.8 / (1.25 * .8)) < .002
        checks = {
            'stoppedPhaseHeld375to404': constant(f, 375, 404, 'phase'),
            'staggerPhaseHeld933to938': constant(f, 933, 938, 'phase'),
            'targetDeadPhaseHeld993to1037': constant(f, 993, 1037, 'phase'),
            'teleport410PreservesPhase': f[410]['Teleport'] and f[409]['phase'] == f[410]['phase'],
            'normalFearPhaseHeld465to476': constant(f, 465, 476, 'phase'),
            'normalFearBodyPrecedesEye': f[465]['fold'] > 0 and f[473]['closure'] == 0 and f[474]['closure'] > 0,
            'fullyFoldedWait547to629': all(x['fold'] == 1 and x['closure'] == 1 for x in f[547:630]),
            'fullyFoldedWaitPhaseHeld547to629': constant(f, 547, 629, 'phase'),
            'unfoldPrecedesFullyOpenEye': f[644]['fold'] == 0 and f[644]['closure'] > 0 and f[650]['closure'] == 0,
            'explicitRestoreFlagsOnly690and1226': [x['frame'] for x in f if x['RestoreRotation']] == [690, 1226],
            'hitFirstCutPoseHeld1340to1341': f[1340]['body'] == f[1341]['body'] == 'strike-10' and f[1340]['phase'] == f[1341]['phase'],
            'lightFirstCutPoseHeld1407to1408': f[1407]['body'] == f[1408]['body'] == 'strike-10' and f[1407]['phase'] == f[1408]['phase'],
            'bodyLocalTransformFixed': all(x['bodyLocalPosition'] == [0., 0., 0.] and x['bodyLocalScale'] == [1., 1., 1.] and x['bodyLocalRotation'] == 0 for x in f),
            'bodyPpu224': all(x['spritePixelsPerUnit'] == 224 for x in f),
            'deathPriorPoseHeld': f[1449]['body'] == f[1450]['body'] and f[1449]['phase'] == f[1450]['phase'],
            'deathCameraChangeExplicit': f[1449]['orthographicSize'] == 1.65 and f[1450]['orthographicSize'] == 4.5,
            'deathOriginalVisibleThrough1457': all(x['bodyEnabled'] for x in f[1450:1458]),
            'deathOriginalDisabled1458Onward': all(not x['bodyEnabled'] for x in f[1458:]),
            'deathOnePersistentRemainsRoot': all(x['remainsRoots'] == 1 for x in f[1450:]),
            'deathOneEventDespiteSecondKill': f[-1]['killCalls'] == 2 and all(x['deathEvents'] == 1 for x in f[1450:]),
        }
        assert all(checks.values()), (key, {k: v for k, v in checks.items() if not v})
        profile_results.append({
            'profile': key,
            'manifestSha256': p['manifestSha256'],
            'walkSpeed': p['speed'],
            'walkExpectedCyclesPerSecond': p['speed'] / 1.25,
            'walkObservedCyclesPerSecond': walk_hz,
            'walkPhaseDeltaRange': [min(walk_deltas), max(walk_deltas)],
            'retreatObservedCyclesPerSecond': retreat_hz,
            'checks': checks,
            'exactRgbBeforeCount': sum(r['exactRgbBefore'] for r in p['frames']),
            'reviewedCurrentUniqueFrames': len({n for pg in pages['pages'] if pg['profile'] == key and pg['reviewed'] for n in pg['frames']}),
        })

    hash_inputs += [(Path(pg['path']), pg['sha256']) for pg in pages['pages']]
    with ThreadPoolExecutor(max_workers=4) as pool:
        matches = list(pool.map(lambda x: sha(x[0]) == x[1], hash_inputs))
    assert all(matches), [str(p) for (p, _), ok in zip(hash_inputs, matches) if not ok]
    for pg in pages['pages']:
        if not pg['reviewed']:
            assert pg['profileIndex'] == 1 and pg['page'] in [33, 34, 35, 36, *range(48, 54), *range(57, 73)]
            pg['status'] = 'SUPERSEDED_OLD_EIGHT_SAMPLE_BRIDGE_NOT_REVIEWED'
            pg['reviewNote'] = 'Parent replaced this bridge series with BridgeFrames15 and a longer return. No visual approval inferred.'
        else:
            pg['status'] = 'VIEWED_NATIVE'
    pages['status'] = 'UNCHANGED_BODY_REVIEW_COMPLETE; FULL_TEMPORAL_GATE_OPEN'
    (OUT / 'native-page-coverage.json').write_text(json.dumps(pages, indent=2))

    result = {
        'status': 'UNCHANGED_BODY_PRESENTATION_PASS_WITHIN_REVIEWED_SCOPE; FULL_TEMPORAL_GATE_OPEN',
        'recordedAtUtc': datetime.now(timezone.utc).isoformat(),
        'sourceCapture': str(ROOT),
        'sourceRunSha256': c['runSha256'],
        'sourceProfiles': 9,
        'sourceScenariosPerProfile': 25,
        'sourceFramesAndHashesVerified': 13860,
        'nativePagesAndHashesVerified': len(pages['pages']),
        'nativePagesActuallyViewed': sum(pg['reviewed'] for pg in pages['pages']),
        'nativeFrameInstancesActuallyViewed': sum(len(pg['frames']) for pg in pages['pages'] if pg['reviewed']),
        'nativeUniqueCurrentFramesActuallyViewed': sum(p['reviewedCurrentUniqueFrames'] for p in profile_results),
        'supersededPagesNotViewed': sum(not pg['reviewed'] for pg in pages['pages']),
        'presentationFactEquivalenceGroups': len(json.loads((OUT / 'presentation-sequence-equivalence.json').read_text())['groups']),
        'exactRgbScenarioSequenceGroups': c['uniqueScenarioSequences'],
        'profiles': profile_results,
        'acceptedScope': ['body/core registration and skin in reviewed actual native pixels', 'normal fear body-before-eye timing', 'compact-to-fully-folded wait and stationary unfold', 'walk stop/resume, phase wrap, turn and teleport phase handling', 'explicit checkpoint restores classified as fixtures', 'actual kill-to-gore handoff and one persistent remains root'],
        'openScope': ['BridgeFrames15 additional 672 frame geometry/art', 'new longer return runtime neighbor continuity and smoothness', 'subjective x1 motion tempo in actual playback'],
        'limits': ['Selected native transition windows, not a claim to visually inspect all13860 runtime PNGs.', 'Exact renderer presentation-fact equivalence is not decoded RGB equivalence.', 'Old first-cut reset is fixed, but old eight-sample return is too brisk for the requested smoothness.', 'Phase frequency arithmetic is not a subjective motion art PASS.', 'Pure2D gameplay and offline virtual chains do not establish real3D anatomy or support physics.'],
    }
    (OUT / 'independent-completion.json').write_text(json.dumps(result, indent=2))
    c['status'] = result['status']
    c['completionEvidence'] = str(OUT / 'independent-completion.json')
    (OUT / 'review-coverage.json').write_text(json.dumps(c, indent=2))
    print(json.dumps({k: v for k, v in result.items() if k not in ['profiles', 'acceptedScope', 'openScope', 'limits']}, indent=2))


if __name__ == '__main__':
    main()
