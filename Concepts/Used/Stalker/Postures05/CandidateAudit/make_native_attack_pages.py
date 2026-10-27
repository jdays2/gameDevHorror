"""All792 attack frames, one fixed native-pixel crop and eight per page."""
import argparse
import hashlib
import json
from pathlib import Path
import sys
import numpy as np
from PIL import Image,ImageDraw,ImageFont

HERE=Path(__file__).resolve().parent
FAMILY=HERE.parent
sys.path.insert(0,str(FAMILY))
from frame_colors import decode
from audit_native_family import validate_composition,digest


def composite(image,background):
    rgba=decode(image)
    rgb=rgba[...,:3]+np.asarray(background)*(1-rgba[...,3:4])
    rgb=np.where(rgb<=.0031308,rgb*12.92,1.055*np.maximum(rgb,0)**(1/2.4)-.055)
    return Image.fromarray(np.rint(np.clip(rgb,0,1)*255).astype('uint8'))


def main():
    parser=argparse.ArgumentParser()
    parser.add_argument('--out',required=True)
    args=parser.parse_args()
    out=HERE/args.out
    if out.exists():
        raise FileExistsError('Attack art contact pages are frozen')
    poses,composition,composition_hash=validate_composition('attack')
    (out/'Green').mkdir(parents=True)
    bounds=[]
    hashes={}
    for pose in poses:
        path=FAMILY/'Raw'/f'{pose["name"]}.png'
        with Image.open(path) as image:
            alpha=np.asarray(image.convert('RGBA'))[...,3]
        yy,xx=np.nonzero(alpha>0)
        bounds.append([int(xx.min()),int(yy.min()),int(xx.max())+1,int(yy.max())+1])
        hashes[pose['name']]=digest(path)
    box=(max(0,min(b[0] for b in bounds)-6),max(0,min(b[1] for b in bounds)-6),
         min(576,max(b[2] for b in bounds)+6),min(576,max(b[3] for b in bounds)+6))
    width,height=box[2]-box[0],box[3]-box[1]
    font=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',16)
    pages=[]
    batches=[]
    for group in ['entry','strike','return']:
        selected=[p for p in poses if p['group']==group]
        if group=='strike':
            batches.extend((f'strike-{offset:02d}-{offset+7:02d}',selected[offset:offset+8]) for offset in range(0,24,8))
        else:
            batches.extend((f'{group}-phase{phase:02d}',[p for p in selected if p['phase']==phase]) for phase in range(48))
    for name,batch in batches:
        sheet=Image.new('RGB',((width+12)*4,(height+32)*2),'#00ff00')
        draw=ImageDraw.Draw(sheet)
        for index,pose in enumerate(batch):
            path=FAMILY/'Raw'/f'{pose["name"]}.png'
            with Image.open(path) as image:
                native=composite(image.convert('RGBA'),[0,1,0]).crop(box)
            x,y=index%4*(width+12),index//4*(height+32)
            sheet.paste(native,(x,y+32))
            draw.text((x+4,y+7),pose['name']+' | native1x',fill='black',font=font)
        sheet.save(out/'Green'/f'{name}.png')
        pages.append({'page':f'Green/{name}.png','names':[pose['name'] for pose in batch]})
    assert len(pages)==99
    assert len({name for page in pages for name in page['names']})==792
    assert digest(FAMILY/'attack-composition.json')==composition_hash
    report={'compositionManifestSha256':composition_hash,'fixedNativeCrop':box,'pixelsPerUnit':224,
        'resizing':False,'frames':792,'pages':pages,'rawPngSha256':hashes,
        'status':'Review pages created; inspection results must be recorded separately.'}
    (out/'manifest.json').write_text(json.dumps(report,indent=2))
    print('READY99 native contact pages,792 frames, fixed crop',box,flush=True)


if __name__=='__main__':
    main()
