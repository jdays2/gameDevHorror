import hashlib
import json
import shutil
import subprocess
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

HERE = Path(__file__).resolve().parent
PROJECT = next((p for p in HERE.parents if (p / 'Assets/EmergencyShift').is_dir()), HERE.parents[1] / 'outputs/EmergencyShift')
if not HERE.is_relative_to(PROJECT) and not (HERE / 'assets-before.json').exists():
    (HERE / 'assets-before.json').write_text(json.dumps({p.relative_to(PROJECT).as_posix(): hashlib.sha256(p.read_bytes()).hexdigest() for p in (PROJECT / 'Assets/EmergencyShift').rglob('*') if p.is_file()}, indent=2))
    (HERE / 'git-before.txt').write_bytes(subprocess.check_output(['git', '-C', str(PROJECT), 'status', '--porcelain=v1']))
    shutil.copyfile(PROJECT / 'Assets/EmergencyShift/Scenes/MorgueRouteTest.unity', HERE / 'scene-before.unity')

family = PROJECT / 'Concepts/Used/Environment/CardAccessZone01'
art = PROJECT / 'Assets/EmergencyShift/Art/Environment/CardAccessZone01'
for folder in (family, art):
    folder.mkdir(parents=True, exist_ok=True)
width, depth, ppu = 3, .9, 512
w, h = round(width * ppu), round(depth * ppu)
x, y = np.meshgrid((np.arange(w) + .5) / ppu, (np.arange(h) + .5) / ppu)
base = np.ones((h, w), dtype=bool)
band = base & ((x < .23) | (x > width - .23) | (y > depth - .23))
stripe = band & (((x + y) % .42) < .21)

# Deliberate wear: chipped leading edge and two unequal shoe rubs across the
# central walking path. Same mask removes white and colored paint together.
wear = Image.new('L', (w, h), 255)
draw = ImageDraw.Draw(wear)
def polygon(points, opacity):
    draw.polygon([(round(px * ppu), round(py * ppu)) for px, py in points], fill=opacity)

chips = [
    [(0, .24), (.06, .27), (.038, .30), (.09, .33), (.05, .36), (.015, .41), (0, .40)],
    [(.42, .9), (.45, .85), (.50, .86), (.53, .81), (.57, .85), (.60, .84), (.64, .9)],
    [(2.35, .9), (2.38, .84), (2.43, .85), (2.47, .80), (2.51, .85), (2.58, .86), (2.61, .9)],
    [(2.99, .10), (2.94, .13), (2.95, .18), (2.9, .21), (2.96, .23), (2.99, .28)],
]
for points in chips:
    polygon(points, 0)
polygon([(.82, .34), (.88, .28), (.97, .31), (1.01, .28), (1.09, .36), (1.04, .42), (.96, .4), (.91, .44), (.86, .40)], 185)
polygon([(1.72, .52), (1.81, .47), (1.90, .49), (2.02, .44), (2.16, .49), (2.14, .55), (2.05, .55), (2.01, .59), (1.86, .57), (1.79, .59)], 205)
polygon([(1.18, .63), (1.25, .59), (1.34, .61), (1.35, .64), (1.28, .68), (1.20, .67)], 225)
for points, thickness, opacity in [([(.91, .30), (.95, .34), (1.03, .37)], .012, 15), ([(1.81, .48), (1.88, .52), (2.05, .53)], .017, 25), ([(1.03, .70), (1.11, .73), (1.20, .74)], .012, 60), ([(2.23, .66), (2.27, .73), (2.31, .76)], .008, 40)]:
    draw.line([(round(px * ppu), round(py * ppu)) for px, py in points], fill=opacity, width=round(thickness * ppu))
wear = np.asarray(wear.filter(ImageFilter.GaussianBlur(7)), dtype=np.float32) / 255
wear = 1 - (1 - wear) * .32
paint = Image.open(PROJECT / 'Assets/EmergencyShift/Art/Environment/FloorMarkings/thin-straight.png').getchannel('A')
rows = np.where(np.asarray(paint).mean(axis=1) > 100)[0]
grain = np.asarray(paint.crop((0, int(rows[0]), 512, int(rows[-1]) + 1)).resize((w, h))) / 255
wear *= .94 + .06 * grain
entries = []
for name, mask in [('white-foundation', base), ('access-stripes', stripe)]:
    image = Image.new('RGBA', (w, h), (255, 255, 255, 0))
    image.putalpha(Image.fromarray((mask * wear * 255).astype('uint8')))
    image.save(art / (name + '.png'))
    shutil.copyfile(art / (name + '.png'), family / (name + '.png'))
    entries.append(dict(path=(art / (name + '.png')).relative_to(PROJECT).as_posix(), sha256=hashlib.sha256((art / (name + '.png')).read_bytes()).hexdigest()))
provenance = dict(date='2026-10-19', status='Owner selected B; shortened worn implementation under evaluation', author='Original project technical paint geometry and deliberate alpha-cut wear', source='Concepts/Used/Environment/AccessDoorTrial01/B-striped-panel-*.png (shape reference); FloorMarkings/thin-straight.png (paint grain)', method='Rebuild original coded B geometry at 3x0.9 units; keep 0.42-unit diagonal pitch and reduce border to 0.23; shared alpha damage affects both paint layers; softened after owner feedback to 32 percent strength with 7-pixel feather, retaining at least 68 percent paint coverage before grain; no baked light/shadow', features=['four unequal softly worn edges with partially visible real floor', 'two differently shaped faint shoe rubs across the middle', 'one softer rub and four subtle short scuffs'], historicalDepth=1.5, depth=.9, ppu=ppu, wearStrength=.32, featherPixels=7, output=entries)
(family / 'provenance.json').write_text(json.dumps(provenance, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
if Path(__file__).resolve() != (family / 'prepare.py').resolve():
    shutil.copyfile(__file__, family / 'prepare.py')
print(json.dumps(provenance, ensure_ascii=False, indent=2))
