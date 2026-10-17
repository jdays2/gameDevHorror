"""Local Markdown reader. Only task checkbox characters can be written."""

import argparse
import hashlib
import html
import json
import mimetypes
import os
from pathlib import Path
import re
import secrets
import tempfile
import threading
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from urllib.parse import parse_qs, quote, unquote, urlsplit
import webbrowser


PROJECT = Path(__file__).resolve().parents[2]
TASK = re.compile(r"^(\s*[-*+]\s+\[)([ xX])(\]\s+)(.*)$")
WRITE_LOCK = threading.Lock()


def revision(raw):
    return hashlib.sha256(raw).hexdigest()


def project_file(root, name):
    path = (root / name).resolve()
    if not path.is_relative_to(root.resolve()):
        raise ValueError("Файл вне проекта")
    relative = path.relative_to(root.resolve())
    if any(part.startswith(".") for part in relative.parts) and relative.parts[0] != ".agents":
        raise ValueError("Служебный файл недоступен")
    if relative.parts[0] not in {"Docs", "Concepts", ".agents", "Tools"} and len(relative.parts) != 1:
        raise ValueError("Раздел недоступен")
    return path


def writable(root, path):
    relative = path.relative_to(root.resolve()).as_posix()
    return relative == "Docs/MVP_PLAN.md" or (
        relative.startswith("Docs/Tasks/") and path.suffix.lower() == ".md"
        and path.name not in {"README.md", "TEMPLATE.md"}
    )


def set_checkbox(root, data):
    path = project_file(root, data["doc"])
    if not writable(root, path):
        raise ValueError("Этот документ доступен только для чтения")
    index = data["line"]
    if type(index) is not int or type(data["checked"]) is not bool:
        raise ValueError("Некорректная отметка")
    with WRITE_LOCK:
        raw = path.read_bytes()
        if revision(raw) != data["revision"]:
            raise FileExistsError("Файл изменился. Обновите страницу и повторите отметку.")
        # Decode without stripping BOM; splitlines retains each original line ending.
        lines = raw.decode("utf-8").splitlines(keepends=True)
        if not 0 <= index < len(lines):
            raise ValueError("Строка не найдена")
        match = TASK.match(lines[index].rstrip("\r\n"))
        if not match:
            raise ValueError("В строке нет чекбокса")
        position = match.start(2)
        lines[index] = lines[index][:position] + ("x" if data["checked"] else " ") + lines[index][position + 1:]
        updated = "".join(lines).encode("utf-8")
        temporary = None
        try:
            with tempfile.NamedTemporaryFile(dir=path.parent, prefix=".plan-", delete=False) as handle:
                temporary = Path(handle.name)
                handle.write(updated)
            if path.read_bytes() != raw:
                raise FileExistsError("Файл изменился. Обновите страницу.")
            os.replace(temporary, path)
        finally:
            if temporary and temporary.exists():
                temporary.unlink()
        return revision(updated)


def slug(value):
    return re.sub(r"[^\w\- ]", "", value.lower()).replace(" ", "-")


def link_target(root, document, target):
    target = html.unescape(target).strip("<>")
    if target.startswith("#"):
        return target
    if re.match(r"^https?://", target):
        return target
    if re.match(r"^[A-Za-z]:[/\\]", target):
        # The skills catalog links to installed instruction files, read-only.
        return "/installed?file=" + quote(target, safe="")
    parsed = urlsplit(target)
    if parsed.scheme:
        return "#"
    path = project_file(root, str(document.parent / unquote(parsed.path)))
    name = path.relative_to(root).as_posix()
    fragment = "#" + quote(parsed.fragment) if parsed.fragment else ""
    if path.suffix.lower() == ".md":
        return "/?doc=" + quote(name, safe="/") + fragment
    return "/file?doc=" + quote(name, safe="/")


def inline(root, document, value):
    # Escape raw HTML and use placeholders so formatting cannot rewrite generated tags.
    parts = []

    def keep(markup):
        parts.append(markup)
        return f"\x00{len(parts) - 1}\x00"

    value = re.sub(r"`([^`]+)`", lambda m: keep("<code>" + html.escape(m[1]) + "</code>"), value)
    value = html.escape(value)

    def link(match):
        try:
            target = link_target(root, document, match[2])
        except ValueError:
            return match[1]
        return keep('<a href="' + html.escape(target, quote=True) + '">' + match[1] + "</a>")

    value = re.sub(r"\[([^\]]+)\]\(([^)]+)\)", link, value)
    value = re.sub(r"\*\*(.+?)\*\*", r"<strong>\1</strong>", value)
    value = re.sub(r"(?<!\*)\*([^*]+)\*(?!\*)", r"<em>\1</em>", value)
    return re.sub(r"\x00(\d+)\x00", lambda m: parts[int(m[1])], value)


def render(root, path, raw):
    lines = raw.decode("utf-8-sig").splitlines()
    output = []
    in_code = False
    table = False
    count = done = 0
    can_write = writable(root, path) if path.is_relative_to(root) else False

    for index, line in enumerate(lines):
        if line.startswith("```"):
            output.append("</code></pre>" if in_code else "<pre><code>")
            in_code = not in_code
            continue
        if in_code:
            output.append(html.escape(line) + "\n")
            continue
        if not line.startswith("|") and table:
            output.append("</tbody></table></div>")
            table = False
        if not line.strip():
            continue
        task = TASK.match(line)
        if task:
            count += 1
            checked = task[2].lower() == "x"
            done += checked
            attrs = " checked" if checked else ""
            attrs += " disabled" if not can_write else ""
            output.append(
                '<div class="task' + (" done" if checked else "") + '"><input type="checkbox" id="task-' + str(index)
                + '" data-line="' + str(index) + '"' + attrs + '><div class="task-copy"><label for="task-'
                + str(index) + '">' + inline(root, path, task[4]) + "</label></div></div>"
            )
            continue
        heading = re.match(r"^(#{1,6})\s+(.+)$", line)
        if heading:
            level = len(heading[1])
            output.append(f'<h{level} id="{html.escape(slug(heading[2]))}">' + inline(root, path, heading[2]) + f"</h{level}>")
        elif line.startswith("|"):
            if re.match(r"^\|[\s:|\-]+\|$", line):
                continue
            cells = line.strip().strip("|").split("|")
            tag = "td" if table else "th"
            if not table:
                output.append('<div class="table-wrap"><table><tbody>')
                table = True
            output.append("<tr>" + "".join(f"<{tag}>" + inline(root, path, cell.strip()) + f"</{tag}>" for cell in cells) + "</tr>")
        elif re.match(r"^\s*[-*_]{3,}\s*$", line):
            output.append("<hr>")
        elif re.match(r"^\s*[-*+]\s+", line):
            output.append('<p class="bullet">' + inline(root, path, re.sub(r"^\s*[-*+]\s+", "", line)) + "</p>")
        else:
            output.append("<p>" + inline(root, path, line) + "</p>")
    if table:
        output.append("</tbody></table></div>")
    if in_code:
        output.append("</code></pre>")
    return "\n".join(output), count, done


class ViewerServer(ThreadingHTTPServer):
    def __init__(self, root, port):
        self.root = root.resolve()
        self.token = secrets.token_urlsafe(32)
        super().__init__(("127.0.0.1", port), Handler)
        self.origin = f"http://127.0.0.1:{self.server_port}"


class Handler(BaseHTTPRequestHandler):
    def log_message(self, *args):
        pass

    def send(self, status, body, content_type="text/html; charset=utf-8"):
        payload = body.encode("utf-8") if isinstance(body, str) else body
        self.send_response(status)
        self.send_header("Content-Type", content_type)
        self.send_header("Content-Length", str(len(payload)))
        self.send_header("Cache-Control", "no-store")
        self.send_header("X-Content-Type-Options", "nosniff")
        self.send_header("Referrer-Policy", "no-referrer")
        self.send_header("Content-Security-Policy", "default-src 'self'; script-src 'unsafe-inline'; style-src 'unsafe-inline'; img-src 'self' data:; connect-src 'self'; frame-ancestors 'self'")
        self.end_headers()
        self.wfile.write(payload)

    def valid_host(self):
        return self.headers.get("Host") == urlsplit(self.server.origin).netloc

    def do_GET(self):
        if not self.valid_host():
            return self.send(403, "Forbidden")
        parsed = urlsplit(self.path)
        query = parse_qs(parsed.query)
        try:
            if parsed.path == "/health":
                return self.send(200, "EmergencyShiftPlanViewer", "text/plain")
            if parsed.path == "/installed":
                path = Path(query.get("file", [""])[0]).resolve()
                base = Path.home() / ".codex"
                allowed = [base / "skills", base / "plugins/cache"]
                if path.suffix != ".md" or not any(path.is_relative_to(item.resolve()) for item in allowed):
                    raise ValueError("Файл навыка недоступен")
            else:
                if parsed.path not in {"/", "/file", "/raw"}:
                    return self.send(404, "Not found")
                path = project_file(self.server.root, query.get("doc", ["Docs/MVP_PLAN.md"])[0])
            if path.stat().st_size > 2_000_000:
                raise ValueError("Документ слишком большой")
            raw = path.read_bytes()
            if parsed.path == "/raw":
                return self.send(200, raw, "text/plain; charset=utf-8")
            if parsed.path == "/file":
                if path.suffix.lower() in {".png", ".jpg", ".webp", ".gif", ".mp3", ".wav", ".mp4"}:
                    return self.send(200, raw, mimetypes.guess_type(path)[0] or "application/octet-stream")
                return self.send(200, raw, "text/plain; charset=utf-8")
            if path.suffix.lower() != ".md":
                raise ValueError("Ожидается Markdown-документ")
            markup, count, done = render(self.server.root, path, raw)
            name = path.relative_to(self.server.root).as_posix() if path.is_relative_to(self.server.root) else path.name
            state = {"doc": name, "revision": revision(raw), "token": self.server.token}
            template = Path(__file__).with_name("viewer.html").read_text(encoding="utf-8")
            page = template.replace("{{CONTENT}}", markup).replace("{{DOCUMENT}}", html.escape(name)).replace("{{DONE}}", str(done)).replace("{{TOTAL}}", str(count)).replace("{{STATE}}", json.dumps(state).replace("<", "\\u003c"))
            return self.send(200, page)
        except (ValueError, OSError) as error:
            return self.send(400, html.escape(str(error)))

    def do_POST(self):
        if not self.valid_host() or self.headers.get("Origin") != self.server.origin or self.headers.get("X-Plan-Token") != self.server.token:
            return self.send(403, '{"error":"Forbidden"}', "application/json")
        if self.path != "/checkbox":
            return self.send(404, '{"error":"Not found"}', "application/json")
        try:
            size = int(self.headers.get("Content-Length", "0"))
            if not 0 < size < 4096:
                raise ValueError("Некорректный запрос")
            data = json.loads(self.rfile.read(size))
            value = set_checkbox(self.server.root, data)
            self.send(200, json.dumps({"revision": value}), "application/json")
        except FileExistsError as error:
            self.send(409, json.dumps({"error": str(error)}), "application/json")
        except (ValueError, KeyError, OSError, TypeError) as error:
            self.send(400, json.dumps({"error": str(error)}), "application/json")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--port", type=int, default=8768)
    parser.add_argument("--open", action="store_true")
    args = parser.parse_args()
    server = ViewerServer(PROJECT, args.port)
    print(server.origin + "/?doc=Docs/MVP_PLAN.md", flush=True)
    if args.open:
        webbrowser.open(server.origin + "/?doc=Docs/MVP_PLAN.md")
    try:
        server.serve_forever()
    except KeyboardInterrupt:
        pass
    finally:
        server.server_close()


if __name__ == "__main__":
    main()
