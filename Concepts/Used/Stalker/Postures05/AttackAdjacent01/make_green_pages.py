"""All672 new bridge frames at one fixed native scale, no fitting or filtering."""
from pathlib import Path
import hashlib
import json
import sys
import numpy as np
from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE.parent))
from frame_colors import decode


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    composition_sha = digest(HERE/'attack-composition.json')
    composition = json.loads((HERE/'attack-composition.json').read_text())
    assert composition['poses'] == 1464 and composition['newOddPoses'] == 672
    poses = [p for p in json.loads((HERE/'attack-poses.json').read_text()) if p['group'] != 'strike' and p['sample']%2]
    out = HERE/'ReviewNative/Green'
    out.mkdir(parents=True, exist_ok=False)
    images, bounds = {}, []
    for pose in poses:
        name = pose['name']
        path = HERE/'Raw'/f'{name}.png'
        assert digest(path) == composition['rawPngSha256'][name]
        with Image.open(path) as image:
            rgba = image.convert('RGBA')
            alpha = np.asarray(rgba)[..., 3]
            yy, xx = np.nonzero(alpha)
            bounds.append([xx.min(), yy.min(), xx.max()+1, yy.max()+1])
            premul = decode(rgba)
            rgb = premul[..., :3]+np.array([0, 1, 0])*(1-premul[..., 3:4])
            rgb = np.where(rgb <= .0031308, rgb*12.92, 1.055*np.maximum(rgb, 0)**(1/2.4)-.055)
            images[name] = Image.fromarray(np.rint(np.clip(rgb, 0, 1)*255).astype('uint8'))
    box = [int(min(b[0] for b in bounds))-6, int(min(b[1] for b in bounds))-6,
           int(max(b[2] for b in bounds))+6, int(max(b[3] for b in bounds))+6]
    width, height = box[2]-box[0], box[3]-box[1]
    font = ImageFont.truetype('C:/Windows/Fonts/arial.ttf', 16)
    pages = []
    for start in range(0, len(poses), 8):
        batch = poses[start:start+8]
        sheet = Image.new('RGB', ((width+12)*4, (height+32)*2), '#00ff00')
        draw = ImageDraw.Draw(sheet)
        for index, pose in enumerate(batch):
            x, y = index%4*(width+12), index//4*(height+32)
            sheet.paste(images[pose['name']].crop(box), (x, y+32))
            draw.text((x+4, y+7), pose['name']+' | native1x', font=font, fill='black')
        path = out/f'new-{start:03d}-{start+7:03d}.png'
        sheet.save(path)
        pages.append({'page': str(path.relative_to(HERE)), 'sha256': digest(path), 'names': [p['name'] for p in batch]})
    assert len(pages) == 84
    assert digest(HERE/'attack-composition.json') == composition_sha
    manifest = {'compositionSha256': composition_sha, 'frames': 672, 'pages': pages,
        'fixedNativeCrop': box, 'pixelsPerUnit': 224, 'resizing': False,
        'rawPngSha256': {p['name']: composition['rawPngSha256'][p['name']] for p in poses},
        'status': 'Prepared; actual inspection is recorded separately.'}
    (HERE/'ReviewNative/green-manifest.json').write_text(json.dumps(manifest, indent=2))
    print('READY84 native green pages,672 new odd frames, crop', box, flush=True)


if __name__ == '__main__':
    main()
