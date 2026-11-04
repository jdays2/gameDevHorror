"""Package native main-mix capture without normalization or replacing game audio."""
import argparse
import hashlib
import json
from pathlib import Path
import subprocess

import numpy as np


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--ffmpeg", required=True)
    parser.add_argument("--input", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    metadata = json.loads((args.input / "capture.json").read_text(encoding="utf-8-sig"))
    rate = metadata["sampleRate"]
    channels = metadata["channels"]
    samples = np.fromfile(args.input / "mix.f32", dtype="<f4")
    assert len(samples) > 0 and len(samples) % channels == 0
    assert np.isfinite(samples).all()
    args.output.mkdir(parents=True, exist_ok=True)
    metrics = {"nativeCapture": metadata, "samples": len(samples),
               "seconds": len(samples) / channels / rate,
               "peak": float(np.max(np.abs(samples))),
               "rms": float(np.sqrt(np.mean(samples.astype(float)**2))),
               "clippedSamples": int(np.count_nonzero(np.abs(samples) >= 1)),
               "processing": "Unchanged native float32 samples; PCM24 WAV and AAC video encode, no gain/normalization",
               "rawSha256": hashlib.sha256((args.input / "mix.f32").read_bytes()).hexdigest()}
    subprocess.run([args.ffmpeg, "-y", "-v", "error", "-f", "f32le", "-ar", str(rate),
                    "-ac", str(channels), "-i", str(args.input / "mix.f32"),
                    "-c:a", "pcm_s24le", str(args.output / "native-main-mix.wav")], check=True)
    if (args.input / "0000.png").exists():
        frame_input = ["-framerate", str(metadata["fps"]), "-i", str(args.input / "%04d.png")]
        if metadata.get("framesObserved") and "audioSamples" in metadata["framesObserved"][0]:
            manifest = args.input / "frames.ffconcat"
            lines = ["ffconcat version 1.0"]
            for frame in metadata["framesObserved"]:
                lines += [f"file '{frame['frame']:04d}.png'", "option framerate 48000",
                          f"duration {frame['audioSamples'] / rate:.9f}"]
            lines.append(f"file '{metadata['framesObserved'][-1]['frame']:04d}.png'")
            manifest.write_text("\n".join(lines) + "\n", encoding="utf-8")
            frame_input = ["-f", "concat", "-safe", "0", "-i", str(manifest)]
        subprocess.run([args.ffmpeg, "-y", "-v", "error", *frame_input, "-f", "f32le", "-ar", str(rate),
                        "-ac", str(channels), "-i", str(args.input / "mix.f32"),
                        "-vf", "fps=30,pad=ceil(iw/2)*2:ceil(ih/2)*2", "-c:v", "libx264", "-crf", "20",
                        "-pix_fmt", "yuv420p", "-c:a", "aac", "-b:a", "192k", "-shortest",
                        "-movflags", "+faststart", str(args.output / "native-gameplay.mp4")], check=True)
    (args.output / "mix-analysis.json").write_text(json.dumps(metrics, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"candidate": metadata.get("candidate"), "shots": metadata.get("shots"),
                      "hits": metadata.get("PlayedCount"), "seconds": metrics["seconds"],
                      "peak": metrics["peak"], "rms": metrics["rms"],
                      "clippedSamples": metrics["clippedSamples"]}, indent=2))


if __name__ == "__main__":
    main()
