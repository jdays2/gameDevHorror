from pathlib import Path
import json
import math
import numpy as np
from PIL import Image, ImageDraw
import compose_parts as paint

folder = Path(__file__).resolve().parent / 'StudyTarget'
(folder / 'Raw').mkdir(parents=True, exist_ok=True)
(folder / 'Masks').mkdir(exist_ok=True)
template = json.loads((folder.parent.parent / 'Postures04/poses.json').read_text())[0]
sheet = Image.new('RGB', (1200, 800), '#00ff00')
bones = Image.new('RGB', (1200, 800), '#14212a')
rows = []
for index, spread in enumerate([.08, .10, .14, .18, .22, .26]):
    pose = {'sample': index, 'spread': spread, 'hips': [], 'knees': [], 'feet': []}
    for leg, limb in enumerate(paint.anatomy['limbs']):
        side = -1 if leg % 2 == 0 else 1
        direction = 1 if leg < 2 else -1
        hip = np.array(template['hips'][leg])
        hip[2] = .2
        knee = hip + [side * spread, direction * .17, 0]
        foot = hip + [-side * .12, direction * .08, 0]
        if 2 <= leg < 4:
            knee = hip + [side * (spread + .02), -.03, 0]
            foot = hip + [-side * .08, -.16, 0]
        knee[2] += math.sqrt(limb['upperLength']**2 - np.sum((knee[:2]-hip[:2])**2))
        foot[2] = knee[2] - math.sqrt(limb['lowerLength']**2 - np.sum((foot[:2]-knee[:2])**2))
        pose['hips'].append(hip.tolist())
        pose['knees'].append(knee.tolist())
        pose['feet'].append(foot.tolist())
    image = paint.compose(pose, folder, f'fold-{index}', False)
    image = image.crop((88, 88, 488, 488)).resize((360, 360))
    x, y = index % 3 * 400, index // 3 * 400
    sheet.paste(image, (x+20, y+30))
    ImageDraw.Draw(sheet).text((x+10, y+10), f'{index} | spread {spread}', fill='black')
    draw = ImageDraw.Draw(bones)
    for leg in range(6):
        points = [(x+200+p[0]*224*.9, y+210-p[1]*224*.9) for p in [pose['hips'][leg], pose['knees'][leg], pose['feet'][leg]]]
        draw.line(points, fill=['red', 'pink', 'cyan', 'blue', 'yellow', 'orange'][leg], width=3)
        for px, py in points:
            draw.ellipse((px-4, py-4, px+4, py+4), fill='white')
    draw.text((x+10, y+10), f'{index} | spread {spread}', fill='white')
    rows.append(pose)
sheet.save(folder / 'paint-green.png')
bones.save(folder / 'bones.png')
(folder / 'poses.json').write_text(json.dumps(rows))
