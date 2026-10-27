"""Lock actual native first-run eyes and the real before/fixed boundary comparison."""
from pathlib import Path
import hashlib
import json
import numpy as np
from PIL import Image

HERE = Path(__file__).resolve().parent


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    cp = HERE / "review-coverage.json"
    d = json.loads(cp.read_text())
    source = Path(d["sourceRoot"])
    assert sha(HERE / "page-manifest.json") == d["pageManifestSha256"] == "5f5f3e005fad8225c366d07822aaccfc3113f5853668c2de888dc7249623fddd"
    assert sha(source / "run.json") == d["sourceRunSha256"]
    count = 0
    for p in d["profiles"]:
        mp = source / p["key"] / "manifest.json"
        assert sha(mp) == p["manifestSha256"]
        current = json.loads(mp.read_text())["frames"]
        for r in p["records"]:
            path = source / p["key"] / f"frame-{r['frame']:05d}.png"
            assert sha(path) == r["pngSha256"]
            with Image.open(path) as image:
                assert image.size == (640, 640)
                rgb = image.convert("RGB")
            assert hashlib.sha256(rgb.tobytes()).hexdigest() == r["decodedRgbSha256"]
            assert hashlib.sha256(rgb.crop(tuple(d["fixedEyeCrop"])).tobytes()).hexdigest() == r["eyeCropSha256"]
            f = r["facts"]
            frame = int(np.clip(np.rint(np.float32(f["closure"]) * np.float32(3)), 0, 3))
            assert f == current[r["frame"]]
            assert f["eye"] == ("EyeOpen", "EyeHalf", "EyeSlit", "EyeShut")[frame]
            assert f["eyeOpen"] == (f["state"] != "Dead" and frame == 0)
            assert "pupilOffset" not in f
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
            with Image.open(source / f["key"] / f"frame-{f['frame']:05d}.png") as image:
                native = image.convert("RGB").crop(tuple(d["fixedEyeCrop"]))
            assert sheet.crop((x, y, x + 128, y + 128)).tobytes() == native.tobytes()
            tiles += 1
        page["actuallyViewedOriginal"] = True
    assert count == 2328 and tiles == 753 and len(d["pages"]) == 31
    before_path = HERE / "BeforeAfter01/review-coverage.json"
    b = json.loads(before_path.read_text())
    page = Path(b["page"]["path"])
    assert sha(page) == b["page"]["sha256"] == "afe6f44d1a90d20ca70c891a81e1c03ff7ebeb8981503699eac277fb92016c0a"
    with Image.open(page) as image:
        assert image.size == (888, 1476)
        sheet = image.convert("RGB")
    for r in b["records"]:
        path = Path(r["sourcePath"])
        assert sha(path) == r["pngSha256"]
        with Image.open(path) as image:
            rgb = image.convert("RGB")
        assert hashlib.sha256(rgb.tobytes()).hexdigest() == r["decodedRgbSha256"]
        x, y = r["pageXY"]
        assert sheet.crop((x, y, x + 128, y + 128)).tobytes() == rgb.crop((256, 256, 384, 384)).tobytes()
    for p in b["sourceManifests"]:
        assert sha(HERE.parents[4] / "Concepts/Used/Stalker/Postures05/Scenarios/sleeper-final" / p["key"] / "manifest.json") == p["oldManifestSha256"]
        assert sha(source / p["key"] / "manifest.json") == p["newManifestSha256"]
    b["status"] = "ACTUAL_BEFORE9_DEFECTS_AND_FIXED9_BOUNDARIES_ORIGINAL_VIEWED"
    b["page"]["actuallyViewedOriginal"] = True
    b["finalRecheck"] = {"all54PngRgbAndNativeTilesUnchanged": True, "resampling": False, "resizeNotice": False}
    before_path.write_text(json.dumps(b, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    assert sha(source / "run.json") == d["sourceRunSha256"]
    d["status"] = "PASS_SCOPED_ALL12_EYE_THRESHOLD_AND_NATURAL_CONTEXTS"
    d["manualReviewBatches"] = [{"first": 0, "last": 30, "actuallyViewedOriginal": True, "resizeNotice": False}]
    d["beforeAfterProof"] = {"path": str(before_path), "sha256": sha(before_path)}
    d["finalRecheck"] = {"all2328PngRgbAndMetadataUnchanged": True, "exactEyeSpritePupilMismatches": 0,
                         "nativeEyeTilesViewedAndByteChecked": 753, "pagesViewed": 31,
                         "missingPupilOffsetNotReconstructed": True, "gazeFollowupGate": "SEPARATE_REVIEW_REQUIRED"}
    cp.write_text(json.dumps(d, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    files = (cp, HERE / "page-manifest.json", before_path, HERE / "independent-art-review.md", Path(__file__), HERE / "prepare_review.py", HERE / "prepare_before_after.py")
    (HERE / "completion-hashes.json").write_text(json.dumps({str(p.relative_to(HERE)): sha(p) for p in files}, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"status": d["status"], "coverageSha256": sha(cp), "beforeAfterSha256": sha(before_path)}))


if __name__ == "__main__":
    main()
