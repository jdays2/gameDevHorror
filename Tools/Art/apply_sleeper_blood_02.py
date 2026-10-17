"""Bake generated blood decals onto the existing registered Sleeper drawings.

Run from the Unity project folder with Pillow and numpy. Source frames come from
the recorded Git revision so running the bake again never accumulates paint.
No silhouette, atlas geometry, native Sprite or meta is regenerated.
"""
from pathlib import Path
from io import BytesIO
import argparse
import hashlib
import json
import re
import subprocess

import numpy as np
from PIL import Image, ImageDraw, ImageFilter


PROJECT = Path(__file__).resolve().parents[2]
ROOT = PROJECT / "Assets/EmergencyShift/Art/Enemies/Sleeper/StalkerPlaceholder01"
LIBRARY = PROJECT / "Concepts/Used/Sleeper/BloodMarks02"
SOURCE_REVISION = "e267261"
POSE_OFFSETS = [(0, 0), (.0410714, -.0205357), (.0164286, -.0410714), (.0472321, -.02875)]


def original(relative):
    path = (ROOT / relative).relative_to(PROJECT).as_posix()
    data = subprocess.check_output(["git", "show", f"{SOURCE_REVISION}:{path}"], cwd=PROJECT)
    return Image.open(BytesIO(data)).convert("RGBA")


def sprite_info(path, height):
    text = path.read_text(encoding="utf-8-sig")
    rect = re.search(r"m_Rect:.*?x: ([\d.]+).*?y: ([\d.]+).*?width: ([\d.]+).*?height: ([\d.]+)", text, re.S)
    x, y, width, size_y = map(float, rect.groups())
    pivot = re.search(r"m_Pivot: \{x: ([\d.]+), y: ([\d.]+)", text)
    px, py = map(float, pivot.groups())
    ppu = float(re.search(r"m_PixelsToUnits: ([\d.]+)", text).group(1))
    return (int(x), int(height - y - size_y), int(x + width), int(height - y)), (x + width * px, height - y - size_y * py), ppu


def patches():
    atlas = Image.open(LIBRARY / "Source/blood-decals.png").convert("RGBA")
    width, height = atlas.size
    result = []
    for row in range(2):
        for column in range(2):
            tile = atlas.crop((column * width // 2, row * height // 2, (column + 1) * width // 2, (row + 1) * height // 2))
            bounds = tile.getchannel("A").getbbox()
            if bounds is None:
                raise ValueError("Empty generated blood decal")
            result.append(tile.crop(bounds))
    return result


def paint(canvas, decal, center, size, angle=0, strength=.86):
    mark = decal.resize((max(2, round(size[0])), max(2, round(size[1]))), Image.Resampling.LANCZOS)
    mark = mark.rotate(angle, Image.Resampling.BICUBIC, expand=True)
    overlay = Image.new("RGBA", canvas.size)
    overlay.paste(mark, (round(center[0] - mark.width / 2), round(center[1] - mark.height / 2)))
    destination = np.asarray(canvas).copy()
    source = np.asarray(overlay).astype(np.float32)
    opacity = source[:, :, 3:4] / 255 * strength
    opacity *= (destination[:, :, 3:4] > 0)
    # Retain the original skin folds below the surface paint.
    luminance = destination[:, :, :3].mean(axis=2, keepdims=True) / 255
    pigment = source[:, :, :3] * (.65 + .35 * luminance)
    destination[:, :, :3] = np.rint(destination[:, :, :3] * (1 - opacity) + pigment * opacity).clip(0, 255).astype(np.uint8)
    return Image.fromarray(destination)


def pale_eye(canvas, center, radius):
    data = np.asarray(canvas).copy()
    yy, xx = np.indices(data.shape[:2])
    distance = ((xx - center[0]) / radius[0]) ** 2 + ((yy - center[1]) / radius[1]) ** 2
    weight = np.clip((1 - distance) / .08, 0, 1)[:, :, None]
    weight *= (data[:, :, 3:4] > 0)
    rgb = data[:, :, :3].astype(np.float32)
    luminance = rgb[:, :, 0] * .24 + rgb[:, :, 1] * .64 + rgb[:, :, 2] * .12
    # Almost white sclera, retaining the actual radial fibres and fine veins.
    light = (112 + luminance * .55)[:, :, None]
    target = light * np.array([1, .995, .98]) + (rgb - luminance[:, :, None]) * .10
    data[:, :, :3] = np.rint(rgb * (1 - weight) + target * weight).clip(0, 255).astype(np.uint8)
    return Image.fromarray(data)


def blood_centres(image, rect, eye_centre, minimum_distance):
    crop = np.asarray(image.crop(rect))
    rgb = crop[:, :, :3].astype(float)
    mask = (rgb[:, :, 0] > rgb[:, :, 1] * 1.6) & (rgb[:, :, 0] > rgb[:, :, 2] * 1.3) & (rgb[:, :, 0] > 65) & (crop[:, :, 3] > 128)
    # Join adjacent drops of the same original claw mark, without finding skin.
    mask = np.asarray(Image.fromarray((mask * 255).astype(np.uint8)).filter(ImageFilter.MaxFilter(7))) > 0
    visited = np.zeros(mask.shape, bool)
    components = []
    for y, x in zip(*np.where(mask)):
        if visited[y, x]:
            continue
        stack = [(y, x)]
        visited[y, x] = True
        points = []
        while stack:
            cy, cx = stack.pop()
            points.append((cx, cy))
            for dy, dx in ((0, 1), (0, -1), (1, 0), (-1, 0)):
                ny, nx = cy + dy, cx + dx
                if 0 <= ny < mask.shape[0] and 0 <= nx < mask.shape[1] and mask[ny, nx] and not visited[ny, nx]:
                    visited[ny, nx] = True
                    stack.append((ny, nx))
        if len(points) >= 24:
            points = np.array(points)
            centre = points.mean(axis=0)
            location = (centre[0] + rect[0], centre[1] + rect[1])
            distance = np.hypot(location[0] - eye_centre[0], location[1] - eye_centre[1])
            if distance > minimum_distance:
                components.append((len(points), location))
    selected = [centre for count, centre in sorted(components, reverse=True)[:3]]
    # Keep each distinct stain on the same anatomical claw across the poses.
    # Pixel area changes during folding and must not determine stain identity.
    return sorted(selected, key=lambda point: np.arctan2(point[1] - eye_centre[1], point[0] - eye_centre[0]))


def bake_existing(decals):
    results = []
    for family, prefix in (("walk", "Walk"), ("recoil", "Recoil"), ("attack", "Attack"), ("death", "Death")):
        relative = f"PaleFlesh/{family}-sheet.png"
        before = original(relative)
        image = before.copy()
        frames = sorted((ROOT / "Frames").glob(prefix + "*.asset"))
        for index, path in enumerate(frames):
            rect, centre, ppu = sprite_info(path, image.height)
            if family == "recoil":
                offset = POSE_OFFSETS[index]
                centre = (centre[0] + offset[0] * ppu, centre[1] - offset[1] * ppu)
            if family in ("walk", "attack"):
                image = pale_eye(image, centre, (.10 * ppu, .105 * ppu))
            for number, (dx, dy, width, height, angle) in enumerate(((-.21, -.065, .065, .035, -35), (.21, .045, .045, .06, 20), (-.13, .16, .055, .035, 5))):
                image = paint(image, decals[number], (centre[0] + dx * ppu, centre[1] + dy * ppu), (width * ppu, height * ppu), angle)
            if family != "death":
                claw_distance = (.15 if family == "recoil" else .4) * ppu
                for number, point in enumerate(blood_centres(before, rect, centre, claw_distance)):
                    image = paint(image, decals[number % 3], point, (.10 * ppu, .055 * ppu), 30 - number * 35)
        if family == "death":
            # Same anatomical claws in the two intact poses and the released limbs.
            for number, point in enumerate(((172, 130), (548, 343), (170, 543), (758, 139), (1151, 358), (787, 537), (214, 713), (541, 953), (160, 1100))):
                image = paint(image, decals[number % 3], point, (32, 18), -20 + number * 17)
        image.save(ROOT / relative)
        old = np.asarray(before)
        new = np.asarray(image)
        assert np.array_equal(old[:, :, 3], new[:, :, 3])
        assert np.array_equal(old[old[:, :, 3] == 0], new[old[:, :, 3] == 0])
        results.append({"texture": relative, "alphaUnchanged": True, "sizeUnchanged": image.size == before.size, "changedPixels": int(np.any(old != new, axis=2).sum())})
    return results


def bake_eyes(decals):
    before = original("LargeEye01/eye-atlas.png")
    image = before.copy()
    centres = [(312, 333), (939, 333), (312, 959), (939, 959)]
    radii = [(149, 140), (142, 29), (142, 2), None]
    for centre, radius in zip(centres, radii):
        if radius is not None:
            image = pale_eye(image, centre, radius)
        for number, (dx, dy, width, height, angle) in enumerate(((-.145, -.14, .065, .035, -50), (.175, .045, .04, .06, 15), (-.10, .17, .035, .045, 0))):
            image = paint(image, decals[number], (centre[0] + dx * 1160, centre[1] + dy * 1160), (width * 1160, height * 1160), angle)
    image.save(ROOT / "LargeEye01/eye-atlas.png")
    assert np.array_equal(np.asarray(before)[:, :, 3], np.asarray(image)[:, :, 3])
    return dead_eye_organ(image)


def pupil_layer(centre, radius):
    pupil = Image.new("RGBA", (560 * 4, 560 * 4))
    drawing = ImageDraw.Draw(pupil)
    drawing.ellipse((round((centre[0] - radius[0]) * 4), round((centre[1] - radius[1]) * 4), round((centre[0] + radius[0]) * 4), round((centre[1] + radius[1]) * 4)), fill=(6, 4, 6, 255))
    return pupil.resize((560, 560), Image.Resampling.LANCZOS)


def dead_eye_organ(image):
    """Assemble a fixed, slightly rolled eye without changing the live atlas.

    Warp only the exposed fibres, like the runtime gaze shader. The fleshy
    membrane stays registered; the pupil is foreshortened and clipped by it.
    """
    organ = image.crop((32, 44, 592, 604))
    backing = Image.new("RGBA", organ.size)
    drawing = ImageDraw.Draw(backing)
    drawing.ellipse((164, 173, 396, 405), fill=(214, 194, 191, 255))
    organ = Image.alpha_composite(backing, organ)
    # Preserve the previous organ's exact alpha, including nearly opaque
    # source pixels that the old centered pupil covered.
    original_alpha = Image.alpha_composite(organ, pupil_layer((280, 289), (75.4, 75.4))).getchannel("A")
    data = np.asarray(organ).copy()
    yy, xx = np.indices(data.shape[:2], dtype=np.float32)
    distance = np.sqrt(((xx - 280) / 154.28) ** 2 + ((yy - 289) / 146.16) ** 2)
    # Image coordinates: up and left, with a small part behind the upper lid.
    gaze = (-30, -82)
    # A disk-preserving map prevents a large rolled gaze from pulling the
    # lower membrane into the eye. Only the inner ocular surface is sampled.
    position = (xx - 280) / 154.28 + 1j * (yy - 289) / 146.16
    offset = gaze[0] / 154.28 + 1j * gaze[1] / 146.16
    source = (position - offset) / (1 - np.conjugate(offset) * position)
    source_x = np.clip(280 + source.real * 154.28, 0, organ.width - 1)
    source_y = np.clip(289 + source.imag * 146.16, 0, organ.height - 1)
    x0 = source_x.astype(int)
    y0 = source_y.astype(int)
    x1 = np.minimum(x0 + 1, organ.width - 1)
    y1 = np.minimum(y0 + 1, organ.height - 1)
    fx = (source_x - x0)[:, :, None]
    fy = (source_y - y0)[:, :, None]
    rgb = data[:, :, :3].astype(np.float32)
    shifted = (rgb[y0, x0] * (1 - fx) + rgb[y0, x1] * fx) * (1 - fy)
    shifted += (rgb[y1, x0] * (1 - fx) + rgb[y1, x1] * fx) * fy
    transition = np.clip((distance - .91) / .09, 0, 1)
    aperture = 1 - transition * transition * (3 - 2 * transition)
    data[:, :, :3] = np.rint(rgb * (1 - aperture[:, :, None]) + shifted * aperture[:, :, None]).clip(0, 255).astype(np.uint8)
    organ = Image.fromarray(data)
    centre = (280 + gaze[0], 289 + gaze[1])
    radius = (75.4 * .94, 75.4 * .72)
    pupil = pupil_layer(centre, radius)
    pupil_data = np.asarray(pupil).copy()
    pupil_data[:, :, 3] = np.rint(pupil_data[:, :, 3] * aperture).astype(np.uint8)
    organ = Image.alpha_composite(organ, Image.fromarray(pupil_data))
    organ.putalpha(original_alpha)
    return organ


def corpse_parts(organ, decals):
    atlas = Image.open(LIBRARY / "Source/generated-cores.png").convert("RGBA")
    production = PROJECT / "Assets/EmergencyShift/Art/Enemies/Sleeper/BloodMarks02"
    production.mkdir(parents=True, exist_ok=True)
    parts = []
    for index, name in enumerate(("HollowCore", "SplitCore", "RetainedEyeCore")):
        column, row = index % 2, index // 2
        tile = atlas.crop((column * atlas.width // 2, row * atlas.height // 2, (column + 1) * atlas.width // 2, (row + 1) * atlas.height // 2))
        if name == "RetainedEyeCore":
            # Cover the rejected generated eyeball completely with the gameplay eye.
            eye = organ.resize((330, 330), Image.Resampling.LANCZOS)
            tile.alpha_composite(eye, (round(tile.width * .61 - 165), round(tile.height * .50 - 170)))
        bounds = tile.getchannel("A").getbbox()
        tile = tile.crop(bounds)
        tile.save(production / (name + ".png"))
        parts.append({"name": name, "worldWidth": .68 if index != 1 else .72})
    detached = Image.new("RGBA", (700, 730))
    tail = decals[2].resize((120, 260), Image.Resampling.LANCZOS).rotate(30, Image.Resampling.BICUBIC, expand=True)
    detached.alpha_composite(tail, (270, 420))
    detached.alpha_composite(organ, (70, 40))
    detached = detached.crop(detached.getchannel("A").getbbox())
    detached.save(production / "DetachedEye.png")
    parts.append({"name": "DetachedEye", "worldWidth": .44})
    return parts


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--dead-eye-only", action="store_true", help="Reassemble remains from the current live eye; leave live atlases untouched")
    arguments = parser.parse_args()
    decals = patches()
    if arguments.dead_eye_only:
        image = Image.open(ROOT / "LargeEye01/eye-atlas.png").convert("RGBA")
        parts = corpse_parts(dead_eye_organ(image), decals)
        print(json.dumps({"corpseParts": len(parts), "liveAtlasesUntouched": True, "fixedGazePixels": [-30, -82]}))
        return
    changed = bake_existing(decals)
    organ = bake_eyes(decals)
    parts = corpse_parts(organ, decals)
    report = {
        "sourceRevision": SOURCE_REVISION,
        "textures": changed,
        "eyeAlphaUnchanged": True,
        "corpseParts": parts,
        "eyeSource": "Actual LargeEye01 EyeOpen artwork; static corpse pupil rolled up-left and foreshortened. Generated eyeballs rejected.",
        "latestRemainsCorrection": "Docs/Validation/SleeperDeadEye03.json",
    }
    (LIBRARY / "bake-report.json").write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"updatedAtlases": len(changed) + 1, "corpseParts": len(parts), "sourceRevision": SOURCE_REVISION}))


if __name__ == "__main__":
    main()
