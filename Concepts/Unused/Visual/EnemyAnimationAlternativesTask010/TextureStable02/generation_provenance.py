"""Retain builtin raw-image provenance, including rejected masters."""
from pathlib import Path
import json,hashlib
ROOT=Path(__file__).resolve().parent
ids=['exec-2fddc650-f744-4f28-b8bc-bb0304b826c3.png',
     'exec-16e69803-c76d-4aeb-ab6f-ca72e8c7c210.png',
     'exec-56dd1a67-1bf1-4837-976d-0d94a0e1e444.png',
     'exec-c5452e74-c081-4d8c-bf40-fe6281da4688.png',
     'exec-1571cc02-fc04-464f-b9a0-9139527f74b5.png',
     'exec-1ce9834f-c6a0-4d2f-aca3-35e8a9c9547a.png']
default=Path('C:/Users/Mark/.codex/generated_images/01a1a841-d038-78c1-a0f9-34060430b475')
reasons=['LR endpoint still outside skin','LR endpoint5.8px outside','LR endpoint3.72px outside',
         'LR endpoint4.18px outside; extra gloss not preferred','LR horn over-rotated horizontally','Limited bone gate passed; one fixed texture for all24 poses']
records=[]
for i,name in enumerate(ids,1):
    suffix='' if i==1 else f'-r{i}'
    raw=ROOT/f'master-correction{suffix}-raw.png';prompt=ROOT/f'master-correction{suffix}-prompt.txt'
    sha=hashlib.sha256(raw.read_bytes()).hexdigest()
    assert sha==hashlib.sha256((default/name).read_bytes()).hexdigest()
    records.append({'revision':i,'raw':raw.name,'prompt':prompt.name,'defaultCopySource':str(default/name),
                    'rawSHA256':sha,'exactDefaultCopy':True,'selected':i==6,'reason':reasons[i-1]})
(ROOT/'generation-manifest.json').write_text(json.dumps({'tool':'built-in image_gen','records':records,
    'perFrameGeneration':False,'selectedFinalMaterial':'master.png','source':json.loads((ROOT/'master-provenance.json').read_text(encoding='utf-8'))},ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print('6 exact builtin raw copies; r6 selected, other5 rejected; prompts retained')
