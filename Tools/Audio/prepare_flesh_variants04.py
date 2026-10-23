"""Make six close sound-design variants of the owner's selected B, retaining B itself."""
import argparse
import hashlib
import json
from pathlib import Path
import subprocess
import numpy as np

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / "Concepts/Unused/Audio/FleshImpactStudies04/B-heavy-wet-rupture.wav"
ASSETS = ROOT / "Assets/EmergencyShift/Audio/FleshContacts04"
FAMILY = ROOT / "Concepts/Used/Audio/FleshContacts04"
RATE = 48000
# Body/tissue/skin gains, tissue rate, tissue delay ms, skin rate, length.
VARIANTS = [
    ("flesh-contact-B", "Original B", 1., 1., 1., 1., 0., 1., .34),
    ("flesh-contact-B-02", "Dense body", 1.13, .97, .86, .97, 1.5, 1.01, .33),
    ("flesh-contact-B-03", "Sharp puncture", .91, 1.02, 1.15, 1.07, -.8, 1.04, .30),
    ("flesh-contact-B-04", "Wet tear", .97, 1.13, .97, .93, 2.5, .99, .35),
    ("flesh-contact-B-05", "Compact crunch", 1.06, .96, 1.06, 1.10, -.5, 1.06, .29),
    ("flesh-contact-B-06", "Fibrous rupture", .95, 1.10, 1.04, 1.03, 3.5, .96, .34),
]


def decode(encoder, path):
    result = subprocess.run([encoder, "-v", "error", "-i", str(path), "-ar", str(RATE), "-ac", "2", "-f", "f32le", "-"], check=True, capture_output=True)
    return np.frombuffer(result.stdout, dtype="<f4").reshape(-1, 2).copy()


def write(encoder, path, signal):
    path.parent.mkdir(parents=True, exist_ok=True)
    subprocess.run([encoder, "-y", "-v", "error", "-f", "f32le", "-ar", str(RATE), "-ac", "2", "-i", "-", "-c:a", "pcm_s24le", str(path)], input=signal.astype("<f4").tobytes(), check=True)


def warp(signal, rate, delay_ms=0):
    samples = np.arange(len(signal))
    position = (samples - delay_ms * RATE / 1000) * rate
    return np.column_stack([np.interp(position, samples, signal[:, channel], left=0, right=0) for channel in range(2)])


def rms(signal):
    return float(np.sqrt(np.mean(signal * signal)))


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--ffmpeg", required=True)
    args = parser.parse_args()
    original = decode(args.ffmpeg, SOURCE)
    # Zero padding avoids wrapping a tail into the attack during the complementary band split.
    padding = RATE // 4
    padded = np.pad(original, ((padding, padding), (0, 0)))
    frequencies = np.fft.rfftfreq(len(padded), 1 / RATE)
    spectrum = np.fft.rfft(padded, axis=0)
    low_mask = 1 / (1 + (frequencies / 380) ** 4)
    high_mask = 1 - 1 / (1 + (frequencies / 3200) ** 4)
    body = np.fft.irfft(spectrum * low_mask[:, None], n=len(padded), axis=0)[padding:padding + len(original)]
    skin = np.fft.irfft(spectrum * high_mask[:, None], n=len(padded), axis=0)[padding:padding + len(original)]
    tissue = original - body - skin
    reference = decode(args.ffmpeg, ASSETS / "flesh-contact-B.wav")
    reference_rms = rms(reference[:RATE // 5])
    rows, clips = [], []
    for index, (name, label, body_gain, tissue_gain, skin_gain, tissue_rate, delay, skin_rate, seconds) in enumerate(VARIANTS):
        path = ASSETS / (name + ".wav")
        if index == 0:
            signal = reference
        else:
            signal = body * body_gain + warp(tissue, tissue_rate, delay) * tissue_gain + warp(skin, skin_rate) * skin_gain
            signal = original * .35 + signal * .65
            # Keep the first contact instant recognizable; change the subsequent crunch/tear.
            blend = np.clip((np.arange(len(signal)) / RATE - .004) / .012, 0, 1)[:, None]
            signal = original * (1 - blend) + signal * blend
            signal = signal[:round(seconds * RATE)].copy()
            signal *= np.clip((len(signal) - 1 - np.arange(len(signal))) / (RATE * .04), 0, 1)[:, None]
            signal *= reference_rms / rms(signal[:RATE // 5])
            signal *= min(1, .80 / float(np.abs(signal).max()))
            write(args.ffmpeg, path, signal)
            signal = decode(args.ffmpeg, path)
        peak = float(np.abs(signal).max())
        attack_db = 20 * np.log10(rms(signal[:RATE // 5]) / reference_rms)
        if peak > .801 or abs(attack_db) > 1.25 or not np.isfinite(signal).all() or len(signal) / RATE / .97 >= .4:
            raise ValueError(f"Variant exceeds the family level or four-voice duration boundary: {name}, peak={peak}, attackDb={attack_db}, seconds={len(signal) / RATE}")
        rows.append({"name":name,"character":label,"sourceSha256":hashlib.sha256(SOURCE.read_bytes()).hexdigest(),"sha256":hashlib.sha256(path.read_bytes()).hexdigest(),
                     "seconds":len(signal) / RATE,"peak":peak,"attackRmsDbVsB":float(attack_db),"clippedSamples":int((np.abs(signal)>=1).sum()),
                "processing":{"originalBlend":1 if index == 0 else .35,"bodyGain":body_gain,"tissueGain":tissue_gain,"skinGain":skin_gain,"tissueRate":tissue_rate,"tissueDelayMs":delay,"skinRate":skin_rate}})
        clips.append(signal)
    if len({entry["sha256"] for entry in rows}) != 6:
        raise ValueError("Duplicate audio in the variation bank")
    preview = np.zeros((round(RATE * .65) * len(clips), 2))
    for index, clip in enumerate(clips):
        start = index * round(RATE * .65)
        preview[start:start+len(clip)] = clip
    write(args.ffmpeg, FAMILY / "Preview/B-six-variants.wav", preview)
    subprocess.run([args.ffmpeg,"-y","-v","error","-i",str(FAMILY / "Preview/B-six-variants.wav"),"-c:a","libmp3lame","-b:a","192k",str(FAMILY / "Preview/B-six-variants.mp3")],check=True)
    mp3 = decode(args.ffmpeg, FAMILY / "Preview/B-six-variants.mp3")
    if np.abs(mp3).max() >= 1:
        raise ValueError("Preview encoding clipped")
    manifest = {"date":"2026-10-23","source":str(SOURCE.relative_to(ROOT)).replace("\\","/"),"method":"offline complementary bands, independent tissue/skin timing, restrained envelope and level matching; original B unchanged",
                "sampleRate":RATE,"channels":2,"pcmBits":24,"runtimeGain":.28,"originalTaskId":"f372d894-36b2-4c46-9823-af7f078e469f","variants":rows,
                "preview":{"path":"Preview/B-six-variants.mp3","decodedPeak":float(np.abs(mp3).max()),"clippedSamples":int((np.abs(mp3)>=1).sum())}}
    (FAMILY / "variants.json").write_text(json.dumps(manifest,indent=2),encoding="utf-8")
    print(json.dumps({"variants":[{"name":r["name"],"seconds":r["seconds"],"peak":r["peak"],"attackRmsDbVsB":r["attackRmsDbVsB"]} for r in rows],"preview":manifest["preview"]},indent=2))


if __name__ == "__main__":
    main()
