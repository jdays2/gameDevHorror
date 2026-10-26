"""Check saved frames and retain separate anatomy/skin/temporal evidence."""
from pathlib import Path
import hashlib
import json
import sys
import numpy as np
from PIL import Image, ImageDraw, ImageFilter
import build_motions as rig

ROOT = Path(__file__).resolve().parent
GROUPS = json.loads((rig.SOURCE/'anatomy-muscle-plan-v4.json').read_text(encoding='utf-8-sig'))['groups']


def background(image, color='#ebeae6'):
    result = Image.new('RGBA', image.size, color)
    result.alpha_composite(image)
    return result


def registered(image, pose):
    offset = rig.OFFSET+np.asarray(pose['rootOffset3D'][:2])*375.*rig.SCALE
    return image.transform((1254, 1254), Image.Transform.AFFINE, (rig.SCALE, 0., offset[0], 0., rig.SCALE, offset[1]), Image.Resampling.BICUBIC)


def rotation_between(u, v):
    u = u/np.linalg.norm(u)
    v = v/np.linalg.norm(v)
    cross = np.cross(u, v)
    cosine = np.dot(u, v)
    skew = np.array([[0., -cross[2], cross[1]], [cross[2], 0., -cross[0]], [-cross[1], cross[0], 0.]])
    return np.eye(3)+skew+skew@skew/max(1.+cosine, 1e-9)


def group_points(group, pose):
    a, b = np.asarray(group['from']), np.asarray(group['to'])
    if group['id'][0]=='T':
        moved = np.array([a, b])+np.asarray(pose['rootOffset3D'])
    else:
        ident = group['id'][:2]
        rest = rig.REST[ident]
        posed = np.asarray(pose['limbs'][ident]['joints3D'])
        middle = (a+b)/2.
        distances = []
        for c, d in zip(rest[:2], rest[1:3]):
            axis = d-c
            q = np.clip(np.dot(middle-c, axis)/np.dot(axis, axis), 0., 1.)
            distances.append(np.linalg.norm(middle-c-q*axis))
        segment = int(np.argmin(distances))
        rotation = rotation_between(rest[segment+1]-rest[segment], posed[segment+1]-posed[segment])
        moved = posed[segment]+(np.array([a, b])-rest[segment])@rotation.T
    return rig.screen(rig.project(moved))


def muscle_overlay(image, pose, identifiers=False):
    base = background(image, '#262b31')
    layer = Image.new('RGBA', image.size)
    draw = ImageDraw.Draw(layer)
    for group in GROUPS:
        a, b = group_points(group, pose)
        rgb = tuple(int(group['color'][k:k+2], 16) for k in [1, 3, 5])
        if identifiers:
            draw.line([tuple(a), tuple(b)], fill=rgb+(255,), width=3)
            direction = (b-a)/max(np.linalg.norm(b-a), 1e-9)
            side = np.array([-direction[1], direction[0]])
            draw.polygon([tuple(b), tuple(b-direction*10+side*4), tuple(b-direction*10-side*4)], fill=rgb+(255,))
            draw.ellipse((a[0]-4, a[1]-4, a[0]+4, a[1]+4), outline=rgb+(255,), width=2)
            draw.rectangle((b[0]-3, b[1]-3, b[0]+3, b[1]+3), outline=rgb+(255,), width=2)
            middle = (a+b)/2.
            draw.text(tuple(middle+np.array([5., -10.])), group['id'], font=rig.SMALL, fill=rgb+(255,))
        else:
            radius = group['radius']*375.*rig.SCALE
            draw.line([tuple(a), tuple(b)], fill=rgb+(80,), width=max(3, round(radius*2)))
            for point in [a, b]:
                draw.ellipse((point[0]-radius, point[1]-radius, point[0]+radius, point[1]+radius), fill=rgb+(60,))
    base.alpha_composite(layer)
    return base


def run(state):
    folder = ROOT/state
    config = rig.CONFIG[state]
    sequence = rig.poses(state)
    frames = [Image.open(folder/'frames'/f'{state}-{i:02d}.png').convert('RGBA') for i in range(len(sequence))]
    geometry = json.loads((folder/'deformation-measurements.json').read_text(encoding='utf-8'))
    report_frames = []
    lengths = []
    for i, (image, pose) in enumerate(zip(frames, sequence)):
        alpha = np.asarray(image.getchannel('A'))
        report_frames.append({'index': i, 'size': list(image.size), 'mode': image.mode, 'bbox': list(image.getbbox()),
                              'edgeAlphaMax': int(max(alpha[0].max(), alpha[-1].max(), alpha[:, 0].max(), alpha[:, -1].max())),
                              'sha256': hashlib.sha256((folder/'frames'/f'{state}-{i:02d}.png').read_bytes()).hexdigest()})
        for ident, limb in pose['limbs'].items():
            actual = np.linalg.norm(np.diff(limb['joints3D'], axis=0), axis=1)
            expected = np.linalg.norm(np.diff(rig.REST[ident], axis=0), axis=1)
            lengths.extend(abs(actual-expected).tolist())
    registrations = [registered(image, pose) for image, pose in zip(frames, sequence)]
    boxes = {'head': (440, 620, 515, 690), 'neck': (600, 610, 640, 710), 'back': (790, 615, 930, 705), 'rump': (1070, 635, 1120, 700)}
    brightness = {}
    for name, box in boxes.items():
        values = [float(np.mean(np.asarray(image.crop(box))[..., :3]@np.array([.2126, .7152, .0722]))) for image in registrations]
        brightness[name] = {'means': values, 'min': min(values), 'max': max(values), 'range255': max(values)-min(values)}
    world_velocity_errors = []
    contacts = []
    if state!='attack':
        duration = sum(config['durationsMs'])/1000.
        speed = config['stride']/(config['duty']*duration)
        for t in np.arange(0., 1., .002):
            p = rig.locomotion_pose(state, t)
            contacts.append(sum(limb['state']=='stance' for limb in p['limbs'].values()))
            before = rig.locomotion_pose(state, t-1e-6)
            after = rig.locomotion_pose(state, t+1e-6)
            for ident, limb in p['limbs'].items():
                duty = config['duty']*(config['stride'] if ident[0]=='F' else config['rearStride'])/config['stride']
                if .002<limb['phase']<duty-.002:
                    a = np.asarray(before['limbs'][ident]['joints3D'][3])
                    b = np.asarray(after['limbs'][ident]['joints3D'][3])
                    velocity = (b-a)/(2e-6*duration)-np.array([speed, 0., 0.])
                    world_velocity_errors.append(float(np.linalg.norm(velocity)))
        first, last = rig.locomotion_pose(state, 0.), rig.locomotion_pose(state, 1.)
        loop_error = max(float(np.max(abs(np.asarray(first['limbs'][ident]['joints3D'])-np.asarray(last['limbs'][ident]['joints3D'])))) for ident in rig.REST)
        brace_error = None
    else:
        contacts = [sum(limb['state'] in ['stance', 'brace'] for limb in pose['limbs'].values()) for pose in sequence]
        loop_error = None
        brace_error = max(float(np.max(abs(np.asarray(pose['limbs'][ident]['joints3D'][3])-np.asarray(sequence[0]['limbs'][ident]['joints3D'][3])))) for pose in sequence for ident in ['RU', 'RL'])
    rig.contact_sheet([muscle_overlay(image, pose) for image, pose in zip(frames, sequence)], folder/'03-volume-overlays.jpg')
    rig.contact_sheet([muscle_overlay(image, pose, True) for image, pose in zip(frames, sequence)], folder/'04-muscle-id-overlays.jpg')
    for index in [0, len(frames)//4, len(frames)//2, 3*len(frames)//4]:
        muscle_overlay(frames[index], sequence[index]).save(folder/f'03-volumes-{index:02d}.png')
        muscle_overlay(frames[index], sequence[index], True).save(folder/f'04-muscle-ids-{index:02d}.png')
    comparison = Image.new('RGB', (960, 450), '#ebeae6')
    draw = ImageDraw.Draw(comparison)
    for col, index in enumerate([8, 9] if len(frames)>9 else [0, len(frames)-1]):
        crop = background(registrations[index]).crop((360, 535, 760, 795)).resize((480, 312), Image.Resampling.LANCZOS)
        comparison.paste(crop.convert('RGB'), (col*480, 35))
        draw.text((col*480+12, 8), f'{state} {index:02d} / registered', font=rig.FONT, fill='#20262c')
    draw.text((15, 375), f'Neck luma range: {brightness["neck"]["range255"]:.3f}/255; back: {brightness["back"]["range255"]:.3f}/255', font=rig.FONT, fill='#20262c')
    comparison.save(folder/'review-neck-08-09.png')
    details = Image.new('RGB', (4*340, 3*170), '#ebeae6')
    for col, index in enumerate([0, len(frames)//4, len(frames)//2, 3*len(frames)//4]):
        im = background(frames[index])
        for row, box in enumerate([(550, 345, 635, 387), (550, 625, 635, 667), (765, 585, 850, 627)]):
            details.paste(im.crop(box).resize((340, 168), Image.Resampling.NEAREST).convert('RGB'), (col*340, row*170))
    details.save(folder/'review-joint-details.png')
    identical_end = report_frames[0]['sha256']==report_frames[-1]['sha256']
    report = {'task': 'TASK-005', 'state': state, 'revision': 'simplified-motion-r1',
              'masterSha256': hashlib.sha256((ROOT/'simplified-master-v1.png').read_bytes()).hexdigest(),
              'frameCount': len(frames), 'durationsMs': config['durationsMs'], 'durationMs': sum(config['durationsMs']), 'loop': config['loop'],
              'pivot': rig.screen([760., 665.]).tolist(), 'perFrameFit': False, 'maxBoneLengthError3D': max(lengths),
              'minSupportingFeet': min(contacts), 'maxSupportingFeet': max(contacts),
              'maxStanceWorldFootVelocityError': max(world_velocity_errors) if world_velocity_errors else None,
              'rearBraceDisplacement': brace_error, 'exactLoopPoseError': loop_error, 'firstLastImageIdentical': identical_end,
              'registeredBrightness': brightness, 'functionalMuscleCount': len(GROUPS), 'functionalMuscleIds': [group['id'] for group in GROUPS],
              'frames': report_frames, 'deformation': geometry,
              'technicalExport': 'PASS' if all(item['edgeAlphaMax']==0 and item['mode']=='RGBA' for item in report_frames) else 'FAIL',
              'surfaceOrientation': 'PASS' if all(item['flippedTriangles']==0 for item in geometry) else 'FAIL',
              'artisticApproval': False, 'unityImport': False, 'authoringReview': 'not applicable; no scene objects changed',
              'limitations': ['Contacts and vertical heights are designed trajectories, not a rigid-body physics simulation.', '22 aggregates are a prospective functional schematic, not a recovered medical atlas.', 'Sprite state transitions at arbitrary trigger times are not integrated in Unity.']}
    (folder/'technical-validation.json').write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding='utf-8')
    print(json.dumps({'state': state, 'export': report['technicalExport'], 'surface': report['surfaceOrientation'], 'supports': [min(contacts), max(contacts)], 'footError': report['maxStanceWorldFootVelocityError'], 'brace': brace_error, 'identicalEnd': identical_end, 'brightnessRanges': {key:value['range255'] for key,value in brightness.items()}}, ensure_ascii=False))


if __name__=='__main__':
    for state in sys.argv[1:] or ['walk', 'attack', 'run']:
        run(state)
