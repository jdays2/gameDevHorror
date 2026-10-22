"""Reproducible transient shaping of existing licensed project recordings; no synthesized layers."""
from pathlib import Path
import hashlib
import json
import wave
import numpy as np

ROOT = Path(__file__).resolve().parents[2]
DEST = ROOT / "Assets/EmergencyShift/Audio/WeaponImpacts02"
PROVENANCE = ROOT / "Concepts/Used/Audio/WeaponImpacts02"


def read_pcm(path):
    with wave.open(str(path), "rb") as source:
        rate, channels, width = source.getframerate(), source.getnchannels(), source.getsampwidth()
        data = source.readframes(source.getnframes())
    if width == 3:
        packed = np.frombuffer(data, dtype=np.uint8).reshape(-1, 3).astype(np.int32)
        values = packed[:, 0] | (packed[:, 1] << 8) | (packed[:, 2] << 16)
        values = (values ^ 0x800000) - 0x800000
        values = values.astype(np.float64) / 8388608
    elif width == 2:
        values = np.frombuffer(data, dtype="<i2").astype(np.float64) / 32768
    else:
        raise ValueError(f"Unsupported PCM width: {path}: {width}")
    return values.reshape(-1, channels), rate


def process(source, output, material):
    original, rate = read_pcm(source)
    signal = original - original.mean(axis=0)
    time = np.arange(len(signal)) / rate
    # Bring the recorded contact forward, then leave a quieter material-specific decay.
    attack = 1 + .8 * np.exp(-time / .026)
    tail = .7 + .3 * np.exp(-time / .12)
    spectrum = np.fft.rfft(signal, axis=0)
    frequency = np.fft.rfftfreq(len(signal), 1 / rate)
    if material == "concrete":
        shelf = 1 + .22 * frequency / (frequency + 2200)
    elif material == "wood":
        shelf = 1 + .18 * np.exp(-((frequency - 950) / 900) ** 2)
    else:
        shelf = 1 + .15 * np.exp(-((frequency - 600) / 650) ** 2)
    signal = np.fft.irfft(spectrum * shelf[:, None], n=len(signal), axis=0)
    signal *= (attack * tail)[:, None]
    signal *= np.minimum(1, time / .0007)[:, None]
    signal *= np.minimum(1, (len(signal) / rate - time) / .012)[:, None]
    signal *= .78 / max(float(np.abs(signal).max()), 1e-8)
    with wave.open(str(output), "wb") as target:
        target.setnchannels(signal.shape[1])
        target.setsampwidth(2)
        target.setframerate(rate)
        target.writeframes(np.rint(signal * 32767).astype("<i2").tobytes())
    return {"source": source.relative_to(ROOT).as_posix(), "output": output.relative_to(ROOT).as_posix(),
            "source_sha256": hashlib.sha256(source.read_bytes()).hexdigest(),
            "sha256": hashlib.sha256(output.read_bytes()).hexdigest(), "sample_rate": rate,
            "seconds": len(signal) / rate, "peak": float(np.abs(signal).max()),
            "attack_rms_before": float(np.sqrt(np.mean(original[:int(rate * .04)] ** 2))),
            "attack_rms_after": float(np.sqrt(np.mean(signal[:int(rate * .04)] ** 2)))}


if __name__ == "__main__":
    DEST.mkdir(parents=True, exist_ok=True)
    PROVENANCE.mkdir(parents=True, exist_ok=True)
    sources = [(path, "wood" if "wood" in path.name else "concrete")
               for path in sorted((ROOT / "Assets/EmergencyShift/Audio/WorldImpacts").glob("*.wav"))]
    sources += [(path, "flesh") for path in sorted(
        (ROOT / "Assets/EmergencyShift/Audio/Stalker/Membrane04").glob("stalker_hit_*.wav"))]
    manifest = [process(path, DEST / path.name, material) for path, material in sources]
    (PROVENANCE / "manifest.json").write_text(json.dumps(manifest, indent=2), encoding="utf-8")
    print(json.dumps(manifest, indent=2))
