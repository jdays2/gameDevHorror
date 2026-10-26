"""Compare actual per-leg alpha, excluding the shared torso attachment cover."""
from pathlib import Path
import json
import numpy as np
from PIL import Image,ImageFilter,ImageDraw

OUT=Path(__file__).resolve().parent
torso=Image.open(OUT/'CleanMasks/torso.png').getchannel('A')
cover=np.array(torso.filter(ImageFilter.MaxFilter(7)))>8
# Proximal paint intentionally joins the torso; compare the independently
# readable shafts and spikes beyond their anatomical attachment neighbourhood.
anatomy=json.loads((OUT.parent/'Locomotion02/anatomy.json').read_text())
yy,xx=np.mgrid[:576,:576]
for limb in anatomy['limbs']:
    hx,hy=limb['hip']
    cover |= (xx-(288+hx-187))**2+(yy-(288+hy-200))**2 <= (224*.13)**2
failures=[]
total=0
maximum=0
bounds=[]
for row in range(9):
    for phase in range(48):
        masks=[np.array(Image.open(OUT/'CleanMasks'/f'leg-{row}-{phase:02d}-{leg}.png').getchannel('A'))>128 for leg in range(6)]
        union=np.logical_or.reduce(masks)| (np.array(torso)>128)
        yy,xx=np.nonzero(union)
        bounds.append({'row':row,'phase':phase,'width':int(xx.max()-xx.min()+1),'height':int(yy.max()-yy.min()+1)})
        for first in range(6):
            for second in range(first+1,6):
                overlap=masks[first]&masks[second]&~cover
                count=int(overlap.sum())
                maximum=max(maximum,count)
                total+=count
                if count:
                    failures.append({'row':row,'phase':phase,'first':first,'second':second,'pixels':count})
                    if len(failures)<=12 or row==8 and phase==0:
                        image=Image.new('RGBA',(576,576),'#00ff00')
                        image.alpha_composite(Image.open(OUT/'FixedRaw'/f'body-{row}-{phase:02d}.png').convert('RGBA'))
                        rgba=np.array(image)
                        rgba[overlap]=[255,0,0,255]
                        Image.fromarray(rgba).save(OUT/f'overlap-{row}-{phase:02d}-{first}-{second}.png')
open_heights=[r['height'] for r in bounds if r['row']==0]
folded_heights=[r['height'] for r in bounds if r['row']==8]
report={'poses':432,'visibleOverlapPixels':total,'maximumPairPixels':maximum,'failingPairs':failures,
 'openHeights':[min(open_heights),max(open_heights)],'foldedHeights':[min(folded_heights),max(folded_heights)],
 'foldedToOpenMeanHeight':float(np.mean(folded_heights)/np.mean(open_heights)),
 'exclusion':'Actual torso alpha expanded 3px and a 0.13-unit proximal attachment radius; leg alpha >128/255', 'bounds':bounds}
(OUT/'limb-overlap.json').write_text(json.dumps(report,indent=2))
print(json.dumps({k:v for k,v in report.items() if k not in ['bounds','failingPairs']}))
print('Failing pairs:',len(failures))
