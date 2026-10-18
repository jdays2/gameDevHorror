"""Offline blood transfer studies using the existing BloodPools01 material.

Shapes represent pressure, sliding contact and lost pigment, not handprints.
Only the source concept family is written; no Unity assets or scenes are modified.
"""
from pathlib import Path
import hashlib
import importlib.util
import json
import sys

import numpy as np
from PIL import Image, ImageDraw, ImageFont


sys.dont_write_bytecode = True
ROOT = Path(__file__).resolve().parents[2]
DEST = ROOT / "Concepts/Used/Environment/BloodTraces05"
SIZE = 1024
SEED = 48319


def load_module(name, relative_path):
    specification = importlib.util.spec_from_file_location(name, ROOT / relative_path)
    module = importlib.util.module_from_spec(specification)
    specification.loader.exec_module(module)
    return module


source = load_module("blood_noise_03", "Concepts/Unused/Visual/ProceduralBlood01/render_samples.py")
pool_baker = load_module("blood_material_03", "Tools/Art/bake_blood_pools_01.py")
smoothstep = source.smoothstep
axis = np.linspace(-1, 1, SIZE, dtype=np.float32)
X, Y = np.meshgrid(axis, axis)


def stretched_noise(rng, columns, rows):
    pixels = rng.integers(0, 256, (rows, columns), dtype=np.uint8)
    return np.asarray(
        Image.fromarray(pixels).resize((SIZE, SIZE), Image.Resampling.BICUBIC),
        dtype=np.float32,
    ) / 255


def transfer(rng, start, end, half_width, density):
    """Independent scuffed transfers, with small local deviations and torn ends."""
    direction = np.asarray(end, dtype=np.float32) - start
    length = float(np.linalg.norm(direction))
    if length <= 0 or half_width <= 0:
        raise ValueError("A transfer needs positive length and width")
    normal = np.array([-direction[1], direction[0]]) / length
    t = ((X - start[0]) * direction[0] + (Y - start[1]) * direction[1]) / length ** 2
    across = (X - start[0]) * normal[0] + (Y - start[1]) * normal[1]

    def pixel(along, offset):
        point = np.asarray(start) + direction * along + normal * offset
        return tuple((point + 1) * .5 * (SIZE - 1))

    # Unequal strips encode sliding contact. Their edges and breaks are separate;
    # no common displacement is allowed to bend all the internal gaps together.
    contact = Image.new("L", (SIZE, SIZE))
    painter = ImageDraw.Draw(contact)
    count = int(np.clip(half_width * 245, 22, 74))
    for index in range(count):
        offset = rng.uniform(-.92, .92) * half_width
        begin = rng.uniform(-.09, .34)
        if index % 5 == 0:
            begin = rng.uniform(.30, .68)
        finish = rng.uniform(.70, 1.06)
        width = rng.uniform(.003, .024) * (.7 + half_width)
        drift = rng.uniform(-.30, .30) * half_width
        # Each contact has its own nonperiodic control points. There is no
        # displacement field shared by neighbouring contacts.
        anchors = np.array([0, rng.uniform(.22, .4), rng.uniform(.59, .82), 1.08])
        deviations = rng.uniform(-.16, .16, 4) * half_width
        intervals = [(begin, finish)]
        if index % 3 == 0 and finish - begin > .35:
            cut = rng.uniform(begin + .11, finish - .13)
            intervals = [(begin, cut), (cut + rng.uniform(.025, .13), finish)]
        for first, last in intervals:
            positions = np.sort(np.concatenate([[first, last], rng.uniform(first, last, 5)]))
            center = offset + drift * positions + np.interp(positions, anchors, deviations)
            center += rng.uniform(-.008, .008, len(positions)) * (.5 + half_width)
            upper_width = width * rng.uniform(.25, 1.45, len(positions))
            lower_width = width * rng.uniform(.25, 1.35, len(positions))
            upper_width[[0, -1]] *= rng.uniform(.02, .25, 2)
            lower_width[[0, -1]] *= rng.uniform(.02, .25, 2)
            upper = [pixel(u, v + w) for u, v, w in zip(positions, center, upper_width)]
            lower = [pixel(u, v - w) for u, v, w in zip(positions[::-1], center[::-1], lower_width[::-1])]
            painter.polygon(upper + lower, fill=int(rng.integers(72, 255)))

    strips = np.asarray(contact, dtype=np.float32) / 255
    coarse = source.fbm(rng, SIZE, 22)
    grain = source.noise_field(rng, SIZE, 420)
    # The rubbed film bridges some strips but leaves ragged local voids. Noise
    # changes pigment coverage only; the contact control points above own direction.
    outline = 1 - smoothstep(.72, 1.03, np.abs(across) / half_width + (coarse - .5) * .32)
    along = smoothstep(-.06, .18, t) * (1 - smoothstep(.86, 1.04, t))
    film = outline * along * smoothstep(.32, .70, coarse) * .46
    abrasion = smoothstep(.14, .52, grain + coarse * .20)
    pressure = .15 + np.clip(t, 0, 1) ** 1.35 * 1.40
    return (strips + film) * abrasion * pressure * density


def deposit(rng, cx, cy, sx, sy, strength):
    """Reuse the irregular pool silhouette at contact-patch scale."""
    seed = int(rng.integers(1, 900000))
    field = source.make_pool((X - cx) / sx, (Y - cy) / sy, seed, 2)
    return field * strength


def droplets(rng, positions):
    density = np.zeros_like(X)
    for cx, cy, radius in positions:
        distance = source.ellipse(X - cx, Y - cy, radius, radius * rng.uniform(.7, 1.3))
        distance += (source.noise_field(rng, SIZE, 160) - .5) * radius * .32
        shape = 1 - smoothstep(-.0015, .002, distance)
        density = np.maximum(density, shape * rng.uniform(.5, 1.5))
    return density


def short_wipe(rng):
    # One sideways slip and a small dense stopping patch; no fingers or palm.
    density = transfer(rng, (-.84, .17), (.54, -.12), .19, 1.05)
    density += transfer(rng, (-.55, -.04), (.6, -.18), .085, .6)
    density += deposit(rng, .49, -.13, .22, .20, .63)
    density += droplets(rng, [(.68, .21, .022), (.72, .30, .012), (.80, -.34, .012), (.58, .37, .006)])
    return density


def long_drag(rng):
    # A heavier object leaves two unequal contacts; pigment fails mid-stroke.
    density = transfer(rng, (-.88, .15), (.73, -.13), .25, 1.1)
    density += transfer(rng, (-.70, .34), (.65, .01), .09, .7)
    dry_band = np.exp(-((X + .10) / .11) ** 2)
    density *= 1 - dry_band * smoothstep(.18, .72, stretched_noise(rng, 20, 55)) * .85
    density += deposit(rng, .65, -.14, .19, .25, .72)
    density += droplets(rng, [(-.67, .53, .015), (-.4, .43, .009), (.49, -.44, .024), (.78, .32, .015)])
    return density


def scattered_stains(rng):
    # Different local events: one pooled stain, a wiped edge and isolated drops.
    density = deposit(rng, -.29, -.08, .37, .32, .91)
    density += transfer(rng, (-.52, -.11), (.19, -.22), .075, .43)
    density += deposit(rng, .45, .27, .16, .12, .65)
    density += deposit(rng, -.56, .46, .13, .09, .37)
    density += droplets(rng, [(.18, .06, .035), (.57, -.12, .022), (.34, -.45, .015), (-.11, .39, .020),
                             (-.66, -.38, .014), (.73, .41, .008), (.03, .54, .007), (.56, -.41, .006)])
    return density


def bone_slips(rng):
    # Sparse pointed contacts slide under unequal loads. They are not foot pads.
    density = np.zeros_like(X)
    contacts = [(-.63, -.31, .26, .07, .85, -.14), (-.23, .26, .37, .09, 1.0, -.22),
                (.13, -.15, .45, .11, .7, .10), (.51, .36, .26, .065, .42, -.04)]
    for cx, cy, length, width, load, drift in contacts:
        end = (cx + length * .4, cy + drift * .4)
        start = (cx - length * .6, cy - drift * .6)
        density += transfer(rng, start, end, width, load)
        density += deposit(rng, end[0] - .025, end[1], width * 1.0, width * .7, load * .45)
    density += droplets(rng, [(-.72, .17, .013), (.37, -.4, .019), (.42, -.47, .007), (-.04, .47, .009)])
    return density


def rgba_from_density(density):
    # Same colour/thickness/alpha law and restrained baked highlight as death blood.
    rgba = pool_baker.material(density, 1, source, 1.75)
    border = 1 - smoothstep(.91, .98, np.maximum(np.abs(X), np.abs(Y)))
    rgba[..., 3] *= border
    rgba[..., :3] = np.where(rgba[..., 3:4] > 0, rgba[..., :3], 0)
    return Image.fromarray(np.uint8(np.clip(rgba, 0, 1) * 255))


def floor_patch(kind, size):
    name = ["warehouse-concrete-base-01.png", "lab-tile-base-01.png"][kind]
    image = Image.open(ROOT / "Concepts/Used/Environment/FloorMaterials01" / name)
    return image.convert("RGBA").resize((size, size), Image.Resampling.LANCZOS)


def preview(entries):
    # Preserve aspect ratio; each composition appears on both existing floors.
    cell, gap, header, footer = 420, 16, 102, 43
    result = Image.new("RGB", (cell * 2 + gap * 3, header + (cell + footer + gap) * 4), "#192023")
    draw = ImageDraw.Draw(result)
    font = lambda size: ImageFont.truetype("C:/Windows/Fonts/arial.ttf", size)
    draw.text((gap, 15), "КРОВАВЫЕ СМАЗЫ · ПРОБА 05", font=font(27), fill="#e1e4de")
    draw.text((gap, 53), "Бетон / плитка · один и тот же прозрачный PNG", font=font(19), fill="#acb8b4")
    for index, entry in enumerate(entries):
        y = header + index * (cell + footer + gap)
        sprite = Image.open(DEST / "Sprites" / (entry["id"] + ".png"))
        sprite = sprite.resize((cell, cell), Image.Resampling.LANCZOS)
        for floor in range(2):
            background = floor_patch(floor, cell)
            background.alpha_composite(sprite)
            result.paste(background.convert("RGB"), (gap + floor * (cell + gap), y))
        draw.text((gap, y + cell + 9), f"{index + 1:02}. {entry['label']}", font=font(21), fill="#e1e4de")
    result.save(DEST / "overview.png")
    for kind, suffix in [(0, "concrete"), (1, "tile")]:
        side, margin = 560, 18
        board = Image.new("RGB", (side * 2 + margin * 3, 1322), "#192023")
        board_draw = ImageDraw.Draw(board)
        board_draw.text((margin, 17), "КРОВАВЫЕ СМАЗЫ · ПРОБА 05", font=font(29), fill="#e1e4de")
        for index, entry in enumerate(entries):
            x = margin + (index % 2) * (side + margin)
            y = 70 + (index // 2) * 620
            sprite = Image.open(DEST / "Sprites" / (entry["id"] + ".png"))
            sprite = sprite.resize((side, side), Image.Resampling.LANCZOS)
            background = floor_patch(kind, side)
            background.alpha_composite(sprite)
            board.paste(background.convert("RGB"), (x, y))
            board_draw.text((x, y + side + 10), f"{index + 1:02}. {entry['label']}", font=font(21), fill="#e1e4de")
        board.save(DEST / ("examples-" + suffix + ".png"))
    # A single larger sample also shows partial transfer on a neutral surface.
    white = Image.new("RGBA", (SIZE, SIZE), "#edece7")
    white.alpha_composite(Image.open(DEST / "Sprites/short-wipe.png"))
    white.convert("RGB").save(DEST / "short-wipe-neutral.png")


def main():
    (DEST / "Sprites").mkdir(parents=True, exist_ok=True)
    entries = []
    samples = [("short-wipe", "Короткий смаз с плотным краем", short_wipe),
               ("long-drag", "Волочение с разрывами контакта", long_drag),
               ("scattered-stains", "Пятна и отдельные капли", scattered_stains),
               ("bone-slips", "Срывы костяных лап с кровью", bone_slips)]
    for index, (name, label, build) in enumerate(samples):
        seed = SEED + index * 1013
        image = rgba_from_density(build(np.random.default_rng(seed)))
        path = DEST / "Sprites" / (name + ".png")
        image.save(path)
        alpha = np.asarray(image)[..., 3]
        assert image.mode == "RGBA" and image.size == (SIZE, SIZE)
        assert not np.any(alpha[[0, -1], :]) and not np.any(alpha[:, [0, -1]])
        entries.append({"id": name, "label": label, "seed": seed, "size": SIZE,
                        "alphaMax": int(alpha.max()), "coveredFraction": round(float(np.mean(alpha > 8)), 4)})
    preview(entries)
    provenance = {
        "date": "2026-10-18", "status": "Offline samples pending owner evaluation; not imported",
        "method": "Independent nonperiodic contact control points, tapered torn ends, local pigment abrasion; no shared displacement, sine or AI images",
        "revision": "05; rejected drafts 03/04 removed by content cleanup, recoverable from Git",
        "generator": "Tools/Art/bake_blood_traces_03.py", "runtimeGeneration": False,
        "reused": ["Concepts/Unused/Visual/ProceduralBlood01/render_samples.py",
                   "Tools/Art/bake_blood_pools_01.py: material"],
        "reference": {"suppliedBy": "owner", "file": "codex-clipboard-eb76a2ae-82f9-4d9a-be03-4a7082ddbf26.png",
                      "visibleCredit": "shutterstock.com · 2798661061",
                      "use": "Visual direction only: dragged transfer, density contrast, streaks and drops. No pixels extracted."},
        "entries": entries,
        "sha256": {p.relative_to(DEST).as_posix(): hashlib.sha256(p.read_bytes()).hexdigest()
                   for p in sorted(DEST.rglob("*.png"))},
    }
    (DEST / "provenance.json").write_text(json.dumps(provenance, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"samples": entries, "directory": str(DEST)}, ensure_ascii=False))


if __name__ == "__main__":
    main()
