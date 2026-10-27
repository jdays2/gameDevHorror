"""Convert the original insect alarm to a short mono PCM clip; no pitch/synthesis/layering."""
from pathlib import Path
import argparse, hashlib, json, subprocess, wave
import numpy as np

ROOT = Path(__file__).resolve().parent
PROJECT = ROOT.parents[3]

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--ffmpeg', required=True)
    args = parser.parse_args()
    source = ROOT / 'Originals/insect-screech.mp3'
    decoded = subprocess.run([args.ffmpeg, '-v', 'error', '-i', str(source), '-ac', '1', '-ar', '32000', '-f', 'f32le', '-'], check=True, capture_output=True).stdout
    samples = np.frombuffer(decoded, dtype='<f4').copy()
    active = np.flatnonzero(np.abs(samples) > .004)
    start = max(0, int(active[0]) - 64) if len(active) else 0
    end = min(len(samples), int(active[-1]) + 641) if len(active) else len(samples)
    samples = samples[start:end]
    peak = float(np.max(np.abs(samples)))
    gain = min(1, .7079458 / max(peak, .000001))
    samples *= gain
    fade_in, fade_out = min(64, len(samples)), min(480, len(samples))
    samples[:fade_in] *= np.linspace(0, 1, fade_in)
    samples[-fade_out:] *= np.linspace(1, 0, fade_out)
    output = PROJECT / 'Assets/EmergencyShift/Audio/Enemies/Scorpion/Detection.wav'
    with wave.open(str(output), 'wb') as wav:
        wav.setparams((1, 2, 32000, 0, 'NONE', 'not compressed'))
        wav.writeframes(np.round(samples * 32767).astype('<i2').tobytes())
    report = dict(source=source.relative_to(PROJECT).as_posix(), output=output.relative_to(PROJECT).as_posix(),
                  channels=1, sampleRate=32000, sampleBits=16, seconds=len(samples)/32000,
                  leadingSamplesRemoved=start, trailingSamplesRemoved=len(decoded)//4-end, gain=gain,
                  peak=float(np.max(np.abs(samples))), rms=float(np.sqrt(np.mean(samples*samples))),
                  fadeInMs=2, fadeOutMs=15, pitchChanged=False, equalization=False, layers=False,
                  originalSha256=hashlib.sha256(source.read_bytes()).hexdigest(), outputSha256=hashlib.sha256(output.read_bytes()).hexdigest(),
                  auditoryReview=False)
    (ROOT / 'processing.json').write_text(json.dumps(report, indent=2)+'\n')
    print(json.dumps(report))

if __name__ == '__main__':
    main()
