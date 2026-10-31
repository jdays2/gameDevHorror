"""Offline concept checks; no Unity access. Existing project analysis runtime required.

python analyze_audition.py --analysis-root <outputs/audio-reference-analysis>
"""
from pathlib import Path
import argparse
import hashlib
import json
import subprocess
import sys

parser = argparse.ArgumentParser()
parser.add_argument('--analysis-root', required=True, type=Path)
args = parser.parse_args()
analysis_root = args.analysis_root.resolve()
root = Path(__file__).resolve().parent
project = root.parents[4]
sys.path[:0] = [str(analysis_root / 'runtime'), str(analysis_root / 'inference-runtime')]
import numpy as np
import librosa
import imageio_ffmpeg

# Reuse the project's existing semantic comparison, with identical model and labels.
source = (analysis_root / 'clap_semantics.py').read_text(encoding='utf-8')
scope = {'__file__': str(analysis_root / 'clap_semantics.py')}
exec(compile(source.split('specs = [')[0], str(analysis_root / 'clap_semantics.py'), 'exec'), scope)
ffmpeg = imageio_ffmpeg.get_ffmpeg_exe()
files = [
    (project / 'Concepts/Unused/Audio/MenuThemes02/Originals/below_the_ward_02.mp3', [0, 10, 80]),
    (root / 'Originals/last_ward_01.mp3', [0, 10, 20]),
    (root / 'Originals/last_ward_02.mp3', [0, 10, 20]),
]
rows, vectors, measures = [], [], []
for path, starts in files:
    raw = subprocess.check_output([ffmpeg, '-v', 'error', '-i', str(path), '-f', 'f32le', '-ac', '2', '-ar', '48000', '-'])
    stereo = np.frombuffer(raw, dtype='<f4').reshape(-1, 2)
    mono = stereo.mean(axis=1)
    low = librosa.resample(mono, orig_sr=48000, target_sr=12000)
    tempo, beats = librosa.beat.beat_track(y=low, sr=12000, start_bpm=90, trim=False)
    chroma = librosa.feature.chroma_stft(y=low, sr=12000).mean(axis=1)
    measures.append({'file': path.name, 'sha256': hashlib.sha256(path.read_bytes()).hexdigest(),
        'duration_seconds': len(mono)/48000, 'finite': bool(np.isfinite(stereo).all()),
        'sample_peak_dbfs': float(20*np.log10(np.max(abs(stereo)))),
        'rms_dbfs': float(20*np.log10(np.sqrt(np.mean(stereo**2)))),
        'estimated_bpm': float(np.asarray(tempo).ravel()[0]),
        'chroma_mean_C_to_B': chroma.tolist(),
        'note': 'Tempo/chroma and model tags are diagnostic estimates, not a transcription or listening verdict.'})
    for start in starts:
        clip = mono[start*48000:(start+10)*48000]
        emb = scope['audio_session'].run(None, {'input_features': scope['features'](clip)})[0]
        emb /= np.linalg.norm(emb, axis=-1, keepdims=True)
        scores = (emb @ scope['text_embeddings'].T)[0]
        ranked, offset = {}, 0
        for group, labels in scope['GROUPS'].items():
            pairs = sorted(zip(labels, scores[offset:offset+len(labels)]), key=lambda x: x[1], reverse=True)
            ranked[group] = [{'description': label, 'similarity': round(float(score), 4)} for label, score in pairs]
            offset += len(labels)
        rows.append({'file': path.name, 'start_seconds': start, 'ranked_descriptions': ranked})
        vectors.append(emb[0])
matrix = np.stack(vectors) @ np.stack(vectors).T
out = {'method': 'Same project CLAP relative comparisons; not calibrated probabilities or human hearing.',
    'model': 'Xenova/clap-htsat-unfused quantized ONNX', 'measurements': measures,
    'windows': rows, 'window_audio_similarity_matrix': matrix.tolist(), 'direct_listening': False}
(root / 'style-analysis.json').write_text(json.dumps(out, ensure_ascii=False, indent=2)+'\n', encoding='utf-8', newline='\n')
print(json.dumps({'measurements': measures, 'top_tags': [{'file': r['file'], 'start': r['start_seconds'],
    **{key: values[:3] for key, values in r['ranked_descriptions'].items()}} for r in rows]}, ensure_ascii=False, indent=2))
