"""Offline, seeded transparent floor damage and struggle studies. No Unity changes."""
from pathlib import Path
import hashlib
import importlib.util
import json

import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont


ROOT = Path(__file__).resolve().parents[2]
DEST = ROOT / "Concepts/Unused/Environment/FloorAftermath01"
NOISE_SOURCE = ROOT / "Concepts/Unused/Visual/ProceduralBlood01/render_samples.py"
spec = importlib.util.spec_from_file_location("blood_noise", NOISE_SOURCE)
blood_noise = importlib.util.module_from_spec(spec)
spec.loader.exec_module(blood_noise)
SIZE = 512
SEED = 75412
axis = np.linspace(-1, 1, SIZE, dtype=np.float32)
X, Y = np.meshgrid(axis, axis)
smoothstep = blood_noise.smoothstep


def coverage(distance):
    return 1 - smoothstep(-.003, .006, distance)


def shift(array, dx, dy):
    result = np.zeros_like(array)
    result[max(dy, 0):min(SIZE + dy, SIZE), max(dx, 0):min(SIZE + dx, SIZE)] = array[max(-dy, 0):min(SIZE - dy, SIZE), max(-dx, 0):min(SIZE - dx, SIZE)]
    return result


class Paint:
    def __init__(self):
        self.premultiplied = np.zeros((SIZE, SIZE, 3), dtype=np.float32)
        self.alpha = np.zeros((SIZE, SIZE), dtype=np.float32)

    def over(self, color, alpha):
        alpha = np.clip(alpha, 0, 1)
        self.premultiplied = np.asarray(color) * alpha[..., None] + self.premultiplied * (1 - alpha[..., None])
        self.alpha = alpha + self.alpha * (1 - alpha)

    def export(self, path):
        fade = 1 - smoothstep(.93, .99, np.maximum(np.abs(X), np.abs(Y)))
        alpha = self.alpha * fade
        rgb = self.premultiplied / np.maximum(self.alpha[..., None], 1e-8)
        rgba = np.concatenate([rgb, alpha[..., None]], axis=-1)
        Image.fromarray(np.uint8(np.clip(rgba, 0, 1) * 255)).save(path)
        mask = np.ones((SIZE, SIZE, 4), dtype=np.uint8) * 255
        mask[..., 3] = np.uint8(np.clip(alpha, 0, 1) * 255)
        Image.fromarray(mask).save(DEST / "TintMasks" / path.name)


def path_mask(points, width, taper=True):
    distance = np.full((SIZE, SIZE), 100, dtype=np.float32)
    for index in range(len(points) - 1):
        start, end = points[index], points[index + 1]
        progress = (index + .5) / (len(points) - 1)
        radius = width * (.92 - .76 * progress if taper else 1)
        segment = blood_noise.capsule(X, Y, start[0], start[1], end[0], end[1], max(.002, radius))
        distance = np.minimum(distance, segment)
    return coverage(distance)


def wander(rng, start, end, count=15, jitter=.025):
    values = np.linspace(0, 1, count)
    points = np.asarray(start)[None, :] * (1 - values[:, None]) + np.asarray(end)[None, :] * values[:, None]
    points[1:-1] += rng.normal(0, jitter, (count - 2, 2))
    return points


def scratch_paint(mask, rng, strength=.86):
    paint = Paint()
    grain = blood_noise.fbm(rng, SIZE, 32)
    rim = np.maximum(shift(mask, 1, 2) - mask, 0)
    paint.over([.83, .82, .77], rim * .62)
    paint.over([.12, .12, .115], mask * (.52 + grain * .45) * strength)
    return paint


def claws(rng, heavy):
    mask = np.zeros_like(X)
    offsets = [-.17, -.015, .19] if not heavy else [-.22, .07]
    for index, offset in enumerate(offsets):
        start = (-.72 + index * .045, offset - rng.uniform(.01, .08))
        end = (.58 - index * .09, offset + rng.uniform(.04, .17))
        t = np.linspace(0, 1, 30)
        points = np.asarray(start)[None, :] * (1 - t[:, None]) + np.asarray(end)[None, :] * t[:, None]
        points[:, 1] += np.sin(t * np.pi) * (.075 if heavy else .035)
        points[:, 1] += np.interp(t, np.linspace(0, 1, 7), rng.normal(0, .008, 7))
        line = path_mask(points, .015 if not heavy else .032)
        abrasion = .66 + .34 * blood_noise.fbm(rng, SIZE, 45)
        mask = np.maximum(mask, line * abrasion)
    if heavy:
        mask = np.maximum(mask, path_mask(wander(rng, (-.52, .3), (.38, -.23), 12, .016), .008))
    return scratch_paint(mask, rng)


def cracks(rng, impact):
    mask = np.zeros_like(X)
    if impact:
        center = np.array([-.08, .03])
        for angle, length in zip([-.12, .8, 2, 3.25, 4.5, 5.3], [.74, .51, .76, .58, .43, .8]):
            end = center + np.array([np.cos(angle), np.sin(angle)]) * length
            points = wander(rng, center, end, 13, .022)
            mask = np.maximum(mask, path_mask(points, rng.uniform(.012, .021)))
            branch_start = points[6]
            branch_end = branch_start + np.array([np.cos(angle + .8), np.sin(angle + .8)]) * .21
            mask = np.maximum(mask, path_mask(wander(rng, branch_start, branch_end, 7, .016), .007))
    else:
        points = wander(rng, (-.82, .35), (.78, -.27), 24, .035)
        mask = path_mask(points, .017)
        for index, sign, length in [(6, -1, .31), (11, 1, .4), (17, -1, .21)]:
            start = points[index]
            end = start + np.array([.12, sign * length])
            mask = np.maximum(mask, path_mask(wander(rng, start, end, 10, .022), .009))
    return scratch_paint(mask, rng, .98)


def chip_mask(rng, elongated=False):
    scaled_x = X / (.7 if elongated else .42)
    scaled_y = Y / (.23 if elongated else .36)
    theta = np.arctan2(scaled_y, scaled_x)
    radius = np.hypot(scaled_x, scaled_y)
    angles = np.linspace(-np.pi, np.pi, 23)
    radii = rng.uniform(.79, 1.15, 23)
    radii[-1] = radii[0]
    contour = np.interp(theta, angles, radii)
    contour += (blood_noise.fbm(rng, SIZE, 24) - .5) * .11
    return coverage((radius - contour) * .22), radius


def chips(rng, elongated):
    mask, radius = chip_mask(rng, elongated)
    paint = Paint()
    grain = blood_noise.fbm(rng, SIZE, 45)
    planes = blood_noise.noise_field(rng, SIZE, 9)
    inner = .2 + grain * .17 + planes * .07
    color = np.stack([inner, inner * .98, inner * .93], axis=-1)
    paint.over(color, mask * .88)
    interior_shadow = np.clip(1 - radius, 0, 1) * mask * .37
    paint.over([.035, .035, .03], interior_shadow)
    rim = np.maximum(shift(mask, 3, 5) - mask, 0)
    paint.over([.7, .69, .64], rim * .93)
    paint.over([.1, .1, .085], np.maximum(shift(mask, -4, -3) - mask, 0) * .76)
    for index in range(18 if not elongated else 10):
        angle = rng.uniform(0, np.pi * 2)
        distance = rng.uniform(.43, .73)
        cx, cy = np.cos(angle) * distance, np.sin(angle) * distance * .8
        size = rng.uniform(.012, .045)
        polygon = []
        for vertex in range(4):
            angle = vertex * np.pi / 2 + rng.uniform(-.4, .4)
            polygon.append(((cx + np.cos(angle) * size) * SIZE / 2 + SIZE / 2,
                            (cy + np.sin(angle) * size * .8) * SIZE / 2 + SIZE / 2))
        shard_image = Image.new('L', (SIZE, SIZE))
        ImageDraw.Draw(shard_image).polygon(polygon, fill=255)
        shard = np.asarray(shard_image, dtype=np.float32) / 255
        paint.over([.39, .38, .35], shard * rng.uniform(.65, .9))
        paint.over([.76, .74, .67], np.maximum(shift(shard, 1, 1) - shard, 0) * .47)
    return paint


def dent(rng):
    radius = np.sqrt(((X + Y * .18) / .53) ** 2 + (Y / .28) ** 2)
    height = np.clip(1 - radius ** 2, 0, 1) ** 2
    gy, gx = np.gradient(height)
    shade = (gx + gy * .7) * 12
    paint = Paint()
    paint.over([.06, .06, .065], height * .11 + np.clip(shade, 0, .36))
    paint.over([.83, .84, .84], np.clip(-shade, 0, .29))
    mask = path_mask(wander(rng, (-.38, .03), (.35, -.05), 14, .005), .004)
    paint.over([.12, .12, .12], mask * .2)
    return paint


def scuff(rng, shoe):
    if shoe:
        rx = (X + .03) / .56
        ry = (Y - .02) / .31
        radius = np.sqrt(rx ** 2 + ry ** 2)
        band = smoothstep(.57, .73, radius) * (1 - smoothstep(.95, 1.08, radius))
        band *= smoothstep(-.05, .18, Y)
        grain = blood_noise.fbm(rng, SIZE, 44)
        band *= smoothstep(.28, .56, grain)
        paint = Paint()
        paint.over([.045, .045, .04], band * .61)
        return paint
    mask = np.zeros_like(X)
    for index in range(5):
        t = np.linspace(0, 1, 24)
        x = -.65 + t * rng.uniform(.86, 1.3)
        y = rng.uniform(-.15, .12) + np.sin(t * np.pi) * rng.uniform(.02, .06)
        y += rng.normal(0, .006, t.shape)
        line = path_mask(np.stack([x, y], axis=-1), rng.uniform(.004, .012), taper=False)
        broken = .2 + .8 * smoothstep(.28, .62, blood_noise.fbm(rng, SIZE, 30))
        mask = np.maximum(mask, line * broken)
    paint = Paint()
    paint.over([.055, .055, .05], mask * (.67 if shoe else .48))
    if not shoe:
        paint.over([.78, .78, .73], np.maximum(shift(mask, 1, 1) - mask, 0) * .25)
    return paint


def blood_material(density, rng):
    veins = blood_noise.fbm(rng, SIZE, 19)
    grain = blood_noise.fbm(rng, SIZE, 85)
    alpha = 1 - np.exp(-density * 1.75)
    thick = 1 - np.exp(-density * .57)
    rgb = np.array([.47, .015, .03]) * (1 - thick[..., None]) + np.array([.125, .004, .012]) * thick[..., None]
    rgb *= (.75 + .38 * veins + .12 * grain)[..., None]
    height = 1 - np.exp(-density * 1.1)
    height = np.asarray(Image.fromarray(np.uint8(height * 255)).filter(ImageFilter.GaussianBlur(1.2)), dtype=np.float32) / 255
    gy, gx = np.gradient(height, 2 / SIZE)
    normal = np.stack([-gx * .18, -gy * .18, np.ones_like(gx)], axis=-1)
    normal /= np.linalg.norm(normal, axis=-1, keepdims=True)
    direction = np.array([-.24, -.30, 1.5])
    direction /= np.linalg.norm(direction)
    specular = np.maximum(0, normal @ direction) ** 112
    specular *= smoothstep(.4, 1.4, density) * smoothstep(.37, .66, veins)
    rgb += specular[..., None] * np.array([.66, .46, .43]) * .3
    paint = Paint()
    paint.over(rgb, alpha)
    return paint


def blood_traces(rng, style):
    density = np.zeros_like(X)
    if style == 'hand':
        mask = coverage(blood_noise.ellipse(X + .2, Y - .04, .2, .23))
        fingers = [((-.35, .04), (-.57, -.08), .047), ((-.3, -.08), (-.4, -.47), .036),
                   ((-.19, -.12), (-.18, -.56), .04), ((-.07, -.09), (.03, -.48), .037), ((.03, -.01), (.16, -.3), .03)]
        for start, end, width in fingers:
            mask = np.maximum(mask, path_mask([start, end], width, taper=False))
        density = mask * 1.25
        for start, end, width in fingers:
            finish = (end[0] + .43, end[1] + .11)
            density += path_mask(wander(rng, end, finish, 12, .004), width * .55) * .24
        palm_trail = coverage(blood_noise.ellipse(X - .12, Y - .1, .44, .16))
        palm_trail *= 1 - smoothstep(-.03, .64, X)
        density += palm_trail * .4
    else:
        count = 3 if style == 'drag' else 7
        for index in range(count):
            offset = (index - (count - 1) / 2) * (.12 if style == 'drag' else .055)
            start = (-.76 + rng.uniform(-.04, .13), offset + rng.uniform(-.035, .035))
            end = (.67 + rng.uniform(-.12, .15), offset + rng.uniform(-.12, .1))
            points = wander(rng, start, end, 24, .003)
            width = rng.uniform(.065, .12) if style == 'drag' else rng.uniform(.04, .07)
            mask = path_mask(points, width)
            pressure = blood_noise.fbm(rng, SIZE, 12)
            breaks = 1 - smoothstep(.54, .75, blood_noise.fbm(rng, SIZE, 32)) * .83
            density += mask * breaks * (.55 + pressure * 2.4)
        if style == 'smear':
            start_pool = coverage(blood_noise.ellipse(X + .58, Y + .02, .15, .22))
            density += start_pool * (.6 + blood_noise.fbm(rng, SIZE, 17))
    density *= .4 + 1.1 * blood_noise.fbm(rng, SIZE, 40)
    for index in range(24):
        cx = rng.uniform(-.8, .82)
        cy = rng.uniform(-.37, .4)
        radius = rng.uniform(.004, .018)
        drop = coverage(blood_noise.ellipse(X - cx, Y - cy, radius, radius * .8))
        density += drop * rng.uniform(.4, 1.4)
    return blood_material(density, rng)


def preview(entries, destination, title):
    cols = 3
    rows = (len(entries) + cols - 1) // cols
    card_width, card_height = 418, 272
    canvas = Image.new('RGB', (cols * card_width + 24, rows * card_height + 94), '#151d20')
    draw = ImageDraw.Draw(canvas)
    font = lambda size: ImageFont.truetype('C:/Windows/Fonts/arial.ttf', size)
    draw.text((24, 18), title, font=font(27), fill='#d6dfda')
    draw.text((24, 57), 'Каждая накладка на бетоне и плитке · прозрачные PNG · процедурная проба', font=font(18), fill='#a6b5b5')
    floors = ROOT / 'Concepts/Used/Environment/FloorMaterials01'
    bases = [Image.open(floors / name).convert('RGBA').resize((512, 512), Image.Resampling.LANCZOS)
             for name in ['warehouse-concrete-base-01.png', 'lab-tile-base-01.png']]
    for index, entry in enumerate(entries):
        x = 12 + index % cols * card_width
        y = 94 + index // cols * card_height
        overlay = Image.open(DEST / 'Sprites' / (entry['id'] + '.png')).resize((196, 196), Image.Resampling.LANCZOS)
        for side, base in enumerate(bases):
            patch = base.crop((40 + index * 13 % 200, 60, 236 + index * 13 % 200, 256))
            patch.alpha_composite(overlay)
            canvas.paste(patch.convert('RGB'), (x + side * 202, y))
        draw.text((x + 4, y + 205), entry['label'], font=font(18), fill='#d6dfda')
        draw.text((x + 4, y + 232), entry['note'], font=font(15), fill='#a6b5b5')
    canvas.save(destination)


def main():
    (DEST / 'Sprites').mkdir(parents=True, exist_ok=True)
    (DEST / 'TintMasks').mkdir(exist_ok=True)
    entries = [
        ('claw-short', 'Короткие царапины когтей', 'Три следа разной длины', lambda rng: claws(rng, False)),
        ('claw-deep', 'Глубокие борозды', 'Неравный нажим и поперечный срыв', lambda rng: claws(rng, True)),
        ('crack-long', 'Разветвлённая трещина', 'Бетон / плитка', lambda rng: cracks(rng, False)),
        ('crack-impact', 'Трещины от удара', 'Неравные ветви от одного центра', lambda rng: cracks(rng, True)),
        ('spall-local', 'Выбоина и сколы', 'Жёсткое минеральное покрытие', lambda rng: chips(rng, False)),
        ('spall-dragged', 'Вытянутый скол', 'След тяжёлого волочения', lambda rng: chips(rng, True)),
        ('surface-dent', 'Неглубокая вмятина', 'Для металла / податливого покрытия', dent),
        ('furniture-scuff', 'Сдвинутая мебель', 'Потёртость с тонким срывом края', lambda rng: scuff(rng, False)),
        ('shoe-scuff', 'Скользнувшая подошва', 'Рваный дугообразный след резины', lambda rng: scuff(rng, True)),
        ('blood-drag', 'Кровавое волочение', 'Прерывистый след с тремя полосами', lambda rng: blood_traces(rng, 'drag')),
        ('blood-smear', 'Размазанная кровь', 'Неравная толщина, просветы и капли', lambda rng: blood_traces(rng, 'smear')),
        ('blood-hand', 'Смазанный отпечаток ладони', 'Пять пальцев и след соскальзывания', lambda rng: blood_traces(rng, 'hand')),
    ]
    manifest = []
    for index, (name, label, note, build) in enumerate(entries):
        seed = SEED + index * 811
        rng = np.random.default_rng(seed)
        build(rng).export(DEST / 'Sprites' / (name + '.png'))
        manifest.append({'id': name, 'label': label, 'note': note, 'seed': seed, 'size': SIZE})
    preview(manifest[:9], DEST / 'mechanical-preview.png', 'ПОВРЕЖДЕНИЯ ПОВЕРХНОСТИ · 9 ПРОБ')
    preview(manifest[9:], DEST / 'struggle-preview.png', 'СЛЕДЫ БОРЬБЫ · 3 ПРОБЫ')
    info = {'date': '2026-10-17', 'method': 'Seeded offline NumPy/Pillow; no AI generation and no external images',
            'generator': 'Tools/Art/bake_floor_aftermath_01.py', 'seed': SEED, 'runtimeGeneration': False,
            'noiseSource': NOISE_SOURCE.relative_to(ROOT).as_posix(),
            'noiseSourceSha256': hashlib.sha256(NOISE_SOURCE.read_bytes().replace(b'\r\n', b'\n')).hexdigest(),
            'entries': manifest, 'limitations': ['Preview only; no Unity integration', 'Shading highlights are baked, not normal mapped',
            'Dents belong to deformable materials; comparison on tile is a mask demonstration', 'Cracks do not follow existing tile joints automatically'],
            'sha256': {p.relative_to(DEST).as_posix(): hashlib.sha256(p.read_bytes()).hexdigest() for p in sorted(DEST.rglob('*.png'))}}
    (DEST / 'provenance.json').write_text(json.dumps(info, ensure_ascii=False, indent=2) + '\n', encoding='utf-8', newline='\n')
    print(json.dumps({'textures': len(entries), 'tintMasks': len(entries), 'directory': str(DEST)}))


if __name__ == '__main__':
    main()
