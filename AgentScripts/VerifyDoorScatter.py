"""Verify the exact native scene delta against the preserved live authoring state."""
import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
BLOCK = re.compile(r"(?m)^--- !u!(\d+) &(-?\d+)(?: stripped)?$")
SCENE = ROOT / "Assets/EmergencyShift/Scenes/MorgueRouteTest.unity"


def blocks(path):
    source = path.read_text(encoding="utf-8-sig")
    matches = list(BLOCK.finditer(source))
    return {match[2]: (match[1], source[match.start():matches[index + 1].start() if index + 1 < len(matches) else len(source)])
            for index, match in enumerate(matches)}


def main():
    before = blocks(ROOT / "Temp/DoorScatterBaseline/LiveBefore.unity")
    after = blocks(SCENE)
    added = after.keys() - before.keys()
    removed = before.keys() - after.keys()
    expected_positions = {"226256974": (("5.4", "5.5"), ("0.35", "-0.7")),
                          "1006571244": (("18", "5.5"), ("-2.7", "0.7")),
                          "1234430364": (("27", "7"), ("2.7", "0.7")),
                          "1373916048": (("35", "7"), ("-2.4", "-0.7"))}
    placement = json.loads((ROOT / "Docs/Validation/DoorScatter01/placement.json").read_text(encoding="utf-8-sig"))
    profile = re.search(r"guid: (\w+)", (ROOT / "Assets/EmergencyShift/Data/Encounters/DoorScatter.asset.meta").read_text())[1]
    unexpected = []
    changed = []
    for identity, (kind, original) in before.items():
        if identity not in after or original == after[identity][1]:
            continue
        current = after[identity][1]
        changed.append(identity)
        expected = original
        if identity in expected_positions:
            for axis, (old, new) in zip(("x", "y"), expected_positions[identity]):
                expected = expected.replace(f"propertyPath: m_LocalPosition.{axis}\n      value: {old}\n",
                                            f"propertyPath: m_LocalPosition.{axis}\n      value: {new}\n")
            # New route components are allowed only on the three existing hall actors.
            if identity != "226256974":
                current = re.sub(r"    m_AddedComponents:\n    - targetCorrespondingSourceObject:.*?\n      insertIndex: -1\n      addedObject: \{fileID: (\d+)\}\n",
                                 lambda match: "    m_AddedComponents: []\n" if match[1] in added else match[0], current)
        elif identity == "226256977":
            expected = expected.replace("guid: 0cdd75466baf3184a9fd7b8366fd497c", "guid: " + profile)
            expected = expected.replace("  startCondition: 0\n  triggerDoor: {fileID: 0}", "  startCondition: 1\n  triggerDoor: {fileID: 48855980}")
        elif identity == "526376212":
            current = "\n".join(line for line in current.split("\n")
                                if not any(line == "  - {fileID: " + new + "}" for new in added))
        elif identity == "1149698797":
            expected = expected.replace("m_LocalPosition: {x: 14, y: 0.35, z: 0}", "m_LocalPosition: {x: 14, y: -0.75, z: 0}")
        if expected != current:
            unexpected.append({"fileId": identity, "classId": kind})
    report = {"scene": str(SCENE.relative_to(ROOT)), "originalBlocks": len(before), "addedBlocks": len(added),
              "removedBlocks": sorted(removed), "changedOriginalBlocks": changed, "unexpectedChanges": unexpected,
              "actors": placement["actorCount"], "routes": len(placement["routes"]),
              "preserved": not removed and not unexpected and placement["actorCount"] == 28 and len(placement["routes"]) == 4}
    (ROOT / "Docs/Validation/DoorScatter01/scene-preservation.json").write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(report))
    if not report["preserved"]:
        raise SystemExit(1)


if __name__ == "__main__":
    main()
