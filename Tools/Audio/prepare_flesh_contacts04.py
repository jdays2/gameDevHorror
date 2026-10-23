"""Adapt the owner's selected B audition to the existing four impact voices."""
import argparse
import hashlib
import json
from pathlib import Path
import subprocess
import numpy as np

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / "Concepts/Unused/Audio/FleshImpactStudies04/B-heavy-wet-rupture.wav"
OUTPUT = ROOT / "Assets/EmergencyShift/Audio/FleshContacts04/flesh-contact-B.wav"
PROVENANCE = ROOT / "Concepts/Used/Audio/FleshContacts04"
RATE = 48000
SECONDS = .34


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--ffmpeg", required=True)
    args = parser.parse_args()
    decoded = subprocess.run([args.ffmpeg, "-v", "error", "-i", str(SOURCE), "-ar", str(RATE), "-ac", "2", "-f", "f32le", "-"], capture_output=True, check=True)
    original = np.frombuffer(decoded.stdout, dtype="<f4").reshape(-1, 2)
    signal = original[:round(SECONDS * RATE)].copy()
    time_left = (len(signal) - 1 - np.arange(len(signal))) / RATE
    signal *= np.clip(time_left / .04, 0, 1)[:, None]
    if not np.isfinite(signal).all() or np.abs(signal).max() >= 1:
        raise ValueError("Invalid trial contact")
    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    PROVENANCE.mkdir(parents=True, exist_ok=True)
    subprocess.run([args.ffmpeg, "-y", "-v", "error", "-f", "f32le", "-ar", str(RATE), "-ac", "2", "-i", "-", "-c:a", "pcm_s24le", str(OUTPUT)], input=signal.astype("<f4").tobytes(), check=True)
    manifest = {"date":"2026-10-23", "status":"selected_B_gameplay_trial_not_final_art_approval",
                "source":str(SOURCE.relative_to(ROOT)).replace("\\", "/"), "sourceSha256":hashlib.sha256(SOURCE.read_bytes()).hexdigest(),
                "taskId":"f372d894-36b2-4c46-9823-af7f078e469f", "output":str(OUTPUT.relative_to(ROOT)).replace("\\", "/"),
                "sha256":hashlib.sha256(OUTPUT.read_bytes()).hexdigest(), "seconds":SECONDS, "sampleRate":RATE, "channels":2, "pcmBits":24,
                "tailFadeSeconds":.04, "peak":float(np.abs(signal).max()), "clippedSamples":int((np.abs(signal) >= 1).sum()),
                "energyRetained":float(np.sum(signal * signal) / np.sum(original * original)), "runtimeGain":.28,
                "maximumClipSecondsAtMinimumPitch":SECONDS / .97, "fourVoicesAtPistolIntervalSeconds":4 * .1}
    (PROVENANCE / "manifest.json").write_text(json.dumps(manifest, indent=2), encoding="utf-8")
    print(json.dumps(manifest, indent=2))


if __name__ == "__main__":
    main()
