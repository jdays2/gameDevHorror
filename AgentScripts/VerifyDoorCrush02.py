"""Preserve pre-existing scene edits and construct a patch containing only native opt-ins."""
from pathlib import Path
import argparse
import difflib
import hashlib
import json
import re
import subprocess

ROOT = Path(__file__).resolve().parents[1]
WORK = ROOT / "work/door-crush02"
LEVEL = "Assets/EmergencyShift/Scenes/MorgueRouteTest.unity"
GUID = "e65f1aa56ad78f14a873f0dfe7b28cc1"


def git(*args):
    return subprocess.check_output(["git", *args], cwd=ROOT)


def protected_files():
    names = {name for name in git("ls-files", "-z").decode().split("\0")
             if name.startswith(("Assets/", "ProjectSettings/", "Packages/"))}
    names.update(path.relative_to(ROOT).as_posix() for path in (ROOT / "Assets").rglob("*") if path.is_file())
    return sorted(names)


def hashes():
    return {name: hashlib.sha256((ROOT / name).read_bytes()).hexdigest()
            for name in protected_files() if (ROOT / name).is_file()}


def snapshot():
    WORK.mkdir(parents=True, exist_ok=True)
    (WORK / "protected-before.json").write_text(json.dumps(hashes(), indent=2), encoding="utf-8")
    (WORK / "git-before.txt").write_bytes(git("status", "--porcelain=v1", "-uall"))
    (WORK / "head-before.txt").write_bytes(git("rev-parse", "HEAD"))


def blocks(text):
    result = {}
    for block in re.split(r"(?=^--- !u!)", text, flags=re.M):
        header = re.match(r"--- !u!(\d+) &(-?\d+)", block)
        if header:
            result[header[2]] = (int(header[1]), block)
    return result


def without_added_components(block):
    return re.sub(r"^    m_AddedComponents:.*?(?=^  [^ ]|\Z)", "", block, flags=re.M | re.S)


def check_scene():
    before = (WORK / "MorgueRouteTest.before.unity").read_text(encoding="utf-8-sig")
    after = (ROOT / LEVEL).read_text(encoding="utf-8-sig")
    old = blocks(before)
    new = blocks(after)
    authoring = json.loads((ROOT / "Docs/Validation/DoorCrush02/first-level-authoring.json").read_text(encoding="utf-8-sig"))
    expected_added = authoring["added"]
    removed = sorted(set(old) - set(new))
    added = sorted(set(new) - set(old))
    changed = sorted(key for key in old.keys() & new.keys() if old[key][1] != new[key][1])
    invalid = []
    component_ids = []
    stripped_ids = []
    rewired_inherited_doors = []
    changed_prefabs = []
    for key in added:
        kind, block = new[key]
        if kind == 114 and "guid: " + GUID in block and "m_Enabled: 1" in block:
            component_ids.append(key)
        elif kind == 1 and re.match(r"--- !u!1 &\d+ stripped", block):
            stripped_ids.append(key)
        else:
            invalid.append(key)
    for key in changed:
        if old[key][0] == 1001 and without_added_components(old[key][1]) == without_added_components(new[key][1]):
            changed_prefabs.append(key)
            continue
        # Unity materializes a stripped root reference when a first instance component is added.
        root = re.search(r"^  m_GameObject: \{fileID: (-?\d+)\}", new[key][1], re.M)
        door_guid = re.search(r"^guid: (\w+)", (ROOT / "Assets/EmergencyShift/Runtime/World/Doors/Door2D.cs.meta").read_text(), re.M)[1]
        if (old[key][0] == 114 and " stripped\n" in old[key][1].split("\n", 1)[0] + "\n"
                and "guid: " + door_guid in old[key][1] and "m_GameObject: {fileID: 0}" in old[key][1]
                and root and root[1] in stripped_ids
                and old[key][1].replace("m_GameObject: {fileID: 0}", "m_GameObject: {fileID: " + root[1] + "}") == new[key][1]):
            rewired_inherited_doors.append(key)
            continue
        invalid.append(key)
    if removed or invalid or len(component_ids) != expected_added or len(changed_prefabs) != expected_added:
        raise RuntimeError(json.dumps({"removed": removed, "unexpected": invalid,
                                       "newCrushComponents": component_ids, "changed": changed}))
    previous_hashes = json.loads((WORK / "protected-before.json").read_text(encoding="utf-8"))
    actual = hashes()
    missing = sorted(set(previous_hashes) - set(actual))
    added_files = sorted(set(actual) - set(previous_hashes))
    other = sorted(name for name in previous_hashes.keys() & actual.keys()
                   if previous_hashes[name] != actual[name] and name != LEVEL)
    if missing or other or added_files:
        raise RuntimeError(json.dumps({"missingProtected": missing, "otherChanges": other, "addedFiles": added_files}))
    report = {"baseline": (WORK / "head-before.txt").read_text().strip(),
              "method": "Native scene YAML blocks; only added-component lists and Unity materialized inherited root references may change",
              "protectedFiles": len(previous_hashes), "unchangedProtectedFiles": len(previous_hashes) - 1,
              "removedBlocks": removed, "changedPrefabInstances": len(changed_prefabs),
              "materializedInheritedDoorReferences": len(rewired_inherited_doors),
              "newDoorCrushComponents": len(component_ids), "newStrippedGameObjectReferences": len(stripped_ids),
              "otherProtectedChanges": other, "missingProtectedFiles": missing, "newProtectedFiles": added_files}
    destination = ROOT / "Docs/Validation/DoorCrush02/preservation.json"
    destination.parent.mkdir(parents=True, exist_ok=True)
    destination.write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    patch = "".join(difflib.unified_diff(before.splitlines(keepends=True), after.splitlines(keepends=True),
                                        fromfile="a/" + LEVEL, tofile="b/" + LEVEL))
    (WORK / "first-level-only.patch").write_text(patch, encoding="utf-8", newline="\n")
    print(json.dumps(report))


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("operation", choices=("snapshot", "verify"))
    operation = parser.parse_args().operation
    if operation == "snapshot":
        snapshot()
    else:
        check_scene()
