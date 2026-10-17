"""Build two edit variations from the owner-approved rupture; no new generated sources.

Usage: python prepare_creature_rupture_06.py --ffmpeg /path/to/ffmpeg
The original approved WAV is copied byte-for-byte as variant 01.
"""
import argparse
import hashlib
import json
from pathlib import Path
import shutil
import subprocess
import wave

import numpy as np

ROOT = Path(__file__).resolve().parents[2]
FAMILY = ROOT / "Concepts/Used/Audio/CreatureRupture06"
OUTPUT = ROOT / "Assets/EmergencyShift/Audio/CreatureRupture06"
SR = 48000


def read(path):
    with wave.open(str(path), "rb") as stream:
        assert stream.getnchannels() == 1 and stream.getsampwidth() == 2
        assert stream.getframerate() == SR
        return np.frombuffer(stream.readframes(stream.getnframes()), "<i2").astype(float) / 32768


def write(path, x):
    with wave.open(str(path), "wb") as stream:
        stream.setnchannels(1)
        stream.setsampwidth(2)
        stream.setframerate(SR)
        stream.writeframes(np.rint(x * 32767).astype("<i2").tobytes())


def join(parts):
    result = parts[0].copy()
    for part in parts[1:]:
        n = min(192, len(part), len(result))
        fade = np.linspace(0, 1, n)
        overlap = result[-n:] * (1 - fade) + part[:n] * fade
        result = np.concatenate((result[:-n], overlap, part[n:]))
    return result


def filter_audio(ffmpeg, x, filters):
    raw = subprocess.check_output([
        ffmpeg, "-v", "error", "-f", "f32le", "-ar", str(SR), "-ac", "1", "-i", "-",
        "-af", filters, "-f", "f32le", "-ar", str(SR), "-ac", "1", "-",
    ], input=x.astype("<f4").tobytes())
    return np.frombuffer(raw, "<f4").astype(float)


def finish(x, target_rms):
    x = x[:SR].copy()
    x -= x.mean()
    rms = np.sqrt(np.mean(x * x))
    x *= min(target_rms / max(rms, 1e-8), .85 / max(np.max(np.abs(x)), 1e-8))
    x[:48] *= np.linspace(0, 1, 48)
    x[-576:] *= np.linspace(1, 0, 576)
    assert np.isfinite(x).all() and np.max(np.abs(x)) < .99
    return x


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--ffmpeg", required=True)
    args = parser.parse_args()
    OUTPUT.mkdir(parents=True, exist_ok=True)
    previews = FAMILY / "Clips"
    previews.mkdir(parents=True, exist_ok=True)
    source = ROOT / "Concepts/Unused/Audio/StalkerRupture06/rupture-06.wav"
    originals = FAMILY / "Originals"
    originals.mkdir(parents=True, exist_ok=True)
    shutil.copyfile(source, originals / "approved-rupture-06.wav")
    shutil.copyfile(source.parent / "Originals/rupture-06.mp3", originals / "approved-rupture-06.mp3")
    x = read(source)
    target_rms = np.sqrt(np.mean(x * x))

    # Keep the first onset, reorder adjacent continuous body bursts with 4 ms crossfades.
    ranges = [(0, .055), (.115, .205), (.055, .115), (.205, .31), (.31, 1)]
    ragged = join([x[round(a * SR):round(b * SR)] for a, b in ranges])
    ragged = filter_audio(args.ffmpeg, ragged, "asetrate=48720,aresample=48000,equalizer=f=420:t=q:w=0.9:g=1")

    # A lower, denser edit retains one onset; a quiet source splat changes the body rhythm.
    heavy = filter_audio(args.ffmpeg, x, "asetrate=47280,aresample=48000,equalizer=f=230:t=q:w=0.85:g=1.5")
    layer = x[round(.17 * SR):round(.24 * SR)].copy()
    layer[:192] *= np.linspace(0, 1, 192)
    layer[-192:] *= np.linspace(1, 0, 192)
    start = round(.22 * SR)
    heavy[start:start + len(layer)] += layer * .12
    variants = [x, finish(ragged, target_rms), finish(heavy, target_rms)]

    records = []
    for index, data in enumerate(variants, 1):
        name = f"rupture_06_{index:02}.wav"
        target = OUTPUT / name
        if index == 1:
            shutil.copyfile(source, target)
        else:
            write(target, data)
        shutil.copyfile(target, previews / name)
        records.append({
            "variant": index,
            "file": name,
            "sha256": hashlib.sha256(target.read_bytes()).hexdigest(),
            "seconds": len(data) / SR,
            "rms": float(np.sqrt(np.mean(data * data))),
            "peak": float(np.max(np.abs(data))),
        })

    silence = np.zeros(round(.4 * SR))
    audition = np.concatenate((variants[0], silence, variants[1], silence, variants[2]))
    write(FAMILY / "audition-01-02-03.wav", audition)
    processing = {
        "source_sha256": hashlib.sha256(source.read_bytes()).hexdigest(),
        "format": "mono PCM16 48000 Hz",
        "variant01": "Approved WAV, byte-for-byte copy",
        "variant02": {"source_ranges_seconds": ranges, "crossfade_seconds": .004, "ffmpeg": "asetrate=48720,aresample=48000,equalizer=f=420:t=q:w=0.9:g=1"},
        "variant03": {"ffmpeg": "asetrate=47280,aresample=48000,equalizer=f=230:t=q:w=0.85:g=1.5", "layer_source_seconds": [.17, .24], "layer_start_seconds": .22, "layer_gain": .12},
        "finishing": "Variations align to approved RMS, peak capped at .85, maximum 1 second, 1 ms onset / 12 ms end fade",
        "variants": records,
        "audition": "Variants 01, 02, 03 separated by .4 seconds of silence; no added tones or voices",
    }
    (FAMILY / "processing.json").write_text(json.dumps(processing, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(records, indent=2))


if __name__ == "__main__":
    main()
