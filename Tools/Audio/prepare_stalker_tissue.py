"""Prepare two generated source recordings; preserve originals and exact trims.

Usage: python prepare_stalker_tissue.py --ffmpeg /path/to/ffmpeg
No generated tones, pitch shifting or added reverb.
"""
import argparse
import hashlib
import json
from pathlib import Path
import subprocess
import wave

import numpy as np

ROOT = Path(__file__).resolve().parents[2]
FAMILY = ROOT / "Concepts/Used/Audio/StalkerTissue05"
OUTPUT = ROOT / "Assets/EmergencyShift/Audio/Stalker/Tissue05"
SR = 48000


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--ffmpeg", required=True)
    args = parser.parse_args()
    OUTPUT.mkdir(parents=True, exist_ok=True)
    records = []
    trims = [
        ("eye-burn.mp3", "eye_burn_01.wav", 0, .48, .42),
        ("wet-rupture.mp3", "flesh_pop_01.wav", .025, .68, .82),
        ("wet-rupture.mp3", "flesh_pop_02.wav", 1.285, 1.91, .82),
    ]
    for source, name, start, end, peak in trims:
        original = FAMILY / "Originals" / source
        data = subprocess.check_output([
            args.ffmpeg, "-v", "error", "-i", str(original),
            "-f", "f32le", "-ar", str(SR), "-ac", "1", "-",
        ])
        x = np.frombuffer(data, "<f4").astype(float)[round(start * SR):round(end * SR)].copy()
        x -= x.mean()
        x *= peak / max(1e-8, np.max(np.abs(x)))
        x[:96] *= np.linspace(0, 1, 96)
        x[-1440:] *= np.linspace(1, 0, 1440)
        assert np.isfinite(x).all() and np.max(np.abs(x)) < .99
        target = OUTPUT / name
        with wave.open(str(target), "wb") as stream:
            stream.setnchannels(1)
            stream.setsampwidth(2)
            stream.setframerate(SR)
            stream.writeframes(np.rint(x * 32767).astype("<i2").tobytes())
        records.append({
            "source": source,
            "source_sha256": hashlib.sha256(original.read_bytes()).hexdigest(),
            "trim_seconds": [start, end],
            "output": str(target.relative_to(ROOT)).replace("\\", "/"),
            "sha256": hashlib.sha256(target.read_bytes()).hexdigest(),
            "seconds": len(x) / SR,
            "peak": float(np.max(np.abs(x))),
            "rms": float(np.sqrt(np.mean(x * x))),
        })
    (FAMILY / "processing.json").write_text(json.dumps(records, indent=2) + "\n", encoding="utf-8")
    print("Prepared burn contact and two independent flesh pops; PCM16 mono 48 kHz.")


if __name__ == "__main__":
    main()
