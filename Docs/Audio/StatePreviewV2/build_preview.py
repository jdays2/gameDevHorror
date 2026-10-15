"""Render an offline three-state ambience audition; does not modify Unity.
Usage: build_preview.py --sources DIR --ffmpeg EXE --out preview.mp3
DIR contains base.mp3, tension.mp3, pressure.mp3 (task IDs in manifest).
Requires numpy and ffmpeg. No network or credentials are used by this script.
"""
import argparse
import hashlib
import json
import subprocess
from pathlib import Path
import numpy as np

RATE = 48000
DURATION = 40
TIMELINE = [
    {"start": 0, "end": 12, "state": "exploration"},
    {"start": 12, "end": 24, "state": "threat"},
    {"start": 24, "end": 36, "state": "combat"},
    {"start": 36, "end": 40, "state": "release"},
]

def smooth(t, start, end):
    x = np.clip((t - start) / (end - start), 0, 1)
    return x*x*(3-2*x)

def decode(ffmpeg, path):
    raw = subprocess.check_output([
        ffmpeg, "-v", "error", "-i", str(path), "-f", "f32le",
        "-ar", str(RATE), "-ac", "2", "-"
    ])
    a = np.frombuffer(raw, dtype="<f4").astype(np.float64).reshape(-1, 2)
    assert len(a) >= RATE * 9 and np.isfinite(a).all(), str(path)
    return a

def band_limit(a, cutoff):
    # Smooth zero-phase shaping, not a rhythmic envelope.
    f = np.fft.rfftfreq(len(a), 1/RATE)
    highpass = 1 - 1/np.sqrt(1+(f/32)**8)
    lowpass = 1/np.sqrt(1+(f/cutoff)**8)
    h = highpass * lowpass
    return np.fft.irfft(np.fft.rfft(a, axis=0)*h[:, None], n=len(a), axis=0)

def repeat_crossfaded(a, samples):
    overlap = RATE
    assert len(a) > overlap
    out = np.zeros((samples+len(a), 2))
    fade = np.linspace(0, np.pi/2, overlap)
    for start in range(0, samples, len(a)-overlap):
        part = a.copy()
        if start:
            part[:overlap] *= np.sin(fade)[:, None]
        part[-overlap:] *= np.cos(fade)[:, None]
        out[start:start+len(part)] += part
    return out[:samples]

def rms(a):
    return float(np.sqrt(np.mean(a*a)))

def main():
    p = argparse.ArgumentParser()
    p.add_argument("--sources", type=Path, required=True)
    p.add_argument("--ffmpeg", required=True)
    p.add_argument("--out", type=Path, required=True)
    args = p.parse_args()
    count = RATE*DURATION
    t = np.arange(count)/RATE
    levels = {"base": (.042, 750), "tension": (.032, 1500), "pressure": (.065, 2500)}
    stems, inputs = {}, {}
    for name, (target, cutoff) in levels.items():
        path = args.sources/(name+".mp3")
        source = decode(args.ffmpeg, path)
        # Preserve the user's chosen base timbre; shape only the added layers.
        filtered = source if name == "base" else band_limit(source, cutoff)
        tile = repeat_crossfaded(filtered, count)
        assert rms(tile) > 1e-6, name
        stems[name] = tile * (target/rms(tile))
        inputs[name] = {"sha256": hashlib.sha256(path.read_bytes()).hexdigest(),
                        "seconds": len(source)/RATE,
                        "lowpassHz": None if name == "base" else cutoff,
                        "rmsTarget": target}
    gains = {
        "base": 1-.18*smooth(t, 24, 27),
        "tension": smooth(t, 12, 15)*(1-.25*smooth(t, 24, 27))*(1-smooth(t, 36, 39)),
        "pressure": smooth(t, 24, 27)*(1-smooth(t, 36, 39)),
    }
    mix = sum(stems[k]*gains[k][:, None] for k in stems)
    mix *= (smooth(t, 0, .6)*(1-smooth(t, 38.5, 40)))[:, None]
    raw_peak = float(np.max(np.abs(mix)))
    gain = min(1., .8/max(raw_peak, 1e-9))
    mix *= gain
    assert np.isfinite(mix).all() and np.max(abs(mix)) <= .800001
    args.out.parent.mkdir(parents=True, exist_ok=True)
    subprocess.run([
        args.ffmpeg, "-v", "error", "-y", "-f", "f32le", "-ar", str(RATE),
        "-ac", "2", "-i", "-", "-c:a", "libmp3lame", "-b:a", "192k", str(args.out)
    ], input=mix.astype("<f4").tobytes(), check=True)
    checked = decode(args.ffmpeg, args.out)
    assert abs(len(checked)/RATE-DURATION) < .05
    assert np.max(abs(checked)) < .98 and np.isfinite(checked).all()
    report = {
        "durationSeconds": len(checked)/RATE, "sampleRate": RATE,
        "channels": 2, "decodedSuccessfully": True,
        "samplePeak": float(np.max(abs(checked))), "globalSafetyGain": gain,
        "sha256": hashlib.sha256(args.out.read_bytes()).hexdigest(),
        "sources": inputs, "timeline": TIMELINE,
        "phaseRms": {q["state"]: rms(checked[int(q["start"]*RATE):int(q["end"]*RATE)])
                     for q in TIMELINE},
        "atSeconds": {str(sec): {k: float(gains[k][int(sec*RATE)]) for k in gains}
                     for sec in [6, 12, 18, 24, 30, 36, 39]},
        "limits": "Offline audition, not Unity playback. No added percussion, rhythm, enemies or gunshots. Generation prompt compliance, artistic quality and absence of unwanted timbres have not been verified by listening.",
    }
    args.out.with_suffix(".validation.json").write_text(
        json.dumps(report, ensure_ascii=False, indent=2)+"\n", encoding="utf-8")
    print(json.dumps({k: report[k] for k in
        ["durationSeconds", "samplePeak", "phaseRms", "sha256"]}))
if __name__ == "__main__":
    main()
