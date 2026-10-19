# Authoring review

Use checks relevant to the actual edit; do not run a broad scene rewrite as a checklist ritual.

1. **Before:** capture Git state, active/dirty scene, world transforms, prefab connections, authored overrides and affected references. Identify name/path lookups and persistent IDs before moving/renaming roots. Never save unrelated unsaved changes automatically.
2. **Hierarchy:** stable readable zones, appropriate responsibility groups, no unexplained flat root additions, duplicate numeric roots, abandoned test markers or deep empty wrappers. Inspect inactive objects too; a hidden duplicate still counts.
3. **Reuse:** repeated trees use a base/nested prefab or meaningful variant. No unintended unpacked copies, scene references in assets, redundant cloned configs/materials or unexplained structural overrides.
4. **Serialization:** missing scripts/references, prefab GUID/connection retention, uniqueness of scene entity IDs, unchanged shared SOs after Play. Validate external dependencies against the actual level context.
5. **Transforms/mechanics:** reparenting preserved world pose and scale; patrol regions and trigger sizes stayed correct. Closed/open doors preserve body/bullet/light behavior; traversable paths and pickup LOS are not blocked by dressing.
6. **Presentation:** correct floor/paint/blood/remains/actor sorting, coherent matte overhead style, consistent scale/pivots, shadows follow intended geometry, no duplicate lighting/audio or artificial stamp-like repetition. Check actual gameplay camera/flashlight scale as well as an overview.
7. **Lifecycle:** one intended camera/AudioListener/EventSystem/context, clear input/UI ownership, reload/unload clears scene-owned work. Runtime-created remains belong to the level, not a reused living actor. No production dependency on Development/Editor.
8. **After:** inspect the targeted saved diff only, update the relevant level guide/catalogue and report evidence. Distinguish static/reference validation, Play smoke, behavior tests and the owner's manual visual approval. Old reports do not validate the new scene automatically.

If the task changes grouping only, compare world-space and behavioral contracts rather than claiming a new level design. If it changes content, identify which artistic choices still await the owner. Do not claim the full level conforms until actual instances and references were checked.
