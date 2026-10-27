"""Native-scale source/target diagnosis: no production writes or mass render."""
from pathlib import Path
import hashlib
import json
import sys
import numpy as np
from PIL import Image, ImageDraw

HERE=Path(__file__).resolve().parent
FAMILY=HERE.parent
PROJECT=FAMILY.parents[3]
sys.path.insert(0,str(FAMILY))
from sprite_painter import SpritePainter, ORIGIN, FLIP, PPU, SIZE, _decode, _over
from compose_parts import encode
from qa_parts import FONT


def embed(source,pivot):
    result=np.zeros((SIZE,SIZE,4),dtype='float32')
    x,y=(ORIGIN-np.asarray(pivot)).astype(int)
    result[y:y+source.shape[0],x:x+source.shape[1]]=source
    return result


def alpha_image(rgba):
    gray=np.rint(np.clip(rgba[...,3],0,1)*255).astype('uint8')
    return Image.fromarray(np.repeat(gray[...,None],3,axis=-1))


def crop(image,center,size=64):
    x,y=np.rint(center).astype(int)
    half=size//2
    return image.crop((x-half,y-half,x+half,y+half))


def main():
    out=HERE/'JointPixelAudit'
    out.mkdir(exist_ok=True)
    painter=SpritePainter(PROJECT)
    anatomy=painter.anatomy
    source_path=PROJECT/anatomy['source']
    source=np.asarray(Image.open(source_path).convert('RGBA').crop(tuple(anatomy['crop'])))
    atlas=Image.open(PROJECT/'Assets/EmergencyShift/Art/Enemies/Stalker/Locomotion02/rig-atlas.png').convert('RGBA')
    identity={'row':0,'phase':0,'hips':[],'knees':[],'feet':[]}
    for limb in anatomy['limbs']:
        for key,point in [('hips','hip'),('knees','knee'),('feet','tip')]:
            xy=FLIP@(np.asarray(limb[point])-np.asarray(anatomy['pivot']))/PPU
            identity[key].append([float(xy[0]),float(xy[1]),0.0])
    _,rebuilt,_=painter.paint(identity)
    poses=json.loads((FAMILY/'poses.json').read_text())
    actual_pose=next(pose for pose in poses if pose['row']==7 and pose['phase']==0)
    actual_whole,actual_limbs,_=painter.paint(actual_pose)
    encode(actual_whole,green=True).save(out/'actual-whole-current.png')
    titles=['PaleFlesh native','atlas native','identity rebuilt','identity alpha','actual rebuilt','actual cap only','actual parts only']
    sheet=Image.new('RGB',(7*180,6*150+70),'#00ff00')
    draw=ImageDraw.Draw(sheet)
    draw.text((8,7),'All crops64x64 native pixels at224PPU; same scale, no crop autofit/resizing.',fill='black',font=FONT)
    for index,title in enumerate(titles):
        draw.text((index*180+5,40),title,fill='black',font=FONT)
    records=[]
    zooms=[]
    for leg,limb in enumerate(anatomy['limbs']):
        slot=leg*2+1
        raw=np.asarray(atlas.crop((slot%4*358,slot//4*406,slot%4*358+358,slot//4*406+406)))
        native=embed(_decode(Image.fromarray(raw)),anatomy['pivot'])
        pale=embed(_decode(Image.fromarray(source)),anatomy['pivot'])
        source_center=ORIGIN+np.asarray(limb['knee'])-np.asarray(anatomy['pivot'])
        hip,knee,toe=[np.asarray(actual_pose[key][leg],dtype='float32') for key in ('hips','knees','feet')]
        target_center=ORIGIN+FLIP@knee[:2]*PPU
        upper,lower,joint,root=painter._parts[leg]
        u=painter._transform(upper,limb['hip'],limb['knee'],hip,knee)
        l=painter._transform(lower,limb['knee'],limb['tip'],knee,toe)
        sk=np.asarray(limb['knee'],dtype='float32')
        j=painter._transform(joint,sk,sk+[1,0],knee,knee+[1/PPU,0,0])
        without_cap=_over(l,u)
        yy,xx=np.mgrid[:SIZE,:SIZE]
        disk=(xx-source_center[0])**2+(yy-source_center[1])**2<=20**2
        difference=np.rint(rebuilt[leg][...,3]*255).astype(int)-np.rint(native[...,3]*255).astype(int)
        opaque_native=disk&(native[...,3]>.95)
        source_rgb=np.asarray(encode(native))[...,:3].astype(int)
        identity_rgb=np.asarray(encode(rebuilt[leg]))[...,:3].astype(int)
        texture_area=opaque_native&(rebuilt[leg][...,3]>.95)
        radial_alpha_loss=np.where(disk,-difference,0)
        worst=np.unravel_index(np.argmax(radial_alpha_loss),difference.shape)
        source_yy,source_xx=np.mgrid[:406,:358]
        source_disk=(source_xx-limb['knee'][0])**2+(source_yy-limb['knee'][1])**2<=20**2
        visible_source=source_disk&(raw[...,3]>230)
        records.append({'leg':leg,'id':limb['id'],'jointRadius20NativePixels':int(disk.sum()),
            'sourceAtlasVsPaleRGBChangedWhereAtlasOpaque':int(((raw[...,:3]!=source[...,:3]).any(axis=-1)&visible_source).sum()),
            'sourceAtlasVsPaleAlphaChangedWhereAtlasOpaque':int(((raw[...,3]!=source[...,3])&visible_source).sum()),
            'identityAlphaLostOver8Pixels':int((disk&(difference < -8)).sum()),
            'identityMaxAlphaLoss':int(radial_alpha_loss.max()),
            'identityMaxAlphaGain':int(np.where(disk,difference,0).max()),
            'identityOpaqueSourceBecameAlphaUnder230':int((opaque_native&(rebuilt[leg][...,3]<230/255)).sum()),
            'identityMeanRGBErrorInOpaqueCore':float(np.abs(source_rgb-identity_rgb)[texture_area].mean()),
            'worstAlphaLossXY':[int(worst[1]),int(worst[0])],
            'worstNativeAlpha':int(round(float(native[worst][3])*255)),
            'worstRebuiltAlpha':int(round(float(rebuilt[leg][worst][3])*255)),
            'targetKneePixel':target_center.tolist(),'targetSubpixelPhase':(target_center-np.rint(target_center)).tolist()})
        images=[crop(encode(pale,green=True),source_center),crop(encode(native,green=True),source_center),
            crop(encode(rebuilt[leg],green=True),source_center),crop(alpha_image(rebuilt[leg]),source_center),
            crop(encode(actual_limbs[leg],green=True),target_center),crop(encode(j,green=True),target_center),
            crop(encode(without_cap,green=True),target_center)]
        for col,image in enumerate(images):
            sheet.paste(image,(col*180+55,leg*150+80))
            draw.text((col*180+6,leg*150+146),f'{limb["id"]}: source/target1x',fill='black',font=FONT)
        if leg in [0,3]:
            zooms.append((limb['id'],images))
        difference_image=np.asarray(encode(native,green=True)).copy()
        difference_image[disk&(difference < -8)]=(255,20,10)
        crop(Image.fromarray(difference_image),source_center).save(out/f'identity-loss-{limb["id"]}-native.png')
    sheet.save(out/'native-scale-source-target.png')
    enlarged=Image.new('RGB',(7*300,2*370),'#00ff00')
    ed=ImageDraw.Draw(enlarged)
    for row,(name,images) in enumerate(zooms):
        for col,image in enumerate(images):
            enlarged.paste(image.resize((256,256),Image.Resampling.NEAREST),(col*300+20,row*370+70))
            ed.text((col*300+6,row*370+10),f'{name} {titles[col]}',fill='black',font=FONT)
            ed.text((col*300+6,row*370+35),'4x nearest diagnostic',fill='black',font=FONT)
    enlarged.save(out/'source-target-4x-nearest.png')
    report={'sourcePaleFleshSha256':hashlib.sha256(source_path.read_bytes()).hexdigest(),
        'painterSha256':hashlib.sha256((FAMILY/'sprite_painter.py').read_bytes()).hexdigest(),
        'pixelsPerUnit':224,'records':records,
        'status':'Current parts-over approach rejected by owner. Identity alpha deficits are defects, regardless of connectivity/geometry checks.',
        'diagnosis':'Complementary feather masks form a partition, but alpha-over multiplies their transparency: two native-alpha .5 parts give .75 alpha rather than1. Circular joint/root feather overlays do not restore the exact partition; rotations then mix different source RGB. Bicubic subpixel sampling adds filtering on top.'}
    (out/'joint-pixel-audit.json').write_text(json.dumps(report,indent=2))
    print(json.dumps(report,indent=2))


if __name__=='__main__':
    main()
