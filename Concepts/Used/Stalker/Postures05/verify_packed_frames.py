"""Round-trip atlas rectangles and pivots to the original fixed frame canvas."""
from pathlib import Path
import argparse
import hashlib
import json
import numpy as np
from PIL import Image

FAMILY = Path(__file__).resolve().parent
PROJECT = FAMILY.parents[3]


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--attack', action='store_true')
    parser.add_argument('--family', type=Path, default=FAMILY)
    args = parser.parse_args()
    family = args.family.resolve()
    kind = 'attack' if args.attack else 'body'
    manifest_path = family / ('attack-sprites.json' if args.attack else 'sprites.json')
    manifest = json.loads(manifest_path.read_bytes())
    atlases = []
    for record in manifest['atlases']:
        path = PROJECT / record['asset']
        if hashlib.sha256(path.read_bytes()).hexdigest() != record['sha256']:
            raise ValueError('Atlas hash mismatch: ' + str(path))
        with Image.open(path) as image:
            atlases.append(np.asarray(image.convert('RGBA')).copy())
    rows = []
    for record in manifest['frames']:
        name = record['name']
        with Image.open(family / 'Raw' / f'{name}.png') as image:
            raw = np.asarray(image.convert('RGBA'))
        with Image.open(family / 'Clean' / f'{name}.png') as image:
            clean = np.asarray(image.convert('RGBA'))
        x, bottom, width, height = record['rect']
        atlas = atlases[record['atlas']]
        top = atlas.shape[0] - bottom - height
        rect = atlas[top:top + height, x:x + width]
        pivot_x, pivot_y = record['pivot']
        left = 288 - pivot_x * width
        upper = 288 - height + pivot_y * height
        if abs(left - round(left)) > 1e-6 or abs(upper - round(upper)) > 1e-6:
            raise ValueError('Subpixel frame origin: ' + name)
        left, upper = round(left), round(upper)
        if min(left, upper) < 0 or left + width > 576 or upper + height > 576:
            raise ValueError('Reconstructed frame outside its original canvas: ' + name)
        reconstructed = np.zeros((576, 576, 4), dtype='uint8')
        reconstructed[upper:upper + height, left:left + width] = rect
        visible = (clean[..., 3] > 0) | (reconstructed[..., 3] > 0)
        if not np.array_equal(reconstructed[visible], clean[visible]):
            raise ValueError('Atlas round trip changes visible frame pixels: ' + name)
        changed = raw[..., 3] != clean[..., 3]
        if np.any(changed & (raw[..., 3] > 8)):
            raise ValueError('Packing cut visible source coverage: ' + name)
        if np.any(clean[..., 3] > raw[..., 3]):
            raise ValueError('Packing added coverage: ' + name)
        rows.append({'name': name, 'atlasRoundTripVisibleByteExact': True,
                     'alphaPixelsChanged': int(changed.sum()),
                     'maximumChangedSourceAlpha': int(raw[..., 3][changed].max()) if changed.any() else 0})
    result = {'frames': len(rows), 'manifestSha256': hashlib.sha256(manifest_path.read_bytes()).hexdigest(),
              'allAtlasRoundTripsVisibleByteExact': True, 'sourceAlphaOver8Unchanged': True,
              'packingAddedCoverage': False, 'alphaPixelsChanged': sum(r['alphaPixelsChanged'] for r in rows),
              'maximumChangedSourceAlpha': max(r['maximumChangedSourceAlpha'] for r in rows), 'records': rows}
    (family / f'{kind}-packed-roundtrip.json').write_text(json.dumps(result, indent=2))
    print(json.dumps({key: value for key, value in result.items() if key != 'records'}, indent=2))


if __name__ == '__main__':
    main()
