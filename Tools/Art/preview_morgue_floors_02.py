"""Copy original imagegen outputs unchanged and assemble labelled floor previews."""
from pathlib import Path
import hashlib
import json
import math
import shutil

import numpy as np
from PIL import Image, ImageDraw, ImageFont


ROOT = Path(__file__).resolve().parents[2]
FAMILY = ROOT / 'Concepts/Used/Environment/MorgueFloors02'


def main():
    specification = json.loads((FAMILY / 'prompts.json').read_text(encoding='utf-8'))
    (FAMILY / 'Textures').mkdir(exist_ok=True)
    records = []
    font = lambda size: ImageFont.truetype('C:/Windows/Fonts/arial.ttf', size)
    side, gap, header, row = 350, 16, 92, 410
    columns = 4
    count = len(specification['entries'])
    rows = math.ceil(count / columns)
    board = Image.new('RGB', (side * columns + gap * (columns + 1), header + row * rows), '#192023')
    draw = ImageDraw.Draw(board)
    draw.text((gap, 14), f'ПОЛЫ БОЛЬНИЦЫ · {count} НОВЫХ ОСНОВ', font=font(29), fill='#e1e4de')
    draw.text((gap, 53), 'На каждом образце условно 6 × 6 игровых единиц · без декалей', font=font(20), fill='#acb8b4')
    repeat_side = 350
    repeats = Image.new('RGB', (repeat_side * columns + gap * (columns + 1), 90 + (repeat_side + 64) * rows), '#192023')
    repeat_draw = ImageDraw.Draw(repeats)
    repeat_draw.text((gap, 14), 'ПРОВЕРКА ПОВТОРА · 2 × 2', font=font(28), fill='#e1e4de')
    repeat_draw.text((gap, 51), 'Исходники без исправления швов; окончательная бесшовность не утверждена', font=font(19), fill='#acb8b4')
    for index, entry in enumerate(specification['entries']):
        source = Path(entry['source'])
        destination = FAMILY / 'Textures' / (entry['id'] + '.png')
        shutil.copy2(source, destination)
        assert source.read_bytes() == destination.read_bytes()
        original = Image.open(destination)
        assert original.width == original.height
        assert original.convert('RGBA').getchannel('A').getextrema() == (255, 255)
        image = original.convert('RGB')
        x = gap + (index % columns) * (side + gap)
        y = header + (index // columns) * row
        board.paste(image.resize((side, side), Image.Resampling.LANCZOS), (x, y))
        label = f'{index + 1:02}. {entry["label"]}'.replace(' · ', '\n')
        draw.multiline_text((x, y + side + 6), label, font=font(18), fill='#e1e4de', spacing=2)
        repeat_tile = image.resize((repeat_side // 2, repeat_side // 2), Image.Resampling.LANCZOS)
        rx = gap + (index % columns) * (repeat_side + gap)
        ry = 90 + (index // columns) * (repeat_side + 64)
        for dy in [0, repeat_side // 2]:
            for dx in [0, repeat_side // 2]:
                repeats.paste(repeat_tile, (rx + dx, ry + dy))
        repeat_draw.multiline_text((rx, ry + repeat_side + 8), label, font=font(18), fill='#e1e4de', spacing=2)
        pixels = np.asarray(image, dtype=np.float32)
        seams = {'oppositeLeftRightMeanDifference': round(float(np.mean(abs(pixels[:, 0] - pixels[:, -1]))), 2),
                 'oppositeTopBottomMeanDifference': round(float(np.mean(abs(pixels[0] - pixels[-1]))), 2)}
        records.append({'id': entry['id'], 'label': entry['label'], 'file': 'Textures/' + destination.name,
                        'sourceFile': source.name, 'pixels': list(image.size), 'mode': original.mode,
                        'sha256': hashlib.sha256(destination.read_bytes()).hexdigest(), 'edgeDiagnostics': seams})
    board.save(FAMILY / 'overview.png')
    repeats.save(FAMILY / 'repeat-check.png')
    metadata_path = FAMILY / 'provenance.json'
    metadata = json.loads(metadata_path.read_text(encoding='utf-8')) if metadata_path.exists() else {}
    metadata.setdefault('status', 'Source library studies pending owner evaluation; not imported into Unity')
    metadata.setdefault('unityChanged', False)
    metadata.update({'date': '2026-10-18', 'method': 'Built-in imagegen; original PNG bytes copied unchanged',
                'textureCoverageUnits': [6, 6],
                'entries': records, 'promptSet': 'prompts.json', 'previewProcessor': 'Tools/Art/preview_morgue_floors_02.py',
                'seamlessness': 'Preview 2x2 and numerical edge diagnostics only, no production guarantee'})
    metadata_path.write_text(json.dumps(metadata, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    print(json.dumps({'newFloors': len(records), 'sourceBytesPreserved': True}))


if __name__ == '__main__':
    main()
