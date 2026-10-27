"""Offline painter for Stalker's six source chains and whole sprite frames.

The 05 anatomy places the projected attachments beneath the body. The painter
preserves each original upper/distal skin patch and the original rounded knee
and root volumes. Only the shaft is shortened along its axis; joint disks do
not turn into compressed ellipses. There is no post-warp capsule clipping.

Public API: SpritePainter(project_path).paint(pose) returns the whole frame,
six separate limb frames, and body frame, all linear-premultiplied float32 RGBA
arrays of shape (576, 576, 4). No files are written on construction or import.
"""
from dataclasses import dataclass
from pathlib import Path
import json
import numpy as np
from PIL import Image, ImageDraw, ImageFilter


SIZE = 576
PPU = 224.0
ORIGIN = np.array([288.0, 288.0], dtype=np.float32)
FLIP = np.diag([1.0, -1.0]).astype(np.float32)
JOINT_RADII = (12.5, 12.0, 10.0, 10.5, 12.0, 11.5)
ROOT_RADII = (13.0, 12.0, 9.0, 10.0, 12.0, 11.5)


def _decode(image):
    rgba = np.asarray(image).astype(np.float32) / 255.0
    rgb = rgba[..., :3]
    linear = np.where(rgb <= 0.04045, rgb / 12.92, ((rgb + 0.055) / 1.055) ** 2.4)
    return np.concatenate([linear * rgba[..., 3:4], rgba[..., 3:4]], axis=-1)


def _over(top, bottom):
    return top + bottom * (1.0 - top[..., 3:4])


def _segment_distance(points, start, end):
    direction = end - start
    phase = np.clip(np.sum((points - start) * direction, axis=-1) / np.dot(direction, direction), 0.0, 1.0)
    return np.linalg.norm(points - (start + phase[..., None] * direction), axis=-1)


def _largest_component(binary):
    visited = np.zeros_like(binary, dtype=bool)
    largest = []
    height, width = binary.shape
    for y, x in zip(*np.nonzero(binary)):
        if visited[y, x]:
            continue
        pending = [(int(y), int(x))]
        visited[y, x] = True
        component = []
        while pending:
            py, px = pending.pop()
            component.append((py, px))
            for ny in range(max(0, py - 1), min(height, py + 2)):
                for nx in range(max(0, px - 1), min(width, px + 2)):
                    if binary[ny, nx] and not visited[ny, nx]:
                        visited[ny, nx] = True
                        pending.append((ny, nx))
        if len(component) > len(largest):
            largest = component
    result = np.zeros_like(binary, dtype=bool)
    if largest:
        yy, xx = zip(*largest)
        result[yy, xx] = True
    return result


def _clean_source_root(full, hip, radius):
    """Remove source-extraction dirt near the mount, never a target limb."""
    yy, xx = np.mgrid[:full.shape[0], :full.shape[1]]
    distance = np.hypot(xx - hip[0], yy - hip[1])
    proximal_zone = distance < radius + 30.0
    binary = Image.fromarray((full[..., 3] > 0.15).astype(np.uint8) * 255)
    opened = np.asarray(binary.filter(ImageFilter.MinFilter(3)).filter(ImageFilter.MaxFilter(3))) > 128
    original = full[..., 3] > 0.15
    candidate = np.where(proximal_zone, opened, original)
    main = _largest_component(candidate)
    # Restore a pixel of antialiasing around the retained tissue. Only the
    # proximal region is filtered, so a fine distal point cannot be removed.
    retained = np.asarray(Image.fromarray(main.astype(np.uint8) * 255).filter(ImageFilter.MaxFilter(3))) / 255.0
    keep = np.where(proximal_zone, retained, 1.0).astype(np.float32)
    cleaned = full * keep[..., None]
    removed = (full[..., 3] > 4.0 / 255.0) & (cleaned[..., 3] <= 4.0 / 255.0)
    ys, xs = np.nonzero(removed)
    trace = {'removedPixelsAlphaOver4': int(removed.sum()),
        'removedOpaquePixels': int(((full[..., 3] > 0.5) & (cleaned[..., 3] <= 0.5)).sum()),
        'sourceRemovedBounds': [int(xs.min()), int(ys.min()), int(xs.max() + 1), int(ys.max() + 1)] if len(xs) else None,
        'proximalRadiusPixels': radius + 30.0,
        'method': 'Local source opening, principal source chain component, one-pixel AA restoration; distal zone unchanged'}
    return cleaned, trace


def _small_enclosed_regions(opaque, maximum_pixels):
    """Inverse eight-connected components; outer transparency is never filled."""
    parents = []
    areas = []
    outer = []
    runs = []
    previous = []
    height, width = opaque.shape

    def find(index):
        while parents[index] != index:
            parents[index] = parents[parents[index]]
            index = parents[index]
        return index

    def union(a, b):
        a, b = find(a), find(b)
        if a == b:
            return
        if areas[a] < areas[b]:
            a, b = b, a
        parents[b] = a
        areas[a] += areas[b]
        outer[a] = outer[a] or outer[b]

    for y, row in enumerate(~opaque):
        edges = np.diff(np.r_[False, row, False].astype(np.int8))
        starts = np.flatnonzero(edges == 1)
        ends = np.flatnonzero(edges == -1)
        current = []
        cursor = 0
        for left, right in zip(starts, ends):
            left, right = int(left), int(right)
            index = len(parents)
            parents.append(index)
            areas.append(right - left)
            outer.append(y == 0 or y == height - 1 or left == 0 or right == width)
            while cursor < len(previous) and previous[cursor][1] < left:
                cursor += 1
            j = cursor
            while j < len(previous) and previous[j][0] <= right:
                union(index, previous[j][2])
                j += 1
            current.append((left, right, index))
            runs.append((y, left, right, index))
        previous = current
    regions = {}
    for y, left, right, index in runs:
        root = find(index)
        if not outer[root] and areas[root] <= maximum_pixels:
            regions.setdefault(root, []).extend((y, x) for x in range(left, right))
    return list(regions.values())


def _fill_skin_holes(paint, maximum_pixels=6):
    """Repair tiny enclosed sampling seams inside one limb, with native color.

    Both the near-transparent and opaque masks are inspected independently.
    Only bounded regions of at most six pixels are eligible. Open V-shaped
    gaps and all external contours remain exactly as painted.
    """
    yy, xx = np.nonzero(paint[..., 3] > 4.0 / 255.0)
    if len(xx) == 0:
        return paint, []
    left, top = max(0, int(xx.min()) - 1), max(0, int(yy.min()) - 1)
    right = min(paint.shape[1], int(xx.max()) + 2)
    bottom = min(paint.shape[0], int(yy.max()) + 2)
    region = paint[top:bottom, left:right]
    trace = []
    # Opaque seams first. Remaining near-transparent holes belong to a softer
    # antialias enclosure: keep their fill at alpha128 so a soft opening cannot
    # become a newly closed opaque loop as a side effect of this repair.
    for threshold in (128.0 / 255.0, 4.0 / 255.0):
        # Match the exported PNG threshold exactly. Comparing the float alpha
        # first can retain e.g. alpha4.4 as tissue although it exports to4.
        quantized_alpha = np.rint(np.clip(region[..., 3], 0.0, 1.0) * 255.0)
        holes = _small_enclosed_regions(quantized_alpha > round(threshold * 255.0), maximum_pixels)
        for hole in holes:
            hole_set = set(hole)
            for y, x in hole:
                neighbors = []
                weights = []
                for ny in range(max(0, y - 1), min(region.shape[0], y + 2)):
                    for nx in range(max(0, x - 1), min(region.shape[1], x + 2)):
                        if (ny, nx) in hole_set or region[ny, nx, 3] <= 0.5:
                            continue
                        neighbors.append(region[ny, nx, :3] / region[ny, nx, 3])
                        weights.append(1.0 / np.hypot(ny - y, nx - x))
                if neighbors:
                    color = np.average(neighbors, axis=0, weights=weights)
                    fill_alpha = 1.0 if threshold > 0.5 else 128.0 / 255.0
                    region[y, x] = np.r_[color * fill_alpha, fill_alpha]
            hy, hx = zip(*hole)
            trace.append({'alphaThreshold': threshold, 'pixels': len(hole),
                'bbox': [min(hx) + left, min(hy) + top, max(hx) + left + 1, max(hy) + top + 1]})
    return paint, trace


@dataclass(frozen=True)
class _PaintPart:
    """Pre-cropped source paint and cached PIL float channels for affine ROIs."""
    box: tuple
    channels: tuple

    @staticmethod
    def create(paint):
        ys, xs = np.nonzero(paint[..., 3] > 1e-5)
        if len(xs) == 0:
            raise ValueError('A source skin part is empty')
        left = max(0, int(xs.min()) - 2)
        top = max(0, int(ys.min()) - 2)
        right = min(paint.shape[1], int(xs.max()) + 3)
        bottom = min(paint.shape[0], int(ys.max()) + 3)
        cropped = paint[top:bottom, left:right]
        channels = tuple(Image.fromarray(cropped[..., channel]) for channel in range(4))
        return _PaintPart((left, top, right, bottom), channels)


class SpritePainter:
    def __init__(self, project_path):
        self.project_path = Path(project_path).resolve()
        source_folder = self.project_path / 'Concepts/Used/Stalker/Locomotion02'
        self.anatomy = json.loads((source_folder / 'anatomy.json').read_text())
        atlas_path = self.project_path / 'Assets/EmergencyShift/Art/Enemies/Stalker/Locomotion02/rig-atlas.png'
        with Image.open(atlas_path) as atlas_image:
            atlas = atlas_image.convert('RGBA')

        def source_layer(index):
            x, y = index % 4 * 358, index // 4 * 406
            return _decode(atlas.crop((x, y, x + 358, y + 406)))

        yy, xx = np.mgrid[:406, :358]
        points = np.stack([xx, yy], axis=-1).astype(np.float32)
        self._parts = []
        self.source_cleanup = []
        for leg, limb in enumerate(self.anatomy['limbs']):
            full = source_layer(leg * 2 + 1)
            hip, knee, tip = [np.asarray(limb[key], dtype=np.float32) for key in ('hip', 'knee', 'tip')]
            upper_distance = _segment_distance(points, hip, knee)
            lower_distance = _segment_distance(points, knee, tip)
            joint_distance = np.linalg.norm(points - knee, axis=-1)
            root_distance = np.linalg.norm(points - hip, axis=-1)
            upper_axis = (knee - hip) / np.linalg.norm(knee - hip)
            along = (points - hip) @ upper_axis

            full, trace = _clean_source_root(full, hip, ROOT_RADII[leg])
            self.source_cleanup.append({'leg': leg, 'id': limb['id'], **trace})

            upper_keep = np.clip((lower_distance - upper_distance + 2.0) / 4.0, 0.0, 1.0)
            upper_keep *= np.clip((along + 9.0) / 3.0, 0.0, 1.0)
            lower_keep = np.clip((upper_distance - lower_distance + 2.0) / 4.0, 0.0, 1.0)
            # The source disk is removed from both compressed copies and then
            # restored once at its original radius and screen-light orientation.
            joint_radius = JOINT_RADII[leg]
            without_joint = np.clip((joint_distance - (joint_radius - 3.0)) / 3.0, 0.0, 1.0)
            root_radius = ROOT_RADII[leg]
            without_root = np.clip((root_distance - (root_radius - 3.0)) / 3.0, 0.0, 1.0)
            upper = full * (upper_keep * without_joint * without_root)[..., None]
            lower = full * (lower_keep * without_joint)[..., None]
            joint = full * np.clip(joint_radius + 1.0 - joint_distance, 0.0, 1.0)[..., None]
            root = full * np.clip(root_radius + 1.0 - root_distance, 0.0, 1.0)[..., None]
            self._parts.append(tuple(_PaintPart.create(paint) for paint in (upper, lower, joint, root)))

        torso = source_layer(0)
        core_mask = Image.new('L', (358, 406))
        ImageDraw.Draw(core_mask).polygon(((187, 124), (120, 247), (250, 247)), fill=255)
        core_mask = core_mask.filter(ImageFilter.MaxFilter(9)).filter(ImageFilter.GaussianBlur(0.65))
        torso = torso * (np.asarray(core_mask).astype(np.float32) / 255.0)[..., None]
        self._torso = np.zeros((SIZE, SIZE, 4), dtype=np.float32)
        x, y = (ORIGIN - np.asarray(self.anatomy['pivot'])).astype(np.int32)
        self._torso[y:y + 406, x:x + 358] = torso
        self._torso.flags.writeable = False

    @staticmethod
    def _transform(part, source_start, source_end, target_start, target_end):
        source_start = np.asarray(source_start, dtype=np.float32)
        source_end = np.asarray(source_end, dtype=np.float32)
        target_start = np.asarray(target_start[:2], dtype=np.float32)
        target_end = np.asarray(target_end[:2], dtype=np.float32)
        source_axis = FLIP @ (source_end - source_start) / PPU
        source_length = np.linalg.norm(source_axis)
        source_axis /= source_length
        target_axis = target_end - target_start
        target_length = np.linalg.norm(target_axis)
        if target_length < 1e-6:
            raise ValueError('A skin segment has zero projected length')
        target_axis /= target_length
        source_normal = np.array([-source_axis[1], source_axis[0]], dtype=np.float32)
        target_normal = np.array([-target_axis[1], target_axis[0]], dtype=np.float32)
        matrix = np.outer(target_axis, source_axis) * target_length / source_length
        matrix += np.outer(target_normal, source_normal)
        pixel_matrix = FLIP @ matrix @ FLIP
        translation = ORIGIN + FLIP @ target_start * PPU - pixel_matrix @ source_start
        inverse = np.linalg.inv(pixel_matrix)

        # Transform just the source-alpha rectangle. The inverse map is shifted
        # by target ROI origin, then by the cached source crop origin.
        left, top, right, bottom = part.box
        source_corners = np.array(((left, top), (right, top), (left, bottom), (right, bottom)), dtype=np.float32)
        target_corners = source_corners @ pixel_matrix.T + translation
        target_left = max(0, int(np.floor(target_corners[:, 0].min())) - 2)
        target_top = max(0, int(np.floor(target_corners[:, 1].min())) - 2)
        target_right = min(SIZE, int(np.ceil(target_corners[:, 0].max())) + 2)
        target_bottom = min(SIZE, int(np.ceil(target_corners[:, 1].max())) + 2)
        result = np.zeros((SIZE, SIZE, 4), dtype=np.float32)
        if target_left >= target_right or target_top >= target_bottom:
            return result
        target_origin = np.array([target_left, target_top], dtype=np.float32)
        offset = inverse @ (target_origin - translation) - np.array([left, top], dtype=np.float32)
        coefficients = (inverse[0, 0], inverse[0, 1], offset[0], inverse[1, 0], inverse[1, 1], offset[1])
        target_size = (target_right - target_left, target_bottom - target_top)
        roi = np.stack([np.asarray(channel.transform(target_size, Image.Transform.AFFINE, coefficients,
            Image.Resampling.BICUBIC)) for channel in part.channels], axis=-1)
        roi = np.clip(roi, 0.0, 1.0)
        roi[..., :3] = np.minimum(roi[..., :3], roi[..., 3:4])
        result[target_top:target_bottom, target_left:target_right] = roi
        return result

    def paint(self, pose):
        limbs = []
        canvas = np.zeros((SIZE, SIZE, 4), dtype=np.float32)
        for leg, limb in enumerate(self.anatomy['limbs']):
            upper_source, lower_source, joint_source, root_source = self._parts[leg]
            hip = np.asarray(pose['hips'][leg], dtype=np.float32)
            knee = np.asarray(pose['knees'][leg], dtype=np.float32)
            tip = np.asarray(pose['feet'][leg], dtype=np.float32)
            upper = self._transform(upper_source, limb['hip'], limb['knee'], hip, knee)
            lower = self._transform(lower_source, limb['knee'], limb['tip'], knee, tip)
            source_knee = np.asarray(limb['knee'], dtype=np.float32)
            joint = self._transform(joint_source, source_knee, source_knee + [1.0, 0.0], knee, knee + [1.0 / PPU, 0.0, 0.0])
            source_hip = np.asarray(limb['hip'], dtype=np.float32)
            root = self._transform(root_source, source_hip, source_hip + [1.0, 0.0], hip, hip + [1.0 / PPU, 0.0, 0.0])
            paint = _over(root, _over(joint, _over(lower, upper)))
            paint, _ = _fill_skin_holes(paint)
            limbs.append(paint)
            canvas = _over(paint, canvas)
        whole = _over(self._torso, canvas)
        return whole, limbs, self._torso
