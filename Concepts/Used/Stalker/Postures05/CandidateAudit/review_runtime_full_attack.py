"""Exact unique runtime attack images, with source identity and transition trace."""
from pathlib import Path
import hashlib
import json
import numpy as np
from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent
FAMILY = HERE.parent
ROOT = FAMILY/'Scenarios/stalker-final'


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    out = HERE/'RuntimeAttackCuts03'
    out.mkdir(exist_ok=False)
    focused = json.loads((HERE/'RuntimeAttackCuts02/manifest.json').read_text())
    reused = {r['rawPngSha256'][f'frame-{f["frame"]:05d}.png'] for r in focused['profiles'] for f in r['frames']}
    poses = {f'body-{p["row"]}-{p["phase"]:02d}': p for p in json.loads((FAMILY/'poses.json').read_text())}
    poses.update({p['name']: p for p in json.loads((FAMILY/'attack-poses.json').read_text())})
    paths = sorted(ROOT.glob('*/manifest.json'), key=lambda p: (not p.parent.name.startswith('MechanicsSandbox-stalker-20hp'), p.parent.name))
    assert len(paths) == 9
    profiles, unique, bounds = [], {}, []
    for path in paths:
        data = json.loads(path.read_text(encoding='utf-8-sig'))
        assert len(data['frames']) == 1540
        selected = set()
        for span in data['spans']:
            if span['scenario'].startswith(('13-', '21c-', '22b-')):
                selected.update(range(span['first']-3, span['last']+4))
            elif span['scenario'].startswith(('27-', '28-')):
                selected.update(range(span['first']+18, span['first']+50))
        frames = [f for f in data['frames'] if f['frame'] in selected]
        assert len(frames) == 226
        transitions, records = [], []
        for frame in frames:
            source = path.parent/f'frame-{frame["frame"]:05d}.png'
            sha = digest(source)
            records.append({'frame': frame['frame'], 'sha256': sha, 'scenario': frame['scenario'], 'local': frame['local'],
                'body': frame['body'], 'state': frame['state'], 'phase': frame['phase'], 'attack': frame['attack'],
                'fold': frame['fold'], 'eye': frame['eye'], 'bodyAsset': frame['bodyAsset']})
            if sha not in unique:
                with Image.open(source) as image:
                    assert image.size == (640, 640)
                    rgb = image.convert('RGB')
                    arr = np.asarray(rgb).astype('int16')
                    yy, xx = np.nonzero(np.max(np.abs(arr-arr[0, 0]), axis=-1) > 3)
                    bounds.append([xx.min(), yy.min(), xx.max()+1, yy.max()+1])
                unique[sha] = {'profile': path.parent.name, **records[-1], 'source': str(source),
                    'alreadyReviewedExactFocusedImage': sha in reused}
        for a, b in zip(frames, frames[1:]):
            if a['frame']+1 != b['frame']:
                continue
            ppu = 640/(2*b['orthographicSize'])
            feet = np.linalg.norm(np.asarray(poses[b['body']]['feet'])[:, :2]-np.asarray(poses[a['body']]['feet'])[:, :2], axis=-1)*ppu
            transitions.append({'beforeFrame': a['frame'], 'afterFrame': b['frame'],
                'beforeBody': a['body'], 'afterBody': b['body'], 'beforeScenario': a['scenario'], 'afterScenario': b['scenario'],
                'beforeState': a['state'], 'afterState': b['state'], 'relativeToeJumpPixels': feet.tolist(),
                'maximumToeJumpPixels': float(feet.max()), 'maximumForeToeJumpPixels': float(feet[:2].max())})
        profiles.append({'profile': path.parent.name, 'manifestSha256': digest(path), 'sourceHierarchy': data['sourceHierarchy'],
            'frames': records, 'transitions': transitions})
    box = [int(min(b[0] for b in bounds))-8, int(min(b[1] for b in bounds))-8,
           int(max(b[2] for b in bounds))+8, int(max(b[3] for b in bounds))+8]
    assert min(box) >= 0 and max(box) <= 640
    width, height = box[2]-box[0], box[3]-box[1]
    font = ImageFont.truetype('C:/Windows/Fonts/arial.ttf', 14)
    new = [v for v in unique.values() if not v['alreadyReviewedExactFocusedImage']]
    (out/'NativePages').mkdir()
    pages = []
    for start in range(0, len(new), 8):
        batch = new[start:start+8]
        sheet = Image.new('RGB', ((width+8)*4, (height+48)*2), '#00ff00')
        draw = ImageDraw.Draw(sheet)
        for index, record in enumerate(batch):
            with Image.open(record['source']) as image:
                native = image.convert('RGB').crop(box)
            x, y = index%4*(width+8), index//4*(height+48)
            sheet.paste(native, (x, y+48))
            draw.text((x+4, y+3), f'{start+index:03d}: {record["frame"]} {record["body"]}', font=font, fill='black')
            draw.text((x+4, y+23), record['state']+' '+record['scenario'][:3], font=font, fill='black')
        target = out/'NativePages'/f'unique-{start:03d}-{start+len(batch)-1:03d}.png'
        sheet.save(target)
        pages.append({'page': str(target.relative_to(out)), 'sha256': digest(target), 'images': [v['sha256'] for v in batch]})
    manifest = {'sourceRoot': str(ROOT), 'profiles': profiles, 'uniqueImages': list(unique.values()), 'pages': pages,
        'fixedNativeCrop': box, 'rawFrameSize': [640, 640], 'resizing': False,
        'selectedInstances': sum(len(v['frames']) for v in profiles), 'uniqueImageCount': len(unique),
        'reusedExactFocusedImages': sum(v['alreadyReviewedExactFocusedImage'] for v in unique.values()),
        'newImagesToInspect': len(new), 'status': 'Prepared; manual review is recorded separately.'}
    (out/'manifest.json').write_text(json.dumps(manifest, indent=2))
    worst = sorted([{'profile': r['profile'], **v} for r in profiles for v in r['transitions']], key=lambda v: -v['maximumToeJumpPixels'])
    (out/'endpoint-transitions.json').write_text(json.dumps(worst, indent=2))
    print(json.dumps({k: manifest[k] for k in ['fixedNativeCrop', 'selectedInstances', 'uniqueImageCount', 'reusedExactFocusedImages', 'newImagesToInspect']}, indent=2))
    print('New native pages:', len(pages))
    print(json.dumps(worst[:2], indent=2))


if __name__ == '__main__':
    main()
