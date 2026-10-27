"""Prove exact RGBA aliases and show unique new poses in native 2x2 pages."""
from hashlib import sha256
from pathlib import Path
import argparse
import json
import sys

import numpy as np
from PIL import Image, ImageDraw


HERE = Path(__file__).resolve().parent
BASE = HERE.parent
PROJECT = BASE.parents[3]
WARM = PROJECT / 'Concepts/Used/Stalker/Postures05/AttackAdjacent01'
sys.path.insert(0, str(BASE))
sys.path.insert(0, str(WARM.parent))
from frame_colors import decode
from native_material_frames import composite_background


def main(body=False, base_attack=False):
    source_family = BASE if body or base_attack else HERE
    if body:
        poses = json.loads((WARM.parent / 'poses.json').read_bytes())
    elif base_attack:
        poses = json.loads((WARM.parent / 'attack-poses.json').read_bytes())
    else:
        poses = [pose for pose in json.loads((WARM / 'attack-poses.json').read_bytes())
                 if pose['group'] != 'strike' and pose['sample'] % 2 == 1]
    groups, order = {}, []
    for pose in poses:
        name = pose.get('name', f'body-{pose.get("row", 0)}-{pose["phase"]:02d}')
        path = source_family / 'Raw' / (name + '.png')
        with Image.open(path) as image:
            rgba = np.asarray(image.convert('RGBA')).copy()
        rgba_hash = sha256(rgba.tobytes()).hexdigest()
        if rgba_hash not in groups:
            groups[rgba_hash] = {'representative': name, 'aliases': [], 'rgbaSha256': rgba_hash}
            order.append(rgba_hash)
        else:
            with Image.open(source_family / 'Raw' / (groups[rgba_hash]['representative'] + '.png')) as image:
                representative = np.asarray(image.convert('RGBA'))
            if not np.array_equal(rgba, representative):
                raise AssertionError('RGBA hash alias did not match exactly')
        groups[rgba_hash]['aliases'].append(name)
    records = [groups[key] for key in order]
    output = BASE / 'BodyNativeUnique' if body else BASE / 'BaseAttackNativeUnique' if base_attack else HERE / 'NativeUnique'
    output.mkdir(exist_ok=True)
    backgrounds = {'green': (0, 255, 0), 'dark': (18, 22, 26), 'light': (235, 235, 235)}
    if base_attack:
        backgrounds = {'green': backgrounds['green']}
    for start in range(0, len(records), 4):
        subset = records[start:start + 4]
        for background, color in backgrounds.items():
            sheet = Image.new('RGB', (1152, 1216), color)
            draw = ImageDraw.Draw(sheet)
            for index, record in enumerate(subset):
                with Image.open(source_family / 'Raw' / (record['representative'] + '.png')) as image:
                    paint = decode(image.convert('RGBA'))
                x, y = index % 2 * 576, index // 2 * 608
                sheet.paste(composite_background(paint, color), (x, y + 32))
                draw.text((x + 8, y + 10), record['representative'] + f' | 224PPU | exact aliases {len(record["aliases"])}',
                          fill='white' if background == 'dark' else 'black')
            sheet.save(output / f'unique-{start // 4:03d}-{background}.png', compress_level=3)
    result = {'newPoses': len(poses), 'kind': 'body' if body else 'base-attack' if base_attack else 'new-odd-attack', 'uniqueRgbaFrames': len(records),
              'duplicatesProvenByWholeRgbaSha256AndArrayEquality': len(poses) - len(records),
              'nativePpu': 224, 'pageSizePixels': [1152, 1216], 'resampling': False,
              'pagesPerBackground': (len(records) + 3) // 4, 'backgrounds': list(backgrounds),
              'records': records}
    map_path = BASE / 'body-native-unique-map.json' if body else BASE / 'base-attack-native-unique-map.json' if base_attack else HERE / 'native-unique-map.json'
    map_path.write_text(json.dumps(result, indent=2), encoding='utf-8')
    print(json.dumps({key: value for key, value in result.items() if key != 'records'}), flush=True)


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    modes = parser.add_mutually_exclusive_group()
    modes.add_argument('--body', action='store_true')
    modes.add_argument('--base-attack', action='store_true')
    args = parser.parse_args()
    main(args.body, args.base_attack)
