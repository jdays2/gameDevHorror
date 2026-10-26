"""Keep painted flesh on its own bone chains before composing whole sprites.

The historical polar skin mapping stretches small attachment scraps sideways.
This pass trims those scraps; it does not remove a crossed leg or alter joints.
"""
from pathlib import Path
import json
import numpy as np
from PIL import Image

OUT=Path(__file__).resolve().parent
(OUT/'FixedRaw').mkdir(exist_ok=True)
(OUT/'CleanMasks').mkdir(exist_ok=True)
poses=json.loads((OUT/'poses.json').read_text())
yy,xx=np.mgrid[:576,:576]
points=np.stack([(xx-288)/224,(288-yy)/224],axis=-1).astype('float32')
def decode(image):
    rgba=np.array(image).astype('float32')/255
    rgb=rgba[...,:3]
    return np.where(rgb<=.04045,rgb/12.92,((rgb+.055)/1.055)**2.4),rgba[...,3]
def encode(rgb,alpha):
    rgb=np.clip(rgb,0,1)
    srgb=np.where(rgb<=.0031308,rgb*12.92,1.055*rgb**(1/2.4)-.055)
    return Image.fromarray((np.concatenate([srgb,alpha[...,None]],axis=-1)*255).round().astype('uint8'))
def corridor(a,b,r0,r1,grid):
    a=np.array(a[:2],dtype='float32'); b=np.array(b[:2],dtype='float32'); axis=b-a
    t=np.clip(np.sum((grid-a)*axis,axis=-1)/max(np.dot(axis,axis),1e-8),0,1)
    distance=np.linalg.norm(grid-(a+t[...,None]*axis),axis=-1)
    radius=r0+(r1-r0)*t
    return np.clip((radius-distance)*224+1,0,1)
torso_rgb,torso_alpha=decode(Image.open(OUT/'Masks/torso.png').convert('RGBA'))
stats=[]
for pose in poses:
    row,phase=pose['row'],pose['phase']
    rgb=np.zeros_like(torso_rgb)
    alpha=np.zeros_like(torso_alpha)
    for leg in range(6):
        image=Image.open(OUT/'Masks'/f'leg-{row}-{phase:02d}-{leg}.png').convert('RGBA')
        box=image.getchannel('A').point(lambda value:255 if value>8 else 0).getbbox()
        x0,y0,x1,y1=box
        x0,y0,x1,y1=max(0,x0-2),max(0,y0-2),min(576,x1+2),min(576,y1+2)
        paint,a=decode(image.crop((x0,y0,x1,y1)))
        grid=points[y0:y1,x0:x1]
        hip,knee,foot=pose['hips'][leg],pose['knees'][leg],pose['feet'][leg]
        keep=np.maximum(corridor(hip,knee,.08,.064,grid),corridor(knee,foot,.062,.018,grid))
        trimmed=int(((a>.5)&(keep<.5)).sum())
        paint*=keep[...,None]
        a*=keep
        full=Image.new('RGBA',(576,576))
        full.paste(encode(paint,a),(x0,y0))
        full.save(OUT/'CleanMasks'/f'leg-{row}-{phase:02d}-{leg}.png',compress_level=1)
        rgb[y0:y1,x0:x1]=paint+rgb[y0:y1,x0:x1]*(1-a[...,None])
        alpha[y0:y1,x0:x1]=a+alpha[y0:y1,x0:x1]*(1-a)
        stats.append({'row':row,'phase':phase,'leg':leg,'trimmedPixels':trimmed})
    rgb=torso_rgb+rgb*(1-torso_alpha[...,None])
    alpha=torso_alpha+alpha*(1-torso_alpha)
    encode(rgb,alpha).save(OUT/'FixedRaw'/f'body-{row}-{phase:02d}.png',compress_level=1)
Image.open(OUT/'Masks/torso.png').save(OUT/'CleanMasks/torso.png')
(OUT/'paint-corridors.json').write_text(json.dumps({'upperRadii':[.08,.064],'lowerRadii':[.062,.018],
 'purpose':'Trim stretched root scraps around the same six fixed-length chains; no joint positions changed',
 'trimmedOpaquePixels':sum(s['trimmedPixels'] for s in stats),'samples':stats},indent=2))
print('Composed 432 sprites from trimmed limb paint')
