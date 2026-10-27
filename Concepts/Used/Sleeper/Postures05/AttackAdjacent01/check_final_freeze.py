"""Read-only final hash/content check; writes only its own compact evidence file."""
from pathlib import Path
import hashlib
import json
import re


HERE = Path(__file__).resolve().parent
BODY = HERE.parent
ROOT = next(path for path in HERE.parents if (path / "Assets").is_dir())


def read(path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def sha(path):
    digest = hashlib.sha256()
    with path.open("rb") as source:
        for block in iter(lambda: source.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest()


def main():
    expected = {}
    for path in (BODY / "body-native-inputs.json", BODY / "attack-native-inputs.json",
                 HERE / "final-source-hashes.json"):
        for name, value in read(path).items():
            assert name not in expected or expected[name] == value, name
            expected[name] = value
    for path in (BODY / "body-composition.json", BODY / "attack-composition.json"):
        for record in read(path)["records"]:
            expected[(BODY / "Raw" / (record["name"] + ".png")).relative_to(ROOT).as_posix()] = record["rawPngSha256"]
    for name, value in read(HERE / "native-composition.json")["rawPngSha256"].items():
        expected[(HERE / "Raw" / (name + ".png")).relative_to(ROOT).as_posix()] = value
    packed = read(HERE / "native-packed-verification.json")
    for record in packed["records"]:
        expected[(HERE / "Clean" / (record["name"] + ".png")).relative_to(ROOT).as_posix()] = record["cleanPngSha256"]
    manifests = [BODY / "sprites.json", BODY / "attack-sprites.json", HERE / "attack-sprites.json"]
    atlas_count = 0
    for path in manifests:
        for atlas in read(path)["atlases"]:
            expected[atlas["asset"]] = atlas["sha256"]
            atlas_count += 1
    reviews = [(BODY / "body-native-green-review.json", BODY / "body-native-unique-map.json", BODY / "BodyNativeUnique"),
               (HERE / "native-green-review.json", HERE / "native-unique-map.json", HERE / "NativeUnique"),
               (BODY / "base-attack-native-green-review.json", BODY / "base-attack-native-unique-map.json", BODY / "BaseAttackNativeUnique")]
    page_count = 0
    for path, mapping, family in reviews:
        report = read(path)
        assert report["status"] == "Native green visual inspection complete", path
        assert report["sourcePosesCoveredIncludingProvenExactRgbaAliases"] == report["sourcePoses"]
        assert report["reviewedUniqueFrames"] == report["uniqueWholeRgbaFrames"]
        assert not report["resampling"] and not report["findings"]
        assert sha(mapping) == report["mapSha256"]
        for name, value in report["pageHashes"].items():
            expected[(family / name).relative_to(ROOT).as_posix()] = value
            page_count += 1
    def reviewed_names(mapping):
        names = [name for record in read(mapping)["records"] for name in record["aliases"]]
        assert len(names) == len(set(names)), mapping
        return set(names)
    base_attack_names = reviewed_names(reviews[2][1])
    odd_attack_names = reviewed_names(reviews[1][1])
    reused = read(HERE / "native-composition.json")["reusedPoseMapping"]
    assert base_attack_names == {record["source"] for record in reused}
    reused_names = {record["target"] for record in reused}
    dense_names = set(read(HERE / "native-composition.json")["rawPngSha256"])
    assert not reused_names.intersection(odd_attack_names)
    assert reused_names.union(odd_attack_names) == dense_names
    strikes = {f"strike-{index:02d}" for index in range(24)}
    assert strikes.issubset(base_attack_names) and strikes.issubset(reused_names)
    for record in read(HERE / "copied-native-files.json"):
        for family, field in ((BODY, "source"), (HERE, "target")):
            name = (family / record[field]).relative_to(ROOT).as_posix()
            assert name not in expected or expected[name] == record["sha256"], name
            expected[name] = record["sha256"]
    mismatches = [name for name, value in expected.items() if not (ROOT / name).is_file() or sha(ROOT / name) != value]
    assert not mismatches, mismatches
    assert sha(HERE / "attack-sprites.json") == packed["manifestSha256"]
    assert sha(HERE / "pack_native_visible_exact.py") == packed["packerSha256"]
    broken_links = []
    for path in BODY.rglob("*.md"):
        for link in re.findall(r"!?\[[^\]]*\]\(([^)]+)\)", path.read_text(encoding="utf-8")):
            target = link.split("#")[0].strip("<>")
            if target and not re.match(r"^[a-zA-Z]+:", target) and not (path.parent / target).exists():
                broken_links.append([path.relative_to(ROOT).as_posix(), link])
    assert not broken_links, broken_links
    body_review = read(reviews[0][0])
    attack_review = read(reviews[1][0])
    base_attack_review = read(reviews[2][0])
    result = {
        "status": "PASS: frozen source, Raw, Clean, production atlas and reviewed-page bytes unchanged",
        "checkedFileHashes": len(expected),
        "sourceMaps": 3,
        "productionAtlasPngsIncludingHistoricalBase": atlas_count,
        "nativeGreenPagesActuallyInspectedAndHashChecked": page_count,
        "bodyGreen": {key: body_review[key] for key in ("sourcePoses", "uniqueWholeRgbaFrames", "nativeGreenPagesActuallyViewed")},
        "newOddAttackGreen": {key: attack_review[key] for key in ("sourcePoses", "uniqueWholeRgbaFrames", "nativeGreenPagesActuallyViewed")},
        "baseAttackGreen": {key: base_attack_review[key] for key in ("sourcePoses", "uniqueWholeRgbaFrames", "nativeGreenPagesActuallyViewed")},
        "allDenseAttackGreenCovered": {"poses": len(dense_names), "exactlyReusedAndReviewed": len(reused_names), "newAndReviewed": len(odd_attack_names), "strikeFramesCovered": len(strikes), "disjointCoverage": True},
        "denseAttack": {key: packed[key] for key in ("poses", "uniqueImages", "atlases", "copiedOldNativePosesByteExact", "newlyPaintedOddPoses", "materialLayersChecked", "addedAlphaPixels", "removedAlphaPixels", "changedVisibleRgbPixels", "manifestSha256")},
        "brokenOwnReadmeLinks": broken_links,
        "independentDarkLightReview": "Owned by independent auditor; not certified by this green pass",
        "unityRuntimeAndInstallation": "Owned by root; this producer never called Unity",
        "sceneAuthoringReview": "Not applicable: no scene objects edited",
        "ownerCreativeApproval": "Not inferred",
    }
    (HERE / "final-freeze-check.json").write_text(json.dumps(result, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(result, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
