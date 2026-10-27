"""Middle-leg layout proposals, before skin and without canonical writes."""
from copy import deepcopy
from pathlib import Path
import json
import numpy as np

HERE = Path(__file__).resolve().parent


def main():
    source = json.loads((HERE / 'DeepMountStudy/poses.json').read_text())
    closed = next(pose for pose in source if pose['row'] == 8)
    proposals = []
    for knee_y, toe_y in [(.06, -.08), (.06, -.10)]:
        pose = deepcopy(closed)
        pose['name'] = f'closed-middle-k{knee_y:.2f}-t{toe_y:.2f}'
        for leg in [2, 3]:
            hip = np.asarray(pose['hips'][leg])
            knee = np.asarray(pose['knees'][leg])
            toe = np.asarray(pose['feet'][leg])
            knee[1], toe[1] = knee_y, toe_y
            knee[2] = hip[2] + np.sqrt(.4 ** 2 - np.sum((knee[:2] - hip[:2]) ** 2))
            toe[2] = knee[2] - np.sqrt(.41 ** 2 - np.sum((toe[:2] - knee[:2]) ** 2))
            if toe[2] < 0:
                raise ValueError('Raised closed toes must stay above the floor')
            pose['knees'][leg], pose['feet'][leg] = knee.tolist(), toe.tolist()
        proposals.append(pose)
    out = HERE / 'ClosedMiddleStudy02'
    out.mkdir(exist_ok=True)
    (out / 'poses.json').write_text(json.dumps(proposals, indent=2))


if __name__ == '__main__':
    main()
