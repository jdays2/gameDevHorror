"""Exercise persistence, concurrent edits and write boundaries in isolated files."""

import json
from pathlib import Path
import tempfile
import threading
import unittest
from urllib.error import HTTPError
from urllib.request import Request, urlopen

from server import ViewerServer, project_file, render, revision, set_checkbox


class ViewerTests(unittest.TestCase):
    def setUp(self):
        scratch = Path(__file__).resolve().parents[2] / 'work/plan-viewer-tests'
        scratch.mkdir(parents=True, exist_ok=True)
        self.directory = tempfile.TemporaryDirectory(dir=scratch)
        self.root = Path(self.directory.name).resolve()
        self.assertTrue(self.root.is_relative_to(scratch.resolve()))
        self.addCleanup(self.directory.cleanup)
        (self.root / 'Docs').mkdir()
        self.path = self.root / 'Docs/MVP_PLAN.md'
        self.original = b'\xef\xbb\xbf# Plan\r\n\r\n- [ ] First\r\n- [x] Second\r\n'
        self.path.write_bytes(self.original)

    def payload(self, checked=True):
        return dict(doc='Docs/MVP_PLAN.md', line=2, checked=checked, revision=revision(self.path.read_bytes()))

    def test_persists_only_marker_preserving_bom_and_crlf(self):
        data = self.payload()
        set_checkbox(self.root, data)
        self.assertEqual(self.path.read_bytes(), self.original.replace(b'[ ]', b'[x]'))
        set_checkbox(self.root, self.payload(False))
        self.assertEqual(self.path.read_bytes(), self.original)

    def test_stale_browser_cannot_overwrite_agent_edit(self):
        data = self.payload()
        updated = self.original + b'\r\nAgent note\r\n'
        self.path.write_bytes(updated)
        with self.assertRaises(FileExistsError):
            set_checkbox(self.root, data)
        self.assertEqual(self.path.read_bytes(), updated)

    def test_no_arbitrary_write_or_path_traversal(self):
        with self.assertRaises(ValueError):
            project_file(self.root, '../outside.md')
        self.assertFalse((self.root.parent / 'outside.md').exists())
        self.path = self.root / 'README.md'
        self.path.write_bytes(self.original)
        data = self.payload()
        data['doc'] = 'README.md'
        with self.assertRaises(ValueError):
            set_checkbox(self.root, data)
        self.assertEqual(self.path.read_bytes(), self.original)

    def test_real_checkboxes_escaped_html_and_links(self):
        raw = '# Test\n\n- [x] **Done**\n- [ ] [Details](../README.md)\n<script>alert(1)</script>\n'.encode()
        markup, count, done = render(self.root, self.path, raw)
        self.assertEqual((count, done), (2, 1))
        self.assertEqual(markup.count('type="checkbox"'), 2)
        self.assertNotIn('<script>', markup)
        self.assertIn('/?doc=README.md', markup)
        markup, _, _ = render(self.root, self.path, '[Skill](<C:/Users/Mark/.codex/skills/example/SKILL.md>)'.encode())
        self.assertIn('/installed?file=C%3A', markup)

    def test_http_write_requires_current_origin_and_session_token(self):
        server = ViewerServer(self.root, 0)
        self.addCleanup(server.server_close)
        worker = threading.Thread(target=server.serve_forever, daemon=True)
        worker.start()
        self.addCleanup(server.shutdown)
        body = json.dumps(self.payload()).encode()
        with self.assertRaises(HTTPError) as error:
            urlopen(Request(server.origin + '/checkbox', data=body, method='POST'))
        self.assertEqual(error.exception.code, 403)
        request = Request(server.origin + '/checkbox', data=body, method='POST', headers={
            'Origin': server.origin, 'X-Plan-Token': server.token, 'Content-Type': 'application/json'
        })
        with urlopen(request) as response:
            self.assertEqual(response.status, 200)
        with urlopen(server.origin + '/?doc=Docs/MVP_PLAN.md') as response:
            page = response.read().decode()
        self.assertIn('data-line="2" checked', page)
        self.assertEqual(self.path.read_bytes(), self.original.replace(b'[ ]', b'[x]'))


if __name__ == '__main__':
    unittest.main()
