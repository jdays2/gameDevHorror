"""Level-match two generated auditions; preserve raw sources and keep game assets unchanged."""
import argparse
import hashlib
import json
from pathlib import Path
import subprocess
import numpy as np

ROOT = Path(__file__).resolve().parents[2]
FAMILY = ROOT / "Concepts/Unused/Audio/FleshImpactStudies04"
RATE = 48000


def decode(encoder, path):
    result = subprocess.run([encoder, "-v", "error", "-i", str(path), "-ar", str(RATE), "-ac", "2", "-f", "f32le", "-"], check=True, capture_output=True)
    return np.frombuffer(result.stdout, dtype="<f4").reshape(-1, 2).copy()


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--ffmpeg", required=True)
    args = parser.parse_args()
    metadata = json.loads((FAMILY / "generation.json").read_text(encoding="utf-8"))
    manifest = []
    for entry in metadata["entries"]:
        source = FAMILY / entry["source"]
        signal = decode(args.ffmpeg, source)
        if len(signal) == 0 or not np.isfinite(signal).all():
            raise ValueError("Invalid generated audio")
        raw_peak = float(np.abs(signal).max())
        signal -= signal.mean(axis=0)
        time = np.arange(len(signal)) / RATE
        signal *= np.minimum(1, time / .0005)[:, None]
        signal *= np.minimum(1, (len(signal) / RATE - time) / .035)[:, None]
        signal *= .78 / max(float(np.abs(signal).max()), 1e-9)
        stem = "A-sharp-penetration" if entry["variant"] == "A" else "B-heavy-wet-rupture"
        wav = FAMILY / (stem + ".wav")
        mp3 = FAMILY / (stem + ".mp3")
        subprocess.run([args.ffmpeg, "-y", "-v", "error", "-f", "f32le", "-ar", str(RATE), "-ac", "2", "-i", "-", "-c:a", "pcm_s24le", str(wav)], input=signal.astype("<f4").tobytes(), check=True)
        subprocess.run([args.ffmpeg, "-y", "-v", "error", "-i", str(wav), "-c:a", "libmp3lame", "-b:a", "192k", str(mp3)], check=True)
        preview = decode(args.ffmpeg, mp3)
        peak = float(np.abs(preview).max())
        if peak >= 1:
            raise ValueError("Encoded audition exceeds full scale")
        row = {"variant":entry["variant"], "taskId":entry["taskId"], "source":entry["source"], "sourceSha256":hashlib.sha256(source.read_bytes()).hexdigest(),
               "rawDecodedPeak":raw_peak, "seconds":len(signal) / RATE, "sampleRate":RATE, "channels":2,
               "wav":wav.name, "wavSha256":hashlib.sha256(wav.read_bytes()).hexdigest(), "mp3":mp3.name,
               "mp3Sha256":hashlib.sha256(mp3.read_bytes()).hexdigest(), "previewDecodedPeak":peak,
               "previewRms":float(np.sqrt(np.mean(preview * preview))), "previewClippedSamples":int((np.abs(preview) >= 1).sum())}
        manifest.append(row)
    (FAMILY / "manifest.json").write_text(json.dumps(manifest, indent=2), encoding="utf-8")
    print(json.dumps(manifest, indent=2))


if __name__ == "__main__":
    main()
