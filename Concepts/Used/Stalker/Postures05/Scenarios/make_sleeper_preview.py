"""Assemble actual reviewed samples at 1x; never repaint or interpolate a frame."""
import hashlib
import json
import shutil
import subprocess
from pathlib import Path

import numpy as np
from PIL import Image

PROJECT = Path(__file__).resolve().parents[5]
BASE = Path(__file__).resolve().parent
KEY = "MechanicsSandbox-sleeper-live-ce174f83"
OLD = BASE / "sleeper-final" / KEY
NEW = BASE / "eye-threshold-final" / KEY
DEST = PROJECT / "Concepts/Used/Sleeper/Postures05/Review"
EVIDENCE = PROJECT / "Concepts/Unused/Visual/StalkerWholeFrameReview01/StaticEvidence"


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def relative(path):
    return path.relative_to(PROJECT).as_posix()


def main():
    DEST.mkdir(exist_ok=True)
    old = json.loads((OLD / "manifest.json").read_text(encoding="utf-8-sig"))["frames"]
    new = json.loads((NEW / "manifest.json").read_text(encoding="utf-8-sig"))["frames"]
    keys = ("body", "frameIndex", "phase", "closure", "fold", "eye", "visualRotation", "bodyLocalPosition", "bodyLocalScale")
    assert all(old[i].get(key) == new[i].get(key) for i in range(105) for key in keys)
    differences = []
    for index in range(105):
        a = np.asarray(Image.open(OLD / f"frame-{index:05d}.png").convert("RGB"))
        b = np.asarray(Image.open(NEW / f"frame-{index:05d}.png").convert("RGB"))
        mask = np.any(a != b, axis=2)
        if mask.any():
            yy, xx = np.where(mask)
            differences.append({"frame": index, "changedPixels": int(mask.sum()),
                                "bounds": [int(xx.min()), int(yy.min()), int(xx.max()), int(yy.max())]})
    assert [row["frame"] for row in differences] == list(range(90, 101))
    assert all(290 <= row["bounds"][0] <= row["bounds"][2] <= 350 and
               290 <= row["bounds"][1] <= row["bounds"][3] <= 350 for row in differences)
    ranges = [(0, 0, 105, "sleep-and-corrected-natural-wake"),
              (1, 105, 285, "walk-up-and-down"),
              (1, 525, 687, "attack-recovery-and-walk"),
              (1, 1114, 1200, "hit-interruption"),
              (1, 1200, 1275, "partial-wake-target-death")]
    filters = [f"[{source}:v]trim=start_frame={first}:end_frame={end},setpts=PTS-STARTPTS[v{i}]"
               for i, (source, first, end, _) in enumerate(ranges)]
    filters.append("".join(f"[v{i}]" for i in range(len(ranges))) + f"concat=n={len(ranges)}:v=1:a=0[out]")
    ffmpeg = PROJECT.parents[1] / "work/creature-tools/Lib/site-packages/imageio_ffmpeg/binaries/ffmpeg-win-x86_64-v7.1.exe"
    output = DEST / "sleeper-final-x1.mp4"
    subprocess.run([str(ffmpeg), "-hide_banner", "-loglevel", "error", "-y", "-i", str(NEW / "green.mp4"),
                    "-i", str(OLD / "green.mp4"), "-filter_complex", ";".join(filters), "-map", "[out]",
                    "-an", "-c:v", "libx264", "-crf", "16", "-preset", "fast", "-pix_fmt", "yuv420p", str(output)], check=True)
    proof = {"sampleHz": 60, "playbackSpeed": 1, "spatialResampling": False, "temporalInterpolation": False,
             "frames": sum(end - first for _, first, end, _ in ranges), "rangeEndsExclusive": True,
             "sources": [{"path": relative(folder / "green.mp4"), "sha256": sha(folder / "green.mp4"),
                          "manifestSha256": sha(folder / "manifest.json")} for folder in (NEW, OLD)],
             "scenes": [{"source": source, "firstFrame": first, "endFrame": end, "name": name}
                        for source, first, end, name in ranges],
             "outputSha256": sha(output), "prefixMetadataIdentical": True, "prefixPixelDifferences": differences,
             "joinProof": "Source prefix metadata equal; only eye pixels F90..100 differ. F101..104 are byte-identical, including gaze, before the splice into walking.",
             "scope": "Montage of actual controlled production presentation scenarios. Corrected eye replay supplies sleep/wake; unchanged open-eye walking/combat and partial-wake cut come from reviewed full capture. Not a continuous free-AI playtest."}
    (DEST / "sleeper-final-x1.json").write_text(json.dumps(proof, ensure_ascii=False, indent=2), encoding="utf-8")
    EVIDENCE.mkdir(exist_ok=True)
    source = NEW / "frame-00020.png"
    selected = EVIDENCE / "sleeper-sleep-runtime.png"
    shutil.copyfile(source, selected)
    (EVIDENCE / "sleeper-sleep-runtime.json").write_text(json.dumps({"source": relative(source),
        "sha256": sha(source), "manifestSha256": sha(NEW / "manifest.json"),
        "frame": 20, "state": new[20]["state"], "body": new[20]["body"],
        "eye": new[20]["eye"], "eyeOpen": new[20]["eyeOpen"], "modifiedPixels": False}, indent=2), encoding="utf-8")
    print(json.dumps({"output": relative(output), "frames": proof["frames"], "seconds": proof["frames"] / 60,
                      "outputSha256": proof["outputSha256"]}, indent=2))


if __name__ == "__main__":
    main()
