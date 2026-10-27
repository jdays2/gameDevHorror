"""Native-pixel review pages and endpoint diagnostics for corrected captures."""
from pathlib import Path
import hashlib
import json
import numpy as np
from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent
FAMILY = HERE.parent
ROOT = FAMILY/'Scenarios/stalker-interruption-after'


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    out = HERE/'RuntimeAttackCuts02'
    out.mkdir(exist_ok=False)
    poses = {f'body-{p["row"]}-{p["phase"]:02d}': p for p in json.loads((FAMILY/'poses.json').read_text())}
    poses.update({p['name']: p for p in json.loads((FAMILY/'attack-poses.json').read_text())})
    profiles = sorted(ROOT.glob('*/manifest.json'), key=lambda p: (not p.parent.name.startswith('MechanicsSandbox-stalker-20hp'), p.parent.name))
    assert len(profiles) == 9
    bounds, records, images = [], [], {}
    for path in profiles:
        manifest = json.loads(path.read_text(encoding='utf-8-sig'))
        assert len(manifest['frames']) == 176
        chosen = [f for f in manifest['frames'] if f['scenario'].startswith(('27-', '28-')) and 18 <= f['local'] <= 49]
        assert len(chosen) == 64
        raw_hashes = {}
        for frame in manifest['frames']:
            raw = path.parent/f'frame-{frame["frame"]:05d}.png'
            assert raw.exists(), raw
            raw_hashes[raw.name] = digest(raw)
        transitions = []
        for a, b in zip(chosen, chosen[1:]):
            if a['scenario'] != b['scenario']:
                continue
            pa, pb = poses[a['body']], poses[b['body']]
            ppu = 640/(2*b['orthographicSize'])
            feet = np.linalg.norm(np.asarray(pb['feet'])[:, :2]-np.asarray(pa['feet'])[:, :2], axis=-1)*ppu
            knees = np.linalg.norm(np.asarray(pb['knees'])[:, :2]-np.asarray(pa['knees'])[:, :2], axis=-1)*ppu
            transitions.append({'beforeFrame': a['frame'], 'afterFrame': b['frame'], 'scenario': b['scenario'],
                'beforeLocal': a['local'], 'afterLocal': b['local'], 'stateBefore': a['state'], 'stateAfter': b['state'],
                'beforeBody': a['body'], 'afterBody': b['body'], 'relativeToeJumpPixels': feet.tolist(),
                'relativeKneeJumpPixels': knees.tolist(), 'maximumToeJumpPixels': float(feet.max()),
                'maximumForeToeJumpPixels': float(feet[:2].max())})
        for frame in chosen:
            with Image.open(path.parent/f'frame-{frame["frame"]:05d}.png') as image:
                assert image.size == (640, 640)
                rgb = image.convert('RGB')
                pixels = np.asarray(rgb).astype('int16')
                background = pixels[0, 0]
                yy, xx = np.nonzero(np.max(np.abs(pixels-background), axis=-1) > 3)
                bounds.append([xx.min(), yy.min(), xx.max()+1, yy.max()+1])
                images[(path.parent.name, frame['frame'])] = rgb.copy()
        records.append({'profile': path.parent.name, 'manifestSha256': digest(path),
            'sourceHierarchy': manifest['sourceHierarchy'], 'sourceVariantConfiguration': manifest['sourceVariantConfiguration'],
            'frames': chosen, 'rawPngSha256': raw_hashes, 'transitions': transitions})
    box = [int(min(b[0] for b in bounds))-8, int(min(b[1] for b in bounds))-8,
           int(max(b[2] for b in bounds))+8, int(max(b[3] for b in bounds))+8]
    assert min(box) >= 0 and max(box) <= 640
    width, height = box[2]-box[0], box[3]-box[1]
    font = ImageFont.truetype('C:/Windows/Fonts/arial.ttf', 14)
    pages = []
    for pi, record in enumerate(records):
        folder = out/f'{pi:02d}-{record["profile"]}'
        folder.mkdir()
        for case in ['27', '28']:
            selected = [f for f in record['frames'] if f['scenario'].startswith(case+'-')]
            for start in range(0, 32, 8):
                sheet = Image.new('RGB', ((width+8)*4, (height+48)*2), '#00ff00')
                draw = ImageDraw.Draw(sheet)
                batch = selected[start:start+8]
                for index, frame in enumerate(batch):
                    x, y = index%4*(width+8), index//4*(height+48)
                    sheet.paste(images[(record['profile'], frame['frame'])].crop(box), (x, y+48))
                    draw.text((x+4, y+3), f'{frame["frame"]} / local{frame["local"]}: {frame["body"]}', font=font, fill='black')
                    draw.text((x+4, y+23), frame['state'], font=font, fill='black')
                target = folder/f'case{case}-locals{batch[0]["local"]:02d}-{batch[-1]["local"]:02d}.png'
                sheet.save(target)
                pages.append({'page': str(target.relative_to(out)), 'sha256': digest(target), 'profile': record['profile'],
                    'case': case, 'frames': [f['frame'] for f in batch]})
        assert digest(ROOT/record['profile']/'manifest.json') == record['manifestSha256']
    manifest = {'sourceRoot': str(ROOT), 'rawFrameSize': [640, 640], 'fixedNativeCrop': box,
        'resizing': False, 'profiles': records, 'pages': pages, 'reviewImages': len(images),
        'status': 'Pages prepared; manual inspection is recorded separately.'}
    (out/'manifest.json').write_text(json.dumps(manifest, indent=2))
    worst = sorted([{'profile': r['profile'], **t} for r in records for t in r['transitions']], key=lambda t: -t['maximumToeJumpPixels'])
    (out/'endpoint-transitions.json').write_text(json.dumps(worst, indent=2))
    print('READY', len(records), 'profiles;', len(pages), 'native pages;', len(images), 'selected exact capture frames; crop', box)
    print(json.dumps([{k: t[k] for k in ['profile', 'beforeFrame', 'afterFrame', 'beforeBody', 'afterBody', 'maximumToeJumpPixels']} for t in worst[:8]], indent=2))


if __name__ == '__main__':
    main()
