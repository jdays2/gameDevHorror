"""Check the actual rigid hand silhouettes throughout each broad attack."""
import json
import numpy as np
from PIL import Image, ImageFilter
import build_wide_attacks as wide

wide.configure()
base = wide.base


def hand_image(pose, ident):
    piece = Image.open(wide.ROOT/f'layer-foot-{ident}.png').convert('RGBA')
    rest = base.project(base.REST[ident][2])
    posed = np.asarray(pose['limbs'][ident]['projected'][2])
    matrix = wide.rotation(pose['limbs'][ident]['handAngleRad']).T
    affine = matrix/base.SCALE
    offset = rest-matrix@posed-affine@base.OFFSET
    coefficients = (*affine[0], offset[0], *affine[1], offset[1])
    return piece.transform((1024, 1024), Image.Transform.AFFINE, coefficients, Image.Resampling.BICUBIC).getchannel('A')


def run():
    result = {'revision': 'broad-attacks-r1', 'solidAlphaThreshold': 64, 'states': {}}
    for state in wide.CONFIG:
        records = []
        for p in wide.runtime.poses(state):
            upper = hand_image(p, 'FU')
            lower = hand_image(p, 'FL')
            overlap = int(np.count_nonzero((np.asarray(upper)>64) & (np.asarray(lower)>64)))
            assert overlap==0, (state, p['index'], overlap)
            upper_small = upper.point(lambda value: 255 if value>64 else 0).resize((256, 256), Image.Resampling.NEAREST)
            lower_small = lower.point(lambda value: 255 if value>64 else 0).resize((256, 256), Image.Resampling.NEAREST)
            near = bool(np.any((np.asarray(upper_small.filter(ImageFilter.MaxFilter(5)))>0) & (np.asarray(lower_small)>0)))
            records.append({'index': p['index'], 'solidOverlapPixels': overlap, 'withinApprox8pxBand': near})
        for t in np.linspace(0., 1., 1001):
            wide.pose(state, float(t))
        result['states'][state] = {'overlap': 'PASS', 'continuousIkSamples': 1001, 'frames': records}
    (wide.ROOT/'hand-clearance.json').write_text(json.dumps(result, indent=2), encoding='utf-8')
    print({state: {'solidOverlap': 0, 'nearFrames': [r['index'] for r in data['frames'] if r['withinApprox8pxBand']]}
           for state, data in result['states'].items()})


if __name__=='__main__':
    run()
