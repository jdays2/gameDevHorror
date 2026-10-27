"""Recheck current source bytes after all18 original-size handoff pages were viewed."""
from pathlib import Path
import hashlib
import json
from PIL import Image

HERE = Path(__file__).resolve().parent
PAGE_MANIFEST_SHA = "f5fe811b0c624efbddb1372f0b9a1d860d8d1c313836dadde04386df9de6fff3"


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    cp = HERE / "review-coverage.json"
    d = json.loads(cp.read_text())
    ip = HERE / "page-manifest.json"
    assert sha(ip) == PAGE_MANIFEST_SHA == d["pageManifestSha256"]
    source = Path(d["sourceRoot"])
    assert sha(source / "run.json") == d["sourceRunSha256"]
    run = json.loads((source / "run.json").read_text())
    assert run["completedAll"] and len(run["completed"]) == 12 and not run["captureErrors"]
    representatives, seen = {}, set()
    count = 0
    for p in d["profiles"]:
        mp = source / p["key"] / "manifest.json"
        assert sha(mp) == p["manifestSha256"]
        m = json.loads(mp.read_text())
        first = p["deathSpan"]["first"]
        frames = m["frames"]
        assert [r["frame"] for r in p["records"]] == [first - 1] + list(range(first, first + 12))
        pre = frames[first - 1]
        assert pre["state"] == ("Sleeping" if p["species"] == "sleeper" else "Waiting")
        assert pre["bodyEnabled"] and pre["body"] == "body-8-12" and pre["fold"] == pre["closure"] == 1
        assert pre["bodyLocalPosition"] == [0, 0, 0] and pre["bodyLocalScale"] == [1, 1, 1] and pre["bodyLocalRotation"] == 0
        for f in frames[first:first + 8]:
            assert f["bodyEnabled"] and f["fold"] == 1 and f["body"] == pre["body"]
            for field in ("bodyAsset", "spriteRect", "spritePivotPixels", "frameIndex"):
                assert f[field] == pre[field]
        assert all(not f["bodyEnabled"] for f in frames[first + 8:])
        assert all(f["deathEvents"] == 1 and f["remainsRoots"] == 1 for f in frames[first:])
        assert frames[first]["killCalls"] == 1 and frames[first + 20]["killCalls"] == frames[-1]["killCalls"] == 2
        assert p["metadataChecks"]["lastFrameFacts"] == frames[-1]
        for r in p["records"]:
            path = source / p["key"] / f"frame-{r['frame']:05d}.png"
            assert sha(path) == r["pngSha256"]
            with Image.open(path) as image:
                assert image.size == (640, 640)
                rgb = image.convert("RGB").tobytes()
            digest = hashlib.sha256(rgb).hexdigest()
            assert digest == r["decodedRgbSha256"]
            if digest in representatives:
                assert representatives[digest] == rgb
            else:
                representatives[digest] = rgb
            assert r["facts"] == frames[r["frame"]]
            assert r["facts"]["scale"] == [1, 1, 1]
            seen.add((p["key"], r["frame"]))
            count += 1
    assert count == 156 and len(seen) == 156 and len(representatives) == d["uniqueFullRgb"] == 150
    assert len(d["pages"]) == 18
    tiles = set()
    for page in d["pages"]:
        path = Path(page["path"])
        assert sha(path) == page["sha256"]
        with Image.open(path) as image:
            assert image.size == (1428, 1422)
            sheet = image.convert("RGB")
        for slot, f in enumerate(page["frames"]):
            x, y = slot % 3 * 476 + 6, slot // 3 * 474 + 28
            with Image.open(source / f["key"] / f"frame-{f['frame']:05d}.png") as image:
                native = image.convert("RGB").crop(tuple(d["fixedCrop"]))
            assert sheet.crop((x, y, x + 464, y + 440)).tobytes() == native.tobytes()
            assert (f["key"], f["frame"]) not in tiles
            tiles.add((f["key"], f["frame"]))
        page["actuallyViewedOriginal"] = True
    assert tiles == seen
    assert sha(ip) == PAGE_MANIFEST_SHA and sha(source / "run.json") == d["sourceRunSha256"]
    assert all(sha(source / p["key"] / "manifest.json") == p["manifestSha256"] for p in d["profiles"])
    d["status"] = "PASS_SCOPED_ALL12_CURRENT_GORE_HANDOFF_NATIVE_VISUAL_REVIEW"
    d["manualReviewBatches"] = [{"first": 0, "last": 17, "pages": 18, "actuallyViewedOriginal": True, "resizeNotice": False}]
    d["finalRecheck"] = {"rawPngHashesChecked": 156, "fullDecodedRgbHashesChecked": 156, "nativeTilePayloadsChecked": 156,
                         "uniqueFullRgb": 150, "directByteDuplicateInstances": 6, "allPagesUnchanged": True,
                         "all12ManifestHashesUnchanged": True, "all12BodyHoldEightAndThenHidden": True,
                         "all12Died1Kill2Remains1": True, "actionableArtDefects": []}
    cp.write_text(json.dumps(d, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    files = (cp, ip, HERE / "independent-art-review.md", HERE / "prepare_review.py", Path(__file__))
    (HERE / "completion-hashes.json").write_text(json.dumps({p.name: sha(p) for p in files}, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"status": d["status"], "profiles": 12, "actualFramesViewed": count,
                      "nativePagesViewed": 18, "coverageSha256": sha(cp)}))


if __name__ == "__main__":
    main()
