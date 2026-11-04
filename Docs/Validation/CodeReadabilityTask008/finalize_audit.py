"""Record the explicitly reviewed final TASK-008 verdict against actual evidence.

Source quality comes from the recorded manual decisions, never these assertions.
No Unity, imported asset, stage, commit or Notion operation is performed here.
"""
import datetime
import hashlib
import json
from pathlib import Path
import subprocess

EVIDENCE = Path(__file__).resolve().parent
PROJECT = EVIDENCE.parents[2]


def read(name):
    return json.loads((EVIDENCE / name).read_text(encoding="utf-8-sig"))


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    source = read("source-review.final.json")
    assert not source["pendingPaths"] and len(source["files"]) == 950
    for row in source["files"]:
        assert digest(PROJECT / row["path"]) == row["sha256"], row["path"]
    for name, count in (("all-editmode.final.json", 1478), ("all-playmode.final.json", 117)):
        tests = read(name)
        assert tests["status"] == "completed"
        assert tests["summary"] == {"total": count, "passed": count, "failed": 0, "skipped": 0, "inconclusive": 0}
    native = read("native.phase2.final.json")
    assert native["preserved"] and native["differences"] == []
    assert native["missingScripts"] == native["missingReferences"] == 0
    assets = read("native.asset-references.final.json")
    assert assets["passed"] and not assets["failures"]
    saved = read("saved-inputs.phase2.after.json")
    assert saved["passed"] and len(saved["changed"]) == 17 and saved["task007FilesVerified"] == 114
    assert not saved["unexpected"] and not saved["task007Differences"]
    assert read("historical-test-outputs.after.json")["passed"]
    build = read("release-build.json")
    assert build["result"] == "Succeeded" and build["errors"] == 0 and build["warnings"] == 1
    assert build["releaseOnly"] and build["existingPickupProfileIncluded"] and not build["missingPlayerHitAssets"]
    assert digest(Path(build["player"])) == build["playerSha256"]
    console = read("console.final.json")["groundTruth"]
    assert not console["compilationFailed"] and console["consoleErrors"] == 0
    registry_path = PROJECT / "Docs/Reports/CodeAudit.json"
    registry = json.loads(registry_path.read_text(encoding="utf-8-sig"))
    assert "task008FinalVerification" not in registry
    assert registry["counts"] == {"reviewed_boundary": 944, "preserved_unread_draft_exclusion": 6}
    qualified = {row["path"]: row for row in registry["files"]}
    assert set(qualified) == {row["path"] for row in source["files"]}
    for row in source["files"]:
        assert qualified[row["path"]]["sha256"] == row["sha256"]
        if row["state"] != "preserved_unread_draft_exclusion":
            assert qualified[row["path"]]["reviewedSha256"] == row["sha256"]
    head = subprocess.check_output(["git", "rev-parse", "HEAD"], cwd=PROJECT).decode().strip()
    evidence = {
        "task": "TASK-008", "qualifiedUtc": datetime.datetime.now(datetime.timezone.utc).isoformat(),
        "baseHead": head, "sourceSnapshot": "source-review.final.json",
        "sourceSnapshotSha256": digest(EVIDENCE / "source-review.final.json"),
        "manualReviewedSources": 944, "preservedUnreadDrafts": 6,
        "editMode": "1478/1478", "playMode": "117/117", "smoke": "15/15", "sleeper": "25/25",
        "build": "release-build.json", "nativePreservation": "native.phase2.final.json.gz",
        "savedInputs": "saved-inputs.phase2.after.json", "nativeAssets": "native.asset-references.final.json",
        "console": "console.final.json", "knownBuildWarning": "RuntimePipelineConfig absent; Pipeline disabled in ordinary Player.",
        "scope": "Explicit manual source conclusions and actual scoped/full regressions of the shared working tree. Six named owner-paused drafts unread; historical external recipes source-only. No clean publication, performance or artistic acceptance claim.",
        "authoringReview": "Not applicable: no authored scene objects added/moved or scenes saved. One explicit ProjectContext prefab installer append is verified separately."
    }
    with (EVIDENCE / "final-verification.json").open("x", encoding="utf-8") as output:
        json.dump(evidence, output, ensure_ascii=False, indent=2)
    for flag in ("complete", "sourceCoverageComplete", "technicalSourceReviewComplete", "technicalMigrationComplete"):
        registry[flag] = True
    registry["ownerAcceptanceComplete"] = False
    registry["verifiedCodeRevision"] = "Shared working tree on base " + head + "; exact source SHA snapshot: " + evidence["sourceSnapshotSha256"]
    registry["sourceCoverage"] = {"reviewedFiles": 944, "eligibleFiles": 944, "totalFiles": 950,
        "excludedUnreadDraftFiles": 6, "queuedFiles": 0, "hashesCurrent": True, "date": "2026-11-04",
        "scope": evidence["scope"], "validation": "Manual receipts/module and per-file decisions: source-review.final.json. Actual integration: final-verification.json. Exclusions are not counted as reviewed."}
    registry["latestReview"] = {"date": "2026-11-04", "report": "Docs/Reports/CodeReadabilityTask008.md",
        "files": 944, "totalInventory": 950, "excludedUnreadDrafts": 6,
        "validation": "1478/1478 EditMode,117/117 PlayMode,smoke15/Sleeper25/native Scorpion motion,266 native prefab/profile assets,ordinary Windows64 Succeeded0errors.2703 loaded records and114 foreign inputs preserved;17 explicit saved-input changes."}
    registry["reviewPasses"].append(registry["latestReview"].copy())
    registry["task008FinalVerification"] = evidence
    registry["limits"] = evidence["scope"]
    registry_path.write_text(json.dumps(registry, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"task008TechnicalComplete": True, "reviewed": 944, "excludedUnread": 6, "ownerAcceptance": False}))


if __name__ == "__main__":
    main()
