"""Record only the 49 current-body-p05 pages actually inspected by sleeper_native."""
from collections import Counter
from hashlib import sha256
from pathlib import Path
import json

import numpy as np
from PIL import Image


HERE = Path(__file__).resolve().parent
FAMILY = HERE.parent
ROOT = next(path for path in HERE.parents if (path / 'Assets').is_dir())
PROFILE = 'MechanicsSandbox-stalker-default-default-live-a05a489a'


def digest(path):
    return sha256(path.read_bytes()).hexdigest()


def main():
    coverage_path = FAMILY / 'review-coverage.json'
    coverage = json.loads(coverage_path.read_bytes())
    source_profile = next(row for row in coverage['profiles'] if row['key'] == PROFILE)
    source = Path(coverage['sourceRoot']) / PROFILE
    manifest = source / 'manifest.json'
    assert digest(manifest) == source_profile['manifestSha256']
    page_map = json.loads((FAMILY / 'native-page-coverage.json').read_bytes())
    selected = [row for row in page_map['pages'] if row['name'] == 'current-body-p05']
    assert len(selected) == 49
    selected.sort(key=lambda row: row['path'])
    assert [Path(row['path']).name for row in selected] == [f'current-body-p05-{i:03d}.png' for i in range(49)]
    frames = {row['frame']: row for row in source_profile['frames']}
    viewed = []
    pages = []
    for index, row in enumerate(selected):
        path = Path(row['path'])
        assert row['profile'] == PROFILE and digest(path) == row['sha256']
        with Image.open(path) as image:
            assert image.size == (1536, 1104)
            pixels = np.asarray(image.convert('RGB'))
        for tile, frame_id in enumerate(row['frames']):
            record = frames[frame_id]
            frame_path = source / f'frame-{frame_id:05d}.png'
            assert digest(frame_path) == record['sha256']
            with Image.open(frame_path) as image:
                rgb = np.asarray(image.convert('RGB'))
            assert sha256(rgb.tobytes()).hexdigest() == record['decodedRgbSha256']
            x = (tile % 3) * 512
            y = (tile // 3) * 552 + 40
            assert np.array_equal(pixels[y:y+512, x:x+512], rgb[64:576, 64:576]), (index, frame_id)
            yy, xx = np.where(np.any(rgb != np.array([0, 255, 0], dtype=np.uint8), axis=2))
            assert len(xx) and xx.min() >= 64 and xx.max() < 576 and yy.min() >= 64 and yy.max() < 576, frame_id
            viewed.append({'frame': frame_id, 'sourcePngSha256': record['sha256'],
                           'sourceDecodedRgbSha256': record['decodedRgbSha256'], 'facts': record['facts']})
        pages.append({'index': index, 'path': path.relative_to(ROOT).as_posix(), 'sha256': row['sha256'],
                      'frames': row['frames'], 'actuallyViewed': True,
                      'display': 'view_image(detail=original) and image(..., original); no resize_notice'})
    assert len(viewed) == 291 and len({row['frame'] for row in viewed}) == 291
    assert digest(manifest) == source_profile['manifestSha256']
    for row in pages:
        assert digest(ROOT / row['path']) == row['sha256']
    result = {'status': 'PASS: actual native green visual review complete for the assigned current body pages',
              'reviewer': 'sleeper_native', 'sourceProfile': PROFILE,
              'sourceManifestSha256': source_profile['manifestSha256'],
              'sourceCoverageSnapshotSha256': digest(coverage_path),
              'actualPagesViewed': 49, 'actualFrameInstancesViewed': 291,
              'nativeScale': 1, 'nativePageSize': [1536, 1104], 'sourceCrop': [64, 64, 576, 576],
              'fitFilteringOrResampling': False, 'pagePayloadsByteExactToSourceCrop': 291,
              'allVisibleSourcePixelsInsideCrop': True,
              'actualViewingBatches': [[0, 7], [8, 15], [16, 23], [24, 31], [32, 39], [40, 48]],
              'scenarioFrameCounts': dict(Counter(row['facts']['scenario'] for row in viewed)),
              'findings': [],
              'fixtureInterpretation': [
                  {'frame': 690, 'scenario': '11-restore-compact-turn', 'RestoreRotation': True,
                   'meaning': 'Explicit restored compact turn; not a natural fold transition'},
                  {'frame': 1038, 'scenario': '21a-waiting-before-close-attack',
                   'meaning': 'Harness directly sets Waiting and Closure=1; not the natural recoil eye timing test'},
                  {'frame': 1196, 'scenario': '22a-folded-before-checkpoint-cut',
                   'meaning': 'Harness directly sets Waiting and Closure=1 for checkpoint preparation'},
                  {'frame': 1226, 'scenario': '22b-checkpoint-restored-attack', 'RestoreRotation': True,
                   'meaning': 'Explicit checkpoint restore explains the closed-to-open reset after1225; not a recovery seam'}],
              'fixtureSource': 'AgentScripts/CaptureCreatureScenarios05.cs:650,672,676,677',
              'limits': ['Only these49 pages /291 source frames were personally viewed in this pass',
                         'Other profile pages, full attack sequences and death pages belong to other reviewers',
                         'No inherited art PASS from earlier RGB or offline sources',
                         'No claim of owner artistic approval, full real-time cadence acceptance or Sleeper runtime acceptance',
                         'No Unity or production edits; shared native-page-coverage.json was not changed'],
              'pages': pages, 'viewedSourceFrames': viewed}
    report = HERE / 'actual-body-p05-review.json'
    report.write_text(json.dumps(result, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    print(json.dumps({'status': result['status'], 'pages': 49, 'frameInstances': 291,
                      'payloadsByteExact': 291, 'findings': [], 'report': str(report),
                      'reportSha256': digest(report)}, ensure_ascii=False, indent=2))


if __name__ == '__main__':
    main()
