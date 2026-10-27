"""Pack the dense adjacent family, preserving all Raw visible RGBA and alpha."""
from pathlib import Path
import hashlib
import json
import sys
import numpy as np
from PIL import Image

HERE = Path(__file__).resolve().parent
FAMILY = HERE.parent
PROJECT = FAMILY.parents[3]
ART = PROJECT/'Assets/EmergencyShift/Art/Enemies/Stalker/Postures05/AttackAdjacent01'
sys.path.insert(0, str(FAMILY))
from pack_sprites import components, pad_rgb


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    assert not (HERE/'attack-sprites.json').exists(), 'Completed packing is immutable'
    # A failed local packing attempt may have saved a verified atlas already.
    # Resume only this family's own staged files, never unrelated assets.
    if ART.exists():
        for target in ART.glob('*.png'):
            staged = HERE/'AtlasStaging'/target.name
            assert staged.exists() and digest(staged) == digest(target), target
    composition_sha = digest(HERE/'attack-composition.json')
    composition = json.loads((HERE/'attack-composition.json').read_text())
    assert composition['poses'] == 1464 and composition['copiedRawAndMasksByteExact']
    assert composition['posesSha256'] == digest(HERE/'attack-poses.json')
    assert composition['painterSha256'] == digest(FAMILY/'native_frame_painter.py')
    assert composition['composerSha256'] == digest(HERE/'compose_adjacent.py')
    qa = json.loads((HERE/'QA/summary.json').read_text())
    assert qa['compositionSha256'] == composition_sha
    for key in ['nonpositiveJacobianPixels', 'wholeBorderPositiveAlphaPixels', 'erode4AlphaBelow230Pixels',
                'disconnectedLegFramesAlphaOver4', 'disconnectedWholeFramesAlphaOver4']:
        assert qa[key] == 0, (key, qa[key])
    snapshots = {'packingScriptSha256': digest(HERE/'pack_adjacent.py'),
                 'canonicalPackingHelperSha256': digest(FAMILY/'pack_sprites.py'),
                 'compositionSha256': composition_sha, 'numericQaSha256': digest(HERE/'QA/summary.json')}
    poses = json.loads((HERE/'attack-poses.json').read_text())
    clean_folder = HERE/'Clean'
    clean_folder.mkdir(exist_ok=True)
    staging = HERE/'AtlasStaging'
    staging.mkdir(exist_ok=True)
    images, unique, records = [], {}, []
    for index, pose in enumerate(poses):
        name = pose['name']
        path = HERE/'Raw'/f'{name}.png'
        assert digest(path) == composition['rawPngSha256'][name]
        with Image.open(path) as image:
            raw = np.asarray(image.convert('RGBA')).copy()
        rgba = raw.copy()
        alpha = raw[..., 3]
        assert not alpha[[0, -1]].any() and not alpha[:, [0, -1]].any(), name
        groups = components(alpha > 8)
        assert len(groups) == 1, (name, 'Repair actual source artwork; no contour repair in packer', len(groups))
        # The canonical helper supplies texture-filter RGB padding, not coverage.
        # Restore every alpha-positive native pixel, including alpha1..8; only
        # hidden alpha0 RGB may change. No erode/fill/disconnected-pixel cleanup.
        visible = alpha > 0
        rgba[~visible, :3] = 0
        pad_rgb(rgba)
        rgba[visible] = raw[visible]
        assert np.array_equal(rgba[..., 3], alpha)
        assert np.array_equal(rgba[visible], raw[visible])
        image = Image.fromarray(rgba)
        clean_path = clean_folder/f'{name}.png'
        if clean_path.exists():
            assert np.array_equal(np.asarray(Image.open(clean_path).convert('RGBA')), rgba), name
        else:
            image.save(clean_path, compress_level=3)
        x0, y0, x1, y1 = Image.fromarray(alpha).getbbox()
        bbox = (max(0, x0-3), max(0, y0-3), min(576, x1+3), min(576, y1+3))
        sha = hashlib.sha256(rgba.tobytes()).hexdigest()
        if sha not in unique:
            unique[sha] = len(images)
            images.append((image.crop(bbox), bbox))
        records.append({'name': name, 'group': pose['group'], 'phase': pose['phase'], 'sample': pose['sample'],
            'unique': unique[sha], 'cleanRgbaSha256': sha, 'rawAlphaByteExact': True, 'rawVisibleRgbaByteExact': True})
        if (index+1)%96 == 0:
            print(f'Contour-preserving clean frames: {index+1}/1464', flush=True)
    ART.mkdir(parents=True, exist_ok=True)
    manifest = {'phases': 48, 'closureRows': 9, 'pixelsPerUnit': 224, 'alpha': 'straight', 'kind': 'attack',
        'bridgeFrames': 15, 'strikeFrames': 24, 'entryFraction': .15, 'returnFraction': .85,
        'returnCompleteFraction': .97, 'posesSha256': composition['posesSha256'],
        'cycleDistance': 1.25, 'compactStrideScale': .8,
        'compositionAlgorithm': composition['algorithm'], 'compositionSha256': composition_sha,
        'frames': [], 'atlases': [], 'rawVisibleRgbaByteExact': True, 'rawAlphaByteExact': True,
        'artGate': 'Separate new-frame green/dark/light reports; packing is not artistic approval.'}
    placements = []
    atlas_index, x, y, row_height = 0, 0, 0, 0
    atlas = Image.new('RGBA', (4096, 4096))
    def save_atlas():
        staged = staging/f'attack-{atlas_index:02d}.png'
        atlas.save(staged, compress_level=6)
        target = ART/staged.name
        if target.exists():
            assert digest(target) == digest(staged), target
        else:
            target.write_bytes(staged.read_bytes())
        manifest['atlases'].append({'asset': target.relative_to(PROJECT).as_posix(), 'sha256': digest(target)})
    for image, bbox in images:
        if x+image.width > 4096:
            x, y, row_height = 0, y+row_height, 0
        if y+image.height > 4096:
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
        manifest['frames'].append({'name': record['name'], 'phase': record['phase'], 'group': record['group'],
            **placements[record['unique']]})
    assert digest(HERE/'attack-composition.json') == composition_sha
    assert digest(HERE/'pack_adjacent.py') == snapshots['packingScriptSha256']
    assert digest(FAMILY/'pack_sprites.py') == snapshots['canonicalPackingHelperSha256']
    (HERE/'attack-sprites.json').write_text(json.dumps(manifest, indent=2))
    report = {'frames': len(records), 'uniqueImages': len(images), 'atlases': len(manifest['atlases']),
        'manifestSha256': digest(HERE/'attack-sprites.json'), **snapshots, 'rawAlphaPixelsChanged': 0,
        'rawVisibleRgbaByteDifferences': 0, 'contourMorphology': False, 'holeFilling': False,
        'detachedPixelsRemoved': 0, 'rgbPaddingScope': 'Hidden alpha0 RGB only', 'records': records}
    (HERE/'attack-packing.json').write_text(json.dumps(report, indent=2))
    print(json.dumps({key: value for key, value in report.items() if key != 'records'}, indent=2), flush=True)


if __name__ == '__main__':
    main()
