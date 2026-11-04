"""Offline bodily-injury auditions after owner rejected the cloth/friction bank."""
import argparse
import hashlib
import json

import numpy as np

from prepare_audio import ROOT, SR, add, decode, fade, loudness, write


FAMILY = ROOT / "Concepts/Unused/Audio/PlayerHitTask009/Bite02"
FILTERS = "highpass=f=70,acompressor=threshold=0.04:ratio=4:attack=0.01:release=60,lowpass=f=2000:p=2,lowpass=f=2000:p=2"


def spectrum(signal):
    power = np.abs(np.fft.rfft(signal * np.hanning(len(signal)))) ** 2
    frequencies = np.fft.rfftfreq(len(signal), 1 / SR)
    total = float(power.sum())
    return {
        "energyAbove3kHz": float(power[frequencies >= 3000].sum() / total),
        "energy80to800Hz": float(power[(frequencies >= 80) & (frequencies <= 800)].sum() / total),
        "powerCentroidHz": float((power * frequencies).sum() / total),
    }


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--ffmpeg", required=True)
    args = parser.parse_args()
    prepared = {}
    for key in "DEF":
        source = FAMILY / f"Sources/{key}_source.mp3"
        signal = decode(source, args.ffmpeg, FILTERS)
        block = round(.002 * SR)
        windows = signal[:len(signal) // block * block].reshape(-1, block)
        energy = np.sqrt(np.mean(windows ** 2, axis=1))
        active = np.flatnonzero(energy > max(.003, float(energy.max()) * .035))
        assert len(active), "No audible source event"
        start = max(0, int(active[0]) * block - block)
        end = min(len(signal), (int(active[-1]) + 2) * block, start + round(.42 * SR))
        signal = signal[start:end]
        signal = fade(signal - signal.mean(), release=.035)
        path = FAMILY / f"Edits/{key}_filtered.wav"
        write(path, signal / max(1, float(np.max(np.abs(signal))) / .8))
        signal = decode(path, args.ffmpeg)
        level = float(loudness(path, args.ffmpeg)["input_i"])
        prepared[key] = (signal, level, {"source": str(source.relative_to(ROOT)).replace("\\", "/"),
                                       "sourceSha256": hashlib.sha256(source.read_bytes()).hexdigest(),
                                       "sourceStartSeconds": start / SR, "sourceEndSeconds": end / SR})

    target = min(-24, *[level + 20 * np.log10(.65 / np.max(np.abs(signal)))
                       for signal, level, _ in prepared.values()])
    result = {"task": "TASK-009", "revision": "Bite02", "status": "Offline auditions, owner assessment pending",
              "filters": FILTERS, "maximumDurationSeconds": .42, "releaseSeconds": .035,
              "commonTargetLUFS": float(target), "normalization": "Common static loudness target measured with the same one-second padding; peak ceiling .65",
              "previewGain": .8, "candidates": {}, "previews": [],
              "limits": "Measurements describe spectrum and levels; they do not prove the desired injury character or owner approval."}
    combined = np.zeros(round(12.4 * SR))
    for slot, (key, (signal, level, origin)) in enumerate(prepared.items()):
        signal *= 10 ** ((target - level) / 20)
        signal *= min(1, .65 / np.max(np.abs(signal)))
        path = FAMILY / f"Exports/{key}_hit.wav"
        entry = write(path, signal)
        entry.update(origin)
        entry["loudness"] = loudness(path, args.ffmpeg)
        entry["spectrum"] = spectrum(signal)
        result["candidates"][key] = entry
        preview = np.zeros(round(4.6 * SR))
        for time in [.3, 2.3]:
            add(preview, signal, time, .8)
            add(combined, signal, slot * 4 + time, .8)
        result["previews"].append(write(FAMILY / f"Previews/{key}_double.wav", preview))
    result["previews"].append(write(FAMILY / "Previews/DEF_comparison.wav", combined))
    result["comparisonOrder"] = ["D at .3/2.3s", "E at 4.3/6.3s", "F at 8.3/10.3s"]
    result["priorBankSpectrum"] = {f"B{index}": spectrum(decode(ROOT / f"Assets/EmergencyShift/Audio/PlayerHit01/player-hit-{index:02}.wav", args.ffmpeg))
                                   for index in range(1, 5)}
    (FAMILY / "processing.json").write_text(json.dumps(result, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"targetLUFS": target, "candidates": {key: {"seconds": item["seconds"], "LUFS": item["loudness"]["input_i"],
                       "peak": item["peak"], **item["spectrum"]} for key, item in result["candidates"].items()},
                       "oldBankSpectrum": result["priorBankSpectrum"]}, indent=2))


if __name__ == "__main__":
    main()
