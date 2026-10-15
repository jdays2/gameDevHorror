---
name: emergency-shift-unity-migration
description: Safely modify Emergency Shift C#, Unity scenes, prefabs, configs or assemblies. Read for every project task; execute this checklist only when those resources change.
---

# Unity changes and migration

For C# writing/review/refactoring, read [readability-and-configs](references/readability-and-configs.md). For levels, enemy lifecycle, death effects, saves, encounters or crowd scaling, also read [level-and-enemy-lifetimes](references/level-and-enemy-lifetimes.md). Discussion-only tasks use these to assess designs, not to authorize code changes.

Before a migration, capture the dirty Git state and actual serialized values. Stop Play and preserve manual edits. Move Unity assets with .meta, retaining GUIDs. Do not hand-replace a live scene with an old generator.

Place gameplay in Runtime by responsibility, authoring in Editor, temporary probes in Development, tests in Tests. Runtime cannot depend on Editor/Development. Updating paths includes authoring scripts and offline asset processors. A MonoBehaviour probe cannot blindly move into an Editor-only assembly if it must be attached in Play.

EnemyDefinition/StalkerBehaviourProfile/EnemyPatrolProfile hold shared tuning only. Health, aggro, timers, ammo and collected upgrades belong to instances. Spawn location, detection radius and patrol bounds stay in the scene. Capture exceptional per-instance stats in distinct configs; never average them away. Missing required profiles must be explicit errors.

Use common health/perception/motor/melee/patrol modules with a species brain and presentation adapters. Do not add ECS, DI or an Update manager without a measured need. Avoid common code importing the Stalker state enum.

For consequential changes: compile; inspect actual console errors; compare serialized values and missing scripts/references; run relevant EditMode and scene regressions; build when assemblies/runtime integration changed. Build with normal settings restored afterward. A successful Editor test does not verify release inclusion boundaries. Keep temporary probes out of saved scenes. Runtime mutations must not dirty shared SO assets.

Docs/ARCHITECTURE.md describes actual boundaries; Docs/Guides/CHECKPOINTS.md defines the implemented local checkpoint policy. Preserve permanent IDs, content IDs, level saveRevision and silent restoration of Dead/settled remains. A new persistence payload needs presence/version validation and a death/reload test using isolated slots. Checkpoint support does not imply campaign transitions or persistent runtime-spawn factories. Docs/VALIDATION.md lists runnable checks.

Review actual source before declaring it clean: formatting, data ownership and dependency direction matter in the newly extracted components too. Keep an identified existing defect in Docs/ENGINEERING_REVIEW.md until fixed; a skill update does not close that defect. Source research is recorded there with explicit adoption limits.
