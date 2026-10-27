"""Close the manual native-size review with immutable source and exact alias proof."""
from pathlib import Path
from collections import defaultdict
from datetime import datetime, timezone
import hashlib
import json

from PIL import Image

HERE = Path(__file__).resolve().parent
PROJECT = HERE.parents[4]


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    coverage_path = HERE / "review-coverage.json"
    data = json.loads(coverage_path.read_text(encoding="utf-8"))
    source = PROJECT / data["source"]
    assert sha(source / "run.json") == data["runSha256"]
    records = {}
    sequence_groups = defaultdict(list)
    for profile in data["profiles"]:
        assert sha(source / profile["key"] / "manifest.json") == profile["manifestSha256"]
        assert len(profile["records"]) == 176
        body_transforms = set()
        for record in profile["records"]:
            identity = (record["key"], record["frame"])
            assert identity not in records
            records[identity] = record
            facts = record["facts"]
            body_transforms.add((tuple(facts["bodyLocalPosition"]),
                                 tuple(facts["bodyLocalScale"]), facts["bodyLocalRotation"]))
            assert facts["death"] == 0 and facts["bodyEnabled"]
        assert body_transforms == {((0.0, 0.0, 0.0), (1.0, 1.0, 1.0), 0.0)}
        profile["bodyLocalTransformStable"] = True
        sequence_groups[tuple(r["decodedRgbSha256"] for r in profile["records"])].append(profile["profile"])
    assert len(records) == 1584

    verified = set()
    for digest, group in data["exactRgbGroups"].items():
        representative = group["representative"]
        path = source / representative["key"] / f"frame-{representative['frame']:05d}.png"
        with Image.open(path) as im:
            assert im.size == (640, 640)
            expected = im.convert("RGB").tobytes()
        assert hashlib.sha256(expected).hexdigest() == digest
        for alias in group["aliases"]:
            identity = (alias["key"], alias["frame"])
            assert identity not in verified
            record = records[identity]
            assert record["decodedRgbSha256"] == digest
            path = source / alias["key"] / f"frame-{alias['frame']:05d}.png"
            assert sha(path) == record["pngSha256"]
            with Image.open(path) as im:
                assert im.size == (640, 640)
                raw = im.convert("RGB").tobytes()
            assert raw == expected, identity
            verified.add(identity)
    assert verified == set(records)
    assert len(data["exactRgbGroups"]) == 504

    page_counts = defaultdict(int)
    shown_unique = []
    shown_context = []
    for page in data["pages"]:
        assert page["viewedOriginal"] is True and page["nativeCaptureCropPayloadExact"] is True
        path = PROJECT / page["path"]
        assert sha(path) == page["sha256"]
        with Image.open(path) as im:
            assert im.size == (1392, 1326)
        page_counts[page["type"]] += 1
        for frame in page["frames"]:
            assert records[(frame["key"], frame["frame"])]["decodedRgbSha256"] == frame["decodedRgbSha256"]
            if page["type"] == "unique":
                shown_unique.append(frame["decodedRgbSha256"])
            else:
                shown_context.append((frame["profile"], frame["frame"]))
    assert dict(page_counts) == {"unique": 42, "temporal": 21}
    assert len(shown_unique) == 504 and set(shown_unique) == set(data["exactRgbGroups"])
    context_frames = (65, 66, 67, 68, 73, 74, 75, 86, 87, 88, 89,
                      132, 133, 134, 135, 140, 141, 142, 153, 154, 155, 156, 157, 158, 163, 164, 165)
    assert shown_context == [(p, f) for p in range(9) for f in context_frames]

    for profile in data["profiles"]:
        assert all(c["sameSelectedSprite"] for c in profile["firstCuts"])
        assert [r["bridgeSamples"] for r in profile["returns"]] == [list(range(14))] * 2
        assert [r["walkEndpointFrame"] for r in profile["returns"]] == [88, 155]
        assert profile["returns"][1]["firstPositiveFoldFrame"] == 157
    data["status"] = "PASS — focused actual green presentation audit, scenarios 01/27/28"
    data["visualFindings"] = [
        "No actionable pose reset, mouth pop, detached seam, transparent joint core, clipped limb tip or body-origin jump observed in the reviewed actual captures.",
        "All 18 first cuts retain selected strike-10 and the visible mouth/front/support arrangement. First hit-cut RGB differences are recorded explicitly; selected-sprite hold is not claimed as universal byte-identical camera output.",
        "Return samples 00 through 13 each appear once, followed by the current-phase walk endpoint. The earlier short-bridge skip is absent in the reviewed presentation.",
        "Eye closure proceeds during retraction. Positive body folding begins only after the walk endpoint, then progresses to the requested half-folded fear posture."
    ]
    data["completion"] = {
        "sourceAndPageHashesUnchanged": True,
        "all1584FullRgbInstancesDirectByteComparedToRepresentative": True,
        "approximateEqualityUsed": False,
        "fullSequenceGroupsByExactRgb": list(sequence_groups.values()),
        "uniquePagesActuallyViewedOriginal": 42,
        "temporalPagesActuallyViewedOriginal": 21,
        "chronologicalContextInstances": 243,
        "allNineBodyLocalTransformsStable": True,
        "ownerApprovalClaimed": False,
        "fullAiOrAllScenarioAcceptanceClaimed": False,
        "unityCalls": 0,
        "productionEdits": 0,
        "finalAuthoring": "N/A — review artifacts only; no scene objects added or moved",
        "verifiedAtUtc": datetime.now(timezone.utc).isoformat()
    }
    coverage_path.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    required = ["prepare_review.py", "finalize_review.py", "independent-art-review.md", "review-coverage.json"]
    hashes = {name: sha(HERE / name) for name in required}
    (HERE / "completion-hashes.json").write_text(json.dumps({"status": data["status"], "sha256": hashes}, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"status": data["status"], "frames": len(verified), "uniqueFullRgb": 504,
                      "nativePagesViewed": dict(page_counts), "sequenceGroups": list(sequence_groups.values()),
                      "coverageSha256": hashes["review-coverage.json"]}, ensure_ascii=False), flush=True)


if __name__ == "__main__":
    main()
