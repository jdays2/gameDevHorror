"""Original generated source -> mono PCM, conservative fades, bounded production cues."""
from pathlib import Path
import hashlib, json, subprocess, wave
import numpy as np

ROOT = Path(__file__).resolve().parent
PROJECT = ROOT.parents[3]
FFMPEG = PROJECT.parents[1] / 'work/creature-tools/Lib/site-packages/imageio_ffmpeg/binaries/ffmpeg-win-x86_64-v7.1.exe'
OUT = PROJECT / 'Assets/EmergencyShift/Audio/Enemies/Scorpion'
OUT.mkdir(parents=True, exist_ok=True)
RATE = 32000

def decode(key):
    raw = subprocess.check_output([str(FFMPEG), '-v', 'error', '-i', str(ROOT/'Sources'/f'{key}.mp3'), '-f', 'f32le', '-ac', '1', '-ar', str(RATE), '-'])
    return np.frombuffer(raw, dtype='<f4').copy()

def save(name, x, peak=.78, loop=False):
    x = x.astype(np.float64)
    x -= np.mean(x)
    if np.max(np.abs(x)) > .00001:
        x *= peak / np.max(np.abs(x))
    fade = min(int(.012*RATE), len(x)//8)
    x[:fade] *= np.linspace(0, 1, fade)
    x[-fade:] *= np.linspace(1, 0, fade)
    path = OUT / (name+'.wav')
    pcm = np.clip(x*32767, -32767, 32767).astype('<i2')
    with wave.open(str(path), 'wb') as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes(pcm.tobytes())
    return dict(file=str(path.relative_to(PROJECT)), seconds=len(x)/RATE, peak=float(np.max(np.abs(x))), rms=float(np.sqrt(np.mean(x*x))), clipped=int(np.sum(np.abs(pcm)==32767)), sha256=hashlib.sha256(path.read_bytes()).hexdigest())

def main():
    report=[]
    steps=decode('steps')
    # Select one short, high-energy contact from each non-overlapping source quarter.
    for i, part in enumerate(np.array_split(steps, 4)):
        window=int(.24*RATE)
        energy=np.convolve(part*part, np.ones(int(.025*RATE))/int(.025*RATE), mode='same')
        peak=int(np.argmax(energy))
        start=max(0, min(peak-int(.035*RATE), len(part)-window))
        report.append(save('Step'+str(i+1), part[start:start+window], .60))
    for key,limit,peak in [('hit',.50,.85),('alarm',1.45,.83)]:
        x=decode(key)
        active=np.flatnonzero(np.abs(x)>.02*np.max(np.abs(x)))
        start=max(0, int(active[0])-int(.012*RATE)) if len(active) else 0
        report.append(save(key.title(), x[start:start+int(limit*RATE)], peak))
    report.append(save('Presence',decode('idle'),.55,True))
    (ROOT/'processing.json').write_text(json.dumps(dict(rate=RATE,channels=1,bits=16,outputs=report,method='Original generated MP3, DC removal, mono resample, contact extraction, peak normalization and 12ms edge fades. No reference audio copied.'),indent=2)+'\n')
    from process_attack_r3 import main as rebuild_attack
    rebuild_attack()
    print(json.dumps(report,indent=2))


if __name__ == '__main__':
    main()
