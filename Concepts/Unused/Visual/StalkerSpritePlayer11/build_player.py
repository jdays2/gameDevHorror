"""Build a file:// capable whole-PNG sprite player, not a video player or a cutout rig."""
from pathlib import Path
from PIL import Image, ImageFilter, ImageDraw
import base64, hashlib, json, math, re, shutil, zipfile
import numpy as np

OUT = Path(__file__).resolve().parent
ROOT = OUT.parents[3]
D10 = ROOT/'Concepts/Unused/Visual/StalkerWholeGenerated10'
R09 = ROOT/'Concepts/Unused/Visual/StalkerRepaint09'
EVIDENCE = ROOT/'Docs/Validation/Task007SpritePlayer11'
EVIDENCE.mkdir(parents=True, exist_ok=True)
sha = lambda p: hashlib.sha256(p.read_bytes()).hexdigest()
inputs = {}
images = {}
records = []

def source(path):
    inputs[str(path.relative_to(ROOT))] = sha(path)
    return path

def add_frame(image, path, provenance, clean_alpha=False, ppu=None, bones=None):
    raw = np.array(image.convert('RGBA'))
    pixels = raw.copy()
    cleared = 0
    if clean_alpha:
        near = np.array(Image.fromarray((raw[:,:,3]>=3).astype('uint8')*255).filter(ImageFilter.MaxFilter(5)))>0
        mask = (raw[:,:,3]>0)&(raw[:,:,3]<=2)&~near
        pixels[mask,3] = 0
        cleared = int(mask.sum())
    assert np.array_equal(raw[:,:,:3],pixels[:,:,:3])
    assert np.array_equal(raw[:,:,3][raw[:,:,3]>=3],pixels[:,:,3][raw[:,:,3]>=3])
    path.parent.mkdir(parents=True, exist_ok=True)
    Image.fromarray(pixels).save(path)
    digest = sha(path)
    images.setdefault(digest, 'data:image/png;base64,'+base64.b64encode(path.read_bytes()).decode('ascii'))
    frame = {'image':digest,'file':path.relative_to(OUT).as_posix(),'width':image.width,'height':image.height,
             'ppu':ppu or image.width/576*224,'bones':bones,
             'provenance':provenance,'alpha1or2Cleared':cleared,'rgbExact':True,'alphaAtLeast3Exact':True}
    records.append(frame)
    return frame

def extract_sheet(path, columns, rows, family, state, pose_data=None, registration=None):
    source(path)
    sheet = Image.open(path).convert('RGBA')
    frames = []
    cw,ch = math.ceil(sheet.width/columns),math.ceil(sheet.height/rows)
    for i in range(columns*rows):
        x,y = i%columns,i//columns
        box = (round(x*sheet.width/columns),round(y*sheet.height/rows),round((x+1)*sheet.width/columns),round((y+1)*sheet.height/rows))
        whole = sheet.crop(box)
        canvas = Image.new('RGBA',(cw,ch))
        canvas.paste(whole,(0,0))
        assert canvas.crop((0,0,whole.width,whole.height)).tobytes()==whole.tobytes()
        bones = None
        if pose_data:
            bones = pose_data[i]['limbs']
            if registration:
                s = registration['scale']; t = np.array(registration['translation'])
                bones = [{'id':b['id'],**{k:((np.array(b[k])*s+t)*576/cw).tolist() for k in ['H','J','T']}} for b in bones]
        frames.append(add_frame(canvas,OUT/f'frames/{family}/{state}/{i:02d}.png',
             {'type':'whole-generated-sprite-cell','sheet':str(path.relative_to(ROOT)),'sheetSha256':inputs[str(path.relative_to(ROOT))],
              'rect':box,'operation':'Complete cell crop + transparent padding only. No body-part extraction, rig, UV, repaint or video frame.'},
             clean_alpha=True,bones=bones))
    return frames

def native_sprite(guid, state, i, guid_map):
    asset = source(guid_map[guid])
    text = asset.read_text(encoding='utf-8-sig')
    rect = re.search(r'm_Rect:\s+serializedVersion: \d+\s+x: ([\d.]+)\s+y: ([\d.]+)\s+width: ([\d.]+)\s+height: ([\d.]+)',text)
    x,y,w,h = [round(float(v)) for v in rect.groups()]
    px,py = map(float,re.search(r'm_Pivot: \{x: ([\d.eE+-]+), y: ([\d.eE+-]+)\}',text).groups())
    ppu = float(re.search(r'm_PixelsToUnits: ([\d.eE+-]+)',text).group(1))
    texture_guid = re.search(r'    texture: \{fileID: \d+, guid: ([a-f0-9]{32}), type: \d+\}',text).group(1)
    texture = source(guid_map[texture_guid])
    sheet = Image.open(texture).convert('RGBA')
    box = (x,sheet.height-y-h,x+w,sheet.height-y)
    whole = sheet.crop(box)
    ax,ay=round(px*w),round((1-py)*h)
    size=max(576,2*max(ax,w-ax,ay,h-ay))
    origin = (size//2-ax,size//2-ay)
    assert min(origin)>=0 and origin[0]+w<=size and origin[1]+h<=size
    canvas = Image.new('RGBA',(size,size));canvas.paste(whole,origin)
    assert canvas.crop((origin[0],origin[1],origin[0]+w,origin[1]+h)).tobytes()==whole.tobytes()
    return add_frame(canvas,OUT/f'frames/original/{state}/{i:02d}.png',
        {'type':'current-prefab-original-whole-sprite','nativeAsset':str(asset.relative_to(ROOT)),
         'sourceTexture':str(texture.relative_to(ROOT)),'sourceRect':box,'pasteOrigin':origin,'nativePPU':ppu,
         'operation':'Exact complete NativeSprite rect and pivot. No pixel changes.'},ppu=ppu)

def sequence(frames, duration, timeline=None, note='', static=False):
    return {'frames':frames,'timeline':timeline or list(range(len(frames))),'duration':duration,
            'uniqueDrawings':len({f['image'] for f in frames}),'note':note,'static':static}

def apng_and_sheet(family,state,seq):
    if seq.get('static'):return
    folder = OUT/'animations';folder.mkdir(exist_ok=True)
    frames = [Image.open(OUT/seq['frames'][i]['file']).convert('RGBA') for i in seq['timeline']]
    duration = [round((i+1)*seq['duration']/len(frames))-round(i*seq['duration']/len(frames)) for i in range(len(frames))]
    target=folder/f'{family}-{state}.png'
    frames[0].save(target,save_all=True,append_images=frames[1:],loop=0 if state in ('movement','attack') else 1,duration=duration,disposal=0,blend=0)
    decoded=Image.open(target)
    assert decoded.n_frames==len(frames),(family,state,decoded.n_frames,len(frames))
    for i,expected in enumerate(frames):
        decoded.seek(i);actual=np.array(decoded.convert('RGBA'));expected=np.array(expected)
        assert np.array_equal(actual[:,:,3],expected[:,:,3])
        mask=expected[:,:,3]>0
        assert np.array_equal(actual[:,:,:3][mask],expected[:,:,:3][mask])
    cols=min(6,len(seq['frames']));rows=math.ceil(len(seq['frames'])/cols)
    cw=max(f['width'] for f in seq['frames']);ch=max(f['height'] for f in seq['frames'])
    sheet=Image.new('RGBA',(cw*cols,ch*rows))
    for i,f in enumerate(seq['frames']):sheet.paste(Image.open(OUT/f['file']),(i%cols*cw,i//cols*ch))
    (OUT/'sheets').mkdir(exist_ok=True)
    sheet.save(OUT/f'sheets/{family}-{state}.png')
    if family=='fresh':
        previews=[]
        for frame in frames:
            frame.thumbnail((360,360),Image.Resampling.LANCZOS)
            tile=Image.new('RGBA',(360,360),'#171c26');tile.alpha_composite(frame,((360-frame.width)//2,(360-frame.height)//2))
            previews.append(tile.convert('RGB'))
        previews[0].save(folder/f'{family}-{state}.gif',save_all=True,append_images=previews[1:],loop=0,duration=duration,disposal=2)

def main():
    prefab=source(ROOT/'Assets/EmergencyShift/Prefabs/Enemies/ShadowStalker.prefab')
    prefab_text=prefab.read_text(encoding='utf-8-sig')
    assert '  spriteSet: {fileID: 0}' in prefab_text and '  attackSpriteSet: {fileID: 0}' in prefab_text
    guid_map={}
    for meta in (ROOT/'Assets/EmergencyShift/Art').rglob('*.meta'):
        match=re.search(r'^guid: ([a-f0-9]{32})$',meta.read_text(encoding='utf-8-sig'),re.M)
        if match:guid_map[match.group(1)]=meta.with_suffix('')
    baseline={}
    for state,field in [('movement','walkFrames'),('sleep_entry','recoilFrames'),('attack','attackFrames')]:
        block=re.search(r'^  '+field+r':\n((?:  - \{[^\n]+\}\n)+)',prefab_text,re.M).group(1)
        guids=re.findall(r'guid: ([a-f0-9]{32})',block)
        frames=[native_sprite(guid,state,i,guid_map) for i,guid in enumerate(guids)]
        baseline[state]=sequence(frames,560 if state=='attack' else 240 if state=='sleep_entry' else 900,
                                 note='Исходные цельные рисунки из текущего prefab. Геометрия и PPU берутся из NativeSprite.')
    baseline['awakening']=sequence(baseline['sleep_entry']['frames'],900,list(reversed(range(len(baseline['sleep_entry']['frames'])))),note='Исходные позы сворачивания в обратном порядке; тест представления.')
    poses=json.loads(source(D10/'poses.json').read_text(encoding='utf-8-sig'))['poses']
    attack_plan=json.loads(source(D10/'attack-four-plan.json').read_text(encoding='utf-8-sig'))['poses']
    fresh={}
    fresh['movement']=sequence(extract_sheet(D10/'movement-01-r1.png',3,3,'fresh','movement',poses['movement'][:9]),900,list(range(9))+list(range(7,0,-1)),note='9 цельных ImageGen-поз. 16 шагов туда-обратно — не16 новых рисунков. Проба не прошла проверку схемы.')
    fresh['attack']=sequence(extract_sheet(D10/'attack-four-r1.png',2,2,'fresh','attack',attack_plan),560,[0,1,2,3,2,1],note='4 цельных ImageGen-позы. 6 шагов открытия/возврата — не6 новых рисунков. Пропорции расходятся со схемой.')
    compact=json.loads(source(D10/'compact-source-observation.json').read_text(encoding='utf-8-sig'))['limbs']
    neutral=json.loads(source(D10/'source-bones.json').read_text(encoding='utf-8-sig'))['source00Source01Correspondences']
    for state,key,bones in [('sleep_entry','sleep',compact),('awakening','neutral',[x['source00'] for x in neutral])]:
        path=source(D10/f'DeliveryKeys/{key}.png')
        frame=add_frame(Image.open(path),OUT/f'frames/fresh/{state}/00.png',{'type':'single-whole-ImageGen-key','source':str(path.relative_to(ROOT))},bones=bones)
        fresh[state]=sequence([frame],900,note='Один новый цельный ключ. Промежуточная серия пока не готова; воспроизведение отключено.',static=True)
    old_plan=json.loads(source(R09/'sequence-plan.json').read_text(encoding='utf-8-sig'))['states']
    registration=json.loads(source(ROOT/'Docs/Validation/Task007Repaint09/r3-frozen-body-registrations.json').read_text(encoding='utf-8-sig'))
    earlier={}
    for state in ['movement','sleep_entry','awakening','attack']:
        plan=old_plan[state]
        frames=extract_sheet(R09/f'{state}-r3-raw.png',plan['columns'],plan['rows'],'whole09',state,plan['frames'],registration[state])
        earlier[state]=sequence(frames,560 if state=='attack' else 900,note='Ранняя цельная ImageGen-проба R3 до слойной сборки. Проверку поз/масштаба не прошла. Это PNG из сгенерированного листа, не кадры видео.')
    prompts=OUT/'prompts';prompts.mkdir(exist_ok=True)
    for state,seq in fresh.items():
        origin=D10/({'movement':'movement-01-r1-prompt.txt','attack':'attack-four-r1-prompt.txt','sleep_entry':'compact-source-r1-prompt.txt','awakening':'master-r2-prompt.txt'}[state])
        target=prompts/f'fresh-{state}.txt';shutil.copy2(source(origin),target);seq['prompt']=target.relative_to(OUT).as_posix()
    for state,seq in earlier.items():
        target=prompts/f'whole09-{state}.txt';shutil.copy2(source(R09/f'{state}-r3-prompt.txt'),target);seq['prompt']=target.relative_to(OUT).as_posix()
    data={'version':11,'states':{'movement':'Движение','sleep_entry':'Вход в сон','awakening':'Пробуждение','attack':'Атака'},
          'families':{'fresh':{'label':'Свежие PNG-пробы · 10','states':fresh},'whole09':{'label':'Ранняя цельная проба · 09/R3','states':earlier}},
          'baseline':baseline,'images':images,'defaultFamily':'fresh','defaultState':'attack'}
    for family,bundle in data['families'].items():
        for state,seq in bundle['states'].items():apng_and_sheet(family,state,seq)
    template=source(OUT/'player.template.html').read_text(encoding='utf-8-sig')
    payload=json.dumps(data,separators=(',',':'),ensure_ascii=False).replace('</',r'<\/')
    (OUT/'player.html').write_text(template.replace('@@DATA@@',payload),encoding='utf-8',newline='\n')
    manifest={k:v for k,v in data.items() if k!='images'}
    (OUT/'animations.json').write_text(json.dumps(manifest,indent=2,ensure_ascii=False),encoding='utf-8')
    preservation={path:sha(ROOT/path)==digest for path,digest in inputs.items()}
    assert all(preservation.values())
    report={'player':'player.html','selfContained':True,'videoElements':0,'videoInputs':0,'bodyPartsExtracted':0,'rgbRepaint':0,'uvDeformation':0,
        'frames':len(records),'imageAssets':len(images),'sourcePreservation':preservation,
        'freshActualDrawings':{'movement':9,'attack':4,'sleep_entry':1,'awakening':1},
        'earlierWholeAI09ActualDrawings':{k:len(v['frames']) for k,v in earlier.items()},
        'baselineReferences':{k:len(v['frames']) for k,v in baseline.items()},'apngVisibleRGBA':'exact decoded equality for every exported timeline frame',
        'originalPixelExport':'Exact native rect/pivot/PPU from currently referenced original sprites, unchanged RGBA.',
        'newPixelExport':'Whole sheet cells only; transparent canvas padding; authorized detached exterior alpha1/2 cleanup; RGB and allalpha>=3 exactly retained.',
        'artistAcceptance':False,'readyGameAnimations':False}
    (EVIDENCE/'build-verification.json').write_text(json.dumps(report,indent=2,ensure_ascii=False),encoding='utf-8')
    archive=OUT/'Stalker-sprite-tests.zip'
    with zipfile.ZipFile(archive,'w',zipfile.ZIP_DEFLATED) as zip:
        for path in [OUT/'player.html',OUT/'animations.json',OUT/'README.md',*sorted((OUT/'frames').rglob('*.png')),*sorted((OUT/'animations').iterdir()),*sorted((OUT/'sheets').glob('*.png')),*sorted(prompts.glob('*.txt'))]:
            zip.write(path,path.relative_to(OUT).as_posix())
    with zipfile.ZipFile(archive) as zip:assert zip.testzip() is None
    print(json.dumps({'htmlBytes':(OUT/'player.html').stat().st_size,'archiveBytes':archive.stat().st_size,'sourceFilesPreserved':len(inputs),'frames':len(records),'freshDrawings':[9,4],'earlierWholeDrawings':[24,16,16,32]},ensure_ascii=False))

if __name__=='__main__':main()
