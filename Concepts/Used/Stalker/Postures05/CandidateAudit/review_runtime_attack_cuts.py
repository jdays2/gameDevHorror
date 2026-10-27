"""Read-only exact captured state-cut proof; no Unity or animation mutation."""
from pathlib import Path
import json
import hashlib
import numpy as np
from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent
FAMILY = HERE.parent
SCENARIO = FAMILY/'Scenarios/stalker-interruption-before/MechanicsSandbox-stalker-20hp-default-stationary-990b0376'


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    out = HERE/'RuntimeAttackCuts01'
    assert not (out/'attack-state-cuts.json').exists(), 'Keep completed before proof immutable'
    out.mkdir(exist_ok=True)
    manifest = json.loads((SCENARIO/'manifest.json').read_text(encoding='utf-8-sig'))
    frames = {f['frame']: f for f in manifest['frames']}
    bodies = {f'body-{p["row"]}-{p["phase"]:02d}': p for p in json.loads((FAMILY/'poses.json').read_text())}
    attacks = {p['name']: p for p in json.loads((FAMILY/'attack-poses.json').read_text())}
    poses = {**bodies, **attacks}
    anatomy = json.loads((FAMILY.parent/'Locomotion02/anatomy.json').read_text())
    font = ImageFont.truetype('C:/Windows/Fonts/arial.ttf', 15)
    cases = []
    for title, before, after in [('27-hit', 1340, 1341), ('28-light-fear', 1407, 1408)]:
        a, b = frames[before], frames[after]
        assert a['scenario'] == b['scenario']
        assert a['body'] == 'strike-10'
        assert b['body'].startswith('body-')
        assert a['spritePixelsPerUnit'] == b['spritePixelsPerUnit'] == 224
        assert a['scale'] == b['scale'] == [1., 1., 1.]
        assert a['bodyLocalPosition'] == b['bodyLocalPosition']
        assert a['bodyLocalScale'] == b['bodyLocalScale']
        assert a['phase'] == b['phase']
        screen_ppu = 640/(2*a['orthographicSize'])
        pa, pb = poses[a['body']], poses[b['body']]
        endpoint_delta = []
        for leg in range(6):
            knee = (np.asarray(pb['knees'][leg][:2])-pa['knees'][leg][:2])*screen_ppu
            toe = (np.asarray(pb['feet'][leg][:2])-pa['feet'][leg][:2])*screen_ppu
            endpoint_delta.append({'leg': leg, 'id': anatomy['limbs'][leg]['id'],
                'kneeProjectedJumpPixelsRelativeToBody': float(np.linalg.norm(knee)),
                'toeProjectedJumpPixelsRelativeToBody': float(np.linalg.norm(toe)),
                'toeXYDeltaAtScreenScale': toe.tolist()})
        proof = Image.new('RGB', (1280, 1344), '#333333')
        draw = ImageDraw.Draw(proof)
        inspected = []
        for index, number in enumerate([before-1, before, after, after+1]):
            frame = frames[number]
            path = SCENARIO/f'frame-{number:05d}.png'
            with Image.open(path) as image:
                assert image.size == (640, 640)
                proof.paste(image.convert('RGB'), (index%2*640, index//2*672+32))
            draw.text((index%2*640+6, index//2*672+6),
                f'{number} | {frame["state"]} | {frame["body"]} | attack {frame["attack"]:.6f}', font=font, fill='white')
            inspected.append({'frame': number, 'rawSha256': digest(path)})
        proof.save(out/f'{title}-native640-neighbours.png')
        fields = ['frame', 'seconds', 'scenario', 'local', 'state', 'body', 'phase', 'fold', 'attack',
                  'staggered', 'position', 'velocity', 'scale', 'spritePixelsPerUnit', 'bodyLocalPosition',
                  'bodyLocalScale', 'bodyLocalRotation', 'orthographicSize', 'bounds']
        cases.append({'case': title, 'before': {k: a[k] for k in fields}, 'after': {k: b[k] for k in fields},
            'exactFrameDeltaSeconds': b['seconds']-a['seconds'], 'sourcePngsActuallyViewed': inspected,
            'projectedPixelsPerUnit': screen_ppu, 'endpointJumps': endpoint_delta,
            'finding': 'Visible whole-sprite jump and immediate mouth disappearance on attack interruption; not a PPU, scale, pivot or source skin defect.'})
    report = {'scenarioManifestSha256': digest(SCENARIO/'manifest.json'),
        'bodyPosesSha256': digest(FAMILY/'poses.json'), 'attackPosesSha256': digest(FAMILY/'attack-poses.json'),
        'rawCaptureSize': [640, 640], 'rawImagesResized': False,
        'cases': cases, 'verdict': 'FAIL: interrupted runtime attack presentation. Static artwork verdict remains separate.',
        'correctionConstraint': 'Recover from the currently displayed attack sample using whole frames. A direct switch to return-00 also closes the peak mouth immediately; do not restart at that sample. Gameplay interruption must remain immediate.'}
    (out/'attack-state-cuts.json').write_text(json.dumps(report, indent=2))
    print(json.dumps([{'case': c['case'], 'foreToeJumpPixels': [round(v['toeProjectedJumpPixelsRelativeToBody'], 2) for v in c['endpointJumps'][:2]],
        'from': c['before']['body'], 'to': c['after']['body']} for c in cases], indent=2))


if __name__ == '__main__':
    main()
