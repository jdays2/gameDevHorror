"""Apply explicit manual TASK-008 Scorpion decisions after metadata refresh.

Does not infer source quality from counters, names, tokens or a passing compile.
Only the listed a05f2ab7 sources can receive these specific reviewed conclusions.
"""
import collections
import copy
import hashlib
import json
from pathlib import Path
import subprocess

EVIDENCE = Path(__file__).resolve().parent
PROJECT = EVIDENCE.parents[2]
REGISTRY = PROJECT / "Docs/Reports/CodeAudit.json"
COMMIT = "a05f2ab7e0e2a270cbfec41f64603c82e191eff6"
DECISIONS = {
    "Assets/EmergencyShift/Runtime/Features/Scorpion/Logic/ScorpionSettings.cs": "Immutable validated tuning snapshot; accepts zero pursuit, requires positive absolute attack speed and finite positive derived gather cycle. No SO or per-instance state ownership. Seven scalars are configuration, not hidden service dependencies.",
    "Assets/EmergencyShift/Runtime/Features/Scorpion/Logic/ScorpionModel.cs": "Own clock/timeline, facing, gait, aggro and motion speed; shared settings readonly. Steering applies per-second rates to dt; gathering uses independent travel/attack-speed cycle. Committed heading, finish/cancel/death order retained. Pure math/clock domain; no View, input or camera authority.",
    "Assets/EmergencyShift/Runtime/Features/Scorpion/Logic/ScorpionController.cs": "Short current-facts Step, finite nonnegative dt validated before effects, absolute attack speed avoids 0*Infinity. Existing one-shot melee finish/interrupt and resolved-route steering retained; model plus two ports are an explicit three-dependency coordinator exception.",
    "Assets/EmergencyShift/Runtime/Features/Scorpion/ScorpionControllerFactory.cs": "Composition maps existing authored definition/presentation once to settings and a personal model. Scoped factory does not imply shared actor state. No serialized field/value/path migration; authored-asset JSON and mapped scalar values verified by real factory test.",
    "Assets/EmergencyShift/Runtime/Features/Scorpion/View/Scorpion2D.cs": "Native health/perception/motor/melee adapter retains actual travel and allowed physical speed, passes current dt/facts. First standalone aggro emits cue once; Alert remains silent. Hit/death/native-disable order retained; no concrete audio lookup remains in actor.",
    "Assets/EmergencyShift/Runtime/Features/Scorpion/View/ScorpionPack2D.cs": "Explicit room membership/door subscriptions and personal pack state; alert live members, select one living caller, request cue before counter increment. Native independent second pack and repeat suppression checked. No world-wide species dispatcher or audio playback ownership.",
    "Assets/EmergencyShift/Runtime/Features/Scorpion/View/ScorpionAudio2D.cs": "View owns cue subscription in OnEnable/OnDisable and native playback/counters. Existing pause/hearing/mute/perceptible-cue and fatal-hit guards retained. Re-enable is not historical event replay; three subscription cycles still produce one pack voice.",
    "Assets/EmergencyShift/Tests/EditMode/ScorpionTests.cs": "Original 13 controls migrated without weakening assertions. Real counterexample first failed on finite velocity; now covers zero pursuit, exact authored factory map/immutable assets, separate gather duration, invalid dt before effects, instance isolation and existing motion/melee interruption order. Twenty model/factory cases pass; with audio-hit fixture28/28.",
    "Assets/EmergencyShift/Tests/PlayMode/ScorpionAlarmSceneTests.cs": "Four real-prefab/level-composition scenarios, not a copied selector: selected voice and cue order, lifecycle no replay, muted negative plus audible positive control, second-pack isolation, standalone/fatal hit. Baseline4/4 and post4/4. Temporary actors unload; no saved authoring or art acceptance claim.",
    "AgentScripts/Task008/Task008NativeAudit.cs": "Explicit non-imported evidence helper compares loaded authored hierarchy/normalized Editor JSON and native refs, saved scene hashes and dirty/setup state. Own JSON-null instrumentation mismatch diagnosed and fixed without changing baseline. Gzip exact reads exercised. No scene load/save; closed scenes and gameplay require other validation.",
}


def main():
    registry = json.loads(REGISTRY.read_text(encoding="utf-8-sig"))
    entries = {row["path"]: row for row in registry["files"]}
    old = []
    reviews = []
    for path, conclusion in DECISIONS.items():
        current = (PROJECT / path).read_bytes()
        committed = subprocess.run(["git", "show", COMMIT + ":" + path], cwd=PROJECT,
                                   check=True, capture_output=True).stdout
        # Git may normalize line endings; the actual reviewed bytes remain recorded below.
        if current.replace(b"\r\n", b"\n") != committed.replace(b"\r\n", b"\n"):
            raise RuntimeError("The qualified commit source changed: " + path)
        row = entries[path]
        sha = hashlib.sha256(current).hexdigest()
        if row["sha256"] != sha:
            raise RuntimeError("Metadata refresh is stale: " + path)
        old.append(copy.deepcopy(row))
        review = {"path": path, "reviewedSha256": sha, "reviewDate": "2026-11-04",
                  "reviewStatus": "reviewed_boundary", "notes": conclusion,
                  "reviewReport": "Docs/Reports/CodeReadabilityTask008.md",
                  "reviewScope": "Scoped source/manual review and actual Scorpion28 EditMode/4 native PlayMode; not complete TASK-008 or release verification.",
                  "reviewedCommit": COMMIT}
        if row.get("reviewedSha256") != sha:
            history = {key: copy.deepcopy(value) for key, value in row.items()
                       if key in ("reviewedSha256", "reviewDate", "reviewReport", "reviewScope", "reviewStatus", "notes")}
            row.setdefault("reviewHistory", []).append(history)
        row.update({key: value for key, value in review.items() if key != "path"})
        row["sourceChangedSinceReview"] = False
        reviews.append(review)
    with (EVIDENCE / "scorpion.review-history.before.json").open("x", encoding="utf-8") as output:
        json.dump(old, output, ensure_ascii=False, indent=2)
    with (EVIDENCE / "scorpion.semantic-review.json").open("x", encoding="utf-8") as output:
        json.dump({"state": "explicit scoped manual decisions", "reviews": reviews}, output, ensure_ascii=False, indent=2)
    registry["counts"] = dict(collections.Counter(row["reviewStatus"] for row in registry["files"]))
    registry["task008ScopedReview"] = {"date": "2026-11-04", "commit": COMMIT,
        "qualifiedPaths": list(DECISIONS), "completeTask": False,
        "verification": "Scorpion28 EditMode and4 native PlayMode, 2703 loaded objects preserved, 114 foreign imported files unchanged; full suite/build pending."}
    registry["limits"] = "TASK-008 in progress. Ten explicitly listed Scorpion/evidence sources received new SHA-qualified manual decisions. Other inherited statuses are historical, not new TASK-008 coverage. Full current-source qualification, remaining fixes, full native regressions and ordinary build are pending. Other chats have coordinated native windows."
    for flag in ("complete", "sourceCoverageComplete", "technicalSourceReviewComplete", "technicalMigrationComplete"):
        registry[flag] = False
    registry["sourceCoverage"]["hashesCurrent"] = False
    REGISTRY.write_text(json.dumps(registry, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"manuallyQualified": len(reviews), "currentPaths": len(registry["files"]), "counts": registry["counts"], "taskComplete": False}))


if __name__ == "__main__":
    main()
