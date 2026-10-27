"""Study only: one continuous polar UV for each whole native limb.

There are no competing axial charts, RGBA blends, caps, fills or target masks.
The angular map is periodic monotone C1 Hermite. Positive angular and radial
derivatives preserve orientation for r>0; the polar origin is reported apart.
"""
import numpy as np

from native_uv_painter import SpritePainter as SourceCache
from native_uv_painter import bilinear, ORIGIN, FLIP, PPU, SIZE, JOINT_RADII, _over

TAU = 2 * np.pi


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


class SpritePainter(SourceCache):
    def paint(self, pose):
        limbs = []
        whole = np.zeros((SIZE, SIZE, 4), dtype='float32')
        for leg, limb in enumerate(self.anatomy['limbs']):
            skin = warp_rgba(self.sources[leg], limb, pose, leg, points=self._grid)
            limbs.append(skin)
            whole = _over(skin, whole)
        return _over(self._torso, whole), limbs, self._torso
