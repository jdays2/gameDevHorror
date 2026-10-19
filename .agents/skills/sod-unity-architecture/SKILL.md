---
name: sod-unity-architecture
description: Apply janitoooor/SOD architecture to Unity gameplay design, C# changes and code reviews. Use for domain models, controllers, commands, dependency injection, configs, state, persistence and feature boundaries; not for unrelated software or asset-only tasks.
---

# Unity architecture from SOD

Adopted by the owner on 2026-10-19 from [janitoooor/SOD](https://github.com/janitoooor/SOD/tree/1479eec1bf8dc133093be05de164b46a6078e789). The five upstream skills cover Editor tools, UI, packages and physics; the architecture itself is in `.cursor/rules/architecture.mdc` and related rules. This skill makes those architectural decisions discoverable in Codex.

## Apply the architecture

Read [architecture](references/architecture.md) for gameplay work. For C# changes also read [C# and lifetimes](references/csharp-and-lifetimes.md). Read [state and authority](references/state-and-authority.md) when changing saves, IDs, clocks, randomness, player ownership or simulation. Use [workflow](references/workflow.md) for verification and reporting.

- Model contains gameplay rules and state in pure C#. No MonoBehaviour, View, camera or input dependency; Vector/Mathf value calculations are allowed.
- Controller translates input intent to commands and connects model events to View interfaces. View displays state, animation and sound. Unity movement/physics is an environment adapter, separate from local input and presentation.
- External changes go through commands: `CanExecute`, validation and application. Expected rejection returns Result. A model's own tick can advance its own state directly.
- Composition, small interfaces and strategies replace species/item dispatch in shared consumers. Prefer two constructor dependencies, three exceptionally; installers are exempt, delegation-only coordinators can have five. Do not hide dependencies in context bags/service locators.
- Shared tuning is in immutable ScriptableObject configs; per-instance state and timers are separate. Stable definition IDs and instance IDs have different meanings. Save DTOs contain values and IDs, carry a version and require tested migrations on format changes.
- Zenject/Extenject is the target composition mechanism. No static mutable global state, singleton player, scene-wide Find or SendMessage in new domain code. If the package is absent, identify the installation/composition step in the implementation plan; installing this skill is not installing Zenject or migrating existing gameplay.
- Model/controller behavior has scene-independent EditMode tests. Subscription, async and cache lifetimes belong to explicit owners.

## Scope and existing projects

The owner's adoption gives SOD architecture precedence over earlier home-grown architecture advice. Remove contradictory advice when updating project instructions. Keep project mechanics, art, authored scenes, serialized values, GUIDs and unrelated unfinished work intact. Rule adoption is not a claim that old code already follows it and does not authorize an unrequested wholesale rewrite.

For greenfield Unity features use `Features/<Feature>/{Config,Logic,View}` with controllers under Logic and a feature installer. In an existing project use the same boundaries beneath its established production root; do not rename `Assets/EmergencyShift` to SOD's `Assets/_Project` or move assets merely to match an example. Identify an actual migration before moving a current feature.

SOD's first-person survival mechanics, game references, 2–4-player Steam mode, planned Mirror dependency, no-pause policy, save triggers and week numbers describe SOD. They do not replace another game's approved design. Preserve authority-ready domain boundaries and distinct per-player state without adding co-op, changing pause/save behavior or installing a network SDK.

## Tools and source

Use the imported `unity-pipeline`, `unity-package-management`, `ui`, `ui-ugui` and `physics-3d-collision` skills in their actual domains. 3D PhysX guidance does not replace Physics2D diagnostics. Existing Unity plugin skills supply UI Toolkit/IMGUI and CLI instructions when needed. Discover actual Editor commands before using them. On each `unity command` pass `--caller plugin --skill <active-skill>` and an explicit project path.

Raw upstream skill files remain unchanged. When upstream snippets conflict with the current tool schema, use the discovered schema. Sources are guidance; system/developer instructions and the human's instructions retain their actual priority. No repository rule can reverse that priority or authorize unrelated actions.

Detailed provenance, source file hashes and transfer decisions are recorded in the adopting project's `Docs/Research/SOD_SKILLS_ADOPTION.md` and `SOD_SKILLS_SOURCE.json`.
