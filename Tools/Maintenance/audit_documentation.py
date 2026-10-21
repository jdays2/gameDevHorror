"""Read-only documentation/link inventory; optionally regenerate the current file map."""
from pathlib import Path
import argparse
import collections
import hashlib
import json
import re
from urllib.parse import unquote

ROOT = Path(__file__).resolve().parents[2]
LINK = re.compile(r"!?\[[^\]]*\]\(([^)]+)\)")


def inventory():
    documents = sorted(p for folder in ("Docs", "Concepts", ".agents/skills")
                       for p in (ROOT / folder).rglob("*.md"))
    documents += [ROOT / "AGENTS.md"]
    hashes = collections.defaultdict(list)
    broken = []
    for path in documents:
        content = path.read_text(encoding="utf-8-sig")
        hashes[hashlib.sha256(content.encode("utf-8")).hexdigest()].append(path.relative_to(ROOT).as_posix())
        visible = re.sub(r"```.*?```", "", content, flags=re.S)
        for match in LINK.finditer(visible):
            target = match.group(1).strip().split(' "', 1)[0].strip("<>")
            if re.match(r"^[a-zA-Z][a-zA-Z0-9+.-]*://|^mailto:", target) or target.startswith("#"):
                continue
            file = unquote(target.split("#", 1)[0])
            if file and not (path.parent / file).resolve().exists():
                broken.append({"source": path.relative_to(ROOT).as_posix(), "target": target})
    return {
        "documentCount": len(documents),
        "docsCount": sum(p.is_relative_to(ROOT / "Docs") for p in documents),
        "exactDuplicateGroups": [paths for paths in hashes.values() if len(paths) > 1],
        "brokenLocalLinks": broken,
        "limits": "File existence and exact content hashes only; external links/anchors, semantics and usage require review.",
    }


def file_map():
    lines = ["# Карта файлов", "", "Сформирована из текущих файлов проекта. Это навигация, а не доказательство использования или соответствия SOD. [Реестр проверок](Reports/CodeAudit.json) и [архитектура](ARCHITECTURE.md). Обновление: `Tools/Maintenance/audit_documentation.py --write-file-map`.", ""]
    sources = sorted(p for folder in ("Assets/EmergencyShift", "AgentScripts", "Concepts")
                     for p in (ROOT / folder).rglob("*.cs"))
    groups = collections.defaultdict(list)
    for path in sources:
        groups[path.parent.relative_to(ROOT).as_posix()].append(path)
    for folder, paths in sorted(groups.items()):
        lines += ["## " + folder, ""]
        lines += [f"- [{path.name}](../{path.relative_to(ROOT).as_posix()})" for path in paths]
        lines.append("")
    for label, pattern in (("Сборки", "*.asmdef"), ("Сохранённые сцены", "*.unity")):
        lines += ["## " + label, ""]
        for path in sorted((ROOT / "Assets/EmergencyShift").rglob(pattern)):
            lines.append(f"- [{path.name}](../{path.relative_to(ROOT).as_posix()})")
        lines.append("")
    return "\n".join(lines)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--write-file-map", action="store_true")
    parser.add_argument("--report", type=Path)
    args = parser.parse_args()
    if args.write_file_map:
        (ROOT / "Docs/FILE_MAP.md").write_bytes(file_map().encode("utf-8"))
    result = inventory()
    data = json.dumps(result, ensure_ascii=False, indent=2) + "\n"
    if args.report:
        args.report.write_bytes(data.encode("utf-8"))
    print(data)
    return 1 if result["brokenLocalLinks"] else 0


if __name__ == "__main__":
    raise SystemExit(main())
