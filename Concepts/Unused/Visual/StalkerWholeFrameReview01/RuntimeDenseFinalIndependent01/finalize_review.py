"""Verify only this auditor's 67 actually viewed, unchanged native pages."""
from pathlib import Path
from datetime import datetime, timezone
import hashlib
import json

import numpy as np
from PIL import Image

HERE = Path(__file__).resolve().parent
SHARED = HERE.parent / "RuntimeDenseFinal01"


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    output = HERE / "review-coverage.json"
    data = json.loads(output.read_text(encoding="utf-8"))
    source = Path(data["sourceRoot"])
    provenance = json.loads((SHARED / "review-coverage.json").read_text(encoding="utf-8"))
    expected_frames = {p["key"]: {f["frame"]: f for f in p["frames"]} for p in provenance["profiles"]}
    manifests = {}
    for profile in data["sourceProfiles"]:
        path = source / profile["key"] / "manifest.json"
        assert sha(path) == profile["manifestSha256"], profile["key"]
        manifests[profile["key"]] = json.loads(path.read_text(encoding="utf-8"))
    names = [Path(p["path"]).name for p in data["pages"]]
    expected_names = [f"current-body-p01-{n:03d}.png" for n in range(49)]
    expected_names += [f"death-p{n:02d}-{p:03d}.png" for n in range(9) for p in range(2)]
    assert len(names) == 67 and set(names) == set(expected_names)
    assert sum(len(p["frames"]) for p in data["pages"]) == 363
    seen_frames = set()
    for page in data["pages"]:
        assert page["reviewed"] is True
        path = Path(page["path"])
        assert sha(path) == page["sha256"], str(path)
        with Image.open(path) as im:
            sheet = im.convert("RGB")
        full = page["fullCanvas"]
        assert sheet.size == ((1280, 1360) if full else (1536, 1104))
        width, columns, row_height = (640, 2, 680) if full else (512, 3, 552)
        facts_rows = []
        for slot, frame in enumerate(page["frames"]):
            key = page["profile"]
            frame_path = source / key / f"frame-{frame:05d}.png"
            expected = expected_frames[key][frame]
            assert sha(frame_path) == expected["sha256"], (key, frame)
            with Image.open(frame_path) as im:
                assert im.size == (640, 640)
                rgb = im.convert("RGB")
            assert hashlib.sha256(rgb.tobytes()).hexdigest() == expected["decodedRgbSha256"]
            native = rgb if full else rgb.crop((64, 64, 576, 576))
            x, y = slot % columns * width, slot // columns * row_height + 40
            assert sheet.crop((x, y, x + width, y + width)).tobytes() == native.tobytes(), (str(path), slot)
            pixels = np.asarray(rgb)
            foreground = np.any(pixels != (0, 255, 0), axis=2)
            ys, xs = np.nonzero(foreground)
            bounds = [int(xs.min()), int(ys.min()), int(xs.max()) + 1, int(ys.max()) + 1]
            if not full:
                assert min(bounds[:2]) >= 64 and max(bounds[2:]) <= 576, (key, frame, bounds)
            facts = manifests[key]["frames"][frame]
            facts_rows.append({"frame": frame, "pngSha256": expected["sha256"],
                               "decodedRgbSha256": expected["decodedRgbSha256"],
                               "foregroundBounds": bounds, "facts": facts})
            seen_frames.add((key, frame))
        page["sourceFramesVerified"] = facts_rows
        page["nativePayloadByteExact"] = True
        page["sha256UnchangedAfterView"] = True
    for profile in data["sourceProfiles"]:
        frames = manifests[profile["key"]]["frames"]
        before, first, held, handoff = [frames[i] for i in (1449, 1450, 1457, 1458)]
        assert before["orthographicSize"] == 1.65 and first["orthographicSize"] == 4.5
        assert before["body"] == first["body"] == held["body"] == handoff["body"]
        assert first["scale"] == [1.0, 1.0, 1.0]
        assert first["bodyEnabled"] and held["bodyEnabled"] and not handoff["bodyEnabled"]
        assert first["deathEvents"] == held["deathEvents"] == handoff["deathEvents"] == 1
        assert first["remainsRoots"] == held["remainsRoots"] == handoff["remainsRoots"] == 1
    data["status"] = "PASS — scoped actual native P01 body windows and all-nine death handoffs"
    data["completion"] = {"actuallyViewedPagesOriginal": 67, "frameInstances": 363,
                           "distinctSourceFramesShown": len(seen_frames),
                           "resampling": False, "allPageHashesUnchanged": True,
                           "allNativePayloadsDirectByteExact": True,
                           "allSelectedSourcePngHashesMatchFrozenProvenance": True,
                           "allNineManifestHashesUnchanged": True,
                           "sharedCoverageModified": False, "unityCalls": 0,
                           "productionEdits": 0, "ownerArtApprovalClaimed": False,
                           "finalAuthoring": "N/A — review artifacts only",
                           "verifiedAtUtc": datetime.now(timezone.utc).isoformat()}
    output.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    names = ("finalize_review.py", "independent-art-review.md", "review-coverage.json")
    hashes = {name: sha(HERE / name) for name in names}
    (HERE / "completion-hashes.json").write_text(json.dumps({"status": data["status"], "sha256": hashes}, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"status": data["status"], **data["completion"], "coverageSha256": hashes["review-coverage.json"]}, ensure_ascii=False), flush=True)


if __name__ == "__main__":
    main()
