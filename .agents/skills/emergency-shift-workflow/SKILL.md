---
name: emergency-shift-workflow
description: Work on Emergency Shift Unity horror with its current project rules. Use for every task in this project, including discussion, review, docs, code, assets and cleanup. Do not apply game-specific rules to unrelated projects.
---

# Emergency Shift workflow

1. Locate the Unity project by its AGENTS.md and Assets/EmergencyShift. Read AGENTS.md, Docs/README.md, Docs/SPEC.md and the task-relevant document. User instructions override project defaults.
2. Distinguish discussion from authorized mutation. Preserve manual scene tuning, placements, patrols and accepted sounds/art. Do not regenerate levels to solve local problems.
3. Use the adopted [SOD architecture](../sod-unity-architecture/SKILL.md) for gameplay design/C# and review. Its MVC, command, dependency, config and identity rules replace older architecture advice. Separate shared tuning, per-spawn scene data and instance state; preserve the game's species contracts. Current MonoBehaviour code and Configure wiring are legacy, not proof of a completed MVC/Zenject migration.
   Readability is part of completion: one meaningful action per line, explicit control-flow blocks, no nested ternaries, meaningful names. Short code means less cognitive work, not compressed statements. Guards and a finite state switch are legitimate; growing type-dispatch chains need a design review.
4. Update the canonical requirement/guide where behavior changed. Teach the owner the relevant C# reasoning briefly; do not bury it in implementation chronology.
5. Validate the changed boundary. Use the migration and content skills for their domains. Do not claim creative approval, campaign support, scalability or FPS from a compile/build.
6. Report outcome, evidence and remaining limitations. Follow existing Git authorization without sweeping unrelated manual changes into a commit.

For design or code changes, identify the owner of configuration, runtime state and cleanup. Check the cost of adding the next level/enemy/weapon: ordinary content should not require edits across unrelated systems. State planned boundaries as plans, not implemented features. Do not turn a request to research/update rules into a runtime rewrite.

Always retain: top-down 2D; scene-authored content; permanent aggro after proximity+LOS; delayed light fear; no automatic defensive lamps; one procedural blood event per death; darker remains below living actors. Check Docs/Enemies/STALKER.md for the full accepted behavior. OPT-01 mass-death pooling is deliberately deferred.

Current scope: MechanicsSandbox is the primary authored test scene; FirstShift was removed at the owner’s request. Defer playable-level/campaign construction. See Docs/SANDBOX.md. Sandbox R starts fresh, T refills owned weapon ammo, and the range temporarily grants player health; do not attach checkpoint/objective controllers there without a new task.
