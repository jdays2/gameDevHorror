import json
from PIL import Image, ImageDraw
from prepare_cold_adjacent_review import SOURCE, OUT, BACKGROUNDS, sha, composite

path = OUT / 'review-coverage.json'
data = json.loads(path.read_text())
crop = (132, 63, 446, 520)
pages = []
for background, colour in BACKGROUNDS.items():
    folder = OUT / background
    folder.mkdir(exist_ok=True)
    for page in range(58):
        records = data['records'][page * 8:page * 8 + 8]
        sheet = Image.new('RGB', (1304, 978), colour)
        draw = ImageDraw.Draw(sheet)
        for index, rec in enumerate(records):
            with Image.open(SOURCE / 'Raw' / (rec['representative'] + '.png')) as im:
                rgba = im.convert('RGBA')
            box = rgba.getchannel('A').getbbox()
            assert box and box[0] >= crop[0] and box[1] >= crop[1] and box[2] <= crop[2] and box[3] <= crop[3], (rec['representative'], box)
            x, y = index % 4 * 326 + 6, index // 4 * 489 + 26
            native = composite(rgba, colour).crop(crop)
            sheet.paste(native, (x, y))
            draw.text((x, y - 18), rec['representative'], fill=(190, 190, 190) if background == 'dark' else (30, 30, 30))
            assert sheet.crop((x, y, x+314, y+457)).tobytes() == native.tobytes()
        output = folder / f'page-{page:03d}.png'
        sheet.save(output)
        pages.append({'background': background, 'page': page, 'path': str(output), 'sha256': sha(output),
                      'representatives': [r['representative'] for r in records],
                      'aliases': [n for r in records for n in r['aliases']], 'viewed': False,
                      'nativePayloadIndependentlyExactRawComposite': True})
data['originalNativeUniquePages'] = data.pop('pages')
data['pages'] = pages
data['pageSizePixels'] = [1304, 978]
data['fixedCropPixels'] = crop
data['fixedCropAllNonzeroAlphaInside'] = True
data['resampling'] = False
path.write_text(json.dumps(data, indent=2))
print('Prepared 116 pages; native pixels; 461 unique x 2 backgrounds; all nonzero alpha inside fixed crop.')
