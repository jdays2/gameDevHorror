"""Offline audition from attributed CC0 recordings; does not modify Unity assets.

Inputs are the public MP3 previews retained in Docs/Audio/WeaponRealism02/Sources.
48 kHz PCM exports do not restore the quality lost in those source previews.
Requires NumPy 2.3.5 and an explicit --ffmpeg path; no synthesis or pitch shifting.
"""
from pathlib import Path
import argparse, hashlib, json, subprocess, wave
import numpy as np

ROOT = Path(__file__).resolve().parents[2]
FOLDER = ROOT / 'Docs/Audio/WeaponRealism02'
RATE = 48000


def decode(ffmpeg, path):
    command = [ffmpeg, '-v', 'error', '-i', str(path), '-f', 'f32le',
               '-acodec', 'pcm_f32le', '-ar', str(RATE), '-ac', '2', '-']
    return np.frombuffer(subprocess.check_output(command), '<f4').reshape(-1, 2).astype(np.float64)


def cut(samples, start, end):
    clip = samples[round(start * RATE):round(end * RATE)].copy()
    # Preserve recorded transients; only sub-millisecond entry and 15 ms exit fades.
    clip -= clip.mean(axis=0)
    fade_in = min(round(.00015 * RATE), len(clip))
    fade_out = min(round(.015 * RATE), len(clip))
    clip[:fade_in] *= np.linspace(0, 1, fade_in)[:, None]
    clip[-fade_out:] *= np.linspace(1, 0, fade_out)[:, None]
    return clip


def save(path, samples):
    assert np.isfinite(samples).all() and 1e-5 < np.max(np.abs(samples)) < .96
    pcm = np.rint(samples * 32767).astype('<i2')
    with wave.open(str(path), 'wb') as stream:
        stream.setnchannels(2); stream.setsampwidth(2); stream.setframerate(RATE)
        stream.writeframes(pcm.tobytes())
    return dict(file=path.name, frames=len(samples), seconds=len(samples)/RATE,
                channels=2, rate=RATE, bits=16, peak=float(np.max(np.abs(pcm.astype(float)/32767))),
                rms=float(np.sqrt(np.mean((pcm.astype(float)/32767)**2))),
                sha256=hashlib.sha256(path.read_bytes()).hexdigest())


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--ffmpeg', required=True)
    parser.add_argument('--force', action='store_true')
    args = parser.parse_args()
    if (FOLDER/'01-pistol-9mm.wav').exists() and not args.force:
        raise SystemExit('Audition exists; deliberate replacement requires --force.')
    sources = json.loads((FOLDER/'Sources/sources.json').read_text(encoding='utf-8'))
    decoded = {}
    for source in sources:
        path = FOLDER/'Sources'/source['file']
        assert hashlib.sha256(path.read_bytes()).hexdigest() == source['sha256']
        decoded[source['id']] = decode(args.ffmpeg, path)
    studies = [
        ('01-pistol-9mm', 'pistol9mm', [(.10, 1.66)]*3, .70,
         '9mm source; three repetitions of the same take, no added reverb'),
        ('02-pistol-45', 'pistol45', [(0, .4607)]*3, .70,
         'Colt .45 recording according to author; three repetitions, no added bass'),
        ('03-pistol-small-room', 'pistolIndoor', [(.005, .666)]*3, .70,
         'Recorded 9mm blank round in a tiled restroom; three repetitions, natural source reflections'),
        ('04-pistol-layered', 'pistolLayered', [(0, 1.42), (1.60, 3.02), (3.18, 4.76)], .70,
         'Three authored layered gunshot takes; source author combined self-recorded firearms and mechanism foley; not a literal Glock recording'),
        ('05-casings-tile', 'casings', [(.07, 1.50), (2.60, 4.03), (4.46, 5.89)], .56,
         'Three different actual casing drops on tile; mixed casing sizes in original, exact caliber of these takes unspecified'),
        ('06-casings-concrete', 'brass', [(.02, 1.45), (2.30, 3.73), (4.06, 5.36)], .56,
         'Three different actual brass casing drops on concrete; no synthesized metal oscillators'),
    ]
    reports = []
    for name, source, windows, peak, note in studies:
        clips = [cut(decoded[source], start, end) for start, end in windows]
        # One linear gain per study preserves variation/dynamics. Float MP3 decoding
        # may overshoot 1.0: scale before PCM conversion instead of clipping it.
        gain = peak / max(float(np.max(np.abs(clip))) for clip in clips)
        montage = np.zeros((round(6.3*RATE), 2))
        for clip, position in zip(clips, [.35, 2.35, 4.35]):
            start = round(position*RATE)
            montage[start:start+len(clip)] += clip*gain
        data = save(FOLDER/(name+'.wav'), montage)
        data.update(source=source, sourceWindows=windows, placementSeconds=[.35, 2.35, 4.35],
                    linearGain=gain, processing='Decode, resample, stereo preserve, DC removal, edge fades, linear gain, spaced montage. No synthesis/compression/pitch shifting.', note=note)
        reports.append(data)
    result = dict(date='2026-10-14', status='Audition only; user rejected earlier procedural weapon timbre. No Unity/runtime changes.',
                  sourceQuality='Publicly available licensed MP3 previews, not original lossless masters. WAV export is a working container, not a quality upgrade.',
                  numpyVersion=np.__version__, files=reports, sources=sources,
                  limitations='Numerical/decoder QA; no subjective listening verdict, Unity integration or final game mix.')
    (FOLDER/'manifest.json').write_text(json.dumps(result, ensure_ascii=False, indent=2)+'\n', encoding='utf-8')
    print(json.dumps(dict(previews=len(reports), sources=len(sources), synthesis=False, unityChanged=False)))


if __name__ == '__main__':
    main()
