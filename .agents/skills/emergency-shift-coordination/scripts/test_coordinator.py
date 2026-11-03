"""Isolated behavioral checks; never read or mutate the real game registry."""

import json
from pathlib import Path
import subprocess
import sys
import shutil
import unittest
import uuid


SCRIPT = Path(__file__).with_name("coordinator.py")


class CoordinatorTests(unittest.TestCase):
    def setUp(self):
        self.base = SCRIPT.parents[4] / "work/roles-tests"
        self.root = self.base / uuid.uuid4().hex
        self.root.mkdir(parents=True)
        self.project = self.root / "project"
        (self.project / "Assets/EmergencyShift").mkdir(parents=True)
        self.registry = self.root / "state.json"
        self.prefix = [sys.executable, str(SCRIPT), "--project", str(self.project),
                       "--state", str(self.registry)]
        for owner in ("first", "second"):
            self.run_cli("join", "--owner", owner, "--task", owner)

    def tearDown(self):
        resolved = self.root.resolve()
        if resolved.parent != self.base.resolve():
            raise RuntimeError("Refusing cleanup outside the isolated test directory")
        shutil.rmtree(resolved)

    def run_cli(self, *arguments, expected=0):
        result = subprocess.run(self.prefix + list(arguments), capture_output=True, text=True, encoding="utf-8")
        self.assertEqual(result.returncode, expected, result.stdout + result.stderr)
        return json.loads(result.stdout)

    def test_game_waits_for_explicit_handoff_and_queue_is_preserved(self):
        self.run_cli("claim", "--owner", "first", "--resource", "game")
        self.run_cli("claim", "--owner", "second", "--resource", "game", expected=2)
        state = self.run_cli("handoff", "--owner", "first", "--resource", "game",
                             "--to", "second", "--note", "Stopped; no Unity work remains.")
        self.assertEqual(state["leases"][0]["owner"], "second")
        self.run_cli("claim", "--owner", "first", "--resource", "game", expected=2)

    def test_overlapping_paths_conflict_but_disjoint_outputs_are_parallel(self):
        self.run_cli("claim", "--owner", "first", "--resource", "files", "--paths", "Docs/Design")
        self.run_cli("claim", "--owner", "second", "--resource", "files", "--paths",
                     "docs/design/report.md", expected=2)
        self.run_cli("claim", "--owner", "second", "--resource", "files", "--paths", "Concepts/Separate")

    def test_files_cannot_bypass_game_gate(self):
        for path in ("Assets/a.cs", "Packages/manifest.json", "ProjectSettings/Editor.asset", ".git/index", "."):
            self.run_cli("claim", "--owner", "second", "--resource", "files", "--paths", path, expected=2)
        self.run_cli("claim", "--owner", "second", "--resource", "files", "--paths", "Docs/*.md", expected=2)

    def test_git_is_exclusive_and_handoff_requires_release(self):
        self.run_cli("claim", "--owner", "first", "--resource", "game")
        self.run_cli("claim", "--owner", "first", "--resource", "git")
        self.run_cli("claim", "--owner", "second", "--resource", "git", expected=2)
        self.run_cli("handoff", "--owner", "first", "--resource", "game",
                     "--to", "second", "--note", "Not stopped", expected=2)
        self.run_cli("release", "--owner", "first", "--resource", "git", "--note", "Index operations finished")
        self.run_cli("handoff", "--owner", "first", "--resource", "game",
                     "--to", "second", "--note", "Stopped")

    def test_failed_transition_does_not_corrupt_registry(self):
        self.run_cli("claim", "--owner", "first", "--resource", "game")
        before = self.registry.read_bytes()
        self.run_cli("handoff", "--owner", "second", "--resource", "game",
                     "--to", "first", "--note", "Wrong owner", expected=2)
        self.assertEqual(before, self.registry.read_bytes())
        self.run_cli("join", "--owner", "third", "--task", "third", "--after", "first", expected=2)

    def test_process_exit_does_not_expire_lease(self):
        self.run_cli("claim", "--owner", "first", "--resource", "game")
        state = self.run_cli("status")
        self.assertEqual(state["leases"][0]["owner"], "first")
        self.run_cli("release", "--owner", "second", "--resource", "game", "--note", "Takeover", expected=2)

    def test_worker_can_rejoin_for_a_later_turn_without_taking_current_owner(self):
        self.run_cli("claim", "--owner", "first", "--resource", "game")
        self.run_cli("handoff", "--owner", "first", "--resource", "game", "--to", "second", "--note", "Stopped")
        state = self.run_cli("join", "--owner", "first", "--task", "next", "--after", "second")
        self.assertEqual(state["queue"], ["second", "first"])
        self.assertEqual(state["leases"][0]["owner"], "second")

    def test_independent_docs_do_not_block_game_but_owner_must_release_files(self):
        self.run_cli("claim", "--owner", "first", "--resource", "game")
        self.run_cli("claim", "--owner", "second", "--resource", "files", "--paths", "Docs/Separate.md")
        self.run_cli("claim", "--owner", "first", "--resource", "files", "--paths", "Docs/Other.md")
        self.run_cli("release", "--owner", "first", "--resource", "game", "--note", "Pending file writer", expected=2)
        self.run_cli("handoff", "--owner", "first", "--resource", "game",
                     "--to", "second", "--note", "Pending file writer", expected=2)

    def test_racing_git_claims_have_exactly_one_owner(self):
        processes = [subprocess.Popen(self.prefix + ["claim", "--owner", owner, "--resource", "git"],
                                     stdout=subprocess.PIPE, stderr=subprocess.PIPE)
                     for owner in ("first", "second")]
        results = []
        for process in processes:
            process.communicate(timeout=15)
            results.append(process.returncode)
        self.assertEqual(sorted(results), [0, 2])
        self.assertEqual(len(self.run_cli("status")["leases"]), 1)


if __name__ == "__main__":
    unittest.main()
