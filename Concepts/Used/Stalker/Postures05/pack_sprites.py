"""Pack reviewed straight-alpha sprites without cutting their visible contours."""
from pathlib import Path
import argparse
import hashlib
import json
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

folder = Path(__file__).resolve().parent
project = folder.parents[3]
art = project / 'Assets/EmergencyShift/Art/Enemies/Stalker/Postures05'


def components(mask):
    parents, spans, previous = [], [], []

    def root(index):
        while parents[index] != index:
            parents[index] = parents[parents[index]]
            index = parents[index]
        return index

    for y, row in enumerate(mask):
        edges = np.flatnonzero(np.diff(np.r_[False, row, False]))
        current = []
        for left, right in edges.reshape(-1, 2):
            index = len(parents)
            parents.append(index)
            spans.append((y, left, right))
            for old_left, old_right, old_index in previous:
                if old_right >= left and old_left <= right:
                    parents[root(old_index)] = root(index)
            current.append((left, right, index))
        previous = current
    groups = {}
    for index, span in enumerate(spans):
        groups.setdefault(root(index), []).append(span)
    return sorted(groups.values(), key=lambda group: sum(b-a for y, a, b in group), reverse=True)


def pad_rgb(rgba):
    saved_alpha = rgba[..., 3].copy()
    valid = saved_alpha > 8
    for _ in range(3):
        next_valid = valid.copy()
        for dy, dx in [(0, 1), (0, -1), (1, 0), (-1, 0)]:
            take = ~next_valid & np.roll(valid, (dy, dx), (0, 1))
            rgba[take, :3] = np.roll(rgba[..., :3], (dy, dx), (0, 1))[take]
            next_valid[take] = True
        valid = next_valid
    rgba[..., 3] = saved_alpha


def pack(attack=False):
    art.mkdir(parents=True, exist_ok=True)
    (folder / 'Clean').mkdir(exist_ok=True)
    (folder / 'Review').mkdir(exist_ok=True)
    pose_file = 'attack-poses.json' if attack else 'poses.json'
    kind = 'attack' if attack else 'body'
    composition = json.loads((folder / f'{kind}-composition.json').read_text())
    if composition.get('algorithm') != 'whole-native-continuous-polar':
        raise RuntimeError('The parts-over experiment was rejected; use compose_frames.py.')
    if composition.get('composerSha256') != hashlib.sha256((folder / 'compose_frames.py').read_bytes()).hexdigest():
        raise RuntimeError('The composer changed after composition; rerender before packing.')
    if composition['painterSha256'] != hashlib.sha256((folder / 'native_frame_painter.py').read_bytes()).hexdigest():
        raise RuntimeError('The material painter changed after composition; rerender before packing.')
    if composition['posesSha256'] != hashlib.sha256((folder / pose_file).read_bytes()).hexdigest():
        raise RuntimeError('Pose data changed since composition; rerender before packing.')
    dependencies = {
        'bonesSha256': folder / 'bones.json',
        'colorsSha256': folder / 'frame_colors.py',
        'sourceAtlasSha256': project / 'Assets/EmergencyShift/Art/Enemies/Stalker/Locomotion02/rig-atlas.png',
        'nativeWalkSha256': project / 'Assets/EmergencyShift/Art/Enemies/Stalker/PaleFlesh/walk-sheet.png',
        'anatomySha256': project / 'Concepts/Used/Stalker/Locomotion02/anatomy.json',
    }
    if attack:
        dependencies['mouthSha256'] = folder / 'attack_mouth.py'
        dependencies['nativeAttackSha256'] = project / 'Assets/EmergencyShift/Art/Enemies/Stalker/PaleFlesh/attack-sheet.png'
        dependencies['nativeAttackSpriteSha256'] = project / 'Assets/EmergencyShift/Art/Enemies/Stalker/Frames/Attack03.asset'
    for key, path in dependencies.items():
        if composition.get(key) != hashlib.sha256(path.read_bytes()).hexdigest():
            raise RuntimeError(f'{path.name} changed after composition; rerender before packing.')
    poses = json.loads((folder / pose_file).read_text())
    anatomy = json.loads((folder / 'bones.json').read_text())
    manifest = {'phases': 48, 'closureRows': 9, 'pixelsPerUnit': 224, 'alpha': 'straight', 'kind': kind,
                'posesSha256': composition['posesSha256'],
                'cycleDistance': anatomy['cycleDistance'],
                'compactStrideScale': anatomy['compactStrideScale'],
                'compositionAlgorithm': composition['algorithm'],
                'frames': [], 'atlases': []}
    if attack:
        manifest.update({'bridgeFrames': 8, 'strikeFrames': 24, 'entryFraction': .15, 'returnFraction': .85})
    records, images, identical = [], [], {}
    for pose in poses:
        name = pose.get('name', f'body-{pose.get("row", 0)}-{pose["phase"]:02d}')
        path = folder / 'Raw' / f'{name}.png'
        rgba = np.array(Image.open(path).convert('RGBA'))
        alpha = rgba[..., 3]
        assert not any((alpha[index] > 8).any() for index in [0, -1]), name
        assert not any((alpha[:, index] > 8).any() for index in [0, -1]), name
        groups = components(alpha > 8)
        major = [group for group in groups if sum(b-a for y, a, b in group) > 4]
        if len(major) != 1:
            raise RuntimeError(f'{name}: {len(major)} disconnected parts. Repair the composition, not the packer.')
        removed = 0
        for group in groups[1:]:
            for y, left, right in group:
                rgba[y, left:right, 3] = 0
                removed += right-left
        # Only sub-visible isolated antialiasing is removed; no morphological
        # opening, capsule clipping or filling intentional V-shaped spaces.
        edge = np.array(Image.fromarray((rgba[..., 3] > 8).astype('uint8') * 255).filter(ImageFilter.MaxFilter(3))) > 0
        rgba[~edge, 3] = 0
        pad_rgb(rgba)
        image = Image.fromarray(rgba)
        image.save(folder / 'Clean' / f'{name}.png', compress_level=3)
        bbox = image.getbbox()
        x0, y0, x1, y1 = bbox
        bbox = (max(0, x0-3), max(0, y0-3), min(576, x1+3), min(576, y1+3))
        digest = hashlib.sha256(rgba.tobytes()).hexdigest()
        if digest not in identical:
            identical[digest] = len(images)
            images.append((image.crop(bbox), bbox))
        records.append({'pose': pose, 'name': name, 'unique': identical[digest], 'bbox': bbox,
                        'sha256': digest, 'components': len(major), 'tinyDetachedPixelsRemoved': removed})

    atlas_index, x, y, row_height = 0, 0, 0, 0
    atlas = Image.new('RGBA', (4096, 4096))
    placements = []
    prefix = 'attack' if attack else 'body'

    def save_atlas():
        target = art / f'{prefix}-{atlas_index:02d}.png'
        atlas.save(target, compress_level=6)
        manifest['atlases'].append({'asset': target.relative_to(project).as_posix(),
                                   'sha256': hashlib.sha256(target.read_bytes()).hexdigest()})

    for image, bbox in images:
        if x + image.width > 4096:
            x, y, row_height = 0, y + row_height, 0
        if y + image.height > 4096:
            save_atlas()
            atlas_index += 1
            atlas = Image.new('RGBA', (4096, 4096))
            x, y, row_height = 0, 0, 0
        atlas.paste(image, (x, y))
        placements.append({'atlas': atlas_index, 'rect': [x, 4096-y-image.height, image.width, image.height],
                           'pivot': [(288-bbox[0])/image.width, (bbox[3]-288)/image.height]})
        x += image.width
        row_height = max(row_height, image.height)
    save_atlas()
    for record in records:
        pose = record['pose']
        frame = {'name': record['name'], 'phase': pose['phase'], **placements[record['unique']]}
        if attack:
            frame['group'] = pose['group']
        else:
            frame['closure'] = pose['row']
        manifest['frames'].append(frame)
    manifest_name = 'attack-sprites.json' if attack else 'sprites.json'
    (folder / manifest_name).write_text(json.dumps(manifest, indent=2))
    stats = {'frames': len(records), 'uniqueImages': len(images), 'atlases': len(manifest['atlases']),
             'components': 1, 'contourMorphology': False,
             'tinyDetachedPixelsRemoved': sum(record['tinyDetachedPixelsRemoved'] for record in records),
             'source': 'Complete native limb materials, continuous inverse UV, preserved visible outline'}
    (folder / f'{prefix}-packing.json').write_text(json.dumps(stats, indent=2))
    if not attack:
        for row in range(9):
            sheet = Image.new('RGB', (1600, 1320), '#00ff00')
            draw = ImageDraw.Draw(sheet)
            for phase in range(48):
                image = Image.open(folder / 'Clean' / f'body-{row}-{phase:02d}.png').convert('RGBA')
                image = image.resize((192, 192), Image.Resampling.LANCZOS)
                left, top = phase % 8 * 200, phase // 8 * 220
                sheet.paste(image, (left+4, top+22), image)
                draw.text((left+5, top+5), f'fold {row}/8 | phase {phase:02d}/47', fill='black')
            sheet.save(folder / 'Review' / f'closure-{row}.jpg', quality=96)
    print(json.dumps(stats))


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--attack', action='store_true')
    pack(parser.parse_args().attack)
