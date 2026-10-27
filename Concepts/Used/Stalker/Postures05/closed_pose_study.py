"""One proposed closed pose against the rejected longitudinal hook directions.

This study changes no canonical poses, Unity resources, or accepted art.
Visible end points follow the native Recoil03 reference. Hidden mounts and
middle end points are authoring proposals, not measurements of real anatomy.
"""
from copy import deepcopy
from pathlib import Path
import json
import sys

import numpy as np
from PIL import Image, ImageDraw

FAMILY = Path(__file__).resolve().parent
PROJECT = FAMILY.parents[3]
sys.path.insert(0, str(FAMILY / 'CandidateAudit'))
from single_uv_prototype import warp
from sprite_painter import SpritePainter, _clean_source_root, _decode, _over, ROOT_RADII
from compose_parts import encode
from qa_parts import bounded_holes


def proposed_pose(original):
    pose = deepcopy(original)
    lengths = [(0.46, 0.46), (0.46, 0.46), (0.40, 0.41),
               (0.40, 0.41), (0.48, 0.43), (0.48, 0.43)]
    for leg in range(6):
        side = -1 if leg % 2 == 0 else 1
        if leg < 2:
            knee = np.array([side * .34, .20])
            toe = np.array([side * .068, .411])
        elif leg < 4:
            knee = np.array([side * .38, .02])
            toe = np.array([side * .13, -.12])
        else:
            knee = np.array([side * .34, -.154])
            toe = np.array([side * .06, -.349])
        hip = np.array(pose['hips'][leg])
        upper, lower = lengths[leg]
        knee_z = hip[2] + np.sqrt(upper * upper - np.sum((knee - hip[:2]) ** 2))
        toe_z = knee_z - np.sqrt(lower * lower - np.sum((toe - knee) ** 2))
        pose['knees'][leg] = [*knee.tolist(), float(knee_z)]
        pose['feet'][leg] = [*toe.tolist(), float(toe_z)]
    return pose


def main():
    out = FAMILY / 'ClosedPoseStudy'
    out.mkdir(exist_ok=True)
    painter = SpritePainter(PROJECT)
    original = next(pose for pose in json.loads((FAMILY / 'poses.json').read_text())
                    if pose['row'] == 8 and pose['phase'] == 0)
    proposal = proposed_pose(original)
    with Image.open(PROJECT / 'Assets/EmergencyShift/Art/Enemies/Stalker/Locomotion02/rig-atlas.png') as atlas:
        sources = []
        for leg, limb in enumerate(painter.anatomy['limbs']):
            slot = leg * 2 + 1
            x, y = slot % 4 * 358, slot // 4 * 406
            source = _decode(atlas.convert('RGBA').crop((x, y, x + 358, y + 406)))
            source, unused = _clean_source_root(source, np.asarray(limb['hip']), ROOT_RADII[leg])
            sources.append(source)
    report = {'status': 'One proposed pose only; not production or accepted art', 'cases': []}
    panel = Image.new('RGB', (1152, 620), '#00ff00')
    for column, (label, pose) in enumerate([('old-hooks', original), ('native-hook-directions', proposal)]):
        whole = np.zeros((576, 576, 4), dtype='float32')
        limbs = []
        for leg, limb in enumerate(painter.anatomy['limbs']):
            paint = warp(sources[leg], limb, pose, leg)
            limbs.append(paint)
            whole = _over(paint, whole)
        whole = _over(painter._torso, whole)
        image = encode(whole, green=True)
        image.save(out / f'{label}-green.png')
        encode(whole).save(out / f'{label}.png')
        panel.paste(image, (column * 576, 44))
        ImageDraw.Draw(panel).text((column * 576 + 20, 10), label, fill='black')
        report['cases'].append({'name': label, 'pose': pose,
            'perLegHolePixelsOver128': [sum(part['pixels'] for part in bounded_holes(
                np.rint(paint[..., 3] * 255) > 128)) for paint in limbs]})
    panel.save(out / 'closed-comparison-green.png')
    (out / 'proposed-pose.json').write_text(json.dumps(proposal, indent=2))
    (out / 'comparison.json').write_text(json.dumps(report, indent=2))


if __name__ == '__main__':
    main()
