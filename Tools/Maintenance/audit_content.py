"""Read-only inventory of serialized GUID references and local Markdown links."""
from pathlib import Path
import collections
import json
import re

ROOT = Path(__file__).resolve().parents[2]


def inventory():
    metas = {}
    for path in (ROOT / "Assets").rglob("*.meta"):
        match = re.search(r"^guid: ([0-9a-f]{32})", path.read_text(errors="replace"), re.M)
        if match:
            metas[match[1]] = path.relative_to(ROOT).as_posix()[:-5]
    references = collections.defaultdict(set)
    missing = collections.defaultdict(set)
    for base in ["Assets", "ProjectSettings", "Packages"]:
        for path in (ROOT / base).rglob("*"):
            if not path.is_file() or path.suffix.lower() not in {
                ".asset", ".unity", ".prefab", ".mat", ".meta", ".controller",
                ".overridecontroller", ".playable", ".json", ".cs", ".shader", ".uxml", ".uss"
            }:
                continue
            source = path.relative_to(ROOT).as_posix()
            content = path.read_text(errors="replace")
            for guid in re.findall(r"\bguid:\s*([0-9a-f]{32})\b", content):
                if guid in metas:
                    target = metas[guid]
                    if source != target + ".meta":
                        references[target].add(source)
                elif guid[:16] != "0" * 16:
                    missing[guid].add(source)
    broken_links = []
    for base in ["Docs", "Concepts"]:
        for path in (ROOT / base).rglob("*.md"):
            for link in re.findall(r"!?\[[^\]]*\]\(([^)]+)\)", path.read_text(encoding="utf-8-sig")):
                target = link.split("#")[0].strip("<>")
                if target and not re.match(r"^[a-zA-Z]+:", target) and not (path.parent / target).exists():
                    broken_links.append([path.relative_to(ROOT).as_posix(), link])
    return {
        "meta": metas,
        "references": {key: sorted(value) for key, value in references.items()},
        "unreferenced": sorted(path for path in metas.values()
                               if path.startswith("Assets/EmergencyShift/")
                               and (ROOT / path).is_file() and path not in references
                               and Path(path).suffix.lower() not in {".cs", ".asmdef", ".json", ".unity"}),
        "missing_guids": {key: sorted(value) for key, value in missing.items()},
        "broken_links": broken_links,
    }


if __name__ == "__main__":
    import argparse
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output", default="Temp/content-audit.json")
    args = parser.parse_args()
    report = inventory()
    destination = ROOT / args.output
    destination.parent.mkdir(parents=True, exist_ok=True)
    destination.write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")
    print(json.dumps({"unreferenced_candidates": report["unreferenced"],
                      "external_or_missing_guid_count": len(report["missing_guids"]),
                      "broken_links": report["broken_links"]}, ensure_ascii=False, indent=2))
