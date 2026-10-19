import hashlib
import json
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw

HERE = Path(__file__).resolve().parent
PROJECT = next((p for p in HERE.parents if (p / 'Assets/EmergencyShift').is_dir()), HERE.parents[1] / 'outputs/EmergencyShift')
family = PROJECT / 'Concepts/Used/Environment/AccessDoorTrial01'
destination = PROJECT / 'Assets/EmergencyShift/Art/Environment/AccessDoorTrial01'
texture = Image.open(PROJECT / 'Assets/EmergencyShift/Art/Environment/FloorMarkings/thin-straight.png').getchannel('A')
pixels = np.array(texture)
rows = np.where(pixels.mean(axis=1) > 100)[0]
texture = texture.crop((0, int(rows[0]), 512, int(rows[-1]) + 1))
records = []
for name, height in [('A-solid-panel', .9), ('B-striped-panel', 1.5), ('C-double-outline', 1.2)]:
    width = 3
    w, h = round(width * 512), round(height * 512)
    x, y = np.meshgrid((np.arange(w) + .5) / 512, (np.arange(h) + .5) / 512)
    outer = (x >= .03) & (x <= width - .03) & (y >= .03) & (y <= height - .03)
    if name.startswith('A'):
        base = outer
        color = outer & (x >= .10) & (x <= width - .10) & (y >= .10) & (y <= height - .10)
        color &= ~((x > .20) & (x < width - .20) & (y > .20) & (y < height - .20))
        paths = '<rect x=".03" y=".03" width="2.94" height=".84"/>', '<rect x=".15" y=".15" width="2.7" height=".6" fill="none" stroke="white" stroke-width=".1"/>'
    elif name.startswith('B'):
        base = outer
        band = outer & ((x < .30) | (x > width - .30) | (y > height - .30))
        color = band & (((x + y) % .42) < .21)
        paths = '<rect x=".03" y=".03" width="2.94" height="1.44"/>', '<defs><pattern id="p" width=".42" height=".42" patternUnits="userSpaceOnUse" patternTransform="rotate(-45)"><rect width=".21" height=".42" fill="white"/></pattern></defs><path d="M.165 .03V1.335H2.835V.03" fill="none" stroke="url(#p)" stroke-width=".27"/>'
    else:
        base = outer & ((x < .25) | (x > width - .25) | (y > height - .25))
        color = outer & (x >= .09) & (x <= width - .09) & (y <= height - .09)
        color &= ((x < .19) | (x > width - .19) | (y > height - .19))
        paths = '<path d="M.14 .03V1.06H2.86V.03" fill="none" stroke="white" stroke-width=".22"/>', '<path d="M.14 .03V1.06H2.86V.03" fill="none" stroke="white" stroke-width=".10"/>'
    wear = .94 + .06 * np.asarray(texture.resize((w, h))) / 255
    for layer, mask, svg in zip(['base', 'color'], [base, color], paths):
        image = Image.new('RGBA', (w, h), (255, 255, 255, 0))
        image.putalpha(Image.fromarray((mask * wear * 255).astype('uint8')))
        filename = f'{name}-{layer}.png'
        image.save(destination / filename)
        image.save(family / filename)
        (family / f'{name}-{layer}.svg').write_text(f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {width} {height}"><g fill="white">{svg}</g></svg>')
        records.append(dict(path=(destination / filename).relative_to(PROJECT).as_posix(), sha256=hashlib.sha256((destination / filename).read_bytes()).hexdigest(), dimensions=[w, h], ppu=512))
provenance = json.loads((family / 'provenance.json').read_text())
provenance['revision'] = '02: owner rejected the card pictogram; compare three text-free white/colored paint treatments'
provenance['markingVariants'] = records
provenance['historical'] = ['card-stencil.png', 'card-stencil.svg: rejected initial icon, removed from production scene']
(family / 'provenance.json').write_text(json.dumps(provenance, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
if Path(__file__).resolve() != (family / 'marking_variants.py').resolve():
    (family / 'marking_variants.py').write_bytes(Path(__file__).read_bytes())
print(json.dumps(records, indent=2))
