"""Controlled comparison on identical saved masks, with exact changed pixels."""
from pathlib import Path
import hashlib
import json
import shutil
import sys
import numpy as np
from PIL import Image, ImageDraw

HERE=Path(__file__).resolve().parent
FAMILY=HERE.parent
sys.path.insert(0,str(FAMILY))
from sprite_painter import _decode, _fill_skin_holes, _over
from compose_parts import encode
from qa_parts import bounded_holes, load_mask, green, FONT


def main():
    source=FAMILY/'PreviewParts'
    out=HERE/'HoleReview/ControlledFill'
    (out/'Raw').mkdir(parents=True,exist_ok=True)
    (out/'Masks').mkdir(exist_ok=True)
    records=[]
    digest=hashlib.sha256()
    before_holes={4:0,128:0}
    after_holes={4:0,128:0}
    comparisons=[]
    # The inverse mask must retain diagonal exits and normal open V gaps.
    test=np.ones((9,9),dtype=bool)
    test[0,:]=False
    test[:,0]=False
    test[-1,:]=False
    test[:,-1]=False
    test[4,4]=False
    assert bounded_holes(test)==[{'pixels':1,'bbox':[4,4,5,5]}]
    test[1:4,1:4]=False
    assert bounded_holes(test)==[]  # Eight-connected diagonal exit to exterior.
    for base in sorted((source/'Masks').glob('leg-*-0.png')):
        stem=base.stem[4:-2]
        torso_image=Image.open(source/'Masks/torso.png').convert('RGBA')
        canvas=np.zeros((576,576,4),dtype='float32')
        for leg in range(6):
            path=source/'Masks'/f'leg-{stem}-{leg}.png'
            digest.update(path.name.encode())
            digest.update(path.read_bytes())
            before=load_mask(path)
            paint=_decode(Image.fromarray(before))
            repaired,trace=_fill_skin_holes(paint.copy())
            changed=np.any(repaired!=paint,axis=-1)
            for threshold in [4,128]:
                before_holes[threshold]+=sum(hole['pixels'] for hole in bounded_holes(before[...,3]>threshold))
                after_alpha=np.rint(repaired[...,3]*255).astype('uint8')
                after_holes[threshold]+=sum(hole['pixels'] for hole in bounded_holes(after_alpha>threshold))
            if trace:
                allowed=np.zeros(changed.shape,dtype=bool)
                for item in trace:
                    x0,y0,x1,y1=item['bbox']
                    allowed[y0:y1,x0:x1]=True
                assert not (changed&~allowed).any()
                records.append({'mask':path.name,'changedPixels':int(changed.sum()),'trace':trace})
                if len(comparisons)<8:
                    comparisons.append((path.name,before,np.asarray(encode(repaired)),trace))
            encode(repaired).save(out/'Masks'/path.name)
            canvas=_over(repaired,canvas)
        shutil.copy2(source/'Masks/torso.png',out/'Masks/torso.png')
        whole=_over(_decode(torso_image),canvas)
        row,phase=map(int,stem.split('-'))
        encode(whole).save(out/'Raw'/f'body-{row}-{phase:02d}.png')
    sheet=Image.new('RGB',(1200,960),(0,255,0))
    draw=ImageDraw.Draw(sheet)
    for index,(name,before,after,trace) in enumerate(comparisons):
        x,y=index%4*300,index//4*480
        left,top,right,bottom=trace[0]['bbox']
        cx,cy=(left+right)//2,(top+bottom)//2
        box=(cx-22,cy-22,cx+22,cy+22)
        sheet.paste(green(before).crop(box).resize((264,264),Image.Resampling.NEAREST),(x+12,y+50))
        sheet.paste(green(after).crop(box).resize((132,132),Image.Resampling.NEAREST),(x+84,y+330))
        draw.text((x+8,y+8),name,fill='black',font=FONT)
        draw.text((x+8,y+30),'Before / after below',fill='black',font=FONT)
    sheet.save(HERE/'HoleReview/bounded-holes-before-after.png')
    report={'sourceMasksSha256':digest.hexdigest(),'frames':54,'beforeHolePixels':before_holes,
        'afterHolePixels':after_holes,'changedPixels':sum(item['changedPixels'] for item in records),
        'records':records,'method':'Per-limb enclosed inverse8 components <=6px, native neighboring color. No external contours or open V gaps filled.'}
    (HERE/'HoleReview/fill-trace.json').write_text(json.dumps(report,indent=2))
    print(json.dumps({key:value for key,value in report.items() if key!='records'},indent=2))


if __name__=='__main__':
    main()
