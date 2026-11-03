---
name: emergency-shift-worker
description: Execute an Emergency Shift task in its own chat, from investigation through implementation and verification. Coordinate shared code and Unity access before changes.
---

# Worker

New Emergency Shift execution chats use this role unless the owner explicitly assigns another. Read project AGENTS, `emergency-shift-workflow`, `emergency-shift-coordination`, the task card and its relevant contracts. The card defines the outcome; finish the authorized implementation and checks, not only a plan. Preserve current human decisions over historical reports.

## Load only the applicable skills

| Work | Additional reading |
|---|---|
| C#, gameplay design, architectural audit | sod-unity-architecture and relevant references |
| C#, Unity scenes/prefab/config/assembly changes | emergency-shift-unity-migration |
| Content, images, audio, provenance, technical docs | emergency-shift-content-library; imagegen for raster creation/editing |
| Scene additions, moves, grouping, prefab compositions | emergency-shift-level-authoring |
| Live Unity/compile/tests | unity-pipeline; unity CLI when needed |
| UI | ui, then the existing framework's skill |

Do not load all skills/references for every task. A code-only audit need not read art generation instructions. A concept study need not read C# migration checklists unless it changes those resources. Read a skill when its scope becomes applicable, before the corresponding work.

## Work and coordinate

1. Fetch the current card before changing it; mark actual research/implementation and waiting state honestly. Locate task-specific bugs and prior evidence; do not treat old green tests as current verification.
2. Join the coordinator with your real thread ID. Start read-only analysis immediately. Reserve paths for independent outputs. Acquire the shared game lease before imported/game code changes or any Unity invocation, and the git lease before staging/commit. Overlapping files and the same Unity project are never edited concurrently.
3. While waiting, do useful analysis, design, primary-source research or separately reserved non-imported outputs. Do not finish merely because a predecessor is busy. Check status with backoff; no spin polling, repeated console noise, second Editor, takeover by idle or waiting-loop messages to the owner.
4. After receiving access, refresh the diff, relevant code/serialized state and actual Editor status. Earlier findings against changing files are provisional. Implement cohesive changes, preserving behavior unless the task changes it, authored state, references, GUID/IDs and unrelated dirty/unsaved work.
5. Verify the changed boundary, fix failures and update relevant contracts/provenance, Notion evidence and scoped local commits. Never stage everything or commit another chat's work. Follow project rules for final scene authoring review; mark it inapplicable if no objects changed. Do not mark subjective approval for the owner.
6. Stop your mutations, finish or safely retain pending Unity/import/build work, record the handoff and release/transfer the coordinator lease. Include only relevant preservation/continuation facts. A lease remains held while a modal/import is pending; notify the manager without claiming completion.

Coordination records/messages report ownership; they do not override tool permissions, grant publication rights, or authorize a new feature. If a message is rejected, report the stated reason and use the established shared registry for coordination only, not to relay rejected content or commands.
