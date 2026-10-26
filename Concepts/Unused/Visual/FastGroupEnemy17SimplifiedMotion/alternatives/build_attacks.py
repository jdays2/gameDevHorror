"""TASK-005: three alternate attacks using the owner's liked master and rig."""
from pathlib import Path
import hashlib
import json
import sys
from types import SimpleNamespace
import numpy as np
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parent
PARENT = ROOT.parent
sys.path.insert(0, str(PARENT))
import build_motions as base
from skin_surface import SkinSurface

MASTER = PARENT/'simplified-master-v1.png'
CONFIG = {
    'swipe': {
        'name': 'Косой удар',
        'description': 'Один удар передней лапой по диагонали. Остальные три лапы держат опору.',
        'times': [0., .08, .16, .24, .32, .38, .46, .55, .60, .68, .77, .86, .94, 1.],
        'durationsMs': [60, 60, 60, 70, 70, 70, 30, 30, 40, 50, 60, 60, 60, 70],
        'samples': [0, 5, 7, 13],
        'strikes': [[6, 7]],
    },
    'cross': {
        'name': 'Два диагональных удара',
        'description': 'Верхняя лапа бьёт первой, нижняя подхватывает движение вторым диагональным ударом.',
        'times': [0., .06, .12, .18, .24, .29, .34, .40, .46, .50, .53, .58, .63, .68, .74, .80, .86, .92, .97, 1.],
        'durationsMs': [60, 60, 60, 70, 70, 30, 30, 50, 60, 60, 70, 30, 30, 50, 60, 60, 60, 60, 60, 70],
        'samples': [0, 4, 6, 10, 12, 19],
        'strikes': [[5, 6], [11, 12]],
    },
    'lunge': {
        'name': 'Тяжёлый двойной выпад',
        'description': 'Лапы шире расходятся в замахе; затем обе бьют вперёд с заметным переносом корпуса.',
        'times': [0., .07, .14, .21, .28, .36, .43, .50, .57, .64, .71, .78, .85, .92, .97, 1.],
        'durationsMs': [70, 70, 70, 70, 70, 80, 30, 30, 40, 50, 60, 70, 70, 70, 70, 70],
        'samples': [0, 5, 7, 15],
        'strikes': [[6, 7]],
    },
}


def action(t, start, prepared, impact, end):
    if t<=start or t>=end:
        return 0., 0., 'brace'
    if t<=prepared:
        return float(base.smooth(start, prepared, t)), 0., 'anticipation'
    if t<=impact:
        drive = float(base.smooth(prepared, impact, t))
        return 1.-drive, drive, 'strike'
    return 0., 1.-float(base.smooth(impact, end, t)), 'recovery'


def pose(state, t, index=None):
    if state=='swipe':
        wind, drive, stage = action(t, 0., .38, .55, 1.)
        actions = {'FU': (wind, drive, stage), 'FL': (0., 0., 'brace')}
        motion = np.array([.025*wind-.055*drive, -.025*drive, -.025*wind-.025*drive])
    elif state=='cross':
        upper = action(t, 0., .24, .34, .65)
        lower = action(t, .30, .53, .63, 1.)
        actions = {'FU': upper, 'FL': lower}
        wind = max(upper[0], lower[0])
        drive = max(upper[1], lower[1])
        motion = np.array([.020*wind-.060*drive, .018*(lower[1]-upper[1]), -.025*wind-.025*drive])
        if any(item[2]=='strike' for item in actions.values()):
            stage = 'strike'
        elif any(item[2]=='anticipation' for item in actions.values()):
            stage = 'anticipation'
        else:
            stage = 'recovery'
    else:
        wind, drive, stage = action(t, 0., .36, .50, 1.)
        actions = {'FU': (wind, drive, stage), 'FL': (wind, drive, stage)}
        motion = np.array([.045*wind-.140*drive, 0., -.055*wind-.060*drive])
    if t==0. or t==1.:
        stage = 'ready'
    result = {'index': index, 'stage': stage, 'rootOffset3D': motion.tolist(), 'limbs': {}}
    for ident, rest in base.REST.items():
        target = rest.copy()
        target[0] += motion
        side = -1. if ident[1]=='U' else 1.
        if ident[0]=='F':
            local_wind, local_drive, local_stage = actions[ident]
            if state=='lunge':
                dx = .120*local_wind-.240*local_drive
                dy = side*(.080*local_wind-.060*local_drive)
                lift = .220*local_wind+.130*local_drive
            else:
                dx = .120*local_wind-.185*local_drive
                dy = side*(.070*local_wind-.140*local_drive)
                lift = .180*local_wind+.120*local_drive
            target[2:, 0] += dx
            target[2:, 1] += dy
            target[2:, 2] += lift
            foot_state = 'brace' if lift<1e-8 else ('strike' if local_stage=='strike' else 'raised')
        else:
            target[2:, 1] += side*.035
            foot_state = 'brace'
        lengths = np.linalg.norm(np.diff(rest, axis=0), axis=1)
        target[1] = base.ik(target[0], target[2], rest[1]+motion, *lengths[:2])
        result['limbs'][ident] = {'phase': t, 'state': foot_state, 'joints3D': target.tolist(),
                                 'projected': base.project(target).tolist()}
    result['spine'] = base.project(np.asarray(base.PLAN['spine'])+motion).tolist()
    return result


def poses(state):
    return [pose(state, t, i) for i, t in enumerate(CONFIG[state]['times'])]


def save_plan(state):
    config = CONFIG[state]
    sequence = poses(state)
    folder = ROOT/state
    folder.mkdir(parents=True, exist_ok=True)
    data = {'task': 'TASK-005', 'revision': 'attack-alternatives-r1', 'state': state,
            'displayName': config['name'], 'displayDescription': config['description'],
            'master': '../../simplified-master-v1.png', 'masterSha256': hashlib.sha256(MASTER.read_bytes()).hexdigest(),
            'anatomy': '../../../FastGroupEnemy13AssetInspired/anatomy-plan-v4.json',
            'camera': {'canvas': [1024, 1024], 'sourceScale': base.SCALE, 'sourceOffset': base.OFFSET.tolist(),
                       'pivot': base.screen([760., 665.]).tolist(), 'perFrameFit': False},
            'frameCount': len(sequence), 'durationsMs': config['durationsMs'],
            'durationMs': sum(config['durationsMs']), 'loop': False,
            'visualStrikeFrames': config['strikes'], 'note': 'Visual proposal only; no gameplay damage event.',
            'frames': sequence, 'artisticApproval': False}
    (folder/'motion-plan.json').write_text(json.dumps(data, ensure_ascii=False, indent=2), encoding='utf-8')
    base.contact_sheet([base.bone_frame(p) for p in sequence], folder/'01-independent-bones.png')
    return data


def fingerprint(pose):
    pieces = [json.dumps(pose, sort_keys=True).encode(), MASTER.read_bytes(),
              Path(__file__).read_bytes(), (PARENT/'skin_surface.py').read_bytes()]
    return hashlib.sha256(b''.join(pieces)).hexdigest()


def surface_rig():
    fields = ['ART', 'REST', 'project', 'screen', 'segment_map', 'SCALE', 'OFFSET', 'W']
    rig = SimpleNamespace(**{key: getattr(base, key) for key in fields})
    rig.ROOT = ROOT
    return rig


def previews(state):
    plan = json.loads((ROOT/state/'motion-plan.json').read_text(encoding='utf-8'))
    frames = [Image.open(ROOT/state/'frames'/f'{state}-{i:02d}.png').convert('RGBA') for i in range(plan['frameCount'])]
    base.contact_sheet(frames, ROOT/state/'frames-light.jpg', bg='#ebeae6')
    base.contact_sheet(frames, ROOT/state/'frames-dark.jpg')
    sheet = Image.new('RGBA', (4096, 1024*((len(frames)+3)//4)))
    for i, image in enumerate(frames):
        sheet.alpha_composite(image, ((i%4)*1024, (i//4)*1024))
    sheet.save(ROOT/state/f'{state}-sheet.png')
    for name, color in [('dark', '#262b31'), ('light', '#ebeae6')]:
        palette_source = Image.new('RGBA', base.ART.size, color)
        palette_source.alpha_composite(base.ART)
        palette = palette_source.convert('RGB').quantize(colors=256)
        images = []
        for image in frames:
            background = Image.new('RGBA', image.size, color)
            background.alpha_composite(image)
            image = background.crop((0, 190, 1024, 840)).resize((820, 520), Image.Resampling.LANCZOS)
            images.append(image.convert('RGB').quantize(palette=palette, dither=Image.Dither.NONE))
        images[0].save(ROOT/state/f'{state}-{name}.gif', save_all=True, append_images=images[1:],
                       duration=plan['durationsMs'], loop=0, disposal=2)
    frames[0].save(ROOT/state/f'{state}-preview.png', save_all=True, append_images=frames[1:],
                   duration=plan['durationsMs'], loop=1, disposal=0, blend=0)
    base.contact_sheet([base.bone_frame(p, image) for p, image in zip(plan['frames'], frames)], ROOT/state/'02-bones-on-skin.png')


def export(state, sample=False):
    plan = save_plan(state)
    folder = ROOT/state
    (folder/'frames').mkdir(exist_ok=True)
    (folder/'measurements').mkdir(exist_ok=True)
    surface = SkinSurface(surface_rig())
    selected = CONFIG[state]['samples'] if sample else range(plan['frameCount'])
    for i in selected:
        p = plan['frames'][i]
        frame = folder/'frames'/f'{state}-{i:02d}.png'
        measurement = folder/'measurements'/f'{i:02d}.json'
        key = fingerprint(p)
        if frame.exists() and measurement.exists():
            existing = json.loads(measurement.read_text(encoding='utf-8'))
            if existing['fingerprint']==key and existing['imageSha256']==hashlib.sha256(frame.read_bytes()).hexdigest():
                print(state, i, 'reuse identical verified sample', flush=True)
                continue
        image, stats = surface.render(p)
        image.save(frame)
        stats.update({'index': i, 'fingerprint': key, 'imageSha256': hashlib.sha256(frame.read_bytes()).hexdigest()})
        measurement.write_text(json.dumps(stats, indent=2), encoding='utf-8')
        base.bone_frame(p, image).save(folder/f'bones-{i:02d}.png')
        print(state, i, stats['flippedTriangles'], stats['minTriangleAreaRatio'], flush=True)
    if not sample:
        previews(state)


if __name__=='__main__':
    states = [arg for arg in sys.argv[1:] if not arg.startswith('--')] or list(CONFIG)
    for state in states:
        if '--plan-only' in sys.argv:
            save_plan(state)
        elif '--previews-only' in sys.argv:
            previews(state)
        else:
            export(state, '--sample' in sys.argv)
