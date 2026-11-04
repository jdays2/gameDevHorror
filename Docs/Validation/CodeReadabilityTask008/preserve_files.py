"""Capture/compare saved Unity inputs; never change an imported file."""
import argparse
import hashlib
import json
from pathlib import Path

EVIDENCE = Path(__file__).resolve().parent
PROJECT = EVIDENCE.parents[2]
BASELINE = EVIDENCE / "saved-inputs.before.json"


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
    parser.add_argument("--allow", nargs="*", default=[])
    parser.add_argument("--report", default="saved-inputs.after.json")
    args = parser.parse_args()
    current = inventory()
    handoff = json.loads((PROJECT / "Docs/Validation/Task007FixedSprites06/handoff-to-task008.json").read_text(encoding="utf-8-sig"))
    foreign_changes = [row["path"] for row in handoff["ownImportedFilesToPreserve"] if current.get(row["path"]) != row["sha256"]]
    if args.mode == "capture":
        if foreign_changes:
            raise RuntimeError("Handoff inputs differ: " + str(foreign_changes))
        with BASELINE.open("x", encoding="utf-8") as output:
            json.dump({"task": "TASK-008", "files": current, "task007Files": len(handoff["ownImportedFilesToPreserve"]), "scope": "Saved Assets/Packages/ProjectSettings bytes, not unsaved Editor buffers"}, output, indent=2)
        print(json.dumps({"capturedFiles": len(current), "task007FilesVerified": len(handoff["ownImportedFilesToPreserve"])}))
        return
    before = json.loads(BASELINE.read_text(encoding="utf-8"))["files"]
    changed = sorted(path for path in before.keys() | current.keys() if before.get(path) != current.get(path))
    unexpected = [path for path in changed if path not in set(args.allow)]
    result = {"passed": not unexpected and not foreign_changes, "changed": changed, "unexpected": unexpected, "task007Differences": foreign_changes, "task007FilesVerified": len(handoff["ownImportedFilesToPreserve"])}
    if Path(args.report).name != args.report:
        parser.error("--report must be an evidence filename")
    with (EVIDENCE / args.report).open("x", encoding="utf-8") as output:
        json.dump(result, output, indent=2)
    print(json.dumps(result))
    if not result["passed"]:
        raise SystemExit(1)


if __name__ == "__main__":
    main()
