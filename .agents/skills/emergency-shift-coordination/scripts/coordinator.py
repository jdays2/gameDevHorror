"""Cooperative chat leases, outside Unity Assets. No processes or tools are launched."""

import argparse
from contextlib import contextmanager
from datetime import datetime, timezone
import json
import os
from pathlib import Path
import sys
import time
import uuid


class Conflict(Exception):
    pass


def canonical(path):
    return str(Path(path).resolve()).replace("\\", "/").casefold().rstrip("/")


def overlaps(left, right):
    return left == right or left.startswith(right + "/") or right.startswith(left + "/")


def empty_state():
    return {"version": 1, "jobs": {}, "queue": [], "leases": [], "last_handoff": None}


def read_state(path):
    if not path.exists():
        return empty_state()
    state = json.loads(path.read_text(encoding="utf-8"))
    if state.get("version") != 1:
        raise Conflict("Unknown registry version; inspect it instead of overwriting.")
    return state


@contextmanager
def transaction(path):
    path.parent.mkdir(parents=True, exist_ok=True)
    lock = path.with_suffix(".lock")
    deadline = time.monotonic() + 5
    while True:
        try:
            fd = os.open(str(lock), os.O_WRONLY | os.O_CREAT | os.O_EXCL, 0o600)
            break
        except FileExistsError:
            if time.monotonic() >= deadline:
                raise Conflict("Registry transaction busy. A stale lock needs inspection; no auto-takeover.")
            time.sleep(0.05)
    temporary = path.with_name(path.name + "." + uuid.uuid4().hex + ".tmp")
    try:
        with os.fdopen(fd, "w", encoding="utf-8") as stream:
            json.dump({"pid": os.getpid(), "created": stamp()}, stream)
        state = read_state(path)
        yield state
        temporary.write_text(json.dumps(state, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
        os.replace(temporary, path)
    finally:
        temporary.unlink(missing_ok=True)
        lock.unlink(missing_ok=True)


def stamp():
    return datetime.now(timezone.utc).isoformat()


def project_root(value):
    if value:
        root = Path(value).resolve()
        if not (root / "Assets/EmergencyShift").is_dir():
            raise Conflict("--project must identify the existing Emergency Shift project.")
        return root
    for origin in (Path.cwd(), Path(__file__).resolve().parent):
        for root in (origin, *origin.parents):
            if (root / "Assets/EmergencyShift").is_dir():
                return root
            nested = root / "outputs/EmergencyShift"
            if (nested / "Assets/EmergencyShift").is_dir():
                return nested
    raise Conflict("Emergency Shift project not found; provide --project.")


def apply(state, args, project):
    owner = args.owner
    if args.command == "join":
        if owner in state["jobs"]:
            if state["jobs"][owner]["role"] != args.role:
                raise Conflict("Changing a registered role needs manager review; no implicit replacement.")
            if args.role == "manager" or owner in state["queue"]:
                return
        if args.after and args.after not in state["queue"]:
            raise Conflict("Predecessor is not queued; inspect current ownership first.")
        if args.after and state["queue"][-1] != args.after:
            raise Conflict("That predecessor already has a successor; join after the queue tail.")
        state["jobs"][owner] = {"task": args.task, "role": args.role, "joined": stamp()}
        if args.role == "worker":
            if args.after:
                state["queue"].insert(state["queue"].index(args.after) + 1, owner)
            else:
                state["queue"].append(owner)
        return
    if owner not in state["jobs"]:
        raise Conflict("Join with the real thread ID before requesting a lease.")
    own = next((lease for lease in state["leases"]
                if lease["owner"] == owner and lease["resource"] == args.resource), None)
    if args.command == "claim":
        if any(any(symbol in item for symbol in "*?[]") for item in args.paths):
            raise Conflict("Use exact paths or directory subtrees, not glob patterns.")
        paths = sorted(set(canonical(project / item) for item in args.paths))
        if args.resource == "files":
            if not paths:
                raise Conflict("A files lease needs exact paths/directory subtrees, without globs.")
            imported = [canonical(project / item) for item in ("Assets", "Packages", "ProjectSettings", ".git")]
            if any(overlaps(path, root) for path in paths for root in imported):
                raise Conflict("Imported game roots need game access; Git metadata needs git access.")
        if args.resource == "game" and (not state["queue"] or state["queue"][0] != owner):
            raise Conflict("Wait for explicit release/handoff by the predecessor; idle is not release.")
        for lease in state["leases"]:
            if lease is own or lease["owner"] == owner:
                continue
            same_exclusive = args.resource in ("game", "git") and lease["resource"] == args.resource
            shared_path = any(overlaps(left, right) for left in paths for right in lease["paths"])
            if same_exclusive or shared_path:
                raise Conflict("Reserved by " + lease["owner"] + ": " + lease["resource"])
        if own:
            own["paths"] = paths
        else:
            state["leases"].append({"owner": owner, "resource": args.resource,
                                    "paths": paths, "since": stamp()})
        return
    if not own:
        raise Conflict("This thread does not hold that lease.")
    if not args.note.strip():
        raise Conflict("Record a concise stopped-work/preservation handoff note.")
    if args.command == "handoff":
        if args.resource != "game":
            raise Conflict("Only game access is handed off; release git/files separately.")
        if any(lease["owner"] == owner and lease["resource"] != "game" for lease in state["leases"]):
            raise Conflict("Release your git/files reservations before handing off game access.")
        if len(state["queue"]) < 2 or state["queue"][0] != owner or state["queue"][1] != args.to:
            raise Conflict("Recipient must be the next queued worker; resolve queue changes explicitly.")
        state["queue"].pop(0)
        own.update(owner=args.to, paths=[], since=stamp())
        state["last_handoff"] = {"from": owner, "to": args.to, "note": args.note, "at": stamp()}
    else:
        if args.resource == "game" and any(lease["owner"] == owner and lease["resource"] != "game"
                                           for lease in state["leases"]):
            raise Conflict("Release your git/files reservations before releasing game access.")
        state["leases"].remove(own)
        if args.resource == "game":
            state["queue"].remove(owner)
        state["last_handoff"] = {"from": owner, "to": None, "resource": args.resource,
                                 "note": args.note, "at": stamp()}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--project")
    parser.add_argument("--state", type=Path, help="Explicit registry path, primarily for isolated tests.")
    sub = parser.add_subparsers(dest="command", required=True)
    sub.add_parser("status")
    join = sub.add_parser("join")
    join.add_argument("--owner", required=True)
    join.add_argument("--task", required=True)
    join.add_argument("--role", choices=("manager", "worker"), default="worker")
    join.add_argument("--after")
    for name in ("claim", "release", "handoff"):
        command = sub.add_parser(name)
        command.add_argument("--owner", required=True)
        command.add_argument("--resource", choices=("game", "git", "files"), required=True)
        if name == "claim":
            command.add_argument("--paths", nargs="*", default=[])
        else:
            command.add_argument("--note", required=True)
        if name == "handoff":
            command.add_argument("--to", required=True)
    args = parser.parse_args()
    try:
        project = project_root(args.project)
        path = args.state or project.parent / ".coordination/EmergencyShift/state.json"
        if args.command == "status":
            state = read_state(path)
        else:
            with transaction(path) as state:
                apply(state, args, project)
        print(json.dumps({"ok": True, "registry": str(path), **state}, ensure_ascii=False, indent=2))
        return 0
    except (Conflict, OSError, ValueError, KeyError) as error:
        print(json.dumps({"ok": False, "reason": str(error)}, ensure_ascii=False))
        return 2


if __name__ == "__main__":
    sys.exit(main())
