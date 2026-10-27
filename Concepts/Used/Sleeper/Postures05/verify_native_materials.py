"""Read actual Sleeper PNGs back from disk and verify every packed frame."""
from hashlib import sha256
from pathlib import Path
import json
import sys

import numpy as np
from PIL import Image, ImageDraw


FAMILY = Path(__file__).resolve().parent
PROJECT = FAMILY.parents[3]
CANONICAL = PROJECT / 'Concepts/Used/Stalker/Postures05'


def digest(path):
    return sha256(Path(path).read_bytes()).hexdigest()


def rgba(path):
    with Image.open(path) as image:
        return np.asarray(image.convert('RGBA')).copy()


def verify(attack):
    kind = 'attack' if attack else 'body'
    manifest_path = FAMILY / ('attack-sprites.json' if attack else 'sprites.json')
    manifest = json.loads(manifest_path.read_bytes())
    canonical_manifest = json.loads((CANONICAL / manifest_path.name).read_bytes())
    canonical_records = {frame['name']: frame for frame in canonical_manifest['frames']}
    if manifest['pixelsPerUnit'] != 224 or manifest['cycleDistance'] != 1.25 or manifest['compactStrideScale'] != .8:
        raise AssertionError('The motion scale or pixel registration changed')
    inputs = json.loads((FAMILY / f'{kind}-native-inputs.json').read_bytes())
    for relative, expected in inputs.items():
        if digest(PROJECT / relative) != expected:
            raise AssertionError('A frozen native bake input changed: ' + relative)
    canonical_composition = json.loads((CANONICAL / f'{kind}-composition.json').read_bytes())
    canonical_dependencies = {
        'posesSha256': CANONICAL / ('attack-poses.json' if attack else 'poses.json'),
        'bonesSha256': CANONICAL / 'bones.json',
        'composerSha256': CANONICAL / 'compose_frames.py',
        'painterSha256': CANONICAL / 'native_frame_painter.py',
        'colorsSha256': CANONICAL / 'frame_colors.py',
        'sourceAtlasSha256': PROJECT / 'Assets/EmergencyShift/Art/Enemies/Stalker/Locomotion02/rig-atlas.png',
        'nativeWalkSha256': PROJECT / 'Assets/EmergencyShift/Art/Enemies/Stalker/PaleFlesh/walk-sheet.png',
        'anatomySha256': PROJECT / 'Concepts/Used/Stalker/Locomotion02/anatomy.json',
    }
    if attack:
        canonical_dependencies.update({
            'mouthSha256': CANONICAL / 'attack_mouth.py',
            'nativeAttackSha256': PROJECT / 'Assets/EmergencyShift/Art/Enemies/Stalker/PaleFlesh/attack-sheet.png',
            'nativeAttackSpriteSha256': PROJECT / 'Assets/EmergencyShift/Art/Enemies/Stalker/Frames/Attack03.asset',
        })
    for key, path in canonical_dependencies.items():
        if digest(path) != canonical_composition[key]:
            raise AssertionError('Canonical mass source differs from its reviewed bake: ' + str(path))
    composition = json.loads((FAMILY / f'{kind}-composition.json').read_bytes())
    source_records = {frame['name']: frame for frame in composition['records']}
    atlases = []
    for atlas in manifest['atlases']:
        path = PROJECT / atlas['asset']
        if digest(path) != atlas['sha256']:
            raise AssertionError('Production PNG hash differs: ' + str(path))
        atlases.append(rgba(path))
    rows = []
    alpha_removed = 0
    maximum_removed_alpha = 0
    layer_count = 0
    for record in manifest['frames']:
        name = record['name']
        raw_path = FAMILY / 'Raw' / (name + '.png')
        if digest(raw_path) != source_records[name]['rawPngSha256']:
            raise AssertionError('Raw hash differs from the completed bake: ' + name)
        raw = rgba(raw_path)
        clean = rgba(FAMILY / 'Clean' / (name + '.png'))
        canonical_raw = rgba(CANONICAL / 'Raw' / (name + '.png'))
        canonical_clean = rgba(CANONICAL / 'Clean' / (name + '.png'))
        if not np.array_equal(raw[..., 3], canonical_raw[..., 3]):
            raise AssertionError('Raw alpha differs from Stalker: ' + name)
        if not np.array_equal(clean[..., 3], canonical_clean[..., 3]):
            raise AssertionError('Clean alpha differs from Stalker: ' + name)
        x, bottom, width, height = record['rect']
        left = 288 - record['pivot'][0] * width
        top = 288 - height + record['pivot'][1] * height
        if abs(left - round(left)) > 1e-6 or abs(top - round(top)) > 1e-6:
            raise AssertionError('Packed frame has a subpixel registration: ' + name)
        left, top = round(left), round(top)
        restored = np.zeros((576, 576, 4), dtype=np.uint8)
        restored[top:top + height, left:left + width] = atlases[record['atlas']][4096 - bottom - height:4096 - bottom, x:x + width]
        visible = (clean[..., 3] > 0) | (restored[..., 3] > 0)
        if not np.array_equal(restored[visible], clean[visible]):
            raise AssertionError('Actual PNG atlas round trip changes visible RGBA: ' + name)
        if not np.array_equal(restored[..., 3], canonical_clean[..., 3]):
            raise AssertionError('Actual PNG atlas round trip changes canonical alpha: ' + name)
        changed = raw[..., 3] != clean[..., 3]
        if np.any(clean[..., 3] > raw[..., 3]) or np.any(changed & (raw[..., 3] > 8)):
            raise AssertionError('Packing introduced visible coverage cleanup: ' + name)
        alpha_removed += int(changed.sum())
        if changed.any():
            maximum_removed_alpha = max(maximum_removed_alpha, int(raw[..., 3][changed].max()))
        stem = name if attack else name.removeprefix('body-')
        layers = [f'leg-{stem}-{leg}.png' for leg in range(6)]
        if attack:
            layers += [f'torso-{name}.png', f'mouth-{name}.png']
        for filename in layers:
            own = rgba(FAMILY / 'Masks' / filename)
            canonical = rgba(CANONICAL / 'Masks' / filename)
            if not np.array_equal(own[..., 3], canonical[..., 3]):
                raise AssertionError('A complete material layer changed alpha: ' + filename)
            layer_count += 1
        canonical_record = canonical_records[name]
        rows.append({'name': name, 'actualAtlasVisibleRgbaByteExact': True,
                     'actualAtlasAlphaByteExactToCanonicalClean': True,
                     'rawAndAllLayerAlphaBytesExact': True,
                     'canonicalRectExact': record['rect'] == canonical_record['rect'],
                     'canonicalPivotExact': record['pivot'] == canonical_record['pivot'],
                     'canonicalAtlasIndexExact': record['atlas'] == canonical_record['atlas']})
    if not attack:
        if not np.array_equal(rgba(FAMILY / 'Masks/torso.png')[..., 3], rgba(CANONICAL / 'Masks/torso.png')[..., 3]):
            raise AssertionError('Body source torso alpha differs')
        layer_count += 1
    result = {'frames': len(rows), 'materialLayersChecked': layer_count,
              'actualProductionPngsReadBack': len(atlases), 'allVisibleRgbaRoundTripsByteExact': True,
              'allPackedAlphaByteExactToCanonicalClean': True, 'allRawAlphaByteExactToCanonicalRaw': True,
              'addedAlphaPixels': 0, 'removedAlphaPixelsComparedToCanonicalClean': 0,
              'rawToCleanCanonicalTinyAlphaPixelsRemoved': alpha_removed,
              'maximumRemovedCanonicalRawAlpha': maximum_removed_alpha,
              'allRectPivotAndAtlasIndicesCanonical': all(row['canonicalRectExact'] and row['canonicalPivotExact'] and row['canonicalAtlasIndexExact'] for row in rows),
              'inputHashesUnchanged': True, 'manifestSha256': digest(manifest_path), 'records': rows}
    (FAMILY / f'{kind}-actual-png-verification.json').write_text(json.dumps(result, indent=2), encoding='utf-8')
    print(json.dumps({key: value for key, value in result.items() if key != 'records'}), flush=True)
    return result


def attack_overview():
    sys.path.insert(0, str(CANONICAL))
    from frame_colors import decode
    from native_material_frames import composite_background
    output = FAMILY / 'Review'
    output.mkdir(exist_ok=True)
    sheet = Image.new('RGB', (6 * 288, 4 * 320), '#00ff00')
    draw = ImageDraw.Draw(sheet)
    for sample in range(24):
        name = f'strike-{sample:02d}'
        with Image.open(FAMILY / 'Clean' / (name + '.png')) as image:
            paint = decode(image.convert('RGBA'))
        preview = composite_background(paint, (0, 255, 0)).resize((288, 288), Image.Resampling.BOX)
        x, y = sample % 6 * 288, sample // 6 * 320
        sheet.paste(preview, (x, y + 32))
        draw.text((x + 8, y + 10), name + ' | overview 112 PPU', fill='black')
    sheet.save(output / 'strike24-green-overview.png')


def main():
    body = verify(False)
    attack = verify(True)
    study = json.loads((FAMILY / 'NativeMaterialStudy01/study.json').read_bytes())
    recipe = study['recipe']
    for relative, expected in ((recipe['source'], recipe['sourceSha256']),
                               (recipe['target'], recipe['targetSha256']),
                               (recipe['mouth']['stalkerSource'], recipe['mouth']['stalkerSourceSha256']),
                               (recipe['mouth']['sleeperSource'], recipe['mouth']['sleeperSourceSha256'])):
        if digest(PROJECT / relative) != expected:
            raise AssertionError('A reviewed native material source changed since the bounded study: ' + relative)
    for record in study['frames']:
        name = record['name']
        if not np.array_equal(rgba(FAMILY / 'Raw' / (name + '.png')), rgba(FAMILY / 'NativeMaterialStudy01' / (name + '.png'))):
            raise AssertionError('Parallel mass result differs from the reviewed single-thread study: ' + name)
    attack_overview()
    result = {'frames': body['frames'] + attack['frames'],
              'verifierSha256': digest(__file__),
              'allNativeSourceAndPackedAlphaChecksPassed': True,
              'materialLayersChecked': body['materialLayersChecked'] + attack['materialLayersChecked'],
              'productionPngsReadBack': body['actualProductionPngsReadBack'] + attack['actualProductionPngsReadBack'],
              'reviewedSingleThreadStudyMatchesParallelMassRgba': len(study['frames']),
              'addedAlphaPixels': 0, 'removedAlphaPixelsComparedToCanonicalClean': 0,
              'runtimeIk': False, 'unityImportedByThisTool': False,
              'artApproval': 'Independent visual inspection and Unity runtime gate are separate; owner creative acceptance is not inferred'}
    (FAMILY / 'native-material-verification.json').write_text(json.dumps(result, indent=2), encoding='utf-8')
    print(json.dumps(result), flush=True)


if __name__ == '__main__':
    main()
