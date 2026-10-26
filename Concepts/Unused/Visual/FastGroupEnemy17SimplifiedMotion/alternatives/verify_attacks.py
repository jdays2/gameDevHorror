"""Inspect actual alternate-attack exports and preserve independent QA evidence."""
import hashlib
import json
import sys
import numpy as np
from PIL import Image, ImageDraw, ImageFont
import build_attacks as attacks
import verify_motions as shared

ROOT = attacks.ROOT
base = attacks.base


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def inspect(state, sample=False):
    folder = ROOT/state
    plan = json.loads((folder/'motion-plan.json').read_text(encoding='utf-8'))
    selected = attacks.CONFIG[state]['samples'] if sample else range(plan['frameCount'])
    images = []
    records = []
    lengths = []
    rear_motion = []
    support_counts = []
    for index in selected:
        pose = plan['frames'][index]
        path = folder/'frames'/f'{state}-{index:02d}.png'
        image = Image.open(path).convert('RGBA')
        alpha = np.asarray(image.getchannel('A'))
        stats = json.loads((folder/'measurements'/f'{index:02d}.json').read_text(encoding='utf-8'))
        assert stats['imageSha256']==sha(path)
        assert stats['fingerprint']==attacks.fingerprint(pose)
        edge = int(max(alpha[0].max(), alpha[-1].max(), alpha[:, 0].max(), alpha[:, -1].max()))
        assert image.size==(1024, 1024) and edge==0
        assert stats['flippedTriangles']==0
        # The unchanged master contains isolated alpha=1 fringe specks. Keep
        # them in full PNGs, while rejecting any visible clipping in the crop.
        crop_alpha = int(max(alpha[:190].max(), alpha[840:].max()))
        assert crop_alpha<=1
        for ident, limb in pose['limbs'].items():
            actual = np.linalg.norm(np.diff(limb['joints3D'], axis=0), axis=1)
            expected = np.linalg.norm(np.diff(base.REST[ident], axis=0), axis=1)
            lengths.extend(abs(actual-expected).tolist())
            if ident[0]=='R' or (state=='swipe' and ident=='FL'):
                rear_motion.append(float(np.max(abs(np.array(limb['joints3D'][3])-plan['frames'][0]['limbs'][ident]['joints3D'][3]))))
        support_counts.append(sum(limb['state']=='brace' for limb in pose['limbs'].values()))
        records.append({'index': index, 'sha256': sha(path), 'bbox': image.getbbox(), 'edgeAlphaMax': edge,
                        'previewCropExcludedAlphaMax': crop_alpha,
                        'flippedTriangles': stats['flippedTriangles'], 'minTriangleAreaRatio': stats['minTriangleAreaRatio']})
        images.append(image)
    assert max(lengths)<1e-10 and max(rear_motion)==0.
    # Separate muscle volume and ID/attachment passes. These are prospective
    # functional zones, not medical measurements of hidden tissues.
    for suffix, identifiers in [('03-volumes', False), ('04-muscle-ids', True)]:
        overlays = [shared.muscle_overlay(im, plan['frames'][i], identifiers) for i, im in zip(selected, images)]
        base.contact_sheet(overlays, folder/f'{suffix}{"-samples" if sample else ""}.jpg')
        for index, overlay in zip(selected, overlays):
            if index in attacks.CONFIG[state]['samples']:
                overlay.save(folder/f'{suffix}-{index:02d}.png')
    base.contact_sheet(images, folder/f'{"samples" if sample else "frames"}-light.jpg', bg='#ebeae6')
    base.contact_sheet(images, folder/f'{"samples" if sample else "frames"}-dark.jpg')
    if sample:
        print(state, 'samples technical PASS', flush=True)
        return
    first = folder/'frames'/f'{state}-00.png'
    last = folder/'frames'/f'{state}-{plan["frameCount"]-1:02d}.png'
    assert sha(first)==sha(last)==sha(attacks.PARENT/'attack'/'frames'/'attack-00.png')
    apng = Image.open(folder/f'{state}-preview.png')
    assert apng.n_frames==plan['frameCount'] and apng.info['loop']==1
    for index, (image, duration) in enumerate(zip(images, plan['durationsMs'])):
        apng.seek(index)
        assert abs(apng.info['duration']-duration)<.01
        assert np.array_equal(np.asarray(image), np.asarray(apng.convert('RGBA')))
    palettes = {}
    for background in ['dark', 'light']:
        gif = Image.open(folder/f'{state}-{background}.gif')
        assert gif.n_frames==plan['frameCount']
        palettes[background] = hashlib.sha256(bytes(gif.getpalette())).hexdigest()
        for index, duration in enumerate(plan['durationsMs']):
            gif.seek(index)
            assert gif.info['duration']==duration
    registered = [shared.registered(im, p) for im, p in zip(images, plan['frames'])]
    brightness = {}
    for name, box in {'neck': (600, 610, 640, 710), 'back': (790, 615, 930, 705)}.items():
        values = [float(np.mean(np.asarray(im.crop(box))[..., :3]@np.array([.2126, .7152, .0722]))) for im in registered]
        brightness[name] = {'range255': max(values)-min(values)}
    report = {'task': 'TASK-005', 'state': state, 'revision': 'attack-alternatives-r1', 'technicalExport': 'PASS',
              'masterSha256': sha(attacks.MASTER), 'frameCount': plan['frameCount'], 'durationMs': plan['durationMs'],
              'maxBoneLengthError3D': max(lengths), 'maxBraceDisplacement': max(rear_motion),
              'supportingFeet': [min(support_counts), max(support_counts)],
              'firstLastAndLikedNeutralIdentical': True, 'allApngFramesMatchPng': True,
              'gifPaletteHashes': palettes, 'registeredBrightness': brightness, 'frames': records,
              'muscleGroupCount': len(shared.GROUPS), 'artisticApproval': False, 'unityImport': False,
              'authoringReview': 'not applicable; no scene objects changed',
              'limitations': ['Contact heights are designed trajectories, not a physics simulation.',
                              '22 functional aggregates are not a medical atlas.', 'No gameplay damage events or Unity state transitions.']}
    (folder/'technical-validation.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
    print(state, 'full technical PASS', plan['frameCount'], plan['durationMs'], brightness, flush=True)


def review_clip():
    palette_source = Image.new('RGBA', base.ART.size, '#262b31')
    palette_source.alpha_composite(base.ART)
    palette = palette_source.convert('RGB').quantize(colors=256)
    title_font = ImageFont.truetype('C:/Windows/Fonts/arialbd.ttf', 22)
    frames = []
    durations = []
    manifest = {'task': 'TASK-005', 'revision': 'attack-alternatives-r1', 'master': '../simplified-master-v1.png',
                'masterSha256': sha(attacks.MASTER), 'ownerLikedOriginalAnimations': True,
                'alternativeArtisticApproval': False, 'alternatives': {}}
    for label, (state, config) in zip(['A', 'B', 'C'], attacks.CONFIG.items()):
        report = json.loads((ROOT/state/'technical-validation.json').read_text(encoding='utf-8'))
        for background in ['dark', 'light']:
            assert report['gifPaletteHashes'][background]==json.loads((ROOT/'swipe'/'technical-validation.json').read_text(encoding='utf-8'))['gifPaletteHashes'][background]
        plan = json.loads((ROOT/state/'motion-plan.json').read_text(encoding='utf-8'))
        prepared = []
        for index in range(plan['frameCount']):
            raw = Image.open(ROOT/state/'frames'/f'{state}-{index:02d}.png').convert('RGBA')
            background = Image.new('RGBA', raw.size, '#262b31')
            background.alpha_composite(raw)
            image = Image.new('RGB', (820, 568), '#262b31')
            image.paste(background.crop((0, 190, 1024, 840)).resize((820, 520), Image.Resampling.LANCZOS).convert('RGB'), (0, 48))
            ImageDraw.Draw(image).text((20, 12), f'{label} · {config["name"]}', font=title_font, fill='#f0e4d1')
            prepared.append(image.quantize(palette=palette, dither=Image.Dither.NONE))
        frames.extend(prepared*2)
        durations.extend(plan['durationsMs']*2)
        manifest['alternatives'][state] = {'label': label, 'name': config['name'], 'frameCount': plan['frameCount'],
                                         'durationMs': plan['durationMs'], 'plan': f'{state}/motion-plan.json',
                                         'preview': f'{state}/{state}-dark.gif', 'loop': False}
    frames[0].save(ROOT/'attack-choices.gif', save_all=True, append_images=frames[1:], duration=durations, loop=0, disposal=2)
    (ROOT/'animation-set.json').write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding='utf-8')
    hashes = {str(path.relative_to(ROOT)).replace('\\', '/'): sha(path) for path in ROOT.rglob('*')
              if path.is_file() and path.suffix in ['.png', '.gif', '.jpg']}
    (ROOT/'art-sha256.json').write_text(json.dumps(hashes, indent=2), encoding='utf-8')


if __name__=='__main__':
    states = [arg for arg in sys.argv[1:] if not arg.startswith('--')] or list(attacks.CONFIG)
    for state in states:
        inspect(state, '--sample' in sys.argv)
    if '--sample' not in sys.argv and set(states)==set(attacks.CONFIG):
        review_clip()
