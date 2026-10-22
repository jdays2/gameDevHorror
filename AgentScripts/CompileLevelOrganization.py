"""Prepare the explicit, one-shot level migration for Unity Pipeline run_script.

This only combines authoring source; it never opens Unity or mutates assets/scenes.
Original source IDs in the migration map belong to the 2026-10-22 before snapshot.
Do not replay Apply on already organized scenes or a different authored map.
"""
from pathlib import Path
import re

root = Path(__file__).resolve().parents[1]
names = [
    "LevelReuseLibrary", "LevelAuthoringState", "OrganizeLevelScenes",
    "FinishLevelOrganization", "ExportLevelOrganization",
]
imports = set()
bodies = []
for name in names:
    source = (root / "AgentScripts" / (name + ".cs")).read_text(encoding="utf-8")
    imports.update(re.findall(r"^using [^\n]+", source, re.MULTILINE))
    bodies.append(re.sub(r"^using [^\n]+\n", "", source, flags=re.MULTILINE))
output = root / "work/level-reuse-20261022/Authoring.cs"
output.parent.mkdir(parents=True, exist_ok=True)
output.write_text("\n".join(sorted(imports)) + "\n" + "\n".join(bodies), encoding="utf-8")
print(output)
