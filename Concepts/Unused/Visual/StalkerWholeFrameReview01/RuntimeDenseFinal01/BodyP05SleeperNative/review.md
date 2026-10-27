# Current Stalker body p05: native green runtime image review

2026-10-27, reviewer `sleeper_native`. All 49 assigned `current-body-p05-000.png` through `current-body-p05-048.png` pages were actually viewed at original detail, native 1× without fit, filtering, resampling or resize notices. They show 291 source frames from `MechanicsSandbox-stalker-default-default-live-a05a489a`. This review uses the current rendered RGB and does not inherit an earlier material verdict.

Result: no actionable visual defect found in this assigned set. The eye remains registered with the body through changes in direction, ordinary walking, stopping and teleport fixture410. Skin, blood and knee surfaces stay connected with complete contours; no new transparent strip, detached tissue, clipped tip or wrong-side splice was seen.

Natural recoil465–476 shows body folding before the eye shuts: the body is already moving inward while the eye remains open, then the half/slit states appear. Full waiting547+ and1052+ retains the compact body with tucked whole limbs and complete blade tips. Recovery630+ and1083+ progressively opens the limbs and then the eye; it does not leave an open body represented only by a closed eye. The displayed body/attack boundary samples remain registered; the full attacks are reviewed separately.

Fixture boundaries were checked against source facts and `CaptureCreatureScenarios05.cs`. Frame690 is an explicit restored compact turn. Waiting preparations1038 and1196 directly assign `Closure=1`, so their immediate eye change is outside the natural recoil timing test. Closed1225 to extended1226 is an explicit checkpoint restore with `RestoreRotation=true`; it is recorded as a fixture cut, not misclassified as a natural recovery or an art seam.

[actual-body-p05-review.json](actual-body-p05-review.json) records every viewed page SHA, all291 source frame PNG/RGB hashes and facts, actual viewing batches, exact crop comparisons and the scope limits. Every512×512 image payload equals its source crop `(64,64,576,576)` byte for byte; the crop contains every non-green source pixel. The source profile manifest and all49 page hashes were rechecked at completion.

This is a review of the assigned current runtime images. Other profiles, complete attacks and death pages belong to the other reviewers. This report does not establish the owner's artistic approval, full real-time cadence acceptance or Sleeper runtime acceptance. No Unity, production source, atlas, prefab or scene was edited; the shared page coverage file was left untouched.
