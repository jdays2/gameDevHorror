"""Rebuild only the organic claw cue; retain the rejected r1 source and all other sounds."""
from pathlib import Path
import hashlib
import json
import subprocess
import wave
import numpy as np
from process_audio import ROOT, PROJECT, FFMPEG, RATE, OUT

DURATION = .34
ACCENT = .0448  # (.40 strike - .32 cue) * .56-second attack.


def filtered(name, speed, cutoff):
    raw = subprocess.check_output([
        str(FFMPEG), '-v', 'error', '-i', str(ROOT/'Sources'/f'{name}.mp3'),
        '-af', f'aresample={RATE},asetrate={RATE*speed},aresample={RATE},highpass=f=65,lowpass=f={cutoff}:p=2',
        '-f', 'f32le', '-ac', '1', '-ar', str(RATE), '-'])
    return np.frombuffer(raw, dtype='<f4').copy().astype(np.float64)


def accent_sample(x):
    energy = np.convolve(x*x, np.ones(160)/160, mode='same')
    return int(np.argmax(energy))


def aligned(x):
    start = accent_sample(x) - round(ACCENT*RATE)
    if start < 0:
        x = np.pad(x, (-start, 0))
        start = 0
    result = x[start:start+round(DURATION*RATE)].copy()
    result = np.pad(result, (0, round(DURATION*RATE)-len(result)))
    result -= result.mean()
    result[:128] *= np.linspace(0, 1, 128)
    result[-1920:] *= np.linspace(1, 0, 1920) ** 1.5
    result /= max(np.max(np.abs(result)), .000001)
    return result


def save(path, x):
    x = x * (.78 / np.max(np.abs(x)))
    pcm = np.round(x*32767).astype('<i2')
    with wave.open(str(path), 'wb') as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes(pcm.tobytes())
    return dict(file=path.relative_to(PROJECT).as_posix(), seconds=len(x)/RATE,
        peak=float(np.max(np.abs(x))), rms=float(np.sqrt(np.mean(x*x))),
        clipped=int(np.sum(np.abs(pcm)==32767)), accentSeconds=accent_sample(x)/RATE,
        sha256=hashlib.sha256(path.read_bytes()).hexdigest())


def main():
    # Resample first so pitch processing is independent of the source codec's sample rate.
    palm = aligned(filtered('attack-r2-2', .80, 2500))
    shell = aligned(filtered('attack-r2-1', .78, 3200))
    body = aligned(filtered('attack-r2-2', .60, 700))
    cue = aligned(np.tanh((.64*palm + .16*shell + .32*body)*5))
    output = save(OUT/'Attack.wav', cue)
    report = dict(revision='organic-clamp-r2', outputs=[output],
        method='Two original generated organic grasps, slowed playback, 65Hz high-pass, 700/2500/3200Hz low-pass layers, tanh soft compression, aligned 5ms RMS accent, DC removal, peak -2.16dBFS, 4ms attack/60ms release fades.',
        cuePhase=.32, strikePhase=.40, attackSeconds=.56, targetAccentSeconds=ACCENT,
        slowestPitch=.96, latestEndSeconds=.32*.56+DURATION/.96,
        note='Waveform/timing QA only. This agent cannot hear audio; owner artistic acceptance remains open. No reference samples copied.')
    (ROOT/'attack-r2-processing.json').write_text(json.dumps(report, indent=2)+'\n')
    processing = json.loads((ROOT/'processing.json').read_text())
    processing['outputs'] = [p for p in processing['outputs'] if not p['file'].replace('\\','/').endswith('/Attack.wav')]
    processing['outputs'].append(output)
    processing['attackRevision'] = 'attack-r2-processing.json'
    (ROOT/'processing.json').write_text(json.dumps(processing, indent=2)+'\n')
    print(json.dumps(report, indent=2))


if __name__ == '__main__':
    main()
