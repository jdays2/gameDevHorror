"""Capture/compare saved Unity inputs; never change an imported file."""
import argparse
import hashlib
import json
from pathlib import Path

EVIDENCE = Path(__file__).resolve().parent
PROJECT = EVIDENCE.parents[2]
DEFAULT_HANDOFF = "Docs/Validation/Task007FixedSprites06/handoff-to-task008.json"


def digest(path):
    with path.open("rb") as stream:
        return hashlib.file_digest(stream, "sha256").hexdigest()


def inventory():
    return {
        path.relative_to(PROJECT).as_posix(): digest(path)
        for root in ("Assets", "Packages", "ProjectSettings")
        for path in sorted((PROJECT / root).rglob("*"))
        if path.is_file()
    }


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("mode", choices=("capture", "verify"))
    parser.add_argument("--baseline", default="saved-inputs.before.json")
    parser.add_argument("--handoff", help="Capture: explicit current TASK007 preservation manifest; verify defaults to the recorded manifest")
    parser.add_argument("--allow", nargs="*", default=[])
    parser.add_argument("--report", default="saved-inputs.after.json")
    args = parser.parse_args()
    if Path(args.baseline).name != args.baseline or not args.baseline.endswith(".json"):
        parser.error("--baseline must be a local evidence JSON filename")
    if Path(args.report).name != args.report or not args.report.endswith(".json"):
        parser.error("--report must be a local evidence JSON filename")
    baseline = EVIDENCE / args.baseline
    captured = json.loads(baseline.read_text(encoding="utf-8")) if args.mode == "verify" else None
    recorded_handoff = captured.get("task007Handoff", DEFAULT_HANDOFF) if captured else DEFAULT_HANDOFF
    handoff_name = args.handoff or recorded_handoff
    if captured and handoff_name != recorded_handoff:
        parser.error("Verification must use the manifest recorded in its baseline")
    handoff_path = (PROJECT / handoff_name).resolve()
    if not handoff_path.is_relative_to(PROJECT.resolve()) or not handoff_path.is_file():
        parser.error("--handoff must identify an existing manifest within this project")
    handoff_sha = digest(handoff_path)
    if captured and captured.get("task007HandoffSha256", handoff_sha) != handoff_sha:
        raise RuntimeError("The recorded preservation manifest changed; do not silently replace baseline evidence")
    current = inventory()
    handoff = json.loads(handoff_path.read_text(encoding="utf-8-sig"))
    foreign_changes = [row["path"] for row in handoff["ownImportedFilesToPreserve"] if current.get(row["path"]) != row["sha256"]]
    if args.mode == "capture":
        if foreign_changes:
            raise RuntimeError("Handoff inputs differ: " + str(foreign_changes))
        with baseline.open("x", encoding="utf-8") as output:
            json.dump({"task": "TASK-008", "files": current, "task007Files": len(handoff["ownImportedFilesToPreserve"]),
                       "task007Handoff": handoff_name, "task007HandoffSha256": handoff_sha,
                       "scope": "Saved Assets/Packages/ProjectSettings bytes, not unsaved Editor buffers"}, output, indent=2)
        print(json.dumps({"capturedFiles": len(current), "task007FilesVerified": len(handoff["ownImportedFilesToPreserve"])}))
        return
    before = captured["files"]
    changed = sorted(path for path in before.keys() | current.keys() if before.get(path) != current.get(path))
    unexpected = [path for path in changed if path not in set(args.allow)]
    result = {"passed": not unexpected and not foreign_changes, "baseline": args.baseline,
              "task007Handoff": handoff_name, "task007HandoffSha256": handoff_sha,
              "changed": changed, "unexpected": unexpected, "task007Differences": foreign_changes,
              "task007FilesVerified": len(handoff["ownImportedFilesToPreserve"])}
    with (EVIDENCE / args.report).open("x", encoding="utf-8") as output:
        json.dump(result, output, indent=2)
    print(json.dumps(result))
    if not result["passed"]:
        raise SystemExit(1)


if __name__ == "__main__":
    main()
