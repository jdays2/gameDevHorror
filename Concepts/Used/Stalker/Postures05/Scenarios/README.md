# Real sprite animator scenarios

The capture tool clones actual authored actors through their level factory, restores validated
actor snapshots, and invokes the production `CreatureSpriteAnimator2D.LateUpdate` once for each
sample. Neither the tool nor the image processor chooses a body sprite or eye frame. Selected
sprite subassets, animation index, gait phase, body fold, eye closure, state, velocity, rotation,
scale and bounds are recorded for every frame. These are controlled presentation diagnostics;
AI transition mechanics are covered by separate Play Mode regressions.

The diagnostic uses neutral full light, exact green, a fixed 640×640 orthographic camera, and
60 Hz samples. Ground shadows are hidden to expose silhouette defects. Body, iris, pupil,
materials, renderer colours, proportions and species presentation adapters are the actual
instance resources. The camera follows position while preserving the world rotation and scale.

MechanicsSandbox, MorgueRouteTest and BasementPrototype are loaded into disposable Play copies. Existing Play roots are temporarily
disabled so their Physics2D walls and global URP light cannot contaminate the copied map. Every
temporary scene is unloaded and prior root activity, active scene and timing are restored in
`finally`. No map, prefab, profile or production asset is saved by this tool.

Source selection uses the full read-only preservation audit: 50 authored Stalkers, including
inactive actors, and three authored Sleepers across the three maps. One real Stalker represents
each presentation configuration, without an Alpha-name heuristic. Definition and behaviour
profile GUIDs, attacks enabled, Rigidbody constraints/simulation, Crowd enabled, animator/set/
locomotion/material references, body tint and actor/visual/body scales define the grouping key.
Scales and tint are rounded to five decimal places for grouping; the source manifest retains
the exact raw authored metadata, selected hierarchy/GlobalObjectId and every group member.
Output folders combine definition/profile labels, mobility and a configuration hash.

The current nine Stalker source groups are:

- MechanicsSandbox: default Stalkers at 1.5 units/s split into live and stationary configurations,
  plus three separate stationary definitions (2/6/20 HP), each with attacks disabled and
  FreezeAll. Crowd remains enabled.
- MorgueRouteTest: 27 Stalkers using its 1.2 units/s definition, plus the 1.5 units/s default
  definition on Stalker_Intro. Both use the default light behaviour profile.
- BasementPrototype: ten default-definition Stalkers with the Basement light profile and two
  with the default light profile, all at 1.5 units/s. The profiles have different reaction delays
  (.33/.45 seconds), so their GUIDs remain distinct even though their current art is shared.

All actor/visual/body scales are approximately one and body tint is white; the audit retains
the original floating-point values. Sandbox's live and stationary Sleepers remain separate,
and Basement's Sleeper is the third individual source. No map supplies an authored Alpha
definition. Retreat uses 3.8; Sleeper lunge uses 6. Forced walking/attack states on stationary
sources are presentation diagnostics, while the original attacks/physics/Crowd flags stay in
the source metadata. They do not prove stationary targets can perform those actions in play.

An authored source may own `ScriptedStalkerRoute2D`, notably Morgue's Stalker_Intro. That
component is removed only from its disposable clone, immediately after factory instantiation
and before snapshot calls; its disposal releases the clone's scripted control and subscriptions.
The authored source retains its route. Each manifest records `authoredRoutePresent` and
`fixtureRouteRemoved`, and the clone is checked for missing scripts/references. This fixture
excludes scripted route and checkpoint mechanics; it neither relaxes the production snapshot
guard nor establishes a route/AI regression result.

Coverage includes idle/sleep, wake, both travel directions, 90/180 degree turns, start/stop,
teleport, recoil while the eye remains open, retreat, full fold, unfolding, compact restore,
attack windup/strike/end, walk↔attack boundaries, earliest valid close attack after unfolding,
checkpoint cuts into attack, wake→near attack, stagger, target death and the entire death
sprite timeline. The first deterministic gait phase is 0.25; subsequent movement uses the real
animator's distance-based phase update and teleport rejection.

For the final death scenario, `EnemyHealth2D.Kill()` is called at the start and repeated once
while already dead. This invokes the real death/Gore event path; event count, attempted kills,
remains root count and visibility of the replaced original body are recorded. Blood pools,
rupture mist and independent moving fragments remain active. The final death camera widens
from 1.65 to 4.5 world units so flying remains stay visible; each sample records that camera
size and numeric area ratios compensate for this diagnostic camera change. The historical
before run used silent restored death sprites, so its `Death02→Death03` body loss alone cannot
be classified as a gameplay visual defect without this real Gore handoff check.

Run `CaptureCreatureScenarios05.Capture("before")` or `Capture("final")` in Play Mode, then stop
Play. `LightingSmoke()` captures only two samples per actual source for setup validation.
`StalkerReview()` captures all nine Stalker configuration representatives into `stalker-dense-final`;
`SleeperReview()` captures all three Sleepers into `sleeper-final`. `GoreProbe()` uses the
last live Waiting/full-fold or Sleeping pose immediately before a real Kill and writes
`gore-current`; `GoreFinalProbe()` captures all twelve sources into `gore-final`, preserving the
historical six-source baseline. Every public entry resets its mode before applying its own
options, so a preceding species review or lighting smoke cannot narrow the Gore run.
`InterruptionReview()` captures idle and cases 27/28 for all nine Stalker configurations into
`stalker-dense-edges`; it is a bounded check of the actual attack cancellation adapter,
not a replacement for the full scenario suite. The pre-fix full run remains in
`stalker-interruption-before`. The completed Bridge8 correction capture remains in
`stalker-interruption-after`, and its subsequent full suite in `stalker-final`; neither is
overwritten by the dense Bridge15 replay. `SleeperEdgeReview()` includes partial waking cut
by target death (case29), together with idle and attack interruption, in `sleeper-edges`.
`process_scenarios.py before` (or `final`) generates all consecutive-frame contact sheets,
boundary strips, MP4s and numeric review candidates. Full PNGs and complete-sheet caches are
ignored by Git; manifests, videos and selected boundary evidence remain reviewable. Numeric
flags are not visual approval.

The initial development probe had dark lighting and setup console errors from overlapping
additive scenes. Those frames were overwritten by the correct neutral-lit `before` run. The
subsequent `final` run must use the fixed root isolation and confirm no new capture errors;
the original diagnostic console errors are not production gameplay failures.

The subsequent real Gore baseline (`gore-current`) completed 1,080 samples across all six
sources, with zero capture errors. It proves the original defect: `Death00` replaces the
folded/sleeping whole frame for eight samples before the body is hidden on sample 8.
Two Kill calls produce one Died event and one remains root in every source. Contact sheets
`death-first-frames.jpg` and `actual-gore-review.json` preserve this evidence. This baseline
used a 2.8 death camera; some distant flying blood clips its edge, independently of the
body-frame defect. The final diagnostic widens that framing to 4.5. The view correction must
be verified with `gore-final` before it is declared fixed.

`verify_transitions.py stalker-dense-final --require-current-coverage --require-exact-metadata`
and the equivalent `sleeper-final` command check the final capture manifests and PNGs.
These captures completed: `stalker-dense-final`9×1540=13,860 and `sleeper-final`3×1365=4095, both with captureErrors0 and strict exact diagnostics PASS. The independent native visual reports are indexed by `Concepts/Unused/Visual/StalkerWholeFrameReview01`. Full cold visual review found9 natural/restore wake frames with an open white iris but a disabled pupil. A subsequent one-line shared eye fix uses the actual discrete lid frame for pupil visibility. `EyeThresholdReview()` captures natural boundaries and a14-sample threshold matrix across all12 configurations into `eye-threshold-final`(2328 samples); `EyeThresholdFollowup()` records50×12=600 samples including36 open samples and a right-to-left target change. Both new strict exact reports pass. These bounded eye runs supplement the full body/temporal review; the older cold eye frames are not retroactively labelled clean.
They require the current interruption cases 27/28 and Sleeper partial-wake case29 where applicable and compare recorded
sprite rect, pixel pivot and PPU with the species' body/attack packing manifests. A trimmed
sprite need not have a normalized pivot of .5: its recorded pixel pivot must match the
fixed 576-pixel canvas recipe. Live body-local position, scale and rotation must stay stable;
rotation of the parent visual remains legitimate.

The verifier records native-pixel neighbor strips at state boundaries and the largest
silhouette changes in `TransitionReview/`. For image comparisons it removes the visual's
recorded world rotation, preventing a normal turn from being called a pivot jump.
Silhouette XOR/IoU, central image changes and fold steps are review candidates. They do not
prove an artistic defect or approval. Exact failures cover missing samples, live camera-edge
contact/render loss, import metadata mismatch and changed body-local transforms. Source
tissue already cut before rendering still requires direct frame inspection. Dead frames use
the separate `verify_gore.py` contract and are excluded from these image comparisons.

Historical captures lack exact import/transform metadata and some lack the completed-run
flag. The script still produces candidate evidence, but those runs cannot satisfy the final
strict command. `--profile <name>` narrows a diagnostic run and `--no-strips` writes only JSON.
The report hashes the manifest, ordered source PNGs and packing manifests so a later asset
iteration does not silently inherit prior acceptance evidence. Cases 27/28 are controlled
state cuts; their names do not establish that a real hit/light/AI transition was exercised.

After the disk filled, only completed historical full-frame/AllFrames caches were pruned,
with their videos, manifests, ordered hashes, reports and selected native evidence retained.
`reproducible-cache-cleanup.json` records the exact scope. A fresh pixel verification of those
pruned runs requires recapture; their existing reports must not be presented as a new run.
