"""Independent prospective arthropod rig; diagnostic drawings, not creature artwork."""
from pathlib import Path
import json
import math
import sys
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parent
SIZE = 1024
SCALE = 340.0
CENTER = (512.0, 548.0)
FONT = ImageFont.truetype('C:/Windows/Fonts/arial.ttf', 17)
SMALL = ImageFont.truetype('C:/Windows/Fonts/arial.ttf', 14)


def make_plan():
    legs = {}
    rows = [(.26, .48, .47, .69, .71), (.10, .58, .22, .80, .24),
            (-.08, .57, -.18, .78, -.35), (-.24, .46, -.46, .63, -.71)]
    for side, sign in [('L', -1), ('R', 1)]:
        for index, (root_y, knee_x, knee_y, foot_x, foot_y) in enumerate(rows):
            legs[f'{side}{index + 1}'] = [[sign * .20, root_y, .13],
                                          [sign * knee_x, knee_y, .20],
                                          [sign * foot_x, foot_y, .015]]
    palps = {}
    for side, sign in [('L', -1), ('R', 1)]:
        palps[side] = {'arm': [[sign * .13, .36, .14], [sign * .35, .70, .15],
                              [sign * .55, .93, .08]],
                       'hinge': [sign * .57, 1.04, .08],
                       'fixedTip': [sign * .50, 1.27, .05],
                       'movingTip': [sign * .72, 1.23, .05]}
    return {'revision': 'scorpion-anatomy-r1', 'task': 'TASK-005',
            'scope': 'Separate fast group enemy art family; mechanics remain a design brief.',
            'units': 'prospective local metres, not recovered measurements',
            'axes': {'x': 'right', 'y': 'forward', 'z': 'height above ground'},
            'groundZ': 0, 'soleThickness': .015, 'bodyHeight': .13,
            'projection': {'canvas': [SIZE, SIZE], 'pixelsPerUnit': SCALE,
                           'originPixelsTopLeft': CENTER, 'camera': 'orthographic dorsal'},
            'bodyAxis': [[0, .46, .14], [0, .13, .13], [0, -.26, .11], [0, -.46, .10]],
            'walkingLegs': legs, 'pedipalps': palps,
            'tail': [[0, -.46, .10], [0, -.66, .09], [.05, -.84, .10],
                     [.20, -.99, .12], [.38, -.96, .15], [.47, -.80, .18], [.39, -.67, .17]],
            'counts': {'walkingLegs': 8, 'majorLinksPerWalkingLeg': 2,
                       'pedipalps': 2, 'pincerFingersPerPedipalp': 2,
                       'tailLinksIncludingSting': 6, 'primaryEyes': 1},
            'support': 'Eight ground contacts in neutral; alternating tetrapods in fast gait. Palps carry no locomotion load.',
            'symmetry': 'Walking legs and palps mirror anatomically. Tail has a declared lateral curve.',
            'surface': 'Stalker pale pink flesh, broad simple volumes, ivory bone tips; no glossy human shoulders.',
            'artisticApproval': False}


def point(p):
    return (CENTER[0] + SCALE * p[0], CENTER[1] - SCALE * p[1])


def chains(plan):
    yield 'axis', plan['bodyAxis'], '#e6b773'
    for ident, chain in plan['walkingLegs'].items():
        yield ident, chain, '#5cd5d0'
    for ident, palp in plan['pedipalps'].items():
        yield ident + 'P', palp['arm'] + [palp['hinge']], '#dfb0f0'
        yield ident + 'F', [palp['hinge'], palp['fixedTip']], '#dfb0f0'
        yield ident + 'M', [palp['hinge'], palp['movingTip']], '#dfb0f0'
    yield 'tail', plan['tail'], '#e6b773'


def top(plan):
    image = Image.new('RGB', (SIZE, SIZE), '#242a30')
    d = ImageDraw.Draw(image)
    d.text((28, 20), 'SCORPION / '+plan['revision']+' / dorsal orthographic', font=FONT, fill='white')
    d.text((28, 46), '8 walking legs + 2 separate pincers + 1 articulated tail; front points UP', font=SMALL, fill='#b8c4cc')
    outline = [[0, .49, .13], [-.24, .32, .13], [-.23, -.20, .13],
               [-.15, -.43, .13], [0, -.49, .13], [.15, -.43, .13],
               [.23, -.20, .13], [.24, .32, .13], [0, .49, .13]]
    d.line([point(p) for p in outline], fill='#65717a', width=2)
    for ident, chain, color in chains(plan):
        pts = [point(p) for p in chain]
        d.line(pts, fill=color, width=5)
        for x, y in pts:
            d.ellipse((x-5, y-5, x+5, y+5), fill=color, outline='#11171b', width=1)
        if ident in plan['walkingLegs']:
            x, y = pts[1]
            d.text((x+9, y-8), ident, font=FONT, fill='white')
    for eye in plan.get('eyePositions',[[0,.20,.13]]):
        x,y=point(eye)
        radius=8 if 'eyePositions' in plan else 27
        d.ellipse((x-radius,y-radius,x+radius,y+radius),outline='#dfe4e5',width=2)
    d.text((28, 964), 'Geometry is a declared design. Hidden anatomy is not inferred from a bitmap.', font=SMALL, fill='#b8c4cc')
    return image


def projections(plan):
    image = Image.new('RGB', (1260, 840), '#242a30')
    d = ImageDraw.Draw(image)
    views = [('TOP x/y', (0, 1), (315, 315), 190, -1),
             ('SIDE y/z', (1, 2), (945, 310), 190, -1),
             ('FRONT x/z', (0, 2), (315, 700), 190, -1)]
    for label, axes, origin, scale, vertical in views:
        d.text((origin[0]-260, origin[1]-245), label, fill='white', font=FONT)
        if axes[1] == 2:
            d.line((origin[0]-270, origin[1], origin[0]+270, origin[1]), fill='#69747b', width=2)
        for ident, chain, color in chains(plan):
            pts = [(origin[0]+p[axes[0]]*scale, origin[1]+p[axes[1]]*scale*vertical) for p in chain]
            d.line(pts, fill=color, width=3)
            for x, y in pts:
                d.ellipse((x-3,y-3,x+3,y+3), fill=color)
    origin = (945, 690)
    d.text((680, 455), 'THREE QUARTER / diagnostic only', fill='white', font=FONT)
    for _, chain, color in chains(plan):
        pts = [(origin[0]+(p[0]-.45*p[1])*170,
                origin[1]-(p[2]+.36*p[1]) * 260) for p in chain]
        d.line(pts, fill=color, width=3)
    return image


def construction(plan):
    """A rigid bone maquette, with no muscle or skin pass inferred from failed art."""
    image = Image.new('RGB',(SIZE,SIZE),'white')
    d = ImageDraw.Draw(image)
    outline = [[0,.49,.13],[-.24,.32,.13],[-.23,-.20,.13],[-.15,-.43,.13],
               [0,-.49,.13],[.15,-.43,.13],[.23,-.20,.13],[.24,.32,.13],[0,.49,.13]]
    d.polygon([point(p) for p in outline],fill='#c3d9e4',outline='#385d78',width=3)
    for ident,chain,color in chains(plan):
        if ident=='axis':
            continue
        pts = [point(p) for p in chain]
        thickness = 14 if ident in plan['walkingLegs'] else 21
        if ident.endswith(('F','M')):
            thickness = 17
        d.line(pts,fill='#78a8be',width=thickness)
        for x,y in pts[:-1]:
            radius = thickness*.65
            d.ellipse((x-radius,y-radius,x+radius,y+radius),fill='#9bc4d7',outline='#385d78',width=2)
        d.line(pts,fill='#385d78',width=2)
    x,y=point([0,.20,.13])
    d.ellipse((x-27,y-27,x+27,y+27),fill='#ecf0f2',outline='#385d78',width=2)
    d.ellipse((x-11,y-11,x+11,y+11),fill='#385d78')
    return image


def validate(plan):
    pairs = []
    for number in range(1,5):
        for a,b in zip(plan['walkingLegs'][f'L{number}'], plan['walkingLegs'][f'R{number}']):
            pairs.append(max(abs(a[0]+b[0]), abs(a[1]-b[1]), abs(a[2]-b[2])))
    lengths = {key: [math.dist(a,b) for a,b in zip(chain,chain[1:])]
               for key,chain in plan['walkingLegs'].items()}
    return {'revision':plan['revision'], 'symmetryError':max(pairs),
            'legLengths':lengths, 'groundContacts':len(plan['walkingLegs']),
            'contactSurfaceZ':0, 'footBoneZ':.015, 'soleThickness':.015,
            'allFeetTouchGround':all(abs(c[-1][2]-.015)<1e-9 for c in plan['walkingLegs'].values()),
            'note':'Numeric geometry gate only. Inspect projections and the eventual artwork separately.'}


def overlay(plan, source, output):
    art = Image.open(source).convert('RGBA')
    scale = art.width / SIZE
    canvas = Image.new('RGBA', art.size, '#272d33')
    canvas.alpha_composite(art)
    d = ImageDraw.Draw(canvas)
    for ident, chain, color in chains(plan):
        pts = [(point(p)[0]*scale, point(p)[1]*scale) for p in chain]
        d.line(pts,fill=color,width=3)
        for x,y in pts:
            d.ellipse((x-6,y-6,x+6,y+6),outline='white',width=2)
        if ident in plan['walkingLegs']:
            d.text((pts[1][0]+8,pts[1][1]),ident,font=FONT,fill='white')
    canvas.convert('RGB').save(output,quality=94)
    print(json.dumps({'image':str(source),'size':art.size,
                      'registration':{'uniformScale':scale,'offset':[0,0]},
                      'noJointAdjustment':True}),flush=True)


if __name__ == '__main__':
    ROOT.mkdir(parents=True, exist_ok=True)
    plan = make_plan()
    if '--overlay' in sys.argv:
        source = ROOT / sys.argv[sys.argv.index('--overlay')+1]
        overlay(plan,source,source.with_name(source.stem+'-bones.jpg'))
        raise SystemExit(0)
    (ROOT/'anatomy-plan.json').write_text(json.dumps(plan,indent=2),encoding='utf-8')
    (ROOT/'anatomy-validation.json').write_text(json.dumps(validate(plan),indent=2),encoding='utf-8')
    top(plan).save(ROOT/'01-independent-skeleton.png')
    projections(plan).save(ROOT/'01-skeleton-projections.jpg',quality=92)
    construction(plan).save(ROOT/'01-rigid-maquette.png')
    print(json.dumps(validate(plan)),flush=True)
