"""Derived real-recording firearm mix. Requires numpy and an explicit ffmpeg path.

Original accepted recordings are immutable inputs; no synthesized blast layers.
"""
from pathlib import Path
import argparse, hashlib, json, subprocess, wave
import numpy as np
ROOT = Path(__file__).resolve().parents[2]
FAMILY = ROOT / 'Concepts/Used/Audio/FirearmMix02'
OUT = ROOT / 'Assets/EmergencyShift/Audio/FirearmMix02'
RATE = 48000

def sha(p):
    return hashlib.sha256(p.read_bytes()).hexdigest()

def decode(ffmpeg, p):
    raw = subprocess.check_output([ffmpeg, '-v', 'error', '-i', str(p), '-f', 'f32le', '-ar', str(RATE), '-ac', '2', '-'])
    return np.frombuffer(raw, '<f4').reshape(-1, 2).astype(np.float64)

def fade(x):
    x = x.copy()
    x -= x.mean(axis=0)
    x[:8] *= np.linspace(0, 1, 8)[:, None]
    n = min(720, len(x))
    x[-n:] *= np.linspace(1, 0, n)[:, None]
    return x

def tone(x, low_db, late_decay):
    f = np.fft.rfftfreq(len(x), 1 / RATE)
    low = 1 / (1 + (f / 420) ** 2)
    gain = 10 ** (low_db * low / 20) * (1 - np.exp(-(f / 45) ** 4))
    x = np.fft.irfft(np.fft.rfft(x, axis=0) * gain[:, None], n=len(x), axis=0)
    if late_decay:
        x *= np.exp(-np.maximum(0, np.arange(len(x)) / RATE - 0.08) / late_decay)[:, None]
    return fade(x)

def write(name, x, peak):
    x = fade(x)
    x *= peak / np.max(abs(x))
    q = np.rint(x * 8388607).astype(np.int32).reshape(-1)
    packed = np.stack([q >> s & 255 for s in (0, 8, 16)], axis=-1).astype(np.uint8)
    p = OUT / (name + '.wav')
    with wave.open(str(p), 'wb') as w:
        w.setnchannels(2)
        w.setsampwidth(3)
        w.setframerate(RATE)
        w.writeframes(packed.tobytes())
    return {'file': p.relative_to(ROOT).as_posix(), 'sha256': sha(p), 'seconds': len(x) / RATE, 'peak': float(abs(x).max()), 'rms150': float(np.sqrt(np.mean(x[:7200] ** 2)))}

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--ffmpeg', required=True)
    args = parser.parse_args()
    OUT.mkdir(parents=True, exist_ok=True)
    records = []
    for i in range(1, 7):
        source = ROOT / f'Assets/EmergencyShift/Audio/PistolIndoor/pistol_indoor_{i:02}.wav'
        x = decode(args.ffmpeg, source)
        a = abs(x).max(axis=1)
        start = max(0, int(np.flatnonzero(a > a.max() * 0.1)[0]) - 144)
        x = tone(x[start:], -1.5, 0.65)
        row = write(f'pistol_{i:02}', x, 0.6)
        row.update(source=source.relative_to(ROOT).as_posix(), sourceSha256=sha(source), trimStartSample=start, lowDb=-1.5, additionalLateDecay=0.65)
        records.append(row)
    for i in range(1, 4):
        source = FAMILY / f'Inputs/shot_01_{i:02}.wav'
        x = tone(decode(args.ffmpeg, source), 2.5, None)
        row = write(f'shotgun_{i:02}', x, 0.82)
        row.update(source=source.relative_to(ROOT).as_posix(), sourceSha256=sha(source), lowDb=2.5)
        records.append(row)
    source = FAMILY / 'Sources/pistol-dry.mp3'
    x = decode(args.ffmpeg, source)
    row = write('empty_01', x[round(5.08 * RATE):round(5.31 * RATE)], 0.22)
    row.update(source=source.relative_to(ROOT).as_posix(), sourceSha256=sha(source), window=[5.08, 5.31])
    records.append(row)
    x = np.zeros((round(1.4 * RATE), 2))
    events = []
    for name, a, b, at, gain in [('magazine-release', 0.34, 0.42, 0.03, 0.22), ('pistol-handling', 22.16, 22.4, 0.65, 0.28), ('pistol-handling', 8.16, 8.24, 1.16, 0.24)]:
        source = FAMILY / 'Sources' / (name + '.mp3')
        y = fade(decode(args.ffmpeg, source)[round(a * RATE):round(b * RATE)])
        y *= gain / max(1e-08, np.max(abs(y)))
        start = round(at * RATE)
        x[start:start + len(y)] += y
        events.append({'source': source.relative_to(ROOT).as_posix(), 'sourceSha256': sha(source), 'window': [a, b], 'at': at})
    row = write('pistol_reload_01', x, 0.28)
    row['events'] = events
    records.append(row)
    report = {
        'date': '2026-10-16',
        'status': 'Owner-requested weapon balance and magazine trial; listening approval pending',
        'processing': (
            '48kHz stereo PCM24. Recorded inputs only; DC removal, HP45, '
            'broad 420Hz low-shelf pistol -1.5dB / shotgun +2.5dB, extra restrained '
            'pistol tail decay, peak normalization and short edge fades. '
            'No synthetic bass/reverb/compressor.'
        ),
        'sourceCaveat': (
            'Public MP3 previews are not lossless masters. New foley previews '
            'have decoder overshoots; selected events are linearly normalized; '
            'no restoration of original recording quality claimed.'
        ),
        'runtimeGains': {
            'pistolShot': .45,
            'shotgunShot': .92,
            'empty': .3,
            'mechanism': .55,
            'pistolCasing': .14,
        },
        'pistolMagazine': {'capacity': 8, 'reloadSeconds': 1.4},
        'outputs': records,
    }
    (FAMILY / 'manifest.json').write_text(json.dumps(report, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    print(json.dumps([{'file': r['file'], 'rms150': r['rms150'], 'peak': r['peak']} for r in records], indent=2))
if __name__ == '__main__':
    main()
