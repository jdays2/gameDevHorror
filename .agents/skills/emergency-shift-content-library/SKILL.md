---
name: emergency-shift-content-library
description: Organize or create Emergency Shift concepts, sprites, audio, documentation and source provenance. Read for every project task; use the content checks when content or docs change.
---

# Content and documentation

Read Concepts/README.md and Docs/SPEC.md. Production resources stay under Assets/EmergencyShift. Concepts/Used contains implemented source families, with replaced versions marked Historical. Concepts/Unused contains rejected or unused studies. Used is not a promise of current use or artistic approval.

Before removing/moving a Unity resource, inspect dependencies from all saved scenes, prefabs and render-pipeline settings. An old folder name does not mean a texture is unused: active sprite subassets may reference it. Preserve .meta and source/license records. Keep historical duplicate GUIDs outside Assets. Never move caches, runtimes or secrets into the content library.

## Readable asset libraries

Organize production libraries by purpose and reusable family. Do not keep a growing flat Materials folder: separate shared rendering, environment families, characters, effects, physics and development studies as applicable. Create a folder when it has a real owner; avoid empty taxonomy and a folder per single asset. A family-specific shader, mask and supporting data stay beside their materials; genuinely shared resources have one shared owner. Reuse the existing material unless a persistent visual difference requires another; per-instance presentation does not justify duplicating shared assets.

Apply organization to actual assets, not only documentation. Move them through AssetDatabase while Play is stopped; preserve GUIDs, file contents and unsaved scenes. Inspect saved scene/prefab/render-pipeline dependencies before and after. Update hardcoded paths, dynamically constructed output directories, editor builders and active offline processors so they cannot recreate the old flat layout. Keep dated validation/provenance captures intact; record old-to-new paths, GUIDs and hashes in a relocation manifest/catalogue. Verify loaded material/shader references and shader errors. The current project map is `Docs/Guides/MATERIALS_LIBRARY.md`.

Retain source authors/licenses, prompts or generation provenance and processing instructions. Do not commit signed download URLs or tokens. Update Concepts/catalog.json when canonical materials move. Do not infer approval from an ambient browser URL.

## Generated visual assets: natural irregularity

For this project's generated props, materials and arrangements, avoid the sterile AI-catalogue look: every contour mechanically perfect, every item centered, every gap identical. Apply this requirement when writing prompts and reviewing outputs; a generic "not AI-looking" negative prompt is insufficient.

- Specify plausible, restrained variation appropriate to the object: slightly uneven seams, wood grain and board widths, local rim irregularity, unequal packing gaps, or contents placed a little off-center/at different angles. Keep manufactured construction coherent; do not warp every straight edge or turn every circular object into an arbitrary blob.
- Distinguish technical framing from internal arrangement. The sprite canvas/pivot may be centered, while a crate on a pallet or boxes on a shelf need not be centered or identically aligned. Position objects as people would use/store them, with a credible purpose rather than random offsets everywhere.
- Clean/intact does not mean geometrically flawless or showroom-arranged. Natural variation does not require dirt, rust, blood, dents or damage; respect the requested condition and do not introduce those variants automatically. Uniform procedural noise over every surface is not a substitute for intentional variation.
- For maintained, used environments, choose a few specific signs of an object's practical history before generating: one swivel caster turned differently, a replaced fastener, a faint jar ring, a local handling rub or a small touch-up. The owner permits such restrained marks; do not automatically forbid every scratch, spot or stain unless a pristine condition was explicitly requested. Keep most surfaces maintained and vary the cause between objects instead of stamping the same grunge recipe everywhere.
- Preserve strict overhead projection, readable silhouettes, family style, alpha and modular connection geometry. Keep tile seams and assembly ports accurate where required; put variation inside those boundaries. Prefer separable contents when reuse benefits from different arrangements.
- Inspect the family together at close-up and intended gameplay scale. Check for repeated perfect centering, cloned spacing/details, excessive symmetry and uniformly polished surfaces. If these dominate, make a targeted correction within the current generation task before presenting the result. Skill updates alone do not authorize regenerating existing assets or changing scenes.
- Record the intended individual feature and verify that it actually appears in the composited preview, including mechanical differences such as caster direction. A prompt or provenance description is not proof. Check real RGBA compositing: hidden RGB in alpha-zero pixels may look like a halo in an RGB-only viewer. Preserve pipe ports, straight cabinet bodies and other functional geometry while varying surface history and movable parts.

When authoring reusable enemy/death/encounter content, distinguish a shared definition and visual variant from an individual spawn and a persistent remains record. Record units, valid ranges, variant selection and required prefab references in the handoff. An asset family must not require new type checks across bullets, HUD and audio just to add another data variant. Local checkpoint/remains ownership is implemented; further campaign/pool/dynamic-spawn work still needs task scope. Preserve exact settled presentation snapshots when changing sprite layouts, and revise the level saveRevision if compatibility changes.

Current Stalker reference is pale gray-pink flesh, strict top-down, full connected legs, transparent sprite backgrounds. Accepted membrane/pistol/casings/foley remain unless the task changes them. New musical direction requires a short audition and user approval before full expansion/integration; existing authorized maintenance needs no new approval ceremony.

Canonical technical docs: Docs/README.md, SPEC.md, ARCHITECTURE.md, Features, Guides, Enemies and Audio. All active plans, content proposals, tasks, bugs and changelog/release records live in Notion; follow Docs/NOTION_WORKFLOW.md and .agents/notion-project.json. MVP_PLAN and other marked planning files are historical snapshots. Update one source of truth rather than append contradictory histories. Delete irrelevant intermediate test dumps only after preserving current evidence and provenance. Generate FILE_MAP from actual source layout, not imagined systems.

Current scope: MechanicsSandbox is the primary authored test scene; FirstShift was removed at the owner’s request. Defer playable-level/campaign construction. See Docs/SANDBOX.md. Sandbox R starts fresh, T refills owned weapon ammo, and the range temporarily grants player health; do not attach checkpoint/objective controllers there without a new task.
