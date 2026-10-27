"""Compare positive-kernel mouth sampling against ringing-prone filters."""
from pathlib import Path
import json
import numpy as np
from PIL import Image, ImageDraw, ImageFilter
from attack_mouth import AttackMouth, opening, encode, SIZE, ORIGIN, PPU

FAMILY = Path(__file__).resolve().parent


def positive_layer(mouth, pose):
    amount = opening(pose)
    if amount <= 0:
        return np.zeros((SIZE, SIZE, 4), np.float32)
    width = max(1, round(SIZE * amount ** .7))
    height = max(1, round(SIZE * amount))
    offset_x = float(ORIGIN[0] * (1 - width / SIZE))
    lip_y = float(ORIGIN[1] - .30 * PPU)
    offset_y = lip_y * (1 - height / SIZE)
    channels = []
    for channel in range(4):
        resized = Image.fromarray(mouth.source_patch[..., channel]).resize((width, height), Image.Resampling.BOX)
        placed = resized.transform((SIZE, SIZE), Image.Transform.AFFINE,
            (1, 0, -offset_x, 0, 1, -offset_y), Image.Resampling.BILINEAR)
        channels.append(np.asarray(placed))
    patch = np.clip(np.stack(channels, axis=-1), 0, 1)
    patch[..., :3] = np.minimum(patch[..., :3], patch[..., 3:4])
    if amount < .35:
        growth = min(1.0, max(0.0, (amount - .15) / .20))
        growth = growth * growth * (3 - 2 * growth)
        radius = growth * 24
        low, high = int(np.floor(radius)), int(np.ceil(radius))
        lower_image = mouth.closed_coverage.filter(ImageFilter.MaxFilter(low * 2 + 1)) if low else mouth.closed_coverage
        upper_image = mouth.closed_coverage.filter(ImageFilter.MaxFilter(high * 2 + 1)) if high else mouth.closed_coverage
        lower = np.asarray(lower_image, np.float32) / 255
        upper = np.asarray(upper_image, np.float32) / 255
        patch *= (lower * (1 - (radius - low)) + upper * (radius - low))[..., None]
    return patch.astype(np.float32)


def main():
    output = FAMILY / 'MouthEdgeStudy'
    output.mkdir(exist_ok=True)
    mouth = AttackMouth()
    poses = [p for p in json.loads((FAMILY / 'attack-poses.json').read_bytes()) if p['group'] == 'strike']
    sheet = Image.new('RGB', (8 * 300, 6 * 180), '#00ff00')
    draw = ImageDraw.Draw(sheet)
    rows = []
    for index, pose in enumerate(poses):
        old, new = mouth.layer(pose), positive_layer(mouth, pose)
        for variant, layer in [('old', old), ('positive', new)]:
            encode(layer).save(output / f'{pose["name"]}-{variant}.png')
            alpha = np.asarray(encode(layer))[..., 3]
            support = np.asarray(Image.fromarray((alpha > 128).astype('uint8') * 255).filter(ImageFilter.MaxFilter(5))) > 0
            ghost = (alpha > 1) & ~support
            rows.append({'name': pose['name'], 'variant': variant,
                         'outside2pxSolid': int(ghost.sum()),
                         'maximumOutsideAlpha': int(alpha[ghost].max()) if ghost.any() else 0})
            x, y = index % 8 * 300, index // 8 * 360 + (0 if variant == 'old' else 180)
            sheet.paste(encode(layer, green=True).crop((138, 124, 438, 274)), (x, y + 25))
            draw.text((x + 5, y + 4), pose['name'] + ' ' + variant, fill='black')
    sheet.save(output / 'all24-mouth-kernels-native-green.png')
    (output / 'kernel-comparison.json').write_text(json.dumps(rows, indent=2))
    print(json.dumps({'oldOutside2': sum(r['outside2pxSolid'] for r in rows if r['variant'] == 'old'),
                      'positiveOutside2': sum(r['outside2pxSolid'] for r in rows if r['variant'] == 'positive')}, indent=2))


if __name__ == '__main__':
    main()
