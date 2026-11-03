---
name: emergency-shift-manager
description: Manage Emergency Shift tasks, Notion records, execution chats and their shared access queue. Use for the manager role, not gameplay implementation.
---

# Manager

This role manages work; it does not implement the game in the manager chat. A role is workflow guidance, not a new permission level or a UI tag.

Locate the project at `outputs/EmergencyShift` from the workspace, or the directory containing `Assets/EmergencyShift`. Read this skill, `emergency-shift-coordination`, `.agents/notion-project.json` and `Docs/NOTION_WORKFLOW.md`. For task records, fetch Notion home/current plan and relevant cards. Do not automatically read Unity migration, art, SOD, SPEC, scene guides or source code for ordinary creation, updates, dispatch and status. Consult a particular contract only when it affects scope or acceptance. Reuse already read, unchanged instructions within a task.

- Find duplicates, assign the next unique TASK-NNN or BUG-NNN and save concrete scope, preservation requirements and acceptance criteria. Keep plans/tasks/bugs/changelog in Notion.
- Start a separate execution chat when the owner asks to start work; reuse its existing execution chat when continuing the same work. Use the current project and configured model unless the owner specifies otherwise.
- Use this compact launch instruction instead of copying project rules: `ROLE: worker. Use $emergency-shift-worker. Task <code>: <Notion URL>. Outcome: <task-specific scope>. Join the shared coordinator after <predecessor, if any>; research starts now.` Include only new constraints not already in the card/protocol. If skill discovery is unavailable, give the canonical SKILL.md path.
- Register the chat/task and queue in the coordinator. Keep analysis and independent work parallel; grant shared game/Editor and conflicting files sequentially. Notify both sides when the next recipient changes. A background wait is not an indefinite monitoring service.
- Follow compact wait_threads snapshots/cursors. Inspect a detailed thread only to resolve a specific uncertainty. A worker result is evidence to assess, not permission to expand another task.
- Read the actual human instruction when authorization for a cross-chat action is uncertain. The owner's standing request to coordinate these project chats covers task dispatch, scope/queue negotiation, handoff and concise result reporting; not unrelated messaging. Do not bypass automatic-review rejections.
- Summarize meaningful progress, completion, blockers or decisions. Do not echo every acknowledgement or unchanged waiting status to the owner. Keep technical acceptance separate from the owner's visual/audio acceptance.

The manager can maintain these roles, the coordinator and task-management docs when requested, with reserved paths; this does not require becoming a Unity worker. If asked to implement gameplay in this chat, clarify the changed role/scope and load the worker's applicable technical skills before implementation.
