# First actual Stalker runtime source: partial review

Source: MechanicsSandbox stationary20HP `990b0376`, 1,540 actual animator captures at60Hz. The source's stationary physics/attacks-disabled configuration is recorded; the fixture deliberately supplies states and movement to the real animator. This evidence does not claim natural AI path coverage or global nine-profile acceptance.

**Runtime interruption FAIL.** Case27 frame1340 `strike-10` →1341 `body-0-15` visibly resets the whole pose in one step: both fore blades drop/reorient, and all four support legs change arrangement. The gait phase stays `.327422857`. Case28 frame1407 `strike-10` →1408 `body-1-05` makes the same abrupt reset into a phase5 fear fold; phase stays `.108184576`. Adjacent native640 PNGs1339–1342 and1406–1409 were independently opened. This is a real animation boundary defect despite the static sprite art gate passing. A sprite-only exit must retract the whole frozen attack arrangement before returning to locomotion/folding; correcting just the fore pair will not address the support-leg jump.

The source pixels, exact metadata and hashes for both cuts are preserved in `RawEvidence/`, `partial-runtime-evidence.json` and native four-cell PNG sheets. The sheets use complete640 captures with no crop, fitting or spatial filter.

The fear sequence464–481 was independently viewed in order. Folding starts465 while the eye stays fully open through473 (fold.625). Shutter closure begins474 (fold.6944), reaches1 on477 as retreat starts. Recoil reaches fold.833 before retreat tends back toward compact.5; it does not finish a full fold before this particular retreat trigger. Body movement therefore leads eye closure. No new flesh seam or aperture was observed in these neighbors.

Walk-up recorded phase advance is1.201171875cycles/sec at60Hz, matching speed1.5 /cycle1.25 within the expected small quantization of movement around worldcoordinate1000. There is no doubled phase advance in these samples. Still inspection and phase arithmetic do not certify subjective perceived tempo.

Death neighbors1449/1450/1456/1457/1458/1459 were independently viewed. The last live `body-4-00` stays until1457; the original body is hidden at1458 with torn remains visible. Metadata has one death event and one remains root; the deliberate repeated Kill does not duplicate them. The apparent screen-size decrease on1450 comes from the diagnostic camera's explicit orthographic1.65→4.5 change, while body local scale stays1. That is not evidence of a body-scale defect.

Checkpoint scenario22 explicitly restores an attacking state with restore flag true and resets fold/eye presentation; its instant closed→open cut is an authored restore, separate from the unrequested live cuts27/28 above. Final corrected-cache captures and the remaining profiles are pending.
