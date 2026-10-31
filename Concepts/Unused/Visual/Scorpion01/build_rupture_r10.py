"""Export the existing rig as 12 real detached parts for every run/attack pose."""
from pathlib import Path
import json, math, hashlib
import numpy as np
from PIL import Image, ImageDraw
import build_native as rig

ROOT = Path(__file__).resolve().parent
ART = rig.ART
OUT = ROOT / 'rupture-r10'

def layers(p, surface):
    output = []
    for ident, limb in p['legs'].items():
        number = int(ident[1])
        template = ident[0] + ('F' if number == 1 else 'R' if number == 4 else 'M')
        image, _ = surface[template].render(rig.project(limb['joints3D']))
        output.append((ident, image))
    image, _ = surface['tail'].render(rig.project(p['tail']))
    output.append(('tail', image))
    cap = surface['jointCap'].resize((24,24), Image.Resampling.LANCZOS)
    mask = Image.new('L', (24,24))
    ImageDraw.Draw(mask).ellipse((1,1,22,22), fill=255)
    rgba = np.asarray(cap).copy()
    rgba[...,3] = np.minimum(rgba[...,3], np.asarray(mask))
    cap = Image.fromarray(rgba).resize((26,26), Image.Resampling.LANCZOS)
    elbow = cap.resize((32,32), Image.Resampling.LANCZOS)
    for side, palp in p['palps'].items():
        part = Image.new('RGBA', (1024,1024))
        for first, last in zip(palp['arm'], palp['arm'][1:]):
            image, _ = surface['arm'].render(rig.project([first,last]))
            part.alpha_composite(image)
        part.alpha_composite(rig.parts.render_chela(palp, side, rig.project, surface['chela']))
        x,y = rig.project(palp['arm'][1])
        part.alpha_composite(elbow, (round(x)-16, round(y)-16))
        output.append((side+'-pedipalp', part))
    core = Image.new('RGBA', (1024,1024))
    roots = [limb['joints3D'][0] for limb in p['legs'].values()]
    roots += [palp['arm'][0] for palp in p['palps'].values()]
    for root in roots:
        x,y = rig.project(root)
        core.alpha_composite(cap, (round(x)-13, round(y)-13))
    core.alpha_composite(rig.torso_surface(p))
    output.append(('core', core))
    return output

def main():
    OUT.mkdir(exist_ok=True)
    surface = rig.surfaces()
    packed, poses, checks = [], [], []
    for state, count in [('run',24), ('attack',32)]:
        for frame in range(count):
            t = frame/count if state == 'run' else sum(rig.ATTACK_MS[:frame])/sum(rig.ATTACK_MS)
            p = rig.pose(t, state)
            parts = layers(p, surface)
            assembled = Image.new('RGBA', (1024,1024))
            row = dict(state=state, frame=frame, parts=[])
            for name, image in parts:
                assembled.alpha_composite(image)
                bounds = image.getbbox()
                # Even crop coordinates preserve the exact 2:1 export registration.
                x0,y0,x1,y1 = bounds
                x0=max(0,(x0-4)//2*2); y0=max(0,(y0-4)//2*2)
                x1=min(1024,math.ceil((x1+4)/2)*2); y1=min(1024,math.ceil((y1+4)/2)*2)
                crop = image.crop((x0,y0,x1,y1)).resize(((x1-x0)//2,(y1-y0)//2), Image.Resampling.LANCZOS)
                ident=f'{state}-{frame:02d}-{name}'
                info=dict(name=ident, part=name, offset=[((x0+x1)/2-512)/400,(548-(y0+y1)/2)/400])
                packed.append((info,crop))
                row['parts'].append(info)
            reference=Image.open(ROOT/'frames'/state/f'{state}-{frame:02d}.png').convert('RGBA')
            a,b=np.asarray(assembled).astype(int),np.asarray(reference).astype(int)
            alpha_error=int(np.max(np.abs(a[...,3]-b[...,3])))
            changed=int(np.count_nonzero(np.any(np.abs(a-b)>4,axis=-1)))
            checks.append(dict(state=state,frame=frame,parts=len(parts),alphaError=alpha_error,changedPixelsOver4=changed))
            if frame in [0,6,12,18] or (state=='attack' and frame in [25,31]):
                assembled.resize((512,512),Image.Resampling.LANCZOS).save(OUT/f'{state}-{frame:02d}-assembled.png')
            poses.append(row)
            print(json.dumps(checks[-1]),flush=True)
    width=4096; x=y=row_height=0
    atlas=Image.new('RGBA',(width,4096))
    for info,crop in sorted(packed,key=lambda item:(-item[1].height,item[0]['name'])):
        if x+crop.width+4>width:
            x=0; y+=row_height; row_height=0
        if y+crop.height+4>4096:
            raise RuntimeError('Rupture atlas overflow')
        atlas.alpha_composite(crop,(x+2,y+2))
        info['rectTopLeft']=[x+2,y+2,crop.width,crop.height]
        x+=crop.width+4; row_height=max(row_height,crop.height+4)
    height=2**math.ceil(math.log2(y+row_height))
    atlas=atlas.crop((0,0,width,height))
    destination=ART/'RuptureR10.png'
    atlas.save(destination)
    data=dict(revision='r10-12-rig-parts',atlas=destination.relative_to(rig.PROJECT).as_posix(),size=list(atlas.size),ppu=200,
              poses=poses,partsPerPose=12,source='Existing R8 geometry and paint. No new anatomy or per-frame repaint.',
              sourcePoseOrigin=[512,548],nativePixelsPerUnit=400)
    (ROOT/'rupture-r10.json').write_text(json.dumps(data,indent=2)+'\n')
    (OUT/'reassembly.json').write_text(json.dumps(dict(checks=checks,sha256=hashlib.sha256(destination.read_bytes()).hexdigest(),
        scope='Before scattering. Root caps assigned to core once, elbow caps to whole pedipalps. Existing surfaces unchanged.'),indent=2)+'\n')
    print(json.dumps(dict(atlas=list(atlas.size),sprites=len(packed),maxChangedPixels=max(c['changedPixelsOver4'] for c in checks))))

if __name__=='__main__':
    main()
