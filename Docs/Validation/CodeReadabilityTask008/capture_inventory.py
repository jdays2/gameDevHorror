"""Capture preliminary source metadata into this task's reserved evidence directory.

Uses the existing read-only collector; never refreshes CodeAudit or calls Unity.
"""
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
    (OUTPUT / name).write_text(json.dumps(value, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def main():
    spec = importlib.util.spec_from_file_location("task008_inventory", PROJECT / "Tools/Maintenance/audit_code.py")
    collector = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(collector)
    inventory = collector.inventory()
    trace = json.loads((OUTPUT / "read-trace.json").read_text(encoding="utf-8"))
    reads = {entry["path"]: entry for entry in trace["reads"]}
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
        row["finalQualification"] = "pending explicit predecessor handoff and final revision review"
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
        "state": "preliminary_during_TASK007_game_lease",
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
            "TASK007 owns the changing game; this is not the post-handoff preservation baseline.",
            "Saved YAML external GUID traversal is neither native AssetDatabase nor local fileID validation.",
            "No Unity call, compile, test, build, CodeAudit refresh or gameplay write is performed.",
            "Historical and unread sources are not automatically marked reviewed."
        ],
        "files": rows
    }
    write("source-inventory.pre-handoff.json", result)
    print(json.dumps({key: result[key] for key in ("state", "head", "areaCounts", "sourceScopeCounts", "traceFiles")}, ensure_ascii=False))


if __name__ == "__main__":
    main()
