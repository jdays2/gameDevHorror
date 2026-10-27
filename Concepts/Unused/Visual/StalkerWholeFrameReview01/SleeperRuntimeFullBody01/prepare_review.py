"""Native actual sleeper-final body scope, with complete RGB duplicate proofs."""
from collections import Counter
from hashlib import sha256
from pathlib import Path
import argparse
import json

import numpy as np
from PIL import Image, ImageDraw, ImageFont


HERE = Path(__file__).resolve().parent
ROOT = next(path for path in HERE.parents if (path / 'Assets').is_dir())
SOURCE = ROOT / 'Concepts/Used/Stalker/Postures05/Scenarios/sleeper-final'
SCENARIOS = {
    '01-idle': 45,
    '02-waking-entire-transition': 60,
    '03-walk-up': 90,
    '04-walk-down': 90,
    '05-turn-90-and-180': 120,
    '06-start-stop-teleport': 120,
    '16-stagger-hit-stop-and-resume': 60,
    '21-sleep-before-near-attack': 30,
    '22-wake-to-near-attack-boundary': 54,
}
CROP = (64, 64, 576, 576)
PAGE_SIZE = (1536, 1104)
FONT = ImageFont.truetype('C:/Windows/Fonts/arial.ttf', 15)
EXPECTED_CAPTURE_HELPER = '60cbd61e5096482a6492f11a864061a3634afa92c44bfb6974926c9cf38bfb71'


def digest(path):
    return sha256(path.read_bytes()).hexdigest()


def rgb(path):
    with Image.open(path) as image:
        assert image.size == (640, 640), str(path)
        return np.asarray(image.convert('RGB'))


def save(path, value):
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')


def prepare():
    run_path = SOURCE / 'run.json'
    run = json.loads(run_path.read_bytes())
    assert run['completedAll'] and run['sampleHz'] == 60 and run['squarePixels'] == 640
    assert run['captureErrors'] == []
    helper = ROOT / 'AgentScripts/CaptureCreatureScenarios05.cs'
    assert digest(helper) == EXPECTED_CAPTURE_HELPER
    manifest_paths = sorted(SOURCE.glob('*/manifest.json'), key=lambda p: ('MechanicsSandbox-sleeper-live' not in p.parent.name, p.parent.name))
    assert len(manifest_paths) == 3
    groups, profiles, frames, pages = [], [], [], []
    by_rgb = {}
    for profile_index, manifest_path in enumerate(manifest_paths):
        manifest = json.loads(manifest_path.read_bytes())
        key = manifest_path.parent.name
        assert len(manifest['frames']) == 1365
        owned = [facts for facts in manifest['frames'] if facts['scenario'] in SCENARIOS]
        assert dict(Counter(facts['scenario'] for facts in owned)) == SCENARIOS
        selected = []
        for facts in owned:
            index = facts['frame']
            source = manifest_path.parent / f'frame-{index:05d}.png'
            pixels = rgb(source)
            rgb_hash = sha256(pixels.tobytes()).hexdigest()
            yy, xx = np.where(np.any(pixels != np.array([0, 255, 0], dtype=np.uint8), axis=2))
            assert len(xx) and xx.min() >= 64 and xx.max() < 576 and yy.min() >= 64 and yy.max() < 576, (key, index)
            name = {'profile': key, 'frame': index}
            if rgb_hash in by_rgb:
                group_index = by_rgb[rgb_hash]
                representative = groups[group_index]['representative']
                assert np.array_equal(pixels, rgb(SOURCE / representative['profile'] / f"frame-{representative['frame']:05d}.png"))
                groups[group_index]['aliases'].append(name)
                new = False
            else:
                group_index = len(groups)
                by_rgb[rgb_hash] = group_index
                groups.append({'representative': name, 'fullDecodedRgbSha256': rgb_hash, 'aliases': [name]})
                new = True
            record = {'profile': key, 'frame': index, 'sourcePngSha256': digest(source),
                      'fullDecodedRgbSha256': rgb_hash, 'exactRgbGroup': group_index, 'facts': facts}
            frames.append(record)
            if profile_index == 0 or new:
                selected.append(record)
        profiles.append({'key': key, 'manifestSha256': digest(manifest_path),
                         'totalManifestFrames': len(manifest['frames']),
                         'spans': [span for span in manifest['spans'] if span['scenario'] in SCENARIOS],
                         'ownedFrames': len(owned), 'speed': manifest['speed'], 'sampleHz': manifest['sampleHz'],
                         'attackDuration': manifest['AttackDuration'], 'strikeAt': manifest['StrikeAt'],
                         'sourceVariantConfiguration': manifest['sourceVariantConfiguration'],
                         'directDisplayedFrames': len(selected),
                         'displayMode': 'all owned chronological frames' if profile_index == 0 else 'only new complete RGB; all others exact to an already displayed image'})
        for start in range(0, len(selected), 6):
            batch = selected[start:start+6]
            image = Image.new('RGB', PAGE_SIZE, (22, 24, 28))
            draw = ImageDraw.Draw(image)
            for slot, row in enumerate(batch):
                x, y = (slot % 3) * 512, (slot // 3) * 552
                payload = rgb(SOURCE / key / f"frame-{row['frame']:05d}.png")[64:576, 64:576]
                image.paste(Image.fromarray(payload), (x, y+40))
                facts = row['facts']
                draw.text((x+3, y), f"{row['frame']} {facts['body']} {facts['state']}", font=FONT, fill='white')
                draw.text((x+3, y+19), f"A{facts['attack']:.3f} F{facts['fold']:.3f} E{facts['closure']:.3f} P{facts['phase']:.3f}", font=FONT, fill='white')
            path = HERE / f'full-body-p{profile_index:02d}-{start//6:03d}.png'
            image.save(path)
            pages.append({'path': path.name, 'sha256': digest(path), 'profile': key,
                          'frames': [row['frame'] for row in batch], 'actuallyViewed': False})
    data = {'status': 'PREPARED_NOT_YET_VIEWED', 'sourceRoot': str(SOURCE),
            'sourceRunSha256': digest(run_path), 'captureHelperSha256': digest(helper),
            'profiles': profiles, 'totalSourceFrames': len(frames),
            'uniqueCompleteRgbImages': len(groups), 'aliasesProvenBySha256AndArrayEquality': len(frames)-len(groups),
            'nativeScale': 1, 'sourceSize': [640, 640], 'pageSize': list(PAGE_SIZE),
            'crop': list(CROP), 'resampling': False, 'allNonGreenSourcePixelsInsideCrop': True,
            'background': '#00FF00', 'pageCount': len(pages), 'captureErrorsRecordedByRun': 0,
            'firstProfileCompleteOwnedChronologicalSequenceDisplayed': True,
            'groups': groups, 'frames': frames, 'pages': pages,
            'scope': 'Only actual sleeper-final cases 01-06,16,21,22 all 3 configurations; no focused review inheritance',
            'scenarios': SCENARIOS, 'visualFindings': [], 'actualViewBatches': []}
    save(HERE / 'coverage.json', data)
    print(json.dumps({key: data[key] for key in ['status','totalSourceFrames','uniqueCompleteRgbImages','aliasesProvenBySha256AndArrayEquality','pageCount']}, indent=2))
    print(json.dumps(data['profiles'], indent=2))


def verify(allow_capture_helper_change=False):
    data = json.loads((HERE / 'coverage.json').read_bytes())
    assert digest(SOURCE / 'run.json') == data['sourceRunSha256']
    assert data['captureHelperSha256'] == EXPECTED_CAPTURE_HELPER
    current_helper_hash = digest(ROOT / 'AgentScripts/CaptureCreatureScenarios05.cs')
    helper_matches = current_helper_hash == data['captureHelperSha256']
    if not helper_matches:
        assert allow_capture_helper_change
        previous_path = HERE / 'verification-before-eye-followup.json'
        previous = json.loads(previous_path.read_bytes())
        assert previous['captureHelperSha256'] == EXPECTED_CAPTURE_HELPER
        assert previous['allSourcePngAndRgbHashesRechecked']
    for profile in data['profiles']:
        assert digest(SOURCE / profile['key'] / 'manifest.json') == profile['manifestSha256']
    frames = {(row['profile'], row['frame']): row for row in data['frames']}
    shown = set()
    for page in data['pages']:
        path = HERE / page['path']
        assert digest(path) == page['sha256']
        with Image.open(path) as image:
            pixels = np.asarray(image.convert('RGB'))
            assert image.size == PAGE_SIZE
        for slot, index in enumerate(page['frames']):
            key = (page['profile'], index)
            source = SOURCE / key[0] / f'frame-{index:05d}.png'
            assert digest(source) == frames[key]['sourcePngSha256']
            x, y = (slot % 3) * 512, (slot // 3) * 552 + 40
            assert np.array_equal(pixels[y:y+512, x:x+512], rgb(source)[64:576, 64:576])
            shown.add(key)
    for group in data['groups']:
        rep = group['representative']
        assert (rep['profile'], rep['frame']) in shown
        representative = rgb(SOURCE / rep['profile'] / f"frame-{rep['frame']:05d}.png")
        for alias in group['aliases']:
            key = (alias['profile'], alias['frame'])
            source = SOURCE / key[0] / f'frame-{key[1]:05d}.png'
            assert digest(source) == frames[key]['sourcePngSha256']
            pixels = rgb(source)
            assert sha256(pixels.tobytes()).hexdigest() == group['fullDecodedRgbSha256']
            assert np.array_equal(pixels, representative)
    result = {'status': 'PASS: scoped old full manifests/PNG/RGB, native page payloads and full RGB aliases unchanged; capture helper checked separately',
              'sourceFrames': len(frames), 'directlyDisplayed': len(shown), 'pages': len(data['pages']),
              'uniqueCompleteRgbImages': len(data['groups']),
              'allSourcePngAndRgbHashesRechecked': True, 'allPagePayloadsByteExactToSource': True,
              'allAliasesRecheckedByArrayEquality': True, 'captureErrors': [],
              'sourceRunSha256': data['sourceRunSha256'], 'captureHelperSha256': data['captureHelperSha256'],
              'currentCaptureHelperSha256': current_helper_hash, 'captureHelperStillMatchesOldFullCapture': helper_matches,
              'helperChangeLimit': None if helper_matches else 'Parent changed CaptureCreatureScenarios05.cs after initial successful full proof for separately authorized bounded eye-threshold-final capture. The initial proof is preserved in verification-before-eye-followup.json. This does not claim unchanged current helper.',
              'initialProofPath': None if helper_matches else 'verification-before-eye-followup.json',
              'initialProofSha256': None if helper_matches else digest(HERE / 'verification-before-eye-followup.json'),
              'reviewHelperSha256': digest(Path(__file__)), 'coverageSha256': digest(HERE / 'coverage.json'),
              'profiles': data['profiles'], 'visualVerdictIsSeparateFromNumericVerification': True}
    save(HERE / 'verification.json', result)
    print(json.dumps({key: result[key] for key in ['status','sourceFrames','directlyDisplayed','pages','uniqueCompleteRgbImages']}, indent=2))


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--verify', action='store_true')
    args = parser.parse_args()
    verify() if args.verify else prepare()
