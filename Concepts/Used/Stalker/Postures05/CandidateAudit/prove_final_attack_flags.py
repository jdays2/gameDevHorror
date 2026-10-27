"""Trace retained weak-alpha/contact flags without mutating the frozen artwork."""
from pathlib import Path
import json
import sys

import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont

HERE = Path(__file__).resolve().parent
FAMILY = HERE.parent
sys.path.insert(0, str(FAMILY))
from native_frame_painter import SpritePainter, bilinear, uv_field
from make_native_attack_pages import composite
from audit_native_family import digest, validate_composition


def neighbours(source, uv):
    uv = np.where(np.abs(uv - np.rint(uv)) < 1e-4, np.rint(uv), uv)
    x, y = uv
    x0, y0 = int(np.floor(x)), int(np.floor(y))
    fx, fy = x - x0, y - y0
    result = []
    for dx, dy, weight in [(0, 0, (1-fx)*(1-fy)), (1, 0, fx*(1-fy)),
                           (0, 1, (1-fx)*fy), (1, 1, fx*fy)]:
        sx, sy = x0+dx, y0+dy
        alpha = float(source[sy, sx, 3]) if 0 <= sx < source.shape[1] and 0 <= sy < source.shape[0] else 0.
        result.append({'pixel': [sx, sy], 'weight': float(weight), 'alphaByte': int(np.rint(alpha*255))})
    return result


def main():
    out = HERE/'FinalAttack792QA01'
    poses, _, composition_sha = validate_composition('attack')
    poses = {pose['name']: pose for pose in poses}
    painter = SpritePainter(FAMILY.parents[3])
    checks = json.loads((out/'attack-checks.json').read_text())
    traces = []
    for check in checks:
        name = check['name']
        pose = poses[name]
        whole = np.asarray(Image.open(FAMILY/'Raw'/f'{name}.png').convert('RGBA'))
        torso = np.asarray(Image.open(FAMILY/'Masks'/f'torso-{name}.png').convert('RGBA'))[..., 3]
        for limb_record in check['limbs']:
            if not limb_record['weakAlphaUpTo6Outside2pxSolidDilation']:
                continue
            leg = limb_record['leg']
            alpha = np.asarray(Image.open(FAMILY/'Masks'/f'leg-{name}-{leg}.png').convert('RGBA'))[..., 3]
            dilation = np.asarray(Image.fromarray((alpha > 128).astype('uint8')*255).filter(ImageFilter.MaxFilter(5))) > 128
            yy, xx = np.nonzero((alpha > 0) & (alpha <= 6) & ~dilation)
            points = np.column_stack([xx, yy])
            uv = uv_field(painter.anatomy['limbs'][leg], pose, leg, points=points)
            samples = bilinear(painter.sources[leg], uv)
            predicted = np.rint(samples[:, 3]*255).astype('uint8')
            assert np.array_equal(predicted, alpha[yy, xx]), (name, leg)
            for index, (x, y) in enumerate(points):
                own = neighbours(painter.sources[leg], uv[index])
                original = neighbours(painter.raw_sources[leg], uv[index])
                assert all(value['weight'] >= 0 for value in own)
                assert abs(sum(value['weight'] for value in own)-1) < 1e-8
                traces.append({'name': name, 'leg': leg, 'targetPixel': [int(x), int(y)],
                    'limbAlphaByte': int(alpha[y, x]), 'wholeAlphaByte': int(whole[y, x, 3]),
                    'torsoAlphaByte': int(torso[y, x]), 'sourceUV': uv[index].tolist(),
                    'positiveBilinearSampleAlpha': float(samples[index, 3]),
                    'sourceNeighbours': own, 'originalNativeNeighbours': original,
                    'sourceAlphaNeighboursUnchanged': [v['alphaByte'] for v in own] == [v['alphaByte'] for v in original]})

    font = ImageFont.truetype('C:/Windows/Fonts/arial.ttf', 15)
    cases = [('entry-16-04', (196, 377, 270, 438)),
             ('return-16-03', (196, 377, 270, 438)),
             ('entry-00-00', (248, 135, 292, 186)),
             ('entry-31-00', (302, 246, 348, 287))]
    sheet = Image.new('RGB', (1490, 330*len(cases)), '#454545')
    draw = ImageDraw.Draw(sheet)
    for row, (name, box) in enumerate(cases):
        raw = Image.open(FAMILY/'Raw'/f'{name}.png').convert('RGBA')
        draw.text((8, row*330+5), f'{name} | native 1x left; same source pixels nearest 4x; green / dark / light', font=font, fill='white')
        for col, bg in enumerate(([0, 1, 0], [.015, .015, .015], [.88, .88, .88])):
            crop = composite(raw, bg).crop(box)
            sheet.paste(crop, (8+col*490, row*330+35))
            sheet.paste(crop.resize((crop.width*4, crop.height*4), Image.Resampling.NEAREST), (100+col*490, row*330+35))
    sheet.save(out/'attack-weak-AA-and-proximal-contexts.png')
    (out/'weak-alpha-source-trace.json').write_text(json.dumps(traces, indent=2))

    # Validate the exact reviewed PNGs and preserve page hashes after inspection.
    art = HERE/'FinalAttack792Art01'
    manifest = json.loads((art/'manifest.json').read_text())
    assert manifest['compositionManifestSha256'] == composition_sha
    assert len(manifest['rawPngSha256']) == 792
    for name, expected in manifest['rawPngSha256'].items():
        assert digest(FAMILY/'Raw'/f'{name}.png') == expected, name
    page_hashes = {}
    for background in ['Green', 'Dark', 'Light']:
        for page in manifest['pages']:
            page_path = art/background/Path(page['page']).name
            page_hashes[str(page_path.relative_to(art))] = digest(page_path)
    assert len(page_hashes) == 297
    assert digest(FAMILY/'attack-composition.json') == composition_sha
    proof = {'compositionManifestSha256': composition_sha, 'tracedWeakPixels': len(traces),
        'exactPredictedAlphaMatches': len(traces), 'negativeBilinearWeights': 0,
        'sourceAlphaNeighbourUnchangedPixels': sum(t['sourceAlphaNeighboursUnchanged'] for t in traces),
        'sourceOwnerOrCleanupNeighbourChangedPixels': sum(not t['sourceAlphaNeighboursUnchanged'] for t in traces),
        'sourceChangedNeighbourPixelsCoveredByActualTorso': sum(not t['sourceAlphaNeighboursUnchanged'] and t['torsoAlphaByte'] > 128 for t in traces),
        'visibleWeakPixelsOutsideActualTorso': sum(t['torsoAlphaByte'] <= 128 and t['wholeAlphaByte'] <= 6 for t in traces),
        'rawPngHashesVerified': 792, 'nativePagesHashesRecorded': len(page_hashes),
        'pageSha256': page_hashes,
        'interpretation': 'One positive-weight source sample reproduces every retained alpha byte. Native/ownership edge details remain in metrics; neither thresholding nor alpha erosion was applied.'}
    (out/'weak-alpha-and-review-integrity.json').write_text(json.dumps(proof, indent=2))
    print(json.dumps({k: v for k, v in proof.items() if k != 'pageSha256'}, indent=2))


if __name__ == '__main__':
    main()
