# Prefabs, variants and ownership

## Decide the reusable boundary

| Content | Preferred boundary | Instance-specific data |
|---|---|---|
| Repeated cabinet/chair/trolley/pipe | Existing base prop prefab | Position, permitted tint/orientation |
| Door + moving leaf + collider/shadow + reader feedback | Functional door prefab with child parts | Card requirement, persistent scene ID, cross-room links |
| Light fixture + local light + audio | Functional light prefab | Actual local light/audio/phase settings via the existing configuration contract |
| Repeated desk/cart/medical arrangement | Assembly prefab containing nested prop prefabs | Placement and intentional local additions |
| Stable alternative to one common assembly | Prefab Variant | Difference has a descriptive name and survives reuse |
| Temporary clutter/unique plot arrangement | Scene-authored group, unless it has a clear reuse contract | Scene-only links/story placement |
| Entire unique level | Authored scene and reusable LevelRig | World layout, goals, spawn locations, level/save IDs |

The second ordinary occurrence is a signal to reuse/create a prefab rather than duplicate an unlinked tree. A one-off may still be a prefab when its function must be repeatable on future levels. Do not create a prefab for every leaf sprite or duplicate scripts/configs merely to satisfy this rule. One meaningful object/assembly remains one reusable unit.

## Shared versus local changes

Fix shared construction in the base prefab: component wiring, default sprite/collider, standard shadow/audio setup and relative positions of functional parts. Use a variant for a named durable alternative, not for each world coordinate. Use instance overrides for permitted placement, patrol/trigger bounds, spawn IDs and local configuration. Repeated unexplained structural overrides indicate a missing variant or a poorly chosen base.

Applying an override to the base affects other levels. Compare the diff and affected instances first; do not Apply All from one hand-tuned scene indiscriminately. Do not unpack to solve an ordinary wiring/layout problem. Existing hand-tuned values must be captured before extraction or base changes.

Prefab assets may reference other prefab assets and shared definitions, not scene objects. Assign player, HUD, encounter owner and other-level relationships in the scene installer/context or explicit serialized level link. Runtime state is not saved into SO/prefab assets. A copied prefab instance must get a valid unique persistent ID when the level's persistence authoring prepares it.

## Functional and visual reuse

Keep sprite pivot/PPU, scale and attachment ports consistent with the family. Root scale should not stretch collision/light/patrol behavior accidentally. Pipes snap by their established port definitions; floor/paint/blood are composable layers with correct sorting, not baked into every furniture texture. Use shared materials/styles/profiles; do not make duplicate assets that contain identical data just to rename an instance.

Reuse the maintained matte top-down family while varying contents, orientation and plausible wear deliberately. Random arbitrary rotation/scale of every object is not natural variation. Clean objects can still have a replaced fastener, a caster turned differently or local handling history when allowed by the art brief. Do not add universal dirt/damage or incompatible 3D-looking highlights.

Before extraction check the assembly in Prefab Mode and as a second instance in an isolated preview, without adding unwanted production scene objects. Verify that default local wiring works and required external bindings are explicit. The next level should require placement plus intentional data, not code edits in unrelated HUD/weapon/AI systems.
