"""Package real Unity presentation captures; no generative repaint or frame selection.

Each contact sheet contains consecutive frames, and every source frame occurs once in AllFrames.
Boundary strips retain neighbors of state/body-family changes. Numeric flags identify review
candidates and never substitute for visual inspection of the corresponding sheets.
"""
import argparse
import hashlib
import json
import math
import re
from pathlib import Path
import subprocess

import numpy as np
from PIL import Image, ImageDraw, ImageFont

BASE = Path(__file__).resolve().parent
PROJECT = BASE.parents[4]
FONT = ImageFont.truetype("C:/Windows/Fonts/arial.ttf", 16)
SMALL = ImageFont.truetype("C:/Windows/Fonts/arial.ttf", 14)


def read_json(path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def write_json(path, value):
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2), encoding="utf-8")


def family(name):
    name = name or "NULL"
    if name.startswith("body-"):
        return "whole-body"
    return re.sub(r"\d+", "", name).rstrip("-_")


def presentation_checks(frames):
    teleport_failures = []
    stopped_phase_failures = []
    stagger_phase_failures = []
    closed_eye_walking_samples = []
    for index, frame in enumerate(frames):
        if index == 0:
            continue
        prior = frames[index - 1]
        delta = (frame["phase"] - prior["phase"]) % 1
        changed = delta > .00001 and delta < .99999
        if frame.get("Teleport") and changed:
            teleport_failures.append(index)
        speed_squared = sum(v * v for v in frame["velocity"])
        if speed_squared < .000001 and frame["scenario"] == prior["scenario"] and changed:
            stopped_phase_failures.append(index)
        if frame["staggered"] and changed:
            stagger_phase_failures.append(index)
        if frame["posture"] == "Extended" and frame["closure"] >= .166 and speed_squared > .01 and not frame["staggered"]:
            closed_eye_walking_samples.append({"frame": index, "phaseAdvanced": changed, "body": frame["body"]})
    death = [f for f in frames if f["state"] == "Dead"]
    real_kill = any(f.get("killCalls", 0) for f in death)
    kill_check = {"performed": real_kill}
    if real_kill:
        first_dead = next(index for index, frame in enumerate(frames) if frame["state"] == "Dead")
        last_live_body = frames[first_dead - 1]["body"] if first_dead else None
        visible_dead = [frame for frame in death if frame["bodyEnabled"]]
        kill_check.update({
            "killCalls": max(f.get("killCalls", 0) for f in death),
            "deathEvents": max(f.get("deathEvents", 0) for f in death),
            "maxRemainsRoots": max(f.get("remainsRoots", 0) for f in death),
            "originalBodyHiddenFrom": next((f["frame"] for f in death if not f["bodyEnabled"]), None),
            "duplicateDeathOrRemains": max(f.get("deathEvents", 0) for f in death) > 1 or max(f.get("remainsRoots", 0) for f in death) > 1,
            "lastLiveBody": last_live_body,
            "visibleDeadSamplesBeforeReplacement": len(visible_dead),
            "liveBodyPreservedUntilReplacement": last_live_body is not None and all(frame["body"] == last_live_body for frame in visible_dead),
            "preRuptureBodyChanges": [frame["frame"] for frame in visible_dead if frame["body"] != last_live_body],
        })
    return {
        "teleportPhaseRejectionFailures": teleport_failures,
        "stoppedPhaseFailures": stopped_phase_failures,
        "staggerPhaseFailures": stagger_phase_failures,
        "closedEyeExtendedWalking": closed_eye_walking_samples,
        "actualKill": kill_check,
        "scope": "Checks actual recorded animator facts; no claim of artistic approval or complete AI validation.",
    }


def sheet(folder, frames, indices, destination, title, columns=4, cell=320):
    width = columns * cell
    height = math.ceil(len(indices) / columns) * (cell + 49) + 38
    result = Image.new("RGB", (width, height), "#172019")
    draw = ImageDraw.Draw(result)
    draw.text((10, 10), title, fill="white", font=FONT)
    for position, index in enumerate(indices):
        frame = frames[index]
        source = Image.open(folder / f"frame-{index:05d}.png").convert("RGB")
        source.thumbnail((cell, cell), Image.Resampling.LANCZOS)
        x = position % columns * cell
        y = position // columns * (cell + 49) + 38
        result.paste(source, (x, y))
        draw.text((x + 5, y + cell + 1), f"{index:05d} {frame['state']} / {frame['body']}", fill="white", font=SMALL)
        fold = frame.get("fold")
        fold_text = "legacy" if fold is None else f"fold {fold:.3f}"
        draw.text((x + 5, y + cell + 23), f"{fold_text} eye {frame['closure']:.2f} phase {frame['phase']:.3f}", fill="#b0c8b2", font=SMALL)
    destination.parent.mkdir(parents=True, exist_ok=True)
    result.save(destination, quality=88, subsampling=0)


def process(folder, encode=True, all_sheets=True):
    manifest = read_json(folder / "manifest.json")
    frames = manifest["frames"]
    captures = []
    flags = []
    previous_mask = None
    previous_count = None
    previous_family = None
    previous_ortho = None
    previous_border = 0
    boundary_indices = set()
    for frame in frames:
        index = frame["frame"]
        path = folder / f"frame-{index:05d}.png"
        with Image.open(path) as image:
            pixels = np.asarray(image.convert("RGB"))
        # Neutral exact green is the diagnostic backdrop. Soft skin edges remain foreground.
        green = (pixels[:, :, 1] > 220) & (pixels[:, :, 0] < 20) & (pixels[:, :, 2] < 20)
        mask = ~green
        yy, xx = np.where(mask)
        count = int(mask.sum())
        border = int(mask[:2].sum() + mask[-2:].sum() + mask[:, :2].sum() + mask[:, -2:].sum())
        ortho = frame.get("orthographicSize", manifest["orthographicSize"])
        area_ratio = count * ortho ** 2 / max(1, (previous_count or count) * (previous_ortho or ortho) ** 2)
        current_family = family(frame["body"])
        prior_visible = index > 0 and frames[index - 1].get("bodyEnabled", True)
        family_changed = previous_family is not None and current_family != previous_family and prior_visible and frame.get("bodyEnabled", True)
        state_changed = index > 0 and frame["state"] != frames[index - 1]["state"]
        xor = int(np.count_nonzero(mask != previous_mask)) if previous_mask is not None else 0
        captures.append({
            "frame": index,
            "pixelBounds": [int(xx.min()), int(yy.min()), int(xx.max()), int(yy.max())] if count else None,
            "foregroundPixels": count,
            "foregroundRatioToPrevious": round(area_ratio, 5),
            "silhouetteXorPixels": xor,
            "borderForegroundPixels": border,
            "sha256": hashlib.sha256(path.read_bytes()).hexdigest(),
        })
        reasons = []
        if border:
            reasons.append("capture-border-clipping")
        if count == 0:
            reasons.append("empty-frame")
        if frame["body"] is None:
            reasons.append("missing-body-sprite")
        if family_changed:
            reasons.append("body-art-family-boundary")
        if area_ratio < .75 or area_ratio > 1.3334:
            reasons.append("foreground-area-jump")
        if reasons:
            flags.append({"frame": index, "scenario": frame["scenario"], "reasons": reasons, "before": frames[max(0, index - 1)]["body"], "after": frame["body"], "areaRatio": area_ratio})
        # One strip at each end of a contiguous clipping interval is enough;
        # per-frame flags remain in JSON and every frame has an AllFrames cell.
        meaningful_reasons = set(reasons) - {"capture-border-clipping"}
        if meaningful_reasons or state_changed or (border and not previous_border):
            boundary_indices.add(index)
        if previous_border and not border:
            boundary_indices.add(index - 1)
        previous_mask = mask
        previous_count = count
        previous_family = current_family
        previous_ortho = ortho
        previous_border = border
    if previous_border:
        boundary_indices.add(len(frames) - 1)
    for span in manifest["spans"]:
        boundary_indices.add(span["first"])
        boundary_indices.add(span["last"])
    if all_sheets:
        for start in range(0, len(frames), 16):
            stop = min(start + 16, len(frames))
            sheet(folder, frames, list(range(start, stop)), folder / "AllFrames" / f"frames-{start:05d}-{stop-1:05d}.jpg", f"{folder.name} — consecutive frames {start}…{stop-1}")
    expected_boundaries = {f"boundary-{index:05d}.jpg" for index in boundary_indices}
    for old_sheet in (folder / "Boundaries").glob("boundary-*.jpg"):
        if old_sheet.name not in expected_boundaries:
            old_sheet.unlink()
    for index in sorted(boundary_indices):
        indices = list(range(max(0, index - 5), min(len(frames), index + 7)))
        sheet(folder, frames, indices, folder / "Boundaries" / f"boundary-{index:05d}.jpg", f"{folder.name} — {frames[index]['scenario']} / frame {index}")
    if encode:
        workspace = PROJECT.parents[1]
        binary = workspace / "work/creature-tools/Lib/site-packages/imageio_ffmpeg/binaries/ffmpeg-win-x86_64-v7.1.exe"
        if not binary.exists():
            raise FileNotFoundError(binary)
        subprocess.run([str(binary), "-y", "-loglevel", "error", "-framerate", "60", "-i", str(folder / "frame-%05d.png"), "-c:v", "libx264", "-crf", "18", "-pix_fmt", "yuv420p", "-movflags", "+faststart", str(folder / "green.mp4")], check=True)
    report = {
        "profile": folder.name,
        "frames": len(frames),
        "scenarios": len(manifest["spans"]),
        "missingFrames": [f["frame"] for f in frames if not (folder / f"frame-{f['frame']:05d}.png").exists()],
        "visibleBodySpriteCount": len(set(f["body"] for f in frames)),
        "captureClippedFrames": [f["frame"] for f in captures if f["borderForegroundPixels"]],
        "reviewFlags": flags,
        "numericFlagsAreArtApproval": False,
        "presentationChecks": presentation_checks(frames),
        "samples": captures,
    }
    write_json(folder / "frame-analysis.json", report)
    return {key: report[key] for key in ("profile", "frames", "scenarios", "visibleBodySpriteCount", "captureClippedFrames", "reviewFlags")}


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("run", nargs="?", default="final")
    parser.add_argument("--no-video", action="store_true")
    parser.add_argument("--no-all-sheets", action="store_true")
    parser.add_argument("--profile", default=None)
    args = parser.parse_args()
    run = BASE / args.run
    manifests = sorted(run.glob("*/manifest.json"))
    if args.profile:
        manifests = [p for p in manifests if args.profile in p.parent.name]
    reports = [process(path.parent, not args.no_video, not args.no_all_sheets) for path in manifests]
    write_json(run / "analysis-summary.json", reports)
    print(json.dumps([{**{k: v for k, v in report.items() if k not in ("reviewFlags", "captureClippedFrames")},
        "captureClippedFrameCount": len(report["captureClippedFrames"]),
        "reviewFlagCount": len(report["reviewFlags"])} for report in reports], ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
