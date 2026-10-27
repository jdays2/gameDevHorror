"""Retain actual old/fixed natural eye boundaries without claiming pose/RGB identity."""
from pathlib import Path
import hashlib
import json
from PIL import Image, ImageDraw

HERE = Path(__file__).resolve().parent
PROJECT = HERE.parents[4]
OLD = PROJECT / "Concepts/Used/Stalker/Postures05/Scenarios/sleeper-final"
NEW = PROJECT / "Concepts/Used/Stalker/Postures05/Scenarios/eye-threshold-final"
FROZEN = HERE.parent / "RuntimeSleeperFullFinal01/review-coverage.json"
EXPECTED = "c4d60d59f8fdb0f9363d9f1bfdb77acaba020ccd8ef98eab91741aaa59c5272c"


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    out = HERE / "BeforeAfter01"
    out.mkdir(exist_ok=True)
    assert not (out / "review-coverage.json").exists()
    assert sha(FROZEN) == EXPECTED
    full = json.loads(FROZEN.read_text())
    frozen = {p["key"]: {f["frame"]: f for f in p["frames"]} for p in full["profiles"]}
    current = json.loads((HERE / "review-coverage.json").read_text())
    sheet = Image.new("RGB", (888, 1476), (20, 24, 28))
    draw = ImageDraw.Draw(sheet)
    records, manifests = [], []
    row = 0
    for p in current["profiles"]:
        if p["species"] != "sleeper":
            continue
        key = p["key"]
        old_mp, new_mp = OLD / key / "manifest.json", NEW / key / "manifest.json"
        old_hash, new_hash = sha(old_mp), sha(new_mp)
        old_m, new_m = json.loads(old_mp.read_text()), json.loads(new_mp.read_text())
        manifests.append({"key": key, "oldManifestSha256": old_hash, "newManifestSha256": new_hash})
        for old_center, new_center in ((90, 90), (783, 141), (957, 225)):
            for col, (label, source, center, manifest) in enumerate((("OLD", OLD, old_center, old_m), ("FIX", NEW, new_center, new_m))):
                for relative in (-1, 0, 1):
                    i = center + relative
                    path = source / key / f"frame-{i:05d}.png"
                    png_sha = sha(path)
                    with Image.open(path) as image:
                        rgb = image.convert("RGB")
                    rgb_sha = hashlib.sha256(rgb.tobytes()).hexdigest()
                    if label == "OLD":
                        assert png_sha == frozen[key][i]["sha256"] and rgb_sha == frozen[key][i]["decodedRgbSha256"]
                    f = manifest["frames"][i]
                    if relative == 0:
                        assert f["eye"] == "EyeOpen" and f["eyeOpen"] == (label == "FIX")
                    x, y = (col * 3 + relative + 1) * 148 + 8, row * 164 + 32
                    crop = rgb.crop((256, 256, 384, 384))
                    sheet.paste(crop, (x, y))
                    draw.text((x, y - 29), f"{label} P{p['profile']} F{i}", fill="white")
                    draw.text((x, y - 15), f"{f['eye']} pupil{int(f['eyeOpen'])}", fill="white")
                    records.append({"kind": label, "key": key, "frame": i, "sourcePath": str(path), "pngSha256": png_sha,
                                    "decodedRgbSha256": rgb_sha, "eyeCropSha256": hashlib.sha256(crop.tobytes()).hexdigest(),
                                    "pageXY": [x, y], "facts": f})
            row += 1
        assert sha(old_mp) == old_hash and sha(new_mp) == new_hash
    assert len(records) == 54 and row == 9 and sha(FROZEN) == EXPECTED
    page = out / "natural-boundary-old-fixed.png"
    sheet.save(page)
    proof = {"status": "PREPARED_NOT_VIEWED", "frozenBeforeFullProvenanceSha256": EXPECTED, "sourceManifests": manifests,
             "page": {"path": str(page), "sha256": sha(page), "sizePixels": [888, 1476], "actuallyViewedOriginal": False},
             "records": records, "limits": "Actual old27/fixed27 native eye crops; logical same scenario boundaries, not asserted identical body phase/camera-registered RGB. No reconstructed old pixels."}
    (out / "review-coverage.json").write_text(json.dumps(proof, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"actualNativeCrops": 54, "pageSha256": sha(page)}))


if __name__ == "__main__":
    main()
