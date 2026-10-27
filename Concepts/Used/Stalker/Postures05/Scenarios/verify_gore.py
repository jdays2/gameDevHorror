"""Read actual Kill/Gore captures; preserve first-frame handoff evidence."""
import argparse
import json
from pathlib import Path

from PIL import Image, ImageDraw

BASE = Path(__file__).resolve().parent


def verify(run, require_hold=False):
    folder = BASE / run
    setup = json.loads((folder / 'run.json').read_text(encoding='utf-8-sig'))
    failures = []
    if not setup['completedAll'] or setup['captureErrors']:
        failures.append({'captureSetup': setup['captureErrors'], 'completedAll': setup['completedAll']})
    reviews = []
    for path in sorted(folder.glob('*/manifest.json')):
        manifest = json.loads(path.read_text(encoding='utf-8-sig'))
        frames = manifest['frames']
        first = next(index for index, frame in enumerate(frames) if frame['state'] == 'Dead')
        prior = frames[first - 1]
        death = frames[first:]
        visible = [frame for frame in death if frame['bodyEnabled']]
        final = death[-1]
        changed = [frame['frame'] for frame in visible if frame['body'] != prior['body']]
        hidden = next((frame['local'] for frame in death if not frame['bodyEnabled']), None)
        record = {
            'actor': path.parent.name, 'priorState': prior['state'], 'priorSprite': prior['body'],
            'priorClosure': prior['closure'], 'priorDisplayedFold': prior['fold'],
            'deathFirstSprite': death[0]['body'], 'visibleDeadSamples': [frame['local'] for frame in visible],
            'bodyHiddenLocalSample': hidden, 'preRuptureBodyChanges': changed,
            'liveBodyPreservedUntilReplacement': not changed,
            'killCalls': final['killCalls'], 'deathEvents': final['deathEvents'],
            'remainsRoots': final['remainsRoots'], 'sceneOverrides': manifest['authoredOverrides'],
        }
        reviews.append(record)
        if final['killCalls'] != 2 or final['deathEvents'] != 1 or final['remainsRoots'] != 1 or hidden is None:
            failures.append({'actor': path.parent.name, 'killOrReplacementContract': record})
        if require_hold and changed:
            failures.append({'actor': path.parent.name, 'lastLiveSpriteNotPreserved': changed})
        selected = [prior] + death[:9] + [death[15], death[-1]]
        sheet = Image.new('RGB', (6 * 320, 2 * 350), '#00ff00')
        draw = ImageDraw.Draw(sheet)
        for position, frame in enumerate(selected):
            source = path.parent / f'frame-{frame["frame"]:05d}.png'
            with Image.open(source) as image:
                image = image.convert('RGB').resize((320, 320), Image.Resampling.LANCZOS)
            x, y = position % 6 * 320, position // 6 * 350
            sheet.paste(image, (x, y + 25))
            draw.text((x + 4, y + 4), f'{frame["state"]} {frame["local"]} {frame["body"]} visible={frame["bodyEnabled"]}', fill='black')
        sheet.save(path.parent / 'death-first-frames.jpg', quality=94, subsampling=0)
    report = {'run': run, 'sources': len(reviews), 'captureErrors': setup['captureErrors'],
        'requireLastLiveFrameHold': require_hold, 'pass': not failures, 'failures': failures, 'reviews': reviews}
    (folder / 'actual-gore-review.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
    if failures:
        raise AssertionError(f'Actual Kill/Gore contract failed in {run}: {len(failures)} findings')
    return {'run': run, 'sources': len(reviews), 'captureErrors': len(setup['captureErrors']),
        'lastLiveHoldFailures': sum(bool(review['preRuptureBodyChanges']) for review in reviews),
        'requireLastLiveFrameHold': require_hold, 'pass': True}


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('run', nargs='?', default='gore-final')
    parser.add_argument('--require-hold', action='store_true')
    args = parser.parse_args()
    print(json.dumps(verify(args.run, args.require_hold), indent=2))
