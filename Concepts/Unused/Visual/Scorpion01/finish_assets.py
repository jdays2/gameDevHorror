"""Verify the declared scorpion gait, export stable atlases and review sheets."""
from pathlib import Path
import hashlib
import json
import math
import numpy as np
from PIL import Image, ImageDraw, ImageFont
import build_native as native

ROOT=Path(__file__).resolve().parent
PROJECT=ROOT.parents[3]
ART=PROJECT/'Assets/EmergencyShift/Art/Enemies/Scorpion'
FONT=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',18)


def sheet(paths,name,background):
    tiles=[]
    for index,path in enumerate(paths):
        im=Image.open(path).convert('RGBA')
        canvas=Image.new('RGBA',im.size,background)
        canvas.alpha_composite(im)
        canvas=canvas.resize((256,256),Image.Resampling.LANCZOS).convert('RGB')
        ImageDraw.Draw(canvas).text((8,235),f'{index:02d}',font=FONT,fill='#9aa6ad')
        tiles.append(canvas)
    out=Image.new('RGB',(256*6,256*math.ceil(len(tiles)/6)),background)
    for i,tile in enumerate(tiles):
        out.paste(tile,(i%6*256,i//6*256))
    out.save(ROOT/name,quality=92)


def main():
    p=native.fixed_plan()
    errors=[]
    contacts=[]
    planted_velocity=[]
    appendage_errors=[]
    jaw_gaps=[]
    eps=1e-6
    for kind,seconds,cycles in [('run',native.NATIVE_SECONDS,1),('attack',native.ATTACK_SECONDS,native.ATTACK_CYCLES)]:
        for t in np.linspace(0,1,1000,endpoint=False):
            pose=native.pose(float(t),kind)
            following=native.pose(float(t)+eps,kind)
            contacts.append(sum(l['contact'] for l in pose['legs'].values()))
            for ident,limb in pose['legs'].items():
                actual=np.linalg.norm(np.diff(limb['joints3D'],axis=0),axis=1)
                expected=np.linalg.norm(np.diff(p['walkingLegs'][ident],axis=0),axis=1)
                errors.extend(abs(actual-expected))
                if limb['contact'] and following['legs'][ident]['contact']:
                    a=np.asarray(limb['joints3D'][-1])
                    b=np.asarray(following['legs'][ident]['joints3D'][-1])
                    velocity=(b-a)/(eps*seconds)
                    velocity[1]+=native.STRIDE/native.DUTY/seconds*cycles
                    planted_velocity.append(np.linalg.norm(velocity))
    for kind in ['run','attack']:
        for t in np.linspace(0,1,400):
            q=native.pose(float(t),kind)
            for ident,palp in q['palps'].items():
                reference=p['pedipalps'][ident]
                for key in ['arm','fingers']:
                    actual=palp['arm']+[palp['hinge']] if key=='arm' else [palp['fixedTip'],palp['hinge'],palp['movingTip']]
                    expected=reference['arm']+[reference['hinge']] if key=='arm' else [reference['fixedTip'],reference['hinge'],reference['movingTip']]
                    appendage_errors.extend(abs(np.linalg.norm(np.diff(actual,axis=0),axis=1)-np.linalg.norm(np.diff(expected,axis=0),axis=1)))
                jaw_gaps.append(float(np.linalg.norm(np.asarray(palp['fixedTip'])-np.asarray(palp['movingTip']))))
            appendage_errors.extend(abs(np.linalg.norm(np.diff(q['tail'],axis=0),axis=1)-np.linalg.norm(np.diff(p['tail'],axis=0),axis=1)))
    ART.mkdir(parents=True,exist_ok=True)
    all_records=[]
    states=[]
    for kind,name in [('run','Run'),('attack','Attack')]:
        export=json.loads((ROOT/(kind+'-export.json')).read_text())
        count=export['count']
        paths=[ROOT/'frames'/kind/f'{kind}-{i:02d}.png' for i in range(count)]
        atlas=Image.new('RGBA',(512*6,512*math.ceil(count/6)))
        eyes=Image.new('RGBA',(256*6,256*math.ceil(count/6)))
        for index,path in enumerate(paths):
            im=Image.open(path).convert('RGBA')
            alpha=np.asarray(im.getchannel('A'))
            edge=int(max(alpha[0].max(),alpha[-1].max(),alpha[:,0].max(),alpha[:,-1].max()))
            assert edge==0,(path,edge)
            atlas.alpha_composite(im.resize((512,512),Image.Resampling.LANCZOS),(index%6*512,index//6*512))
            glow=Image.open(path.with_name(path.stem+'-eyes.png')).convert('RGBA')
            eyes.alpha_composite(glow.resize((256,256),Image.Resampling.LANCZOS),(index%6*256,index//6*256))
            metrics=json.loads((ROOT/'measurements'/kind/f'{index:02d}.json').read_text())
            assert metrics['flippedTriangles']==0
            assert metrics['chelaWalkingOverlapPixels']==0,(kind,index,metrics['chelaWalkingOverlapPixels'])
            assert metrics['chelaPairOverlapPixels']==0,(kind,index,metrics['chelaPairOverlapPixels'])
            all_records.append({'file':path.relative_to(ROOT).as_posix(),'sha256':hashlib.sha256(path.read_bytes()).hexdigest(),
                                'edgeAlpha':edge,'flippedTriangles':metrics['flippedTriangles'],
                                'maximumSurfaceCorrectionPx':metrics['maximumSurfaceCorrectionPx'],'jointPinErrorPx':metrics['jointPinErrorPx'],
                                'chelaWalkingOverlapPixels':metrics['chelaWalkingOverlapPixels'],
                                'chelaPairOverlapPixels':metrics['chelaPairOverlapPixels']})
        target=ART/(name+'.png')
        atlas.save(target)
        eye_target=ART/(name+'Eyes.png')
        eyes.save(eye_target)
        states.append({'name':name,'asset':target.relative_to(PROJECT).as_posix(),'columns':6,'cell':512,
                       'count':count,'durationsMs':export['durationsMs'],'loop':export['loop'],
                       'referenceSpeed':export['referenceSpeed']*.85,'pixelsPerUnit':200,'pivotPixelsTopLeft':[256,274],
                       'emission':{'name':name+'Eyes','asset':eye_target.relative_to(PROJECT).as_posix(),
                                   'columns':6,'cell':256,'count':count,'pixelsPerUnit':100,'pivotPixelsTopLeft':[128,137]}})
        sheet(paths,kind+'-clean-dark.jpg','#282e33')
        sheet(paths,kind+'-clean-light.jpg','#e5e5df')
        sheet([path.with_name(path.stem+'-bones.jpg') for path in paths],kind+'-all-bones.jpg','#282e33')
    neutral=ART/'Neutral.png'
    neutral.write_bytes((ROOT/'native-neutral.png').read_bytes())
    neutral_eyes=ART/'NeutralEyes.png'
    Image.open(ROOT/'neutral-eyes.png').resize((256,256),Image.Resampling.LANCZOS).save(neutral_eyes)
    manifest={'task':'TASK-005','revision':p['revision'],'role':'fast group attacker, visual prototype',
              'sourceForward':'up','audio':False,'brain':False,'damageEvents':False,
              'idle':{'name':'Idle','asset':neutral.relative_to(PROJECT).as_posix(),'pixelsPerUnit':400,'pivotPixelsTopLeft':[512,548],
                      'emission':{'name':'NeutralEyes','asset':neutral_eyes.relative_to(PROJECT).as_posix(),'pixelsPerUnit':100,'pivotPixelsTopLeft':[128,137]}},
              'emissionMaterial':'Assets/EmergencyShift/Materials/Characters/Scorpion/EyeGlow.mat',
              'states':states,'remains':[],'previewSpeed':2.4}
    native.write(ROOT/'asset-pack.json',manifest)
    a,b=native.pose(0),native.pose(1)
    loop_error=max(float(np.max(abs(np.asarray(a['legs'][i]['joints3D'])-np.asarray(b['legs'][i]['joints3D'])))) for i in a['legs'])
    def boundary_error(first,last):
        values=[]
        for ident in first['legs']:
            values.append(np.max(abs(np.asarray(first['legs'][ident]['joints3D'])-np.asarray(last['legs'][ident]['joints3D']))))
        for ident in first['palps']:
            for key in first['palps'][ident]:
                values.append(np.max(abs(np.asarray(first['palps'][ident][key])-np.asarray(last['palps'][ident][key]))))
        values.append(np.max(abs(np.asarray(first['tail'])-np.asarray(last['tail']))))
        return float(max(values))
    entry_error=boundary_error(native.pose(0,'run'),native.pose(0,'attack'))
    exit_error=boundary_error(native.pose(0,'run'),native.pose(1,'attack'))
    result={'revision':p['revision'],'minimumContacts':min(contacts),'maxBoneLengthError':float(max(errors)),
            'maxPincerAndTailBoneLengthError':float(max(appendage_errors)),
            'jawTipGapRange': [min(jaw_gaps),max(jaw_gaps)],
            'maxPlantedWorldVelocityError':float(max(planted_velocity)), 'gaitLoopPoseError':loop_error,
            'attackEntryPoseError':entry_error,'attackExitToRunPoseError':exit_error,
            'attackGaitCycles':native.ATTACK_CYCLES,'attackSeconds':native.ATTACK_SECONDS,
            'attackReferenceSpeed':native.STRIDE/native.DUTY/native.ATTACK_SECONDS*native.ATTACK_CYCLES*.85,
            'sourceFrames':all_records,'functionalGroups':json.loads((ROOT/'functional-groups.json').read_text())['count'],'artisticApproval':False,
            'scope':'Visual rig and exports only. Group preview is not implemented cooperative enemy AI.',
            'surfaceCorrections':'Local anti-fold barrier; joint pins remain fixed, bone poses unchanged.',
            'authoringReview':'Not applicable; no scene objects changed.'}
    native.write(ROOT/'technical-validation.json',result)
    assert min(contacts)>=4 and max(errors)<1e-10 and max(appendage_errors)<1e-10 and max(planted_velocity)<1e-5 and loop_error<1e-10
    assert entry_error<1e-10 and exit_error<1e-10
    print(json.dumps({k:v for k,v in result.items() if k!='sourceFrames'}),flush=True)


if __name__=='__main__':
    main()
