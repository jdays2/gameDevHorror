# C# conventions and lifetimes

Adapted from SOD `.editorconfig`, `Docs/Conventions.md`, `.cursor/rules/unity-csharp.mdc` and `memory-performance.mdc` at `1479eec1bf8dc133093be05de164b46a6078e789`.

- Use the current Unity-supported C# version, block-scoped namespaces, four spaces and Allman braces. One action per line, explicit braces and descriptive names. Expression-bodied simple properties/methods are fine. No nested ternaries.
- Namespace follows the feature/layer path; an `Interfaces` folder does not add a namespace segment. Prefer one type per matching filename. Organize large layers into cohesive subsystems; mirror them in tests.
- Private/protected fields are camelCase without underscore; types, methods, properties, events and constants PascalCase; interfaces start with I. `var` when the type is explicit on the right, otherwise explicit type. Serialized fields are private with SerializeField.
- Use System usings first and remove unused imports. Keep explicit access modifiers on class members. Change style in the touched file; do not mass-reformat unrelated code.
- Member order: constants, fields, properties, events, constructors, Unity messages, public methods, private methods. A 200–300-line file invites a cohesive split, not automatic fragmentation.
- Game input uses Input Actions. Direct device/key checks belong only to explicitly scoped development cheats. Multiple input devices should invoke the same commands.
- Every event subscription has an unsubscribe. Models use Dispose; views use their appropriate disable/destroy lifetime. Avoid anonymous subscription lambdas that cannot be removed. Cancel coroutines/async work with their owner.
- Hot-path updates avoid LINQ, allocation, boxing, string construction and capturing lambdas. Reuse buffers and collections. Cache components at initialization, Animator/Shader IDs and bounded lookup tables. Do not Find/GetComponent/Camera.main every tick.
- A cache has an owner and limit. UI text updates on changes; use TMP formatting APIs if the project uses TMP. Do not migrate an existing font/UI framework merely to adopt this guidance.
- Avoid new Resources-based loading. Package-mandated Zenject ProjectContext/DOTween settings are specific exceptions. Addressables needs paired release and an actual loading need; SOD's week-three schedule is not portable.
- Pools reset state/subscriptions on return; introduce them when the authorized feature needs repeated creation or measurements justify it. Large-world zone sleep and LOD must not disable authoritative simulation based on one local camera. Do not import a world/LOD system or reopen a deferred optimization task just by installing rules.
- Further performance changes require Profiler evidence with the observed workload. Zero allocation on a known hot path does not prove frame-rate scalability.
