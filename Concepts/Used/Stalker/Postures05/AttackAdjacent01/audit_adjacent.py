"""Strict pixel/mask audit of new odd bridge poses; preserves every flag."""
from pathlib import Path
import hashlib
import json
import sys
import numpy as np
from PIL import Image, ImageFilter

HERE = Path(__file__).resolve().parent
FAMILY = HERE.parent
sys.path[:0] = [str(FAMILY), str(FAMILY/'CandidateAudit')]
from native_frame_painter import SpritePainter, field_with_metrics, uv_field
from qa_parts import components, bounded_holes


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def local_components(binary):
    yy, xx = np.nonzero(binary)
    if not len(xx):
        return []
    x0, y0, x1, y1 = int(xx.min()), int(yy.min()), int(xx.max())+1, int(yy.max())+1
    found = components(binary[y0:y1, x0:x1])
    for part in found:
        a, b, c, d = part['bbox']
        part['bbox'] = [a+x0, b+y0, c+x0, d+y0]
    return found


def main():
    composition_path = HERE/'attack-composition.json'
    composition_sha = digest(composition_path)
    composition = json.loads(composition_path.read_text())
    assert composition['poses'] == 1464 and composition['newOddPoses'] == 672
    for key, path in [('posesSha256', HERE/'attack-poses.json'), ('painterSha256', FAMILY/'native_frame_painter.py'),
                      ('colorsSha256', FAMILY/'frame_colors.py'), ('mouthSha256', FAMILY/'attack_mouth.py'),
                      ('composerSha256', HERE/'compose_adjacent.py')]:
        assert composition[key] == digest(path), key
    out = HERE/'QA'
    out.mkdir(exist_ok=True)
    assert not (out/'summary.json').exists()
    poses = [p for p in json.loads((HERE/'attack-poses.json').read_text()) if p['group'] != 'strike' and p['sample']%2]
    assert len(poses) == 672
    painter = SpritePainter(FAMILY.parents[3])
    records = []
    masks_sha = hashlib.sha256()
    raw_sha = hashlib.sha256()
    for index, pose in enumerate(poses):
        name = pose['name']
        raw_path = HERE/'Raw'/f'{name}.png'
        assert digest(raw_path) == composition['rawPngSha256'][name]
        raw_sha.update(name.encode())
        raw_sha.update(raw_path.read_bytes())
        whole = np.asarray(Image.open(raw_path).convert('RGBA'))[..., 3]
        torso = np.asarray(Image.open(HERE/'Masks'/f'torso-{name}.png').convert('RGBA'))[..., 3]
        mouth = np.asarray(Image.open(HERE/'Masks'/f'mouth-{name}.png').convert('RGBA'))[..., 3]
        assert not mouth.any(), name
        alphas, limbs = [], []
        for leg in range(6):
            path = HERE/'Masks'/f'leg-{name}-{leg}.png'
            masks_sha.update(path.name.encode())
            masks_sha.update(path.read_bytes())
            alpha = np.asarray(Image.open(path).convert('RGBA'))[..., 3]
            alphas.append(alpha)
            yy, xx = np.nonzero(alpha > 230)
            _, metrics = field_with_metrics(painter.anatomy['limbs'][leg], pose, leg, np.column_stack([xx, yy]))
            jacobian = metrics['jacobian']
            eroded = np.asarray(Image.fromarray((alpha>128).astype('uint8')*255).filter(ImageFilter.MinFilter(9))) > 128
            inside = alpha[eroded]
            dilation = np.asarray(Image.fromarray((alpha>128).astype('uint8')*255).filter(ImageFilter.MaxFilter(5))) > 128
            weak = (alpha > 0)&(alpha <= 6)&~dilation
            holes = []
            for threshold in [4, 128]:
                found = bounded_holes(alpha > threshold)
                for part in found:
                    a, b, c, d = part['bbox']
                    part['torsoAlphaRangeInBox'] = [int(torso[b:d, a:c].min()), int(torso[b:d, a:c].max())]
                    part['wholeAlphaRangeInBox'] = [int(whole[b:d, a:c].min()), int(whole[b:d, a:c].max())]
                holes.append({'alphaOver': threshold, 'parts': found})
            limbs.append({'leg': leg, 'id': painter.anatomy['limbs'][leg]['id'],
                'componentsAlphaOver4': local_components(alpha > 4), 'holes': holes,
                'borderPositiveAlpha': int(np.count_nonzero(alpha[[0, -1]])+np.count_nonzero(alpha[:, [0, -1]])),
                'jacobianMinimum': float(jacobian.min()), 'nonpositiveJacobianPixels': int(np.count_nonzero(jacobian <= 0)),
                'erode4AlphaMinimum': int(inside.min()) if len(inside) else None,
                'erode4AlphaBelow230': int(np.count_nonzero(inside < 230)),
                'weakAlphaUpTo6Outside2pxSolidDilation': int(weak.sum())})
        pairs = []
        for a in range(6):
            for b in range(a+1, 6):
                contact = (alphas[a] > 128)&(alphas[b] > 128)&(torso <= 128)
                yy, xx = np.nonzero(contact)
                part = {'pair': [a, b], 'pixels': int(contact.sum())}
                if len(xx):
                    points = np.column_stack([xx, yy])
                    part['bbox'] = [int(xx.min()), int(yy.min()), int(xx.max())+1, int(yy.max())+1]
                    trace = []
                    for leg in [a, b]:
                        limb = painter.anatomy['limbs'][leg]
                        uv = uv_field(limb, pose, leg, points=points)
                        hip = np.linalg.norm(uv-np.asarray(limb['hip']), axis=-1)
                        knee = np.linalg.norm(uv-np.asarray(limb['knee']), axis=-1)
                        trace.append({'leg': leg, 'firstSourceUV': uv[0].tolist(),
                            'sourceHipDistanceRange': [float(hip.min()), float(hip.max())],
                            'sourceKneeDistanceRange': [float(knee.min()), float(knee.max())]})
                    part['sourceTrace'] = trace
                    part['status'] = 'Retained; actual-context review required. No hip-sphere exemption.'
                pairs.append(part)
        records.append({'name': name, 'limbs': limbs, 'all15Pairs': pairs,
            'wholeComponentsAlphaOver4': local_components(whole > 4),
            'wholeBorderPositiveAlpha': int(np.count_nonzero(whole[[0, -1]])+np.count_nonzero(whole[:, [0, -1]]))})
        if (index+1)%48 == 0:
            print(f'New odd strict QA: {index+1}/672', flush=True)
    assert digest(composition_path) == composition_sha
    (out/'new-odd-checks.json').write_text(json.dumps(records, indent=2))
    pairs = [{'name': r['name'], **p} for r in records for p in r['all15Pairs'] if p['pixels']]
    holes = [{'name': r['name'], 'leg': leg['leg'], **h} for r in records for leg in r['limbs'] for h in leg['holes'] if h['parts']]
    summary = {'newOddFrames': len(records), 'compositionSha256': composition_sha,
        'newOddRawAggregateSha256': raw_sha.hexdigest(), 'newOddLegMasksAggregateSha256': masks_sha.hexdigest(),
        'newPairsTested': 672*15, 'preservedOldPairsProvenByByteIdentity': 792*15,
        'pairCandidates': sorted(pairs, key=lambda v: -v['pixels']), 'boundedHoleCandidates': holes,
        'nonpositiveJacobianPixels': sum(l['nonpositiveJacobianPixels'] for r in records for l in r['limbs']),
        'jacobianMinimum': min(l['jacobianMinimum'] for r in records for l in r['limbs']),
        'wholeBorderPositiveAlphaPixels': sum(r['wholeBorderPositiveAlpha'] for r in records),
        'erode4AlphaBelow230Pixels': sum(l['erode4AlphaBelow230'] for r in records for l in r['limbs']),
        'disconnectedLegFramesAlphaOver4': sum(len(l['componentsAlphaOver4']) != 1 for r in records for l in r['limbs']),
        'disconnectedWholeFramesAlphaOver4': sum(len(r['wholeComponentsAlphaOver4']) != 1 for r in records),
        'weakAlphaOutside2pxCount': sum(l['weakAlphaUpTo6Outside2pxSolidDilation'] for r in records for l in r['limbs']),
        'interpretation': 'Numeric only; every nonzero overlap and bounded cavity retained for actual visual/source trace.'}
    (out/'summary.json').write_text(json.dumps(summary, indent=2))
    print(json.dumps({k: v for k, v in summary.items() if k not in ['pairCandidates', 'boundedHoleCandidates']}, indent=2))
    print('Contacts:', len(pairs), 'holes:', len(holes), flush=True)


if __name__ == '__main__':
    main()
