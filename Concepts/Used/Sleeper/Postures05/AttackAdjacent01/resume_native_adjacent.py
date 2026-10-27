"""Recover the disk-full bake, retaining every complete native odd pose."""
from concurrent.futures import ThreadPoolExecutor
from hashlib import sha256
from pathlib import Path
import argparse
import importlib.util
import json

import numpy as np
from PIL import Image


HERE = Path(__file__).resolve().parent
BASE = HERE.parent
PROJECT = BASE.parents[3]
WARM = PROJECT / 'Concepts/Used/Stalker/Postures05/AttackAdjacent01'
EXPECTED_WARM_COMPOSITION = '95d9f5b3efeada473a4c75eb81b7ce54da3a8c98ba0e97ca463416def713b519'


def digest(path):
    return sha256(Path(path).read_bytes()).hexdigest()


def main(workers):
    if (HERE / 'native-composition.json').exists():
        raise RuntimeError('Completed material bake is frozen; do not resume it')
    producer_path = HERE / 'compose_native_adjacent.py'
    spec = importlib.util.spec_from_file_location('unchanged_interrupted_native_bake', producer_path)
    producer = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(producer)
    if digest(WARM / 'attack-composition.json') != EXPECTED_WARM_COMPOSITION:
        raise AssertionError('Canonical adjacent composition differs from the confirmed completed Raw source')
    warm_composition = json.loads((WARM / 'attack-composition.json').read_bytes())
    snapshots = {path.relative_to(PROJECT).as_posix(): digest(path) for path in producer.paths_for_source()}
    snapshots[Path(__file__).relative_to(PROJECT).as_posix()] = digest(__file__)
    snapshots[(WARM / 'attack-composition.json').relative_to(PROJECT).as_posix()] = EXPECTED_WARM_COMPOSITION
    poses, reused, odd = producer.load_geometry()
    copies = json.loads((HERE / 'copied-native-files.json').read_bytes())
    if len(copies) != 792 * 9:
        raise AssertionError('Expected all 7,128 old exact file copies before resuming')
    for record in copies:
        source, target = BASE / record['source'], HERE / record['target']
        if digest(source) != record['sha256'] or digest(target) != record['sha256']:
            raise AssertionError('An old792 native copy changed: ' + record['target'])
        snapshots[source.relative_to(PROJECT).as_posix()] = record['sha256']
    complete, missing = [], []
    for pose in odd:
        name = pose['name']
        own_paths = [HERE / 'Raw' / (name + '.png')] + [HERE / 'Masks' / filename for filename in producer.layer_paths(name)]
        warm_paths = [WARM / 'Raw' / (name + '.png')] + [WARM / 'Masks' / filename for filename in producer.layer_paths(name)]
        ready = True
        hashes = {}
        for own, warm in zip(own_paths, warm_paths):
            if not own.is_file():
                ready = False
                break
            try:
                rgba = producer.read_rgba(own)
            except (OSError, ValueError):
                ready = False
                break
            canonical = producer.read_rgba(warm)
            if not np.array_equal(rgba[..., 3], canonical[..., 3]):
                raise AssertionError('A completed native odd PNG has different alpha: ' + str(own))
            hashes[own.relative_to(PROJECT).as_posix()] = digest(own)
            snapshots[warm.relative_to(PROJECT).as_posix()] = digest(warm)
        if ready:
            complete.append(pose)
            snapshots.update(hashes)
        else:
            missing.append(pose)
    print(f'Recovery validation: 792 exact copies; {len(complete)} complete odd poses retained; {len(missing)} missing/incomplete odd poses', flush=True)
    from frame_colors import encode
    from native_material_frames import NativeMaterialPainter
    painter = NativeMaterialPainter()
    if any(array.flags.writeable for array in (*painter.painter.sources, painter.painter._torso, painter.mouth.source_patch)):
        raise AssertionError('Native material sources must remain read-only')
    # A bounded fresh render confirms that retained valid PNGs use the current
    # frozen native material, beyond the full alpha/CRC checks of every file.
    sampled = complete[::max(1, len(complete) // 8)][:8]
    for pose in sampled:
        whole, limbs, torso, mouth = painter.paint(pose)
        names = [HERE / 'Raw' / (pose['name'] + '.png')] + [HERE / 'Masks' / filename for filename in producer.layer_paths(pose['name'])]
        for path, pixels in zip(names, [whole, *limbs, torso, mouth]):
            if not np.array_equal(np.asarray(encode(pixels)), producer.read_rgba(path)):
                raise AssertionError('Retained odd pose differs from its immutable material renderer: ' + str(path))
    producer.require_unchanged(snapshots)

    def render(pose):
        name = pose['name']
        whole, limbs, torso, mouth = painter.paint(pose)
        own_paths = [HERE / 'Raw' / (name + '.png')] + [HERE / 'Masks' / filename for filename in producer.layer_paths(name)]
        warm_paths = [WARM / 'Raw' / (name + '.png')] + [WARM / 'Masks' / filename for filename in producer.layer_paths(name)]
        for own, warm, pixels in zip(own_paths, warm_paths, [whole, *limbs, torso, mouth]):
            image = encode(pixels)
            if not np.array_equal(np.asarray(image)[..., 3], producer.read_rgba(warm)[..., 3]):
                raise AssertionError('Recovered odd material alpha differs from canonical: ' + name)
            producer.save_png(own, image)
        return name

    with ThreadPoolExecutor(max_workers=workers) as executor:
        for index, name in enumerate(executor.map(render, missing)):
            if (index + 1) % 48 == 0:
                print(f'Recovered native odd poses: {index + 1}/{len(missing)}', flush=True)
    producer.require_unchanged(snapshots)
    raw_hashes = {pose['name']: digest(HERE / 'Raw' / (pose['name'] + '.png')) for pose in poses}
    result = {'algorithm': 'native-rgb-material-adjacent-bridge', 'poses': 1464,
              'copiedNativePoses': 792, 'newOddPoses': 672, 'copiedFiles': len(copies),
              'oldRawAndMasksByteExact': True, 'runtimeIk': False,
              'nativeMaterialRecipeSha256': digest(BASE / 'native-material-recipe.json'),
              'posesSha256': digest(WARM / 'attack-poses.json'),
              'sourceHashes': snapshots, 'rawPngSha256': raw_hashes,
              'reusedPoseMapping': reused,
              'recovery': {'cause': 'OSError 28: no space left on device',
                           'completeOddPosesPreserved': len(complete),
                           'missingOrIncompleteOddPosesRendered': len(missing),
                           'retainedOddPosesFreshVisibleAndLayerRgbaComparison': len(sampled),
                           'allRetainedPngAlphaExactToCanonical': True,
                           'old792NeverRewarped': True},
              'status': 'Material bake complete; strict visible-exact packing remains required'}
    producer.save_json(HERE / 'native-composition.json', result)
    print('RECOVERED native1464: all old files exact, all added poses complete', flush=True)


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--workers', type=int, choices=range(1, 5), default=2)
    main(parser.parse_args().workers)
