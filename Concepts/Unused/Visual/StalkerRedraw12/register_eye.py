"""One PPU per sheet, eye pivot only. Never fits later limb silhouettes."""
from pathlib import Path
from PIL import Image
import numpy as np
from whole_registration import corr,gray,OUT

def register_eye(image,scale=None):
    source=Image.open(OUT/'anchors/movement/00-matte.png').crop((260,260,317,317))
    a=gray(image);h,w=a.shape;best=(-2,None,None,None)
    for q in ([scale] if scale is not None else np.arange(.25,1.051,.01)):
        size=(round(57*q),round(57*q));t=gray(source.resize(size,Image.Resampling.LANCZOS));tc=t-t.mean();n=t.size
        sums=corr(a,np.ones(t.shape));sums2=corr(a*a,np.ones(t.shape));num=corr(a,tc)
        score=num/np.sqrt(np.maximum(sums2-sums*sums/n,1e-10)*(tc*tc).sum())
        yy,xx=np.indices(score.shape);cx=xx+28*q;cy=yy+27*q
        score[(abs(cx-w*.5)>w*.23)|(abs(cy-h*.5)>h*.25)]=-2
        y,x=np.unravel_index(np.argmax(score),score.shape);s=float(score[y,x])
        if s>best[0]:best=(s,float(q),float(x+28*q),float(y+27*q))
    return {'score':best[0],'wholeScaleFromSource':best[1],'originNative':[best[2],best[3]],'fitScope':'Single PPU calibrated at first neutral iris for entire sheet; later frames only whole-frame eye pivot, NO per-limb or silhouette fit.'}

if __name__=='__main__':
    import json
    m=json.loads((OUT/'DenseTrials/native-frames-r2.json').read_text())
    for s in ['movement','sleep_entry','attack']:
        f=m[s]['frames'][0];print(s,register_eye(Image.open(OUT/f['file']).convert('RGB')))
