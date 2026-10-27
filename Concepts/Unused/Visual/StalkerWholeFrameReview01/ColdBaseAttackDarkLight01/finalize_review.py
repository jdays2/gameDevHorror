"""Finalize only after the explicitly recorded manual page view batches."""
from pathlib import Path
import datetime
import hashlib
import json
from PIL import Image

HERE = Path(__file__).resolve().parent
PROJECT = HERE.parents[4]
SOURCE = PROJECT / "Concepts/Used/Sleeper/Postures05"


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    path = HERE / "review-coverage.json"
    data = json.loads(path.read_text())
    assert len(data["pages"]) == 80 and all(page["viewedOriginal"] for page in data["pages"]), "Manual viewing incomplete"
    for name, digest in data["sourceDependencySha256"].items():
        assert sha(SOURCE / name) == digest, name
    for name, digest in data["sourceRawPngSha256"].items():
        assert sha(SOURCE / "Raw" / f"{name}.png") == digest, name
    groups = {}
    for record in data["records"]:
        groups.setdefault(record["rgbaSha256"], record)
    assert len(groups) == 468
    covered = set()
    exact_aliases = 0
    for digest, record in groups.items():
        with Image.open(SOURCE / "Raw" / f"{record['representative']}.png") as im:
            reference = im.convert("RGBA").tobytes()
        assert hashlib.sha256(reference).hexdigest() == digest
        for name in record["aliases"]:
            with Image.open(SOURCE / "Raw" / f"{name}.png") as im:
                assert im.convert("RGBA").tobytes() == reference, name
            assert name not in covered
            covered.add(name)
        exact_aliases += len(record["aliases"]) - 1
    assert len(covered) == 792 and exact_aliases == 324
    background_coverage = {}
    explicit_strikes = {f"strike-{i:02d}" for i in range(24)}
    for background in ("dark", "light"):
        pages = [r for r in data["pages"] if r["background"] == background]
        assert len(pages) == 40
        for page in pages:
            page_path = PROJECT / page["path"]
            assert sha(page_path) == page["sha256"]
            with Image.open(page_path) as im:
                assert im.size == (1304, 1467)
        names = {name for page in pages for name in page["coveredNames"]}
        representatives = {name for page in pages for name in page["representatives"]}
        assert names == covered and explicit_strikes <= representatives
        background_coverage[background] = {"sourceNames": len(names), "exactUniqueWholeRgba": 468, "explicitStrikeNames": sorted(explicit_strikes), "pagesActuallyViewedOriginal": 40}
    data.update({"status": "COMPLETE INDEPENDENT VISUAL REVIEW; NO ACTIONABLE BASE COLD ART DEFECT OBSERVED", "finalCheckedUtc": datetime.datetime.now(datetime.timezone.utc).isoformat(), "directFullImageRgbaAliasEqualityChecked": True, "finalSourceAndPageHashesUnchanged": True, "backgroundCoverage": background_coverage, "visualFindings": [], "ownerArtApprovalClaimed": False, "runtimeAcceptanceClaimed": False})
    path.write_text(json.dumps(data, indent=2) + "\n")
    result = {"scope": "792 base cold attacks on DARK/LIGHT", "viewedOriginalPages": 80, "exactUniqueWholeRgba": 468, "byteExactDuplicateAliases": exact_aliases, "sourceNameBackgroundCases": 1584, "sourceAndPagesUnchanged": True, "filesSha256": {name: sha(HERE / name) for name in ("review-coverage.json", "independent-art-review.md", "prepare_review.py", "finalize_review.py")}}
    (HERE / "completion-hashes.json").write_text(json.dumps(result, indent=2) + "\n")
    print(json.dumps(result, indent=2))


if __name__ == "__main__":
    main()
