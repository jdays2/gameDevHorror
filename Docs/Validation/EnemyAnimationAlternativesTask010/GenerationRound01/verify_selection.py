from pathlib import Path
import json,hashlib,re
import numpy as np
from PIL import Image
V=Path(__file__).resolve().parent
P=V.parents[3]
R=P/'Concepts/Unused/Visual/EnemyAnimationAlternativesTask010/GenerationRound01'
S=R/'Selection'
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
baseline=json.loads((V.parent/'source-preservation.json').read_text(encoding='utf-8'))
bad=[s for s,h in baseline['inputs'].items() if sha(P/s)!=h]
assert not bad,bad
manifest=json.loads((R/'generation-manifest.json').read_text(encoding='utf-8'))
names={'baseGen0':'A-direct-r1','baseGen1':'B-bone-guided-r1','A-clean':'A-direct-r2','B-clean':'B-bone-guided-r2','C-base':'C-volume-guided-r1'}
for item in manifest['items']:
    copied=R/(names.get(item['key'],item['key'])+'.png')
    assert sha(copied)==sha(Path(item['defaultArtifact'])),copied
animated={}
for branch in 'ABC':
    frames=[Image.open(S/f'{branch}-{i:02}.png').convert('RGBA') for i in range(6)]
    assert all(im.size==(640,640) for im in frames)
    unique=len({hashlib.sha256(im.tobytes()).hexdigest() for im in frames});assert unique==6
    animation=Image.open(S/f'{branch}-walk.png');assert animation.n_frames==6
    for i,expected in enumerate(frames):
        animation.seek(i);actual=np.array(animation.convert('RGBA'));original=np.array(expected)
        assert np.array_equal(actual[:,:,3],original[:,:,3])
        mask=original[:,:,3]>0;assert np.array_equal(actual[mask],original[mask])
        assert animation.info['duration']==125
    animated[branch]={'uniqueRGBA':unique,'frames':6,'frameMs':125,'loopMs':750,'alphaAndVisibleRGBAExact':True}
corners={}
for i in range(6):
    array=np.array(Image.open(S/f'C-{i:02}.png').convert('RGBA'))
    corners[str(i)]={'emptyPointAlpha':int(array[100,50,3]),'emptyPatchMaxAlpha':int(array[40:100,40:100,3].max())}
    assert corners[str(i)]['emptyPointAlpha']==0 and corners[str(i)]['emptyPatchMaxAlpha']<=1
links=[]
for path in [R/'README.md',R.parent/'README.md',P/'Docs/Research/EnemyAnimationAlternativesTask010.md']:
    for link in re.findall(r'\]\(([^)]+)\)',path.read_text(encoding='utf-8')):
        if link.startswith(('https:','http:','#')):continue
        target=(path.parent/link.split('#')[0]).resolve();assert target.exists(),target;links.append(str(target))
for link in re.findall(r'(?:src|href)="([^"]+)"',(S/'selection.html').read_text(encoding='utf-8')):
    assert (S/link).exists(),link
result={'selectedOriginalInputsUnchanged':len(baseline['inputs']),'copiedGeneratedResults':len(manifest['items']),
 'animations':animated,'CemptyAlpha':corners,'localMarkdownLinksChecked':len(links),
 'independentReview':'independent-review.md; all18 native and both backgrounds reviewed twice',
 'bonePhysicsProductionPass':False,'ownerArtAcceptance':False,'UnityInvoked':False,
 'Blender':'5.2.2 LTS; actual offline orthographic volume references, not final skin mesh',
 'browserUI':'not tested; page has no controls or scripts, APNG sequence decoded/verified',
 'authoringReview':'not applicable; no scene changes'}
(V/'verification.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(result,ensure_ascii=False,indent=2))
