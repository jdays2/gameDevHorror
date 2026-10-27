"""Registered current Sleeper mouth reference and read-only palette comparison.

The geometry/registration is owned by Stalker Postures05.attack_mouth. Only the
matched current BloodMarks02 PNG changes, preserving the native cavity/teeth.
"""
from pathlib import Path
from hashlib import sha256
import importlib.util
import json
import sys

import numpy as np
from PIL import Image, ImageDraw


FOLDER = Path(__file__).resolve().parent
PROJECT = FOLDER.parents[3]
SOURCE_FAMILY = PROJECT / "Concepts/Used/Stalker/Postures05"
NATIVE_ATTACK = PROJECT / "Assets/EmergencyShift/Art/Enemies/Sleeper/StalkerPlaceholder01/PaleFlesh/attack-sheet.png"


def load_source_module():
    sys.path.insert(0, str(SOURCE_FAMILY))
    spec = importlib.util.spec_from_file_location("canonical_attack_mouth", SOURCE_FAMILY / "attack_mouth.py")
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def register_reference_patch(painter, module):
    with Image.open(NATIVE_ATTACK) as image:
        source = module._decode(image.convert("RGBA"))
        source_height = image.height
    source_ppu = painter.recipe["sourcePixelsPerUnit"]
    pivot_x, pivot_y = painter.recipe["sourcePivotBottomLeftPixels"]
    ratio = source_ppu / module.PPU
    coefficients = (ratio, 0, pivot_x - module.ORIGIN[0] * ratio,
                    0, ratio, source_height - pivot_y - module.ORIGIN[1] * ratio)
    channels = []
    for channel in range(4):
        plane = Image.fromarray(source[..., channel])
        channels.append(np.asarray(plane.transform((module.SIZE, module.SIZE), Image.Transform.AFFINE,
                                                   coefficients, Image.Resampling.BICUBIC)))
    registered = np.clip(np.stack(channels, axis=-1), 0, 1)
    registered[..., :3] = np.minimum(registered[..., :3], registered[..., 3:4])
    canonical_alpha = painter.source_patch[..., 3:4]
    if np.any(canonical_alpha > registered[..., 3:4] + 1e-6):
        raise ValueError("Canonical mouth has opaque areas outside the registered native source")
    mask = np.divide(canonical_alpha, registered[..., 3:4], out=np.zeros_like(canonical_alpha),
                     where=registered[..., 3:4] > 1e-8)
    reference = registered * np.clip(mask, 0, 1)
    reference[..., 3:4] = canonical_alpha
    return reference


class NativeMouthReference:
    def __init__(self):
        self.module = load_source_module()
        self.painter = self.module.AttackMouth(PROJECT)
        self.stalker_patch = self.painter.source_patch.copy()
        stalker_path = PROJECT / self.painter.recipe["source"]
        with Image.open(stalker_path) as stalker_image, Image.open(NATIVE_ATTACK) as sleeper_image:
            if not np.array_equal(np.asarray(stalker_image.getchannel("A")), np.asarray(sleeper_image.getchannel("A"))):
                raise ValueError("Current native attack atlases have different alpha")
        self.sleeper_patch = register_reference_patch(self.painter, self.module)
        if not np.array_equal(self.stalker_patch[..., 3], self.sleeper_patch[..., 3]):
            raise ValueError("Native registered mouth sources no longer have identical alpha")
        self.recipe = {
            "stalkerSource": stalker_path.relative_to(PROJECT).as_posix(),
            "stalkerSourceSha256": sha256(stalker_path.read_bytes()).hexdigest(),
            "sleeperSource": NATIVE_ATTACK.relative_to(PROJECT).as_posix(),
            "sleeperSourceSha256": sha256(NATIVE_ATTACK.read_bytes()).hexdigest(),
            "canonicalMouthScript": (SOURCE_FAMILY / "attack_mouth.py").relative_to(PROJECT).as_posix(),
            "canonicalMouthScriptSha256": sha256((SOURCE_FAMILY / "attack_mouth.py").read_bytes()).hexdigest(),
            "registration": self.painter.recipe,
            "registeredPatchAlphaIdentical": True,
            "nativeAtlasAlphaIdentical": True,
            "palettePolicy": "Actual native Sleeper cavity, teeth, lips and skin; no generic mouth tint or whitening",
            "composition": "Replace the mouth contribution in linear premultiplied color; preserve packed whole alpha bytes",
        }

    def layer(self, pose, sleeper=True):
        self.painter.source_patch = self.sleeper_patch if sleeper else self.stalker_patch
        return self.painter.layer(pose)

    def replace_palette(self, original, recolored, pose, pivot, matrix, transform_palette, source_family=None):
        if self.module.opening(pose) <= 0:
            return recolored
        source = self.layer(pose, False)
        target = self.layer(pose, True)
        if not np.array_equal(source[..., 3], target[..., 3]):
            raise ValueError("Warped native mouth alpha differs between the species")
        if source_family is not None:
            path = source_family / "Masks" / f'mouth-{pose["name"]}.png'
            with Image.open(path) as image:
                saved = np.asarray(image.convert("RGBA"))
            if not np.array_equal(saved, np.asarray(self.module.encode(source))):
                raise ValueError("Mouth mask is stale relative to the canonical warper: " + pose["name"])
        left = round(self.module.ORIGIN[0] - pivot[0])
        top = round(self.module.ORIGIN[1] - pivot[1])
        source = source[top:top + original.height, left:left + original.width]
        target = target[top:top + original.height, left:left + original.width]
        whole = self.module._decode(original)
        transmittance = 1 - source[..., 3:4]
        below = np.divide(whole - source, transmittance, out=np.zeros_like(whole), where=transmittance > 1e-6)
        below = np.clip(below, 0, 1)
        below[..., :3] = np.minimum(below[..., :3], below[..., 3:4])
        # Each saved layer is straight RGBA. Decoding creates the working
        # premultiplied representation once; this is not a second unpremultiply.
        below_image = self.module.encode(below)
        cold_below = self.module._decode(transform_palette(below_image, matrix))
        combined = target + cold_below * (1 - target[..., 3:4])
        replacement = np.asarray(self.module.encode(combined))
        result = np.asarray(recolored).copy()
        visible_mouth = (source[..., 3] > 0) & (result[..., 3] > 0)
        result[..., :3][visible_mouth] = replacement[..., :3][visible_mouth]
        return Image.fromarray(result)


def compare():
    spec = importlib.util.spec_from_file_location("sleeper_recolor", FOLDER / "recolor_body_frames.py")
    recolor = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(recolor)
    matrix, unused = recolor.palette_from_current_pngs()
    reference = NativeMouthReference()
    samples = (3, 6, 9, 12, 15, 18)
    sheet = Image.new("RGB", (1800, 960), "#00ff28")
    draw = ImageDraw.Draw(sheet)
    rows = []
    for column, sample in enumerate(samples):
        pose = {"group": "strike", "sample": sample}
        source = np.asarray(reference.module.encode(reference.layer(pose, False)))
        target = np.asarray(reference.module.encode(reference.layer(pose, True)))
        mapped = np.asarray(recolor.transform_palette(Image.fromarray(source), matrix))
        rgb = source[..., :3].astype(float)
        opaque = source[..., 3] > 230
        masks = {
            "deepRed": opaque & (rgb[..., 0] > rgb[..., 1] * 1.8) & (rgb[..., 0] > rgb[..., 2] * 1.3) & (rgb[..., 0] < 65),
            "red": opaque & (rgb[..., 0] > rgb[..., 1] * 1.6) & (rgb[..., 0] > rgb[..., 2] * 1.3),
            "neutralWhite": opaque & (rgb.min(axis=-1) > 130) & (np.ptp(rgb, axis=-1) < 25),
        }
        colors = {}
        for name, mask in masks.items():
            if mask.any():
                colors[name] = {
                    "pixels": int(mask.sum()),
                    "stalkerMeanRgb": np.round(source[..., :3][mask].mean(0), 3).tolist(),
                    "affineMeanRgb": np.round(mapped[..., :3][mask].mean(0), 3).tolist(),
                    "nativeSleeperMeanRgb": np.round(target[..., :3][mask].mean(0), 3).tolist(),
                }
        rows.append({"sample": sample, "opening": reference.module.opening(pose), "colors": colors})
        for row, (label, pixels) in enumerate((("Stalker native", source), ("Global affine candidate", mapped), ("Sleeper native reference", target))):
            crop = Image.fromarray(pixels).crop((221, 158, 355, 272)).resize((288, 245), Image.Resampling.NEAREST)
            sheet.paste(crop, (column * 300 + 6, row * 320 + 48), crop)
            draw.text((column * 300 + 6, row * 320 + 8), f"{label} | sample {sample:02d}", fill="black")
    sheet.save(FOLDER / "native-mouth-palette-review.png")
    report = {"comparison": rows, "source": NATIVE_ATTACK.relative_to(PROJECT).as_posix(),
              "finding": "Global skin affine reduces red chroma in the dark cavity. Native registered Sleeper mouth reference is required.",
              "productionWritten": False}
    (FOLDER / "native-mouth-palette-review.json").write_text(json.dumps(report, indent=2), encoding="utf-8")
    (FOLDER / "mouth-palette-recipe.json").write_text(json.dumps(reference.recipe, indent=2), encoding="utf-8")
    print(json.dumps(report))


if __name__ == "__main__":
    compare()
