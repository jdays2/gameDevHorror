"""Stage only owned TASK-008 paths and two reviewed navigation doc edits.

Requires the explicit git/files reservations. Never stage another chat's existing
Enemies prose or any path outside the exact own allow-list/directories.
"""
import json
import re
from pathlib import Path
import subprocess

PROJECT = Path(__file__).resolve().parents[3]


def git(*args, data=None):
    return subprocess.run(["git", *args], cwd=PROJECT, input=data, capture_output=True, check=True).stdout


def main():
    state = json.loads((PROJECT.parent / ".coordination/EmergencyShift/state.json").read_text(encoding="utf-8-sig"))
    assert any(row["owner"] == "01a19e81-75f9-7743-9343-d660775cac53" and row["resource"] == "git" for row in state["leases"])
    assert not git("diff", "--cached", "--name-only").strip(), "Do not replace another staged change."
    evidence = PROJECT / "Docs/Validation/CodeReadabilityTask008"
    imported = json.loads((evidence / "allowed-phase2-inputs.json").read_text())["paths"]
    own = ["AgentScripts/Task008", "Tools/Maintenance/audit_code.py", "Docs/ARCHITECTURE.md",
           "Docs/Features/World.md", "Docs/FILE_MAP.md", "Docs/Reports/CodeAudit.json",
           "Docs/Reports/CodeAudit06.md", "Docs/Reports/CodeReadabilityTask008.md",
           "Docs/Validation/CodeReadabilityTask008"]
    git("add", "--", *imported, *own)
    path = "Docs/Features/Enemies.md"
    before = git("show", "HEAD:" + path).decode("utf-8")
    current = (PROJECT / path).read_text(encoding="utf-8-sig")
    for prefix in ("| Управление телом, обход, путь |", "Расчёт A* и личный кэш находятся в EnemyChasePathModel;"):
        old = [line for line in before.splitlines() if line.startswith(prefix)]
        new = [line for line in current.splitlines() if line.startswith(prefix)]
        assert len(old) == len(new) == 1 and old[0] != new[0]
        assert "EnemyChasePathFactory" in new[0]
        before = before.replace(old[0], new[0])
    blob = git("hash-object", "-w", "--stdin", data=before.encode("utf-8")).decode().strip()
    git("update-index", "--cacheinfo", "100644", blob, path)
    staged = git("diff", "--cached", "--name-only").decode().splitlines()
    exact = set(imported) | {path} | set(item for item in own if not (PROJECT / item).is_dir())
    directories = [item + "/" for item in own if (PROJECT / item).is_dir()]
    assert all(item in exact or any(item.startswith(prefix) for prefix in directories) for item in staged)
    check = subprocess.run(["git", "diff", "--cached", "--check"], cwd=PROJECT, capture_output=True)
    if check.returncode:
        issues = check.stdout.decode("utf-8").replace("\r\n", "\n")
        # Unity writes an empty native m_Name with a space after the colon.
        # Preserve the verified prefab bytes; permit only this exact serializer line.
        assert re.fullmatch(r"Assets/EmergencyShift/Resources/ProjectContext\.prefab:\d+: trailing whitespace\.\n\+  m_Name: \n", issues), issues
    print(json.dumps({"stagedOwnedPaths": len(staged), "foreignEnemiesProseLeftUnstaged": True,
                      "gameLeaseRequiredHere": False, "committed": False}))


if __name__ == "__main__":
    main()
