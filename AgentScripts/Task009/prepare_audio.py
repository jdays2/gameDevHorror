"""Reproducible offline TASK-009 auditions. Never writes Assets or invokes Unity."""
import argparse
import hashlib
import json
from pathlib import Path
import re
import subprocess
import wave

import numpy as np


ROOT = Path(__file__).resolve().parents[2]
FAMILY = ROOT / "Concepts/Unused/Audio/PlayerHitTask009"
SR = 48000
GAIN = .8


def decode(path, ffmpeg, filters=None):
    command = [ffmpeg, "-v", "error", "-i", str(path), "-ac", "1", "-ar", str(SR)]
    if filters:
        command += ["-af", filters]
    return np.frombuffer(subprocess.check_output(command + ["-f", "f32le", "-"]), "<f4").astype(float)


def write(path, signal):
    assert np.isfinite(signal).all()
    assert np.max(np.abs(signal)) < .99
    path.parent.mkdir(parents=True, exist_ok=True)
    with wave.open(str(path), "wb") as output:
        output.setnchannels(1)
        output.setsampwidth(2)
        output.setframerate(SR)
        output.writeframes(np.rint(signal * 32767).astype("<i2").tobytes())
    block = 96
    windows = signal[:len(signal) // block * block].reshape(-1, block)
    active = np.flatnonzero(np.sqrt(np.mean(windows**2, axis=1)) > max(.001, np.max(np.abs(signal)) * .01))
    return {"path": path.relative_to(ROOT).as_posix(), "seconds": len(signal) / SR,
            "peak": float(np.max(np.abs(signal))), "rms": float(np.sqrt(np.mean(signal**2))),
            "firstActiveMs": int(active[0]) * 2 if len(active) else None,
            "endSample": float(signal[-1]), "dcMean": float(np.mean(signal)),
            "clippedSamples": int(np.count_nonzero(np.abs(signal) >= 1)),
            "sha256": hashlib.sha256(path.read_bytes()).hexdigest()}


def loudness(path, ffmpeg):
    result = subprocess.run([ffmpeg, "-hide_banner", "-i", str(path), "-af",
                             "apad=pad_dur=1,loudnorm=I=-24:TP=-3:LRA=7:print_format=json",
                             "-f", "null", "-"], capture_output=True, text=True, check=True)
    return json.loads(re.findall(r"\{[^{}]+\}", result.stderr)[-1])


def fade(signal, release=.045):
    signal = signal.copy()
    attack = min(round(.001 * SR), len(signal))
    tail = min(round(release * SR), len(signal))
    signal[:attack] *= np.linspace(0, 1, attack)
    signal[-tail:] *= np.linspace(1, 0, tail)
    return signal


def add(mix, signal, time, gain=1):
    offset = round(time * SR)
    count = min(len(signal), len(mix) - offset)
    mix[offset:offset + count] += signal[:count] * gain


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--ffmpeg", required=True)
    args = parser.parse_args()
    edits = {}
    for key in "ABC":
        signal = decode(FAMILY / f"Sources/{key}_source.mp3", args.ffmpeg,
                        "highpass=f=110,lowpass=f=6500")
        if key == "A":
            signal = signal[:round(.29 * SR)]
        elif key == "B":
            signal = signal[:round(.32 * SR)]
        else:
            # Generated exhale starts late. Move its unvoiced section next to contact;
            # keep it quiet, short and explicitly unapproved as a character voice.
            contact = fade(signal[:round(.13 * SR)], .035)
            breath = fade(signal[round(.38 * SR):round(.69 * SR)], .07)
            signal = np.zeros(round(.35 * SR))
            add(signal, contact, 0)
            add(signal, breath, .025, .26)
        signal = fade(signal - signal.mean())
        path = FAMILY / f"Edits/{key}_base.wav"
        write(path, signal / max(1, np.max(np.abs(signal)) / .75))
        signal = decode(path, args.ffmpeg)
        measured = loudness(path, args.ffmpeg)
        edits[key] = (signal, float(measured["input_i"]))

    # Select one common achievable K-weighted integrated loudness. No individual
    # peak normalization masquerades as equal loudness.
    target = min(-24, *[level + 20 * np.log10(.68 / np.max(np.abs(signal)))
                       for signal, level in edits.values()])
    report = {"sampleRate": SR, "format": "PCM16 mono", "commonTargetLUFS": target,
              "auditionGain": GAIN, "normalization": "FFmpeg loudnorm input_i; common static gain; peak ceiling .68",
              "variants": {}, "previews": [], "context": "Offline montage using existing game samples, not a recorded playthrough."}
    families = {}
    for key, (signal, measured) in edits.items():
        signal *= 10**((target - measured) / 20)
        variants = []
        entries = []
        for index, rate in enumerate([1, .982, 1.021, .993], 1):
            independent = FAMILY / f"Sources/B_take{index:02}_source.mp3"
            if key == "B" and index > 1 and independent.exists():
                filters = "highpass=f=110,lowpass=f=6500"
                if index == 2:
                    filters += ",acompressor=threshold=0.03:ratio=4:attack=0.01:release=50"
                shifted = decode(independent, args.ffmpeg, filters)
                shifted = fade(shifted[:round(.32 * SR)])
                edit = FAMILY / f"Edits/B_take{index:02}.wav"
                write(edit, shifted / max(1, np.max(np.abs(shifted)) / .75))
                shifted = decode(edit, args.ffmpeg)
                level = float(loudness(edit, args.ffmpeg)["input_i"])
                shifted *= 10**((target - level) / 20)
                rate = 1
            else:
                shifted = np.interp(np.arange(0, len(signal) - 1, rate), np.arange(len(signal)), signal)
            shifted = fade(shifted)
            # Small gain differences are deliberate natural variation, < 0.5 dB.
            shifted *= [1, .97, 1.015, .99][index - 1]
            shifted *= min(1, .68 / np.max(np.abs(shifted)))
            path = FAMILY / f"Exports/{key}/player-hit-{index:02}.wav"
            entry = write(path, shifted)
            entry["loudness"] = loudness(path, args.ffmpeg)
            entry["sourceRate"] = rate
            entry["source"] = (independent if key == "B" and index > 1 and independent.exists()
                               else FAMILY / f"Sources/{key}_source.mp3").relative_to(ROOT).as_posix()
            entry["sourceSha256"] = hashlib.sha256((ROOT / entry["source"]).read_bytes()).hexdigest()
            entries.append(entry)
            variants.append(shifted)
        families[key] = variants
        report["variants"][key] = entries
        single = np.zeros(round(1.4 * SR))
        add(single, variants[0], .15, GAIN)
        report["previews"].append(write(FAMILY / f"Previews/{key}_single.wav", single))
        series = np.zeros(round(5.5 * SR))
        for index, time in enumerate([.15, 1, 1.85, 2.7, 3.55, 4.4]):
            add(series, variants[[0, 2, 1, 3, 2, 0][index]], time, GAIN)
        report["previews"].append(write(FAMILY / f"Previews/{key}_series.wav", series))

    # Identical context for all candidates; gains mirror current player profiles.
    audio = ROOT / "Assets/EmergencyShift/Audio"
    pistol = decode(audio / "FirearmMix02/pistol_01.wav", args.ffmpeg)
    step = decode(audio / "PlayerFootsteps02/shoe-contact-02.wav", args.ffmpeg)
    windup = decode(audio / "Stalker/Membrane04/stalker_windup_01.wav", args.ffmpeg)
    lunge = decode(audio / "Stalker/Membrane04/stalker_lunge_01.wav", args.ffmpeg)
    bite = decode(audio / "Stalker/Membrane04/stalker_bite_01.wav", args.ffmpeg)
    context = np.zeros(round(8 * SR))
    report["contextGains"] = {"pistol": .45, "playerStep": .24, "stalkerWindup": 1,
                              "stalkerLunge": .9, "stalkerAcceptedBite": 1,
                              "note": "Illustrative close-range montage; no native distance/pan/priority/mixer simulation."}
    report["contextTimeline"] = {"hitSeconds": [2.6, 6.2], "nearbyShotSeconds": [2.56, 6.16],
                                 "comparison": "Same accepted Bite and gunshot 40ms before each player hit in all three mixes."}
    for time in [.2, .5, .8, 3.4, 3.7, 6.8]:
        add(context, step, time, .24)
    for time in [1.1, 1.65, 2.56, 4.6, 5.2, 6.16]:
        add(context, pistol, time, .45)
    for time in [2.2, 5.8]:
        add(context, windup, time, 1)
        add(context, lunge, time + .3, .9)
        add(context, bite, time + .4, 1)
    for key in "ABC":
        mix = context.copy()
        for index, time in enumerate([2.6, 6.2]):
            add(mix, families[key][index], time, GAIN)
        report["previews"].append(write(FAMILY / f"Previews/{key}_context-offline.wav", mix))
    comparison = np.zeros(round(21.6 * SR))
    for index, key in enumerate("ABC"):
        add(comparison, decode(FAMILY / f"Previews/{key}_single.wav", args.ffmpeg), index * 7.2)
        add(comparison, decode(FAMILY / f"Previews/{key}_series.wav", args.ffmpeg), index * 7.2 + 1.5)
    report["previews"].append(write(FAMILY / "Previews/ABC_single-and-series.wav", comparison))
    (FAMILY / "processing.json").write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"targetLUFS": target, "baseLevels": {k: v[0]["loudness"]["input_i"] for k, v in report["variants"].items()},
                      "previews": len(report["previews"])}, indent=2))


if __name__ == "__main__":
    main()
