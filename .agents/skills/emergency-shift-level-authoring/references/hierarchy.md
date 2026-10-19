# Hierarchy: location first, responsibility inside

Use a small, recognizable root set. For a new scene the following is the default pattern; retain an existing equivalent arrangement when it is already clear.

```text
00_LevelRig                    player, camera, level composition
10_World
  R01_Morgue
    Geometry                   floor, walls, shared-boundary ownership explicit
    Props                      reusable furniture/equipment instances
    Dressing                   markings, blood/drag traces, local remains decor
    LightingAudio              local fixtures, sources and ambience
    Gameplay                   door instances, pickups, authored enemy spawns
  C01_ServiceCorridor
    ...only the groups needed here...
20_LevelGameplay               encounters, objectives and cross-zone links
30_UI                          existing Canvas and one compatible EventSystem
90_Development                 explicitly temporary/instrumented tools, if needed
```

Room/zone codes are identifiers for human orientation, not a new save-ID algorithm. A door between rooms has one owner; do not duplicate it or its blocking wall in both room groups. A shared corridor/wall strip can have one explicit SharedGeometry owner. Do not create empty categories just to fill the diagram.

Prefer 2–4 useful levels of nesting; another wrapper is justified by a meaningful shared operation, not by symmetry. Room children should be understandable when selected alone. Name instances by function, adding a meaningful role/location suffix when needed, for example `MedicalTrolley_CleanSupplies`, not a chain of `(1) (2) Copy` or `New latest final` names. Numeric root prefixes express ordering, not chronology. Existing names used by tools must be migrated with those tools or retained.

Grouping roots normally have identity rotation/scale. Keep colliders/lights/audio within the functional prefab so moving the visible object also moves its mechanics. Do not put individual meshes/sprites in a different distant branch from their collider just to make the list look tidy. Do not use inactive parent groups as improvised encounter logic: activation belongs to an explicit encounter/feature contract.

Readability checks: can the owner select all props of one room, find its doors/loot, identify temporary content and locate a source of light/audio with one or two expansions? Can one room or assembly move without pulling unrelated geometry or losing scene links? If not, revise the grouping plan before mutation.

Keep logical function in the spatial layout: storage serves a work area; a cart stands where it could be used; exits and paths remain readable. Vary arrangement for a reason, with restrained handling history. Do not hide required pickups in decorative clutter or imply interactivity solely from an accidental glow.
