"""Native attack head overlay in the same linear-premultiplied 576px world canvas.

The approved Attack03 source provides the lips, teeth and cavity. A filtered
geometric opening unfolds that one stable patch; no new paint, limbs, eye,
torso scale or runtime deformation is introduced. Closed poses are byte-exact.
"""
import argparse
import hashlib
import json
import os
from pathlib import Path
import re

import numpy as np
from PIL import Image, ImageDraw, ImageFilter
from native_frame_painter import SIZE, PPU, ORIGIN, SpritePainter
from frame_colors import decode as _decode, over as _over

FOLDER = Path(__file__).resolve().parent
PROJECT = FOLDER.parents[3]
HEAD_POLYGON = ((0, .49), (.05, .47), (.10, .43), (.15, .38), (.19, .33),
                (.22, .28), (.225, .23), (.20, .18), (.15, .13), (.08, .10),
                (0, .09), (-.08, .10), (-.15, .13), (-.20, .18), (-.225, .23),
                (-.22, .28), (-.19, .33), (-.15, .38), (-.10, .43), (-.05, .47))


def encode(rgba, green=False):
    alpha = rgba[..., 3:4]
    rgb = rgba[..., :3]
    if green:
        rgb = rgb + np.array([0, 1, 0], np.float32) * (1 - alpha)
    else:
        rgb = np.divide(rgb, alpha, out=np.zeros_like(rgb), where=alpha > 1e-8)
    rgb = np.where(rgb <= .0031308, rgb * 12.92, 1.055 * np.maximum(rgb, 0) ** (1 / 2.4) - .055)
    if not green:
        rgb = np.concatenate([rgb, alpha], axis=-1)
    return Image.fromarray((np.clip(rgb, 0, 1) * 255).round().astype(np.uint8))


def atomic_png(image, path):
    path = Path(path)
    path.parent.mkdir(parents=True, exist_ok=True)
    temporary = path.with_name(path.name + '.mouth-tmp')
    image.save(temporary, format='PNG', compress_level=1)
    os.replace(temporary, path)


def opening(pose):
    if pose.get('group') != 'strike':
        return 0.0
    progress = pose['sample'] / 23.0
    phase = progress / .4 if progress <= .4 else (1 - progress) / .6
    phase = min(1.0, max(0.0, phase))
    return phase * phase * (3 - 2 * phase)


class AttackMouth:
    def __init__(self, project=PROJECT, closed_torso=None):
        self.project = Path(project)
        if closed_torso is None:
            closed_torso = SpritePainter(self.project)._torso
        self.closed_coverage = Image.fromarray((closed_torso[..., 3] * 255).round().astype(np.uint8))
        asset = self.project / 'Assets/EmergencyShift/Art/Enemies/Stalker/Frames/Attack03.asset'
        text = asset.read_text(encoding='utf-8-sig')
        transform = re.search(r'uvTransform: \{x: ([^,]+), y: ([^,]+), z: ([^,]+), w: ([^}]+)\}', text)
        if transform is None:
            raise ValueError('Native Attack03 has no world-to-atlas transform')
        source_ppu, pivot_x, _, pivot_y = map(float, transform.groups())
        source_path = self.project / 'Assets/EmergencyShift/Art/Enemies/Stalker/PaleFlesh/attack-sheet.png'
        with Image.open(source_path) as image:
            source = _decode(image.convert('RGBA'))
            source_height = image.height
        channels = [Image.fromarray(source[..., channel]) for channel in range(4)]
        ratio = source_ppu / PPU
        coefficients = (ratio, 0, pivot_x - ORIGIN[0] * ratio,
                        0, ratio, source_height - pivot_y - ORIGIN[1] * ratio)
        registered = np.stack([np.asarray(channel.transform((SIZE, SIZE), Image.Transform.AFFINE,
            coefficients, Image.Resampling.BICUBIC)) for channel in channels], axis=-1)
        registered = np.clip(registered, 0, 1)
        registered[..., :3] = np.minimum(registered[..., :3], registered[..., 3:4])
        mask = Image.new('L', (SIZE, SIZE))
        polygon = [(float(ORIGIN[0] + x * PPU), float(ORIGIN[1] - y * PPU)) for x, y in HEAD_POLYGON]
        ImageDraw.Draw(mask).polygon(polygon, fill=255)
        mask = mask.filter(ImageFilter.GaussianBlur(.45))
        self.source_patch = registered * (np.asarray(mask, np.float32) / 255)[..., None]
        self.recipe = {
            'processorSha256': hashlib.sha256(Path(__file__).read_bytes()).hexdigest(),
            'source': str(source_path.relative_to(self.project)).replace('\\', '/'),
            'sourceSha256': hashlib.sha256(source_path.read_bytes()).hexdigest(),
            'spriteAsset': str(asset.relative_to(self.project)).replace('\\', '/'),
            'spriteAssetSha256': hashlib.sha256(asset.read_bytes()).hexdigest(),
            'sourcePixelsPerUnit': source_ppu,
            'sourcePivotBottomLeftPixels': [pivot_x, pivot_y],
            'targetPixelsPerUnit': PPU,
            'targetOriginTopLeftPixels': ORIGIN.tolist(),
            'sourceHeadPolygonWorld': HEAD_POLYGON,
            'closedBodyCoverageSha256': hashlib.sha256(np.asarray(self.closed_coverage).tobytes()).hexdigest(),
            'earlyContour': 'Width opening^.7; opening <=.15 stays inside canonical body coverage; .15-.35 grows that coverage geometrically before exposing native outer lip.',
            'method': 'Geometric collapse of native lips/teeth/cavity to closed lip line y=.30; positive BOX area filtering and bilinear subpixel placement avoid ringing; linear-premultiplied composition; no runtime crossfade or new paint.',
            'workingColor': 'linear premultiplied float32; files straight sRGB RGBA',
            'changes': 'Strike24 head only; no limbs/pose/eye/torso scale edits. Entry and return remain exactly closed.',
        }

    def layer(self, pose):
        amount = opening(pose)
        if amount <= 0:
            return np.zeros((SIZE, SIZE, 4), np.float32)
        # Every source interval collapses into the same closed lip line. A
        # fixed lower anchor stretches the lower teeth in the first samples.
        # Area-filter the shrinking patch before placing it: point sampling a
        # nearly flat cavity produces hard horizontal cuts and flickering AA.
        width = max(1, round(SIZE * amount ** .7))
        height = max(1, round(SIZE * amount))
        scale_x, scale_y = width / SIZE, height / SIZE
        lip_y = float(ORIGIN[1] - .30 * PPU)
        offset_x = float(ORIGIN[0] * (1 - scale_x))
        offset_y = lip_y * (1 - scale_y)
        channels = []
        for channel in range(4):
            resized = Image.fromarray(self.source_patch[..., channel]).resize((width, height), Image.Resampling.BOX)
            placed = resized.transform((SIZE, SIZE), Image.Transform.AFFINE,
                (1, 0, -offset_x, 0, 1, -offset_y), Image.Resampling.BILINEAR)
            channels.append(np.asarray(placed))
        patch = np.clip(np.stack(channels, axis=-1), 0, 1)
        patch[..., :3] = np.minimum(patch[..., :3], patch[..., 3:4])
        if amount < .35:
            growth = min(1.0, max(0.0, (amount - .15) / .20))
            growth = growth * growth * (3 - 2 * growth)
            radius = growth * 24
            low, high = int(np.floor(radius)), int(np.ceil(radius))
            lower_image = self.closed_coverage.filter(ImageFilter.MaxFilter(low * 2 + 1)) if low else self.closed_coverage
            upper_image = self.closed_coverage.filter(ImageFilter.MaxFilter(high * 2 + 1)) if high else self.closed_coverage
            lower = np.asarray(lower_image, np.float32) / 255
            upper = np.asarray(upper_image, np.float32) / 255
            coverage = lower * (1 - (radius - low)) + upper * (radius - low)
            patch *= coverage[..., None]
        return patch.astype(np.float32)

    def apply(self, pose, whole, limbs, torso):
        patch = self.layer(pose)
        if opening(pose) <= 0:
            return whole, torso, patch
        decorated_torso = _over(patch, torso)
        below = np.zeros_like(whole)
        for limb in limbs:
            below = _over(limb, below)
        return _over(decorated_torso, below), decorated_torso, patch


def load_rgba(path):
    with Image.open(path) as image:
        return _decode(image.convert('RGBA'))


def registered_eye():
    """Read the actual open-eye atlas at its prefab's unshifted, unit-scale mount."""
    asset = PROJECT / 'Assets/EmergencyShift/Art/Enemies/Stalker/LargeEye01/EyeOpen.asset'
    prefab = PROJECT / 'Assets/EmergencyShift/Prefabs/Enemies/ShadowStalker.prefab'
    prefab_text = prefab.read_text(encoding='utf-8-sig')
    iris_id = re.search(r'^  iris: \{fileID: (\d+)\}', prefab_text, re.MULTILINE).group(1)
    iris = re.search(r'--- !u!212 &' + iris_id + r'\n(.*?)(?=\n--- !u!|\Z)', prefab_text, re.DOTALL).group(1)
    object_id = re.search(r'm_GameObject: \{fileID: (\d+)\}', iris).group(1)
    transforms = re.findall(r'--- !u!4 &\d+\n(.*?)(?=\n--- !u!|\Z)', prefab_text, re.DOTALL)
    transform = next(block for block in transforms if f'm_GameObject: {{fileID: {object_id}}}' in block)
    position = tuple(map(float, re.search(r'm_LocalPosition: \{x: ([^,]+), y: ([^,]+), z: ([^}]+)\}', transform).groups()))
    scale = tuple(map(float, re.search(r'm_LocalScale: \{x: ([^,]+), y: ([^,]+), z: ([^}]+)\}', transform).groups()))
    if position[:2] != (0, 0) or scale[:2] != (1, 1):
        raise ValueError('Eye mount changed; review native mouth/eye registration before continuing')
    text = asset.read_text(encoding='utf-8-sig')
    match = re.search(r'uvTransform: \{x: ([^,]+), y: ([^,]+), z: ([^,]+), w: ([^}]+)\}', text)
    source_ppu, pivot_x, _, pivot_y = map(float, match.groups())
    texture = asset.parent / 'eye-atlas.png'
    with Image.open(texture) as image:
        source = _decode(image.convert('RGBA'))
        height = image.height
    rect = re.search(r'm_Rect:\s+serializedVersion: \d+\s+x: ([^\n]+)\s+y: ([^\n]+)\s+width: ([^\n]+)\s+height: ([^\n]+)', text)
    left, bottom, width, rect_height = map(float, rect.groups())
    yy, xx = np.mgrid[:height, :source.shape[1]]
    inside = (xx >= left) & (xx < left + width) & (yy >= height - bottom - rect_height) & (yy < height - bottom)
    source *= inside[..., None]
    ratio = source_ppu / PPU
    coefficients = (ratio, 0, pivot_x - ORIGIN[0] * ratio,
                    0, ratio, height - pivot_y - ORIGIN[1] * ratio)
    eye = np.stack([np.asarray(Image.fromarray(source[..., channel]).transform((SIZE, SIZE), Image.Transform.AFFINE,
        coefficients, Image.Resampling.BICUBIC)) for channel in range(4)], axis=-1)
    eye = np.clip(eye, 0, 1)
    eye[..., :3] = np.minimum(eye[..., :3], eye[..., 3:4])
    return eye, {'asset': str(asset.relative_to(PROJECT)).replace('\\', '/'),
        'sourcePixelsPerUnit': source_ppu, 'sourcePivotBottomLeftPixels': [pivot_x, pivot_y],
        'localPosition': [0, 0], 'localScale': [1, 1],
        'prefab': str(prefab.relative_to(PROJECT)).replace('\\', '/'),
        'prefabSha256': hashlib.sha256(prefab.read_bytes()).hexdigest(),
        'scope': 'Offline actual EyeOpen sprite registration; shader/gaze/live transform checked by Unity scenario capture.'}


def read_u8(path):
    with Image.open(path) as image:
        return np.asarray(image.convert('RGBA'))


def process(audit=False):
    poses = json.loads((FOLDER / 'attack-poses.json').read_text(encoding='utf-8-sig'))
    composition_path = FOLDER / 'attack-composition.json'
    composition = json.loads(composition_path.read_text()) if composition_path.exists() else {}
    already_applied = composition.get('mouthOverlayApplied', False)
    pose_hash = hashlib.sha256((FOLDER / 'attack-poses.json').read_bytes()).hexdigest()
    if already_applied and composition.get('posesSha256') != pose_hash:
        raise ValueError('Attack mass composition does not match current pose export; wait for final mass completion')
    if audit and (not already_applied or composition.get('poses') != 792):
        raise ValueError('Read-only audit requires the completed 792-pose mass with mouthOverlayApplied=true')
    painter = AttackMouth()
    torso_path = FOLDER / 'Masks/torso.png'
    torso_u8 = read_u8(torso_path)
    torso = _decode(Image.fromarray(torso_u8))
    eye, eye_recipe = registered_eye()
    traces = []
    failures = []
    endpoint_checks = []
    selected = [p for p in poses if p['group'] == 'strike' and p['sample'] in (0, 3, 6, 9, 15, 23)]
    preview = Image.new('RGB', (1800, 350), '#00ff00')
    draw = ImageDraw.Draw(preview)
    strike_sheet = Image.new('RGB', (6 * 320, 4 * 350), '#00ff00')
    strike_draw = ImageDraw.Draw(strike_sheet)
    for index, pose in enumerate(poses):
        if not audit and pose not in selected:
            continue
        name = pose['name']
        path = FOLDER / 'Raw' / (name + '.png')
        if not path.exists():
            if audit:
                raise FileNotFoundError('Finish the full mass composition before mouth postprocessing: ' + str(path))
            continue
        original_hash = hashlib.sha256(path.read_bytes()).hexdigest()
        amount = opening(pose)
        whole = load_rgba(path)
        if already_applied:
            body = load_rgba(FOLDER / 'Masks' / f'torso-{name}.png')
            mouth = load_rgba(FOLDER / 'Masks' / f'mouth-{name}.png')
            decorated = whole
        else:
            limbs = []
            if amount > 0:
                limbs = [load_rgba(FOLDER / 'Masks' / f'leg-{name}-{leg}.png') for leg in range(6)]
            decorated, body, mouth = painter.apply(pose, whole, limbs, torso)
        if audit:
            # This branch only reads Raw and Masks. It never rewrites a source
            # sprite or applies the patch to already-composited antialiasing.
            body_u8 = read_u8(FOLDER / 'Masks' / f'torso-{name}.png')
            mouth_u8 = read_u8(FOLDER / 'Masks' / f'mouth-{name}.png')
            expected_mouth = painter.layer(pose)
            expected_mouth_u8 = np.asarray(encode(expected_mouth))
            outside = expected_mouth[..., 3] == 0
            outside_changes = int(np.any(body_u8 != torso_u8, axis=-1)[outside].sum())
            mouth_error = int(np.abs(mouth_u8.astype(np.int16) - expected_mouth_u8.astype(np.int16)).max())
            closed_changes = int(np.any(body_u8 != torso_u8, axis=-1).sum()) if amount == 0 else 0
            if outside_changes or mouth_error or closed_changes:
                failures.append({'name': name, 'outsideMouthChangedPixels': outside_changes,
                    'mouthLayerMaxByteError': mouth_error, 'closedTorsoChangedPixels': closed_changes})
            endpoint_equal = None
            if (pose['group'] == 'entry' and pose['sample'] == 0) or (pose['group'] == 'return' and pose['sample'] == 7):
                walk_name = f'body-0-{pose["phase"]:02d}.png'
                endpoint_equal = bool(np.array_equal(read_u8(path), read_u8(FOLDER / 'Raw' / walk_name)))
                endpoint_checks.append({'name': name, 'walk': walk_name, 'equalPixels': endpoint_equal})
                if not endpoint_equal:
                    failures.append({'name': name, 'walkEndpointMismatch': walk_name})
            reconstruction_error = None
            if pose['group'] == 'strike':
                below = np.zeros_like(whole)
                for leg in range(6):
                    below = _over(load_rgba(FOLDER / 'Masks' / f'leg-{name}-{leg}.png'), below)
                expected_whole = _over(body, below)
                reconstruction_error = float(np.abs(expected_whole - whole).max())
                # Saved independent masks quantize six layers before this
                # reconstruction; source composition quantizes only once.
                if reconstruction_error > .012:
                    failures.append({'name': name, 'wholeReconstructionMaxPremultipliedError': reconstruction_error})
                column, row = pose['sample'] % 6, pose['sample'] // 6
                image = encode(_over(eye, whole), green=True).resize((320, 320), Image.Resampling.LANCZOS)
                strike_sheet.paste(image, (column * 320, row * 350 + 25))
                strike_draw.text((column * 320 + 5, row * 350 + 5), f'{name} mouth {amount:.3f}', fill='black')
            traces.append({'name': name, 'opening': amount, 'wholeBeforeSha256': original_hash,
                'wholeAfterSha256': hashlib.sha256(path.read_bytes()).hexdigest(),
                'mouthPixelsAlphaOver4': int((mouth[..., 3] > 4 / 255).sum()),
                'outsideMouthChangedPixels': outside_changes, 'mouthLayerMaxByteError': mouth_error,
                'closedTorsoChangedPixels': closed_changes, 'walkEndpointEqualPixels': endpoint_equal,
                'wholeReconstructionMaxPremultipliedError': reconstruction_error,
                'eyeOverlapPixels': int(((mouth[..., 3] > 4 / 255) & (eye[..., 3] > 4 / 255)).sum()),
                'torsoMask': f'Masks/torso-{name}.png', 'mouthMask': f'Masks/mouth-{name}.png'})
        if pose in selected:
            column = selected.index(pose)
            image = encode(decorated, green=True).resize((300, 300), Image.Resampling.LANCZOS)
            preview.paste(image, (column * 300, 30))
            draw.text((column * 300 + 8, 6), f'{name} mouth {amount:.3f}', fill='black')
    preview.save(FOLDER / 'attack-mouth-preview.jpg', quality=94, subsampling=0)
    if audit:
        if len(traces) != 792:
            raise AssertionError(f'Expected 792 per-pose torso/mouth layers, got {len(traces)}')
        (FOLDER / 'attack-mouth-trace.json').write_text(json.dumps(traces, indent=2), encoding='utf-8')
        report = {'mode': 'Read-only audit: no Raw or Masks writes', 'poses': len(traces),
            'sourcePoseSha256': pose_hash, 'strikeFrames': sum(p['group'] == 'strike' for p in poses),
            'exactWalkEndpoints': len(endpoint_checks), 'endpoints': endpoint_checks,
            'registration': eye_recipe, 'failures': failures, 'pass': not failures}
        (FOLDER / 'attack-mouth-audit.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
        strike_sheet.save(FOLDER / 'attack-mouth-eye-strike24.jpg', quality=94, subsampling=0)
        if failures:
            raise AssertionError(f'Read-only mouth audit found {len(failures)} failures; see attack-mouth-audit.json')
    return {'poses': len(traces), 'audited': audit, 'alreadyAppliedInline': already_applied, 'preview': str(FOLDER / 'attack-mouth-preview.jpg')}


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--audit', action='store_true', help='Read-only audit of completed mass; writes reports/previews only.')
    arguments = parser.parse_args()
    print(json.dumps(process(arguments.audit), indent=2))
