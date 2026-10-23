"""Encode the unboosted Unity main mix and authored-scene screenshots; report raw peaks."""
import argparse
import json
from pathlib import Path
import subprocess
import numpy as np
from PIL import Image


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--ffmpeg", required=True)
    parser.add_argument("--input", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--after-suffix", default="final")
    args = parser.parse_args()
    args.output.mkdir(parents=True, exist_ok=True)
    metrics = {}
    for scene in ("morgue", "sandbox"):
        clips = []
        for phase, suffix in (("before", "before"), ("after", args.after_suffix)):
            source = args.input / f"{scene}-{suffix}"
            report = json.loads((source / "report.json").read_text())
            mix = np.fromfile(source / "mix.f32", dtype="<f4")
            with_smoke = np.array(Image.open(source / "0022.png"), dtype=float)
            without_smoke = np.array(Image.open(source / "without-smoke.png"), dtype=float)
            delta = np.abs(with_smoke - without_smoke).max(axis=2)
            key = f"{scene}-{phase}"
            metrics[key] = {"peak": float(np.abs(mix).max()), "clipped_samples": int((np.abs(mix) >= 1).sum()),
                            "smoke_pixels_over_2_levels": int((delta > 2).sum()), "smoke_max_delta_8bit": float(delta.max()), "capture": report}
            (args.output / f"{key}.json").write_text(json.dumps(metrics[key], indent=2), encoding="utf-8")
            Image.open(source / "0022.png").save(args.output / f"{key}.png")
            video = args.output / f"{key}.mp4"
            label = f"{scene.upper()} / {phase.upper()} / original camera and main mix"
            subprocess.run([args.ffmpeg, "-y", "-loglevel", "error", "-framerate", "30", "-i", str(source / "%04d.png"),
                            "-f", "f32le", "-ar", "48000", "-ac", "2", "-i", str(source / "mix.f32"),
                            "-vf", f"drawtext=fontfile='C\\:/Windows/Fonts/arial.ttf':text='{label}':x=12:y=12:fontsize=18:fontcolor=white:box=1:boxcolor=black@0.65",
                            "-c:v", "libx264", "-crf", "20", "-pix_fmt", "yuv420p", "-c:a", "aac", "-b:a", "192k", "-shortest", str(video)], check=True)
            clips.append(video)
        concat = args.output / f"{scene}-concat.txt"
        concat.write_text("\n".join(f"file '{path.name}'" for path in clips), encoding="utf-8")
        subprocess.run([args.ffmpeg, "-y", "-loglevel", "error", "-f", "concat", "-safe", "0", "-i", str(concat), "-c", "copy", "-movflags", "+faststart", str(args.output / f"{scene}-comparison.mp4")], check=True)
        concat.unlink()
        for video in clips:
            video.unlink()
    # A silent slow-motion view makes the short lateral expansion easy to judge.
    source = args.input / f"morgue-{args.after_suffix}"
    bursts = []
    for label, start in (("PISTOL", 14), ("SHOTGUN", 134)):
        video = args.output / f"{label.lower()}-burst.mp4"
        subprocess.run([args.ffmpeg, "-y", "-loglevel", "error", "-framerate", "10", "-start_number", str(start),
                        "-i", str(source / "%04d.png"), "-t", "2.4", "-r", "30", "-an", "-vf",
                        f"drawtext=fontfile='C\\:/Windows/Fonts/arial.ttf':text='{label} / 0.33x / shape review':x=12:y=12:fontsize=18:fontcolor=white:box=1:boxcolor=black@0.65",
                        "-c:v", "libx264", "-crf", "18", "-pix_fmt", "yuv420p", str(video)], check=True)
        bursts.append(video)
    concat = args.output / "bursts-concat.txt"
    concat.write_text("\n".join(f"file '{path.name}'" for path in bursts), encoding="utf-8")
    subprocess.run([args.ffmpeg, "-y", "-loglevel", "error", "-f", "concat", "-safe", "0", "-i", str(concat), "-c", "copy", "-movflags", "+faststart", str(args.output / "side-smoke.mp4")], check=True)
    concat.unlink()
    for video in bursts:
        video.unlink()
    (args.output / "metrics.json").write_text(json.dumps(metrics, indent=2), encoding="utf-8")
    print(json.dumps({key: {k: v for k, v in value.items() if k != "capture"} for key, value in metrics.items()}, indent=2))


if __name__ == "__main__":
    main()
