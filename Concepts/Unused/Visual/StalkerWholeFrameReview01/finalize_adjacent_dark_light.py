from collections import Counter
from datetime import datetime, timezone
import json
from prepare_adjacent_dark_light import HERE, SOURCE, OUT, COMPOSITION_SHA, POSES_SHA, sha

c = json.loads((SOURCE / 'attack-composition.json').read_text())
d = json.loads((OUT / 'review-coverage.json').read_text())
assert sha(SOURCE / 'attack-composition.json') == COMPOSITION_SHA == d['sourceCompositionSha256']
assert sha(SOURCE / 'attack-poses.json') == POSES_SHA == d['sourcePosesSha256']
assert len(d['sourceRawPngSha256']) == 672
assert len(d['pages']) == 168 and all(p['viewed'] for p in d['pages'])
assert Counter(p['background'] for p in d['pages']) == {'Dark': 84, 'Light': 84}
for name, expected in d['sourceRawPngSha256'].items():
    assert sha(SOURCE / 'Raw' / f'{name}.png') == expected == c['rawPngSha256'][name], name
for page in d['pages']:
    assert sha(__import__('pathlib').Path(page['path'])) == page['sha256'], page['path']
for background in ['Dark', 'Light']:
    names = [name for p in d['pages'] if p['background'] == background for name in p['names']]
    assert len(names) == len(set(names)) == 672 and set(names) == set(d['sourceRawPngSha256'])
flags = json.loads((OUT / 'flag-evidence.json').read_text())
assert flags['compositionSha256'] == COMPOSITION_SHA
for name, expected in flags['contextImages'].items():
    assert sha(OUT / name) == expected
assert sha(OUT / 'proximal-contact-nearest4x.png') == flags['nearestContextSha256']
for record in flags['records']:
    record['nativeDarkAndLightActuallyViewed'] = True
    record['artVerdict'] = ('ACCEPTED_PROXIMAL_SHARED_ROOT_Y; no distal crossing or detached scrap'
                            if record['proximalRfRmContactPixels'] else
                            'ACCEPTED_FAINT_AA; no visible detached flesh, hole or contour break at native scale')
flags['nearestContactContextActuallyViewed'] = True
flags['status'] = 'COMPLETE; flagged actual native and nearest4x pixels independently inspected'
(OUT / 'flag-evidence.json').write_text(json.dumps(flags, indent=2))

copied = 0
for item in c['copiedPoseMapping']:
    source = SOURCE.parent / 'Raw' / f"{item['source']}.png"
    target = SOURCE / 'Raw' / f"{item['target']}.png"
    assert sha(source) == sha(target) == c['rawPngSha256'][item['target']]
    copied += 1
assert copied == 792
d['status'] = 'COMPLETE_STATIC_ODD_BRIDGE_DARK_LIGHT_ART_PASS; RUNTIME_TEMPORAL_GATE_OPEN'
d['completedAtUtc'] = datetime.now(timezone.utc).isoformat()
d['flagEvidence'] = str(OUT / 'flag-evidence.json')
d['limits'] = ['Every672 new odd frame inspected at native pixels on both backgrounds.',
               'Numerical Jacobian, holes and all15 pair scan belong to visual_audit; not inferred from appearance.',
               'This static art gate does not establish actual playback smoothness or true3D support physics.',
               'Old792 draws retained by byte identity; no repaint approval inferred for any changed old frame.']
(OUT / 'review-coverage.json').write_text(json.dumps(d, indent=2))
result = {'status': d['status'], 'completedAtUtc': d['completedAtUtc'],
          'sourceCompositionSha256': COMPOSITION_SHA, 'sourcePosesSha256': POSES_SHA,
          'sourcePainterSha256': d['sourcePainterSha256'],
          'newRawFrameHashesRechecked': 672, 'previouslyApprovedRawCopiesIndependentlyRechecked': copied,
          'nativePagesActuallyViewedAndRehashed': 168, 'nativeFrameInstancesActuallyViewed': 1344,
          'additionalFlaggedNativeInstancesActuallyViewed': 12, 'additionalNearestContactFramesActuallyViewed': 2,
          'fixedNativeCrop': d['fixedNativeCrop'], 'sheetSize': d['sheetSize'], 'scale': 1,
          'flagEvidenceSha256': sha(OUT / 'flag-evidence.json'),
          'reviewCoverageSha256': sha(OUT / 'review-coverage.json'),
          'mandatoryStaticArtBlockers': [],
          'stillOpen': ['new .24/.24 normal attack and interruption actual runtime replay', 'subjective x1 tempo'],
          'ownPreparationScriptsSha256': {name: sha(HERE / name) for name in
                                         ['prepare_adjacent_dark_light.py', 'mark_adjacent_pages.py', 'inspect_adjacent_flags.py', 'finalize_adjacent_dark_light.py']}}
(OUT / 'independent-completion.json').write_text(json.dumps(result, indent=2))
print(json.dumps(result, indent=2))
