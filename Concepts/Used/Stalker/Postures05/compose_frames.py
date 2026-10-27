"""Bake reviewed whole native materials into complete straight-alpha frames."""
from pathlib import Path
from concurrent.futures import ThreadPoolExecutor
import argparse
import hashlib
import json

from PIL import Image, ImageDraw

FAMILY = Path(__file__).resolve().parent
PROJECT = FAMILY.parents[3]
ALGORITHM = 'whole-native-continuous-polar'


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def dependency_paths(attack):
    paths = {
        'posesSha256': FAMILY / ('attack-poses.json' if attack else 'poses.json'),
        'bonesSha256': FAMILY / 'bones.json',
        'composerSha256': FAMILY / 'compose_frames.py',
        'painterSha256': FAMILY / 'native_frame_painter.py',
        'colorsSha256': FAMILY / 'frame_colors.py',
        'sourceAtlasSha256': PROJECT / 'Assets/EmergencyShift/Art/Enemies/Stalker/Locomotion02/rig-atlas.png',
        'nativeWalkSha256': PROJECT / 'Assets/EmergencyShift/Art/Enemies/Stalker/PaleFlesh/walk-sheet.png',
        'anatomySha256': PROJECT / 'Concepts/Used/Stalker/Locomotion02/anatomy.json',
    }
    if attack:
        paths.update({
            'mouthSha256': FAMILY / 'attack_mouth.py',
            'nativeAttackSha256': PROJECT / 'Assets/EmergencyShift/Art/Enemies/Stalker/PaleFlesh/attack-sheet.png',
            'nativeAttackSpriteSha256': PROJECT / 'Assets/EmergencyShift/Art/Enemies/Stalker/Frames/Attack03.asset',
        })
    return paths


def require_unchanged(paths, snapshots):
    changed = [str(path) for key, path in paths.items() if digest(path) != snapshots[key]]
    if changed:
        raise RuntimeError('Composition inputs changed during rendering; rerender before packing: ' + ', '.join(changed))


def save_png(image, path):
    temporary = path.with_name(path.stem + '.writing.png')
    image.save(temporary, compress_level=1)
    temporary.replace(path)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--attack', action='store_true')
    parser.add_argument('--preview', action='store_true')
    parser.add_argument('--workers', type=int, choices=range(1, 5), default=1)
    args = parser.parse_args()
    kind = 'attack' if args.attack else 'body'
    composition_path = FAMILY / f'{kind}-composition.json'
    if not args.preview:
        composition_path.unlink(missing_ok=True)
    paths = dependency_paths(args.attack)
    snapshots = {key: digest(path) for key, path in paths.items()}
    pose_path = FAMILY / ('attack-poses.json' if args.attack else 'poses.json')
    pose_bytes = pose_path.read_bytes()
    if hashlib.sha256(pose_bytes).hexdigest() != snapshots['posesSha256']:
        raise RuntimeError('Pose data changed while composition was starting; rerun the composer.')
    poses = json.loads(pose_bytes)
    # Import only after snapshotting code and data used by this render.
    from frame_colors import encode
    from native_frame_painter import SpritePainter

    out = FAMILY / 'PreviewWhole' if args.preview else FAMILY
    if args.preview:
        if args.attack:
            names = {'entry-12-03', 'strike-00', 'strike-09', 'strike-18', 'return-12-04'}
            poses = [pose for pose in poses if pose['name'] in names]
        else:
            poses = [pose for pose in poses if pose['phase'] in [0, 1, 12, 24, 25, 36]]
    (out / 'Raw').mkdir(parents=True, exist_ok=True)
    (out / 'Masks').mkdir(exist_ok=True)
    painter = SpritePainter(PROJECT)
    mouth = None
    if args.attack:
        from attack_mouth import AttackMouth
        mouth = AttackMouth(PROJECT, painter._torso)
    else:
        save_png(encode(painter._torso), out / 'Masks/torso.png')
    sheet = None
    if args.preview:
        sheet = Image.new('RGB', (4 * 576, ((len(poses) + 3) // 4) * 612), '#00ff00')
    def render(item):
        index, pose = item
        name = pose.get('name', f'body-{pose.get("row", 0)}-{pose["phase"]:02d}')
        whole, limbs, torso = painter.paint(pose)
        if mouth is not None:
            whole, torso, mouth_layer = mouth.apply(pose, whole, limbs, torso)
            save_png(encode(torso), out / 'Masks' / f'torso-{name}.png')
            save_png(encode(mouth_layer), out / 'Masks' / f'mouth-{name}.png')
        save_png(encode(whole), out / 'Raw' / f'{name}.png')
        for leg, limb in enumerate(limbs):
            stem = name if args.attack else f'{pose["row"]}-{pose["phase"]:02d}'
            save_png(encode(limb), out / 'Masks' / f'leg-{stem}-{leg}.png')
        preview = encode(whole, green=True) if sheet is not None else None
        return index, name, preview

    # The painter's source arrays are read-only; each job owns its frame and paths.
    with ThreadPoolExecutor(max_workers=args.workers) as executor:
        for index, name, preview in executor.map(render, enumerate(poses)):
            if sheet is not None:
                x, y = index % 4 * 576, index // 4 * 612
                sheet.paste(preview, (x, y + 36))
                ImageDraw.Draw(sheet).text((x + 10, y + 10), name, fill='black')
            if (index + 1) % 48 == 0:
                print(f'{kind}: {index + 1}/{len(poses)}', flush=True)
    if sheet is not None:
        require_unchanged(paths, snapshots)
        sheet.save(out / f'{kind}-native-green.png')
        return
    require_unchanged(paths, snapshots)
    report = {
        'algorithm': ALGORITHM,
        'poses': len(poses),
        'alpha': 'straight',
        'workingColor': 'linear premultiplied',
        **snapshots,
        'mouthOverlayApplied': mouth is not None,
        'contourCapsuleClipping': False,
        'runtimeIk': False,
        'method': 'One continuous native material per limb; traced body ownership; complete baked frames',
    }
    if mouth is not None:
        (FAMILY / 'attack-mouth-recipe.json').write_text(json.dumps(mouth.recipe, indent=2))
    temporary_report = composition_path.with_suffix('.writing.json')
    temporary_report.write_text(json.dumps(report, indent=2))
    temporary_report.replace(composition_path)
    print(f'{len(poses)} complete {kind} frames', flush=True)


if __name__ == '__main__':
    main()
