---
name: emergency-shift-level-authoring
description: Structure and author Emergency Shift levels for clear navigation and reuse. Use for creating, dressing, grouping or reviewing Unity scenes, reusable room/prop compositions and prefab variants; not for unrelated games or code-only changes without scene authoring.
---

# Level structure and reusable authoring

Apply the owner's requirement: a level must be easy to navigate in Hierarchy, ordinary repeated content must remain reusable, and the next level must not require copying scene-specific gameplay code. This skill defines authoring decisions; it does not authorize replacing existing maps or changing mechanics.

## Read only what the task needs

Start with the project's AGENTS, Docs/SPEC, Docs/ART_DIRECTION and the affected level/feature contract. Use [hierarchy and composition](references/hierarchy.md) for layout/grouping, [prefab ownership](references/prefabs.md) for repeated objects/assemblies, and [review](references/review.md) before handing off an authored change. Content provenance and natural variation follow emergency-shift-content-library. Gameplay architecture follows sod-unity-architecture.

## Essential rules

- Author scenes through the connected Unity Editor. Stop Play before authoring. Preserve the actual in-memory scene, existing GUIDs, authored positions/patrols, references and overrides. Do not rebuild a manual map from an earlier generator or saved scene over the current one.
- Group by meaningful location and responsibility. The owner should find the objects of a room, its interactions, local lights and decoration without scanning a long flat root list. Use stable room/zone names and clear object names. Avoid unexplained numbered duplicates, generic GameObject/Empty and version-history group names.
- A repeated prop or repeated functional assembly is a prefab by default. Check the library before creating another. Nested prefabs compose reusable objects; variants express genuine persistent differences. Placement, patrol bounds, spawn IDs, local light settings and links to other scene objects belong to the instance/level, not a shared asset.
- Keep the Project library navigable as well as Hierarchy. Materials and supporting shaders/masks are grouped by purpose/family; shared resources are reused and studies are separated. Follow emergency-shift-content-library and the project's Materials guide. An organized scene does not compensate for a flat growing asset folder.
- Grouping alone is not a prefab design. Preserve one functional owner per assembly: door/sensor/feedback, light/fixture/audio, desk/contents, remains/blood arrangement. Do not pack an entire unique level into a giant prefab or clone a level controller for each room.
- Keep grouping transforms neutral where possible. Reparenting preserves world transforms; verify physical shapes, lights, particles, sorting, prefab links and world-space patrol areas. Renaming is not allowed to silently break name/path lookup or persistence identity.
- Current visual direction is strict overhead, simplified matte 2D, with the owner's exceptions for existing floors/blood. Keep believable functional arrangement and restrained individual variation. Reuse is not identical rotation/spacing/contents stamped throughout the level. Use the existing family's scale, pivots, palette and modular ports.
- Geometry, movement collision, light blocking, rendering order and decoration are separate concerns. A blood mark must not block a doorway; grouping order in Hierarchy does not determine SpriteRenderer sort order. Preserve the accepted floor/blood/remains/actor relationship and read the actual renderer/material conventions before changing them.
- Scene-specific dependencies are explicit through level composition. Prefab assets must not capture scene-player/UI/door references. Stable IDs and definition IDs are different; duplicates created while authoring must not duplicate persistent entity IDs in a saved scene.
- Development objects are clearly separated, absent from release unless intentionally instrumented, and do not own production gameplay. No temporary probe, duplicate camera/AudioListener/EventSystem or loose test object is left in an authored production scene.
- Finish with a compact report of groups/prefabs/variants changed, permitted overrides, verification and any remaining manual visual assessment. Update the affected level guide and prefab catalogue, not a new parallel source of truth.

## Existing scenes versus new scenes

For new levels use the hierarchy pattern in the reference and the project's actual LevelRig/template. For an existing level first map dependencies and preserve its authored state; improve the requested area incrementally. The pattern is a target, not permission to rename every object or change every prefab instance during an unrelated task. A technical restructuring must preserve the played space and gameplay.

The owner's full code/structure pass is tracked in [Notion PLAN-00](https://app.notion.com/p/3ff8d6dc06b681b6950bd8115222669c); per-file evidence is in Docs/Reports/CodeAudit.json. Record real deviations and actual fixes; installing this skill alone does not mean the scenes have already been reorganized.
