"""Native pages and exact full-RGB alias proofs for completed sleeper-edges only."""
from collections import Counter
from hashlib import sha256
from pathlib import Path
import argparse
import json

import numpy as np
from PIL import Image, ImageDraw, ImageFont


HERE = Path(__file__).resolve().parent
ROOT = next(path for path in HERE.parents if (path / 'Assets').is_dir())
SOURCE = ROOT / 'Concepts/Used/Stalker/Postures05/Scenarios/sleeper-edges'
CROP = (64, 64, 576, 576)
PAGE_SIZE = (1536, 1104)
FONT = ImageFont.truetype('C:/Windows/Fonts/arial.ttf', 15)


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
    manifest_paths = sorted(SOURCE.glob('*/manifest.json'), key=lambda p: ('MechanicsSandbox-sleeper-live' not in p.parent.name, p.parent.name))
    assert len(manifest_paths) == 3
    groups = []
    by_rgb = {}
    profiles = []
    frames = []
    pages = []
    for profile_index, manifest_path in enumerate(manifest_paths):
        manifest = json.loads(manifest_path.read_bytes())
        key = manifest_path.parent.name
        assert len(manifest['frames']) == 206
        assert {row['scenario'] for row in manifest['frames']} == {'01-idle', '27-attack-to-hit-state-cut', '29-waking-to-target-death'}
        selected = []
        for facts in manifest['frames']:
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
                         'spans': manifest['spans'], 'frames': len(manifest['frames']),
                         'speed': manifest['speed'], 'sampleHz': manifest['sampleHz'],
                         'attackDuration': manifest['AttackDuration'], 'strikeAt': manifest['StrikeAt'],
                         'sourceVariantConfiguration': manifest['sourceVariantConfiguration'],
                         'directDisplayedFrames': len(selected),
                         'displayMode': 'all chronological frames' if profile_index == 0 else 'only new complete RGB; all others exact to an already displayed image'})
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
            path = HERE / f'edge-p{profile_index:02d}-{start//6:03d}.png'
            image.save(path)
            pages.append({'path': path.name, 'sha256': digest(path), 'profile': key,
                          'frames': [row['frame'] for row in batch], 'actuallyViewed': False})
    helper = ROOT / 'AgentScripts/CaptureCreatureScenarios05.cs'
    data = {'status': 'PREPARED_NOT_YET_VIEWED', 'sourceRoot': str(SOURCE),
            'sourceRunSha256': digest(run_path), 'captureHelperSha256': digest(helper),
            'profiles': profiles, 'totalSourceFrames': len(frames),
            'uniqueCompleteRgbImages': len(groups), 'aliasesProvenBySha256AndArrayEquality': len(frames)-len(groups),
            'nativeScale': 1, 'sourceSize': [640, 640], 'pageSize': list(PAGE_SIZE),
            'crop': list(CROP), 'resampling': False, 'allNonGreenSourcePixelsInsideCrop': True,
            'background': '#00FF00', 'pageCount': len(pages),
            'firstProfileCompleteChronologicalSequenceDisplayed': True,
            'captureErrorsRecordedByRun': len(run.get('captureErrors', [])),
            'captureErrorsAreOutsideThisArtVerdict': True,
            'groups': groups, 'frames': frames, 'pages': pages,
            'scope': 'Only exact current sleeper-edges618; no full capture or future frames inherit this review'}
    save(HERE / 'coverage.json', data)
    print(json.dumps({key: data[key] for key in ['status','totalSourceFrames','uniqueCompleteRgbImages','aliasesProvenBySha256AndArrayEquality','pageCount','captureErrorsRecordedByRun']}, indent=2))


def verify():
    data = json.loads((HERE / 'coverage.json').read_bytes())
    assert digest(SOURCE / 'run.json') == data['sourceRunSha256']
    assert digest(ROOT / 'AgentScripts/CaptureCreatureScenarios05.cs') == data['captureHelperSha256']
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
        rep = group['representative']; representative_key = (rep['profile'], rep['frame'])
        assert representative_key in shown
        representative = rgb(SOURCE / rep['profile'] / f"frame-{rep['frame']:05d}.png")
        for alias in group['aliases']:
            key = (alias['profile'], alias['frame'])
            source = SOURCE / key[0] / f'frame-{key[1]:05d}.png'
            assert digest(source) == frames[key]['sourcePngSha256']
            pixels = rgb(source)
            assert sha256(pixels.tobytes()).hexdigest() == group['fullDecodedRgbSha256']
            assert np.array_equal(pixels, representative)
    temporal = []
    for profile in data['profiles']:
        records = [row for row in data['frames'] if row['profile'] == profile['key']]
        facts = {row['frame']: row['facts'] for row in records}
        sleep = [row['facts'] for row in records if row['facts']['scenario'] == '01-idle']
        assert len(sleep) == 45 and all(row['fold'] == 1 and row['eye'] == 'EyeShut' and row['body'].startswith('body-8-') for row in sleep)
        wake = [row['facts'] for row in records if row['facts']['scenario'] == '29-waking-to-target-death']
        assert len(wake) == 75 and wake[0]['RestoreRotation']
        assert wake[29]['state'] == 'Waking' and wake[30]['state'] == 'TargetDead'
        transition = [wake[29]] + wake[30:38]
        assert 0 < transition[1]['fold'] < transition[0]['fold']
        assert all(0 <= left['fold']-right['fold'] < 0.08 for left, right in zip(transition, transition[1:]))
        assert transition[-1]['fold'] == 0
        hit = [row['facts'] for row in records if row['facts']['scenario'] == '27-attack-to-hit-state-cut']
        cut = next(i for i, row in enumerate(hit) if row['state'] == 'Recovering')
        assert cut == 41 and hit[cut-1]['body'] == hit[cut]['body'] == 'strike-11'
        strikes = [int(row['body'].split('-')[1]) for row in hit[cut:] if row['body'].startswith('strike-')]
        assert strikes == sorted(strikes) and strikes[0] == 11 and strikes[-1] == 23
        temporal.append({'profile': profile['key'], 'sleepFrames': 45,
                         'lastAttackBeforeHit': hit[cut-1], 'firstRecovering': hit[cut],
                         'postCutStrikeIndices': strikes,
                         'wakeTargetDeadTransition': transition,
                         'separateEarlierPrepeakCutCovered': False})
    result = {'status':'PASS: all source manifests/frames, frozen helper, pages and complete RGB aliases unchanged',
              'sourceFrames': len(frames), 'directlyDisplayed': len(shown), 'pages': len(data['pages']),
              'uniqueCompleteRgbImages': len(data['groups']),
              'allSourcePngAndRgbHashesRechecked': True, 'allPagePayloadsByteExactToSource': True,
              'allAliasesRecheckedByArrayEquality': True,
              'sourceRunSha256': data['sourceRunSha256'],
              'captureHelperSha256': data['captureHelperSha256'],
              'reviewHelperSha256': digest(Path(__file__)),
              'profiles': data['profiles'], 'temporalFacts': temporal,
              'visualVerdictIsSeparateFromNumericVerification': True}
    save(HERE / 'verification.json', result)
    print(json.dumps({key: result[key] for key in ['status','sourceFrames','directlyDisplayed','pages','uniqueCompleteRgbImages']}, indent=2))


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--verify', action='store_true')
    args = parser.parse_args()
    verify() if args.verify else prepare()
