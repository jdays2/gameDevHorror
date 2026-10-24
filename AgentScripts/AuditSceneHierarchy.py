"""Read-only saved-scene inventory; native Editor audit remains authoritative.

Capture before/after outside Assets. Never writes a Unity asset or interprets a
coordinate as ownership. Prefab instances are listed as intact authored units.
"""
import hashlib
import json
import re
import shutil
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "work/scene-grouping-task002"
BLOCK = re.compile(r"(?m)^--- !u!(\d+) &(-?\d+)(?: stripped)?\r?$")


def field(text, name, default=""):
    found = re.search(r"(?m)^  " + re.escape(name) + r": (.*)$", text)
    return found[1].strip() if found else default


def ref(text, name):
    found = re.search(r"(?m)^  " + re.escape(name) + r": \{fileID: (-?\d+)", text)
    return found[1] if found else "0"


def blocks(path):
    text = path.read_text(encoding="utf-8-sig")
    matches = list(BLOCK.finditer(text))
    return {m[2]: (m[1], text[m.end():matches[i + 1].start() if i + 1 < len(matches) else len(text)])
            for i, m in enumerate(matches)}


def inventory(path, guids):
    data = blocks(path)
    names = {key: field(body, "m_Name") for key, (kind, body) in data.items() if kind == "1"}
    nodes = {}
    for key, (kind, body) in data.items():
        if kind not in {"4", "224"} or "m_GameObject:" not in body:
            continue
        if ref(body, "m_PrefabInstance") != "0":
            continue
        nodes[key] = {"id": key, "name": names.get(ref(body, "m_GameObject"), "?"),
                      "parent": ref(body, "m_Father"), "position": field(body, "m_LocalPosition"), "prefab": ""}
    for key, (kind, body) in data.items():
        if kind != "1001":
            continue
        guid = re.search(r"m_SourcePrefab: \{fileID: \d+, guid: ([a-f0-9]+)", body)
        prefab = guids.get(guid[1], "MISSING") if guid else "MISSING"
        overrides = re.findall(r"propertyPath: m_Name\s+value: (.*)", body)
        name = overrides[0].strip() if overrides else Path(prefab).stem
        stripped = [identity for identity, (t, b) in data.items()
                    if t in {"4", "224"} and ref(b, "m_PrefabInstance") == key]
        identity = stripped[0] if stripped else "prefab:" + key
        parent = re.search(r"m_TransformParent: \{fileID: (-?\d+)", body)
        nodes[identity] = {"id": identity, "name": name, "parent": parent[1] if parent else "0",
                           "prefab": prefab, "instanceId": key}
    def full(node, seen=None):
        seen = set() if seen is None else seen
        if node["id"] in seen:
            return "CYCLE/" + node["name"]
        parent = nodes.get(node["parent"])
        return (full(parent, seen | {node["id"]}) + "/" if parent else "") + node["name"]
    for node in nodes.values():
        node["path"] = full(node)
    return {"scene": path.relative_to(ROOT).as_posix(), "sha256": hashlib.sha256(path.read_bytes()).hexdigest(),
            "serializedBlocks": len(data), "authoredUnits": len(nodes),
            "roots": [n["name"] for n in nodes.values() if n["parent"] == "0"],
            "nodes": sorted(nodes.values(), key=lambda n: n["path"])}


def main():
    phase = sys.argv[1] if len(sys.argv) > 1 else "before"
    destination = OUT / phase
    destination.mkdir(parents=True, exist_ok=False)
    guids = {}
    for meta in (ROOT / "Assets").rglob("*.meta"):
        m = re.search(r"(?m)^guid: ([a-f0-9]+)$", meta.read_text(encoding="utf-8-sig"))
        if m:
            guids[m[1]] = meta.with_suffix("").relative_to(ROOT).as_posix()
    scenes = sorted((ROOT / "Assets").rglob("*.unity"))
    rows = [inventory(p, guids) for p in scenes]
    for path in scenes:
        target = destination / path.relative_to(ROOT)
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(path, target)
    status = subprocess.check_output(["git", "status", "--porcelain=v1", "-uall"], cwd=ROOT, text=True)
    (destination / "git-status.txt").write_text(status, encoding="utf-8")
    (destination / "git-diff.patch").write_bytes(subprocess.check_output(["git", "diff", "--binary"], cwd=ROOT))
    dirty = {}
    for line in status.splitlines():
        path = ROOT / line[3:]
        if path.is_file():
            dirty[line[3:]] = hashlib.sha256(path.read_bytes()).hexdigest()
    (destination / "dirty-hashes.json").write_text(json.dumps(dirty, indent=2), encoding="utf-8")
    (destination / "saved-inventory.json").write_text(json.dumps(rows, ensure_ascii=False, indent=2), encoding="utf-8")
    for row in rows:
        print(row["scene"], row["serializedBlocks"], row["authoredUnits"], ", ".join(row["roots"]))


if __name__ == "__main__":
    main()
