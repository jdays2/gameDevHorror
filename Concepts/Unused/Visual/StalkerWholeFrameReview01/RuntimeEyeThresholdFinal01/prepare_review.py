"""Freeze the completed first eye run and build eyes-only native chronological crops."""
from pathlib import Path
import hashlib
import json
import numpy as np
from PIL import Image, ImageDraw

HERE = Path(__file__).resolve().parent
PROJECT = HERE.parents[4]
SOURCE = PROJECT / "Concepts/Used/Stalker/Postures05/Scenarios/eye-threshold-final"
CROP = (256, 256, 384, 384)
EYES = ("EyeOpen", "EyeHalf", "EyeSlit", "EyeShut")
RUNTIME = PROJECT / "Assets/EmergencyShift/Runtime/Presentation/Visuals/CreatureSpriteAnimator2D.cs"


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def selected(manifest, sleeper):
    frames = manifest["frames"]
    result = set()
    for span in manifest["spans"]:
        a, b = span["first"], span["last"]
        sequence = frames[a:b + 1]
        result.update((a, b))
        for n, f in enumerate(sequence):
            if n and (f["eye"], f["eyeOpen"]) != (sequence[n - 1]["eye"], sequence[n - 1]["eyeOpen"]):
                result.update(range(max(a, f["frame"] - 2), min(b + 1, f["frame"] + 3)))
        if span["scenario"].startswith("30-"):
            result.update(range(a, b + 1))
        if sleeper and span["scenario"].startswith(("02-", "17-", "22-")):
            first_open = next((f["frame"] for f in sequence if f["eyeOpen"]), None)
            if first_open is not None:
                result.update(range(first_open, b + 1))
    return result


def main():
    assert not (HERE / "page-manifest.json").exists(), "Already frozen"
    run_path = SOURCE / "run.json"
    run_sha = sha(run_path)
    runtime_sha = sha(RUNTIME)
    run = json.loads(run_path.read_text())
    assert run["completedAll"] and len(run["completed"]) == 12 and not run["captureErrors"]
    profiles, views, full_groups = [], [], {}
    for p, c in enumerate(run["completed"]):
        key = c["key"]
        mp = SOURCE / key / "manifest.json"
        msha = sha(mp)
        m = json.loads(mp.read_text())
        records = []
        indices = selected(m, c["species"] == "sleeper")
        for f in m["frames"]:
            path = SOURCE / key / f"frame-{f['frame']:05d}.png"
            psha = sha(path)
            with Image.open(path) as image:
                assert image.size == (640, 640)
                rgb = image.convert("RGB")
            raw = rgb.tobytes()
            digest = hashlib.sha256(raw).hexdigest()
            crop = rgb.crop(CROP).tobytes()
            eye_frame = int(np.clip(np.rint(np.float32(f["closure"]) * np.float32(3)), 0, 3))
            expected_open = f["state"] != "Dead" and eye_frame == 0
            assert f["eye"] == EYES[eye_frame] and f["eyeOpen"] == expected_open, (key, f["frame"], f)
            assert "pupilOffset" not in f, "First run has no offset diagnostics; do not fabricate them"
            r = {"profile": p, "key": key, "frame": f["frame"], "pngSha256": psha, "decodedRgbSha256": digest,
                 "eyeCropSha256": hashlib.sha256(crop).hexdigest(), "expectedEyeFrameFloat32TiesToEven": eye_frame,
                 "actuallySelectedForNativeEyeReview": f["frame"] in indices, "facts": f}
            records.append(r)
            if f["frame"] in indices:
                views.append(r)
            full_groups.setdefault(digest, []).append({"key": key, "frame": f["frame"]})
        assert sha(mp) == msha
        profiles.append({"profile": p, "key": key, "species": c["species"], "manifestSha256": msha,
                         "sourceVariantConfiguration": m["sourceVariantConfiguration"], "spans": m["spans"], "records": records})
    assert sum(len(p["records"]) for p in profiles) == 2328 and len(views) == 753
    pages = []
    (HERE / "pages").mkdir(exist_ok=True)
    for first in range(0, len(views), 25):
        subset = views[first:first + 25]
        sheet = Image.new("RGB", (740, 820), (20, 24, 28))
        draw = ImageDraw.Draw(sheet)
        for slot, r in enumerate(subset):
            x, y = slot % 5 * 148 + 8, slot // 5 * 164 + 32
            with Image.open(SOURCE / r["key"] / f"frame-{r['frame']:05d}.png") as image:
                native = image.convert("RGB").crop(CROP)
            sheet.paste(native, (x, y))
            f = r["facts"]
            draw.text((x, y - 29), f"P{r['profile']} F{r['frame']} c{f['closure']:.7f}", fill="white")
            draw.text((x, y - 15), f"{f['eye']} pupil{int(f['eyeOpen'])}", fill="white")
            assert sheet.crop((x, y, x + 128, y + 128)).tobytes() == native.tobytes()
        path = HERE / "pages" / f"page-{first // 25:03d}.png"
        sheet.save(path)
        pages.append({"page": first // 25, "path": str(path), "sha256": sha(path), "sizePixels": [740, 820],
                      "actuallyViewedOriginal": False, "frames": [{k: r[k] for k in ("profile", "key", "frame", "eyeCropSha256")} for r in subset]})
    assert sha(run_path) == run_sha
    immutable = {"sourceRoot": str(SOURCE), "sourceRunSha256": run_sha, "nativeScale": 1, "crop": list(CROP), "pages": pages}
    ip = HERE / "page-manifest.json"
    ip.write_text(json.dumps(immutable, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    evidence = {"status": "FROZEN_METADATA_PASS_NATIVE_VIEWS_PENDING", "sourceRoot": str(SOURCE),
                "sourceRunSha256": run_sha, "pageManifestSha256": sha(ip), "profiles": profiles, "pages": pages,
                "metadataFramesChecked": 2328, "nativeEyeCropInstancesPlanned": 753, "fixedEyeCrop": list(CROP),
                "runtimeReadTimeSnapshot": {"path": str(RUNTIME), "sha256": runtime_sha,
                                            "claim": "Read-time fixed source evidence only; not a compiled-capture hash."},
                "captureHarnessSha256": None, "captureHarnessProvenanceNote": "Root changed helper for subsequent followup; its new SHA must not be attributed to this run.",
                "pupilOffsetDiagnosticsPresent": False, "exactFullRgbGroups": full_groups,
                "limits": "Eyes-only. Metadata checked entire2328; original native crop review restricted to753 selected threshold/context instances. No missing pupilOffset reconstructed; gaze-followup acceptance separate."}
    (HERE / "review-coverage.json").write_text(json.dumps(evidence, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"framesFrozen": 2328, "nativeEyeCropViews": len(views), "pages": len(pages), "sourceRunSha256": run_sha,
                      "pageManifestSha256": sha(ip), "runtimeReadTimeSha256": runtime_sha}))


if __name__ == "__main__":
    main()
