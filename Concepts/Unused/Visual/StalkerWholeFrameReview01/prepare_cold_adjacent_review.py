from pathlib import Path
import hashlib
import json
from PIL import Image
import numpy as np
from prepare_adjacent_dark_light import HERE, SOURCE as WARM, sha, composite

SOURCE = HERE.parents[3] / 'Concepts/Used/Sleeper/Postures05/AttackAdjacent01'
OUT = HERE / 'ColdAttackAdjacent01'
COMPOSITION_SHA = '0895af7a0ba7640ff576f97b7a393794afacda04f6e8e703bd88dd5a79192639'
MAP_SHA = '62ecc03442cdfcdacf5cec9f6fb97c74001e98f40f3eeb2ac5b3108d84927899'
BACKGROUNDS = {'dark': (18, 22, 26), 'light': (235, 235, 235)}


def main():
    assert sha(SOURCE / 'native-composition.json') == COMPOSITION_SHA
    assert sha(SOURCE / 'native-unique-map.json') == MAP_SHA
    comp = json.loads((SOURCE / 'native-composition.json').read_text())
    mapping = json.loads((SOURCE / 'native-unique-map.json').read_text())
    assert mapping['newPoses'] == 672 and mapping['uniqueRgbaFrames'] == 461
    assert mapping['pageSizePixels'] == [1152, 1216] and not mapping['resampling']
    records, names, raw_hashes = [], [], {}
    for rec in mapping['records']:
        rep_path = SOURCE / 'Raw' / f"{rec['representative']}.png"
        with Image.open(rep_path) as im:
            representative = im.convert('RGBA').tobytes()
        assert hashlib.sha256(representative).hexdigest() == rec['rgbaSha256']
        for name in rec['aliases']:
            path = SOURCE / 'Raw' / f'{name}.png'
            assert sha(path) == comp['rawPngSha256'][name]
            with Image.open(path) as im:
                rgba = im.convert('RGBA')
                assert rgba.size == (576, 576) and rgba.tobytes() == representative
                alpha = rgba.getchannel('A').tobytes()
            with Image.open(WARM / 'Raw' / f'{name}.png') as im:
                assert im.convert('RGBA').getchannel('A').tobytes() == alpha, name
            raw_hashes[name] = comp['rawPngSha256'][name]
            names.append(name)
        records.append({**rec, 'wholeRgbaAliasesIndependentlyByteExact': True,
                        'allAliasAlphaIndependentlyExactWarm': True})
    assert len(names) == len(set(names)) == 672
    OUT.mkdir(exist_ok=True)
    pages = []
    for background, colour in BACKGROUNDS.items():
        for page in range(116):
            subset = records[page * 4:page * 4 + 4]
            path = SOURCE / 'NativeUnique' / f'unique-{page:03d}-{background}.png'
            with Image.open(path) as im:
                sheet = im.convert('RGB')
                assert sheet.size == (1152, 1216)
            for index, rec in enumerate(subset):
                with Image.open(SOURCE / 'Raw' / f"{rec['representative']}.png") as im:
                    expected = composite(im.convert('RGBA'), colour)
                x, y = index % 2 * 576, index // 2 * 608 + 32
                tile = sheet.crop((x, y, x + 576, y + 576))
                assert tile.tobytes() == expected.tobytes(), (background, page, rec['representative'])
            pages.append({'background': background, 'page': page, 'path': str(path),
                          'sha256': sha(path), 'representatives': [r['representative'] for r in subset],
                          'aliases': [n for r in subset for n in r['aliases']],
                          'payloadIndependentlyExactRawComposite': True, 'viewed': False})
        print(background + ':116 native payload pages verified', flush=True)
    assert sha(SOURCE / 'native-composition.json') == COMPOSITION_SHA
    assert sha(SOURCE / 'native-unique-map.json') == MAP_SHA
    result = {'status': 'PREPARED; NATIVE_MATERIAL_VISUAL_REVIEW_PENDING',
              'source': str(SOURCE), 'compositionSha256': COMPOSITION_SHA, 'uniqueMapSha256': MAP_SHA,
              'sourceRawPngSha256': raw_hashes, 'newOddFrames': 672, 'exactUniqueWholeRgba': 461,
              'exactRgbaDuplicateAliases': 211, 'coldAlphaExactWarmFrameCount': 672,
              'pageSizePixels': [1152, 1216], 'nativePpu': 224, 'resampling': False,
              'nativePayloadTilesIndependentlyVerified': 922,
              'records': records, 'pages': pages,
              'scope': 'Cold native skin palette, blood/material continuity, transparency and overlap edge presentation. Geometry math is a separate previously reviewed gate.'}
    (OUT / 'review-coverage.json').write_text(json.dumps(result, indent=2))
    print(json.dumps({k: v for k, v in result.items() if k not in ['records', 'pages', 'sourceRawPngSha256']}))


if __name__ == '__main__':
    main()
