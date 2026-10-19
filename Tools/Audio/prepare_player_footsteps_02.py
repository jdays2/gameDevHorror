"""Prepare short player shoe contacts; retained generated source, no runtime DSP."""
import argparse
import hashlib
import json
from pathlib import Path
import subprocess
import wave

import numpy as np


ROOT = Path(__file__).resolve().parents[2]
FAMILY = ROOT / 'Concepts/Used/Audio/PlayerFootsteps02'
DESTINATION = ROOT / 'Assets/EmergencyShift/Audio/PlayerFootsteps02'
SR = 48000
# Six clean, compact contacts from one footwear/source recording.
CUTS = [(0.12, 0.34), (1.20, 1.42), (1.63, 1.81),
        (2.70, 2.88), (3.20, 3.38), (3.62, 3.80)]


def write_wav(path, signal):
    assert np.isfinite(signal).all()
    assert abs(signal).max() < .99
    path.parent.mkdir(parents=True, exist_ok=True)
    with wave.open(str(path), 'wb') as stream:
        stream.setnchannels(1)
        stream.setsampwidth(2)
        stream.setframerate(SR)
        stream.writeframes(np.rint(signal * 32767).astype('<i2').tobytes())
    return {
        'path': path.relative_to(ROOT).as_posix(),
        'seconds': round(len(signal) / SR, 6),
        'peak': round(float(abs(signal).max()), 6),
        'rms': round(float(np.sqrt(np.mean(signal ** 2))), 6),
        'sha256': hashlib.sha256(path.read_bytes()).hexdigest(),
    }


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--ffmpeg', required=True)
    args = parser.parse_args()
    source = FAMILY / 'Sources/crisp-contacts.mp3'
    raw = subprocess.check_output([
        args.ffmpeg, '-v', 'error', '-i', str(source), '-ac', '1', '-ar', str(SR),
        '-af', 'highpass=f=90,lowpass=f=4200', '-f', 'f32le', '-',
    ])
    recording = np.frombuffer(raw, '<f4').astype(float)
    clips = []
    entries = []
    for number, (start, end) in enumerate(CUTS, 1):
        contact = recording[round(start * SR):round(end * SR)].copy()
        # Align the first meaningful 2ms window, leaving a 2ms lead-in.
        block = round(.002 * SR)
        envelope = np.sqrt(np.mean(contact[:len(contact)//block*block].reshape(-1, block)**2, axis=1))
        onset = int(np.flatnonzero(envelope > envelope.max() * .14)[0]) * block
        trim = max(0, onset - block)
        contact = contact[trim:trim + round(.18 * SR)]
        contact -= contact.mean()
        attack = round(.001 * SR)
        release = round(.028 * SR)
        contact[:attack] *= np.linspace(0, 1, attack)
        contact[-release:] *= np.linspace(1, 0, release)
        rms = float(np.sqrt(np.mean(contact**2)))
        gain = min(.055 / max(rms, 1e-9), .68 / max(abs(contact).max(), 1e-9))
        contact *= gain
        clips.append(contact)
        entry = write_wav(DESTINATION / f'shoe-contact-{number:02}.wav', contact)
        entry.update(sourceCut=[start, end], onsetTrimSeconds=trim / SR, gain=gain)
        entries.append(entry)

    # The actual nominal cadence: 3.5 units/s / 1.05 units per contact.
    # Same .24 profile gain and +/-2.5% pitch range as the game.
    rng = np.random.default_rng(19)
    preview = np.zeros(round(4.3 * SR))
    order = [0, 3, 1, 5, 2, 4, 0, 2, 5, 1, 4, 3]
    for index, variant in enumerate(order):
        clip = clips[variant]
        pitch = float(rng.uniform(.975, 1.025))
        shifted = np.interp(np.arange(0, len(clip)-1, pitch), np.arange(len(clip)), clip)
        offset = round((.25 + index * .3) * SR)
        preview[offset:offset+len(shifted)] += shifted * .24
    preview_entry = write_wav(FAMILY / 'Previews/walking-game-level.wav', preview)
    # Explicit listening copy; this boost is never used by the game.
    listening_entry = write_wav(FAMILY / 'Previews/walking-audition-plus-9dB.wav', preview * 10**(9/20))
    report = {
        'source': source.relative_to(ROOT).as_posix(),
        'sourceSha256': hashlib.sha256(source.read_bytes()).hexdigest(),
        'filters': 'mono 48000 Hz; highpass 90 Hz; lowpass 4200 Hz',
        'format': 'PCM16 mono 48000 Hz; maximum 180ms; 1ms attack/28ms release',
        'targetRms': .055,
        'peakLimit': .68,
        'clips': entries,
        'previews': [preview_entry, listening_entry],
        'previewSettings': {'intervalSeconds': .3, 'gain': .24, 'pitchRange': [.975, 1.025]},
        'approval': 'Replacement requested; creative assessment by owner pending.',
    }
    (FAMILY / 'processing.json').write_text(json.dumps(report, indent=2) + '\n', encoding='utf-8')
    print(json.dumps({'clips': entries, 'previews': [preview_entry, listening_entry]}, indent=2))


if __name__ == '__main__':
    main()
