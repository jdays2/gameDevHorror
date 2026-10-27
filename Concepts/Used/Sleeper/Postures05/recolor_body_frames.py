"""Derive the current Sleeper palette, then recolor the reviewed Stalker body atlas.

Run from the project with Pillow and numpy. This does not import Unity assets or
modify the accepted source PNGs. Sprite geometry and every alpha byte are kept.
"""
from copy import deepcopy
from hashlib import sha256
from pathlib import Path
import argparse
import importlib.util
import json
import re
import sys

import numpy as np
from PIL import Image, ImageDraw


PROJECT = Path(__file__).resolve().parents[4]
LIBRARY = Path(__file__).resolve().parent
STALKER = PROJECT / "Assets/EmergencyShift/Art/Enemies/Stalker/PaleFlesh/walk-sheet.png"
SLEEPER = PROJECT / "Assets/EmergencyShift/Art/Enemies/Sleeper/StalkerPlaceholder01/PaleFlesh/walk-sheet.png"
PRODUCTION = PROJECT / "Assets/EmergencyShift/Art/Enemies/Sleeper/Postures05"


def file_hash(path):
    return sha256(path.read_bytes()).hexdigest()


def load_paint_helpers():
    tools = PROJECT / "Tools/Art"
    sys.path.insert(0, str(tools))
    spec = importlib.util.spec_from_file_location("sleeper_blood", tools / "apply_sleeper_blood_02.py")
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def palette_from_current_pngs():
    source = np.asarray(Image.open(STALKER).convert("RGBA"))
    target = np.asarray(Image.open(SLEEPER).convert("RGBA"))
    if source.shape != target.shape or not np.array_equal(source[:, :, 3], target[:, :, 3]):
        raise ValueError("Registered current palette sources must have identical silhouettes")
    x = source[:, :, :3].reshape(-1, 3).astype(float)
    y = target[:, :, :3].reshape(-1, 3).astype(float)
    mask = (source[:, :, 3].ravel() > 240) & (target[:, :, 3].ravel() > 240)
    mask &= (x.min(1) > 30) & (x.max(1) < 235)
    mask &= (x[:, 0] < 1.5 * x[:, 1]) & (y[:, 0] < 1.4 * y[:, 1])
    x = x[mask]
    y = y[mask]
    design = np.column_stack((x, np.ones(len(x))))
    inliers = np.ones(len(x), bool)
    for unused in range(10):
        matrix = np.linalg.lstsq(design[inliers], y[inliers], rcond=None)[0]
        error = np.max(np.abs(design @ matrix - y), axis=1)
        inliers = error < max(1.2, float(np.percentile(error, 60)))
    if inliers.sum() < 10000 or np.percentile(error[inliers], 99) > 1:
        raise ValueError("Current PNGs do not yield a reliable registered skin palette")
    report = {
        "method": "robust affine least squares on matching current skin pixels; saturated blood, eye edits and decals rejected as outliers",
        "source": str(STALKER.relative_to(PROJECT)).replace("\\", "/"),
        "target": str(SLEEPER.relative_to(PROJECT)).replace("\\", "/"),
        "sourceSha256": file_hash(STALKER),
        "targetSha256": file_hash(SLEEPER),
        "matrixRgbOneToRgb": matrix.tolist(),
        "candidatePixels": len(x),
        "inlierPixels": int(inliers.sum()),
        "inlierMaxChannelErrorP99": float(np.percentile(error[inliers], 99)),
        "spriteRendererTint": "white",
    }
    return matrix, report


def transform_palette(image, matrix):
    pixels = np.asarray(image).copy()
    matrix = matrix.astype(np.float32)
    for top in range(0, image.height, 256):
        tile = pixels[top:top + 256]
        rgb = tile[:, :, :3].astype(np.float32)
        blood = (rgb[:, :, 0] > rgb[:, :, 1] * 1.6) & (rgb[:, :, 0] > rgb[:, :, 2] * 1.3) & (rgb[:, :, 0] > 65)
        transformed = rgb @ matrix[:3] + matrix[3]
        tile[:, :, :3] = np.rint(transformed).clip(0, 255).astype(np.uint8)
        tile[:, :, :3][blood] = rgb[blood].astype(np.uint8)
    return Image.fromarray(pixels)


def source_claw_attachments(helpers):
    source = Image.open(STALKER).convert("RGBA")
    sprite_path = PROJECT / "Assets/EmergencyShift/Art/Enemies/Sleeper/StalkerPlaceholder01/Frames/Walk00-corrected.asset"
    rectangle, center, ppu = helpers.sprite_info(sprite_path, source.height)
    centers = helpers.blood_centres(source, rectangle, center, .4 * ppu)
    profile_path = PROJECT / "Assets/EmergencyShift/Data/Enemies/StalkerLimbRig.asset"
    text = profile_path.read_text(encoding="utf-8-sig")
    limbs = []
    for match in re.finditer(r"- Id: (\w+)\s+Hip:.*?Knee: \{x: ([\d.eE+-]+), y: ([\d.eE+-]+)\}\s+Tip: \{x: ([\d.eE+-]+), y: ([\d.eE+-]+)\}", text, re.S):
        name, kx, ky, fx, fy = match.groups()
        limbs.append((name, np.array([float(kx), float(ky)]), np.array([float(fx), float(fy)])))
    if len(limbs) != 6 or len(centers) != 3:
        raise ValueError("Expected six registered limbs and the same three native blood marks")
    attachments = []
    for index, point in enumerate(centers):
        position = np.array([(point[0] - center[0]) / ppu, (center[1] - point[1]) / ppu])
        leg = min(range(6), key=lambda candidate: np.linalg.norm(position - limbs[candidate][2]))
        name, knee, foot = limbs[leg]
        projected_length = np.linalg.norm(foot - knee)
        axis = (foot - knee) / projected_length
        normal = np.array([-axis[1], axis[0]])
        offset = position - foot
        attachments.append({
            "leg": leg,
            "id": name,
            "decal": index,
            "along": float(offset @ axis),
            "alongFraction": float(offset @ axis / projected_length),
            "across": float(offset @ normal),
            "sourceProjectedLength": float(projected_length),
            "sourceAxisAngle": float(np.arctan2(axis[1], axis[0])),
            "sourceBloodCenter": position.tolist(),
        })
    return attachments


def pose_name(pose):
    if "name" in pose:
        return pose["name"]
    return f'body-{pose.get("row", 0)}-{pose["phase"]:02d}'


def visible_carriers(source_family, pose, image_size, pivot, allow_missing, attack=False):
    mask_folder = source_family / "Masks"
    torso_path = mask_folder / f"torso-{pose_name(pose)}.png" if attack else mask_folder / "torso.png"
    if not torso_path.exists():
        if allow_missing:
            return None, [None] * 6
        raise ValueError("Final body visibility masks are required before Sleeper paint transfer")
    torso_image = Image.open(torso_path).getchannel("A")
    torso = np.asarray(torso_image).astype(np.float32) / 255
    body_paint_carrier = torso.copy()
    if attack:
        mouth_path = mask_folder / f"mouth-{pose_name(pose)}.png"
        mouth = np.asarray(Image.open(mouth_path).getchannel("A")).astype(np.float32) / 255
        body_paint_carrier = np.clip(torso - mouth, 0, 1)
    legs = []
    for leg in range(6):
        if attack:
            path = mask_folder / f'leg-{pose_name(pose)}-{leg}.png'
        else:
            path = mask_folder / f'leg-{pose["row"]}-{pose["phase"]:02d}-{leg}.png'
        legs.append(np.asarray(Image.open(path).getchannel("A")).astype(np.float32) / 255)
    uncovered = 1 - torso
    visible = [None] * 6
    for leg in reversed(range(6)):
        visible[leg] = legs[leg] * uncovered
        uncovered *= 1 - legs[leg]
    left = round(torso_image.width / 2 - pivot[0])
    top = round(torso_image.height / 2 - pivot[1])
    box = (left, top, left + image_size[0], top + image_size[1])
    crop = lambda mask: np.asarray(Image.fromarray(mask).crop(box))
    return crop(body_paint_carrier), [crop(mask) for mask in visible]


def paint_on_carrier(image, decal, center, size, angle, carrier, helpers):
    painted = helpers.paint(image, decal, center, size, angle)
    if carrier is None:
        return painted
    before = np.asarray(image).copy()
    after = np.asarray(painted)
    weight = np.clip(carrier / np.maximum(before[:, :, 3].astype(np.float32) / 255, 1e-6), 0, 1)
    rgb = before[:, :, :3].astype(np.float32)
    rgb += (after[:, :, :3].astype(np.float32) - rgb) * weight[:, :, None]
    before[:, :, :3] = np.rint(rgb).clip(0, 255).astype(np.uint8)
    return Image.fromarray(before)


def deform_limb_decal(decal, attachment, target_axis, target_length, ppu):
    angle = attachment["sourceAxisAngle"]
    source_axis = np.array([np.cos(angle), np.sin(angle)])
    source_normal = np.array([-source_axis[1], source_axis[0]])
    target_normal = np.array([-target_axis[1], target_axis[0]])
    scale_along = target_length / attachment["sourceProjectedLength"]
    world_matrix = np.outer(target_axis, source_axis) * scale_along + np.outer(target_normal, source_normal)
    flip = np.diag([1., -1.])
    pixel_matrix = flip @ world_matrix @ flip
    mark = decal.resize((round(.10 * ppu), round(.055 * ppu)), Image.Resampling.LANCZOS)
    mark = mark.rotate(30 - attachment["decal"] * 35, Image.Resampling.BICUBIC, expand=True)
    source_size = np.array(mark.size)
    target_size = np.maximum(2, np.ceil(np.abs(pixel_matrix) @ source_size).astype(int) + 2)
    inverse = np.linalg.inv(pixel_matrix)
    offset = source_size / 2 - inverse @ (target_size / 2)
    coefficients = (inverse[0, 0], inverse[0, 1], offset[0], inverse[1, 0], inverse[1, 1], offset[1])
    return mark.transform(tuple(target_size), Image.Transform.AFFINE, coefficients, Image.Resampling.BICUBIC)


def paint_registered_marks(image, pivot, ppu, pose, attachments, helpers, decals, torso_carrier, leg_carriers):
    # The established BloodMarks02 eye whitening and body marks use the same
    # fixed torso/eye anchor. The runtime's original eye sprites remain intact.
    image = helpers.pale_eye(image, pivot, (.10 * ppu, .105 * ppu))
    for number, (dx, dy, width, height, angle) in enumerate(((-.21, -.065, .065, .035, -35), (.21, .045, .045, .06, 20), (-.13, .16, .055, .035, 5))):
        center = (pivot[0] + dx * ppu, pivot[1] + dy * ppu)
        image = paint_on_carrier(image, decals[number], center, (width * ppu, height * ppu), angle, torso_carrier, helpers)
    for attachment in attachments:
        leg = attachment["leg"]
        knee = np.array(pose["knees"][leg][:2])
        foot = np.array(pose["feet"][leg][:2])
        projected_length = np.linalg.norm(foot - knee)
        if projected_length < 1e-6:
            raise ValueError("Cannot transfer a mark to a vanished projected distal segment")
        axis = (foot - knee) / projected_length
        normal = np.array([-axis[1], axis[0]])
        position = foot + axis * attachment["alongFraction"] * projected_length + normal * attachment["across"]
        center = (pivot[0] + position[0] * ppu, pivot[1] - position[1] * ppu)
        mark = deform_limb_decal(decals[attachment["decal"]], attachment, axis, projected_length, ppu)
        image = paint_on_carrier(image, mark, center, mark.size, 0, leg_carriers[leg], helpers)
    return image


def save_contacts(frames, manifest, report_folder, attack=False):
    review = report_folder / ("ReviewAttack" if attack else "Review")
    review.mkdir(exist_ok=True)
    canvas_size = 576
    for frame in manifest["frames"]:
        width, height = frame["rect"][2:]
        px, py = frame["pivot"]
        if max(width * px, width * (1 - px), height * py, height * (1 - py)) > canvas_size / 2:
            raise ValueError("Frame exceeds the registered 576 pixel canvas")
    pages = []
    if attack:
        for group in ("entry", "strike", "return"):
            selected = [frame for frame in manifest["frames"] if frame["group"] == group]
            for start in range(0, len(selected), 48):
                pages.append((f"{group}-{start // 48:02d}", selected[start:start + 48]))
    else:
        for row in range(manifest["closureRows"]):
            selected = [frame for frame in manifest["frames"] if frame["closure"] == row]
            pages.append((f"closure-{row}", selected))
    for page_name, selected in pages:
        sheet = Image.new("RGB", (1600, 1260), (0, 255, 40))
        draw = ImageDraw.Draw(sheet)
        for index, frame in enumerate(selected):
            crop = frames[frame["name"]]
            image = Image.new("RGBA", (canvas_size, canvas_size))
            pivot = (frame["pivot"][0] * crop.width, (1 - frame["pivot"][1]) * crop.height)
            image.paste(crop, (round(canvas_size / 2 - pivot[0]), round(canvas_size / 2 - pivot[1])))
            image.thumbnail((195, 185), Image.Resampling.LANCZOS)
            x = index % 8 * 200
            y = index // 8 * 210
            sheet.paste(image, (x + (200 - image.width) // 2, y + 18), image)
            draw.text((x + 8, y + 3), frame["name"], fill=(0, 0, 0))
        sheet.save(review / f"{page_name}.jpg", quality=94)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--source-family", default="Concepts/Used/Stalker/Postures05")
    parser.add_argument("--palette-only", action="store_true")
    parser.add_argument("--attack", action="store_true", help="Process the named attack matrix and independent attack visibility masks")
    parser.add_argument("--study", action="store_true", help="Write a temporary palette study outside Assets; do not prepare a production manifest")
    arguments = parser.parse_args()
    matrix, palette_report = palette_from_current_pngs()
    helpers = load_paint_helpers()
    attachments = source_claw_attachments(helpers)
    palette_report["nativeBloodAttachments"] = attachments
    (LIBRARY / "palette-map.json").write_text(json.dumps(palette_report, indent=2), encoding="utf-8")
    if arguments.palette_only:
        print(json.dumps({"paletteInliers": palette_report["inlierPixels"], "p99": palette_report["inlierMaxChannelErrorP99"], "clawMarks": len(attachments)}))
        return
    source_family = PROJECT / arguments.source_family
    report_folder = LIBRARY
    production = PRODUCTION
    if arguments.study:
        report_folder = LIBRARY / "Study" / source_family.name
        production = report_folder / "Atlases"
        report_folder.mkdir(parents=True, exist_ok=True)
    manifest_name = "attack-sprites.json" if arguments.attack else "sprites.json"
    poses_name = "attack-poses.json" if arguments.attack else "poses.json"
    manifest = json.loads((source_family / manifest_name).read_text(encoding="utf-8-sig"))
    poses = json.loads((source_family / poses_name).read_text(encoding="utf-8-sig"))
    poses = {pose_name(pose): pose for pose in poses}
    ppu = manifest.get("pixelsPerUnit", manifest.get("ppu"))
    if manifest["phases"] != 48 or ppu != 224:
        raise ValueError("Use the reviewed 48 phase, 224 PPU matrix contract")
    if arguments.attack:
        expected_frames = manifest["phases"] * manifest["bridgeFrames"] * 2 + manifest["strikeFrames"]
    else:
        if manifest["closureRows"] != 9:
            raise ValueError("Use the reviewed nine body-fold rows")
        expected_frames = manifest["phases"] * manifest["closureRows"]
    if len(manifest["frames"]) != expected_frames or len(poses) != expected_frames:
        raise ValueError("Pose and packed frame counts differ")
    output = deepcopy(manifest)
    production.mkdir(parents=True, exist_ok=True)
    decals = helpers.patches()
    reviewed_frames = {}
    report = []
    carrier_masks_used = True
    native_mouth = None
    if arguments.attack:
        from native_mouth_palette import NativeMouthReference
        native_mouth = NativeMouthReference()
        (report_folder / "mouth-palette-recipe.json").write_text(json.dumps(native_mouth.recipe, indent=2), encoding="utf-8")
    for atlas_index, atlas in enumerate(manifest["atlases"]):
        source_path = PROJECT / atlas["asset"]
        if file_hash(source_path) != atlas["sha256"]:
            raise ValueError("Source atlas does not match the reviewed manifest: " + str(source_path))
        original = Image.open(source_path).convert("RGBA")
        base_recolored = transform_palette(original, matrix)
        recolored = base_recolored.copy()
        shared_placements = {}
        for frame in (item for item in manifest["frames"] if item["atlas"] == atlas_index):
            x, bottom, width, height = frame["rect"]
            top = original.height - bottom - height
            box = (x, top, x + width, top + height)
            crop = base_recolored.crop(box)
            pivot = (frame["pivot"][0] * width, (1 - frame["pivot"][1]) * height)
            key = frame["name"]
            if native_mouth is not None:
                crop = native_mouth.replace_palette(original.crop(box), crop, poses[key], pivot, matrix, transform_palette, source_family)
            torso_carrier, leg_carriers = visible_carriers(source_family, poses[key], crop.size, pivot, arguments.study, arguments.attack)
            carrier_masks_used &= torso_carrier is not None
            crop = paint_registered_marks(crop, pivot, 224, poses[key], attachments, helpers, decals, torso_carrier, leg_carriers)
            digest = sha256(crop.tobytes()).hexdigest()
            if box in shared_placements and shared_placements[box][1] != digest:
                previous_name = shared_placements[box][0]
                raise ValueError(f"Shared placement needs distinct painted variants: {previous_name}, {key}")
            shared_placements[box] = (key, digest)
            recolored.paste(crop, box)
            reviewed_frames[key] = crop
        if not np.array_equal(np.asarray(original)[:, :, 3], np.asarray(recolored)[:, :, 3]):
            raise ValueError("Recoloring changed reviewed alpha geometry")
        kind = "attack" if arguments.attack else "body"
        target_path = production / f"{kind}-{atlas_index:02}.png"
        recolored.save(target_path)
        output["atlases"][atlas_index] = {"asset": target_path.relative_to(PROJECT).as_posix(), "sha256": file_hash(target_path)}
        report.append({"source": atlas["asset"], "sourceSha256": file_hash(source_path), "target": target_path.relative_to(PROJECT).as_posix(), "sha256": file_hash(target_path), "alphaIdentical": True})
    output["paletteMap"] = "palette-map.json"
    output["sourceMatrix"] = str((source_family / manifest_name).relative_to(PROJECT)).replace("\\", "/")
    output["studyOnly"] = arguments.study
    (report_folder / manifest_name).write_text(json.dumps(output, indent=2), encoding="utf-8")
    report_name = "attack-recolor-report.json" if arguments.attack else "recolor-report.json"
    (report_folder / report_name).write_text(json.dumps({"frames": len(reviewed_frames), "atlases": report, "bloodSource": "Concepts/Used/Sleeper/BloodMarks02/Source/blood-decals.png", "bloodClippedToOwnVisibleCarrier": carrier_masks_used, "bodyBloodExcludedFromMouth": arguments.attack, "mouthPalette": "registered native Sleeper Attack03" if arguments.attack else None, "nativeEyeAndDeathSourcesUnchanged": True, "sourceAlphaRepresentation": "straight RGBA; no additional unpremultiply", "studyOnly": arguments.study}, indent=2), encoding="utf-8")
    save_contacts(reviewed_frames, manifest, report_folder, arguments.attack)
    print(json.dumps({"frames": len(reviewed_frames), "atlases": len(report), "alphaIdentical": True}))


if __name__ == "__main__":
    main()
