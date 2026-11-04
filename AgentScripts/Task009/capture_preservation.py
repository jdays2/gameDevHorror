"""Preserve current imported inputs across a capture-only Unity window."""
import argparse
import hashlib
import json
from pathlib import Path
import subprocess


ROOT = Path(__file__).resolve().parents[2]


def git(*args):
    return subprocess.check_output(["git", *args], cwd=ROOT, text=True, encoding="utf-8")


def snapshot(extra_paths=()):
    raw = git("status", "--porcelain=v1", "-uall", "--", "Assets", "Packages", "ProjectSettings")
    paths = {line[3:] for line in raw.splitlines()}
    paths.update(extra_paths)
    paths.update([
        "Assets/EmergencyShift/Scenes/MechanicsSandbox.unity",
        "Assets/EmergencyShift/Data/Audio/PlayerHitAudio.asset",
        "Assets/EmergencyShift/Resources/ProjectContext.prefab",
    ])
    return {path: hashlib.sha256((ROOT / path).read_bytes()).hexdigest() if (ROOT / path).is_file() else None
            for path in sorted(paths)}


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("mode", choices=["before", "after"])
    parser.add_argument("--label", required=True)
    args = parser.parse_args()
    if not args.label.replace("-", "").isalnum():
        raise ValueError("Use a simple capture label.")
    baseline = ROOT / f"work/task009-player-hit/{args.label}-preservation-before.json"
    if args.mode == "before":
        current = snapshot()
        if baseline.exists():
            raise FileExistsError("Preserve an existing baseline before rerunning.")
        baseline.parent.mkdir(parents=True, exist_ok=True)
        baseline.write_text(json.dumps({"head": git("rev-parse", "HEAD").strip(), "files": current}, indent=2) + "\n",
                            encoding="utf-8")
        print(json.dumps({"protectedImportedFiles": len(current), "baseline": str(baseline)}))
    else:
        previous = json.loads(baseline.read_text(encoding="utf-8"))
        current = snapshot(previous["files"])
        changed = [path for path in current.keys() | previous["files"].keys()
                   if current.get(path) != previous["files"].get(path)]
        result = {"baselineHead": previous["head"], "protectedImportedFiles": len(previous["files"]),
                  "changed": sorted(changed), "passed": not changed,
                  "boundary": "Current dirty/untracked imported inputs plus the open authored scene, production B profile and ProjectContext. No imported changes are authorized by this capture-only window."}
        output = ROOT / f"Docs/Validation/PlayerHitTask009/{args.label}/preservation.json"
        output.parent.mkdir(parents=True, exist_ok=True)
        output.write_text(json.dumps(result, indent=2) + "\n", encoding="utf-8")
        print(json.dumps(result))
        if changed:
            raise SystemExit(2)


if __name__ == "__main__":
    main()
