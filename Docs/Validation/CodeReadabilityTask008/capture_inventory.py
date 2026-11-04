"""Capture preliminary source metadata into this task's reserved evidence directory.

Uses the existing read-only collector; never refreshes CodeAudit or calls Unity.
"""
import argparse
import collections
import datetime
import hashlib
import importlib.util
import json
from pathlib import Path
import subprocess
import sys

sys.dont_write_bytecode = True
OUTPUT = Path(__file__).resolve().parent
PROJECT = OUTPUT.parents[2]


def write(name, value):
    with (OUTPUT / name).open("x", encoding="utf-8") as destination:
        destination.write(json.dumps(value, ensure_ascii=False, indent=2) + "\n")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--name", default="source-inventory.pre-handoff.json")
    parser.add_argument("--phase", choices=("pre-handoff", "post-handoff"), default="pre-handoff")
    args = parser.parse_args()
    if Path(args.name).name != args.name or not args.name.startswith("source-inventory.") or not args.name.endswith(".json"):
        parser.error("--name must be a source-inventory.*.json filename in this evidence directory")
    if (OUTPUT / args.name).exists():
        parser.error("Snapshot already exists; choose a new --name to preserve prior evidence")
    spec = importlib.util.spec_from_file_location("task008_inventory", PROJECT / "Tools/Maintenance/audit_code.py")
    collector = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(collector)
    inventory = collector.inventory()
    reads = {}
    for name in ("read-trace.json", "additional-read-trace.json", "recent-read-trace.json", "scorpion.semantic-review.json"):
        if not (OUTPUT / name).exists():
            continue
        trace = json.loads((OUTPUT / name).read_text(encoding="utf-8"))
        reads.update({entry["path"]: entry for entry in trace.get("reads", trace.get("reviews", []))})
    rows = []
    for path, entry in inventory.items():
        row = {key: entry[key] for key in ("path", "sourceScope", "sha256", "guid", "lines", "serializedUsers", "dependencyScenes")}
        row["tracePresent"] = path in reads
        if path.startswith("Assets/EmergencyShift/Runtime/Development/"):
            row["assemblyArea"] = "Development"
        elif path.startswith("Assets/EmergencyShift/Runtime/"):
            row["assemblyArea"] = "Runtime"
        elif path.startswith("Assets/EmergencyShift/Editor/"):
            row["assemblyArea"] = "Editor"
        elif path.startswith("Assets/EmergencyShift/Tests/"):
            row["assemblyArea"] = "Tests"
        else:
            row["assemblyArea"] = "Outside automatic Unity compilation"
        row["finalQualification"] = "pending final revision review; read locator is not a reviewedSha256 verdict"
        rows.append(row)
    git = lambda *args: subprocess.run(["git", "--no-optional-locks", *args], cwd=PROJECT, check=True, capture_output=True).stdout
    status = git("status", "--porcelain=v1", "-uall")
    historical = json.loads((PROJECT / "Docs/Reports/CodeAudit.json").read_text(encoding="utf-8-sig"))
    assemblies = []
    for path in sorted((PROJECT / "Assets/EmergencyShift").rglob("*.asmdef")):
        value = json.loads(path.read_text(encoding="utf-8-sig"))
        assemblies.append({"path": path.relative_to(PROJECT).as_posix(), **value})
    result = {
        "schemaVersion": 1,
        "task": "TASK-008",
        "state": args.phase + "_mechanical_metadata",
        "capturedUtc": datetime.datetime.now(datetime.timezone.utc).isoformat(),
        "head": git("rev-parse", "HEAD").decode().strip(),
        "gitStatusSha256": hashlib.sha256(status).hexdigest(),
        "gitStatusEntries": len(status.splitlines()),
        "areaCounts": dict(collections.Counter(row["assemblyArea"] for row in rows)),
        "sourceScopeCounts": dict(collections.Counter(row["sourceScope"] for row in rows)),
        "traceFiles": sum(row["tracePresent"] for row in rows),
        "pendingTracePaths": [row["path"] for row in rows if not row["tracePresent"]],
        "historicalRegistry": {key: historical.get(key) for key in ("date", "baselineRevision", "verifiedCodeRevision", "complete", "sourceCoverageComplete", "technicalSourceReviewComplete", "technicalMigrationComplete", "ownerAcceptanceComplete")},
        "assemblies": assemblies,
        "limits": [
            "Metadata and read locators do not prove semantic correctness or final source coverage.",
            "This working-tree metadata snapshot is not a native scene preservation baseline.",
            "Saved YAML external GUID traversal is neither native AssetDatabase nor local fileID validation.",
            "No Unity call, compile, test, build, CodeAudit refresh or gameplay write is performed.",
            "Historical and unread sources are not automatically marked reviewed."
        ],
        "files": rows
    }
    write(args.name, result)
    print(json.dumps({key: result[key] for key in ("state", "head", "areaCounts", "sourceScopeCounts", "traceFiles")}, ensure_ascii=False))


if __name__ == "__main__":
    main()
