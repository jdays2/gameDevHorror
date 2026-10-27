"""Byte regression of standalone canonical painter against frozen AH04 skin."""
import hashlib
import json
import numpy as np
from PIL import Image
from continuous_polar_painter import SpritePainter as StudyCache
from source_ownership_refined import owned_materials
from source_ownership_study import HERE,FAMILY,PROJECT,digest
from native_frame_painter import SpritePainter,warp_rgba
from frame_colors import encode
from prove_continuous_polar import source_pose


def main():
    folder=HERE/'ActualHeldOut04'
    poses=json.loads((folder/'poses.json').read_text())
    painter=SpritePainter(PROJECT)
    study=StudyCache(PROJECT)
    expected_sources,expected_torso,owner,native=owned_materials(study)
    assert all(np.array_equal(a,b) for a,b in zip(painter.sources,expected_sources))
    assert all(np.array_equal(a,b) for a,b in zip(painter.raw_sources,study.raw_sources))
    assert np.array_equal(painter._torso,expected_torso)
    assert all(not item.flags.writeable for item in painter.sources+painter.raw_sources)
    assert not painter._torso.flags.writeable
    identity=[]
    neutral=source_pose(painter)
    from native_frame_painter import ORIGIN
    offset=(ORIGIN-np.asarray(painter.anatomy['pivot'])).astype(int)
    for leg,limb in enumerate(painter.anatomy['limbs']):
        got=np.asarray(encode(warp_rgba(painter.raw_sources[leg],limb,neutral,leg)))
        expected=np.zeros((576,576,4),dtype='uint8')
        expected[offset[1]:offset[1]+406,offset[0]:offset[0]+358]=np.asarray(encode(painter.raw_sources[leg]))
        identity.append(int(np.count_nonzero(got!=expected)))
    assert not any(identity)
    print('READY canonical sources/torso float-exact, readonly; native identity all6 byte-exact',flush=True)
    records=[]
    for index,pose in enumerate(poses):
        name=pose['name']
        whole,limbs,torso=painter.paint(pose)
        stem=f'{pose["row"]}-{pose["phase"]:02d}' if 'row' in pose else name
        errors=[]
        hidden_errors=[]
        for leg,limb in enumerate(limbs):
            expected=np.asarray(Image.open(folder/'Masks'/f'leg-{stem}-{leg}.png').convert('RGBA'))
            got=np.asarray(encode(limb))
            visible=(got[...,3]>0)|(expected[...,3]>0)
            changed=got!=expected
            errors.append(int(np.count_nonzero(changed & visible[...,None]))
                +int(np.count_nonzero(got[...,3]!=expected[...,3])))
            hidden_errors.append(int(np.count_nonzero(changed & (~visible)[...,None])))
        whole_error=None
        if pose.get('group')!='strike':
            expected=np.asarray(Image.open(folder/'Raw'/f'{name}.png').convert('RGBA'))
            got=np.asarray(encode(whole))
            visible=(got[...,3]>0)|(expected[...,3]>0)
            whole_error=int(np.count_nonzero((got!=expected)&visible[...,None]))+int(np.count_nonzero(got[...,3]!=expected[...,3]))
        record={'name':name,'visibleLimbByteDifferences':errors,'alphaZeroHiddenRgbByteDifferences':hidden_errors,
            'visibleWholeByteDifferences':whole_error}
        records.append(record)
        assert not any(errors) and not whole_error,record
        if index%8==7:
            print(f'Canonical pixel equality {index+1}/{len(poses)}',flush=True)
    report={'canonicalModuleSha256':digest(FAMILY/'native_frame_painter.py'),
        'referenceManifestSha256':digest(folder/'manifest.json'),'frames':len(poses),
        'sourcesAndTorsoFloatExact':True,'sourcesReadonly':True,'rawIdentityByteDifferences':identity,
        'visibleLimbByteDifferences':sum(sum(r['visibleLimbByteDifferences']) for r in records),
        'visibleWholeByteDifferences':sum(r['visibleWholeByteDifferences'] or 0 for r in records),
        'alphaZeroRgbDifferenceReason':'AH04 reused body PNG cache through decode/encode, which clears RGB at quantized alpha0. Canonical preserves freshly sampled hidden RGB; alpha and all visible RGBA must match exactly.',
        'strikeWholeExcludedReason':'Native mouth overlay is separately owned and changed after frozen AH04; all6 strike limbs still compared.',
        'cases':records}
    (folder/'canonical-pixel-regression.json').write_text(json.dumps(report,indent=2))
    print('DONE canonical all65 visibleRGBA/alpha byte equality; strike mouth excluded',flush=True)


if __name__=='__main__':
    main()
