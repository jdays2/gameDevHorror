import argparse
import json
from pathlib import Path
import numpy as np
from PIL import Image

parser = argparse.ArgumentParser(description='Compare Unity sprite triangle meshes with current PNG alpha silhouettes.')
parser.add_argument('--project', required=True, type=Path)
parser.add_argument('--meshes', required=True, type=Path)
parser.add_argument('--output', required=True, type=Path)
args = parser.parse_args()
project = args.project
source = args.meshes
rows = json.loads(source.read_text())
results = []
for row in rows:
    with Image.open(project / row['texture']) as im:
        alpha = np.array(im.convert('RGBA'))[:, :, 3]
    height, width = alpha.shape
    # Pixel centers in the texture's bottom-left coordinate system; 4-pixel diagnostic grid.
    step = 4
    x = np.arange(.5, width, step)
    y = np.arange(.5, height, step)
    xx, yy = np.meshgrid(x, y)
    opaque = alpha[height - 1 - y.astype(int)[:, None], x.astype(int)[None, :]] > 51
    vertices = np.array(row['vertices']) * row['ppu'] + np.array(row['pivot'])
    mesh = np.zeros(opaque.shape, dtype=bool)
    indices = row['triangles']
    for i in range(0, len(indices), 3):
        a, b, c = vertices[np.array(indices[i:i + 3])]
        d1 = (xx - a[0]) * (b[1] - a[1]) - (yy - a[1]) * (b[0] - a[0])
        d2 = (xx - b[0]) * (c[1] - b[1]) - (yy - b[1]) * (c[0] - b[0])
        d3 = (xx - c[0]) * (a[1] - c[1]) - (yy - c[1]) * (a[0] - c[0])
        mesh |= ((d1 >= 0) & (d2 >= 0) & (d3 >= 0)) | ((d1 <= 0) & (d2 <= 0) & (d3 <= 0))
    outside = int((mesh & ~opaque).sum())
    missed = int((opaque & ~mesh).sum())
    intersection = int((mesh & opaque).sum())
    union = int((mesh | opaque).sum())
    results.append({
        'prefab': row['prefab'], 'texture': row['texture'], 'vertices': len(vertices),
        'meshAreaSamples': int(mesh.sum()), 'opaqueAreaSamples': int(opaque.sum()),
        'outsideMeshFraction': outside / max(1, int(mesh.sum())),
        'missedOpaqueFraction': missed / max(1, int(opaque.sum())),
        'iou': intersection / max(1, union), 'gridStepPixels': step
    })
results.sort(key=lambda r: r['iou'])
args.output.write_text(json.dumps(results, indent=2), encoding='utf-8')
for r in results:
    print(f"{Path(r['texture']).name:38} verts={r['vertices']:4} IoU={r['iou']:.3f} outside={r['outsideMeshFraction']:.1%} missed={r['missedOpaqueFraction']:.1%}")
