"""Preserve approved even attack samples byte-exact; bake only new odd poses."""
from pathlib import Path
from concurrent.futures import ThreadPoolExecutor
import hashlib
import json
import sys
import numpy as np
from PIL import Image

HERE = Path(__file__).resolve().parent
FAMILY = HERE.parent
PROJECT = FAMILY.parents[3]
sys.path.insert(0, str(FAMILY))


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def save_png(image, path):
    temporary = path.with_suffix('.writing.png')
    image.save(temporary, compress_level=1)
    temporary.replace(path)


def main():
    assert not (HERE/'attack-composition.json').exists(), 'Completed adjacent family is frozen'
    assert not (HERE/'Raw').exists(), 'Do not overwrite incomplete study without auditing its cause'
    paths = {
        'posesSha256': HERE/'attack-poses.json',
        'poseValidationSha256': HERE/'attack-pose-validation.json',
        'composerSha256': HERE/'compose_adjacent.py',
        'oldPosesSha256': FAMILY/'attack-poses.json',
        'oldCompositionSha256': FAMILY/'attack-composition.json',
        'painterSha256': FAMILY/'native_frame_painter.py',
        'colorsSha256': FAMILY/'frame_colors.py',
        'mouthSha256': FAMILY/'attack_mouth.py',
        'sourceAtlasSha256': PROJECT/'Assets/EmergencyShift/Art/Enemies/Stalker/Locomotion02/rig-atlas.png',
        'nativeWalkSha256': PROJECT/'Assets/EmergencyShift/Art/Enemies/Stalker/PaleFlesh/walk-sheet.png',
        'anatomySha256': PROJECT/'Concepts/Used/Stalker/Locomotion02/anatomy.json',
        'nativeAttackSha256': PROJECT/'Assets/EmergencyShift/Art/Enemies/Stalker/PaleFlesh/attack-sheet.png',
        'nativeAttackSpriteSha256': PROJECT/'Assets/EmergencyShift/Art/Enemies/Stalker/Frames/Attack03.asset',
        'rigProfileSha256': PROJECT/'Assets/EmergencyShift/Data/Enemies/StalkerLimbRig.asset',
    }
    snapshots = {key: digest(path) for key, path in paths.items()}
    old_composition = json.loads(paths['oldCompositionSha256'].read_text())
    assert old_composition['poses'] == 792
    assert old_composition['algorithm'] == 'whole-native-continuous-polar'
    for key in ['painterSha256', 'colorsSha256', 'mouthSha256', 'sourceAtlasSha256', 'nativeWalkSha256', 'anatomySha256', 'nativeAttackSha256', 'nativeAttackSpriteSha256']:
        assert snapshots[key] == old_composition[key], ('Approved source changed', key)
    old = {pose['name']: pose for pose in json.loads(paths['oldPosesSha256'].read_text())}
    new = json.loads(paths['posesSha256'].read_text())
    assert len(old) == 792 and len(new) == 1464
    assert len({p['name'] for p in new}) == len(new)
    copied, odd = [], []
    for pose in new:
        if pose['group'] == 'strike':
            source_name = pose['name']
        elif pose['sample'] % 2 == 0:
            source_name = f'{pose["group"]}-{pose["phase"]:02d}-{pose["sample"]//2:02d}'
        else:
            odd.append(pose)
            continue
        reference = old[source_name]
        for key in ['hips', 'knees', 'feet']:
            assert np.array_equal(np.asarray(pose[key]), np.asarray(reference[key])), (pose['name'], key)
        copied.append({'source': source_name, 'target': pose['name']})
    assert len(copied) == 792 and len(odd) == 672
    # Independent constant-length, bend and authored floor check before skin work.
    first = next(iter(old.values()))
    base_upper = np.linalg.norm(np.asarray(first['knees'])-first['hips'], axis=-1)
    base_lower = np.linalg.norm(np.asarray(first['feet'])-first['knees'], axis=-1)
    cross = lambda x, y: x[:, 0]*y[:, 1]-x[:, 1]*y[:, 0]
    sign = np.sign(cross(np.asarray(first['feet'])[:, :2]-np.asarray(first['hips'])[:, :2],
                         np.asarray(first['knees'])[:, :2]-np.asarray(first['hips'])[:, :2]))
    geometry = []
    for pose in new:
        hip, knee, foot = [np.asarray(pose[key]) for key in ['hips', 'knees', 'feet']]
        error = max(np.max(np.abs(np.linalg.norm(knee-hip, axis=-1)-base_upper)),
                    np.max(np.abs(np.linalg.norm(foot-knee, axis=-1)-base_lower)))
        winding = cross(foot[:, :2]-hip[:, :2], knee[:, :2]-hip[:, :2])*sign
        assert error < 3e-7 and min(foot[:, 2]) >= -1e-7 and min(winding) > 0, pose['name']
        contacts = int(np.count_nonzero(np.abs(foot[:, 2]) < 1e-5))
        assert contacts >= 3, (pose['name'], contacts)
        geometry.append({'name': pose['name'], 'boneLengthError': float(error),
            'minimumFootZ': float(foot[:, 2].min()), 'minimumSignedWinding': float(winding.min()),
            'floorContacts': contacts})
    (HERE/'geometry-checks.json').write_text(json.dumps(geometry, indent=2))
    (HERE/'Raw').mkdir()
    (HERE/'Masks').mkdir()
    files = []
    for index, item in enumerate(copied):
        old_name, name = item['source'], item['target']
        pairs = [(FAMILY/'Raw'/f'{old_name}.png', HERE/'Raw'/f'{name}.png')]
        pairs += [(FAMILY/'Masks'/f'{kind}-{old_name}.png', HERE/'Masks'/f'{kind}-{name}.png') for kind in ['torso', 'mouth']]
        pairs += [(FAMILY/'Masks'/f'leg-{old_name}-{leg}.png', HERE/'Masks'/f'leg-{name}-{leg}.png') for leg in range(6)]
        for source, target in pairs:
            data = source.read_bytes()
            sha = hashlib.sha256(data).hexdigest()
            temporary = target.with_suffix('.writing.png')
            temporary.write_bytes(data)
            temporary.replace(target)
            assert digest(target) == sha
            files.append({'source': str(source.relative_to(FAMILY)), 'target': str(target.relative_to(HERE)), 'sha256': sha})
        if (index+1) % 96 == 0:
            print(f'Exact approved frame copies: {index+1}/792', flush=True)
    (HERE/'copied-approved-files.json').write_text(json.dumps(files, indent=2))
    from frame_colors import encode
    from native_frame_painter import SpritePainter
    from attack_mouth import AttackMouth
    painter = SpritePainter(PROJECT)
    assert all(not source.flags.writeable for source in painter.sources)
    mouth = AttackMouth(PROJECT, painter._torso)
    def render(pose):
        whole, limbs, torso = painter.paint(pose)
        whole, torso, mouth_layer = mouth.apply(pose, whole, limbs, torso)
        name = pose['name']
        save_png(encode(whole), HERE/'Raw'/f'{name}.png')
        save_png(encode(torso), HERE/'Masks'/f'torso-{name}.png')
        save_png(encode(mouth_layer), HERE/'Masks'/f'mouth-{name}.png')
        for leg, limb in enumerate(limbs):
            save_png(encode(limb), HERE/'Masks'/f'leg-{name}-{leg}.png')
        return name
    with ThreadPoolExecutor(max_workers=4) as executor:
        for index, name in enumerate(executor.map(render, odd)):
            if (index+1) % 48 == 0:
                print(f'New odd bridge frames: {index+1}/672', flush=True)
    changed = [key for key, path in paths.items() if digest(path) != snapshots[key]]
    assert not changed, ('Inputs changed during bake', changed)
    for item in files:
        assert digest(FAMILY/item['source']) == item['sha256'], ('Approved old frame changed', item['source'])
        assert digest(HERE/item['target']) == item['sha256']
    raw_hashes = {pose['name']: digest(HERE/'Raw'/f'{pose["name"]}.png') for pose in new}
    result = {'algorithm': 'whole-native-continuous-polar-adjacent-bridge', 'poses': len(new),
        'phases': 48, 'bridgeFrames': 15, 'strikeFrames': 24, 'copiedApprovedPoses': len(copied),
        'newOddPoses': len(odd), 'copiedFileCount': len(files), 'sourcePoseArraysExact': True,
        'copiedRawAndMasksByteExact': True, 'alpha': 'straight', 'workingColor': 'linear premultiplied',
        'mouthOverlayApplied': True, 'runtimeIk': False, 'contourCapsuleClipping': False,
        **snapshots, 'rawPngSha256': raw_hashes, 'copiedPoseMapping': copied,
        'geometryMaximumLengthError': max(v['boneLengthError'] for v in geometry),
        'geometryMinimumFloorContacts': min(v['floorContacts'] for v in geometry),
        'artReviewStatus': 'New odd frames need independent numeric and native-scale visual review.'}
    temporary = HERE/'attack-composition.writing.json'
    temporary.write_text(json.dumps(result, indent=2))
    temporary.replace(HERE/'attack-composition.json')
    print('READY1464: 792 approved byte-exact copies and672 new odd poses', flush=True)


if __name__ == '__main__':
    main()
