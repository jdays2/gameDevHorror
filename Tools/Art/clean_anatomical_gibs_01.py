"""Keep each original crop's main alpha component; preserve its canvas/pivot/scale.

Existing death atlases remain unchanged. The clean crops get their own textures;
the original native Sprite assets are rebound in Unity, preserving their GUIDs.
"""
from collections import deque
import json
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw


PROJECT = Path(__file__).resolve().parents[2]
DESTINATION = PROJECT / "Assets/EmergencyShift/Art/Effects/AnatomicalGibs01"
# Original Unity bottom-left atlas rects, captured before rebinding the Sprite assets.
# Reading their current rects would read the new local PNG origin on a repeat bake.
SOURCE_RECTS = [
    (90, 349, 186, 251),
    (355, 37, 174, 185),
    (400, 392, 129, 183),
    (35, 171, 159, 163),
    (459, 245, 156, 147),
    (297, 187, 62, 55),
]


def largest_component(alpha):
    mask = alpha > 8
    visited = np.zeros_like(mask)
    largest = []
    components = []
    for y, x in zip(*np.where(mask)):
        if visited[y, x]:
            continue
        queue = deque([(y, x)])
        visited[y, x] = True
        points = []
        while queue:
            cy, cx = queue.popleft()
            points.append((cy, cx))
            for dy in (-1, 0, 1):
                for dx in (-1, 0, 1):
                    ny = cy + dy
                    nx = cx + dx
                    if 0 <= ny < mask.shape[0] and 0 <= nx < mask.shape[1] and mask[ny, nx] and not visited[ny, nx]:
                        visited[ny, nx] = True
                        queue.append((ny, nx))
        components.append(len(points))
        if len(points) > len(largest):
            largest = points
    keep = np.zeros_like(mask)
    for y, x in largest:
        keep[y, x] = True
    # Keep the original antialias fringe within one pixel of the retained part.
    padded = np.pad(keep, 1)
    grown = np.zeros_like(keep)
    for dy in range(3):
        for dx in range(3):
            grown |= padded[dy:dy + keep.shape[0], dx:dx + keep.shape[1]]
    return grown, sorted(components, reverse=True)


def main():
    report = []
    previews = []
    for species, directory in [("Stalker", "Stalker"), ("Sleeper", "Sleeper/StalkerPlaceholder01")]:
        art = PROJECT / "Assets/EmergencyShift/Art/Enemies" / directory
        atlas = Image.open(art / "PaleFlesh/death-sheet.png").convert("RGBA")
        output = DESTINATION / species
        output.mkdir(parents=True, exist_ok=True)
        for index in range(6):
            sprite = art / "Frames" / f"Gib{index:02}.asset"
            x, y, width, height = SOURCE_RECTS[index]
            pixels = np.asarray(atlas.crop((x, atlas.height - y - height, x + width, atlas.height - y))).copy()
            before = pixels.copy()
            keep, components = largest_component(pixels[:, :, 3])
            pixels[~keep] = 0
            assert np.array_equal(pixels[keep], before[keep])
            Image.fromarray(pixels).save(output / f"Gib{index:02}.png")
            previews.append((species, index, Image.fromarray(pixels)))
            report.append({"species": species, "index": index, "sourceSprite": sprite.relative_to(PROJECT).as_posix(), "sourceRect": [x, y, width, height], "canvas": [width, height], "componentsBefore": components, "removedOpaquePixels": int(np.count_nonzero((before[:, :, 3] > 8) & ~keep))})
    family = PROJECT / "Concepts/Used/Shared/AnatomicalGibs01"
    family.mkdir(parents=True, exist_ok=True)
    sheet = Image.new("RGBA", (1380, 570), (48, 54, 54, 255))
    draw = ImageDraw.Draw(sheet)
    for row in range(2):
        for column in range(6):
            species, index, image = previews[row * 6 + column]
            origin = (column * 230 + 10, row * 285 + 28)
            sheet.alpha_composite(image, origin)
            draw.text((column * 230 + 10, row * 285 + 8), f"{species} {index}", fill="white")
    sheet.save(family / "clean-crops.png")
    (family / "crop-report.json").write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"cleanCrops": len(report), "removedPixels": sum(item["removedOpaquePixels"] for item in report)}))


if __name__ == "__main__":
    main()
