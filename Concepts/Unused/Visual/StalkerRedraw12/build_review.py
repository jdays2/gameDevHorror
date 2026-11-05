"""Whole-PNG review only. No body-part operation and no new RGB skin art."""
from pathlib import Path
from PIL import Image,ImageDraw
import numpy as np,json,math,hashlib,shutil
from whole_registration import register,render
from register_eye import register_eye
from review_quad import ssim

OUT=Path(__file__).resolve().parent
REVIEW=OUT/'ReviewPlayer';REVIEW.mkdir(exist_ok=True)
PROJECT=OUT.parents[3];EVIDENCE=PROJECT/'Docs/Validation/Task007Redraw12';EVIDENCE.mkdir(parents=True,exist_ok=True)
native=json.loads((OUT/'DenseTrials/native-frames-r2.json').read_text())
source=json.loads((OUT/'plan.json').read_text())
compact=json.loads((PROJECT/'Concepts/Unused/Visual/StalkerWholeGenerated10/compact-source-observation.json').read_text())
strike=json.loads((PROJECT/'Concepts/Unused/Visual/StalkerWholeGenerated10/attack-source-observation.json').read_text())
labels={'movement':'Движение','sleep_entry':'Вход в сон','awakening':'Пробуждение','attack':'Атака'}
colors=['#00bed5','#ddb900','#28b982','#ec8e35','#9a83ed','#dc6eae']
frames={};report={};images={}

def lerp(a,b,t):
    return [{'id':x['id'],**{k:((1-t)*np.array(x[k])+t*np.array(y[k])).tolist() for k in ['H','J','T']}} for x,y in zip(a,b)]

def expected(state,i,n):
    f=i/(n-1)
    if state=='movement':return lerp(source['walkOriginalControls'][0],source['walkOriginalControls'][1],(1-math.cos(2*math.pi*i/n))/2)
    if state=='attack':
        opening=0 if f<.16 else math.sin(math.pi*min(1,(f-.16)/.72))**2 if f<.88 else 0
        return lerp(source['walkOriginalControls'][0],strike['limbs'],opening)
    return lerp(source['walkOriginalControls'][0],compact['limbs'],f if state=='sleep_entry' else 1-f)

def matte(im,bg='#e5e6e2'):
    b=Image.new('RGBA',im.size,bg);b.alpha_composite(im);return b.convert('RGB')

def silhouette_delta(a,b):
    aa=np.array(a.resize((144,144)))[:,:,3]>128;bb=np.array(b.resize((144,144)))[:,:,3]>128
    # Symmetric95th percentile over whole silhouettes, not a skeleton PASS.
    from PIL import ImageFilter
    aa=aa&~(np.array(Image.fromarray(aa.astype('uint8')*255).filter(ImageFilter.MinFilter(3)))>0)
    bb=bb&~(np.array(Image.fromarray(bb.astype('uint8')*255).filter(ImageFilter.MinFilter(3)))>0)
    distances=[]
    for m,n in [(aa,bb),(bb,aa)]:
        dist=np.full(m.shape,44.);dist[n]=0
        for r in range(1,11):
            expanded=np.array(Image.fromarray(n.astype('uint8')*255).filter(ImageFilter.MaxFilter(2*r+1)))>0
            dist[(dist==44)&expanded]=r*4
        distances.extend(dist[m].tolist())
    return float(np.percentile(distances,95))

for state in ['movement','sleep_entry','attack']:
    n=len(native[state]['frames']);data=[];renders=[]
    folder=REVIEW/'frames'/state;folder.mkdir(parents=True,exist_ok=True)
    first=Image.open(OUT/native[state]['frames'][0]['file']).convert('RGBA')
    fixed_scale=register_eye(matte(first))['wholeScaleFromSource']
    for i,item in enumerate(native[state]['frames']):
        im=Image.open(OUT/item['file']).convert('RGBA');f=i/(n-1)
        if state=='movement':ref=OUT/f'anchors/movement/{round(i*8/n)%8:02d}.png'
        elif state=='sleep_entry':ref=OUT/f'anchors/sleep_entry/{round(f*3):02d}.png'
        else:ref=OUT/f'anchors/attack/{min(5,round(f*5)):02d}.png'
        fit=register_eye(matte(im),fixed_scale)
        if state=='sleep_entry':
            # Offline global body origins observed in the complete atlas.
            # Closed eyelids cannot be located by an open-iris template.
            gx=[139,390,635,880,1124][i%5]
            gy=[170,471,772,1076][i//5]
            x0,y0,_,_=item['sourceWholeCreatureRect']
            fit['originNative']=[gx-x0,gy-y0]
            fit['originMeasurement']='Manual whole-body eye/lid center in atlas, approximately2nativepx uncertainty; no anatomical landmark fitted.'
        fit['calibration']='One global PPU for the entire state sheet, calibrated at neutral iris. Never adapt scale to limb silhouettes.'
        canonical=render(im,fit)
        # Display registration is one uniform PPU + pivot on a COMPLETE sprite.
        # Native full-creature PNG remains the authoritative unaltered RGB.
        path=folder/f'{i:02d}.png';canonical.save(path);renders.append(canonical)
        bones=expected(state,i,n)
        data.append({**item,'registration':fit,'reviewFile':path.relative_to(OUT).as_posix(),'bones':bones,'referenceFile':ref.relative_to(OUT).as_posix()})
        if i%8==0:print(f'{state} {i+1}/{n}',flush=True)
    frames[state]=data;images[state]=renders
frames['awakening']=list(reversed(frames['sleep_entry']));images['awakening']=list(reversed(images['sleep_entry']))
for i,item in enumerate(frames['awakening']):
    frames['awakening'][i]={**item,'bones':expected('awakening',i,20),'sharedWholePoseWithSleepEntry':True}

for state,drawings in images.items():
    n=len(drawings);pairs=list(zip(drawings,drawings[1:]))
    if state in ['movement','attack']:pairs.append((drawings[-1],drawings[0]))
    metrics=[]
    for i,(a,b) in enumerate(pairs):
        aa=np.array(matte(a));bb=np.array(matte(b))
        lower_a=aa[307:332,250:330];lower_b=bb[307:332,250:330]
        metrics.append({'from':i,'to':(i+1)%n,'wholeOutlineP95DeltaSourcePx':silhouette_delta(a,b),'lowerTorsoPatchSSIM':ssim(lower_a,lower_b)})
    report[state]={'frameReferences':n,'uniquePngHashes':len(set(x['sha256'] for x in frames[state])),'neighborMetrics':metrics,'maxWholeOutlineP95DeltaSourcePx':max(x['wholeOutlineP95DeltaSourcePx'] for x in metrics),'minLowerTorsoSSIM':min(x['lowerTorsoPatchSSIM'] for x in metrics),'metricLimits':'Outline: approximate symmetric Chebyshev95th percentile on144px diagnostic,4sourcepx quantization, not joint correspondence. SSIM: fixed lower-body rectangle, includes intended fold/attack occlusion; not a stand-alone gate. Byte-unique PNGs do not prove distinct pose/motion.','anatomy':'NOT_ACCEPTED: fixed source controls disagree; no muscle/skin acceptance inferred from these numbers.'}
    light=[matte(im) for im in drawings]
    light[0].save(REVIEW/f'{state}.png',save_all=True,append_images=light[1:],duration=65 if state=='movement' else 75,loop=0 if state in ['movement','attack'] else 1,format='PNG',disposal=0,blend=0)
    cols=6 if state=='movement' else 5 if state in ['sleep_entry','awakening'] else 8
    thumb=192;sheet=Image.new('RGB',(cols*thumb,math.ceil(n/cols)*thumb),'#e5e6e2')
    for i,im in enumerate(light):sheet.paste(im.resize((thumb,thumb),Image.Resampling.LANCZOS),((i%cols)*thumb,(i//cols)*thumb))
    sheet.save(REVIEW/f'{state}-contact.png')
    picks=[0,n//4,n//2,3*n//4,n-1]
    overlay=Image.new('RGB',(576*len(picks),576),'#e5e6e2')
    for j,i in enumerate(picks):
        im=matte(drawings[i]);d=ImageDraw.Draw(im)
        for limb,color in zip(frames[state][i]['bones'],colors):
            d.line([tuple(limb[k]) for k in ['H','J','T']],fill=color,width=2)
            for key in ['H','J','T']:
                x,y=limb[key];d.ellipse((x-4,y-4,x+4,y+4),outline=color,width=1)
        d.text((20,20),f'{state} frame {i}',fill='black');overlay.paste(im,(j*576,0))
    overlay.save(EVIDENCE/f'{state}-fixed-bones.png')
    print(state+' exported',flush=True)

(REVIEW/'frames.json').write_text(json.dumps(frames,indent=2),encoding='utf-8')
(EVIDENCE/'sequence-review.json').write_text(json.dumps({'status':'FAIL / not accepted; task remains incomplete','scope':'36movement,20sleep,20wake(shared reverse20whole drawings),32attack.88unique newAI drawings; no video, part assembly, UV or RGB painting.','registration':'One constant uniform PPU per whole state sheet, first neutral iris calibration; only whole-frame eye/body pivot changes. No later silhouette/limb scale fitting. Earlier full-silhouette registration failed and is excluded. Native RGB and alpha>=3 exact to whole-source rectangle. Not anatomy acceptance.','families':report,'musclePass':'STOPPED: bone/control disagreement is not corrected.','originalGameplay':'27daf5b0 legacy whole-sprite rollback retained; no Assets/Scenes or live Unity changes.'},indent=2,ensure_ascii=False),encoding='utf-8')
from preview_assets import write_player,make_gifs
write_player(frames)
make_gifs(frames)
print('Built all4state whole-PNG player. No acceptance claimed.',flush=True)
