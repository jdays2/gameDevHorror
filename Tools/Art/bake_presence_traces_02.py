"""Subtle, offline floor traces. Contacts reference the Stalker's pointed bone tips."""
from pathlib import Path
import hashlib
import importlib.util
import json

import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont


ROOT = Path(__file__).resolve().parents[2]
DEST = ROOT / 'Concepts/Used/Environment/PresenceTraces02'
SOURCE = ROOT / 'Concepts/Unused/Visual/ProceduralBlood01/render_samples.py'
spec = importlib.util.spec_from_file_location('presence_noise', SOURCE)
noise = importlib.util.module_from_spec(spec)
spec.loader.exec_module(noise)
SIZE = 640
SEED = 21743
axis = np.linspace(-1, 1, SIZE, dtype=np.float32)
X, Y = np.meshgrid(axis, axis)
smoothstep = noise.smoothstep


def grain(rng, rows=100, cols=15):
    pixels = rng.integers(0, 256, (rows, cols), dtype=np.uint8)
    return np.asarray(Image.fromarray(pixels).resize((SIZE, SIZE), Image.Resampling.BICUBIC), dtype=np.float32) / 255


def brush(rng, curved):
    broad = noise.fbm(rng, SIZE, 6)
    fine = noise.fbm(rng, SIZE, 34)
    qx = X + (broad - .5) * .15
    qy = Y + (noise.fbm(rng, SIZE, 8) - .5) * .13
    if curved:
        qy -= .16 * np.sin(qx * 2.3)
    rx = .76
    ry = .13 if curved else .22
    distance = noise.ellipse(qx, qy, rx, ry)
    distance += (fine - .5) * .08
    shape = 1 - smoothstep(-.026, .034, distance)
    pressure = .24 + .56 * smoothstep(.25, .8, grain(rng))
    pressure *= .4 + .6 * fine
    pressure *= 1 - smoothstep(.39, .79, X)
    # Local abrasion and a few gaps rather than uniform noise over the floor.
    wear = smoothstep(.23, .57, grain(rng, 140, 25))
    alpha = shape * pressure * wear * .73
    return alpha, True


def bone_contact(rng, cx, cy, angle, scale, pressure, drag=False):
    dx, dy = X - cx, Y - cy
    x = (np.cos(angle) * dx + np.sin(angle) * dy) / scale
    y = (-np.sin(angle) * dx + np.cos(angle) * dy) / scale
    # One narrow pointed contact, not a palm, animal pad, or full leg silhouette.
    t = np.clip((x + .36) / .72, 0, 1)
    center = .1 * np.sin(t * np.pi)
    radius = (.11 * (1 - t) ** 1.4 + .007) * (.65 + .35 * np.sin(t * np.pi))
    distance = np.abs(y - center) - radius
    shape = (1 - smoothstep(-.008, .014, distance)) * smoothstep(-.42, -.28, x) * (1 - smoothstep(.3, .4, x))
    abrasion = .28 + .72 * smoothstep(.24, .66, noise.fbm(rng, SIZE, 70))
    alpha = shape * abrasion * pressure
    if drag:
        tail_y = .02 + .05 * np.sin(x * 2)
        tail = (1 - smoothstep(.012, .035, np.abs(y - tail_y))) * smoothstep(.25, .34, x) * (1 - smoothstep(.52, 1.3, x))
        alpha += tail * pressure * .35 * abrasion
    return alpha


def contacts(rng):
    alpha = np.zeros_like(X)
    for cx, cy, angle, size, pressure in [(-.46, -.17, .12, .39, .48), (.03, .2, -.48, .3, .24), (.49, -.08, .34, .32, .38)]:
        alpha = np.maximum(alpha, bone_contact(rng, cx, cy, angle, size, pressure, cx < 0))
    return alpha, True


def trail(rng, style):
    alpha = np.zeros_like(X)
    # Three leg pairs and two steps. Some clean/faint contacts leave only a trace.
    leg_offsets = [-.22, .015, .24]
    for cycle in range(2):
        for side in [-1, 1]:
            for leg, offset in enumerate(leg_offsets):
                if cycle == 0 and side == 1 and leg == 1:
                    continue
                cx = -.48 + cycle * .67 + offset + rng.uniform(-.035, .04)
                cy = side * (.32 + rng.uniform(-.055, .055))
                angle = side * (.45 - leg * .38) + rng.uniform(-.2, .2)
                if style == 'turn':
                    cy += .25 * np.sin((cx + .3) * 1.8)
                    angle += .45 * np.cos((cx + .3) * 1.8)
                pressure = rng.uniform(.12, .46)
                if leg == 1:
                    pressure *= .45
                scale = rng.uniform(.11, .18)
                mark = bone_contact(rng, cx, cy, angle, scale, pressure, cycle == 1 and leg == 2)
                alpha = np.maximum(alpha, mark)
    return alpha, style != 'dry'


def scrape(rng, split):
    alpha = np.zeros_like(X)
    if split:
        points = np.array([[-.77, -.1], [-.46, -.13], [-.2, -.08], [.06, -.1], [.3, -.01], [.6, .02], [.78, .09]])
        points[1:-1] += rng.normal(0, .012, (5, 2))
    else:
        t = np.linspace(0, 1, 42)
        points = np.stack([-.76 + t * 1.48, .06 * np.sin(t * np.pi) + .02 * t], axis=-1)
    for index in range(len(points) - 1):
        start, end = points[index], points[index + 1]
        taper = 1 - index / len(points) * .73
        radius = (.004 if split else .009) * taper
        d = noise.capsule(X, Y, start[0], start[1], end[0], end[1], radius)
        alpha = np.maximum(alpha, (1 - smoothstep(-.001, .004, d)) * (.43 if split else .3))
    if not split:
        secondary = bone_contact(rng, -.48, -.075, .04, .45, .1, True)
        alpha = np.maximum(alpha, secondary)
    alpha *= .38 + .62 * smoothstep(.22, .65, grain(rng, 130, 21))
    return alpha, False


def export(alpha, bloody, rng, path):
    alpha = np.clip(alpha, 0, .56)
    alpha *= 1 - smoothstep(.93, .99, np.maximum(np.abs(X), np.abs(Y)))
    if bloody:
        pigment = noise.fbm(rng, SIZE, 17)
        rgb = np.zeros((SIZE, SIZE, 3), dtype=np.float32)
        rgb[..., 0] = .29 + pigment * .13
        rgb[..., 1] = .011 + pigment * .009
        rgb[..., 2] = .024 + pigment * .013
        rgb *= (1 - alpha[..., None] * .17)
    else:
        rgb = np.full((SIZE, SIZE, 3), .12, dtype=np.float32)
    rgba = np.concatenate([rgb, alpha[..., None]], axis=-1)
    Image.fromarray(np.uint8(np.clip(rgba, 0, 1) * 255)).save(path)
    mask = np.full((SIZE, SIZE, 4), 255, dtype=np.uint8)
    mask[..., 3] = np.uint8(alpha * 255)
    Image.fromarray(mask).save(DEST / 'TintMasks' / path.name)


def preview(entries):
    width, row_height = 1160, 250
    canvas = Image.new('RGB', (width, row_height * 4 + 88), '#151d20')
    draw = ImageDraw.Draw(canvas)
    font = lambda size: ImageFont.truetype('C:/Windows/Fonts/arial.ttf', size)
    draw.text((22, 18), 'СЛЕДЫ ПРИСУТСТВИЯ · ПРОБА 02', font=font(26), fill='#d8e1de')
    draw.text((22, 53), 'Лёгкие разводы и контакт костяных остриёв · бетон / плитка', font=font(18), fill='#a7b7b7')
    floors = ROOT / 'Concepts/Used/Environment/FloorMaterials01'
    bases = [Image.open(floors / file).convert('RGBA').resize((640, 640), Image.Resampling.LANCZOS)
             for file in ['warehouse-concrete-base-01.png', 'lab-tile-base-01.png']]
    for index, entry in enumerate(entries):
        x = 14 + (index % 2) * 576
        y = 88 + (index // 2) * row_height
        texture = Image.open(DEST / 'Sprites' / (entry['id'] + '.png')).resize((272, 205), Image.Resampling.LANCZOS)
        for side, floor in enumerate(bases):
            background = floor.crop((60, 60, 332, 265))
            background.alpha_composite(texture)
            canvas.paste(background.convert('RGB'), (x + side * 278, y))
        draw.text((x + 2, y + 211), entry['label'], font=font(18), fill='#d8e1de')
    canvas.save(DEST / 'overview.png')


def main():
    (DEST / 'Sprites').mkdir(parents=True, exist_ok=True)
    (DEST / 'TintMasks').mkdir(exist_ok=True)
    specs = [
        ('blood-wipe-a', 'Тонкий неравномерный кровавый развод', 1.6, lambda rng: brush(rng, False)),
        ('blood-wipe-b', 'Стёртый смаз с просветами', 1.8, lambda rng: brush(rng, True)),
        ('bone-contact', 'Три разных зацепа костяного острия', .9, contacts),
        ('bone-trail-blood', 'Редкая цепочка кровавых контактов', 3.8, lambda rng: trail(rng, 'blood')),
        ('bone-trail-dry', 'Почти сухие следы на пыли', 3.8, lambda rng: trail(rng, 'dry')),
        ('bone-trail-turn', 'Неровная цепочка на повороте', 3.4, lambda rng: trail(rng, 'turn')),
        ('bone-drag-scrape', 'Короткий срыв костяного острия', 1.6, lambda rng: scrape(rng, False)),
        ('hairline-split', 'Тонкий одиночный разрыв поверхности', 1.9, lambda rng: scrape(rng, True)),
    ]
    entries = []
    for index, (name, label, width, build) in enumerate(specs):
        seed = SEED + index * 947
        rng = np.random.default_rng(seed)
        alpha, bloody = build(rng)
        export(alpha, bloody, rng, DEST / 'Sprites' / (name + '.png'))
        entries.append({'id': name, 'label': label, 'worldCanvasWidth': width, 'seed': seed, 'size': SIZE, 'bloody': bloody})
    preview(entries)
    metadata = {'date': '2026-10-17', 'method': 'Offline mathematical masks, pressure/abrasion fields; no AI generation',
                'generator': 'Tools/Art/bake_presence_traces_02.py', 'runtimeGeneration': False, 'entries': entries,
                'anatomicalReference': 'Concepts/Used/Stalker/Locomotion01/contact-sheet.png',
                'referenceInterpretation': 'One pointed bone tip per leg. The marks suggest point contact, not a complete limb imprint. No foot-lock/contact simulation exists.',
                'status': 'Game preview pending owner evaluation',
                'sha256': {p.relative_to(DEST).as_posix(): hashlib.sha256(p.read_bytes()).hexdigest() for p in sorted(DEST.rglob('*.png'))}}
    (DEST / 'provenance.json').write_text(json.dumps(metadata, ensure_ascii=False, indent=2) + '\n', encoding='utf-8', newline='\n')
    print(json.dumps({'textures': len(entries), 'directory': str(DEST)}))


if __name__ == '__main__':
    main()
