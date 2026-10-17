"""Slice a simple torn-muscle master into matching Stalker/Sleeper palettes.

One anatomy source, two baked palettes; gameplay uses ordinary sprite references.
Existing warm sprite canvases and native Sprite geometry remain unchanged.
"""
from pathlib import Path
import json

import numpy as np
from PIL import Image


PROJECT = Path(__file__).resolve().parents[2]
LIBRARY = PROJECT / "Concepts/Used/Shared/FleshChunks04"
PRODUCTION = PROJECT / "Assets/EmergencyShift/Art/Effects/CreatureFlesh04"
NAMES = ["MuscleWad", "TornFlap", "DenseClot", "TissueStrip"]


def skin_palette(path, cold):
    data = np.asarray(Image.open(path).convert("RGBA")).astype(float)
    red, green, blue, alpha = data.transpose(2, 0, 1)
    if cold:
        mask = (green > red + 6) & (blue > red + 4)
    else:
        mask = (red > green + 12) & (red < green + 65) & (blue > green)
    mask &= (alpha > 245) & (green > 105) & (green < 205)
    return np.median(data[:, :, :3][mask], axis=0)


def recolor(image, palette):
    data = np.asarray(image).copy()
    rgb = data[:, :, :3].astype(float)
    light = rgb @ np.array([.24, .64, .12])
    palette_light = palette @ np.array([.24, .64, .12])
    target = light[:, :, None] * palette / palette_light
    redness = rgb[:, :, 0] - np.maximum(rgb[:, :, 1], rgb[:, :, 2])
    skin = 1 - np.clip((redness - 70) / 35, 0, 1)
    weight = skin[:, :, None]
    data[:, :, :3] = np.rint(rgb * (1 - weight) + target * weight).clip(0, 255).astype(np.uint8)
    return Image.fromarray(data)


def main():
    source = Image.open(LIBRARY / "Source/simple-flesh-sheet.png").convert("RGBA")
    warm = skin_palette(PROJECT / "Assets/EmergencyShift/Art/Enemies/Stalker/PaleFlesh/walk-sheet.png", False)
    cold = skin_palette(PROJECT / "Assets/EmergencyShift/Art/Enemies/Sleeper/StalkerPlaceholder01/PaleFlesh/walk-sheet.png", True)
    (PRODUCTION / "Sleeper").mkdir(exist_ok=True)
    for index, name in enumerate(NAMES):
        column, row = index % 2, index // 2
        tile = source.crop((column * source.width // 2, row * source.height // 2, (column + 1) * source.width // 2, (row + 1) * source.height // 2))
        bounds = tile.getchannel("A").point(lambda x: 255 if x > 8 else 0).getbbox()
        tile = tile.crop(bounds)
        size = Image.open(PRODUCTION / (name + ".png")).size
        tile.thumbnail((size[0] - 4, size[1] - 4), Image.Resampling.LANCZOS)
        canvas = Image.new("RGBA", size)
        canvas.alpha_composite(tile, ((size[0] - tile.width) // 2, (size[1] - tile.height) // 2))
        recolor(canvas, warm).save(PRODUCTION / (name + ".png"))
        recolor(canvas, cold).save(PRODUCTION / "Sleeper" / (name + ".png"))
    report = {"stalkerSkinMedianRGB": warm.tolist(), "sleeperSkinMedianRGB": cold.tolist(), "geometryShared": True, "canvasesPreserved": True}
    (LIBRARY / "palette-report.json").write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(report))


if __name__ == "__main__":
    main()
