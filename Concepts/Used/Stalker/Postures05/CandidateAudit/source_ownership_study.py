"""Traced original-body ownership, before a single whole-limb UV sample.

Study only. The binary boundary assigns the original central skin/rim to the
body exactly once. It does not cut a target limb, add a socket cap or feather
two complementary alpha layers. Own proximal muscle outside the trace stays
on its leg and must be covered by a plausible attachment/mount separately.
"""
from pathlib import Path
import hashlib
import json
from itertools import combinations
import numpy as np
from PIL import Image, ImageDraw

from continuous_polar_painter import SpritePainter, warp_rgba
from native_uv_painter import _decode, _over, ORIGIN
from compose_parts import encode
from qa_parts import bounded_holes, components, FONT

HERE = Path(__file__).resolve().parent
FAMILY = HERE.parent
PROJECT = FAMILY.parents[3]

# Source pixels in the native358x406 Walk00 crop, read against its real rim.
# The fore socket at(243,167) is outside this boundary; the parent-body corner
# at(252,241) is inside. This is a drawn ownership proposal, not a 3D claim.
BODY_OWNER = [(185,123),(193,125),(198,132),(200,143),(205,151),
    (214,167),(223,183),(232,201),(242,217),(249,224),(255,231),
    (257,238),(254,245),(246,247),(235,246),(221,247),(208,243),
    (195,240),(179,239),(164,242),(151,247),(138,247),(126,245),
    (118,246),(113,242),(112,235),(116,227),(125,219),(136,200),
    (146,181),(156,164),(166,148),(174,137),(177,129)]


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def owned_materials(painter):
    original_path = PROJECT/'Assets/EmergencyShift/Art/Enemies/Stalker/PaleFlesh/walk-sheet.png'
    with Image.open(original_path) as original:
        native = _decode(original.convert('RGBA').crop((30,84,388,490)))
    image = Image.new('L', (358,406))
    ImageDraw.Draw(image).polygon(BODY_OWNER, fill=255)
    owner = np.asarray(image) > 0
    sources = tuple(source * (~owner)[...,None] for source in painter.sources)
    for source in sources:
        source.flags.writeable = False
    torso = np.zeros((576,576,4), dtype='float32')
    x,y = (ORIGIN - np.asarray(painter.anatomy['pivot'])).astype(int)
    torso[y:y+406,x:x+358] = native * owner[...,None]
    return sources, torso, owner, native


def paint(painter, pose, sources, torso):
    limbs = [warp_rgba(source, limb, pose, leg, points=painter._grid)
             for leg, (source,limb) in enumerate(zip(sources,painter.anatomy['limbs']))]
    whole = np.zeros((576,576,4), dtype='float32')
    for limb in limbs:
        whole = _over(limb, whole)
    return _over(torso, whole), limbs, torso


def metrics(limbs, torso):
    alphas = [np.asarray(encode(limb))[...,3] for limb in limbs]
    torso_alpha = np.asarray(encode(torso))[...,3]
    pairs=[]
    for a,b in combinations(range(6),2):
        mask = (alphas[a]>128)&(alphas[b]>128)&(torso_alpha<=128)
        yy,xx=np.nonzero(mask)
        pairs.append({'pair':[a,b],'pixels':int(mask.sum()),
            'bbox':[int(xx.min()),int(yy.min()),int(xx.max())+1,int(yy.max())+1] if len(xx) else None})
    return {'all15Pairs':pairs,'legs':[{'leg':leg,'componentsOver4':components(alpha>4),
        'holes':[{'alphaOver':threshold,'parts':bounded_holes(alpha>threshold)} for threshold in [4,128]]}
        for leg,alpha in enumerate(alphas)]}


def main():
    out=HERE/'SourceOwnershipStudy01'
    if (out/'manifest.json').exists():
        raise FileExistsError('Frozen source ownership proof exists; use another version')
    (out/'Raw').mkdir(parents=True,exist_ok=True)
    (out/'Masks').mkdir(exist_ok=True)
    painter=SpritePainter(PROJECT)
    sources,torso,owner,native=owned_materials(painter)
    source_trace=[]
    evidence=Image.new('RGB',(4*358,2*456),'#00ff00')
    draw=ImageDraw.Draw(evidence)
    native_image=encode(native,green=True)
    overlay=np.asarray(native_image).copy()
    overlay[owner]=(overlay[owner]*.65+np.array([120,120,255])*.35).round().astype('uint8')
    traced=Image.fromarray(overlay)
    ImageDraw.Draw(traced).line(BODY_OWNER+[BODY_OWNER[0]],fill='blue',width=1)
    evidence.paste(traced,(0,50))
    draw.text((6,10),'Body owns blue region; native source',fill='black',font=FONT)
    for leg,(source,owned,limb) in enumerate(zip(painter.sources,sources,painter.anatomy['limbs'])):
        removed=(source[...,3]>4/255)&owner
        yy,xx=np.nonzero(removed)
        points=np.column_stack([xx,yy])
        distances=np.linalg.norm(points-np.asarray(limb['knee']),axis=1) if len(points) else []
        source_trace.append({'leg':leg,'id':limb['id'],'removedAlphaOver4':int(removed.sum()),
            'removedAlphaOver128':int(((source[...,3]>128/255)&owner).sum()),
            'nearestRemovedPixelToKnee':float(min(distances)) if len(distances) else None,
            'unownedSourceByteExact':bool(np.array_equal(source[~owner],owned[~owner])),
            'ownedSourceComponentsOver4':components(owned[...,3]>4/255)})
        x,y=(leg+1)%4*358,(leg+1)//4*456
        evidence.paste(encode(owned,green=True),(x,y+50))
        draw.text((x+6,y+8),f'{limb["id"]} body pixels removed:{int(removed.sum())}',fill='black',font=FONT)
    evidence.save(out/'traced-source-ownership-native.png')
    encode(native*owner[...,None]).save(out/'body-source-owned.png')
    Image.fromarray(owner.astype('uint8')*255).save(out/'body-source-ownership-mask.png')
    frozen=json.loads((HERE/'HeldOut01/poses.json').read_text())
    cases=[('open0-phase12',next(p for p in frozen if p['name']=='body-0-12')),
           ('closed-proposed',json.loads((FAMILY/'ClosedPoseStudy/proposed-pose.json').read_text()))]
    sheet=Image.new('RGB',(1152,1272),'#00ff00')
    d=ImageDraw.Draw(sheet)
    records=[]
    for row,(name,pose) in enumerate(cases):
        for col,label in enumerate(['sameUV-oldownership','sameUV-tracedownership']):
            mats,body=(painter.sources,painter._torso) if col==0 else (sources,torso)
            whole,limbs,actual_torso=paint(painter,pose,mats,body)
            key=f'{name}-{label}'
            encode(whole).save(out/'Raw'/f'{key}.png')
            encode(actual_torso).save(out/'Masks'/f'torso-{key}.png')
            for leg,limb in enumerate(limbs):
                encode(limb).save(out/'Masks'/f'leg-{key}-{leg}.png')
            sheet.paste(encode(whole,green=True),(col*576,row*636+60))
            d.text((col*576+8,row*636+8),name+' | '+label,fill='black',font=FONT)
            d.text((col*576+8,row*636+32),'Native224PPU; same pose; no deepmount yet',fill='black',font=FONT)
            records.append({'name':key,'pose':pose,**metrics(limbs,actual_torso)})
    sheet.save(out/'owner-before-after-native-green.png')
    report={'status':'Source ownership study only. Real own sockets remain for a separate deep mount control.',
        'binaryBodySourcePolygon':BODY_OWNER,'sourceOwnership':source_trace,'cases':records}
    (out/'proof.json').write_text(json.dumps(report,indent=2))
    manifest={'continuousUvSha256':digest(HERE/'continuous_polar_painter.py'),
        'sourceCacheSha256':digest(HERE/'native_uv_painter.py'),
        'sourceAtlasSha256':digest(PROJECT/'Assets/EmergencyShift/Art/Enemies/Stalker/Locomotion02/rig-atlas.png'),
        'originalWalkSha256':digest(PROJECT/'Assets/EmergencyShift/Art/Enemies/Stalker/PaleFlesh/walk-sheet.png'),
        'rawPngSha256':{path.name:digest(path) for path in sorted((out/'Raw').glob('*.png'))}}
    (out/'manifest.json').write_text(json.dumps(manifest,indent=2))
    print(json.dumps({'source':source_trace,'overlaps':[(r['name'],sum(p['pixels'] for p in r['all15Pairs'])) for r in records]},indent=2))


if __name__=='__main__':
    main()
