"""Record exact per-limb bounded holes, including actual body occlusion."""
from pathlib import Path
import json
import numpy as np
from PIL import Image, ImageDraw
from qa_parts import bounded_holes, green, load_mask, FONT, torso_path

HERE=Path(__file__).resolve().parent
FAMILY=HERE.parent


def main():
    folder=FAMILY/'PreviewParts'
    qa=json.loads((HERE/'Preview54AuditWithHoles/qa.json').read_text())
    out=HERE/'HoleReview'
    out.mkdir(exist_ok=True)
    records=[]
    unique=set()
    for entry in qa['perLegBoundedHoles']:
        key=(entry['leg'],entry['alphaOver'],tuple(entry['largestBbox']))
        if key in unique:
            continue
        unique.add(key)
        limb=load_mask(folder/'Masks'/f'leg-{entry["maskStem"]}-{entry["leg"]}.png')
        torso=load_mask(torso_path(folder,entry['name']))
        whole=load_mask(folder/'Raw'/f'{entry["name"]}.png')
        for hole in bounded_holes(limb[...,3]>entry['alphaOver']):
            x0,y0,x1,y1=hole['bbox']
            inverse=limb[y0:y1,x0:x1,3]<=entry['alphaOver']
            records.append({**entry,'hole':hole,
                'pixelsOutsideActualTorso':int((inverse&(torso[y0:y1,x0:x1,3]<=128)).sum()),
                'pixelsTransparentInWhole':int((inverse&(whole[y0:y1,x0:x1,3]<=entry['alphaOver'])).sum())})
    records.sort(key=lambda item:-item['hole']['pixels'])
    sheet=Image.new('RGB',(1440,960),(0,255,0))
    draw=ImageDraw.Draw(sheet)
    for index,entry in enumerate(records[:12]):
        limb=load_mask(folder/'Masks'/f'leg-{entry["maskStem"]}-{entry["leg"]}.png')
        whole=load_mask(folder/'Raw'/f'{entry["name"]}.png')
        x0,y0,x1,y1=entry['hole']['bbox']
        cx,cy=(x0+x1)//2,(y0+y1)//2
        box=(cx-26,cy-26,cx+26,cy+26)
        xx,yy=index%4*360,index//4*320
        draw.text((xx+6,yy+3),f'{entry["name"]} leg {entry["leg"]} a>{entry["alphaOver"]}',fill='black',font=FONT)
        draw.text((xx+6,yy+24),f'{entry["hole"]["pixels"]} px | visible {entry["pixelsTransparentInWhole"]}',fill='black',font=FONT)
        left=green(whole).crop(box).resize((156,156),Image.Resampling.NEAREST)
        right=green(limb).crop(box).resize((156,156),Image.Resampling.NEAREST)
        sheet.paste(left,(xx+6,yy+55))
        sheet.paste(right,(xx+180,yy+55))
        draw.rectangle((xx+6+(x0-box[0])*3,yy+55+(y0-box[1])*3,
            xx+6+(x1-box[0])*3-1,yy+55+(y1-box[1])*3-1),outline='red',width=1)
        draw.rectangle((xx+180+(x0-box[0])*3,yy+55+(y0-box[1])*3,
            xx+180+(x1-box[0])*3-1,yy+55+(y1-box[1])*3-1),outline='red',width=1)
        draw.text((xx+6,yy+220),'Left whole / right isolated leg',fill='black',font=FONT)
    sheet.save(out/'bounded-holes-before.png')
    (out/'holes-before.json').write_text(json.dumps(records,indent=2))
    print(json.dumps({'uniqueHoles':len(records),
        'visibleUniquePixels':sum(record['pixelsTransparentInWhole'] for record in records)},indent=2))


if __name__=='__main__':
    main()
