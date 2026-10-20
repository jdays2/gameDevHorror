# Working and verifying with the SOD approach

Adapted from SOD `.cursor/rules/workflow.mdc`, `docs-sync.mdc`, `git-commits.mdc`, `unity-cli.mdc` and `assets-animation.mdc` at `1479eec1bf8dc133093be05de164b46a6078e789`.

Read the project's documentation index and current plan, then only task-relevant sections. Emergency Shift keeps active plans/tasks/bugs/changelog in Notion: follow Docs/NOTION_WORKFLOW.md and .agents/notion-project.json before work and when recording results; local SPEC/Features remain the technical contract. Preserve manual plan edits and unfinished work. Roadmaps describe direction; unresolved gameplay choices require the owner, while routine technical choices and already authorized work continue. Do not introduce repeated approval for an action the human already requested.

Capture Git status and inspect the touched diff before changing files. Keep changes reviewable and scoped. Do not modify vendored third-party sources; prefer project variants/adapters. Preserve licenses and Unity meta GUIDs. Do not replace a user-authored scene with a generator.

Compile and read actual console errors after code changes. Use the connected Editor and discover commands rather than guessing. Scene/prefab/SO edits use Editor APIs; eval is for diagnosis. Follow the current CLI schema and pass explicit project targeting and caller/skill flags. No second Editor against the same open project.

Models/controllers use scene-independent EditMode tests. A normal Play smoke reads console then stops; it is not evidence of visual quality, input feel or full behavior. Do not invent automatic player simulations to duplicate an easy manual check. More extensive automation should address a difficult reproducible case or explicit user request. Never claim the author's manual playtest passed from an automated smoke.

Synchronize the affected canonical feature/architecture/plan document and known limitations. Search old values when a contract changes. Use the adopting project's document names and tracking IDs rather than copying SOD's weeks, Backlog or Questions files into it. Record deviations and future migrations explicitly.

Use Conventional Commits in English. Documentation/rules can be committed after diff review and whitespace checks. Code/asset commits require the relevant tests and the human's authorization; existing session authorization counts. Never include unrelated unfinished files. Push only with the human's standing or current authorization. Screenshots are optional temporary evidence, not mandatory committed artifacts.

SOD's game-design references, first-person animation requirements, fixed development weeks and particular UI scenarios remain source-project choices, not generic instructions for every Unity game. Communicate findings and limitations concisely in the user's language.
