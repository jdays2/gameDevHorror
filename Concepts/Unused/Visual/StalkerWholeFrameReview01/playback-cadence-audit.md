# Read-only playback/cadence audit

2026-10-27. Local sources only. No browser, HTTP, UI, server invocation, Unity execution or production/viewer edit.

**No doubled playback/frame-advance was found.** Both viewers have one `requestAnimationFrame` chain. Their logical timeline advances once at elapsed seconds ×60×selected rate. Asynchronous draw, image decode and prefetch do not advance the clock. Close/green frames are indexed at 60Hz; gameplay frames use `floor(frame/4)`, producing the intended 15Hz image sampling without speeding the timeline.

## What is proved by files

- `Locomotion02/viewer.html`: x1 adds `(now-last)/1000*60` once. Close frame step is1, other cameras step4. The walking range100–150 is50 timeline frames =.8333 seconds, one cycle at1.2Hz. Pursuit190–230 is40 frames =.6667 seconds, one cycle at1.5Hz. Its run range250–355 does not contain an integer number of cycles, so the loop boundary can jump; that does not double elapsed playback speed.
- `Locomotion02/preview-metadata.json`: 480 frames /8 seconds, close60Hz, other scales15Hz. `serve_viewer.py` fixes its file root to its own **Locomotion02** directory and defaults to port8796. That entrypoint serves a historical rig capture, not the current runtime animator. No live server identity or cached browser state was inspected.
- `Locomotion03Sprites/viewer.html`: one `tick` adds elapsed×60×speed into a fractional carry and consumes its integer count once. The `.1`-second delta clamp can lose wall time after long delays; it cannot produce a sustained2× advance. Default walking clip0–100 lasts1.6667 seconds and contains two cycles at1.2Hz.
- Its actual `preview-timeline.json` contains480 sequential frame IDs. Independently recomputed phase-step residual against `speed/60` is at most4.9×10⁻⁸; mean walking phase rate is1.199999575 cycles/second. Frame0 starts at phase.02 because the capture advances one sample before drawing; this is an initial offset, not doubled cadence.
- `AgentScripts/PreviewStalkerSprites.cs` explicitly advances phase once by `speed/60`, and position once by the same travel. It disables ordinary behaviours during its controlled capture. `PreviewStalkerRig.cs` also manually drives a disabled rig once per1/60-second sample. Its old left comparison has `phase=distance/1.05`; this makes that historical left cycle about5% slower, not twice faster. The metadata does not certify that this old left comparison has the exact same cycle length as current runtime.

## Current runtime path

`ShadowStalker.prefab` references `StalkerSpriteLocomotion.asset` and `StalkerSprites.asset`. The actor root has scale1; the1.04 scale near the start of the prefab belongs to **Contact shadow**, not the actor. Actual scene-instance overrides were not inspected.

`CreatureSpriteAnimator2D.LateUpdate` chooses either distance-based `UpdateLocomotion` when the profile exists **or** the legacy `walkClock` branch when it does not. These paths are mutually exclusive. In the configured profile path, phase advances once by:

`translationDistance / (distancePerCycle × actorLossyScale × strideScale)`

The current distance is1 local unit/cycle; supportHold and supportYaw are0. `StalkerSpriteAnimator` reads the resulting phase; `StalkerSpriteSet.FrameIndex` only quantizes it into one of48 columns. Neither adds another phase advance. `walkFramesPerSecond=7` belongs to the fallback and does not set configured whole-sprite cadence. Random initial phase changes only starting pose. Per-leg offsets0/.5 are baked once by the authoring pose composer; runtime does not add them a second time.

At unit actor scale:

| State/formula example | World distance/cycle | Cycles/second | Seconds/cycle |
|---|---:|---:|---:|
| Open walk at1.2 units/s | 1 | 1.2 | .8333 |
| Open pursuit at1.5 units/s | 1 | 1.5 | .6667 |
| Open retreat at3.8 units/s, if open | 1 | 3.8 | .2632 |
| Current compact stride.6 at3.8 units/s | .6 | 6.3333 | .1579 |

Compact stride.6 deliberately raises cadence by1/.6≈1.667 while preserving movement speed. That is a real rate choice, not a doubled clock. The old03 controlled preview hardcodes a one-unit phase cycle even while closed and does not apply the current compact stride scale; its retreat preview therefore cannot certify current compact runtime cadence.

Each six-leg cycle includes multiple alternating leg contacts; more visible leg events do not imply multiple phase clocks. A48-column bank at1.2cycles/s can select about57.6 sprite columns/second; the original eight drawings at the same cycle rate change only9.6 columns/second. Frame count is not cycle frequency.

## Limit

The file evidence rejects a demonstrated2× scheduler bug and confirms the declared1.2Hz open-walk timeline. It cannot judge whether1.2Hz looks too fast for this particular creature, whether the current drawn stride has enough reach to match the one-unit contract, or what the user actually saw in an existing browser cache/scene instance. Check artistic tempo with a new native video after the artwork is coherent; include actual displacement, cycle count and instance scale in that capture. No timing or gameplay value was changed here.
