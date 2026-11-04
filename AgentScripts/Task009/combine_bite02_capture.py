"""Join three native auditions with visible labels and unchanged source levels."""
import argparse
import hashlib
import json
from pathlib import Path
import subprocess


ROOT = Path(__file__).resolve().parents[2]


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--ffmpeg", required=True)
    parser.add_argument("--revision", default="Bite02")
    parser.add_argument("--candidates", default="DEF")
    args = parser.parse_args()
    if not args.revision.isalnum() or len(args.candidates) != 3 or not args.candidates.isalpha():
        raise ValueError("Use an alphanumeric revision and three candidate letters.")
    output = ROOT / f"Docs/Validation/PlayerHitTask009/{args.revision}-native"
    inputs = []
    filters = []
    segments = []
    output_path = output / f"{args.candidates}-native-gameplay.mp4"
    if output_path.exists():
        raise FileExistsError("Preserve the existing comparison before rerunning.")
    for index, key in enumerate(args.candidates):
        folder = output / key
        metrics = json.loads((folder / "mix-analysis.json").read_text(encoding="utf-8-sig"))
        capture = metrics["nativeCapture"]
        assert capture["candidate"] == key and capture["shots"] == 0 and capture["PlayedCount"] == 4
        assert capture["originalProfileUnchanged"] and metrics["clippedSamples"] == 0
        video = folder / "native-gameplay.mp4"
        audio = folder / "native-main-mix.wav"
        inputs += ["-i", str(video), "-i", str(audio)]
        filters += [f"[{index * 2}:v]setpts=PTS-STARTPTS,drawtext=fontfile=/Windows/Fonts/arial.ttf:text={key}:fontsize=48:fontcolor=white:box=1:boxcolor=black@0.75:boxborderw=12:x=w-tw-30:y=30[v{index}]",
                    f"[{index * 2 + 1}:a]asetpts=PTS-STARTPTS[a{index}]"]
        segments.append({"candidate": key, "seconds": metrics["seconds"], "hits": 4, "shots": 0,
                         "peak": metrics["peak"], "clippedSamples": 0,
                         "video": video.relative_to(ROOT).as_posix(),
                         "videoSha256": hashlib.sha256(video.read_bytes()).hexdigest(),
                         "audio": audio.relative_to(ROOT).as_posix(),
                         "audioSha256": hashlib.sha256(audio.read_bytes()).hexdigest(),
                         "nativeRawSha256": metrics["rawSha256"]})
    filters.append("[v0][a0][v1][a1][v2][a2]concat=n=3:v=1:a=1[v][a]")
    subprocess.run([args.ffmpeg, "-y", "-v", "error", *inputs, "-filter_complex", ";".join(filters),
                    "-map", "[v]", "-map", "[a]", "-c:v", "libx264", "-crf", "20", "-pix_fmt", "yuv420p",
                    "-c:a", "aac", "-b:a", "192k", "-movflags", "+faststart", str(output_path)], check=True)
    audio_inputs = []
    for key in args.candidates:
        audio_inputs += ["-i", str(output / key / "native-main-mix.wav")]
    subprocess.run([args.ffmpeg, "-y", "-v", "error", *audio_inputs,
                    "-filter_complex", "[0:a][1:a][2:a]concat=n=3:v=0:a=1[a]", "-map", "[a]",
                    "-c:a", "pcm_s24le", str(output / f"{args.candidates}-native-main-mix.wav")], check=True)
    result = {"task": "TASK-009", "revision": args.revision + " native comparison", "segments": segments,
              "order": ", then ".join(args.candidates) + "; four actual accepted hits each, zero shots",
              "processing": "Visible candidate letter and concatenation only. Native WAV levels unchanged; AAC video encode. Concat may pad audio by less than one video frame at segment boundaries.",
              "limits": "One audition source per direction, repeated on four native hits with the existing profile pitch range; not a finished variation bank or artistic approval."}
    (output / "comparison.json").write_text(json.dumps(result, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"output": str(output_path), "segments": segments}, indent=2))


if __name__ == "__main__":
    main()
