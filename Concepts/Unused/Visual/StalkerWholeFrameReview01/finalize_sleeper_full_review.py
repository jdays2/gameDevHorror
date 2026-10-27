"""Lock the actually reviewed pre-eye-fix full Sleeper capture; no Unity writes.

This file records manual views performed by the named reviewers. Hash, byte
alias and metadata checks supplement those views, rather than create an art PASS.
"""
from collections import Counter, defaultdict
from datetime import datetime, timezone
import hashlib
import json
from pathlib import Path
import shutil
import statistics

from PIL import Image

AUDIT = Path(__file__).resolve().parent
OUT = AUDIT / "RuntimeSleeperFullFinal01"


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def read(path):
    return json.loads(Path(path).read_text(encoding="utf-8-sig"))


def write(path, data):
    path = Path(path)
    path.parent.mkdir(parents=True, exist_ok=True)
    tmp = path.with_suffix(path.suffix + ".tmp")
    tmp.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    tmp.replace(path)


def source_png(source, key, number):
    return source / key / f"frame-{number:05d}.png"


frozen_path = OUT / "review-coverage.json"
assert sha(frozen_path) == "c4d60d59f8fdb0f9363d9f1bfdb77acaba020ccd8ef98eab91741aaa59c5272c"
frozen = read(frozen_path)
source = Path(frozen["sourceRoot"])
run = read(source / "run.json")
assert run["completedAll"] and not run["captureErrors"]
assert len(run["completed"]) == 3
assert sum(p["frames"] for p in run["completed"]) == 4095
assert sha(source / "run.json") == frozen["runSha256"]
assert sha(OUT / "native-page-manifest.json") == "15911fff68c5cfdbe25b82f24d1c6747cf084ec8a4c305d9ef4947f9632685bc"

own = read(OUT / "own-native-coverage.json")
body_path = AUDIT / "SleeperRuntimeFullBody01/coverage.json"
edge_path = AUDIT / "RuntimeSleeperEdgesFinal01/review-coverage.json"
assert sha(body_path) == "8f7e7e3362eb805382205c4e217c20e8d08dc8eebc5beb9256c87c21dfca38fd"
assert sha(edge_path) == "f0138441b35329418a090f9bd62ab0093704068de993d2304a4c2c434192f952"
body = read(body_path)
edge = read(edge_path)
assert body["globalFrozenProvenance"]["sha256"] == sha(frozen_path)
assert edge["finalRecheck"]["sharedCoverageSha256"] == sha(frozen_path)

records = {}
for profile in frozen["profiles"]:
    key = profile["key"]
    assert sha(source / key / "manifest.json") == profile["manifestSha256"]
    actual = read(source / key / "manifest.json")
    assert len(actual["frames"]) == len(profile["frames"]) == 1365
    assert len(profile["scenarios"]) == 22
    for item, facts in zip(profile["frames"], actual["frames"]):
        assert item["facts"] == facts
        records[(key, item["frame"])] = item
assert len(records) == 4095

# Join the three independently owned reports without giving context neighbors a
# second owner. Each owner points to an actually displayed CURRENT full image.
resolved = {}
displayed_by_owner = defaultdict(set)
for page in own["pages"]:
    for frame in page["frames"]:
        displayed_by_owner["native_whole_audit"].add((frame["profile"], frame["frame"]))
for page in body["pages"]:
    assert page["actuallyViewed"]
    for number in page["frames"]:
        displayed_by_owner["sleeper_native"].add((page["profile"], number))
for page in edge["pages"]:
    assert page["actuallyViewedOriginal"]
    for frame in page["frames"]:
        displayed_by_owner["runtime_review"].add((frame["key"], frame["frame"]))


def resolve(key, number, owner, rep, png_hash, rgb_hash, facts=None):
    identity = (key, number)
    frozen_record = records[identity]
    assert frozen_record["owner"] == owner
    assert identity not in resolved
    assert frozen_record["sha256"] == png_hash
    assert frozen_record["decodedRgbSha256"] == rgb_hash
    if facts is not None:
        assert frozen_record["facts"] == facts
    assert rep in displayed_by_owner[owner]
    assert records[rep]["decodedRgbSha256"] == rgb_hash
    resolved[identity] = {"owner": owner, "representative": rep}


for target in own["sourceTargets"]:
    if target["owner"] != "native_whole_audit":
        continue
    rep = target["exactFullRgbRepresentative"]
    resolve(target["profile"], target["frame"], "native_whole_audit",
            (rep["profile"], rep["frame"]), target["sourcePngSha256"], target["decodedRgbSha256"])

body_reps = {}
for group in body["groups"]:
    rep = (group["representative"]["profile"], group["representative"]["frame"])
    for alias in group["aliases"]:
        body_reps[(alias["profile"], alias["frame"])] = rep
for item in body["frames"]:
    resolve(item["profile"], item["frame"], "sleeper_native",
            body_reps[(item["profile"], item["frame"])], item["sourcePngSha256"],
            item["fullDecodedRgbSha256"], item["facts"])

edge_reps = {}
for group in edge["exactRgbGroups"].values():
    rep = (group["representative"]["key"], group["representative"]["frame"])
    for alias in group["aliases"]:
        edge_reps[(alias["key"], alias["frame"])] = rep
for profile in edge["profiles"]:
    for item in profile["records"]:
        resolve(profile["key"], item["frame"], "runtime_review",
                edge_reps[(profile["key"], item["frame"])], item["pngSha256"],
                item["decodedRgbSha256"], item["facts"])

assert set(resolved) == set(records)
assert Counter(x["owner"] for x in resolved.values()) == {
    "native_whole_audit": 1335, "sleeper_native": 2007, "runtime_review": 753}
print("All4095 exact owner joins resolved", flush=True)

# One RGB image at a time: no large decoded image cache. Every current raw PNG,
# decoded full RGB hash and every alias is rechecked AFTER all manual views.
by_rep = defaultdict(list)
for identity, item in resolved.items():
    by_rep[item["representative"]].append(identity)
checked = 0
direct_aliases = 0
for rep, identities in by_rep.items():
    with Image.open(source_png(source, *rep)) as image:
        assert image.size == (640, 640)
        reference_bytes = image.convert("RGB").tobytes()
    assert hashlib.sha256(reference_bytes).hexdigest() == records[rep]["decodedRgbSha256"]
    for identity in identities:
        path = source_png(source, *identity)
        assert sha(path) == records[identity]["sha256"], str(path)
        with Image.open(path) as image:
            assert image.size == (640, 640)
            current_bytes = image.convert("RGB").tobytes()
        assert hashlib.sha256(current_bytes).hexdigest() == records[identity]["decodedRgbSha256"]
        assert current_bytes == reference_bytes, str(identity)
        direct_aliases += identity != rep
        checked += 1
    if checked // 500 != (checked - len(identities)) // 500:
        print(f"After-view PNG/RGB/byte lock {checked}/4095", flush=True)
assert checked == 4095
del reference_bytes, current_bytes

# Own53 pages really were viewed original, sequentially, before this finalizer.
# Payload rows are native1x; label bands are outside the copied image region.
own_payloads = 0
for page in own["pages"]:
    path = Path(page["path"])
    assert sha(path) == page["sha256"]
    with Image.open(path) as sheet:
        assert sheet.size == (1428, 1452)
        sheet = sheet.convert("RGB")
        for slot, frame in enumerate(page["frames"]):
            x, y = (slot % 3) * 476 + 6, (slot // 3) * 484 + 40
            with Image.open(source_png(source, frame["profile"], frame["frame"])) as image:
                payload = image.convert("RGB").crop(tuple(own["fixedCrop"])).tobytes()
            assert sheet.crop((x, y, x + 464, y + 440)).tobytes() == payload
            bounds = records[(frame["profile"], frame["frame"])]["foregroundBounds"]
            assert bounds[0] >= 104 and bounds[1] >= 96 and bounds[2] <= 568 and bounds[3] <= 536
            own_payloads += 1
    page["actuallyViewedOriginal"] = True
    page["reviewer"] = "native_whole_audit"
    page["viewMethod"] = "tools.view_image(detail=original), image(...,original); no resizing/resampling"
    page["afterViewSha256"] = sha(path)
assert own_payloads == 462
for page in body["pages"]:
    assert sha(body_path.parent / page["path"]) == page["sha256"]
for page in edge["pages"]:
    assert sha(Path(page["path"])) == page["sha256"]
print("All192 viewed page hashes stable; own462 native payloads rechecked", flush=True)

metadata = []
eye_defects = []
for profile in frozen["profiles"]:
    key = profile["key"]
    facts = [r["facts"] for r in profile["frames"]]
    assert all(f["spritePixelsPerUnit"] == 224 for f in facts)
    assert all(f["bodyLocalPosition"] == [0, 0, 0] for f in facts)
    assert all(f["bodyLocalScale"] == [1, 1, 1] for f in facts)
    assert all(f["bodyLocalRotation"] == 0 for f in facts)
    walk_hz = statistics.mean((facts[i]["phase"] - facts[i - 1]["phase"]) % 1 * 60
                              for i in list(range(106, 195)) + list(range(196, 285)))
    assert abs(walk_hz - 2.1 / 1.25) < 0.002
    for first, last in [(525, 626), (967, 1068)]:
        assert max(f["phase"] for f in facts[first:last + 1]) - min(f["phase"] for f in facts[first:last + 1]) < 1e-7
    assert facts[747]["RestoreRotation"]
    assert all(facts[i]["body"] == facts[837]["body"] for i in range(837, 882))
    assert all(facts[i]["phase"] == facts[837]["phase"] for i in range(837, 882))
    assert [i for i in range(1275, 1365) if facts[i]["bodyEnabled"]] == list(range(1275, 1283))
    assert facts[-1]["deathEvents"] == 1 and facts[-1]["killCalls"] == 2 and facts[-1]["remainsRoots"] == 1
    local_eye_defects = [f["frame"] for f in facts if f["state"] != "Dead" and f["eye"] == "EyeOpen" and not f["eyeOpen"]]
    assert local_eye_defects == [90, 783, 957]
    for number in local_eye_defects:
        record = records[(key, number)]
        eye_defects.append({"profile": key, "frame": number, "closure": facts[number]["closure"],
                            "irisSprite": facts[number]["eye"], "pupilEnabled": facts[number]["eyeOpen"],
                            "sourcePngSha256": record["sha256"], "decodedRgbSha256": record["decodedRgbSha256"]})
    metadata.append({
        "profile": key, "speed": profile["speed"], "sampleHz": profile["sampleHz"],
        "attackDuration": profile["attackDuration"], "strikeAt": profile["strikeAt"],
        "walkCyclesPerSecondActual": walk_hz, "walkCyclesPerSecondExpected": 2.1 / 1.25,
        "normalAndNearAttackRecoveryPhaseHeld": True,
        "restoreFrames": [f["frame"] for f in facts if f["RestoreRotation"]],
        "normalSequence": [{k: facts[i][k] for k in ["frame", "state", "body", "phase", "fold", "closure"]}
                           for i in [524, 525, 526, 539, 540, 571, 572, 585, 586, 587, 588, 626, 627]],
        "restoreAndTargetDeadSequence": [{k: facts[i][k] for k in ["frame", "state", "body", "phase", "fold", "closure", "RestoreRotation"]}
                                         for i in [746, 747, 748, 782, 783, 784, 791, 792, 793, 836, 837, 881, 882]],
        "nearSequence": [{k: facts[i][k] for k in ["frame", "state", "body", "phase", "fold", "closure"]}
                         for i in [965, 966, 967, 968, 981, 982, 1013, 1014, 1027, 1028, 1029, 1030, 1068, 1069]],
        "deathBodyHold": list(range(1275, 1283)), "deathFirstHidden": 1283,
        "deathEvents": facts[-1]["deathEvents"], "killCallsFinal": facts[-1]["killCalls"],
        "remainsRoots": facts[-1]["remainsRoots"], "knownPreFixWhiteIrisFrames": local_eye_defects,
    })

selected_numbers = [89, 90, 91, 524, 525, 526, 539, 540, 571, 572, 585, 586, 587, 588,
                    626, 627, 746, 747, 748, 782, 783, 784, 791, 792, 793, 836, 837,
                    881, 882, 956, 957, 958, 965, 966, 967, 968, 981, 982, 1013, 1014,
                    1027, 1028, 1029, 1030, 1068, 1069, 1113, 1114, 1154, 1155, 1175,
                    1176, 1229, 1230, 1236, 1274, 1275, 1282, 1283, 1364]
selected = []
for profile in frozen["profiles"]:
    key = profile["key"]
    for number in selected_numbers:
        src = source_png(source, key, number)
        dst = OUT / "SelectedSourcePNG" / key / src.name
        dst.parent.mkdir(parents=True, exist_ok=True)
        shutil.copyfile(src, dst)
        assert sha(dst) == records[(key, number)]["sha256"]
        selected.append({"profile": key, "frame": number, "path": str(dst),
                         "sha256": sha(dst), "decodedRgbSha256": records[(key, number)]["decodedRgbSha256"],
                         "knownEyeDefect": number in [90, 783, 957]})
write(OUT / "selected-source-png.json", {"count": len(selected), "images": selected})

ordered = []
for identity in sorted(records):
    r = records[identity]
    rep = resolved[identity]["representative"]
    ordered.append({"profile": identity[0], "frame": identity[1], "sha256": r["sha256"],
                    "decodedRgbSha256": r["decodedRgbSha256"], "owner": r["owner"],
                    "actuallyDisplayedCurrentRepresentative": {"profile": rep[0], "frame": rep[1]}})
write(OUT / "ordered-source-hashes.json", {"sourceFrames": 4095, "sourceRunSha256": frozen["runSha256"], "records": ordered})
own["status"] = "COMPLETE_CURRENT_FULL_BODY_AND_TEMPORAL_REVIEW; CONFIRMED_PRE_FIX_EYE_THRESHOLD_DEFECT"
own["actuallyViewedOriginalPages"] = 53
own["actuallyDisplayedChronologicalInstances"] = 462
own["reviewer"] = "native_whole_audit"
own["afterViewAllNativePayloadsExact"] = True
own["knownEyeDefectFramesAllProfiles"] = [90, 783, 957]
own["fixedEyeAcceptance"] = "Separate new bounded all12-source replays; not inherited from this old full capture."
write(OUT / "own-native-coverage.json", own)

# Immutable capture/code boundary: the first full run is NOT claimed to have the
# now-fixed source code hash, and its capture helper subsequently gained metadata.
completion = {
    "status": "ALL4095_BODY_AND_TEMPORAL_COVERED_WITH_CONFIRMED_PRE_FIX_EYE_THRESHOLD_DEFECT",
    "completedAtUtc": datetime.now(timezone.utc).isoformat(),
    "sourceRoot": str(source), "sourceRunSha256": frozen["runSha256"],
    "frozenSourceProvenanceSha256": sha(frozen_path), "sourceProfiles": 3,
    "sourceFrames": 4095, "scenariosPerProfile": 22, "captureErrors": [],
    "allCurrentPngAndDecodedRgbHashesRecheckedAfterViews": True,
    "all4095CurrentFullRgbImagesDirectByteComparedToActuallyDisplayedCurrentRepresentatives": True,
    "directAliasInstancesExcludingIdentity": direct_aliases,
    "exactOwnerCounts": dict(Counter(x["owner"] for x in resolved.values())),
    "ownerGapCount": 0, "ownerOverlapCount": 0,
    "currentActualNativePages": 192, "currentDisplayedFrameInstances": 1367,
    "credits": [
        {"reviewer": "native_whole_audit", "ownedFrames": 1335, "pages": 53, "displayedChronologicalInstances": 462,
         "report": "own-native-coverage.json", "sha256": sha(OUT / "own-native-coverage.json")},
        {"reviewer": "sleeper_native", "ownedFrames": 2007, "pages": 112, "displayedChronologicalInstances": 669,
         "report": str(body_path), "sha256": sha(body_path)},
        {"reviewer": "runtime_review", "ownedFrames": 753, "pages": 27, "displayedInstances": 236,
         "report": str(edge_path), "sha256": sha(edge_path)},
    ],
    "all192ReviewedPageHashesUnchanged": True, "ownNativePayloadsRechecked": own_payloads,
    "metadata": metadata, "knownPreFixEyeDefects": eye_defects,
    "productionDelta": {
        "oldPupilPredicate": "!dead && presentation.Closure < 0.166f",
        "newPupilPredicate": "!dead && eyeFrame == 0",
        "oldIrisPredicate": "RoundToInt(Closure * 3)",
        "deltaEvidence": "EyeThresholdDelta01/delta-plan.json",
        "oldFullCodeHashEqualsFixedCodeHash": False,
        "oldWholeFileReconstructionLimit": "Before whole source was reconstructed from confirmed one-line delta, not independently saved before editing.",
        "captureHelperChangedForSeparateFollowup": True,
        "statefulGazeCaveat": "Earlier pupil enabling can advance gaze; later eye RGB requires bounded moving-target followup, not old pixel inheritance.",
        "fixedEyeGate": "Separate RuntimeEyeThresholdFinal01 current all12 replay plus followup proof; pending at this completion lock."
    },
    "retainedSourcePngCount": len(selected), "selectedSourceManifest": "selected-source-png.json",
    "selectedSourceManifestSha256": sha(OUT / "selected-source-png.json"),
    "orderedSourceHashes": "ordered-source-hashes.json", "orderedSourceHashesSha256": sha(OUT / "ordered-source-hashes.json"),
    "scopeLimits": [
        "Presentation animator capture uses actual scene-authored source clones and diagnostic RestoreState fixtures; it is not free AI gameplay or owner real-time feel acceptance.",
        "Current full4095 source PNG bytes were checked; only credited representative images and chronological contexts were actually displayed.",
        "No focused cold or warm visual PASS was inherited. All aliases are complete current640x640 RGB bytes without alignment, masking, resize or tolerance.",
        "Green actual runtime only here; static material review on green/dark/light is linked separately in the audit index.",
        "Explicit fixture restore747 and diagnostic death camera1.65 to4.5 are not reported as continuous-animation failures.",
        "Old full eye threshold defect is retained as a finding, and this report does not declare fixed-code full4095 capture.",
        "Scene authoring control not applicable: this independent audit changed no production asset, C#, scene or Unity state."
    ],
    "rawCacheRelease": {
        "permittedAfterThisLock": True, "scope": str(source / "<three exact profile folders>/frame-*.png"),
        "condition": "Root cleanup only; retain run/3manifests, ordered hashes, all native pages/reports/videos and SelectedSourcePNG originals including all9 old eye defects and immediate neighbors.",
        "noDeletionByThisAudit": True
    }
}

for profile in frozen["profiles"]:
    assert sha(source / profile["key"] / "manifest.json") == profile["manifestSha256"]
assert sha(source / "run.json") == frozen["runSha256"]
assert sha(frozen_path) == "c4d60d59f8fdb0f9363d9f1bfdb77acaba020ccd8ef98eab91741aaa59c5272c"
write(OUT / "independent-completion.json", completion)
write(OUT / "completion-hashes.json", {
    "status": completion["status"],
    "files": {p.name: sha(p) for p in [frozen_path, OUT / "native-page-manifest.json",
              OUT / "own-native-coverage.json", OUT / "ordered-source-hashes.json",
              OUT / "selected-source-png.json", OUT / "independent-completion.json"]},
    "finalizerSha256": sha(Path(__file__))
})
print(json.dumps({"status": completion["status"], "sourceFrames": 4095,
                  "completionSha256": sha(OUT / "independent-completion.json"),
                  "ownCoverageSha256": sha(OUT / "own-native-coverage.json"),
                  "selectedOriginals": len(selected), "allSourceAndPageHashLocks": True}, indent=2), flush=True)
