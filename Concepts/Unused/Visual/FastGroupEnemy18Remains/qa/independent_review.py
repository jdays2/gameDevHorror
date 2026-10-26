"""Read assets; write ONLY independent diagnostics beside this file.

The observed joint markers are measurements, never replacement anatomy.
The B3 pose is a retrospective fixed-length feasibility witness. It is not
a prospective generation plan or an inferred medical/physical reconstruction.
"""
from pathlib import Path
import argparse
import hashlib
import json
import math
import numpy as np
from PIL import Image, ImageDraw, ImageFont

QA = Path(__file__).resolve().parent
ROOT = QA.parent
VIS = ROOT.parent
BONE_FILE = VIS / 'FastGroupEnemy13AssetInspired/anatomy-plan-v4.json'
MUSCLE_FILE = VIS / 'FastGroupEnemy13AssetInspired/anatomy-muscle-plan-v4.json'
MASTER = VIS / 'FastGroupEnemy17SimplifiedMotion/simplified-master-v1.png'
BONES = json.loads(BONE_FILE.read_text(encoding='utf-8'))
MUSCLES = json.loads(MUSCLE_FILE.read_text(encoding='utf-8'))
SCALE = float(BONES['projection']['scale'])
ORIGIN = np.array(BONES['projection']['origin'], float)
BG = (233, 232, 228, 255)
CYAN = '#007ea8'
RED = '#d42a31'
GOLD = '#b07800'
GRAY = '#787b80'
FONT = ImageFont.truetype('C:/Windows/Fonts/arial.ttf', 19)
SMALL = ImageFont.truetype('C:/Windows/Fonts/arial.ttf', 16)
TITLE = ImageFont.truetype('C:/Windows/Fonts/arial.ttf', 26)


def project(p):
    return np.asarray(p)[..., :2] * SCALE + ORIGIN


def world(p, z):
    return np.array([*(np.asarray(p, float) - ORIGIN) / SCALE, z])


REST = {}
for limb in BONES['fronts']:
    ident = 'FU' if limb['side'] == 'screenUpper' else 'FL'
    REST[ident] = np.array([limb[k] for k in ['shoulder', 'elbow', 'wrist', 'palm']], float)
for limb in BONES['rears']:
    ident = 'RU' if limb['side'] == 'screenUpper' else 'RL'
    REST[ident] = np.array([limb[k] for k in ['hip', 'knee', 'hock', 'palm']], float)
LENGTHS = {key: np.linalg.norm(np.diff(value, axis=0), axis=1) for key, value in REST.items()}

# Pixel observations, fixed before generating these diagnostic images.
# Visible joint-zone centres have an estimated +/-10px measurement range.
OBS = {
    'B3': {'shoulderCut': [734., 401.], 'elbow': [551., 260.], 'wrist': [350., 245.]},
    'B4': {'shoulderCut': [835., 415.], 'elbow': [655., 264.], 'wrist': [353., 246.]},
}


def feasibility(observed):
    s, e, w = [np.array(observed[k], float) for k in ['shoulderCut', 'elbow', 'wrist']]
    upper, fore, carpus = LENGTHS['FU']
    d_upper = float(np.linalg.norm(s-e))
    d_fore = float(np.linalg.norm(e-w))
    record = {'observationsPx': observed, 'estimatedCentreUncertaintyPx': 10,
              'projectionUpperPx': d_upper, 'projectionForearmPx': d_fore,
              'maximumUpperPx': float(upper*SCALE), 'maximumForearmPx': float(fore*SCALE)}
    if d_fore > fore*SCALE or d_upper > upper*SCALE:
        record['verdict'] = 'FAIL_MAXIMUM_PROJECTED_LENGTH'
        record['excessForearmPx'] = float(d_fore-fore*SCALE)
        record['minimumForearmUnderAxisUncertaintyPx'] = max(0., float(abs(e[0]-w[0])-20.))
        return record, None
    wz = float(REST['FU'][2, 2])
    ez = wz + math.sqrt(float(fore*fore-(d_fore/SCALE)**2))
    sz = ez - math.sqrt(float(upper*upper-(d_upper/SCALE)**2))
    # Preserve the SOURCE carpus and complete finger geometry as one rigid hand.
    palm = w + (project(REST['FU'][3])-project(REST['FU'][2]))
    pz = wz + float(REST['FU'][3, 2]-REST['FU'][2, 2])
    chain = np.array([world(s, sz), world(e, ez), world(w, wz), world(palm, pz)])
    record.update(verdict='FIXED_LENGTH_FEASIBILITY_GO_WITH_LIMITS',
                  witnessJoints3D=chain.tolist(),
                  witnessLengthErrors=(np.linalg.norm(np.diff(chain, axis=0), axis=1)-LENGTHS['FU']).tolist(),
                  witnessNote='Elbow raised in a bent detached limb; proximal cut and hand are low. '
                              'Existence proof only: no measured height, stability, or floor contact claim.')
    return record, chain


def canvas(asset=None):
    out = Image.new('RGBA', (1254, 1254), BG)
    if asset:
        out.alpha_composite(Image.open(ROOT/asset).convert('RGBA'))
    return out


def text(draw, xy, value, fill='#15212c', font=SMALL):
    box = draw.textbbox(xy, value, font=font)
    draw.rectangle((box[0]-3, box[1]-2, box[2]+3, box[3]+2), fill=(245, 245, 241, 228))
    draw.text(xy, value, font=font, fill=fill)


def dash(draw, a, b, color, width=3, period=13):
    a, b = np.array(a, float), np.array(b, float)
    length = np.linalg.norm(b-a)
    if length == 0:
        return
    for t in np.arange(0, length, period):
        p, q = a+(b-a)*t/length, a+(b-a)*min(t+period*.56, length)/length
        draw.line([tuple(p), tuple(q)], fill=color, width=width)


def joint(draw, p, color, radius=5, name=None):
    x, y = p
    draw.ellipse((x-radius, y-radius, x+radius, y+radius), outline=color, width=3)
    if name:
        text(draw, (x+7, y-20), name, color)


def draw_digits(draw, ident, translation=(0., 0.), color=CYAN):
    source = BONES['fronts'] if ident[0] == 'F' else BONES['rears']
    limb = next(x for x in source if x['side'] == ('screenUpper' if ident[1]=='U' else 'screenLower'))
    offset = np.array(translation)
    palm = project(limb['palm'])+offset
    for digit in limb['digits']:
        keys = ['proximal', 'distal', 'clawBase'] if ident[0]=='F' else ['base', 'tip']
        pts = [palm] + [project(digit[k])+offset for k in keys]
        draw.line([tuple(p) for p in pts], fill=color, width=2)
        for p in pts[1:]:
            joint(draw, p, color, radius=2)


def draw_bones(out, omit=(), chains=None, source_digits=True):
    draw = ImageDraw.Draw(out)
    spine = project(BONES['spine'])
    draw.line([tuple(p) for p in spine], fill=CYAN, width=3)
    head = project(BONES['head']['center'])
    draw.line([tuple(head), tuple(spine[0])], fill=CYAN, width=3)
    joint(draw, head, CYAN, 7, 'head source axis')
    for ident, rest in REST.items():
        if ident in omit:
            continue
        chain = (chains or {}).get(ident, rest)
        pts = project(chain)
        for k, (a,b) in enumerate(zip(pts, pts[1:])):
            if ident[0]=='R' and k == 0:
                dash(draw, a, b, GRAY)
            else:
                draw.line([tuple(a), tuple(b)], fill=CYAN, width=3)
        for k,p in enumerate(pts):
            joint(draw, p, CYAN, 4, f'{ident}{k}')
        if source_digits:
            offset = pts[3]-project(rest[3])
            draw_digits(draw, ident, offset)


def footer(out, title, lines):
    draw = ImageDraw.Draw(out)
    text(draw, (30, 28), title, font=TITLE)
    for i,line in enumerate(lines):
        text(draw, (30, 1090+i*28), line, font=FONT)


def save(out, name):
    out.convert('RGB').save(QA/name)


def rotation_between(a, b):
    a, b = a/np.linalg.norm(a), b/np.linalg.norm(b)
    v = np.cross(a,b)
    c = float(np.dot(a,b))
    if np.linalg.norm(v) < 1e-10:
        return np.eye(3) if c > 0 else -np.eye(3)
    skew = np.array([[0., -v[2], v[1]], [v[2], 0., -v[0]], [-v[1], v[0], 0.]])
    return np.eye(3)+skew+skew@skew/(1+c)


def muscle_points(group, variant, b3):
    a, b = np.array(group['from']), np.array(group['to'])
    ident = group['id']
    if variant == 'B3' and ident.startswith('FU'):
        k = 0 if ident in ['FU1','FU2','FU3'] else 1
        source, target = REST['FU'], b3
        matrix = rotation_between(source[k+1]-source[k], target[k+1]-target[k])
        a = target[k] + matrix@(a-source[k])
        b = target[k] + matrix@(b-source[k])
    return project(a), project(b)


def status(ident, variant):
    if variant == 'C2':
        return 'distal_fragment_source_only' if ident in ['FU4','FU5','FL4','FL5','RU4','RL4'] else 'ruptured_or_occluded_unverifiable'
    if variant == 'B3' and ident == 'FU1':
        return 'severed_at_shoulder_not_continuous'
    if ident in ['FU1','FU2','FU3','T1','T3']:
        return 'wound_or_occluded_attachment_unverified'
    return 'source_attachment_declared_retained_under_skin_unverified'


def muscle_pass(asset, variant, b3, volumes):
    if variant == 'C2':
        # Never project intact source muscles onto disrupted heap tissue.
        actual = Image.open(QA/'independent-02-C2-parts.png').convert('RGBA')
        ImageDraw.Draw(actual).rectangle((0,1070,1254,1254),fill=BG)
        footer(actual, 'C2 / declared breaks and visible terminal parts', [
            'LEFT: actual damaged artwork; only surviving parts are accounted for.',
            'RIGHT: unchanged SOURCE inventory of 22 aggregates, not located in this heap.',
            'No intact proximal attachment, continuous torso, or hidden muscle count is asserted.',
            'Both sheets retain the source IDs; none are fitted into damaged tissue.'])
        reference = muscle_pass(MASTER, 'SOURCE', b3, volumes)
        combined = Image.new('RGBA', (2508,1254), BG)
        combined.alpha_composite(actual)
        combined.alpha_composite(reference,(1254,0))
        return combined
    out = canvas(asset)
    layer = Image.new('RGBA', out.size)
    draw = ImageDraw.Draw(layer)
    label_boxes = []
    def group_label(point, value, color):
        point = np.asarray(point)
        for dx,dy in [(0,0),(0,24),(0,-24),(24,24),(-24,-24),(0,48),(0,-48)]:
            xy = (point[0]+dx,point[1]+dy)
            box = draw.textbbox(xy,value,font=SMALL)
            padded = (box[0]-4,box[1]-4,box[2]+4,box[3]+4)
            if not any(padded[0]<old[2] and padded[2]>old[0] and
                       padded[1]<old[3] and padded[3]>old[1] for old in label_boxes):
                break
        label_boxes.append(padded)
        if dx or dy:
            draw.line([tuple(point),(xy[0],xy[1]+8)],fill=color,width=1)
        text(draw,xy,value,color)
    for group in MUSCLES['groups']:
        ident = group['id']
        a,b = muscle_points(group, variant, b3)
        unknown = variant != 'SOURCE' and status(ident,variant) in [
            'severed_at_shoulder_not_continuous', 'wound_or_occluded_attachment_unverified']
        color = GRAY if unknown else group.get('color', CYAN)
        if volumes and not unknown:
            width = max(3, round(2*SCALE*group['radius']))
            rgba = (*tuple(bytes.fromhex(color.lstrip('#'))), 58)
            draw.line([tuple(a),tuple(b)], fill=rgba, width=width)
            for p in [a,b]:
                r=width/2
                draw.ellipse((p[0]-r,p[1]-r,p[0]+r,p[1]+r), fill=rgba)
        else:
            if unknown:
                dash(draw,a,b,color,width=2)
            else:
                draw.line([tuple(a),tuple(b)], fill=color, width=2)
                delta = b-a
                if np.linalg.norm(delta)>0:
                    unit = delta/np.linalg.norm(delta)
                    normal = np.array([-unit[1],unit[0]])
                    draw.polygon([tuple(b),tuple(b-unit*9+normal*4),tuple(b-unit*9-normal*4)], fill=color)
            label = ident + (' ?' if unknown else '')
            group_label((a+b)/2, label, color)
    out.alpha_composite(layer)
    label = '03 functional volumes' if volumes else '04 source IDs / directions / attachment status'
    footer(out, f'{variant}: {label}', [
        'Source: anatomy-muscle-plan-v4, 22 functional aggregates. No per-group fit.',
        'Capsules are rough envelopes and may overshoot skin; hidden attachments are unverified.',
        'Gray dashed = severed / disrupted / not located. Not a medical reconstruction.',
        'SOURCE ONLY: alive master anatomy, not a reconstruction or location map of C2.' if variant=='SOURCE' else
        'B3 FU1 is severed at shoulder; a line across the gap is never treated as intact.' if variant=='B3' else
        'A2 wounds do not authorize inventing missing limbs or proving intact hidden muscles.',
    ])
    return out


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--muscles-after-bone-review', action='store_true')
    args = parser.parse_args()
    QA.mkdir(parents=True, exist_ok=True)
    records = {}
    chain_b3 = None
    for key in OBS:
        record, chain = feasibility(OBS[key])
        records[key] = record
        if key == 'B3':
            chain_b3 = chain
    report = {
        'task':'TASK-005', 'review':'independent F18 remains bone-first review',
        'source':{str(p.name):hashlib.sha256(p.read_bytes()).hexdigest() for p in [BONE_FILE,MUSCLE_FILE,MASTER]},
        'assets':{name:hashlib.sha256((ROOT/name).read_bytes()).hexdigest() for name in ['corpse-v2.png','torn-v3.png','torn-v4.png','heap-v2.png']},
        'commonBodyRegistration':{'uniformScale':1.0,'offsetPx':[0.,0.],
                                  'perBoneLengthFit':False,'B3PostArtworkPoseWitness':True},
        'sourceSegmentLengths3D':{k:v.tolist() for k,v in LENGTHS.items()},
        'sourceMaximumProjectedPx':{k:(v*SCALE).tolist() for k,v in LENGTHS.items()},
        'measurements':records,
        'boneScope':{'A2':'Unposed source-v4 projection over unchanged main body. Pose/height not reconstructed.',
                     'B3':'Retrospective 3D feasibility witness with fixed lengths. Observed centres are explicitly separate measurements.',
                     'B4':'Hard maximum-projection failure; no muscle pass generated.',
                     'C2':'Broken distal-chain accounting only. No complete torso/rear proximal chain inferred or fitted.'},
        'muscleInventory':{variant:[{'id':g['id'],'sourceCount':g['count'],'status':status(g['id'],variant)} for g in MUSCLES['groups']] for variant in ['A2','B3','C2']},
        'limitations':['No inferred exact medical anatomy or hidden muscle count.',
                       'Fixed-length feasibility is necessary, not proof of actual 3D pose, floor contact, or stability.',
                       'No individual segment lengths, group radii, or insertions adjusted to the artwork.',
                       'C2 damaged aggregate tissue cannot prove 22 intact visible muscle groups.',
                       'Owner artistic approval and Unity validation are separate.'],
    }
    report['alphaChecks'] = {}
    for variant, asset in [('A2','corpse-v2.png'),('B3','torn-v3.png'),('C2','heap-v2.png')]:
        raw = Image.open(ROOT/asset).convert('RGBA')
        alpha = np.array(raw.getchannel('A'))
        report['alphaChecks'][variant] = {
            'size':list(raw.size), 'nonzeroAlphaBBox':list(raw.getchannel('A').getbbox()),
            'alpha16BBox':list(Image.fromarray((alpha>=16).astype('uint8')*255).getbbox()),
            'alpha128BBox':list(Image.fromarray((alpha>=128).astype('uint8')*255).getbbox()),
            'outerBorderMaxAlpha':int(max(alpha[0].max(),alpha[-1].max(),alpha[:,0].max(),alpha[:,-1].max())),
            'partialAlphaPixels':int(((alpha>0)&(alpha<255)).sum())}
        for name, color in [('light',(233,232,228,255)),('dark',(23,25,29,255))]:
            bg = Image.new('RGBA',raw.size,color)
            bg.alpha_composite(raw)
            save(bg,f'independent-05-{variant}-{name}.png')
    # Persist the observations and independent criteria BEFORE drawing overlays.
    (QA/'independent-review.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
    out=canvas()
    draw_bones(out)
    footer(out,'Independent anatomy v4 / unchanged source projection',[
        'Camera 1254 x 1254; scale375px/unit; origin(760,665). Body registration1x+(0,0).',
        'Four limbs; each front3 digit chains; each rear3main+1lateral.',
        'True 3D lengths fixed; top projection may be shorter, never longer.',
        'This template is prospective source anatomy, not a reconstructed corpse pose.'])
    save(out,'independent-01-source-bones.png')
    out=canvas('corpse-v2.png'); draw_bones(out)
    footer(out,'A2 / unadjusted source-v4 bones',[
        'No individual joint or segment moved to match the drawing.',
        'Joint centres are schematic; wounds/hidden joints do not prove exact anatomy.',
        'Upper elbow/shoulder pose differences require visual interpretation; no length refit.',
        'All four limbs remain connected; front3/3 and rear3+1/3+1.'])
    save(out,'independent-02-A2-bones.png')
    out=canvas('torn-v3.png'); draw_bones(out,chains={'FU':chain_b3})
    draw=ImageDraw.Draw(out)
    joint(draw,project(REST['FU'][0]),RED,9,'body shoulder BREAK')
    joint(draw,project(chain_b3[0]),RED,9,'detached proximal BREAK')
    footer(out,'B3 / fixed-length 3D feasibility witness',[
        f'Observed top lengths: upper{records["B3"]["projectionUpperPx"]:.1f}px, forearm{records["B3"]["projectionForearmPx"]:.1f}px.',
        f'Source maxima: upper{LENGTHS["FU"][0]*SCALE:.1f}px, forearm{LENGTHS["FU"][1]*SCALE:.1f}px. Source lengths unchanged.',
        'Hand and 3 finger chains moved rigidly; altered angles/heights preserve source lengths.',
        'Retrospective feasibility only. Hidden heights, floor support and medical shape unproved.'])
    save(out,'independent-02-B3-bones.png')
    out=canvas('torn-v4.png'); draw_bones(out,omit=('FU',))
    draw=ImageDraw.Draw(out)
    obs=OBS['B4'];w=np.array(obs['wrist']);e=np.array(obs['elbow']);r=LENGTHS['FU'][1]*SCALE
    draw.ellipse((w[0]-r,w[1]-r,w[0]+r,w[1]+r),outline=GOLD,width=3)
    direction=(e-w)/np.linalg.norm(e-w);limit=w+direction*r
    draw.line([tuple(w),tuple(e)],fill=RED,width=4)
    draw.line([tuple(w),tuple(limit)],fill=GOLD,width=5)
    for p,name in [(w,'measured wrist'),(e,'measured elbow')]:
        draw.rectangle((p[0]-10,p[1]-10,p[0]+10,p[1]+10),outline=RED,width=2)
        joint(draw,p,RED,5,name)
    joint(draw,limit,GOLD,7,'MAX endpoint')
    footer(out,'B4 / BONE FAIL - exceeds any possible projection',[
        f'Observed wrist{tuple(int(x) for x in w)} to elbow{tuple(int(x) for x in e)}: {np.linalg.norm(e-w):.1f}px.',
        f'Unchanged true3D forearm0.729246unit x375 = MAX{r:.3f}px.',
        f'Excess{np.linalg.norm(e-w)-r:.1f}px. Even +/-10px X at both centres leaves282px.',
        'Gold circle/segment = independent maximum. Red endpoint outside. NO volume/ID pass.'])
    save(out,'independent-02-B4-max-length-FAIL.png')
    out=canvas('heap-v2.png');draw=ImageDraw.Draw(out)
    # Only the source distal forelimb chains remain traceable at nearly unchanged positions.
    for ident in ['FU','FL']:
        pts=project(REST[ident])
        draw.line([tuple(p) for p in pts[1:]],fill=CYAN,width=3)
        draw_digits(draw,ident)
        joint(draw,pts[1],RED,7,'proximal CHAIN DISRUPTED')
    for xy,label in [((895,292),'RU terminal:3+1'),((950,1030),'RL terminal:3+1'),((420,748),'one head; torso disrupted')]:
        text(draw,xy,label,CYAN,font=FONT)
    footer(out,'C2 / declared-break distal-part accounting',[
        'Two forehands3 each; two rear feet3+1 each; one head.',
        'No intact shoulder/hip/spine chains reconstructed through the meat heap.',
        'Source forearm/digit overlay only; rear-foot topology counted, exact joint pose unverified.',
        'Visible inventory GO; full hidden-anatomy / 22-intact-muscle GO is not claimed.'])
    save(out,'independent-02-C2-parts.png')
    # Stage 03/04 is gated by the actual independent visual bone review.
    if args.muscles_after_bone_review:
        for variant, asset in [('A2','corpse-v2.png'),('B3','torn-v3.png'),('C2','heap-v2.png')]:
            for stage, volumes in [('03-volumes',True),('04-ID',False)]:
                save(muscle_pass(asset,variant,chain_b3,volumes),f'independent-{stage}-{variant}.png')
        report['boneVisualReview'] = {
            'A2':'CONDITIONAL_GO_SOURCE_SCHEME_AND_VISIBLE_CHAINS',
            'B3':'GO_FIXED_LENGTH_FEASIBILITY_ONLY',
            'B4':'FAIL_NO_MUSCLE_PASS',
            'C2':'GO_VISIBLE_PART_ACCOUNTING_ONLY_NO_INTACT_HIDDEN_CHAINS'}
        report['musclePassGenerated'] = True
        report['musclePassVisualReview'] = 'PENDING'
        report['C2MusclePresentation'] = 'Separate actual damaged-parts panel and source-only master inventory panel; no intact muscle locations overlaid onto heap.'
        (QA/'independent-review.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
    print(json.dumps({'measurements':records,'boneDiagnosticsReady':True,'musclePassGenerated':args.muscles_after_bone_review}))


if __name__=='__main__':
    main()
