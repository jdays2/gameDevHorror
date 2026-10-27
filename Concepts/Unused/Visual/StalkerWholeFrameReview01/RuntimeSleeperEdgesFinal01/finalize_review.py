"""Lock the completed original-size visual review against current raw capture bytes."""
from pathlib import Path
import hashlib
import json
from PIL import Image

HERE = Path(__file__).resolve().parent
SHARED = HERE.parent / "RuntimeSleeperFullFinal01/review-coverage.json"
SHARED_SHA = "c4d60d59f8fdb0f9363d9f1bfdb77acaba020ccd8ef98eab91741aaa59c5272c"
IMMUTABLE_SHA = "2394004820b9f4ce57dd989b97219b96b1cbebd9ee721402dfe6ad07d16901c8"


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    coverage_path = HERE / "review-coverage.json"
    d = json.loads(coverage_path.read_text())
    ip = HERE / "page-manifest.json"
    assert sha(ip) == IMMUTABLE_SHA == d["pageManifestSha256"]
    assert sha(SHARED) == SHARED_SHA
    shared = json.loads(SHARED.read_text())
    shared_profiles = {p["key"]: p for p in shared["profiles"]}
    source = Path(d["sourceRoot"])
    assert sha(source / "run.json") == d["sourceRunSha256"]
    run = json.loads((source / "run.json").read_text())
    assert run["completedAll"] and len(run["completed"]) == 3 and not run["captureErrors"]
    raw_representatives = {}
    facts_by_key = {}
    observed = []
    instances = 0
    for p in d["profiles"]:
        key = p["key"]
        assert sha(source / key / "manifest.json") == p["manifestSha256"] == shared_profiles[key]["manifestSha256"]
        frozen = {f["frame"]: f for f in shared_profiles[key]["frames"]}
        facts = {r["frame"]: r["facts"] for r in p["records"]}
        facts_by_key[key] = facts
        for r in p["records"]:
            path = source / key / f"frame-{r['frame']:05d}.png"
            assert sha(path) == r["pngSha256"] == frozen[r["frame"]]["sha256"]
            with Image.open(path) as image:
                assert image.size == (640, 640)
                rgb = image.convert("RGB").tobytes()
            digest = hashlib.sha256(rgb).hexdigest()
            assert digest == r["decodedRgbSha256"] == frozen[r["frame"]]["decodedRgbSha256"]
            assert frozen[r["frame"]]["owner"] == "runtime_review"
            if digest in raw_representatives:
                assert rgb == raw_representatives[digest]
            else:
                raw_representatives[digest] = rgb
            f = r["facts"]
            if f["state"] != "Dead":
                assert f["bodyLocalPosition"] == [0, 0, 0]
                assert f["bodyLocalScale"] == [1, 1, 1]
                assert f["bodyLocalRotation"] == 0
            assert f["scale"] == [1, 1, 1]
            instances += 1
        assert facts[1154]["state"] == "Attacking" and facts[1155]["state"] == "Recovering"
        assert facts[1154]["body"] == facts[1155]["body"] == "strike-11"
        for field in ("bodyAsset", "spriteRect", "spritePivotPixels", "frameIndex", "fold"):
            assert facts[1154][field] == facts[1155][field]
        assert facts[1161]["body"] == "strike-23" and facts[1176]["body"] == "body-0-00"
        assert all(facts[i]["fold"] == 0 for i in range(1154, 1200))
        assert facts[1229]["state"] == "Waking" and 0 < facts[1230]["fold"] < facts[1229]["fold"] < 1
        assert facts[1230]["state"] == "TargetDead" and facts[1230]["closure"] == 0
        assert all(facts[i + 1]["fold"] <= facts[i]["fold"] for i in range(1229, 1236))
        assert facts[1236]["fold"] == 0
        assert facts[1274]["body"] == "body-0-00" and facts[1274]["state"] == "TargetDead"
        assert all(facts[i]["body"] == facts[1274]["body"] and facts[i]["bodyEnabled"] for i in range(1275, 1283))
        assert all(not facts[i]["bodyEnabled"] for i in range(1283, 1365))
        assert all(facts[i]["deathEvents"] == 1 and facts[i]["remainsRoots"] == 1 for i in range(1275, 1365))
        assert facts[1275]["killCalls"] == 1 and facts[1295]["killCalls"] == facts[1364]["killCalls"] == 2
        observed.append({"key": key, "attackDuration": p["attackDuration"], "strikeAt": p["strikeAt"],
                         "interruptionSequence": [{"frame": i, "body": facts[i]["body"]} for i in range(1154, 1177)],
                         "partialWakeCut": [{"frame": i, "body": facts[i]["body"], "fold": facts[i]["fold"], "closure": facts[i]["closure"]} for i in range(1229, 1237)],
                         "deathHoldFrames": list(range(1275, 1283)), "firstHiddenFrame": 1283,
                         "deathCameraOrthoBeforeAfter": [facts[1274]["orthographicSize"], facts[1275]["orthographicSize"]],
                         "deathEvents": 1, "killCallsFinal": 2, "remainsRoots": 1})
    assert instances == 753 and len(raw_representatives) == 140
    assert len(d["pages"]) == 27
    tile_count = 0
    for page in d["pages"]:
        path = Path(page["path"])
        assert sha(path) == page["sha256"]
        with Image.open(path) as image:
            assert image.size == (1428, 1422)
            sheet = image.convert("RGB")
        for slot, frame in enumerate(page["frames"]):
            x, y = slot % 3 * 476 + 6, slot // 3 * 474 + 28
            with Image.open(source / frame["key"] / f"frame-{frame['frame']:05d}.png") as image:
                native = image.convert("RGB").crop(tuple(d["fixedCrop"]))
            assert sheet.crop((x, y, x + 464, y + 440)).tobytes() == native.tobytes()
            tile_count += 1
        page["actuallyViewedOriginal"] = True
    assert tile_count == 236
    assert sha(SHARED) == SHARED_SHA and sha(ip) == IMMUTABLE_SHA
    assert sha(source / "run.json") == d["sourceRunSha256"]
    for p in d["profiles"]:
        assert sha(source / p["key"] / "manifest.json") == p["manifestSha256"]
    d["status"] = "PASS_SCOPED_CURRENT_FULL_NATIVE_VISUAL_REVIEW"
    d["manualReviewBatches"] = [
        {"type": "unique", "first": 0, "last": 15, "pages": 16, "actuallyViewedOriginal": True, "resizeNotice": False},
        {"type": "temporal", "first": 0, "last": 10, "pages": 11, "actuallyViewedOriginal": True, "resizeNotice": False}]
    d["finalRecheck"] = {"all753PngAndRgbMatchFrozenProvenance": True, "sharedCoverageSha256": SHARED_SHA,
                         "exactDecodedByteAliases": 613, "nativeTilePayloadsChecked": 236,
                         "allPageHashesUnchanged": True, "allSourceManifestHashesUnchanged": True,
                         "observedSequences": observed, "actionableArtDefects": []}
    coverage_path.write_text(json.dumps(d, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    files = (coverage_path, ip, HERE / "independent-art-review.md", Path(__file__), HERE / "prepare_review.py")
    (HERE / "completion-hashes.json").write_text(json.dumps({p.name: sha(p) for p in files}, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"status": d["status"], "sourceInstances": instances, "uniqueFullRgb": len(raw_representatives),
                      "viewedPages": len(d["pages"]), "nativeTilesChecked": tile_count, "coverageSha256": sha(coverage_path)}))


if __name__ == "__main__":
    main()
