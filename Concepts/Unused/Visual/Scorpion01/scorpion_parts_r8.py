"""R8 reference-led heavy chela; one shared manus and one hinged opposing finger."""
import numpy as np
from PIL import Image, ImageDraw
import scorpion_parts_r5 as previous

ROOT = previous.ROOT
CHELA = {'wrist': [256, 430], 'palm': [250, 280],
         'fixedBase': [330, 210], 'fixedMid': [337, 160], 'fixedTip': [293, 170],
         'hinge': [172, 218], 'movingMid': [155, 156],
         'movingTip': [250.505532183, 114.508865968]}
CHELA_KEYS = previous.CHELA_KEYS
TAIL = previous.TAIL
RATIO = .00150
close_chela = previous.close_chela
layer = previous.layer


def update_plan(plan):
    previous.update_plan(plan)
    for side, sign in [('L', -1), ('R', 1)]:
        def local(key):
            x, y = CHELA[key]
            return [(-.34 + (x-256)*RATIO)*(-sign), .68+(430-y)*RATIO, .08]
        plan['pedipalps'][side] = {
            'arm': [[sign*.13, .36, .14], [sign*.30, .40, .15], local('wrist')],
            **{key: local(key) for key in CHELA if key != 'wrist'}}
    plan['changesFromR7'] = {
        'claws': '1.5x texture scale plus shorter finger geometry; manus soft volume widened another 22% across its middle. Same one-finger hinge construction.',
        'wrists': 'Original wrist and elbow positions; no extension of thin supporting arms.',
        'windupDegrees': 4,
        'preserved': 'All walking legs, body, tail, eyes, gait, strike timing and canvas/pivot.'}
    plan['chelaConstruction']['sourceGuidePixels'] = CHELA
    plan['chelaConstruction']['manusSoftVolume'] = {'transverseMiddleScale': 1.22, 'rangeY': [225, 415], 'wristAndFingerGuidesUnchanged': True}


def guides():
    image = Image.new('RGB', (512, 512), '#dfe4e3')
    d = ImageDraw.Draw(image)
    # Declared rounded manus and finger volumes, before texture generation.
    d.ellipse((60, 178, 425, 417), fill='#98b7b5', outline='#455a5e', width=3)
    d.polygon([(219, 390), (291, 390), (281, 452), (235, 452)], fill='#98b7b5')
    for keys in [['palm', 'fixedBase', 'fixedMid', 'fixedTip'], ['hinge', 'movingMid', 'movingTip']]:
        d.line([tuple(CHELA[k]) for k in keys], fill='#98b7b5', width=38)
    for keys in [['wrist', 'palm', 'fixedBase', 'fixedMid', 'fixedTip'], ['hinge', 'movingMid', 'movingTip']]:
        d.line([tuple(CHELA[k]) for k in keys], fill='#23476a', width=3)
    for x, y in CHELA.values():
        d.ellipse((x-4, y-4, x+4, y+4), fill='#f2d174', outline='#23476a')
    image.save(ROOT/'r8-chela-independent-guide.png')


def prepare(Surface, write):
    keys = list(CHELA)
    source = [[272, 449], [268, 307], [381, 222], [408, 156], [286, 94],
              [143, 232], [114, 123], [226, 26]]
    edges = [(0, 1), (1, 2), (2, 3), (3, 4), (1, 5), (5, 6), (6, 7)]
    image = Image.open(ROOT/'chela-r8-heavy-paint.png').convert('RGBA').resize((512, 512), Image.Resampling.LANCZOS)
    class HeavyManusSurface(Surface):
        def deform(self, target):
            points = super().deform(target)
            y = points[:, 1]
            weight = np.clip((y-225)/50, 0, 1) * np.clip((415-y)/50, 0, 1)
            weight = weight*weight*(3-2*weight)
            axis = np.interp(y, [225, 280, 430], [250, 250, 256])
            # Native soft-volume shaping; the wrist, hinge and finger pins stay fixed.
            points[:, 0] += .22*weight*(points[:, 0]-axis)
            return points
    baked, metrics = HeavyManusSurface(image, source, edges).render([CHELA[key] for key in keys])
    baked.crop((0, 0, 512, 512)).save(ROOT/'chela-source-r8.png')
    write(ROOT/'r8-native-registration.json', {
        'sourceUV': dict(zip(keys, source)), 'declaredGuide': CHELA,
        'linearWorldScaleRelativeToR7': 1.5, 'metrics': metrics,
        'scope': 'Texture registered into the predeclared guide; rig not fitted to generated pixels.'})


def chela_layers():
    # Use the existing deterministic split and common hinge sector with the new UV.
    image = Image.open(ROOT/'chela-source-r8.png').convert('RGBA').resize((512, 512), Image.Resampling.LANCZOS)
    pixels = np.asarray(image).copy()
    yy, xx = np.mgrid[:512, :512]
    xy = np.stack([xx, yy], axis=-1)
    def distance(keys):
        result = np.full((512, 512), 1e9)
        for ka, kb in zip(keys, keys[1:]):
            a, b = np.asarray(CHELA[ka]), np.asarray(CHELA[kb])
            delta = b-a
            q = np.clip(np.sum((xy-a)*delta, axis=-1)/np.dot(delta, delta), 0, 1)
            result = np.minimum(result, np.linalg.norm(xy-a-q[..., None]*delta, axis=-1))
        return result
    hinge = np.asarray(CHELA['hinge'])
    across = xx-hinge[0]
    # An oblique curved insertion keeps the actual upper palm intact when the
    # finger closes. The old horizontal ownership cutoff exposed a square notch.
    insertion = hinge[1] - .45*across - .004*across*across
    branch = (distance(['hinge', 'movingMid', 'movingTip']) < distance(['wrist', 'palm', 'fixedBase', 'fixedMid', 'fixedTip'])) & (yy < insertion)
    radius = np.linalg.norm(xy-hinge, axis=-1)
    overlap = radius < 25
    jaw = pixels.copy()
    jaw[~(branch | overlap), 3] = 0
    pixels[branch & ~overlap, 3] = 0
    cap = np.asarray(image).copy()
    cap[radius > 25, 3] = 0
    return Image.fromarray(pixels), Image.fromarray(jaw), Image.fromarray(cap)


def render_chela(palp, side, project, layers):
    main, jaw, cap = layers
    body = layer(main, CHELA['wrist'], CHELA['fixedTip'], project(palp['arm'][-1]), project(palp['fixedTip']), side == 'R')
    moving = layer(jaw, CHELA['hinge'], CHELA['movingTip'], project(palp['hinge']), project(palp['movingTip']), side == 'R')
    moving.alpha_composite(body)
    moving.alpha_composite(layer(cap, CHELA['wrist'], CHELA['fixedTip'], project(palp['arm'][-1]), project(palp['fixedTip']), side == 'R'))
    return moving


if __name__ == '__main__':
    guides()
