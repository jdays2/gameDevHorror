"""Recheck frozen full-family inputs and record final manual-review coverage."""
from pathlib import Path
import hashlib
import json
from audit_native_family import validate_composition, digest

HERE = Path(__file__).resolve().parent
FAMILY = HERE.parent


def main():
    report = {}
    for kind, folder in [('body', 'FinalBody432QA01'), ('attack', 'FinalAttack792QA01')]:
        poses, _, composition_sha = validate_composition(kind)
        summary = json.loads((HERE/folder/'summary.json').read_text())
        assert composition_sha == summary['compositionManifestHashes'][kind]
        raw_digest, mask_digest, torso_digest = [hashlib.sha256() for _ in range(3)]
        for pose in poses:
            name = pose['name'] if kind == 'attack' else f'body-{pose["row"]}-{pose["phase"]:02d}'
            stem = name if kind == 'attack' else f'{pose["row"]}-{pose["phase"]:02d}'
            raw = FAMILY/'Raw'/f'{name}.png'
            raw_digest.update(raw.name.encode())
            raw_digest.update(raw.read_bytes())
            for leg in range(6):
                path = FAMILY/'Masks'/f'leg-{stem}-{leg}.png'
                mask_digest.update(path.name.encode())
                mask_digest.update(path.read_bytes())
            path = FAMILY/'Masks'/(f'torso-{name}.png' if kind == 'attack' else 'torso.png')
            torso_digest.update(name.encode())
            torso_digest.update(path.read_bytes())
        assert raw_digest.hexdigest() == summary['rawImagesSha256']
        assert mask_digest.hexdigest() == summary['masksSha256']
        checks = json.loads((HERE/folder/f'{kind}-checks.json').read_text())
        assert len(checks) == len(poses)
        assert all(len(check['all15Pairs']) == 15 for check in checks)
        report[kind] = {'frames': len(poses), 'compositionManifestSha256': composition_sha,
            'rawImagesSha256': raw_digest.hexdigest(), 'legMasksSha256': mask_digest.hexdigest(),
            'torsoMasksSha256': torso_digest.hexdigest(), 'all15PairsTested': len(checks)*15,
            'nonzeroContactFrames': len({v['name'] for v in summary['pairCandidates']}),
            'nonzeroContactPixelsSummedAcrossFrames': sum(v['pixels'] for v in summary['pairCandidates']),
            'jacobianMinimum': min(leg['jacobianMinimum'] for check in checks for leg in check['limbs']),
            'nonpositiveJacobianPixels': summary['nonpositiveJacobianPixels'],
            'boundedHoleRecords': len(summary['boundedHoleCandidates']),
            'wholeBorderPositiveAlphaPixels': summary['borderAlphaPositivePixels'],
            'erode4AlphaBelow230Pixels': summary['erode4AlphaBelow230Pixels']}
    art = HERE/'FinalAttack792Art01'
    proof = json.loads((HERE/'FinalAttack792QA01/weak-alpha-and-review-integrity.json').read_text())
    for path, expected in proof['pageSha256'].items():
        assert digest(art/path) == expected
    progress = {bg: {'entryPhases': [0, 47], 'strikeSamples': [0, 23], 'returnPhases': [0, 47],
        'framesViewed': 792, 'nativePagesViewed': 99,
        'finding': 'All native pages actually inspected; no mandatory visible skin, attachment, blade or mouth-edge artifact found.'}
        for bg in ['green', 'dark', 'light']}
    progress['status'] = 'Complete: static attack artwork PASS for frozen composition; runtime cadence and transitions are separate.'
    (art/'review-progress.json').write_text(json.dumps(progress, indent=2))
    report['attackArt'] = {'reviewedUniqueFrames': 792, 'backgroundInstances': 2376,
        'nativePagesActuallyViewed': 297, 'pageHashesUnchanged': True, 'pixelsPerUnit': 224,
        'fixedCrop': [132, 63, 446, 520], 'resizing': False, 'staticVerdict': 'PASS',
        'runtimeReview': 'Separate; not inferred from these static pages.'}
    (HERE/'final-review-integrity.json').write_text(json.dumps(report, indent=2))
    print(json.dumps(report, indent=2))


if __name__ == '__main__':
    main()
