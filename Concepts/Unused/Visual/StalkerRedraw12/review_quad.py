"""Read full-cell sprite drawings, align entire frame to fixed eye pivot, inspect.

No limb surgery/UV/RGB painting. Only allowed detached alpha1/2 cleanup.
"""
from pathlib import Path
from PIL import Image,ImageFilter,ImageDraw
import numpy as np,json,sys
OUT=Path(__file__).resolve().parent

def center(im):
    ar=np.array(im);mask=(ar[:,:,:3].max(2)<55)&(ar[:,:,3]>160)
    yy,xx=np.indices(mask.shape);mask &= (xx>220)&(xx<350)&(yy>230)&(yy<345)
    seen=set();cc=[]
    for y,x in zip(*np.nonzero(mask)):
        if (y,x) in seen:continue
        q=[(y,x)];seen.add((y,x));component=[]
        while q:
            py,px=q.pop();component.append((px,py))
            for dy,dx in [(1,0),(-1,0),(0,1),(0,-1)]:
                ny,nx=py+dy,px+dx
                if 0<=ny<576 and 0<=nx<576 and mask[ny,nx] and (ny,nx) not in seen:seen.add((ny,nx));q.append((ny,nx))
        cc.append(component)
    c=max(cc,key=len);return np.mean(c,axis=0).tolist()

def ssim(a,b):
    a=a.astype(float)/255;b=b.astype(float)/255
    ma=a.mean((0,1));mb=b.mean((0,1));va=a.var((0,1));vb=b.var((0,1));cov=((a-ma)*(b-mb)).mean((0,1))
    return float(np.mean((2*ma*mb+.01**2)*(2*cov+.03**2)/((ma*ma+mb*mb+.01**2)*(va+vb+.03**2))))

def whole_scale(im,s):
    size=round(576*s);scaled=im.resize((size,size),Image.Resampling.LANCZOS)
    out=Image.new('RGBA',(576,576));out.alpha_composite(scaled,(round(288-288*s),round(287-287*s)));return out

def extract(file,tag):
    sheet=Image.open(file).convert('RGBA');w,h=sheet.size
    images=[];records=[]
    folder=OUT/'Review'/tag;folder.mkdir(parents=True,exist_ok=True)
    for i in range(4):
        x=(i%2)*w//2;y=(i//2)*h//2
        im=sheet.crop((x,y,x+w//2,y+h//2)).resize((576,576),Image.Resampling.LANCZOS)
        ar=np.array(im);a=ar[:,:,3];near=np.array(Image.fromarray((a>2).astype('uint8')*255).filter(ImageFilter.MaxFilter(17)))
        removed=(a>0)&(a<=2)&(near==0);ar[removed,3]=0;im=Image.fromarray(ar)
        c=center(im);dx,dy=288-c[0],287-c[1]
        # Entire-frame pivot registration, equivalent to SpriteRenderer origin.
        canvas=Image.new('RGBA',(576,576));canvas.alpha_composite(im,(round(dx),round(dy)));images.append(canvas)
        canvas.save(folder/f'{i:02d}.png')
        matte=Image.new('RGBA',canvas.size,'#e5e6e2');matte.alpha_composite(canvas);matte.convert('RGB').save(folder/f'{i:02d}-matte.png')
        records.append({'cell':i,'pupilBeforeRegistration':c,'wholeFrameTranslation':[round(dx),round(dy)],'detachedAlpha1or2Removed':int(removed.sum())})
    for i in range(1,4):
        a=np.array(images[i-1])[240:335,245:332,:3];b=np.array(images[i])[240:335,245:332,:3]
        records[i]['bodyPatchSSIMFromPrevious']=ssim(a,b)
        records[i]['bodyPatchMAEFromPrevious']=float(np.abs(a.astype(float)-b).mean()/255)
    # Optional diagnostic ONLY: one global SpriteRenderer-like uniform scale.
    # Never modifies a body part, never replaces the original controls/gates.
    template=np.array(images[0])[240:335,245:332,:3]
    for i in range(1,4):
        candidates=[]
        for scale in np.linspace(.90,1.10,41):
            adjusted=whole_scale(images[i],float(scale));patch=np.array(adjusted)[240:335,245:332,:3]
            candidates.append((ssim(template,patch),float(scale),adjusted))
        score,scale,best=max(candidates,key=lambda x:x[0])
        records[i]['diagnosticWholeUniformScale']=scale
        records[i]['diagnosticBodySSIMFromFrame0AfterUniformScale']=score
        best.save(folder/f'{i:02d}-uniform-scale-diagnostic.png')
    plate=Image.new('RGB',(1152,1152),'#e5e6e2')
    for i in range(4):plate.paste(Image.open(folder/f'{i:02d}-matte.png'),((i%2)*576,(i//2)*576))
    plate.save(folder/'registered-quad.png')
    (folder/'registration.json').write_text(json.dumps(records,indent=2),encoding='utf-8');print(json.dumps(records))
    return images

if __name__=='__main__':extract(OUT/sys.argv[1],sys.argv[2])
