"""Study-only smooth upper-shaft volume before a single polar UV sample.

The normal-coordinate map is monotone. A compact band on the lower-ray side
protects the distal branch rather than choosing a hard owner or pasting it
back. There are no additional RGBA samples, layers, opacity edits or caps.
"""
import numpy as np
from continuous_polar_painter import parameters,uv_field,bilinear,SIZE


def prewarp(limb,pose,leg,points,peak=1.0):
    p=parameters(limb,pose,leg)
    points=np.asarray(points,dtype='float64')
    knee=p['targetKnee']
    upper_angle=p['targetAngle']
    axis=np.array([np.cos(upper_angle),np.sin(upper_angle)])
    normal=np.array([-axis[1],axis[0]])
    relative=points-knee
    axial=relative@axis
    transverse=relative@normal
    phase=np.clip((axial-p['radius'])/(p['targetLengths'][0]-p['radius']),0,1)
    # Explicit authoring fold, not an identity-pixel exception. Neutral/source
    # row0 has no deliberate soft-volume change at any proposed peak setting.
    fold=float(np.clip(pose.get('row',0)/8,0,1))
    amplitude=(peak-1)*fold*16*phase*phase*(1-phase)*(1-phase)
    lower_angle=upper_angle+p['targetArc']
    lower_axis=np.array([np.cos(lower_angle),np.sin(lower_angle)])
    lower_axial=float(lower_axis@axis)
    lower_normal=float(lower_axis@normal)
    side=1.0 if lower_normal>=0 else -1.0
    if lower_axial>1e-5:
        lower_distance=np.maximum(axial,0)*abs(lower_normal/lower_axial)
    else:
        lower_distance=np.full_like(axial,1000)
    # Smooth maximum provides a C1 positive band. Fourteen native pixels keep
    # the lower centreline and its full native tendon/blade tissue outside it.
    margin=14.0
    band=.5+2*np.logaddexp(0,(lower_distance-margin-.5)/2)
    signed=transverse*side
    q=signed/band
    toward=(signed>=0)
    support=np.maximum(0,1-q*q)
    compact=support*support
    compact_derivative=np.where((q>=0)&(q<1),-4*signed/(band*band)*support,0)*side
    gaussian=np.exp(-(transverse/16)**2)
    gaussian_derivative=-2*transverse/(16*16)*gaussian
    shape=np.where(toward,compact,gaussian)
    shape_derivative=np.where(toward,compact_derivative,gaussian_derivative)
    denominator=1+amplitude*shape
    mapped_normal=transverse/denominator
    determinant=(denominator-amplitude*transverse*shape_derivative)/(denominator*denominator)
    mapped=knee+axial[...,None]*axis+mapped_normal[...,None]*normal
    return mapped,determinant


def field_with_metrics(limb,pose,leg,points=None,peak=1.0):
    if points is None:
        yy,xx=np.mgrid[:SIZE,:SIZE]
        points=np.stack([xx,yy],axis=-1)
    mapped,q_jacobian=prewarp(limb,pose,leg,points,peak)
    from continuous_polar_painter import field_with_metrics as polar_field
    uv,metrics=polar_field(limb,pose,leg,mapped)
    metrics['prewarpJacobian']=q_jacobian
    metrics['jacobian']=metrics['jacobian']*q_jacobian
    return uv,metrics


def warp_rgba(source,limb,pose,leg,peak=1.0,points=None):
    uv,_=field_with_metrics(limb,pose,leg,points,peak)
    return bilinear(source,uv)
