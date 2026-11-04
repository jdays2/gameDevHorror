"""Prepare restrained human-reaction auditions without destructive timbre effects."""
import argparse
import hashlib
import json
from pathlib import Path

import numpy as np

from prepare_audio import SR, ROOT, add, decode, fade, loudness, write


FAMILY = ROOT / "Concepts/Unused/Audio/PlayerHitTask009/Reaction04"


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--ffmpeg", required=True)
    args = parser.parse_args()
    edits = {}
    for key in "JKL":
        source = FAMILY / f"Sources/{key}_source.mp3"
        signal = decode(source, args.ffmpeg, "highpass=f=65")
        block = 240
        count = len(signal) // block
        rms = np.sqrt(np.mean(signal[:count * block].reshape(count, block)**2, axis=1))
        active = np.flatnonzero(rms > max(.0006, float(rms.max()) * .018))
        if len(active) == 0:
            raise ValueError(f"Silent source {key}")
        start = max(0, int(active[0]) * block - round(.012 * SR))
        end = min(len(signal), (int(active[-1]) + 1) * block + round(.06 * SR))
        signal = fade(signal[start:end] - np.mean(signal[start:end]), .025)
        signal *= min(1, .85 / np.max(np.abs(signal)))
        path = FAMILY / f"Edits/{key}_trim.wav"
        write(path, signal)
        measured = float(loudness(path, args.ffmpeg)["input_i"])
        edits[key] = (signal, measured, start / SR, end / SR)

    target = min(-25.2, *[level + 20 * np.log10(.68 / np.max(np.abs(signal)))
                          for signal, level, _, _ in edits.values()])
    report = {"task": "TASK-009", "revision": "Reaction04", "sampleRate": SR,
              "commonTargetLUFS": target, "previewGain": .8,
              "processing": "Mono48k decode; highpass65Hz; energy-based silence trim; DC removal; 1ms/25ms fades; common static loudness target with peak ceiling. No lowpass, compressor, distortion, pitch shift or layering.",
              "auditionStatus": "Unapproved; source descriptions are intended directions, not listening verification.",
              "variants": {}, "previews": []}
    combined = np.zeros(round(12.4 * SR))
    for index, (key, (signal, level, start, end)) in enumerate(edits.items()):
        signal *= 10**((target - level) / 20)
        path = FAMILY / f"Exports/{key}_hit.wav"
        metrics = write(path, signal)
        metrics.update({"loudness": loudness(path, args.ffmpeg), "sourceStart": start, "sourceEnd": end,
                        "sourceSha256": hashlib.sha256((FAMILY / f"Sources/{key}_source.mp3").read_bytes()).hexdigest()})
        report["variants"][key] = metrics
        preview = np.zeros(round(3.8 * SR))
        add(preview, signal, .3, .8)
        add(preview, signal, 2.3, .8)
        report["previews"].append(write(FAMILY / f"Previews/{key}_double.wav", preview))
        add(combined, signal, index * 4 + .3, .8)
        add(combined, signal, index * 4 + 2.3, .8)
    report["previews"].append(write(FAMILY / "Previews/JKL_comparison.wav", combined))
    (FAMILY / "processing.json").write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"target": target, "variants": report["variants"]}, indent=2))


if __name__ == "__main__":
    main()
