"""Reuse all 792 native Sleeper frames exactly; paint only 672 added poses."""
from concurrent.futures import ThreadPoolExecutor
from copy import deepcopy
from hashlib import sha256
from pathlib import Path
import argparse
import importlib.util
import json
import sys

import numpy as np
from PIL import Image, ImageDraw


HERE = Path(__file__).resolve().parent
BASE = HERE.parent
PROJECT = BASE.parents[3]
WARM_BASE = PROJECT / 'Concepts/Used/Stalker/Postures05'
WARM = WARM_BASE / 'AttackAdjacent01'
ART = PROJECT / 'Assets/EmergencyShift/Art/Enemies/Sleeper/Postures05/AttackAdjacent01'
sys.path.insert(0, str(BASE))
sys.path.insert(0, str(WARM_BASE))


def digest(path):
    return sha256(Path(path).read_bytes()).hexdigest()


def read_rgba(path):
    with Image.open(path) as image:
        return np.asarray(image.convert('RGBA')).copy()


def save_json(path, value):
    temporary = path.with_suffix('.writing.json')
    temporary.write_text(json.dumps(value, indent=2), encoding='utf-8')
    temporary.replace(path)


def save_png(path, image):
    temporary = path.with_suffix('.writing.png')
    image.save(temporary, compress_level=3)
    temporary.replace(path)


def layer_paths(name):
    return [f'leg-{name}-{leg}.png' for leg in range(6)] + [f'torso-{name}.png', f'mouth-{name}.png']


def paths_for_source():
    paths = [Path(__file__), WARM / 'attack-poses.json', WARM / 'attack-pose-validation.json',
             BASE / 'native_material_frames.py', BASE / 'native_mouth_palette.py',
             BASE / 'build_native_materials.py', BASE / 'native-material-recipe.json',
             BASE / 'attack-composition.json', BASE / 'attack-actual-png-verification.json',
             BASE / 'attack-packed-roundtrip.json', BASE / 'attack-sprites.json',
             WARM_BASE / 'attack-poses.json', WARM_BASE / 'attack-composition.json',
             WARM_BASE / 'native_frame_painter.py', WARM_BASE / 'frame_colors.py',
             WARM_BASE / 'attack_mouth.py', WARM_BASE / 'pack_sprites.py']
    old_inputs = json.loads((BASE / 'attack-native-inputs.json').read_bytes())
    for relative, expected in old_inputs.items():
        path = PROJECT / relative
        if digest(path) != expected:
            raise AssertionError('Base792 frozen input changed: ' + relative)
    recipe = json.loads((BASE / 'native-material-recipe.json').read_bytes())
    paths += [PROJECT / recipe['source'], PROJECT / recipe['target'],
              PROJECT / recipe['mouth']['stalkerSource'], PROJECT / recipe['mouth']['sleeperSource']]
    return paths


def require_unchanged(snapshots):
    for relative, expected in snapshots.items():
        if digest(PROJECT / relative) != expected:
            raise AssertionError('Adjacent material input changed: ' + relative)


def load_geometry():
    poses = json.loads((WARM / 'attack-poses.json').read_bytes())
    old = {pose['name']: pose for pose in json.loads((WARM_BASE / 'attack-poses.json').read_bytes())}
    if len(poses) != 1464 or len({pose['name'] for pose in poses}) != 1464:
        raise AssertionError('Expected 1,464 uniquely named attack poses')
    reused, added = [], []
    for pose in poses:
        if pose['group'] == 'strike':
            old_name = pose['name']
        elif pose['sample'] % 2 == 0:
            old_name = f'{pose["group"]}-{pose["phase"]:02d}-{pose["sample"] // 2:02d}'
        else:
            added.append(pose)
            continue
        for key in ('hips', 'knees', 'feet'):
            if not np.array_equal(np.asarray(pose[key]), np.asarray(old[old_name][key])):
                raise AssertionError('Reused geometry changed: ' + pose['name'])
        reused.append({'source': old_name, 'target': pose['name']})
    if len(reused) != 792 or len(added) != 672:
        raise AssertionError('Old/new pose counts differ from 792/672')
    return poses, reused, added


def bake(workers):
    if (HERE / 'native-composition.json').exists() or (HERE / 'Raw').exists():
        raise RuntimeError('Preserve the existing adjacent study; do not silently overwrite it')
    snapshots = {path.relative_to(PROJECT).as_posix(): digest(path) for path in paths_for_source()}
    poses, reused, added = load_geometry()
    old_records = {record['name']: record for record in json.loads((BASE / 'attack-composition.json').read_bytes())['records']}
    (HERE / 'Raw').mkdir()
    (HERE / 'Masks').mkdir()
    copies = []
    for index, mapping in enumerate(reused):
        old_name, name = mapping['source'], mapping['target']
        pairs = [(BASE / 'Raw' / (old_name + '.png'), HERE / 'Raw' / (name + '.png'))]
        pairs += [(BASE / 'Masks' / old_layer, HERE / 'Masks' / new_layer)
                  for old_layer, new_layer in zip(layer_paths(old_name), layer_paths(name))]
        if digest(pairs[0][0]) != old_records[old_name]['rawPngSha256']:
            raise AssertionError('An approved native Raw changed: ' + old_name)
        for source, target in pairs:
            payload = source.read_bytes()
            expected = sha256(payload).hexdigest()
            target.write_bytes(payload)
            if digest(target) != expected:
                raise AssertionError('An exact native copy changed: ' + name)
            snapshots[source.relative_to(PROJECT).as_posix()] = expected
            copies.append({'source': source.relative_to(BASE).as_posix(),
                           'target': target.relative_to(HERE).as_posix(), 'sha256': expected})
        if (index + 1) % 96 == 0:
            print(f'Native exact frame copies: {index + 1}/792', flush=True)
    save_json(HERE / 'copied-native-files.json', copies)
    from frame_colors import encode
    from native_material_frames import NativeMaterialPainter
    painter = NativeMaterialPainter()
    if any(array.flags.writeable for array in (*painter.painter.sources, painter.painter._torso, painter.mouth.source_patch)):
        raise AssertionError('The source material must be immutable')

    def render(pose):
        name = pose['name']
        whole, limbs, torso, mouth = painter.paint(pose)
        save_png(HERE / 'Raw' / (name + '.png'), encode(whole))
        for layer, filename in zip([*limbs, torso, mouth], layer_paths(name)):
            save_png(HERE / 'Masks' / filename, encode(layer))
        return name

    with ThreadPoolExecutor(max_workers=workers) as executor:
        for index, name in enumerate(executor.map(render, added)):
            if (index + 1) % 48 == 0:
                print(f'Native added odd poses: {index + 1}/672', flush=True)
    require_unchanged(snapshots)
    raw_hashes = {pose['name']: digest(HERE / 'Raw' / (pose['name'] + '.png')) for pose in poses}
    result = {'algorithm': 'native-rgb-material-adjacent-bridge', 'poses': 1464,
              'copiedNativePoses': 792, 'newOddPoses': 672, 'copiedFiles': len(copies),
              'oldRawAndMasksByteExact': True, 'runtimeIk': False,
              'nativeMaterialRecipeSha256': digest(BASE / 'native-material-recipe.json'),
              'posesSha256': digest(WARM / 'attack-poses.json'),
              'sourceHashes': snapshots, 'rawPngSha256': raw_hashes,
              'reusedPoseMapping': reused,
              'status': 'Material bake complete; canonical adjacent alpha comparison and packing still required'}
    save_json(HERE / 'native-composition.json', result)
    print('Native1464 material bake ready; finish waits for reviewed Stalker adjacent Raw/Clean/manifest', flush=True)


def finish():
    composition = json.loads((HERE / 'native-composition.json').read_bytes())
    require_unchanged(composition['sourceHashes'])
    poses, reused, added = load_geometry()
    warm_composition = json.loads((WARM / 'attack-composition.json').read_bytes())
    warm_manifest = json.loads((WARM / 'attack-sprites.json').read_bytes())
    if warm_composition['poses'] != 1464 or warm_manifest['bridgeFrames'] != 15:
        raise AssertionError('Canonical adjacent production is incomplete')
    if warm_composition['posesSha256'] != composition['posesSha256']:
        raise AssertionError('Canonical adjacent geometry differs from the native bake')
    warm_frames = {record['name']: record for record in warm_manifest['frames']}
    copy_records = json.loads((HERE / 'copied-native-files.json').read_bytes())
    for record in copy_records:
        if digest(BASE / record['source']) != record['sha256'] or digest(HERE / record['target']) != record['sha256']:
            raise AssertionError('A reused native file is not byte exact: ' + record['target'])
    helper_spec = importlib.util.spec_from_file_location('canonical_pack_helpers', WARM_BASE / 'pack_sprites.py')
    helpers = importlib.util.module_from_spec(helper_spec)
    helper_spec.loader.exec_module(helpers)
    (HERE / 'Clean').mkdir(exist_ok=True)
    snapshots = dict(composition['sourceHashes'])
    for name in ('attack-composition.json', 'attack-sprites.json'):
        path = WARM / name
        snapshots[path.relative_to(PROJECT).as_posix()] = digest(path)
    images, indices, records = [], {}, []
    layers_checked = 0
    canonical_alpha_removed = 0
    max_removed_alpha = 0
    for index, pose in enumerate(poses):
        name = pose['name']
        raw_path = HERE / 'Raw' / (name + '.png')
        if digest(raw_path) != composition['rawPngSha256'][name]:
            raise AssertionError('A native adjacent Raw changed: ' + name)
        raw = read_rgba(raw_path)
        warm_raw_path = WARM / 'Raw' / (name + '.png')
        warm_clean_path = WARM / 'Clean' / (name + '.png')
        warm_raw, warm_clean = read_rgba(warm_raw_path), read_rgba(warm_clean_path)
        if not np.array_equal(raw[..., 3], warm_raw[..., 3]):
            raise AssertionError('Native adjacent Raw alpha differs: ' + name)
        for path in (warm_raw_path, warm_clean_path):
            snapshots[path.relative_to(PROJECT).as_posix()] = digest(path)
        for filename in layer_paths(name):
            own = read_rgba(HERE / 'Masks' / filename)
            warm_path = WARM / 'Masks' / filename
            warm_layer = read_rgba(warm_path)
            if not np.array_equal(own[..., 3], warm_layer[..., 3]):
                raise AssertionError('Native adjacent layer alpha differs: ' + filename)
            snapshots[warm_path.relative_to(PROJECT).as_posix()] = digest(warm_path)
            layers_checked += 1
        clean = raw.copy()
        clean[..., 3] = warm_clean[..., 3]
        changed = raw[..., 3] != clean[..., 3]
        if np.any(clean[..., 3] > raw[..., 3]) or np.any(changed & (raw[..., 3] > 8)):
            raise AssertionError('Canonical adjacent pack performed new visible alpha cleanup: ' + name)
        canonical_alpha_removed += int(changed.sum())
        if changed.any():
            max_removed_alpha = max(max_removed_alpha, int(raw[..., 3][changed].max()))
        helpers.pad_rgb(clean)
        save_png(HERE / 'Clean' / (name + '.png'), Image.fromarray(clean))
        frame = warm_frames[name]
        width, height = frame['rect'][2:]
        left = 288 - frame['pivot'][0] * width
        top = 288 - height + frame['pivot'][1] * height
        if abs(left - round(left)) > 1e-6 or abs(top - round(top)) > 1e-6:
            raise AssertionError('Canonical origin has subpixel registration: ' + name)
        left, top = round(left), round(top)
        bbox = (left, top, left + width, top + height)
        image_hash = sha256(clean.tobytes()).hexdigest()
        if image_hash not in indices:
            indices[image_hash] = len(images)
            images.append((Image.fromarray(clean).crop(bbox), bbox))
        records.append({'pose': pose, 'unique': indices[image_hash], 'cleanSha256': digest(HERE / 'Clean' / (name + '.png'))})
        if (index + 1) % 96 == 0:
            print(f'Native adjacent exact alpha check: {index + 1}/1464', flush=True)
    require_unchanged(snapshots)
    atlas_images = [Image.new('RGBA', (4096, 4096))]
    placements = []
    x, y, row_height = 0, 0, 0
    for image, bbox in images:
        if x + image.width > 4096:
            x, y, row_height = 0, y + row_height, 0
        if y + image.height > 4096:
            atlas_images.append(Image.new('RGBA', (4096, 4096)))
            x, y, row_height = 0, 0, 0
        atlas_images[-1].paste(image, (x, y))
        placements.append({'atlas': len(atlas_images) - 1, 'rect': [x, 4096 - y - image.height, image.width, image.height],
                           'pivot': [(288 - bbox[0]) / image.width, (bbox[3] - 288) / image.height]})
        x += image.width
        row_height = max(row_height, image.height)
    ART.mkdir(parents=True, exist_ok=True)
    manifest = deepcopy(warm_manifest)
    manifest['compositionAlgorithm'] = composition['algorithm']
    manifest['atlases'] = []
    manifest['frames'] = []
    for index, image in enumerate(atlas_images):
        path = ART / f'attack-{index:02d}.png'
        save_png(path, image)
        manifest['atlases'].append({'asset': path.relative_to(PROJECT).as_posix(), 'sha256': digest(path)})
    del atlas_images
    for record in records:
        pose = record['pose']
        manifest['frames'].append({'name': pose['name'], 'phase': pose['phase'], 'group': pose['group'],
                                    **placements[record['unique']]})
    # Read actual production PNGs from disk, not the pre-encode image objects.
    atlases = [read_rgba(PROJECT / record['asset']) for record in manifest['atlases']]
    for frame in manifest['frames']:
        name = frame['name']
        clean = read_rgba(HERE / 'Clean' / (name + '.png'))
        canonical_clean = read_rgba(WARM / 'Clean' / (name + '.png'))
        x, bottom, width, height = frame['rect']
        left = round(288 - frame['pivot'][0] * width)
        top = round(288 - height + frame['pivot'][1] * height)
        restored = np.zeros((576, 576, 4), dtype=np.uint8)
        restored[top:top + height, left:left + width] = atlases[frame['atlas']][4096 - bottom - height:4096 - bottom, x:x + width]
        visible = (clean[..., 3] > 0) | (restored[..., 3] > 0)
        if not np.array_equal(restored[visible], clean[visible]) or not np.array_equal(restored[..., 3], canonical_clean[..., 3]):
            raise AssertionError('Native adjacent actual PNG round trip changed coverage or visible RGBA: ' + name)
    require_unchanged(snapshots)
    save_json(HERE / 'attack-sprites.json', manifest)
    report = {'poses': 1464, 'bridgeFrames': 15, 'strikeFrames': 24,
              'copiedOldNativePosesByteExact': 792, 'newlyPaintedOddPoses': 672,
              'copiedOldNativeRawAndMaskFilesByteExact': len(copy_records),
              'materialLayersAlphaChecked': layers_checked,
              'allRawAndLayerAlphaByteExactToCanonical': True,
              'allPackedAlphaByteExactToCanonicalClean': True,
              'allActualProductionPngVisibleRgbaRoundTripsExact': True,
              'addedAlphaPixels': 0, 'removedAlphaPixelsComparedToCanonicalClean': 0,
              'canonicalRawTinyAlphaPixelsRemoved': canonical_alpha_removed,
              'maximumRemovedCanonicalRawAlpha': max_removed_alpha,
              'uniqueImages': len(images), 'atlases': len(manifest['atlases']),
              'packing': 'Independent complete-RGBA deduplication; original576 center/224PPU preserved',
              'sourceHashesUnchanged': True, 'runtimeIk': False,
              'manifestSha256': digest(HERE / 'attack-sprites.json'),
              'artReviewStatus': 'Native visual review and Unity runtime review remain separate gates'}
    save_json(HERE / 'native-packed-verification.json', report)
    save_json(HERE / 'final-source-hashes.json', snapshots)
    print(json.dumps(report), flush=True)
    review(added)


def review(poses):
    from frame_colors import decode
    from native_material_frames import composite_background
    output = HERE / 'Review'
    output.mkdir(exist_ok=True)
    backgrounds = {'green': (0, 255, 0), 'dark': (18, 22, 26), 'light': (235, 235, 235)}
    for start in range(0, len(poses), 16):
        group = poses[start:start + 16]
        for name, color in backgrounds.items():
            sheet = Image.new('RGB', (2304, ((len(group) + 3) // 4) * 608), color)
            draw = ImageDraw.Draw(sheet)
            for index, pose in enumerate(group):
                with Image.open(HERE / 'Clean' / (pose['name'] + '.png')) as image:
                    paint = decode(image.convert('RGBA'))
                x, y = index % 4 * 576, index // 4 * 608
                sheet.paste(composite_background(paint, color), (x, y + 32))
                draw.text((x + 8, y + 10), pose['name'] + ' | native224PPU | new odd pose', fill='white' if name == 'dark' else 'black')
            save_png(output / f'new-odd-{start // 16:03d}-{name}.png', sheet)
    save_json(HERE / 'review-pages.json', {'poses': len(poses), 'pages': (len(poses) + 15) // 16,
                                         'backgrounds': list(backgrounds), 'nativePpu': 224,
                                         'order': [pose['name'] for pose in poses]})
    print('Native672 new-pose review pages ready on three backgrounds', flush=True)


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    mode = parser.add_mutually_exclusive_group(required=True)
    mode.add_argument('--bake', action='store_true')
    mode.add_argument('--finish', action='store_true')
    parser.add_argument('--workers', type=int, choices=range(1, 5), default=2)
    args = parser.parse_args()
    if args.bake:
        bake(args.workers)
    else:
        finish()
