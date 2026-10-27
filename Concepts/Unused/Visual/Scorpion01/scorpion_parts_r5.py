"""R5 compact chela: broad manus, short curved fingers, one movable hinge."""
from pathlib import Path
import math
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

ROOT=Path(__file__).resolve().parent
CHELA={'wrist':[256,430],'palm':[250,280],
       'fixedBase':[330,190],'fixedMid':[340,120],'fixedTip':[280,90],
       'hinge':[165,200],'movingMid':[132,115],'movingTip':[206,47]}
TAIL=[[175,70],[175,150],[185,230],[230,300],[300,300],[345,245],[340,185],[265,120]]
RATIO=.001
CHELA_KEYS=[key for key in CHELA if key!='wrist']


def close_chela(palp,side,amount,rotate):
    wrist=np.asarray(palp['arm'][-1])
    for key in CHELA_KEYS:
        palp[key]=rotate(palp[key],-side*math.radians(7)*amount,wrist).tolist()
    hinge=np.asarray(palp['hinge'])
    for key in ['movingMid','movingTip']:
        palp[key]=rotate(palp[key],side*math.radians(31)*amount,hinge).tolist()


def update_plan(p):
    for side,sign in [('L',-1),('R',1)]:
        # Left artwork coordinates define an independent reusable maquette.
        def local(key):
            x,y=CHELA[key]
            return [(-.34+(x-256)*RATIO)*(-sign),.68+(430-y)*RATIO,.08]
        p['pedipalps'][side]={'arm':[[sign*.13,.36,.14],[sign*.30,.40,.15],local('wrist')],
                               **{key:local(key) for key in CHELA if key!='wrist'}}
    p['tail']=[[(x-175)*.70/340,-.46-(y-70)*.70/340,.10+i*.018] for i,(x,y) in enumerate(TAIL)]
    p['counts'].update(tailLinksIncludingSting=7,metasomaSegments=5,telsonVesicle=1,aculeus=1)
    p['chelaConstruction']={'fixedFinger':'Rigid extension of manus, not a second hinged digit.',
                            'movingFinger':'One hinge at the outer manus; two curve guides are one rigid finger.',
                            'twoSidedClosure':'Manus turns slightly at wrist while the opposing finger closes.',
                            'sourceGuidePixels':CHELA}
    p['tailConstruction']={'sourceGuidePixels':TAIL,'segments':5,'telson':'Distinct venom vesicle and curved aculeus; no separate venom gameplay.'}


def guides():
    image=Image.new('RGB',(512,512),'#dfe4e3')
    d=ImageDraw.Draw(image)
    d.polygon([(235,447),(207,407),(142,367),(111,297),(127,229),(165,195),
               (252,176),(329,172),(381,228),(389,303),(355,371),(292,418),(278,447)],fill='#98b7b5',outline='#455a5e',width=3)
    d.line([tuple(CHELA[k]) for k in ['palm','fixedBase','fixedMid','fixedTip']],fill='#98b7b5',width=39)
    d.line([tuple(CHELA[k]) for k in ['hinge','movingMid','movingTip']],fill='#98b7b5',width=35)
    for keys in [['wrist','palm','fixedBase','fixedMid','fixedTip'],['hinge','movingMid','movingTip']]:
        d.line([tuple(CHELA[k]) for k in keys],fill='#23476a',width=3)
    for k,point in CHELA.items():
        x,y=point
        d.ellipse((x-4,y-4,x+4,y+4),fill='#f2d174',outline='#23476a')
    image.save(ROOT/'r5-chela-independent-guide.png')
    tail=Image.new('RGB',(512,512),'#dfe4e3')
    d=ImageDraw.Draw(tail)
    for index,(a,b) in enumerate(zip(TAIL,TAIL[1:])):
        width=[55,53,51,48,44,57,13][index]
        d.line([tuple(a),tuple(b)],fill='#98b7b5',width=width)
        x,y=a
        r=width*.5
        d.ellipse((x-r,y-r,x+r,y+r),fill='#98b7b5')
    # The last link is a curved, rigid stinger, not another flesh cylinder.
    d.line([(340,185),(315,169),(291,148),(265,120)],fill='#98b7b5',width=11)
    d.line([tuple(p) for p in TAIL],fill='#23476a',width=3)
    for x,y in TAIL:
        d.ellipse((x-4,y-4,x+4,y+4),fill='#f2d174',outline='#23476a')
    tail.save(ROOT/'r4-tail-independent-guide.png')


def prepare(Surface,write):
    """Generated component paint is UV input; the native maquette stays fixed."""
    keys=['wrist','palm','fixedBase','fixedMid','fixedTip','hinge','movingMid','movingTip']
    source=[[252,454],[270,310],[360,230],[375,135],[265,35],[193,218],[135,135],[190,46]]
    edges=[(0,1),(1,2),(2,3),(3,4),(1,5),(5,6),(6,7)]
    image=Image.open(ROOT/'chela-r5-blood-paint.png').convert('RGBA').resize((512,512),Image.Resampling.LANCZOS)
    skin=Surface(image,source,edges)
    baked,chela_metrics=skin.render([CHELA[key] for key in keys])
    baked.crop((0,0,512,512)).save(ROOT/'chela-source-r5.png')
    source_tail=[[167,105],[176,197],[214,291],[261,363],[374,354],[400,310],[357,185],[282,120]]
    image=Image.open(ROOT/'tail-r4-sage-paint.png').convert('RGBA').resize((512,512),Image.Resampling.LANCZOS)
    baked,tail_metrics=Surface(image,source_tail).render(TAIL)
    baked.crop((0,0,512,512)).save(ROOT/'tail-source-r4.png')
    atlas=Image.open(ROOT/'legs-r4-sage-paint.png').convert('RGBA').resize((1432,1624),Image.Resampling.LANCZOS)
    atlas.save(ROOT/'sources/sage-atlas.png')
    # Reuse the original red-eye mask so new bloody skin never emits light.
    body=Image.open(ROOT/'body-source-r4-sage.png').convert('RGBA')
    rgba=np.asarray(body).astype(float)
    centers=[[405,254],[618,254],[338,335],[689,335],[414,418],
             [610,418],[296,440],[728,440],[350,505],[675,505]]
    write(ROOT/'eye-emission-regions.json',{'eyeCentersSourcePixels':centers,'count':10,'lights':0,
          'note':'Red painted eye globes only, no dilation. Mask derives from pre-blood texture so skin stains do not emit.'})
    # Emit within the painted red globes; no dilation into neighbouring skin.
    emission_mask=np.clip((rgba[...,0]-np.maximum(rgba[...,1],rgba[...,2])*1.25)/120,0,1)*.7
    emission_mask[650:]=0
    alpha=Image.fromarray(np.uint8(emission_mask*255))
    emission=Image.new('RGBA',body.size,(255,3,1,0))
    emission.putalpha(alpha)
    emission.save(ROOT/'eyes-emission-source.png')
    write(ROOT/'r5-native-registration.json',{'chelaUV':dict(zip(keys,source)),'chelaMaquette':CHELA,
          'tailUV':source_tail,'tailMaquette':TAIL,'chelaMetrics':chela_metrics,'tailMetrics':tail_metrics,
          'scope':'UV paint registration into the previously declared native maquette, not approval of the generated raw anatomy.',
          'bodyRegistrationPixels':{'anterior':30,'posterior':1500}})


def layer(image,a,b,c,d,mirror=False):
    a,b,c,d=map(lambda v:np.asarray(v,dtype=float),(a,b,c,d))
    x=(b-a)/np.linalg.norm(b-a)
    y=np.array([-x[1],x[0]])
    u=(d-c)/np.linalg.norm(d-c)
    v=np.array([-u[1],u[0]])*(-1 if mirror else 1)
    matrix=np.column_stack([u,v])@np.row_stack([x,y])*np.linalg.norm(d-c)/np.linalg.norm(b-a)
    inverse=np.linalg.inv(matrix)
    shift=a-inverse@c
    return image.transform((1024,1024),Image.Transform.AFFINE,
                           (*inverse[0],shift[0],*inverse[1],shift[1]),Image.Resampling.BICUBIC)


def chela_layers():
    image=Image.open(ROOT/'chela-source-r5.png').convert('RGBA').resize((512,512),Image.Resampling.LANCZOS)
    pixels=np.asarray(image).copy()
    yy,xx=np.mgrid[:512,:512]
    xy=np.stack([xx,yy],axis=-1)
    def distance(keys):
        result=np.full((512,512),1e9)
        for ka,kb in zip(keys,keys[1:]):
            a,b=np.asarray(CHELA[ka]),np.asarray(CHELA[kb])
            delta=b-a
            q=np.clip(np.sum((xy-a)*delta,axis=-1)/np.dot(delta,delta),0,1)
            result=np.minimum(result,np.linalg.norm(xy-a-q[...,None]*delta,axis=-1))
        return result
    moving=distance(['hinge','movingMid','movingTip'])
    fixed=distance(['wrist','palm','fixedBase','fixedMid','fixedTip'])
    hinge=np.asarray(CHELA['hinge'])
    branch=(moving<fixed)&(yy<hinge[1]+8)
    jaw=pixels.copy()
    # Keep the common disk plus only the lower/outer insertion sector. A large
    # round cap intrudes into the finger opening; a hard split opens Attack06.
    radius=np.linalg.norm(xy-hinge,axis=-1)
    hinge_overlap=(radius<28)|((radius<38)&(xx<=hinge[0])&(yy>=hinge[1]-2))
    jaw[~(branch|hinge_overlap),3]=0
    pixels[branch&~hinge_overlap,3]=0
    cap=np.asarray(image).copy()
    cap[radius>28,3]=0
    return Image.fromarray(pixels),Image.fromarray(jaw),Image.fromarray(cap)


def render_chela(palp,side,project,layers):
    main,jaw,cap=layers
    body=layer(main,CHELA['wrist'],CHELA['fixedTip'],project(palp['arm'][-1]),project(palp['fixedTip']),side=='R')
    moving=layer(jaw,CHELA['hinge'],CHELA['movingTip'],project(palp['hinge']),project(palp['movingTip']),side=='R')
    moving.alpha_composite(body)
    moving.alpha_composite(layer(cap,CHELA['wrist'],CHELA['fixedTip'],project(palp['arm'][-1]),project(palp['fixedTip']),side=='R'))
    return moving


if __name__=='__main__':
    guides()
