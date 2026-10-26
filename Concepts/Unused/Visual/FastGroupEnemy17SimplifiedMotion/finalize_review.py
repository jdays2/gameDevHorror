"""Validate decoded previews against the exported PNGs; package a review clip."""
from pathlib import Path
import hashlib
import json
import numpy as np
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parent
STATES = ['walk', 'attack', 'run']


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    source = ROOT.parent/'FastGroupEnemy13AssetInspired'/'base-v3.png'
    assert digest(source)=='f5294cae2ed443b79dfb7aa26a783c2a8e49bf3b68826bbe5d52921f0d167b3a'
    reports = []
    plans = {}
    shared_palettes = {}
    for state in STATES:
        folder = ROOT/state
        plan = json.loads((folder/'motion-plan.json').read_text(encoding='utf-8'))
        plans[state] = plan
        assert plan['masterSha256']==digest(ROOT/'simplified-master-v1.png')
        apng = Image.open(folder/f'{state}-preview.png')
        assert apng.n_frames==plan['frameCount']
        assert apng.info['loop']==(0 if plan['loop'] else 1)
        for index, duration in enumerate(plan['durationsMs']):
            apng.seek(index)
            frame = Image.open(folder/'frames'/f'{state}-{index:02d}.png').convert('RGBA')
            assert abs(apng.info['duration']-duration)<.01
            assert np.array_equal(np.asarray(apng.convert('RGBA')), np.asarray(frame))
        for background in ['dark', 'light']:
            gif = Image.open(folder/f'{state}-{background}.gif')
            assert gif.n_frames==plan['frameCount']
            palette = bytes(gif.getpalette())
            if background in shared_palettes:
                assert shared_palettes[background]==palette
            shared_palettes[background] = palette
            for index, duration in enumerate(plan['durationsMs']):
                gif.seek(index)
                assert gif.info['duration']==duration
        reports.append({'state': state, 'decodedApngMatchesEveryPng': True,
                        'apngAndGifDurationsMatchPlan': True, 'frameCount': plan['frameCount'],
                        'durationMs': plan['durationMs'], 'apngLoopCount': 0 if plan['loop'] else 1})
    report = {'result': 'PASS', 'states': reports, 'gifPalettesSharedAcrossStates': True,
              'source13Unchanged': True, 'artisticApproval': False}
    (ROOT/'preview-validation.json').write_text(json.dumps(report, indent=2), encoding='utf-8')

    # A labelled convenience clip. Exact transparent frames and timing remain
    # available separately in the viewer, APNGs and per-state manifests.
    master = Image.open(ROOT/'simplified-master-v1.png').convert('RGBA')
    palette_base = Image.new('RGBA', master.size, '#262b31')
    palette_base.alpha_composite(master)
    palette = palette_base.convert('RGB').quantize(colors=256)
    frames = []
    durations = []
    title_font = ImageFont.truetype('C:/Windows/Fonts/arialbd.ttf', 22)
    names = {'walk': 'Ходьба', 'attack': 'Атака', 'run': 'Бег'}
    for state, repetitions in [('walk', 2), ('attack', 2), ('run', 4)]:
        images = []
        plan = plans[state]
        for index in range(plan['frameCount']):
            raw = Image.open(ROOT/state/'frames'/f'{state}-{index:02d}.png').convert('RGBA')
            base = Image.new('RGBA', raw.size, '#262b31')
            base.alpha_composite(raw)
            result = Image.new('RGB', (820, 568), '#262b31')
            result.paste(base.crop((0, 190, 1024, 840)).resize((820, 520), Image.Resampling.LANCZOS).convert('RGB'), (0, 48))
            ImageDraw.Draw(result).text((20, 12), f'TASK-005 · {names[state]} · {plan["durationMs"]} мс', font=title_font, fill='#f0e4d1')
            images.append(result.quantize(palette=palette, dither=Image.Dither.NONE))
        frames.extend(images*repetitions)
        durations.extend(plan['durationsMs']*repetitions)
    frames[0].save(ROOT/'motion-review.gif', save_all=True, append_images=frames[1:], duration=durations, loop=0, disposal=2)

    animation_set = {'task': 'TASK-005', 'revision': 'simplified-motion-r1',
                     'canvas': [1024, 1024], 'direction': 'left', 'camera': 'strict overhead',
                     'pivotPixelsTopLeft': [602.2, 511.8],
                     'pivotNormalizedBottomLeft': [602.2/1024., 1.-511.8/1024.],
                     'master': 'simplified-master-v1.png', 'masterSha256': digest(ROOT/'simplified-master-v1.png'),
                     'states': {}, 'unityImport': False, 'artisticApproval': False}
    for state, plan in plans.items():
        animation_set['states'][state] = {'frameCount': plan['frameCount'], 'durationsMs': plan['durationsMs'],
                                         'durationMs': plan['durationMs'], 'loop': plan['loop'],
                                         'frames': [f'{state}/frames/{state}-{i:02d}.png' for i in range(plan['frameCount'])],
                                         'sheet': f'{state}/{state}-sheet.png', 'sheetColumns': 4,
                                         'plan': f'{state}/motion-plan.json', 'validation': f'{state}/technical-validation.json'}
    (ROOT/'animation-set.json').write_text(json.dumps(animation_set, ensure_ascii=False, indent=2), encoding='utf-8')
    hashes = {str(path.relative_to(ROOT)).replace('\\', '/'): digest(path)
              for path in ROOT.rglob('*') if path.is_file() and path.suffix.lower() in ['.png', '.gif', '.jpg']}
    (ROOT/'art-sha256.json').write_text(json.dumps(hashes, indent=2), encoding='utf-8')
    print(json.dumps(report))


if __name__=='__main__':
    main()
