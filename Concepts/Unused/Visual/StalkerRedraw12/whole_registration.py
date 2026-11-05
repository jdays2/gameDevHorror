"""REJECTED full-silhouette registration experiment; never anatomy acceptance.

Its scale estimate fails on intermediate folded poses and must not be used in
the delivered player. Only corr/gray/render utilities are reused by the fixed
per-sheet PPU player. Native complete-source PNGs are never modified here.
"""
from pathlib import Path
from PIL import Image,ImageDraw
import numpy as np,json,sys
OUT=Path(__file__).resolve().parent

def gray(im):return np.array(im.convert('L'),float)/255
def corr(a,b):
    h,w=a.shape;kh,kw=b.shape;shape=(h+kh-1,w+kw-1)
    fa=np.fft.rfft2(a,shape);fb=np.fft.rfft2(b[::-1,::-1],shape)
    full=np.fft.irfft2(fa*fb,shape)
    return full[kh-1:h,kw-1:w]

def register(image,reference=None):
    source=Image.open(reference or OUT/'anchors/movement/00.png').convert('RGBA')
    aa=np.array(source)[:,:,3]>128;yy,xx=np.nonzero(aa)
    box=(max(0,int(xx.min())-8),max(0,int(yy.min())-8),min(source.width,int(xx.max())+9),min(source.height,int(yy.max())+9))
    first=Image.new('RGBA',source.size,'#e5e6e2');first.alpha_composite(source)
    template=first.convert('RGB').crop(box)
    mask=Image.new('L',template.size,255)
    origin=np.array([288-box[0],287-box[1]],float)
    a=gray(image);h,w=a.shape;best=(-1,None,None,None)
    for scale in np.arange(.25,2.101,.025):
        size=(round(template.width*scale),round(template.height*scale))
        if size[0]>w or size[1]>h:continue
        t=gray(template.resize(size,Image.Resampling.LANCZOS));m=gray(mask.resize(size,Image.Resampling.NEAREST));n=m.sum()
        mean=(t*m).sum()/n;tc=(t-mean)*m;tv=(tc*tc).sum()
        sums=corr(a,m);sums2=corr(a*a,m);num=corr(a,tc)
        den=np.sqrt(np.maximum(sums2-sums*sums/n,1e-10)*tv)
        score=num/den
        yy,xx=np.indices(score.shape)
        cx=xx+origin[0]*scale;cy=yy+origin[1]*scale
        score[(abs(cx-w*.5)>w*.22)|(abs(cy-h*.5)>h*.24)]=-2
        y,x=np.unravel_index(np.argmax(score),score.shape);s=float(score[y,x])
        if s>best[0]:best=(s,float(scale),float(x+origin[0]*scale),float(y+origin[1]*scale))
    return {'score':best[0],'wholeScaleFromSource':best[1],'originNative':[best[2],best[3]],'reference':str(reference or OUT/'anchors/movement/00.png'),'fitScope':'One global whole-sprite similarity only, no limb fit or source/control movement; diagnostic, NOT anatomical PASS.'}

def render(im,fit,world=576):
    # First movement is roughly2.6nativepx per source-unit? Anchor from one
    # immutable whole-reference body; no individual-limb geometric changes.
    factor=1/fit['wholeScaleFromSource']
    w,h=round(im.width*factor),round(im.height*factor)
    ox,oy=fit['originNative'];out=Image.new('RGBA',(world,world))
    out.alpha_composite(im.resize((w,h),Image.Resampling.LANCZOS),(round(world/2-ox*factor),round(287-oy*factor)))
    return out

if __name__=='__main__':
    manifest=json.loads((OUT/'DenseTrials/native-frames.json').read_text())
    selected=sys.argv[1:]
    for state in selected or manifest.keys():
        frames=manifest[state]['frames'];picks=[0,len(frames)//4,len(frames)//2,len(frames)-1] if '--samples' in selected else range(len(frames))
        plate=Image.new('RGB',(576*4,576),'#e5e6e2')
        for n,i in enumerate(picks):
            item=frames[i];path=OUT/item['file'];im=Image.open(path).convert('RGBA')
            matte=Image.new('RGBA',im.size,'#e5e6e2');matte.alpha_composite(im)
            t=i/max(1,len(frames)-1)
            if state=='movement':ref=OUT/f'anchors/movement/{round(i*8/len(frames))%8:02d}.png'
            elif state in ['sleep_entry','awakening']:ref=OUT/f'anchors/sleep_entry/{round((t if state=="sleep_entry" else 1-t)*3):02d}.png'
            else:ref=OUT/f'anchors/attack/{min(5,round(t*5)):02d}.png'
            fit=register(matte.convert('RGB'),ref);item['wholeRegistration']=fit
            rendered=render(im,fit)
            folder=OUT/'DenseTrials/registered'/state;folder.mkdir(parents=True,exist_ok=True)
            rendered.save(folder/f'{i:02d}.png')
            display=Image.new('RGBA',rendered.size,'#e5e6e2');display.alpha_composite(rendered)
            display.convert('RGB').save(folder/f'{i:02d}-matte.png')
            if n<4:plate.paste(display.convert('RGB'),(n*576,0))
        plate.save(OUT/'DenseTrials'/f'{state}-registered-first4.png')
    (OUT/'DenseTrials/registered-frames.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')
    print(json.dumps({s:[f.get('wholeRegistration') for f in manifest[s]['frames'] if f.get('wholeRegistration')] for s in manifest}))
