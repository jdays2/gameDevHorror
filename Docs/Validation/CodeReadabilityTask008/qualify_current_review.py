"""Apply recorded manual TASK-008 source decisions after the final metadata refresh.

This copies explicit read receipts and human conclusions; it does not inspect tokens,
infer quality, run Unity, or mark integration/build/task readiness. A pending source,
changed SHA or mismatching inventory stops the update before any registry write.
"""
import argparse
import collections
import copy
import hashlib
import json
from pathlib import Path

EVIDENCE = Path(__file__).resolve().parent
PROJECT = EVIDENCE.parents[2]
REGISTRY = PROJECT / "Docs/Reports/CodeAudit.json"


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--review", required=True, help="New source-review JSON in this evidence directory")
    parser.add_argument("--before", required=True, help="New immutable registry backup filename")
    args = parser.parse_args()
    for value in (args.review, args.before):
        if Path(value).name != value or not value.endswith(".json"):
            parser.error("Use local JSON filenames only.")
    review = json.loads((EVIDENCE / args.review).read_text(encoding="utf-8-sig"))
    original_bytes = REGISTRY.read_bytes()
    registry = json.loads(original_bytes.decode("utf-8-sig"))
    decisions = {row["path"]: row for row in review["files"]}
    entries = {row["path"]: row for row in registry["files"]}
    if set(entries) != set(decisions) or len(entries) != len(registry["files"]):
        raise RuntimeError("Source review and refreshed registry inventories differ.")
    allowed = {"manual_source_conclusion", "scoped_source_and_native_verified", "preserved_unread_draft_exclusion"}
    for path, decision in decisions.items():
        if decision["state"] not in allowed:
            raise RuntimeError("Unresolved source decision: " + path)
        sha = hashlib.sha256((PROJECT / path).read_bytes()).hexdigest()
        if sha != decision["sha256"] or sha != entries[path]["sha256"]:
            raise RuntimeError("Source/metadata changed after the recorded review: " + path)
        if decision["state"] != "preserved_unread_draft_exclusion":
            if not decision.get("readReceipt") or not decision.get("conclusion"):
                raise RuntimeError("Missing actual read receipt/manual conclusion: " + path)
    for path, decision in decisions.items():
        row = entries[path]
        old = {key: copy.deepcopy(value) for key, value in row.items()
               if key.startswith("review") or key in ("notes", "responsibility", "dependencies", "usageEvidence")}
        old.pop("reviewHistory", None)
        if old:
            row.setdefault("reviewHistory", []).append(old)
        row["reviewDate"] = "2026-11-04"
        row.pop("reviewedCommit", None)
        row["reviewedSnapshot"] = "Docs/Validation/CodeReadabilityTask008/" + args.review
        row["reviewReport"] = "Docs/Reports/CodeReadabilityTask008.md"
        row["task008Decision"] = {"evidence": "Docs/Validation/CodeReadabilityTask008/" + args.review,
                                  "state": decision["state"], "module": decision.get("module")}
        if decision["state"] == "preserved_unread_draft_exclusion":
            row.pop("reviewedSha256", None)
            row["reviewStatus"] = "preserved_unread_draft_exclusion"
            row["notes"] = "Owner-paused non-imported draft; intentionally unread/uncompiled/unexecuted. Exact bytes and reason: preserved-draft-exclusions.json. Not counted as a reviewed source."
            row["reviewScope"] = "Explicit preserved draft exclusion; metadata/hash preservation only."
            row["sourceChangedSinceReview"] = False
        else:
            row["reviewStatus"] = "reviewed_boundary"
            row["reviewedSha256"] = decision["sha256"]
            row["sourceChangedSinceReview"] = False
            row["responsibility"] = decision.get("module", "Explicit scoped source decision")
            specific = decision["readReceipt"].get("conclusion", decision["readReceipt"].get("notes", ""))
            row["notes"] = decision["conclusion"] + (" Per-file decision: " + specific if isinstance(specific, str) and specific else "")
            row["usageEvidence"] = decision["readReceipt"]
            row["reviewScope"] = decision.get("exceptions", "Manual source review; runtime evidence qualified separately.")
            # Old generic dependency text remains history, not a new inferred verdict.
            row.pop("dependencies", None)
    registry["counts"] = dict(collections.Counter(row["reviewStatus"] for row in registry["files"]))
    registry["task008SourceReview"] = {"date": "2026-11-04", "evidence": args.review,
                                       "counts": review["summary"], "integrationQualifiedHere": False}
    for flag in ("complete", "sourceCoverageComplete", "technicalSourceReviewComplete", "technicalMigrationComplete"):
        registry[flag] = False
    registry["sourceCoverage"]["hashesCurrent"] = False
    registry["limits"] = "Current manual TASK-008 source decisions and named preserved draft exclusions are recorded. Integration/build readiness requires separate actual evidence and explicit final qualification; no readiness inferred by this script."
    with (EVIDENCE / args.before).open("xb") as backup:
        backup.write(original_bytes)
    REGISTRY.write_text(json.dumps(registry, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"sourceDecisionsApplied": len(decisions), "counts": registry["counts"], "integrationQualifiedHere": False}))


if __name__ == "__main__":
    main()
