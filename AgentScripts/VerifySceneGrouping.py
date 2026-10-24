"""Verify TASK-002 native snapshots and saved scene serialization without edits."""
import collections
import hashlib
import json
import re
from pathlib import Path
from AuditSceneHierarchy import blocks

ROOT = Path(__file__).resolve().parents[1]
WORK = ROOT / "work/scene-grouping-task002"
OUT = ROOT / "Docs/Validation/SceneGroupingTask002"
TOLERANCE = 0.00005


def load(path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def native(scene):
    before = load(WORK / "native-before" / (scene + ".json"))
    after = load(WORK / "native-after" / (scene + ".json"))
    old = {u["id"]: u for u in before["units"]}
    new = {u["id"]: u for u in after["units"]}
    allowed_names = {m["id"] for p in load(OUT/"authoring-plan.json") for m in p["moves"]}
    allowed_names.update(m["id"] for m in load(WORK/"morgue-changes.json")["changes"])
    errors = []
    maximum = 0
    for identity, unit in old.items():
        if identity not in new:
            errors.append("Missing original unit: " + unit["path"])
            continue
        actual = new[identity]
        delta = max(abs(a-b) for a, b in zip(unit["matrix"], actual["matrix"]))
        maximum = max(maximum, delta)
        for field in ["active", "prefab", "prefabStatus", "components"]:
            if unit[field] != actual[field]:
                errors.append("Changed " + field + ": " + unit["path"])
        if delta > TOLERANCE:
            errors.append("World matrix changed: " + unit["path"])

    changed = collections.Counter()
    numeric_max = 0
    for key, value in before["fields"].items():
        actual = after["fields"].get(key)
        if value == actual:
            continue
        owner, prop = key.split("|", 1)
        # These are Unity's transient in-process object handles. The containing
        # ObjectReference property is independently compared by GlobalObjectId.
        if prop.endswith((".m_FileID", ".m_PathID")):
            continue
        if prop == "m_Name" or prop.startswith("m_Name.Array"):
            if owner not in allowed_names:
                errors.append("Unplanned name change: " + key)
            changed["name"] += 1
            continue
        if prop == "m_Father" or prop.startswith("m_Children"):
            changed["hierarchy"] += 1
            continue
        if prop.startswith(("m_LocalPosition", "m_LocalScale", "m_LocalRotation", "m_LocalEulerAnglesHint")):
            try:
                delta = abs(float(value)-float(actual))
                if delta <= TOLERANCE:
                    numeric_max = max(numeric_max, delta)
                    changed["localTransformRounding"] += 1
                    continue
            except (TypeError, ValueError):
                pass
        errors.append("Unexpected serialized field: " + key + " = " + str(value) + " -> " + str(actual))
    if before["dependencies"] != after["dependencies"]:
        errors.append("Asset dependencies changed")
    errors.extend(after["issues"])
    ids = [v for k, v in after["fields"].items() if k.endswith("|persistentId")]
    if len(ids) != len(set(ids)) or any(not v for v in ids):
        errors.append("Empty/duplicate persistent ID")
    duplicate_units = collections.Counter((u["parent"], u["name"]) for u in after["units"])
    return before, after, dict(scene=before["scene"], beforeObjects=before["objects"], afterObjects=after["objects"],
        beforeUnits=len(old), afterUnits=len(new), preservedOriginalUnits=len(old.keys() & new.keys()),
        maximumWorldMatrixDelta=maximum, maximumLocalScalarRounding=numeric_max,
        changedNativeFields=dict(changed), originalFields=len(before["fields"]),
        missingScriptsAndReferences=len(after["issues"]), persistentIds=len(ids),
        looseSpriteRenderers=sum(1 for u in after["units"] if not u["prefab"] and "UnityEngine.SpriteRenderer" in u["components"]),
        duplicateAuthoredSiblingNames=sum(1 for count in duplicate_units.values() if count > 1),
        errors=errors)


def saved(path):
    old = blocks(WORK / "before" / path)
    new = blocks(ROOT / path)
    errors = []
    changed = []
    for identity, (kind, body) in old.items():
        if identity not in new:
            errors.append("Original block removed: " + identity)
            continue
        actual = new[identity][1]
        if body == actual:
            continue
        changed.append(dict(fileId=identity, classId=kind))
        if kind == "1001":
            def normalize(text):
                text = re.sub(r"m_TransformParent: \{fileID: -?\d+\}", "m_TransformParent: PARENT", text)
                # The only new prefab overrides allowed are instance names.
                text = re.sub(r"    - target: .*\n      propertyPath: m_Name\n      value: .*\n      objectReference: .*\n", "", text)
                # Unity decomposes the same world matrix on SetParent(true).
                text = re.sub(r"(propertyPath: m_Local(?:Position|Scale|Rotation|EulerAnglesHint)\.[xyz w]+\n      value:) [^\n]+", r"\1 TRANSFORM", text)
                return text
            if normalize(body) != normalize(actual):
                errors.append("Prefab overrides changed beyond name/local transform: " + identity)
        elif kind not in {"1", "4", "224", "1660057539"}:
            errors.append("Non-hierarchy component block changed: " + identity + "/" + kind)
    added = new.keys()-old.keys()
    if any(new[k][0] not in {"1", "4"} for k in added):
        errors.append("Unexpected added component type")
    return dict(originalBlocks=len(old), addedBlocks=len(added), removedBlocks=len(old.keys()-new.keys()),
                changedBlocks=changed, errors=errors,
                beforeSHA256=hashlib.sha256((WORK/"before"/path).read_bytes()).hexdigest(),
                afterSHA256=hashlib.sha256((ROOT/path).read_bytes()).hexdigest())


def main():
    rows = []
    for file in sorted((WORK / "native-before").glob("*.json")):
        if file.stem in {"summary", "0", "SampleScene"} or file.stem.startswith("live-"):
            continue
        before, after, result = native(file.stem)
        result["savedSerialization"] = saved(Path(result["scene"]))
        result["errors"].extend(result["savedSerialization"]["errors"])
        rows.append(result)
        print(file.stem, before["objects"], "->", after["objects"], "world delta", result["maximumWorldMatrixDelta"], "errors", len(result["errors"]))
        for error in result["errors"][:8]:
            print(" ", error)
    report = dict(task="TASK-002", bug="BUG-050", date="2026-10-24", tolerance=TOLERANCE,
                  productionScenes=len(rows), scenes=rows, passed=not any(r["errors"] for r in rows),
                  originalSetup=load(WORK/"native-before/summary.json")["setup"],
                  restoredSetup=load(WORK/"restored-setup.json"))
    (OUT/"preservation.json").write_text(json.dumps(report, ensure_ascii=False, indent=2)+"\n", encoding="utf-8")
    raise SystemExit(0 if report["passed"] else 1)


if __name__ == "__main__":
    main()
