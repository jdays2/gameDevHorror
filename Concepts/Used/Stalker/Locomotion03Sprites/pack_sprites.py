"""Deterministic alpha repair and packing of the offline composed body frames."""
from pathlib import Path
from collections import deque
import hashlib
import json
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

OUT = Path(__file__).resolve().parent
PROJECT = OUT.parents[3]
ART = PROJECT/'Assets/EmergencyShift/Art/Enemies/Stalker/Locomotion03Sprites'
ART.mkdir(parents=True, exist_ok=True)
(OUT/'Clean').mkdir(exist_ok=True)
(OUT/'Review').mkdir(exist_ok=True)

def components(mask):
    """Run-length connected components; eight-connected, without optional packages."""
    parents = []
    spans = []
    previous = []
    def root(i):
        while parents[i] != i:
            parents[i] = parents[parents[i]]
            i = parents[i]
        return i
    for y, row in enumerate(mask):
        edges = np.flatnonzero(np.diff(np.r_[False,row,False]))
        current = []
        for left,right in edges.reshape(-1,2):
            index = len(parents)
            parents.append(index)
            spans.append((y,left,right))
            for a,b,j in previous:
                if b >= left and a <= right:
                    parents[root(j)] = root(index)
            current.append((left,right,index))
        previous = current
    groups = {}
    for i,span in enumerate(spans):
        groups.setdefault(root(i), []).append(span)
    return sorted(groups.values(), key=lambda g: sum(b-a for y,a,b in g), reverse=True)

def paint_nearest(rgba, fill):
    valid = rgba[...,3] > 8
    colors = rgba[...,:3].copy()
    needed = fill.copy()
    for _ in range(32):
        if not needed.any():
            break
        next_valid = valid.copy()
        for dy,dx in [(0,1),(0,-1),(1,0),(-1,0)]:
            shifted = np.roll(valid,(dy,dx),(0,1))
            take = needed & shifted
            colors[take] = np.roll(colors,(dy,dx),(0,1))[take]
            next_valid[take] = True
            needed[take] = False
        valid = next_valid
    if needed.any():
        raise RuntimeError('A repair exceeds the local colour neighbourhood')
    rgba[fill,:3] = colors[fill]
    rgba[fill,3] = 255

manifest = {'phases':48, 'closureRows':9, 'pixelsPerUnit':224, 'frames':[], 'atlases':[]}
repairs = []
images = []
anatomy = json.loads((OUT.parent/'Locomotion02/anatomy.json').read_text())
yy,xx = np.mgrid[:512,:512]
roots = np.zeros((512,512),dtype=bool)
for limb in anatomy['limbs']:
    hx,hy = limb['hip']
    roots |= (xx-(256+hx-187))**2 + (yy-(256+hy-200))**2 <= 24**2
for path in sorted((OUT/'Raw').glob('body-*.png')):
    rgba = np.array(Image.open(path).convert('RGBA'))
    # GPU blending onto alpha-zero stores premultiplied RGB. Restore straight
    # alpha before compositing the result as a sprite a second time.
    alpha = rgba[...,3].astype(float)/255
    srgb = rgba[...,:3].astype(float)/255
    linear = np.where(srgb<=.04045, srgb/12.92, ((srgb+.055)/1.055)**2.4)
    linear = np.clip(linear/np.maximum(alpha[...,None],1/255),0,1)
    straight = np.where(linear<=.0031308, linear*12.92, 1.055*linear**(1/2.4)-.055)
    rgba[...,:3] = (straight*255).round().astype('uint8')
    original = rgba[...,3].copy()
    groups = components(original > 8)
    removed = 0
    for group in groups[1:]:
        count = sum(b-a for y,a,b in group)
        if count > 64:
            raise RuntimeError(f'{path.name}: disconnected major part ({count}px)')
        for y,a,b in group:
            rgba[max(0,y-1):y+2,max(0,a-1):b+1,3] = 0
        removed += count
    # Trim one-pixel skin whiskers while preserving the larger painted contour.
    opened = np.array(Image.fromarray(rgba[...,3]).filter(ImageFilter.MinFilter(3)).filter(ImageFilter.MaxFilter(3)))
    rgba[...,3] = np.minimum(rgba[...,3], opened)
    # Opening may detach the end of a pre-existing skin whisker. Remove that
    # fragment as well, before repairing the anatomical attachment neighbourhood.
    for group in components(rgba[...,3]>8)[1:]:
        count = sum(b-a for y,a,b in group)
        if count > 128:
            raise RuntimeError(f'{path.name}: opening split a real limb ({count}px)')
        for y,a,b in group:
            rgba[max(0,y-1):y+2,max(0,a-1):b+1,3] = 0
        removed += count
    closed = np.array(Image.fromarray(rgba[...,3]).filter(ImageFilter.MaxFilter(7)).filter(ImageFilter.MinFilter(7)))
    seams = roots & (closed>200) & (rgba[...,3]<128)
    paint_nearest(rgba, seams)
    holes = np.zeros(alpha.shape,dtype=bool)
    for group in components(rgba[...,3] <= 8):
        count = sum(b-a for y,a,b in group)
        if count <= 128 and all(y not in (0,511) and a>0 and b<512 for y,a,b in group):
            for y,a,b in group:
                holes[y,a:b] = True
    paint_nearest(rgba, holes)
    # Alpha below the visibility threshold must remain close to the connected
    # body. Otherwise GPU antialiasing leaves nearly invisible detached hairs.
    connected_edge = np.array(Image.fromarray((rgba[...,3]>8).astype('uint8')*255).filter(ImageFilter.MaxFilter(3)))>0
    rgba[~connected_edge,3] = 0
    # Extend RGB into the transparent edge for bilinear filtering/compression.
    edge = np.array(Image.fromarray((rgba[...,3]>8).astype('uint8')*255).filter(ImageFilter.MaxFilter(5)))>0
    pad = edge & (rgba[...,3]<=8)
    saved_alpha = rgba[...,3].copy()
    paint_nearest(rgba, pad)
    rgba[...,3] = saved_alpha
    image = Image.fromarray(rgba)
    image.save(OUT/'Clean'/path.name)
    bbox = image.getbbox()
    x0,y0,x1,y1 = bbox
    bbox = (max(0,x0-3),max(0,y0-3),min(512,x1+3),min(512,y1+3))
    images.append((path.stem, image.crop(bbox), bbox))
    repairs.append({'frame':path.stem,'detachedPixelsRemoved':int(removed),'holesFilledPixels':int(holes.sum()),
                    'attachmentSeamPixelsFilled':int(seams.sum()),
                    'edgePixelsTrimmed':int(((original>8)&(rgba[...,3]<=8)).sum()),
                    'componentsAfter':len(components(rgba[...,3]>8))})

atlas = Image.new('RGBA',(4096,4096))
atlas_index = 0
x=y=row_height=0
def save_atlas():
    name=f'body-{atlas_index:02d}.png'
    atlas.save(ART/name)
    manifest['atlases'].append({'asset':str((ART/name).relative_to(PROJECT)).replace('\\','/'), 'sha256':hashlib.sha256((ART/name).read_bytes()).hexdigest()})
for name,image,bbox in images:
    if x+image.width>4096:
        x=0
        y+=row_height
        row_height=0
    if y+image.height>4096:
        save_atlas()
        atlas_index+=1
        atlas=Image.new('RGBA',(4096,4096))
        x=y=row_height=0
    atlas.paste(image,(x,y))
    _,closure,phase=name.split('-')
    manifest['frames'].append({'name':name,'closure':int(closure),'phase':int(phase),'atlas':atlas_index,
        'rect':[x,4096-y-image.height,image.width,image.height],
        'pivot':[(256-bbox[0])/image.width,(bbox[3]-256)/image.height]})
    x+=image.width
    row_height=max(row_height,image.height)
save_atlas()
(OUT/'sprites.json').write_text(json.dumps(manifest,indent=2))
(OUT/'alpha-repairs.json').write_text(json.dumps(repairs,indent=2))

# Every one of the 432 poses is available in green-background contact sheets.
for closure in range(9):
    sheet = Image.new('RGB',(1600,960),'#00ff00')
    draw = ImageDraw.Draw(sheet)
    for phase in range(48):
        image = Image.open(OUT/'Clean'/f'body-{closure}-{phase:02d}.png').convert('RGBA')
        image.thumbnail((192,144),Image.Resampling.LANCZOS)
        x,y=(phase%8)*200,(phase//8)*160
        sheet.paste(image,(x+(200-image.width)//2,y+14),image)
        draw.text((x+5,y+2),f'close {closure}/8 | {phase:02d}/47',fill='black')
    sheet.save(OUT/'Review'/f'closure-{closure}.jpg',quality=96)
print(json.dumps({'frames':len(images),'atlases':atlas_index+1,'islandsRemoved':sum(r['detachedPixelsRemoved'] for r in repairs),
 'holePixelsFilled':sum(r['holesFilledPixels'] for r in repairs),'maxComponents':max(r['componentsAfter'] for r in repairs)}))
