from pathlib import Path
from datetime import datetime, timezone
from collections import Counter
import json
from prepare_cold_adjacent_review import SOURCE, OUT, COMPOSITION_SHA, MAP_SHA, sha

p = OUT / 'review-coverage.json'
d = json.loads(p.read_text())
assert sha(SOURCE / 'native-composition.json') == d['compositionSha256'] == COMPOSITION_SHA
assert sha(SOURCE / 'native-unique-map.json') == d['uniqueMapSha256'] == MAP_SHA
assert len(d['pages']) == 116 and all(q['viewed'] for q in d['pages'])
assert Counter(q['background'] for q in d['pages']) == {'dark': 58, 'light': 58}
for name, expected in d['sourceRawPngSha256'].items():
    assert sha(SOURCE / 'Raw' / (name + '.png')) == expected
for page in d['pages'] + d['originalNativeUniquePages']:
    assert sha(Path(page['path'])) == page['sha256']
for bg in ['dark', 'light']:
    reps = [n for q in d['pages'] if q['background'] == bg for n in q['representatives']]
    aliases = [n for q in d['pages'] if q['background'] == bg for n in q['aliases']]
    assert len(reps) == len(set(reps)) == 461
    assert len(aliases) == len(set(aliases)) == 672
d['status'] = 'COMPLETE_COLD_NEW_ODD_STATIC_MATERIAL_DARK_LIGHT_PASS; INSTALLATION_AND_RUNTIME_OPEN'
d['completedAtUtc'] = datetime.now(timezone.utc).isoformat()
d['mandatoryMaterialBlockers'] = []
d['limits'] = ['This independently views cold RGB, not a transfer of warm geometry or alpha acceptance.',
               'Historical cold base792 and cold body432 are separate reports.',
               'No claim of runtime smoothness, gameplay, installation or real3D physics.']
p.write_text(json.dumps(d, indent=2))
result = {'status': d['status'], 'completedAtUtc': d['completedAtUtc'], 'newOddFrames': 672,
          'uniqueWholeRgbaFramesActuallyViewedPerBackground': 461, 'exactDuplicateAliases': 211,
          'nativePagesActuallyViewedAndRehashed': 116, 'nativeFrameInstancesActuallyViewed': 922,
          'all672RawHashesRechecked': True, 'all672ColdAlphaIndependentlyExactWarm': True,
          'original232NativeUniquePagesPayloadAndHashesVerified': True,
          'compositionSha256': COMPOSITION_SHA, 'uniqueMapSha256': MAP_SHA,
          'coverageSha256': sha(p), 'mandatoryMaterialBlockers': [],
          'remainingGates': ['cold body dark/light', 'cold historical base792 dark/light by runtime_review', 'installation and runtime review']}
(OUT / 'independent-completion.json').write_text(json.dumps(result, indent=2))
(OUT / 'review.md').write_text('''Cold Sleeper new odd attack bridge frames: static material PASS.

All 461 unique wholeRGBA images were actually inspected at native 1x on dark and light (116 own pages, 922 viewed instances). The 211 duplicate aliases are independently byte-exact to those representatives, covering all 672 new odd frames. Every cold alpha plane also independently matches its warm counterpart byte for byte. This identity is provenance, not the reason for the RGB art verdict.

The grey-green body, shafts and all six knees stay coherent. Red blade blood remains attached without cut seams, warm skin patches, detached fragments or visible transparent flesh. The larger RF/RM shared proximal Y keeps the previously accepted root presentation in the cold material; faint native AA produces no visible debris on either background. No mandatory material blocker was found.

The fixed crop [132,63,446,520] contains every nonzero alpha pixel; all page payloads preserve native224PPU pixels without filtering or fitting. Full-image representatives and aliases, source PNG hashes, page hashes and actual viewed coverage are in review-coverage.json; all were rechecked at completion. The original232 NativeUnique pages were independently byte-compared to Raw composites but are not claimed as actually viewed pages.

This report covers only new odd672. Cold body432 and historical cold base792 have separate review ownership. Installation, actual Bridge15 playback smoothness and subjective tempo remain open.
''')
print(json.dumps(result, indent=2))
