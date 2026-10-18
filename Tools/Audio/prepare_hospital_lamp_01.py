"""Short recorded fluorescent-lamp sound studies with an explicit light timeline.

Offline source preparation only. Runtime use is recorded separately in integration.json.
Requires the retained CC0 MP3 previews in Sources.
"""
import argparse
import hashlib
import json
from pathlib import Path
import subprocess
import wave

import numpy as np


ROOT = Path(__file__).resolve().parents[2]
FAMILY = ROOT / 'Concepts/Used/Audio/HospitalLamp01'
SR = 48000
DURATION = 8.2
CUTS = [(1.31, 1.73), (18.54, 18.87), (29.04, 29.44)]
PROBES = [
    {'id': 'ward-soft', 'label': 'Палата · мягкое мерцание', 'humGain': .7,
     'eventGain': .65, 'darkIntervals': [[4.90, 5.02], [5.16, 5.25]]},
    {'id': 'corridor-faulty', 'label': 'Коридор · неисправный стартер', 'humGain': 1.0,
     'eventGain': 1.0, 'darkIntervals': [[1.80, 1.90], [2.02, 2.15], [4.40, 4.68], [4.80, 4.92], [5.15, 5.50]]},
]


def decode(ffmpeg, path, filters):
    data = subprocess.check_output([ffmpeg, '-v', 'error', '-i', str(path), '-ac', '1', '-ar', str(SR),
                                    '-af', filters, '-f', 'f32le', '-'])
    return np.frombuffer(data, '<f4').astype(float)


def level(signal, rms, peak):
    signal = signal.copy()
    signal -= signal.mean()
    gain = min(rms / max(np.sqrt(np.mean(signal ** 2)), 1e-9), peak / max(abs(signal).max(), 1e-9))
    return signal * gain


def fade(signal, attack=.003, release=.018):
    signal = signal.copy()
    a = min(len(signal), round(attack * SR))
    r = min(len(signal), round(release * SR))
    signal[:a] *= np.linspace(0, 1, a)
    signal[-r:] *= np.linspace(1, 0, r)
    return signal


def write(path, signal):
    assert np.isfinite(signal).all() and abs(signal).max() < .99
    with wave.open(str(path), 'wb') as stream:
        stream.setnchannels(1)
        stream.setsampwidth(2)
        stream.setframerate(SR)
        stream.writeframes(np.rint(signal * 32767).astype('<i2').tobytes())
    return {'file': path.relative_to(FAMILY).as_posix(), 'seconds': len(signal) / SR,
            'peak': round(float(abs(signal).max()), 6),
            'rms': round(float(np.sqrt(np.mean(signal ** 2))), 6),
            'sha256': hashlib.sha256(path.read_bytes()).hexdigest()}


def loop_from(signal, overlap):
    n = round(overlap * SR)
    blend = np.linspace(0, 1, n)
    seam = signal[-n:] * (1 - blend) + signal[:n] * blend
    return np.concatenate((seam, signal[n:-n]))


def lamp_mask(intervals, length):
    mask = np.ones(length)
    ramp = round(.004 * SR)
    for start, end in intervals:
        a, b = round(start * SR), round(end * SR)
        mask[a:b] = 0
        mask[a-ramp:a] = np.minimum(mask[a-ramp:a], np.linspace(1, 0, ramp))
        mask[b:b+ramp] = np.minimum(mask[b:b+ramp], np.linspace(0, 1, ramp))
    return mask


def add(target, signal, at, gain):
    start = round(at * SR)
    count = min(len(signal), len(target) - start)
    target[start:start+count] += signal[:count] * gain


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--ffmpeg', required=True)
    args = parser.parse_args()
    (FAMILY / 'Clips').mkdir(parents=True, exist_ok=True)
    (FAMILY / 'Previews').mkdir(exist_ok=True)
    hum = decode(args.ffmpeg, FAMILY / 'Sources/hum.mp3', 'highpass=f=90,lowpass=f=4800')
    starter = decode(args.ffmpeg, FAMILY / 'Sources/starter.mp3', 'highpass=f=150,lowpass=f=6200')
    loop = loop_from(hum[round(.25 * SR):round(3.55 * SR)], .16)
    loop = level(loop, .045, .18)
    clips = [write(FAMILY / 'Clips/hum-loop.wav', loop)]
    contacts = []
    for index, (start, end) in enumerate(CUTS):
        contact = fade(level(starter[round(start * SR):round(end * SR)], .045, .28))
        contacts.append(contact)
        clips.append(write(FAMILY / f'Clips/flicker-{index+1:02}.wav', contact))
    previews = []
    for probe in PROBES:
        length = round(DURATION * SR)
        mask = lamp_mask(probe['darkIntervals'], length)
        audio = np.resize(loop, length) * mask * probe['humGain']
        events = []
        for index, (start, end) in enumerate(probe['darkIntervals']):
            # The short electrical contact may continue as the tube goes dark;
            # the steady hum follows the light's actual power envelope.
            off_clip = index % len(contacts)
            on_clip = (index + 1) % len(contacts)
            add(audio, contacts[off_clip], start, probe['eventGain'] * .32)
            add(audio, contacts[on_clip], end, probe['eventGain'] * .72)
            events.extend([{'at': start, 'event': 'dark', 'clip': off_clip + 1},
                           {'at': end, 'event': 'restrike', 'clip': on_clip + 1}])
        audio = fade(audio, .12, .28)
        record = write(FAMILY / ('Previews/' + probe['id'] + '.wav'), audio)
        previews.append({**probe, **record, 'events': events})
        # Timing diagram only: a schematic lamp bar, not a generated game sprite.
        dark_expression = '+'.join(f'between(t,{start},{end})' for start, end in probe['darkIntervals'])
        enabled = f'not({dark_expression})'
        filters = ['drawbox=x=130:y=196:w=700:h=55:color=0x46545b:t=fill',
                   'drawbox=x=155:y=209:w=650:h=28:color=0x1c292c:t=fill']
        for x, y, width, height, color in [(136, 182, 688, 81, '0x83c6cc@0.06'),
                                          (149, 195, 662, 56, '0x83c6cc@0.10'),
                                          (158, 214, 644, 17, '0x94d5db'),
                                          (160, 220, 640, 5, '0xe1f5f4')]:
            filters.append(f"drawbox=x={x}:y={y}:w={width}:h={height}:color={color}:t=fill:enable='{enabled}'")
        subprocess.run([args.ffmpeg, '-v', 'error', '-y', '-f', 'lavfi', '-i',
                        f'color=c=0x10191d:s=960x480:r=30:d={DURATION}', '-i', str(FAMILY / record['file']),
                        '-vf', ','.join(filters), '-c:v', 'libx264', '-preset', 'fast', '-crf', '23',
                        '-pix_fmt', 'yuv420p', '-c:a', 'aac', '-b:a', '160k', '-shortest', '-movflags', '+faststart',
                        str(FAMILY / ('Previews/' + probe['id'] + '.mp4'))], check=True)
    metadata = {'date': '2026-10-18', 'status': 'Recorded source family; current Unity use is in integration.json',
                'method': 'Edits of retained CC0 real recordings; no generative model or synthetic tones',
                'processor': 'Tools/Audio/prepare_hospital_lamp_01.py', 'sampleRate': SR, 'format': 'PCM16 mono WAV',
                'sourceManifest': 'sources.json', 'starterCutsSeconds': CUTS,
                'humSourceSeconds': [.25, 3.55], 'loopCrossfadeSeconds': .16,
                'filters': {'hum': 'highpass90/lowpass4800', 'contact': 'highpass150/lowpass6200'},
                'clips': clips, 'previews': previews,
                'synchronization': 'Offline audio and schematic MP4 use the same darkIntervals; runtime follows actual Light2D output as described in integration.json',
                'integrationReference': 'integration.json', 'processorChangesUnity': False}
    (FAMILY / 'provenance.json').write_text(json.dumps(metadata, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    print(json.dumps({'clips': len(clips), 'auditions': len(previews),
                      'previewPeaks': [p['peak'] for p in previews], 'unityChanged': False}))


if __name__ == '__main__':
    main()
