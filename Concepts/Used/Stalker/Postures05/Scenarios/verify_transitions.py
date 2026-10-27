"""Review recorded presentation boundaries; never certify artwork from numeric scores.

Exact failures are missing samples, live render/capture loss, imported sprite metadata
that differs from its packing manifest, and unrequested body-local transform changes.
Silhouette/central-image changes are review candidates, since a legitimate mouth or
fold transition can change them. Rotation is removed before comparing image samples.
"""
import argparse
import hashlib
import json
import math
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw

BASE = Path(__file__).resolve().parent
PROJECT = BASE.parents[4]
GREEN = (0, 255, 0)
BASE_CASES = {
    "01-idle", "03-walk-up", "04-walk-down", "05-turn-90-and-180",
    "06-start-stop-teleport", "13-attack-windup-strike-recovery",
    "15-attack-to-walk", "16-stagger-hit-stop-and-resume", "19-target-dead-stop",
    "20-death-actual-kill-gore", "27-attack-to-hit-state-cut",
}
STALKER_CASES = {
    "07-recoil-open-eye-to-shut", "08-retreat-compact", "09-wait-fully-folded",
    "10-recover-open-and-walk", "11-restore-compact-turn", "12-recover-before-combat",
    "21a-waiting-before-close-attack", "21b-light-off-close-unfold", "21c-natural-close-attack",
    "21d-close-attack-resume", "22a-folded-before-checkpoint-cut",
    "22b-checkpoint-restored-attack", "22c-checkpoint-attack-resume",
    "28-attack-to-light-fear-state-cut",
}
SLEEPER_CASES = {
    "02-waking-entire-transition", "14-post-lunge-recovery", "17-restore-waking-half",
    "18-restore-hunting", "21-sleep-before-near-attack", "22-wake-to-near-attack-boundary",
    "23-hunt-near-one-frame", "24-near-attack-after-wake", "25-near-attack-recovery",
    "26-near-attack-resume",
    "29-waking-to-target-death",
}


def read(path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def write(path, value):
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2), encoding="utf-8")


def sprite_expectations():
    expected = {}
    sources = []
    for species in ("Stalker", "Sleeper"):
        for name in ("sprites.json", "attack-sprites.json", "AttackAdjacent01/attack-sprites.json"):
            path = PROJECT / "Concepts/Used" / species / "Postures05" / name
            if not path.exists():
                continue
            manifest = read(path)
            sources.append({"path": str(path.relative_to(PROJECT)), "sha256": digest(path)})
            for frame in manifest["frames"]:
                atlas = manifest["atlases"][frame["atlas"]]["asset"]
                rect = frame["rect"]
                expected[(atlas, frame["name"])] = {
                    "spriteRect": rect,
                    "spritePivotPixels": [frame["pivot"][0] * rect[2], frame["pivot"][1] * rect[3]],
                    "spritePixelsPerUnit": manifest["pixelsPerUnit"],
                }
    return expected, sources


def close(a, b, tolerance):
    return len(a) == len(b) and all(math.isfinite(float(x)) and abs(x - y) <= tolerance for x, y in zip(a, b))


def canonical_image(path, frame):
    with Image.open(path) as source:
        image = source.convert("RGB")
    # A deliberate world rotation is not a pivot jump. Nearest sampling adds no blur
    # to the diagnostic comparison, and source PNGs are never modified.
    return np.asarray(image.rotate(-frame["visualRotation"], Image.Resampling.NEAREST, fillcolor=GREEN))


def silhouette(pixels):
    return ~((pixels[:, :, 1] > 220) & (pixels[:, :, 0] < 20) & (pixels[:, :, 2] < 20))


def image_delta(prior, current, ortho):
    left = silhouette(prior)
    right = silhouette(current)
    union = int(np.count_nonzero(left | right))
    xor = int(np.count_nonzero(left ^ right))
    height, width = left.shape
    yy, xx = np.ogrid[:height, :width]
    radius = min(height, width) / (ortho * 2) * .3
    core = (xx - (width - 1) / 2) ** 2 + (yy - (height - 1) / 2) ** 2 < radius ** 2
    central_delta = float(np.abs(prior.astype(np.float32) - current.astype(np.float32))[core].mean() / 255)
    return {"silhouetteXorPixels": xor, "silhouetteXorOverUnion": xor / max(1, union),
            "silhouetteIoU": 1 - xor / max(1, union), "centralRgbMeanDelta": central_delta}


def save_strip(folder, frames, index):
    selected = frames[max(0, index - 3):min(len(frames), index + 3)]
    if any(not (folder / f"frame-{frame['frame']:05d}.png").exists() for frame in selected):
        return None
    with Image.open(folder / f"frame-{selected[0]['frame']:05d}.png") as source:
        size = source.size[0]
    sheet = Image.new("RGB", (size * 3, (size + 48) * 2), "#172019")
    draw = ImageDraw.Draw(sheet)
    for cell, frame in enumerate(selected):
        x, y = cell % 3 * size, cell // 3 * (size + 48)
        with Image.open(folder / f"frame-{frame['frame']:05d}.png") as source:
            sheet.paste(source.convert("RGB"), (x, y + 48))
        draw.text((x + 6, y + 5), f"{frame['frame']:05d} {frame['state']} {frame['body']}", fill="white")
        draw.text((x + 6, y + 24), f"fold={frame.get('fold')} attack={frame['attack']:.3f} local={frame['local']}", fill="white")
    destination = folder / "TransitionReview"
    destination.mkdir(exist_ok=True)
    path = destination / f"cut-{index:05d}.png"
    sheet.save(path)
    return str(path.relative_to(folder))


def review_profile(path, expected, require_coverage=False, strips=True):
    manifest = read(path)
    frames = manifest["frames"]
    folder = path.parent
    failures = []
    candidates = []
    pairs = []
    images_sha = hashlib.sha256()
    previous = None
    previous_pixels = None
    local_baseline = None
    checked_imports = 0
    checked_local = 0
    live_samples = 0
    unmatched_live = []
    missing_live_metadata = []
    seen = {span["scenario"] for span in manifest["spans"]}
    needed = BASE_CASES | (SLEEPER_CASES if manifest["species"].startswith("sleeper") else STALKER_CASES)
    missing_cases = sorted(needed - seen)
    if require_coverage and missing_cases:
        failures.append({"kind": "incomplete-current-scenario-coverage", "scenarios": missing_cases})
    for span in manifest["spans"]:
        selected = frames[span["first"]:span["last"] + 1]
        if len(selected) != span["count"] or any(frame["scenario"] != span["scenario"] or frame["local"] != local
                                                 for local, frame in enumerate(selected)):
            failures.append({"kind": "scenario-span-or-local-sample-mismatch", "span": span})
    for index, frame in enumerate(frames):
        if frame["frame"] != index:
            failures.append({"kind": "nonconsecutive-manifest", "index": index, "frame": frame["frame"]})
        image_path = folder / f"frame-{frame['frame']:05d}.png"
        if not image_path.exists():
            failures.append({"kind": "missing-png", "frame": frame["frame"]})
            previous = previous_pixels = None
            continue
        images_sha.update(image_path.name.encode())
        images_sha.update(bytes.fromhex(digest(image_path)))
        dead = frame["state"] == "Dead"
        metadata = expected.get((frame["bodyAsset"], frame["body"]))
        if not dead:
            live_samples += 1
            if metadata is None:
                unmatched_live.append(index)
            if any(frame.get(key) is None for key in ("spritePivotPixels", "spriteRect", "spritePixelsPerUnit",
                                                       "bodyLocalPosition", "bodyLocalScale", "bodyLocalRotation")):
                missing_live_metadata.append(index)
        if metadata and "spritePivotPixels" in frame:
            checked_imports += 1
            for key, value in metadata.items():
                actual = frame.get(key)
                good = actual is not None
                if good and isinstance(value, list):
                    good = close(actual, value, .01)
                elif good:
                    good = math.isfinite(actual) and abs(actual - value) < .001
                if not good:
                    failures.append({"kind": "sprite-import-mismatch", "frame": index, "field": key,
                                     "expected": value, "actual": actual})
        if not dead and "bodyLocalPosition" in frame:
            local = {key: frame[key] for key in ("bodyLocalPosition", "bodyLocalScale", "bodyLocalRotation")}
            if local_baseline is None:
                local_baseline = local
            checked_local += 1
            if not math.isfinite(local["bodyLocalRotation"]) or not close(
                    local["bodyLocalPosition"], local_baseline["bodyLocalPosition"], .00001) or not close(
                    local["bodyLocalScale"], local_baseline["bodyLocalScale"], .00001) or abs(
                    (local["bodyLocalRotation"] - local_baseline["bodyLocalRotation"] + 180) % 360 - 180) > .0001:
                failures.append({"kind": "body-local-transform-change", "frame": index, "baseline": local_baseline, "actual": local})
        pixels = canonical_image(image_path, frame)
        if not dead:
            with Image.open(image_path) as source:
                raw_mask = silhouette(np.asarray(source.convert("RGB")))
            border = int(raw_mask[0].sum() + raw_mask[-1].sum() + raw_mask[:, 0].sum() + raw_mask[:, -1].sum())
            if border or not raw_mask.any() or not frame.get("bodyEnabled", True) or frame["body"] is None:
                failures.append({"kind": "live-render-or-capture-loss", "frame": index, "borderForegroundPixels": border,
                                 "emptyForeground": not bool(raw_mask.any()), "bodyEnabled": frame.get("bodyEnabled", True)})
        ortho = frame.get("orthographicSize", manifest["orthographicSize"])
        if previous is not None and not dead and previous["state"] != "Dead" and ortho == previous.get(
                "orthographicSize", manifest["orthographicSize"]):
            delta = image_delta(previous_pixels, pixels, ortho)
            reasons = []
            state_changed = frame["state"] != previous["state"]
            scenario_changed = frame["scenario"] != previous["scenario"]
            if state_changed:
                reasons.append("state-boundary")
            if scenario_changed:
                reasons.append("scenario-boundary")
            if frame.get("RestoreRotation"):
                reasons.append("explicit-checkpoint-cut")
            fold = frame.get("fold")
            prior_fold = previous.get("fold")
            if fold is not None and prior_fold is not None and abs(fold - prior_fold) > .15:
                reasons.append("body-fold-step-over-0.15")
            if delta["silhouetteXorOverUnion"] > .2:
                reasons.append("silhouette-change-over-20-percent")
            if delta["centralRgbMeanDelta"] > .05:
                reasons.append("central-image-change-over-5-percent")
            pair = {"frame": index, "scenario": frame["scenario"], "fromState": previous["state"],
                    "toState": frame["state"], "fromBody": previous["body"], "toBody": frame["body"],
                    "intentionalRestoreCut": bool(frame.get("RestoreRotation")),
                    **{key: round(value, 6) if isinstance(value, float) else value for key, value in delta.items()}}
            pairs.append(pair)
            if reasons:
                candidates.append({**pair, "reasons": reasons})
        previous, previous_pixels = frame, pixels
    top = sorted(pairs, key=lambda pair: pair["silhouetteXorOverUnion"], reverse=True)[:12]
    selected = {pair["frame"] for pair in top}
    selected.update(pair["frame"] for pair in candidates if "state-boundary" in pair["reasons"] or
                    "explicit-checkpoint-cut" in pair["reasons"])
    strips_written = [save_strip(folder, frames, index) for index in sorted(selected)] if strips else []
    strips_written = [path for path in strips_written if path is not None]
    return {"profile": folder.name, "manifestSha256": digest(path), "orderedCapturePngSha256": images_sha.hexdigest(),
            "frames": len(frames), "missingCurrentCases": missing_cases, "exactFailures": failures,
            "spriteImportSamplesChecked": checked_imports, "bodyLocalTransformSamplesChecked": checked_local,
            "liveSamples": live_samples, "unmatchedLiveSpriteFrames": unmatched_live,
            "missingLiveExactMetadataFrames": missing_live_metadata,
            "missingExactMetadata": bool(unmatched_live or missing_live_metadata),
            "largestSilhouetteChanges": top, "reviewCandidates": candidates, "nativePixelStrips": strips_written}


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("run", nargs="?", default="stalker-dense-final")
    parser.add_argument("--profile")
    parser.add_argument("--require-current-coverage", action="store_true")
    parser.add_argument("--require-exact-metadata", action="store_true")
    parser.add_argument("--no-strips", action="store_true")
    args = parser.parse_args()
    folder = BASE / args.run
    setup = read(folder / "run.json")
    expected, sources = sprite_expectations()
    paths = sorted(folder.glob("*/manifest.json"))
    if args.profile:
        paths = [path for path in paths if args.profile in path.parent.name]
    setup_failures = []
    if not setup.get("completedAll", False) or setup.get("captureErrors") or not paths:
        setup_failures.append({"kind": "incomplete-or-failed-run", "completedAll": setup.get("completedAll"),
                               "captureErrors": setup.get("captureErrors"), "manifests": len(paths)})
    if not args.profile:
        declared = {item["key"] for item in setup.get("completed", [])}
        actual = {path.parent.name for path in paths}
        if declared != actual:
            setup_failures.append({"kind": "completed-profile-set-differs-from-manifests",
                                   "declared": sorted(declared), "actual": sorted(actual)})
    reviews = [review_profile(path, expected, args.require_current_coverage, not args.no_strips) for path in paths]
    if args.require_exact_metadata:
        for review in reviews:
            if review["missingExactMetadata"]:
                review["exactFailures"].append({"kind": "missing-sprite-import-or-body-local-metadata"})
    result = {"run": args.run, "captureRunSha256": digest(folder / "run.json"), "packingManifests": sources,
              "scope": "Controlled snapshot presentation diagnostics; no claim of real AI hit/light transitions.",
              "numericResultIsArtApproval": False, "setupFailures": setup_failures,
              "exactDiagnosticsPass": not setup_failures and not any(review["exactFailures"] for review in reviews),
              "limitations": ["Review candidate scores cannot distinguish intentional limb/mouth motion from a bad seam.",
                              "Packing metadata verifies the imported pivot and PPU against the fixed-canvas recipe; normalized .5 is not required.",
                              "Camera edge contact detects capture clipping; pre-cut or missing source tissue needs artwork review.",
                              "Death/Gore uses a different camera and must be checked by verify_gore.py."], "profiles": reviews}
    suffix = "-" + args.profile if args.profile else ""
    output = folder / f"transition-review{suffix}.json"
    write(output, result)
    print(json.dumps({"run": args.run, "exactDiagnosticsPass": result["exactDiagnosticsPass"],
                      "setupFailures": len(setup_failures), "report": str(output),
                      "profiles": [{"profile": review["profile"], "frames": review["frames"],
                                    "exactFailures": len(review["exactFailures"]), "candidates": len(review["reviewCandidates"]),
                                    "missingExactMetadata": review["missingExactMetadata"]} for review in reviews]}, indent=2))
    if not result["exactDiagnosticsPass"]:
        raise SystemExit(1)


if __name__ == "__main__":
    main()
