"""Separate anatomy passes, contact checks and three-way preview export."""
from pathlib import Path
import hashlib
import json
import sys
import numpy as np
from PIL import Image,ImageDraw
import build_body as body
import verify_motions as checks

ROOT = Path(__file__).resolve().parent
rig = body.rig
checks.rig = rig
original_group_points = checks.group_points


def group_points(group,pose):
    if group['id'][0]=='T':
        points = body.body_map(np.array([group['from'],group['to']]),pose['body'])
        return rig.screen(rig.project(points))
    return original_group_points(group,pose)


checks.group_points = group_points


def inputs():
    plan = json.loads((ROOT/'motion-plan.json').read_text())
    frames = [Image.open(ROOT/'frames'/f'body-{i:02d}.png').convert('RGBA') for i in range(body.COUNT)]
    return frames,plan['frames']


def overlays(mode):
    frames,poses = inputs()
    if mode=='bones':
        images = [rig.bone_frame(p,im) for im,p in zip(frames,poses)]
        name = '02-bones-on-skin'
    else:
        identifiers = mode=='muscles'
        images = [checks.muscle_overlay(im,p,identifiers) for im,p in zip(frames,poses)]
        name = '04-muscle-ids' if identifiers else '03-volumes'
    rig.contact_sheet(images,ROOT/f'{name}.jpg')
    for index in [0,6,12,18]:
        images[index].save(ROOT/'review'/f'{name}-{index:02d}.png')


def frame_at(age,config):
    at = age%config['duration']
    for i,duration in enumerate(config['durationsMs']):
        at -= duration/1000
        if at<1e-9:
            return i
    return len(config['frames'])-1


def comparison_gif(config):
    paths = [ROOT.parent/'FastGroupEnemy20HeavyLocomotion/old.json',ROOT.parent/'FastGroupEnemy20HeavyLocomotion/new.json',ROOT/'body.json']
    clips = []
    scale = .37
    for path in paths:
        c = json.loads(path.read_text())
        images = [Image.open(path.parent/name).convert('RGBA').rotate(90).resize((379,379),Image.Resampling.LANCZOS) for name in c['frames']]
        clips.append((c,images))
    game_cycle = config['duration']*config['referenceSpeed']/1.65
    tile = game_cycle*1.65*400*scale
    frames = []
    labels = ['1 · Исходный бег','2 · Обновлённые лапы','3 · Лапы + корпус']
    for i in range(72):
        time = i/24*game_cycle
        canvas = Image.new('RGBA',(1200,650),'#292e31')
        draw = ImageDraw.Draw(canvas)
        drift = (time*1.65*400*scale)%tile
        for x in np.arange(0,1200,tile):
            draw.line([(round(x),85),(round(x),600)],fill='#394246')
        for y in np.arange(85-tile,600+tile,tile):
            draw.line([(0,round(y-drift)),(1200,round(y-drift))],fill='#394246')
        for col,(c,images) in enumerate(clips):
            index = frame_at(time*1.65/c['referenceSpeed'],c)
            canvas.alpha_composite(images[index],(round(200+col*400-511.8*scale),round(340-(1024-602.2)*scale)))
            draw.rectangle((col*400,0,col*400+400,85),fill='#20272c')
            draw.text((col*400+30,30),labels[col],font=rig.FONT,fill='#ecf0f1')
        for x in [400,800]:
            draw.line([(x,0),(x,650)],fill='#536168')
        draw.rectangle((0,600,1200,650),fill='#20272c')
        draw.text((20,615),'Одна скорость: 1.65 ед/с. У вариантов 2 и 3 одинаковые траектории опорных лап.',font=rig.SMALL,fill='#bccbd0')
        frames.append(canvas.convert('RGB'))
    swatch = Image.new('RGB',(300,163*12))
    for i,im in enumerate(frames[::6]):
        swatch.paste(im.resize((300,163)),(0,i*163))
    palette = swatch.quantize(colors=255,method=Image.Quantize.MEDIANCUT)
    encoded = [im.quantize(palette=palette,dither=Image.Dither.NONE) for im in frames]
    ticks = [round(i/24*game_cycle*100) for i in range(73)]
    encoded[0].save(ROOT/'three-way-comparison.gif',save_all=True,append_images=encoded[1:],duration=[10*(ticks[i+1]-ticks[i]) for i in range(72)],loop=0,optimize=False,disposal=1)


def finish():
    frames,poses = inputs()
    duration = sum(body.heavy.DURATIONS)/1000
    reference_speed = body.heavy.STRIDE/(body.heavy.DUTY*duration)*375*rig.SCALE/400
    config = {'revision':'body-motion-r2','frames':[f'frames/body-{i:02d}.png' for i in range(body.COUNT)],'durationsMs':body.heavy.DURATIONS,'duration':duration,'referenceSpeed':reference_speed,'pivotPixelsTopLeft':rig.screen([760,665]).tolist(),'loop':True}
    body.write_json('body.json',config)
    rig.contact_sheet([checks.background(im,'#ebeae6') for im in frames],ROOT/'05-clean-light.jpg')
    rig.contact_sheet([checks.background(im,'#262b31') for im in frames],ROOT/'06-clean-dark.jpg')
    lengths,spine_lengths,foot_changes = [],[],[]
    for t in np.arange(0,1,.002):
        p = body.pose(t)
        baseline = body.heavy.pose(t)
        for ident,limb in p['limbs'].items():
            actual = np.linalg.norm(np.diff(limb['joints3D'],axis=0),axis=1)
            expected = np.linalg.norm(np.diff(rig.REST[ident],axis=0),axis=1)
            lengths.extend(abs(actual-expected))
            foot_changes.append(float(np.max(abs(np.asarray(limb['joints3D'][2:])-np.asarray(baseline['limbs'][ident]['joints3D'][2:])))))
        actual = np.linalg.norm(np.diff(p['body']['spine3D'],axis=0),axis=1)
        expected = np.linalg.norm(np.diff(body.REST_SPINE,axis=0),axis=1)
        spine_lengths.extend(abs(actual-expected))
    first,last = body.pose(0),body.pose(1)
    loop_error = max(float(np.max(abs(np.asarray(first['limbs'][ident]['joints3D'])-np.asarray(last['limbs'][ident]['joints3D'])))) for ident in rig.REST)
    geometry = [json.loads((ROOT/'measurements'/f'{i:02d}.json').read_text()) for i in range(body.COUNT)]
    exports = []
    for index,im in enumerate(frames):
        a = np.asarray(im.getchannel('A'))
        exports.append({'index':index,'mode':im.mode,'size':list(im.size),'edgeAlphaMax':int(max(a[0].max(),a[-1].max(),a[:,0].max(),a[:,-1].max())),'sha256':hashlib.sha256((ROOT/config['frames'][index]).read_bytes()).hexdigest()})
    nodes = np.array([p['body']['spine3D'] for p in poses])
    relative = (nodes-nodes[:,1:2,:])*375*rig.SCALE
    report = {
        'task':'TASK-005','revision':'body-motion-r2','frameCount':body.COUNT,
        'masterSha256':hashlib.sha256(body.heavy.MASTER.read_bytes()).hexdigest(),
        'maxLimbBoneLengthError':float(max(lengths)),
        'maxSpineBoneLengthError':float(max(spine_lengths)),
        'maxWristPalmDifferenceFromR2':max(foot_changes),
        'minContacts':min(sum(l['state']=='stance' for l in p['limbs'].values()) for p in poses),
        'exactLimbLoopPoseError':loop_error,
        'exactSpineLoopPoseError':float(np.max(abs(np.asarray(first['body']['spine3D'])-np.asarray(last['body']['spine3D'])))),
        'relativeSpineNodeLateralRangesPixels':np.ptp(relative[:,:,1],axis=0).tolist(),
        'functionalMuscleIds':[g['id'] for g in checks.GROUPS],
        'frames':exports,'deformation':geometry,
        'technicalExport':'PASS' if all(e['edgeAlphaMax']==0 and e['mode']=='RGBA' and e['size']==[1024,1024] for e in exports) else 'FAIL',
        'surfaceOrientation':'PASS' if all(g['flippedTriangles']==0 for g in geometry) else 'FAIL',
        'artisticApproval':False,'unityImport':False,
        'authoringReview':'not applicable; no scene objects changed',
        'limitations':['Authored kinematics, not a rigid-body or soft-tissue simulation.','No new dynamic lighting: one original painting follows the moving surface.','Old footage has the previous head design; columns 2 and 3 use the same approved J design.','GIF repeats a short capture; the old cycle is cut at replay, while HTML plays each loop continuously.','New state transitions and runtime integration are outside this comparison.']
    }
    body.write_json('technical-validation.json',report)
    frames[0].save(ROOT/'body-native.apng',save_all=True,append_images=frames[1:],duration=body.heavy.DURATIONS,loop=0,disposal=0,blend=0)
    comparison_gif(config)
    print(json.dumps({k:report[k] for k in ['technicalExport','surfaceOrientation','maxLimbBoneLengthError','maxSpineBoneLengthError','maxWristPalmDifferenceFromR2','minContacts','relativeSpineNodeLateralRangesPixels']}),flush=True)


if __name__=='__main__':
    modes = {'--bones':'bones','--volumes':'volumes','--muscles':'muscles'}
    selected = next((modes[arg] for arg in sys.argv[1:] if arg in modes),None)
    if selected:
        overlays(selected)
    else:
        finish()
