# Sleeper partial-wake cut: read-only source review

Reviewed `SleeperSpriteAnimator`, its `CreatureSpriteAnimator2D` lifecycle, `SleeperModel.Closure`, `SleeperPresentationSource2D`, the focused PlayMode test `PartialWakeTargetDeathFinishesTheBodyMovementWithoutSnappingToWalk`, and capture scenario29. No shared source was changed and Unity was not called.

No new lifecycle blocker was found. The first body selection initializes from the actual target; while Unfolding, the displayed fold follows the brain's0.9s waking timeline. A later TargetDead state has an extended target but retains the last displayed fold and advances through remaining body rows with MoveTowards. ClosureSeconds is validated as finite and at least0.01s. Attack/retraction explicitly owns fold0, while base death handling preserves the displayed corpse pose before locomotion selection. OnEnable and RestoreVisualRotation reset initialization so a restored actor does not interpolate from unrelated prior presentation state.

The test's first post-cut assertion meaningfully rejects the old direct jump to row0. At the present0.24s full fold duration, the remaining half-fold completes in approximately0.12s; its20-frame60Hz deadline is sufficient. That fixed deadline assumes current tuning rather than arbitrary future closure durations. `LevelContext2D.CreateInstance` injects the scene player before Awake, so a missing explicit Configure call here does not imply a null target. The test uses a validated restored TargetDead snapshot, however; it does not independently prove the target's death event drives that transition.

Scenario29 preserves the relevant presentation history: rotation is reset only at sample0, Waking continues for30 samples, and TargetDead begins at sample30 without another visual reset. It can therefore expose the original row4→0 defect. Its actual captured frames, the PlayMode test result and gameplay target-death integration remain unexecuted evidence at this review. Source review is not runtime acceptance.

Итоговый контроль авторинга: неприменим; this review changed no scene objects or Unity resources.
