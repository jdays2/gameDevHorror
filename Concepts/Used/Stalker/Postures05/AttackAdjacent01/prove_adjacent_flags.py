"""Source-trace all retained weak AA pixels and draw exact native flag contexts."""
from pathlib import Path
import hashlib
import json
import sys
import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont

HERE = Path(__file__).resolve().parent
FAMILY = HERE.parent
sys.path[:0] = [str(FAMILY), str(FAMILY/'CandidateAudit')]
from native_frame_painter import SpritePainter, bilinear, uv_field
from prove_final_attack_flags import neighbours
from make_native_attack_pages import composite


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    composition = json.loads((HERE/'attack-composition.json').read_text())
    summary = json.loads((HERE/'QA/summary.json').read_text())
    assert digest(HERE/'attack-composition.json') == summary['compositionSha256']
    assert digest(FAMILY/'native_frame_painter.py') == composition['painterSha256']
    poses = {p['name']: p for p in json.loads((HERE/'attack-poses.json').read_text())}
    painter = SpritePainter(FAMILY.parents[3])
    traces = []
    for record in json.loads((HERE/'QA/new-odd-checks.json').read_text()):
        name = record['name']
        assert digest(HERE/'Raw'/f'{name}.png') == composition['rawPngSha256'][name]
        whole = np.asarray(Image.open(HERE/'Raw'/f'{name}.png').convert('RGBA'))[..., 3]
        torso = np.asarray(Image.open(HERE/'Masks'/f'torso-{name}.png').convert('RGBA'))[..., 3]
        for leg_record in record['limbs']:
            if not leg_record['weakAlphaUpTo6Outside2pxSolidDilation']:
                continue
            leg = leg_record['leg']
            alpha = np.asarray(Image.open(HERE/'Masks'/f'leg-{name}-{leg}.png').convert('RGBA'))[..., 3]
            dilated = np.asarray(Image.fromarray((alpha>128).astype('uint8')*255).filter(ImageFilter.MaxFilter(5))) > 128
            yy, xx = np.nonzero((alpha>0)&(alpha<=6)&~dilated)
            points = np.column_stack([xx, yy])
            uv = uv_field(painter.anatomy['limbs'][leg], poses[name], leg, points=points)
            sampled = bilinear(painter.sources[leg], uv)
            assert np.array_equal(np.rint(sampled[:, 3]*255).astype('uint8'), alpha[yy, xx])
            for index, (x, y) in enumerate(points):
                own = neighbours(painter.sources[leg], uv[index])
                raw = neighbours(painter.raw_sources[leg], uv[index])
                assert min(n['weight'] for n in own) >= 0
                assert abs(sum(n['weight'] for n in own)-1) < 1e-8
                traces.append({'name': name, 'leg': leg, 'targetPixel': [int(x), int(y)],
                    'limbAlphaByte': int(alpha[y,x]), 'wholeAlphaByte': int(whole[y,x]),
                    'torsoAlphaByte': int(torso[y,x]), 'sourceUV': uv[index].tolist(),
                    'sourceNeighbours': own, 'originalNativeNeighbours': raw,
                    'sourceAlphaNeighboursUnchanged': [n['alphaByte'] for n in own] == [n['alphaByte'] for n in raw]})
    assert len(traces) == summary['weakAlphaOutside2pxCount']
    # All contacts retain their actual counts. The source trace must identify
    # proximal tissue; this condition does not exempt any mask intersection.
    assert all(p['pair'] == [1,3] for p in summary['pairCandidates'])
    proof = {'compositionSha256': summary['compositionSha256'], 'tracedWeakPixels': len(traces),
        'exactPositiveBilinearAlphaMatches': len(traces), 'negativeWeights': 0,
        'sourceAlphaNeighboursUnchanged': sum(t['sourceAlphaNeighboursUnchanged'] for t in traces),
        'sourceOwnerNeighboursChanged': sum(not t['sourceAlphaNeighboursUnchanged'] for t in traces),
        'changedNeighbourPixelsCoveredByActualTorso': sum(not t['sourceAlphaNeighboursUnchanged'] and t['torsoAlphaByte']>128 for t in traces),
        'retainedProximalPairFrames': len(summary['pairCandidates']),
        'retainedPairPixelSum': sum(p['pixels'] for p in summary['pairCandidates']),
        'maximumPairPixels': max(p['pixels'] for p in summary['pairCandidates']),
        'contactSourceHipDistanceMaximum': max(t['sourceHipDistanceRange'][1] for p in summary['pairCandidates'] for t in p['sourceTrace']),
        'contactSourceKneeDistanceMinimum': min(t['sourceKneeDistanceRange'][0] for p in summary['pairCandidates'] for t in p['sourceTrace']),
        'classification': 'RF/RM proximal Y attachment; actual context review recorded separately. No hip sphere, pixel removal or hole fill.'}
    (HERE/'QA/weak-alpha-source-trace.json').write_text(json.dumps(traces, indent=2))
    (HERE/'QA/flag-source-summary.json').write_text(json.dumps(proof, indent=2))
    cases = [('entry-31-01', (303,243,350,290)), ('return-31-13', (303,243,350,290)),
             ('entry-20-09', (196,367,274,439)), ('return-20-05', (196,367,274,439)),
             ('entry-03-03', (196,367,274,439)), ('return-03-11', (196,367,274,439))]
    font = ImageFont.truetype('C:/Windows/Fonts/arial.ttf', 15)
    for start in range(0,len(cases),2):
        sheet = Image.new('RGB',(1490,660),'#454545')
        draw = ImageDraw.Draw(sheet)
        for row,(name,box) in enumerate(cases[start:start+2]):
            raw = Image.open(HERE/'Raw'/f'{name}.png').convert('RGBA')
            draw.text((8,row*330+5),name+' | native1x left; nearest4x right | green / dark / light',font=font,fill='white')
            for col,bg in enumerate(([0,1,0],[.015]*3,[.88]*3)):
                crop = composite(raw,bg).crop(box)
                sheet.paste(crop,(8+col*490,row*330+35))
                sheet.paste(crop.resize((crop.width*4,crop.height*4),Image.Resampling.NEAREST),(100+col*490,row*330+35))
        sheet.save(HERE/'QA'/f'flag-contexts-{start//2:02d}.png')
    print(json.dumps(proof,indent=2),flush=True)


if __name__ == '__main__':
    main()
