# Independent actual Stalker dense-edge review

PASS for the recorded green presentation of scenarios **01 idle, 27 attack → hit, and 28 attack → light fear**, across all nine authored Stalker source configurations. No actionable pose jump, mouth pop, detached seam, transparent joint core, clipped limb tip or body-origin jump was found in the actual images reviewed.

Source: `Concepts/Used/Stalker/Postures05/Scenarios/stalker-dense-edges`. Run SHA-256: `857e2d9db4b81839001e3aea4c24cc534030a2a83a55c7ef84a31e378a8f0362`. The run contains 1,584 saved 640 × 640 PNGs, 176 per configuration, sampled at 60 Hz. All 1,584 frames are mapped in `review-coverage.json` with original PNG hashes, decoded full RGB hashes, frame facts, authored configuration and exact aliases.

All **504 distinct full RGB images** were actually inspected through `view_image(detail="original")` and `image(..., "original")` on 42 compact pages. Another 21 pages show 243 ordered transition context instances covering the cuts, bridge endpoints, start of folding and phase wrap separately for all nine configurations. All 63 pages were displayed at their original 1,392 × 1,326 size without a resize notice. The fixed crop `(168,120)-(504,528)` copies 336 × 408 capture pixels at native 1× without scaling, normalization, rotation or filtering. It contains every non-green pixel: the total foreground union is `(177,130)-(492,516)`. Numerical checks supplement this visual inspection.

The remaining 1,080 frame instances are exact full RGB duplicates. `finalize_review.py` directly compares decoded RGB bytes for every instance against its representative, verifies all 1,584 original PNG hashes, all nine source manifests, the run and all 63 page hashes. Approximate color or pose equality is never used. Per-profile ordering is preserved even where the displayed pixels are identical.

## Observed transitions

- **First cut:** all nine hit cuts `F066 → F067` and light-fear cuts `F133 → F134` retain `strike-10`, fold zero, the visible open mouth, front pair and four supports. The previous instantaneous attack-to-body replacement is absent. Camera RGB is not universally identical: hit cut P0 changes 8,664 pixels by at most 9/255 per channel; P4 changes 16 pixels by at most 27/255; other hit cuts change one pixel by 1/255. These are visible-lighting/eye-detail differences with no observed limb reset. All nine fear-cut RGB pairs are byte-identical. Full measurements are in `firstCuts`; the selected-sprite hold is not presented as universal camera-byte equality.
- **Retraction:** hit case selects `strike-12,14,16,18,20,22` at F068–F073, then return samples **00–13** at **F074–F087**, and current-phase walk at **F088**. Fear case repeats that progression at F135–F140, return **00–13** at **F141–F154**, and walk at **F155**. Each of the 14 intermediate bridge samples appears once, in order. Mouth closure and redistribution of the legs are progressive in the actual images. No former eight-sample bridge skip or abrupt four-support reset was observed.
- **Moving phase:** return columns follow the current locomotion phase. P6, the Morgue definition, has a distinct progression and crosses the phase wrap during fear return; its endpoint is `body-0-18` at F155, rather than the default `body-0-25`. Both joins remain visually coherent. Authored world scale and body local transform remain stable. Trimmed sprite bounds change with the pose; a fixed bounds centroid is not an acceptance requirement.
- **Fear body and eye:** eye closure starts at F143 and reaches one at F146 while the attack retracts with fold zero. Body remains unfolded at F155–F156. First positive fold is **F157** (`0.06944445`), after the front pair has returned, and the requested half-folded posture reaches `0.5` at F164. The folded body is visibly distinct; the reaction does not consist only of a shut eye. Turning and locomotion phase progression remain coherent through the wrap.
- **Art/bounds:** joints remain attached with opaque cores; the actual green background does not break through the limb shafts or body connections. Sharp tips remain intact within the capture and crop. Overlap changes as legs move, without a newly exposed chopped end or separated joint. No actionable seam or clipping defect was found in idle, the dense interruption sequence or the subsequent fold.

## Source configurations

| Page profile | Scene | Authored variant |
|---|---|---|
| P0 | MechanicsSandbox | 20 HP stationary target |
| P1 | MechanicsSandbox | 2 HP stationary target |
| P2 | MechanicsSandbox | 6 HP stationary target |
| P3 | MechanicsSandbox | Default stationary target |
| P4 | MechanicsSandbox | Default moving Stalker |
| P5 | MorgueRouteTest | Default moving Stalker |
| P6 | MorgueRouteTest | Morgue definition moving Stalker |
| P7 | BasementPrototype | Basement light profile moving Stalker |
| P8 | BasementPrototype | Default light profile moving Stalker |

The manifest retains definition/profile GUIDs, attacks enabled, Rigidbody constraints, Crowd setting, visual scales, tint, material and body/attack sets. Stationary actors use forced presentation snapshots; source route/checkpoint mechanics are excluded on the disposable presentation clone. This review therefore establishes the recorded animator presentation for these three cases. It does not claim AI transition testing, acceptance of every scenario in the full run, scene dark/light acceptance or owner approval of the art.

No Unity calls or production edits were made for this review. Final authoring: N/A; no scene objects were added or moved.
