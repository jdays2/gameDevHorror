# State, identity and authority

Adapted from SOD `.cursor/rules/save-data.mdc`, `network-ready.mdc` and `architecture.mdc` at `1479eec1bf8dc133093be05de164b46a6078e789`.

## Portable requirements

- Definition IDs are stable, unique strings. State refers to IDs rather than filenames or asset names. Validate duplicates and empty IDs. A rename preserves identity.
- Separate DefinitionId, EntityId/ItemInstanceId and PlayerId. Persistent IDs are not GetInstanceID, GetHashCode or a collection index. Dynamic identity needs a persisted allocator/namespace and creation count when that system is implemented.
- Serializable DTOs contain values and IDs, no MonoBehaviour, View, Rigidbody or live Unity Object. Keep a schema version. A format change increases version, supplies migration from supported old data and tests that migration. Do not silently reinterpret incompatible payloads.
- Simulation owns damage, health, loot, inventory, AI and other gameplay state. Input and UI submit intent; commands validate target, player ownership and resources before application. Local single-player follows the same validation path.
- Per-player state is separate even when the initial local session has one player. UI's local-player selection is not authority. Do not add unused network transport or promise multiplayer readiness from this separation alone.
- Clock/step and gameplay RNG enter the model through interfaces. Do not read Time.deltaTime, UnityEngine.Random or local camera visibility inside new domain rules. Cosmetic random choices use a separate stream. Persist gameplay RNG state when reproducible save/restore requires it.
- Animation/FX is presentation. Domain timing lives in rules/configs; a clip completion cannot gate authoritative damage or death. Save restoration must not replay a death or duplicate effects.
- Network SDK types and RPCs, if later authorized, remain in adapters. Transform writes are owned by the controller/motor. State and rules must work without a rendered local player.

## SOD-specific decisions

The source repository chooses a local authoritative host, future Steam co-op 2–4, distant dedicated PvE 12–24, Mirror, no gameplay world pause and host-only saves. Those are SOD product decisions. Installing this skill does not switch another project to that product design, replace its approved pause/checkpoint policy or add multiplayer. Read that project's SPEC/decisions before applying those choices. Preserve domain boundaries now; agree actual networking, clock, pause and save-trigger changes within a feature task.
