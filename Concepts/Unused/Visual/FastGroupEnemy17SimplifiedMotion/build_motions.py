"""TASK-005: walk, claw attack and run from one simplified painted master.

Only the master was creatively edited with imagegen. Animation uses fixed
anatomy-v4 lengths, rigid claw layers and a continuous local skin surface.
"""
from pathlib import Path
import hashlib
import json
import math
import sys
import numpy as np
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parent
SOURCE = ROOT.parent/'FastGroupEnemy13AssetInspired'
PLAN = json.loads((SOURCE/'anatomy-plan-v4.json').read_text(encoding='utf-8-sig'))
ART = Image.open(ROOT/'simplified-master-v1.png').convert('RGBA')
SCALE = .72
OFFSET = np.array([55., 33.])
W = 1024
FONT = ImageFont.truetype('C:/Windows/Fonts/arial.ttf', 18)
SMALL = ImageFont.truetype('C:/Windows/Fonts/arial.ttf', 14)
REST = {}
for kind, limbs in [('F', PLAN['fronts']), ('R', PLAN['rears'])]:
    for side, limb in zip(['U', 'L'], limbs):
        keys = ['shoulder', 'elbow', 'wrist', 'palm'] if kind=='F' else ['hip', 'knee', 'hock', 'palm']
        REST[kind+side] = np.asarray([limb[key] for key in keys], dtype=float)

CONFIG = {
    'walk': {'count': 16, 'durationsMs': [60]*16, 'loop': True, 'stride': .24, 'duty': .78,
             'rearStride': .24, 'phases': {'FU': 0., 'RL': .25, 'FL': .5, 'RU': .75}, 'frontLift': .075, 'rearLift': .055},
    'attack': {'count': 14, 'durationsMs': [70, 70, 70, 70, 80, 80, 30, 30, 40, 50, 60, 70, 70, 70], 'loop': False,
               'stages': {'anticipationFrames': [0, 5], 'strikeFrames': [6, 7], 'recoveryFrames': [8, 13]}},
    'run': {'count': 16, 'durationsMs': [30]*16, 'loop': True, 'stride': .34, 'duty': .43,
            'rearStride': .24, 'phases': {'FU': 0., 'FL': .5, 'RU': .52, 'RL': .02}, 'frontLift': .18, 'rearLift': .125}
}


def smooth(a, b, value):
    q = np.clip((value-a)/(b-a), 0., 1.)
    return q*q*(3.-2.*q)


def project(points):
    return np.asarray(points)[..., :2]*375.+np.array([760., 665.])


def screen(points):
    return np.asarray(points)*SCALE+OFFSET


def ik(root, endpoint, old_joint, a, b):
    delta = endpoint-root
    distance = np.linalg.norm(delta)
    if not abs(a-b)<distance<a+b:
        raise ValueError(f'Unreachable limb target: {distance}, range {abs(a-b)}..{a+b}')
    axis = delta/distance
    along = (a*a-b*b+distance*distance)/(2.*distance)
    side = old_joint-root
    side -= axis*np.dot(side, axis)
    side /= np.linalg.norm(side)
    return root+along*axis+math.sqrt(max(0., a*a-along*along))*side


def segment_map(points, a, b, c, d):
    u = (b-a)/np.linalg.norm(b-a)
    v = (d-c)/np.linalg.norm(d-c)
    up = np.array([-u[1], u[0]])
    vp = np.array([-v[1], v[0]])
    local = points-a
    ratio = np.linalg.norm(d-c)/np.linalg.norm(b-a)
    return c+np.sum(local*u, axis=-1)[..., None]*ratio*v+np.sum(local*up, axis=-1)[..., None]*vp


def foot_trajectory(q, stride, duty, low):
    q %= 1.
    if q<duty:
        return low+stride*q/duty, 0., 'stance'
    u = (q-duty)/(1.-duty)
    blend = u*u*u*(10.+u*(-15.+6.*u))
    tangent = stride*(1.-duty)/duty
    x = low+stride+tangent*u+(-stride-tangent)*blend
    return x, math.sin(math.pi*u)**2, 'recovery'


def locomotion_pose(state, t, index=None):
    config = CONFIG[state]
    if state=='walk':
        motion = np.array([.006*math.cos(4.*math.pi*t), .005*math.sin(2.*math.pi*t), -.008*math.cos(4.*math.pi*t)])
    else:
        motion = np.array([.012*math.cos(4.*math.pi*t), .002*math.sin(2.*math.pi*t), .025*math.cos(4.*math.pi*(t-.465))])
    data = {'index': index, 'rootOffset3D': motion.tolist(), 'limbs': {}}
    for ident, rest in REST.items():
        front = ident[0]=='F'
        side = -1. if ident[1]=='U' else 1.
        stride = config['stride'] if front else config['rearStride']
        duty = config['duty']*stride/config['stride']
        q = (t+config['phases'][ident])%1.
        low = (-.10 if state=='walk' else -.14) if front else (-.10 if state=='walk' else -.10)
        dx, lift, phase = foot_trajectory(q, stride, duty, low)
        target = rest.copy()
        target[0] += motion
        target[2:, 0] += dx
        if front:
            target[2:, 1] -= side*(.015 if state=='walk' else .035)*lift
        else:
            target[2:, 1] += side*(.035+(.006 if state=='walk' else .014)*lift)
        target[2:, 2] += config['frontLift' if front else 'rearLift']*lift
        lengths = np.linalg.norm(np.diff(rest, axis=0), axis=1)
        target[1] = ik(target[0], target[2], rest[1]+motion, *lengths[:2])
        data['limbs'][ident] = {'phase': q, 'state': phase, 'joints3D': target.tolist(), 'projected': project(target).tolist()}
    data['spine'] = project(np.asarray(PLAN['spine'])+motion).tolist()
    return data


def attack_values(u):
    if u<=.38:
        amount = float(smooth(0., .38, u))
        return .135*amount, .18*amount, 0., 'anticipation'
    if u<=.55:
        amount = float(smooth(.38, .55, u))
        return .135-.355*amount, .18-.035*amount, amount, 'strike'
    amount = float(smooth(.55, 1., u))
    return -.22*(1.-amount), .145*(1.-amount), 1.-amount, 'recovery'


def attack_pose(t, index=None):
    _, _, drive, stage = attack_values(t)
    windup = float(smooth(0., .38, t))*(1.-float(smooth(.38, .55, t)))
    motion = np.array([.025*windup-.085*drive, 0., -.055*drive-.025*windup])
    data = {'index': index, 'rootOffset3D': motion.tolist(), 'stage': stage, 'limbs': {}}
    for ident, rest in REST.items():
        target = rest.copy()
        target[0] += motion
        side = -1. if ident[1]=='U' else 1.
        if ident[0]=='F':
            # Slight lead of the upper claw; both return to the identical master.
            phase = t if ident=='FU' else min(1., max(0., (t-.035)/.965))
            dx, lift, local_drive, phase_name = attack_values(phase)
            target[2:, 0] += dx
            target[2:, 1] -= side*.025*local_drive
            target[2:, 2] += lift
            state = 'stance' if lift<1e-8 else ('strike' if phase_name=='strike' else 'raised')
        else:
            target[2:, 1] += side*.035
            state = 'brace'
        lengths = np.linalg.norm(np.diff(rest, axis=0), axis=1)
        target[1] = ik(target[0], target[2], rest[1]+motion, *lengths[:2])
        data['limbs'][ident] = {'phase': t, 'state': state, 'joints3D': target.tolist(), 'projected': project(target).tolist()}
    data['spine'] = project(np.asarray(PLAN['spine'])+motion).tolist()
    return data


def poses(state):
    count = CONFIG[state]['count']
    if state=='attack':
        # Spend real frames in anticipation, use a fast extended strike, then
        # return to exactly the starting pose. Timing is stored independently.
        phases = [0., .08, .16, .24, .32, .38, .46, .55, .60, .68, .77, .86, .94, 1.]
        return [attack_pose(t, i) for i, t in enumerate(phases)]
    return [locomotion_pose(state, i/count, i) for i in range(count)]


def bone_frame(p, background=None):
    im = background.copy() if background is not None else Image.new('RGBA', (W, W), '#262b31')
    draw = ImageDraw.Draw(im)
    for ident, limb in p['limbs'].items():
        color = '#78ebba' if limb['state'] in ['stance', 'brace'] else '#ffc078'
        points = [tuple(screen(point)) for point in limb['projected']]
        draw.line(points, fill=color, width=3)
        for x, y in points:
            draw.ellipse((x-4, y-4, x+4, y+4), fill=color)
        draw.text((points[-1][0]+5, points[-1][1]-16), ident, font=SMALL, fill=color)
    draw.line([tuple(screen(point)) for point in p['spine']], fill='#79bcff', width=3)
    return im


def contact_sheet(images, path, columns=4, size=320, bg='#262b31'):
    sheet = Image.new('RGB', (columns*size, math.ceil(len(images)/columns)*(size+24)), bg)
    draw = ImageDraw.Draw(sheet)
    for i, im in enumerate(images):
        x, y = (i%columns)*size, (i//columns)*(size+24)
        base = Image.new('RGBA', im.size, bg)
        base.alpha_composite(im.convert('RGBA'))
        sheet.paste(base.convert('RGB').resize((size, size), Image.Resampling.LANCZOS), (x, y))
        draw.text((x+8, y+size+3), f'{i:02d}', font=SMALL, fill='#24292f' if bg=='#ebeae6' else 'white')
    sheet.save(path)


def master_check():
    base = ART.transform((W, W), Image.Transform.AFFINE, (1./SCALE, 0., -OFFSET[0]/SCALE, 0., 1./SCALE, -OFFSET[1]/SCALE), Image.Resampling.BICUBIC)
    neutral = {'index': 0, 'rootOffset3D': [0., 0., 0.], 'limbs': {ident: {'projected': project(rest).tolist(), 'state': 'stance'} for ident, rest in REST.items()}, 'spine': project(PLAN['spine']).tolist()}
    bone_frame(neutral, base).save(ROOT/'01-master-bones.png')


def save_plan(state, sequence):
    target = ROOT/state
    target.mkdir(exist_ok=True)
    config = CONFIG[state]
    data = {'task': 'TASK-005', 'revision': 'simplified-motion-r1', 'state': state, 'master': '../simplified-master-v1.png',
            'masterSha256': hashlib.sha256((ROOT/'simplified-master-v1.png').read_bytes()).hexdigest(),
            'anatomy': '../../FastGroupEnemy13AssetInspired/anatomy-plan-v4.json', 'camera': {'canvas': [W, W], 'sourceScale': SCALE, 'sourceOffset': OFFSET.tolist(), 'pivot': screen([760., 665.]).tolist(), 'perFrameFit': False},
            'frameCount': config['count'], 'durationsMs': config['durationsMs'], 'durationMs': sum(config['durationsMs']), 'loop': config['loop'], 'frames': sequence, 'artisticApproval': False}
    if state!='attack':
        data['groundSpeed'] = config['stride']/(config['duty']*sum(config['durationsMs'])/1000.)
        data['trajectories'] = {ident: {'stride': config['stride'] if ident[0]=='F' else config['rearStride'], 'duty': config['duty']*(config['stride'] if ident[0]=='F' else config['rearStride'])/config['stride']} for ident in REST}
    else:
        data['stages'] = config['stages']
        data['note'] = 'Conceptual visual strike interval, not a Unity damage event.'
    (target/'motion-plan.json').write_text(json.dumps(data, ensure_ascii=False, indent=2), encoding='utf-8')
    contact_sheet([bone_frame(p) for p in sequence], target/'01-independent-bones.png')


def previews(state, frames):
    target = ROOT/state
    config = CONFIG[state]
    contact_sheet(frames, target/'frames-dark.jpg')
    contact_sheet(frames, target/'frames-light.jpg', bg='#ebeae6')
    sheet = Image.new('RGBA', (W*4, W*math.ceil(len(frames)/4)))
    for i, im in enumerate(frames):
        sheet.alpha_composite(im, ((i%4)*W, (i//4)*W))
    sheet.save(target/f'{state}-sheet.png')
    for name, color in [('dark', '#262b31'), ('light', '#ebeae6')]:
        images = []
        for im in frames:
            base = Image.new('RGBA', im.size, color)
            base.alpha_composite(im)
            images.append(base.crop((0, 190, W, 840)).resize((820, 520), Image.Resampling.LANCZOS).convert('RGB'))
        # The same master owns the GIF palette in every state, too. Independent
        # state palettes can introduce a small colour change at comparison time.
        palette_source = Image.new('RGBA', ART.size, color)
        palette_source.alpha_composite(ART)
        palette = palette_source.convert('RGB').quantize(colors=256)
        quantized = [im.quantize(palette=palette, dither=Image.Dither.NONE) for im in images]
        quantized[0].save(target/f'{state}-{name}.gif', save_all=True, append_images=quantized[1:], duration=config['durationsMs'], loop=0, disposal=2)
    # PNG animation retains exact durations and colour; the GIF is a convenience.
    frames[0].save(target/f'{state}-preview.png', save_all=True, append_images=frames[1:], duration=config['durationsMs'], loop=0 if config['loop'] else 1, disposal=0, blend=0)


def export(state, sample=False):
    from skin_surface import SkinSurface
    sequence = poses(state)
    save_plan(state, sequence)
    target = ROOT/state
    (target/'frames').mkdir(exist_ok=True)
    surface = SkinSurface(sys.modules[__name__])
    selected = sorted(set([0, len(sequence)//4, len(sequence)//2, 3*len(sequence)//4])) if sample else range(len(sequence))
    frames = []
    measurements = []
    for i in selected:
        image, stats = surface.render(sequence[i])
        filename = target/f'sample-{i:02d}.png' if sample else target/'frames'/f'{state}-{i:02d}.png'
        image.save(filename)
        if sample:
            bone_frame(sequence[i], image).save(target/f'sample-bones-{i:02d}.png')
        frames.append(image)
        measurements.append({'index': i, **stats})
        print(state, i, stats, flush=True)
    if not sample:
        (target/'deformation-measurements.json').write_text(json.dumps(measurements, indent=2), encoding='utf-8')
        contact_sheet([bone_frame(p, im) for p, im in zip(sequence, frames)], target/'02-bones-on-skin.png')
        previews(state, frames)


if __name__=='__main__':
    state = sys.argv[1] if len(sys.argv)>1 else 'walk'
    master_check()
    if '--plan-only' in sys.argv:
        save_plan(state, poses(state))
    elif '--previews-only' in sys.argv:
        previews(state, [Image.open(ROOT/state/'frames'/f'{state}-{i:02d}.png').convert('RGBA') for i in range(CONFIG[state]['count'])])
    else:
        export(state, '--sample' in sys.argv)
