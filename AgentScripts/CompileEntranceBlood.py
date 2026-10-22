"""Combine explicit Editor authoring and its preservation guard; never edits Unity data."""
from pathlib import Path
import re

root = Path(__file__).resolve().parents[1]
imports = set()
bodies = []
for name in ("LevelAuthoringState", "AuthorEntranceBlood"):
    source = (root / "AgentScripts" / (name + ".cs")).read_text(encoding="utf-8-sig")
    imports.update(re.findall(r"^using [^\n]+", source, re.MULTILINE))
    bodies.append(re.sub(r"^using [^\n]+\n", "", source, flags=re.MULTILINE))
output = root / "work/entrance-traces-20261022/Authoring.cs"
output.parent.mkdir(parents=True, exist_ok=True)
output.write_text("\n".join(sorted(imports)) + "\n" + "\n".join(bodies), encoding="utf-8")
print(output)
