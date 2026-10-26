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
    for t in np.linspace(0,1,1000,endpoint=False):
        pose=native.pose(float(t))
        following=native.pose(float(t)+eps)
        contacts.append(sum(l['contact'] for l in pose['legs'].values()))
        for ident,limb in pose['legs'].items():
            actual=np.linalg.norm(np.diff(limb['joints3D'],axis=0),axis=1)
            expected=np.linalg.norm(np.diff(p['walkingLegs'][ident],axis=0),axis=1)
            errors.extend(abs(actual-expected))
            if limb['contact'] and following['legs'][ident]['contact']:
                a=np.asarray(limb['joints3D'][-1])
                b=np.asarray(following['legs'][ident]['joints3D'][-1])
                velocity=(b-a)/(eps*native.NATIVE_SECONDS)
                velocity[1]+=native.STRIDE/native.DUTY/native.NATIVE_SECONDS
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
        for index,path in enumerate(paths):
            im=Image.open(path).convert('RGBA')
            alpha=np.asarray(im.getchannel('A'))
            edge=int(max(alpha[0].max(),alpha[-1].max(),alpha[:,0].max(),alpha[:,-1].max()))
            assert edge==0,(path,edge)
            atlas.alpha_composite(im.resize((512,512),Image.Resampling.LANCZOS),(index%6*512,index//6*512))
            metrics=json.loads((ROOT/'measurements'/kind/f'{index:02d}.json').read_text())
            assert metrics['flippedTriangles']==0
            all_records.append({'file':path.relative_to(ROOT).as_posix(),'sha256':hashlib.sha256(path.read_bytes()).hexdigest(),
                                'edgeAlpha':edge,'flippedTriangles':metrics['flippedTriangles'],
                                'maximumSurfaceCorrectionPx':metrics['maximumSurfaceCorrectionPx'],'jointPinErrorPx':metrics['jointPinErrorPx']})
        target=ART/(name+'.png')
        atlas.save(target)
        states.append({'name':name,'asset':target.relative_to(PROJECT).as_posix(),'columns':6,'cell':512,
                       'count':count,'durationsMs':export['durationsMs'],'loop':export['loop'],
                       'referenceSpeed':export['referenceSpeed']*.85,'pixelsPerUnit':200,'pivotPixelsTopLeft':[256,274]})
        sheet(paths,kind+'-clean-dark.jpg','#282e33')
        sheet(paths,kind+'-clean-light.jpg','#e5e5df')
        sheet([path.with_name(path.stem+'-bones.jpg') for path in paths],kind+'-all-bones.jpg','#282e33')
    neutral=ART/'Neutral.png'
    neutral.write_bytes((ROOT/'native-neutral.png').read_bytes())
    manifest={'task':'TASK-005','revision':p['revision'],'role':'fast group attacker, visual prototype',
              'sourceForward':'up','audio':False,'brain':False,'damageEvents':False,
              'idle':{'name':'Idle','asset':neutral.relative_to(PROJECT).as_posix(),'pixelsPerUnit':400,'pivotPixelsTopLeft':[512,548]},
              'states':states,'remains':[],'previewSpeed':2.4}
    native.write(ROOT/'asset-pack.json',manifest)
    a,b=native.pose(0),native.pose(1)
    loop_error=max(float(np.max(abs(np.asarray(a['legs'][i]['joints3D'])-np.asarray(b['legs'][i]['joints3D'])))) for i in a['legs'])
    attack_start=Image.open(ROOT/'frames/attack/attack-00.png').convert('RGBA')
    attack_end=Image.open(ROOT/'frames/attack/attack-15.png').convert('RGBA')
    result={'revision':p['revision'],'minimumContacts':min(contacts),'maxBoneLengthError':float(max(errors)),
            'maxPincerAndTailBoneLengthError':float(max(appendage_errors)),
            'jawTipGapRange': [min(jaw_gaps),max(jaw_gaps)],
            'maxPlantedWorldVelocityError':float(max(planted_velocity)), 'gaitLoopPoseError':loop_error,
            'attackFirstLastPixelsIdentical':np.array_equal(np.asarray(attack_start),np.asarray(attack_end)),
            'sourceFrames':all_records,'functionalGroups':39,'artisticApproval':False,
            'scope':'Visual rig and exports only. Group preview is not implemented cooperative enemy AI.',
            'surfaceCorrections':'Local anti-fold barrier; joint pins remain fixed, bone poses unchanged.',
            'authoringReview':'Not applicable; no scene objects changed.'}
    native.write(ROOT/'technical-validation.json',result)
    assert min(contacts)>=4 and max(errors)<1e-10 and max(appendage_errors)<1e-10 and max(planted_velocity)<1e-5 and loop_error<1e-10
    assert result['attackFirstLastPixelsIdentical']
    print(json.dumps({k:v for k,v in result.items() if k!='sourceFrames'}),flush=True)


if __name__=='__main__':
    main()
