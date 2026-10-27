"""Record a human/model native-page review after those pages were viewed."""
from hashlib import sha256
from pathlib import Path
import argparse
import json


HERE = Path(__file__).resolve().parent
BASE = HERE.parent


def main(body, through, base_attack=False):
    family = BASE if body or base_attack else HERE
    map_path = BASE / 'body-native-unique-map.json' if body else BASE / 'base-attack-native-unique-map.json' if base_attack else HERE / 'native-unique-map.json'
    pages = BASE / 'BodyNativeUnique' if body else BASE / 'BaseAttackNativeUnique' if base_attack else HERE / 'NativeUnique'
    data = json.loads(map_path.read_bytes())
    if through < 0 or through >= data['pagesPerBackground']:
        raise ValueError('Review page index is outside the native set')
    reviewed = data['records'][:min((through + 1) * 4, len(data['records']))]
    page_hashes = {f'unique-{index:03d}-green.png': sha256((pages / f'unique-{index:03d}-green.png').read_bytes()).hexdigest()
                   for index in range(through + 1)}
    complete = through + 1 == data['pagesPerBackground']
    report = {'kind': 'cold-body-green' if body else 'cold-base-attack-green' if base_attack else 'cold-new-odd-attack-green',
              'status': 'Native green visual inspection complete' if complete else 'Native green visual inspection in progress',
              'sourcePoses': data['newPoses'], 'uniqueWholeRgbaFrames': data['uniqueRgbaFrames'],
              'nativeGreenPagesActuallyViewed': through + 1,
              'reviewedUniqueFrames': len(reviewed),
              'sourcePosesCoveredIncludingProvenExactRgbaAliases': sum(len(row['aliases']) for row in reviewed),
              'nativePageSize': [1152, 1216], 'pixelsPerUnit': 224, 'resampling': False,
              'display': 'view_image(detail=original), image(..., original); no resize_notice',
              'mapSha256': sha256(map_path.read_bytes()).hexdigest(), 'pageHashes': page_hashes,
              'findings': [],
              'criteriaActuallyInspected': ['Continuous native joint skin and mounts', 'No unexpected transparency or cut strips',
                                          'No detached or duplicated limb tissue', 'No visible holes or wrong overlap',
                                          'Native cold skin/bone color and blood remain anchored'],
              'independentDarkLightReview': 'Separate reviewer; not certified by this green pass',
              'geometryAndAlpha': 'Shared frozen Stalker geometry plus per-pose byte-exact alpha proofs; separate evidence',
              'unityRuntime': 'Not inspected by this offline producer',
              'ownerCreativeApproval': 'Not inferred'}
    target = family / ('body-native-green-review.json' if body else 'base-attack-native-green-review.json' if base_attack else 'native-green-review.json')
    target.write_text(json.dumps(report, indent=2), encoding='utf-8')
    print(json.dumps({key: value for key, value in report.items() if key not in ('pageHashes', 'criteriaActuallyInspected')}, indent=2))


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    modes = parser.add_mutually_exclusive_group()
    modes.add_argument('--body', action='store_true')
    modes.add_argument('--base-attack', action='store_true')
    parser.add_argument('--through', type=int, required=True)
    args = parser.parse_args()
    main(args.body, args.through, args.base_attack)
