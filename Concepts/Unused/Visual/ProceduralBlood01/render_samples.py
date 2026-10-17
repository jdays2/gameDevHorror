"""Offline procedural blood material studies. NumPy + Pillow, no blood textures.

Only floor images come from the game's existing material library. This is an
art prototype with a fixed overhead light, not a Unity shader or fluid solver.
"""

import argparse
import json
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont


FAMILY = Path(__file__).resolve().parent
PROJECT = FAMILY.parents[3]
SIZE = 640


def smoothstep(low, high, value):
    t = np.clip((value - low) / (high - low), 0, 1)
    return t * t * (3 - 2 * t)


def noise_field(rng, size, scale):
    pixels = rng.integers(0, 256, (scale, scale), dtype=np.uint8)
    image = Image.fromarray(pixels).resize((size, size), Image.Resampling.BICUBIC)
    return np.asarray(image, dtype=np.float32) / 255


def fbm(rng, size, scale):
    result = np.zeros((size, size), dtype=np.float32)
    amplitude = 0.53
    for octave in range(4):
        result += noise_field(rng, size, scale * (2 ** octave)) * amplitude
        amplitude *= 0.5
    return result


def ellipse(x, y, rx, ry):
    return (np.sqrt((x / rx) ** 2 + (y / ry) ** 2) - 1) * min(rx, ry)


def union(a, b, width):
    h = np.clip(0.5 + 0.5 * (b - a) / width, 0, 1)
    return b * (1 - h) + a * h - width * h * (1 - h)


def capsule(x, y, ax, ay, bx, by, radius):
    dx = bx - ax
    dy = by - ay
    t = np.clip(((x - ax) * dx + (y - ay) * dy) / (dx * dx + dy * dy), 0, 1)
    return np.hypot(x - ax - dx * t, y - ay - dy * t) - radius


def make_pool(x, y, seed, style):
    rng = np.random.default_rng(seed)
    angle = rng.uniform(0, np.pi * 2)
    qx = np.cos(angle) * x - np.sin(angle) * y
    qy = np.sin(angle) * x + np.cos(angle) * y
    size = x.shape[0]
    coarse = fbm(rng, size, 7)
    fine = fbm(rng, size, 35)
    zx = qx + (coarse - 0.5) * 0.28
    zy = qy + (fbm(rng, size, 8) - 0.5) * 0.28
    radius = (0.66, 0.47)
    if style == 1:
        radius = (0.53, 0.34)
    elif style == 2:
        radius = (0.60, 0.51)
    distance = ellipse(zx, zy, *radius)
    for _ in range(6):
        theta = rng.uniform(0, np.pi * 2)
        offset = rng.uniform(0.27, 0.55)
        cx = np.cos(theta) * offset
        cy = np.sin(theta) * offset
        lobe = ellipse(zx - cx, zy - cy, rng.uniform(0.12, 0.27), rng.uniform(0.09, 0.21))
        distance = union(distance, lobe, 0.10)
    if style == 1:
        for _ in range(8):
            start_y = rng.uniform(-0.27, 0.27)
            streak = capsule(zx, zy, 0.08, start_y, rng.uniform(0.6, 1.25), start_y + rng.uniform(-0.28, 0.28), rng.uniform(0.009, 0.035))
            distance = union(distance, streak, 0.025)
    distance += (fine - 0.5) * 0.13
    distance += (noise_field(rng, size, 185) - 0.5) * 0.011
    coverage = 1 - smoothstep(-0.004, 0.008, distance)
    thickness = 0.30 + np.clip(-distance * 4, 0, 1.25) + coarse * 0.85
    if style == 2:
        coverage *= 1 - smoothstep(0.67, 0.85, fbm(rng, size, 18)) * 0.95
        thickness *= 0.85 + 1.05 * fine
    density = coverage * thickness

    # Sparse edge freckles, elongated impacts and larger satellite drops.
    drops = 28
    if style == 1:
        drops = 88
    for index in range(drops):
        theta = rng.uniform(0, np.pi * 2)
        offset = rng.uniform(0.58, 1.45)
        cx = np.cos(theta) * offset
        cy = np.sin(theta) * offset
        if style == 1 and index < 61:
            cx = rng.uniform(0.45, 1.48)
            cy = rng.normal(0, 0.40)
        radius = rng.uniform(0.004, 0.014)
        if index % 7 == 0:
            radius = rng.uniform(0.025, 0.055)
        drop = ellipse(qx - cx, qy - cy, radius * rng.uniform(0.8, 1.6), radius)
        mask = 1 - smoothstep(-0.002, 0.005, drop)
        density = np.maximum(density, mask * rng.uniform(0.35, 1.3))

    # A diluted, irregular wet stain around some edges; not a uniform glow.
    wash = (1 - smoothstep(0.0, 0.075, distance)) * (1 - coverage)
    wash *= smoothstep(0.38, 0.68, coarse) * 0.23
    return density + wash


def floor_image(kind, size):
    names = ["warehouse-concrete-base-01.png", "lab-tile-base-01.png"]
    path = PROJECT / "Assets/EmergencyShift/Art/Environment/FloorMaterials" / names[kind]
    tile_size = int(size / 1.6)
    source = Image.open(path).convert("RGB").resize((tile_size, tile_size), Image.Resampling.LANCZOS)
    result = Image.new("RGB", (size, size))
    for y in range(0, size, tile_size):
        for x in range(0, size, tile_size):
            result.paste(source, (x, y))
    return np.asarray(result, dtype=np.float32) / 255


def render(seed, count, style, floor):
    coordinates = np.linspace(-1.4, 1.4, SIZE, dtype=np.float32)
    x, y = np.meshgrid(coordinates, coordinates)
    density = np.zeros_like(x)
    positions = np.random.default_rng(seed + 512)
    for index in range(count):
        cx = 0.0
        cy = 0.0
        if index > 0:
            cx, cy = positions.uniform(-0.86, 0.86, 2)
        density += make_pool((x - cx) / 0.82, (y - cy) / 0.82, seed + index * 503 + style * 1103, style)
    rng = np.random.default_rng(seed + 97)
    grain = fbm(rng, SIZE, 95)
    veins = fbm(rng, SIZE, 19)
    opacity = 1 - np.exp(-density * 1.38)
    thick = 1 - np.exp(-density * 0.57)
    thin = np.array([0.47, 0.015, 0.030])
    deep = np.array([0.125, 0.004, 0.012])
    blood = thin * (1 - thick[..., None]) + deep * thick[..., None]
    blood *= (0.75 + 0.38 * veins + 0.12 * grain)[..., None]
    clot = smoothstep(0.53, 0.78, fbm(rng, SIZE, 48))
    clot_strength = 0.13
    if style == 2:
        clot_strength = 0.42
    blood *= (1 - clot * clot_strength * thick)[..., None]

    # Thin meniscus and slowly varying liquid surface. Light is a preview rig.
    roughness = 0.02
    if style == 1:
        roughness = 0.06
    elif style == 2:
        roughness = 0.12
    wet_height = (1 - np.exp(-density * 1.15)) * (1 - roughness + roughness * veins)
    wet_height += clot * thick * 0.035
    height8 = Image.fromarray(np.uint8(np.clip(wet_height, 0, 1) * 255))
    height = np.asarray(height8.filter(ImageFilter.GaussianBlur(1.5)), dtype=np.float32) / 255
    gy, gx = np.gradient(height, 2.8 / SIZE)
    normal = np.stack([-gx * 0.23, -gy * 0.23, np.ones_like(gx)], axis=-1)
    normal /= np.linalg.norm(normal, axis=-1, keepdims=True)
    half_light = np.array([-0.24, -0.30, 1.5])
    half_light /= np.linalg.norm(half_light)
    specular = np.maximum(0, normal @ half_light) ** 112
    specular *= smoothstep(0.33, 1.25, density)
    specular *= 0.3 + 0.7 * smoothstep(0.35, 0.63, veins)
    strength = 0.40
    if style == 2:
        strength = 0.23
    blood += specular[..., None] * np.array([0.66, 0.46, 0.43]) * strength
    base = floor_image(floor, SIZE)
    result = base * (1 - opacity[..., None]) + blood * opacity[..., None]
    return Image.fromarray(np.uint8(np.clip(result, 0, 1) * 255)), density


def contact_sheet(images, labels, filename):
    size = 600
    header = 50
    result = Image.new("RGB", (size * 3, size + header), (27, 30, 31))
    font = ImageFont.truetype("C:/Windows/Fonts/arial.ttf", 23)
    draw = ImageDraw.Draw(result)
    for index, (image, label) in enumerate(zip(images, labels)):
        result.paste(image.resize((size, size), Image.Resampling.LANCZOS), (index * size, header))
        draw.text((index * size + 18, 14), label, font=font, fill=(230, 231, 230))
    result.save(filename)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--seed", type=int, default=47)
    args = parser.parse_args()
    labels = ["Свежая кровь", "Рваный выброс", "Густая кровь"]
    report = {"seed": args.seed, "size": SIZE, "method": "procedural density + wet height shading", "samples": []}
    for count, floor, suffix in [(1, 0, "single-concrete"), (15, 1, "overlap-tile")]:
        samples = []
        for style in range(3):
            image, density = render(args.seed, count, style, floor)
            filename = f"{suffix}-{style + 1}.png"
            image.save(FAMILY / filename)
            samples.append(image)
            report["samples"].append({"file": filename, "count": count, "style": style, "floor": floor, "coverage": round(float(np.mean(density > 0.05)), 4)})
        contact_sheet(samples, labels, FAMILY / f"{suffix}.png")
    (FAMILY / "render-report.json").write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(report, ensure_ascii=False))


if __name__ == "__main__":
    main()
