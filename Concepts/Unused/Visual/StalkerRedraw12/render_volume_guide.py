"""Render an untextured engineering pose guide from fixed bones/volumes.

This is not final creature art. No source skin pixels are sampled, moved or warped.
"""
from pathlib import Path
from PIL import Image,ImageDraw,ImageFilter
import numpy as np,json

OUT=Path(__file__).resolve().parent
SIZE=576
Y,X=np.mgrid[:SIZE,:SIZE].astype(float)

def disk(x,y,r):return r-np.hypot(X-x,Y-y)

def tube(a,b,radii):
    a=np.array(a,float);b=np.array(b,float);v=b-a
    t=np.clip(((X-a[0])*v[0]+(Y-a[1])*v[1])/np.dot(v,v),0,1)
    radius=np.interp(t,np.array(radii)[:,0],np.array(radii)[:,1])
    return radius-np.hypot(X-a[0]-t*v[0],Y-a[1]-t*v[1])

def smooth_union(a,b,k=3):
    h=np.clip(.5+.5*(a-b)/k,0,1)
    return b+(a-b)*h+k*h*(1-h)

def triangle(points,radius):
    min_distance=np.full((SIZE,SIZE),1e9);inside=np.ones((SIZE,SIZE),bool)
    for a,b in zip(points,points[1:]+points[:1]):
        a=np.array(a,float);b=np.array(b,float);v=b-a
        t=np.clip(((X-a[0])*v[0]+(Y-a[1])*v[1])/np.dot(v,v),0,1)
        min_distance=np.minimum(min_distance,np.hypot(X-a[0]-t*v[0],Y-a[1]-t*v[1]))
        inside &= v[0]*(Y-a[1])-v[1]*(X-a[0])<=0
    return np.where(inside,min_distance,-min_distance)+radius

def render(limbs,path,show_bones=False):
    flesh=triangle([(289,235),(239,312),(339,312)],12)
    flesh=smooth_union(flesh,disk(289,225,11),2)
    hard=np.full((SIZE,SIZE),-1000.)
    for limb in limbs:
        h,j,t=[np.array(limb[k],float) for k in ['H','J','T']]
        toward=np.array([288,287])-h;toward/=np.linalg.norm(toward)
        root=tube(h,h+toward*25,[(0,15),(1,11)])
        proximal=tube(h,j,[(0,16),(.14,15),(.35,8),(.85,7),(1,11)])
        flesh=smooth_union(flesh,root,3)
        flesh=smooth_union(flesh,proximal,2)
        flesh=smooth_union(flesh,disk(*j,13),1)
        sleeve=tube(j,j+(t-j)*.22,[(0,12),(.8,8),(1,7)])
        flesh=smooth_union(flesh,sleeve,1)
        cone=tube(j+(t-j)*.15,t,[(0,7),(.15,7),(.7,2),(1,0)])
        hard=np.maximum(hard,cone)
    full=np.maximum(flesh,hard)
    alpha=np.clip(full+.5,0,1)
    height=np.sqrt(np.maximum(full,0))*4
    gy,gx=np.gradient(height);norm=np.sqrt(gx*gx+gy*gy+1)
    light=np.clip((gx*.4+gy*.5+1)/norm,0,1)
    value=110+100*light
    rgb=np.repeat(value[:,:,None],3,2)
    rgb[hard>flesh]=np.repeat((180+65*light)[hard>flesh,None],3,1)
    rgba=np.dstack((np.clip(rgb,0,255).astype('uint8'),(alpha*255).astype('uint8')))
    im=Image.fromarray(rgba)
    d=ImageDraw.Draw(im)
    d.ellipse((265,263,311,311),fill='#c7cbcb',outline='#4b5055',width=2)
    d.ellipse((280,279,296,295),fill='#242a30')
    if show_bones:
        colors=['#18c5d4','#d9b621','#49c88c','#df8d41','#a498f2','#d582c1']
        for limb,color in zip(limbs,colors):d.line([tuple(limb[k]) for k in ['H','J','T']],fill=color,width=2)
    matte=Image.new('RGBA',im.size,'#e5e6e2');matte.alpha_composite(im)
    matte.convert('RGB').save(path)

if __name__=='__main__':
    data=json.loads((OUT/'plan.json').read_text(encoding='utf-8-sig'))
    a,b=data['walkOriginalControls'][:2]
    mid=[{'id':x['id'],**{k:((np.array(x[k])+y[k])/2).tolist() for k in ['H','J','T']}} for x,y in zip(a,b)]
    render(mid,OUT/'mid50-volume-guide.png')
    render(mid,OUT/'mid50-bone-volume-guide.png',True)
    (OUT/'mid50-control.json').write_text(json.dumps({'fraction':.5,'limbs':mid,'kind':'Untextured projected2D volume diagram, not final RGB skin or claimed recovered3D.','groups':'Same22functional groups from source-muscles.json; no source skin sampled or deformed.'},indent=2),encoding='utf-8')
    print('Saved fixed whole volume and bone-volume guides; no final sprite art rendered by code.')
