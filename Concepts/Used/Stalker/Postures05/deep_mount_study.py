"""Uninstalled socket-layout proposals; canonical poses stay unchanged."""
from copy import deepcopy
from pathlib import Path
import json
import numpy as np

HERE = Path(__file__).resolve().parent
LENGTHS = [(0.46, 0.46), (0.46, 0.46), (0.40, 0.41),
           (0.40, 0.41), (0.48, 0.43), (0.48, 0.43)]


def grounded_knee(hip, foot, height, upper, lower, bend):
    a = np.sqrt(upper * upper - (hip[2] - height) ** 2)
    b = np.sqrt(lower * lower - (foot[2] - height) ** 2)
    delta = foot[:2] - hip[:2]
    distance = np.linalg.norm(delta)
    if not abs(a - b) < distance < a + b:
        raise ValueError('Proposed socket cannot retain this exact world foot')
    axis = delta / distance
    along = (a * a - b * b + distance * distance) / (2 * distance)
    radius = np.sqrt(a * a - along * along)
    point = hip[:2] + along * axis + radius * bend * np.array([-axis[1], axis[0]])
    return np.array([*point, height])


def main():
    poses = json.loads((HERE / 'poses.json').read_text())
    selected = [next(p for p in poses if p['row'] == row and p['phase'] == phase)
                for row, phase in [(0, 12), (0, 24), (8, 0)]]
    output, checks = [], []
    for original in selected:
        pose = deepcopy(original)
        for leg in range(4):
            old_hip = np.array(original['hips'][leg])
            old_knee = np.array(original['knees'][leg])
            foot = np.array(original['feet'][leg])
            hip = old_hip.copy()
            if leg < 2:
                # Pull the socket inward across the body. Keeping its original
                # fore/aft anchor preserves the existing maximum-reach feet.
                hip[0] *= .4 / .65
            else:
                hip[:2] *= .4 / .7
            upper, lower = LENGTHS[leg]
            if pose['row'] == 8:
                knee = old_knee.copy()
                knee[2] = hip[2] + np.sqrt(upper * upper - np.sum((knee[:2] - hip[:2]) ** 2))
                foot[2] = knee[2] - np.sqrt(lower * lower - np.sum((foot[:2] - knee[:2]) ** 2))
            else:
                d, k = foot[:2] - old_hip[:2], old_knee[:2] - old_hip[:2]
                bend = np.sign(d[0] * k[1] - d[1] * k[0])
                knee = grounded_knee(hip, foot, (hip[2] + foot[2]) * .5, upper, lower, bend)
            pose['hips'][leg], pose['knees'][leg], pose['feet'][leg] = hip.tolist(), knee.tolist(), foot.tolist()
        pose['name'] = f'deep-mount-{pose["row"]}-{pose["phase"]:02d}'
        errors = []
        for leg, (upper, lower) in enumerate(LENGTHS):
            hip, knee, foot = [np.array(pose[key][leg]) for key in ('hips', 'knees', 'feet')]
            errors.extend([abs(np.linalg.norm(knee - hip) - upper), abs(np.linalg.norm(foot - knee) - lower)])
        checks.append({'name': pose['name'], 'maxBoneLengthError': max(errors),
                       'minFootHeight': min(p[2] for p in pose['feet']),
                       'maxFootXYChange': max(np.linalg.norm(np.array(a[:2]) - b[:2])
                                             for a, b in zip(pose['feet'], original['feet']))})
        output.append(pose)
    out = HERE / 'DeepMountStudyV2'
    out.mkdir(exist_ok=True)
    (out / 'poses.json').write_text(json.dumps(output, indent=2))
    (out / 'proposal.json').write_text(json.dumps({'status': 'Proposal only; no canonical or Unity changes',
        'sourceHipMultipliers': {'foreXY': [.4, .65], 'middleXY': [.4, .4], 'hindXY': [.72, .72]},
        'checks': checks}, indent=2))


if __name__ == '__main__':
    main()
