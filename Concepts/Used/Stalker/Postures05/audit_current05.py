"""Independent read-only audit of imported Postures04 and Sleeper originals.

Only writes the sibling BeforeAudit evidence directory. No Unity or asset edits.
Annotations and sprite extraction preserve original source pixels and pivots.
"""
from pathlib import Path
import hashlib
import json
import math
import re
import numpy as np
from PIL import Image, ImageDraw, ImageFont, ImageFilter

HERE = Path(__file__).resolve().parent
PROJECT = HERE.parents[3]
OLD = HERE.parent / 'Postures04'
OUT = HERE / 'BeforeAudit'
OUT.mkdir(exist_ok=True)
FONT = ImageFont.truetype('C:/Windows/Fonts/arial.ttf', 18)
SMALL = ImageFont.truetype('C:/Windows/Fonts/arial.ttf', 13)
GREEN = (0, 255, 0, 255)
CANVAS = 576
PPU = 224


def digest(image):
    # Alpha-zero RGB is padding and is excluded from visible duplicate checks.
    array = np.asarray(image).copy()
    array[array[..., 3] == 0] = 0
    return hashlib.sha256(array.tobytes()).hexdigest()


def green(image):
    bg = Image.new('RGBA', image.size, GREEN)
    return Image.alpha_composite(bg, image).convert('RGB')


def point(value):
    return (CANVAS / 2 + value[0] * PPU, CANVAS / 2 - value[1] * PPU)


def projected_fold(pose, leg):
    hip = np.asarray(pose['hips'][leg][:2])
    knee = np.asarray(pose['knees'][leg][:2])
    foot = np.asarray(pose['feet'][leg][:2])
    a = hip - knee
    b = foot - knee
    return math.degrees(math.acos(float(np.clip(np.dot(a, b) / (np.linalg.norm(a) * np.linalg.norm(b)), -1, 1))))


def distance_to_segment(points, a, b):
    axis = b - a
    t = np.clip(np.sum((points - a) * axis, axis=-1) / max(float(np.dot(axis, axis)), 1e-8), 0, 1)
    return np.linalg.norm(points - (a + t[..., None] * axis), axis=-1)


def extract_current():
    manifest = json.loads((OLD / 'sprites.json').read_text())
    poses = json.loads((OLD / 'poses.json').read_text())
    textures = [Image.open(PROJECT / entry['asset']).convert('RGBA') for entry in manifest['atlases']]
    frames = {}
    report = []
    for entry, pose in zip(manifest['frames'], poses):
        x, y, w, h = entry['rect']
        texture = textures[entry['atlas']]
        crop = texture.crop((x, texture.height-y-h, x+w, texture.height-y))
        full = Image.new('RGBA', (CANVAS, CANVAS))
        anchor = (round(CANVAS/2-entry['pivot'][0]*w), round(CANVAS/2-(1-entry['pivot'][1])*h))
        full.paste(crop, anchor)
        frames[entry['name']] = full
        alpha = np.asarray(full)[..., 3]
        yy, xx = np.nonzero(alpha > 128)
        angles = [projected_fold(pose, leg) for leg in range(6)]
        tip_to_own_upper = []
        for leg in range(6):
            hip = np.asarray(pose['hips'][leg][:2])
            knee = np.asarray(pose['knees'][leg][:2])
            foot = np.asarray(pose['feet'][leg][:2])
            tip_to_own_upper.append(float(distance_to_segment(foot, hip, knee) * PPU))
        report.append({'name': entry['name'], 'row': pose['row'], 'phase': pose['phase'],
            'visibleHash': digest(full), 'opaqueArea': int(len(xx)),
            'bounds': [int(xx.min()), int(yy.min()), int(xx.max()+1), int(yy.max()+1)],
            'borderPixels': int((alpha[0]>1).sum()+(alpha[-1]>1).sum()+(alpha[:,0]>1).sum()+(alpha[:,-1]>1).sum()),
            'projectedKneeInteriorDegrees': angles, 'footDistanceFromOwnUpperPixels': tip_to_own_upper})
    return frames, poses, report


def create_stalker_evidence(frames, poses, report):
    # All source pixels are reviewed at the same scale, never per-frame fit-to-box.
    for row in range(9):
        sheet = Image.new('RGB', (8*200, 6*220), GREEN[:3])
        draw = ImageDraw.Draw(sheet)
        for phase in range(48):
            sprite = green(frames[f'body-{row}-{phase:02d}']).resize((200, 200), Image.Resampling.LANCZOS)
            x, y = phase % 8 * 200, phase // 8 * 220
            sheet.paste(sprite, (x, y+20))
            draw.text((x+4, y+2), f'04 / fold {row}/8 / phase {phase:02d}', fill='black', font=SMALL)
        sheet.save(OUT / f'all-poses-{row}.jpg', quality=96)
    selected = [(0, 0), (0, 12), (0, 20), (0, 36), (4, 0), (6, 0), (7, 0), (8, 0)]
    sheet = Image.new('RGB', (4*CANVAS, 2*(CANVAS+70)), GREEN[:3])
    draw = ImageDraw.Draw(sheet)
    for i, (row, phase) in enumerate(selected):
        full = green(frames[f'body-{row}-{phase:02d}'])
        x, y = i % 4 * CANVAS, i // 4 * (CANVAS+70)
        sheet.paste(full, (x, y+70))
        draw.text((x+10, y+7), f'Postures04 | fold {row}/8 | phase {phase:02d}', fill='black', font=FONT)
        draw.text((x+10, y+31), 'Rounded distal silhouette / roots require visual judgment', fill='black', font=SMALL)
    sheet.save(OUT / 'stalker-source-pixels-selected.png')
    # Show skin and bones separately; their coexistence is not an automatic PASS.
    sheet = Image.new('RGB', (3*CANVAS, 2*(CANVAS+45)), GREEN[:3])
    draw = ImageDraw.Draw(sheet)
    for column, row in enumerate([0, 4, 8]):
        key = f'body-{row}-00'
        original = green(frames[key])
        marked = original.copy()
        mark = ImageDraw.Draw(marked)
        pose = poses[row*48]
        for leg in range(6):
            h, k, f = point(pose['hips'][leg]), point(pose['knees'][leg]), point(pose['feet'][leg])
            mark.line([h, k], fill=(255, 190, 0), width=3)
            mark.line([k, f], fill=(0, 100, 255), width=3)
            for p in [h, k, f]:
                mark.ellipse((p[0]-4,p[1]-4,p[0]+4,p[1]+4), outline='black', width=2)
            mark.text((k[0]+6, k[1]-8), str(leg), fill='black', font=FONT)
        sheet.paste(original, (column*CANVAS,45))
        sheet.paste(marked, (column*CANVAS,CANVAS+90))
        draw.text((column*CANVAS+12,8), f'Fold {row}/8 | identical world scale',fill='black',font=FONT)
        draw.text((column*CANVAS+12,CANVAS+53), 'Upper = orange; lower = blue',fill='black',font=FONT)
    sheet.save(OUT / 'stalker-fold-bones.png')
    # Explicit defect locations, so artistic rejection is not just a statistic.
    marked = Image.new('RGB', (2*CANVAS, CANVAS+120), GREEN[:3])
    draw = ImageDraw.Draw(marked)
    for column, row in enumerate([0,8]):
        marked.paste(green(frames[f'body-{row}-00']), (column*CANVAS,120))
        draw.text((column*CANVAS+12,8), f'Postures04 | fold {row}/8, phase 00', fill='black', font=FONT)
    callouts = [
        (0, (247, 256), 29, 'A', 'A: exposed proximal bulb / double root contour'),
        (0, (430, 294), 26, 'B', 'B: foreshortened middle claw loses readable length'),
        (1, (147, 297), 27, 'C', 'C: round stump silhouette; acute lower segment hidden'),
        (1, (230, 209), 24, 'D', 'D: nearly parallel segments read as pasted thin ribbon'),
    ]
    for column, location, radius, label, text in callouts:
        x,y=column*CANVAS+location[0],120+location[1]
        draw.ellipse((x-radius,y-radius,x+radius,y+radius),outline=(255,70,10),width=3)
        draw.text((x+radius,y-radius),label,fill='black',font=FONT)
    for column in range(2):
        for offset, callout in enumerate([item for item in callouts if item[0]==column]):
            draw.text((column*CANVAS+12,40+offset*25),callout[-1],fill='black',font=SMALL)
    marked.save(OUT / 'marked-defects.png')
    # Compare actual source/clean alpha at joint, without classifying every cut as wrong.
    trims=[]
    trim_sheet=Image.new('RGB',(3*CANVAS,CANVAS+75),GREEN[:3])
    trim_draw=ImageDraw.Draw(trim_sheet)
    for column, (row,phase,leg) in enumerate([(0,0,0),(0,0,3),(8,0,2)]):
        raw=np.asarray(Image.open(OLD/'Masks'/f'leg-{row}-{phase:02d}-{leg}.png').convert('RGBA'))
        clean=np.asarray(Image.open(OLD/'CleanMasks'/f'leg-{row}-{phase:02d}-{leg}.png').convert('RGBA'))
        diagnostic=np.zeros_like(raw)
        diagnostic[...,3]=np.maximum(raw[...,3],clean[...,3])
        diagnostic[raw[...,3]>128,:3]=(60,100,240)
        diagnostic[clean[...,3]>128,:3]=(255,255,255)
        removed=(raw[...,3]>128)&(clean[...,3]<128)
        diagnostic[removed,:3]=(255,30,20)
        image=green(Image.fromarray(diagnostic))
        trim_sheet.paste(image,(column*CANVAS,75))
        trim_draw.text((column*CANVAS+10,8),f'Fold {row}/8 | phase {phase:02d} | leg {leg}',fill='black',font=FONT)
        trim_draw.text((column*CANVAS+10,34),'Red = removed alpha; white = kept (not auto PASS/FAIL)',fill='black',font=SMALL)
        trims.append({'row':row,'phase':phase,'leg':leg,'rawOpaquePixels':int((raw[...,3]>128).sum()),
            'cleanOpaquePixels':int((clean[...,3]>128).sum()),'removedOpaquePixels':int(removed.sum())})
    trim_sheet.save(OUT/'mask-cut-evidence.png')
    # Record transition ranking and loop closure to focus human review.
    transitions = []
    for row in range(9):
        for phase in range(48):
            a = np.asarray(frames[f'body-{row}-{phase:02d}']).astype('float32')/255
            b = np.asarray(frames[f'body-{row}-{(phase+1)%48:02d}']).astype('float32')/255
            rgb_a = a[...,:3]*a[...,3,None]
            rgb_b = b[...,:3]*b[...,3,None]
            transitions.append({'row':row,'from':phase,'to':(phase+1)%48,
                'alphaChange':float(np.mean(np.abs(a[...,3]-b[...,3]))),
                'premultipliedRgbChange':float(np.mean(np.abs(rgb_a-rgb_b)))})
    byrow=[]
    for row in range(9):
        group=[s for s in report if s['row']==row]
        byrow.append({'row':row, 'visibleUniqueFrames':len({s['visibleHash'] for s in group}),
            'minimumInteriorKneeAngle':min(min(s['projectedKneeInteriorDegrees']) for s in group),
            'maximumInteriorKneeAngle':max(max(s['projectedKneeInteriorDegrees']) for s in group),
            'meanFootDistanceFromOwnUpperPixels':float(np.mean([s['footDistanceFromOwnUpperPixels'] for s in group]))})
    return {'frames':report,'rows':byrow,'transitions':transitions,'selectedMaskCutSamples':trims,
        'worstPhaseTransitions':sorted(transitions,key=lambda s:s['alphaChange'],reverse=True)[:20],
        'limitations':'Connectivity/bounds/differences do not prove anatomy or absence of visual defects. Small angle is a review signal, not automatic failure.'}


def proximal_exclusion_audit(frames, poses):
    """Surface ignored joint zones as candidates instead of blanket-exempting them."""
    torso=Image.open(OLD/'CleanMasks/torso.png').getchannel('A')
    actual_cover=np.asarray(torso.filter(ImageFilter.MaxFilter(7)))>128
    yy,xx=np.mgrid[:CANVAS,:CANVAS]
    records=[]
    worst=None
    cache={}
    for pose in poses:
        row,phase=pose['row'],pose['phase']
        # Folded duplicates are legitimate; avoid decoding identical masks 48 times.
        cache_key=(row,phase if row<8 else 0)
        if cache_key in cache:
            records.append({'row':row,'phase':phase,**cache[cache_key]})
            continue
        masks=[np.asarray(Image.open(OLD/'CleanMasks'/f'leg-{row}-{phase:02d}-{leg}.png').getchannel('A'))>128 for leg in range(6)]
        old_cover=actual_cover.copy()
        for hip in pose['hips']:
            x,y=point(hip)
            old_cover|=(xx-x)**2+(yy-y)**2<(.13*PPU)**2
        pair_records=[]
        union=np.zeros((CANVAS,CANVAS),dtype=bool)
        for a in range(6):
            for b in range(a+1,6):
                overlap=masks[a]&masks[b]&~actual_cover
                count=int(overlap.sum())
                if count:
                    union|=overlap
                    pair_records.append({'pair':[a,b],'outsideActualTorsoCoverPixels':count,
                        'insideOldBlanketHipExemption':int((overlap&old_cover).sum()),
                        'outsideOldBlanketHipExemption':int((overlap&~old_cover).sum())})
        result={'jointOverlapCandidates':pair_records,'unionPixels':int(union.sum())}
        cache[cache_key]=result
        records.append({'row':row,'phase':phase,**result})
        if worst is None or union.sum()>worst[0]:
            worst=(int(union.sum()),row,phase,union,old_cover)
    count,row,phase,union,cover=worst
    full=np.asarray(green(frames[f'body-{row}-{phase:02d}'])).copy()
    full[union]=(255,30,20)
    image=Image.fromarray(full)
    draw=ImageDraw.Draw(image)
    for leg,hip in enumerate(poses[row*48+phase]['hips']):
        x,y=point(hip)
        r=.13*PPU
        draw.ellipse((x-r,y-r,x+r,y+r),outline=(200,0,200),width=2)
        draw.text((x-6,y-8),str(leg),fill='black',font=FONT)
    panel=Image.new('RGB',(CANVAS,CANVAS+80),GREEN[:3])
    panel.paste(image,(0,80))
    draw=ImageDraw.Draw(panel)
    draw.text((8,8),f'Joint candidate peak: fold {row}/8, phase {phase:02d}',fill='black',font=FONT)
    draw.text((8,35),'Red = paired limb alpha outside actual torso +3px',fill='black',font=SMALL)
    draw.text((8,55),'Purple circles = previous blanket joint exemptions',fill='black',font=SMALL)
    panel.save(OUT/'joint-exemption-candidates.png')
    return {'frames':records,'worstUnionPixels':count,'worstFrame':[row,phase],
        'interpretation':'A candidate is not automatic failure: adjacent attachment tissues can naturally touch. However, old 0.13-unit discs must not hide protruding root lobes from human review. Actual torso alpha only is the strict visibility mask.'}


def guid_catalog():
    catalog = {}
    for path in (PROJECT/'Assets/EmergencyShift/Art/Enemies/Sleeper').rglob('*.meta'):
        match = re.search(r'^guid: ([0-9a-f]+)$', path.read_text(errors='replace'), re.M)
        if match:
            catalog[match[1]]=Path(str(path)[:-5])
    return catalog


def read_sprite(path, catalog):
    source = path.read_text()
    name = re.search(r'  m_Name: (.+)',source).group(1)
    rect = re.search(r'  m_Rect:\s+serializedVersion: \d+\s+x: ([-\d.]+)\s+y: ([-\d.]+)\s+width: ([-\d.]+)\s+height: ([-\d.]+)',source)
    x,y,w,h=map(float,rect.groups())
    ppu=float(re.search(r'm_PixelsToUnits: ([-\d.]+)',source).group(1))
    pivot=tuple(map(float,re.search(r'm_Pivot: \{x: ([-\d.]+), y: ([-\d.]+)\}',source).groups()))
    guid=re.search(r'texture: \{fileID: \d+, guid: ([0-9a-f]+)',source).group(1)
    texture=Image.open(catalog[guid]).convert('RGBA')
    sprite=texture.crop((int(x),int(texture.height-y-h),int(x+w),int(texture.height-y)))
    full=Image.new('RGBA',(CANVAS,CANVAS))
    sprite=sprite.resize((round(w*PPU/ppu),round(h*PPU/ppu)),Image.Resampling.LANCZOS)
    anchor=(round(CANVAS/2-pivot[0]*sprite.width),round(CANVAS/2-(1-pivot[1])*sprite.height))
    full.paste(sprite,anchor)
    return full, {'name':name,'ppu':ppu,'pivot':pivot,'spriteRectPixels':[x,y,w,h],
        'spriteRectWorld':[w/ppu,h/ppu],'asset':str(path.relative_to(PROJECT)).replace('\\','/'),
        'texture':str(catalog[guid].relative_to(PROJECT)).replace('\\','/'),
        'sha256':hashlib.sha256(catalog[guid].read_bytes()).hexdigest()}


def create_sleeper_evidence():
    catalog=guid_catalog()
    assets=PROJECT/'Assets/EmergencyShift/Art/Enemies/Sleeper/StalkerPlaceholder01/Frames'
    records=[]
    clips=[('Walk',[f'Walk{i:02d}-corrected.asset' for i in range(8)]),
        ('Wake (actual order)',[f'Recoil{i:02d}-corrected.asset' for i in [3,2,1,0]]+['Walk00-corrected.asset']),
        ('Attack',['Walk00-corrected.asset']+[f'Attack{i:02d}.asset' for i in range(1,5)]+['Walk00-corrected.asset']),
        ('Death',[f'Death{i:02d}.asset' for i in range(4)])]
    for label, names in clips:
        sheet=Image.new('RGB',(4*360,math.ceil(len(names)/4)*400),GREEN[:3])
        draw=ImageDraw.Draw(sheet)
        for index,name in enumerate(names):
            full,record=read_sprite(assets/name,catalog)
            records.append({'clip':label,**record})
            x,y=index%4*360,index//4*400
            sheet.paste(green(full).resize((360,360),Image.Resampling.LANCZOS),(x,y+40))
            draw.text((x+8,y+3),record['name'],fill='black',font=FONT)
            draw.text((x+8,y+24),f'PPU {record["ppu"]:.3f} | same world scale',fill='black',font=SMALL)
        sheet.save(OUT/f'sleeper-{label.split()[0].lower()}.png')
    wake_last=next(record for record in records if record['name']=='Recoil00-corrected')
    walk_first=next(record for record in records if record['name']=='Walk00-corrected')
    return {'scope':'Saved sprite RGBA/rects/PPU, body only. Separate runtime eye layers and AI timing require root Unity validation.',
        'records':records,
        'wakeFinalSpriteBoundsRatio':[b/a for a,b in zip(wake_last['spriteRectWorld'],walk_first['spriteRectWorld'])],
        'warning':'Different PPU values alone do not prove an error. At final Recoil00 to Walk00 selection, saved world rect grows 30.1% in width and 52.4% in height. This is a static saved-sprite discontinuity; Play timing is not newly reproduced by this script.'}


def main():
    frames,poses,report=extract_current()
    stalker=create_stalker_evidence(frames,poses,report)
    stalker['proximalExclusionAudit']=proximal_exclusion_audit(frames,poses)
    sleeper=create_sleeper_evidence()
    (OUT/'objective-audit.json').write_text(json.dumps({'stalker':stalker,'sleeper':sleeper},indent=2))
    print(json.dumps({'stalkerFrames':len(report),'borderPixels':sum(s['borderPixels'] for s in report),
        'folded':stalker['rows'][-1],'sleeperFramesReviewed':len(sleeper['records']),
        'output':str(OUT)},indent=2))


if __name__=='__main__':
    main()
