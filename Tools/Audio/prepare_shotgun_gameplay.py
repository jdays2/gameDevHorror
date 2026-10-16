"""Trim the approved-for-trial recorded B bank for immediate gameplay attacks.

Requires NumPy. Preserves the original source studies and writes a separate
runtime bank, with exact trims and hashes in the existing provenance family.
"""
from pathlib import Path
import hashlib
import json
import wave

import numpy as np


ROOT = Path(__file__).resolve().parents[2]
FAMILY = ROOT / 'Concepts/Used/Audio/ShotgunRealism01'
OUTPUT = ROOT / 'Assets/EmergencyShift/Audio/ShotgunPump'


def read_pcm24(path):
    with wave.open(str(path), 'rb') as stream:
        assert stream.getsampwidth() == 3
        assert stream.getframerate() == 48000
        assert stream.getnchannels() == 2
        raw = np.frombuffer(stream.readframes(stream.getnframes()), np.uint8)
    b = raw.reshape(-1, 3).astype(np.int32)
    values = b[:, 0] | (b[:, 1] << 8) | (b[:, 2] << 16)
    values = np.where(values >= 8388608, values - 16777216, values)
    return values.reshape(-1, 2) / 8388608


def main():
    records = []
    for index in range(1, 4):
        source = FAMILY / 'Clips' / f'shot_b_{index:02}.wav'
        x = read_pcm24(source)
        envelope = np.max(np.abs(x), axis=1)
        onset = int(np.flatnonzero(envelope > envelope.max() * .1)[0])
        # Keep 3 ms of the natural leading attack. No pitch, EQ or compression.
        start = max(0, onset - 144)
        x = x[start:start + 28800].copy()
        x[:8] *= np.linspace(0, 1, 8)[:, None]
        x[-720:] *= np.linspace(1, 0, 720)[:, None]
        target = OUTPUT / f'shot_01_{index:02}.wav'
        q = np.rint(x * 8388607).astype(np.int32).reshape(-1)
        packed = np.stack([(q >> shift) & 255 for shift in (0, 8, 16)], axis=-1)
        with wave.open(str(target), 'wb') as stream:
            stream.setnchannels(2)
            stream.setsampwidth(3)
            stream.setframerate(48000)
            stream.writeframes(packed.astype(np.uint8).tobytes())
        records.append({
            'source': source.relative_to(ROOT).as_posix(),
            'sourceSha256': hashlib.sha256(source.read_bytes()).hexdigest(),
            'output': target.relative_to(ROOT).as_posix(),
            'sha256': hashlib.sha256(target.read_bytes()).hexdigest(),
            'trimStartSample': start,
            'seconds': len(x) / 48000,
            'peak': float(abs(x).max()),
            'attackRms150ms': float(np.sqrt(np.mean(x[:7200] ** 2))),
        })
    (FAMILY / 'gameplay-bank.json').write_text(
        json.dumps({
            'date': '2026-10-16',
            'status': 'Owner-requested gameplay correction; final listening approval pending',
            'processing': 'Trim to 3 ms before 10%-peak onset; 600 ms length; 8-sample entry and 15 ms exit fades. Existing recorded B processing retained; no new EQ/compression/synthesis.',
            'reference': {'url': 'https://www.youtube.com/watch?v=KBoWPXfoUdY',
                          'role': 'Owner listening reference only; fetch unavailable; no audio copied'},
            'outputs': records,
        }, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')


if __name__ == '__main__':
    main()
