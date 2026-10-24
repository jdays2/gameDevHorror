"""Compare native snapshots and prepare a scene patch containing only delay fields."""
import difflib
import json
from pathlib import Path
import re
import subprocess

ROOT = Path(__file__).resolve().parents[1]
EVIDENCE = ROOT / "Docs/Validation/DoorScatterDelay01"
SCENE = "Assets/EmergencyShift/Scenes/MorgueRouteTest.unity"
DELAYS = {
    "morgue-intro-light-retreat": "0",
    "morgue-door-scatter-hall-01": "0.45",
    "morgue-door-scatter-hall-02": "0.2",
    "morgue-door-scatter-hall-03": "0.7",
}


def read(name):
    return json.loads((EVIDENCE / name).read_text(encoding="utf-8-sig"))


before, after = read("before.json"), read("after.json")
assert before["transforms"] == after["transforms"], "Authored world poses or hierarchy changed"
assert not after["play"] and not after["dirty"] and after["missing"] == 0
assert len(before["routes"]) == len(after["routes"]) == 4
for old, new in zip(before["routes"], after["routes"]):
    assert old["id"] == new["id"]
    assert old["path"] == new["path"] and old["prefab"] == new["prefab"]
    assert old["points"] == new["points"], "Marker IDs, references or positions changed"
    old_data = json.loads(old["serialized"])["MonoBehaviour"]
    new_data = json.loads(new["serialized"])["MonoBehaviour"]
    assert abs(new_data.pop("startDelaySeconds") - float(DELAYS[new["id"]])) < 1e-6
    old_data.pop("startDelaySeconds", None)
    assert old_data == new_data, "Another serialized route value changed"

head = subprocess.check_output(["git", "show", "HEAD:" + SCENE], cwd=ROOT).decode("utf-8")
working = (ROOT / SCENE).read_text(encoding="utf-8")
blocks = re.split(r"(?m)(?=^--- !u!)", head)
edited = 0
for index, block in enumerate(blocks):
    match = re.search(r"(?m)^  encounterId: (.+)$", block)
    if not match or match[1] not in DELAYS:
        continue
    identifier, value = match[1], DELAYS[match[1]]
    assert "startDelaySeconds:" not in block
    blocks[index], count = re.subn(
        r"(?m)^(  triggerDoor: .+\n)",
        lambda found: found[1] + "  startDelaySeconds: " + value + "\n",
        block,
    )
    assert count == 1
    actual = next(part for part in re.split(r"(?m)(?=^--- !u!)", working) if "  encounterId: " + identifier + "\n" in part)
    assert re.search(r"(?m)^  startDelaySeconds: " + re.escape(value) + r"$", actual)
    edited += 1
assert edited == 4
patch = "".join(difflib.unified_diff(head.splitlines(True), "".join(blocks).splitlines(True), "a/" + SCENE, "b/" + SCENE))
(EVIDENCE / "scene-delay-only.patch").write_text(patch, encoding="utf-8", newline="\n")
summary = {
    "transforms": len(after["transforms"]),
    "world_and_hierarchy_delta": 0,
    "routes": 4,
    "point_entries_preserved": sum(len(route["points"]) for route in after["routes"]),
    "prefabs_and_route_values_preserved": True,
    "missing_scripts": 0,
    "editor_stopped_clean": True,
    "scene_patch_insertions": 4,
    "unrelated_worktree_changes_excluded": True,
}
(EVIDENCE / "preservation.json").write_text(json.dumps(summary, indent=2) + "\n", encoding="utf-8")
print(json.dumps(summary))
