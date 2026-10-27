import hashlib
import json
from PIL import Image, ImageDraw
from prepare_cold_adjacent_review import SOURCE as ATTACK, BACKGROUNDS, sha, composite

SOURCE = ATTACK.parent
OUT = __import__('pathlib').Path(__file__).resolve().parent / 'ColdBodyFinal01'
OUT.mkdir(exist_ok=True)
comp_path = SOURCE / 'body-composition.json'
map_path = SOURCE / 'body-native-unique-map.json'
comp_sha, map_sha = sha(comp_path), sha(map_path)
comp = json.loads(comp_path.read_text())
mapping = json.loads(map_path.read_text())
assert comp['poses'] == mapping['newPoses'] == 432 and mapping['uniqueRgbaFrames'] == 385
hashes = {r['name']: r['rawPngSha256'] for r in comp['records']}
records = []
for rec in mapping['records']:
    with Image.open(SOURCE / 'Raw' / (rec['representative'] + '.png')) as im:
        rep = im.convert('RGBA').tobytes()
    assert hashlib.sha256(rep).hexdigest() == rec['rgbaSha256']
    for name in rec['aliases']:
        path = SOURCE / 'Raw' / (name + '.png')
        assert sha(path) == hashes[name]
        with Image.open(path) as im:
            assert im.convert('RGBA').tobytes() == rep
    records.append({**rec, 'allAliasesIndependentlyByteExact': True})
assert sum(len(r['aliases']) for r in records) == 432
# Union crop derived from actual RGBA, then rounded outward to a fixed grid. No auto-fit per pose.
boxes = []
for rec in records:
    with Image.open(SOURCE / 'Raw' / (rec['representative'] + '.png')) as im:
        boxes.append(im.getchannel('A').getbbox())
crop = (min(b[0] for b in boxes)-6, min(b[1] for b in boxes)-6,
        max(b[2] for b in boxes)+6, max(b[3] for b in boxes)+6)
w, h = crop[2]-crop[0], crop[3]-crop[1]
assert (w+12)*4 <=1536 and ((w+12)*4)*((h+32)*2) <=2_000_000
pages = []
for bg, colour in BACKGROUNDS.items():
    folder = OUT / bg
    folder.mkdir(exist_ok=True)
    for page in range(49):
        selected = records[page*8:page*8+8]
        sheet = Image.new('RGB', ((w+12)*4, (h+32)*2), colour)
        draw = ImageDraw.Draw(sheet)
        for i, rec in enumerate(selected):
            with Image.open(SOURCE / 'Raw' / (rec['representative']+'.png')) as im:
                raw = im.convert('RGBA')
            native = composite(raw, colour).crop(crop)
            x,y = (i%4)*(w+12)+6, (i//4)*(h+32)+26
            sheet.paste(native, (x,y))
            draw.text((x,y-18),rec['representative'],fill=(190,190,190) if bg=='dark' else (30,30,30))
            assert sheet.crop((x,y,x+w,y+h)).tobytes() == native.tobytes()
        path = folder / f'page-{page:03d}.png'
        sheet.save(path)
        pages.append({'background':bg,'page':page,'path':str(path),'sha256':sha(path),
                      'representatives':[r['representative'] for r in selected],
                      'aliases':[n for r in selected for n in r['aliases']], 'viewed':False})
    print(bg+':49 compact native pages prepared',flush=True)
assert sha(comp_path) == comp_sha and sha(map_path)==map_sha
data = {'status':'PREPARED; VISUAL_REVIEW_PENDING','source':str(SOURCE),'poses':432,
        'uniqueWholeRgba':385,'exactAliases':47,'compositionSha256':comp_sha,'mapSha256':map_sha,
        'sourceRawPngSha256':hashes,'records':records,'pages':pages,'fixedCropPixels':crop,
        'nativePpu':224,'resampling':False,'pageSizePixels':list(sheet.size),'allNonzeroAlphaInsideFixedCrop':True}
(OUT/'review-coverage.json').write_text(json.dumps(data,indent=2))
print(json.dumps({k:v for k,v in data.items() if k not in ['records','pages','sourceRawPngSha256']}))
