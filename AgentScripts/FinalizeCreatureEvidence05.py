"""Read-only final production-PNG and active-document link checks, independent of pruned caches."""
import hashlib
import json
import re
import subprocess
from pathlib import Path
from urllib.parse import unquote

PROJECT = Path(__file__).resolve().parents[1]
DOCS = [
    "Docs/Reports/StalkerWholeSprites05.md", "Docs/Enemies/STALKER.md", "Docs/Enemies/SLEEPER.md",
    "Concepts/Used/Stalker/Postures05/README.md", "Concepts/Used/Stalker/Postures05/AttackAdjacent01/README.md",
    "Concepts/Used/Sleeper/Postures05/README.md", "Concepts/Used/Sleeper/Postures05/AttackAdjacent01/README.md",
    "Concepts/Used/Sleeper/Postures05/native-material-final-review.md",
    "Concepts/Used/Stalker/Postures05/Scenarios/README.md",
    "Concepts/Unused/Visual/StalkerWholeFrameReview01/StaticEvidence/README.md",
    "Concepts/Unused/Visual/StalkerWholeFrameReview01/README.md",
]


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    atlases = []
    for species in ("Stalker", "Sleeper"):
        family = PROJECT / f"Concepts/Used/{species}/Postures05"
        for manifest_name in ("sprites.json", "attack-sprites.json", "AttackAdjacent01/attack-sprites.json"):
            manifest_path = family / manifest_name
            manifest = json.loads(manifest_path.read_text(encoding="utf-8-sig"))
            for atlas in manifest["atlases"]:
                actual = sha(PROJECT / atlas["asset"])
                assert actual == atlas["sha256"], atlas["asset"]
                atlases.append({"asset": atlas["asset"], "sha256": actual,
                                "manifest": manifest_path.relative_to(PROJECT).as_posix(),
                                "manifestSha256": sha(manifest_path)})
    assert len(atlases) == 30
    frozen_roots = ("Concepts/Used/Stalker/Postures05/", "Concepts/Used/Sleeper/Postures05/",
                    "Concepts/Unused/Visual/StalkerWholeFrameReview01/")
    frozen_code = {
        "AgentScripts/CaptureCreatureScenarios05.cs",
        "Assets/EmergencyShift/Editor/Authoring/StalkerSprites/StalkerPoseComposer.cs",
        "Assets/EmergencyShift/Editor/Authoring/StalkerSprites/StalkerAttackPoseComposer.cs",
        "Assets/EmergencyShift/Runtime/Presentation/Visuals/CreatureSpriteAnimator2D.cs",
        "Assets/EmergencyShift/Runtime/Presentation/Visuals/CreatureAttackSpritePlayback.cs",
        "Assets/EmergencyShift/Runtime/Features/Enemies/Config/CreatureAttackSpriteSet.cs",
        "Assets/EmergencyShift/Runtime/Enemies/Stalker/StalkerSpriteAnimator.cs",
        "Assets/EmergencyShift/Runtime/Enemies/Sleeper/SleeperSpriteAnimator.cs",
    }
    frozen_count = 0
    index_mismatches = []
    for row in subprocess.check_output(["git", "ls-files", "-s", "-z"], cwd=PROJECT).split(b"\0"):
        if not row:
            continue
        metadata, name = row.split(b"\t", 1)
        name = name.decode("utf-8")
        if name.startswith(frozen_roots) or name in frozen_code:
            data = (PROJECT / name).read_bytes()
            actual = hashlib.sha1(b"blob " + str(len(data)).encode() + b"\0" + data).hexdigest()
            frozen_count += 1
            if actual != metadata.split()[1].decode():
                index_mismatches.append(name)
    links = []
    missing = []
    for name in DOCS:
        source = PROJECT / name
        for target in re.findall(r"\]\(([^)]+)\)", source.read_text(encoding="utf-8-sig")):
            target = target.strip().strip("<>").split("#", 1)[0]
            if not target or re.match(r"(?:https?|mailto|codex|plugin|app):", target):
                continue
            path = Path(unquote(target))
            if not path.is_absolute():
                path = source.parent / path
            exists = path.exists()
            links.append({"source": name, "target": target, "exists": exists})
            if not exists:
                missing.append(links[-1])
    output = PROJECT / "Concepts/Used/Stalker/Postures05/Validation/final-production-and-links.json"
    report = {"productionPngCount": len(atlases), "productionHashesMatch": True, "atlases": atlases,
              "frozenIndexFilesChecked": frozen_count, "frozenIndexByteMismatches": index_mismatches,
              "activeDocumentCount": len(DOCS), "checkedLocalLinks": len(links), "missingLinks": missing,
              "documents": [{"path": name, "sha256": sha(PROJECT / name)} for name in DOCS],
              "scope": "Current30 production PNGs (including8 retained historical base-attack atlases) and listed active-document links only; no claim of global GUID/reference validation or fresh pixel verification of pruned capture caches."}
    output.write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")
    print(json.dumps({key: report[key] for key in ("productionPngCount", "productionHashesMatch", "activeDocumentCount", "checkedLocalLinks", "missingLinks", "frozenIndexFilesChecked", "frozenIndexByteMismatches")}, indent=2))
    assert not missing
    assert not index_mismatches


if __name__ == "__main__":
    main()
