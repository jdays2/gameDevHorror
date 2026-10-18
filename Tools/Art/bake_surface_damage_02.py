"""Eight offline surface-damage overlays with material-specific contact cues."""
from pathlib import Path
import hashlib
import importlib.util
import json
import sys

import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont


sys.dont_write_bytecode = True
ROOT = Path(__file__).resolve().parents[2]
DEST = ROOT / 'Concepts/Unused/Environment/SurfaceDamage02'
spec = importlib.util.spec_from_file_location('surface_noise', ROOT / 'Concepts/Unused/Visual/ProceduralBlood01/render_samples.py')
noise = importlib.util.module_from_spec(spec)
spec.loader.exec_module(noise)
SIZE = 768
axis = np.linspace(-1, 1, SIZE, dtype=np.float32)
X, Y = np.meshgrid(axis, axis)


def offset(mask, dx, dy):
    result = np.zeros_like(mask)
    result[max(dy, 0):min(SIZE + dy, SIZE), max(dx, 0):min(SIZE + dx, SIZE)] = mask[max(-dy, 0):min(SIZE - dy, SIZE), max(-dx, 0):min(SIZE - dx, SIZE)]
    return result


def polygon(points):
    image = Image.new('L', (SIZE, SIZE))
    ImageDraw.Draw(image).polygon([((x + 1) * SIZE / 2, (y + 1) * SIZE / 2) for x, y in points], fill=255)
    return np.asarray(image, dtype=np.float32) / 255


def chipped_polygon(points, rng, straight_edges=()):
    broken = []
    for index, start in enumerate(points):
        end = points[(index + 1) % len(points)]
        start = np.asarray(start)
        direction = np.asarray(end) - start
        length = float(np.linalg.norm(direction))
        normal = np.array([-direction[1], direction[0]]) / max(length, .0001)
        broken.append(start)
        if index in straight_edges:
            continue
        count = max(2, int(length * 70))
        for t in np.linspace(0, 1, count + 2)[1:-1]:
            broken.append(start + direction * t + normal * rng.uniform(-.013, .013))
    return polygon(broken)


def line(points, width, rng, strength=1):
    mask = np.zeros_like(X)
    for index, (start, end) in enumerate(zip(points, points[1:])):
        radius = width * rng.uniform(.55, 1.25) * (1 - .6 * index / len(points))
        distance = noise.capsule(X, Y, *start, *end, radius)
        mask = np.maximum(mask, 1 - noise.smoothstep(-.001, .003, distance))
    grain = noise.noise_field(rng, SIZE, 290)
    return mask * (.35 + .65 * noise.smoothstep(.15, .7, grain)) * strength


class Surface:
    def __init__(self):
        self.color = np.zeros((SIZE, SIZE, 3), dtype=np.float32)
        self.alpha = np.zeros_like(X)

    def over(self, color, alpha):
        alpha = np.clip(alpha, 0, 1)
        self.color = np.asarray(color) * alpha[..., None] + self.color * (1 - alpha[..., None])
        self.alpha = alpha + self.alpha * (1 - alpha)

    def incision(self, mask, intensity=.5):
        self.over([.72, .72, .68], np.maximum(offset(mask, 1, 1) - mask, 0) * intensity * .45)
        self.over([.12, .13, .125], mask * intensity)

    def exposed(self, mask, rng, strength=.8):
        grain = noise.fbm(rng, SIZE, 40)
        tone = .38 + grain * .26 + noise.noise_field(rng, SIZE, 310) * .025
        self.over(np.stack([tone, tone * 1.02, tone * .97], axis=-1), mask * strength)
        for index in range(4):
            cx, cy = rng.uniform(-.6, .6, 2)
            sx, sy = rng.uniform(.08, .28, 2)
            facet = polygon([(cx - sx, cy), (cx + sx * .6, cy - sy), (cx + sx, cy + sy * .45)])
            shade = [.31, .33, .32] if index % 2 else [.62, .62, .56]
            self.over(shade, facet * mask * rng.uniform(.09, .19))
        self.over([.15, .16, .15], np.maximum(offset(mask, -1, -1) - mask, 0) * .38)
        self.over([.78, .77, .70], np.maximum(offset(mask, 1, 2) - mask, 0) * .56)

    def save(self, path):
        border = 1 - noise.smoothstep(.94, .99, np.maximum(abs(X), abs(Y)))
        alpha = self.alpha * border
        rgb = self.color / np.maximum(self.alpha[..., None], 1e-8)
        rgb[alpha <= 0] = 0
        rgba = np.concatenate([rgb, alpha[..., None]], axis=-1)
        Image.fromarray(np.uint8(np.clip(rgba, 0, 1) * 255)).save(path)
        mask = np.full((SIZE, SIZE, 4), 255, dtype=np.uint8)
        mask[..., 3] = np.uint8(alpha * 255)
        Image.fromarray(mask).save(DEST / 'TintMasks' / path.name)


def castor_scuff(rng):
    surface = Surface()
    for index in range(22):
        y = rng.uniform(-.19, .17)
        start = rng.uniform(-.72, -.26)
        end = rng.uniform(.1, .70)
        mask = line([(start, y), ((start + end) / 2, y + .025), (end, y + rng.uniform(-.025, .045))], rng.uniform(.003, .013), rng)
        surface.over([.14, .15, .145], mask * rng.uniform(.10, .27))
    return surface


def metal_drag(rng):
    surface = Surface()
    for points, width, strength in [([(-.8, .18), (-.46, .13), (-.2, .1)], .012, .52),
                                     ([(-.12, .09), (.22, .06), (.73, .04)], .009, .46),
                                     ([(-.63, -.09), (-.17, -.1), (.48, -.14)], .006, .30)]:
        surface.incision(line(points, width, rng), strength)
    return surface


def bone_tip(rng):
    surface = Surface()
    surface.incision(line([(-.57, .28), (-.40, .11), (-.2, -.03), (.05, -.13), (.48, -.16)], .013, rng), .55)
    surface.incision(line([(-.35, .13), (-.23, .17), (-.10, .18)], .004, rng), .22)
    return surface


def hairline_crack(rng):
    surface = Surface()
    points = [(-.78, .24), (-.53, .16), (-.42, .2), (-.24, .03), (-.08, .01), (.09, -.11), (.32, -.14), (.44, -.21), (.75, -.23)]
    surface.incision(line(points, .007, rng), .65)
    surface.incision(line([(-.24, .03), (-.18, .20), (-.1, .25)], .003, rng), .31)
    return surface


def tile_corner(rng):
    surface = Surface()
    mask = chipped_polygon([(-.33, -.27), (.21, -.27), (.15, -.15), (.05, -.10), (-.04, .06), (-.15, .13), (-.33, .38)], rng, (0, 6))
    surface.exposed(mask, rng, .86)
    surface.incision(line([(-.1, -.07), (.02, .1), (.07, .34)], .005, rng), .42)
    for points in [[(.25, -.22), (.32, -.19), (.26, -.13)], [(-.12, .29), (-.04, .34), (-.12, .37)]]:
        surface.exposed(chipped_polygon(points, rng), rng, .7)
    return surface


def long_spall(rng):
    surface = Surface()
    shape = [(-.72, .02), (-.48, -.09), (-.30, -.04), (-.09, -.14), (.08, -.1), (.22, -.12), (.52, -.03), (.71, -.01), (.54, .06), (.23, .03), (.04, .13), (-.17, .08), (-.44, .15), (-.56, .09)]
    surface.exposed(chipped_polygon(shape, rng), rng, .68)
    surface.incision(line([(-.61, .06), (-.39, .04), (-.25, .01)], .004, rng), .18)
    return surface


def coating_flake(rng):
    surface = Surface()
    shapes = [[(-.65, .1), (-.44, -.02), (-.24, -.07), (.02, -.03), (-.07, .05), (-.33, .09), (-.50, .16)],
              [(.07, -.07), (.22, -.13), (.42, -.08), (.62, -.14), (.54, -.02), (.31, .04), (.19, .01)]]
    for shape in shapes:
        surface.exposed(chipped_polygon(shape, rng), rng, .62)
    return surface


def shallow_crease(rng):
    surface = Surface()
    mask = line([(-.61, .10), (-.32, .025), (-.10, -.025), (.22, -.06), (.48, -.03)], .013, rng)
    blurred = np.asarray(Image.fromarray(np.uint8(mask * 255)).filter(ImageFilter.GaussianBlur(9)), dtype=np.float32) / 255
    surface.over([.16, .17, .17], blurred * .34)
    surface.incision(mask, .16)
    surface.over([.8, .8, .76], offset(blurred, 3, 5) * .20)
    return surface


SAMPLES = [
    ('castor-scuff', 'Потёртость от колёс', 'ceramic-small-sage', 1.1, castor_scuff),
    ('metal-drag', 'След волочения металла', 'concrete-service', 2.2, metal_drag),
    ('bone-tip', 'Зацеп костяного острия', 'porcelain-ivory', .65, bone_tip),
    ('hairline-crack', 'Тонкая трещина', 'cement-slabs', 1.6, hairline_crack),
    ('tile-corner-chip', 'Скол угла плитки', 'porcelain-ivory', .45, tile_corner),
    ('elongated-spall', 'Вытянутый скол бетона', 'concrete-service', 1.3, long_spall),
    ('coating-flake', 'Содранное покрытие', 'resin-pale', 1.2, coating_flake),
    ('shallow-crease', 'Неглубокая вмятина', 'resin-pale', .8, shallow_crease),
]


def preview(entries):
    cell, gap, header, row = 340, 16, 91, 310
    board = Image.new('RGB', (cell * 4 + gap * 5, header + row * 2), '#192023')
    draw = ImageDraw.Draw(board)
    font = lambda size: ImageFont.truetype('C:/Windows/Fonts/arial.ttf', size)
    draw.text((gap, 14), 'ПОВРЕЖДЕНИЯ ПОВЕРХНОСТЕЙ · 8 НАКЛАДОК', font=font(28), fill='#e1e4de')
    draw.text((gap, 53), 'Примеры увеличены для просмотра · подложка только в превью', font=font(20), fill='#acb8b4')
    for index, entry in enumerate(entries):
        x = gap + (index % 4) * (cell + gap)
        y = header + (index // 4) * row
        floor = Image.open(ROOT / 'Concepts/Unused/Environment/MorgueFloors02/Textures' / (entry['previewFloor'] + '.png')).convert('RGBA')
        # A crop gives a readable material scale. Tile chips are specialised
        # overlays: their straight outer edges must be aligned to an actual seam.
        floor = floor.resize((SIZE, SIZE), Image.Resampling.LANCZOS).crop((100, 135, 668, 571))
        floor = floor.resize((cell, 260), Image.Resampling.LANCZOS)
        overlay = Image.open(DEST / 'Sprites' / (entry['id'] + '.png'))
        if entry['id'] == 'tile-corner-chip':
            # Six actual tiles across the generated porcelain source. Place the
            # chip's straight edges at the nearest visible tile intersection.
            overlay = overlay.resize((256, 256), Image.Resampling.LANCZOS)
            floor.alpha_composite(overlay, (84, 56))
        else:
            bounds = overlay.getchannel('A').getbbox()
            overlay = overlay.crop(bounds)
            overlay.thumbnail((cell - 40, 210), Image.Resampling.LANCZOS)
            floor.alpha_composite(overlay, ((cell - overlay.width) // 2, (260 - overlay.height) // 2))
        board.paste(floor.convert('RGB'), (x, y))
        draw.text((x + 2, y + 270), f'{index + 1:02}. {entry["label"]}', font=font(19), fill='#e1e4de')
    board.save(DEST / 'overview.png')


def main():
    (DEST / 'Sprites').mkdir(parents=True, exist_ok=True)
    (DEST / 'TintMasks').mkdir(exist_ok=True)
    entries = []
    for index, (name, label, floor, width, build) in enumerate(SAMPLES):
        seed = 93217 + index * 947
        path = DEST / 'Sprites' / (name + '.png')
        build(np.random.default_rng(seed)).save(path)
        entries.append({'id': name, 'label': label, 'previewFloor': floor, 'suggestedCanvasWidthUnits': width,
                        'seed': seed, 'pixels': [SIZE, SIZE], 'sha256': hashlib.sha256(path.read_bytes()).hexdigest()})
    preview(entries)
    provenance = {'date': '2026-10-18', 'status': 'Offline samples pending owner evaluation; no Unity import',
                  'method': 'Procedural paths, angular chips, local coverage and shallow baked contact cues; no imagegen',
                  'generator': 'Tools/Art/bake_surface_damage_02.py', 'noiseSource': 'Concepts/Unused/Visual/ProceduralBlood01/render_samples.py',
                  'runtimeGeneration': False, 'entries': entries,
                  'constraints': ['No human hand/footprint', 'No radial impact centre or circular crater',
                                  'Dent for yielding coatings/sheet metal only', 'Tile corner chip must align with tile edge'],
                  'sha256': {p.relative_to(DEST).as_posix(): hashlib.sha256(p.read_bytes()).hexdigest() for p in sorted(DEST.rglob('*.png'))}}
    (DEST / 'provenance.json').write_text(json.dumps(provenance, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    print(json.dumps({'overlays': len(entries), 'size': SIZE}))


if __name__ == '__main__':
    main()
