"""Reuse exact raw skin renders when only the final raster pass changes.

Previously repaired images must be rendered again. An unchanged previous pass
is the identity, so its pixels are exact raw input for the new raster pass.
"""
import json
import sys
import hashlib
import build_wide_attacks as build
from raster_finish import finish
from PIL import Image

ROOT = build.ROOT
SNAPSHOT = ROOT/'raster-cache-before.json'


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def context():
    return {path.name: sha(path) for path in [ROOT/'build_wide_attacks.py', build.MASTER,
                                             build.runtime.PARENT/'skin_surface.py']}


if __name__=='__main__':
    capture = '--capture' in sys.argv
    snapshot = {'context': context(), 'frames': {}} if capture else json.loads(SNAPSHOT.read_text())
    assert snapshot['context']==context(), 'Raw render inputs changed; full render required.'
    for state in build.CONFIG:
        plan = json.loads((ROOT/state/'motion-plan.json').read_text(encoding='utf-8'))
        for pose in plan['frames']:
            index = pose['index']
            key = f'{state}/{index:02d}'
            frame = ROOT/state/'frames'/f'{state}-{index:02d}.png'
            measurement = ROOT/state/'measurements'/f'{index:02d}.json'
            stats = json.loads(measurement.read_text())
            assert stats['imageSha256']==sha(frame)
            if capture:
                assert stats['fingerprint']==build.fingerprint(pose)
                snapshot['frames'][key] = {'pose': pose, 'stats': stats}
                continue
            previous = snapshot['frames'][key]
            assert previous['pose']==pose and previous['stats']==stats
            if stats['filledRasterPinholes']:
                print('Rerender required', key, flush=True)
                continue
            # The old final pass changed zero pixels: this is exact raw skin.
            image, holes = finish(Image.open(frame))
            if holes:
                image.save(frame)
            stats['cacheUpgrade'] = {'previousFingerprint': stats['fingerprint'],
                                     'rawImageSha256': stats['imageSha256'],
                                     'previousRasterPassWasIdentity': True}
            stats['filledRasterPinholes'] = holes
            stats['fingerprint'] = build.fingerprint(pose)
            stats['imageSha256'] = sha(frame)
            measurement.write_text(json.dumps(stats, indent=2))
    if capture:
        SNAPSHOT.write_text(json.dumps(snapshot, indent=2))
