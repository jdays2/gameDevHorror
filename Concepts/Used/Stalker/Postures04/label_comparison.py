"""Label the unchanged Unity camera capture for the owner's visual review."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

folder = Path(__file__).resolve().parent
image = Image.open(folder / 'pose-comparison.png').convert('RGB')
draw = ImageDraw.Draw(image)
font = ImageFont.truetype('C:/Windows/Fonts/arial.ttf', 27)
small = ImageFont.truetype('C:/Windows/Fonts/arial.ttf', 20)
draw.text((44, 62), 'Один масштаб и камера. Первые две позы — с одинаково открытым глазом.', font=font, fill='black')
labels = [
    (260, 'Обычная походка', 'Сталкер'),
    (620, 'Лапы сложены', 'Проверка тела при открытом глазе'),
    (980, 'Поджатая походка', 'Отступление'),
    (1340, 'Спящий', 'Фактический кадр сна Recoil03'),
]
for x, title, detail in labels:
    draw.text((x, 720), title, font=font, anchor='mt', fill='black')
    draw.text((x, 760), detail, font=small, anchor='mt', fill='black')
image.save(folder / 'pose-comparison-labelled.png')
