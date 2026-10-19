"""Archive one AI atlas and composite previews; do not repaint source pixels."""
from pathlib import Path
import hashlib
import json

from PIL import Image, ImageDraw, ImageFont


ROOT = Path(__file__).resolve().parents[4]
DEST = ROOT / "Concepts/Unused/Environment/HumanRemains03"
SOURCE = DEST / "atlas.png"
FLOORS = ROOT / "Concepts/Used/Environment/FloorMaterials01"
FONT = "C:/Windows/Fonts/arial.ttf"


def font(size):
    return ImageFont.truetype(FONT, size)


def patch(kind, size):
    name = "warehouse-concrete-base-01.png" if kind == "concrete" else "lab-tile-base-01.png"
    base = Image.open(FLOORS / name).convert("RGBA")
    # Existing 6-unit floor source: 128 px per unit in these static previews.
    base = base.resize((768, 768), Image.Resampling.LANCZOS)
    result = Image.new("RGBA", size)
    for y in range(0, size[1], 768):
        for x in range(0, size[0], 768):
            result.alpha_composite(base, (x, y))
    return result


def main():
    DEST.mkdir(parents=True, exist_ok=True)
    atlas = Image.open(SOURCE).convert("RGBA")
    cells = [(0, 0, 512, 620), (512, 0, 1024, 620), (1024, 0, 1536, 620),
             (0, 620, 512, 1024), (512, 620, 1024, 1024), (1024, 620, 1536, 1024)]
    names = ["Охранник", "Лаборант", "Торс", "Рука", "Нога", "Плоть / ткань"]
    crops = []
    for cell in cells:
        crop = atlas.crop(cell)
        bounds = crop.getchannel("A").point(lambda value: 255 if value > 16 else 0).getbbox()
        if bounds is None:
            raise ValueError("Empty atlas cell")
        crops.append(crop.crop(bounds))

    overview = Image.new("RGBA", (1200, 850), "#151d20")
    draw = ImageDraw.Draw(overview)
    draw.text((30, 24), "ЖЕРТВЫ · ПРОБА 03", font=font(29), fill="#d5dfda")
    draw.text((30, 65), "Один тестовый набор · крупные формы · без лиц", font=font(19), fill="#a4b4b4")
    for index, crop in enumerate(crops):
        x = 24 + (index % 3) * 392
        y = 110 + (index // 3) * 355
        background = patch("concrete" if index % 2 == 0 else "tile", (376, 304))
        factor = min(330 / crop.width, 275 / crop.height)
        if index == 2:
            factor *= .82
        if index >= 3:
            factor *= .72
        sprite = crop.resize((round(crop.width * factor), round(crop.height * factor)), Image.Resampling.LANCZOS)
        background.alpha_composite(sprite, ((376 - sprite.width) // 2, (304 - sprite.height) // 2))
        overview.alpha_composite(background, (x, y))
        draw.text((x + 8, y + 313), names[index], font=font(20), fill="#d5dfda")
    overview.convert("RGB").save(DEST / "overview.png")

    scale = Image.new("RGBA", (1200, 610), "#151d20")
    draw = ImageDraw.Draw(scale)
    draw.text((30, 22), "ЧИТАЕМОСТЬ В УМЕНЬШЕНИИ", font=font(27), fill="#d5dfda")
    draw.text((30, 61), "Тела около 120 px · статичная проба, не кадр из Unity", font=font(18), fill="#a4b4b4")
    # A single scale factor retains source relative sizes in the reading test.
    factor = 120 / crops[1].height
    for row, kind in enumerate(["concrete", "tile"]):
        background = patch(kind, (1152, 215))
        for index, crop in enumerate(crops):
            sprite = crop.resize((max(1, round(crop.width * factor)), max(1, round(crop.height * factor))), Image.Resampling.LANCZOS)
            background.alpha_composite(sprite, (46 + index * 188, (215 - sprite.height) // 2))
        scale.alpha_composite(background, (24, 110 + row * 230))
    scale.convert("RGB").save(DEST / "camera-scale.png")

    print(json.dumps({"directory": str(DEST), "sourceMode": atlas.mode, "sourceSize": atlas.size,
                      "cellBounds": cells, "sourceSha256": hashlib.sha256(SOURCE.read_bytes()).hexdigest(),
                      "processing": "source copied unchanged; crop/resize/alpha-composite for previews only"}, ensure_ascii=False))


if __name__ == "__main__":
    main()
