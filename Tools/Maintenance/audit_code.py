"""Refresh/check code inventory metadata; never infer a semantic review or unused code."""
from pathlib import Path
import argparse
import collections
import hashlib
import json
import re

ROOT = Path(__file__).resolve().parents[2]
REGISTRY = ROOT / "Docs/Reports/CodeAudit.json"
GUID = re.compile(r"\bguid:\s*([0-9a-fA-F]{32})\b")
TYPES = re.compile(r"\b(?:class|struct|interface|enum)\s+(\w+)")
SERIALIZED = {".unity", ".prefab", ".asset", ".mat", ".controller", ".overridecontroller"}


def inventory():
    paths = sorted(p for folder in ("Assets/EmergencyShift", "AgentScripts", "Concepts")
                   for p in (ROOT / folder).rglob("*.cs"))
    sources = {p.relative_to(ROOT).as_posix(): p.read_text(encoding="utf-8-sig") for p in paths}
    guid_paths = {}
    for path in (ROOT / "Assets").rglob("*.meta"):
        match = GUID.search(path.read_text(encoding="utf-8-sig", errors="replace"))
        if match:
            guid_paths[match[1].lower()] = path.relative_to(ROOT).as_posix()[:-5]
    users = collections.defaultdict(set)
    edges = {}
    for path in (ROOT / "Assets").rglob("*"):
        if path.suffix.lower() not in SERIALIZED or not path.is_file():
            continue
        relative = path.relative_to(ROOT).as_posix()
        targets = {guid_paths[g.lower()] for g in GUID.findall(
            path.read_text(encoding="utf-8-sig", errors="replace")) if g.lower() in guid_paths}
        edges[relative] = targets
        for target in targets:
            users[target].add(relative)
    scenes = {}
    for scene in sorted(p for p in edges if p.endswith(".unity")):
        pending, visited = [scene], set()
        while pending:
            target = pending.pop()
            if target not in visited:
                visited.add(target)
                pending.extend(edges.get(target, ()))
        scenes[scene] = visited
    path_guids = {path: guid for guid, path in guid_paths.items()}
    result = {}
    for path, content in sources.items():
        types = sorted(set(TYPES.findall(content)))
        pattern = re.compile(r"\b(?:" + "|".join(map(re.escape, types)) + r")\b") if types else None
        result[path] = {
            "path": path,
            "sourceScope": "unity" if path.startswith("Assets/") else "agent_tool" if path.startswith("AgentScripts/") else "content_source",
            "sha256": hashlib.sha256((ROOT / path).read_bytes()).hexdigest(),
            "guid": path_guids.get(path),
            "types": types,
            "lines": len(content.splitlines()),
            "serializedUsers": sorted(users[path]),
            "dependencyScenes": [scene for scene, targets in scenes.items() if path in targets],
            "lexicalSourceUsers": [other for other, text in sources.items()
                                   if other != path and pattern and pattern.search(text)],
            "entryPointHints": [hint for hint in ("MenuItem", "InitializeOnLoad", "RuntimeInitializeOnLoadMethod", "Test", "TestCase")
                                if re.search(r"\[" + hint + r"\b", content)],
        }
    return result


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--refresh", action="store_true", help="Update metadata, queue new files, invalidate changed reviews")
    parser.add_argument("--date", help="Explicit review snapshot date, required for --refresh")
    args = parser.parse_args()
    if args.refresh and not args.date:
        parser.error("--refresh requires --date YYYY-MM-DD")
    registry = json.loads(REGISTRY.read_text(encoding="utf-8-sig"))
    old = {entry["path"]: entry for entry in registry["files"]}
    current = inventory()
    added = sorted(current.keys() - old.keys())
    removed = sorted(old.keys() - current.keys())
    changed = [p for p in current if p in old and current[p]["sha256"] != old[p]["sha256"]]
    if args.refresh:
        for path, metadata in current.items():
            entry = old.get(path, {"baselineSha256": None, "reviewStatus": "queued",
                                   "notes": "New inventory entry; no semantic review yet."})
            if entry.get("reviewStatus") != "queued":
                entry.setdefault("reviewedSha256", entry["sha256"])
            if path in changed and entry["reviewStatus"] != "queued":
                entry["previousReviewStatus"] = entry["reviewStatus"]
                entry["reviewStatus"] = "partial"
            entry.update(metadata)
            entry["sourceChangedSinceReview"] = bool(entry.get("reviewedSha256") and entry["reviewedSha256"] != metadata["sha256"])
            current[path] = entry
        registry["files"] = list(current.values())
        if removed:
            registry.setdefault("removedEntries", []).extend(old[p] for p in removed)
        registry["date"] = args.date
        registry["complete"] = False
        registry["counts"] = dict(collections.Counter(entry["reviewStatus"] for entry in current.values()))
        registry["scopeCounts"] = dict(collections.Counter(entry["sourceScope"] for entry in current.values()))
        registry["method"] = "Manual semantic boundary reviews keyed to reviewedSha256; mechanical inventory of own C# in Assets/EmergencyShift, AgentScripts and Concepts. No automatic semantic/unused-code verdict."
        registry["referenceMethod"] = "Saved YAML external GUID graph (Assets), transitive scene-to-prefab/asset traversal; not AssetDatabase or local fileID validation. Lexical type tokens include comments/strings and are hints only."
        REGISTRY.write_text(json.dumps(registry, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    stale = [p for p, entry in current.items() if p in old and old[p].get("reviewedSha256")
             and old[p]["reviewedSha256"] != entry["sha256"]]
    print(json.dumps({"files": len(current), "added": added, "removed": removed, "changed": changed,
                      "staleReviews": stale, "refreshed": args.refresh}, ensure_ascii=False, indent=2))
    return 0 if args.refresh or not (added or removed or changed or stale) else 1


if __name__ == "__main__":
    raise SystemExit(main())
