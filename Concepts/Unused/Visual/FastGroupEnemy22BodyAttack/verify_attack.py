"""Validate the sweep transfer and export a synchronized before/after preview."""
from pathlib import Path
import hashlib
import json
import sys
import numpy as np
from PIL import Image,ImageDraw
import build_attack as attack
import verify_motions as checks

ROOT = Path(__file__).resolve().parent
rig = attack.rig
checks.rig = rig
original_points = checks.group_points


def group_points(group,pose):
    if group['id'][0]=='T':
        points = attack.body.body_map(np.array([group['from'],group['to']]),pose['body'])
        return rig.screen(rig.project(points))
    return original_points(group,pose)


checks.group_points = group_points


def inputs():
    plan = json.loads((ROOT/'motion-plan.json').read_text(encoding='utf-8'))
    frames = [Image.open(ROOT/'frames'/f'sweep-{i:02d}.png').convert('RGBA') for i in range(plan['frameCount'])]
    return plan,frames


def overlays(mode):
    plan,frames = inputs()
    if mode=='bones':
        images = [rig.bone_frame(p,im) for im,p in zip(frames,plan['frames'])]
        name = '02-bones-on-skin'
    else:
        identifiers = mode=='muscles'
        images = [checks.muscle_overlay(im,p,identifiers) for im,p in zip(frames,plan['frames'])]
        name = '04-muscle-ids' if identifiers else '03-volumes'
    rig.contact_sheet(images,ROOT/f'{name}.jpg')
    for index in [0,7,9,17]:
        images[index].save(ROOT/'review'/f'{name}-{index:02d}.png')


def preview(plan,frames,old_folder):
    old_frames = [Image.open(old_folder/'frames'/f'sweep-{i:02d}.png').convert('RGBA') for i in range(len(frames))]
    output = []
    labels = ['Прежняя атака','Новая · движение корпуса']
    scale = .48
    for index in range(len(frames)):
        canvas = Image.new('RGBA',(1000,760),'#292e31')
        draw = ImageDraw.Draw(canvas)
        for x in range(0,1000,100):
            draw.line([(x,80),(x,700)],fill='#394246')
        for y in range(80,700,100):
            draw.line([(0,y),(1000,y)],fill='#394246')
        for col,image in enumerate([old_frames[index],frames[index]]):
            sprite = image.rotate(90).resize((492,492),Image.Resampling.LANCZOS)
            canvas.alpha_composite(sprite,(round(250+500*col-511.8*scale),round(400-(1024-602.2)*scale)))
            draw.rectangle((col*500,0,col*500+500,80),fill='#20272c')
            draw.text((col*500+30,28),labels[col],font=rig.FONT,fill='#edf0f1')
        draw.line([(500,0),(500,760)],fill='#536168')
        draw.rectangle((0,700,1000,760),fill='#20272c')
        names={'ready':'Стойка','anticipation':'Замах','strike':'Удар','recovery':'Возврат'}
        draw.text((25,720),f'{names[plan["frames"][index]["stage"]]} · одинаковая дуга когтей и темп 1.21 с',font=rig.FONT,fill='#bccbd0')
        output.append(canvas.convert('RGB'))
    swatch=Image.new('RGB',(250,190*len(output)))
    for i,im in enumerate(output):
        swatch.paste(im.resize((250,190)),(0,190*i))
    palette=swatch.quantize(colors=255,method=Image.Quantize.MEDIANCUT)
    encoded=[im.quantize(palette=palette,dither=Image.Dither.NONE) for im in output]
    durations=attack.DURATIONS.copy()
    durations[-1]+=500
    encoded[0].save(ROOT/'attack-before-after.gif',save_all=True,append_images=encoded[1:],duration=durations,loop=0,optimize=False,disposal=1)


def finish():
    plan,frames=inputs()
    old_folder=ROOT.parent/'FastGroupEnemy17SimplifiedMotion/alternatives/heavy/sweep'
    old_plan=json.loads((old_folder/'motion-plan.json').read_text(encoding='utf-8'))
    common={'durationsMs':attack.DURATIONS,'duration':sum(attack.DURATIONS)/1000,'loop':False,'stages':[p['stage'] for p in plan['frames']],'pivotPixelsTopLeft':rig.screen([760,665]).tolist()}
    attack.write_json('new.json',{**common,'frames':[f'frames/sweep-{i:02d}.png' for i in range(len(frames))]})
    attack.write_json('old.json',{**common,'frames':[f'../FastGroupEnemy17SimplifiedMotion/alternatives/heavy/sweep/frames/sweep-{i:02d}.png' for i in range(len(frames))]})
    rig.contact_sheet([checks.background(im,'#ebeae6') for im in frames],ROOT/'05-clean-light.jpg')
    rig.contact_sheet([checks.background(im,'#262b31') for im in frames],ROOT/'06-clean-dark.jpg')
    lengths,spine_lengths,arc_changes,brace_changes,support=[],[],[],[],[]
    neutral=attack.pose(0)
    for t in np.linspace(0,1,401):
        p=attack.pose(float(t))
        old=attack.broad.pose('sweep',float(t))
        support.append(sum(l['state'] in ['stance','brace'] for l in p['limbs'].values()))
        actual=np.linalg.norm(np.diff(p['body']['spine3D'],axis=0),axis=1)
        expected=np.linalg.norm(np.diff(attack.body.REST_SPINE,axis=0),axis=1)
        spine_lengths.extend(abs(actual-expected))
        for ident,limb in p['limbs'].items():
            actual=np.linalg.norm(np.diff(limb['joints3D'],axis=0),axis=1)
            expected=np.linalg.norm(np.diff(rig.REST[ident],axis=0),axis=1)
            lengths.extend(abs(actual-expected))
            arc_changes.append(float(np.max(abs(np.asarray(limb['joints3D'][2:])-np.asarray(old['limbs'][ident]['joints3D'][2:])))))
            arc_changes.append(abs(limb['handAngleRad']-old['limbs'][ident]['handAngleRad']))
            if ident!='FU':
                brace_changes.append(float(np.max(abs(np.asarray(limb['joints3D'][3])-np.asarray(neutral['limbs'][ident]['joints3D'][3])))))
    geometry=[json.loads((ROOT/'measurements'/f'{i:02d}.json').read_text()) for i in range(len(frames))]
    exports=[]
    for i,image in enumerate(frames):
        a=np.asarray(image.getchannel('A'))
        exports.append({'index':i,'size':list(image.size),'mode':image.mode,'edgeAlphaMax':int(max(a[0].max(),a[-1].max(),a[:,0].max(),a[:,-1].max())),'sha256':hashlib.sha256((ROOT/'frames'/f'sweep-{i:02d}.png').read_bytes()).hexdigest()})
    first_last_equal=exports[0]['sha256']==exports[-1]['sha256']
    report={
        'task':'TASK-005','revision':'body-sweep-r1','state':'sweep','frameCount':len(frames),'durationMs':sum(attack.DURATIONS),
        'masterSha256':hashlib.sha256(attack.body.heavy.MASTER.read_bytes()).hexdigest(),
        'maxLimbBoneLengthError':float(max(lengths)),'maxSpineBoneLengthError':float(max(spine_lengths)),
        'maxHandArcAndAngleDifferenceFromOld':max(arc_changes),'maxBraceDisplacement':max(brace_changes),
        'minSupportingFeet':min(support),'firstAndLastImageIdentical':first_last_equal,
        'sameTimingAsOld':old_plan['durationsMs']==plan['durationsMs'],
        'functionalMuscleIds':[g['id'] for g in checks.GROUPS],
        'frames':exports,'deformation':geometry,
        'technicalExport':'PASS' if all(e['edgeAlphaMax']==0 and e['size']==[1024,1024] and e['mode']=='RGBA' for e in exports) else 'FAIL',
        'surfaceOrientation':'PASS' if all(g['flippedTriangles']==0 for g in geometry) else 'FAIL',
        'artisticApproval':False,'unityImport':False,'authoringReview':'not applicable; no scene objects changed',
        'limitations':['Authored kinematics, not force simulation.','Only the primary wide sweep is transferred; other attack variants retain their previous exports.','One original painting follows the moving surface; no new lighting or generated texture per frame.','Neutral attack stance is explicit; transitions from an arbitrary walking phase are not integrated.','No runtime damage event or Unity state-machine change.']
    }
    attack.write_json('technical-validation.json',report)
    frames[0].save(ROOT/'attack-native.apng',save_all=True,append_images=frames[1:],duration=attack.DURATIONS,loop=1,disposal=0,blend=0)
    preview(plan,frames,old_folder)
    print(json.dumps({k:report[k] for k in ['technicalExport','surfaceOrientation','maxLimbBoneLengthError','maxSpineBoneLengthError','maxHandArcAndAngleDifferenceFromOld','maxBraceDisplacement','minSupportingFeet','firstAndLastImageIdentical','sameTimingAsOld']}),flush=True)


if __name__=='__main__':
    modes={'--bones':'bones','--volumes':'volumes','--muscles':'muscles'}
    mode=next((modes[arg] for arg in sys.argv[1:] if arg in modes),None)
    if mode:
        overlays(mode)
    else:
        finish()
