"""Join explicit source-read receipts and manual module decisions without marking readiness.

No lexical quality detector, test claim, Unity call or CodeAudit mutation. Unknown or
changed bytes remain pending. Final integration evidence is supplied separately.
"""
import argparse
import collections
import datetime
import hashlib
import json
from pathlib import Path

EVIDENCE = Path(__file__).resolve().parent
PROJECT = EVIDENCE.parents[2]


def read(name):
    return json.loads((EVIDENCE / name).read_text(encoding="utf-8-sig"))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output", required=True)
    args = parser.parse_args()
    if Path(args.output).name != args.output or not args.output.endswith(".json"):
        parser.error("Use a new local evidence JSON filename.")
    baseline = {row["path"]: row for row in read("source-inventory.pre-handoff-03.json")["files"]}
    original = {row["path"]: row for row in read("read-trace.json")["reads"]}
    additional = {row["path"]: row for row in read("additional-read-trace.json")["reads"]}
    scoped = {row["path"]: row for row in read("scorpion.semantic-review.json")["reviews"]}
    recent_path = EVIDENCE / "recent-read-trace.json"
    recent = {row["path"]: row for row in read(recent_path.name)["reads"]} if recent_path.exists() else {}
    exclusions = {row["path"]: row for row in read("preserved-draft-exclusions.json")["files"]}
    modules = read("semantic-modules.json")["modules"]
    paths = sorted(p.relative_to(PROJECT).as_posix() for root in
                   ("Assets/EmergencyShift", "AgentScripts", "Concepts")
                   for p in (PROJECT / root).rglob("*.cs"))
    rows = []
    for path in paths:
        sha = hashlib.sha256((PROJECT / path).read_bytes()).hexdigest()
        matches = [module for module in modules if path.startswith(module["prefix"])]
        module = max(matches, key=lambda item: len(item["prefix"])) if matches else None
        row = {"path": path, "sha256": sha, "state": "pending", "reasons": []}
        if module is None:
            row["reasons"].append("No explicit manual module decision.")
        else:
            row["module"] = module["prefix"]
            row["conclusion"] = module["conclusion"]
            row["exceptions"] = module["exceptions"]
        if path in exclusions:
            if sha == exclusions[path]["sha256"]:
                row["state"] = "preserved_unread_draft_exclusion"
            else:
                row["reasons"].append("Preserved excluded draft bytes changed.")
        elif path in recent and sha == recent[path]["sha256"]:
            row["readReceipt"] = recent[path]
            if module is not None:
                row["state"] = "manual_source_conclusion"
        elif path in scoped and sha == scoped[path]["reviewedSha256"]:
            row["readReceipt"] = scoped[path]
            row["state"] = "scoped_source_and_native_verified"
        elif path in baseline and sha == baseline[path]["sha256"] and (path in original or path in additional):
            row["readReceipt"] = additional.get(path, original.get(path))
            row["baseline"] = "source-inventory.pre-handoff-03.json"
            if module is not None:
                row["state"] = "manual_source_conclusion"
        else:
            row["reasons"].append("Current bytes require a new explicit read/decision; old receipt or matching name is insufficient.")
        rows.append(row)
    summary = dict(collections.Counter(row["state"] for row in rows))
    result = {
        "task": "TASK-008", "capturedUtc": datetime.datetime.now(datetime.timezone.utc).isoformat(),
        "state": "Source-only progress; final integration and changed-source refresh pending",
        "qualityMethod": "Explicit manual module conclusions attached to recorded real reads. Hashes only qualify whether those bytes changed, never infer correctness. No readiness flags or CodeAudit records are modified.",
        "summary": summary,
        "pendingPaths": [row["path"] for row in rows if row["state"] == "pending"],
        "integrationComplete": False, "buildVerified": False, "ownerAcceptance": False,
        "files": rows
    }
    with (EVIDENCE / args.output).open("x", encoding="utf-8") as output:
        json.dump(result, output, ensure_ascii=False, indent=2)
        output.write("\n")
    print(json.dumps({"summary": summary, "pendingPaths": result["pendingPaths"]}, ensure_ascii=False))


if __name__ == "__main__":
    main()
