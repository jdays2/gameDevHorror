"""Meaningful preservation/format checks, never a visual/anatomy acceptance."""
from pathlib import Path
from PIL import Image,ImageChops
import hashlib,json,zipfile,shutil
OUT=Path(__file__).resolve().parent;PROJECT=OUT.parents[3];EVIDENCE=PROJECT/'Docs/Validation/Task007Redraw12';REVIEW=OUT/'ReviewPlayer'
original=json.loads((PROJECT/'Docs/Validation/Task007WholeFrames07/ParallelMethods08/original-preservation.json').read_text(encoding='utf-8-sig'))
checks=[]
for item in original['checks']:
    p=PROJECT/item['path']
    if not p.exists():p=PROJECT/'Concepts/Unused/Visual/StalkerWholeFrames07'/item['path']
    got=hashlib.sha256(p.read_bytes()).hexdigest();assert got==item['sha256'],str(p)
    checks.append({'path':str(p.relative_to(PROJECT)).replace('\\','/'),'sha256':got,'exact':True})
(EVIDENCE/'original-preservation.json').write_text(json.dumps({'result':'PASS original files only','checks':checks},indent=2),encoding='utf-8')
native=json.loads((OUT/'DenseTrials/native-frames-r2.json').read_text());frames=json.loads((REVIEW/'frames.json').read_text())
count=0
for state in ['movement','sleep_entry','attack']:
    sheet=Image.open(OUT/native[state]['sheet']).convert('RGBA')
    for f in native[state]['frames']:
        raw=sheet.crop(f['sourceWholeCreatureRect']);im=Image.open(OUT/f['file']).convert('RGBA')
        assert im.size==raw.size
        import numpy as np
        a=np.array(raw);b=np.array(im)
        assert np.array_equal(a[:,:,:3],b[:,:,:3])
        assert np.array_equal(a[:,:,3][a[:,:,3]>=3],b[:,:,3][a[:,:,3]>=3])
        assert f['opaquePixelsOnCellEdge']==0
        count+=1
apng=[]
for state,items in frames.items():
    anim=Image.open(REVIEW/f'{state}.png');assert anim.n_frames==len(items)
    expected_loop=0 if state in ['movement','attack'] else 1;assert anim.info['loop']==expected_loop
    for i,item in enumerate(items):
        frame=Image.open(OUT/item['reviewFile']).convert('RGBA');mat=Image.new('RGBA',frame.size,'#e5e6e2');mat.alpha_composite(frame)
        anim.seek(i);assert ImageChops.difference(anim.convert('RGB'),mat.convert('RGB')).getbbox() is None,(state,i)
    apng.append({'state':state,'frames':anim.n_frames,'loop':expected_loop,'visiblePixelsExact':True})
package=OUT/'Stalker12-rejected-trials.zip'
with zipfile.ZipFile(package,'w',compression=zipfile.ZIP_DEFLATED) as z:
    z.write(OUT/'README.md','README.md')
    for file in REVIEW.rglob('*'):
        if file.is_file() and file.name!='data.json' and 'frames' not in file.relative_to(REVIEW).parts:z.write(file,'ReviewPlayer/'+file.relative_to(REVIEW).as_posix())
    for state in ['movement','sleep_entry','attack']:
        z.write(OUT/'DenseTrials'/f'{state}-r2.png',f'source-sheets/{state}-r2.png')
        z.write(OUT/'DenseTrials'/f'{state}-r2-prompt.txt',f'prompts/{state}-r2-prompt.txt')
        for item in native[state]['frames']:z.write(OUT/item['file'],f'native-frames/{state}/{item["index"]:02d}.png')
    z.write(OUT/'DenseTrials/native-frames-r2.json','native-frames/source-rectangles.json')
with zipfile.ZipFile(package) as z:assert z.testzip() is None
result={'result':'PASS format/preservation ONLY; art/anatomy FAIL','unchangedOriginalFiles':len(checks),'wholeNativePngsRGBAndAlpha3Exact':count,'apng':apng,'packageBytes':package.stat().st_size,'browserUI':'Not run; prior file URL policy rejection not bypassed.','game':'No Unity/Assets/Scenes changes in this work.'}
(EVIDENCE/'technical-checks.json').write_text(json.dumps(result,indent=2),encoding='utf-8');print(json.dumps(result))
