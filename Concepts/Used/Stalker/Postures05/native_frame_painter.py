"""Whole native skin frames from a continuous, orientation-preserving UV field.

Offline authoring only. API returns linear-premultiplied float32 RGBA576x576:
SpritePainter(project).paint(pose) -> whole, six limbs, torso.
One full source limb is sampled once. No IK, caps, feathered part overlays,
hole filling, width inflation or target silhouette clipping is used.
The source body/rim has one explicit owner before any limb is transformed.
Import and construction do not write files or touch Unity.
"""
from pathlib import Path
import json
import numpy as np
from PIL import Image, ImageDraw, ImageFilter
from frame_colors import decode, over

SIZE = 576
PPU = 224.0
ORIGIN = np.array([288.0, 288.0], dtype=np.float32)
FLIP = np.diag([1.0, -1.0]).astype(np.float32)
JOINT_RADII = (12.5, 12.0, 10.0, 10.5, 12.0, 11.5)
ROOT_RADII = (13.0, 12.0, 9.0, 10.0, 12.0, 11.5)
TAU = 2 * np.pi

# Source pixels, native Walk00 crop(30,84,388,490). The lower boundary
# preserves its existing antialiased underbelly; it does not synthesize alpha.
BODY_OWNER = ((185, 123), (193, 125), (198, 132), (200, 143), (205, 151), (214, 167), (223, 183), (232, 201), (242, 217), (249, 224), (255, 231), (257, 238), (257, 247), (249, 252), (235, 254), (221, 255), (213, 255), (207, 252), (200, 250), (190, 247), (180, 247), (170, 250), (163, 253), (151, 254), (138, 253), (126, 251), (118, 248), (113, 242), (112, 235), (116, 227), (125, 219), (136, 200), (146, 181), (156, 164), (166, 148), (174, 137), (177, 129))

def _largest_component(binary):
    visited = np.zeros_like(binary, dtype=bool)
    largest = []
    height, width = binary.shape
    for y, x in zip(*np.nonzero(binary)):
        if visited[y, x]:
            continue
        pending = [(int(y), int(x))]
        visited[y, x] = True
        component = []
        while pending:
            py, px = pending.pop()
            component.append((py, px))
            for ny in range(max(0, py - 1), min(height, py + 2)):
                for nx in range(max(0, px - 1), min(width, px + 2)):
                    if binary[ny, nx] and not visited[ny, nx]:
                        visited[ny, nx] = True
                        pending.append((ny, nx))
        if len(component) > len(largest):
            largest = component
    result = np.zeros_like(binary, dtype=bool)
    if largest:
        yy, xx = zip(*largest)
        result[yy, xx] = True
    return result

def _clean_native_root(full, hip, radius):
    """Remove source-extraction dirt near the mount, never a target limb."""
    yy, xx = np.mgrid[:full.shape[0], :full.shape[1]]
    distance = np.hypot(xx - hip[0], yy - hip[1])
    proximal_zone = distance < radius + 30.0
    binary = Image.fromarray((full[..., 3] > 0.15).astype(np.uint8) * 255)
    opened = np.asarray(binary.filter(ImageFilter.MinFilter(3)).filter(ImageFilter.MaxFilter(3))) > 128
    original = full[..., 3] > 0.15
    candidate = np.where(proximal_zone, opened, original)
    main = _largest_component(candidate)
    # Restore a pixel of antialiasing around the retained tissue. Only the
    # proximal region is filtered, so a fine distal point cannot be removed.
    retained = np.asarray(Image.fromarray(main.astype(np.uint8) * 255).filter(ImageFilter.MaxFilter(3))) / 255.0
    keep = np.where(proximal_zone, retained, 1.0).astype(np.float32)
    cleaned = full * keep[..., None]
    removed = (full[..., 3] > 4.0 / 255.0) & (cleaned[..., 3] <= 4.0 / 255.0)
    ys, xs = np.nonzero(removed)
    trace = {'removedPixelsAlphaOver4': int(removed.sum()),
        'removedOpaquePixels': int(((full[..., 3] > 0.5) & (cleaned[..., 3] <= 0.5)).sum()),
        'sourceRemovedBounds': [int(xs.min()), int(ys.min()), int(xs.max() + 1), int(ys.max() + 1)] if len(xs) else None,
        'proximalRadiusPixels': radius + 30.0,
        'method': 'Local source opening, principal source chain component, one-pixel AA restoration; distal zone unchanged'}
    return cleaned, trace

def bilinear(source,uv):
    # Arithmetic noise at an exact source pose must not filter native pixels.
    uv=np.where(np.abs(uv-np.rint(uv))<1e-4,np.rint(uv),uv)
    x,y=uv[...,0],uv[...,1]
    x0,y0=np.floor(x).astype(int),np.floor(y).astype(int)
    fx,fy=x-x0,y-y0
    result=np.zeros((*x.shape,4),dtype='float32')
    for dx,dy,weight in [(0,0,(1-fx)*(1-fy)),(1,0,fx*(1-fy)),
                         (0,1,(1-fx)*fy),(1,1,fx*fy)]:
        xx,yy=x0+dx,y0+dy
        valid=(xx>=0)&(yy>=0)&(xx<source.shape[1])&(yy<source.shape[0])
        result[valid]+=source[yy[valid],xx[valid]]*weight[valid,None]
    return result

def parameters(limb, pose, leg, locked_radius=None):
    source_hip, source_knee, source_toe = [np.asarray(limb[key], dtype='float64')
                                        for key in ('hip', 'knee', 'tip')]
    target_hip, target_knee, target_toe = [ORIGIN + FLIP @ np.asarray(pose[key][leg][:2]) * PPU
                                        for key in ('hips', 'knees', 'feet')]
    source_axes = np.stack([source_hip - source_knee, source_toe - source_knee])
    target_axes = np.stack([target_hip - target_knee, target_toe - target_knee])
    source_lengths = np.linalg.norm(source_axes, axis=1)
    target_lengths = np.linalg.norm(target_axes, axis=1)
    radius = JOINT_RADII[leg] + 8 if locked_radius is None else locked_radius
    if min(*source_lengths, *target_lengths) <= radius + 1:
        raise ValueError('The native joint zone does not fit the projected shaft')
    source_angles = np.arctan2(source_axes[:, 1], source_axes[:, 0])
    target_angles = np.arctan2(target_axes[:, 1], target_axes[:, 0])
    source_arc = (source_angles[1] - source_angles[0]) % TAU
    target_arc = (target_angles[1] - target_angles[0]) % TAU
    if min(source_arc, target_arc, TAU - source_arc, TAU - target_arc) <= 1e-6:
        raise ValueError('Coincident bone rays cannot define this polar mapping')
    # Shared endpoint derivatives make both arcs periodic C1. This sufficient
    # Fritsch/Carlson circle bound limits both arcs together, without a cusp.
    slopes = target_lengths / source_lengths
    arc_means = [source_arc / target_arc, (TAU - source_arc) / (TAU - target_arc)]
    limiter = min(1.0, *(3 / np.linalg.norm(slopes / mean) for mean in arc_means))
    slopes *= limiter
    return {'sourceKnee': source_knee, 'targetKnee': target_knee,
            'sourceLengths': source_lengths, 'targetLengths': target_lengths,
            'sourceAngle': source_angles[0], 'targetAngle': target_angles[0],
            'sourceArc': source_arc, 'targetArc': target_arc,
            'radius': radius, 'slopes': slopes, 'slopeLimiter': limiter,
            'radialScales': (source_lengths - radius) / (target_lengths - radius)}

def hermite(t, length, value0, value1, slope0, slope1):
    t2, t3 = t * t, t * t * t
    value = ((2 * t3 - 3 * t2 + 1) * value0 + (t3 - 2 * t2 + t) * length * slope0
             + (-2 * t3 + 3 * t2) * value1 + (t3 - t2) * length * slope1)
    derivative = (((6 * t2 - 6 * t) * value0 + (3 * t2 - 4 * t + 1) * length * slope0
                   + (-6 * t2 + 6 * t) * value1 + (3 * t2 - 2 * t) * length * slope1) / length)
    return value, derivative

def field_with_metrics(limb, pose, leg, points=None, locked_radius=None):
    p = parameters(limb, pose, leg, locked_radius)
    if points is None:
        yy, xx = np.mgrid[:SIZE, :SIZE]
        points = np.stack([xx, yy], axis=-1).astype('float64')
    else:
        points = np.asarray(points, dtype='float64')
    relative = points - p['targetKnee']
    radius = np.linalg.norm(relative, axis=-1)
    angle = (np.arctan2(relative[..., 1], relative[..., 0]) - p['targetAngle']) % TAU
    inside = angle <= p['targetArc']
    arc_length = np.where(inside, p['targetArc'], TAU - p['targetArc'])
    t = np.where(inside, angle / p['targetArc'],
                 (angle - p['targetArc']) / (TAU - p['targetArc']))
    value0 = np.where(inside, 0, p['sourceArc'])
    value1 = np.where(inside, p['sourceArc'], TAU)
    slope0 = np.where(inside, p['slopes'][0], p['slopes'][1])
    slope1 = np.where(inside, p['slopes'][1], p['slopes'][0])
    mapped_angle, angular_derivative = hermite(t, arc_length, value0, value1, slope0, slope1)
    mapped_angle += p['sourceAngle']
    smooth = t * t * (3 - 2 * t)
    scale0 = np.where(inside, p['radialScales'][0], p['radialScales'][1])
    scale1 = np.where(inside, p['radialScales'][1], p['radialScales'][0])
    radial_scale = scale0 + (scale1 - scale0) * smooth
    mapped_radius = np.where(radius <= p['radius'], radius,
                             p['radius'] + (radius - p['radius']) * radial_scale)
    radial_derivative = np.where(radius <= p['radius'], 1, radial_scale)
    uv = p['sourceKnee'] + mapped_radius[..., None] * np.stack(
        [np.cos(mapped_angle), np.sin(mapped_angle)], axis=-1)
    centre = radius <= 1e-12
    ratio = np.divide(mapped_radius, radius, out=np.ones_like(radius), where=~centre)
    determinant = ratio * radial_derivative * angular_derivative
    determinant = np.where(centre, np.nan, determinant)
    return uv, {'jacobian': determinant, 'polarCentre': centre,
                'angularDerivative': angular_derivative, 'radialDerivative': radial_derivative,
                'radius': radius, 'parameters': p}

def uv_field(limb, pose, leg, inner=None, outer=None, points=None):
    # API-compatible optional outer means the native locked joint radius.
    return field_with_metrics(limb, pose, leg, points, outer)[0]

def warp_rgba(source, limb, pose, leg, inner=None, outer=None, points=None):
    """Sample one full source RGBA, including signed RGB delta layers, once."""
    return bilinear(source, uv_field(limb, pose, leg, inner, outer, points))

class SpritePainter:
    """Read-only native materials and one registered canvas for all poses."""
    def __init__(self, project_path):
        self.project_path = Path(project_path).resolve()
        source_folder = self.project_path / 'Concepts/Used/Stalker/Locomotion02'
        self.anatomy = json.loads((source_folder / 'anatomy.json').read_text())
        atlas_path = self.project_path / 'Assets/EmergencyShift/Art/Enemies/Stalker/Locomotion02/rig-atlas.png'
        with Image.open(atlas_path) as image:
            atlas = image.convert('RGBA')

        def layer(slot):
            x, y = slot % 4 * 358, slot // 4 * 406
            return decode(atlas.crop((x, y, x + 358, y + 406)))

        owner_image = Image.new('L', (358, 406))
        ImageDraw.Draw(owner_image).polygon(BODY_OWNER, fill=255)
        owner = np.asarray(owner_image) > 0
        owner.flags.writeable = False
        self.body_source_owner = owner
        raw_sources, sources, trace = [], [], []
        for leg, limb in enumerate(self.anatomy['limbs']):
            raw = layer(leg * 2 + 1)
            clean, cleanup = _clean_native_root(raw, np.asarray(limb['hip']), ROOT_RADII[leg])
            source = clean * (~owner)[..., None]
            cleanup['removedBodyOwnedPixelsAlphaOver4'] = int(((clean[..., 3] > 4 / 255) & owner).sum())
            raw.flags.writeable = False
            source.flags.writeable = False
            raw_sources.append(raw)
            sources.append(source)
            trace.append({'leg': leg, 'id': limb['id'], **cleanup})
        self.raw_sources = tuple(raw_sources)
        self.sources = tuple(sources)
        self.source_cleanup = tuple(trace)
        original_path = self.project_path / 'Assets/EmergencyShift/Art/Enemies/Stalker/PaleFlesh/walk-sheet.png'
        with Image.open(original_path) as original:
            native = decode(original.convert('RGBA').crop((30, 84, 388, 490)))
        self._torso = np.zeros((SIZE, SIZE, 4), dtype=np.float32)
        x, y = (ORIGIN - np.asarray(self.anatomy['pivot'])).astype(int)
        self._torso[y:y + 406, x:x + 358] = native * owner[..., None]
        self._torso.flags.writeable = False
        yy, xx = np.mgrid[:SIZE, :SIZE]
        self._grid = np.stack([xx, yy], axis=-1).astype('float64')
        self._grid.flags.writeable = False

    def paint(self, pose):
        limbs = []
        whole = np.zeros((SIZE, SIZE, 4), dtype=np.float32)
        for leg, limb in enumerate(self.anatomy['limbs']):
            skin = warp_rgba(self.sources[leg], limb, pose, leg, points=self._grid)
            limbs.append(skin)
            whole = over(skin, whole)
        return over(self._torso, whole), limbs, self._torso
