"""Prepare the bounded first gore handoff for all twelve current source variants."""
from pathlib import Path
import hashlib
import json
import numpy as np
from PIL import Image, ImageDraw

HERE = Path(__file__).resolve().parent
SOURCE = HERE.parents[4] / "Concepts/Used/Stalker/Postures05/Scenarios/gore-final"
CROP = (88, 104, 552, 544)
SIZE = (1428, 1422)


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    assert not (HERE / "page-manifest.json").exists(), "Immutable prepared evidence already exists"
    run_path = SOURCE / "run.json"
    run_sha = sha(run_path)
    run = json.loads(run_path.read_text())
    assert run["completedAll"] and len(run["completed"]) == 12 and not run["captureErrors"]
    profiles, all_records = [], []
    union = [640, 640, 0, 0]
    groups, representatives = {}, {}
    for p, source in enumerate(run["completed"]):
        key = source["key"]
        mp = SOURCE / key / "manifest.json"
        msha = sha(mp)
        manifest = json.loads(mp.read_text())
        death = next(s for s in manifest["spans"] if s["scenario"].startswith("20-"))
        first = death["first"]
        frames = manifest["frames"]
        records = []
        for i in [first - 1] + list(range(first, first + 12)):
            f = frames[i]
            path = SOURCE / key / f"frame-{i:05d}.png"
            psha = sha(path)
            with Image.open(path) as image:
                assert image.size == (640, 640)
                rgb = image.convert("RGB")
            raw = rgb.tobytes()
            digest = hashlib.sha256(raw).hexdigest()
            a = np.asarray(rgb)
            y, x = np.nonzero(np.any(a != (0, 255, 0), axis=2))
            bounds = [int(x.min()), int(y.min()), int(x.max()) + 1, int(y.max()) + 1]
            assert bounds[0] >= CROP[0] and bounds[1] >= CROP[1] and bounds[2] <= CROP[2] and bounds[3] <= CROP[3]
            union = [min(union[0], bounds[0]), min(union[1], bounds[1]), max(union[2], bounds[2]), max(union[3], bounds[3])]
            r = {"profile": p, "key": key, "frame": i, "relativeToKill": i - first, "pngSha256": psha,
                 "decodedRgbSha256": digest, "foregroundBounds": bounds, "facts": f}
            records.append(r)
            all_records.append(r)
            if digest in representatives:
                assert raw == representatives[digest]
            else:
                representatives[digest] = raw
            groups.setdefault(digest, []).append({"key": key, "frame": i})
        assert frames[first - 1]["state"] == ("Sleeping" if source["species"] == "sleeper" else "Waiting")
        assert frames[first - 1]["fold"] == frames[first]["fold"] == 1
        assert frames[first - 1]["body"] == frames[first]["body"] == "body-8-12"
        assert all(f["body"] == frames[first - 1]["body"] and f["bodyEnabled"] for f in frames[first:first + 8])
        assert all(not f["bodyEnabled"] for f in frames[first + 8:])
        assert all(f["deathEvents"] == 1 and f["remainsRoots"] == 1 for f in frames[first:])
        assert frames[first + 20]["killCalls"] == frames[-1]["killCalls"] == 2
        assert sha(mp) == msha
        profiles.append({"profile": p, "key": key, "species": source["species"], "manifestSha256": msha,
                         "sourceVariantConfiguration": manifest["sourceVariantConfiguration"], "deathSpan": death,
                         "sourceNote": manifest["note"], "records": records,
                         "metadataChecks": {"deathEvents": 1, "killCallsFinal": 2, "remainsRoots": 1,
                                            "bodyHeldDeadFrames": 8, "firstHiddenFrame": first + 8,
                                            "lastFrameFacts": frames[-1]}})
    assert len(all_records) == 156
    (HERE / "pages").mkdir(exist_ok=True)
    pages = []
    for first in range(0, len(all_records), 9):
        subset = all_records[first:first + 9]
        sheet = Image.new("RGB", SIZE, (20, 24, 28))
        draw = ImageDraw.Draw(sheet)
        for slot, r in enumerate(subset):
            x, y = slot % 3 * 476 + 6, slot // 3 * 474 + 28
            with Image.open(SOURCE / r["key"] / f"frame-{r['frame']:05d}.png") as image:
                native = image.convert("RGB").crop(CROP)
            sheet.paste(native, (x, y))
            f = r["facts"]
            draw.text((x, y - 27), f"P{r['profile']} F{r['frame']} cut{r['relativeToKill']:+d} {f['state']} visible{int(f['bodyEnabled'])}", fill="white")
            draw.text((x, y - 14), f"{f['body']} fold{f['fold']:.2f} eye{f['closure']:.2f} D{f['deathEvents']} K{f['killCalls']} R{f['remainsRoots']}", fill="white")
            assert sheet.crop((x, y, x + 464, y + 440)).tobytes() == native.tobytes()
        path = HERE / "pages" / f"page-{first // 9:03d}.png"
        sheet.save(path)
        pages.append({"page": first // 9, "path": str(path), "sha256": sha(path), "sizePixels": list(SIZE),
                      "nativePayloadExact": True, "actuallyViewedOriginal": False,
                      "frames": [{k: r[k] for k in ("profile", "key", "frame", "decodedRgbSha256")} for r in subset]})
    assert sha(run_path) == run_sha
    immutable = {"sourceRoot": str(SOURCE), "sourceRunSha256": run_sha, "crop": list(CROP), "nativeScale": 1,
                 "foregroundUnion": union, "pages": pages}
    ip = HERE / "page-manifest.json"
    ip.write_text(json.dumps(immutable, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    coverage = {"status": "PREPARED_NOT_VIEWED", "sourceRoot": str(SOURCE), "sourceRunSha256": run_sha,
                "sourceProfileCount": 12, "sourceInstanceCount": 156, "uniqueFullRgb": len(groups),
                "pageManifestSha256": sha(ip), "profiles": profiles, "pages": pages, "exactRgbGroups": groups,
                "fixedCrop": list(CROP), "foregroundUnion": union, "resampling": False,
                "limits": "All12 current variants, last live + first12 dead samples; no full late-particle visual coverage or AI claim."}
    (HERE / "review-coverage.json").write_text(json.dumps(coverage, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"pages": len(pages), "actualInstances": 156, "uniqueFullRgb": len(groups), "foregroundUnion": union,
                      "pageManifestSha256": sha(ip), "sourceRunSha256": run_sha}))


if __name__ == "__main__":
    main()
