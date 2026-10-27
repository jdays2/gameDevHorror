"""Pack adjacent native frames without changing any visible RGB or alpha."""
from copy import deepcopy
from hashlib import sha256
from pathlib import Path
import importlib.util
import json

import numpy as np
from PIL import Image


HERE = Path(__file__).resolve().parent
BASE = HERE.parent
PROJECT = BASE.parents[3]
WARM_BASE = PROJECT / 'Concepts/Used/Stalker/Postures05'
WARM = WARM_BASE / 'AttackAdjacent01'
ART = PROJECT / 'Assets/EmergencyShift/Art/Enemies/Sleeper/Postures05/AttackAdjacent01'


def digest(path):
    return sha256(Path(path).read_bytes()).hexdigest()


def module_at(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def main():
    composition = json.loads((HERE / 'native-composition.json').read_bytes())
    snapshots = dict(composition['sourceHashes'])
    snapshots[Path(__file__).relative_to(PROJECT).as_posix()] = digest(__file__)
    producer = module_at('immutable_native_adjacent_bake', HERE / 'compose_native_adjacent.py')
    producer.require_unchanged(snapshots)
    poses, reused, added = producer.load_geometry()
    warm_composition = json.loads((WARM / 'attack-composition.json').read_bytes())
    manifest_path = WARM / 'attack-sprites.json'
    warm_manifest = json.loads(manifest_path.read_bytes())
    if warm_composition['poses'] != 1464 or warm_manifest['bridgeFrames'] != 15:
        raise AssertionError('Canonical adjacent production is incomplete')
    if warm_composition['posesSha256'] != composition['posesSha256']:
        raise AssertionError('Native and canonical adjacent geometry differ')
    warm_frames = {record['name']: record for record in warm_manifest['frames']}
    for filename in ('attack-composition.json', 'attack-sprites.json'):
        path = WARM / filename
        snapshots[path.relative_to(PROJECT).as_posix()] = digest(path)
    copies = json.loads((HERE / 'copied-native-files.json').read_bytes())
    for record in copies:
        if digest(BASE / record['source']) != record['sha256'] or digest(HERE / record['target']) != record['sha256']:
            raise AssertionError('A base792 Raw/mask copy changed: ' + record['target'])
    helpers = module_at('stable_canonical_pack_helpers', WARM_BASE / 'pack_sprites.py')
    (HERE / 'Clean').mkdir(exist_ok=True)
    unique_images, indices, records = [], {}, []
    for index, pose in enumerate(poses):
        name = pose['name']
        raw_path = HERE / 'Raw' / (name + '.png')
        if digest(raw_path) != composition['rawPngSha256'][name]:
            raise AssertionError('Native adjacent Raw changed: ' + name)
        with Image.open(raw_path) as encoded:
            encoded.verify()
        raw = producer.read_rgba(raw_path)
        warm_raw_path = WARM / 'Raw' / (name + '.png')
        warm_clean_path = WARM / 'Clean' / (name + '.png')
        warm_raw = producer.read_rgba(warm_raw_path)
        warm_clean = producer.read_rgba(warm_clean_path)
        if not np.array_equal(raw[..., 3], warm_raw[..., 3]) or not np.array_equal(warm_clean[..., 3], warm_raw[..., 3]):
            raise AssertionError('Every adjacent alpha byte must equal canonical Raw and Clean: ' + name)
        warm_visible = warm_raw[..., 3] > 0
        if not np.array_equal(warm_raw[warm_visible], warm_clean[warm_visible]):
            raise AssertionError('Canonical adjacent packing changed visible source RGB: ' + name)
        for path in (raw_path, warm_raw_path, warm_clean_path):
            snapshots[path.relative_to(PROJECT).as_posix()] = digest(path)
        for filename in producer.layer_paths(name):
            own_path, warm_path = HERE / 'Masks' / filename, WARM / 'Masks' / filename
            with Image.open(own_path) as encoded:
                encoded.verify()
            own_layer = producer.read_rgba(own_path)
            warm_layer = producer.read_rgba(warm_path)
            if not np.array_equal(own_layer[..., 3], warm_layer[..., 3]):
                raise AssertionError('Native adjacent material layer alpha differs: ' + filename)
            for path in (own_path, warm_path):
                snapshots[path.relative_to(PROJECT).as_posix()] = digest(path)
        clean = raw.copy()
        padding_candidate = raw.copy()
        helpers.pad_rgb(padding_candidate)
        # The legacy helper proposes RGB near the outline. Only alpha-zero
        # proposals are accepted; every visible native RGBA byte is retained.
        hidden = raw[..., 3] == 0
        clean[..., :3][hidden] = padding_candidate[..., :3][hidden]
        if not np.array_equal(clean[~hidden], raw[~hidden]) or not np.array_equal(clean[..., 3], raw[..., 3]):
            raise AssertionError('Padding changed visible native material: ' + name)
        producer.save_png(HERE / 'Clean' / (name + '.png'), Image.fromarray(clean))
        frame = warm_frames[name]
        width, height = frame['rect'][2:]
        left = 288 - frame['pivot'][0] * width
        top = 288 - height + frame['pivot'][1] * height
        if abs(left - round(left)) > 1e-6 or abs(top - round(top)) > 1e-6:
            raise AssertionError('Canonical adjacent origin is fractional: ' + name)
        left, top = round(left), round(top)
        bbox = (left, top, left + width, top + height)
        image_hash = sha256(clean.tobytes()).hexdigest()
        if image_hash not in indices:
            indices[image_hash] = len(unique_images)
            unique_images.append((Image.fromarray(clean).crop(bbox), bbox))
        records.append({'name': name, 'pose': pose, 'unique': indices[image_hash],
                        'cleanPngSha256': digest(HERE / 'Clean' / (name + '.png')),
                        'allRawAlphaAndEightLayerAlphaByteExactToCanonical': True,
                        'cleanVisibleRgbaAndAllAlphaByteExactToOwnRaw': True})
        if (index + 1) % 96 == 0:
            print(f'Native visible-exact adjacent alpha/layer check: {index + 1}/1464', flush=True)
    producer.require_unchanged(snapshots)
    atlases = [Image.new('RGBA', (4096, 4096))]
    placements = []
    x, y, row_height = 0, 0, 0
    for image, bbox in unique_images:
        if x + image.width > 4096:
            x, y, row_height = 0, y + row_height, 0
        if y + image.height > 4096:
            atlases.append(Image.new('RGBA', (4096, 4096)))
            x, y, row_height = 0, 0, 0
        atlases[-1].paste(image, (x, y))
        placements.append({'atlas': len(atlases) - 1,
                           'rect': [x, 4096 - y - image.height, image.width, image.height],
                           'pivot': [(288 - bbox[0]) / image.width, (bbox[3] - 288) / image.height]})
        x += image.width
        row_height = max(row_height, image.height)
    ART.mkdir(parents=True, exist_ok=True)
    manifest = deepcopy(warm_manifest)
    manifest['compositionAlgorithm'] = 'native-rgb-material-adjacent-bridge-visible-exact'
    manifest['nativeMaterialRecipeSha256'] = digest(BASE / 'native-material-recipe.json')
    manifest['visibleRgbaByteExactToRaw'] = True
    manifest['allRawAlphaPreserved'] = True
    manifest['atlases'], manifest['frames'] = [], []
    for index, atlas in enumerate(atlases):
        path = ART / f'attack-{index:02d}.png'
        producer.save_png(path, atlas)
        manifest['atlases'].append({'asset': path.relative_to(PROJECT).as_posix(), 'sha256': digest(path)})
    del atlases
    for record in records:
        pose = record['pose']
        manifest['frames'].append({'name': pose['name'], 'phase': pose['phase'], 'group': pose['group'],
                                   **placements[record['unique']]})
    actual_atlases = [producer.read_rgba(PROJECT / atlas['asset']) for atlas in manifest['atlases']]
    for frame, record in zip(manifest['frames'], records):
        name = frame['name']
        raw = producer.read_rgba(HERE / 'Raw' / (name + '.png'))
        clean = producer.read_rgba(HERE / 'Clean' / (name + '.png'))
        if digest(HERE / 'Clean' / (name + '.png')) != record['cleanPngSha256']:
            raise AssertionError('Native Clean changed during packing: ' + name)
        x, bottom, width, height = frame['rect']
        left = round(288 - frame['pivot'][0] * width)
        top = round(288 - height + frame['pivot'][1] * height)
        restored = np.zeros((576, 576, 4), dtype=np.uint8)
        restored[top:top + height, left:left + width] = actual_atlases[frame['atlas']][4096 - bottom - height:4096 - bottom, x:x + width]
        visible = (raw[..., 3] > 0) | (restored[..., 3] > 0)
        if not np.array_equal(restored[visible], raw[visible]) or not np.array_equal(restored[..., 3], raw[..., 3]):
            raise AssertionError('Actual atlas changes native Raw visible RGB or alpha: ' + name)
        record['actualAtlasVisibleRgbaAndAllAlphaByteExactToRaw'] = True
    producer.require_unchanged(snapshots)
    producer.save_json(HERE / 'attack-sprites.json', manifest)
    report = {'poses': 1464, 'bridgeFrames': 15, 'strikeFrames': 24,
              'copiedOldNativePosesByteExact': 792, 'newlyPaintedOddPoses': 672,
              'copiedOldRawAndMaskPngsByteExact': len(copies),
              'materialLayersChecked': 1464 * 8,
              'allRawAndLayerAlphaByteExactToCanonicalRaw': True,
              'allCleanAndActualPackedVisibleRgbaByteExactToOwnRaw': True,
              'allActualPackedAlphaByteExactToCanonicalRawAndClean': True,
              'addedAlphaPixels': 0, 'removedAlphaPixels': 0, 'changedVisibleRgbPixels': 0,
              'paddingPolicy': 'Stable canonical padding helper proposed RGB; accept only at alpha zero',
              'uniqueImages': len(unique_images), 'atlases': len(manifest['atlases']),
              'independentCompleteRgbaDeduplication': True,
              'worldOriginPixels': [288, 288], 'canvasPixels': 576, 'pixelsPerUnit': 224,
              'sourceHashesUnchanged': True, 'runtimeIk': False,
              'manifestSha256': digest(HERE / 'attack-sprites.json'),
              'packerSha256': digest(__file__), 'records': records}
    producer.save_json(HERE / 'native-packed-verification.json', report)
    producer.save_json(HERE / 'final-source-hashes.json', snapshots)
    print(json.dumps({key: value for key, value in report.items() if key != 'records'}), flush=True)
    producer.review(added)
    producer.require_unchanged(snapshots)


if __name__ == '__main__':
    main()
