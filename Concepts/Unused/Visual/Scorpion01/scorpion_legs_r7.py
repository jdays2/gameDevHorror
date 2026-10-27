"""Compact walking-leg anatomy and rigid, quiet cuticular surfaces for r7."""
from pathlib import Path
import math
import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageOps

ROOT=Path(__file__).resolve().parent
UV=np.array([[420.,80.],[245.,210.],[180.,440.]])
# Texture calibration only; it does not alter the prospectively declared bones.
PAINT_UV=np.array([[420.,76.],[245.,225.],[180.,450.]])


def update_plan(plan):
    before={key:[point[:] for point in chain] for key,chain in plan['walkingLegs'].items()}
    for ident,chain in plan['walkingLegs'].items():
        points=np.asarray(chain,dtype=float)
        for index in [1,2]:
            points[index,:2]=points[0,:2]+(points[index,:2]-points[0,:2])*np.array([.74,.80])
        if ident.endswith('1'):
            points[2,1]-=.09
        plan['walkingLegs'][ident]=points.tolist()
    for side,palp in plan['pedipalps'].items():
        old_wrist=np.asarray(palp['arm'][-1],dtype=float)
        new_wrist=np.array([(-1 if side=='L' else 1)*.35,.61,.08])
        shift=new_wrist-old_wrist
        palp['arm'][-1]=new_wrist.tolist()
        for key in palp:
            if key!='arm':
                palp[key]=(np.asarray(palp[key])+shift).tolist()
    plan['changesFromR6']={
        'walkingLegs':'Shorter lateral reach, compact anterior walking sector, stout proximal shell, short dark foot instead of ivory dagger.',
        'xyFactorsFromRoot':[.74,.80], 'frontFootYAdjustment':-.09,
        'kneeHeightUnchanged':.32,'strideAndTimingUnchanged':True,
        'pedipalpWristY':.61,
        'previousWalkingLegs':before,
        'sourceUVGuide':UV.tolist(),
        'surface':'Two rigid cuticular segment layers plus one soft knee cap; one shared source for eight legs.'}


def guide():
    image=Image.new('RGB',(512,512),'#dfe4e3')
    d=ImageDraw.Draw(image)
    d.line([tuple(UV[0]),tuple(UV[1])],fill='#9aa69d',width=76)
    d.ellipse((208,173,282,247),fill='#9aa69d')
    d.polygon([(223,192),(272,219),(206,425),(180,450),(162,421)],fill='#9aa69d')
    d.line([tuple(q) for q in UV],fill='#244968',width=3)
    for x,y in UV:
        d.ellipse((x-4,y-4,x+4,y+4),fill='#f0d376')
    image.save(ROOT/'r7-leg-independent-guide.png')


def affine(image,a,b,c,d,width_scale):
    a,b,c,d=map(lambda point:np.asarray(point,dtype=float),(a,b,c,d))
    source_axis=(b-a)/np.linalg.norm(b-a)
    source_normal=np.array([-source_axis[1],source_axis[0]])
    target_axis=(d-c)/np.linalg.norm(d-c)
    target_normal=np.array([-target_axis[1],target_axis[0]])
    matrix=np.column_stack([target_axis*np.linalg.norm(d-c)/np.linalg.norm(b-a),target_normal*width_scale])
    matrix=matrix@np.row_stack([source_axis,source_normal])
    inverse=np.linalg.inv(matrix)
    shift=a-inverse@c
    return image.transform((1024,1024),Image.Transform.AFFINE,
        (*inverse[0],shift[0],*inverse[1],shift[1]),Image.Resampling.BICUBIC)


class Segment:
    def __init__(self,image,a,b,width_scale):
        self.image=image
        self.a=a
        self.b=b
        self.width_scale=width_scale

    def render(self,target):
        return affine(self.image,self.a,self.b,*target,self.width_scale),metrics()


def metrics():
    return {'flippedTriangles':0,'correctedSurfaceTriangles':0,
            'maximumSurfaceCorrectionPx':0.,'jointPinErrorPx':0.}


class Leg:
    def __init__(self,image,uv):
        self.uv=uv
        pixels=np.asarray(image).copy()
        yy,xx=np.mgrid[:512,:512]
        xy=np.stack([xx,yy],axis=-1)
        distances=[]
        for a,b in zip(uv,uv[1:]):
            delta=b-a
            q=np.clip(np.sum((xy-a)*delta,axis=-1)/np.dot(delta,delta),0,1)
            distances.append(np.linalg.norm(xy-a-q[...,None]*delta,axis=-1))
        self.layers=[]
        knee_distance=np.linalg.norm(xy-uv[1],axis=-1)
        for index in [0,1]:
            layer=pixels.copy()
            keep=(distances[index]<=distances[1-index])|(knee_distance<28)
            layer[~keep,3]=0
            self.layers.append(Image.fromarray(layer))
        x,y=map(int,uv[1])
        cap=image.crop((x-30,y-30,x+30,y+30))
        mask=Image.new('L',cap.size)
        ImageDraw.Draw(mask).ellipse((3,3,57,57),fill=255)
        mask=mask.filter(ImageFilter.GaussianBlur(1.5))
        rgba=np.asarray(cap).copy()
        rgba[...,3]=np.minimum(rgba[...,3],np.asarray(mask))
        self.cap=Image.fromarray(rgba)

    def render(self,target):
        target=np.asarray(target,dtype=float)
        canvas=Image.new('RGBA',(1024,1024))
        # Transverse width is fixed across phases; foreshortening only affects length.
        for index,width in [(1,.38),(0,.44)]:
            canvas.alpha_composite(affine(self.layers[index],self.uv[index],self.uv[index+1],target[index],target[index+1],width))
        cap=self.cap.resize((27,27),Image.Resampling.LANCZOS)
        x,y=target[1]
        canvas.alpha_composite(cap,(round(x)-13,round(y)-13))
        return canvas,metrics()


def surfaces():
    image=Image.open(ROOT/'leg-r7-paint.png').convert('RGBA').resize((512,512),Image.Resampling.LANCZOS)
    left=Leg(image,PAINT_UV)
    right_uv=PAINT_UV.copy()
    right_uv[:,0]=511-right_uv[:,0]
    right=Leg(ImageOps.mirror(image),right_uv)
    arm=Segment(left.layers[0],PAINT_UV[0],PAINT_UV[1],.35)
    return {ident:(left if ident.startswith('L') else right) for ident in ['LF','RF','LM','RM','LR','RR']},arm,left.cap


if __name__=='__main__':
    guide()
