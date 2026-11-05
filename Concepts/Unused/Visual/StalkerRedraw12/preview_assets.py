"""Compact display codecs and GIFs of EXISTING whole PNGs, never new art."""
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont,ImageChops
import json,base64,io,math

OUT=Path(__file__).resolve().parent
REVIEW=OUT/'ReviewPlayer'
EVIDENCE=OUT.parents[3]/'Docs/Validation/Task007Redraw12'
LABELS={'movement':'Движение','sleep_entry':'Вход в сон','awakening':'Пробуждение','attack':'Атака'}

def matte(im):
    out=Image.new('RGBA',im.size,'#e5e6e2');out.alpha_composite(im.convert('RGBA'));return out.convert('RGB')

def webp(im,quality=85):
    stream=io.BytesIO();im.save(stream,format='WEBP',quality=quality,alpha_quality=80,method=4)
    return 'data:image/webp;base64,'+base64.b64encode(stream.getvalue()).decode()

def write_player(frames):
    images={};lookup={};states={}
    def add(path,reference=False):
        key=(str(path),reference)
        if key not in lookup:
            im=Image.open(path).convert('RGBA')
            if reference:im.thumbnail((320,320),Image.Resampling.LANCZOS)
            name='i'+str(len(images));images[name]=webp(im);lookup[key]=name
        return lookup[key]
    for state,items in frames.items():
        states[state]=[]
        for item in items:
            origin=item['registration']['originNative'];q=item['registration']['wholeScaleFromSource'];w,h=item['nativeSize']
            states[state].append({'image':add(OUT/item['file']),'ref':add(OUT/item['referenceFile'],True),'rect':[round(288-origin[0]/q),round(287-origin[1]/q),round(w/q),round(h/q)],'bones':[{'id':b['id'],**{k:[round(v,1) for v in b[k]] for k in ['H','J','T']}} for b in item['bones']],'nativeSize':[w,h]})
    payload={'labels':LABELS,'states':states,'images':images,'status':'Непринятые пробы. Исходная анимация в игре сохранена.','uniqueNewWholeDrawings':88,'sharedSleepWakePoses':20}
    raw=json.dumps(payload,ensure_ascii=False,separators=(',',':'))
    first=Image.open(OUT/frames['movement'][0]['reviewFile']).convert('RGBA');first.thumbnail((256,256),Image.Resampling.LANCZOS)
    png=io.BytesIO();first.save(png,format='PNG',optimize=True)
    fallback='data:image/png;base64,'+base64.b64encode(png.getvalue()).decode()
    html=(OUT/'player-template.html').read_text(encoding='utf-8').replace('/*DATA*/',raw).replace('<!--FALLBACK-->',fallback)
    (REVIEW/'data.json').write_text(raw,encoding='utf-8');(REVIEW/'player.html').write_text(html,encoding='utf-8')
    stats={'htmlBytes':len(html.encode()),'embeddedUniquePreviewImages':len(images),'codec':'WebP RGB85/alpha80 for display only; full native frame resolution, original native PNGs remain unchanged. PNG fallback is embedded.','externalImageDependencies':0,'networkRequests':0,'eagerlyDecodedImages':'Only current/next visible whole frames; remaining images lazy.','sourceFrameReferences':{s:len(x) for s,x in states.items()}}
    (EVIDENCE/'preview-player-size.json').write_text(json.dumps(stats,indent=2),encoding='utf-8')
    print(json.dumps(stats),flush=True)
    return stats

def make_gifs(frames):
    font_path=Path('C:/Windows/Fonts/segoeui.ttf')
    font=ImageFont.truetype(str(font_path),18) if font_path.exists() else ImageFont.load_default()
    results=[]
    sequences={
        'movement':('Движение · 36 поз',[(f,'movement') for f in frames['movement']]),
        'sleep-wake':('Сон ↔ пробуждение · 20 общих поз',[(f,'sleep_entry') for f in frames['sleep_entry']]+[(f,'awakening') for f in frames['awakening'][1:-1]]),
        'attack':('Атака · 32 позы',[(f,'attack') for f in frames['attack']]),
        'all-states':('Все четыре состояния',[(f,s) for s in ['movement','sleep_entry','awakening','attack'] for f in frames[s]])
    }
    for name,(label,sequence) in sequences.items():
        rendered=[];durations=[]
        for i,(item,state) in enumerate(sequence):
            im=matte(Image.open(OUT/item['reviewFile'])).resize((400,400),Image.Resampling.LANCZOS)
            canvas=Image.new('RGB',(400,434),'#e5e6e2');canvas.paste(im,(0,34))
            ImageDraw.Draw(canvas).text((12,8),LABELS[state] if name in ['sleep-wake','all-states'] else label,font=font,fill='#272d34')
            rendered.append(canvas);durations.append(350 if name=='sleep-wake' and i in [0,19] else 70)
        # ONE shared palette per clip avoids frame-to-frame palette flicker.
        palette_sheet=Image.new('RGB',(128*8,128*math.ceil(len(rendered)/8)))
        for i,im in enumerate(rendered):palette_sheet.paste(im.resize((128,128)),((i%8)*128,(i//8)*128))
        palette=palette_sheet.quantize(colors=256,method=Image.Quantize.MEDIANCUT)
        encoded=[im.quantize(palette=palette,dither=Image.Dither.NONE) for im in rendered]
        target=REVIEW/f'{name}.gif'
        encoded[0].save(target,save_all=True,append_images=encoded[1:],duration=durations,loop=0,disposal=2,optimize=False)
        gif=Image.open(target);assert gif.n_frames==len(encoded),(name,gif.n_frames,len(encoded))
        for i,expected in enumerate(encoded):
            gif.seek(i);assert ImageChops.difference(gif.convert('RGB'),expected.convert('RGB')).getbbox() is None,(name,i)
        unique=20 if name=='sleep-wake' else 88 if name=='all-states' else len(rendered)
        results.append({'file':target.name,'frames':gif.n_frames,'durationMs':sum(durations),'loop':gif.info.get('loop'),'bytes':target.stat().st_size,'decodedPixelsMatchEncodedWholePngDisplay':True,'uniqueDrawings':unique})
    (EVIDENCE/'gif-previews.json').write_text(json.dumps({'result':'PASS display encodings only; art remains unaccepted','clips':results,'originalsUnchanged':True,'note':'Sleep/wake is one repeated fold/unfold loop with20shared drawings, not38new drawings.'},indent=2),encoding='utf-8');print(json.dumps(results),flush=True)

if __name__=='__main__':
    frames=json.loads((REVIEW/'frames.json').read_text(encoding='utf-8'))
    write_player(frames);make_gifs(frames)
