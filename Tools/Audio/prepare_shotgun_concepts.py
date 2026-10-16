"""Offline pump-shotgun auditions following the accepted pistol recording pipeline.

Requires NumPy 2.3.5 and an explicit FFmpeg binary. Never writes inside Assets.
Source MP3 previews, authors, verified licenses and hashes are retained beside the output.
"""
from pathlib import Path
import argparse
import hashlib
import json
import subprocess
import tempfile
import wave

import numpy as np


ROOT = Path(__file__).resolve().parents[2]
BASE = ROOT / 'Concepts/Unused/Audio/ShotgunRealism01'
RATE = 48000


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def decode(ffmpeg, path):
    raw = subprocess.check_output([
        ffmpeg, '-v', 'error', '-i', str(path), '-f', 'f32le', '-ar', str(RATE), '-ac', '2', '-'
    ])
    return np.frombuffer(raw, '<f4').reshape(-1, 2).astype(np.float64)


def edges(x):
    x = x.copy()
    x -= x.mean(axis=0)
    entry = min(8, len(x))
    exit_length = min(720, len(x))
    x[:entry] *= np.linspace(0, 1, entry)[:, None]
    x[-exit_length:] *= np.linspace(1, 0, exit_length)[:, None]
    x[0] = 0
    x[-1] = 0
    return x


def prepare(x, late_decay=None):
    # Same broad, restrained spectral treatment as the accepted indoor pistol.
    frequency = np.fft.rfftfreq(len(x), 1 / RATE)
    high = 1 - 1 / (1 + (frequency / 3200) ** 2)
    gain = 10 ** (-1.0 * high / 20) * (1 - np.exp(-(frequency / 45) ** 4))
    x = np.fft.irfft(np.fft.rfft(x, axis=0) * gain[:, None], n=len(x), axis=0)
    if late_decay is not None:
        time = np.arange(len(x)) / RATE
        x *= np.exp(-np.maximum(0, time - .065) / late_decay)[:, None]
    return edges(x)


def pcm24(path, x):
    peak = float(np.max(np.abs(x)))
    assert np.isfinite(x).all() and 1e-5 < peak < .95
    q = np.rint(x * 8388607).astype(np.int32).reshape(-1)
    raw = np.stack([(q >> shift) & 255 for shift in (0, 8, 16)], axis=-1).astype(np.uint8).tobytes()
    with wave.open(str(path), 'wb') as stream:
        stream.setnchannels(2)
        stream.setsampwidth(3)
        stream.setframerate(RATE)
        stream.writeframes(raw)
    return dict(file=path.relative_to(BASE).as_posix(), seconds=len(x) / RATE,
                rate=RATE, channels=2, bits=24, peak=peak,
                rms=float(np.sqrt(np.mean(x * x))), sha256=sha(path))


def montage(seconds, events):
    x = np.zeros((round(seconds * RATE), 2))
    for time, clip, gain in events:
        start = round(time * RATE)
        count = min(len(clip), len(x) - start)
        assert count > 0
        x[start:start + count] += clip[:count] * gain
    assert np.max(np.abs(x)) < .95
    return x


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--ffmpeg', required=True)
    parser.add_argument('--force', action='store_true')
    args = parser.parse_args()
    if (BASE / 'manifest.json').exists() and not args.force:
        raise SystemExit('Audition exists. Use --force only for intentional regeneration.')
    (BASE / 'Clips').mkdir(exist_ok=True)
    (BASE / 'Previews').mkdir(exist_ok=True)
    sources = json.loads((BASE / 'sources.json').read_text(encoding='utf-8'))
    raw = {}
    for source in sources:
        path = BASE / source['file']
        assert sha(path) == source['sha256']
        raw[source['id']] = decode(args.ffmpeg, path)

    clips = {}
    report = []
    banks = [
        ('shot-close', 'shot_a', [(.12, .90), (1.81, 2.85), (3.22, 3.99)], .70, .29),
        ('shot-pump', 'shot_b', [(12.24, 13.55), (15.69, 17.04), (18.74, 20.01)], .70, .29),
        ('pump-studio', 'pump', [(.07, .68), (1.33, 1.96), (2.29, 2.89)], .45, None),
        ('reload-pa08', 'shell_insert', [(9.70, 10.70), (11.78, 12.95), (13.55, 14.87)], .38, None),
        ('reload-pa08', 'empty_trigger', [(1.48, 1.88)], .28, None),
        ('hull-concrete', 'hull_concrete', [(1.55, 3.18)], .30, None),
    ]
    for source_id, prefix, windows, target_peak, late in banks:
        prepared = [prepare(raw[source_id][round(a * RATE):round(b * RATE)].copy(), late) for a, b in windows]
        common_gain = target_peak / max(float(np.max(np.abs(x))) for x in prepared)
        for index, (x, window) in enumerate(zip(prepared, windows), 1):
            name = f'{prefix}_{index:02}'
            x *= common_gain
            clips[name] = x
            item = pcm24(BASE / 'Clips' / (name + '.wav'), x)
            item.update(source=source_id, sourceWindow=list(window), commonBankGain=common_gain, lateDecay=late)
            report.append(item)

    existing = {}
    references = [
        ('pistol', 'Assets/EmergencyShift/Audio/PistolIndoor/pistol_indoor_01.wav'),
        ('concrete_1', 'Assets/EmergencyShift/Audio/WorldImpacts/impact-concrete-01.wav'),
        ('concrete_2', 'Assets/EmergencyShift/Audio/WorldImpacts/impact-concrete-02.wav'),
        ('wood', 'Assets/EmergencyShift/Audio/WorldImpacts/impact-wood-01.wav'),
        ('flesh', 'Assets/EmergencyShift/Audio/Stalker/Membrane04/stalker_hit_01.wav'),
    ]
    reference_report = []
    for key, relative in references:
        file = ROOT / relative
        existing[key] = decode(args.ffmpeg, file)
        reference_report.append(dict(id=key, file=relative, sha256=sha(file), role='Existing accepted game palette, reused offline; source file unchanged'))

    # A few short contacts evoke a scatter pattern; flesh keeps one accepted creature reaction.
    for material, names in [('concrete', ['concrete_1', 'concrete_2']), ('wood', ['wood'])]:
        events = [(offset, existing[names[index % len(names)]], gain)
                  for index, (offset, gain) in enumerate([(0, .65), (.014, .32), (.031, .24)])]
        length = max(offset + len(clip) / RATE for offset, clip, gain in events) + .02
        x = montage(length, events)
        x *= .46 / max(1e-8, np.max(np.abs(x)))
        x = edges(x)
        clips['impact_' + material] = x
        item = pcm24(BASE / 'Clips' / ('impact_' + material + '_cluster.wav'), x)
        item.update(source='Existing accepted ' + material + ' palette', contactsSeconds=[0, .014, .031], role='Offline mixing proposal, no runtime change')
        report.append(item)
    flesh = existing['flesh'].copy()
    flesh *= .42 / max(1e-8, np.max(np.abs(flesh)))
    clips['impact_flesh'] = flesh
    item = pcm24(BASE / 'Clips/impact_flesh_reference.wav', flesh)
    item.update(source='Existing accepted Stalker hit; one reaction per volley, not one scream per pellet')
    report.append(item)

    preview_report = []
    def preview(name, seconds, events, labels):
        x = montage(seconds, events)
        path = BASE / 'Previews' / (name + '.mp3')
        with tempfile.TemporaryDirectory(prefix='es-shotgun-') as folder:
            temp = Path(folder) / 'preview.wav'
            # Temporary WAV has no project-relative path, so write its container directly.
            q = np.rint(x * 8388607).astype(np.int32).reshape(-1)
            raw24 = np.stack([(q >> shift) & 255 for shift in (0, 8, 16)], axis=-1).astype(np.uint8).tobytes()
            with wave.open(str(temp), 'wb') as stream:
                stream.setnchannels(2)
                stream.setsampwidth(3)
                stream.setframerate(RATE)
                stream.writeframes(raw24)
            subprocess.run([args.ffmpeg, '-v', 'error', '-y', '-i', str(temp), '-c:a', 'libmp3lame', '-b:a', '320k', str(path)], check=True)
        decoded = decode(args.ffmpeg, path)
        assert np.isfinite(decoded).all() and np.max(np.abs(decoded)) < .95
        preview_report.append(dict(file=path.relative_to(BASE).as_posix(), seconds=len(decoded) / RATE,
                                   peakAfterMp3=float(np.max(np.abs(decoded))), labels=labels, sha256=sha(path)))

    shot_events = [(.35 + index * 1.45, existing['pistol'], 1) for index in range(3)]
    shot_events += [(5.2 + index * 1.45, clips[f'shot_a_{index+1:02}'], 1) for index in range(3)]
    shot_events += [(10.1 + index * 1.45, clips[f'shot_b_{index+1:02}'], 1) for index in range(3)]
    preview('01-pistol-and-shotgun', 15, shot_events,
            [{'at':0, 'label':'Accepted pistol reference; three repetitions'}, {'at':4.9, 'label':'Shotgun A: three recorded close-side takes'}, {'at':9.8, 'label':'Shotgun B: three recorded isolated takes, medium outdoor source perspective'}])

    mechanics = [(.25 + index * .95, clips[f'pump_{index+1:02}'], 1) for index in range(3)]
    mechanics += [(3.5 + index * 1.45, clips[f'shell_insert_{index%3+1:02}'], 1) for index in range(4)]
    mechanics += [(9.5, clips['empty_trigger_01'], 1), (10.6, clips['hull_concrete_01'], 1)]
    preview('02-pump-load-and-hull', 13, mechanics,
            [{'at':0, 'label':'Three complete pump takes'}, {'at':3.2, 'label':'Four loading events; fourth repeats take 1'}, {'at':9.2, 'label':'Empty trigger candidate'}, {'at':10.3, 'label':'Actual empty shotgun hull on concrete'}])

    combined = []
    for index in range(4):
        start = .25 + index * 1.55
        combined += [(start, clips[f'shot_a_{index%3+1:02}'], 1), (start+.48, clips[f'pump_{index%3+1:02}'], .85),
                     (start+.88, clips['hull_concrete_01'], .6)]
    combined += [(7 + index * 1.4, clips[f'shell_insert_{index%3+1:02}'], 1) for index in range(4)]
    combined += [(13, clips['shot_a_02'], 1), (13.48, clips['pump_02'], .85), (13.88, clips['hull_concrete_01'], .6)]
    preview('03-four-shell-cycle', 16, combined,
            [{'at':0, 'label':'Four single shots, each with pump and hull'}, {'at':6.7, 'label':'Load four shells one at a time'}, {'at':12.7, 'label':'One shot after reload; timing is an audition, not approved balance'}])

    impacts = []
    for index, key in enumerate(('concrete', 'wood', 'flesh')):
        impacts.append((.3 + index * 4, clips['impact_' + key], 1))
        impacts += [(1.65 + index * 4, clips['shot_a_01'], .85), (1.72 + index * 4, clips['impact_' + key], .6)]
    preview('04-shared-impact-palette', 13, impacts,
            [{'at':0, 'label':'Concrete: impact alone, then shotgun with impact'}, {'at':4, 'label':'Wood: same structure'}, {'at':8, 'label':'Flesh: one accepted Stalker hit, same structure'}])

    result = dict(date='2026-10-16', status='Audition only, not approved or integrated', numpy=np.__version__,
                  reference='Accepted indoor pistol recording pipeline and existing material/creature impact palette',
                  processing='Recorded sources only; DC/45Hz high pass, broad -1dB high EQ, preserved attack, late shot decay .29s, fades, common linear gain within each take bank. No synthesized source, added reverb, pitch shift, saturation, or compression.',
                  sourceQuality='Public MP3 previews retained; 48kHz PCM24 WAV does not restore original lossless quality. B source is an actual UTS-15 slug-shot recording; it does not define gameplay ammunition.',
                  sourceRecords=sources, existingReferences=reference_report, clips=report, previews=preview_report,
                  limitations='Numerical and decoder QA only; take/event identity follows source descriptions and selected windows. Subjective listening and final in-game mixing remain for the owner. Loading montage uses 3 source events with the first repeated for the fourth insert.',
                  unityChanged=False)
    (BASE / 'manifest.json').write_bytes((json.dumps(result, ensure_ascii=False, indent=2) + '\n').encode('utf-8'))
    print(json.dumps(dict(clips=len(report), previews=len(preview_report), maximumPreviewPeak=max(item['peakAfterMp3'] for item in preview_report), unityChanged=False)))


if __name__ == '__main__':
    main()
