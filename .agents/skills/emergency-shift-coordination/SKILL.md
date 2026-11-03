---
name: emergency-shift-coordination
description: Coordinate parallel Emergency Shift chats with persistent shared game, git and file reservations. Use before shared changes or queue handoff, in manager and worker roles.
---

# Shared coordination

The canonical protocol is `Docs/Guides/CHAT_COORDINATION.md` in the Emergency Shift project. Read it once when joining/changing ownership, not on every tool call. Resolve the project from the workspace's `outputs/EmergencyShift` or its `Assets/EmergencyShift` marker.

Use `scripts/coordinator.py --project <absolute project> status` to inspect the shared queue/leases. The script is beside this SKILL.md. See the protocol for join/claim/handoff commands and recovery. Use a working Python interpreter, not a broken WindowsApps alias; the Codex bundled runtime is suitable.

- Parallel: read-only investigation and separately reserved non-imported documents/concept outputs.
- Exclusive `game`: all writes to the shared imported game resources/code and all Editor/import/compile/Play/tests/build actions; one owner across both code and Editor.
- Exclusive `git`: staging/commit and other index/HEAD mutations in this checkout; reserve it even for docs.
- `files`: exact files or directory subtrees outside imported game roots. Ancestor/descendant and case-insensitive overlaps conflict. The helper rejects imported game paths claimed as independent files.
- Register requests/leases before changes. Waiting does not prohibit useful independent work. A queue item, role label, idle thread, expired time or old message is not a lease.
- `handoff` atomically records explicit transfer to the queued successor, after the current holder has stopped work. `release` explicitly frees a lease. Never infer release from a crash or forcibly clear another owner.

The registry is cooperative ownership state, not a sandbox or an authorization service. Human instructions and tool permissions still apply. Messages are useful for scope changes and blockers; ownership can be verified in the registry without repeatedly transmitting repository details between chats. Do not put secrets, raw logs or copied rejected messages there.
