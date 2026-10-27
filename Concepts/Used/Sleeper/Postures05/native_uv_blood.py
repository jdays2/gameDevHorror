"""Study-only Sleeper material transfer through the canonical single-UV warp.

Native BloodMarks02 paint is registered in the original whole-limb material.
The canonical warp samples a signed, linear-premultiplied RGB delta using the
same inverse UVs as the skin. Alpha is never used as a second decal mask or
modified. This module has no production output or Unity entry point.
"""
from hashlib import sha256
from pathlib import Path
import json

import numpy as np
from PIL import Image

from recolor_body_frames import transform_palette


LIBRARY = Path(__file__).resolve().parent
PROJECT = LIBRARY.parents[3]
SOURCE_CROP = (30, 84, 388, 490)
SOURCE_SHAPE = (406, 358, 4)
CANVAS_SHAPE = (576, 576, 4)
SOURCE_OFFSET = (101, 88)
INTERIOR_ALPHA = 240.0 / 255.0


def _hash(path):
    return sha256(path.read_bytes()).hexdigest()


def _linear(rgb):
    rgb = np.asarray(rgb, dtype=np.float32) / 255.0
    return np.where(rgb <= 0.04045, rgb / 12.92, ((rgb + 0.055) / 1.055) ** 2.4)


def _straight_bytes(paint):
    alpha = paint[..., 3:4]
    linear = np.divide(paint[..., :3], alpha, out=np.zeros_like(paint[..., :3]), where=alpha > 1e-8)
    srgb = np.where(linear <= 0.0031308, linear * 12.92,
                    1.055 * np.maximum(linear, 0.0) ** (1.0 / 2.4) - 0.055)
    return np.rint(np.clip(srgb, 0.0, 1.0) * 255.0).astype(np.uint8)


def _over(top, bottom):
    return top + bottom * (1.0 - top[..., 3:4])


class NativeUvSleeperMaterial:
    """Read-only registered native paint; render methods return new arrays.

    ``warp_rgba(source, limb, pose, leg)`` must accept a signed RGB float32
    array, including nonzero RGB with alpha zero. It must perform the same
    single inverse-UV sample used for the canonical whole-limb skin.
    """

    def __init__(self, project=PROJECT):
        self.project = Path(project).resolve()
        stalker = self.project / "Assets/EmergencyShift/Art/Enemies/Stalker/PaleFlesh/walk-sheet.png"
        sleeper = self.project / "Assets/EmergencyShift/Art/Enemies/Sleeper/StalkerPlaceholder01/PaleFlesh/walk-sheet.png"
        palette_path = LIBRARY / "palette-map.json"
        palette = json.loads(palette_path.read_text(encoding="utf-8-sig"))
        if (_hash(stalker), _hash(sleeper)) != (palette["sourceSha256"], palette["targetSha256"]):
            raise ValueError("The prepared palette no longer matches the current native PNGs")
        self.matrix = np.asarray(palette["matrixRgbOneToRgb"], dtype=np.float32)
        with Image.open(stalker) as image:
            self.registered_source = np.asarray(image.convert("RGBA").crop(SOURCE_CROP)).copy()
        with Image.open(sleeper) as image:
            self.registered_target = np.asarray(image.convert("RGBA").crop(SOURCE_CROP)).copy()
        if not np.array_equal(self.registered_source[..., 3], self.registered_target[..., 3]):
            raise ValueError("Native Sleeper and Stalker source registration has different alpha")
        anatomy_path = self.project / "Concepts/Used/Stalker/Locomotion02/anatomy.json"
        self.anatomy = json.loads(anatomy_path.read_text(encoding="utf-8-sig"))
        if tuple(self.anatomy["crop"]) != SOURCE_CROP or self.anatomy["ppu"] != 224:
            raise ValueError("The canonical material uses a different native registration")
        self.native_linear = _linear(self.registered_target[..., :3])
        self.recipe = {
            "status": "Study helper only; no production transfer or art acceptance",
            "source": stalker.relative_to(self.project).as_posix(),
            "sourceSha256": _hash(stalker),
            "target": sleeper.relative_to(self.project).as_posix(),
            "targetSha256": _hash(sleeper),
            "paletteSha256": _hash(palette_path),
            "sourceCrop": list(SOURCE_CROP),
            "registeredAlphaByteExact": True,
            "interiorAlphaThreshold": INTERIOR_ALPHA,
            "method": "Registered native source RGB delta, canonical single inverse UV sample, final material transmittance; alpha unchanged",
        }
        for array in (self.matrix, self.registered_source, self.registered_target, self.native_linear):
            array.flags.writeable = False

    def source_layers(self, canonical_source):
        """Return palette-base and native RGB delta in original limb coordinates.

        ``canonical_source`` is the painter's complete limb after its own source
        cleanup. Its alpha and source pixels are retained, with no new cleanup,
        hole fill, feathering, target clipping, or shaft approximation here.
        """
        source = np.asarray(canonical_source, dtype=np.float32)
        if source.shape != SOURCE_SHAPE:
            raise ValueError("Expected one registered 406 by 358 whole-limb source")
        rgb = _straight_bytes(source)
        covered = source[..., 3] > 0.5
        difference = np.abs(rgb.astype(np.int16) - self.registered_source[..., :3].astype(np.int16))
        if np.any(difference[covered] > 1):
            raise ValueError("The canonical whole-limb material is not the registered native source")
        rgba = np.dstack((rgb, np.rint(np.clip(source[..., 3], 0.0, 1.0) * 255.0).astype(np.uint8)))
        palette_rgb = np.asarray(transform_palette(Image.fromarray(rgba), self.matrix))[..., :3]
        base = source.copy()
        base[..., :3] = _linear(palette_rgb) * source[..., 3:4]
        delta = np.zeros_like(source)
        interior = (source[..., 3] >= INTERIOR_ALPHA) & (self.registered_source[..., 3] >= 240)
        delta[..., :3] = (self.native_linear - _linear(palette_rgb)) * source[..., 3:4] * interior[..., None]
        if not np.array_equal(base[..., 3], source[..., 3]) or np.any(delta[..., 3]):
            raise AssertionError("Material preparation changed alpha")
        return base, delta

    def torso_layers(self, canonical_torso):
        torso = np.asarray(canonical_torso, dtype=np.float32)
        if torso.shape != CANVAS_SHAPE:
            raise ValueError("Expected the registered 576 by 576 torso canvas")
        x, y = SOURCE_OFFSET
        height, width = SOURCE_SHAPE[:2]
        outside = torso[..., 3].copy()
        outside[y:y + height, x:x + width] = 0
        if np.any(outside):
            raise ValueError("The canonical torso extends outside the registered material")
        base_crop, delta_crop = self.source_layers(torso[y:y + height, x:x + width])
        base = torso.copy()
        delta = np.zeros_like(torso)
        base[y:y + height, x:x + width] = base_crop
        delta[y:y + height, x:x + width] = delta_crop
        return base, delta

    def paint(self, pose, canonical_sources, canonical_torso, warp_rgba, canonical_whole=None):
        """Compose a new Sleeper frame; return premultiplied paint and evidence.

        Caller supplies the canonical painter's immutable source layers and its
        exact warp callable. The added native paint is masked only in RGB by
        final own-material visibility and opaque interior. No target alpha is
        clipped or repainted. This entry point deliberately handles body poses;
        native attack-mouth replacement remains its separate registered recipe.
        """
        if "group" in pose:
            raise ValueError("This study helper has not been approved for attack assembly")
        if len(canonical_sources) != 6:
            raise ValueError("Expected six canonical whole-limb materials")
        base_limbs, deltas = [], []
        for leg, (source, limb) in enumerate(zip(canonical_sources, self.anatomy["limbs"])):
            base_source, source_delta = self.source_layers(source)
            base = warp_rgba(base_source, limb, pose, leg)
            delta = warp_rgba(source_delta, limb, pose, leg)
            if base.shape != CANVAS_SHAPE or delta.shape != CANVAS_SHAPE or np.any(delta[..., 3]):
                raise ValueError("The canonical warp must preserve the signed RGB-only delta")
            base_limbs.append(base)
            deltas.append(delta)
        base_torso, torso_delta = self.torso_layers(canonical_torso)
        whole = np.zeros(CANVAS_SHAPE, dtype=np.float32)
        for limb in base_limbs:
            whole = _over(limb, whole)
        whole = _over(base_torso, whole)
        before_alpha = whole[..., 3].copy()
        before_rgb = whole[..., :3].copy()
        if canonical_whole is not None and not np.array_equal(before_alpha, np.asarray(canonical_whole)[..., 3]):
            raise ValueError("The study did not use the canonical frame's exact source coverage")
        opaque_whole = before_alpha >= INTERIOR_ALPHA
        transmittance = 1.0 - base_torso[..., 3]
        leg_report = [None] * 6
        for leg in reversed(range(6)):
            own_interior = base_limbs[leg][..., 3] >= INTERIOR_ALPHA
            weight = transmittance * own_interior * opaque_whole
            whole[..., :3] += deltas[leg][..., :3] * weight[..., None]
            changed = np.max(np.abs(deltas[leg][..., :3]), axis=-1) * weight > 1.0 / 65535.0
            leg_report[leg] = {
                "leg": self.anatomy["limbs"][leg]["id"],
                "visiblePaintPixels": int(changed.sum()),
                "paintOnTranslucentOwnEdgePixels": int((changed & ~own_interior).sum()),
            }
            transmittance *= 1.0 - base_limbs[leg][..., 3]
        torso_interior = base_torso[..., 3] >= INTERIOR_ALPHA
        whole[..., :3] += torso_delta[..., :3] * (torso_interior & opaque_whole)[..., None]
        whole[..., :3] = np.clip(whole[..., :3], 0.0, whole[..., 3:4])
        if not np.array_equal(whole[..., 3], before_alpha):
            raise AssertionError("Native material transfer changed the canonical alpha")
        changed_rgb = np.max(np.abs(whole[..., :3] - before_rgb), axis=-1) > 1.0 / 65535.0
        edge_changes = int((changed_rgb & ~opaque_whole).sum())
        if edge_changes:
            raise AssertionError("Native paint reached a translucent external edge")
        report = {
            "row": pose["row"], "phase": pose["phase"],
            "alphaFloatExactToCanonical": canonical_whole is not None,
            "alphaUnchangedDuringPaint": True,
            "paintOnTranslucentWholeEdgePixels": edge_changes,
            "limbs": leg_report,
        }
        return whole, report
