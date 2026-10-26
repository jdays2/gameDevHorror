"""Close only tiny enclosed raster pinholes, retaining all open limb gaps."""
import numpy as np
from PIL import Image


def tiny_holes(alpha, limit=16, threshold=128):
    mask = alpha<threshold
    height, width = mask.shape
    parents = []
    runs = []
    previous = []

    def root(label):
        while parents[label]!=label:
            parents[label] = parents[parents[label]]
            label = parents[label]
        return label

    for y in range(height):
        edges = np.flatnonzero(np.diff(np.r_[False, mask[y], False]))
        current = []
        cursor = 0
        for start, end in zip(edges[::2], edges[1::2]):
            label = len(parents)
            parents.append(label)
            while cursor<len(previous) and previous[cursor][1]<=start:
                cursor += 1
            other = cursor
            while other<len(previous) and previous[other][0]<end:
                parents[root(previous[other][2])] = root(label)
                other += 1
            current.append((int(start), int(end), label))
            runs.append((label, y, int(start), int(end)))
        previous = current
    groups = {}
    for label, y, start, end in runs:
        group = groups.setdefault(root(label), {'size': 0, 'edge': False, 'runs': []})
        group['size'] += end-start
        group['edge'] |= y in [0, height-1] or start==0 or end==width
        group['runs'].append((y, start, end))
    return [group['runs'] for group in groups.values() if not group['edge'] and group['size']<=limit]


def finish(image):
    rgba = np.array(image.convert('RGBA'))
    source = rgba.copy()
    cores = tiny_holes(rgba[..., 3])
    holes = {}
    for core in cores:
        seed_y, seed_x, _ = core[0]
        low, high = 128, 240
        best = core
        # Find the largest still-enclosed AA component around the real core.
        # Once it connects to the open silhouette gap, stop expanding it.
        while low<=high:
            threshold = (low+high)//2
            candidates = tiny_holes(rgba[..., 3], threshold=threshold)
            matched = next((runs for runs in candidates if any(
                y==seed_y and start<=seed_x<end for y, start, end in runs)), None)
            if matched is None:
                high = threshold-1
            else:
                best = matched
                low = threshold+1
        holes[tuple(best)] = best
    records = []
    for runs in holes.values():
        points = [(x, y) for y, start, end in runs for x in range(start, end)]
        for x, y in points:
            neighbors = []
            weights = []
            for dy in range(-2, 3):
                for dx in range(-2, 3):
                    if (dx or dy) and source[y+dy, x+dx, 3]>=240:
                        neighbors.append(source[y+dy, x+dx, :3])
                        weights.append(1./(dx*dx+dy*dy))
            assert neighbors
            rgba[y, x, :3] = np.rint(np.average(neighbors, axis=0, weights=weights)).astype(np.uint8)
            rgba[y, x, 3] = 255
        records.append({'pixels': len(points), 'bbox': [min(x for x, y in points), min(y for x, y in points),
                                                       max(x for x, y in points)+1, max(y for x, y in points)+1]})
    return (Image.fromarray(rgba) if holes else image), records


if __name__=='__main__':
    from pathlib import Path
    for path in sorted(Path(__file__).parent.glob('*/frames/*.png')):
        _, holes = finish(Image.open(path))
        if holes:
            print(path.parent.parent.name, path.name, holes)
