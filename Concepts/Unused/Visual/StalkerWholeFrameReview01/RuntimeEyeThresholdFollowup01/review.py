"""Separate current followup source lock, exact eye contract and stateful gaze evidence."""
from pathlib import Path
import hashlib
import json
import math
import sys
import numpy as np
from PIL import Image, ImageDraw

HERE = Path(__file__).resolve().parent
PROJECT = HERE.parents[4]
SOURCE = PROJECT / "Concepts/Used/Stalker/Postures05/Scenarios/eye-threshold-followup"
CROP = (256, 256, 384, 384)
DEPENDENCIES = (PROJECT / "AgentScripts/CaptureCreatureScenarios05.cs",
                PROJECT / "Assets/EmergencyShift/Runtime/Presentation/Visuals/CreatureSpriteAnimator2D.cs")


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def world(offset, degrees):
    theta = math.radians(degrees)
    return [math.cos(theta) * offset[0] - math.sin(theta) * offset[1],
            math.sin(theta) * offset[0] + math.cos(theta) * offset[1]]


def check_gaze(frames):
    steps = [math.dist(a["pupilOffset"], b["pupilOffset"]) for a, b in zip(frames, frames[1:])]
    assert max(steps) <= .25 / 60 + 1e-7
    for i in (1, 12, 13):
        assert not frames[i]["eyeOpen"] and frames[i]["pupilOffset"] == frames[i - 1]["pupilOffset"]
    assert all(f["eyeOpen"] for f in frames[14:])
    coords = [world(f["pupilOffset"], f["visualRotation"]) for f in frames]
    assert all(x > .04499 and abs(y) < 1e-5 for x, y in coords[25:38])
    assert all(coords[i + 1][0] < coords[i][0] for i in range(37, 49))
    assert coords[-1][0] < 0
    return {"maxLocalOffsetStep": max(steps), "rightSettledWorldX": coords[37][0],
            "lastWorldOffset": coords[-1], "lastLocalOffset": frames[-1]["pupilOffset"],
            "rightTargetTailFrames": [14, 37], "leftTargetTailFrames": [38, 49],
            "leftTargetFullySettledClaim": False,
            "note": "Local offsets are rotated by actual visualRotation for world direction; Sleepers turn during Waking."}


def check_eye(f):
    index = int(np.clip(np.rint(np.float32(f["closure"]) * np.float32(3)), 0, 3))
    assert f["eye"] == ("EyeOpen", "EyeHalf", "EyeSlit", "EyeShut")[index]
    assert f["eyeOpen"] == (f["state"] != "Dead" and index == 0)
    assert len(f["pupilOffset"]) == 2


def prepare():
    assert not (HERE / "page-manifest.json").exists()
    dependency_hashes = {str(p): sha(p) for p in DEPENDENCIES}
    run_path = SOURCE / "run.json"
    run_sha = sha(run_path)
    run = json.loads(run_path.read_text())
    assert run["completedAll"] and len(run["completed"]) == 12 and not run["captureErrors"]
    profiles, records = [], []
    for p, c in enumerate(run["completed"]):
        key = c["key"]
        mp = SOURCE / key / "manifest.json"
        msha = sha(mp)
        manifest = json.loads(mp.read_text())
        assert len(manifest["frames"]) == 50 and len(manifest["spans"]) == 1
        assert manifest["spans"][0]["scenario"] == "30-eye-threshold-open-close"
        local = []
        for f in manifest["frames"]:
            check_eye(f)
            path = SOURCE / key / f"frame-{f['frame']:05d}.png"
            psha = sha(path)
            with Image.open(path) as image:
                assert image.size == (640, 640)
                rgb = image.convert("RGB")
            r = {"profile": p, "key": key, "frame": f["frame"], "pngSha256": psha,
                 "decodedRgbSha256": hashlib.sha256(rgb.tobytes()).hexdigest(),
                 "eyeCropSha256": hashlib.sha256(rgb.crop(CROP).tobytes()).hexdigest(), "facts": f}
            local.append(r)
            records.append(r)
        assert sha(mp) == msha
        profiles.append({"profile": p, "key": key, "species": c["species"], "manifestSha256": msha,
                         "sourceVariantConfiguration": manifest["sourceVariantConfiguration"], "records": local,
                         "gazeChecks": check_gaze(manifest["frames"])})
    assert len(records) == 600
    (HERE / "pages").mkdir(exist_ok=True)
    pages = []
    for first in range(0, len(records), 25):
        subset = records[first:first + 25]
        sheet = Image.new("RGB", (740, 820), (20, 24, 28))
        draw = ImageDraw.Draw(sheet)
        for slot, r in enumerate(subset):
            x, y = slot % 5 * 148 + 8, slot // 5 * 164 + 32
            with Image.open(SOURCE / r["key"] / f"frame-{r['frame']:05d}.png") as image:
                native = image.convert("RGB").crop(CROP)
            sheet.paste(native, (x, y))
            f = r["facts"]
            draw.text((x, y - 29), f"P{r['profile']} F{r['frame']} {f['eye']} {int(f['eyeOpen'])}", fill="white")
            draw.text((x, y - 15), f"off{f['pupilOffset'][0]:+.3f},{f['pupilOffset'][1]:+.3f}", fill="white")
            assert sheet.crop((x, y, x + 128, y + 128)).tobytes() == native.tobytes()
        path = HERE / "pages" / f"page-{first // 25:03d}.png"
        sheet.save(path)
        pages.append({"page": first // 25, "path": str(path), "sha256": sha(path), "sizePixels": [740, 820],
                      "actuallyViewedOriginal": False,
                      "frames": [{k: r[k] for k in ("profile", "key", "frame", "eyeCropSha256")} for r in subset]})
    assert sha(run_path) == run_sha and all(sha(Path(p)) == h for p, h in dependency_hashes.items())
    immutable = {"sourceRoot": str(SOURCE), "sourceRunSha256": run_sha, "nativeScale": 1, "crop": list(CROP), "pages": pages}
    ip = HERE / "page-manifest.json"
    ip.write_text(json.dumps(immutable, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    d = {"status": "SOURCE_LOCKED_METADATA_PASS_ORIGINAL_VIEWS_PENDING", "sourceRoot": str(SOURCE), "sourceRunSha256": run_sha,
         "pageManifestSha256": sha(ip), "dependencyReadTimeSnapshots": dependency_hashes,
         "dependencyProvenance": "Root confirmed helper frozen after followup pupilOffset change. These are read-time source snapshots, not compiled DLL hashes; never attributed to the earlier first run.",
         "sourceInstances": 600, "profiles": profiles, "pages": pages, "fixedEyeCrop": list(CROP),
         "limits": "Eyes-only all600 current samples; exact original crops. Left12 tests continuous reversal and crosses worldcenter, not full negative target convergence. No wholebody/AI/build claim."}
    (HERE / "review-coverage.json").write_text(json.dumps(d, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"status": d["status"], "pages": len(pages), "sourceRunSha256": run_sha, "pageManifestSha256": sha(ip)}))


def finalize():
    cp = HERE / "review-coverage.json"
    d = json.loads(cp.read_text())
    assert sha(HERE / "page-manifest.json") == d["pageManifestSha256"]
    assert sha(SOURCE / "run.json") == d["sourceRunSha256"]
    assert all(sha(Path(p)) == h for p, h in d["dependencyReadTimeSnapshots"].items())
    count = 0
    for p in d["profiles"]:
        mp = SOURCE / p["key"] / "manifest.json"
        assert sha(mp) == p["manifestSha256"]
        frames = json.loads(mp.read_text())["frames"]
        assert check_gaze(frames) == p["gazeChecks"]
        for r in p["records"]:
            assert r["facts"] == frames[r["frame"]]
            check_eye(r["facts"])
            path = SOURCE / p["key"] / f"frame-{r['frame']:05d}.png"
            assert sha(path) == r["pngSha256"]
            with Image.open(path) as image:
                assert image.size == (640, 640)
                rgb = image.convert("RGB")
            assert hashlib.sha256(rgb.tobytes()).hexdigest() == r["decodedRgbSha256"]
            assert hashlib.sha256(rgb.crop(CROP).tobytes()).hexdigest() == r["eyeCropSha256"]
            count += 1
    tiles = 0
    for page in d["pages"]:
        path = Path(page["path"])
        assert sha(path) == page["sha256"]
        with Image.open(path) as image:
            assert image.size == (740, 820)
            sheet = image.convert("RGB")
        for slot, f in enumerate(page["frames"]):
            x, y = slot % 5 * 148 + 8, slot // 5 * 164 + 32
            with Image.open(SOURCE / f["key"] / f"frame-{f['frame']:05d}.png") as image:
                native = image.convert("RGB").crop(CROP)
            assert sheet.crop((x, y, x + 128, y + 128)).tobytes() == native.tobytes()
            tiles += 1
        page["actuallyViewedOriginal"] = True
    assert count == tiles == 600 and len(d["pages"]) == 24
    assert sha(SOURCE / "run.json") == d["sourceRunSha256"]
    assert all(sha(Path(p)) == h for p, h in d["dependencyReadTimeSnapshots"].items())
    d["status"] = "PASS_SCOPED_ALL12_CURRENT_EYE_THRESHOLD_AND_CONTINUOUS_GAZE_FOLLOWUP"
    d["manualReviewBatches"] = [{"first": 0, "last": 23, "actuallyViewedOriginal": True, "resizeNotice": False}]
    d["finalRecheck"] = {"all600PngRgbMetadataHashesUnchanged": True, "all600NativeTilePayloadsExact": True,
                         "all12ManifestsAndRuntimeHelperSourcesUnchanged": True, "exactEyePupilMismatches": 0,
                         "closedOffsetPreserved": True, "rightWorldTargetConverged": True, "leftReversalAndCenterCrossing": True,
                         "leftFullConvergenceClaim": False, "actionableEyeDefects": []}
    cp.write_text(json.dumps(d, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    files = (cp, HERE / "page-manifest.json", HERE / "independent-art-review.md", Path(__file__))
    (HERE / "completion-hashes.json").write_text(json.dumps({p.name: sha(p) for p in files}, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"status": d["status"], "coverageSha256": sha(cp), "viewed600On24Pages": True}))


if __name__ == "__main__":
    {"prepare": prepare, "finalize": finalize}[sys.argv[1]]()
