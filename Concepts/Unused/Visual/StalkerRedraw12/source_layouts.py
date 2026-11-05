"""Only whole ORIGINAL PNG references at correct common SpriteRenderer scale."""
from pathlib import Path
from PIL import Image
import json,numpy as np
OUT=Path(__file__).resolve().parent
plan=json.loads((OUT/'plan.json').read_text())
specs={
 'movement':{'cols':6,'rows':6,'keys':[(0,'movement/00'),(4,'movement/01'),(8,'movement/02'),(12,'movement/03'),(17,'movement/04'),(21,'movement/05'),(26,'movement/06'),(31,'movement/07'),(36,'movement/00')]},
 'sleep_entry':{'cols':5,'rows':4,'keys':[(0,'movement/00'),(6,'sleep_entry/01'),(13,'sleep_entry/02'),(19,'sleep_entry/03')]},
 'attack':{'cols':8,'rows':4,'keys':[(0,'movement/00'),(8,'attack/01'),(14,'attack/02'),(18,'attack/03'),(25,'attack/04'),(31,'movement/00')]}
}
for state,s in specs.items():
    cols,rows=s['cols'],s['rows'];sheet=Image.new('RGB',(576*cols,576*rows),'#e5e6e2');refs=[]
    for i in range(cols*rows):
        index,path=min(s['keys'],key=lambda x:abs(x[0]-i))
        im=Image.open(OUT/'anchors'/f'{path}-matte.png')
        sheet.paste(im,((i%cols)*576,(i//cols)*576))
        refs.append({'frame':i,'nearestImmutableWholeSource':path,'sourceAnchorAtFrame':index,'role':'Whole existing source pose reference. AI must draw missing in-betweens; not a new finished frame.'})
    sheet.save(OUT/'DenseTrials'/f'{state}-r3-whole-source-layout.png')
    s['frameReferences']=refs
(OUT/'DenseTrials/r3-source-layouts.json').write_text(json.dumps(specs,indent=2),encoding='utf-8')
print('Prepared whole-original layouts36/20/32, no body-part sampling or final skin painting.')
