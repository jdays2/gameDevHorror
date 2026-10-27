"""Trace native mouth alpha independently of limb geometry; diagnostics only."""
from pathlib import Path
import json
import numpy as np
from PIL import Image, ImageFilter
from attack_mouth import AttackMouth, encode

FAMILY = Path(__file__).resolve().parent


def main():
    output = FAMILY / 'MouthEdgeStudy'
    output.mkdir(exist_ok=True)
    mouth = AttackMouth()
    poses = json.loads((FAMILY / 'attack-poses.json').read_bytes())
    pose = next(p for p in poses if p['name'] == 'strike-09')
    patch = mouth.layer(pose)
    report = {}
    for name, material in [('registered-source', mouth.source_patch), ('strike-09-layer', patch)]:
        alpha = np.asarray(encode(material))[..., 3]
        solid = alpha > 128
        values = {}
        for radius in [1, 2, 3, 4]:
            expanded = np.asarray(Image.fromarray(solid.astype('uint8') * 255).filter(ImageFilter.MaxFilter(radius * 2 + 1))) > 0
            ghost = (alpha > 1) & ~expanded
            values[str(radius)] = {'pixelsOutsideSolidExpansion': int(ghost.sum()),
                                  'maximumAlpha': int(alpha[ghost].max()) if ghost.any() else 0}
        encode(material, green=True).save(output / f'{name}-green.png')
        encode(material).save(output / f'{name}.png')
        Image.fromarray(alpha).save(output / f'{name}-alpha.png')
        report[name] = values
    (output / 'alpha-trace.json').write_text(json.dumps(report, indent=2))
    print(json.dumps(report, indent=2))


if __name__ == '__main__':
    main()
