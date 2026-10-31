# TASK-005 · r12 · Technical QA

2026-10-31. Two existing subagents reviewed the palette-only change independently. Their checks were read-only; they did not change the Unity scene or approve the artwork for the owner.

- `scorpion_motion_qa`: JavaScript syntax and VM checks passed for the 128-entry LRU, eviction, alpha preservation and independent palette keys. Four actors share the same clip, phase and approach root; death uses four copies of the same twelve source parts. Single-palette mode, returning to comparison and the untinted Claw family passed. Audio remains independent of palette selection.
- The motion review found a stale `nextSqueak` timestamp after changing families while time resets to zero. `options()` now resets that timer to 3–5 seconds. The agent reran the case successfully; palette selection still preserves animation time, selected phase and the scheduled squeak.
- `visual_check`: all four palettes are distinguishable, though FleshPink and BlushIvory are close at night. JSON/material colors and body/remains references agree. No new silhouette holes or cropping were observed in the reviewed captures.
- Pale materials exposed existing native compression lines. Source alpha in their sampling area was zero. Raising Run/Attack import compressionQuality from 50 to 100 removed the lines in native A/B captures. The visual reviewer confirmed exact background RGB at both reported locations and no new silhouette damage in that pose. Format remains BC7; source PNGs, sprite IDs and dimensions are unchanged.

Root verification: the actual browser renders all four labeled variants in synchronized Run phase 01/24 and reports no console errors. Dark, light, night, attack, death and single Chalk views have saved screenshots. Native capture used an isolated preview scene; the authored scene remained unchanged. The actual MechanicsSandbox room was inspected with four FleshPink body references and eye intensity .12. No actors were placed, moved or saved during this task; authoring review is not applicable.

Both reviewers returned technical GO after the fixes. This is not an all-frame native recapture, performance benchmark, audio audition or owner art acceptance. Runtime behavior and Player builds were not changed or retested for this palette task. See [content checks](content-checks.json) and [native evidence](native.json).
