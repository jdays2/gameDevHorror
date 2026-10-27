"""Preview all newly added Raw poses before the strict pack is published."""
from pathlib import Path
import json
import sys

from PIL import Image, ImageDraw


HERE = Path(__file__).resolve().parent
BASE = HERE.parent
PROJECT = BASE.parents[3]
WARM = PROJECT / 'Concepts/Used/Stalker/Postures05/AttackAdjacent01'
sys.path.insert(0, str(BASE))
sys.path.insert(0, str(WARM.parent))

from frame_colors import decode
from native_material_frames import composite_background


def main():
    composition = json.loads((HERE / 'native-composition.json').read_bytes())
    poses = [pose for pose in json.loads((WARM / 'attack-poses.json').read_bytes())
             if pose['group'] != 'strike' and pose['sample'] % 2 == 1]
    if composition['poses'] != 1464 or len(poses) != 672:
        raise AssertionError('Complete adjacent material bake is required')
    output = HERE / 'Review'
    output.mkdir(exist_ok=True)
    backgrounds = {'green': (0, 255, 0), 'dark': (18, 22, 26), 'light': (235, 235, 235)}
    for start in range(0, len(poses), 16):
        group = poses[start:start + 16]
        for background, color in backgrounds.items():
            sheet = Image.new('RGB', (2304, ((len(group) + 3) // 4) * 608), color)
            draw = ImageDraw.Draw(sheet)
            for index, pose in enumerate(group):
                with Image.open(HERE / 'Raw' / (pose['name'] + '.png')) as image:
                    paint = decode(image.convert('RGBA'))
                x, y = index % 4 * 576, index // 4 * 608
                sheet.paste(composite_background(paint, color), (x, y + 32))
                draw.text((x + 8, y + 10), pose['name'] + ' | native224PPU | new odd pose', fill='white' if background == 'dark' else 'black')
            temporary = output / f'new-odd-{start // 16:03d}-{background}.writing.png'
            sheet.save(temporary, compress_level=3)
            temporary.replace(output / f'new-odd-{start // 16:03d}-{background}.png')
    report = {'poses': 672, 'pages': 42, 'backgrounds': list(backgrounds), 'nativePpu': 224,
              'source': 'Raw; strict pack is required to preserve every visible RGBA byte',
              'order': [pose['name'] for pose in poses]}
    (HERE / 'review-pages.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
    print('672 native Raw poses ready on 42 pages per background', flush=True)


if __name__ == '__main__':
    main()
