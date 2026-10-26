"""Broad shoulder/wrist arcs after the owner rejected restrained attacks."""
from pathlib import Path
import hashlib
import json
import math
import sys
import numpy as np
from PIL import Image
from raster_finish import finish

ROOT = Path(__file__).resolve().parent
sys.path.insert(0, str(ROOT.parent))
import build_attacks as runtime
base = runtime.base
from skin_surface import SkinSurface
MASTER = runtime.MASTER

CONFIG = {
    'sweep': {'name': 'Размашистый удар',
              'description': 'Большой замах верхней лапой, широкая дуга с поворотом кисти и резкая сечка вниз.',
              'times': [0., .06, .12, .18, .24, .30, .36, .40, .45, .50, .56, .62, .68, .74, .80, .88, .94, 1.],
              'durationsMs': [70, 70, 70, 80, 80, 80, 90, 100, 30, 30, 40, 50, 60, 70, 70, 70, 70, 80],
              'samples': [0, 7, 9, 17], 'strikes': [[8, 9]]},
    'flurry': {'name': 'Две широкие сечки',
               'description': 'Два размашистых диагональных удара: верхняя лапа, затем нижняя, с разворотом когтей.',
               'times': [0., .06, .12, .18, .24, .28, .34, .40, .46, .52, .57, .63, .69, .74, .80, .86, .92, .96, .98, 1.],
               'durationsMs': [60, 60, 70, 70, 80, 90, 30, 30, 50, 70, 90, 30, 30, 50, 60, 70, 70, 70, 70, 80],
               'samples': [0, 5, 7, 10, 12, 19], 'strikes': [[6, 7], [11, 12]]},
    'cleave': {'name': 'Двойной размашистый удар',
               'description': 'Обе лапы широко раскрываются, затем сходятся мощным ударом перед корпусом.',
               'times': [0., .06, .12, .18, .24, .30, .36, .40, .45, .50, .56, .62, .68, .74, .80, .88, .94, 1.],
               'durationsMs': [70, 70, 80, 80, 80, 90, 100, 100, 30, 30, 40, 50, 60, 70, 70, 70, 80, 90],
               'samples': [0, 7, 9, 17], 'strikes': [[8, 9]]},
}


def rotation(angle):
    c, s = math.cos(angle), math.sin(angle)
    return np.array([[c, -s], [s, c]])


def pose(state, t, index=None):
    if t==0. or t==1.:
        result = base.attack_pose(0., index)
        result['stage'] = 'ready'
        for limb in result['limbs'].values():
            limb['handAngleRad'] = 0.
            limb['phase'] = t
        return result
    if state=='flurry':
        actions = {'FU': runtime.action(t, 0., .28, .40, .70),
                   'FL': runtime.action(t, .28, .57, .69, 1.)}
    else:
        active = runtime.action(t, 0., .40, .50, 1.)
        actions = {'FU': active, 'FL': active if state=='cleave' else (0., 0., 'brace')}
    wind = max(item[0] for item in actions.values())
    drive = max(item[1] for item in actions.values())
    motion = np.array([.040*wind-(.130 if state=='cleave' else .080)*drive,
                       .020*(actions['FL'][1]-actions['FU'][1]), -.045*wind-.045*drive])
    stages = [item[2] for item in actions.values()]
    stage = 'strike' if 'strike' in stages else ('anticipation' if 'anticipation' in stages else 'recovery')
    if t==0. or t==1.:
        stage = 'ready'
    result = {'index': index, 'stage': stage, 'rootOffset3D': motion.tolist(), 'limbs': {}}
    for ident, rest in base.REST.items():
        target = rest.copy()
        target[0] += motion
        side = -1. if ident[1]=='U' else 1.
        angle = 0.
        if ident[0]=='F':
            local_wind, local_drive, local_stage = actions[ident]
            inward = .080 if state=='cleave' else .340
            strike_angle = .400 if state=='cleave' else .550
            dx = .100*local_wind-.240*local_drive
            dy = side*(.280*local_wind-inward*local_drive)
            lift = .250*local_wind+.180*local_drive
            angle = -side*(.900*local_wind-strike_angle*local_drive)
            target[2, :2] += [dx, dy]
            target[2, 2] += lift
            target[3, :2] = target[2, :2]+rotation(angle)@(rest[3, :2]-rest[2, :2])
            target[3, 2] = target[2, 2]+rest[3, 2]-rest[2, 2]
            foot_state = 'brace' if lift<1e-8 else ('strike' if local_stage=='strike' else 'raised')
        else:
            target[2:, 1] += side*.035
            foot_state = 'brace'
        lengths = np.linalg.norm(np.diff(rest, axis=0), axis=1)
        target[1] = base.ik(target[0], target[2], rest[1]+motion, *lengths[:2])
        result['limbs'][ident] = {'phase': t, 'state': foot_state, 'handAngleRad': angle,
                                 'joints3D': target.tolist(), 'projected': base.project(target).tolist()}
    result['spine'] = base.project(np.asarray(base.PLAN['spine'])+motion).tolist()
    return result


class RotatingSurface(SkinSurface):
    def constraints(self, pose):
        target = super().constraints(pose)
        for ident in ['FU', 'FL']:
            angle = pose['limbs'][ident]['handAngleRad']
            if abs(angle)<1e-12:
                continue
            owned = (self.owners==ident) & (self.parts=='hand')
            rest = base.project(base.REST[ident][2])
            posed = np.asarray(pose['limbs'][ident]['projected'][2])
            matrix = rotation(angle)
            target[owned] = (self.source[owned]-rest)@matrix.T+posed
        return target

    def render(self, pose):
        image, stats = self.raster(self.deform(pose))
        for ident, piece in self.feet.items():
            rest = base.project(base.REST[ident][2])
            posed = np.asarray(pose['limbs'][ident]['projected'][2])
            angle = pose['limbs'][ident].get('handAngleRad', 0.)
            if abs(angle)<1e-12:
                displacement = base.OFFSET+(posed-rest)*base.SCALE
                coefficients = (1./base.SCALE, 0., -displacement[0]/base.SCALE,
                                0., 1./base.SCALE, -displacement[1]/base.SCALE)
                image.alpha_composite(piece.transform((1024, 1024), Image.Transform.AFFINE, coefficients, Image.Resampling.BICUBIC))
                continue
            matrix = rotation(angle).T
            affine = matrix/base.SCALE
            offset = rest-matrix@posed-affine@base.OFFSET
            coefficients = (*affine[0], offset[0], *affine[1], offset[1])
            image.alpha_composite(piece.transform((1024, 1024), Image.Transform.AFFINE, coefficients, Image.Resampling.BICUBIC))
        image, holes = finish(image)
        stats['filledRasterPinholes'] = holes
        return image, stats


def fingerprint(p):
    files = [json.dumps(p, sort_keys=True).encode(), MASTER.read_bytes(), Path(__file__).read_bytes(),
             (runtime.PARENT/'skin_surface.py').read_bytes(), (ROOT/'raster_finish.py').read_bytes()]
    return hashlib.sha256(b''.join(files)).hexdigest()


def save_plan(state):
    config = CONFIG[state]
    sequence = runtime.poses(state)
    folder = ROOT/state
    folder.mkdir(parents=True, exist_ok=True)
    plan = {'task': 'TASK-005', 'revision': 'broad-attacks-r1', 'state': state,
            'displayName': config['name'], 'displayDescription': config['description'],
            'master': '../../../simplified-master-v1.png', 'masterSha256': hashlib.sha256(MASTER.read_bytes()).hexdigest(),
            'camera': {'canvas': [1024, 1024], 'pivot': base.screen([760., 665.]).tolist(), 'perFrameFit': False},
            'frameCount': len(sequence), 'durationsMs': config['durationsMs'], 'durationMs': sum(config['durationsMs']),
            'loop': False, 'frames': sequence, 'visualStrikeFrames': config['strikes'], 'artisticApproval': False,
            'motionChange': 'Large shoulder arc plus rigid hand rotation; longer anticipation and slower recovery.'}
    (folder/'motion-plan.json').write_text(json.dumps(plan, ensure_ascii=False, indent=2), encoding='utf-8')
    base.contact_sheet([base.bone_frame(p) for p in sequence], folder/'01-independent-bones.png')
    return plan


def previews(state):
    folder = ROOT/state
    config = CONFIG[state]
    plan = json.loads((folder/'motion-plan.json').read_text(encoding='utf-8'))
    frames = [Image.open(folder/'frames'/f'{state}-{i:02d}.png').convert('RGBA') for i in range(plan['frameCount'])]
    base.contact_sheet(frames, folder/'frames-light.jpg', bg='#ebeae6')
    base.contact_sheet(frames, folder/'frames-dark.jpg')
    base.contact_sheet([base.bone_frame(p, im) for p, im in zip(plan['frames'], frames)], folder/'02-bones-on-skin.png')
    sheet = Image.new('RGBA', (4096, 1024*((len(frames)+3)//4)))
    for i, im in enumerate(frames):
        sheet.alpha_composite(im, ((i%4)*1024, (i//4)*1024))
    sheet.save(folder/f'{state}-sheet.png')
    for name, color in [('dark', '#262b31'), ('light', '#ebeae6')]:
        palette_source = Image.new('RGBA', base.ART.size, color)
        palette_source.alpha_composite(base.ART)
        palette = palette_source.convert('RGB').quantize(colors=256)
        images = []
        for frame in frames:
            background = Image.new('RGBA', (1024, 1024), color)
            background.alpha_composite(frame)
            images.append(background.resize((820, 820), Image.Resampling.LANCZOS).convert('RGB').quantize(palette=palette, dither=Image.Dither.NONE))
        images[0].save(folder/f'{state}-{name}.gif', save_all=True, append_images=images[1:], duration=config['durationsMs'], loop=0, disposal=2)
    frames[0].save(folder/f'{state}-preview.png', save_all=True, append_images=frames[1:], duration=config['durationsMs'], loop=1, disposal=0, blend=0)


def configure():
    runtime.ROOT = ROOT
    runtime.CONFIG = CONFIG
    runtime.pose = pose
    runtime.save_plan = save_plan
    runtime.fingerprint = fingerprint
    runtime.previews = previews
    runtime.SkinSurface = RotatingSurface


if __name__=='__main__':
    configure()
    states = [arg for arg in sys.argv[1:] if not arg.startswith('--')] or list(CONFIG)
    for state in states:
        if '--plan-only' in sys.argv:
            save_plan(state)
        elif '--previews-only' in sys.argv:
            previews(state)
        else:
            runtime.export(state, '--sample' in sys.argv)
