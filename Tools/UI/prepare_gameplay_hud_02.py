"""Original flat UI glyphs matching the authored concept; no reference screenshot pixels."""
from pathlib import Path
from PIL import Image, ImageDraw

root = Path(__file__).resolve().parents[2]
output = root / 'Assets/EmergencyShift/Art/UI/GameplayHud02'
output.mkdir(parents=True, exist_ok=True)
scale = 4


def polygon(name, points, size=(100, 50)):
    image = Image.new('RGBA', (size[0] * scale, size[1] * scale))
    draw = ImageDraw.Draw(image)
    draw.polygon([(round(x * scale), round(y * scale)) for x, y in points], fill='white')
    image.resize(size, Image.Resampling.LANCZOS).save(output / (name + '.png'))


polygon('pistol', [(3, 5), (88, 5), (92, 10), (97, 10), (97, 20), (54, 20), (50, 29), (40, 31), (33, 46), (13, 46), (18, 21), (3, 21)])
polygon('shotgun', [(1, 9), (86, 9), (86, 5), (92, 5), (92, 9), (99, 9), (99, 17), (59, 17), (59, 23), (43, 23), (32, 35), (26, 35), (22, 27), (1, 47)])
image = Image.new('RGBA', (64 * scale, 64 * scale))
draw = ImageDraw.Draw(image)
draw.polygon([(19 * scale, 8 * scale), (45 * scale, 8 * scale), (41 * scale, 23 * scale), (38 * scale, 26 * scale), (38 * scale, 55 * scale), (26 * scale, 55 * scale), (26 * scale, 26 * scale), (23 * scale, 23 * scale)], fill='white')
draw.rectangle((22 * scale, 10 * scale, 42 * scale, 14 * scale), fill=(0, 0, 0, 0))
draw.rectangle((29 * scale, 29 * scale, 35 * scale, 37 * scale), fill=(0, 0, 0, 0))
image.resize((64, 64), Image.Resampling.LANCZOS).save(output / 'flashlight.png')
Image.new('RGBA', (4, 4), 'white').save(output / 'white.png')
print(output)
