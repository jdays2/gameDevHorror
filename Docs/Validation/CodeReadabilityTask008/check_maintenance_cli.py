"""Isolated CLI regressions; never refresh the shared project's audit registry."""
from pathlib import Path
import argparse
import hashlib
import json
import shutil
import subprocess
import sys
import tempfile
import unittest

PROJECT = Path(__file__).resolve().parents[3]


class MaintenanceCliTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory(prefix="tmp-", dir=Path(__file__).parent)
        self.addCleanup(self.cleanup_fixture)
        self.root = Path(self.temporary.name)
        for folder in ("Tools/Maintenance", "Assets/EmergencyShift", "AgentScripts",
                       "Concepts", "Docs/Reports", ".agents/skills"):
            (self.root / folder).mkdir(parents=True, exist_ok=True)
        self.write("AGENTS.md", "# Test fixture\n")
        for name in ("audit_code.py", "audit_documentation.py"):
            shutil.copyfile(PROJECT / "Tools/Maintenance" / name,
                            self.root / "Tools/Maintenance" / name)

    def cleanup_fixture(self):
        target = Path(self.temporary.name).resolve()
        if not target.is_relative_to(Path(__file__).parent.resolve()) or not target.name.startswith("tmp-"):
            raise RuntimeError(f"Refusing cleanup outside reserved validation directory: {target}")
        self.temporary.cleanup()

    def write(self, relative, content):
        path = self.root / relative
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(content, encoding="utf-8")
        return path

    def run_cli(self, name, *arguments):
        return subprocess.run([sys.executable, "-B", str(self.root / "Tools/Maintenance" / name),
                               *arguments], cwd=self.root, text=True, encoding="utf-8",
                              capture_output=True, check=False, timeout=30)

    def audit_fixture(self):
        entries = []
        for name in ("Same", "Changed", "Removed"):
            path = self.write(f"Assets/EmergencyShift/{name}.cs", f"class {name} {{ }}\n")
            digest = hashlib.sha256(path.read_bytes()).hexdigest()
            entries.append({"path": path.relative_to(self.root).as_posix(), "sha256": digest,
                            "reviewedSha256": digest, "reviewStatus": "reviewed_boundary",
                            "reviewDate": "2026-10-22", "notes": f"Manual review: {name}"})
        registry = {"date": "2026-10-22", "files": entries, "complete": True,
                    "sourceCoverageComplete": True, "technicalSourceReviewComplete": True,
                    "technicalMigrationComplete": True, "ownerAcceptanceComplete": False,
                    "sourceCoverage": {"reviewedFiles": 3, "totalFiles": 3, "queuedFiles": 0,
                                       "hashesCurrent": True, "validation": "Historical validation"},
                    "latestReview": {"date": "2026-10-22", "validation": "Historical tests"},
                    "reviewPasses": [{"date": "2026-10-22", "report": "Historical.md"}]}
        path = self.write("Docs/Reports/CodeAudit.json", json.dumps(registry))
        self.write("Assets/EmergencyShift/Changed.cs", "class Changed { int value; }\n")
        (self.root / "Assets/EmergencyShift/Removed.cs").unlink()
        self.write("Assets/EmergencyShift/Added.cs", "class Added { }\n")
        return path, registry

    def test_read_only_drift_does_not_write_registry(self):
        path, _ = self.audit_fixture()
        before = path.read_bytes()
        result = self.run_cli("audit_code.py")
        self.assertEqual(1, result.returncode, result.stderr)
        summary = json.loads(result.stdout)
        self.assertEqual(["Assets/EmergencyShift/Added.cs"], summary["added"])
        self.assertEqual(["Assets/EmergencyShift/Removed.cs"], summary["removed"])
        self.assertEqual(["Assets/EmergencyShift/Changed.cs"], summary["staleReviews"])
        self.assertEqual(before, path.read_bytes())

    def test_refresh_requires_date_without_touching_registry(self):
        path, _ = self.audit_fixture()
        before = path.read_bytes()
        result = self.run_cli("audit_code.py", "--refresh")
        self.assertEqual(2, result.returncode)
        self.assertIn("--refresh requires --date", result.stderr)
        self.assertEqual(before, path.read_bytes())

    def test_refresh_invalidates_readiness_and_preserves_manual_history(self):
        path, before = self.audit_fixture()
        result = self.run_cli("audit_code.py", "--refresh", "--date", "2026-11-03")
        self.assertEqual(0, result.returncode, result.stderr)
        after = json.loads(path.read_text(encoding="utf-8"))
        for flag in ("complete", "sourceCoverageComplete", "technicalSourceReviewComplete",
                     "technicalMigrationComplete", "ownerAcceptanceComplete"):
            self.assertIs(False, after[flag], flag)
        self.assertIs(False, after["sourceCoverage"]["hashesCurrent"])
        self.assertEqual(before["latestReview"], after["latestReview"])
        self.assertEqual(before["reviewPasses"], after["reviewPasses"])
        by_path = {entry["path"]: entry for entry in after["files"]}
        same = by_path["Assets/EmergencyShift/Same.cs"]
        self.assertEqual("reviewed_boundary", same["reviewStatus"])
        self.assertEqual(before["files"][0]["reviewedSha256"], same["reviewedSha256"])
        changed = by_path["Assets/EmergencyShift/Changed.cs"]
        self.assertEqual("partial", changed["reviewStatus"])
        self.assertEqual("reviewed_boundary", changed["previousReviewStatus"])
        self.assertEqual(before["files"][1]["reviewedSha256"], changed["reviewedSha256"])
        self.assertEqual("2026-10-22", changed["reviewDate"])
        self.assertTrue(changed["sourceChangedSinceReview"])
        added = by_path["Assets/EmergencyShift/Added.cs"]
        self.assertEqual("queued", added["reviewStatus"])
        self.assertNotIn("reviewedSha256", added)
        self.assertEqual([before["files"][2]], after["removedEntries"])
        self.assertEqual({"queued": 1, "partial": 1, "reviewed_boundary": 1}, after["counts"])
        result = self.run_cli("audit_code.py", "--refresh", "--date", "2026-11-03")
        self.assertEqual(0, result.returncode, result.stderr)
        repeated = json.loads(path.read_text(encoding="utf-8"))
        self.assertEqual(after["removedEntries"], repeated["removedEntries"])
        self.assertEqual(changed["reviewedSha256"],
                         next(e for e in repeated["files"] if e["path"] == changed["path"])["reviewedSha256"])

    def test_document_links_resolve_paths_before_query_fragment_and_decoding(self):
        self.write("Concepts/viewer.html", "<html></html>")
        self.write("Docs/chapter #1.md", "# Encoded filename\n")
        self.write("Docs/links.md", "[Viewer](../Concepts/viewer.html?revision=r1&state=walk#frame)\n"
                   "[Encoded](chapter%20%231.md?mode=preview#section)\n"
                   "[Mail](mailto:example@example.com) [Web](https://example.com/no-file)\n"
                   "[Local anchor](#section)\n```text\n[Example](missing-example.md)\n```\n")
        result = self.run_cli("audit_documentation.py")
        self.assertEqual(0, result.returncode, result.stdout + result.stderr)
        self.assertEqual([], json.loads(result.stdout)["brokenLocalLinks"])

    def test_real_missing_document_link_still_fails_and_keeps_original_target(self):
        self.write("Docs/links.md", "[Missing](missing.md?revision=r1#section)\n")
        result = self.run_cli("audit_documentation.py")
        self.assertEqual(1, result.returncode, result.stderr)
        self.assertEqual([{"source": "Docs/links.md", "target": "missing.md?revision=r1#section"}],
                         json.loads(result.stdout)["brokenLocalLinks"])


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--report", type=Path)
    arguments = parser.parse_args()
    suite = unittest.defaultTestLoader.loadTestsFromTestCase(MaintenanceCliTests)
    result = unittest.TextTestRunner(verbosity=2).run(suite)
    if arguments.report:
        arguments.report.write_text(json.dumps({
            "testsRun": result.testsRun, "failures": [str(t) for t, _ in result.failures],
            "errors": [str(t) for t, _ in result.errors], "passed": result.wasSuccessful(),
            "scope": "Copied maintenance CLIs in disposable minimal projects; shared registry and Unity untouched."
        }, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    raise SystemExit(0 if result.wasSuccessful() else 1)
