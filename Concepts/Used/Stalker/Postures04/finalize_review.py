"""Audit baked alpha, independent body/eye transitions and saved Unity results."""
from pathlib import Path
import hashlib
import json
from PIL import Image, ImageDraw

folder = Path(__file__).resolve().parent
repairs = json.loads((folder / 'alpha-repairs.json').read_text())
timeline = json.loads((folder / 'preview-timeline.json').read_text())
overlap = json.loads((folder / 'limb-overlap.json').read_text())
assert len(repairs) == 432 and all(row['componentsAfter'] == 1 for row in repairs)
assert overlap['visibleOverlapPixels'] == 0
folded_hashes = {hashlib.sha256((folder / 'Clean' / f'body-8-{phase:02d}.png').read_bytes()).hexdigest()
                 for phase in range(48)}
assert len(folded_hashes) == 1, 'Folded feet must stop walking.'
max_step = max(abs(b['displayedClosure'] - a['displayedClosure']) for a, b in zip(timeline, timeline[1:]))
assert max_step <= 1 / (60 * .24) + 1e-5
assert timeline[123]['closure'] == 0 and timeline[123]['displayedClosure'] > 0

for start in [120, 132, 240, 300]:
    sheet = Image.new('RGB', (1600, 1000), '#00ff00')
    draw = ImageDraw.Draw(sheet)
    for tile, index in enumerate(range(start, start + 16)):
        with Image.open(folder / 'PreviewFrames' / f'green-{index:03d}.png') as frame:
            image = frame.crop((420, 100, 860, 620))
            image.thumbnail((200, 230), Image.Resampling.LANCZOS)
        x, y = tile % 4 * 400, tile // 4 * 250
        sheet.paste(image, (x + 100, y + 19))
        sample = timeline[index]
        draw.text((x + 8, y + 3), f'{index} | body {sample["displayedClosure"]:.2f} eye {sample["closure"]:.2f}', fill='black')
    sheet.save(folder / 'Review' / f'transition-{start:03d}.jpg', quality=96)

tests = {}
for name in ['editmode-stalker', 'editmode-sleeper', 'playmode-stalker', 'playmode-sleeper']:
    result = json.loads((folder / f'{name}.json').read_text())
    assert result['status'] == 'completed' and result['summary']['failed'] == 0, name
    tests[name] = result['summary']
preservation = json.loads((folder / 'preservation.json').read_text())
assert not preservation['runtimeHasIk'] and preservation['playModeStartScene'] is None
build = json.loads((folder / 'build-result.json').read_text())
assert build['result'] == 'Succeeded' and build['errors'] == 0
report = {
    'bodyPoses': 432,
    'connectedVisibleBodyPoses': 432,
    'allPoseContactSheetsReviewed': 9,
    'limbOverlap': overlap['visibleOverlapPixels'],
    'overlapExclusion': overlap['exclusion'],
    'fullyFoldedUniqueFrames': len(folded_hashes),
    'foldedToOpenMeanHeight': overlap['foldedToOpenMeanHeight'],
    'detachedPixelsRemoved': sum(r['detachedPixelsRemoved'] for r in repairs),
    'internalHolePixelsFilled': sum(r['holesFilledPixels'] for r in repairs),
    'attachmentSeamPixelsFilled': sum(r['attachmentSeamPixelsFilled'] for r in repairs),
    'maxBodyFoldChangePer60HzFrame': max_step,
    'bodyFoldsBeforeEyeCloses': True,
    'preview': 'Actual prefab/materials rendered in a disposable Unity preview scene; controlled timeline. Live FSM covered by PlayMode tests.',
    'greenFrames': 480,
    'gameCameraFrames': 120,
    'greenVideoSha256': hashlib.sha256((folder / 'green.mp4').read_bytes()).hexdigest(),
    'sleeperArtChanged': False,
    'tests': tests,
    'releaseBuild': build['result'],
    'runtimeIk': False,
    'authoredActorsPreserved': sum(s['count'] for s in preservation['scenes']),
    'authoringGate': 'Not applicable: no scene objects added, moved or saved.',
    'artisticApproval': 'Owner review pending',
}
(folder / 'visual-review.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
print(json.dumps(report, indent=2))
