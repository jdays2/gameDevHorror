"""Twelve additional blood-transfer shapes, with proposed physical sizes.

Offline Concepts-only output. Corrected transfers from the shared generator,
with BloodPools01 material. Earlier BloodTraces03 files remain historical.
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
DEST = ROOT / "Concepts/Used/Environment/BloodTraces05/Variations"
specification = importlib.util.spec_from_file_location("blood_transfer_03", ROOT / "Tools/Art/bake_blood_traces_03.py")
blood = importlib.util.module_from_spec(specification)
specification.loader.exec_module(blood)

# Each shape has a cause and a different layout, rather than a scale-only copy.
# stroke = (start, end, half_width, pigment); pool = (x,y,sx,sy,pigment).
SAMPLES = [
    {"id": "s01-drops", "label": "Несколько капель", "width": .25,
     "strokes": [], "pools": [(-.15, .03, .19, .15, .73)],
     "drops": [(.28, -.21, .052), (.11, .33, .028), (-.4, -.23, .015)]},
    {"id": "s02-tip-slip", "label": "Срыв костяного острия", "width": .4,
     "strokes": [((-.66, .15), (.4, -.16), .07, .95)],
     "pools": [(.34, -.16, .105, .08, .7)], "drops": [(.54, .13, .016)]},
    {"id": "s03-short-rub", "label": "Короткий стёртый мазок", "width": .55,
     "strokes": [((-.52, .16), (.43, -.12), .23, .7),
                 ((-.16, .3), (.4, .12), .08, .3)],
     "pools": [], "drops": [(-.53, -.26, .029), (.63, -.32, .018)]},
    {"id": "s04-double-touch", "label": "Два разных касания", "width": .7,
     "strokes": [((-.72, -.27), (-.17, -.04), .09, 1.05),
                 ((-.06, .29), (.48, .17), .13, .63)],
     "pools": [(-.22, -.065, .08, .09, .3)], "drops": [(.53, -.33, .025)]},
    {"id": "m01-oblique", "label": "Косой плотный смаз", "width": .95,
     "strokes": [((-.77, .4), (.55, -.34), .18, 1.2)],
     "pools": [(.49, -.3, .21, .17, .66)], "drops": [(.65, .15, .021), (.73, -.47, .012)]},
    {"id": "m02-offset", "label": "Смаз со сдвигом", "width": 1.2,
     "strokes": [((-.78, .16), (.56, -.03), .13, .91),
                 ((-.37, .25), (.61, .1), .06, .42)],
     "pools": [(.55, -.025, .12, .15, .43)], "drops": [(-.23, .53, .016)]},
    {"id": "m03-interrupted", "label": "Прерывистый контакт", "width": 1.45,
     "strokes": [((-.79, .17), (-.21, .03), .17, .75),
                 ((-.10, -.01), (.69, -.22), .2, 1.0)],
     "pools": [(.61, -.18, .14, .16, .32)], "drops": [(-.18, .37, .018), (.32, .23, .024)]},
    {"id": "m04-spilled-edge", "label": "Пятно со сдвинутым краем", "width": 1.7,
     "strokes": [((-.59, .12), (.64, -.03), .1, .57)],
     "pools": [(-.25, .02, .40, .32, .86), (.45, -.08, .12, .16, .38)],
     "drops": [(-.6, -.39, .033), (.16, .4, .026), (.61, .39, .014), (.76, -.37, .022)]},
    {"id": "l01-unequal-drags", "label": "Два следа волочения", "width": 2.1,
     "strokes": [((-.85, -.17), (.67, -.02), .17, 1.05),
                 ((-.5, .4), (.50, .21), .105, .7)],
     "pools": [(.6, -.04, .14, .15, .49)], "drops": [(.72, -.36, .022), (-.32, -.47, .017)]},
    {"id": "l02-snagged", "label": "Волочение с рывком", "width": 2.45,
     "strokes": [((-.86, .28), (.20, -.09), .19, .77),
                 ((.07, -.15), (.70, -.05), .25, 1.14)],
     "pools": [(.66, -.03, .15, .22, .58)],
     "drops": [(-.24, .39, .026), (.32, .38, .013), (.76, -.47, .024)]},
    {"id": "l03-broad-thin", "label": "Широкий затёртый след", "width": 2.8,
     "strokes": [((-.87, .09), (.71, -.13), .33, .49),
                 ((-.6, -.23), (.55, -.26), .075, .36)],
     "pools": [], "drops": [(-.53, .49, .012), (.73, .35, .023), (.20, -.5, .014)]},
    {"id": "l04-heavy-drag", "label": "Густой смаз и россыпь", "width": 3.2,
     "strokes": [((-.89, .09), (.63, -.11), .27, 1.08),
                 ((-.68, .35), (.56, .18), .10, .72)],
     "pools": [(.60, -.12, .25, .25, .87), (.35, -.28, .17, .11, .37)],
     "drops": [(.65, .43, .032), (.81, .31, .014), (.80, -.47, .022), (-.62, -.39, .016), (.12, .49, .01)]},
]


def build_shape(sample, rng):
    density = np.zeros_like(blood.X)
    for parameters in sample["strokes"]:
        density += blood.transfer(rng, *parameters)
    for parameters in sample["pools"]:
        density += blood.deposit(rng, *parameters)
    density += blood.droplets(rng, sample["drops"])
    return density


def export(sample, seed, limit):
    image = blood.rgba_from_density(build_shape(sample, np.random.default_rng(seed)))
    bounds = image.getchannel("A").getbbox()
    assert bounds is not None
    left, top, right, bottom = bounds
    image = image.crop((max(0, left - 12), max(0, top - 12), min(1024, right + 12), min(1024, bottom + 12)))
    image.thumbnail((limit - 24, limit - 24), Image.Resampling.LANCZOS)
    # Add clean padding after resampling, preserving the shape's aspect ratio.
    padded = Image.new("RGBA", (image.width + 24, image.height + 24))
    padded.alpha_composite(image, (12, 12))
    path = DEST / "Sprites" / (sample["id"] + ".png")
    padded.save(path)
    alpha = np.asarray(padded)[..., 3]
    assert not np.any(alpha[[0, -1], :]) and not np.any(alpha[:, [0, -1]])
    return {"id": sample["id"], "label": sample["label"], "seed": seed,
            "sizeClass": sample["id"][0], "pixels": list(padded.size),
            "suggestedCanvasWidthUnits": sample["width"],
            "alphaMax": int(alpha.max()), "parameters": sample}


def floor_canvas(kind, width, height, pixels_per_unit):
    names = ["warehouse-concrete-base-01.png", "lab-tile-base-01.png"]
    tile = Image.open(ROOT / "Concepts/Used/Environment/FloorMaterials01" / names[kind]).convert("RGBA")
    # Production floor tiles repeat every six units; keep this scale in previews.
    side = int(6 * pixels_per_unit)
    tile = tile.resize((side, side), Image.Resampling.LANCZOS)
    result = Image.new("RGBA", (width, height))
    for y in range(0, height, side):
        for x in range(0, width, side):
            result.paste(tile, (x, y))
    return result


def previews(entries):
    font = lambda size: ImageFont.truetype("C:/Windows/Fonts/arial.ttf", size)
    cell, gap, row, header, ppu = 350, 14, 292, 98, 98
    width = cell * 4 + gap * 5
    for kind, name in [(0, "concrete"), (1, "tile")]:
        board = Image.new("RGB", (width, header + row * 3 + 20), "#192023")
        draw = ImageDraw.Draw(board)
        draw.text((gap, 13), "ПРОБА 05 · МАЛЫЕ / СРЕДНИЕ / КРУПНЫЕ", font=font(27), fill="#e1e4de")
        draw.text((gap, 52), "Общий масштаб: 1 игровая единица = 98 px. Подпись — ширина PNG в сцене.", font=font(20), fill="#acb8b4")
        for index, entry in enumerate(entries):
            x = gap + (index % 4) * (cell + gap)
            y = header + (index // 4) * row
            floor = floor_canvas(kind, cell, row - 56, ppu)
            sprite = Image.open(DEST / "Sprites" / (entry["id"] + ".png"))
            target_width = round(entry["suggestedCanvasWidthUnits"] * ppu)
            target_height = max(1, round(sprite.height * target_width / sprite.width))
            sprite = sprite.resize((target_width, target_height), Image.Resampling.LANCZOS)
            floor.alpha_composite(sprite, ((cell - target_width) // 2, (floor.height - target_height) // 2))
            board.paste(floor.convert("RGB"), (x, y))
            label = f"{entry['id'][:3].upper()} · {entry['suggestedCanvasWidthUnits']:g} ед."
            draw.text((x + 3, y + row - 49), label, font=font(19), fill="#e1e4de")
            draw.text((x + 3, y + row - 25), entry["label"], font=font(17), fill="#acb8b4")
        board.save(DEST / ("sizes-" + name + ".png"))

    detail = Image.new("RGB", (width, 98 + 3 * 275), "#192023")
    draw = ImageDraw.Draw(detail)
    draw.text((gap, 14), "ПРОБА 05 · ФОРМЫ КРУПНЫМ ПЛАНОМ", font=font(28), fill="#e1e4de")
    draw.text((gap, 53), "Увеличение каждого примера для оценки текстуры; масштаб здесь разный.", font=font(20), fill="#acb8b4")
    for index, entry in enumerate(entries):
        x = gap + (index % 4) * (cell + gap)
        y = 98 + (index // 4) * 275
        floor = floor_canvas(0, cell, 220, 98)
        sprite = Image.open(DEST / "Sprites" / (entry["id"] + ".png"))
        sprite.thumbnail((cell - 32, 194), Image.Resampling.LANCZOS)
        floor.alpha_composite(sprite, ((cell - sprite.width) // 2, (floor.height - sprite.height) // 2))
        detail.paste(floor.convert("RGB"), (x, y))
        draw.text((x + 3, y + 229), entry["id"][:3].upper() + " · " + entry["label"], font=font(17), fill="#e1e4de")
    detail.save(DEST / "details.png")


def main():
    (DEST / "Sprites").mkdir(parents=True, exist_ok=True)
    entries = []
    for index, sample in enumerate(SAMPLES):
        limit = [512, 768, 1024][index // 4]
        entries.append(export(sample, 71381 + index * 997, limit))
    previews(entries)
    metadata = {"date": "2026-10-18", "status": "Offline expansion; owner evaluation pending; no Unity import",
                "generator": "Tools/Art/bake_blood_trace_variations_03.py",
                "reused": "Tools/Art/bake_blood_traces_03.py and BloodPools01 material",
                "method": "Independent contact deviations and torn ends; no shared wave displacement or AI images",
                "revision": "05; original BloodTraces03 and BloodTraces04 retained without changes",
                "runtimeGeneration": False, "units": "Suggested canvas widths in Unity units, not physical metres; not assigned to prefabs",
                "entries": entries,
                "sha256": {p.relative_to(DEST).as_posix(): hashlib.sha256(p.read_bytes()).hexdigest()
                           for p in sorted(DEST.rglob("*.png"))}}
    (DEST / "provenance.json").write_text(json.dumps(metadata, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"newVariants": len(entries), "totalFamilyVariants": len(entries) + 4,
                      "suggestedWidths": [entry["suggestedCanvasWidthUnits"] for entry in entries]}))


if __name__ == "__main__":
    main()
