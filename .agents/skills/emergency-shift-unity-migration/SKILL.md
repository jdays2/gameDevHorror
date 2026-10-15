---
name: emergency-shift-unity-migration
description: Safely modify Emergency Shift C#, Unity scenes, prefabs, configs or assemblies. Read for every project task; execute this checklist only when those resources change.
---

# Unity changes and migration

Before a migration, capture the dirty Git state and actual serialized values. Stop Play and preserve manual edits. Move Unity assets with .meta, retaining GUIDs. Do not hand-replace a live scene with an old generator.

Place gameplay in Runtime by responsibility, authoring in Editor, temporary probes in Development, tests in Tests. Runtime cannot depend on Editor/Development. Updating paths includes authoring scripts and offline asset processors. A MonoBehaviour probe cannot blindly move into an Editor-only assembly if it must be attached in Play.

EnemyDefinition/StalkerBehaviourProfile/EnemyPatrolProfile hold shared tuning only. Health, aggro, timers, ammo and collected upgrades belong to instances. Spawn location, detection radius and patrol bounds stay in the scene. Capture exceptional per-instance stats in distinct configs; never average them away. Missing required profiles must be explicit errors.

Use common health/perception/motor/melee/patrol modules with a species brain and presentation adapters. Do not add ECS, DI or an Update manager without a measured need. Avoid common code importing the Stalker state enum.

For consequential changes: compile; inspect actual console errors; compare serialized values and missing scripts/references; run relevant EditMode and scene regressions; build when assemblies/runtime integration changed. Build with normal settings restored afterward. A successful Editor test does not verify release inclusion boundaries. Keep temporary probes out of saved scenes. Runtime mutations must not dirty shared SO assets.

Docs/ARCHITECTURE.md describes real boundaries and remaining demo-only flow. Docs/VALIDATION.md lists runnable checks. Avoid claiming campaign/save support from LevelContext/LevelRig.
