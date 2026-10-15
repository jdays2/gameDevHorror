---
name: emergency-shift-workflow
description: Work on Emergency Shift Unity horror with its current project rules. Use for every task in this project, including discussion, review, docs, code, assets and cleanup. Do not apply game-specific rules to unrelated projects.
---

# Emergency Shift workflow

1. Locate the Unity project by its AGENTS.md and Assets/EmergencyShift. Read AGENTS.md, Docs/README.md, Docs/SPEC.md and the task-relevant document. User instructions override project defaults.
2. Distinguish discussion from authorized mutation. Preserve manual scene tuning, placements, patrols and accepted sounds/art. Do not regenerate levels to solve local problems.
3. Separate shared tuning assets, per-spawn scene data and runtime state. Prefer composition across enemy species; no inheritance from the Stalker AI. Abstraction must correspond to a real variation or dependency boundary.
4. Update the canonical requirement/guide where behavior changed. Teach the owner the relevant C# reasoning briefly; do not bury it in implementation chronology.
5. Validate the changed boundary. Use the migration and content skills for their domains. Do not claim creative approval, campaign support, scalability or FPS from a compile/build.
6. Report outcome, evidence and remaining limitations. Follow existing Git authorization without sweeping unrelated manual changes into a commit.

Always retain: top-down 2D; scene-authored content; permanent aggro after proximity+LOS; delayed light fear; no automatic defensive lamps; one procedural blood event per death; darker remains below living actors. Check Docs/Enemies/STALKER.md for the full accepted behavior. OPT-01 mass-death pooling is deliberately deferred.
