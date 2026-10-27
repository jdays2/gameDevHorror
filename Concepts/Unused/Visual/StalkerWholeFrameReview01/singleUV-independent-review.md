# Independent single-UV review

2026-10-27. Evidence only. Candidate painter, production assets/code and Unity were not executed or modified. New diagnostic files stay in this review folder. No artistic acceptance is claimed.

## First control: row 7, phase 0

Inputs actually viewed: `SingleUVStudy/single-control-green.png`, its raw RGBA output, `two-joints-4x-nearest.png`, the original native Walk00 and Recoil03 drawings, and my six-joint native comparisons. I read the prototype to understand the source and pose; I did not execute it. `singleUV-independent-crop-manifest.json` records the candidate hash and crop positions.

The new method visibly improves the earlier parts-over control: the two displayed LF/RM joints no longer carry an obvious translucent cut seam, and the six-joint comparison preserves more of the native joint transition. I see one connected skin surface at this pose, no detached random spur and no canvas-edge cut. This observation is limited to this control.

It is not an artistic pass. The full compact silhouette still reads as four nearly horizontal blades plus two small inward hooks, with little of the layered, soft folded mass visible in complete native Recoil03. The rig pose need not exactly copy that native pose, but connected alpha alone cannot establish that this is a convincing creature folding its legs.

Specific areas to carry into held-out review:

| Area | Visible concern at row 7/phase 0 | What a later frame must establish |
|---|---|---|
| LR / lower-left | Distal red/white tissue becomes a short horizontal tab beside the large round knee. The transition is abrupt compared with the longer native distal skin. | Preserve a coherent volume/attachment while the blade turns, not merely an opaque disk and a skinny tab. |
| RR / lower-right | The upper pale shaft edge is sharp; the shaft narrows where it enters under the pink round joint. This is the most noticeable lower-joint pinch. | Avoid a sharp moving wedge, flat cut or changing skin thickness across nearby phases. |
| LM / middle-left | The tip ends directly beneath the left torso corner, making a small white under-body triangle. | Show that it is exactly the intended middle-leg tip, never a duplicated tip, detached root remnant or accidental spur. |
| RM / middle-right | Same under-body hook relationship; its inner tip produces a notch beside the lower-right corner. | Keep intended occlusion and avoid the hook appearing/disappearing over one frame. |
| LF / upper-left | The native knee highlight survives, but the outer skin ridge wraps tightly around the ball into a horizontal distal shaft. No catastrophic seam is visible in the still. | Check whether the ridge flows continuously or swings/pops as UV ownership changes. |
| RF / upper-right | Corresponding tight fold; shaft and round joint remain connected in the still. | Check contour and highlight continuity independently; do not infer the right side from the left result. |

The source itself has rounded knees. Their roundness alone is not a defect. The issue is how the knee joins its two changing volumes and whether the full creature reads as an organic fold. I do not see enough evidence in this one pose to claim a new duplicate root, a visible cutout edge, or a motion pop; those remain checks, not findings.

The exact six-joint comparison is `singleUV-all-six-joints-source-compare.png`. Individual source/target joint crops and whole-frame root-context crops retain native pixels; the sheet uses a 2× nearest enlargement only. This avoids judging a fitted thumbnail or a differently filtered source.

Findings were sent directly to the visual agent.

## Frozen HeldOut01: independent result — FAIL for finished artwork

I inspected all **21** frames on the five native green sheets: body rows 0/4/7/8 at phases 0/12/24/36 and five attack samples. I verified each actual raw PNG hash against the frozen manifest. Six suspicious cases were additionally composited at native 576 pixels on dark/light backgrounds: body 0/00, 0/12, 4/24, 7/24, 8/00 and strike09. Those are evidence samples, not a claim that every H01 frame was viewed on both extra backgrounds. No renderer was executed. Exact scope/hashes are in `H01-independent-reviewed-manifest.json`.

The strongest new visible defect is at the **roots**, separate from the improved knee coverage. In body row0 phases12/36, a pointed pink/dark remnant projects above the lower-right torso corner; a smaller pointed remnant appears beside the left corner. Other phases expose repeated dark collar boundaries around lower attachments. This is extra silhouette/duplicated-looking skin at the attachment, even where alpha is solid. See `H01-root-contexts-2x-nearest.png`, which uses the same fixed source crop and integer 2× nearest enlargement in all six panels. Its crop boundaries deliberately cut long shafts outside the root context; those diagnostic boundaries are not defects in the candidate canvas.

Row7 keeps the distal-tab and middle under-body hook issues noted above. At phase24 the upper blade tips almost meet over the front cap; the silhouettes need an intentional occlusion/crossing rule. Row8 remains an unconvincing closed posture in all four copies: the fore/hind longitudinal hook directions are opposite the observed native closed reference and short exposed tabs replace fuller folded volumes. Four identical row8 hashes are consistent with a static closed hold; they do not establish a compact walking cycle.

The open and intermediate rows preserve six main blade tips and no conspicuous full-canvas crop, but their quarters swing between very different projections. That is not proof of a motion pop: the held-out selection skips eleven intermediate phases each time. It also cannot certify consecutive-frame skin continuity, support or last→first motion. The body core and eye remain visually stable in these still samples. The sampled native-mouth strike stays recognizable; I did not find a conspicuous cutout rectangle around its mouth, but five attack samples do not certify its entry/return sequence.

No detached random extra blade was found in this H01 visual sample. The pointed **skin root remnants** above are a real finding; absence of a detached blade is not permission to pass the set. Zero bounded holes or a valid UV determinant cannot approve these visible forms.

## Separate ClosedPoseStudy direction trial

This is not H01 and did not overwrite it. I viewed the actual new `native-hook-directions.png` as a whole on green/dark/light and compared all six knee regions with the native limb source. Hashes are preserved in `closed-proposed-reviewed-hashes.json`; fixed native crops are `closed-proposed-native-{green,dark,light}.png` and `closed-proposed-six-joints-source-compare.png`.

The new fore/hind direction visibly improves folding: upper blades point inward+forward from the outer knees and lower blades inward+back. The lower blades now have readable tapered lengths rather than near-horizontal tabs. I see no conspicuous canvas cut or detached spur in this still. This is a worthwhile directional correction, not a final artistic pass.

Remaining weakness: the proximal walking shafts remain narrow and rope-like, while native Recoil03 has thicker overlapping soft masses. The middle pair hug the underside; lower knee/flesh overlaps partly disappear behind the fixed body with abrupt local skin/color boundaries. The new posture reads more intentionally folded but flatter/lighter than the native closed drawing. It needs a coherent root/soft-volume treatment and reinspection of the same silhouette, not another claim based on alpha coverage. Motion and transitions remain untested.

The proposed virtual Z values can enforce mathematical segment lengths in a chosen skeleton. They do not establish true anatomical heights/lengths from one top-down PNG, and they do not supply missing drawn soft volume.

## Native closed pose reference

`native-Recoil03-observed-chains.png` and `.json` record a separate observational 2D reference. Orange dashed links lead from inferred hips to visible outer skin-knee centers; cyan links lead to visible distal ends. Magenta circles mark the four visible pointed blade endpoints. Magenta squares on the middle pair mark termination at torso occlusion, **not** a known anatomical endpoint; their true `tip` is null in JSON. All hips and actual bone centers are uncertain.

With x right/y down, the native fore knee→tip vectors are LF(+105,−80), RF(−99,−80) pixels. Hind vectors are LR(+107,+75), RR(−106,+75). Current frozen row8 local Y points the corresponding distal segments in the opposite longitudinal directions. This supports the direction hypothesis independently of the renderer's coverage tests.

`native-Recoil03-soft-envelope-observations.png` is a separate rough annotation of projected soft-skin masses. It is not a completed muscle-authoring pass, a muscle count or an anatomical acceptance. Purple question marks retain the two lower rounded folds as **unassigned**: their presence cannot by itself establish extra muscles, joints or limbs. Middle and lower projected envelopes partly lie behind claws/torso. No physical 3D lengths, ground contacts or complete internal skeleton were inferred from this single image.

Both concrete findings and limits were sent directly to the root and visual agent. No production or painter change was made.
