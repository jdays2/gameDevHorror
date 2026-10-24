"""Verify saved native evidence; optionally prepare a patch from an explicit old ref."""
import argparse
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


def read_evidence(name):
    return json.loads((EVIDENCE / name).read_text(encoding="utf-8-sig"))


def route_blocks(text):
    result = {}
    for block in re.split(r"(?m)(?=^--- !u!)", text):
        match = re.search(r"(?m)^  encounterId: (.+)$", block)
        if match and match[1] in DELAYS:
            if match[1] in result:
                raise ValueError("Duplicate encounter ID: " + match[1])
            result[match[1]] = block
    if set(result) != set(DELAYS):
        raise ValueError("Expected all four authored route components")
    return result


def verify_snapshots():
    before = read_evidence("before.json")
    after = read_evidence("after.json")
    assert before["transforms"] == after["transforms"], "World poses or hierarchy changed"
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

    current = route_blocks((ROOT / SCENE).read_text(encoding="utf-8"))
    for identifier, value in DELAYS.items():
        if not re.search(r"(?m)^  startDelaySeconds: " + re.escape(value) + r"$", current[identifier]):
            raise ValueError("Unexpected current delay: " + identifier)
    return {
        "transforms": len(after["transforms"]),
        "world_and_hierarchy_delta": 0,
        "routes": 4,
        "point_entries_preserved": sum(len(route["points"]) for route in after["routes"]),
        "prefabs_and_route_values_preserved": True,
        "missing_scripts": 0,
        "saved_editor_snapshot_stopped_clean": True,
        "current_delay_fields_verified": 4,
    }


def prepare_patch(base_ref, output):
    target = (ROOT / output).resolve()
    if not target.is_relative_to(ROOT):
        raise ValueError("Patch output must stay inside the project")
    if target.exists():
        raise FileExistsError("Refusing to overwrite patch output: " + str(target))

    head = subprocess.check_output(["git", "show", base_ref + ":" + SCENE], cwd=ROOT).decode("utf-8")
    routes = route_blocks(head)
    updated = head
    for identifier, block in routes.items():
        if "startDelaySeconds:" in block:
            raise ValueError("Patch base already contains delay fields; choose the pre-change ref")
        replacement, count = re.subn(
            r"(?m)^(  triggerDoor: .+\n)",
            lambda found: found[1] + "  startDelaySeconds: " + DELAYS[identifier] + "\n",
            block,
        )
        if count != 1:
            raise ValueError("Expected one door reference: " + identifier)
        updated = updated.replace(block, replacement, 1)
    patch = "".join(difflib.unified_diff(
        head.splitlines(True), updated.splitlines(True), "a/" + SCENE, "b/" + SCENE,
    ))
    target.parent.mkdir(parents=True, exist_ok=True)
    with target.open("x", encoding="utf-8", newline="\n") as stream:
        stream.write(patch)
    return str(target.relative_to(ROOT))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--patch-base", help="Explicit pre-delay Git ref; omitted for read-only verification")
    parser.add_argument("--patch-output", help="New project-relative patch file; existing files are never overwritten")
    args = parser.parse_args()
    if bool(args.patch_base) != bool(args.patch_output):
        parser.error("--patch-base and --patch-output must be provided together")
    summary = verify_snapshots()
    if args.patch_base:
        summary["patch"] = prepare_patch(args.patch_base, args.patch_output)
    print(json.dumps(summary))


if __name__ == "__main__":
    main()
