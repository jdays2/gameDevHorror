"""Offline geometric vinyl seams, caster paths and repair panels for FloorWear01.

Native authored masks, extending SurfaceDamage02. No AI source or runtime generation.
"""
from pathlib import Path
import hashlib
import json

import numpy as np
from PIL import Image, ImageDraw, ImageFont


ROOT = Path(__file__).resolve().parents[2]
FAMILY = ROOT / 'Concepts/Used/Environment/CorridorFloorWear02'
ART = ROOT / 'Assets/EmergencyShift/Art/Environment/FloorWear01'
SCALE = 3
ENTRIES = []


def save(name, image, feature):
    image = image.resize((image.width // SCALE, image.height // SCALE), Image.Resampling.LANCZOS)
    # Preserve clean hidden RGB for alpha sampling; these are tintable geometric masks.
    pixels = np.asarray(image).copy()
    pixels[pixels[:, :, 3] == 0, :3] = 255
    image = Image.fromarray(pixels)
    for folder in (FAMILY / 'Sprites', ART):
        folder.mkdir(parents=True, exist_ok=True)
        image.save(folder / f'{name}.png')
    ENTRIES.append({'id': name, 'pixels': list(image.size), 'pixelsPerUnit': 256,
                    'feature': feature, 'sha256': hashlib.sha256((ART / f'{name}.png').read_bytes()).hexdigest()})


def canvas(width, height):
    return Image.new('RGBA', (width * SCALE, height * SCALE), (255, 255, 255, 0))


def stroke(draw, points, width, opacity, color=(255, 255, 255)):
    draw.line([(round(x * SCALE), round(y * SCALE)) for x, y in points],
              fill=(*color, round(opacity * 255)), width=max(1, round(width * SCALE)), joint='curve')


def seam():
    image = canvas(512, 32)
    draw = ImageDraw.Draw(image)
    # Continuous end sockets; the weld stays straight, local contact pressure varies.
    stroke(draw, [(0, 16), (512, 16)], 5, .54)
    stroke(draw, [(78, 16), (151, 16)], 6, .64)
    stroke(draw, [(307, 16), (350, 16)], 4, .40)
    save('vinyl-weld-seam', image, 'Straight thin weld; continuous ends, two restrained pressure changes, no raised lip.')


def tracks(turn=False):
    width, height = (704, 480) if turn else (768, 320)
    image = canvas(width, height)
    draw = ImageDraw.Draw(image)
    rng = np.random.default_rng(7129 if turn else 7133)
    for wheel in range(2):
        # Two related wheel paths, with differing contact gaps and pressure.
        for trace in range(3):
            lateral = (trace - 1) * 2.3
            begin = rng.uniform(.015, .07)
            while begin < .97:
                end = min(.98, begin + rng.uniform(.07, .26))
                points = []
                for t in np.linspace(begin, end, 10):
                    if turn:
                        angle = -.10 + t * 1.1
                        radius = 250 + wheel * 150 + lateral
                        x = 78 + radius * np.sin(angle)
                        y = 28 + radius * (1 - np.cos(angle)) + wheel * 95
                    else:
                        x = 35 + t * 698
                        y = 80 + wheel * 155 + lateral + 3 * np.sin(t * 4.2)
                    points.append((x, y))
                fade = np.sin(np.pi * (.08 + .84 * (begin + end) / 2))
                stroke(draw, points, rng.uniform(2, 4.6), rng.uniform(.20, .48) * fade)
                begin = end + rng.uniform(.012, .075)
    save('wheel-tracks-turn' if turn else 'wheel-tracks-straight', image,
         'Paired caster paths; unequal contact gaps, soft pressure taper; no tyre tread or footprints.')


def patch(strip=False):
    width, height = (640, 192) if strip else (512, 352)
    image = canvas(width, height)
    draw = ImageDraw.Draw(image)
    if strip:
        points = [(23, 38), (599, 35), (616, 45), (613, 151), (32, 156), (23, 145)]
        body = (123, 143, 151, 110)
    else:
        points = [(33, 25), (462, 28), (479, 40), (475, 319), (41, 322), (32, 309)]
        body = (119, 140, 154, 110)
    draw.polygon([(x * SCALE, y * SCALE) for x, y in points], fill=body)
    # A cut/weld perimeter, not a baked shadow. One clipped corner per repair.
    stroke(draw, points + [points[0]], 3.8, .32, (51, 65, 73))
    if strip:
        stroke(draw, [(149, 101), (239, 103)], 3.5, .07, (77, 91, 98))
        stroke(draw, [(410, 124), (468, 123)], 2.4, .05, (77, 91, 98))
    else:
        stroke(draw, [(350, 216), (413, 222)], 3.6, .07, (67, 85, 95))
        stroke(draw, [(102, 74), (159, 75)], 2.5, .05, (67, 85, 95))
    save('vinyl-patch-strip' if strip else 'vinyl-patch-panel', image,
         'Flat replacement vinyl, slightly different batch tone; clipped corner, welded edge, two local handling rubs. No lighting baked in.')


def preview():
    board = Image.new('RGB', (1200, 650), '#1b242b')
    draw = ImageDraw.Draw(board)
    font = ImageFont.truetype('C:/Windows/Fonts/arial.ttf', 20)
    draw.text((24, 16), 'CORRIDOR FLOOR WEAR 02 / native overlay library', font=font, fill='#d1d9dc')
    for index, entry in enumerate(ENTRIES):
        x = 22 + index % 3 * 396
        y = 64 + index // 3 * 288
        floor = Image.new('RGBA', (372, 235), (109, 131, 148, 255))
        overlay = Image.open(FAMILY / 'Sprites' / f'{entry["id"]}.png').convert('RGBA')
        if entry['id'].startswith(('wheel-', 'vinyl-weld')):
            rgba = np.asarray(overlay).copy()
            rgba[:, :, :3] = (29, 42, 50)
            overlay = Image.fromarray(rgba)
        overlay.thumbnail((348, 205), Image.Resampling.LANCZOS)
        floor.alpha_composite(overlay, ((372-overlay.width)//2, (235-overlay.height)//2))
        board.paste(floor.convert('RGB'), (x, y))
        draw.text((x, y+241), entry['id'], font=font, fill='#d1d9dc')
    (FAMILY / 'Previews').mkdir(parents=True, exist_ok=True)
    board.save(FAMILY / 'Previews/library.png')


def main():
    seam()
    tracks()
    tracks(turn=True)
    patch()
    patch(strip=True)
    preview()
    report = {'date': '2026-10-19', 'method': 'Native geometric raster masks with supersampled paths and polygons; no imagegen.',
              'generator': 'Tools/Art/bake_corridor_floor_wear_02.py', 'runtimeGeneration': False,
              'library': 'Assets/EmergencyShift/Art/Environment/FloorWear01',
              'existingReuse': 'caster-scuff prefab references the existing FloorWear01/castor-scuff.png unchanged.',
              'entries': ENTRIES}
    (FAMILY / 'provenance.json').write_text(json.dumps(report, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    print(json.dumps({'newSprites': len(ENTRIES), 'family': str(FAMILY)}))


if __name__ == '__main__':
    main()
