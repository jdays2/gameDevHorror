"""Freeze the independently viewed body matrix and its exact numeric follow-ups.

Read-only for all production/candidate inputs. Writes only this audit folder.
This verifies provenance; the art findings are explicitly human/model inspection.
"""
from pathlib import Path
from collections import Counter
import hashlib
import json
from PIL import Image

OUT = Path(__file__).resolve().parent
PROJECT = OUT.parents[3]
FAMILY = PROJECT / "Concepts/Used/Stalker/Postures05"
QA = FAMILY / "CandidateAudit/FinalBody432QA01"


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    coverage_path = OUT / "FullBodyFinal04-review-coverage.json"
    coverage = json.loads(coverage_path.read_text())
    assert len(coverage["frames"]) == 432
    assert len(coverage["pages"]) == 216
    assert all(page["reviewed"] for page in coverage["pages"])
    for frame in coverage["frames"]:
        path = Path(coverage["rawDirectory"]) / frame["name"]
        assert digest(path) == frame["sha256"], frame["name"]
    for page in coverage["pages"]:
        assert digest(Path(page["path"])) == page["sha256"], page["path"]
    assert digest(FAMILY / "body-composition.json") == coverage["bodyCompositionSha256"]
    assert digest(FAMILY / "poses.json") == coverage["bodyPosesSha256"]

    qa = json.loads((QA / "summary.json").read_text())
    contacts = qa["pairCandidates"]
    assert all(contact["pair"] == [1, 3] for contact in contacts)
    largest = max(contacts, key=lambda contact: contact["pixels"])
    hidden = []
    for name in ("body-3-28", "body-4-29"):
        raw = Image.open(FAMILY / "Raw" / (name + ".png")).convert("RGBA")
        mask_name = "leg-" + name[5:] + "-3.png"
        mask_image = Image.open(FAMILY / "Masks" / mask_name)
        mask = mask_image.getchannel("A") if "A" in mask_image.getbands() else mask_image.convert("L")
        hidden.append({"name": name, "pixel": [323, 296],
                       "independentlyReadWholeRgba": list(raw.getpixel((323, 296))),
                       "independentlyReadLimbMaskAlpha": mask.getpixel((323, 296))})
        assert hidden[-1]["independentlyReadWholeRgba"][3] == 253
        assert hidden[-1]["independentlyReadLimbMaskAlpha"] == 3

    packed = json.loads((FAMILY / "body-packed-roundtrip.json").read_text())
    report = {
        "scope": "Independent full body static artwork review; 432 poses on all three backgrounds. No motion/runtime approval.",
        "artVerdict": "PASS: no remaining visible body skin/attachment blocker in reviewed pixels",
        "reviewedFrames": 432,
        "reviewedBackgroundInstances": 1296,
        "reviewedPages": 216,
        "backgroundPageCounts": dict(Counter(page["background"] for page in coverage["pages"])),
        "allRaw432HashesUnchangedAtCompletion": True,
        "allReviewPage216HashesUnchangedAtCompletion": True,
        "bodyCompositionSha256": coverage["bodyCompositionSha256"],
        "bodyPosesSha256": coverage["bodyPosesSha256"],
        "bodyComposition": coverage["bodyComposition"],
        "nativePresentation": {"ppu": 224, "canvas": [576, 576], "sharedCrop": coverage["sharedCropPixels"],
                               "rescale": False, "autoFit": False, "spatialFilter": False,
                               "compositing": "linear light from original straight RGBA"},
        "numericFollowups": {
            "producer": "Sibling visual_audit; independently classified from current raw frames and exact context image",
            "summarySha256": digest(QA / "summary.json"),
            "contextImageSha256": digest(QA / "body-contact-and-hole-contexts-4x.png"),
            "holeTraceSha256": digest(QA / "body-hole-trace.json"),
            "contactFrames": len(contacts), "contactPixelsAcrossFrames": sum(item["pixels"] for item in contacts),
            "contactPairs": [[1, 3]], "largestContact": largest,
            "contactClassification": "Accepted proximal RF/RM Y overlap at body edge. Both shafts readable, no distal crossing, cut tissue, root needle or double collar. Numeric contact is retained; no claim of zero skin overlap or measured 3D separation.",
            "hiddenLimbMaskCavities": hidden,
            "cavityClassification": "No visible aperture on green/dark/light: source ownership boundary lies beneath continuous torso. No fill or torso-mask expansion justified."
        },
        "packingEvidence": {
            "producer": "Root verify_packed_frames.py; report inspected, packing not independently reimplemented here",
            "reportSha256": digest(FAMILY / "body-packed-roundtrip.json"),
            "frames": packed["frames"],
            "allAtlasRoundTripsVisibleByteExact": packed["allAtlasRoundTripsVisibleByteExact"],
            "alphaPixelsChanged": packed["alphaPixelsChanged"],
            "packingAddedCoverage": packed["packingAddedCoverage"]
        },
        "separateGates": {
            "offlineCoordinates": "PASS under Final04-independent-body-attack-coordinate-gate.json, virtual authored chains only",
            "upperVolume": "Baseline credible. Middle upper segments are thin, consistent with open source material; native Recoil03 is a different drawing/pose. No mandatory volume edit justified by this full matrix.",
            "skinArt": "PASS body432 static native pixels",
            "motionAndTempo": "Pending native playback; still review does not approve cadence, smoothness or temporal silhouette",
            "attackFullArt": "Sibling ownership, outside this body verdict",
            "sleeperPaletteAndSleep": "Outside this body matrix review",
            "physical3DSupport": "Not established by PNG or authored endpoint Z"
        }
    }
    (OUT / "FullBodyFinal04-independent-completion.json").write_text(json.dumps(report, indent=2))
    coverage["status"] = "COMPLETE: all432 body frames reviewed on green, dark and light; visible static body art PASS"
    coverage["completionReport"] = str(OUT / "FullBodyFinal04-independent-completion.json")
    coverage_path.write_text(json.dumps(coverage, indent=2))
    print(json.dumps({"bodyArt": report["artVerdict"], "frames": 432, "backgroundInstances": 1296,
                      "pages": 216, "rawHashesRechecked": True, "contactFrames": len(contacts),
                      "contactPixels": sum(item["pixels"] for item in contacts), "hiddenCavities": hidden}, indent=2))


if __name__ == "__main__":
    main()
