"""Freeze and expose every actual dense-edge capture; no source drawing is changed."""
from pathlib import Path
from collections import defaultdict
import hashlib
import json
import numpy as np
from PIL import Image, ImageDraw

HERE = Path(__file__).resolve().parent
PROJECT = HERE.parents[4]
SOURCE = PROJECT / "Concepts/Used/Stalker/Postures05/Scenarios/stalker-dense-edges"
CROP = (168, 120, 504, 528)
CELL = (348, 442)
SIZE = (1392, 1326)
CONTEXT = (65, 66, 67, 68, 73, 74, 75, 86, 87, 88, 89,
           132, 133, 134, 135, 140, 141, 142, 153, 154, 155, 156, 157, 158, 163, 164, 165)


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def source_rgb(key, frame):
    with Image.open(SOURCE / key / f"frame-{frame:05d}.png") as im:
        return im.convert("RGB")


def make_pages(records, prefix):
    folder = HERE / prefix
    folder.mkdir(exist_ok=True)
    pages = []
    for start in range(0, len(records), 12):
        subset = records[start:start + 12]
        sheet = Image.new("RGB", SIZE, (20, 24, 28))
        draw = ImageDraw.Draw(sheet)
        for slot, record in enumerate(subset):
            facts = record["facts"]
            source = source_rgb(record["key"], record["frame"])
            native = source.crop(CROP)
            x, y = slot % 4 * CELL[0] + 6, slot // 4 * CELL[1] + 30
            sheet.paste(native, (x, y))
            draw.text((x, y - 29), f"P{record['profile']} F{record['frame']:03d} L{facts['local']:02d} {facts['state']}", fill="white")
            draw.text((x, y - 15), f"{facts['body']} fold{facts['fold']:.3f} eye{facts['closure']:.3f}", fill="white")
            assert sheet.crop((x, y, x + 336, y + 408)).tobytes() == native.tobytes()
        path = folder / f"page-{start // 12:03d}.png"
        sheet.save(path)
        pages.append({"type": prefix, "page": start // 12, "path": str(path.relative_to(PROJECT)), "sha256": sha(path), "frames": [{"profile": r["profile"], "key": r["key"], "frame": r["frame"], "decodedRgbSha256": r["decodedRgbSha256"]} for r in subset], "viewedOriginal": False, "nativeCaptureCropPayloadExact": True})
    return pages


def main():
    run_sha = sha(SOURCE / "run.json")
    run = json.loads((SOURCE / "run.json").read_text())
    assert run["completedAll"] and not run["captureErrors"]
    assert len(run["completed"]) == 9
    all_records, profiles, unique, groups = [], [], [], {}
    union = [640, 640, 0, 0]
    for profile_index, profile in enumerate(run["completed"]):
        key = profile["key"]
        manifest_path = SOURCE / key / "manifest.json"
        manifest_sha = sha(manifest_path)
        manifest = json.loads(manifest_path.read_text())
        assert len(manifest["frames"]) == profile["frames"] == 176
        records = []
        for facts in manifest["frames"]:
            frame = facts["frame"]
            path = SOURCE / key / f"frame-{frame:05d}.png"
            png_sha = sha(path)
            image = source_rgb(key, frame)
            raw = image.tobytes()
            digest = hashlib.sha256(raw).hexdigest()
            pixels = np.asarray(image)
            foreground = np.any(pixels != (0, 255, 0), axis=2)
            ys, xs = np.nonzero(foreground)
            bounds = [int(xs.min()), int(ys.min()), int(xs.max()) + 1, int(ys.max()) + 1]
            union = [min(union[0], bounds[0]), min(union[1], bounds[1]), max(union[2], bounds[2]), max(union[3], bounds[3])]
            assert bounds[0] >= CROP[0] and bounds[1] >= CROP[1] and bounds[2] <= CROP[2] and bounds[3] <= CROP[3], (key, frame, bounds)
            record = {"profile": profile_index, "key": key, "frame": frame, "pngSha256": png_sha, "decodedRgbSha256": digest, "foregroundBounds": bounds, "facts": facts}
            records.append(record)
            all_records.append(record)
            if digest not in groups:
                groups[digest] = {"representative": {"key": key, "frame": frame}, "aliases": []}
                unique.append(record)
            groups[digest]["aliases"].append({"key": key, "frame": frame})
        first_cuts = []
        for previous, current in ((66, 67), (133, 134)):
            before, after = records[previous], records[current]
            a = np.asarray(source_rgb(key, previous), dtype=np.int16)
            b = np.asarray(source_rgb(key, current), dtype=np.int16)
            difference = np.abs(a - b)
            assert before["facts"]["body"] == after["facts"]["body"] == "strike-10"
            assert before["facts"]["fold"] == after["facts"]["fold"] == 0
            first_cuts.append({"before": previous, "after": current, "sameSelectedSprite": True, "decodedRgbByteExact": bool(np.array_equal(a, b)), "changedPixels": int(np.any(difference, axis=2).sum()), "maximumChannelDifference": int(difference.max())})
        return_rows = []
        for span in manifest["spans"]:
            if span["scenario"].startswith(("27-", "28-")):
                selected = [r for r in records if r["facts"]["scenario"] == span["scenario"]]
                returning = [r for r in selected if r["facts"]["body"].startswith("return-")]
                samples = [int(r["facts"]["body"].split("-")[-1]) for r in returning]
                assert samples == list(range(14)), (key, span["scenario"], samples)
                assert all(r["facts"]["fold"] == 0 for r in returning)
                endpoint = records[returning[-1]["frame"] + 1]
                assert endpoint["facts"]["body"].startswith("body-0-") and endpoint["facts"]["fold"] == 0
                first_fold = next((r["frame"] for r in selected if r["facts"]["fold"] > 0), None)
                if span["scenario"].startswith("28-"):
                    assert first_fold > endpoint["frame"]
                return_rows.append({"scenario": span["scenario"], "bridgeFrames": [r["frame"] for r in returning], "bridgeSamples": samples, "currentPhaseColumns": [r["facts"]["body"] for r in returning], "walkEndpointFrame": endpoint["frame"], "walkEndpointSprite": endpoint["facts"]["body"], "firstPositiveFoldFrame": first_fold})
        assert sha(manifest_path) == manifest_sha
        profiles.append({"profile": profile_index, "key": key, "manifestSha256": manifest_sha, "configuration": manifest["sourceVariantConfiguration"], "spans": manifest["spans"], "sourceNote": manifest["note"], "firstCuts": first_cuts, "returns": return_rows, "records": records})
    assert len(all_records) == 1584 and len(unique) == 504
    context = [record for profile in profiles for record in profile["records"] if record["frame"] in CONTEXT]
    pages = make_pages(unique, "unique") + make_pages(context, "temporal")
    assert sha(SOURCE / "run.json") == run_sha
    report = {"status": "PREPARED; VISUAL REVIEW PENDING", "source": str(SOURCE.relative_to(PROJECT)), "runSha256": run_sha, "sourceFrameInstances": 1584, "profileCount": 9, "uniqueFullDecodedRgb": 504, "exactRgbDuplicateInstances": 1080, "allActualFramesMapped": True, "all24StrikeNotTheScope": "This is the actual diagnostic selection, including the selected strike samples, not a static library coverage claim.", "pageSizePixels": list(SIZE), "fixedCaptureCropPixels": list(CROP), "foregroundUnionPixels": union, "cropContainsEveryNonGreenCapturePixel": True, "resampling": False, "rotationNormalization": False, "profiles": profiles, "exactRgbGroups": groups, "pages": pages, "manualReviewBatches": [], "visualFindings": [], "scope": "Actual recorded presentation of nine authored configurations; forced snapshots and clone routes exclude AI transition/scene gameplay acceptance."}
    (HERE / "review-coverage.json").write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"profiles": 9, "instances": len(all_records), "uniqueRGB": len(unique), "uniquePages": 42, "temporalContextInstances": len(context), "temporalPages": len(pages) - 42, "foregroundUnion": union, "runSha256": run_sha}), flush=True)


if __name__ == "__main__":
    main()
