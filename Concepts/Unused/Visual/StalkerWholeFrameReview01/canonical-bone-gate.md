# New canonical bone/attachment gate

2026-10-27. Independent read-only review of the newly exported C# canonical `poses.json`, not the old frozen H01 coordinates. No C#, painter, Unity state or production resource was changed. All outputs are in this unused audit folder.

The new **formal coordinate gate passes 432/432 poses**. This is an offline 2D anatomical-layout/virtual-3D-chain result. It does not pass skin, muscle drawing, physics, grounded rest, actual 3D occlusion or finished artwork. H01 remains an art FAIL.

The actual new plans are `canonical-bone-plans-row4-row8-top-side.png`, viewed before proposing envelopes. It contains no skin: row4/phase0 and row8/phase0 each have a top projection plus separate left/right side projections. The thin gray polygon only connects attachment points; it is not a torso contour or body surface. `canonical-bone-gate.json` stores the exact selected plans, source/pose/C# hashes, per-row results and comparison with the native observed closed pose.

| Check, independently recomputed | Result |
|---|---:|
| Finite unique poses, 9 rows ×48 phases | 432 |
| Maximum error against configured upper/lower 3D lengths | 1.12×10⁻⁷ units |
| Minimum source-winding product | .00062555; positive in every chain |
| Minimum fore/middle knee-Y gap | .152724 units |
| Minimum middle/hind knee-Y gap | .174000 units |
| Minimum distal projection / configured distal length | .698856 |
| Lowest bone endpoint Z | 0 |
| Fully closed coordinate variation across 48 phases | 0 |
| Fully closed fore-forward/hind-backward direction | 48/48 |

Winding is compared with the adopted **open Walk00 source chain**. This does not prove that every hidden attachment in the independently drawn native closed illustration uses identical anatomy. Positive winding also does not establish no visible skin fold, good thickness, no intersection or organic shape.

All bone centerlines stay at/above floor because each straight segment joins nonnegative-Z endpoints. A centerline check does not validate a skin surface. Rows0–4 have 3–6 toe contacts within 10⁻⁶ of Z0. Rows5–8 have none: the authored tuck raises all feet. Fully closed knees have Z≈.537–.640, toes Z≈.244–.378, while hip/body reference is Z.2. No body underside/top/support surface is specified; no grounded resting anatomy or support mechanism is claimed. The game is flat 2D and this audit adds no physics asset or runtime IK.

The fore/hind signs need not hold throughout every open gait phase. They are checked as the closed-pose requirement; rows5–8 also happen to retain those signs throughout the exported cycle. The per-row counts remain explicit in JSON rather than disguising open-gait directions as closed-pose failures.

## Comparison with the native closed observations

The comparison uses one uniform **PPU384 + eye crop anchor(174,166)**. No per-limb fit or point adjustment is applied. Fore/hind knee and pointed-tip projections agree with the previously marked native pixels within approximately 0–4 pixels. Middle knee centers differ by roughly 4–6 pixels. Their true tips are hidden in the painting, so they cannot be certified against the canonical endpoints. In the native drawing, the visible middle bone fragment runs toward the front/torso; the canonical full toe is placed behind/below its knee under the body in top view. That remains a pose/occlusion choice to assess in the complete image, not a proven native tip match.

The marked native hips were explicitly inferred. They substantially differ from current canonical attachments, which are kept under the core. This is not a valid reason to force the canonical hips to uncertain painted landmarks or to declare the original inferred hips true anatomy. Fore/hind endpoint agreement is useful authoring evidence; it is not complete skeletal acceptance of a PNG.

## Separate envelope proposal

After viewing the bone-only plan, I created `canonical-soft-envelope-proposal-row4-row8.png` and `canonical-soft-envelope-proposal.json`. They are separate from every source concept and contain **draft 2D projected soft-envelope parameters**, not skin or an implemented painter change.

The source contract remains six unchanged chains with three existing functional groups each: proximal actuator, joint stabilizer and distal tendon sleeve, 18 groups total. Attachment IDs and bone positions remain fixed. Closed proximal peaks are deliberately fuller than row4 to address the thin walking-shaft result. Their proposed radii are authoring choices, not measurements of hidden muscle anatomy. Diagnostic group outlines must be hidden by continuous skin in final art.

Root ownership must be evaluated in the actual full artwork: one body-owned skin cover, no repeated collar, hard circular patch, floating point or hole. Mid/hind tip occlusion is a 2D composition decision to inspect in pixels; this proposal does not invent a body-top height to justify it. Envelope width does not supply an automatic muscle/skin/art PASS. The next comparison must show versioned whole controls for source ownership and then volume on green/light/dark before any mass export.
