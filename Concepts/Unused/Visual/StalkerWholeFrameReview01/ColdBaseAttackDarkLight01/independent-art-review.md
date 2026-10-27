# Sleeper base attack: independent DARK/LIGHT native review

Reviewed on 2026-10-27. Scope: the **base 792** frames in `Concepts/Used/Sleeper/Postures05/Raw`, described by `attack-composition.json` and `attack-sprites.json`. The adjacent-frame family is outside this report.

**Result:** complete independent visual pass; no actionable defect found in the cold skin/blood/mouth material, limb junctions, transparency or framing of these base drawings. This is an agent review, not the owner's artistic approval or a runtime acceptance result.

## Actual visual coverage

- All 80 pages (`dark/page-000.png`–`page-039.png`, and the corresponding `light/` pages) were actually viewed through `view_image(detail="original")`, emitted as `image(detail="original")`. No resize notice was returned. Each page is 1304 × 1467 pixels, with native 1:1 drawing pixels and no resampling.
- Each background contains all 468 distinct complete RGBA images. The 324 remaining source names are exact full-image RGBA duplicates, individually checked against their representative. Similar-looking drawings with even one different byte remain separate.
- All `strike-00`–`strike-23` were explicitly displayed and reviewed on both backgrounds, including three byte-identical repeated strike drawings. Therefore there are 471 displayed tiles per background, covering 792 source names per background (1584 name/background cases).
- The fixed source crop `(132,63,446,520)` contains the entire nonzero-alpha union `(138,69,440,514)`, with a six-pixel margin. It does not crop off a tip, joint, mouth edge or other visible drawing. Cell payloads were compared against the source-over composite pixel by pixel.
- DARK is RGB `(18,22,26)`; LIGHT is `(235,235,235)`. Pages add only an opaque background, labels and spacing. Source PNGs, atlases, Unity assets and runtime code were not edited.

## Observations from the images

The grey-green skin retains a consistent surface and highlight pattern as the limbs change angle. The red blood areas stay on the same fore-left, middle-right and hind-left distal surfaces; no palette flash, tinted rectangular patch or detached blood fragment was apparent. The subtle red seams on the body remain coherent.

Joint surfaces remain bounded and connected to the limb segments. The light background exposes the thin dark native outlines; the dark background exposes pale edge contamination. I did not observe a broad light matte, a transparent joint core, detached edge slivers or a dark cut through a connected limb. Rounded joint highlights are visible surface shading rather than a missing core.

Every explicit strike drawing was inspected. The mouth opens from the small closed cap into a connected grey-green rim, red inner tissue and dark cavity, then closes back to the same chamber pose. No floating mouth rim or detached tooth, abrupt grey/pink palette substitution or residual open hole was seen in the closed endpoints. The eye and body stay registered beneath the mouth throughout the sequence.

Entry phases 00–47 and every distinct return drawing were inspected. The six limb chains remain legible and connected; the tips remain sharp and complete. The opposing hind tips approach one another in the tighter phases around 12–18 and 36–39, but remain distinguishable in these drawings rather than merging into a chopped shape. No frame-specific wrong-side limb splice was apparent. The seven previously documented near-alias pairs with byte differences were retained and viewed independently, not substituted with warm or canonical images.

## Evidence and limits

`review-coverage.json` contains the source PNG hashes, three source manifest hashes, the exact-RGBA representative/alias map, every page hash, per-background name coverage and the manual viewing batches. `completion-hashes.json` records the final report/coverage/script hashes and the final source/page recheck. Source dependencies and all 792 source PNGs remained unchanged through completion.

This is a review of the actual cold raster drawings on flat DARK/LIGHT backgrounds. It does not establish attack cadence, selection order, animation interruption behavior, imported atlas rendering or scene lighting. Those remain separate runtime/green/scene checks. Numerical alpha and byte checks establish coverage and provenance; the visual observations above came from viewing the pages.

Final authoring control: not applicable; no scene objects were added, moved or changed.
