from pathlib import Path
import hashlib
import json
import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont
from prepare_adjacent_dark_light import SOURCE, OUT, COMPOSITION_SHA, sha, composite, BACKGROUNDS

names = ['entry-31-01', 'return-31-13', 'entry-20-09', 'return-20-05', 'entry-03-03', 'return-03-11']
composition = json.loads((SOURCE / 'attack-composition.json').read_text())
assert sha(SOURCE / 'attack-composition.json') == COMPOSITION_SHA
coverage = json.loads((OUT / 'review-coverage.json').read_text())
box = coverage['fixedNativeCrop']
width, height = box[2] - box[0], box[3] - box[1]
font = ImageFont.truetype('C:/Windows/Fonts/arial.ttf', 16)
records = []
for name in names:
    path = SOURCE / 'Raw' / f'{name}.png'
    assert sha(path) == composition['rawPngSha256'][name]
    whole = np.asarray(Image.open(path).convert('RGBA'))
    torso = np.asarray(Image.open(SOURCE / 'Masks' / f'torso-{name}.png').convert('RGBA'))[..., 3]
    alphas = [np.asarray(Image.open(SOURCE / 'Masks' / f'leg-{name}-{leg}.png').convert('RGBA'))[..., 3] for leg in range(6)]
    contact = (alphas[1] > 128) & (alphas[3] > 128) & (torso <= 128)
    yy, xx = np.nonzero(contact)
    weak = []
    for leg, alpha in enumerate(alphas):
        dilation = np.asarray(Image.fromarray((alpha > 128).astype('uint8') * 255).filter(ImageFilter.MaxFilter(5))) > 128
        wy, wx = np.nonzero((alpha > 0) & (alpha <= 6) & ~dilation)
        weak.extend({'leg': leg, 'xy': [int(x), int(y)], 'legAlpha': int(alpha[y, x]), 'wholeRgba': whole[y, x].tolist()} for y, x in zip(wy, wx))
    records.append({'name': name, 'rawSha256': sha(path), 'proximalRfRmContactPixels': int(contact.sum()),
                    'contactBbox': [int(xx.min()), int(yy.min()), int(xx.max()) + 1, int(yy.max()) + 1] if len(xx) else None,
                    'faintMaskPixelsOutside2pxSolidDilation': weak})

for page in range(2):
    sheet = Image.new('RGB', ((width + 12) * 2, (height + 32) * 3), '#888888')
    draw = ImageDraw.Draw(sheet)
    for row, name in enumerate(names[page * 3:page * 3 + 3]):
        im = Image.open(SOURCE / 'Raw' / f'{name}.png').convert('RGBA')
        for col, (background, colour) in enumerate(BACKGROUNDS.items()):
            x, y = col * (width + 12), row * (height + 32)
            draw.rectangle((x, y, x + width + 11, y + height + 31), fill=colour)
            sheet.paste(composite(im, colour).crop(box), (x, y + 32))
            draw.text((x + 4, y + 7), name + ' | ' + background + ' native1x', font=font,
                      fill='white' if background == 'Dark' else 'black')
    sheet.save(OUT / f'flagged-native-{page}.png')

root_box = (280, 216, 370, 318)
scale = 4
cw, ch = (root_box[2] - root_box[0]) * scale, (root_box[3] - root_box[1]) * scale
sheet = Image.new('RGB', ((cw + 12) * 3, (ch + 32) * 2), '#888888')
draw = ImageDraw.Draw(sheet)
for row, name in enumerate(names[:2]):
    im = Image.open(SOURCE / 'Raw' / f'{name}.png').convert('RGBA')
    for col, (background, colour) in enumerate(BACKGROUNDS.items()):
        x, y = col * (cw + 12), row * (ch + 32)
        sheet.paste(composite(im, colour).crop(root_box).resize((cw, ch), Image.Resampling.NEAREST), (x, y + 32))
        draw.text((x + 4, y + 7), name + ' ' + background + ' nearest4x', font=font, fill='white')
    a = np.asarray(Image.open(SOURCE / 'Masks' / f'leg-{name}-1.png').convert('RGBA'))[..., 3]
    b = np.asarray(Image.open(SOURCE / 'Masks' / f'leg-{name}-3.png').convert('RGBA'))[..., 3]
    torso = np.asarray(Image.open(SOURCE / 'Masks' / f'torso-{name}.png').convert('RGBA'))[..., 3]
    mask = np.zeros((576, 576, 3), dtype=np.uint8)
    mask[a > 128] = [230, 120, 30]
    mask[b > 128] = [30, 190, 240]
    mask[(a > 128) & (b > 128) & (torso <= 128)] = [255, 0, 255]
    x, y = 2 * (cw + 12), row * (ch + 32)
    sheet.paste(Image.fromarray(mask).crop(root_box).resize((cw, ch), Image.Resampling.NEAREST), (x, y + 32))
    draw.text((x + 4, y + 7), name + ' actual21px overlap', font=font, fill='white')
sheet.save(OUT / 'proximal-contact-nearest4x.png')
result = {'compositionSha256': COMPOSITION_SHA, 'records': records,
          'contextImages': {p.name: sha(p) for p in OUT.glob('flagged-native-*.png')},
          'nearestContextSha256': sha(OUT / 'proximal-contact-nearest4x.png'),
          'status': 'Prepared; actual review recorded separately. No mask exclusions or source repairs.'}
(OUT / 'flag-evidence.json').write_text(json.dumps(result, indent=2))
print(json.dumps({'contacts': [(r['name'], r['proximalRfRmContactPixels']) for r in records],
                  'weak': [(r['name'], len(r['faintMaskPixelsOutside2pxSolidDilation'])) for r in records]}))
