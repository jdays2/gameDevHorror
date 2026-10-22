"""Encode camera-rendered Unity frames and captured stereo main mix; no fake sound montage."""
import argparse
import json
from pathlib import Path
import subprocess
import numpy as np

parser = argparse.ArgumentParser()
parser.add_argument('--ffmpeg', required=True)
parser.add_argument('--input', type=Path, required=True)
parser.add_argument('--output', type=Path, required=True)
args = parser.parse_args()
args.output.mkdir(parents=True, exist_ok=True)
metrics = {}
for version in ('before', 'after'):
    source = args.input / version
    report = json.loads((source / 'capture.json').read_text())
    audio = np.fromfile(source / 'mix.f32', dtype='<f4')
    metrics[version] = {'peak': float(np.abs(audio).max()), 'clipped_samples': int((np.abs(audio) >= 1).sum()),
                        'frames': report['frames'], 'shots': report['shots'], 'audio_samples': len(audio)}
    title = 'BEFORE' if version == 'before' else 'AFTER - MUZZLE HAZE / IMPACTS 02'
    labels = [('PISTOL - CONCRETE', 0, 2), ('PISTOL - WOOD', 2, 4), ('PISTOL - FLESH', 4, 6),
              ('SHOTGUN - CONCRETE', 6, 8), ('SHOTGUN - WOOD', 8, 10), ('SHOTGUN - FLESH', 10, 13)]
    font = 'C\\:/Windows/Fonts/arial.ttf'
    filters = [f"drawtext=fontfile='{font}':text='{title}':x=24:y=22:fontsize=26:fontcolor=white:box=1:boxcolor=black@0.65"]
    for label, start, end in labels:
        filters.append(f"drawtext=fontfile='{font}':text='{label}':x=24:y=60:fontsize=20:fontcolor=white:enable='between(t,{start},{end})'")
    subprocess.run([args.ffmpeg, '-y', '-v', 'error', '-framerate', '30', '-i', str(source / '%04d.png'),
                    '-f', 'f32le', '-ar', str(report['sampleRate']), '-ac', '2', '-i', str(source / 'mix.f32'),
                    '-vf', ','.join(filters), '-c:v', 'libx264', '-crf', '20', '-pix_fmt', 'yuv420p',
                    '-c:a', 'aac', '-b:a', '192k', '-shortest', '-movflags', '+faststart',
                    str(args.output / (version + '.mp4'))], check=True)
    subprocess.run([args.ffmpeg, '-y', '-v', 'error', '-f', 'f32le', '-ar', str(report['sampleRate']),
                    '-ac', '2', '-i', str(source / 'mix.f32'), '-c:a', 'libmp3lame', '-b:a', '192k',
                    str(args.output / (version + '.mp3'))], check=True)
sequence = args.output / 'sequence.txt'
sequence.write_text("file 'before.mp4'\nfile 'after.mp4'\n", encoding='utf-8')
subprocess.run([args.ffmpeg, '-y', '-v', 'error', '-f', 'concat', '-safe', '0', '-i', str(sequence),
                '-c', 'copy', '-movflags', '+faststart', str(args.output / 'comparison.mp4')], check=True)
sequence.unlink()
(args.output / 'mix-metrics.json').write_text(json.dumps(metrics, indent=2), encoding='utf-8')
print(json.dumps(metrics, indent=2))
