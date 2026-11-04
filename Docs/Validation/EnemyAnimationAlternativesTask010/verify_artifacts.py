"""Integrity checks for TASK-010 evidence; not a browser or Unity test."""
from pathlib import Path
from datetime import datetime, timezone
import hashlib
import json
import re
from html.parser import HTMLParser
import numpy as np
from PIL import Image

HERE = Path(__file__).resolve().parent
PROJECT = HERE.parents[2]
OUT = PROJECT / "Concepts/Unused/Visual/EnemyAnimationAlternativesTask010"
REPORT = PROJECT / "Docs/Research/EnemyAnimationAlternativesTask010.md"


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def load(path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


baseline = load(HERE / "source-preservation.json")
changed = [p for p, h in baseline["inputs"].items() if digest(PROJECT / p) != h]
assert not changed, changed
poses = load(OUT / "poses.json")
jsdata = (OUT / "poses.js").read_text(encoding="utf-8")
assert json.loads(jsdata.removeprefix("window.RESEARCH_POSES = ").rstrip().removesuffix(";")) == poses["clips"]
assert len(poses["clips"]) == 5
assert all(len(clip) == 49 for clip in poses["clips"].values())

broken = []
local_links = []


def check_link(base, link):
    if re.match(r"(?:https?:|mailto:|#|data:)", link):
        return
    target = (base / link.split("#")[0]).resolve()
    local_links.append(str(target))
    if not target.exists():
        broken.append(str(target))


for md in [REPORT, OUT / "README.md", HERE / "README.md"]:
    for link in re.findall(r"\]\(([^)]+)\)", md.read_text(encoding="utf-8")):
        check_link(md.parent, link)


class Links(HTMLParser):
    def handle_starttag(self, tag, attrs):
        for key, value in attrs:
            if key in ("src", "href"):
                check_link(OUT, value)


Links().feed((OUT / "comparison.html").read_text(encoding="utf-8"))
assert not broken, broken

animations = {}
for kind in ("walk", "fold", "unfold", "attack"):
    image = Image.open(OUT / f"schematic-{kind}.png")
    expected = 48 if kind == "walk" else 49
    assert image.n_frames == expected, (kind, image.n_frames)
    assert image.size == (860, 480)
    durations = []
    for i in range(image.n_frames):
        image.seek(i)
        image.load()
        durations.append(image.info["duration"])
        assert 20 < image.info["duration"] < 22
    animations[kind] = dict(frames=image.n_frames, dimensions=list(image.size),
                            durationMs=sum(durations), plays=image.info.get("loop"))
    assert image.info.get("loop") == (0 if kind == "walk" else 1)

reference_checks = []
for info in load(HERE / "references.json"):
    source = Image.open(PROJECT / info["texture"]).convert("RGBA")
    x, y, w, h = info["rectUnityBottomLeft"]
    original = source.crop((x, source.height-y-h, x+w, source.height-y))
    reference = Image.open(OUT / info["output"]).convert("RGBA")
    px, py = info["paste"]
    restored = reference.crop((px, py, px+w, py+h))
    assert np.array_equal(np.array(original), np.array(restored))
    reference_checks.append(dict(output=info["output"], fullRectRGBAEqual=True))

metrics = load(HERE / "numeric-checks.json")
for item in metrics["metrics"].values():
    assert item["maxBoneLengthErrorLocalUnits"] < 1e-12
    assert item["maxBetweenSamplesPlantedFootDisplacementLocalUnits"] < 1e-12
    assert item["rootProjectionOutsidePlantedConvexHullFrames"] == 0
    assert item["minimumJointHeight"] >= -1e-12

result = dict(checkedUtc=datetime.now(timezone.utc).isoformat(),
              selectedInputsUnchanged=len(baseline["inputs"]), localLinksChecked=len(local_links), brokenLocalLinks=broken,
              poseJSMatchesJSON=True, schematicSamples=245, animations=animations, references=reference_checks,
              browserUI="NOT_TESTED: CUA iab unavailable and browser inventory empty; no alternative transport used",
              unity="NOT_INVOKED", ownerArtAcceptance=False,
              sceneAuthoringReview="Неприменим: сценовые объекты не менялись.")
(HERE / "artifact-checks.json").write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding="utf-8")
print(json.dumps(result, ensure_ascii=False, indent=2))
