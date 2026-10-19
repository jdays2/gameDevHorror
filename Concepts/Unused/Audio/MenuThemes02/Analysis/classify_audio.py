"""Probabilistic audio-event tags; these are not verified instrument credits."""
from pathlib import Path
import csv
import json
import subprocess
import sys

ROOT = Path(__file__).resolve().parent
sys.path.insert(0, str(ROOT / 'inference-runtime'))
import numpy as np
import onnxruntime as ort
import imageio_ffmpeg

labels = [row['display_name'] for row in csv.DictReader((ROOT / 'yamnet_class_map.csv').open(encoding='utf-8'))]
session = ort.InferenceSession(str(ROOT / 'yamnet.onnx'), providers=['CPUExecutionProvider'])
ffmpeg = imageio_ffmpeg.get_ffmpeg_exe()

def classify(path):
    raw = subprocess.check_output([ffmpeg, '-v', 'error', '-i', str(path), '-f', 'f32le', '-ac', '1', '-ar', '16000', '-'])
    y = np.frombuffer(raw, dtype='<f4').copy()
    windows = []
    all_scores = []
    for start in range(0, len(y), 16000 * 10):
        chunk = y[start:start + 16000 * 10]
        if len(chunk) < 16000:
            continue
        scores = session.run(None, {'waveform': chunk})[0]
        mean = scores.mean(axis=0)
        all_scores.append(scores)
        top = np.argsort(mean)[::-1][:12]
        windows.append({'start_seconds': start / 16000, 'tags': {labels[i]: round(float(mean[i]), 4) for i in top}})
    mean = np.concatenate(all_scores).mean(axis=0)
    top = np.argsort(mean)[::-1][:25]
    return {'file': path.name, 'duration_seconds': len(y)/16000, 'mean_top_tags': {labels[i]: round(float(mean[i]), 4) for i in top}, 'windows': windows}

names = sys.argv[1:] or ['alien_shooter_menu.mp3', 'alien_shooter_2_theme.mp3']
result = [classify(ROOT / name) for name in names]
out = ROOT / ('yamnet-' + '-'.join(Path(n).stem for n in names) + '.json')
out.write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding='utf-8')
print(json.dumps(result, ensure_ascii=False))
