"""Separate gates for the completed silent visual set."""
from pathlib import Path
import hashlib
import importlib.util
import json
import sys
import numpy as np
from PIL import Image, ImageDraw
import build_pack as pack

ROOT=Path(__file__).resolve().parent
sys.path.insert(0,str(ROOT.parent/'FastGroupEnemy21BodyMotion'))
import verify_body as inherited
checks=inherited.checks
rig=pack.rig
checks.rig=rig
REVIEW=ROOT/'review'
REVIEW.mkdir(exist_ok=True)


def inputs():
    poses=json.loads((ROOT/'walk-plan.json').read_text())['poses']
    return [Image.open(ROOT/'walk'/f'walk-{i:02d}.png').convert('RGBA') for i in range(24)],poses


def live(mode):
    frames,poses=inputs()
    if mode=='bones':
        images=[rig.bone_frame(p,im) for im,p in zip(frames,poses)]
    else:
        images=[checks.muscle_overlay(im,p,mode=='muscles') for im,p in zip(frames,poses)]
    rig.contact_sheet(images,REVIEW/('walk-'+mode+'.jpg'))
    for index in [0,6,12,18]:
        images[index].convert('RGB').save(REVIEW/f'walk-{mode}-{index:02d}.jpg',quality=93)


def hit(mode):
    poses=json.loads((ROOT/'hit-plan.json').read_text())['poses']
    frames=[Image.open(ROOT/'hit'/f'hit-{i:02d}.png').convert('RGBA') for i in range(6)]
    if mode=='bones':
        images=[rig.bone_frame(p,im) for im,p in zip(frames,poses)]
    else:
        images=[checks.muscle_overlay(im,p,mode=='muscles') for im,p in zip(frames,poses)]
    rig.contact_sheet(images,REVIEW/('hit-'+mode+'.jpg'))


def remains(mode):
    path=ROOT.parent/'FastGroupEnemy18Remains/qa/independent_review.py'
    spec=importlib.util.spec_from_file_location('claw_remains_checks',path)
    qa=importlib.util.module_from_spec(spec)
    spec.loader.exec_module(qa)
    qa.ROOT=ROOT/'remains'
    qa.QA=REVIEW
    qa.MASTER=ROOT.parent/'FastGroupEnemy19SpiderEyes/J-closed-mandibles.png'
    record,chain=qa.feasibility(qa.OBS['B3'])
    names=[('A2','carcass-j.png'),('B3','torn-j.png'),('C2','heap-j.png')]
    images=[]
    for variant,name in names:
        if mode=='bones':
            image=qa.canvas(name)
            if variant=='A2':
                qa.draw_bones(image)
            elif variant=='B3':
                qa.draw_bones(image,chains={'FU':chain})
            else:
                draw=ImageDraw.Draw(image)
                for ident in ['FU','FL']:
                    points=qa.project(qa.REST[ident])
                    draw.line([tuple(p) for p in points[1:]],fill=qa.CYAN,width=3)
                    qa.draw_digits(draw,ident)
            qa.footer(image,variant+' / canonical J head / unchanged source anatomy',[
                'Head is a local design update. Four limbs; front3+3, rear3+1/3+1.',
                'B uses the earlier fixed-length feasibility witness, not measured heights.',
                'C accounts for visible broken parts; hidden complete chains are not inferred.'])
            if variant=='C2':
                image.save(REVIEW/'independent-02-C2-parts.png')
        else:
            image=qa.muscle_pass(name,variant,chain,mode=='volumes')
        image.convert('RGB').save(REVIEW/f'remains-{variant}-{mode}.jpg',quality=93)
        images.append(image)
    rig.contact_sheet(images,REVIEW/f'remains-{mode}.jpg')
    return record


def finish():
    frames,poses=inputs()
    for color,name in [('#e4e4df','light'),('#262b31','dark')]:
        rig.contact_sheet([checks.background(im,color) for im in frames],REVIEW/f'walk-clean-{name}.jpg')
    records=[]
    for source in sorted((ROOT/'walk').glob('*.png'))+sorted((ROOT/'hit').glob('*.png'))+sorted((ROOT/'remains').glob('*.png')):
        im=Image.open(source)
        alpha=np.asarray(im.getchannel('A'))
        records.append({'path':source.relative_to(ROOT).as_posix(),'mode':im.mode,'size':im.size,
                        'edgeAlphaMax':int(max(alpha[0].max(),alpha[-1].max(),alpha[:,0].max(),alpha[:,-1].max())),
                        'sha256':hashlib.sha256(source.read_bytes()).hexdigest()})
    geometry=[json.loads((ROOT/'measurements'/f'{i:02d}.json').read_text()) for i in range(24)]
    minimum=min(sum(l['state']=='stance' for l in p['limbs'].values()) for p in poses)
    result={'revision':'claw-visual-r1','walkFrames':24,'minimumWalkContacts':minimum,
            'flippedWalkTriangles':sum(s['flippedTriangles'] for s in geometry),
            'maximumBoneLengthError':json.loads((ROOT/'walk-plan.json').read_text())['maxBoneLengthError'],
            'sourceFrames':records,'sourceLoopError':max(float(np.max(abs(np.asarray(pack.walk(0)['limbs'][i]['joints3D'])-np.asarray(pack.walk(1)['limbs'][i]['joints3D'])))) for i in rig.REST),
            'audioSourcesExpected':0,'gameplayBrainExpected':False,'ownerArtisticApproval':False,
            'remainsInventory':{'heads':1,'frontHands':2,'frontDigitsPerHand':3,'rearFeet':2,'rearDigitsPerFoot':'3+1'},
            'limits':['Kinematic visual assets; no new gameplay behaviour or damage events.',
                      'Remains B preserves earlier feasibility limits; C does not claim intact hidden musculature.',
                      'Animator state selection does not blend arbitrary sprite poses.']}
    hit_stats=[json.loads((ROOT/'measurements/hit'/f'{i:02d}.json').read_text()) for i in range(6)]
    result['hitFrames']=6
    result['hitDurationMs']=sum(json.loads((ROOT/'hit-plan.json').read_text())['durationsMs'])
    result['flippedHitTriangles']=sum(s['flippedTriangles'] for s in hit_stats)
    result['hitReturnsToNeutral']=np.array_equal(np.asarray(Image.open(ROOT/'hit/hit-00.png')),np.asarray(Image.open(ROOT/'hit/hit-05.png')))
    assert result['flippedHitTriangles']==0 and result['hitReturnsToNeutral']
    pack.write(ROOT/'technical-validation.json',result)
    assert result['flippedWalkTriangles']==0 and minimum>=3
    assert all(p['mode']=='RGBA' and p['edgeAlphaMax']<=1 for p in records)
    production=[]
    for item in json.loads((ROOT/'asset-pack.json').read_text())['remains']:
        image=Image.open(pack.PROJECT/item['asset']).convert('RGBA')
        original=Image.open(pack.PROJECT/item['source']).convert('RGBA')
        assert np.array_equal(np.asarray(image.crop((16,16,16+original.width,16+original.height))),np.asarray(original))
        a=np.asarray(image.getchannel('A'))
        edge=int(max(a[0].max(),a[-1].max(),a[:,0].max(),a[:,-1].max()))
        assert edge==0
        production.append({'asset':item['asset'],'paddingPixels':16,'sourcePixelsPreservedExactly':True,'outerEdgeAlpha':edge})
    result['productionRemains']=production
    result['sourceAlphaNote']='Two raw generated remains have alpha=1/255 specks on the canvas edge. Production adds 16 transparent pixels on all sides and shifts the pivot equally; source RGBA is preserved exactly.'
    pack.write(ROOT/'technical-validation.json',result)
    print(json.dumps({k:result[k] for k in ['walkFrames','minimumWalkContacts','flippedWalkTriangles','maximumBoneLengthError','sourceLoopError']}),flush=True)


if __name__=='__main__':
    mode=sys.argv[1] if len(sys.argv)>1 else 'finish'
    if mode.startswith('hit-'):
        hit(mode[4:])
    elif mode in ['bones','volumes','muscles']:
        live(mode)
        remains(mode)
    else:
        finish()
