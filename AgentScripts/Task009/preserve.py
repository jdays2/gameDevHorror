"""Snapshot shared dirty inputs and selected clean boundaries; never edits game files."""
import argparse
import hashlib
import json
from pathlib import Path
import subprocess

ROOT = Path(__file__).resolve().parents[2]
EVIDENCE = ROOT / "Docs/Validation/PlayerHitTask009"
BOUNDARIES = ["Assets/EmergencyShift/Runtime/Levels/LevelContext2D.cs",
              "Assets/EmergencyShift/Runtime/Player/PlayerLife2D.cs",
              "Assets/EmergencyShift/Resources/ProjectContext.prefab", "Docs/Audio/AUDIO_GUIDE.md"]


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest() if path.is_file() else None


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("mode", choices=["snapshot", "verify"])
    args = parser.parse_args()
    path = EVIDENCE / "preservation-baseline.json"
    if args.mode == "snapshot":
        raw = subprocess.check_output(["git", "status", "--porcelain=v1", "-uall"], cwd=ROOT, text=True)
        dirty = [line[3:] for line in raw.splitlines() if line[3:].startswith(("Assets/", "Packages/", "ProjectSettings/"))]
        baseline = {"head": subprocess.check_output(["git", "rev-parse", "HEAD"], cwd=ROOT, text=True).strip(),
                    "foreignDirty": {p: digest(ROOT / p) for p in dirty}, "boundaries": {}}
        for item in BOUNDARIES:
            baseline["boundaries"][item] = {"sha256": digest(ROOT / item),
                                           "text": (ROOT / item).read_text(encoding="utf-8-sig")}
        path.write_text(json.dumps(baseline, indent=2) + "\n", encoding="utf-8")
        print(json.dumps({"head": baseline["head"], "protectedDirty": len(dirty), "boundaries": len(BOUNDARIES)}))
    else:
        baseline = json.loads(path.read_text(encoding="utf-8"))
        changed = [p for p, old in baseline["foreignDirty"].items()
                   if p not in BOUNDARIES and digest(ROOT / p) != old]
        result = {"protectedDirty": len(baseline["foreignDirty"]), "unexpectedChanged": changed,
                  "sharedBoundaryDeltasReviewedSeparately": BOUNDARIES}
        (EVIDENCE / "preservation-result.json").write_text(json.dumps(result, indent=2) + "\n", encoding="utf-8")
        print(json.dumps(result, indent=2))
        if changed:
            raise SystemExit(2)


if __name__ == "__main__":
    main()
