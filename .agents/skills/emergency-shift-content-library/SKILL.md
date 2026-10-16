---
name: emergency-shift-content-library
description: Organize or create Emergency Shift concepts, sprites, audio, documentation and source provenance. Read for every project task; use the content checks when content or docs change.
---

# Content and documentation

Read Concepts/README.md and Docs/SPEC.md. Production resources stay under Assets/EmergencyShift. Concepts/Used contains implemented source families, with replaced versions marked Historical. Concepts/Unused contains rejected or unused studies. Used is not a promise of current use or artistic approval.

Before removing/moving a Unity resource, inspect dependencies from all saved scenes, prefabs and render-pipeline settings. An old folder name does not mean a texture is unused: active sprite subassets may reference it. Preserve .meta and source/license records. Keep historical duplicate GUIDs outside Assets. Never move caches, runtimes or secrets into the content library.

Retain source authors/licenses, prompts or generation provenance and processing instructions. Do not commit signed download URLs or tokens. Update Concepts/catalog.json when canonical materials move. Do not infer approval from an ambient browser URL.

When authoring reusable enemy/death/encounter content, distinguish a shared definition and visual variant from an individual spawn and a persistent remains record. Record units, valid ranges, variant selection and required prefab references in the handoff. An asset family must not require new type checks across bullets, HUD and audio just to add another data variant. Local checkpoint/remains ownership is implemented; further campaign/pool/dynamic-spawn work still needs task scope. Preserve exact settled presentation snapshots when changing sprite layouts, and revise the level saveRevision if compatibility changes.

Current Stalker reference is pale gray-pink flesh, strict top-down, full connected legs, transparent sprite backgrounds. Accepted membrane/pistol/casings/foley remain unless the task changes them. New musical direction requires a short audition and user approval before full expansion/integration; existing authorized maintenance needs no new approval ceremony.

Canonical current docs: Docs/README.md, SPEC.md, ARCHITECTURE.md, MVP_PLAN.md, Guides, Enemies and Audio. Update one source of truth rather than append contradictory histories. Delete irrelevant intermediate test dumps only after preserving current evidence and provenance. Generate FILE_MAP from actual source layout, not imagined systems.

Current scope: MechanicsSandbox is the primary authored test scene; FirstShift was removed at the owner’s request. Defer playable-level/campaign construction. See Docs/SANDBOX.md. Sandbox R starts fresh, T refills owned weapon ammo, and the range temporarily grants player health; do not attach checkpoint/objective controllers there without a new task.
