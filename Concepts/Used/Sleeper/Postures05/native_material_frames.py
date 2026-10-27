"""Sleeper native material on the reviewed Stalker05 single-UV geometry.

The source alpha, body ownership, UV field and composition order are immutable.
Only the registered source RGB changes. This does not use affine palette fitting,
additional blood decals, a second coverage layer or any target contour repair.
"""
from hashlib import sha256
from pathlib import Path
import argparse
import json
import sys

import numpy as np
from PIL import Image, ImageDraw


FAMILY = Path(__file__).resolve().parent
PROJECT = FAMILY.parents[3]
CANONICAL = PROJECT / 'Concepts/Used/Stalker/Postures05'
EXPECTED_PAINTER_SHA256 = '2777a84fff7194b1be4aa75642f47801180703f60b6b6b1e5790dd53dc24dd7e'
SOURCE_CROP = (30, 84, 388, 490)
SOURCE_OFFSET = (101, 88)
sys.path.insert(0, str(CANONICAL))

from frame_colors import encode
from native_frame_painter import SpritePainter
from native_mouth_palette import NativeMouthReference


def digest(path):
    return sha256(Path(path).read_bytes()).hexdigest()


def linear(rgb):
    rgb = np.asarray(rgb, dtype=np.float32) / 255.0
    return np.where(rgb <= .04045, rgb / 12.92, ((rgb + .055) / 1.055) ** 2.4)


class NativeMaterialPainter:
    """Owns independent, read-only material arrays and a native mouth painter."""

    def __init__(self, project=PROJECT):
        project = Path(project).resolve()
        painter_path = CANONICAL / 'native_frame_painter.py'
        if digest(painter_path) != EXPECTED_PAINTER_SHA256:
            raise ValueError('The reviewed canonical painter changed; repeat the geometry gate first')
        self.canonical = SpritePainter(project)
        self.painter = SpritePainter(project)
        source_path = project / 'Assets/EmergencyShift/Art/Enemies/Stalker/PaleFlesh/walk-sheet.png'
        target_path = project / 'Assets/EmergencyShift/Art/Enemies/Sleeper/StalkerPlaceholder01/PaleFlesh/walk-sheet.png'
        with Image.open(source_path) as image:
            native_source = np.asarray(image.convert('RGBA').crop(SOURCE_CROP)).copy()
        with Image.open(target_path) as image:
            native_target = np.asarray(image.convert('RGBA').crop(SOURCE_CROP)).copy()
        if not np.array_equal(native_source[..., 3], native_target[..., 3]):
            raise ValueError('The registered native source atlases have different alpha')
        native_linear = linear(native_target[..., :3])
        source_checks = []

        def material(source, name):
            if source.shape != native_source.shape:
                raise ValueError('Material registration changed: ' + name)
            source_bytes = np.asarray(encode(source))
            covered = source[..., 3] > 0
            difference = np.abs(source_bytes[..., :3].astype(np.int16) - native_source[..., :3].astype(np.int16))
            if np.any(difference[covered] > 1):
                raise ValueError('Canonical material is not the native source RGB: ' + name)
            target = source.copy()
            target[..., :3] = native_linear * source[..., 3:4]
            target.flags.writeable = False
            target_bytes = np.asarray(encode(target))
            if not np.array_equal(target[..., 3], source[..., 3]):
                raise AssertionError('Material replacement changed alpha: ' + name)
            if not np.array_equal(target_bytes[..., :3][covered], native_target[..., :3][covered]):
                raise AssertionError('Material replacement changed native RGB: ' + name)
            source_checks.append({
                'material': name,
                'coveredPixels': int(covered.sum()),
                'canonicalNativeMaxRgbByteError': int(difference[covered].max()) if covered.any() else 0,
                'targetNativeVisibleRgbByteExact': True,
                'sourceAlphaFloatExact': True,
            })
            return target

        self.painter.sources = tuple(material(source, limb['id']) for source, limb in
                                     zip(self.canonical.sources, self.canonical.anatomy['limbs']))
        x, y = SOURCE_OFFSET
        height, width = native_source.shape[:2]
        torso = self.canonical._torso.copy()
        outside = torso[..., 3].copy()
        outside[y:y + height, x:x + width] = 0
        if np.any(outside):
            raise ValueError('Canonical torso is outside the native material registration')
        torso[y:y + height, x:x + width] = material(torso[y:y + height, x:x + width], 'torso')
        torso.flags.writeable = False
        self.painter._torso = torso
        mouth_reference = NativeMouthReference()
        self.mouth = mouth_reference.painter
        self.mouth.source_patch = mouth_reference.sleeper_patch.copy()
        self.mouth.source_patch.flags.writeable = False
        self.canonical_mouth = mouth_reference.module.AttackMouth(project, self.canonical._torso)
        self.recipe = {
            'algorithm': 'whole-native-rgb-material-single-uv',
            'source': source_path.relative_to(project).as_posix(),
            'sourceSha256': digest(source_path),
            'target': target_path.relative_to(project).as_posix(),
            'targetSha256': digest(target_path),
            'painterSha256': digest(painter_path),
            'colorsSha256': digest(CANONICAL / 'frame_colors.py'),
            'posesSha256': digest(CANONICAL / 'poses.json'),
            'attackPosesSha256': digest(CANONICAL / 'attack-poses.json'),
            'bonesSha256': digest(CANONICAL / 'bones.json'),
            'processorSha256': digest(__file__),
            'sourceCrop': list(SOURCE_CROP),
            'sourceOffsetInCanvas': list(SOURCE_OFFSET),
            'sourceAlphaByteExact': True,
            'sourceChecks': source_checks,
            'mouth': mouth_reference.recipe,
            'coveragePolicy': 'Exact canonical source alpha, ownership and single UV sample; no added decal alpha, clipping, fill or erosion',
            'materialPolicy': 'Actual native Sleeper skin, bone, blood and cavity RGB before the canonical warp',
            'runtimeIk': False,
        }

    def paint(self, pose):
        whole, limbs, torso = self.painter.paint(pose)
        if 'group' in pose:
            whole, torso, mouth = self.mouth.apply(pose, whole, limbs, torso)
        else:
            mouth = None
        return whole, limbs, torso, mouth

    def canonical_paint(self, pose):
        whole, limbs, torso = self.canonical.paint(pose)
        if 'group' in pose:
            whole, torso, mouth = self.canonical_mouth.apply(pose, whole, limbs, torso)
        else:
            mouth = None
        return whole, limbs, torso, mouth


def composite_background(paint, background):
    rgb = paint[..., :3] + linear(np.asarray(background))[None, None, :] * (1 - paint[..., 3:4])
    rgb = np.where(rgb <= .0031308, rgb * 12.92, 1.055 * np.maximum(rgb, 0) ** (1 / 2.4) - .055)
    return Image.fromarray(np.rint(np.clip(rgb, 0, 1) * 255).astype(np.uint8))


def study():
    output = FAMILY / 'NativeMaterialStudy01'
    output.mkdir(exist_ok=True)
    body = json.loads((CANONICAL / 'poses.json').read_text(encoding='utf-8-sig'))
    attacks = json.loads((CANONICAL / 'attack-poses.json').read_text(encoding='utf-8-sig'))
    poses = [pose for pose in body if pose['row'] in (0, 4, 8) and pose['phase'] in (0, 12, 24, 36)]
    poses += [pose for pose in attacks if pose['group'] == 'strike' and pose['sample'] in (0, 9, 18, 23)]
    painter = NativeMaterialPainter()
    backgrounds = {'green': (0, 255, 0), 'dark': (18, 22, 26), 'light': (235, 235, 235)}
    sheets = {name: Image.new('RGB', (4 * 576, 4 * 608), color) for name, color in backgrounds.items()}
    rows = []
    for index, pose in enumerate(poses):
        name = pose.get('name', f'body-{pose.get("row", 0)}-{pose["phase"]:02d}')
        cold, limbs, torso, mouth = painter.paint(pose)
        canonical, canonical_limbs, canonical_torso, canonical_mouth = painter.canonical_paint(pose)
        if not np.array_equal(cold[..., 3], canonical[..., 3]):
            raise AssertionError('Whole float alpha differs: ' + name)
        if any(not np.array_equal(a[..., 3], b[..., 3]) for a, b in zip(limbs, canonical_limbs)):
            raise AssertionError('A limb float alpha differs: ' + name)
        if not np.array_equal(torso[..., 3], canonical_torso[..., 3]):
            raise AssertionError('Torso float alpha differs: ' + name)
        if mouth is not None and not np.array_equal(mouth[..., 3], canonical_mouth[..., 3]):
            raise AssertionError('Mouth float alpha differs: ' + name)
        image = encode(cold)
        rgba = np.asarray(image)
        raw_path = CANONICAL / 'Raw' / (name + '.png')
        with Image.open(raw_path) as raw:
            canonical_raw = np.asarray(raw.convert('RGBA'))
        if not np.array_equal(rgba[..., 3], canonical_raw[..., 3]):
            raise AssertionError('Saved Stalker Raw alpha differs: ' + name)
        canonical_bytes = np.asarray(encode(canonical))
        covered = canonical_raw[..., 3] > 0
        if not np.array_equal(canonical_bytes[covered], canonical_raw[covered]):
            raise AssertionError('Canonical rebuild is stale: ' + name)
        image.save(output / (name + '.png'))
        for background, color in backgrounds.items():
            x, y = index % 4 * 576, index // 4 * 608
            sheet = sheets[background]
            sheet.paste(composite_background(cold, color), (x, y + 32))
            ink = 'white' if background == 'dark' else 'black'
            ImageDraw.Draw(sheet).text((x + 10, y + 10), name + ' | native 224 PPU', fill=ink)
        rows.append({'name': name, 'wholeAlphaFloatExact': True, 'sixLimbAlphaFloatExact': True,
                     'torsoAlphaFloatExact': True, 'mouthAlphaFloatExact': mouth is not None,
                     'wholeAlphaByteExactToStalkerRaw': True, 'canonicalVisibleRgbaByteExactToRaw': True,
                     'pngSha256': digest(output / (name + '.png')), 'canonicalRawSha256': digest(raw_path)})
        print(name, flush=True)
    for name, sheet in sheets.items():
        sheet.save(output / ('native-' + name + '.png'))
    report = {'scope': 'Bounded material study only; no production bake or Unity import',
              'poses': len(rows), 'nativePpu': 224, 'previewScale': 1,
              'recipe': painter.recipe, 'frames': rows,
              'artInspection': 'Pending independent visual inspection of all three backgrounds'}
    (output / 'study.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
    print(json.dumps({'poses': len(rows), 'allAlphaFloatAndBytesExact': True, 'output': str(output)}), flush=True)


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--study', action='store_true', required=True)
    parser.parse_args()
    study()
