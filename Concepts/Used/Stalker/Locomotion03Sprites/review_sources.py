import json
from pathlib import Path
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[4]
OUT = Path(__file__).resolve().parent
data = json.loads((OUT / 'inspection-before.json').read_text())['groups']
sheet = Image.new('RGB', (1280, 1000), '#00ff00')
draw = ImageDraw.Draw(sheet)
for row, group in enumerate(['walkFrames', 'recoilFrames', 'attackFrames', 'deathFrames', 'eyeFrames']):
    for index, frame in enumerate(data[group]):
        texture = Image.open(ROOT / frame['texture']).convert('RGBA')
        x, y, w, h = map(round, frame['rect'])
        image = texture.crop((x, texture.height-y-h, x+w, texture.height-y))
        ppu = frame['pixelsPerUnit']
        scale = 100 / ppu
        image = image.resize((round(w*scale), round(h*scale)), Image.Resampling.LANCZOS)
        px, py = frame['pivot']
        at = (index*160+80-round(px*scale), row*200+110-round((h-py)*scale))
        sheet.paste(image, at, image)
        draw.text((index*160+5,row*200+5), group.replace('Frames','')+str(index), fill='black')
        draw.text((index*160+5,row*200+183), f'PPU {ppu:.0f}', fill='black')
sheet.save(OUT/'sources-green.jpg', quality=96)
