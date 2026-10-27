"""Record completed actual original-size inspection, and verify temporal/provenance scope."""
from collections import Counter
import json
import shutil

from prepare_review import HERE, ROOT, SOURCE, SCENARIOS, digest, save, verify


GLOBAL = HERE.parent / 'RuntimeSleeperFullFinal01/review-coverage.json'
GLOBAL_SHA = 'c4d60d59f8fdb0f9363d9f1bfdb77acaba020ccd8ef98eab91741aaa59c5272c'
ASSIGNMENTS = HERE.parent / 'RuntimeSleeperFullFinal01/scenario-assignments.json'
ASSIGNMENTS_SHA = '4f37b5ce89ebde6755f3b5cdf4d39d6d146fc494e72d0e877ad1e7adbb389914'


def fact_subset(row):
    return {key: row[key] for key in ['frame', 'local', 'scenario', 'body', 'eye', 'state', 'phase', 'fold', 'closure', 'velocity', 'visualRotation', 'Teleport', 'RestoreRotation', 'staggered']}


def temporal_profile(profile):
    manifest = json.loads((SOURCE / profile['key'] / 'manifest.json').read_bytes())
    scenarios = {scenario: [row for row in manifest['frames'] if row['scenario'] == scenario] for scenario in SCENARIOS}
    idle = scenarios['01-idle']
    assert all(row['fold'] == 1 and row['closure'] == 1 and row['eye'] == 'EyeShut' and row['body'].startswith('body-8-') for row in idle)
    wake = scenarios['02-waking-entire-transition']
    assert wake[0]['fold'] == 1 and wake[-1]['fold'] == 0
    assert all(0 <= left['fold'] - right['fold'] < .019 for left, right in zip(wake, wake[1:]))
    assert {int(row['body'].split('-')[1]) for row in wake} == set(range(9))
    assert all(row['phase'] == .25 for row in wake)
    walks = [scenarios['03-walk-up'], scenarios['04-walk-down'], scenarios['05-turn-90-and-180']]
    increments = [(right['phase'] - left['phase']) % 1 for rows in walks for left, right in zip(rows, rows[1:])]
    assert all(.027 < step < .029 for step in increments)
    assert all(row['fold'] == 0 and row['eye'] == 'EyeOpen' and row['body'].startswith('body-0-') for rows in walks for row in rows)
    stop = scenarios['06-start-stop-teleport']
    assert len({row['phase'] for row in stop[29:60]}) == 1
    assert len({row['phase'] for row in stop[89:120]}) == 1
    assert [row['local'] for row in stop if row['Teleport']] == [65]
    assert stop[65]['phase'] == stop[64]['phase']
    assert stop[65]['body'] == stop[64]['body']
    stagger = scenarios['16-stagger-hit-stop-and-resume']
    assert all(row['staggered'] for row in stagger[:6])
    assert not any(row['staggered'] for row in stagger[6:])
    assert len({row['phase'] for row in stagger[:6]}) == 1
    assert .027 < (stagger[6]['phase'] - stagger[5]['phase']) % 1 < .029
    before = scenarios['21-sleep-before-near-attack']
    assert all(0 <= right['fold'] - left['fold'] < .070 for left, right in zip(before, before[1:]))
    assert before[0]['fold'] > 0 and before[-1]['fold'] == 1
    fully_closed = next(i for i, row in enumerate(before) if row['fold'] == 1)
    assert all(row['body'].startswith('body-8-') and row['eye'] == 'EyeShut' for row in before[fully_closed:])
    near_wake = scenarios['22-wake-to-near-attack-boundary']
    assert near_wake[0]['fold'] == 1 and 0 < near_wake[-1]['fold'] < .019
    assert near_wake[-1]['body'].startswith('body-0-') and near_wake[-1]['eye'] == 'EyeOpen'
    assert all(0 <= left['fold'] - right['fold'] < .019 for left, right in zip(near_wake, near_wake[1:]))
    assert {int(row['body'].split('-')[1]) for row in near_wake} == set(range(9))
    assert not any(row['RestoreRotation'] for rows in scenarios.values() for row in rows)
    return {
        'profile': profile['key'], 'counts': dict(Counter(row['scenario'] for rows in scenarios.values() for row in rows)),
        'idleFullyFolded': True, 'bothWakesAllNineBodyRows': True,
        'fullWakeFoldStepMaximum': max(left['fold'] - right['fold'] for left, right in zip(wake, wake[1:])),
        'walkPhaseStepRange': [min(increments), max(increments)],
        'approxWalkCyclesPerSecond': sum(increments) / len(increments) * 60,
        'stopRangesFixedPhase': [[434, 464], [494, 524]],
        'teleportLocal': 65, 'teleportPreviousAndActual': [fact_subset(stop[64]), fact_subset(stop[65]), fact_subset(stop[66])],
        'staggerHoldsSixThenResumes': [fact_subset(stagger[5]), fact_subset(stagger[6])],
        'sleepBeforeNearFullyFoldedFirstFrame': before[fully_closed]['frame'],
        'nearWakeLastFrame': fact_subset(near_wake[-1]),
        'noRestoreRotationInOwnedScope': True,
        'fixtureBoundaryLimits': 'Own chronological pages omit cases13-15 between524 and687, and cases17-19 between746 and882; those jumps are not claimed as natural transitions. Case21 explicitly requests Sleeping after TargetDead; this is a presentation fixture, not natural AI re-sleep. Near attack itself begins outside this scope in cases23/24.'
    }


def complete():
    data = json.loads((HERE / 'coverage.json').read_bytes())
    assert len(data['pages']) == 112
    assert data['totalSourceFrames'] == 2007 and data['uniqueCompleteRgbImages'] == 496
    assert digest(GLOBAL) == GLOBAL_SHA
    assert digest(ASSIGNMENTS) == ASSIGNMENTS_SHA
    global_data = json.loads(GLOBAL.read_bytes())
    global_frames = {(profile['key'], row['frame']): row for profile in global_data['profiles'] for row in profile['frames']}
    for row in data['frames']:
        frozen = global_frames[(row['profile'], row['frame'])]
        assert frozen['sha256'] == row['sourcePngSha256']
        assert frozen['decodedRgbSha256'] == row['fullDecodedRgbSha256']
        assert frozen['facts'] == row['facts']
    # These exact112 original images were emitted and inspected, in14 sequential tool batches.
    # This records the reviewer action; it does not infer a visual verdict from a numeric test.
    for page in data['pages']:
        assert digest(HERE / page['path']) == page['sha256']
        page['actuallyViewed'] = True
        page['viewMethod'] = 'tools.view_image(detail=original), emitted with image(...,original);1536x1104; no resize notice'
    data['actualViewBatches'] = [
        {'firstPage': first, 'lastPage': first + 7, 'prefix': 'full-body-p00-',
         'allEightResultsActuallyInspected': True, 'nativeScale': 1, 'resampling': False}
        for first in range(0, 112, 8)
    ]
    data['status'] = 'COMPLETE actual native green review: body/skin/locomotion clean; known pre-fix iris/pupil threshold defect preserved; fix follow-up not yet reviewed'
    eye_evidence_path = HERE / 'eye-threshold-evidence.json'
    eye_evidence = json.loads(eye_evidence_path.read_bytes())
    assert digest(HERE / eye_evidence['page']) == eye_evidence['pageSha256']
    eye_evidence['actuallyViewed'] = True
    eye_evidence['viewMethod'] = 'tools.view_image(detail=original), image(...,original); native192x192 source crops on768x464 page; no resampling'
    save(eye_evidence_path, eye_evidence)
    data['visualFindings'] = [{'id': 'iris-pupil-threshold-before-fix',
                               'status': 'CONFIRMED in current frozen full capture; parent code fix/follow-up separate',
                               'framesAllThreeProfiles': [90, 957], 'nextFramesAllThreeProfiles': [91, 958],
                               'actualFacts': 'EyeOpen iris sprite with actual pupil.enabled false at closure0.166666567; next sample closure0.14814806 has same iris sprite and pupil.enabled true',
                               'nativeEvidencePath': eye_evidence_path.name,
                               'nativeEvidenceSha256': digest(eye_evidence_path),
                               'impact': 'one full-white iris sample at first open frame; not hidden by a partially closed eyelid',
                               'sourceCause': 'iris quantized by RoundToInt(Closure*3) versus independent pupil cutoff Closure<0.166f',
                               'actualPupilScaleNotCaptured': True}]
    data['visuallyChecked'] = ['cold skin and native blood continuity', 'six whole legs and attached roots',
                               'closed/open/half eye registration', 'all nine sleep/wake body rows',
                               'walk phase continuity and loop', 'smooth body rotation',
                               'stop/teleport/stagger phase preservation', 'no detached joint pixels, holes, clipped tips or ghost limbs']
    data['directlyDisplayedFrameInstances'] = 669
    data['directlyDisplayedUniqueCompleteRgbImages'] = 496
    data['alreadyDisplayedDuplicatesWithinPrimaryChronology'] = 173
    data['unshownExactRgbAliasesFromOtherProfiles'] = 1338
    data['focusedReviewReuse'] = False
    data['globalFrozenProvenance'] = {'path': str(GLOBAL.relative_to(ROOT)), 'sha256': GLOBAL_SHA,
                                      'assignmentsPath': str(ASSIGNMENTS.relative_to(ROOT)), 'assignmentsSha256': ASSIGNMENTS_SHA,
                                      'all2007PngRgbAndFactsMatch': True}
    data['temporalFacts'] = [temporal_profile(profile) for profile in data['profiles']]
    data['scopeExclusions'] = ['cases13-15,17-20,23-29 belong to sibling reviewers', 'no natural AI or authored-route behavior claim from diagnostic RestoreState samples', 'no global full-capture art PASS before siblings merge', 'owner artistic approval remains separate']
    save(HERE / 'coverage.json', data)
    initial_proof = HERE / 'verification-before-eye-followup.json'
    if not initial_proof.exists():
        shutil.copyfile(HERE / 'verification.json', initial_proof)
    verify(allow_capture_helper_change=True)
    print(json.dumps({'status': data['status'], 'actualViewedPages': 112, 'ownedCurrentFrames':2007,
                      'nativeUniqueRgb':496, 'provenAliases':1511, 'directDisplayedInstances':669,
                      'globalProvenanceMatches':True, 'coverageSha256':digest(HERE / 'coverage.json'),
                      'verificationSha256':digest(HERE / 'verification.json')}, indent=2))


if __name__ == '__main__':
    complete()
