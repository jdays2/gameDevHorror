"""Independent native dark/light inspection of all new672 odd attack bridges."""
from pathlib import Path
import hashlib
import json
import numpy as np
from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent
SOURCE = HERE.parents[3] / 'Concepts/Used/Stalker/Postures05/AttackAdjacent01'
OUT = HERE / 'AttackAdjacentDarkLight01'
COMPOSITION_SHA = '95d9f5b3efeada473a4c75eb81b7ce54da3a8c98ba0e97ca463416def713b519'
POSES_SHA = 'd6615dc8da453cb1407e37d581acab90d001e50987597161da6bd0423b7d42bf'
BACKGROUNDS = {'Dark': (20, 24, 32), 'Light': (240, 238, 232)}


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def to_linear(rgb):
    return np.where(rgb <= .04045, rgb / 12.92, ((rgb + .055) / 1.055) ** 2.4)


def composite(rgba, background):
    values = np.asarray(rgba, dtype=np.float32) / 255
    alpha = values[..., 3:4]
    bg = to_linear(np.asarray(background, dtype=np.float32) / 255)
    linear = to_linear(values[..., :3]) * alpha + bg * (1 - alpha)
    rgb = np.where(linear <= .0031308, linear * 12.92,
                   1.055 * np.maximum(linear, 0) ** (1 / 2.4) - .055)
    return Image.fromarray(np.rint(np.clip(rgb, 0, 1) * 255).astype('uint8'))


def main():
    assert sha(SOURCE / 'attack-composition.json') == COMPOSITION_SHA
    assert sha(SOURCE / 'attack-poses.json') == POSES_SHA
    c = json.loads((SOURCE / 'attack-composition.json').read_text())
    assert c['poses'] == 1464 and c['newOddPoses'] == 672
    poses = [p for p in json.loads((SOURCE / 'attack-poses.json').read_text())
             if p['group'] != 'strike' and p['sample'] % 2 == 1]
    assert len(poses) == 672 and len({p['name'] for p in poses}) == 672
    images, bounds = {}, []
    for pose in poses:
        name = pose['name']
        path = SOURCE / 'Raw' / f'{name}.png'
        assert sha(path) == c['rawPngSha256'][name], name
        with Image.open(path) as im:
            rgba = im.convert('RGBA')
        assert rgba.size == (576, 576)
        bounds.append(rgba.getchannel('A').getbbox())
        images[name] = rgba
    box = (min(b[0] for b in bounds) - 6, min(b[1] for b in bounds) - 6,
           max(b[2] for b in bounds) + 6, max(b[3] for b in bounds) + 6)
    assert min(box[:2]) >= 0 and max(box[2:]) <= 576
    width, height = box[2] - box[0], box[3] - box[1]
    sheet_size = ((width + 12) * 4, (height + 32) * 2)
    assert sheet_size[0] * sheet_size[1] <= 2_000_000, sheet_size
    OUT.mkdir(exist_ok=True)
    font = ImageFont.truetype('C:/Windows/Fonts/arial.ttf', 16)
    pages = []
    for background, colour in BACKGROUNDS.items():
        (OUT / background).mkdir(exist_ok=True)
        for start in range(0, 672, 8):
            batch = poses[start:start + 8]
            sheet = Image.new('RGB', sheet_size, colour)
            draw = ImageDraw.Draw(sheet)
            for index, pose in enumerate(batch):
                x, y = index % 4 * (width + 12), index // 4 * (height + 32)
                flat = composite(images[pose['name']], colour)
                sheet.paste(flat.crop(box), (x, y + 32))
                draw.text((x + 3, y + 8), pose['name'] + ' | native1x', font=font,
                          fill='white' if background == 'Dark' else 'black')
            path = OUT / background / f'page-{start // 8:03d}.png'
            if path.exists():
                with Image.open(path) as existing:
                    assert existing.size == sheet.size and existing.convert('RGB').tobytes() == sheet.tobytes(), ('Existing own evidence differs', path)
            else:
                temporary = path.with_suffix('.writing.png')
                sheet.save(temporary)
                temporary.replace(path)
            pages.append({'background': background, 'page': start // 8,
                          'path': str(path), 'sha256': sha(path),
                          'names': [p['name'] for p in batch], 'viewed': False})
        print(f'{background}:84 native pages complete', flush=True)
    assert sha(SOURCE / 'attack-composition.json') == COMPOSITION_SHA
    assert sha(SOURCE / 'attack-poses.json') == POSES_SHA
    manifest = {
        'status': 'PREPARED; ACTUAL_NATIVE_VISUAL_REVIEW_PENDING',
        'source': str(SOURCE), 'sourceCompositionSha256': COMPOSITION_SHA,
        'sourcePosesSha256': POSES_SHA, 'sourcePainterSha256': c['painterSha256'],
        'sourceRawPngSha256': {p['name']: c['rawPngSha256'][p['name']] for p in poses},
        'newOddFrames': 672, 'backgrounds': BACKGROUNDS,
        'fixedNativeCrop': box, 'pixelsPerUnit': 224, 'sheetSize': sheet_size,
        'scale': 1, 'filters': None,
        'compositing': 'Independent straight-sRGB decode, linear alpha over, sRGB encode. No source edits.',
        'pages': pages,
    }
    (OUT / 'review-coverage.json').write_text(json.dumps(manifest, indent=2))
    print(json.dumps({'pages': len(pages), 'fixedNativeCrop': box, 'sheetSize': sheet_size,
                      'sourceFrames': 672, 'actualViewed': 0}))


if __name__ == '__main__':
    main()
