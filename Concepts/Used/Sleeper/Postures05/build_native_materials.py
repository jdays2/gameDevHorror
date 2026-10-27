"""Bake and pack the native Sleeper RGB material on frozen Stalker05 coverage."""
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


FAMILY = Path(__file__).resolve().parent
PROJECT = FAMILY.parents[3]
CANONICAL = PROJECT / 'Concepts/Used/Stalker/Postures05'
ART = PROJECT / 'Assets/EmergencyShift/Art/Enemies/Sleeper/Postures05'
ALGORITHM = 'whole-native-rgb-material-single-uv'
FROZEN = {
    'native_frame_painter.py': '2777a84fff7194b1be4aa75642f47801180703f60b6b6b1e5790dd53dc24dd7e',
    'compose_frames.py': '025d82fc2380799dc444fe3491b8bb8b3116fd6b19d353717878643f4858f86d',
    'attack_mouth.py': 'e9b0de8c9bc224554ed51df746e90001ba32fd26bd8b8373da1fa1584d9bf2d2',
    'poses.json': 'c35ae84b8c0ace617a67b6e291c1d11d7be00b3c6510f963c9cb74022571db7a',
    'attack-poses.json': '00b5b6bd8b71092b1d81f97d2cd4e1185f422475c6f542622cf6de28fd47dbe4',
}


def digest(path):
    return sha256(Path(path).read_bytes()).hexdigest()


def read_rgba(path):
    with Image.open(path) as image:
        return np.asarray(image.convert('RGBA')).copy()


def write_json(path, value):
    temporary = path.with_suffix('.writing.json')
    temporary.write_text(json.dumps(value, indent=2), encoding='utf-8')
    temporary.replace(path)


def write_png(path, image):
    temporary = path.with_name(path.stem + '.writing.png')
    image.save(temporary, compress_level=3)
    temporary.replace(path)


def name_of(pose):
    return pose.get('name', f'body-{pose.get("row", 0)}-{pose["phase"]:02d}')


def mask_paths(pose):
    name = name_of(pose)
    stem = name if 'group' in pose else f'{pose["row"]}-{pose["phase"]:02d}'
    paths = [f'Masks/leg-{stem}-{leg}.png' for leg in range(6)]
    if 'group' in pose:
        paths += [f'Masks/torso-{name}.png', f'Masks/mouth-{name}.png']
    return paths


def snapshot_inputs(poses, attack):
    for filename, expected in FROZEN.items():
        if digest(CANONICAL / filename) != expected:
            raise ValueError('Reviewed canonical input changed: ' + filename)
    paths = [Path(__file__), FAMILY / 'native_material_frames.py', FAMILY / 'native_mouth_palette.py']
    paths += [CANONICAL / name for name in FROZEN]
    paths += [CANONICAL / name for name in ('frame_colors.py', 'bones.json', 'pack_sprites.py',
                                          'body-composition.json', 'attack-composition.json',
                                          'sprites.json', 'attack-sprites.json')]
    paths += [PROJECT / name for name in (
        'Assets/EmergencyShift/Art/Enemies/Stalker/Locomotion02/rig-atlas.png',
        'Concepts/Used/Stalker/Locomotion02/anatomy.json',
        'Assets/EmergencyShift/Art/Enemies/Stalker/PaleFlesh/walk-sheet.png',
        'Assets/EmergencyShift/Art/Enemies/Stalker/PaleFlesh/attack-sheet.png',
        'Assets/EmergencyShift/Art/Enemies/Stalker/Frames/Attack03.asset',
        'Assets/EmergencyShift/Art/Enemies/Sleeper/StalkerPlaceholder01/PaleFlesh/walk-sheet.png',
        'Assets/EmergencyShift/Art/Enemies/Sleeper/StalkerPlaceholder01/PaleFlesh/attack-sheet.png')]
    if not attack:
        paths.append(CANONICAL / 'Masks/torso.png')
    for pose in poses:
        name = name_of(pose)
        paths += [CANONICAL / 'Raw' / (name + '.png'), CANONICAL / 'Clean' / (name + '.png')]
        paths += [CANONICAL / relative for relative in mask_paths(pose)]
    snapshots = {path.relative_to(PROJECT).as_posix(): digest(path) for path in paths}
    return snapshots


def require_unchanged(snapshots):
    for relative, expected in snapshots.items():
        if digest(PROJECT / relative) != expected:
            raise RuntimeError('Input changed during native composition/packing: ' + relative)


def clean_and_pack(poses, attack, snapshots):
    spec = importlib.util.spec_from_file_location('reviewed_canonical_pack_helpers', CANONICAL / 'pack_sprites.py')
    helpers = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(helpers)
    kind = 'attack' if attack else 'body'
    filename = 'attack-sprites.json' if attack else 'sprites.json'
    canonical_manifest = json.loads((CANONICAL / filename).read_bytes())
    canonical_frames = {record['name']: record for record in canonical_manifest['frames']}
    (FAMILY / 'Clean').mkdir(exist_ok=True)
    records = []
    alias_groups = {}
    removed_pixels = 0
    max_removed_alpha = 0
    for pose in poses:
        name = name_of(pose)
        canonical = canonical_frames[name]
        raw = read_rgba(FAMILY / 'Raw' / (name + '.png'))
        canonical_raw = read_rgba(CANONICAL / 'Raw' / (name + '.png'))
        canonical_clean = read_rgba(CANONICAL / 'Clean' / (name + '.png'))
        if not np.array_equal(raw[..., 3], canonical_raw[..., 3]):
            raise AssertionError('Raw alpha changed: ' + name)
        rgba = raw.copy()
        rgba[..., 3] = canonical_clean[..., 3]
        changed = raw[..., 3] != rgba[..., 3]
        if np.any(rgba[..., 3] > raw[..., 3]) or np.any(changed & (raw[..., 3] > 8)):
            raise AssertionError('Canonical pack coverage no longer matches reviewed cleanup: ' + name)
        removed_pixels += int(changed.sum())
        if changed.any():
            max_removed_alpha = max(max_removed_alpha, int(raw[..., 3][changed].max()))
        helpers.pad_rgb(rgba)
        write_png(FAMILY / 'Clean' / (name + '.png'), Image.fromarray(rgba))
        if not np.array_equal(rgba[..., 3], canonical_clean[..., 3]):
            raise AssertionError('Clean alpha changed: ' + name)
        x, bottom, width, height = canonical['rect']
        pivot_x, pivot_y = canonical['pivot']
        left, top = 288 - pivot_x * width, 288 - height + pivot_y * height
        if abs(left - round(left)) > 1e-6 or abs(top - round(top)) > 1e-6:
            raise AssertionError('Canonical pivot has subpixel origin: ' + name)
        left, top = round(left), round(top)
        bbox = (left, top, left + width, top + height)
        image_hash = sha256(rgba.tobytes()).hexdigest()
        alias = (canonical['atlas'], tuple(canonical['rect']))
        alias_groups.setdefault(alias, []).append((name, image_hash))
        records.append({'name': name, 'pose': pose, 'rgbaSha256': image_hash, 'bbox': bbox,
                        'canonical': canonical})
    divergences = [members for members in alias_groups.values() if len({item[1] for item in members}) > 1]
    # Shared Stalker rects are safe only after comparing the independently baked
    # Sleeper RGB. If any diverge, repack all frames without losing a variant.
    preserve_layout = not divergences
    manifest = deepcopy(canonical_manifest)
    manifest['compositionAlgorithm'] = ALGORITHM
    manifest['canonicalManifestSha256'] = digest(CANONICAL / filename)
    manifest['nativeMaterialRecipe'] = 'native-material-recipe.json'
    manifest['canonicalLayoutPreserved'] = preserve_layout
    manifest['atlases'] = []
    manifest['frames'] = []
    ART.mkdir(parents=True, exist_ok=True)
    placements = {}
    unique = {}
    for record in records:
        unique.setdefault(record['rgbaSha256'], record)
    if preserve_layout:
        atlas_images = [Image.new('RGBA', (4096, 4096)) for _ in canonical_manifest['atlases']]
        for members in alias_groups.values():
            name = members[0][0]
            record = next(item for item in records if item['name'] == name)
            canonical = record['canonical']
            with Image.open(FAMILY / 'Clean' / (name + '.png')) as image:
                crop = image.convert('RGBA').crop(record['bbox'])
            x, bottom, width, height = canonical['rect']
            atlas_images[canonical['atlas']].paste(crop, (x, 4096 - bottom - height))
        for record in records:
            canonical = record['canonical']
            placements[record['rgbaSha256']] = {key: canonical[key] for key in ('atlas', 'rect', 'pivot')}
    else:
        atlas_images = [Image.new('RGBA', (4096, 4096))]
        x, y, row_height = 0, 0, 0
        for image_hash, record in unique.items():
            left, top, right, lower = record['bbox']
            width, height = right - left, lower - top
            if x + width > 4096:
                x, y, row_height = 0, y + row_height, 0
            if y + height > 4096:
                atlas_images.append(Image.new('RGBA', (4096, 4096)))
                x, y, row_height = 0, 0, 0
            with Image.open(FAMILY / 'Clean' / (record['name'] + '.png')) as image:
                atlas_images[-1].paste(image.convert('RGBA').crop(record['bbox']), (x, y))
            placements[image_hash] = {'atlas': len(atlas_images) - 1,
                                      'rect': [x, 4096 - y - height, width, height],
                                      'pivot': [(288 - left) / width, (lower - 288) / height]}
            x += width
            row_height = max(row_height, height)
    require_unchanged(snapshots)
    for index, image in enumerate(atlas_images):
        path = ART / (f'{kind}-{index:02d}.png')
        write_png(path, image)
        manifest['atlases'].append({'asset': path.relative_to(PROJECT).as_posix(), 'sha256': digest(path)})
    for record in records:
        canonical = record['canonical']
        placement = {key: canonical[key] for key in ('atlas', 'rect', 'pivot')} if preserve_layout else placements[record['rgbaSha256']]
        frame = {'name': record['name'], 'phase': record['pose']['phase'], **placement}
        frame['group' if attack else 'closure'] = canonical['group' if attack else 'closure']
        manifest['frames'].append(frame)
    # Test each actual packed rectangle before publishing the manifest.
    atlas_arrays = [np.asarray(image) for image in atlas_images]
    roundtrip = []
    for frame in manifest['frames']:
        name = frame['name']
        clean = read_rgba(FAMILY / 'Clean' / (name + '.png'))
        canonical_clean = read_rgba(CANONICAL / 'Clean' / (name + '.png'))
        x, bottom, width, height = frame['rect']
        left, top = round(288 - frame['pivot'][0] * width), round(288 - height + frame['pivot'][1] * height)
        reconstructed = np.zeros((576, 576, 4), dtype=np.uint8)
        reconstructed[top:top + height, left:left + width] = atlas_arrays[frame['atlas']][4096 - bottom - height:4096 - bottom, x:x + width]
        visible = (clean[..., 3] > 0) | (reconstructed[..., 3] > 0)
        if not np.array_equal(reconstructed[visible], clean[visible]):
            raise AssertionError('Packed visible RGBA did not round trip: ' + name)
        if not np.array_equal(reconstructed[..., 3], canonical_clean[..., 3]):
            raise AssertionError('Packed alpha differs from canonical Clean: ' + name)
        roundtrip.append({'name': name, 'visibleRgbaByteExact': True, 'alphaByteExactToCanonicalClean': True,
                          'canonicalRectAndPivotExact': frame['rect'] == canonical_frames[name]['rect'] and frame['pivot'] == canonical_frames[name]['pivot'],
                          'cleanPngSha256': digest(FAMILY / 'Clean' / (name + '.png'))})
    require_unchanged(snapshots)
    write_json(FAMILY / filename, manifest)
    report = {'frames': len(records), 'uniqueImages': len(unique), 'atlases': len(atlas_images),
              'canonicalLayoutPreserved': preserve_layout, 'aliasDivergenceGroups': divergences,
              'allRawAlphaByteExactToCanonicalRaw': True, 'allCleanAndPackedAlphaByteExactToCanonicalClean': True,
              'allAtlasRoundTripVisibleRgbaByteExact': True, 'addedAlphaPixels': 0,
              'rawToCleanCanonicalTinyAlphaPixelsRemoved': removed_pixels,
              'maximumRemovedCanonicalRawAlpha': max_removed_alpha, 'newContourCleanup': False,
              'manifestSha256': digest(FAMILY / filename), 'records': roundtrip}
    write_json(FAMILY / f'{kind}-packed-roundtrip.json', report)
    write_json(FAMILY / f'{kind}-packing.json', {key: value for key, value in report.items() if key != 'records'})
    return report


def review_pages(poses, attack):
    from frame_colors import decode
    from native_material_frames import composite_background
    output = FAMILY / 'Review'
    output.mkdir(exist_ok=True)
    kind = 'attack' if attack else 'body'
    backgrounds = {'green': (0, 255, 0), 'dark': (18, 22, 26), 'light': (235, 235, 235)}
    for start in range(0, len(poses), 16):
        group = poses[start:start + 16]
        for background, color in backgrounds.items():
            sheet = Image.new('RGB', (4 * 576, ((len(group) + 3) // 4) * 608), color)
            draw = ImageDraw.Draw(sheet)
            for index, pose in enumerate(group):
                name = name_of(pose)
                with Image.open(FAMILY / 'Clean' / (name + '.png')) as image:
                    paint = decode(image.convert('RGBA'))
                x, y = index % 4 * 576, index // 4 * 608
                sheet.paste(composite_background(paint, color), (x, y + 32))
                draw.text((x + 8, y + 10), name + ' | native 224 PPU', fill='white' if background == 'dark' else 'black')
            write_png(output / f'{kind}-{start // 16:03d}-{background}.png', sheet)
    if not attack:
        for row in (0, 4, 8):
            group = [pose for pose in poses if pose['row'] == row]
            sheet = Image.new('RGB', (8 * 288, 6 * 320), (0, 255, 0))
            draw = ImageDraw.Draw(sheet)
            for index, pose in enumerate(group):
                name = name_of(pose)
                with Image.open(FAMILY / 'Clean' / (name + '.png')) as image:
                    paint = decode(image.convert('RGBA'))
                preview = composite_background(paint, (0, 255, 0)).resize((288, 288), Image.Resampling.BOX)
                x, y = index % 8 * 288, index // 8 * 320
                sheet.paste(preview, (x, y + 32))
                draw.text((x + 8, y + 10), name + ' | overview 112 PPU', fill='black')
            write_png(output / f'closure-{row}-green-overview.png', sheet)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--attack', action='store_true')
    parser.add_argument('--workers', type=int, choices=range(1, 5), default=2)
    parser.add_argument('--pack-only', action='store_true')
    args = parser.parse_args()
    kind = 'attack' if args.attack else 'body'
    poses = json.loads((CANONICAL / ('attack-poses.json' if args.attack else 'poses.json')).read_bytes())
    snapshots = snapshot_inputs(poses, args.attack)
    write_json(FAMILY / f'{kind}-native-inputs.json', snapshots)
    sys.path.insert(0, str(CANONICAL))
    from frame_colors import encode
    from native_material_frames import NativeMaterialPainter
    painter = NativeMaterialPainter()
    # All material arrays, including the mouth patch, are immutable. apply/layer
    # create local arrays and PIL images; workers share no mutable frame state.
    arrays = (*painter.painter.sources, painter.painter._torso, painter.mouth.source_patch)
    if any(array.flags.writeable for array in arrays):
        raise AssertionError('A native material source is mutable')
    (FAMILY / 'Raw').mkdir(exist_ok=True)
    (FAMILY / 'Masks').mkdir(exist_ok=True)
    composition_path = FAMILY / f'{kind}-composition.json'
    if not args.pack_only:
        composition_path.unlink(missing_ok=True)
        if not args.attack:
            torso = encode(painter.painter._torso)
            if not np.array_equal(np.asarray(torso)[..., 3], read_rgba(CANONICAL / 'Masks/torso.png')[..., 3]):
                raise AssertionError('Source torso alpha changed')
            write_png(FAMILY / 'Masks/torso.png', torso)

        def render(pose):
            name = name_of(pose)
            whole, limbs, torso, mouth = painter.paint(pose)
            image = encode(whole)
            if not np.array_equal(np.asarray(image)[..., 3], read_rgba(CANONICAL / 'Raw' / (name + '.png'))[..., 3]):
                raise AssertionError('Whole Raw alpha differs: ' + name)
            write_png(FAMILY / 'Raw' / (name + '.png'), image)
            layers = [encode(limb) for limb in limbs]
            if args.attack:
                layers += [encode(torso), encode(mouth)]
            for relative, layer in zip(mask_paths(pose), layers):
                if not np.array_equal(np.asarray(layer)[..., 3], read_rgba(CANONICAL / relative)[..., 3]):
                    raise AssertionError('Material layer alpha differs: ' + relative)
                write_png(FAMILY / relative, layer)
            return {'name': name, 'rawAlphaByteExact': True, 'allLayerAlphaBytesExact': True,
                    'rawPngSha256': digest(FAMILY / 'Raw' / (name + '.png'))}

        records = []
        with ThreadPoolExecutor(max_workers=args.workers) as executor:
            for index, record in enumerate(executor.map(render, poses)):
                records.append(record)
                if (index + 1) % 24 == 0:
                    print(f'{kind} native bake: {index + 1}/{len(poses)}', flush=True)
        require_unchanged(snapshots)
        report = {'algorithm': ALGORITHM, 'poses': len(poses), 'alpha': 'straight',
                  'workingColor': 'linear premultiplied float32', 'inputHashesUnchanged': True,
                  'wholeAndLayerAlphaByteExactToStalker': True, 'records': records}
        write_json(composition_path, report)
        write_json(FAMILY / 'native-material-recipe.json', painter.recipe)
    else:
        report = json.loads(composition_path.read_bytes())
        if report.get('algorithm') != ALGORITHM or report.get('poses') != len(poses):
            raise ValueError('A complete native material bake is required before pack-only')
        for record in report['records']:
            if digest(FAMILY / 'Raw' / (record['name'] + '.png')) != record['rawPngSha256']:
                raise ValueError('Raw PNG changed since composition: ' + record['name'])
    print(f'{kind}: all native Raw and layer alpha exact; packing', flush=True)
    packed = clean_and_pack(poses, args.attack, snapshots)
    print(json.dumps({key: value for key, value in packed.items() if key != 'records'}), flush=True)
    review_pages(poses, args.attack)
    require_unchanged(snapshots)
    print(f'{kind}: native review pages ready; all frozen inputs unchanged', flush=True)


if __name__ == '__main__':
    main()
