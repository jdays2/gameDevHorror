"""Checks for the keyed R2 gait; anatomy illustrations remain separate passes."""
from pathlib import Path
import hashlib
import json
import sys
import numpy as np
from PIL import Image, ImageDraw
import build_heavy as heavy
import verify_motions as checks

ROOT = Path(__file__).resolve().parent
rig = heavy.base
checks.rig = rig


def images_and_poses():
    plan = json.loads((ROOT/'motion-plan.json').read_text())
    frames = [Image.open(ROOT/'frames'/f'heavy-{i:02d}.png').convert('RGBA') for i in range(heavy.COUNT)]
    return frames, plan['frames']


def bones():
    frames, poses = images_and_poses()
    rig.contact_sheet([rig.bone_frame(pose, image) for image, pose in zip(frames, poses)], ROOT/'02-bones-on-skin.jpg')


def muscles(identifiers=False):
    frames, poses = images_and_poses()
    label = '04-muscle-ids' if identifiers else '03-volumes'
    rig.contact_sheet([checks.muscle_overlay(image, pose, identifiers) for image, pose in zip(frames, poses)], ROOT/f'{label}.jpg')
    for index in [0, 6, 12, 18]:
        checks.muscle_overlay(frames[index], poses[index], identifiers).save(ROOT/f'review/{label}-{index:02d}.png')


def export_and_validate():
    frames, poses = images_and_poses()
    duration = sum(heavy.DURATIONS)/1000
    source_speed = heavy.STRIDE/(heavy.DUTY*duration)
    reference_speed = source_speed*375*rig.SCALE/400
    config = {
        'revision': 'heavy-locomotion-r2',
        'frames': [f'frames/heavy-{i:02d}.png' for i in range(heavy.COUNT)],
        'durationsMs': heavy.DURATIONS,
        'duration': duration,
        'referenceSpeed': reference_speed,
        'loop': True,
        'pivotPixelsTopLeft': rig.screen([760, 665]).tolist()
    }
    (ROOT/'new.json').write_text(json.dumps(config, indent=2),newline="\n")
    frames[0].save(ROOT/'heavy-native.apng', save_all=True, append_images=frames[1:], duration=heavy.DURATIONS, loop=0, disposal=0, blend=0)
    # Diagnostic pair at the same 1.65 world units/s as the HTML comparison.
    # One grid interval equals the travel in one cycle, so the GIF floor loops.
    display_duration = duration*reference_speed/1.65
    pair_frames = []
    scale = .4
    tile = display_duration*1.65*400*scale
    for i in range(heavy.COUNT):
        canvas = Image.new('RGBA', (960, 620), '#292e31')
        draw = ImageDraw.Draw(canvas)
        drift = tile*i/heavy.COUNT
        for x in np.arange(0, 960, tile):
            draw.line([(round(x), 0), (round(x), 620)], fill='#394246')
        for y in np.arange(-tile, 620+tile, tile):
            draw.line([(0, round(y-drift)), (960, round(y-drift))], fill='#394246')
        for actor, center in enumerate([(270,305), (690,305)]):
            index = (i+round(heavy.COUNT*.27)*actor)%heavy.COUNT
            sprite = frames[index].rotate(90).resize((410,410),Image.Resampling.LANCZOS)
            canvas.alpha_composite(sprite,(round(center[0]-511.8*scale), round(center[1]-(1024-602.2)*scale)))
        draw.text((20,580),'R2 / 1.65 world units per second / 2 actors',font=rig.SMALL,fill='#c8d2d7')
        pair_frames.append(canvas.convert('RGB'))
    swatch = Image.new('RGB',(240,120*heavy.COUNT))
    for i,im in enumerate(pair_frames):
        swatch.paste(im.resize((240,155)).crop((0,17,240,137)),(0,120*i))
    palette = swatch.quantize(colors=255,method=Image.Quantize.MEDIANCUT)
    gif_frames = [im.quantize(palette=palette,dither=Image.Dither.NONE) for im in pair_frames]
    ticks = [round(i*display_duration*100/heavy.COUNT) for i in range(heavy.COUNT+1)]
    gif_durations = [(ticks[i+1]-ticks[i])*10 for i in range(heavy.COUNT)]
    gif_frames[0].save(ROOT/'two-heavy-runners.gif',save_all=True,append_images=gif_frames[1:],duration=gif_durations,loop=0,optimize=False,disposal=1)
    rig.contact_sheet([checks.background(image, '#ebeae6') for image in frames], ROOT/'05-clean-light.jpg')
    rig.contact_sheet([checks.background(image, '#262b31') for image in frames], ROOT/'06-clean-dark.jpg')
    registered = [checks.registered(image, pose) for image, pose in zip(frames, poses)]
    brightness = {}
    for name, box in {'head':(440, 620, 515, 690), 'neck':(600, 610, 640, 710), 'back':(790, 615, 930, 705)}.items():
        values = [float(np.mean(np.asarray(image.crop(box))[..., :3]@np.array([.2126,.7152,.0722]))) for image in registered]
        brightness[name] = {'means':values, 'range255':max(values)-min(values)}
    strip = Image.new('RGB', (1200, 350), '#ebeae6')
    draw = ImageDraw.Draw(strip)
    for col, index in enumerate([8, 9, 23, 0]):
        crop = checks.background(registered[index]).crop((360, 530, 760, 805)).resize((300, 207), Image.Resampling.LANCZOS)
        strip.paste(crop.convert('RGB'), (col*300, 50))
        draw.text((col*300+12, 12), f'frame {index:02d} / registered', font=rig.SMALL, fill='#20262c')
    strip.save(ROOT/'review/neck-08-09-loop.png')
    contacts, errors, bone_errors = [], [], []
    for t in np.arange(0, 1, .001):
        pose = heavy.pose(t)
        contacts.append(sum(limb['state']=='stance' for limb in pose['limbs'].values()))
        before, after = heavy.pose(t-1e-6), heavy.pose(t+1e-6)
        for ident, limb in pose['limbs'].items():
            expected = np.linalg.norm(np.diff(rig.REST[ident],axis=0),axis=1)
            actual = np.linalg.norm(np.diff(limb['joints3D'],axis=0),axis=1)
            bone_errors.extend(abs(actual-expected))
            duty = heavy.DUTY if ident[0]=='F' else heavy.REAR_DUTY
            if .003 < limb['phase'] < duty-.003:
                a = np.asarray(before['limbs'][ident]['joints3D'][3])
                b = np.asarray(after['limbs'][ident]['joints3D'][3])
                errors.append(float(np.linalg.norm((b-a)/(2e-6*duration)-np.array([source_speed,0,0]))))
    first, last = heavy.pose(0), heavy.pose(1)
    loop_error = max(float(np.max(abs(np.asarray(first['limbs'][ident]['joints3D'])-np.asarray(last['limbs'][ident]['joints3D'])))) for ident in rig.REST)
    exports = []
    for index, im in enumerate(frames):
        alpha = np.asarray(im.getchannel('A'))
        exports.append({'index':index, 'mode':im.mode, 'size':list(im.size), 'bbox':list(im.getbbox()), 'edgeAlphaMax':int(max(alpha[0].max(),alpha[-1].max(),alpha[:,0].max(),alpha[:,-1].max())), 'sha256':hashlib.sha256((ROOT/config['frames'][index]).read_bytes()).hexdigest()})
    geometry = [json.loads((ROOT/'measurements'/f'{i:02d}.json').read_text()) for i in range(heavy.COUNT)]
    report = {
        'task':'TASK-005', 'revision':'heavy-locomotion-r2',
        'masterSha256':hashlib.sha256(heavy.MASTER.read_bytes()).hexdigest(),
        'frameCount':heavy.COUNT, 'nativeLoopMs':duration*1000,
        'referenceSpeedWorldUnitsPerSecond':reference_speed,
        'viewerGroundSpeed':1.65, 'viewerLoopMs':duration*reference_speed/1.65*1000,
        'minSupportingFeet':min(contacts), 'maxSupportingFeet':max(contacts),
        'maxBoneLengthError3D':float(max(bone_errors)),
        'maxStanceWorldFootVelocityError':max(errors), 'exactLoopPoseError':loop_error,
        'functionalMuscleIds':[group['id'] for group in checks.GROUPS],
        'registeredBrightness':brightness, 'frames':exports, 'deformation':geometry,
        'technicalExport':'PASS' if all(item['edgeAlphaMax']==0 and item['mode']=='RGBA' and item['size']==[1024,1024] for item in exports) else 'FAIL',
        'surfaceOrientation':'PASS' if all(item['flippedTriangles']==0 for item in geometry) else 'FAIL',
        'artisticApproval':False, 'unityImport':False,
        'authoringReview':'not applicable; no scene objects changed',
        'limitations':[
            'Designed contact and height trajectories, not a rigid-body simulation.',
            '22 prospective functional aggregates, not a recovered medical atlas.',
            'One movement proof; transitions to existing attack, walk and death sprites are not transferred to the approved new head.',
            'A technical pass does not establish artistic menace or owner acceptance.'
        ]
    }
    (ROOT/'technical-validation.json').write_text(json.dumps(report,indent=2),newline="\n")
    print(json.dumps({key:report[key] for key in ['technicalExport','surfaceOrientation','minSupportingFeet','maxSupportingFeet','maxBoneLengthError3D','maxStanceWorldFootVelocityError','exactLoopPoseError','viewerLoopMs','registeredBrightness']}))


if __name__=='__main__':
    if '--bones' in sys.argv:
        bones()
    elif '--volumes' in sys.argv:
        muscles(False)
    elif '--muscles' in sys.argv:
        muscles(True)
    else:
        export_and_validate()
