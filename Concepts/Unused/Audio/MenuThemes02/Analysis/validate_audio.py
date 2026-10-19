"""Decode generated originals and record technical properties without editing audio."""
from pathlib import Path
import hashlib
import json
import subprocess
import sys

ROOT = Path(__file__).resolve().parent
sys.path.insert(0, str(ROOT / 'inference-runtime'))
import numpy as np
import imageio_ffmpeg

def inspect(path):
    data = path.read_bytes()
    raw = subprocess.check_output([imageio_ffmpeg.get_ffmpeg_exe(), '-v', 'error', '-i', str(path), '-f', 'f32le', '-ac', '2', '-ar', '48000', '-'])
    y = np.frombuffer(raw, dtype='<f4').reshape(-1, 2)
    peak = float(np.max(np.abs(y)))
    rms = float(np.sqrt(np.mean(y.astype(np.float64)**2)))
    return {'file': path.name, 'sha256': hashlib.sha256(data).hexdigest(), 'bytes': len(data), 'duration_seconds': len(y)/48000, 'finite': bool(np.isfinite(y).all()), 'decoded_sample_peak': peak, 'decoded_sample_peak_dbfs': round(float(20*np.log10(max(peak, 1e-12))), 3), 'rms_dbfs': round(float(20*np.log10(max(rms, 1e-12))), 3), 'samples_at_or_above_0dbfs': int(np.count_nonzero(np.abs(y) >= 1)), 'loop_seam_verified': False, 'perceptual_listening_verified': False, 'processing': 'Original MP3 bytes retained; measurement decode only, no audio edits.'}

paths = [Path(arg).resolve() for arg in sys.argv[1:]]
print(json.dumps([inspect(path) for path in paths], indent=2))
