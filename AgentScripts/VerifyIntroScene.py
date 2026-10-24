"""Check that native intro authoring preserved every pre-existing serialized scene object."""
import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
BASE = ROOT / "Temp/IntroRouteBaseline/MorgueRouteTest.unity"
SCENE = ROOT / "Assets/EmergencyShift/Scenes/MorgueRouteTest.unity"
BLOCK = re.compile(r"(?m)^--- !u!(\d+) &(-?\d+)(?: stripped)?\r?$")


def blocks(path):
    text = path.read_text(encoding="utf-8-sig").replace("\r\n", "\n")
    matches = list(BLOCK.finditer(text))
    return {match[2]: (match[1], text[match.start():matches[index + 1].start() if index + 1 < len(matches) else len(text)])
            for index, match in enumerate(matches)}


def main():
    before = blocks(BASE)
    after = blocks(SCENE)
    added = set(after) - set(before)
    removed = set(before) - set(after)
    changed = []
    unexpected = []
    for identity, (kind, original) in before.items():
        if identity not in after:
            continue
        current = after[identity][1]
        if original == current:
            continue
        changed.append({"fileId": identity, "classId": kind})
        # Adding a child/root reference is the only permitted edit to an old block.
        cleaned = "\n".join(line for line in current.split("\n")
                            if not any(re.fullmatch(r"  - \{fileID: " + re.escape(new_id) + r"\}", line)
                                       for new_id in added))
        if kind not in {"4", "1660057539"} or cleaned != original:
            unexpected.append(identity)
    report = {"scene": str(SCENE.relative_to(ROOT)), "originalBlocks": len(before),
              "addedBlocks": len(added), "removedBlocks": sorted(removed),
              "oldBlocksWithAddedChildReferences": changed, "unexpectedChanges": unexpected,
              "preserved": not removed and not unexpected}
    destination = ROOT / "Docs/Validation/IntroEncounter01/scene-preservation.json"
    destination.write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(report, ensure_ascii=False))
    if not report["preserved"]:
        raise SystemExit(1)


if __name__ == "__main__":
    main()
