"""Short physical contacts from the accepted Membrane04 recordings; preserve creature voices."""
import hashlib
import json
from pathlib import Path
import wave
import numpy as np
from prepare_weapon_impacts02 import read_pcm

ROOT = Path(__file__).resolve().parents[2]
DEST = ROOT / "Assets/EmergencyShift/Audio/FleshContacts03"
PROVENANCE = ROOT / "Concepts/Used/Audio/FleshContacts03"


def main():
    DEST.mkdir(parents=True, exist_ok=True)
    PROVENANCE.mkdir(parents=True, exist_ok=True)
    manifest = []
    for index in range(1, 5):
        source = ROOT / f"Assets/EmergencyShift/Audio/Stalker/Membrane04/stalker_hit_{index:02}.wav"
        signal, rate = read_pcm(source)
        # Keep the recorded wet attack; discard the longer creature reaction/tail.
        mono = signal.mean(axis=1)
        positions = np.arange(int(rate * .17)) * 1.45
        signal = np.interp(positions, np.arange(len(mono)), mono)
        signal -= signal.mean()
        frequency = np.fft.rfftfreq(len(signal), 1 / rate)
        contact_band = (frequency / (frequency + 100)) * (1 + .75 * np.exp(-((frequency - 1900) / 1400) ** 2))
        signal = np.fft.irfft(np.fft.rfft(signal) * contact_band, n=len(signal))
        time = np.arange(len(signal)) / rate
        signal *= np.minimum(1, time / .0008) * np.exp(-time / .075)
        signal *= np.minimum(1, (.17 - time) / .035)
        signal *= .8 / max(np.abs(signal).max(), 1e-8)
        output = DEST / f"flesh-contact-{index:02}.wav"
        with wave.open(str(output), "wb") as target:
            target.setnchannels(1)
            target.setsampwidth(2)
            target.setframerate(rate)
            target.writeframes(np.rint(signal * 32767).astype("<i2").tobytes())
        manifest.append({"source": source.relative_to(ROOT).as_posix(), "source_sha256": hashlib.sha256(source.read_bytes()).hexdigest(),
                         "output": output.relative_to(ROOT).as_posix(), "sha256": hashlib.sha256(output.read_bytes()).hexdigest(),
                         "seconds": len(signal) / rate, "peak": float(np.abs(signal).max()),
                         "attack_rms_40ms": float(np.sqrt(np.mean(signal[:int(rate * .04)] ** 2)))})
    (PROVENANCE / "manifest.json").write_text(json.dumps(manifest, indent=2), encoding="utf-8")
    print(json.dumps(manifest, indent=2))


if __name__ == "__main__":
    main()
