"""Exercise the real preservation CLI against isolated saved-input/handoff files."""
import hashlib
import json
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile

EVIDENCE = Path(__file__).resolve().parent


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    output = EVIDENCE / "preservation-cli.after-resume.json"
    if output.exists():
        raise RuntimeError("Preserve previous test evidence.")
    temporary = Path(tempfile.mkdtemp(prefix="_preservation-cli-", dir=EVIDENCE))
    results = []
    completed = False
    try:
        project = temporary / "project"
        folder = project / "Docs/Validation/CodeReadabilityTask008"
        folder.mkdir(parents=True)
        script = folder / "preserve_files.py"
        shutil.copyfile(EVIDENCE / script.name, script)
        for name in ("Assets", "Packages", "ProjectSettings"):
            (project / name).mkdir()
        own = project / "Assets/Own.asset"
        foreign = project / "Assets/Foreign.asset"
        own.write_bytes(b"own initial\n")
        foreign.write_bytes(b"foreign initial\n")
        original = project / "Docs/Validation/Task007FixedSprites06/handoff-to-task008.json"
        original.parent.mkdir(parents=True)

        def manifest(path):
            path.write_text(json.dumps({"ownImportedFilesToPreserve": [
                {"path": "Assets/Foreign.asset", "sha256": sha(foreign)}
            ]}), encoding="utf-8")

        def run(*args):
            return subprocess.run([sys.executable, "-B", str(script), *args], cwd=project,
                                  capture_output=True, text=True)

        def require(condition, name, command=None):
            results.append({"name": name, "passed": bool(condition)})
            if not condition:
                detail = command.stderr if command else ""
                raise AssertionError(name + "\n" + detail)

        manifest(original)
        command = run("capture")
        require(command.returncode == 0, "Original handoff captures a new baseline", command)
        own.write_bytes(b"own intentional change\n")
        command = run("verify", "--allow", "Assets/Own.asset", "--report", "own.after.json")
        data = json.loads((folder / "own.after.json").read_text(encoding="utf-8"))
        require(command.returncode == 0 and data["passed"] and data["changed"] == ["Assets/Own.asset"],
                "An exact own change passes while foreign input stays protected", command)

        foreign.write_bytes(b"foreign approved restoration in another window\n")
        command = run("verify", "--allow", "Assets/Own.asset", "Assets/Foreign.asset", "--report", "foreign.after.json")
        data = json.loads((folder / "foreign.after.json").read_text(encoding="utf-8"))
        require(command.returncode != 0 and not data["passed"] and data["task007Differences"] == ["Assets/Foreign.asset"],
                "An own allow-list cannot conceal changed protected foreign input", command)

        current = project / "Docs/Validation/current-task007-handoff.json"
        manifest(current)
        current_name = current.relative_to(project).as_posix()
        command = run("capture", "--baseline", "saved-inputs.phase2.json", "--handoff", current_name)
        require(command.returncode == 0, "A fresh explicit handoff supports the approved later state", command)
        phase2 = folder / "saved-inputs.phase2.json"
        before = sha(phase2)
        command = run("capture", "--baseline", phase2.name, "--handoff", current_name)
        require(command.returncode != 0 and sha(phase2) == before,
                "Recapture refuses to overwrite an existing baseline", command)
        command = run("verify", "--baseline", phase2.name, "--report", "phase2.after.json")
        data = json.loads((folder / "phase2.after.json").read_text(encoding="utf-8"))
        require(command.returncode == 0 and data["passed"] and data["task007Handoff"] == current_name and not data["changed"],
                "Verification inherits the recorded handoff rather than the old default", command)

        current.write_bytes(current.read_bytes() + b"\n")
        command = run("verify", "--baseline", phase2.name, "--report", "tampered.after.json")
        require(command.returncode != 0 and not (folder / "tampered.after.json").exists() and sha(phase2) == before,
                "A changed manifest is rejected before writing a misleading report", command)
        command = run("capture", "--handoff", "../outside.json", "--baseline", "outside.before.json")
        require(command.returncode != 0 and not (folder / "outside.before.json").exists(),
                "An outside manifest cannot become a project baseline", command)
        completed = True
    finally:
        # Check the absolute target before recursive cleanup; only this created fixture is removed.
        resolved = temporary.resolve()
        if resolved.parent != EVIDENCE.resolve() or not resolved.name.startswith("_preservation-cli-"):
            raise RuntimeError("Refuse cleanup outside the owned fixture directory.")
        shutil.rmtree(resolved)
        with output.open("x", encoding="utf-8") as destination:
            json.dump({"passed": completed and all(row["passed"] for row in results),
                       "cases": results, "scope": "Actual CLI subprocesses in isolated own fixtures; no shared imported inputs or Unity touched."},
                      destination, indent=2)
    print(json.dumps({"passed": len(results), "failed": sum(not row["passed"] for row in results)}))


if __name__ == "__main__":
    main()
