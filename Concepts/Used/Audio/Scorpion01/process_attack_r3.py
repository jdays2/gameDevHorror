"""Fresh heavy-shell alternatives; no r2 mixture, pitch change or synthetic bass."""
import hashlib
import json
import subprocess
import wave
import numpy as np
from process_audio import ROOT, PROJECT, FFMPEG, RATE, OUT


def main():
    reports = []
    (ROOT/'Previews').mkdir(exist_ok=True)
    for index, label in [(1, 'HeavyShellA'), (2, 'HeavyShellB')]:
        raw = subprocess.check_output([str(FFMPEG), '-v', 'error', '-i', str(ROOT/'Sources'/f'attack-r3-{index}.mp3'),
            '-f', 'f32le', '-ac', '1', '-ar', str(RATE), '-'])
        x = np.frombuffer(raw, dtype='<f4').astype(np.float64)
        x -= x.mean()
        energy = np.convolve(x*x, np.ones(160)/160, mode='same')
        start = int(np.argmax(energy)) - round(.0448*RATE)
        if start < 0:
            x = np.pad(x, (-start, 0))
            start = 0
        x = x[start:start+round(.34*RATE)].copy()
        x = np.pad(x, (0, round(.34*RATE)-len(x)))
        x[:128] *= np.linspace(0, 1, 128)
        x[-1440:] *= np.linspace(1, 0, 1440)
        x *= .75/max(np.max(np.abs(x)), 1e-8)
        pcm = np.round(x*32767).astype('<i2')
        path = ROOT/'Previews'/f'{label}.wav'
        with wave.open(str(path), 'wb') as w:
            w.setnchannels(1)
            w.setsampwidth(2)
            w.setframerate(RATE)
            w.writeframes(pcm.tobytes())
        reports.append(dict(file=path.relative_to(PROJECT).as_posix(), seconds=len(x)/RATE,
            peak=float(np.max(np.abs(x))), rms=float(np.sqrt(np.mean(x*x))),
            accentSeconds=int(np.argmax(np.convolve(x*x, np.ones(160)/160, mode='same')))/RATE,
            clipped=int(np.sum(np.abs(pcm)==32767)), sha256=hashlib.sha256(path.read_bytes()).hexdigest()))
    # B is the fuller paired closure prompt; A remains directly auditionable.
    (OUT/'Attack.wav').write_bytes((ROOT/'Previews/HeavyShellB.wav').read_bytes())
    production = dict(reports[1], file=(OUT/'Attack.wav').relative_to(PROJECT).as_posix())
    report = dict(revision='heavy-shell-r3', active='HeavyShellB', cuePhase=.32, strikePhase=.40,
        attackSeconds=.56, methods='Original sources; mono32k conversion, 5ms-RMS contact selection/alignment, normalization, 4/45ms edge fades. No pitch, EQ, layer mixing or distortion.',
        outputs=reports, production=production,
        auditoryApproval=False, limitation='No audio input available to agent. Candidate B installed for review; owner has rejected r1 and r2.')
    (ROOT/'attack-r3-processing.json').write_text(json.dumps(report, indent=2)+'\n')
    processing = json.loads((ROOT/'processing.json').read_text())
    processing['outputs'] = [p for p in processing['outputs'] if not p['file'].replace('\\','/').endswith('/Attack.wav')]
    processing['outputs'].append(production)
    processing['attackRevision'] = 'attack-r3-processing.json'
    (ROOT/'processing.json').write_text(json.dumps(processing, indent=2)+'\n')
    print(json.dumps(report, indent=2))


if __name__ == '__main__':
    main()
