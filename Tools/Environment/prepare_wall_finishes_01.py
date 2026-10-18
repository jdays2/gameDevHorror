"""Deterministic small wall-face materials, not edits of the reference photographs."""
from pathlib import Path
import hashlib
import json
import math
import random
import shutil
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / 'Concepts/Used/Environment/WallFinishes01'
TARGET = ROOT / 'Assets/EmergencyShift/Art/Environment/WallFinishes01'
WIDTH, HEIGHT = 1024, 128
REPEAT_LENGTH, DEPTH = 1.6, .22


def surface(kind):
    rng = random.Random(1401 + kind)
    image = Image.new('RGBA', (WIDTH, HEIGHT))
    pixels = image.load()
    tile_tones = [rng.uniform(-5, 5) for _ in range(16)]
    for y in range(HEIGHT):
        v = y / (HEIGHT - 1)
        for x in range(WIDTH):
            u = x / WIDTH
            # Very low contrast glaze/roller variation; seams remain straight and periodic.
            tone = 1.2 * math.sin(2 * math.pi * u * 3 + v * 3)
            tone += .6 * math.cos(2 * math.pi * u * 11 - v * 5)
            if kind == 0:
                row = 0 if y < 53 else 1
                pos = (x + row * 64) % 128
                tile = ((x + row * 64) // 128) % 8
                base = (177, 187, 169)
                tone += tile_tones[row * 8 + tile]
                if pos < 3 or 51 <= y <= 54:
                    base = (92, 102, 91)
                    tone = 0
                elif pos < 6 or y in (3, 55):
                    tone += 6
                # One faint replacement-glaze patch, not uniform distressed noise.
                if row == 1 and tile == 5:
                    tone += 4
            elif kind == 1:
                base = (127, 156, 139)
                if 88 <= y < 92:
                    base = (112, 137, 123)
                tone += 1.0 * math.sin(v * 10 + u * math.pi * 2)
                if 170 < x < 240 and 65 < y < 77:
                    tone -= 2.5 * math.sin((x - 170) / 70 * math.pi)
            else:
                base = (186, 192, 188)
                if x % 512 < 3:
                    base = (116, 125, 122)
                if 62 <= y < 72:
                    base = (104, 120, 120)
                if 72 <= y < 75:
                    tone += 4
                if x > 512:
                    tone -= 2
            # Quiet upper junction and a narrow, distinct skirting at the floor edge.
            if y < 3:
                base, tone = (60, 70, 66), 0
            if y >= 109:
                base = ((76, 87, 80), (69, 91, 79), (87, 103, 102))[kind]
                tone *= .4
            if y == 109:
                tone += 10
            if y >= 125:
                base, tone = (37, 45, 43), 0
            pixels[x, y] = tuple(max(0, min(255, round(c + tone))) for c in base) + (255,)
    return image


def corner(straight):
    """North-west interior join: upper/left wall edges, floor towards lower/right."""
    image = Image.new('RGBA', (128, 128))
    for y in range(128):
        for x in range(128):
            distance = min(x, y)
            along = x if y < x else y
            image.putpixel((x, y), straight.getpixel((along, distance)))
    return image


def main():
    sprites = SOURCE / 'Sprites'
    sprites.mkdir(parents=True, exist_ok=True)
    TARGET.mkdir(parents=True, exist_ok=True)
    records = []
    for kind, name in enumerate(['morgue-tile', 'corridor-paint', 'medical-panel']):
        straight = surface(kind)
        # Utility end cap uses the same edge/skirting colors, no external image edits.
        cap = Image.new('RGBA', (24, 128))
        for y in range(128):
            for x in range(24):
                color = straight.getpixel((8, y))
                if x < 3:
                    color = tuple(round(c * .65) for c in color[:3]) + (255,)
                cap.putpixel((x, y), color)
        for suffix, image in [('straight', straight), ('corner', corner(straight)), ('end', cap)]:
            path = sprites / (name + '-' + suffix + '.png')
            image.save(path)
            shutil.copyfile(path, TARGET / path.name)
            records.append({'id': path.stem, 'size': list(image.size), 'sha256': hashlib.sha256(path.read_bytes()).hexdigest()})
    (SOURCE / 'provenance.json').write_text(json.dumps({
        'date': '2026-10-18', 'method': 'offline deterministic mathematical materials; Pillow, no AI generation or photo pixels',
        'source': 'Tools/Environment/prepare_wall_finishes_01.py', 'repeatLengthUnits': REPEAT_LENGTH,
        'faceDepthUnits': DEPTH, 'invariants': 'straight functional seams; repeated horizontal span; neutral diffuse colors; no baked room lighting',
        'individualFeatures': ['one replacement-glaze tile tone', 'one restrained roller/wipe patch', 'slightly different adjacent panel tones'],
        'outputs': records}, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    print(json.dumps({'sprites': len(records), 'source': str(SOURCE), 'target': str(TARGET)}))


if __name__ == '__main__':
    main()
