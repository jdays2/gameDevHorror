# MVC, configuration and dependency boundaries

Adapted from SOD `.cursor/rules/architecture.mdc` at `1479eec1bf8dc133093be05de164b46a6078e789`.

## Layers

- `Logic/`: domain models, rules and serializable state. Pure C#, no MonoBehaviour. Unity math value types are permitted.
- `Logic/Controllers/`: translates intent to commands; presents model changes through interfaces. Model does not reference View or Controller. A presenter may depend on a presentation interface.
- `View/`: MonoBehaviour presentation, animation and audio. Input adapter reports intent. No decisions about damage, resources or whether an action is legal.
- Environment/motor adapter: Unity physics and Transform access, independent of camera, rendering and input. Only an actor's controller/motor writes its pose.
- Config and installer compose a feature; cross-feature communication uses interfaces/events. Do not turn one species' state enum into a common-system dependency.

One responsibility per class. Extend different algorithms with new implementations/strategies; identical behavior with different numbers remains one implementation plus data. Prefer composition; inheritance depth at most two. State switches inside a finite state machine are allowed; type switches selecting species behavior in unrelated consumers are not.

Two constructor dependencies are preferred, three exceptionally. Installers are exempt. Facades/coordinators can have five if they only delegate. Do not package dependencies into a context or service locator merely to satisfy the limit.

## Configs and commands

Tuning numbers belong to thematic SO configs. A config describes one entity/rule. An item owns its effect numbers; a tick rule owns its interval and changes; the affected stat definition does not duplicate those numbers. Same behavior uses one class and a config containing rows, not a separate class/asset per target.

SO assets are immutable in runtime. HP, ammo, aggro, selected target, elapsed intervals and session progress belong to an instance. Scene placement, patrol routes and intentional spawn overrides remain authored instance data. Mathematical invariants are named constants, not duplicated designer tuning.

An external action creates a command, validates `CanExecute` and applies it. Validation must also happen at the simulation boundary; UI disabling is insufficient. Ordinary rejection returns Result. Automatic rules advance their own owned state on their tick; do not create a command for every internal arithmetic operation.

State changes emit events instead of consumers polling each frame. Tick rules read the IDs explicitly referenced by their config, not all world objects. A cosmetic animation event does not authorize damage, death or respawn.

## Composition

Use Zenject/Extenject for target composition, constructor injection in models/controllers, feature installers and explicit lifetimes. No singleton player, static mutable state, FindObjectOfType, GameObject.Find or SendMessage for new gameplay dependencies. An immutable constant/static pure helper is not mutable global state.

World services may be single within the world scope. Personal models live in their own player scope, not a scene-wide singleton. Release subscriptions and owned asynchronous work when that scope ends. If no container is installed, stage its integration and legacy adapter boundary explicitly; do not silently add an alternative service locator or claim migration complete.

## Validation

Models/controllers have EditMode tests without a scene. Test command rejection and application, instance isolation, ownership and lifecycle transitions. New numeric variants should require data rather than edits to weapons, UI, audio and persistence consumers. Record the distinction between the target rules and actual legacy debt.
