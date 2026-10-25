"""Technical export of WHOLE equal sprite cells; never edits/warps body parts."""
import hashlib
import json
import sys
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parent
STATE = sys.argv[1]
SOURCE = ROOT / sys.argv[2]
if STATE not in ('Run01', 'Attack01'):
    raise ValueError('Expected Run01 or Attack01')
art = Image.open(SOURCE).convert('RGBA')
if art.width % 3 or art.height % 2:
    raise ValueError(f'Sheet is not a 3x2 integer grid: {art.size}')
width, height = art.width // 3, art.height // 2
if width != height:
    raise ValueError(f'Cells are not square: {width}x{height}')
dest = ROOT / STATE
delays = [90] * 6 if STATE == 'Run01' else [120, 120, 80, 80, 120, 140]
frames, report = [], []
for i in range(6):
    col, row = i % 3, i // 3
    box = (col * width, row * height, (col + 1) * width, (row + 1) * height)
    cell = art.crop(box)
    alpha = cell.getchannel('A')
    bounds = alpha.point(lambda a: 255 if a > 16 else 0).getbbox()
    border = sum(a > 16 for edge in [
        alpha.crop((0, 0, width, 1)), alpha.crop((0, height - 1, width, height)),
        alpha.crop((0, 0, 1, height)), alpha.crop((width - 1, 0, width, height))
    ] for a in edge.get_flattened_data())
    if bounds is None or border:
        raise ValueError(f'Frame {i}: empty or cell-edge clipping ({border} pixels), bbox={bounds}')
    target = dest / 'Frames' / f'{STATE.lower()}-{i:02d}.png'
    report.append({'index': i, 'file': str(target.relative_to(ROOT)).replace('\\', '/'),
                   'sourceRect': list(box), 'boundsAlphaGt16': list(bounds), 'edgePixels': border,
                   'durationMs': delays[i]})
    frames.append(cell)

# Do not partially overwrite a previous preview when a later cell fails validation.
(dest / 'Frames').mkdir(parents=True, exist_ok=True)
for cell, item in zip(frames, report):
    target = ROOT / item['file']
    cell.save(target)
    item['sha256'] = hashlib.sha256(target.read_bytes()).hexdigest()

def flatten(cell, color):
    bg = Image.new('RGBA', cell.size, color)
    bg.alpha_composite(cell)
    return bg.convert('RGB')

for label, color in [('dark', '#282b30'), ('light', '#e8e7e3')]:
    bg = Image.new('RGBA', art.size, color)
    bg.alpha_composite(art)
    bg.convert('RGB').save(dest / f'sheet-{label}.png')

# One shared GIF palette avoids frame-by-frame palette flicker. GIF is review-only.
rgb_frames = [flatten(f, '#282b30') for f in frames]
palette_source = Image.new('RGB', (width, height * 6))
for i, f in enumerate(rgb_frames):
    palette_source.paste(f, (0, i * height))
palette = palette_source.quantize(colors=256)
gif_frames = [f.quantize(palette=palette) for f in rgb_frames]
preview_delays = list(delays)
if STATE == 'Attack01':
    preview_delays[-1] += 600  # Display pause only, not a gameplay attack cooldown.
gif_path = dest / 'preview.gif'
gif_frames[0].save(gif_path, save_all=True, append_images=gif_frames[1:],
                   duration=preview_delays, loop=0, disposal=2, optimize=False)
gif = Image.open(gif_path)
if gif.n_frames != 6:
    raise ValueError(f'GIF collapsed to {gif.n_frames} frames; inspect repeated artwork')
manifest = {
    'task': 'TASK-005', 'state': STATE, 'source': SOURCE.name, 'grid': [3, 2],
    'qualityStatus': 'NOT_READY_REVIEW_ONLY; art/bone validation is separate and these generated trials failed.',
    'frameSize': [width, height], 'frameCount': 6, 'order': 'row-major',
    'pivotPixels': [298.56 * width / 512, 268.16 * height / 512],
    'pivotNormalizedTopLeft': [298.56 / 512, 268.16 / 512],
    'sourceToGuideCell': {'scale': 0.32, 'translate': [55.36, 55.36]},
    'loop': STATE == 'Run01', 'durationMs': delays, 'previewDurationMs': preview_delays,
    'processing': 'Whole equal cell crop only. No per-frame auto-crop/alignment/scale, no body-part assembly or art deformation.',
    'reviewOnly': 'GIF is flattened and palette-quantized; transparent PNG cells preserve RGBA. Attack preview adds a pause after recovery.',
    'anatomyScope': 'Technical framing checks are separate from bone-first and muscle/identity validation.',
    'frames': report
}
(dest / 'animation.json').write_text(json.dumps(manifest, indent=2) + '\n', encoding='utf8')
print(json.dumps({'state': STATE, 'size': art.size, 'frames': len(frames),
                  'cell': [width, height], 'edgePixels': [r['edgePixels'] for r in report],
                  'gifFrames': gif.n_frames, 'preview': str(gif_path)}))
