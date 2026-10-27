"""Explain why native-color bridge frames cannot share every warm-color rect."""
from pathlib import Path
import json

import numpy as np
from PIL import Image


FAMILY = Path(__file__).resolve().parent
CANONICAL = FAMILY.parents[3] / 'Concepts/Used/Stalker/Postures05'


def main():
    packing = json.loads((FAMILY / 'attack-packing.json').read_bytes())
    poses = {pose['name']: pose for pose in json.loads((CANONICAL / 'attack-poses.json').read_bytes())}
    rows = []
    for members in packing['aliasDivergenceGroups']:
        first_name = members[0][0]
        with Image.open(FAMILY / 'Clean' / (first_name + '.png')) as image:
            first = np.asarray(image.convert('RGBA'))
        for name, unused_hash in members[1:]:
            with Image.open(FAMILY / 'Clean' / (name + '.png')) as image:
                other = np.asarray(image.convert('RGBA'))
            difference = np.abs(first[..., :3].astype(np.int16) - other[..., :3].astype(np.int16))
            visible = (first[..., 3] > 0) | (other[..., 3] > 0)
            changed = np.any(difference > 0, axis=-1)
            coordinate_error = max(float(np.max(np.abs(np.asarray(poses[first_name][key]) - np.asarray(poses[name][key])))) for key in ('hips', 'knees', 'feet'))
            rows.append({'names': [first_name, name], 'alphaBytesIdentical': bool(np.array_equal(first[..., 3], other[..., 3])),
                         'visibleRgbPixelsDifferent': int((visible & changed).sum()),
                         'maximumVisibleRgbByteDifference': int(difference[visible].max()),
                         'alphaZeroRgbPixelsDifferent': int((~visible & changed).sum()),
                         'maximumGeometryCoordinateDifference': coordinate_error})
    result = {'pairs': rows, 'policy': 'Keep each independently rendered native-color frame; do not overwrite an alias even when a quantization difference is small',
              'independentPackingUsed': True}
    (FAMILY / 'attack-alias-differences.json').write_text(json.dumps(result, indent=2), encoding='utf-8')
    print(json.dumps(result), flush=True)


if __name__ == '__main__':
    main()
