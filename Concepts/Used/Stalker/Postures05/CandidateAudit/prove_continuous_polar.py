"""Exactly two immutable mapping proofs; no production or mass writes."""
from pathlib import Path
import hashlib
import json
import shutil
import sys
from itertools import combinations
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

HERE = Path(__file__).resolve().parent
FAMILY = HERE.parent
sys.path.insert(0, str(FAMILY))
from continuous_polar_painter import SpritePainter, warp_rgba, field_with_metrics, uv_field
from native_uv_painter import ORIGIN, FLIP, PPU
from compose_parts import encode
from qa_parts import bounded_holes, components, FONT


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def source_pose(painter):
    pose = {}
    for target, source in [('hips', 'hip'), ('knees', 'knee'), ('feet', 'tip')]:
        pose[target] = [list((FLIP @ (np.asarray(limb[source]) - painter.anatomy['pivot']) / PPU)) + [0]
                        for limb in painter.anatomy['limbs']]
    return pose


def alpha_interior(alpha):
    report = []
    for radius in [2, 4]:
        mask = np.asarray(Image.fromarray((alpha > 128).astype('uint8') * 255).filter(
            ImageFilter.MinFilter(2 * radius + 1))) > 128
        values = alpha[mask]
        report.append({'erosionRadius': radius, 'pixels': int(mask.sum()),
                       'minimum': int(values.min()) if len(values) else None,
                       'below230': int((values < 230).sum())})
    return report


def main():
    out = HERE / 'ContinuousPolarStudy01'
    if out.exists():
        raise FileExistsError('Frozen proof exists; choose a new version for a changed field.')
    (out / 'Raw').mkdir(parents=True)
    (out / 'Masks').mkdir()
    painter = SpritePainter(FAMILY.parents[3])
    identity = []
    native_pose = source_pose(painter)
    offset = (ORIGIN - np.asarray(painter.anatomy['pivot'])).astype(int)
    for leg, limb in enumerate(painter.anatomy['limbs']):
        warped = np.asarray(encode(warp_rgba(painter.raw_sources[leg], limb, native_pose, leg)))
        expected = np.zeros((576, 576, 4), dtype='uint8')
        original = np.asarray(encode(painter.raw_sources[leg]))
        expected[offset[1]:offset[1]+406, offset[0]:offset[0]+358] = original
        identity.append({'leg': leg, 'id': limb['id'],
                         'byteDifferences': int(np.count_nonzero(warped != expected)),
                         'maximumByteDifference': int(np.max(np.abs(warped.astype(int) - expected.astype(int))))})
    proposal = json.loads((FAMILY / 'ClosedPoseStudy/proposed-pose.json').read_text())
    frozen = next(pose for pose in json.loads((HERE / 'HeldOut01/poses.json').read_text())
                  if pose['name'] == 'body-8-00')
    cases = [('proposed-closed', proposal), ('old-worst-hole', frozen)]
    records = []
    sheet = Image.new('RGB', (1152, 636), '#00ff00')
    draw = ImageDraw.Draw(sheet)
    joints = Image.new('RGB', (960, 660), '#00ff00')
    joint_draw = ImageDraw.Draw(joints)
    for index, (name, pose) in enumerate(cases):
        whole, limbs, torso = painter.paint(pose)
        encode(whole).save(out / 'Raw' / f'{name}.png')
        encode(whole, green=True).save(out / f'{name}-native-green.png')
        encode(torso).save(out / 'Masks' / f'torso-{name}.png')
        sheet.paste(encode(whole, green=True), (index * 576, 60))
        draw.text((index * 576 + 10, 10), name + ' | one polar UV | native224PPU', fill='black', font=FONT)
        draw.text((index * 576 + 10, 34), 'Mapping proof only; original root/volume unresolved', fill='black', font=FONT)
        alphas = []
        leg_records = []
        for leg, limb in enumerate(painter.anatomy['limbs']):
            image = encode(limbs[leg])
            image.save(out / 'Masks' / f'leg-{name}-{leg}.png')
            alpha = np.asarray(image)[..., 3]
            alphas.append(alpha)
            yy, xx = np.nonzero(alpha > 230)
            points = np.column_stack([xx, yy])
            uv, metrics = field_with_metrics(limb, pose, leg, points)
            jacobian = metrics['jacobian']
            eps = .001
            a = uv_field(limb, pose, leg, points=points + [eps, 0])
            b = uv_field(limb, pose, leg, points=points - [eps, 0])
            c = uv_field(limb, pose, leg, points=points + [0, eps])
            d = uv_field(limb, pose, leg, points=points - [0, eps])
            dx, dy = (a - b) / (2 * eps), (c - d) / (2 * eps)
            finite_jacobian = dx[:, 0] * dy[:, 1] - dx[:, 1] * dy[:, 0]
            parts = components(alpha > 4)
            border = int(np.count_nonzero(alpha[[0, -1], :] > 1) + np.count_nonzero(alpha[:, [0, -1]] > 1))
            p = metrics['parameters']
            leg_records.append({'leg': leg, 'id': limb['id'], 'opaquePixels': len(points),
                'analyticalJacobianMinimum': float(np.nanmin(jacobian)),
                'analyticalNonpositive': int(np.count_nonzero(jacobian <= 0)),
                'finiteJacobianMinimum': float(finite_jacobian.min()),
                'finiteNonpositive': int(np.count_nonzero(finite_jacobian <= 0)),
                'polarCentreSamples': int(metrics['polarCentre'].sum()),
                'angularDerivativeMinimum': float(metrics['angularDerivative'].min()),
                'radialDerivativeMinimum': float(metrics['radialDerivative'].min()),
                'endpointSlopeLimiter': float(p['slopeLimiter']),
                'componentCountAlphaOver4': len(parts), 'borderPixelsAlphaOver1': border,
                'holes': [{'alphaOver': threshold, 'parts': bounded_holes(alpha > threshold)} for threshold in [4, 128]],
                'interior': alpha_interior(alpha)})
        torso_alpha = np.asarray(encode(torso))[..., 3]
        pairs = []
        for first, second in combinations(range(6), 2):
            mask = (alphas[first] > 128) & (alphas[second] > 128) & (torso_alpha <= 128)
            yy, xx = np.nonzero(mask)
            pairs.append({'pair': [first, second], 'outsideActualTorsoPixels': int(mask.sum()),
                          'bbox': [int(xx.min()), int(yy.min()), int(xx.max())+1, int(yy.max())+1] if len(xx) else None})
        records.append({'name': name, 'pose': pose, 'legs': leg_records, 'all15Pairs': pairs,
                        'wholeInterior': alpha_interior(np.asarray(encode(whole))[..., 3])})
        # Exactly the RF and RR joints, fixed84px native crop, source vs target.
        for slot, leg in enumerate([1, 5]):
            limb = painter.anatomy['limbs'][leg]
            target_knee = ORIGIN + FLIP @ np.asarray(pose['knees'][leg][:2]) * PPU
            source_knee = np.asarray(limb['knee'])
            source_image = encode(painter.sources[leg], green=True)
            target_image = encode(limbs[leg], green=True)
            x = slot * 480
            y = index * 330
            joint_draw.text((x + 6, y + 8), f'{name} {limb["id"]}: native / polar', fill='black', font=FONT)
            for column, (image, centre) in enumerate([(source_image, source_knee), (target_image, target_knee)]):
                cx, cy = np.rint(centre).astype(int)
                crop = image.crop((cx-42, cy-42, cx+42, cy+42))
                joints.paste(crop.resize((252, 252), Image.Resampling.NEAREST), (x + column * 220, y + 50))
        print(f'READY {out / (name + "-native-green.png")}', flush=True)
    sheet.save(out / 'two-controls-native-green.png')
    joints.save(out / 'RF-RR-joints-3x-nearest.png')
    report = {'version': 'ContinuousPolarStudy01', 'scope': 'Exactly two proof frames; no production or mass writes',
              'status': 'Mapping candidate. Root ownership and closed muscle volume are unresolved art defects.',
              'sourceIdentity': identity, 'cases': records}
    (out / 'proof.json').write_text(json.dumps(report, indent=2))
    shutil.copy2(HERE / 'continuous_polar_painter.py', out / 'renderer-snapshot.py.txt')
    manifest = {'rendererSha256': digest(HERE / 'continuous_polar_painter.py'),
                'sourceCacheSha256': digest(HERE / 'native_uv_painter.py'),
                'legacyUtilitySha256': digest(FAMILY / 'sprite_painter.py'),
                'rawPngSha256': {path.name: digest(path) for path in (out / 'Raw').glob('*.png')}}
    (out / 'manifest.json').write_text(json.dumps(manifest, indent=2))
    print(json.dumps({'identityByteDifferences': sum(row['byteDifferences'] for row in identity),
                      'analyticalNonpositive': sum(leg['analyticalNonpositive'] for row in records for leg in row['legs']),
                      'finiteNonpositive': sum(leg['finiteNonpositive'] for row in records for leg in row['legs']),
                      'holesOver4': sum(hole['pixels'] for row in records for leg in row['legs']
                                       for item in leg['holes'] if item['alphaOver'] == 4 for hole in item['parts'])}, indent=2))


if __name__ == '__main__':
    main()
