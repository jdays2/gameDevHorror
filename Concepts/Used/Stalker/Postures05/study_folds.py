from pathlib import Path
import json
import sys
import numpy as np
from PIL import Image, ImageDraw

folder = Path(__file__).resolve().parent / (sys.argv[1] if len(sys.argv) > 1 else 'Study')
rows = json.loads((folder / 'poses.json').read_text())
colors = ['#ff4545', '#ff9898', '#24d5ff', '#70e5ff', '#ffdb30', '#f7f29f']
bones = Image.new('RGB', (1600, 1200), '#14212a')
paint = Image.new('RGB', (1600, 1200), '#00ff00')
for row in rows:
    index = row['sample']
    x, y = index % 4 * 400, index // 4 * 400
    skeleton = Image.new('RGB', (576, 576), '#14212a')
    draw = ImageDraw.Draw(skeleton)
    for leg, color in enumerate(colors):
        chain = [row[key][leg] for key in ['hips', 'knees', 'feet']]
        points = [(288 + p[0] * 224, 288 - p[1] * 224) for p in chain]
        draw.line(points, fill=color, width=3)
        for px, py in points:
            draw.ellipse((px-4, py-4, px+4, py+4), fill=color)
    skeleton = skeleton.crop((88, 88, 488, 488)).resize((360, 360))
    bones.paste(skeleton, (x + 20, y + 30))
    raw = np.asarray(Image.open(folder / f'fold-{index:02d}.png').convert('RGBA')).astype(np.float32) / 255
    rgb = raw[..., :3]
    rgb = np.where(rgb <= .04045, rgb / 12.92, ((rgb + .055) / 1.055) ** 2.4)
    rgb += np.array([0, 1, 0]) * (1 - raw[..., 3:4])
    rgb = np.where(rgb <= .0031308, 12.92 * rgb, 1.055 * np.maximum(rgb, 0) ** (1 / 2.4) - .055)
    image = Image.fromarray((np.clip(rgb, 0, 1) * 255).round().astype('uint8'))
    image = image.crop((88, 88, 488, 488)).resize((360, 360))
    paint.paste(image, (x + 20, y + 30))
    title = f"{index:02d} | foot height {row['footHeight']:.1f} | rise {row['rise']:.1f} | winding {row['minCross']:.5f}"
    ImageDraw.Draw(bones).text((x+5, y+5), title, fill='white')
    ImageDraw.Draw(paint).text((x+5, y+5), title, fill='black')
bones.save(folder / 'bones.png')
paint.save(folder / 'paint-green.png')
print([(r['sample'], round(r['minCross'], 5), round(min(p[2] for p in r['knees']), 3)) for r in rows])
